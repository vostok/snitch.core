using System;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core
{
    [PublicAPI]
    public class ResponseCodeClassifier
    {
        private readonly Func<ResponseCodeClassifierSettings> settingsProvider;

        public ResponseCodeClassifier([CanBeNull] Func<ResponseCodeClassifierSettings> settingsProvider = null)
        {
            this.settingsProvider = settingsProvider;
        }
    }
}