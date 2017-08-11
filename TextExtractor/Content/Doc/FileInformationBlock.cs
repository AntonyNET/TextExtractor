namespace TextExtractor.Content.Doc
{
    using Extensions;

    internal class FileInformationBlock
    {
        public FileInformationBlock(byte[] wdStream)
        {
            var tableNumber = wdStream.ReadInt16(0x000A);
            var ccpText = wdStream.ReadInt32(0x004C);
            var ccpFtn = wdStream.ReadInt32(0x0050);
            var ccpHdd = wdStream.ReadInt32(0x0054);
            var ccpMcr = wdStream.ReadInt32(0x0058);
            var ccpAtn = wdStream.ReadInt32(0x005C);
            var ccpEdn = wdStream.ReadInt32(0x0060);
            var ccpTxbx = wdStream.ReadInt32(0x0064);
            var ccpHdrTxbx = wdStream.ReadInt32(0x0068);

            ClxOffset = wdStream.ReadInt32(0x01A2);
            ClxSize = wdStream.ReadInt32(0x01A6);

            TableNumber = (tableNumber & 0x0200) == 0x0200 ? 1 : 0;
            LastCharPosition = ccpFtn + ccpHdd + ccpMcr + ccpAtn + ccpEdn + ccpTxbx + ccpHdrTxbx;
            LastCharPosition += (LastCharPosition != 0 ? 1 : 0) + ccpText;
        }

        public int LastCharPosition { get; set; }

        public int ClxSize { get; set; }

        public int ClxOffset { get; set; }

        private int TableNumber { get; set; }

        public string GetTableName()
        {
            return $"{TableNumber}Table";
        }
    }
}