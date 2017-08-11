namespace TextExtractor.Content
{
    using System.IO;

    public class TxtExtractor : IContentExtractor
    {
        public string Extract(Stream stream)
        {
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}