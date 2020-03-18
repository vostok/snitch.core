using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Time;

namespace Vostok.Snitch.Core
{
    [PublicAPI]
    public class StatusClassifierSettings
    {
        public TimeSpan TimeoutErrorClassificationThreshold { get; set; } = 100.Milliseconds();

        [CanBeNull]
        public List<ResponseCode> SuccessCodes { get; set; }

        [CanBeNull]
        public List<ResponseCode> WarningCodes { get; set; }

        [CanBeNull]
        public List<ResponseCode> ErrorCodes { get; set; }

        [CanBeNull]
        public Dictionary<string, StatusClassifierServiceSettings> PerServiceSettings { get; set; }
    }
}