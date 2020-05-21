using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Models
{
    [PublicAPI]
    public class TopologyKey
    {
        public const string DefaultEnvironment = "default";

        public readonly string Environment;
        public readonly string Service;

        public TopologyKey(string environment, string service)
        {
            Service = service;
            Environment = environment;
        }

        #region EqualityMembers

        public bool Equals(TopologyKey other) =>
            string.Equals(Service, other.Service) && string.Equals(Environment, other.Environment);

        public override bool Equals(object obj) =>
            obj is TopologyKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Service != null ? Service.GetHashCode() : 0) * 397) ^ (Environment != null ? Environment.GetHashCode() : 0);
            }
        }

        #endregion

        public override string ToString() =>
            $"{nameof(Environment)}: {Environment}, {nameof(Service)}: {Service}";
    }
}