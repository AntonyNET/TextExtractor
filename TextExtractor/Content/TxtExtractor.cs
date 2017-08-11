namespace TextExtractor.Content
{
    using System.IO;

    /// <summary>
    ///     Экстрактор текста из файлов .txt
    /// </summary>
    public class TxtExtractor : IContentExtractor
    {
        public string Extract(Stream stream)
        {
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}