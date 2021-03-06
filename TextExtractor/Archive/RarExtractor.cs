﻿namespace TextExtractor.Archive
{
    using System.Collections.Generic;
    using System.IO;
    using NUnrar.Archive;

    /// <summary>
    ///     Распаковщик архивов .rar
    /// </summary>
    public class RarExtractor : IArchiveExtractor
    {
        public IList<RawDocument> Extract(Stream stream)
        {
            var archivedDocuments = new List<RawDocument>();
            var archive = RarArchive.Open(stream);

            foreach (var rarEntry in archive.Entries)
            {
                if (archive.IsMultipartVolume())
                    return null;

                if (rarEntry.IsDirectory)
                    continue;

                if (rarEntry.IsEncrypted)
                    return null;

                using (var entryStream = new MemoryStream())
                {
                    rarEntry.WriteTo(entryStream);
                    entryStream.Position = 0;

                    var document = new RawDocument(Path.GetFileName(rarEntry.FilePath), entryStream.ToArray());

                    archivedDocuments.Add(document);
                }
            }

            return archivedDocuments;
        }
    }
}