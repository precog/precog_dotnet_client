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
using System.Net;
using System.Net.Mime;
using System.Text;
using Precog.Client;
using RestSharp;
using RestSharp.Serializers;
using RestSharp.Deserializers;

namespace Precog.Client
{
	using Dto;

	/// <summary>
	/// A basic exception thrown when the client encounters an error.
	/// </summary>
	public class PrecogException : Exception
	{
		public PrecogException(string message) : base(message) {}
	}

	/// <summary>
	/// This client is used for ingest, deletion and querying of data, as well as account creation
	/// and information retrieval.
	///
	/// This client provides two classes of methods for all non-Delete operations: cooked and raw. Cooked
	/// methods (Store, StoreMany, Query) use generic types for serialization and deserialization
	/// of the data into and out of user-specified types.
    ///
    /// Raw methods (Ingest, QueryRaw) either consume or produce raw strings containing the data
    /// to be ingested or returned from a query.
	/// </summary>
	public class PrecogClient
	{
		public static Uri DEFAULT_ENDPOINT = new Uri("https://beta.precog.com");

		/// <summary>
		/// Create a new HTTPS client using the default endpoint (https://beta.precog.com).
		/// </summary>
		/// <param name='apiKey'>
		/// The api key for this client.
		/// </param>
		/// <param name='basePath'>
		/// The base path to use for operations with this client.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the apiKey or basePath is <see cref="null" />.
		/// </exception>
		/// <exception cref=ArgumentException>
		/// Thrown if the apiKey is empty.
		/// </exception>
		public PrecogClient(string apiKey, string basePath): this(DEFAULT_ENDPOINT, apiKey, basePath) {}

		/// <summary>
		/// Creates a new client.
		/// </summary>
		/// <param name='endpoint'>
		/// The endpoint URI to use for client operations.
		/// </param>
		/// <param name='apiKey'>
		/// The api key for this client.
		/// </param>
		/// <param name='basePath'>
		/// The base path to use for operations with this client.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the apiKey or basePath is <see cref="null" />.
		/// </exception>
		/// <exception cref=ArgumentException>
		/// Thrown if the apiKey is empty.
		/// </exception>
 		public PrecogClient(Uri endpoint, string apiKey, string basePath)
		{
			if (apiKey == null)
			{
				throw new ArgumentNullException("apiKey must not be null");
			}

			if (apiKey == "")
			{
				throw new ArgumentException("apiKey must not be empty");
			}

			if (basePath == null)
			{
				throw new ArgumentNullException("basePath must not be null");
			}

			_apiKey = apiKey;

			// Make sure that the base path doesn't end with "/" so that
			// relative paths may simply be appended. "/" by itself is special
			// cased
			if (basePath == "/")
			{
				_basePath = "";
			}
			else
			{
				// Canonicalize the path to start with a "/" and not end with "/"
				var parts = new List<string>(basePath.Split('/'));
				_basePath = "/" + String.Join("/", parts.Where(x => x != ""));
			}

			_serializer = new JsonSerializer();
			_client = CreateRestClient(endpoint);


		}

		private static RestClient CreateRestClient (Uri endpoint)
		{
			var client = new RestClient(endpoint.ToString());
			// Make sure that we deserialize from JSON by default
			client.AddHandler("*", new JsonDeserializer());

			return client;
		}

		/// <summary>
		/// Creates a new account with the default endpoint, accessible by the
		/// specified email address and password. An Exception
		/// is thrown if an account already exists for the given email address.
		/// </summary>
		/// <returns>
		/// Account info for the newly created account
		/// </returns>
		/// <param name='email'>
		/// user's email
		/// </param>
		/// <param name='password'>
		/// user's password
		/// </param>
	    public static AccountInfo CreateAccount(String email, String password)
		{
			return CreateAccount(DEFAULT_ENDPOINT, email, password);
	    }

