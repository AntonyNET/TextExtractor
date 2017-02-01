namespace TextExtractor.Extractor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Contract;

    public class CompositeExtractor : IContentExtractor
    {
        private readonly IEnumerable<IContentExtractor> _extractors;

        public CompositeExtractor(params IContentExtractor[] extractors)
        {
            if (extractors == null)
                throw new ArgumentNullException("extractors");

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