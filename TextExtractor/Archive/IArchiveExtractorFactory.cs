namespace TextExtractor.Archive
{
    using System.Collections.Generic;

    /// <summary>
    ///     Фабрика распаковщиков архивов
    /// </summary>
    public interface IArchiveExtractorFactory
    {
        /// <summary>
        ///     Поддверживаемые расширения архивов
        /// </summary>
        IEnumerable<string> SupportedExtensions { get; }

        /// <summary>
        ///     Проверяет поддерживается ли расширение
        /// </summary>
        /// <param name="archiveExtension"></param>
        /// <returns></returns>
        bool IsSupported(string archiveExtension);

        /// <summary>
        ///     Получает распаковщик по имени файла
        /// </summary>
        /// <param name="archiveFileName"></param>
        /// <returns>распаковщик архива</returns>
        IArchiveExtractor GetExtractor(string archiveFileName);
    }
}