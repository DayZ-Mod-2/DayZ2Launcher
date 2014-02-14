using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Security.AccessControl;

namespace zombiesnu.DayZeroLauncher.App.Core
{
    class GUIDCalculator
    {
        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private static string keyHexToKey(byte[] keyHex)
        {
            string arg = "";
            string text = "0123456789ABCDEFGHJKLMNPRSTVWXYZ";
            for (int i = 0; i < 3; i++)
            {
                ulong num = 0uL;
                for (int j = 0; j < 5; j++)
                {
                    num <<= 8;
                    num |= (ulong)keyHex[i * 5 + j];
                }
                for (int j = 0; j < 8; j++)
                {
                    ulong num2 = num >> j * 5 & 31uL;
                    char c = text[(int)num2];
                    arg += c;
                }
            }
            return arg;
        }

        private static string MD5Hex(string password)
        {
            MD5 mD = new MD5CryptoServiceProvider();
            byte[] bytes = Encoding.Default.GetBytes(password);
            byte[] array = mD.ComputeHash(bytes);
            string str = "";
            byte[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                byte b = array2[i];
                str += string.Format("{0:x2}", b);
            }
            return str;
        }

        public static string GetKey()
        {
            try
            {
                const string regKeyName = "SOFTWARE\\Bohemia Interactive Studio\\ArmA 2 OA";
                RegistryKey baseKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine,RegistryView.Registry32);
                byte[] keyHex = (byte[])baseKey.OpenSubKey(regKeyName, RegistryKeyPermissionCheck.Default,RegistryRights.QueryValues).GetValue("KEY");
                string text = keyHexToKey(keyHex);
                string password = string.Concat(new string[]
            {
                    text.Substring(0, 4),
                    "-",
                    text.Substring(4, 5),
                    "-",
                    text.Substring(9, 5),
                    "-",
                    text.Substring(14, 5),
                    "-",
                    text.Substring(19, 5)
            });

                return MD5Hex("BE" + MD5Hex(password));
            }
            catch (Exception)
            {
                return "Could not calculate guid.";
            }

        }
    }
}
