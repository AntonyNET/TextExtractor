namespace TextExtractor
{
    using System.Collections.Generic;

    /// <summary>
    ///     Сервис для получения текста из файла
    /// </summary>
    public interface IDocumentExtractor
    {
        /// <summary>
        ///     Поддерживаемые расширения
        /// </summary>
        IEnumerable<string> AllowedExtensions { get; }

        /// <summary>
        ///     Получение файлов из архива
        /// </summary>
        /// <param name="downloadFile"></param>
        /// <returns></returns>
        IEnumerable<RawDocument> GetArchivedFiles(RawDocument downloadFile);

        /// <summary>
        ///     Получение текста из файла
        /// </summary>
        /// <param name="downloadFile"></param>
        /// <returns></returns>
        string GetContent(RawDocument downloadFile);

        /// <summary>
        ///     Получение текста из файла
        /// </summary>
        /// <param name="filePath">путь до файла, из которого надо извлечь текст</param>
        /// <returns></returns>
        string GetContent(string filePath);

        /// <summary>
        ///     Определяет является ли файл архивом
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        bool IsArchive(string fileName);

        /// <summary>
        ///     Добавляет новый экстрактор текста в сервис
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="newExtractor"></param>
        void AddContentExtractor(string extension, IContentExtractor newExtractor);
    }
}