namespace TextExtractor.Content.Rtf
{
    internal class RtfWord
    {
        public RtfWord(WordType wordType)
        {
            Text = string.Empty;
            WordType = wordType;
        }

        public RtfWord(string text)
        {
            Text = text;
            WordType = WordType.Word;
        }

        public int Offset { get; set; }
        public WordType WordType { get; set; }
        public string Text { get; set; }
    }
}