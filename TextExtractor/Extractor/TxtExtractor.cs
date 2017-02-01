namespace TextExtractor.Extractor
{
    using System.IO;
    using Contract;

    public class TxtExtractor : IContentExtractor
    {
        public string Extract(Stream stream)
        {
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}