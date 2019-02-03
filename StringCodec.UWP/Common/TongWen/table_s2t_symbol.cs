using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringCodec.UWP.Common.TongWen
{
    static public partial class Tables
    {
        static public Dictionary<string, string> s2t_symbol = new Dictionary<string, string>()
        {
            { "·","‧" },
            { "―", "─" },
            { "‖", "∥" },
            { "‘", "『" },
            { "’", "』" },
            { "“", "「" },
            { "”", "」" },
            { "″", "〞" },
            { "∏", "Π" },
            { "∑", "Σ" },
            { "∧", "︿" },
            { "∨", "﹀" },
            { "∶", "︰" },
            { "≈", "≒" },
            { "≤", "≦" },
            { "≥", "≧" },
            { "━", "─" },
            { "┃", "│" },
            { "┏", "┌" },
            { "┓", "┐" },
            { "┗", "└" },
            { "┛", "┘" },
            { "┣", "├" },
            { "┫", "┤" },
            { "┳", "┬" },
            { "┻", "┴" },
            { "╋", "┼" },
            { "〖", "【" },
            {"〗", "】" },
        };
    }
}
