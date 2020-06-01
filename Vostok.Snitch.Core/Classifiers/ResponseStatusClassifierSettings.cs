using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Classifiers
{
    [PublicAPI]
    public class ResponseStatusClassifierSettings : ResponseStatusClassifierServiceSettings
    {
        [CanBeNull]
        public Dictionary<string, ResponseStatusClassifierServiceSettings> PerServiceSettings { get; set; }
    }
}