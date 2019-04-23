using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;


namespace StringCodec.UWP.Common
{
    static public class TextCodecs
    {
        public static string[] LINEBREAK = new string[]{ "\r\n", "\n\r", "\r", "\n", Environment.NewLine };

        public static byte[] hexRange = Encoding.Default.GetBytes("0123456789abcdefABCDEF");

        public enum CODEC {
            URL, HTML, BASE64, UUE, XXE, RAW, UNICODEVALUE, UNICODEGLYPH, QUOTED,
            THUNDER, FLASHGET,
            UUID, GUID,
            MORSE, MORSEABBR,
        };

        public enum HASH
        {
            MD5, MD4, eDonkey, eMule,
            RC2,
            AES, DES, RSA,
            SHA1, SHA256, SHA384, SHA512,
            CRC32,
        }

        #region Basic Encoder/Decoder
        static public class BASE64
        {
            static public async Task<string> Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;
                try
                {
                    byte[] arr = text;
                    var opt = Base64FormattingOptions.None;
                    if (LineBreak) opt = Base64FormattingOptions.InsertLineBreaks;
                    result = Convert.ToBase64String(arr, opt);
                }
                catch (Exception ex)
                {
                    await new MessageDialog($"{"BASE64ERROR".T()}\n{ex.Message}", "ERROR".T()).ShowAsync();
                }

                return (result);
            }

            static public async Task<string> Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;
                try
                {
                    byte[] arr = enc.GetBytes(text);
                    var opt = Base64FormattingOptions.None;
                    if (LineBreak) opt = Base64FormattingOptions.InsertLineBreaks;
                    result = Convert.ToBase64String(arr, opt);
                }
                catch (Exception ex)
                {
                    await new MessageDialog($"{"BASE64ERROR".T()}\n{ex.Message}", "ERROR".T()).ShowAsync();
                }

                return (result);
            }

            static public async Task<string> Encode(string text, bool LineBreak = false)
            {
                return (await Encode(text, Encoding.Default, LineBreak));
            }

            static public async Task<string> Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                try
                {
                    if (text.Length > 512)
                    {
                        var lines = text.Split(new char[]{ '\r', '\n' });
                        StringBuilder sb = new StringBuilder();
                        foreach(var line in lines)
                        {
                            var l = string.IsNullOrEmpty(line) ? string.Empty : line.Trim();
                            if (string.IsNullOrEmpty(l)) continue;
                            if (l.StartsWith("--=")) continue;
                            if (l.IndexOf("charset")>=0)
                            {
                                var charset = l.Substring(l.IndexOf("charset")).Split(new char[] { '=', ':' });
                                if (charset.Length == 2) enc = GetTextEncoder(charset[1].Trim(new char[] { '"', ';' }));
                                continue;
                            }
                            if(l.StartsWith("Content-", StringComparison.CurrentCultureIgnoreCase)) continue;
                            sb.AppendLine(l);
                        }
                        text = sb.ToString();
                    }
                    byte[] arr = Convert.FromBase64String(text);
                    //result = Encoding.UTF8.GetString(arr);
                    result = enc.GetString(arr);
                }
                catch (Exception ex)
                {
                    await new MessageDialog($"{"BASE64ERROR".T()}\n{ex.Message}", "ERROR".T()).ShowAsync();
                }

