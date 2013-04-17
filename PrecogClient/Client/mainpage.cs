/**
\mainpage Welcome to the Precog client for .Net!

This client makes it easy to set up an account, ingest data, and then query that data, all from within your .Net application.

<h1>A Simple Example</h1>

For this example, we'll show account creation, ignest and query of data, followed by deletion of that data. Normally you only need to set up an account once, and this is typically done via the <a href="https://www.precog.com/register">registration page</a>, but we'll show a complete example here.

<code>
using Precog.Client;
using Precog.Client.Format;

public class SomeStuff
{
	public string Name { get; set; }
	public int Count { get; set; }
}

public class PrecogExample
{
	static void Main(string[] args)
	{
		// Create an account. Normally you would create the account outside of your normal
		// client code and reuse the same account everywhere.
		var accountInfo = PrecogClient.CreateAccount("foo@bar.com", "badPassword");

		var client = new PrecogClient(accountInfo.ApiKey, accountInfo.RootPath);

		// This assumes a SomeSource that can generate SomeStuff instances
		SomeStuff myStuff = SomeSource.GetStuff("foo");
		client.Append("/test", myStuff);

		// Or, we could store a collection of SomeStuff instances
		List<SomeStuff> manyStuffs = SomeSource.GetLotsaStuff("foo");
		client.AppendAll("/test", manyStuffs);

		// Or, we can ingest directly from a file
		client.AppendFromFile("/test", Formats.CSV, @"C:\somefile.csv");

		// Uploading a file means we replace (e.g. delete) the existing data at that path
		client.UploadFile("/test", Formats.CSV, @"C:\somefile.csv");

		// A simple query to compute the mean count from our data. The result will be
		// returned as a List<int> in this case, with the first (and only) element containing
		// the mean value.
		var avg = client.Query<int>("/", "mean(load(\"/test\").Count)").Data[0];

		// Clean out the data now that we're done
		client.Delete("/test");
	}
}
</code>
*/