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

namespace Precog.Client
{
	/// <summary>
	///	  Represents the identifier for an asynchronous query (see <see cref="PrecogClient.QueryAsync" />).
	/// </summary>
	public class AsyncQuery
	{
		/// <summary>
		///	  The async job ID for the query
		/// </summary>
		public string JobId { get; set; }

		/// <summary>
		///	  Construct an empty AsyncQuery result.
		/// </summary>
		public AsyncQuery() : this("")
		{
		}

		/// <summary>
		///	  Construct an AsyncQuery result with the specified id.
		/// </summary>
		public AsyncQuery(string jobId)
		{
			JobId = jobId;
		}
	}

	/*
	/// <summary>
	///	  Represents
	/// </summary>
	public class IngestCompletion
	{
		public readonly string Id;

		public IngestCompletion(string id)
		{
			Id = id;
		}
	}
	*/
}