                return (result);
            }
        }

        static public class URL
        {
            static private string Escape(string text, Encoding enc)
            {
                string result = string.Empty;

                if (enc == Encoding.UTF8)
                    result = Uri.EscapeDataString(text);
                else
                {
                    char[] symbol = new char[]{ ' ', ':', ';', '<', '=', '>', '?', '@', '[', '\'', ']', '^', '_', '`',  '&', '#', '"', '%', '|' };
                    var asciis = enc.GetBytes(text);
                    List<string> ReturnString = new List<string>();
                    for (int i = 0; i < asciis.Length; i++)
                    {
                        var ascii = asciis[i];
                        if (ascii < 47 || symbol.Contains((char)ascii) || ascii > 122)
                            ReturnString.Add($"%{Convert.ToInt32(ascii):X2}");
                        else
                            ReturnString.Add($"{(char)ascii}");
                    }
                    result = string.Join("", ReturnString);
                }

                return (result);
            }

            static private string Unescape(string text, Encoding enc)
            {
                string result = string.Empty;

                if (enc == Encoding.UTF8)
                    result = Uri.UnescapeDataString(text);//.Replace('+', ' ');
                else
                {
                    var asciis = Encoding.Default.GetBytes(text);
                    List<byte> ReturnString = new List<byte>();
                    int i = 0;
                    while (i < asciis.Length)
                    {
                        var ascii = asciis[i];
                        if (ascii == '%')
                        {
                            var hex = $"{(char)asciis[i + 1]}{(char)asciis[i + 2]}";
                            ReturnString.Add(byte.Parse(hex, System.Globalization.NumberStyles.HexNumber));
                            i += 2;
                        }
                        else
                            ReturnString.Add(ascii);
                        i++;
                    }
                    result = enc.GetString(ReturnString.ToArray());
                }

                return (result);
            }

            static public string Encode(byte[] text, Encoding enc)
            {
                return (Encode(enc.GetString(text), enc));
            }

            static public string Encode(string text, Encoding enc)
            {
                string result = string.Empty;

                try
                {
                    var url = new Uri(text);
                    var uInfo = Escape(Uri.UnescapeDataString(url.UserInfo), enc).Replace("%3A", ":", StringComparison.CurrentCultureIgnoreCase);
                    var uPath = Escape(Uri.UnescapeDataString(url.AbsolutePath), enc).Replace("%2F", "/", StringComparison.CurrentCultureIgnoreCase);

                    var frag = string.Empty;
                    var fragIdx = url.Fragment.LastIndexOf('#');
                    var fragment = url.Fragment;
                    if (fragIdx > 0)
                    {
                        frag = url.Fragment.Substring(0, fragIdx);
                        fragment = Escape(Uri.UnescapeDataString(url.Fragment.Substring(fragIdx)), enc).Replace("%23", "#", StringComparison.CurrentCultureIgnoreCase);
                    }
                    else
                        frag = url.Fragment.Contains('&') ? url.Fragment : string.Empty;


                    var query = Uri.UnescapeDataString($"{url.Query.TrimStart('?')}{frag}");

                    try
                    {
                        var kv = query.Split('&').Select(q => q.Split('=')).Select(o => o.Length>1 ? $"{Escape(o.First(), enc)}={Escape(string.Join("", o.Skip(1)), enc)}" : $"{Escape(string.Join("", o), enc)}");
                        var qSym = kv.Count()<=0  || string.IsNullOrEmpty(query) ? string.Empty : "?";
                        var uSym = string.IsNullOrEmpty(url.UserInfo) ? string.Empty : "@";
                        result = $"{url.Scheme}://{uInfo}{uSym}{url.DnsSafeHost}{uPath}{qSym}{string.Join("&", kv)}{fragment}".Trim();
                    }
                    catch (Exception)
                    {
                        try
                        {
                            var queryParams = query.Split('&').Select(q => q.Split('='));
                            List<string> queryParamList = new List<string>();
                            foreach (var param in queryParams)
                            {
                                if (param.Length < 1)
                                    continue;
                                else if (param.Length == 1)
                                    queryParamList.Add(Escape(param.First(), enc));
                                else if (param.Length > 1)
                                    queryParamList.Add($"{Escape(param.First(), enc)}={Escape(string.Join("", param.Skip(1)), enc)}");
                            }
                            var qSym = queryParamList.Count()<=0  || string.IsNullOrEmpty(query) ? string.Empty : "?";
                            var uSym = string.IsNullOrEmpty(url.UserInfo) ? string.Empty : "@";
                            result = $"{url.Scheme}://{uInfo}{uSym}{url.DnsSafeHost}{uPath}{qSym}{string.Join("&", queryParamList)}{fragment}".Trim();
                        }
                        catch (Exception ex)
                        {
                            ex.Message.T().ShowMessage("ERROR".T());
                        }
                    }
                }
                catch (Exception)
                {
                    result = Escape(text, enc);
                }

                return (result);
            }

            static public string Encode(string text)
            {
                return (Encode(text, Encoding.Default));
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                result = Unescape(text, enc);

                return (result);
            }
        }

        static public class HTML
        {
            static private string EntityToUnicode(string html, Encoding enc)
            {
                var replacements = new Dictionary<string, string>();
                var regexText = new Regex("(&[a-zA-Z]{2,7};)");
                foreach (Match match in regexText.Matches(html))
                {
                    if (!replacements.ContainsKey(match.Value))
                    {
                        var unicode = System.Web.HttpUtility.HtmlDecode(match.Value);
                        if (unicode.Length == 1)
                        {
                            replacements.Add(match.Value, string.Concat("&#", Convert.ToInt32(unicode[0]), ";"));
                        }
                    }
                }

                var entryPattenDec = new Regex("&\\#([0-9]{1,6});", RegexOptions.IgnoreCase);
                foreach (Match match in entryPattenDec.Matches(html))
                {
                    if (!replacements.ContainsKey(match.Value))
                    {
                        if (match.Groups.Count > 1)
                        {
                            var v = Convert.ToInt32(match.Groups[1].Value);
                            if(127 <= v && v <= 256)
                                replacements.Add(match.Value, $"\\x{Convert.ToInt32(match.Groups[1].Value):X2}");
                            else
                                replacements.Add(match.Value, $"{Convert.ToChar(Convert.ToInt32(match.Groups[1].Value))}");                            
                        }
                    }
                }

                var entryPattenHex = new Regex("&\\#x([a-fA-F0-9]{1,6});", RegexOptions.IgnoreCase);
                foreach (Match match in entryPattenHex.Matches(html))
                {
                    if (!replacements.ContainsKey(match.Value))
                    {
                        if (match.Groups.Count > 1)
                        {
                            var v = Convert.ToInt32(match.Groups[1].Value, 16);
                            if (127 <= v && v <= 256)
                                replacements.Add(match.Value, $"\\x{match.Groups[1].Value}");
                            else
                                replacements.Add(match.Value, $"{Convert.ToChar(Convert.ToInt32(match.Groups[1].Value, 16))}");
                        }
                    }
                }

                foreach (var replacement in replacements)
                {
                    html = html.Replace(replacement.Key, replacement.Value);
                }

                html = RAW.Decode(html, enc);

                return html;
            }

            static private string UnicodeToEntity(string html)
            {
                var replacements = new Dictionary<string, string>();
                foreach (Match match in Regex.Matches(html, @"((^[\u0000-\u007f])|([\u2300-\u23ff])|([\u2600-\u27ff])|([\u1f000-\u1f9ff]))"))
                {
                    if (match.Groups.Count > 1 && !replacements.ContainsKey(match.Value))
                    {
                        var v = Convert.ToInt32(match.Groups[1].Value[0]);
                        if (127 < v) replacements.Add(match.Value, $"&#{v};");
                    }
                }

                foreach (var replacement in replacements)
                {
                    html = html.Replace(replacement.Key, replacement.Value);
                }

                return (html);
            }

            static public async Task<string> Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                return (await Encode(enc.GetString(text), enc, LineBreak));
            }

            static public async Task<string> Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = text.ConvertTo(enc);

                try
                {
                    //result = System.Web.HttpUtility.HtmlEncode(result);
                    result = UnicodeToEntity(System.Net.WebUtility.HtmlEncode(result));
                }
                catch (Exception ex)
                {
                    await new MessageDialog($"HTML{"ERROR".T()}\n{ex.Message}", "ERROR".T()).ShowAsync();
                }

                return (result);
            }

            static public async Task<string> Encode(string text, bool LineBreak = false)
            {
                return (await Encode(text, Encoding.Default, LineBreak));
            }

            static public async Task<string> Decode(string text, Encoding enc)
            {
                string result = text;

                try
                {
                    result = EntityToUnicode(result, enc);
                    //result = EntityToUnicode(System.Net.WebUtility.HtmlDecode(result), enc);
                    //result = result.ConvertFrom(enc);
                }
                catch (Exception ex)
                {
                    await new MessageDialog($"HTML{"ERROR".T()}\n{ex.Message}", "ERROR".T()).ShowAsync();
                }

                return (result);
            }
        }

        static public class UUE
        {
            static public string Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                var contents = new List<char>();

                var t = text.ToList();

                var m = t.Count % 3;
                if (m == 1)
                    t.AddRange(new byte[2] { 0, 0 });
                else if (m == 2)
                    t.Add(0);

                int i = 0;
                while (i < t.Count)
                {
                    //var c = new byte[3]{ text[i], i+1<text.Length ? text[i+1] : (byte)'0', i + 2 < text.Length ? text[i+2] : (byte)'0' };
                    var c = new byte[3]{ t[i], t[i+1], t[i+2] };
                    //var ec = new char[4]
                    //{
                    //    (char)(c[0] / 4 + 32),
                    //    (char)(c[0] % 4 * 16 + c[1] / 16 + 32),
                    //    (char)(c[1] % 16 * 4 + c[2] / 64 + 32),
                    //    (char)(c[2] % 64 + 32)
                    //};
                    var ec = new char[4]
                    {
                        (char)((c[0] >> 2) + 32),
                        (char)((((c[0] << 6 & 0xFF) >> 2) | (c[1] >> 4)) + 32),
                        (char)((((c[1] << 4 & 0xFF) >> 2) | (c[2] >> 6)) + 32),
                        (char)(((c[2] << 2 & 0xFF) >> 2) + 32)
                    };

                    if (ec[0] == 32) ec[0] = (char)96;
                    if (ec[1] == 32) ec[1] = (char)96;
                    if (ec[2] == 32) ec[2] = (char)96;
                    if (ec[3] == 32) ec[3] = (char)96;

                    contents.AddRange(ec);

                    i += 3;
                }

                var lines = new List<string>();
                i = 0;
                int total = contents.Count();
                while (i < total)
                {
                    var line = contents.Skip(i).Take(60);
                    var count = line.Count();
                    if ( count == 60)
                        lines.Add($"M{string.Join("", line)}");
                    else
                        lines.Add($"{(char)(count/4*3+32)}{string.Join("", line)}");

                    i += 60;
                }

                result = string.Join(Environment.NewLine, lines);

                return (result);
            }

            static public string Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                var bytes = enc.GetBytes(text);
                if (bytes is byte[] && bytes.Length > 0)
                    result = Encode(bytes, enc, LineBreak);

                return (result);
            }

            static public string Encode(string text, bool LineBreak = false)
            {
                return (Encode(text, Encoding.Default, LineBreak));
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                var t = new List<char>();
                foreach (var l in text.Split(LINEBREAK, StringSplitOptions.None))
                {
                    if (l.Trim().StartsWith("begin", StringComparison.CurrentCultureIgnoreCase)) continue;
                    else if (l.Trim().StartsWith("end", StringComparison.CurrentCultureIgnoreCase)) continue;
                    t.AddRange(l.Substring(1).ToArray());
                }
                var dt = new List<byte>();
                for (int i = 0; i < t.Count(); i += 4)
                {
                    var c = new byte[4]{ (byte)(t[i]-32), (byte)(t[i+1]-32), (byte)(t[i+2]-32), (byte)(t[i+3]-32) };
                    if (c[0] == 64) c[0] = 0;
                    if (c[1] == 64) c[1] = 0;
                    if (c[2] == 64) c[2] = 0;
                    if (c[3] == 64) c[3] = 0;

                    var dc = new byte[3]
                    {
                        (byte)((c[0] << 2) | ((c[1] >> 4) & 0x03)),
                        (byte)((c[1] << 4) | ((c[2] >> 2) & 0x0F)),
                        (byte)((c[2] << 6) | (c[3]  & 0x3F))
                    };

                    dt.AddRange(dc);
                }
                result = enc.GetString(dt.ToArray()).TrimEnd('\0');

                return (result);
            }
        }

        static public class XXE
        {
            static private string maps = "+-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            static private Dictionary<char, byte> rmaps = new Dictionary<char, byte>();

            static public string Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                var uue = UUE.Encode(text, enc, LineBreak);

                if (!string.IsNullOrEmpty(uue))
                {
                    var xxe = uue.ToArray();
                    for (int i = 0; i< xxe.Length; i++)
                    {
                        if (xxe[i] == '\r' || xxe[i] == '\n') continue;
                        xxe[i] = (char)(xxe[i] - 32);
                        if (xxe[i] == 64) xxe[i] = (char)0;
                        xxe[i] = (char)(maps[xxe[i]]);
                    }
                    result = string.Join("", xxe);
                }

                return (result);
            }

            static public string Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                var bytes = enc.GetBytes(text);
                if (bytes is byte[] && bytes.Length > 0)
                    result = Encode(bytes, enc, LineBreak);

                return (result);
            }

            static public string Encode(string text, bool LineBreak = false)
            {
                return (Encode(text, Encoding.Default, LineBreak));
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                if (!string.IsNullOrEmpty(text))
                {
                    if (rmaps.Count() == 0)
                    {
                        for (int i = 0; i < maps.Length; i++)
                        {
                            rmaps.Add(maps[i], (byte)i);
                        }
                    }

                    var lines = text.Split(LINEBREAK, StringSplitOptions.None);
                    var t = new List<string>();
                    foreach(var l in lines)
                    {
                        if (l.Trim().StartsWith("begin", StringComparison.CurrentCultureIgnoreCase)) continue;
                        else if (l.Trim().StartsWith("end", StringComparison.CurrentCultureIgnoreCase)) continue;
                        t.Add(l);
                    }

                    var xxe = string.Join(Environment.NewLine, t).ToArray();
                    for (int i = 0; i < xxe.Length; i++)
                    {
                        if (xxe[i] == '\r' || xxe[i] == '\n') continue;
                        xxe[i] = (char)(rmaps[xxe[i]]);
                        if (xxe[i] == 0) xxe[i] = (char)64;
                        xxe[i] = (char)(xxe[i] + 32);
                    }
                    result = UUE.Decode(string.Join("", xxe), enc);
                }

                return (result);
            }

        }

        static public class RAW
        {
            static public string Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                List<string> asciis = new List<string>();
                int count = 0;
                foreach(var c in text)
                {
                    asciis.Add($"\\x{c:X2}");
                    if (LineBreak && count % 18 == 0) asciis.Add(Environment.NewLine);
                    count++;
                }
                result = string.Join("", asciis);;

                return (result);
            }

            static public string Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                var bytes = enc.GetBytes(text);
                if (bytes is byte[] && bytes.Length > 0)
                    result = Encode(bytes, enc, LineBreak);

                return (result);
            }

            static public string Encode(string text, bool LineBreak = false)
            {
                return (Encode(text, Encoding.Default, LineBreak));
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                //result = Uri.UnescapeDataString(text.Replace("\\x", "%")).ConvertFrom(enc);

                var raws = new Dictionary<string, string>();
                var entryPattenRaw = new Regex(@"(\\x([a-fA-F0-9]){2})+", RegexOptions.IgnoreCase);
                foreach (Match match in entryPattenRaw.Matches(text))
                {
                    if (!raws.ContainsKey(match.Value))
                    {
                        if (match.Groups.Count > 1)
                        {
                            List<byte> byteList = new List<byte>();                            
                            foreach(Capture c in match.Groups[1].Captures)
                            {
                                byteList.Add((byte)Convert.ToInt32(c.Value.Replace("\\x", ""), 16));
                            }
                            var decoded = enc.GetString(byteList.ToArray());
                            raws.Add(match.Value, decoded);
                        }
                    }
                }

                foreach (var raw in raws)
                {
                    text = text.Replace(raw.Key, raw.Value);
                }

                result = text;

                return (result);
            }
        }

        static public class UnicodeValue
        {
            static public string Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                var content = enc.GetString(text);
                return (Encode(content, enc, LineBreak));
            }

            static public string Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = text;

                var replacements = new Dictionary<string, string>();
                foreach (Match match in Regex.Matches(text, @"([^\u000D\u000A\u0020-\u007E])"))
                {
                    if (match.Groups.Count > 1 && !replacements.ContainsKey(match.Value))
                    {
                        var v = Convert.ToInt32(match.Groups[1].Value[0]);
                        //if (v == 0x0D || v == 0x0A) continue;
                        replacements.Add(match.Value, $"\\u{v:X4}");
                    }
                }

                foreach (var replacement in replacements)
                {
                    result = result.Replace(replacement.Key, replacement.Value);
                }

                return (result);
            }

            static public string Encode(string text, bool LineBreak = false)
            {
                return (Encode(text, Encoding.Default, LineBreak));
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = text;

                var replacements = new Dictionary<string, string>();
                var entryPattenHex = new Regex(@"(\\u([a-fA-F0-9]){4,6})", RegexOptions.IgnoreCase);
                foreach (Match match in entryPattenHex.Matches(text))
                {
                    if (!replacements.ContainsKey(match.Value))
                    {
                        if (match.Groups.Count > 1)
                        {
                            var v = Convert.ToInt32(match.Value.Replace("\\u", ""), 16);
                            replacements.Add(match.Value, $"{Convert.ToChar(v)}");
                        }
                    }
                }

                foreach (var replacement in replacements)
                {
                    result = result.Replace(replacement.Key, replacement.Value);
                }

                return (result);
            }
        }

        static public class UnicodeGlyph
        {
            static public string Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                var content = enc.GetString(text);
                return (Encode(content, enc, LineBreak));
            }

            static public string Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = text;

                var replacements = new Dictionary<string, string>();
                foreach (Match match in Regex.Matches(text, @"([^\u000D\u000A\u0020-\u007E])"))
                {
                    if (match.Groups.Count > 1 && !replacements.ContainsKey(match.Value))
                    {
                        var v = Convert.ToInt32(match.Groups[1].Value[0]);
                        //if (v == 0x0D || v == 0x0A) continue;
                        replacements.Add(match.Value, $"&#x{v:X4};");
                    }
                }

                foreach (var replacement in replacements)
                {
                    result = result.Replace(replacement.Key, replacement.Value);
                }

                return (result);
            }

            static public string Encode(string text, bool LineBreak = false)
            {
                return (Encode(text, Encoding.Default, LineBreak));
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = text;

                var replacements = new Dictionary<string, string>();

                var entryPattenDec = new Regex("&\\#([0-9]{1,6});", RegexOptions.IgnoreCase);
                foreach (Match match in entryPattenDec.Matches(text))
                {
                    if (!replacements.ContainsKey(match.Value))
                    {
                        if (match.Groups.Count > 1)
                        {
                            var v = Convert.ToInt32(match.Groups[1].Value);
                            replacements.Add(match.Value, $"{Convert.ToChar(v)}");
                        }
                    }
                }

                var entryPattenHex = new Regex("&\\#x([a-fA-F0-9]{1,6});", RegexOptions.IgnoreCase);
                foreach (Match match in entryPattenHex.Matches(text))
                {
                    if (!replacements.ContainsKey(match.Value))
                    {
                        if (match.Groups.Count > 1)
                        {
                            var v = Convert.ToInt32(match.Groups[1].Value, 16);
                            replacements.Add(match.Value, $"{Convert.ToChar(v)}");
                        }
                    }
                }

                foreach (var replacement in replacements)
                {
                    result = result.Replace(replacement.Key, replacement.Value);
                }

                return (result);
            }
        }

        static public class QUOTED
        {
            static public string Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                //var asciis = Encoding.Default.GetBytes(text.ConvertTo(enc));
                var asciis = text;
                List<string> ReturnString = new List<string>();
                for (int i = 0; i < asciis.Length; i++)
                {
                    var ascii = asciis[i];
                    if (ascii < 32 || ascii == 61 || ascii > 126)
                        ReturnString.Add($"={Convert.ToInt32(ascii):X2}");
                    else
                        ReturnString.Add($"{(char)ascii}");
                }
                result = string.Join("", ReturnString);

                return (result);
            }

            static public string Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                //var asciis = Encoding.Default.GetBytes(text.ConvertTo(enc));
                var asciis = enc.GetBytes(text);
                List<string> ReturnString = new List<string>();
                for (int i = 0; i < asciis.Length; i++)
                {
                    var ascii = asciis[i];
                    if (ascii < 32 || ascii == 61 || ascii > 126)
                        ReturnString.Add($"={Convert.ToInt32(ascii):X2}");
                    else
                        ReturnString.Add($"{(char)ascii}");
                }
                result = string.Join("", ReturnString);

                return (result);
            }

            static public string Encode(string text, bool LineBreak = false)
            {
                return (Encode(text, Encoding.Default, LineBreak));
            }

            static public string Decode(string text, Encoding enc)
            {
                string result = string.Empty;

                var asciis = Encoding.Default.GetBytes(text);
                List<byte> retBytes = new List<byte>();
                int i = 0;
                while (i < asciis.Length)
                {
                    var ascii = asciis[i];
                    if (ascii == 61 && hexRange.Contains(asciis[i + 1]) && hexRange.Contains(asciis[i + 2]))
                    {
                        var hex = $"{(char)asciis[i + 1]}{(char)asciis[i + 2]}";
                        retBytes.Add(byte.Parse(hex, System.Globalization.NumberStyles.HexNumber));
                        i += 2;
                    }
                    else if (ascii == 61 && (asciis[i + 1] == '\r' || asciis[i + 1] == '\n'))
                        i++;
                    else
                        retBytes.Add(ascii);
                    i++;
                }
                var bom = enc.GetBOM();
                var hasBOM = true;
                for(i = 0; i<bom.Length;i++)
                {
                    if (retBytes[i] != bom[i])
                    {
                        hasBOM = false;
                        break;
                    }
                }
                if (hasBOM) retBytes.RemoveRange(0, bom.Length);

                if (enc == Encoding.UTF8 && retBytes[0] == 0xEF && retBytes[1] == 0xBB & retBytes[2] == 0xBF)
                    retBytes.RemoveRange(0, 3);
                result = enc.GetString(retBytes.ToArray());

                return (result);
            }
        }

        static public class THUNDER
        {
            static public async Task<string> Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                if (text is byte[] && text.Length > 0)
                {
                    var contents = new List<byte>();
                    contents.AddRange("AA".ToArray().Cast<byte>());
                    contents.AddRange(text);
                    contents.AddRange("ZZ".ToArray().Cast<byte>());
                    result = $"thunder://{await BASE64.Encode(contents.ToArray(), enc, LineBreak)}";
                }
                return (result);
            }

            static public async Task<string> Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;
                result = $"thunder://{await BASE64.Encode($"AA{text}ZZ", enc, LineBreak)}";
                return (result);
            }

            static public async Task<string> Encode(string text, bool LineBreak = false)
            {
                return (await Encode(text, Encoding.Default, LineBreak));
            }

            static public async Task<string> Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                var url = Regex.Replace(text, @"^thunder://(.*?)$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                url = await BASE64.Decode(url, enc);
                result = Regex.Replace(url, @"^AA(.*?)ZZ$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return (result);
            }
        }

        static public class FLASHGET
        {
            static public async Task<string> Encode(byte[] text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;

                if (text is byte[] && text.Length > 0)
                {
                    var contents = new List<byte>();
                    contents.AddRange("[FLASHGET]".ToArray().Cast<byte>());
                    contents.AddRange(text);
                    contents.AddRange("[FLASHGET]".ToArray().Cast<byte>());
                    result = $"flashget://{await BASE64.Encode(contents.ToArray(), enc, LineBreak)}";
                }

                return (result);
            }

            static public async Task<string> Encode(string text, Encoding enc, bool LineBreak = false)
            {
                string result = string.Empty;
                result = $"flashget://{await BASE64.Encode($"[FLASHGET]{text}[FLASHGET]", enc, LineBreak)}";
                return (result);
            }

            static public async Task<string> Encode(string text, bool LineBreak = false)
            {
                return (await Encode(text, Encoding.Default, LineBreak));
            }

            static public async Task<string> Decode(string text, Encoding enc)
            {
                string result = string.Empty;
                var url = Regex.Replace(text, @"^flashget://(.*?)$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                url = await BASE64.Decode(url, enc);
                result = Regex.Replace(url, @"^\[FLASHGET\](.*?)\[FLASHGET\]$", "$1", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                return (result);
            }
        }

        static public class MORSE
        {
            private static readonly Dictionary<string, string> MorseTable = new Dictionary<string, string>() {
                // 以下数据来源于 维基百科(Wikipedia) 中文版
                // https://zh.wikipedia.org/zh-cn/%E6%91%A9%E5%B0%94%E6%96%AF%E7%94%B5%E7%A0%81#%E7%8E%B0%E4%BB%A3%E5%9B%BD%E9%99%85%E6%91%A9%E5%B0%94%E6%96%AF%E7%94%B5%E7%A0%81
                //
                // 基础拉丁字母
                { "A", ".-" }, { "B", "-..." }, { "C", "-.-." }, { "D", "-.." }, { "E", "." }, { "F", "..-." }, { "G", "--." },
                { "H", "...." }, { "I", ".." }, { "J", ".---" }, { "K", "-.-" }, { "L", ".-.." }, { "M", "--" }, { "N", "-." },
                { "O", "---" }, { "P", ".--." }, { "Q", "--.-" }, { "R", ".-." }, { "S", "..." }, { "T", "-" }, { "U", "..-" },
                { "V", "...-" }, { "W", ".--" }, { "X", "-..-" }, { "Y", "-.--" }, { "Z", "--.." },
                // 数字
                { "1", ".----" }, { "2", "..---" }, { "3", "...--" }, { "4", "....-" }, { "5", "....." },
                { "6", "-...." }, { "7", "--..." }, { "8", "---.." }, { "9", "----." }, { "0", "-----" },
                // 标点符号
                { ".", ".-.-.-" }, { ":", "---..." }, { ",", "--..--" }, { ";", "-.-.-." }, { "?", "..--.." }, { "=", "-...-" },
                { "\'", ".----." }, { "/", "-..-." }, { "!", "-.-.--" }, { "-", "-....-" }, { "_", "..--.-" }, { "\"", ".-..-." },
                { "(", "-.--." }, { ")", "-.--.-" }, { "$", "...-..-" }, { "&", ".-..." }, { "@", ".--.-." }, { "+", ".-.-." },
                // 派生拉丁字母
                //{"Ä", "·-·-"}, {"Æ", "·-·-"}, {"Ą", "·-·-"}, {"À", "·--·-"}, {"Å", "·--·-"}, {"Ç", "-·-··"}, {"Ĉ", "-·-··"}, {"Ć","-·-··"}, {"Č", "·--·"}, {"Ch", "----"}, {"Š", "----"}, {"Ĥ", "----"}, {"Ð", "··--·"}, {"È", "----"}, {"Ł", "·-··-"},
                //{"É", "----"}, {"Đ", "----"}, {"Ę", "··-··"}, {"Ĝ", "--·-·"}, {"Ĥ", "-·--·"}, {"Ĵ", "·---·"}, {"Ñ", "----"}, {"Ń", "--·--"}, {"Ö", "----"}, {"Ø", "----"}, {"Ó ", "---·"}, {"Ś", "···-···"},
                //{"Ŝ", "···-·"}, {"ß", "···--··"}, {"Þ", "·--··"}, {"Ü", "----"}, {"Ŭ", "··--"}, {"Ź", "--··-·"}, {"Ž", "·--"}, {"Ż", "--··-"},
                // 特殊符号（统一符号）
                // 这是一些有特殊意义的点划组合。它们由二个或多个字母的摩尔斯电码连成一个使用，
                // 这样可以省去正常时把它们做为两个字母发送所必须的中间间隔时间。 
                { "AAAAA", "·-·-·-·-·-" },  // 调用信号，表示“我有消息发送”。
                { "AAA", "·-·-·-" },        // 表示“本句完，接下一句”（同句号）。
                { "HH", "········" },       // 表示“有错，从上一字重新开始”。
                { "AR", "·-·-·" },          // 表示“消息结束”。
                { "AS", "·-···" },          // 等待。
                { "TTTTT", "-----" },       // 表示“我正在接收你的消息”。
                //{ "K", "-·-" },             // 表示“我已准备好，请开始发送消息”。
                //{ "T", "-" },               // 表示“字收到了”。
                { "IMI", "··--··" },        // 表示“请重复你的电码，我不是很明白”（同问号）。
                //{ "R", "·-·" },             // 表示“消息已收到”。
                { "SK", "···-·-" },         // 表示终止（联系结束）。
                { "BT", "-···-" },          // 分隔符。
                { "SOS", "···---···" },     // 求救信号。
                // 以下并不是真正的统一符号：
                { "{REPEAT LAST WORD}", "···-·" }, // 我将重新发送最后一个单词 
                { "{SAME}", "·· ··" },             // 同样
                { "{OOO}", "---------" },          // 错误（Out Of Order）
                // 其它
                { "{UNDERSTOOD}", "...-." },
                { "{ERROR}", "........" },
                { "{INVITATION TO TRANSMIT}", "-.-" },
                { "{WAIT}", ".-..." },
                { "{END OF WORK}", "...-.-" },
                { "{STARTING SIGNAL}", "-.-.-" },
                { " ", "/" }, //{ " ", "\u2423" },
            };

            private static readonly Dictionary<string, string> MorseAbbrTable = new Dictionary<string, string>()
            {
                { "AA", "All after" },                                                // 某字以后
                { "AB", "All before" },                                               // 字以前
                { "ARRL", "American Radio Relay League" },                            // 美国无线电中继联盟
                { "ABT", "About" },                                                   // 大约
                { "ADS", "Address" },                                                 // 地址
                { "AGN", "Again" },                                                   // 再一次
                { "ANT", "Antenna" },                                                 // 天线
                { "BN", "All between" },                                              // ……之间
                { "BUG", "Semiautomatic key" },                                       // 半自动关键
                //{ "C", "Yes" },                                                       // 是，好
                { "CBA", "Callbook address" },                                        // 呼号手册
                { "CFM", "Confirm" },                                                 // 确认
                { "CLG", "Calling" },                                                 // 调用
                { "CQ", "Calling any station" },                                      // 调用任意台站
                { "CUL", "See you later" },                                           // 再见
                { "CUZ", "Because" },                                                 // 因为
                { "CW", "Continuous wave" },                                          // 连续波
                { "CX", "Conditions" },                                               // 状况
                { "CY", "Copy" },                                                     // 抄收
                { "DE", "From" },                                                     // 来自
                { "DX", "Distance (sometimes refers to long distance contact)" },     // 距离（有时指长程通联）
                { "ES", "And" },                                                      // （和；且）
                { "FB", "Fine business (Analogous to \"OK\")" },                      // 类似于“确定”
                { "FCC", "Federal Communications Commission" },                       // （美国）联邦通信委员会
                { "FER", "For" },                                                     // 为了
                { "FREQ", "Frequency" },                                              // 频率
                { "GA", "Good afternoon or Go ahead (depending on context)" },        // 午安；请发报（依上下文而定）
                { "GE", "Good evening" },                                             // 晚安
                { "GM", "Good morning" },                                             // 早安
                { "GND", "Ground (ground potential)" },                               // 地面（地电位）
                { "GUD", "Good" },                                                    // 好
                { "HI", "Laughter" },                                                 // 笑
                { "HR", "Here" },                                                     // 这里
                { "HV", "Have" },                                                     // 有
                { "LID", "Lid" },                                                     // 覆盖
                { "MILS", "Milliamperes" },                                           // 毫安培
                { "NIL", "Nothing" },                                                 // 无收信，空白
                { "NR", "Number" },                                                   // 编号，第……
                { "OB", "Old boy" },                                                  // 老大哥
                { "OC", "Old chap" },                                                 // 老伙计
                { "OM", "Old man (any male amateur radio operator is an OM)" },       // 前辈，老手（男性）（任何男性业余无线电操作员都是OM）
                { "OO", "Official Observer" },                                        // 官方观察员
                { "OP", "Operator" },                                                 // 操作员
                { "OT", "Old timer" },                                                // 老前辈
                { "OTC", "Old timers club" },                                         // 老手俱乐部
                { "OOTC", "Old old timers club" },                                    // 资深老手俱乐部
                { "PSE", "Please" },                                                  // 请
                { "PWR", "Power" },                                                   // 功率
                { "QCWA", "Quarter Century Wireless Association" },                   // 四分之一世界无线电协会
                //{ "R", "Received,Roger or decimal point (depending on context)" },    // 收到；小数点（依上下文而定）
                { "RCVR", "Receiver" },                                               // 接收机
                { "RPT", "Repeat or report (depending on context)" },                 // 重复；报告（依上下文而定）
                { "RST", "Signal report format (Readability-Signal Strength-Tone)" }, // 信号报告格式（可读性信号强度音）
                { "RTTY", "Radioteletype" },                                          // 无线电传
                { "RX", "Receive" },                                                  // 接收
                { "SAE", "Self addressed envelope" },                                 // 回邮信（即已填写自己地址以便对方回信的信封）
                { "SASE", "Self addressed, stamped envelope" },                       // 带邮票的回邮信封
                { "SED", "Said" },                                                    // 说
                { "SEZ", "Says" },                                                    // 说
                { "SIG", "Signal" },                                                  // 信号
                { "SIGS", "Signals" },                                                // 信号
                { "SKED", "Schedule" },                                               // 进程表
                { "SN", "Soon" },                                                     // 很快；不久的将来
                { "SRI", "Sorry" },                                                   // 抱歉
                { "STN", "Station" },                                                 // 台站
                { "TEMP", "Temperature" },                                            // 温度
                { "TMW", "Tomorrow" },                                                // 明天
                { "TNX", "Thanks" },                                                  // 谢谢
                { "TU", "Thank you" },                                                // 谢谢你
                { "TX", "Transmit" },                                                 // 发射
                //{ "U", "You" },                                                       // 你
                { "UR", "Your or you're (depending on context)" },                    // 你的；你是（依上下文而定）
                { "URS", "Yours" },                                                   // 你的
                { "VY", "Very" },                                                     // 非常；很
                { "WDS", "Words" },                                                   // 词
                { "WKD", "Worked" },                                                  // 工作
                { "WL", "Will or Well" },                                             // 将会；好（依上下文而定）
                { "WUD", "Would" },                                                   // 将会
                { "WX", "Weather" },                                                  // 天气
                { "XMTR", "Transmitter" },                                            // 发射机
                { "XYL", "Wife" },                                                    // 妻子
                { "YL", "Young lady (used of any female)" },                          // 女报务员（称呼任何女性报务员）
                { "73", "Best regards" },                                             // 致敬
                { "88", "Love and kisses" },                                          // 吻别
                { "99", "go way" },                                                   // 走开（非友善） 
            };

            static public async Task<string> Encode(byte[] text, Encoding enc)
            {
                string result = string.Empty;

                if (text is byte[] && text.Length > 0)
                {
                    result = await Encode(enc.GetString(text));
                }

                return (result);
            }

            static public async Task<string> Encode(string text)
            {
                string result = string.Empty;

                try
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        List<string> ret = new List<string>();
                        foreach (var c in text.Trim().ToUpper())
                        {
                            var k = c.ToString();
                            if (MorseTable.ContainsKey(k)) ret.Add(MorseTable[k]);
                        }
                        result = string.Join(" ", ret).Trim().Replace(".", "\u30FB").Replace("-", "\u30FC");//.Replace("/", "/ ");
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog($"{"Morse".T()} {"ERROR".T()}\n{ex.Message}", "ERROR".T()).ShowAsync();
                }

                return (result);
            }

            static public async Task<string> Decode(string text, bool KeepAbbr = true)
            {
                string result = string.Empty;

                try
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = Regex.Replace(text, @"[\u00B7\u2022\u2024\u2027\u2219\u25CF\u25E6\u30FB\uFE52\uFF0E\uFF65・]", ".", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        text = Regex.Replace(text, @"[\u2010\u2013\u2014\u2015\u2500\u2501\u2F00\u30FC\uFE58\uFF0D\uFF3F\uFF70\uFFE3\uFFDA_－]", "-", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                        var cs = text.Replace("\u30FB", ".").Replace("\u30FC", "-").Replace("・", ".").Replace("－", "-").Split(" ");
                        List<string> ret = new List<string>();
                        foreach (var c in cs)
                        {
                            if (MorseTable.ContainsValue(c))
                            {
                                var v = MorseTable.FirstOrDefault(kv => kv.Value.Equals(c));
                                ret.Add(v.Key);
                            }
                        }
                        result = string.Join("", ret).Trim();
                        if (!KeepAbbr)
                        {
                            foreach (var kv in MorseAbbrTable)
                            {
                                result = result.Replace(kv.Key, kv.Value, StringComparison.InvariantCultureIgnoreCase);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog($"{"Morse".T()} {"ERROR".T()}\n{ex.Message}", "ERROR".T()).ShowAsync();
                }

                return (result);
            }
        }

        static public class GUID
        {
            static public string Encode(byte[] text, Encoding enc, string fmt = "D", bool UpCase = true, bool UUID = false)
            {
                string result = string.Empty;

                if (text is byte[] && text.Length > 0)
                {
                    result = Encode(enc.GetString(text), fmt, UpCase, UUID);
                }

                return (result);
            }

            static public string Encode(string text, string fmt = "D", bool UpCase = true, bool UUID = false)
            {
                string result = string.Empty;

                if (!Guid.TryParse(text, out Guid guid)) guid = Guid.NewGuid();

                if (string.IsNullOrEmpty(fmt)) fmt = "D";

                result = guid.ToString(fmt);
                if (UpCase) result = result.ToUpper();
                if (UUID)
                {
                    var pos = result.IndexOf('-', 22);
                    if (pos > 0) result = result.Remove(pos, 1);
                }
                if (fmt.Equals("X", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = result.Replace("0X", "0x");
                }

                return (result);
            }

            static public string Decode(string text, string fmt = "")
            {
                string result = string.Empty;

                Guid guid;

                if (string.IsNullOrEmpty(text))
                    text = Encode(text, "D", true, true);

                var pos = text.IndexOf('-', 18);
                if (pos > 0) text = text.Insert(pos + 5, "-").Replace("--", "-");

                if (string.IsNullOrEmpty(fmt))
                {
                    if (Guid.TryParse(text, out guid))
                    {
                        result = guid.ToString("D").ToUpper();
                    }
                }
                else
                {
                    if (Guid.TryParseExact(text, fmt, out guid))
                    {
                        result = guid.ToString(fmt).ToUpper();
                    }
                    else if (Guid.TryParse(text, out guid))
                    {
                        result = guid.ToString(fmt).ToUpper();
                    }
                }
                if (fmt.Equals("X", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = result.Replace("0X", "0x");
                }
                return (result);
            }
        }
        #endregion

        #region Encoder helper routines
        static public async Task<string> Encode(byte[] content, CODEC Codec, Encoding enc, bool LineBreak = false)
        {
            string result = string.Empty;
            try
            {
                switch (Codec)
                {
                    case CODEC.URL:
                        result = URL.Encode(content, enc);
                        break;
                    case CODEC.HTML:
                        result = await HTML.Encode(content, enc);
                        break;
                    case CODEC.BASE64:
                        result = await BASE64.Encode(content, enc, LineBreak);
                        break;
                    case CODEC.UUE:
                        result = UUE.Encode(content, enc);
                        break;
                    case CODEC.XXE:
                        result = XXE.Encode(content, enc);
                        break;
                    case CODEC.RAW:
                        result = RAW.Encode(content, enc);
                        break;
                    case CODEC.UNICODEVALUE:
                        result = UnicodeValue.Encode(content, enc);
                        break;
                    case CODEC.UNICODEGLYPH:
                        result = UnicodeGlyph.Encode(content, enc);
                        break;
                    case CODEC.QUOTED:
                        result = QUOTED.Encode(content, enc);
                        break;
                    case CODEC.THUNDER:
                        result = await THUNDER.Encode(content, enc);
                        break;
                    case CODEC.FLASHGET:
                        result = await FLASHGET.Encode(content, enc);
                        break;
                    case CODEC.MORSE:
                    case CODEC.MORSEABBR:
                        result = await MORSE.Encode(content, enc);
                        break;
                    case CODEC.UUID:
                        result = GUID.Encode(content, enc, "D", true, true);
                        break;
                    case CODEC.GUID:
                        result = GUID.Encode(content, enc);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> Encode(string content, CODEC Codec, Encoding enc, bool LineBreak = false)
        {
            string result = string.Empty;
            try
            {
                switch (Codec)
                {
                    case CODEC.URL:
                        result = URL.Encode(content, enc);
                        break;
                    case CODEC.HTML:
                        result = await HTML.Encode(content, enc);
                        break;
                    case CODEC.BASE64:
                        result = await BASE64.Encode(content, enc, LineBreak);
                        break;
                    case CODEC.UUE:
                        result = UUE.Encode(content, enc);
                        break;
                    case CODEC.XXE:
                        result = XXE.Encode(content, enc);
                        break;
                    case CODEC.RAW:
                        result = RAW.Encode(content, enc);
                        break;
                    case CODEC.UNICODEVALUE:
                        result = UnicodeValue.Encode(content, enc);
                        break;
                    case CODEC.UNICODEGLYPH:
                        result = UnicodeGlyph.Encode(content, enc);
                        break;
                    case CODEC.QUOTED:
                        result = QUOTED.Encode(content, enc);
                        break;
                    case CODEC.THUNDER:
                        result = await THUNDER.Encode(content, enc);
                        break;
                    case CODEC.FLASHGET:
                        result = await FLASHGET.Encode(content, enc);
                        break;
                    case CODEC.MORSE:
                    case CODEC.MORSEABBR:
                        result = await MORSE.Encode(content);
                        break;
                    case CODEC.UUID:
                        result = GUID.Encode(content, "D", true, true);
                        break;
                    case CODEC.GUID:
                        result = GUID.Encode(content);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> Encode(string content, CODEC Codec, bool LineBreak = false)
        {
            return (await Encode(content, Codec, Encoding.Default, LineBreak));

        }

        static public async Task<string> Encode(WriteableBitmap image, string format = ".png", bool prefix = true, bool LineBreak = false)
        {
            string result = string.Empty;
            try
            {
                using (var fileStream = new InMemoryRandomAccessStream())
                {
                    // Get pixels of the WriteableBitmap object 
                    Stream pixelStream = image.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                    var encId = BitmapEncoder.PngEncoderId;
                    var fext = format.ToLower();
                    var mime = "image/png";
                    switch (fext)
                    {
                        case ".bmp":
                            encId = BitmapEncoder.BmpEncoderId;
                            mime = "image/bmp";
                            break;
                        case ".gif":
                            encId = BitmapEncoder.GifEncoderId;
                            mime = "image/gif";
                            break;
                        case ".png":
                            encId = BitmapEncoder.PngEncoderId;
                            mime = "image/png";
                            break;
                        case ".jpg":
                            encId = BitmapEncoder.JpegEncoderId;
                            mime = "image/jpeg";
                            break;
                        case ".jpeg":
                            encId = BitmapEncoder.JpegEncoderId;
                            mime = "image/jpeg";
                            break;
                        case ".tif":
                            encId = BitmapEncoder.TiffEncoderId;
                            mime = "image/tiff";
                            break;
                        case ".tiff":
                            encId = BitmapEncoder.TiffEncoderId;
                            mime = "image/tiff";
                            break;
                        default:
                            encId = BitmapEncoder.PngEncoderId;
                            mime = "image/png";
                            break;
                    }
                    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                    // Save the image file with jpg extension 
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        //(uint)bmp.PixelWidth, (uint)bmp.PixelHeight, 
                        (uint)image.PixelWidth, (uint)image.PixelHeight,
                        96.0, 96.0,
                        pixels);
                    await encoder.FlushAsync();

                    #region Convert InMemoryRandomAccessStream/IRandomAccessStream to byte[]/Array
                    Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0));
                    MemoryStream ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    byte[] arr = ms.ToArray();
                    #endregion

                    var opt = Base64FormattingOptions.None;
                    if (LineBreak) opt = Base64FormattingOptions.InsertLineBreaks;

                    string base64 = string.Empty;

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < arr.Length; i += 57)
                    {
                        byte[] segment = arr.Skip(i).Take(57).ToArray();
                        sb.AppendLine(Convert.ToBase64String(segment, opt));
                    }
                    if (LineBreak) base64 = string.Join("\n", sb);
                    else base64 = string.Join("", sb);
                    //base64 = Convert.ToBase64String(arr, opt);

                    if (prefix) result = $"data:{mime};base64,{base64}";
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> ProgressEncode(WriteableBitmap image, string format = ".png", bool prefix = true, bool LineBreak = false)
        {
            string result = string.Empty;
            int processCount = 0;
            try
            {
                using (var fileStream = new InMemoryRandomAccessStream())
                {
                    #region Encode Image to format
                    // Get pixels of the WriteableBitmap object 
                    Stream pixelStream = image.PixelBuffer.AsStream();
                    byte[] pixels = new byte[pixelStream.Length];
                    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                    var encId = BitmapEncoder.PngEncoderId;
                    var fext = format.ToLower();
                    var mime = "image/png";
                    switch (fext)
                    {
                        case ".bmp":
                            encId = BitmapEncoder.BmpEncoderId;
                            mime = "image/bmp";
                            break;
                        case ".gif":
                            encId = BitmapEncoder.GifEncoderId;
                            mime = "image/gif";
                            break;
                        case ".png":
                            encId = BitmapEncoder.PngEncoderId;
                            mime = "image/png";
                            break;
                        case ".jpg":
                            encId = BitmapEncoder.JpegEncoderId;
                            mime = "image/jpeg";
                            break;
                        case ".jpeg":
                            encId = BitmapEncoder.JpegEncoderId;
                            mime = "image/jpeg";
                            break;
                        case ".tif":
                            encId = BitmapEncoder.TiffEncoderId;
                            mime = "image/tiff";
                            break;
                        case ".tiff":
                            encId = BitmapEncoder.TiffEncoderId;
                            mime = "image/tiff";
                            break;
                        default:
                            encId = BitmapEncoder.PngEncoderId;
                            mime = "image/png";
                            break;
                    }
                    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                    // Save the image file with jpg extension 
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        //(uint)bmp.PixelWidth, (uint)bmp.PixelHeight, 
                        (uint)image.PixelWidth, (uint)image.PixelHeight,
                        96.0, 96.0,
                        pixels);
                    await encoder.FlushAsync();
                    #endregion

                    #region Convert InMemoryRandomAccessStream/IRandomAccessStream to byte[]/Array
                    Stream stream = WindowsRuntimeStreamExtensions.AsStreamForRead(fileStream.GetInputStreamAt(0));
                    MemoryStream ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    byte[] arr = ms.ToArray();
                    #endregion

                    var opt = Base64FormattingOptions.None;
                    if (LineBreak) opt = Base64FormattingOptions.InsertLineBreaks;

                    var dlgProgress = new ProgressDialog();
                    IProgress<int> progress = new Progress<int>(percent => dlgProgress.Value = percent);

                    var dlgResult = dlgProgress.ShowAsync();

                    string base64 = string.Empty;
                    int totalCount = arr.Count();
                    processCount = await Task.Run<int>(() =>
                    {
                        StringBuilder sb = new StringBuilder();

                        int tempCount = 0;
                        for (int i = 0; i < totalCount; i += 57)
                        {
                            byte[] segment = arr.Skip(i).Take(57).ToArray();
                            sb.AppendLine(Convert.ToBase64String(segment, opt));
                            tempCount += segment.Length;

                            if (dlgProgress != null)
                                dlgProgress.Report(tempCount * 100 / totalCount);
                        }
                        if (LineBreak) base64 = string.Join("\n", sb);
                        else base64 = string.Join("", sb);
                        if (dlgProgress != null)
                            dlgProgress.Report(100);

                        return tempCount;
                    });
                    if (prefix) result = $"data:{mime};base64,{base64}";
                    dlgResult.Cancel();
                    dlgProgress.Hide();
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> Encoder(this string content, CODEC Codec, Encoding enc, bool LineBreak = false)
        {
            return (await Encode(content, Codec, enc, LineBreak));
        }

        static public async Task<string> Encoder(this string content, CODEC Codec, bool LineBreak = false)
        {
            return (await Encode(content, Codec, Encoding.Default, LineBreak));
        }

        static public async Task<string> Encoder(this WriteableBitmap image, string format = ".png", bool prefix = true, bool LineBreak = false)
        {
            return (await Encode(image, format, prefix, LineBreak));
        }
        #endregion

        #region Decoder helper routines
        static public async Task<string> Decode(string content, CODEC Codec, Encoding enc)
        {
            string result = string.Empty;
            try
            {
                switch (Codec)
                {
                    case CODEC.URL:
                        result = URL.Decode(content, enc);
                        break;
                    case CODEC.HTML:
                        result = await HTML.Decode(content, enc);
                        break;
                    case CODEC.BASE64:
                        result = await BASE64.Decode(content, enc);
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
                    case CODEC.UNICODEVALUE:
                        result = UnicodeValue.Decode(content, enc);
                        break;
                    case CODEC.UNICODEGLYPH:
                        result = UnicodeGlyph.Decode(content, enc);
                        break;
                    case CODEC.QUOTED:
                        result = QUOTED.Decode(content, enc);
                        break;
                    case CODEC.THUNDER:
                        result = await THUNDER.Decode(content, enc);
                        break;
                    case CODEC.FLASHGET:
                        result = await FLASHGET.Decode(content, enc);
                        break;
                    case CODEC.MORSE:
                        result = await MORSE.Decode(content);
                        break;
                    case CODEC.MORSEABBR:
                        result = await MORSE.Decode(content, false);
                        break;
                    case CODEC.UUID:
                        result = GUID.Decode(content);
                        break;
                    case CODEC.GUID:
                        result = GUID.Decode(content);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<WriteableBitmap> Decode(string content)
        {
            WriteableBitmap result = new WriteableBitmap(1, 1);
            if (content.Length <= 0) return (result);

            try
            {
                string bs = Regex.Replace(content, @"data:image/.*?;base64,", "", RegexOptions.IgnoreCase);
                byte[] arr = Convert.FromBase64String(bs.Trim());
                using (MemoryStream ms = new MemoryStream(arr))
                {
                    byte[] buf = ms.ToArray();
                    using (InMemoryRandomAccessStream fileStream = new InMemoryRandomAccessStream())
                    {
                        using (DataWriter writer = new DataWriter(fileStream.GetOutputStreamAt(0)))
                        {
                            writer.WriteBytes(buf);
                            writer.StoreAsync().GetResults();
                            await fileStream.FlushAsync();
                        }
                        result.SetSource(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<SVG> DecodeSvg(this string content)
        {
            SVG result = new SVG();
            if (content.Length <= 0) return (result);

            try
            {
                //var pattern = @"<svg.*?>";
                var patternSvg = @"<svg.*?\n*\r*.*?>(\n*\r*.*?)*</svg>";
                var patternBase64Svg = @"^data:image/svg.*?;base64,";
                var patternBase64 = @"^data:image/.*?;base64,";
                if (Regex.IsMatch(content, patternSvg, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    await result.Load(content);
                }
                else if (Regex.IsMatch(content, patternBase64Svg, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    string bs = Regex.Replace(content, patternBase64Svg, "", RegexOptions.IgnoreCase);
                    byte[] arr = Convert.FromBase64String(bs.Trim());
                    await result.Load(arr);
                }
                else if (Regex.IsMatch(content, patternBase64, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    string bs = Regex.Replace(content, patternBase64, "", RegexOptions.IgnoreCase);
                    byte[] arr = Convert.FromBase64String(bs.Trim());
                    result.Source = null;
                    result.Image = await Decode(content);
                    result.Bytes = arr;
                    result.Source = null;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog(ex.Message.T(), "ERROR".T()).ShowAsync();
            }
            return (result);
        }

        static public async Task<string> Decoder(this string content, CODEC Codec, Encoding enc)
        {
            return (await Decode(content, Codec, enc));
        }

        static public async Task<WriteableBitmap> Decoder(this string content)
        {
            return (await Decode(content));
        }
        #endregion

        #region CodePage converters
        public static async Task<string> ToBase64(this WriteableBitmap wb, string format, bool prefix, bool linebreak)
        {
            string result = string.Empty;
            try
            {
                if (wb == null || !(wb is WriteableBitmap)) return (result);
                var size = wb.PixelWidth * wb.PixelHeight;
                if (size > 196608)
                {
                    var dlgMessage = new MessageDialog("EncodingBigImageConfirm".T(), "Confirm".T()) { Options = MessageDialogOptions.AcceptUserInputAfterDelay };
                    dlgMessage.Commands.Add(new UICommand("OK".T()) { Id = 0 });
                    dlgMessage.Commands.Add(new UICommand("Cancel".T()) { Id = 1 });
                    // Set the command that will be invoked by default
                    dlgMessage.DefaultCommandIndex = 0;
                    // Set the command to be invoked when escape is pressed
                    dlgMessage.CancelCommandIndex = 1;
                    // Show the message dialog
                    var dlgResult = await dlgMessage.ShowAsync();
                    if ((int)dlgResult.Id == 0)
                    {
                        if (string.IsNullOrEmpty(format)) format = ".png";
                        result = await ProgressEncode(wb, format, prefix, linebreak);
                    }
                }
                else
                    result = await Encode(wb, format, prefix, linebreak);
            }
            catch(Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }
            return (result);
        }

        public static string ConvertTo(this string text, Encoding SrcEnc, Encoding DstEnc)
        {
            var result = string.Empty;

            try
            {
                if (DstEnc == SrcEnc) result = text;
                else
                {
                    byte[] arr = SrcEnc.GetBytes(text);
                    result = DstEnc.GetString(arr);
                }
            }
            catch (Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }
            return (result);
        }

        public static string ConvertTo(this string text, Encoding enc)
        {
            var result = string.Empty;
            try
            {
                //byte[] arr = Encoding.Default.GetBytes(text);
                byte[] arr = enc.GetBytes(text);
                if (arr != null && arr.Length > 0)
                    //result = enc.GetString(arr);
                    result = Encoding.Default.GetString(arr);                    
            }
            catch (Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }
            return (result);
        }

        public static string ConvertFrom(this string text, Encoding enc, bool IsOEM = false)
        {
            var result = string.Empty;
            try
            {
                if (IsOEM)
                {
                    int cp = System.Globalization.CultureInfo.InstalledUICulture.TextInfo.OEMCodePage;

                    //// 判断韩语
                    //if (Regex.IsMatch(text, @"[\uac00-\ud7ff]+"))
                    //{
                    //    cp = 949;
                    //}
                    //// 判断日语
                    //else if (Regex.IsMatch(text, @"[\u0800-\u4e00]+"))
                    //{
                    //    cp = 932;
                    //}
                    //// 判断中文
                    //else if (Regex.IsMatch(text, @"[\u4e00-\u9fa5]+")) // 如果是中文
                    //{
                    //    cp = 936;
                    //}
                    var cpc = enc.CodePage;
                    string[] subtexts = new string[]{ text };
                    if (cpc == 932 || cpc == 936 || cpc == 950 || cpc == 949 || cpc == 65001)
                        subtexts = text.Split(new char[] { '\u30FB', '\uFF65' });

                    List<string> sl = new List<string>();
                    foreach (var t in subtexts)
                    {
                        byte[] arr = null;
                        var cpEnc = GetTextEncoder(cp);
                        arr = cpEnc.GetBytes(t);
                        //switch (cp)
                        //{
                        //    case 936:
                        //        arr = Encoding.GetEncoding("GBK").GetBytes(t);
                        //        break;
                        //    case 950:
                        //        arr = Encoding.GetEncoding("BIG5").GetBytes(t);
                        //        break;
                        //    case 932:
                        //        arr = Encoding.GetEncoding("Shift-JIS").GetBytes(t);
                        //        break;
                        //    case 949:
                        //        arr = Encoding.GetEncoding("Korean").GetBytes(t);
                        //        break;
                        //    case 1200:
                        //        arr = Encoding.Unicode.GetBytes(t);
                        //        break;
                        //    case 1201:
                        //        arr = Encoding.BigEndianUnicode.GetBytes(t);
                        //        break;
                        //    case 1250:
                        //        arr = GetTextEncoder("1250").GetBytes(t);
                        //        break;
                        //    case 1251:
                        //        arr = GetTextEncoder("1251").GetBytes(t);
                        //        break;
                        //    case 1252:
                        //        arr = GetTextEncoder("1252").GetBytes(t);
                        //        break;
                        //    case 1253:
                        //        arr = GetTextEncoder("1253").GetBytes(t);
                        //        break;
                        //    case 1254:
                        //        arr = GetTextEncoder("1254").GetBytes(t);
                        //        break;
                        //    case 1255:
                        //        arr = GetTextEncoder("1255").GetBytes(t);
                        //        break;
                        //    case 1256:
                        //        arr = GetTextEncoder("1256").GetBytes(t);
                        //        break;
                        //    case 1257:
                        //        arr = GetTextEncoder("1257").GetBytes(t);
                        //        break;
                        //    case 1258:
                        //        arr = GetTextEncoder("1258").GetBytes(t);
                        //        break;
                        //    case 65000:
                        //        arr = Encoding.UTF7.GetBytes(t);
                        //        break;
                        //    case 65001:
                        //        arr = Encoding.UTF8.GetBytes(t);
                        //        break;
                        //    default:
                        //        arr = Encoding.ASCII.GetBytes(t);
                        //        break;
                        //}
                        if (arr != null && arr.Length > 0)
                        {
                            //if(enc == Encoding.Default)
                            if (enc.CodePage == Encoding.Default.CodePage)
                                sl.Add(GetTextEncoder(cp).GetString(arr));
                            else
                                sl.Add(enc.GetString(arr));
                        }
                    }
                    result = string.Join("\u30FB", sl);
                }
                else
                {
                    byte[] arr = enc.GetBytes(text);
                    if (arr != null && arr.Length > 0)
                        result = Encoding.Default.GetString(arr);
                }
            }
            catch (Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }
            return (result);
        }

        public static string ToString(this byte[] array, Encoding enc)
        {
            var result = string.Empty;
            try
            {
                if (array != null && array.Length > 0)
                {
                    // UTF-16
                    if (array[0] == 0xFF && array[1] == 0xFE)
                        result = Encoding.Unicode.GetString(array.Skip(2).ToArray());
                    // UTF-16 Big-Endian
                    else if (array[0] == 0xFE && array[1] == 0xFF)
                        result = Encoding.BigEndianUnicode.GetString(array.Skip(2).ToArray());
                    // UTF-8
                    else if (array[0] == 0xEF && array[1] == 0xBB && array[2] == 0xBF)
                        result = Encoding.UTF8.GetString(array.Skip(3).ToArray());
                    // UTF-32
                    else if (array[0] == 0xFF && array[1] == 0xFE && array[2] == 0x00 && array[3] == 0x00)
                        result = Encoding.UTF32.GetString(array.Skip(4).ToArray());
                    // UTF-32 Big-Endian
                    else if (array[0] == 0x00 && array[1] == 0x00 && array[2] == 0xFE && array[3] == 0xFF)
                        result = Encoding.GetEncoding("utf-32BE").GetString(array.Skip(4).ToArray());
                    // UTF-7
                    else if (array[0] == 0x2B && array[1] == 0x2F && array[2] == 0x76 && (array[2] == 0x38 || array[2] == 0x39 || array[2] == 0x2B || array[2] == 0x2F))
                        result = Encoding.UTF7.GetString(array.Skip(4).ToArray());
                    // UTF-1
                    //else if (array[0] == 0xF7 && array[1] == 0x64 && array[2] == 0x4C)
                    //    result = Encoding.GetEncoding("UTF-1").GetString(array.Skip(3).ToArray());
                    // GB-18030
                    else if (array[0] == 0x84 && array[1] == 0x31 && array[2] == 0x95 && array[3] == 0x33)
                        result = Encoding.GetEncoding("GB18030").GetString(array.Skip(4).ToArray());
                    else
                        result = enc.GetString(array);
                }
            }
            catch (Exception ex)
            {
                ex.Message.T().ShowMessage("ERROR".T());
            }
            return (result);
        }
        #endregion

        public static string ReverseOrder(this string text)
        {
            string result = text;

            var sentences = text.Split(LINEBREAK, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            foreach (var sentence in sentences.Reverse())
            {
                var l = sentence.Reverse().ToList();
                for (var i = 0; i < l.Count; i++)
                {
                    if (l[i].Equals('“')) l[i] = '”';
                    else if (l[i].Equals('”')) l[i] = '“';
                }
                sb.AppendLine(string.Join("", l));
            }
            result = sb.ToString();

            return (result);
        }

        #region Latin case converter
        public static string Upper(this string text, System.Globalization.CultureInfo enc = null)
        {
            var culture = enc is System.Globalization.CultureInfo ? enc : System.Globalization.CultureInfo.CurrentCulture;
            return (culture.TextInfo.ToUpper(text));
        }

        public static string Lower(this string text, System.Globalization.CultureInfo enc = null)
        {
            var culture = enc is System.Globalization.CultureInfo ? enc : System.Globalization.CultureInfo.CurrentCulture;
            return (culture.TextInfo.ToLower(text));
        }

        public static string CapsWord(this string text, System.Globalization.CultureInfo enc = null)
        {
            var culture = enc is System.Globalization.CultureInfo ? enc : System.Globalization.CultureInfo.CurrentCulture;
            return (culture.TextInfo.ToTitleCase(text));
        }

        public static string CapsWordForce(this string text, System.Globalization.CultureInfo enc = null)
        {
            var culture = enc is System.Globalization.CultureInfo ? enc : System.Globalization.CultureInfo.CurrentCulture;
            return (culture.TextInfo.ToTitleCase(text.ToLower()));
        }

        public static string CapsSentence(this string text, System.Globalization.CultureInfo enc = null)
        {
            string result = text;

            var culture = enc is System.Globalization.CultureInfo ? enc : System.Globalization.CultureInfo.CurrentCulture;

            var paras = text.Split(LINEBREAK, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            foreach (var para in paras)
            {
                var sentences = para.Split(". ");

                List<string> ls = new List<string>();
                foreach (var sentence in sentences)
                {
                    if (string.IsNullOrEmpty(sentence))
                    {
                        ls.Add("");
                        continue;
                    }

                    var prefix = string.Empty;
                    var first = string.Empty;
                    var suffix = string.Empty;

                    for (var i = 0; i < sentence.Length; i++)
                    {
                        if (char.IsLetter(sentence.Skip(i).First()))
                        {
                            prefix = string.Join("", sentence.Take(i));
                            if (i == 0 || prefix.EndsWith(" ") || prefix.EndsWith(".") || prefix.EndsWith(","))
                            {
                                first = string.Join("", sentence.Skip(i).First());
                                suffix = string.Join("", sentence.Skip(i + 1));
                                ls.Add($"{prefix}{culture.TextInfo.ToTitleCase(first)}{suffix}");
                                break;
                            }
                            else prefix = string.Empty;
                        }
                    }

                    if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(first) && string.IsNullOrEmpty(suffix))
                        ls.Add(sentence);
                }
                sb.AppendLine(string.Join(". ", ls));
            }
            result = sb.ToString();

            return (result);
        }
        #endregion

        #region Japaness case converter
        private static Dictionary<char, char> KanaCaseMap = new Dictionary<char, char>()
        {
            { '\uFF65', '\u30FB' }, // '･' : '・'
            { '\uFF66', '\u30F2' }, // 'ｦ' : 'ヲ'
            { '\uFF67', '\u30A1' }, // 'ｧ' : 'ァ'
            { '\uFF68', '\u30A3' }, // 'ｨ' : 'ィ'
            { '\uFF69', '\u30A5' }, // 'ｩ' : 'ゥ'
            { '\uFF6A', '\u30A7' }, // 'ｪ' : 'ェ'
            { '\uFF6B', '\u30A9' }, // 'ｫ' : 'ォ'
            { '\uFF6C', '\u30E3' }, // 'ｬ' : 'ャ'
            { '\uFF6D', '\u30E5' }, // 'ｭ' : 'ュ'
            { '\uFF6E', '\u30E7' }, // 'ｮ' : 'ョ'
            { '\uFF6F', '\u30C3' }, // 'ｯ' : 'ッ'
            { '\uFF70', '\u30FC' }, // 'ｰ' : 'ー'
            { '\uFF71', '\u30A2' }, // 'ｱ' : 'ア'
            { '\uFF72', '\u30A4' }, // 'ｲ' : 'イ'
            { '\uFF73', '\u30A6' }, // 'ｳ' : 'ウ'
            { '\uFF74', '\u30A8' }, // 'ｴ' : 'エ'
            { '\uFF75', '\u30AA' }, // 'ｵ' : 'オ'
            { '\uFF76', '\u30AB' }, // 'ｶ' : 'カ'
            { '\uFF77', '\u30AD' }, // 'ｷ' : 'キ'
            { '\uFF78', '\u30AF' }, // 'ｸ' : 'ク'
            { '\uFF79', '\u30B1' }, // 'ｹ' : 'ケ'
            { '\uFF7A', '\u30B3' }, // 'ｺ' : 'コ'
            { '\uFF7B', '\u30B5' }, // 'ｻ' : 'サ'
            { '\uFF7C', '\u30B7' }, // 'ｼ' : 'シ'
            { '\uFF7D', '\u30B9' }, // 'ｽ' : 'ス'
            { '\uFF7E', '\u30BB' }, // 'ｾ' : 'セ'
            { '\uFF7F', '\u30BD' }, // 'ｿ' : 'ソ'
            { '\uFF80', '\u30BF' }, // 'ﾀ' : 'タ'
            { '\uFF81', '\u30C1' }, // 'ﾁ' : 'チ'
            { '\uFF82', '\u30C4' }, // 'ﾂ' : 'ツ'
            { '\uFF83', '\u30C6' }, // 'ﾃ' : 'テ'
            { '\uFF84', '\u30C8' }, // 'ﾄ' : 'ト'
            { '\uFF85', '\u30CA' }, // 'ﾅ' : 'ナ'
            { '\uFF86', '\u30CB' }, // 'ﾆ' : 'ニ'
            { '\uFF87', '\u30CC' }, // 'ﾇ' : 'ヌ'
            { '\uFF88', '\u30CD' }, // 'ﾈ' : 'ネ'
            { '\uFF89', '\u30CE' }, // 'ﾉ' : 'ノ'
            { '\uFF8A', '\u30CF' }, // 'ﾊ' : 'ハ'
            { '\uFF8B', '\u30D2' }, // 'ﾋ' : 'ヒ'
            { '\uFF8C', '\u30D5' }, // 'ﾌ' : 'フ'
            { '\uFF8D', '\u30D8' }, // 'ﾍ' : 'ヘ'
            { '\uFF8E', '\u30DB' }, // 'ﾎ' : 'ホ'
            { '\uFF8F', '\u30DE' }, // 'ﾏ' : 'マ'
            { '\uFF90', '\u30DF' }, // 'ﾐ' : 'ミ'
            { '\uFF91', '\u30E0' }, // 'ﾑ' : 'ム'
            { '\uFF92', '\u30E1' }, // 'ﾒ' : 'メ'
            { '\uFF93', '\u30E2' }, // 'ﾓ' : 'モ'
            { '\uFF94', '\u30E4' }, // 'ﾔ' : 'ヤ'
            { '\uFF95', '\u30E6' }, // 'ﾕ' : 'ユ'
            { '\uFF96', '\u30E8' }, // 'ﾖ' : 'ヨ'
            { '\uFF97', '\u30E9' }, // 'ﾗ' : 'ラ'
            { '\uFF98', '\u30EA' }, // 'ﾘ' : 'リ'
            { '\uFF99', '\u30EB' }, // 'ﾙ' : 'ル'
            { '\uFF9A', '\u30EC' }, // 'ﾚ' : 'レ'
            { '\uFF9B', '\u30ED' }, // 'ﾛ' : 'ロ'
            { '\uFF9C', '\u30EF' }, // 'ﾜ' : 'ワ'
            { '\uFF9D', '\u30F3' }, // 'ﾝ' : 'ン'
            { '\uFF9E', '\u309B' }, // 'ﾞ' : '゛'
            { '\uFF9F', '\u309C' }, // 'ﾟ' : '゜'
        };

        private static class JapanDigital
        {
            // General
            static public Dictionary<char, string> Number = new Dictionary<char, string>()
            {
                { '0', "零" }, { '1', "壱" }, { '2', "弐" }, { '3', "参" }, { '4', "四" },
                { '5', "五" }, { '6', "六" }, { '7', "七" }, { '8', "八" }, { '9', "九" }
            };
            static public Dictionary<int, string> DigitBase = new Dictionary<int, string>()
            {
                { 1, "十" }, { 2, "百" }, { 3, "千" },
            };
            static public Dictionary<int, string> Digit = new Dictionary<int, string>()
            {
                { 1, "十" }, { 2, "百" }, { 3, "千" }, { 4, "万" },
                { 5, "十" }, { 6, "百" }, { 7, "千" }, { 8, "億" },
                { 9, "十" }, { 10, "百" }, { 11, "千" }, { 12, "兆" },
                { 13, "十" }, { 14, "百" }, { 15, "千" }, { 16, "京" },
                { 17, "十" }, { 18, "百" }, { 19, "千" }, { 20, "垓" },
                { 21, "十" }, { 22, "百" }, { 23, "千" }, { 24, "秭" },
                { 25, "十" }, { 26, "百" }, { 27, "千" }, { 28, "穰" },
                { 29, "十" }, { 30, "百" }, { 31, "千" }, { 32, "溝" },
                { 33, "十" }, { 34, "百" }, { 35, "千" }, { 36, "澗" },
                { 37, "十" }, { 38, "百" }, { 39, "千" }, { 40, "正" },
                { 41, "十" }, { 42, "百" }, { 43, "千" }, { 44, "載" },
                { 45, "十" }, { 46, "百" }, { 47, "千" }, { 48, "極" },
                { 49, "十" }, { 50, "百" }, { 51, "千" }, { 52, "恒河沙" },
                { 53, "十" }, { 54, "百" }, { 55, "千" }, { 56, "阿僧祇" },
                { 57, "十" }, { 58, "百" }, { 59, "千" }, { 60, "那由他" },
                { 61, "十" }, { 62, "百" }, { 63, "千" }, { 64, "不可思議" },
                { 65, "十" }, { 66, "百" }, { 67, "千" }, { 0, "無量大数" }
            };
            static public Dictionary<string, string> Sign = new Dictionary<string, string>()
            {
                { "+", "正" }, { "-", "負" }, { ".", "・" }
            };

            // Currency
            static public Dictionary<char, string> CurrencyNumber = new Dictionary<char, string>()
            {
                { '0', "零" }, { '1', "壱" }, { '2', "弐" }, { '3', "参" }, { '4', "四" },
                { '5', "五" }, { '6', "六" }, { '7', "七" }, { '8', "八" }, { '9', "九" }
            };
            static public Dictionary<int, string> CurrencyDigitBase = new Dictionary<int, string>()
            {
                { 1, "拾" }, { 2, "百" }, { 3, "千" },
            };
            static public Dictionary<int, string> CurrencyDigit = new Dictionary<int, string>()
            {
                { 1, "拾" }, { 2, "百" }, { 3, "千" }, { 4, "万" },
                { 5, "拾" }, { 6, "百" }, { 7, "千" }, { 8, "億" },
                { 9, "拾" }, { 10, "百" }, { 11, "千" }, { 12, "兆" },
                { 13, "拾" }, { 14, "百" }, { 15, "千" }, { 16, "京" },
                { 17, "拾" }, { 18, "百" }, { 19, "千" }, { 20, "垓" },
                { 21, "拾" }, { 22, "百" }, { 23, "千" }, { 24, "秭" },
                { 25, "拾" }, { 26, "百" }, { 27, "千" }, { 28, "穰" },
                { 29, "拾" }, { 30, "百" }, { 31, "千" }, { 32, "溝" },
                { 33, "拾" }, { 34, "百" }, { 35, "千" }, { 36, "澗" },
                { 37, "拾" }, { 38, "百" }, { 39, "千" }, { 40, "正" },
                { 41, "拾" }, { 42, "百" }, { 43, "千" }, { 44, "載" },
                { 45, "拾" }, { 46, "百" }, { 47, "千" }, { 48, "極" },
                { 49, "拾" }, { 50, "百" }, { 51, "千" }, { 52, "恒河沙" },
                { 53, "拾" }, { 54, "百" }, { 55, "千" }, { 56, "阿僧祇" },
                { 57, "拾" }, { 58, "百" }, { 59, "千" }, { 60, "那由他" },
                { 61, "拾" }, { 62, "百" }, { 63, "千" }, { 64, "不可思議" },
                { 65, "拾" }, { 66, "百" }, { 67, "千" }, { 0, "無量大数" }
            };
            static public Dictionary<int, string> CurrencyUnit = new Dictionary<int, string>()
            {
                { 0, "円" }, { 1, "割" }, { 2, "分" }, { 3, "厘" }, { 4, "毛" }, { 5, "糸" }
            };
            static public Dictionary<string, string> CurrencySign = new Dictionary<string, string>()
            {
                { "+", "正" }, { "-", "負" }, { ".", "円" }
            };
        }

        public static string JapanNumToUpper(this string text, bool IsCurrency = false)
        {
            string result = text;

            var ja = System.Globalization.CultureInfo.GetCultureInfo("ja");

            var pat = @"(([+-]){0,1}(\d+)((,\d{3,3})*)(\.\d+){0,1})";
            Dictionary<string, string> replist = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(text, pat))
            {
                if (m.Groups.Count > 0)
                {
                    var t = m.Value;
                    var tu = t;

                    var s = m.Groups[2].Value;
                    var i = $"{m.Groups[3].Value}{m.Groups[4].Value}".Replace(",", "").TrimStart('0');
                    var f = m.Groups[6].Value.TrimEnd('0');

                    var su = s;
                    var iu = i;
                    var fu = f;

                    if (!string.IsNullOrEmpty(s) && JapanDigital.Sign.ContainsKey(s))
                        su = JapanDigital.Sign[s];

                    List<string> vil = new List<string>();
                    for (int c = 0; c < i.Length; c++)
                    {
                        var cn = i.Length - c - 1;

                        if (cn != 0 || !i[c].Equals('0'))
                        {
                            if (IsCurrency)
                                vil.Add(JapanDigital.CurrencyNumber[i[c]]);
                            else
                                vil.Add(JapanDigital.Number[i[c]]);
                        }

                        if (cn > 0 && !i[c].Equals('0'))
                        {
                            if (IsCurrency)
                                vil.Add(JapanDigital.CurrencyDigit[cn % 68]);
                            else
                                vil.Add(JapanDigital.Digit[cn % 68]);
                        }
                        else if (cn > 1 && cn % 4 == 0 && i[c].Equals('0'))
                        {
                            for (int ic = vil.Count - 1; ic >= 0; ic--)
                            {
                                if (vil[ic].Equals("零"))
                                    vil.RemoveAt(ic);
                                else break;
                            }
                            if (IsCurrency)
                            {
                                if (JapanDigital.CurrencyNumber.ContainsValue(vil.Last()) || JapanDigital.CurrencyDigitBase.ContainsValue(vil.Last()))
                                    vil.Add(JapanDigital.CurrencyDigit[cn % 68]);
                            }
                            else
                            {
                                if (JapanDigital.Number.ContainsValue(vil.Last()) || JapanDigital.DigitBase.ContainsValue(vil.Last()))
                                    vil.Add(JapanDigital.Digit[cn % 68]);
                            }
                        }
                    }

                    iu = string.Join("", vil);

                    if (!string.IsNullOrEmpty(f))
                    {
                        List<string> vfl = new List<string>();
                        if (!IsCurrency) vfl.Add(JapanDigital.Sign["."]);
                        for (int c = 1; c < f.Length; c++)
                        {
                            if (IsCurrency)
                            {
                                if (c == 6)
                                    vfl.Add(JapanDigital.Sign["."]);

                                if (c == 1 || c == 2)
                                {
                                    vfl.Add(JapanDigital.CurrencyNumber[f[c]]);
                                    vfl.Add(JapanDigital.CurrencyUnit[c]);
                                }
                                else
                                    vfl.Add(JapanDigital.CurrencyNumber[f[c]]);
                            }
                            else
                                vfl.Add(JapanDigital.Number[f[c]]);
                        }
                        if (IsCurrency && f.Length > 5)
                            vfl.Add(JapanDigital.CurrencyUnit[5]);
                        fu = string.Join("", vfl);
                    }

                    if (IsCurrency)
                        tu = $"{su}{iu.TrimEnd(JapanDigital.CurrencyNumber['0'][0])}{JapanDigital.CurrencyUnit[0]}{fu}也";
                    else
                        tu = $"{su}{iu.TrimEnd(JapanDigital.Number['0'][0])}{fu}";


                    if (!replist.ContainsKey(t))
                        replist[t] = Regex.Replace(tu, @"零+", "零", RegexOptions.IgnoreCase);
                }
            }

            var repl = replist.Keys.ToList();
            repl.Sort();
            repl.Reverse();
            foreach (var k in repl)
            {
                result = result.Replace(k, replist[k]);
            }

            return (result);
        }

        public static string JapanNumToLower(this string text, bool RMB = false)
        {
            string result = text;

            return (result);
        }

        public static string KatakanaHalfToFull(this string text)
        {
            var result = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {               
                if (text[i] == 32)
                {
                    result += (char)12288;
                }
                if (text[i] < 127)
                {
                    result += (char)(text[i] + 65248);
                }
            }

            if (string.IsNullOrEmpty(result))
                result = text;
            return result;
        }

        public static string KatakanaFullToHalf(this string text)
        {
            var result = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] > 65248 && text[i] < 65375)
                {
                    result += (char)(text[i] - 65248);
                }
                else
                {
                    result += (char)text[i];
                }
            }
            return result;
        }

        public static string KanaToUpper(this string text)
        {
            var result = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {
                if (KanaCaseMap.ContainsKey(text[i])) result += KanaCaseMap[text[i]];
                else result += text[i];
            }
            return (result);
        }

        public static string KanaToLower(this string text)
        {
            var result = string.Empty;
            for (var i = 0; i < text.Length; i++)
            {                
                if (KanaCaseMap.ContainsValue(text[i])) result += KanaCaseMap.FirstOrDefault(x => x.Value == text[i]).Key;
                else result += text[i];
            }
            return (result);
        }

        #endregion

        #region Chinese case converter
        private static class ChinaDigital
        {
            // General
            static public Dictionary<char, string> Number = new Dictionary<char, string>()
            {
                { '0', "零" }, { '1', "一" }, { '2', "二" }, { '3', "三" }, { '4', "四" },
                { '5', "五" }, { '6', "六" }, { '7', "七" }, { '8', "八" }, { '9', "九" }
            };
            static public Dictionary<int, string> DigitBase = new Dictionary<int, string>()
            {
                { 1, "十" }, { 2, "百" }, { 3, "千" },
            };
            static public Dictionary<int, string> Digit = new Dictionary<int, string>()
            {
                { 1, "十" }, { 2, "百" }, { 3, "千" }, { 4, "万" },
                { 5, "十" }, { 6, "百" }, { 7, "千" }, { 8, "亿" },
                { 9, "十" }, { 10, "百" }, { 11, "千" }, { 12, "兆" },
                { 13, "十" }, { 14, "百" }, { 15, "千" }, { 16, "京" },
                { 17, "十" }, { 18, "百" }, { 19, "千" }, { 20, "垓" },
                { 21, "十" }, { 22, "百" }, { 23, "千" }, { 24, "秭" },
                { 25, "十" }, { 26, "百" }, { 27, "千" }, { 28, "穰" },
                { 29, "十" }, { 30, "百" }, { 31, "千" }, { 32, "溝" },
                { 33, "十" }, { 34, "百" }, { 35, "千" }, { 36, "澗" },
                { 37, "十" }, { 38, "百" }, { 39, "千" }, { 40, "正" },
                { 41, "十" }, { 42, "百" }, { 43, "千" }, { 0, "載" },
            };
            static public Dictionary<string, string> Sign = new Dictionary<string, string>()
            {
                { "+", "正" }, { "-", "负" }, { ".", "点" }
            };

            // Currency
            static public Dictionary<char, string> CurrencyNumber = new Dictionary<char, string>()
            {
                { '0', "零" }, { '1', "壹" }, { '2', "贰" }, { '3', "叁" }, { '4', "肆" },
                { '5', "伍" }, { '6', "陆" }, { '7', "染" }, { '8', "捌" }, { '9', "玖" }
            };

            static public Dictionary<int, string> CurrencyDigitBase = new Dictionary<int, string>()
            {
                { 1, "拾" }, { 2, "佰" }, { 3, "仟" },
            };
            static public Dictionary<int, string> CurrencyDigit = new Dictionary<int, string>()
            {
                { 1, "拾" }, { 2, "佰" }, { 3, "仟" }, { 4, "萬" },
                { 5, "拾" }, { 6, "佰" }, { 7, "仟" }, { 8, "億" },
                { 9, "拾" }, { 10, "佰" }, { 11, "仟" }, { 12, "兆" },
                { 13, "拾" }, { 14, "佰" }, { 15, "仟" }, { 16, "京" },
                { 17, "拾" }, { 18, "佰" }, { 19, "仟" }, { 20, "垓" },
                { 21, "拾" }, { 22, "佰" }, { 23, "仟" }, { 24, "秭" },
                { 25, "拾" }, { 26, "佰" }, { 27, "仟" }, { 28, "穰" },
                { 29, "拾" }, { 30, "佰" }, { 31, "仟" }, { 32, "溝" },
                { 33, "拾" }, { 34, "佰" }, { 35, "仟" }, { 36, "澗" },
                { 37, "拾" }, { 38, "佰" }, { 39, "仟" }, { 40, "正" },
                { 41, "拾" }, { 42, "佰" }, { 43, "仟" }, { 0, "載" },
            };
            static public Dictionary<int, string> CurrencyUnit = new Dictionary<int, string>()
            {
                { 0, "圆" }, { 1, "角" }, { 2, "分" },  { 3, "厘" }
            };
            static public Dictionary<string, string> CurrencySign = new Dictionary<string, string>()
            {
                { "+", "正" }, { "-", "负" }, { ".", "圆" }
            };
        }

        public static string ChinaNumToUpper(this string text, bool IsCurrency = false)
        {
            string result = text;

            var zh = System.Globalization.CultureInfo.GetCultureInfo("zh-Hans");

            var pat = @"(([+-]){0,1}(\d+)((,\d{3,3})*)(\.\d+){0,1})";
            Dictionary<string, string> replist = new Dictionary<string, string>();
            foreach(Match m in Regex.Matches(text, pat))
            {
                if (m.Groups.Count > 0)
                {
                    var t = m.Value;
                    var tu = t;

                    var s = m.Groups[2].Value.TrimStart('0');
                    var i = $"{m.Groups[3].Value}{m.Groups[4].Value}".Replace(",", "").TrimStart('0');
                    var f = m.Groups[6].Value.TrimEnd('0');

                    var su = s;
                    var iu = i;
                    var fu = f;

                    if (!string.IsNullOrEmpty(s) && ChinaDigital.Sign.ContainsKey(s))
                        su = ChinaDigital.Sign[s];

                    List<string> vil = new List<string>();
                    for (int c = 0; c < i.Length; c++)
                    {
                        var cn = i.Length - c - 1;

                        if (cn != 0 || !i[c].Equals('0'))
                        {
                            if (IsCurrency)
                                vil.Add(ChinaDigital.CurrencyNumber[i[c]]);
                            else
                                vil.Add(ChinaDigital.Number[i[c]]);
                        }

                        if (cn > 0 && !i[c].Equals('0'))
                        { 
                            if (IsCurrency)
                                vil.Add(ChinaDigital.CurrencyDigit[cn % 44]);
                            else
                                vil.Add(ChinaDigital.Digit[cn % 44]);
                        }
                        else if (cn > 1 && cn % 4 == 0 && i[c].Equals('0'))
                        {
                            for (int ic = vil.Count - 1; ic >= 0; ic--)
                            {
                                if (vil[ic].Equals("零"))
                                    vil.RemoveAt(ic);
                                else break;
                            }
                            if (IsCurrency)
                            {
                                if (ChinaDigital.CurrencyNumber.ContainsValue(vil.Last()) || ChinaDigital.CurrencyDigitBase.ContainsValue(vil.Last()))
                                    vil.Add(ChinaDigital.CurrencyDigit[cn % 44]);
                            }
                            else
                            {
                                if (ChinaDigital.Number.ContainsValue(vil.Last()) || ChinaDigital.DigitBase.ContainsValue(vil.Last()))
                                    vil.Add(ChinaDigital.Digit[cn % 44]);
                            }
                        }
                    }

                    iu = string.Join("", vil);

                    if (!string.IsNullOrEmpty(f))
                    {
                        List<string> vfl = new List<string>();
                        if (!IsCurrency) vfl.Add(ChinaDigital.Sign["."]);
                        for (int c = 1; c < f.Length; c++)
                        {
                            if (IsCurrency)
                            {
                                if (c == 4)
                                    vfl.Add(ChinaDigital.Sign["."]);

                                if (c == 1 || c == 2)
                                {
                                    vfl.Add(ChinaDigital.CurrencyNumber[f[c]]);
                                    vfl.Add(ChinaDigital.CurrencyUnit[c]);
                                }
                                else
                                    vfl.Add(ChinaDigital.CurrencyNumber[f[c]]);
                            }
                            else
                                vfl.Add(ChinaDigital.Number[f[c]]);
                        }
                        if (IsCurrency && f.Length > 3)
                            vfl.Add(ChinaDigital.CurrencyUnit[3]);
                        fu = string.Join("", vfl);
                    }

                    if (IsCurrency)
                        tu = $"{su}{iu.TrimEnd(ChinaDigital.CurrencyNumber['0'][0])}{ChinaDigital.CurrencyUnit[0]}{fu}整";
                    else
                        tu = $"{su}{iu.TrimEnd(ChinaDigital.Number['0'][0])}{fu}";

                    if (!replist.ContainsKey(t))
                        replist[t] = Regex.Replace(tu, @"零+", "零", RegexOptions.IgnoreCase);
                }
            }

            var repl = replist.Keys.ToList();
            repl.Sort();
            repl.Reverse();
            foreach (var k in repl)
            {
                result = result.Replace(k, replist[k]);
            }

            return (result);
        }

        public static string ChinaNumToLower(this string text, bool RMB = false)
        {
            string result = text;

            var zh = System.Globalization.CultureInfo.CreateSpecificCulture("zh-Hans");

            return (result);
        }

        public static string ChinaHalfToFull(this string text)
        {
            string result = text;

            StringBuilder sb = new StringBuilder();
            var lines = text.Split(LINEBREAK, StringSplitOptions.None);
            foreach(var line in lines)
            {
                char[] c = line.ToCharArray();
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i] == 32)
                    {
                        c[i] = (char)12288;
                        continue;
                    }
                    if (c[i] < 127)
                        c[i] = (char)(c[i] + 65248);
                }
                sb.AppendLine(new string(c));
            }
            result = sb.ToString();

            return (result);
        }

        public static string ChinaFullToHalf(this string text)
        {
            string result = text;

            StringBuilder sb = new StringBuilder();
            var lines = text.Split(LINEBREAK, StringSplitOptions.None);
            foreach (var line in lines)
            {
                char[] c = line.ToCharArray();
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i] == 12288)
                    {
                        c[i] = (char)32;
                        continue;
                    }
                    if (c[i] > 65280 && c[i] < 65375)
                        c[i] = (char)(c[i] - 65248);
                }
                sb.AppendLine(new string(c));
            }
            result = sb.ToString();

            return (result);
        }

        public static string ChinaS2T(this string text)
        {
            return (TongWen.Core.Convert(text, TongWen.ChineseConversionDirection.SimplifiedToTraditional));
            //return (ChineseConverter.Convert(text, ChineseConversionDirection.SimplifiedToTraditional));
        }

        public static string ChinaT2S(this string text)
        {
            return (TongWen.Core.Convert(text, TongWen.ChineseConversionDirection.TraditionalToSimplified));
            //return (ChineseConverter.Convert(text, ChineseConversionDirection.TraditionalToSimplified));
        }
        #endregion

        #region Hash Calculator
        public static string CalcCRC32(this string text, Encoding enc = null)
        {
            string result = string.Empty;
            var codec = enc is Encoding ? enc : Encoding.Default;
            using (var hash = CRC32.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = hash.ComputeHash(codec.GetBytes(text));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sb = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("X2"));
                }
                result = sb.ToString();
            }
            return (result);
        }

        public static string CalcMD4(this string text, Encoding enc = null)
        {
            string result = string.Empty;
            var codec = enc is Encoding ? enc : Encoding.Default;
            using (var hash = MD4.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = hash.ComputeHash(codec.GetBytes(text));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sb = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("X2"));
                }
                result = sb.ToString();
            }
            return (result);
        }

        public static string CalcMD5(this string text, Encoding enc = null)
        {
            string result = string.Empty;
            var codec = enc is Encoding ? enc : Encoding.Default;
            using (var hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = hash.ComputeHash(codec.GetBytes(text));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sb = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("X2"));
                }
                result = sb.ToString();
            }
            return (result);
        }

        public static string CalcRIPE160(this string text, Encoding enc = null)
        {
            string result = string.Empty;
            var codec = enc is Encoding ? enc : Encoding.Default;
            //using (var hash = RIPEMD160.Create())
            //{
            //    // Convert the input string to a byte array and compute the hash.
            //    byte[] data = hash.ComputeHash(codec.GetBytes(text));

            //    // Create a new Stringbuilder to collect the bytes
            //    // and create a string.
            //    StringBuilder sb = new StringBuilder();

            //    // Loop through each byte of the hashed data 
            //    // and format each one as a hexadecimal string.
            //    for (int i = 0; i < data.Length; i++)
            //    {
            //        sb.Append(data[i].ToString("X2"));
            //    }
            //    result = sb.ToString();
            //}
            return (result);
        }

        public static string CalcSHA1(this string text, Encoding enc = null)
        {
            string result = string.Empty;
            var codec = enc is Encoding ? enc : Encoding.Default;
            using (var hash = SHA1.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = hash.ComputeHash(codec.GetBytes(text));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sb = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("X2"));
                }
                result = sb.ToString();
            }
            return (result);
        }

        public static string CalcSHA256(this string text, Encoding enc = null)
        {
            string result = string.Empty;
            var codec = enc is Encoding ? enc : Encoding.Default;
            using (var hash = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = hash.ComputeHash(codec.GetBytes(text));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sb = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("X2"));
                }
                result = sb.ToString();
            }
            return (result);
        }

        public static string CalcSHA384(this string text, Encoding enc = null)
        {
            string result = string.Empty;
            var codec = enc is Encoding ? enc : Encoding.Default;
            using (var hash = SHA384.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = hash.ComputeHash(codec.GetBytes(text));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sb = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("X2"));
                }
                result = sb.ToString();
            }
            return (result);
        }

        public static string CalcSHA512(this string text, Encoding enc = null)
        {
            string result = string.Empty;
            var codec = enc is Encoding ? enc : Encoding.Default;
            using (var hash = SHA512.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = hash.ComputeHash(codec.GetBytes(text));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sb = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sb.Append(data[i].ToString("X2"));
                }
                result = sb.ToString();
            }
            return (result);
        }

        #endregion

        #region Text process
        public static string TrimBlankTail(this string text)
        {
            var result = text;

            var lines = text.Split(LINEBREAK, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            foreach(var line in lines)
            {
                sb.AppendLine(line.TrimEnd());
            }
            result = sb.ToString();

            return (result);
        }

        public static string TrimBlanklLine(this string text, bool merge = true)
        {
            var result = text;

            var lines = text.Split(LINEBREAK, StringSplitOptions.RemoveEmptyEntries);
            List<string> ls = new List<string>();
            foreach (var line in lines)
            {
                ls.Add(line.TrimEnd());
            }
            if (merge)
                result = string.Join(Environment.NewLine + Environment.NewLine, ls);
            else
                result = string.Join(Environment.NewLine, ls);

            return (result);
        }

        public static string TabToSpace(this string text, int size = 2)
        {
            var result = text;

            var lines = text.Split(LINEBREAK, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            foreach (var line in lines)
            {
                var space = "  ";
                switch (size)
                {
                    case 2:
                        space = "  ";
                        break;
                    case 4:
                        space = "    ";
                        break;
                    case 8:
                        space = "        ";
                        break;
                    default:
                        space = "  ";
                        break;
                }
                sb.AppendLine(line.TrimEnd().Replace("\t", space));
            }
            result = sb.ToString();

            return (result);
        }

        public static string SpaceToTab(this string text, int size = 2)
        {
            var result = text;

            var lines = text.Split(LINEBREAK, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            foreach (var line in lines)
            {
                var space = "  ";
                switch (size)
                {
                    case 2:
                        space = "  ";
                        break;
                    case 4:
                        space = "    ";
                        break;
                    case 8:
                        space = "        ";
                        break;
                    default:
                        space = "  ";
                        break;
                }
                sb.AppendLine(line.TrimEnd().Replace(space, "\t"));
            }
            result = sb.ToString();

            return (result);
        }

        #endregion

        #region System encoder helper routines
        public static byte[] GetBOM(this Encoding enc)
        {
            byte[] result = new byte[] { };

            if (enc == Encoding.UTF8 || enc.WebName.Equals("utf-8", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[3] { 0xEF, 0xBB, 0xBF };
            else if (enc == Encoding.Unicode || enc.WebName.Equals("utf-16", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[2] { 0xFF, 0xFE };
            else if (enc == Encoding.BigEndianUnicode || enc.WebName.Equals("unicodeFFFE", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[2] { 0xFE, 0xFF };
            else if (enc == Encoding.UTF32 || enc.WebName.Equals("utf-32", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[4] { 0xFF, 0xFE, 0x00, 0x00 };
            else if (enc == Encoding.GetEncoding("utf-32BE") || enc.WebName.Equals("utf-32BE", StringComparison.CurrentCultureIgnoreCase))
                result = new byte[4] { 0x00, 0x00, 0xFE, 0xFF };

            return (result);
        }

        private static Dictionary<int, System.Globalization.CultureInfo> CodePageInfo = new Dictionary<int, System.Globalization.CultureInfo>();
        private static Dictionary<string, System.Globalization.CultureInfo> Cultures = new Dictionary<string, System.Globalization.CultureInfo>();

        public static System.Globalization.CultureInfo GetCulture(string cultureTag)
        {
            System.Globalization.CultureInfo result = System.Globalization.CultureInfo.CurrentCulture;

            if (Cultures.Count <= 0)
            {
                int[] europe = new int[]{ 1250, 1251, 1252, 1253, 1254, 1255, 1256, 1257, 1258, 1259 };
                var cultures = System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures);
                Cultures.TryAdd("1252", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("en"));
                foreach (var culture in cultures)
                {
                    if (!string.IsNullOrEmpty(culture.IetfLanguageTag))
                    {
                        Cultures.TryAdd(culture.IetfLanguageTag.ToLower(), culture);
                        if (europe.Contains(culture.TextInfo.ANSICodePage))
                            Cultures.TryAdd($"{culture.TextInfo.ANSICodePage}", culture);
                    }
                }
                Cultures.TryAdd("gbk", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("zh-hans"));
                Cultures.TryAdd("big5", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("zh-hant"));
                Cultures.TryAdd("jis", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("ja"));
                Cultures.TryAdd("eucjp", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("ja"));
                Cultures.TryAdd("korean", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("ko"));
                Cultures.TryAdd("euckr", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("ko"));
                Cultures.TryAdd("ascii", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("en"));
                Cultures.TryAdd("thai", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("th"));
            }
            var tag = cultureTag.ToLower();
            if (Cultures.ContainsKey(tag)) result = Cultures[tag];

            return (result);
        }

        public static System.Globalization.CultureInfo GetCodePageInfo(int codepage)
        {
            if (CodePageInfo.Count() <= 0)
            {
                var cultures = System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures);
                foreach (var culture in cultures)
                {
                    try
                    {
                        CodePageInfo.TryAdd(culture.TextInfo.ANSICodePage, culture);
                    }
                    catch (Exception) { }
                    try
                    {
                        CodePageInfo.TryAdd(culture.TextInfo.EBCDICCodePage, culture);
                    }
                    catch (Exception) { }

                    if (!culture.IetfLanguageTag.Contains("-"))
                        Cultures.TryAdd(culture.IetfLanguageTag, culture);

                    //try
                    //{
                    //    CodePageInfo.TryAdd(culture.TextInfo.MacCodePage, culture);
                    //}
                    //catch (Exception) { }
                    //try
                    //{
                    //    CodePageInfo.TryAdd(culture.TextInfo.OEMCodePage, culture);
                    //}
                    //catch (Exception) { }
                }
                foreach (var enc in Encoding.GetEncodings())
                {
                    try
                    {
                        CodePageInfo.TryAdd(enc.CodePage, new System.Globalization.CultureInfo(enc.CodePage));
                    }
                    catch (Exception) { }
                }
            }


            if (CodePageInfo.ContainsKey(codepage))
            {
                return (CodePageInfo[codepage]);
            }
            else
                return (null);
        }

        public static Encoding GetTextEncoder(int codepage)
        {
            var result = Encoding.Default;
            switch (codepage)
            {
                case 936:
                    result = Encoding.GetEncoding("GBK");
                    break;
                case 950:
                    result = Encoding.GetEncoding("BIG5");
                    break;
                case 932:
                    result = Encoding.GetEncoding("Shift-JIS");
                    break;
                case 949:
                    result = Encoding.GetEncoding("Korean");
                    break;
                case 1200:
                    result = Encoding.Unicode;
                    break;
                case 1201:
                    result = Encoding.BigEndianUnicode;
                    break;
                case 1250:
                    result = GetTextEncoder("1250");
                    break;
                case 1251:
                    result = GetTextEncoder("1251");
                    break;
                case 1252:
                    result = GetTextEncoder("1252");
                    break;
                case 1253:
                    result = GetTextEncoder("1253");
                    break;
                case 1254:
                    result = GetTextEncoder("1254");
                    break;
                case 1255:
                    result = GetTextEncoder("1255");
                    break;
                case 1256:
                    result = GetTextEncoder("1256");
                    break;
                case 1257:
                    result = GetTextEncoder("1257");
                    break;
                case 1258:
                    result = GetTextEncoder("1258");
                    break;
                case 65000:
                    result = Encoding.UTF7;
                    break;
                case 65001:
                    result = Encoding.UTF8;
                    break;
                default:
                    result = Encoding.ASCII;
                    break;
            }
            return (result);
        }

        public static Encoding GetTextEncoder(string fmt)
        {
            var result = Encoding.Default;

            var ENC_NAME = fmt.Trim();
            if (string.Equals(ENC_NAME, "UTF8", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.UTF8;
            else if (string.Equals(ENC_NAME, "Unicode", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.Unicode;

            else if (string.Equals(ENC_NAME, "GBK", StringComparison.CurrentCultureIgnoreCase)||
                     string.Equals(ENC_NAME, "936", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("GBK");
            else if (string.Equals(ENC_NAME, "BIG5", StringComparison.CurrentCultureIgnoreCase) ||
                     string.Equals(ENC_NAME, "950", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("BIG5");
            else if (string.Equals(ENC_NAME, "JIS", StringComparison.CurrentCultureIgnoreCase) ||
                     string.Equals(ENC_NAME, "932", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Shift-JIS");
            else if (string.Equals(ENC_NAME, "Korean", StringComparison.CurrentCultureIgnoreCase) ||
                     string.Equals(ENC_NAME, "949", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Korean");

            else if (string.Equals(ENC_NAME, "EUCJP", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("EUC-JP");
            else if (string.Equals(ENC_NAME, "EUCKR", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("EUC-KR");


            else if (string.Equals(ENC_NAME, "1250", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1250");
            else if (string.Equals(ENC_NAME, "1251", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1251");
            else if (string.Equals(ENC_NAME, "1252", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1252");
            else if (string.Equals(ENC_NAME, "1253", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1253");
            else if (string.Equals(ENC_NAME, "1254", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1254");
            else if (string.Equals(ENC_NAME, "1255", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1255");
            else if (string.Equals(ENC_NAME, "1256", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1256");
            else if (string.Equals(ENC_NAME, "1257", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1257");
            else if (string.Equals(ENC_NAME, "1258", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("Windows-1258");

            else if (string.Equals(ENC_NAME, "En", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("En").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "Fr", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("Fr").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "De", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("De").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "Es", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("Es").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "Pt", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("Pt").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "Nl", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("Nl").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "Ru", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("Ru").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "It", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("It").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "Gr", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("Gr").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "Da", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("Da").TextInfo.ANSICodePage);
            else if (string.Equals(ENC_NAME, "Cz", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding(GetCulture("Cz").TextInfo.ANSICodePage);

            else if (string.Equals(ENC_NAME, "Thai", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.GetEncoding("IBM-Thai");

            else if (string.Equals(ENC_NAME, "ASCII", StringComparison.CurrentCultureIgnoreCase))
                result = Encoding.ASCII;
            else
                result = Encoding.Default;

            return (result);
        }
        #endregion
    }
}
