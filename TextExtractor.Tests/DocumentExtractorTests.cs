namespace TextExtractor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Archive;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class DocumentExtractorTests
    {
        private DocumentExtractor _documentExtractor;
        private Mock<IContentExtractor> _contentExtractor;
        private Mock<IContentExtractor> _contentExtractor2;

        [SetUp]
        public void Setup()
        {
            var archiveFactory = new Mock<IArchiveExtractorFactory>();

            _documentExtractor = new DocumentExtractor(archiveFactory.Object);
            _contentExtractor = new Mock<IContentExtractor>();
            _contentExtractor2 = new Mock<IContentExtractor>();
        }

        [Test]
        public void AllowedExtensions_MustHaveNotExtensionsByDefault()
        {
            Assert.IsEmpty(_documentExtractor.AllowedExtensions);
        }

        [Test]
        public void AddContentExtractor_MustThrowExceptionIfArgumentsInvalid()
        {
            Assert.Throws<ArgumentException>(() => _documentExtractor.AddContentExtractor(string.Empty, _contentExtractor.Object));
            Assert.Throws<ArgumentException>(() => _documentExtractor.AddContentExtractor("xxx", _contentExtractor.Object));
            Assert.Throws<NullReferenceException>(() => _documentExtractor.AddContentExtractor(".xxx", null));
        }

        [Test]
        public void AddContentExtractor_MustContainsExtensionAndGetContentIfAddExtractor()
        {
            var extension = ".xxx";

            _contentExtractor.Setup(x => x.Extract(It.IsAny<Stream>())).Returns(() => "some content");

            _documentExtractor.AddContentExtractor(extension, _contentExtractor.Object);
            _documentExtractor.GetContent(new RawDocument (extension, new byte[0]));

            Assert.True(_documentExtractor.AllowedExtensions.Contains(extension));
            _contentExtractor.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Once);
        }

        [Test]
        public void GetContent_MustThrowNotSupportedExceptionIfExtentionNotSupported()
        {
            var extension = ".xxx";

            _contentExtractor.Setup(x => x.Extract(It.IsAny<Stream>())).Returns(() => "some content");

            _documentExtractor.AddContentExtractor(extension, _contentExtractor.Object);

            Assert.Throws<NotSupportedException>(() => _documentExtractor.GetContent(new RawDocument(".yyy", new byte[0])));
            _contentExtractor.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Never);
        }

        [Test]
        public void MustUseFirstSuccessfulExtractor()
        {
            var extension = ".xxx";

            _contentExtractor.Setup(x => x.Extract(It.IsAny<Stream>())).Returns(() => "some content");

            _documentExtractor.AddContentExtractor(extension, _contentExtractor.Object);
            _documentExtractor.AddContentExtractor(extension, _contentExtractor2.Object);

            var content = _documentExtractor.GetContent(new RawDocument (extension, new byte[0]));

            _contentExtractor.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Once);
            _contentExtractor2.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Never);
            Assert.AreEqual("some content", content);
        }

        [Test]
        public void MustUseNextExtractorIfFirstCantExtract()
        {
            var extension = ".xxx";

            _contentExtractor.Setup(x => x.Extract(It.IsAny<Stream>())).Throws(new Exception());
            _contentExtractor2.Setup(x => x.Extract(It.IsAny<Stream>())).Returns(() => "some content");

            _documentExtractor.AddContentExtractor(extension, _contentExtractor.Object);
            _documentExtractor.AddContentExtractor(extension, _contentExtractor2.Object);

            var content = _documentExtractor.GetContent(new RawDocument (extension, new byte[0]));

            _contentExtractor.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Once);
            _contentExtractor2.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Once);
            Assert.AreEqual("some content", content);

        }

        [Test]
        public void SampleOfWork()
        {
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
        }
    }
}