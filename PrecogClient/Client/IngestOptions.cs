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

namespace Precog.Client
{
	/// <summary>
	/// This class is used to control ingest parameters.
	/// </summary>
	public class IngestOptions
	{
		/// <summary>
		/// The MIME type of the content being ingested. If this doesn't match the ingest service will reject the data.
		/// </summary>
		public MimeType MimeType { get; set; }

		/// <summary>
		/// In certain special cases where the ingest service is unable to determine data ownership based on
		/// api key alone, you must explicitly send the account id that will own the ingested data.
		/// </summary>
	    public string OwnerAccountId { get; set;}
		internal bool HasOwnerAccountId
		{
			get { return OwnerAccountId != null; }
		}

	    public IngestOptions()
		{
			MimeType = MimeType.JSON;
	    }

		public IngestOptions(MimeType mimeType)
		{
			MimeType = mimeType;
		}
	}
}

