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
        /// <summary>
        /// Creates a PrecogException with the specified message
        /// </summary>
		internal PrecogException(string message, params object[] args) : base(String.Format(message, args)) {}
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
        /// <summary>
        ///   The default endpoint for use by client instances. Client
        ///   static and instance constructors default to this <see cref="Uri" />
        ///   unless one is provided.
        /// </summary>
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
		/// Thrown if the apiKey or basePath is <see langref="null" />.
		/// </exception>
		/// <exception cref='ArgumentException'>
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
		/// Thrown if the apiKey or basePath is <see langref="null" />.
		/// </exception>
		/// <exception cref='ArgumentException'>
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
			//
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
			return CreateAccount(DEFAULT_ENDPOINT, email, password, "");
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
		/// <param name='profile'>
		/// Profile data, in JSON format, to be associated with the account. Profile data is not validated,
		/// so invalid JSON can potentially cause account creation to fail.
		/// </param>
		public static AccountInfo CreateAccount(String email, String password, String profile)
		{
			return CreateAccount(DEFAULT_ENDPOINT, email, password, profile);
		}

		/// <summary>
		/// Creates a new account with the specified endpoint, accessible by the
		/// specified email address and password. An Exception
		/// is thrown if an account already exists for the given email address.
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
			return CreateAccount(endpoint, email, password, "");
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
		/// The email address for the account.
		/// </param>
		/// <param name='password'>
		/// The password for the account.
		/// </param>
		/// <param name='profile'>
		/// Profile data, in JSON format, to be associated with the account. Profile data is not validated,
		/// so invalid JSON can potentially cause account creation to fail.
		/// </param>
		public static AccountInfo CreateAccount(Uri endpoint, String email, String password, String profile)
		{
			if (endpoint.Scheme != "https")
			{
				throw new PrecogException("HTTPS is required for all account-related operations. Invalid endpoint: " + endpoint);
			}

			AccountCreate createInfo = null;
			if (profile != "")
			{
				createInfo = new AccountCreate(email, password);
			}
			else
			{
				createInfo = new AccountCreateProfiled(email, password, profile);
			}

			var client = CreateRestClient(endpoint);

			var request = new RestRequest("/accounts/v1/accounts/", Method.POST);
			request.RequestFormat = DataFormat.Json;
			request.AddBody(createInfo);

			IRestResponse<AccountCreateResponse> response = client.Execute<AccountCreateResponse>(request);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new PrecogException("Error creating account: " + response.Content);
			}

			return AccountDetails(endpoint, email, password, response.Data.accountId);
		}

		/// <summary>
		/// Retrieves the details about a particular account. This
		/// call is the primary mechanism by which you can retrieve
		/// your master API key.
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
		public static AccountInfo AccountDetails(string email, string password, string accountId)
		{
			return AccountDetails(DEFAULT_ENDPOINT, email, password, accountId);
		}

		/// <summary>
		/// Retrieves the details about a particular account. This
		/// call is the primary mechanism by which you can retrieve
		/// your master API key.
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
		public static AccountInfo AccountDetails(Uri endpoint, string email, string password, string accountId)
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
				throw new PrecogException("Error retrieving account details: " + response.Content);
			}

			return response.Data;
		}

		/// <summary>
		/// Append raw strings at the specified path.
		/// </summary>
		/// <description>
		/// For those needing more control over serialization, or non-JSON append,
		/// this method allows full control
		/// over the data content.
		/// </description>
        /// <example>
        /// For example, to append CSV data:
		/// <code>
		/// client.AppendRaw("/some/test/path", Formats.CSV, "this,is,the,header\n,these,are,the,values");
		/// </code>
        /// </example>
		/// <param name='path'>
		/// The storage path for the data.
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='content'>
		/// The content to be appended. No processing is performed on this content.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the provided content is empty or <see langref="null"/>.
		/// </exception>
		public AppendResult AppendRaw(string path, AppendFormat format, string content)
		{
			if (content == null || content=="") {
				throw new ArgumentNullException("argument 'content' must contain a non empty value formatted as described by type");
			}

            ValidatePath(path);

			// RestSharp, sadly, adheres to the spec and won't put params in the URI when using POST method
			// This method always performs batched/sync append
			var paramString = String.Format("?{0}={1}&mode=batch&receipt=true", Params.API_KEY, _apiKey);

			var request = new RestRequest(Services.INGEST + _basePath + path + paramString + format.RequestParameters(), Method.POST);
			// Although we're raw on input, we need this to properly deserialize the result
			request.RequestFormat = DataFormat.Json;

			// We bypass serialization here, since we're "raw"
			request.AddParameter(format.ContentType, content, ParameterType.RequestBody);

			IRestResponse response = _client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new PrecogException("Error appending data: " + response.Content);
			}

			return DeserializeAppendResult(response.Content);
		}

		/// <summary>
		/// Append raw string data at the specified path.
		/// </summary>
		/// <description>
		/// For those needing more control over serialization, or
		/// non-JSON append, this method allows full control over the
		/// data content. The data will be sent using chunked
		/// encoding.
        /// <example>
        /// For example, to append CSV data:
		///   <code>
		///     client.AppendRaw("/some/test/path", Formats.CSV, "this,is,the,header\n,these,are,the,values");
		///   </code>
        /// </example>
		/// </description>
		/// <param name='path'>
		/// The storage path for the data.
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='content'>
		/// The content to be appended. No processing is performed on this content.
		/// </param>
        public AppendResult AppendRawStream(string path, AppendFormat format, Stream content)
        {
            return AppendRawStream(path, format, 0, content);
        }

		/// <summary>
		/// Append raw string data at the specified path.
		/// </summary>
		/// <description>
		/// For those needing more control over serialization, or
		/// non-JSON append, this method allows full control over the
		/// data content.
        /// <example>
        /// For example, to append CSV data:
		///   <code>
		///     client.AppendRaw("/some/test/path", Formats.CSV, "this,is,the,header\n,these,are,the,values");
		///   </code>
        /// </example>
		/// </description>
		/// <param name='path'>
		/// The storage path for the data.
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
        /// <param name='contentLength'>
        /// The length of the content to be read from the Stream. If this is zero, the transfer will be performed using chunked encoding.
        /// </param>
		/// <param name='content'>
		/// The content to be appended. No processing is performed on this content.
		/// </param>
        public AppendResult AppendRawStream(string path, AppendFormat format, long contentLength, Stream content)
        {
			var uriString = String.Format ("{0}/ingest/v1/fs{1}{2}?{3}={4}&mode=batch&receipt=true", _client.BaseUrl, _basePath, path, Params.API_KEY, _apiKey);

			// RestSharp doesn't do raw bodies (?!?!?!)
			HttpWebRequest http = (HttpWebRequest) HttpWebRequest.Create(uriString);

			http.Method = "POST";
			http.ContentType = format.ContentType;

            if (contentLength > 0)
            {
                http.ContentLength = contentLength;
            }
            else
            {
                http.SendChunked = true;
            }

            using (Stream output = http.GetRequestStream())
            {
                content.CopyTo(output);
            }

			using (HttpWebResponse response = (HttpWebResponse) http.GetResponse())
			{
				var body = ReadResponseBody(response);

				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new PrecogException("Error appending data: " + body);
				}

                return DeserializeAppendResult(body);
			}
        }

        private AppendResult DeserializeAppendResult(string rawContent)
        {
            //Console.WriteLine("Got AppendResult: " + rawContent);

            var raw = SimpleJson.DeserializeObject<JsonObject>(rawContent);

            var errors = new List<AppendError>();
            foreach (object error in ((JsonArray) raw["errors"]))
            {
                var err = new AppendError();
                err.Line = (int) (long) ((JsonObject) error)["line"];
                err.Reason = (string) ((JsonObject) error)["reason"];
                errors.Add(err);
            }

            return new AppendResult((int) (long) raw["total"],
                                    (int) (long) raw["ingested"],
                                    (int) (long) raw["failed"],
                                    new IngestCompletion((string) raw["ingestId"]),
                                    errors);
        }

        /// <summary>
        ///   Reads the response content into a string for parsing/processing
        /// </summary>
		private string ReadResponseBody(HttpWebResponse response)
		{
			var buffer = new byte[response.ContentLength];

			var input = response.GetResponseStream();

			// Downcasting to an int is safe here because we know that
			// we'll only ever return small messages/JSON
			input.Read(buffer, 0, (int) response.ContentLength);

			return System.Text.UTF8Encoding.UTF8.GetString(buffer);
		}

		/// <summary>
		/// Appends the file specified at the given path with the given MIME type.
		/// </summary>
		/// <param name='path'>
		/// The storage path for the data in the file.
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='filename'>
		/// The name of the file to append.
		/// </param>
		public AppendResult AppendFromFile(string path, AppendFormat format, string filename)
		{
			using (FileStream input = new FileStream(filename, FileMode.Open))
			{
                return AppendRawStream(path, format, (new FileInfo(filename)).Length, input);
			}
		}

		/// <summary>
		/// Uploads the file specified at the given path, overwriting any existing data at that path.
		/// </summary>
		/// <param name='path'>
		/// The storage path for the data in the file.
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='filename'>
		/// The name of the file to append.
		/// </param>
		public AppendResult UploadFile(string path, AppendFormat format, string filename)
		{
            Delete(path);

			using (FileStream input = new FileStream(filename, FileMode.Open))
			{
                return AppendRawStream(path, format, (new FileInfo(filename)).Length, input);
			}
		}

		/// <summary>
		/// Append a single object at the specified path.
		/// </summary>
		/// <description>
		/// The internal JSON machinery is
		/// used to serialize the data. If you need full control over serialization,
		/// use <see cref="M:Append(string, string, Precog.Client.MimeType)"/> instead.
		/// </description>
        /// <example>
		///   Assume we have a struct we would like to append:
		/// <code>
		/// public class MyData
		/// {
		///	    public string Name { get; set; }
		///	    public int Count { get; set; }
		/// }
		/// </code>
		/// We can construct an instance and use our client to append it directly:
		/// <code>
		/// var foo = new MyData();
		/// //... fill in values
		///
		/// client.Append("/test/data/path", foo);
		/// </code>
        /// </example>
		/// <param name='path'>
		/// The storage path for the record.
		/// </param>
		/// <param name='record'>
		/// A serializable record object.
		/// </param>
		public AppendResult Append<T>(string path, T record)
		{
			return AppendRaw(path, Formats.JSON, _serializer.Serialize(record));
		}

		/// <summary>
		/// Append a multiple objects at the specified path. The internal JSON machinery is
		/// used to serialize the data. If you need full control over serialization,
		/// use <see cref="M:Append(string, string, Precog.Client.MimeType)"/> instead.
		/// </summary>
		/// <param name='path'>
		/// The storage path for the record.
		/// </param>
		/// <param name='records'>
		/// A collection of serializable objects.
		/// </param>
		public AppendResult AppendAll<T>(string path, IEnumerable<T> records)
		{
			return AppendRaw(path, Formats.JSON, _serializer.Serialize(records));
		}

		/// <summary>
		/// Deletes the data at the specified path.
		/// </summary>
        /// <remarks>
        ///   <para>
        ///     Deletes the data s
        ///   </para>
        /// </remarks>
		/// <param name='path'>
		/// The path. E.g. "/1234567890/test" maps to "/ingest/v1/fs/1234567890/test".
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
				throw new PrecogException("Error deleting data: " + result.Content);
			}
		}

		/// <summary>
		/// Execute the given query using the specified base path. No limit, skip or sorting is performed.
		/// </summary>
		/// <param name='path'>
		/// The base path for the query. If "/", all paths inside the query must be fully-specified.
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
		/// Execute the given query using the specified base path.
		/// </summary>
		/// <param name='path'>
		/// The base path for the query. If "/", all paths inside the query must be fully-specified.
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

			return DeserializeQueryResult<T>(result.Content);
		}

        private QueryResult<T> DeserializeQueryResult<T>(string rawContent) where T : new()
        {
            var raw = SimpleJson.DeserializeObject<JsonObject>(rawContent);

			// The following code works around a yet-to-be-determined bug in RestSharp deserialization of nested classes
			var dataRaw = raw["data"].ToString();
            var errors = SimpleJson.DeserializeObject<IList<MessageReport>>(raw["errors"].ToString());
			var warnings = SimpleJson.DeserializeObject<IList<MessageReport>>(raw["warnings"].ToString());

			var data = new List<T>(((JsonArray) raw["data"]).Select<object, T>(x => SimpleJson.DeserializeObject<T>(x.ToString())));

            if (raw.Keys.Contains("serverErrors"))
            {
                var serverErrors = new List<string>(((JsonArray) raw["serverErrors"]).Select<object, string>(x => x.ToString()));

                return new QueryResult<T>(data, dataRaw, serverErrors, errors, warnings);
            }
            else
            {
                return new QueryResult<T>(data, dataRaw, new List<string>(), errors, warnings);
            }
        }

		/// <summary>
		/// Execute the given query using the specified base path and default options. The raw
		/// JSON string result is returned directly.
		/// </summary>
		/// <param name='path'>
		/// The base path for the query. If "/", all paths inside the query must be fully-specified.
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
		/// The base path for the query. If "/", all paths inside the query must be fully-specified.
		/// For example, the following two queries are equivalent:
		/// <code>
		/// client.QueryRaw("/1234567890/test", "count(//foo)");
		/// client.QueryRaw("/", "count(//1234567890/foo/test)");
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

        public AsyncQuery QueryAsync(string path, string query)
        {
            ValidatePath(path);

            var request = new RestRequest(Services.ANALYTICS_ASYNC + String.Format("?apiKey={0}&prefixPath={1}&q={2}", _apiKey, _basePath + path, query), Method.POST);

            var response = _client.Execute<AsyncQuery>(request);

			if (response.StatusCode != HttpStatusCode.Accepted)
			{
				throw new PrecogException("Error during async query : " + response.Content);
			}

			return response.Data;
        }

        public string QueryResultsRaw(AsyncQuery query)
        {
            var request = new RestRequest(Services.ANALYTICS_ASYNC + String.Format("/{0}?apiKey={1}", query.JobId, _apiKey), Method.GET);

            var response = _client.Execute(request);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new PrecogException("Error retrieving async results ({0}): {1}", response.StatusCode, response.Content);
			}

			return response.Content;
        }

        public QueryResult<T> QueryResults<T>(AsyncQuery query) where T : new()
        {
			return DeserializeQueryResult<T>(QueryResultsRaw(query));
        }

		private IRestResponse<T> QueryInternal<T>(string path, string query, QueryOptions options) where T: new()
		{
            var finalPath = ValidatePath(path);

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

        private string ValidatePath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("Path cannot be null");
            }

            if (! path.StartsWith("/"))
            {
                throw new ArgumentException("Invalid path. Paths must start with \"/\": \"{0}\"", path);
            }

			// Horrid workaround for overzealous RestSharp RESTification of GET URIs
			return path == "/" ? "//" : path;
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
            public const string ANALYTICS_ASYNC = "/analytics/v1/queries";
		}

		private RestClient _client;
		private string _apiKey;
		private string _basePath;
		private JsonSerializer _serializer;
	}
}