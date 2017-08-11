namespace TextExtractor.Archive
{
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;

    /// <summary>
    ///     Распаковщик архивов .zip
    /// </summary>
    public class ZipExtractor : IArchiveExtractor
    {
        public IList<RawDocument> Extract(Stream stream)
        {
            var archivedDocuments = new List<RawDocument>();
            
            using (var zip = new ZipArchive(stream))
            {
                foreach (var zipEntry in zip.Entries)
                {
                    if (zipEntry == null)
                        continue;

                    using (var entryStream = new MemoryStream())
                    {
                        using (var zipEntryStream = zipEntry.Open())
                        {
                            zipEntryStream.CopyTo(entryStream, 4096);

                            entryStream.Position = 0;

                            var document = new RawDocument(zipEntry.Name, entryStream.ToArray());

                            archivedDocuments.Add(document);
                        }
                    }
                }
            }

            return archivedDocuments;
        }
    }
}