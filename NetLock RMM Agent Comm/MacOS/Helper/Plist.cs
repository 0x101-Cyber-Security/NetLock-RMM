using Global.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MacOS.Helper
{
    public static class Plist
    {
        public static Dictionary<string, object> Parse(string plistContent)
        {
            try
            {
                var xDoc = XDocument.Parse(plistContent);
                var dictElement = xDoc.Descendants("dict").FirstOrDefault();

                if (dictElement == null)
                {
                    throw new InvalidOperationException("Plist contains no dict element.");
                }

                return ParseDict(dictElement);
            }
            catch (Exception ex)
            {
                Logging.Error("MacOS.Helper.Plist.Parse", "Error parsing plist", ex.ToString());
                return new Dictionary<string, object>();
            }
        }

        private static Dictionary<string, object> ParseDict(XElement dictElement)
        {
            try
            {
                var result = new Dictionary<string, object>();
                var keys = dictElement.Elements("key").ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    var key = keys[i].Value;
                    var valueElement = keys[i].NextNode as XElement;
                    if (valueElement != null)
                    {
                        var value = ParseValue(valueElement);
                        result[key] = value;
                        //Console.WriteLine($"Parsed key: {key}, Value: {value}"); // Debug-Ausgabe
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Logging.Error("MacOS.Helper.Plist.ParseDict", "Error parsing dict", ex.ToString());
                return new Dictionary<string, object>();
            }
        }


        private static object ParseValue(XElement valueElement)
        {
            try
            {
                switch (valueElement.Name.LocalName)
                {
                    case "string":
                        return valueElement.Value;
                    case "integer":
                        if (long.TryParse(valueElement.Value, out var longValue))
                        {
                            return longValue;
                        }
                        else
                        {
                            //Console.WriteLine($"Error parsing integer: {valueElement.Value}");
                            return 0L; // Default value if parsing fails
                        }
                    case "real":
                        return double.TryParse(valueElement.Value, out var doubleValue) ? doubleValue : 0.0;
                    case "true":
                        return true;
                    case "false":
                        return false;
                    case "dict":
                        return ParseDict(valueElement);
                    case "array":
                        return ParseArray(valueElement);
                    default:
                        return valueElement.Value;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("MacOS.Helper.Plist.ParseValue", "Error parsing value", ex.ToString());
                return null;
            }
        }


        private static List<object> ParseArray(XElement arrayElement)
        {
            try
            {
                var result = new List<object>();
                foreach (var childElement in arrayElement.Elements())
                {
                    result.Add(ParseValue(childElement));
                }
                return result;
            }
            catch (Exception ex)
            {
                Logging.Error("MacOS.Helper.Plist.ParseArray", "Error parsing array", ex.ToString());
                return new List<object>();
            }
        }
    }
}
