using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Snitch.Core
{
    [PublicAPI]
    public class StatusClassifierServiceSettings
    {
        [CanBeNull]
        public TimeSpan? TimeoutErrorClassificationThreshold { get; set; }

        [CanBeNull]
        public List<ResponseCode> SuccessCodes { get; set; }

        [CanBeNull]
        public List<ResponseCode> WarningCodes { get; set; }

        [CanBeNull]
        public List<ResponseCode> ErrorCodes { get; set; }
    }
}