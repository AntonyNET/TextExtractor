namespace TextExtractor
{
    public class RawDocument
    {
        public RawDocument(string fileName, byte[] data)
        {
            FileName = fileName;
            Data = data;
        }

        public string FileName { get; set; }
        public byte[] Data { get; set; }
    }
}