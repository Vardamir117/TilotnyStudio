using System.Collections.Generic;
using System.Globalization;

namespace Holocron
{
    public static class LuaParser
    {
        public static string ExtractLuaQuotedValue(string line)
        {
            int start = line.IndexOf('"');
            if (start < 0) return "";

            int end = line.IndexOf('"', start + 1);
            if (end < 0) return "";

            return line.Substring(start + 1, end - start - 1);
        }

        public static string ExtractLuaBracketQuotedKey(string line)
        {
            int bracket = line.IndexOf('[');
            if (bracket < 0) return "";

            int quote_start = line.IndexOf('"', bracket);
            if (quote_start < 0) return "";

            int quote_end = line.IndexOf('"', quote_start + 1);
            if (quote_end < 0) return "";

            return line.Substring(quote_start + 1, quote_end - quote_start - 1);
        }

        public static string ExtractLuaAssignedValue(string line)
        {
            int equal = line.IndexOf('=');
            if (equal < 0) return "";

            string value = line.Substring(equal + 1).Trim();
            if (value.EndsWith(";")) value = value.Substring(0, value.Length - 1).Trim();
            return value;
        }

        public static List<string> ParseLuaStringArray(string line)
        {
            List<string> corenne = new List<string>();
            int open = line.IndexOf('{');
            int close = line.LastIndexOf('}');
            if (open < 0 || close <= open) return corenne;

            string inner = line.Substring(open + 1, close - open - 1);
            string[] split = inner.Split(',');
            foreach (string entry in split)
            {
                string trimmed = entry.Trim().Trim('"');
                if (trimmed != "") corenne.Add(trimmed);
            }

            return corenne;
        }

        public static List<float> ParseLuaFloatArray(string line)
        {
            List<float> corenne = new List<float>();
            int open = line.IndexOf('{');
            int close = line.LastIndexOf('}');
            if (open < 0 || close <= open) return corenne;

            string inner = line.Substring(open + 1, close - open - 1);
            string[] split = inner.Split(',');
            foreach (string entry in split)
            {
                float value;
                if (float.TryParse(entry.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    corenne.Add(value);
                }
            }

            return corenne;
        }
    }
}
