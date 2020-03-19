﻿using System;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core
{
    [PublicAPI]
    public class LatencyClassifierServiceSettings
    {
        [CanBeNull]
        public TimeSpan? SlowClassificationThreshold { get; set; }
    }
}