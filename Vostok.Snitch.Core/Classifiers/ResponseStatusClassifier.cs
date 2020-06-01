using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core.Classifiers
{
    [PublicAPI]
    public class ResponseStatusClassifier
    {
        private readonly Func<ResponseStatusClassifierSettings> settingsProvider;

        public ResponseStatusClassifier([CanBeNull] Func<ResponseStatusClassifierSettings> settingsProvider = null)
        {
            this.settingsProvider = settingsProvider;
        }

        public ResponseStatusClass Classify(string service, string status, TimeSpan latency)
        {
            if (status == null)
                return ResponseStatusClass.Success;

            var settings = settingsProvider?.Invoke() ?? new ResponseStatusClassifierSettings();
            var serviceSettings = GetServiceSettings(settings, service);

            ResponseStatusClass ClassifyTimeout() =>
                latency >= (serviceSettings?.TimeoutErrorClassificationThreshold ?? settings.TimeoutErrorClassificationThreshold)
                    ? ResponseStatusClass.Error
                    : ResponseStatusClass.Warning;

            if (serviceSettings?.SuccessStatuses?.Contains(status) == true)
                return ResponseStatusClass.Success;
            if (serviceSettings?.WarningStatuses?.Contains(status) == true)
                return ResponseStatusClass.Warning;
            if (serviceSettings?.ErrorStatuses?.Contains(status) == true)
                return ResponseStatusClass.Error;
            if (serviceSettings?.TimeoutStatuses?.Contains(status) == true)
                return ClassifyTimeout();

            if (settings.SuccessStatuses?.Contains(status) == true)
                return ResponseStatusClass.Success;
            if (settings.WarningStatuses?.Contains(status) == true)
                return ResponseStatusClass.Warning;
            if (settings.ErrorStatuses?.Contains(status) == true)
                return ResponseStatusClass.Error;
            if (settings?.TimeoutStatuses?.Contains(status) == true)
                return ClassifyTimeout();

            if (Equals(status, ClusterResultStatus.TimeExpired))
                return ClassifyTimeout();

            if (Equals(status, ClusterResultStatus.ReplicasExhausted) ||
                Equals(status, ClusterResultStatus.ReplicasNotFound) ||
                Equals(status, ClusterResultStatus.Throttled) ||
                Equals(status, ClusterResultStatus.UnexpectedException))
                return ResponseStatusClass.Error;

            if (Equals(status, ClusterResultStatus.IncorrectArguments))
                return ResponseStatusClass.Warning;

            return ResponseStatusClass.Success;
        }

        private bool Equals(string status, ClusterResultStatus expected)
        {
            return string.Equals(status, expected.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        private ResponseStatusClassifierServiceSettings GetServiceSettings(ResponseStatusClassifierSettings settings, string service)
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