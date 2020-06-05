using System.Collections.Generic;
using System.Linq;
using Vostok.Clusterclient.Topology.CC.Helpers;
using Vostok.ClusterConfig.Client.Abstractions;
using Vostok.Configuration.Abstractions.SettingsTree;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core.Topologies
{
    internal class CcTopologiesProvider
    {
        private readonly IClusterConfigClient client;
        private readonly ClusterConfigReplicasParser parser;

        public CcTopologiesProvider(IClusterConfigClient client, ILog log)
        {
            this.client = client;
            parser = new ClusterConfigReplicasParser(log.WithMinimumLevel(LogLevel.Warn));
        }

        public List<Topology> Get()
        {
            var tree = client.Get("topology");
            var result = new List<Topology>();

            Dfs(tree, string.Empty, result, parser);

            return result;
        }

        private static void Dfs(ISettingsNode node, string path, List<Topology> result, ClusterConfigReplicasParser parser)
        {
#if DEBUG
            if (result.Count > 100)
                return;
#endif

            if (node.Flatten().ContainsKey(string.Empty))
            {
                var urls = parser.Parse(node, path);
                if (urls != null)
                {
                    result.Add(new Topology(new TopologyKey(TopologyKey.DefaultEnvironment, path), urls.Select(u => new TopologyReplica(u)).ToList()));
                }
            }

            foreach (var child in node.Children.Where(c => c is ObjectNode))
            {
                var childName = path == string.Empty ? child.Name : $"{path}.{child.Name}";
                Dfs(child, childName, result, parser);
            }
        }
    }
}