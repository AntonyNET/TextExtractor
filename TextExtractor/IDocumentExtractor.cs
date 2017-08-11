namespace TextExtractor
{
    using System.Collections.Generic;

    public interface IDocumentExtractor
    {
        IEnumerable<string> AllowedExtensions { get; }
        IEnumerable<RawDocument> GetArchivedFiles(RawDocument downloadFile);
        string GetContent(RawDocument downloadFile);
        bool IsArchive(string fileName);

        void AddContentExtractor(string extension, IContentExtractor newExtractor);
    }
}