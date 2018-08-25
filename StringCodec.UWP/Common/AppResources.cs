using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace StringCodec.UWP.Common
{
    class AppResources
    {
        private static ResourceLoader CurrentResourceLoader
        {
            get { return _loader ?? (_loader = ResourceLoader.GetForCurrentView("Resources")); }
        }

        private static ResourceLoader _loader;
        private static readonly Dictionary<string, string> ResourceCache = new Dictionary<string, string>();

        public static string _(string key)
        {
            return (GetString(key));
        }

        public static string T(string key)
        {
            return (GetString(key));
        }

        public static string GetString(string key)
        {
            if (ResourceCache.TryGetValue(key, out string s))
            {
                return s;
            }
            else
            {
                s = CurrentResourceLoader.GetString(key);
                ResourceCache[key] = s;
                return s;
            }
        }

        /// <summary>
        /// AppName
        /// </summary>
        public static string AppName
        {
            get
            {
                return CurrentResourceLoader.GetString("AppName");
            }
        }
    }
}
