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
		public string AccountId { get; set; }
		public string Email { get; set; }
		public DateTime AccountCreationDate { get; set; }
		public string ApiKey { get; set; }
		public string RootPath { get; set; }
		private string _plan = "Free";
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

	internal class AccountCreateResponse
	{
		public string accountId { get; set; }
	}
}

