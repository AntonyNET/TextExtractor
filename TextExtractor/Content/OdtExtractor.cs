namespace TextExtractor.Content
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    ///     Экстрактор текста из файлов .odt
    /// </summary>
    public class OdtExtractor : IContentExtractor
    {
        private const string ContentFileName = "content.xml";

        public string Extract(Stream stream)
        {
            using (var zipArchive = new ZipArchive(stream))
            {
                var contentEntry = zipArchive.Entries.SingleOrDefault(x => x.Name == ContentFileName);

                if (contentEntry == null)
                    throw new InvalidOperationException("Can not find content.xml in ODT file");

                using (var contentEntryStream = contentEntry.Open())
                {
                    var document = XDocument.Load(contentEntryStream);

                    return document.Root?.Value;
                }
            }
        }
    }
}