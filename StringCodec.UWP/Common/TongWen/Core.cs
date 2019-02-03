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

    static public class Core
    {
        //static private Dictionary<string, string> CustomPS2T = new Dictionary<string, string>();
        //static private Dictionary<string, string> CustomPT2S = new Dictionary<string, string>();

        static public string Convert(string text, ChineseConversionDirection direction)
        {
            string result = string.Empty;

            if (direction == ChineseConversionDirection.SimplifiedToTraditional)
            {
                result = (string)text.Clone();
                foreach (var kv in Tables.s2t)
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
                foreach (var kv in Tables.t2s)
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

        static public void Import(string text)
        {

        }
    }
}
