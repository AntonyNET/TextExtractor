namespace TextExtractor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Archive;
    using Content;
    using Content.Doc;
    using Content.Rtf;

    public class DocumentExtractor : IDocumentExtractor
    {
        private readonly ArchiveExtractorFactory _archiveExtractorFactory = new ArchiveExtractorFactory();

        private readonly Dictionary<string, ICollection<IContentExtractor>> _fileExtensions = new Dictionary<string, ICollection<IContentExtractor>>
                                                                                                  {
                                                                                                          {".xls", new List<IContentExtractor> {new ExcelExtractor()}},
                                                                                                          {".xlsx", new List<IContentExtractor> {new ExcelExtractor()}},
                                                                                                          {".docx", new List<IContentExtractor> {new DocxExtractor()}},
                                                                                                          {
                                                                                                              ".doc", new List<IContentExtractor>
                                                                                                                          {
                                                                                                                              new DocExtractor(),
                                                                                                                              new HtmlExtractor()
                                                                                                                          }
                                                                                                          },
                                                                                                          {".txt", new List<IContentExtractor> {new TxtExtractor()}},
                                                                                                          {
                                                                                                              ".rtf", new List<IContentExtractor>
                                                                                                                          {
                                                                                                                              new RtfExtractor(),
                                                                                                                              new DocExtractor()
                                                                                                                          }
                                                                                                          },
                                                                                                          {".odt", new List<IContentExtractor> {new OdtExtractor()}},
                                                                                                          {".htm", new List<IContentExtractor> {new HtmlExtractor()}},
                                                                                                          {".html", new List<IContentExtractor> {new HtmlExtractor()}},
                                                                                                          {".pdf", new List<IContentExtractor> {new PdfExtractor()}},
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
                    throw new NotSupportedException(extension);

                foreach (var contentExtractor in _fileExtensions[extension])
                {
                    try
                    {
                        return contentExtractor.Extract(stream);
                    }
                    catch (Exception)
                    {
                    }
                }

                throw new Exception("Не удалось получить текст из файла");
            }
        }

        public bool IsArchive(string fileName)
        {
            return _archiveExtractorFactory.IsSupported(GetExtension(fileName));
        }

        public void AddContentExtractor(string extension, IContentExtractor newExtractor)
        {
            if (string.IsNullOrEmpty(extension) || extension.StartsWith(".") == false)
                throw new ArgumentException(nameof(extension));

            if (newExtractor == null)
                throw new NullReferenceException(nameof(newExtractor));

            if (_fileExtensions.ContainsKey(extension) == false)
                _fileExtensions.Add(extension, new List<IContentExtractor>());

            _fileExtensions[extension].Add(newExtractor);
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
                return Path.GetExtension(fileName)?.ToLowerInvariant();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Can't define extension for file {fileName}", ex);
            }
        }
    }
}