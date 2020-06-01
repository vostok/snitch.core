using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Classifiers
{
    [PublicAPI]
    public class ResponseCodeClassifierSettings : ResponseCodeClassifierServiceSettings
    {
        [CanBeNull]
        public Dictionary<string, ResponseCodeClassifierServiceSettings> PerServiceSettings { get; set; }
    }
}