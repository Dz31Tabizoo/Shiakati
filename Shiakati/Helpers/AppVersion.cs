using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Helpers
{
    public static class AppVersion
    {
        public static string GetVersion()
        {
            var attr = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            string version = attr?.InformationalVersion ?? "0.0.0";
            // Supprime le suffixe +hash s'il existe
            int plusIndex = version.IndexOf('+');
            return plusIndex > -1 ? version.Substring(0, plusIndex) : version;
        }
    }
}
