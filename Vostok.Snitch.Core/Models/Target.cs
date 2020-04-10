using JetBrains.Annotations;

namespace Vostok.Snitch.Core.Models
{
    [PublicAPI]
    public struct Target
    {
        public readonly string Environment;
        public readonly string Service;

        public Target(string environment, string service)
        {
            Service = service;
            Environment = environment;
        }

        #region EqualityMembers

        public bool Equals(Target other) =>
            string.Equals(Service, other.Service) && string.Equals(Environment, other.Environment);

        public override bool Equals(object obj) =>
            obj is Target other && Equals(other);

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