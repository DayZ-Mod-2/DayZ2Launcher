using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using SharpCompress.Common;
using SharpCompress.Reader;

// ReSharper disable InconsistentNaming
namespace zombiesnu.DayZeroLauncher.App.Core
{
	public class GameUpdater
	{
		public static bool HttpGet(string page, out string responseBody)
		{
			responseBody = null;
			var request = (HttpWebRequest)WebRequest.Create(page);
			request.Method = "GET";
			request.Timeout = 120000; // ms

			try
			{
				using(var response = request.GetResponse())
				{
					using(var responseStream = response.GetResponseStream())
					{
						if(responseStream == null)
						{
							return false;
						}
						var streamReader = new StreamReader(responseStream);
						responseBody = streamReader.ReadToEnd();
						streamReader.Close();
					}
				}
			}
			catch//(Exception e)
			{
				return false;
			}
			return true;
		}
	}
}