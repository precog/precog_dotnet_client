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
using System.Web;

namespace Precog.Client
{
	using Dto;
	using Format;

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
	/// </summary>
	/// <remarks>
	///	  <para>
	/// This client provides two classes of methods for all non-Delete operations: cooked and raw. Cooked
	/// methods (Store, StoreMany, Query) use generic types for serialization and deserialization
	/// of the data into and out of user-specified types.
	///
	/// Raw methods (Ingest, QueryRaw) either consume or produce raw strings containing the data
	/// to be ingested or returned from a query.
	///	  </para>
	/// </remarks>
	public class PrecogClient
	{
		/// <summary>
		///	  The default endpoint for use by client instances. Client
		///	  static and instance constructors default to this <see cref="Uri" />
		///	  unless one is provided.
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
		/// Thrown if the apiKey or basePath is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Thrown if the apiKey is empty or if basePath is invalid.
		/// </exception>
		public PrecogClient(string apiKey, string basePath): this(DEFAULT_ENDPOINT, apiKey, basePath) {}

		/// <summary>
		/// Creates a new client.
		/// </summary>
		/// <param name='endpoint'>
		/// The endpoint URI to use for client operations. This is not required to be HTTPS.
		/// </param>
		/// <param name='apiKey'>
		/// The api key for this client.
		/// </param>
		/// <param name='basePath'>
		/// The base path to use for operations with this client.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the endpoint, apiKey or basePath is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Thrown if the apiKey is empty or if basePath is invalid.
		/// </exception>
		public PrecogClient(Uri endpoint, string apiKey, string basePath)
		{
			ValidateArg(endpoint, "endpoint");
			ValidateArg(apiKey, "apiKey");
			ValidatePath(basePath);

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

			_endpoint = endpoint;
		}

		/// <summary>
		/// Creates a new account with the default endpoint, accessible by the
		/// specified email address and password.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		An Exception is thrown if an account already exists for the given email address.
		///	  </para>
		/// </remarks>
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
		/// specified email address and password, with the specified profile.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		An Exception is thrown if an account already exists for the given email address.
		///	  </para>
		/// </remarks>
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
		/// <exception cref='ArgumentException'>
		/// Thrown if the email or password is empty.
		/// </exception>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the email or password is null.
		/// </exception>
		public static AccountInfo CreateAccount(String email, String password, String profile)
		{
			return CreateAccount(DEFAULT_ENDPOINT, email, password, profile);
		}

		/// <summary>
		/// Creates a new account with the default endpoint, accessible by the
		/// specified email address and password, with the specified profile.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		An Exception is thrown if an account already exists for the given email address.
		///	  </para>
		/// </remarks>
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
		/// Profile data to be associated with the account. Profile
		/// data is serialized to JSON internally. If control over
		/// serialization is required, use the <see cref="M:CreateAccount(string, string, string)" /> method.
		/// </param>
		/// <exception cref='ArgumentException'>
		/// Thrown if the email or password is empty.
		/// </exception>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the email or password is null.
		/// </exception>
		public static AccountInfo CreateAccount(String email, String password, object profile)
		{
			return CreateAccount(DEFAULT_ENDPOINT, email, password, SimpleJson.SerializeObject(profile));
		}

		/// <summary>
		/// Creates a new account with the specified endpoint, accessible by the
		/// specified email address and password.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		 An Exception is thrown if an account already exists for the given email address.
		///	  </para>
		/// </remarks>
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
		/// <exception cref='ArgumentException'>
		/// Thrown if the email or password is empty.
		/// </exception>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the endpoint, email or password is null.
		/// </exception>
		public static AccountInfo CreateAccount(Uri endpoint, String email, String password)
		{
			return CreateAccount(endpoint, email, password, "");
		}

