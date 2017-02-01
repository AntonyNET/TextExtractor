namespace TextExtractor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Contract;
    using Extractor;
    using Extractor.Archive;
    using Extractor.Doc;
    using Extractor.Rtf;

    public class DocumentExtractor : IDocumentExtractor
    {
        private readonly ArchiveExtractorFactory _archiveExtractorFactory = new ArchiveExtractorFactory();

        private readonly Dictionary<string, Func<IContentExtractor>> _fileExtensions = new Dictionary<string, Func<IContentExtractor>>
                                                                                           {
                                                                                               {".xls", () => new ExcelExtractor()},
                                                                                               {".xlsx", () => new ExcelExtractor()},
                                                                                               {".docx", () => new CompositeExtractor(new DocxExtractor())},
                                                                                               {".doc", () => new CompositeExtractor(new DocExtractor(),
                                                                                                                                     new HtmlExtractor())},
                                                                                               {".txt", () => new CompositeExtractor(new TxtExtractor())},
                                                                                               {".rtf", () => new CompositeExtractor(new RtfExtractor(),
                                                                                                                                     new DocExtractor())},
                                                                                               {".odt", () => new CompositeExtractor(new OdtExtractor())},
                                                                                               {".htm", () => new HtmlExtractor()},
                                                                                               {".html", () => new HtmlExtractor()},
                                                                                               {".pdf", () => new PdfExtractor()},
                                                                                           };

        public IEnumerable<string> AllowedExtensions => _fileExtensions.Keys.Union(_archiveExtractorFactory.SupportedExtensions);

        public IEnumerable<RawDocument> GetArchivedFiles(RawDocument downloadFile)
        {
            if (IsArchive(downloadFile.FileName) == false)
                throw new InvalidOperationException("Тип архива не поддерживается");

            return GetExtractedFiles(downloadFile);
        }

        public string GetContent(RawDocument downloadFile)
        {
            using (var stream = new MemoryStream(downloadFile.Data))
            {
                var extension = GetExtension(downloadFile.FileName);

                if (_fileExtensions.ContainsKey(extension) == false)
                    return string.Empty;

                var content = _fileExtensions[extension]().Extract(stream);

                return RemoveUrls(content);
            }
        }

        public string RemoveUrls(string content)
        {
            return Regex.Replace(content, @"((ftp|ftps|http|https):\/\/)?(www\.)?([a-z0-9\-]+?\.)+[a-z]{2,5} ((\/[a-z0-9\-%]+)+)?(\?( [a-zA-Z0-9\:\/\?#\[\]@!\$&'\(\)\*\+,;=\-._~]+?=[a-zA-Z0-9\:\/\?#\[\]@!\$&'\(\)\*\+,;=\-._~]+&?)+)?", " ", RegexOptions.IgnorePatternWhitespace);
        }

        public bool IsArchive(string fileName)
        {
            return _archiveExtractorFactory.IsSupported(GetExtension(fileName));
        }

        private IEnumerable<RawDocument> GetExtractedFiles(RawDocument document)
        {
            var archiveExtractor = _archiveExtractorFactory.GetExtractor(document.FileName);

            IList<RawDocument> extractedFiles;

            using (var stream = new MemoryStream(document.Data))
                extractedFiles = archiveExtractor.Extract(stream);

            if (extractedFiles == null)
                return null;

            foreach (var archive in extractedFiles.Where(x => IsArchive(x.FileName)).ToList())
            {
                extractedFiles.Remove(archive);
                var rawDocuments = GetExtractedFiles(archive);

                if (rawDocuments != null)
                    foreach (var rawDocument in rawDocuments)
                        extractedFiles.Add(rawDocument);
            }

            return extractedFiles;
        }

        private static string GetExtension(string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName);

                return extension?.ToLowerInvariant();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Can't define extension in file {fileName}", ex);
            }
        }
    }
}