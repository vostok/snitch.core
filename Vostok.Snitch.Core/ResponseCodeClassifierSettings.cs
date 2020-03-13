using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core
{
    [PublicAPI]
    public class ResponseCodeClassifierSettings
    {
        [CanBeNull]
        public Dictionary<string, UrlNormalizerServiceSettings> PerServiceSettings { get; set; }
    }
}