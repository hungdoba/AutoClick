using System.Runtime.InteropServices;
using System.Text;

namespace AutoClick.Utils
{
    internal class IniFile
    {
        private readonly string _filePath;

        public IniFile(string filePath)
        {
            _filePath = filePath;
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder retVal, int size, string filePath);

        public void WriteValue(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _filePath);
        }

        public string ReadValue(string section, string key, string defaultValue = "")
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, retVal, 255, _filePath);
            return retVal.ToString();
        }

        public void WriteBoolean(string section, string key, bool value)
        {
            WriteValue(section, key, value.ToString());
        }

        public bool ReadBoolean(string section, string key, bool defaultValue = false)
        {
            var value = ReadValue(section, key, defaultValue.ToString());
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

    }
}
