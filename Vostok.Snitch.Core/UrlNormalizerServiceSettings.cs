using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core
{
    [PublicAPI]
    public class UrlNormalizerServiceSettings
    {
        [CanBeNull]
        public IReadOnlyCollection<string> FilteredPrefixes { get; set; }
    }
}