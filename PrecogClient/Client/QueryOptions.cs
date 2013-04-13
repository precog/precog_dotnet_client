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
	/// This class controls query parameters.
	/// </summary>
	public class QueryOptions
	{
        /// <summary>
        ///   The order for use in sorting.
        /// </summary>
		public enum Order
		{
            /// <summary>
            ///   Sorts from lowest to highest.
            /// </summary>
			ASCENDING,

            /// <summary>
            ///   Sorts from highest to lowest.
            /// </summary>
			DESCENDING
		}

        /// <summary>
        ///   Constructs a new QueryOptions instance that will return
        ///   unlimited results, starts at the first result element
        ///   (zero offset), and sorts ascending (although by default
        ///   no sort fields are specified)
        /// </summary>
		public QueryOptions ()
		{
			Limit = 0;
			Skip = 0;
			SortOrder = Order.ASCENDING;
		}

		private int _limit;
		private int _skip;

		/// <summary>
		/// Limits how many results are returned. Setting to zero means unlimited.
		/// </summary>
		public int Limit
		{
			get
			{
				return _limit;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("Limit must be non-negative: " + value);
				}

				_limit = value;
			}
		}

		/// <summary>
		/// Sets an offset (in result element count) at which to begin results.
		/// </summary>
        /// <example language="c#">
        /// Limit and Skip can be used to page results, if desired.
        /// <code>
        /// public QueryResult&lt;T&gt; Paged(string path, string query, int pageSize, int pageNumber)
		/// {
		///		var options = new QueryOptions();
		///		options.Limit = pageSize;
		///		options.Skip = pageSize * (pageNumber - 1)
		///		return client.Query&lt;T&gt;(path, query, options);
		/// }
        /// </code>
        /// </example>
		public int Skip
		{
			get
			{
				return _skip;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("Skip must be non-negative: " + value);
				}

				_skip = value;
			}
		}

		/// <summary>
		/// A List of field names to order on, in dotted notation. The sort is performend with
		/// precedence in the order that the names occur in the List.
        /// <example language="c#">
		/// <code>
		/// var opts = new QueryOptions();
		/// opts.SortOn(new List&lt;string&gt; { "foo", "foo.bar", "baz" });
        /// var results = client.Query&lt;T&gt;(path, query, opts);
		/// </code>
        /// </example>
		/// </summary>
		public IList<string> SortOn { get; set; }

        /// <summary>
        ///   Controls the sort order (either <see cref="Order.ASCENDING" /> or <see cref="Order.DESCENDING" />)
        /// </summary>
		public Order SortOrder { get; set; }
	}
}

