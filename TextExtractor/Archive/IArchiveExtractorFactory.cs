namespace TextExtractor.Archive
{
    using System.Collections.Generic;

    public interface IArchiveExtractorFactory
    {
        IEnumerable<string> SupportedExtensions { get; }
        bool IsSupported(string archiveExtension);
        IArchiveExtractor GetExtractor(string archiveFileName);
    }
}