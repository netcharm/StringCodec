using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StringCodec.UWP.Common
{
    class TextCodecs
    {
        class BASE64
        {
            public string Encode(string text)
            {
                string result = string.Empty;

                byte[] arr = Encoding.ASCII.GetBytes(text);
                result = Convert.ToBase64String(arr, Base64FormattingOptions.None);

                return (result);
            }

            public string Decode(string text)
            {
                string result = string.Empty;

                byte[] arr = Convert.FromBase64String(text);
                //result = Encoding.ASCII.ToString(arr);
                

                return (result);
            }

        }
    }
}