		/// <summary>
		/// Creates a new account, accessible by the specified email address and password. An Exception
		/// is thrown if an account already exists for the given email address or if the specified
		/// endpoint does not use HTTPS.
		/// </summary>
		/// <returns>
		/// Account info for the newly created account
		/// </returns>
		/// <param name='endpoint'>
		/// The endpoint URI to use for client operations. Must use HTTPS or an exception will be thrown.
		/// </param>
		/// <param name='email'>
		/// user's email
		/// </param>
		/// <param name='password'>
		/// user's password
		/// </param>
	    public static AccountInfo CreateAccount(Uri endpoint, String email, String password)
		{
			if (endpoint.Scheme != "https")
			{
				throw new PrecogException("HTTPS is required for all account-related operations. Invalid endpoint: " + endpoint);
			}

			var client = CreateRestClient(endpoint);

			var request = new RestRequest("/accounts/v1/accounts/", Method.POST);
			request.RequestFormat = DataFormat.Json;
	        request.AddBody(new AccountCreate(email, password));

	        IRestResponse<AccountCreateResponse> response = client.Execute<AccountCreateResponse>(request);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new PrecogException(response.Content);
			}

			return DescribeAccount(endpoint, email, password, response.Data.accountId);
	    }

		/// <summary>
		/// Retrieves the details about a particular account. This call is the primary mechanism by which you can retrieve your master API key.
		/// </summary>
		/// <returns>
		/// Account info for the specified account
		/// </returns>
		/// <param name='email'>
		/// user's email
		/// </param>
		/// <param name='password'>
		/// user's password
		/// </param>
		/// <param name='accountId'>
		/// account's id number
		/// </param>
		public static AccountInfo DescribeAccount(string email, string password, string accountId)
		{
			return DescribeAccount(DEFAULT_ENDPOINT, email, password, accountId);
		}

		/// <summary>
		/// Retrieves the details about a particular account. This call is the primary mechanism by which you can retrieve your master API key.
		/// </summary>
		/// <returns>
		/// Account info for the specified account
		/// </returns>
		/// <param name='endpoint'>
		/// The endpoint URI to use for client operations. Must use HTTPS or an exception will be thrown.
		/// </param>
		/// <param name='email'>
		/// user's email
		/// </param>
		/// <param name='password'>
		/// user's password
		/// </param>
		/// <param name='accountId'>
		/// account's id number
		/// </param>
	    public static AccountInfo DescribeAccount(Uri endpoint, string email, string password, string accountId)
		{
			if (endpoint.Scheme != "https")
			{
				throw new PrecogException("HTTPS is required for all account-related operations. Invalid endpoint: " + endpoint);
			}

			// We need a new client here specifically to allow for BASIC auth
			var infoClient = CreateRestClient(endpoint);
			infoClient.Authenticator = new HttpBasicAuthenticator(email, password);
			var infoRequest = new RestRequest("/accounts/v1/accounts/" + accountId, Method.GET);
			infoRequest.RequestFormat = DataFormat.Json;

			var response = infoClient.Execute<AccountInfo>(infoRequest);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new PrecogException(response.Content);
			}

