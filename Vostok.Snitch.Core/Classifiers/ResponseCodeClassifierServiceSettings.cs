using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Snitch.Core.Classifiers
{
    [PublicAPI]
    public class ResponseCodeClassifierServiceSettings
    {
        [CanBeNull]
        public TimeSpan? TimeoutErrorClassificationThreshold { get; set; }

        [CanBeNull]
        public HashSet<ResponseCode> SuccessCodes { get; set; }

        [CanBeNull]
        public HashSet<ResponseCode> WarningCodes { get; set; }

        [CanBeNull]
        public HashSet<ResponseCode> ErrorCodes { get; set; }
    }
}