namespace TextExtractor.Content
{
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    ///     Экстрактор текста из файлов .docx
    /// </summary>
    public class DocxExtractor : IContentExtractor
    {
        private const string DocumentEntryName = @"word/document.xml";

        public string Extract(Stream stream)
        {
            using (var zipArchive = new ZipArchive(stream))
            {
                var zipEntry = zipArchive.Entries.SingleOrDefault(x => x.FullName == DocumentEntryName);

                if (zipEntry == null)
                    return null;

                using (var zipEntryStream = zipEntry.Open())
                {
                    var document = XDocument.Load(zipEntryStream);

                    return document.Root?.Value;
                }
            }
        }
    }
}