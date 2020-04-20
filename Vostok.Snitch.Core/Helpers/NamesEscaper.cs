using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Helpers
{
    [PublicAPI]
    public static class NamesEscaper
    {
        public static string Escape(string name)
        {
            return name
                .Replace('.', '_')
                .Replace('/', '-')
                .Replace('\\', '-')
                .Replace(' ', '_')
                .Replace(':', '_')
                .Replace('*', '_')
                .Replace('#', '_')
                .Replace('~', '_');
        }
    }
}