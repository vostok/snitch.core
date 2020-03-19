using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core
{
    [PublicAPI]
    public class UrlNormalizer
    {
        private const char Tilde = '~';

        private readonly Func<UrlNormalizerSettings> settingsProvider;

        public UrlNormalizer([CanBeNull] Func<UrlNormalizerSettings> settingsProvider = null)
        {
            this.settingsProvider = settingsProvider;
        }

        public string NormalizePath([CanBeNull] string service, [CanBeNull] string path)
        {
            if (path == null)
                return null;

            var normalizedPath = Commons.Helpers.Url.UrlNormalizer.NormalizePath(path);

            return Normalize(service, normalizedPath);
        }

        public string NormalizePath([CanBeNull] string service, [CanBeNull] Uri url)
        {
            if (url == null)
                return null;

            var normalizedPath = Commons.Helpers.Url.UrlNormalizer.NormalizePath(url);

            return Normalize(service, normalizedPath);
        }

        private string Normalize(string service, string normalizedPath)
        {
            var settings = settingsProvider?.Invoke();
            var serviceSettings = GetServiceSettings(settings, service);

            if (serviceSettings == null)
                return normalizedPath;

            if (serviceSettings.FilteredPrefixes != null)
            {
                foreach (var filteredPrefix in serviceSettings.FilteredPrefixes)
                {
                    if (filteredPrefix.Length == normalizedPath.Length && normalizedPath.Equals(filteredPrefix, StringComparison.InvariantCultureIgnoreCase))
                        return normalizedPath;

                    if (filteredPrefix.Length < normalizedPath.Length && normalizedPath.StartsWith(filteredPrefix, true, CultureInfo.InvariantCulture))
                    {
                        return filteredPrefix.ToLowerInvariant() + Tilde;
                    }
                }
            }

            return normalizedPath;
        }

        private UrlNormalizerServiceSettings GetServiceSettings(UrlNormalizerSettings settings, string service)
        {
            if (service == null ||
                settings?.PerServiceSettings == null ||
                !settings.PerServiceSettings.TryGetValue(service, out var serviceSettings) ||
                serviceSettings == null)
                return null;

            return serviceSettings;
        }
    }
}