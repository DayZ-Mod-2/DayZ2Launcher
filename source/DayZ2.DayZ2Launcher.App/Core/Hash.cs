using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DayZ2.DayZ2Launcher.App.Core
{
	static class Hash
	{
		public static string HashStringSha256(string data)
		{
			using (SHA256 sha256 = SHA256.Create())
				return BytesToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(data)));
		}

		public static string HashFileSha256(string path)
		{
			using (SHA256 sha256 = SHA256.Create())
			{
				using (FileStream fileStream = File.OpenRead(path))
				{
					return BytesToHexString(sha256.ComputeHash(fileStream));
				}
			}
		}
		public static string HashFileSha256(FileInfo file) => HashFileSha256(file.FullName);

		public static string BytesToHexString(byte[] bytes)
		{
			return String.Concat(bytes.Select(n => n.ToString("x2")));
		}
	}
}
