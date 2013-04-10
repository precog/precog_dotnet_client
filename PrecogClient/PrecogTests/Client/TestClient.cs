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
	public class TestStore
	{
		public static Uri TEST_ENDPOINT = new Uri("https://devapi.precog.com");

		private const int DELAY = 500;
		private const int RETRIES = 60;
		private const string SIMPLE_PATH = "/testStore";

		private PrecogClient client;

		private IList<TestData> originalObjects;
		private IEnumerable<IDictionary<string, object>> originalDicts;
		private IEnumerable<string> originalRaw;
		private AccountInfo accountInfo;

		[TestFixtureSetUp]
		public void Setup()
		{
			var serializer = new JsonSerializer();

			var email = String.Format("dotnettest{0}@precog.com", (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);

			var password = Guid.NewGuid().ToString();

			accountInfo = PrecogClient.CreateAccount(TEST_ENDPOINT, email, password);

			client = new PrecogClient(TEST_ENDPOINT, accountInfo.ApiKey, accountInfo.AccountId);

			var rand = new Random();
			var dataSize = rand.Next(50, 100);

			originalObjects = new List<TestData>(dataSize);

			for (int i = 0; i < dataSize; i++)
			{
				var d = new TestData();
				d.Number = rand.Next(1, 1000); // we don't want these too big, since we'll be summing them
				d.Name = Guid.NewGuid().ToString();
				originalObjects.Add(d);
			}

			originalDicts = originalObjects.Select(x => x.ToDictionary());
			originalRaw = originalObjects.Select(x => serializer.Serialize(x));
		}

		[Test]
		public void DoStoreDicts ()
		{
			DoStore(originalDicts);
		}

		[Test]
		public void DoStoreFile ()
		{
			// We don't want the BOM (Can remove once PLATFORM-629 is resolved)
			var encodingWithoutBOM = new UTF8Encoding(false);

			var tempFile = Path.GetTempFileName();

			Console.WriteLine("Testing file store with " + tempFile);

			using (StreamWriter output = new StreamWriter(tempFile, false, encodingWithoutBOM))
			{
				foreach (string raw in originalRaw)
				{
					output.WriteLine(raw);
				}
			}

			var result = client.StoreFile(SIMPLE_PATH, tempFile, MimeType.JSON);

			Assert.AreEqual(originalObjects.Count, result.Ingested, "Count incorrect");
			Assert.AreEqual(0, result.Failed, "Unexpected failures");
			Assert.IsEmpty(result.Errors);
		}

		[Test]
		public void DoStoreObjects ()
		{
			DoStore(originalObjects);
		}

		[Test]
		public void DoIngest ()
		{
			var result = client.Ingest(SIMPLE_PATH,
			                           String.Join("\n", originalRaw),
			                           MimeType.JSON);

			Assert.AreEqual(originalObjects.Count, result.Ingested);
			Assert.AreEqual(0, result.Failed);
			Assert.IsEmpty(result.Errors);
		}

	    [Test]
		public void FireCountQuery()
		{
			CountQuery(originalObjects.Count * 4);
		}

		[Test]
		public void FireSumQuery()
		{
			var result = client.Query<int>("/", String.Format("sum(load(\"{0}\").Number)", SIMPLE_PATH));

			var expected = originalObjects.Sum(x => x.Number) * 4;

			Assert.AreEqual(expected, result.Data[0]);
		}

		[Test]
		public void FireObjResultQuery()
		{
			var result = client.Query<JsonObject>("/", String.Format("{{ \"count\" : count(load(\"{0}\")), \"sum\" : sum(load(\"{1}\").Number) }}", SIMPLE_PATH, SIMPLE_PATH));

		 	var expectedSum = originalObjects.Sum(x => x.Number) * 4;
			Assert.AreEqual(expectedSum, (int) (long) result.Data[0]["sum"]);

			Assert.AreEqual(originalObjects.Count * 4, (int) (long) result.Data[0]["count"]);
		}

		[Test]
		public void RunDelete()
		{
			client.Delete(SIMPLE_PATH);
			CountQuery(0);
		}

	    public void CountQuery(int expected)
		{
			var retries = RETRIES;

			while (retries > 0)
			{
				try {
					var result = client.Query<int>("/", String.Format("count(load(\"{0}\"))", SIMPLE_PATH));

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

		public void DoStore<T>(IEnumerable<T> data)
		{
			var first = data.First();
			var result = client.Store(SIMPLE_PATH, first);

			Assert.AreEqual(1, result.Ingested);
			Assert.AreEqual(0, result.Failed);
			Assert.IsEmpty(result.Errors);

			var result2 = client.StoreAll(SIMPLE_PATH, data.Skip(1));

			Assert.AreEqual(data.Count() - 1, result2.Ingested);
			Assert.AreEqual(0, result2.Failed);
			Assert.IsEmpty(result2.Errors);
		}
	}
}

