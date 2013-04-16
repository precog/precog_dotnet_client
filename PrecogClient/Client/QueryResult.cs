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

namespace Precog.Client
{
	/// <summary>
	/// Encapsulates the details of a query result.
	/// </summary>
	/// <description>
	/// Note that all queries return a List of
	/// data, even if there will be only one result. For example, a count(...) query will
	/// return the count as the first element of the List.
	/// </description>
	public class QueryResult<T> where T: new()
		{
			/// <summary>
			/// The result data. Each element will have been deserialized to
			/// type "T".
			/// </summary>
			public readonly IList<T> Data;

			/// <summary>
			/// The raw JSON String result (this will be an array of result elements)
			/// </summary>
			public readonly string DataRaw;

			/// <summary>
			///	  A List of server-side errors. These are distinct
			///	  from quirrel compile errors and warnings, reported
			///	  in <see cref="Errors" /> and <see cref="Warnings" />
			/// </summary>
			public readonly IList<string> ServerErrors;

			/// <summary>
			///	  A List of quirrel errors reported by the analytics
			///	  service.
			/// </summary>
			public readonly IList<MessageReport> Errors;

			/// <summary>
			///	  A List of quirrel warnings reported by the analytics
			///	  service.
			/// </summary>
			public readonly IList<MessageReport> Warnings;

			/// <summary>
			///	  Construct a new QueryResult object with empty defaults.
			/// </summary>
			internal QueryResult(IList<T> data, string dataRaw, IList<string> serverErrors, IList<MessageReport> errors, IList<MessageReport> warnings)
			{
				Data = data;
				DataRaw = dataRaw;
				ServerErrors = serverErrors;
				Errors = errors;
				Warnings = warnings;
			}
		}

	/// <summary>
	/// Encapsulates an error or warning message along with position and when the message occurred.
	/// </summary>
	public class MessageReport
	{
		/// <summary>
		///	  A description of the error/warning encountered.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		///	  The position, in the query, of the error/warning encountered.
		/// </summary>
		public Position Position { get; set; }
	}

	/// <summary>
	/// Encapsulates the position of an error or warning message.
	/// </summary>
	public class Position
	{
		/// <summary>
		///	  The line of the query related to the message.
		/// </summary>
		public int Line { get; set; }

		/// <summary>
		///	  The column of the query related to the message.
		/// </summary>
		public int Column { get; set; }

		/// <summary>
		///	  Additional (optional) information related to the message.
		/// </summary>
		public string Text { get; set; }
	}
}

