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

using Precog.Client;

namespace Precog.Client
{
	/// <summary>
	/// Represents the info for a given account
	/// </summary>
	public class AccountInfo
	{
		/// <summary>
		///	  The account id for this account.
		/// </summary>
		public string AccountId { get; set; }

		/// <summary>
		///	  The email associated with this account.
		/// </summary>
		public string Email { get; set; }

		/// <summary>
		///	  The time at which the account was created.
		/// </summary>
		public DateTime AccountCreationDate { get; set; }

		/// <summary>
		///	  The api key for this account.
		/// </summary>
		public string ApiKey { get; set; }

		/// <summary>
		///	  The root path for this account. Unless the account is
		///	  granted access outside of its default root, this should
		///	  prefix any storage/query paths.
		/// </summary>
		public string RootPath { get; set; }

		/// <summary>
		///	  The JSON profile data stored with the account on
		///	  creation. Returned here as a raw string.
		/// </summary>
		public string Profile { get; set; }

		private string _plan = "Free";

		/// <summary>
		///	  The plan type for this account.
		/// </summary>
		public string Plan
		{
			get
			{
				return _plan;
			}
			set
			{
				if (value.ToLower().Contains("type"))
				{
					var parsed = SimpleJson.DeserializeObject<JsonObject>(value);
					_plan = (string) parsed["type"];
				}
				else
				{
					_plan = value;
				}
			}
		}

		/// <summary>
		///	  Create an empty AccountInfo instance.
		/// </summary>
		public AccountInfo()
		{
			AccountId  = "";
			Email  = "";
			AccountCreationDate	 = DateTime.Now;
			ApiKey	= "";
			RootPath  = "";
			Profile	 = "";
			Plan = "";
			// Everything else should come from the REST result. This is optional, so we make sure to fill it in.
			Profile = "";
		}
	}
}

namespace Precog.Client.Dto
{
	internal class AccountCreate
	{
		public AccountCreate(string email, string password)
		{
			this.email = email;
			this.password = password;
		}

		public string email { get; set; }
		public string password { get; set; }
	}

	internal class AccountCreateProfiled : AccountCreate
	{
		public AccountCreateProfiled(string email, string password, String profile) : base(email, password)
		{
			this.profile = profile;
		}

		public string profile { get; set; }
	}

	internal class AccountCreateResponse
	{
		public string accountId { get; set; }
	}
}

