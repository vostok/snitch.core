﻿using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Models
{
    [PublicAPI]
    public enum ResponseCodeClass : byte
    {
        Success = 0,
        Warning = 1,
        Error = 2
    }
}