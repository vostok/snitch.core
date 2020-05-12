using System;
using System.Globalization;
using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Models
{
    [PublicAPI]
    public struct TopologyReplica
    {
        public readonly string Host;
        public readonly int Port;
        public readonly string Path;

        public TopologyReplica(Uri url)
            : this(url.DnsSafeHost, url.Port, url.AbsolutePath)
        {
        }

        public TopologyReplica(string host, int port, string path)
        {
            Host = CutHostname(host).ToLowerInvariant();
            Port = port;
            Path = path.ToLowerInvariant();
        }

        public override string ToString()
        {
            return $"{Host}:{Port}{Path}";
        }

        private static string CutHostname(string host)
        {
            var dotIndex = host.IndexOf('.');
            if (dotIndex < 0)
                return host;

            if (char.IsDigit(host[0]))
                return host;

            return host.Substring(0, dotIndex);
        }

        #region Equality members

        public bool Equals(TopologyReplica other)
        {
            if (Host != other.Host)
                return false;
            if (Port != other.Port)
                return false;

            var len = Math.Min(Path.Length, other.Path.Length);
            return string.Compare(Path, 0, other.Path, 0, len, true, CultureInfo.InvariantCulture) == 0;
        }

        public override bool Equals(object obj) =>
            obj is TopologyReplica other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Host != null ? Host.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Port;
                return hashCode;
            }
        }

        #endregion
    }
}