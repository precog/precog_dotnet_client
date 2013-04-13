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
    /// Represents the result of an Append/Store call
    /// </summary>
    public class AppendResult
    {
        /// <summary>
        /// The total number of records attempted.
        /// </summary>
        public readonly int Total;

        /// <summary>
        /// The total number of records successfully appended.
        /// </summary>
        public readonly int Ingested;

        /// <summary>
        /// The total number of records that failed to append.
        /// </summary>
        public readonly int Failed;

        /// <summary>
        /// The Append ID that can be used to track end-to-end storage of the events.
        /// </summary>
        public readonly IngestCompletion IngestJob;

        /// <summary>
        /// A List of any errors that occurred during append.
        /// </summary>
        public readonly List<AppendError> Errors;

        /// <summary>
        ///   Create a new AppendResult instance
        /// </summary>
        internal AppendResult(int total, int ingested, int failed, IngestCompletion ingestId, List<AppendError> errors)
        {
            Total = total;
            Ingested  = ingested;
            Failed  = failed;
            IngestJob  = ingestId;
            Errors = errors;
        }
    }

    /// <summary>
    /// Encapsulates information about errors during append.
    /// </summary>
    public class AppendError
    {
        /// <summary>
        /// The line of the input on which the error occured.
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// The append error reason message.
        /// </summary>
        public string Reason { get; set; }
    }
}

