namespace TextExtractor.Extractor.Doc
{
    internal class FatDirectory
    {
        public const string RootDirectoryName = "Root Entry";
        public const string WordDocumentDirectoryName = "WordDocument";

        public string Name { get; set; }
        public byte Type { get; set; }
        public byte Color { get; set; }
        public int Left { get; set; }
        public int Rigth { get; set; }
        public int Child { get; set; }
        public int Offset { get; set; }
        public long Size { get; set; }
    }
}