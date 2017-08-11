namespace TextExtractor.Archive
{
    using System.Collections.Generic;
    using System.IO;

    public interface IArchiveExtractor
    {
        IList<RawDocument> Extract(Stream stream);
    }
}