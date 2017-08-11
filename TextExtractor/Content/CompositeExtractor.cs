namespace TextExtractor.Content
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    ///     Составной экстрактор. Пытается получить текст всеми переданными экстракторами.
    ///     Останаливается при первой успешной попытке.
    ///     Если не один экстрактор не смог выделить текст, то выбрасывает исключение
    /// </summary>
    public class CompositeExtractor : IContentExtractor
    {
        private readonly IEnumerable<IContentExtractor> _extractors;

        /// <summary>
        ///     Важен порядок переданных эстракторов. Экстракторы будут вызываться в переданном порядке.
        /// </summary>
        /// <param name="extractors"></param>
        public CompositeExtractor(params IContentExtractor[] extractors)
        {
            if (extractors == null)
                throw new ArgumentNullException(nameof(extractors));

            _extractors = extractors;
        }

        public string Extract(Stream stream)
        {
            foreach (var contentExtractor in _extractors)
            {
                stream.Position = 0;

                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);

                        memoryStream.Position = 0;

                        var result = contentExtractor.Extract(memoryStream);

                        return result;
                    }
                }
                catch
                {
                }
            }

            throw new InvalidOperationException("Noone extractor cant extract text");
        }
    }
}