namespace TextExtractor.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Content.Doc;
    using Moq;
    using Newtonsoft.Json;
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
            _documentExtractor = new DocumentExtractor();
            _contentExtractor = new Mock<IContentExtractor>();
            _contentExtractor2 = new Mock<IContentExtractor>();
        }

        [Test]
        public void MustHaveNotExtensionsByDefault()
        {
            Assert.IsEmpty(_documentExtractor.AllowedExtensions);
        }

        [Test]
        public void MustThrowExceptionIfArgumentsInvalid()
        {
            Assert.Throws<ArgumentException>(() => _documentExtractor.AddContentExtractor(string.Empty, _contentExtractor.Object));
            Assert.Throws<ArgumentException>(() => _documentExtractor.AddContentExtractor("xxx", _contentExtractor.Object));
            Assert.Throws<NullReferenceException>(() => _documentExtractor.AddContentExtractor(".xxx", null));
        }

        [Test]
        public void MustContainsExtensionAndGetContentIfAddExtractor()
        {
            var extension = ".xxx";

            _contentExtractor.Setup(x => x.Extract(It.IsAny<Stream>())).Returns(() => "some content");

            _documentExtractor.AddContentExtractor(extension, _contentExtractor.Object);
            _documentExtractor.GetContent(new RawDocument {FileName = extension, Data = new byte[0]});

            Assert.True(_documentExtractor.AllowedExtensions.Contains(extension));
            _contentExtractor.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Once);
        }

        [Test]
        public void ReturnEmtryStringIfExtentionNotSupported()
        {
            var extension = ".xxx";

            _contentExtractor.Setup(x => x.Extract(It.IsAny<Stream>())).Returns(() => "some content");

            _documentExtractor.AddContentExtractor(extension, _contentExtractor.Object);

            var content = _documentExtractor.GetContent(new RawDocument {FileName = ".yyy", Data = new byte[0]});

            _contentExtractor.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Never);
            Assert.AreEqual(string.Empty, content);
        }

        [Test]
        public void MustUseFirstSuccessfulExtractor()
        {
            var extension = ".xxx";

            _contentExtractor.Setup(x => x.Extract(It.IsAny<Stream>())).Returns(() => "some content");

            _documentExtractor.AddContentExtractor(extension, _contentExtractor.Object);
            _documentExtractor.AddContentExtractor(extension, _contentExtractor2.Object);

            var content = _documentExtractor.GetContent(new RawDocument {FileName = extension, Data = new byte[0]});

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

            var content = _documentExtractor.GetContent(new RawDocument {FileName = extension, Data = new byte[0]});

            _contentExtractor.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Once);
            _contentExtractor2.Verify(x => x.Extract(It.IsAny<Stream>()), Times.Once);
            Assert.AreEqual("some content", content);

        }

        [Test]
        public void asd()
        {
            var n = 10000;
            var a = Enumerable.Range(0, n).ToArray();

            var eq = 0;
            var com = 0;

            for (var i = 0; i < n; i++)
            {
                com++;
                if (i%2 != 0)
                    continue;

                eq++;
                var j = 1;

                com++;
                while (j<n)
                {
                    com++;
                    if (a[j] == a[i])
                    {
                        break;
                    }
                    else
                    {
                        eq++;
                        j *= 2;
                        com++;
                    }
                }
            }

            Console.WriteLine(com + eq);
                        Console.WriteLine(true);
        }


        [Test]
        public void asdasd(Queue<int> q)
        {
            var l = new int[1];
            var i = 0;

            while (q.Count != 0)
            {
                if (i == l.Length)
                {
                    var t = new int[l.Length*2];
                    Array.Copy(l,t,l.Length);
                    l = t;
                }

                l[i] = q.Dequeue();
                i++;
            }
        }

        [Test]
        public void qwe()
        {
            for (int i = 0; i < 50; i++)
            {
                var lastdigit = i%10;

                if (lastdigit == 0 && lastdigit >= 5)
                {
                    Console.WriteLine($"{i} рублей");
                    continue;
                }

                if (lastdigit == 1)
                {
                    Console.WriteLine($"{i} рубль");
                    continue;
                }

                if (Math.Floor((decimal)i/10)%10 == 1)
                {
                    Console.WriteLine($"{i} рублей");
                    continue;
                }

                Console.WriteLine($"{i} рубля");
            }
        }

        [Test]
        public void asdasd()
        {
            var o = JsonConvert.DeserializeObject<Person[]>(File.ReadAllText(@"C:\Projects\TextExtractor\TextExtractor\TextExtractor.Tests\bin\Debug\1.txt"));

            foreach (var person in o)
                person.RealPrice = decimal.Parse(person.Price.Trim('$'));

           o = o.GroupBy(x => x.PersonId)
             .Select(z => new Person
                              {
                                  PersonId = z.Key,
                                  RealPrice = z.Sum(a => a.RealPrice)
                              })
             .ToArray();

            var max = o.Max(x => x.RealPrice);


            o = o.Where(x => x.RealPrice == max).ToArray();

            Console.WriteLine(o);
        }

        private class Person
        {
            public int PersonId { get; set; }
            public string Price { get; set; }
            public decimal RealPrice { get; set; }
        }
    }
}