using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using IdLineParser = JenkinsPlug.IdMappingsStorage.IdLineParser;

namespace JenkinsPlug.Tests
{
    [TestFixture]
    class QueueMapStorageTests
    {
        [TestFixture]
        class LineParserTests
        {
            [Test]
            public void TesUnableToParse()
            {
                string storedId, storedResolvedUrl;

                string line = string.Empty;
                bool bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsFalse(bResult, "Unexpected successful parsing for [" + line + "]");

                line = "#";
                bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsFalse(bResult, "Unexpected successful parsing for [" + line + "]");

                line = "=";
                bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsFalse(bResult, "Unexpected successful parsing for [" + line + "]");

                line = "==";
                bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsFalse(bResult, "Unexpected successful parsing for [" + line + "]");

                line = "16=";
                bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsFalse(bResult, "Unexpected successful parsing for [" + line + "]");

                line = "16= ";
                bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsFalse(bResult, "Unexpected successful parsing for [" + line + "]");

                line = " 16= ";
                bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsFalse(bResult, "Unexpected successful parsing for [" + line + "]");

                line = "A=http://url";
                bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsFalse(bResult, "Unexpected successful parsing for [" + line + "]");

                line = "A=http://url";
                bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsFalse(bResult, "Unexpected successful parsing for [" + line + "]");
            }

            [Test]
            public void TestValidParsing()
            {
                string storedId, storedResolvedUrl;

                string line = " 1== ";
                bool bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsTrue(bResult, "Unexpected faulty parsing for [" + line + "]");
                Assert.AreEqual("1", storedId);
                Assert.AreEqual("=", storedResolvedUrl);

                line = "  16 = http://url=url2   ";
                bResult = IdLineParser.TryParse(line, out storedId, out storedResolvedUrl);
                Assert.IsTrue(bResult, "Unexpected faulty parsing for [" + line + "]");
                Assert.AreEqual("16", storedId);
                Assert.AreEqual("http://url=url2", storedResolvedUrl);
            }
        }

        [TestFixture]
        class StorageTests
        {
            [Test]
            public void TestLoadData()
            {
                string tmpPath = Path.GetTempFileName();
                Dictionary<string, string> cache = new Dictionary<string, string>();

                IdMappingsStorage.Load(tmpPath, cache);
                Assert.AreEqual(0, cache.Count);

                try
                {
                    using (StreamWriter sw = new StreamWriter(tmpPath, false))
                    {
                        sw.WriteLine("#A comment");
                        sw.WriteLine("A=B");
                        sw.WriteLine("1= http:// the===_url ");
                        sw.WriteLine(string.Empty);
                        sw.WriteLine("2 = a value ");
                        sw.WriteLine("2= http://localhost:8080/job/23 ");
                        sw.WriteLine(string.Empty);
                    }

                    IdMappingsStorage.Load(tmpPath, cache);

                    Assert.AreEqual(2, cache.Count);
                    Assert.AreEqual("http:// the===_url", cache["1"]);
                    Assert.AreEqual("http://localhost:8080/job/23", cache["2"]);
                }
                finally
                {
                    if (File.Exists(tmpPath))
                        File.Delete(tmpPath);
                }
            }

            [Test]
            public void TestSaveData()
            {
                string tmpPath = Path.GetTempFileName();
                Dictionary<string, string> cache = new Dictionary<string, string>();
                cache["1"] = " a value ";
                cache["5"] = " http://localhost:8080/job/32";

                try
                {
                    IdMappingsStorage.Save(tmpPath, cache);

                    string[] lines = File.ReadAllLines(tmpPath);
                    Assert.AreEqual(2, lines.Length);

                    Assert.AreEqual("1= a value ", lines[0]);
                    Assert.AreEqual("5= http://localhost:8080/job/32", lines[1]);
                }
                finally
                {
                    if (File.Exists(tmpPath))
                        File.Delete(tmpPath);
                }
            }
        }
    }
}
