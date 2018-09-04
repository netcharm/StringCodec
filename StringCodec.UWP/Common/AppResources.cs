using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;

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

        public static void Reload()
        {
            ResourceContext.GetForCurrentView().Reset();
            ResourceContext.GetForViewIndependentUse().Reset();

            _loader = ResourceLoader.GetForCurrentView("Resources");
            ResourceCache.Clear();
        }

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
                if (string.IsNullOrEmpty(s)) s = key;
                return s;
            }
            else
            {
                s = CurrentResourceLoader.GetString(key);
                ResourceCache[key] = s;
                if (string.IsNullOrEmpty(s)) s = key;
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
