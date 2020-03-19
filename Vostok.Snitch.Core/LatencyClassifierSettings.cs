using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Time;

namespace Vostok.Snitch.Core
{
    [PublicAPI]
    public class LatencyClassifierSettings
    {
        public TimeSpan SlowClassificationThreshold { get; set; } = 2.Seconds();

        [CanBeNull]
        public Dictionary<string, LatencyClassifierServiceSettings> PerServiceSettings { get; set; }
    }
}