		/// <summary>
		/// Creates a new account with the specified endpoint, accessible by the
		/// specified email address and password, with the specified profile data.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		An Exception is thrown if an account already exists
		///		for the given email address, or if the endpoint is not
		///		HTTPS.
		///	  </para>
		/// </remarks>
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
		/// <exception cref='ArgumentException'>
		/// Thrown if the email or password is empty.
		/// </exception>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the endpoint, email or password is null.
		/// </exception>
		public static AccountInfo CreateAccount(Uri endpoint, String email, String password, String profile)
		{
			ValidateArg(endpoint, "endpoint");
			ValidateArg(email, "email");
			ValidateArg(password, "password");

			if (endpoint.Scheme != "https")
			{
				throw new PrecogException("HTTPS is required for all account-related operations. Invalid endpoint: " + endpoint);
			}

			AccountCreate createInfo = null;
			if (profile == null || profile == "")
			{
				createInfo = new AccountCreate(email, password);
			}
			else
			{
				createInfo = new AccountCreateProfiled(email, password, profile);
			}

			var request = CreateRequest(endpoint.ToString() + "accounts/v1/accounts/", "POST");

			WriteRequestBody(request, SimpleJson.SerializeObject(createInfo));

			var result = SimpleJson.DeserializeObject<AccountCreateResponse>(ValidatedResponse(request, "creating account"));

			return AccountDetails(endpoint, email, password, result.accountId);
		}

