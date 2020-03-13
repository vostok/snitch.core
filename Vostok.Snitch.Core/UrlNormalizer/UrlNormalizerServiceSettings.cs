using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core.UrlNormalizer
{
    [PublicAPI]
    public class UrlNormalizerServiceSettings
    {
        [CanBeNull]
        public IReadOnlyCollection<string> FilteredPrefixes { get; set; }
    }
}