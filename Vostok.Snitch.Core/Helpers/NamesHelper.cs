using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Hosting.Abstractions;
using Vostok.Snitch.Core.Models;

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

        internal static (string name, string suffix) GetRealServiceName(string service)
        {
            var index = service.IndexOf(" via ", StringComparison.InvariantCultureIgnoreCase);
            return index != -1
                ? (service.Substring(0, index), service.Substring(index))
                : (service, string.Empty);
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

        internal static IVostokApplicationIdentity AddServiceNameSuffix(IVostokApplicationIdentity identity, string suffix)
        {
            if (string.IsNullOrEmpty(suffix))
                return identity;

            return new ApplicationIdentity(
                identity.Project,
                identity.Subproject,
                identity.Environment,
                $"{identity.Application}{suffix}",
                identity.Instance);
        }
    }
}