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
using System.Net.Mime;

namespace Precog.Client.Format
{
	/// <summary>
	/// This class represents the formats support for Append/Upload of data.
	/// </summary>
	public class AppendFormat
	{
		/// <summary>
		/// The MIME type of the content being appended. If this doesn't match the ingest service will reject the data.
		/// </summary>
		public readonly string ContentType;

		internal virtual string RequestParameters()
		{
			return "";				  
		}

		/// <summary>
		///	  Create a new AppendOption with the specified MIME type 
		/// </summary>
		internal AppendFormat(string mimeType)
		{
			ContentType = mimeType;
		}
	}

	/// <summary>
	///	  A collection of formats supported for Append/Upload of data.
	/// </summary>
	public static class Formats
	{
		/// <summary>
		///	  The standard JSON type for single JSON elements or JSON
		///	  arrays of elements.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		The JSON format indicates either a root-level JSON
		///		element (literal or object), or a JSON array of
		///		elements. In the latter case, each element of the
		///		root-level array is ingested as a separate event. If
		///		you actually want to ingest a JSON array as an event,
		///		us the <see cref="JSONStream" /> format.
		///	  </para>
		/// </remarks>
		/// <example>
		///	  In JSON format, root-level JSON arrays are treated as a
		///	  collection of JSON elements to be processed as separate
		///	  events. This means that the following code produces
		///	  identical ingests.
		/// <code>
		/// client.AppendRaw("/test", "{\"test\":1}", Formats.JSON)
		/// client.AppendRaw("/test", "[{\"test\":1}]", Formats.JSON)
		/// </code>
		/// </example>
		public static AppendFormat JSON = new AppendFormat("application/json");

		/// <summary>
		///	  A JSON type indicated concatenated JSON elements.
		/// </summary>
		/// <remarks>
		///	  <para>
		///		The JSONStream format indicates a concatenation of
		///		distinct JSON objects, arrays, or literals in a
		///		stream, without delimiters between elements.
		///	  </para>
		/// </remarks>
		/// <example>
		///	  The following is an example of a JSON stream:
		///	  <code>
		///	  {"test":1}{"test":2}{"foo":{"bar":{"baz":3}}}
		///	  </code>
		/// </example>
		public static AppendFormat JSONStream = new AppendFormat("application/x-json-stream");

		/// <summary>
		///	  Comma-separated values, as defined in <a href="http://tools.ietf.org/html/rfc4180">RFC 4180</a>.
		/// </summary>
		/// <remarks>
		///	  <para>
		///	  This format uses double quotes (&quot;) as a text
		///	  quoting mark and escape character. If you require
		///	  different quote or escape characters, you can directly
		///	  construct a <see cref="DelimitedFormat" />.
		///	  </para>
		/// </remarks>
		public static AppendFormat CSV = new DelimitedFormat(',');

		/// <summary>
		///	  Tab-separated values, following the guidelines in <a href="http://tools.ietf.org/html/rfc4180">RFC 4180</a>.
		/// </summary>
		/// <remarks>
		///	  <para>
		///	  This format uses double quotes (&quot;) as a text
		///	  quoting mark and escape character. If you require
		///	  different quote or escape characters, you can directly
		///	  construct a <see cref="DelimitedFormat" />.
		///	  </para>
		/// </remarks>
		public static AppendFormat TSV = new DelimitedFormat('\t');

		/// <summary>
		///	  Semicolon-separated values, following the guidelines in <a href="http://tools.ietf.org/html/rfc4180">RFC 4180</a>.
		/// </summary>
		/// <remarks>
		///	  <para>
		///	  This format uses double quotes (&quot;) as a text
		///	  quoting mark and escape character. If you require
		///	  different quote or escape characters, you can directly
		///	  construct a <see cref="DelimitedFormat" />.
		///	  </para>
		/// </remarks>
		public static AppendFormat SSV = new DelimitedFormat(';');
	}

	/// <summary>
	///	  A format of delimited text, following the guidelines in <a href="http://tools.ietf.org/html/rfc4180">RFC 4180</a>.
	/// </summary>
	/// <remarks>
	///	  <para>
	///		This format requires a delimiter, quote character and
	///		escape character. The escape character is used to
	///		represent the quote character inside of quoted
	///		values. Although the actualy delimiter is configurable,
	///		because we follow the general format of <a
	///		href="http://tools.ietf.org/html/rfc4180">RFC 4180</a>,
	///		the MIME type is text/csv.
	///	  </para>
	/// </remarks>
	/// <example>
	///	  As an example, you could process Informix dumps with:
	/// <code>
	/// var informixFormat = new DelimitedFormat("|", "\\", "\"");
	/// </code>
	/// </example>
	public class DelimitedFormat : AppendFormat
	{
		/// <summary>
		///	  Create a new DelimitedFormat with the specified delimiter, using the double quote (&quot;) for escape and quoting characters.
		/// </summary>
		public DelimitedFormat(char delimiter) : this(delimiter, '\"', '\"')
		{		
		}

		/// <summary>
		///	  Create a new DelimitedFormat with the specified delimiter, escape, and quote characters.
		/// </summary>		  
		public DelimitedFormat(char delimiter, char escape, char quote) : base("text/csv")
		{
			Delimiter = delimiter;
			Escape = escape;
			Quote = quote;
		}

		internal override string RequestParameters()
		{
			return base.RequestParameters() + String.Format("&delimiter={0}&quote={1}&escape={2}", Delimiter, Quote, Escape);
		}

		/// <summary>
		///	  The character used to separate fields
		/// </summary>
		public readonly char Delimiter;

		/// <summary>
		///	  The character used to quote values in fields
		/// </summary>
		public readonly char Quote;

		/// <summary>
		///	  The character used to escape quote characters found within quoted values
		/// </summary>
		public readonly char Escape;
	}
}

