using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core.Topologies
{
    [PublicAPI]
    public class ApplicationIdentitiesResolverSettings
    {
        public ApplicationIdentitiesResolverSettings(string unknownEnvironmentSuffix, IServiceDiscoveryManager serviceDiscoveryClient)
        {
            UnknownEnvironmentSuffix = unknownEnvironmentSuffix;
            ServiceDiscoveryClient = serviceDiscoveryClient;
        }

        public string UnknownEnvironmentSuffix { get; }

        public IServiceDiscoveryManager ServiceDiscoveryClient { get; }

        public TimeSpan IterationPeriod { get; set; } = 10.Seconds();

        public Func<IReadOnlyCollection<string>> EnvironmentsWhitelist { get; set; } = () => new[] {TopologyKey.DefaultEnvironment};

        public Func<IReadOnlyCollection<string>> ProjectsWhitelist { get; set; } = () => new string[0];
    }
}