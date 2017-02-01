namespace TextExtractor.Extractor.Doc
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using Contract;
    using Extensions;

    public class DocExtractor : CBFExtractor, IContentExtractor
    {
        public string Extract(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                data = memoryStream.ToArray();
            }

            base.Extract();

            var wordDocumentDirectoryContent = GetDirectoryContentByName(FatDirectory.WordDocumentDirectoryName);
            var fileInformationBlock = new FileInformationBlock(wordDocumentDirectoryContent);
            var pieceTableContent = GetPieceTableContent(fileInformationBlock);

            var i = 0;

            var charPositions = GetCharPositions(pieceTableContent, fileInformationBlock, ref i);
            var pieceDescriptors = GetPieceDescriptors(pieceTableContent, i);

            var text = new StringBuilder();

            for (i = 0; i < pieceDescriptors.Count; i++)
            {
                // Получаем слово со смещением и флагом компрессии
                var fcValue = pieceDescriptors[i].ReadInt32(2);

                var isANSI = (fcValue & 0x40000000) == 0x40000000;
                var textOffset = fcValue & 0x3FFFFFFF;

                var textSize = charPositions[i + 1] - charPositions[i];

                if (isANSI)
                    textOffset /= 2;
                else
                    textSize *= 2;

                var part = wordDocumentDirectoryContent.SkipAndTake(textOffset, textSize);

                text.Append(isANSI
                                ? Encoding.Default.GetString(part)
                                : UnicodeToUtf8(part));
            }

            return WebUtility.HtmlDecode(text.ToString());
        }

        private IList<byte[]> GetPieceDescriptors(byte[] pieceTableContent, int offset)
        {
            var pieceDescriptors = new List<byte[]>();
            var i = offset + 4;

            while (pieceTableContent.Count() - i > 8)
            {
                pieceDescriptors.Add(pieceTableContent.SkipAndTake(i, 8));

                i += 8;
            }

            if (pieceTableContent.Count() - i > 0)
                pieceDescriptors.Add(pieceTableContent.SkipAndTake(i, pieceTableContent.Count() - i));

            return pieceDescriptors;
        } 

        private IList<int> GetCharPositions(byte[] pieceTableContent, FileInformationBlock fileInformationBlock, ref int offset)
        {
            var charPositions = new List<int>();
            var charPosition = pieceTableContent.ReadInt32(offset);

            while (charPosition != fileInformationBlock.LastCharPosition)
            {
                charPositions.Add(charPosition);
                offset += 4;
                charPosition = pieceTableContent.ReadInt32(offset);
            }

            charPositions.Add(charPosition);

            return charPositions;
        }

        private byte[] GetPieceTableContent(FileInformationBlock fileInformationBlock)
        {
            const byte pieceTableStarting = 0x02;

            var tableDirectoryContent = GetDirectoryContentByName(fileInformationBlock.GetTableName());
            var clx = tableDirectoryContent.SkipAndTake(fileInformationBlock.ClxOffset, fileInformationBlock.ClxSize);

            var offset = 0;
            byte[] pieceTableContent = null;

            while (Array.IndexOf(clx,pieceTableStarting, offset) != -1)
            {
                var i = Array.IndexOf(clx, pieceTableStarting, offset);

                var pieceTableSize = clx.ReadInt32(i + 1);
                pieceTableContent = clx.Skip(i + 5).ToArray();

                if (pieceTableContent.Length == pieceTableSize)
                    break;

                offset = i + 1;
            }

            return pieceTableContent;
        }

        private string UnicodeToUtf8(byte[] text)
        {
            var result = new StringBuilder();

            for (var i = 0; i < text.Length; i += 2)
            {
                var cd = text.SkipAndTake(i, 2);

                // Если верхний байт нулевой, то перед нами ANSI
                if (cd[1] == 0)
                {
                    // В случае, если ASCII-значение нижнего байта выше 32, то пишем как есть.
                    if (cd[0] >= 32)
                        result.Append((char) cd[0]);

                    // В противном случае проверяем символы на внедрћнные команды (список можно
                    // дополнить и пополнить).
                    switch ((int) cd[0])
                    {
                        case 0x0D:
                        case 0x07:
                            result.Append("\n");
                            break;
                        case 0x08:
                        case 0x01:
                            result.Append("");
                            break;
                        case 0x13:
                            //HYPER13
                            result.Append("");
                            break;
                        case 0x14:
                            //HYPER14
                            result.Append("");
                            break;
                        case 0x15:
                            //HYPER15
                            result.Append("");
                            break;
                    }
                }
                else // Иначе преобразовываем в HTML entity
                {
                    result.Append("&#x");
                    result.Append(cd.ReadInt16(0).ToString("x"));
                    result.Append(";");
                }
            }

            return result.ToString();
        }
    }
}