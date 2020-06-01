using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core.Classifiers
{
    [PublicAPI]
    public class ResponseCodeClassifier
    {
        private readonly Func<ResponseCodeClassifierSettings> settingsProvider;

        public ResponseCodeClassifier([CanBeNull] Func<ResponseCodeClassifierSettings> settingsProvider = null)
        {
            this.settingsProvider = settingsProvider;
        }

        public ResponseCodeClass Classify(string service, ResponseCode code, TimeSpan latency)
        {
            var settings = settingsProvider?.Invoke() ?? new ResponseCodeClassifierSettings();
            var serviceSettings = GetServiceSettings(settings, service);

            var serviceCodeClass = TryClassify(code, serviceSettings);
            if (serviceCodeClass != null)
                return serviceCodeClass.Value;

            var codeClass = TryClassify(code, settings);
            if (codeClass != null)
                return codeClass.Value;

            if (code.IsSuccessful() || code.IsRedirection() || code.IsInformational())
                return ResponseCodeClass.Success;

            if (code.IsServerError())
                return ResponseCodeClass.Error;

            if (code == ResponseCode.RequestTimeout)
            {
                return latency >= (serviceSettings?.TimeoutErrorClassificationThreshold ?? settings.TimeoutErrorClassificationThreshold)
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

        private ResponseCodeClass? TryClassify(ResponseCode code, ResponseCodeClassifierServiceSettings settings)
        {
            if (settings?.SuccessCodes?.Contains(code) == true)
                return ResponseCodeClass.Success;
            if (settings?.WarningCodes?.Contains(code) == true)
                return ResponseCodeClass.Warning;
            if (settings?.ErrorCodes?.Contains(code) == true)
                return ResponseCodeClass.Error;

            return null;
        }

        private ResponseCodeClassifierServiceSettings GetServiceSettings(ResponseCodeClassifierSettings settings, string service)
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