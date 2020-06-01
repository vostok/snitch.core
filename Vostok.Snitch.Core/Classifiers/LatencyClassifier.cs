using System;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Classifiers
{
    [PublicAPI]
    public class LatencyClassifier
    {
        private readonly Func<LatencyClassifierSettings> settingsProvider;

        public LatencyClassifier([CanBeNull] Func<LatencyClassifierSettings> settingsProvider = null)
        {
            this.settingsProvider = settingsProvider;
        }

        public bool IsSlow(string service, TimeSpan latency)
        {
            var settings = settingsProvider?.Invoke();
            var serviceSettings = GetServiceSettings(settings, service);
            var threshold = serviceSettings?.SlowClassificationThreshold
                            ?? settings?.SlowClassificationThreshold
                            ?? new LatencyClassifierSettings().SlowClassificationThreshold;

            return latency >= threshold;
        }

        private LatencyClassifierServiceSettings GetServiceSettings(LatencyClassifierSettings settings, string service)
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