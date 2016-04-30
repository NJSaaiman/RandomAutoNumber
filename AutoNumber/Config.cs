
using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace AutoNumber
{
    internal static class Config
    {

        public static string GetDisplayFont { get { return GetString("NumbersFont"); } }

        private static string GetString(string keyName)
        {
            return GetKey(keyName);
        }

        private static string GetKey(string keyName)
        {
            if (ConfigurationManager.AppSettings.AllKeys.Contains(keyName, StringComparer.OrdinalIgnoreCase))
            {
                return ConfigurationManager.AppSettings[keyName];
            }
            else
            {
                return "";
            }
        }
    }
}
