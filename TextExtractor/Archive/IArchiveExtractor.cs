namespace TextExtractor.Archive
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    ///     Распаковщик архивов
    /// </summary>
    public interface IArchiveExtractor
    {
        /// <summary>
        ///     Распаковать архив
        /// </summary>
        /// <param name="stream">поток файла архива</param>
        /// <returns>список файлов архива</returns>
        IList<RawDocument> Extract(Stream stream);
    }
}