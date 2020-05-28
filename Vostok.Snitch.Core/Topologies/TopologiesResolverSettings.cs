using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.ClusterConfig.Client.Abstractions;
using Vostok.Commons.Time;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core.Topologies
{
    [PublicAPI]
    public class TopologiesResolverSettings
    {
        public TopologiesResolverSettings(IClusterConfigClient clusterConfigClient, IServiceDiscoveryManager serviceDiscoveryClient)
        {
            ClusterConfigClient = clusterConfigClient;
            ServiceDiscoveryClient = serviceDiscoveryClient;
        }

        public IClusterConfigClient ClusterConfigClient { get; }

        public IServiceDiscoveryManager ServiceDiscoveryClient { get; }

        public TimeSpan IterationPeriod { get; set; } = 10.Seconds();

        public TimeSpan DnsCacheTtl { get; set; } = 30.Minutes();

        public TimeSpan DnsResolveTimeout { get; set; } = 1.Seconds();

        public Func<IReadOnlyCollection<string>> EnvironmentsWhitelist { get; set; } = () => new[] {TopologyKey.DefaultEnvironment};
    }
}