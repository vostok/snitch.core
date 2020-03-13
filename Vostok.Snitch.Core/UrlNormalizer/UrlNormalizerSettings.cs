using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core.UrlNormalizer
{
    [PublicAPI]
    public class UrlNormalizerSettings
    {
        [CanBeNull]
        public Dictionary<string, UrlNormalizerServiceSettings> PerServiceSettings { get; set; }
    }
}