		/// <summary>
		/// Creates a new account with the specified endpoint, accessible by the
		/// specified email address and password, with the specified profile data.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		An Exception is thrown if an account already exists
		///		for the given email address, or if the endpoint is not
		///		HTTPS.
		///	  </para>
		/// </remarks>
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
		/// Profile data to be associated with the account. Profile
		/// data is serialized to JSON internally. If control over
		/// serialization is required, use the <see cref="M:CreateAccount(Uri, string, string, string)" /> method.
		/// </param>
		/// <exception cref='ArgumentException'>
		/// Thrown if the email or password is empty.
		/// </exception>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the endpoint, email or password is null.
		/// </exception>
		public static AccountInfo CreateAccount(Uri endpoint, String email, String password, object profile)
		{
			return CreateAccount(endpoint, email, password, SimpleJson.SerializeObject(profile));
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
		/// <exception cref='ArgumentException'>
		/// Thrown if the email, password or accountId is empty.
		/// </exception>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the email, password or accountid is null.
		/// </exception>
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
		/// <exception cref='ArgumentException'>
		/// Thrown if the email, password or accountId is empty.
		/// </exception>
		/// <exception cref='ArgumentNullException'>
		/// Thrown if the endpoint, email, password or accountid is null.
		/// </exception>
		public static AccountInfo AccountDetails(Uri endpoint, string email, string password, string accountId)
		{
			ValidateArg(endpoint, "endpoint");
			ValidateArg(email, "email");
			ValidateArg(password, "password");
			ValidateArg(accountId, "accountId");

			if (endpoint.Scheme != "https")
			{
				throw new PrecogException("HTTPS is required for all account-related operations. Invalid endpoint: " + endpoint);
			}

			var request = CreateRequest(endpoint.ToString() + "accounts/v1/accounts/" + accountId);
			var credentials = new NetworkCredential(email, password);

			request.Credentials = credentials;
			request.PreAuthenticate = true;

			var response = ValidatedResponse(request, "fetching account details");

			var result = SimpleJson.DeserializeObject<JsonObject>(response);

			var profile = "";

			if (result.Keys.Contains("profile"))
			{
				profile = (string) result["rootPath"];
			}

			var accountId2 = (string) result["accountId"];
			var email2 = (string) result["email"];
			var createdAt = DateTime.Parse((string) result["accountCreationDate"]);
			var passwordChangeAt = DateTime.Parse((string) result["lastPasswordChangeTime"]);
			var apiKey = (string) result["apiKey"];
			var rootPath = (string) result["rootPath"];
			var plan = (string) ((JsonObject) result["plan"])["type"];

			var info = new AccountInfo(accountId2,
									   email2,
									   createdAt,
									   passwordChangeAt,
									   apiKey,
									   rootPath,
									   profile,
									   plan);

			//Console.WriteLine("Got response: " + response);
			//Console.WriteLine("Got info: " + SimpleJson.SerializeObject(info));

			return info;
		}

		/// <summary>
		/// Append raw strings at the specified path with the given owner account Id.
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
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='content'>
		/// The content to be appended. No processing is performed on this content.
		/// </param>
		/// <param name='ownerAccountId'>
		/// The optional owner account id to use for this append. This
		/// is only required if the account cannot be deduced from the
		/// api key for this client. null indicates omission.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path, format or content is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path or content is empty.
		/// </exception>
		public AppendResult AppendRaw(string path, AppendFormat format, string content, string ownerAccountId)
		{
			if (content == null)
			{
				throw new ArgumentNullException("argument 'content' must contain a non empty value formatted as described by type");
			}

			if (content == "")
			{
				throw new ArgumentException("argument 'content' must contain a non empty value formatted as described by type");
			}

			ValidateArg(format, "format");
			ValidatePath(path);

			// RestSharp, sadly, adheres to the spec and won't put params in the URI when using POST method
			// This method always performs batched/sync append
			var paramString = String.Format("?{0}={1}&mode=batch&receipt=true", Params.API_KEY, _apiKey);

			if (ownerAccountId != null)
			{
				paramString += ("&ownerAccountId=" + ownerAccountId);
			}

			var request = CreateRequest(_endpoint.ToString() + Services.INGEST + _basePath + path + paramString + format.RequestParameters(), "POST", format.ContentType);

			WriteRequestBody(request, content);

			return DeserializeAppendResult(ValidatedResponse(request, "appending data"));
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
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='content'>
		/// The content to be appended. No processing is performed on this content.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path, format or content is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path or content is empty.
		/// </exception>
		public AppendResult AppendRaw(string path, AppendFormat format, string content)
		{
			return AppendRaw(path, format, content, null);
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
		///	  <code>
		///		client.AppendRaw("/some/test/path", Formats.CSV, "this,is,the,header\n,these,are,the,values");
		///	  </code>
		/// </example>
		/// </description>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='content'>
		/// The content to be appended. No processing is performed on this content.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path, format or content is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty.
		/// </exception>
		public AppendResult AppendRawStream(string path, AppendFormat format, Stream content)
		{
			return AppendRawStream(path, format, 0, content, null);
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
		///	  <code>
		///		client.AppendRaw("/some/test/path", Formats.CSV, "this,is,the,header\n,these,are,the,values");
		///	  </code>
		/// </example>
		/// </description>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
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
		/// <param name='ownerAccountId'>
		/// The optional owner account id to use for this append. This
		/// is only required if the account cannot be deduced from the
		/// api key for this client. null indicates omission.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path, format or content is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty.
		/// </exception>
		public AppendResult AppendRawStream(string path, AppendFormat format, long contentLength, Stream content, string ownerAccountId)
		{
			ValidatePath(path);
			ValidateArg(format, "format");
			ValidateArg(content, "content");

			var uriString = String.Format ("{0}ingest/v1/fs{1}{2}?{3}={4}&mode=batch&receipt=true", _endpoint.ToString(), _basePath, path, Params.API_KEY, _apiKey);

			if (ownerAccountId != null)
			{
				uriString += ("&ownerAccountId=" + ownerAccountId);
			}

			var request =  CreateRequest(uriString, "POST", format.ContentType);

			if (contentLength > 0)
			{
				request.ContentLength = contentLength;
			}
			else
			{
				request.SendChunked = true;
			}

			using (Stream output = request.GetRequestStream())
			{
				content.CopyTo(output);
			}

			return DeserializeAppendResult(ValidatedResponse(request, "appending data"));
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
									//new IngestCompletion((string) raw["ingestId"]),
									errors);
		}

		private static HttpWebRequest CreateRequest(string url)
		{
			return CreateRequest(url, "GET", "application/json");
		}

		private static HttpWebRequest CreateRequest(string url, string method)
		{
			return CreateRequest(url, method, "application/json");
		}

		private static HttpWebRequest CreateRequest(string url, string method, string mimeType)
		{
			var uri = new Uri(url);
			var request = (HttpWebRequest) HttpWebRequest.Create(uri);
			request.Method = method;
			request.ContentType = mimeType;

			return request;
		}

		/// <summary>
		///	  Reads the response content into a string for parsing/processing
		/// </summary>
		private static string ReadResponseBody(HttpWebResponse response)
		{
			var body  = "";
			using (StreamReader sr = new StreamReader(response.GetResponseStream(), new UTF8Encoding()))
			{
				body = sr.ReadToEnd();
			}

			return body;
		}

		private static void WriteRequestBody(HttpWebRequest request, string data)
		{
			//Console.WriteLine("Sending: " + data);

			var encoding = new UTF8Encoding(false);
			var bytes = encoding.GetBytes(data);

			request.ContentLength = bytes.Length;

			using (var output = request.GetRequestStream())
			{
				output.Write(bytes, 0, bytes.Length);
			}
		}

		private static string ValidatedResponse(HttpWebRequest request, string description)
		{
			return ValidatedResponse(request, description, HttpStatusCode.OK);
		}

		private static string ValidatedResponse(HttpWebRequest request, string description, HttpStatusCode expectedCode)
		{
			//Console.WriteLine("Running {0} {1}", request.Method, request.RequestUri);

			using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
			{
				var body = ReadResponseBody(response);

				//Console.WriteLine("Got: " + body);

				if (response.StatusCode != expectedCode)
				{
					throw new PrecogException("Error {0} against {1}. RC = {2}, content = {3}", request.RequestUri, response.StatusCode, body);
				}

				return body;
			}
		}

		/// <summary>
		/// Appends the file specified at the given path with the given MIME type.
		/// </summary>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='filename'>
		/// The name of the file to append.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path, format or filename is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path or file content is empty.
		/// </exception>
		/// <exception cref='System.IO.FileNotFoundException'>
		/// Is thrown if the filename does not correspond to a file.
		/// </exception>
		public AppendResult AppendFromFile(string path, AppendFormat format, string filename)
		{
			return AppendFromFile(path, format, filename, null);
		}

		/// <summary>
		/// Appends the file specified at the given path with the given MIME type and owner account Id.
		/// </summary>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='filename'>
		/// The name of the file to append.
		/// </param>
		/// <param name='ownerAccountId'>
		/// The optional owner account id to use for this append. This
		/// is only required if the account cannot be deduced from the
		/// api key for this client. null indicates omission.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path, format or filename is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path or file content is empty.
		/// </exception>
		/// <exception cref='System.IO.FileNotFoundException'>
		/// Is thrown if the filename does not correspond to a file.
		/// </exception>
		public AppendResult AppendFromFile(string path, AppendFormat format, string filename, string ownerAccountId)
		{
			if ((new FileInfo(filename)).Length == 0)
			{
				throw new ArgumentException("The file {0} is empty and cannot be appended.", filename);
			}

			using (FileStream input = new FileStream(filename, FileMode.Open))
			{
				return AppendRawStream(path, format, (new FileInfo(filename)).Length, input, ownerAccountId);
			}
		}

		/// <summary>
		/// Uploads the file specified at the given path, overwriting any existing data at that path.
		/// </summary>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='filename'>
		/// The name of the file to append.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path, format or filename is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path or file content is empty.
		/// </exception>
		/// <exception cref='System.IO.FileNotFoundException'>
		/// Is thrown if the filename does not correspond to a file.
		/// </exception>
		public AppendResult UploadFile(string path, AppendFormat format, string filename)
		{
			return UploadFile(path, format, filename, null);
		}

		/// <summary>
		/// Uploads the file specified at the given path, overwriting any existing data at that path.
		/// </summary>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='format'>
		/// Indicates the content type of the append data.
		/// </param>
		/// <param name='filename'>
		/// The name of the file to append.
		/// </param>
		/// <param name='ownerAccountId'>
		/// The optional owner account id to use for this append. This
		/// is only required if the account cannot be deduced from the
		/// api key for this client. null indicates omission.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path, format or filename is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path or file content is empty.
		/// </exception>
		/// <exception cref='System.IO.FileNotFoundException'>
		/// Is thrown if the filename does not correspond to a file.
		/// </exception>
		public AppendResult UploadFile(string path, AppendFormat format, string filename, string ownerAccountId)
		{
			Delete(path);

			using (FileStream input = new FileStream(filename, FileMode.Open))
			{
				return AppendRawStream(path, format, (new FileInfo(filename)).Length, input, ownerAccountId);
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
		///	  Assume we have a struct we would like to append:
		/// <code>
		/// public class MyData
		/// {
		///		public string Name { get; set; }
		///		public int Count { get; set; }
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
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='record'>
		/// A serializable record object.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path or record is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty.
		/// </exception>
		public AppendResult Append<T>(string path, T record)
		{
			return Append<T>(path, record, null);
		}

		/// <summary>
		///	  Append a single object at the specified path with the specified owner account Id.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		For more details see <see cref="M:Append(string, T)" />.
		///	  </para>
		/// </remarks>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='record'>
		/// A serializable record object.
		/// </param>
		/// <param name='ownerAccountId'>
		/// The optional owner account id to use for this append. This
		/// is only required if the account cannot be deduced from the
		/// api key for this client. null indicates omission.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path or record is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty.
		/// </exception>
		public AppendResult Append<T>(string path, T record, string ownerAccountId)
		{
			ValidateArg(record, "record");
			return AppendRaw(path, Formats.JSON, SimpleJson.SerializeObject(record), ownerAccountId);
		}

		/// <summary>
		/// Append a multiple objects at the specified path.
		/// </summary>
		/// <remarks>
		///	  <para>
		/// The internal JSON machinery is
		/// used to serialize the data. If you need full control over serialization,
		/// use <see cref="M:AppendRaw(string, AppendFormat, string)"/> instead.
		///	  </para>
		/// </remarks>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='records'>
		/// A collection of serializable objects.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path or records are null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty.
		/// </exception>
		public AppendResult AppendAll<T>(string path, IEnumerable<T> records)
		{
			return AppendAll<T>(path, records, null);
		}

		/// <summary>
		/// Append a multiple objects at the specified path with the given owner account Id.
		/// </summary>
		/// <remarks>
		///	  <para>
		/// The internal JSON machinery is
		/// used to serialize the data. If you need full control over serialization,
		/// use <see cref="M:AppendRaw(string, AppendFormat, string)"/> instead.
		///	  </para>
		/// </remarks>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <param name='records'>
		/// A collection of serializable objects.
		/// </param>
		/// <param name='ownerAccountId'>
		/// The optional owner account id to use for this append. This
		/// is only required if the account cannot be deduced from the
		/// api key for this client. null indicates omission.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path or records are null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty.
		/// </exception>
		public AppendResult AppendAll<T>(string path, IEnumerable<T> records, string ownerAccountId)
		{
			ValidateArg(records, "records");
			return AppendRaw(path, Formats.JSON, SimpleJson.SerializeObject(records), ownerAccountId);
		}

		/// <summary>
		/// Deletes the data at the specified path.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		Deletes the data s
		///	  </para>
		/// </remarks>
		/// <param name='path'>
		/// The storage path for the data. The path must begin with a "/"
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty or does not start with a "/".
		/// </exception>
		public void Delete(string path)
		{
			ValidatePath(path);

			var request = CreateRequest(_endpoint.ToString() + Services.INGEST + _basePath + path + "?apiKey=" + _apiKey, "DELETE");

			ValidatedResponse(request, "deleting data");
		}

		/// <summary>
		/// Execute the given query using the specified base path. No limit, skip or sorting is performed.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The result type T must have a public no-arg
		///     constructor and properties matching the expected
		///     return fields of the query. For example, if the query
		///     returns results like '{"foo":12, "bar":"baz"}', you
		///     could provide a type like
		/// <code>
		/// public class MyFooBar
		/// {
		///     public int Foo { get; set; }
		///     public string Bar { get; set; }
		/// }
		/// </code>
		///   </para>
		///   <para>
		///     If the provided type does not contain properties that
		///     match the returned results, the values returned will
		///     simply be instances constructed by the no-arg
		///     constructor. It's the end-user's responsibility to
		///     validate returned instances.
		///   </para>
		/// </remarks>
		/// <param name='path'>
		/// The base path for the query (must begin with "/"). If "/",
		/// all paths inside the query must be fully-specified.  For
		/// example, the following two queries are equivalent:
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
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path or query is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty or does not start with a "/", or if the query is empty.
		/// </exception>
		public QueryResult<T> Query<T>(string path, string query) where T: new()
		{
			return Query<T>(path, query, new QueryOptions());
		}

		/// <summary>
		/// Execute the given query using the specified base path.
		/// </summary>
		/// <remarks>
		///   <para>
		///     The result type T must have a public no-arg
		///     constructor and properties matching the expected
		///     return fields of the query. For example, if the query
		///     returns results like '{"foo":12, "bar":"baz"}', you
		///     could provide a type like
		/// <code>
		/// public class MyFooBar
		/// {
		///     public int Foo { get; set; }
		///     public string Bar { get; set; }
		/// }
		/// </code>
		///   </para>
		///   <para>
		///     If the provided type does not contain properties that
		///     match the returned results, the values returned will
		///     simply be instances constructed by the no-arg
		///     constructor. It's the end-user's responsibility to
		///     validate returned instances.
		///   </para>
		/// </remarks>
		/// <param name='path'>
		/// The base path for the query (must begin with "/"). If "/",
		/// all paths inside the query must be fully-specified.  For
		/// example, the following two queries are equivalent:
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
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path or query is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty or does not start with a "/", or if the query is empty.
		/// </exception>
		public QueryResult<T> Query<T>(string path, string query, QueryOptions options) where T: new()
		{
			return DeserializeQueryResult<T>(QueryInternal(path, query, options));
		}

		private QueryResult<T> DeserializeQueryResult<T>(string rawContent) where T : new()
		{
			//Console.WriteLine("Deserializing " + rawContent);
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
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path or query is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty or does not start with a "/", or if the query is empty.
		/// </exception>
		public String QueryRaw(string path, string query)
		{
			return QueryInternal(path, query, new QueryOptions());
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
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path, query or options are null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty or does not start with a "/", or if the query is empty.
		/// </exception>
		public String QueryRaw(string path, string query, QueryOptions options)
		{
			return QueryInternal(path, query, options);
		}

		/// <summary>
		/// Execute the given query asynchronously using the specified base path.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		The results from querys executed with this method may
		///		be retrieved with the <see cref="QueryResults" />,
		///		<see cref="QueryResultsRaw" />, or <see
		///		cref="DownloadQueryResults" /> methods.
		///	  </para>
		/// </remarks>
		/// <example>
		///	  var handle = client.QueryAsync("/", "count(//foo)");
		///	  var result = client.QueryResultsRaw(handle);
		/// </example>
		/// <returns>An <see cref="AsyncQuery" /> handle that can be used to retrieve the query results</returns>
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
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the path or query is null.
		/// </exception>
		/// <exception cref='ArgumentException'>
		/// Is thrown if the path is empty or does not start with a "/", or if the query is empty.
		/// </exception>
		public AsyncQuery QueryAsync(string path, string query)
		{
			ValidatePath(path);
			ValidateArg(query, "query");

			var request = CreateRequest(_endpoint.ToString() + Services.ANALYTICS_ASYNC + String.Format("?apiKey={0}&prefixPath={1}&q={2}", _apiKey, HttpUtility.UrlEncode(_basePath + path), HttpUtility.UrlEncode(query)), "POST");

			var result =  SimpleJson.DeserializeObject<JsonObject>(ValidatedResponse(request, "querying", HttpStatusCode.Accepted));

			return new AsyncQuery((string) result["jobId"]);
		}

		/// <summary>
		///	  Returns the results from an asynchronous query in raw form.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		This method does not yet provide a means to wait for
		///		the query to complete, but this will be fixed
		///		shortly. The JSON object returned carries three
		///		fields: errors, warnings and data. The data field is
		///		an array of JSON elements representing the results.
		///	  </para>
		/// </remarks>
		/// <example>
		///	  var handle = client.QueryAsync("/", "count(//foo)");
		///	  var result = client.QueryResultsRaw(handle);
		/// </example>
		/// <returns>The raw JSON result object from the query.</returns>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the query is null.
		/// </exception>
		public string QueryResultsRaw(AsyncQuery query)
		{
			var request = CreateRequest(_endpoint.ToString() + Services.ANALYTICS_ASYNC + String.Format("/{0}?apiKey={1}", query.JobId, _apiKey));

			return ValidatedResponse(request, "obtaining query results");
		}

		/// <summary>
		///	  Returns the deserialized results from an asynchronous query.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		This method does not yet provide a means to wait for
		///		the query to complete, but this will be fixed
		///		shortly. Similar to the <see cref="M:Query(string, string)" /> method,
		///		the results are deserialized using a basic internal
		///		JSON deserializer. If more control over
		///		deserialization is required, use the <see
		///		cref="QueryResultsRaw" /> method.
		///	  </para>
		/// </remarks>
		/// <example>
		///	  var handle = client.QueryAsync("/", "count(//foo)");
		///	  var result = client.QueryResults&lt;int&gt;(handle);
		/// </example>
		/// <returns>A <see cref="QueryResults" /> representing the result of the query computation.</returns>
		/// <param name='query'>
		/// The query to return results for
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the query is null.
		/// </exception>
		public QueryResult<T> QueryResults<T>(AsyncQuery query) where T : new()
		{
			return DeserializeQueryResult<T>(QueryResultsRaw(query));
		}

		/// <summary>
		///	  Downloads results from the given async query.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		Results are downloaded as a JSON object containing
		///		"errors", "warnings" and "data" fields, with the
		///		result set in "data". This method will throw an
		///		exception if the file already exists.
		///	  </para>
		/// </remarks>
		/// <example>
		///	  var handle = client.QueryAsync("/", "count(//foo)");
		///	  var result = client.DownloadQueryResults(handle, "test.json");
		/// </example>
		/// <returns></returns>
		/// <param name='query'>
		/// The query to return results for
		/// </param>
		/// <param name='filename'>
		/// The name of the file in which to store the results.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown if the query is null.
		/// </exception>
		/// <exception cref='System.IO.IOException'>
		/// Is thrown if an error occurs during writing to the file
		/// </exception>
		public void DownloadQueryResults(AsyncQuery query, string filename)
		{
			var request = CreateRequest(String.Format ("{0}analytics/v1/queries/{1}?apiKey={2}&format=simple", _endpoint.ToString(), query.JobId, _apiKey));

			using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
			{
				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new PrecogException("Error appending data. Code = {0}, Response = \"{1}\"", response.StatusCode, ReadResponseBody(response));
				}

				using (FileStream output = new FileStream(filename, FileMode.CreateNew))
				{
					response.GetResponseStream().CopyTo(output);
				}
			}
		}

		private string QueryInternal(string path, string query, QueryOptions options)
		{
			ValidateArg(query, "query");
			ValidateArg(options, "options");
			ValidatePath(path);

			var queryParams = String.Format("?format=detailed&apiKey={0}&q={1}", _apiKey, HttpUtility.UrlEncode(query));

			if (options.Limit > 0)
			{
				queryParams += "&limit=" + options.Limit;
			}

			if (options.Skip != 0)
			{
				queryParams += "&skip" +  options.Skip;
			}

			if (options.SortOn != null)
			{
				var sortOrder = "asc";

				if (options.SortOrder == QueryOptions.Order.DESCENDING)
				{
					sortOrder = "desc";
				}

				queryParams += String.Format("&sortOn={0}&sortOrder={1}", "[" + String.Join(",", options.SortOn) + "]", sortOrder);
			}

			var request = CreateRequest(_endpoint.ToString() + Services.ANALYTICS + _basePath + path + queryParams);

			return ValidatedResponse(request, "querying");
		}

		private void ValidatePath(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException("Path cannot be null");
			}

			if (! path.StartsWith("/"))
			{
				throw new ArgumentException("Invalid path. Paths must start with \"/\": \"{0}\"", path);
			}
		}

		private static void ValidateArg(object arg, string name)
		{
			if (arg == null)
			{
				throw new ArgumentNullException("{0} must not be null", name);
			}

			if (arg is string && ((string) arg) == "")
			{
				throw new ArgumentException("{0} must not be empty", name);
			}
		}

		private static class Params
		{
			public const string API_KEY = "apiKey";
			public const string QUERY = "q";
			public const string MODE = "mode";
		}

		private static class Services
		{
			public const string INGEST	  = "ingest/v1/fs";
			public const string ANALYTICS = "analytics/v1/fs";
			public const string ANALYTICS_ASYNC = "analytics/v1/queries";
		}

		private Uri _endpoint;
		private string _apiKey;
		private string _basePath;
	}
}