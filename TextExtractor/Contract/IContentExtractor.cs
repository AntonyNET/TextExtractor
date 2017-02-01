namespace TextExtractor.Contract
{
    using System.IO;

    public interface IContentExtractor
    {
        string Extract(Stream stream);
    }
}
