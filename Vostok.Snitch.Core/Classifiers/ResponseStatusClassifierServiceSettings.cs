using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Classifiers
{
    [PublicAPI]
    public class ResponseStatusClassifierServiceSettings
    {
        [CanBeNull]
        public TimeSpan? TimeoutErrorClassificationThreshold { get; set; }

        [CanBeNull]
        public HashSet<string> SuccessStatuses { get; set; }

        [CanBeNull]
        public HashSet<string> WarningStatuses { get; set; }

        [CanBeNull]
        public HashSet<string> ErrorStatuses { get; set; }

        [CanBeNull]
        public HashSet<string> TimeoutStatuses { get; set; }
    }
}