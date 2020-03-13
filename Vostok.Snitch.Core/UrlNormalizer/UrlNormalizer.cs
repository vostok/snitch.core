using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core.UrlNormalizer
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

        public string NormalizePath([CanBeNull] string service, [CanBeNull] Uri url)
        {
            if (url == null)
                return null;

            var normalizedPath = Commons.Helpers.Url.UrlNormalizer.NormalizePath(url);

            var settings = settingsProvider?.Invoke();

            if (service == null ||
                settings?.PerServiceSettings == null ||
                !settings.PerServiceSettings.TryGetValue(service, out var serviceSettings) ||
                serviceSettings == null)
                return normalizedPath;

            if (serviceSettings.FilteredPrefixes != null)
            {
                foreach (var filteredPrefix in serviceSettings.FilteredPrefixes)
                {
                    if (normalizedPath.StartsWith(filteredPrefix, true, CultureInfo.InvariantCulture))
                    {
                        return filteredPrefix.ToLowerInvariant() + Tilde;
                    }
                }
            }

            return normalizedPath;
        }
    }
}