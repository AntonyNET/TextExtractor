# TextExtractor

Библиотека для получения текста из файлов.
Поддреживаемые форматы:
- .doc, .docx
- .xlsx
- .pdf
- .odt
- .rtf
- .html, .htm 
- .txt

Использование:

		var documentExtractor = DocumentExtractor.Default();
            var content = string.Empty;
            var fileName = @"C:\1.rar";
            
            if (documentExtractor.IsArchive(fileName))
            {
                var archivedFiles = documentExtractor.GetArchivedFiles(new RawDocument(fileName, File.ReadAllBytes(fileName)));
                var contentBuilder = new StringBuilder();

                foreach (var archivedFile in archivedFiles)
                    contentBuilder.AppendLine(documentExtractor.GetContent(archivedFile));

                content = contentBuilder.ToString();
            }
            else
            {
                content = documentExtractor.GetContent(new RawDocument(fileName, File.ReadAllBytes(fileName)));
            }

            Console.WriteLine("Extracted content: {0}", content);
			
			
Пока только начал работать над проэктом и он очень сырой.
Буду очень рад любым вопросам, предложениям, помощи в работе библиотеки
