/**
\mainpage Welcome to the Precog client for .Net!

This client makes it easy to set up an account, ingest data, and then query that data, all from within your .Net application. 

<h1>A Simple Example</h1>

For this example, we'll show account creation, ignest and query of data, followed by deletion of that data. Normally you only need to set up an account once, and this is typically done via the <a href="https://www.precog.com/register">registration page</a>, but we'll show a complete example here.

<code>
using Precog.Client;

public class SomeStuff
{
    public string Name { get; set; }
    public int Count { get; set; }
}

public class PrecogExample
{
    static void Main(string[] args)
    {
        var accountInfo = PrecogClient.CreateAccount("foo@bar.com", "badPassword");
        
        var client = new PrecogClient(accountInfo.ApiKey, accountInfo.AccountId);
        
        // This assumes a SomeSource that can generate SomeStuff instances
        SomeStuff myStuff = SomeSource.GetStuff("foo");
        client.Store("/test", myStuff);
        
        // Or, we could store a collection of SomeStuff instances
        List<SomeStuff> manyStuffs = SomeSource.GetLotsaStuff("foo");
        client.StoreAll("/test", manyStuffs);
        
        // Or, we can ingest directly from a file
        client.StoreFile("/test", @"C:\\somefile.csv", MimeType.CSV);

        // A simple query
        var avg = client.Query<int>("/test", "average(//test)").Data[0];

        // Clean out the data now that we're done
        client.Delete("/test");
    }
}
</code>
*/