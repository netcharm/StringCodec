using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringCodec.UWP.Common.TongWen
{
    public enum ChineseConversionDirection
    {
        //
        // 摘要:
        //     简体转换为繁体
        SimplifiedToTraditional = 0,
        //
        // 摘要:
        //     繁體轉換為簡體
        TraditionalToSimplified = 1
    }

    public static class Core
    {
        public static Dictionary<string, string> CustomPS2T { get; set; } = new Dictionary<string, string>();
        public static Dictionary<string, string> CustomPT2S { get; set; } = new Dictionary<string, string>();

        private static readonly string S2T_FILE = "CustomPhraseS2T.csv";
        private static readonly string T2S_FILE = "CustomPhraseT2S.csv";

        public static string ConvertPhrase(string text, ChineseConversionDirection direction)
        {
            string result = string.Empty;

            if (direction == ChineseConversionDirection.SimplifiedToTraditional)
            {
                result = (string)text.Clone();
                foreach(var c in result)
                {
                    var w = $"{c}";
                    if (Tables.s2t.ContainsKey(w))
                        result = result.Replace(w, Tables.s2t[w]);
                }
            }
            else if (direction == ChineseConversionDirection.TraditionalToSimplified)
            {
                result = (string)text.Clone();
                foreach (var c in result)
                {
                    var w = $"{c}";
                    if (Tables.t2s.ContainsKey(w))
                        result = result.Replace(w, Tables.t2s[w]);
                }
            }
            return (result);
        }

        public static string Convert(string text, ChineseConversionDirection direction)
        {
            string result = string.Empty;

            if (direction == ChineseConversionDirection.SimplifiedToTraditional)
            {
                result = (string)text.Clone();
                if(result.Length< Tables.s2t.Count / 2.0)
                {
                    foreach (var c in result)
                    {
                        var w = $"{c}";
                        if (Tables.s2t.ContainsKey(w))
                            result = result.Replace(w, Tables.s2t[w]);
                    }
                }
                else
                {
                    foreach (var kv in Tables.s2t)
                    {
                        result = result.Replace(kv.Key, kv.Value);
                    }
                }
                foreach (var kv in CustomPS2T)
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
                foreach (var kv in Tables.s2t_custom)
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
                foreach (var kv in Tables.s2t_phrase)
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
                foreach (var kv in Tables.s2t_symbol)
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
            }
            else if (direction == ChineseConversionDirection.TraditionalToSimplified)
            {
                result = (string)text.Clone();
                if (result.Length < Tables.t2s.Count / 2.0)
                {
                    foreach (var c in result)
                    {
                        var w = $"{c}";
                        if (Tables.t2s.ContainsKey(w))
                            result = result.Replace(w, Tables.t2s[w]);
                    }
                }
                else
                {
                    foreach (var kv in Tables.t2s)
                    {
                        result = result.Replace(kv.Key, kv.Value);
                    }
                }
                foreach (var kv in CustomPT2S)
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
                foreach (var kv in Tables.t2s_custom)
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
                foreach (var kv in Tables.t2s_phrase)
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
                foreach (var kv in Tables.t2s_symbol)
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
            }
            return (result);
        }

        public static async void LoadCustomPhrase()
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            Windows.Storage.StorageFile fs2t = await localFolder.CreateFileAsync(S2T_FILE, Windows.Storage.CreationCollisionOption.OpenIfExists);
            if (fs2t is Windows.Storage.StorageFile)
            {
                CustomPS2T.Clear();
                List<string> s2t = (await Windows.Storage.FileIO.ReadLinesAsync(fs2t)).ToList();
                foreach (var l in s2t)
                {
                    var kv = l.Split(",");

                    if (kv.Length == 2)
                        CustomPS2T.Add(kv[0].Trim(), kv[1].Trim());
                }
            }

            Windows.Storage.StorageFile ft2s = await localFolder.CreateFileAsync(T2S_FILE, Windows.Storage.CreationCollisionOption.OpenIfExists);
            if (ft2s is Windows.Storage.StorageFile)
            {
                CustomPT2S.Clear();
                List<string> t2s = (await Windows.Storage.FileIO.ReadLinesAsync(ft2s)).ToList();
                foreach (var l in t2s)
                {
                    var kv = l.Split(",");

                    if (kv.Length == 2)
                        CustomPT2S.Add(kv[0].Trim(), kv[1].Trim());
                }
            }
        }

        public static async void SaveCustomPhrase()
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            List<string> s2t = new List<string>();
            foreach (var kv in CustomPS2T)
            {
                s2t.Add($"{kv.Key} , {kv.Value}");
            }
            Windows.Storage.StorageFile fs2t = await storageFolder.CreateFileAsync(S2T_FILE, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteLinesAsync(fs2t, s2t);

            List<string> t2s = new List<string>();
            foreach (var kv in CustomPT2S)
            {
                t2s.Add($"{kv.Key} , {kv.Value}");
            }
            Windows.Storage.StorageFile ft2s = await storageFolder.CreateFileAsync(T2S_FILE, Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await Windows.Storage.FileIO.WriteLinesAsync(ft2s, t2s);
        }

        public static void Import(string text)
        {

        }

        public static void Export(string text)
        {

        }

        public static string ToTraditional(this string text, bool simple = false)
        {
            if (simple)
                return (ConvertPhrase(text, ChineseConversionDirection.SimplifiedToTraditional));
            else
                return (Convert(text, ChineseConversionDirection.SimplifiedToTraditional));
        }

        public static string ToSimplified(this string text, bool simple = false)
        {
            if (simple)
                return (ConvertPhrase(text, ChineseConversionDirection.TraditionalToSimplified));
            else
                return (Convert(text, ChineseConversionDirection.TraditionalToSimplified));
        }


    }
}
