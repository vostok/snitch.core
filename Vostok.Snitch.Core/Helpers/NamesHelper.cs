using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Helpers
{
    [PublicAPI]
    public static class NamesHelper
    {
        public static string Escape(string name)
        {
            return name
                .Replace('.', '_')
                .Replace('/', '-')
                .Replace('\\', '-')
                .Replace(' ', '_')
                .Replace(':', '_')
                .Replace('*', '_')
                .Replace('#', '_')
                .Replace('~', ':');
        }

        internal static string GetRealServiceName(string service)
        {
            if (!string.IsNullOrEmpty(service) && service.Contains(" via "))
                service = service.Substring(0, service.IndexOf(" via ", StringComparison.InvariantCultureIgnoreCase));

            return service;
        }

        internal static string GenerateProjectName(string service, IReadOnlyCollection<string> projectsWhitelist)
        {
            if (!string.IsNullOrEmpty(service) && projectsWhitelist != null)
            {
                var tokens = service.Split('.', '_');
                if (tokens.Length > 1)
                {
                    if (projectsWhitelist.Contains(tokens[0]))
                        return tokens[0];
                }
            }

            return "unknown";
        }
    }
}