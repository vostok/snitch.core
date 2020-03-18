using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core
{
    [PublicAPI]
    public class ResponseCodeClassifier
    {
        private readonly Func<StatusClassifierSettings> settingsProvider;

        public ResponseCodeClassifier([CanBeNull] Func<StatusClassifierSettings> settingsProvider = null)
        {
            this.settingsProvider = settingsProvider;
        }

        public ResponseCodeClass ClassifyResponseCode(string service, ResponseCode code, TimeSpan latency)
        {
            var settings = settingsProvider?.Invoke() ?? new StatusClassifierSettings();
            var serviceSettings = GetServiceSettings(settings, service);

            if (serviceSettings?.SuccessCodes?.Contains(code) == true)
                return ResponseCodeClass.Success;
            if (serviceSettings?.WarningCodes?.Contains(code) == true)
                return ResponseCodeClass.Warning;
            if (serviceSettings?.ErrorCodes?.Contains(code) == true)
                return ResponseCodeClass.Error;

            if (settings.SuccessCodes?.Contains(code) == true)
                return ResponseCodeClass.Success;
            if (settings.WarningCodes?.Contains(code) == true)
                return ResponseCodeClass.Warning;
            if (settings.ErrorCodes?.Contains(code) == true)
                return ResponseCodeClass.Error;

            if (code.IsSuccessful() || code.IsRedirection() || code.IsInformational())
                return ResponseCodeClass.Success;

            if (code.IsServerError())
                return ResponseCodeClass.Error;

            if (code == ResponseCode.RequestTimeout)
            {
                return latency > (serviceSettings?.TimeoutErrorClassificationThreshold ?? settings.TimeoutErrorClassificationThreshold)
                    ? ResponseCodeClass.Error
                    : ResponseCodeClass.Warning;
            }

            if (code.IsNetworkError())
                return ResponseCodeClass.Error;

            if (code == ResponseCode.Unknown ||
                code == ResponseCode.UnknownFailure)
                return ResponseCodeClass.Error;

            return ResponseCodeClass.Warning;
        }

        private StatusClassifierServiceSettings GetServiceSettings(StatusClassifierSettings settings, string service)
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