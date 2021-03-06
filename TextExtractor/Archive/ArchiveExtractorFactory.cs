﻿namespace TextExtractor.Archive
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class ArchiveExtractorFactory : IArchiveExtractorFactory
    {
        private readonly Dictionary<string, Func<IArchiveExtractor>> _archiveExtractors = new Dictionary<string, Func<IArchiveExtractor>>
                                                                                              {
                                                                                                  {".zip", () => new ZipExtractor()},
                                                                                                  {".rar", () => new RarExtractor()}
                                                                                              };

        public IEnumerable<string> SupportedExtensions => _archiveExtractors.Keys;

        public bool IsSupported(string archiveExtension)
        {
            return _archiveExtractors.ContainsKey(archiveExtension);
        }

        public IArchiveExtractor GetExtractor(string archiveFileName)
        {
            var extension = Path.GetExtension(archiveFileName)?.ToLowerInvariant();

            if (extension == null || _archiveExtractors.ContainsKey(extension) == false)
                throw new InvalidOperationException($"Can not found extractor for archive \"{archiveFileName}\"");

            return _archiveExtractors[extension]();
        }
    }
}
