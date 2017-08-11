namespace TextExtractor.Content.Doc.Extensions
{
    using System;

    /// <summary>
    ///     Расширения для работы с массивом байтов
    /// </summary>
    internal static class ByteArrayExtensions
    {
        public static short ReadInt16(this byte[] bytes, int position)
        {
            return BitConverter.ToInt16(bytes, position);
        }

        public static int ReadInt32(this byte[] bytes, int position)
        {
            return BitConverter.ToInt32(bytes, position);
        }

        public static long ReadInt64(this byte[] bytes, int position)
        {
            return BitConverter.ToInt64(bytes, position);
        }

        public static uint ReadUInt32(this byte[] bytes, int position)
        {
            return BitConverter.ToUInt32(bytes, position);
        }

        public static byte[] SkipAndTake(this byte[] bytes, int skipCount, int takeCount)
        {
            if (skipCount < 0)
                skipCount = 0;

            if (skipCount > bytes.Length)
                return new byte[0];

            if (skipCount + takeCount > bytes.Length)
                takeCount = bytes.Length - skipCount;

            var takeBytes = new byte[takeCount];

            for (var i = 0; i < takeBytes.Length; i++)
                takeBytes[i] = bytes[skipCount + i];

            return takeBytes;
        }
    }
}