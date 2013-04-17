/*
  Copyright (C) 2011-2013 by ReportGrid, Inc. All rights reserved.

  Permission is hereby granted, free of charge, to any person obtaining a copy
  of this software and associated documentation files (the "Software"), to deal
  in the Software without restriction, including without limitation the rights
  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  copies of the Software, and to permit persons to whom the Software is
  furnished to do so, subject to the following conditions:

  The above copyright notice and this permission notice shall be included in
  all copies or substantial portions of the Software.

  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
  THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Precog.Client;
using RestSharp.Serializers;

namespace Precog.Client
{
	using Format;

	class TestData
	{
        public int Number { get; set; }
		public string Name { get; set; }

		// This method is used so that we can test idiomatic ingest of Dictionary instances
		public IDictionary<string, object> ToDictionary()
		{
			var dict = new Dictionary<string, object>(2);
			dict["Number"] = Number;
			dict["Name"] = Name;
			return dict;
		}
    }

	[TestFixture]
	public class TestClient
	{
		public static Uri TEST_ENDPOINT = new Uri("https://devapi.precog.com");

		private const int DELAY = 500;
		private const int RETRIES = 60;
		private const string SIMPLE_PATH = "/testStore";
        private const string UPLOAD_PATH = "/testUpload";

		private PrecogClient client;

		private IList<TestData> originalObjects;
		private IEnumerable<string> originalRaw;
		private AccountInfo accountInfo;

        private string email;
        private string password;
        private string objectFile;

		[TestFixtureSetUp]
		public void Setup()
		{
			var serializer = new JsonSerializer();

			email = String.Format("dotnettest{0}@precog.com", (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);

			password = Guid.NewGuid().ToString();

			var rand = new Random();
			var dataSize = rand.Next(25, 100);

			originalObjects = new List<TestData>(dataSize);

			for (int i = 0; i < dataSize; i++)
			{
				var d = new TestData();
				d.Number = rand.Next(1, 1000); // we don't want these too big, since we'll be summing them
				d.Name = Guid.NewGuid().ToString();
				originalObjects.Add(d);
			}

			originalRaw = originalObjects.Select(x => serializer.Serialize(x));

			// We don't want the BOM (Can remove once PLATFORM-629 is resolved)
			var encodingWithoutBOM = new UTF8Encoding(false);

			objectFile = Path.GetTempFileName();

			using (StreamWriter output = new StreamWriter(objectFile, false, encodingWithoutBOM))
			{
				foreach (string raw in originalRaw)
				{
					output.WriteLine(raw);
				}
			}
		}

        [TestFixtureTearDown]
        public void TearDown()
        {
            File.Delete(objectFile);
        }

        [Test]
        public void TestAccountCreation()
        {
			accountInfo = PrecogClient.CreateAccount(TEST_ENDPOINT, email, password);

			client = new PrecogClient(TEST_ENDPOINT, accountInfo.ApiKey, accountInfo.RootPath);
        }

		[Test]
		public void TestIngestDicts ()
		{
            var originalDicts = originalObjects.Select(x => x.ToDictionary());
			DoStore(originalDicts);
		}

        [Test]
        public void TestIngstCSV ()
        {
            var csvData = "Name,Number\n" + String.Join("\n", originalObjects.Select(x => String.Format("{0},{1}", x.Name, x.Number)));

            var result = client.AppendRaw(SIMPLE_PATH, Formats.CSV, csvData);

			Assert.AreEqual(originalObjects.Count, result.Ingested, "Count incorrect");
			Assert.AreEqual(0, result.Failed, "Unexpected failures");
			Assert.IsEmpty(result.Errors);
        }

		[Test]
		public void TestIngestFile ()
		{
			Console.WriteLine("Testing file store with " + objectFile);

			var result = client.AppendFromFile(SIMPLE_PATH, Formats.JSONStream, objectFile);

			Assert.AreEqual(originalObjects.Count, result.Ingested, "Count incorrect");
			Assert.AreEqual(0, result.Failed, "Unexpected failures");
			Assert.IsEmpty(result.Errors);
		}

		[Test]
		public void TestIngestObjects ()
		{
			DoStore(originalObjects);
		}

		[Test]
		public void TestIngestRaw ()
		{
			var result = client.AppendRaw(SIMPLE_PATH,
                                          Formats.JSONStream,
                                          String.Join("\n", originalRaw));

			Assert.AreEqual(originalObjects.Count, result.Ingested);
			Assert.AreEqual(0, result.Failed);
			Assert.IsEmpty(result.Errors);
		}

        [Test]
        public void TestIngestUpload ()
        {
			var result = client.AppendFromFile(UPLOAD_PATH, Formats.JSONStream, objectFile);

			Assert.AreEqual(originalObjects.Count, result.Ingested, "Count incorrect");
			Assert.AreEqual(0, result.Failed, "Unexpected failures");
			Assert.IsEmpty(result.Errors);

            CountQuery(originalObjects.Count, UPLOAD_PATH);

			var result2 = client.AppendFromFile(UPLOAD_PATH, Formats.JSONStream, objectFile);

			Assert.AreEqual(originalObjects.Count, result2.Ingested, "Count incorrect");
			Assert.AreEqual(0, result2.Failed, "Unexpected failures");
			Assert.IsEmpty(result2.Errors);

            // The upload should have replaced things, so the count should stay the same
            CountQuery(originalObjects.Count, UPLOAD_PATH);
        }

	    [Test]
		public void TestQueryCount()
		{
			CountQuery(originalObjects.Count * 5, SIMPLE_PATH);
		}

		[Test]
		public void TestQuerySum() 
		{
			var result = client.Query<int>("/", String.Format("sum(load(\"{0}\").Number)", SIMPLE_PATH));

			var expected = originalObjects.Sum(x => x.Number) * 5;

			Assert.AreEqual(expected, result.Data[0]);
		}

		[Test]
		public void TestQueryObjectResult()
		{
			var result = client.Query<JsonObject>("/", String.Format("{{ \"count\" : count(load(\"{0}\")), \"sum\" : sum(load(\"{1}\").Number) }}", SIMPLE_PATH, SIMPLE_PATH));

			var serializer = new JsonSerializer();
            Console.WriteLine("Sync = " + serializer.Serialize(result));

		 	var expectedSum = originalObjects.Sum(x => x.Number) * 5;
			Assert.AreEqual(expectedSum, (int) (long) result.Data[0]["sum"]);

			Assert.AreEqual(originalObjects.Count * 5, (int) (long) result.Data[0]["count"]);
		}

        [Test]
        public void TestQueryCountAsync()
        {
            var handle = client.QueryAsync("/", String.Format("{{ \"count\" : count(load(\"{0}\")), \"sum\" : sum(load(\"{1}\").Number) }}", SIMPLE_PATH, SIMPLE_PATH));

            System.Threading.Thread.Sleep(5000);

            var result = client.QueryResults<JsonObject>(handle);

			var serializer = new JsonSerializer();
            Console.WriteLine("Async = " + serializer.Serialize(result));

		 	var expectedSum = originalObjects.Sum(x => x.Number) * 5;
			Assert.AreEqual(expectedSum, (int) (long) result.Data[0]["sum"]);

			Assert.AreEqual(originalObjects.Count * 5, (int) (long) result.Data[0]["count"]);            
        }

        /// <summary>
        ///   Tests deletion of data. Named "ZDelete" to force it to be the last test in the stack
        /// </summary>
		[Test]
		public void TestZDelete()
		{
			client.Delete(SIMPLE_PATH);
			CountQuery(0, SIMPLE_PATH);
		}

	    protected void CountQuery(int expected, string path)
		{
			var retries = RETRIES;

			while (retries > 0)
			{
				try {
					Console.WriteLine("Running count query");
					var result = client.Query<int>("/", String.Format("count(load(\"{0}\"))", path));

					if (result.Data[0] == expected)
					{
						Console.WriteLine("Got expected count of " + expected);
						return;
					}
					else
					{
						Console.WriteLine(String.Format("Expected {0}, got {1}", expected, result.Data[0]));
					}
				}
				catch(PrecogException e)
				{
					Assert.Fail("Exception during query: {0}", e);
				}
				System.Threading.Thread.Sleep(DELAY);
				retries--;
			}

			Assert.Fail("Retries exceeded waiting for good query result");
	    }

        /// <summary>
        ///   This test both single instance and collection ingest of POCOs
        /// </summary>
		protected void DoStore<T>(IEnumerable<T> data)
		{
			var first = data.First();
			var result = client.Append(SIMPLE_PATH, first);

			Assert.AreEqual(1, result.Ingested);
			Assert.AreEqual(0, result.Failed);
			Assert.IsEmpty(result.Errors);

			var result2 = client.AppendAll(SIMPLE_PATH, data.Skip(1));

			Assert.AreEqual(data.Count() - 1, result2.Ingested);
			Assert.AreEqual(0, result2.Failed);
			Assert.IsEmpty(result2.Errors);
		}
	}
}

