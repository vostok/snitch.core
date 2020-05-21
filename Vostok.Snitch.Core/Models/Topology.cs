using System.Collections.Generic;

namespace Vostok.Snitch.Core.Models
{
    internal class Topology
    {
        public readonly TopologyKey Key;
        public readonly IReadOnlyList<TopologyReplica> Replicas;

        public Topology(TopologyKey key, IReadOnlyList<TopologyReplica> replicas)
        {
            Key = key;
            Replicas = replicas;
        }

        public override string ToString() =>
            $"{nameof(Key)}: {Key}, {nameof(Replicas)}: " + string.Join(", ", Replicas);

    }
}