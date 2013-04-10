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
	/// Represents the result of an Ingest/Store call
	/// </summary>
	public class IngestResult
	{
		/// <summary>
		/// The total number of records attempted.
		/// </summary>
		public int Total {get; set; }

		/// <summary>
		/// The total number of records successfully ingested.
		/// </summary>
    	public int Ingested { get; set; }

		/// <summary>
		/// The total number of records that failed to ingest.
		/// </summary>
		public int Failed { get; set; }

		/// <summary>
		/// The Ingest ID that can be used to track end-to-end storage of the events.
		/// </summary>
		public string IngestId { get; set; }

		/// <summary>
		/// A List of any errors that occurred during ingest.
		/// </summary>
    	public List<IngestError> Errors { get; set; }

		public IngestResult()
		{
			Errors = new List<IngestError>();
		}
	}

	/// <summary>
	/// Encapsulates information about errors during ingest.
	/// </summary>
	public class IngestError
	{
		/// <summary>
		/// The line of the input on which the error occured.
		/// </summary>
		public int Line { get; set; }

		/// <summary>
		/// The ingest error reason message.
		/// </summary>
		public string Reason { get; set; }
	}
}

