using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StringCodec.UWP.Common
{
    static public class TextCodecs
    {
        public enum CODEC {URL, BASE64, UUE, XXE, RAW, QUOTED, THUNDER, FLASHGET};

        static public class BASE64
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                byte[] arr = Encoding.Default.GetBytes(text);
                var opt = Base64FormattingOptions.None;
                if (LineBreak) opt = Base64FormattingOptions.InsertLineBreaks;
                result = Convert.ToBase64String(arr, opt);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                byte[] arr = Convert.FromBase64String(text);
                //result = Encoding.UTF8.GetString(arr);
                result = enc.GetString(arr);

                return (result);
            }

        }

        static public class URL
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text);

                return (result);
            }

        }

        static public class UUE
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text);

                return (result);
            }

        }

        static public class XXE
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text);

                return (result);
            }

        }

        static public class RAW
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text).Replace("%","\\x");

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text.Replace("\\x","%"));

                return (result);
            }

        }

        static public class QUOTED
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;

                result = Uri.EscapeDataString(text);

                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Uri.UnescapeDataString(text);

                return (result);
            }

        }

        static public class THUNDER
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;
                result = $"thunder://{BASE64.Encode($"AA{text}ZZ", LineBreak)}";
                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                var url = Regex.Replace(text, @"^thunder://(.*?)$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                url = BASE64.Decode(url, enc);                
                result = Regex.Replace(url, @"^AA(.*?)ZZ$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return (result);
            }

        }

        static public class FLASHGET
        {
            static public string Encode(string text, bool LineBreak = false)
            {
                string result = string.Empty;
                result = $"flashget://{BASE64.Encode($"[FLASHGET]{text}[FLASHGET]", LineBreak)}";
                return (result);
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                var url = Regex.Replace(text, @"^flashget://(.*?)$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                url = BASE64.Decode(url, enc);
                result = Regex.Replace(url, @"^\[FLASHGET\](.*?)\[FLASHGET\]$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return (result);
            }

        }

        static public string Encode(string content, CODEC Codec, bool LineBreak = false)
        {
            string result = string.Empty;
            switch (Codec)
            {
                case CODEC.URL:
                    result = URL.Encode(content);
                    break;
                case CODEC.BASE64:
                    result = BASE64.Encode(content, LineBreak);
                    break;
                case CODEC.UUE:
                    result = UUE.Encode(content);
                    break;
                case CODEC.XXE:
                    result = XXE.Encode(content);
                    break;
                case CODEC.RAW:
                    result = RAW.Encode(content);
                    break;
                case CODEC.QUOTED:
                    result = QUOTED.Encode(content);
                    break;
                case CODEC.THUNDER:
                    result = THUNDER.Encode(content);
                    break;
                case CODEC.FLASHGET:
                    result = FLASHGET.Encode(content);
                    break;
                default:
                    break;
            }
            return (result);
        }

        static public string Decode(string content, CODEC Codec, Encoding enc)
        {
            string result = string.Empty;
            switch (Codec)
            {
                case CODEC.URL:
                    result = URL.Decode(content, enc);
                    break;
                case CODEC.BASE64:
                    result = BASE64.Decode(content, enc);
                    break;
                case CODEC.UUE:
                    result = UUE.Decode(content, enc);
                    break;
                case CODEC.XXE:
                    result = XXE.Decode(content, enc);
                    break;
                case CODEC.RAW:
                    result = RAW.Decode(content, enc);
                    break;
                case CODEC.QUOTED:
                    result = QUOTED.Decode(content, enc);
                    break;
                case CODEC.THUNDER:
                    result = THUNDER.Decode(content, enc);
                    break;
                case CODEC.FLASHGET:
                    result = FLASHGET.Decode(content, enc);
                    break;
                default:
                    break;
            }
            return (result);
        }
    }
}