			return response.Data;
	    }

		/// <summary>
		/// Ingest a single object at the specified path.
		/// </summary>
		/// <description>
		/// The internal JSON machinery is
		/// used to serialize the data. If you need full control over serialization,
		/// use <see cref="IngestRaw"/> instead.
		/// As an example, assume we have a struct we would like to ingest:
		/// <code>
		/// public class MyData
		/// {
		/// 	public string Name { get; set; }
		/// 	public int Count { get; set; }
		/// }
		/// </code>
		/// We can construct an instance and use our client to ingest it directly:
		/// <code>
		/// var foo = new MyData();
		/// //... fill in values
		///
		/// client.Ingest("/test/data/path", foo);
		/// </code>
		/// </description>
		/// <param name='path'>
		/// The storage path for the record.
		/// </param>
		/// <param name='record'>
		/// A serializable record object.
		/// </param>
	    public IngestResult Store<T>(string path, T record)
		{
			return Ingest(path, _serializer.Serialize(record), new IngestOptions());
	    }

		/// <summary>
		/// Ingest a multiple objects at the specified path. The internal JSON machinery is
		/// used to serialize the data. If you need full control over serialization,
		/// use <see cref="IngestRaw"/> instead.
		/// </summary>
		/// <param name='path'>
		/// The storage path for the record.
		/// </param>
		/// <param name='records'>
		/// A collection of serializable objects.
		/// </param>
		public IngestResult StoreAll<T>(string path, IEnumerable<T> records)
		{
			// Collapse all entities into a stream
			var content = "";

			foreach (T record in records)
			{
				content += _serializer.Serialize(record) + "\n";
			}

			return Ingest(path, content, new IngestOptions());
		}

		/// <summary>
		/// Stores the file specified at the given path with the given MIME type.
		/// </summary>
		/// <returns>
		/// The file to ingest.
		/// </returns>
		/// <param name='path'>
		/// The storage path for the data in the file.
		/// </param>
		/// <param name='filename'>
		/// The name of the file to ingest.
		/// </param>
		/// <param name='mimeType'>
		/// The MIME type for the file contents.
		/// </param>
		public IngestResult StoreFile(string path, string filename, MimeType mimeType)
		{
			return StoreFile(path, filename, new IngestOptions(mimeType));
		}

		/// <summary>
		/// Stores the file specified at the given path with the given MIME type.
		/// </summary>
		/// <returns>
		/// The file to ingest.
		/// </returns>
		/// <param name='path'>
		/// The storage path for the data in the file.
		/// </param>
		/// <param name='filename'>
		/// The name of the file to ingest.
		/// </param>
		/// <param name='options'>
		/// Options controlling the content type of the ingest data as well as other ingest-related
		/// parameters. MimeType must be set.
		/// </param>
		public IngestResult StoreFile(string path, string filename, IngestOptions options)
		{
			var uriString = String.Format ("{0}/ingest/v1/fs{1}{2}?{3}={4}&mode=batch&receipt=true", _client.BaseUrl, _basePath, path, Params.API_KEY, _apiKey);

			// RestSharp doesn't do raw bodies (?!?!?!)
			HttpWebRequest http = (HttpWebRequest) HttpWebRequest.Create(uriString);

			http.Method = "POST";
			http.ContentType = MimeTypeUtil.FromMimeType(options.MimeType).MediaType;
			http.ContentLength = (new FileInfo(filename)).Length;
			using (FileStream input = new FileStream(filename, FileMode.Open))
			{
				using (Stream output = http.GetRequestStream())
				{
					input.CopyTo(output);
				}
			}

			using (HttpWebResponse response = (HttpWebResponse) http.GetResponse())
			{
				var body = ReadResponseBody(response);

				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new PrecogException(body);
				}

				var raw = SimpleJson.DeserializeObject<JsonObject>(body);

				var result = new IngestResult();

				var errors = new List<IngestError>();
				foreach (object error in ((JsonArray) raw["errors"]))
				{
					var err = new IngestError();
					err.Line = (int) (long) ((JsonObject) error)["line"];
					err.Reason = (string) ((JsonObject) error)["reason"];
					errors.Add(err);
				}

				result.Errors = errors;
				result.Failed = (int) (long) raw["failed"];
				result.Ingested = (int) (long) raw["ingested"];
				result.IngestId = (string) raw["ingestId"];
				result.Total = (int) (long) raw["total"];

				return result;
			}
		}

		private string ReadResponseBody(HttpWebResponse response)
		{
			var buffer = new byte[response.ContentLength];

			var input = response.GetResponseStream();

			// Downcasting to an int is safe here because we know that we'll only ever return small messages/JSON
			input.Read(buffer, 0, (int) response.ContentLength);

			return System.Text.UTF8Encoding.UTF8.GetString(buffer);
		}

		/// <summary>
		/// Ingest raw JSON/CSV strings at the specified path.
		/// </summary>
		/// <description>
		/// For those needing more control over serialization, or non-JSON ingest,
		/// this method allows full control
		/// over the data content. For example, to ingest CSV data:
		/// <code>
		/// client.IngestRaw("/some/test/path", "this,is,the,header\n,these,are,the,values", MimeType.CSV);
		/// </code>
		/// </description>
		/// <param name='path'>
		/// The storage path for the data.
		/// </param>
		/// <param name='content'>
		/// The content to be ingested. No processing is performed on this content.
		/// </param>
		/// <param name='mimeType'>
		/// The MIME type of the provided content.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the provided content is empty or <see cref="null"/>.
		/// </exception>
		public IngestResult Ingest(string path, string content, MimeType mimeType)
		{
			return Ingest(path, content, new IngestOptions(mimeType));
		}

		/// <summary>
		/// Ingest raw JSON/CSV strings at the specified path.
		/// </summary>
		/// <description>
		/// For those needing more control over serialization, or non-JSON ingest,
		/// this method allows full control
		/// over the data content. For example, to ingest CSV data:
		/// <code>
		/// var opts = new IngestOptions(MimeType.CSV);
		/// client.IngestRaw("/some/test/path", "this,is,the,header\n,these,are,the,values", opts);
		/// </code>
		/// </description>
		/// <param name='path'>
		/// The storage path for the data.
		/// </param>
		/// <param name='content'>
		/// The content to be ingested. No processing is performed on this content.
		/// </param>
		/// <param name='options'>
		/// Options controlling the content type of the ingest data as well as other ingest-related
		/// parameters. Content type must be set.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the provided content is empty or <see cref="null"/>.
		/// </exception>
	    public IngestResult Ingest(string path, string content, IngestOptions options)
		{
	        if (content == null || content=="") {
	            throw new ArgumentNullException("argument 'content' must contain a non empty value formatted as described by type");
	        }

			if (path == null || ! path.StartsWith("/"))
			{
				throw new ArgumentException("Invalid path provided. Paths must start with '/': " + path);
			}

			// RestSharp, sadly, adheres to the spec and won't put params in the URI when using POST method
			// This method always performs batched/sync ingest
			var paramString = String.Format("?{0}={1}&mode=batch&receipt=true", Params.API_KEY, _apiKey);

			if (options.HasOwnerAccountId)
			{
				paramString += String.Format("&ownerAccountId={0}", options.OwnerAccountId);
			}

			var request = new RestRequest(Services.INGEST + _basePath + path + paramString, Method.POST);
			// Although we're raw on input, we need this to properly deserialize the result
			request.RequestFormat = DataFormat.Json;

			request.AddHeader("Content-Type", MimeTypeUtil.FromMimeType(options.MimeType).MediaType);
			// We bypass serialization here, since we're "raw"
			request.AddParameter("content", content, ParameterType.RequestBody);

			IRestResponse<IngestResult> response = _client.Execute<IngestResult>(request);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new PrecogException(response.Content);
			}

			return response.Data;
	    }

		/// <summary>
		/// Deletes the data at the specified service-relative path.
		/// </summary>
		/// <param name='path'>
		/// The service-relative path. E.g. "/1234567890/test" maps to "/ingest/v1/fs/1234567890/test".
		/// </param>
	    public void Delete(string path)
		{
			if (path == null || ! path.StartsWith("/"))
			{
				throw new ArgumentException("Invalid path provided. Paths must start with '/': " + path);
			}

			var request = new RestRequest(Services.INGEST + _basePath + path + "?apiKey=" + _apiKey, Method.DELETE);

			var result = _client.Execute(request);

			if (result.StatusCode != HttpStatusCode.OK)
			{
				throw new PrecogException(result.Content);
			}
	    }

		/// <summary>
		/// Execute the given query using the specified account-relative base path. No limit, skip or sorting is performed.
		/// </summary>
		/// <param name='path'>
		/// The account-relative base path for the query. If "/", all paths inside the query must be fully-specified.
		/// For example, the following two queries are equivalent:
		/// <code>
		/// client.Query("/test", "count(//foo)");
		/// client.Query("/", "count(//foo/test)");
		/// </code>
		/// </param>
		/// <param name='query'>
		/// The quirrel query string. No quoting is required.
		/// </param>
		/// <typeparam name='T'>
		/// The result type for the query. The underlying JSON result will be
		/// deserialized to this type.
		/// </typeparam>
		public QueryResult<T> Query<T>(string path, string query) where T: new()
		{
			return Query<T>(path, query, new QueryOptions());
		}

		/// <summary>
		/// Execute the given query using the specified account-relative base path.
		/// </summary>
		/// <param name='path'>
		/// The account-relative base path for the query. If "/", all paths inside the query must be fully-specified.
		/// For example, the following two queries are equivalent:
		/// <code>
		/// client.Query("/test", "count(//foo)");
		/// client.Query("/", "count(//foo/test)");
		/// </code>
		/// </param>
		/// <param name='query'>
		/// The quirrel query string. No quoting is required.
		/// </param>
		/// <param name='options'>
		/// Options controlling the results such as skip, limit, and sorting.
		/// </param>
		/// <typeparam name='T'>
		/// The result type for the query. The underlying JSON result will be
		/// deserialized to this type.
		/// </typeparam>
	    public QueryResult<T> Query<T>(string path, string query, QueryOptions options) where T: new()
		{
			var result = QueryInternal<JsonObject>(path, query, options);
			var raw = SimpleJson.DeserializeObject<JsonObject>(result.Content);

			// The following code works around a yet-to-be-determined bug in RestSharp deserialization of nested classes
			var finalResult = new QueryResult<T>();
			finalResult.DataRaw = raw["data"].ToString();
			finalResult.Errors = SimpleJson.DeserializeObject<IList<MessageReport>>(raw["errors"].ToString());
			finalResult.Warnings = SimpleJson.DeserializeObject<IList<MessageReport>>(raw["warnings"].ToString());

			var serverErrors = (JsonArray) raw["serverErrors"];

			finalResult.ServerErrors = new List<string>(serverErrors.Select<object, string>(x => x.ToString()));

			var dataArray = (JsonArray) raw["data"];

			finalResult.Data = new List<T>(dataArray.Select<object, T>(x => SimpleJson.DeserializeObject<T>(x.ToString())));

			return finalResult;
	    }

		/// <summary>
		/// Execute the given query using the specified base path and default options. The raw
		/// JSON string result is returned directly.
		/// </summary>
		/// <param name='path'>
		/// The service-relative base path for the query. If "/", all paths inside the query must be fully-specified.
		/// For example, the following two queries are equivalent:
		/// <code>
		/// client.Query("/1234567890/test", "count(//foo)");
		/// client.Query("/", "count(//1234567890/foo/test)");
		/// </code>
		/// </param>
		/// <param name='query'>
		/// The quirrel query string. No quoting is required.
		/// </param>
	    public String QueryRaw(string path, string query)
		{
			return QueryInternal<JsonObject>(path, query, new QueryOptions()).Content;
	    }

		/// <summary>
		/// Execute the given query using the specified base path. The raw
		/// JSON string result is returned directly.
		/// </summary>
		/// <param name='path'>
		/// The service-relative base path for the query. If "/", all paths inside the query must be fully-specified.
		/// For example, the following two queries are equivalent:
		/// <code>
		/// client.Query("/1234567890/test", "count(//foo)");
		/// client.Query("/", "count(//1234567890/foo/test)");
		/// </code>
		/// </param>
		/// <param name='query'>
		/// The quirrel query string. No quoting is required.
		/// </param>
		/// <param name='options'>
		/// Options controlling the results such as skip, limit, and sorting.
		/// </param>
	    public String QueryRaw(string path, string query, QueryOptions options)
		{
			return QueryInternal<JsonObject>(path, query, options).Content;
	    }


		private IRestResponse<T> QueryInternal<T>(string path, string query, QueryOptions options) where T: new()
		{
			if (path == null || ! path.StartsWith("/"))
			{
				throw new ArgumentException("Invalid path provided. Paths must start with '/': " + path);
			}

			// Horrid workaround for overzealous RestSharp RESTification of GET URIs
			var finalPath = path == "/" ? "//" : path;

			var request = new RestRequest(Services.ANALYTICS + _basePath + finalPath, Method.GET);
			request.RequestFormat = DataFormat.Json;

			if (options.Limit > 0)
			{
				request.AddParameter("limit", options.Limit);
			}

			if (options.Skip != 0)
			{
				request.AddParameter("skip", options.Skip);
			}

			if (options.SortOn != null)
			{
				request.AddParameter("sortOn", "[" + String.Join(",", options.SortOn) + "]");

				if (options.SortOrder == QueryOptions.Order.ASCENDING)
				{
					request.AddParameter("sortOrder", "asc");
				}
				else
				{
					request.AddParameter("sortOrder", "desc");
				}
			}

			request.AddParameter(Params.QUERY, query);
			request.AddParameter(Params.API_KEY, _apiKey);
			request.AddParameter("format", "detailed");

			var response = _client.Execute<T>(request);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new PrecogException(response.Content);
			}

			return response;
		}

		private static class Params
		{
			public const string API_KEY = "apiKey";
			public const string QUERY = "q";
			public const string MODE = "mode";
		}

		private static class Services
		{
			public const string INGEST    = "/ingest/v1/fs";
			public const string ANALYTICS = "/analytics/v1/fs";
		}

		private RestClient _client;
		private string _apiKey;
		private string _basePath;
		private JsonSerializer _serializer;
	}
}