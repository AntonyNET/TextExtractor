namespace TextExtractor.Content
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// 
    /// </summary>
    public class PdfExtractor : IContentExtractor
    {
        /// <summary>
        ///  Extracts text form the providing stream assuming it as PDF format. 
        ///  Throws FormatException if the format is invalid
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public string Extract(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return ParseWithITextSharp(stream);
        }

        private string ParseWithITextSharp(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            iTextSharp.text.pdf.PdfReader rdr = null;

            try
            {
                rdr =  new iTextSharp.text.pdf.PdfReader(stream);
                var sb = new StringBuilder();
                for (var i = 1; i <= rdr.NumberOfPages; i++)
                    sb.Append(iTextSharp.text.pdf.parser.PdfTextExtractor.GetTextFromPage(rdr, i));
                return sb.ToString();
            }
            finally
            {
                rdr?.Close();
            }
        }
    }
}
