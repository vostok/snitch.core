using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vostok.Commons.Helpers.Url;
using Vostok.Logging.Abstractions;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core.Topologies
{
    internal class SdTopologiesProvider
    {
        private readonly IServiceDiscoveryManager manager;
        private readonly Func<IReadOnlyCollection<string>> environmentsWhitelist;

        public SdTopologiesProvider(IServiceDiscoveryManager manager, Func<IReadOnlyCollection<string>> environmentsWhitelist, ILog log)
        {
            this.manager = manager;
            this.environmentsWhitelist = environmentsWhitelist;
        }

        public async Task<List<Topology>> GetAsync()
        {
            var result = new List<Topology>();

            var environments = await manager.GetAllEnvironmentsAsync().ConfigureAwait(false);

            foreach (var environment in environments.Where(e => environmentsWhitelist().Contains(e)))
            {
                var applications = await manager.GetAllApplicationsAsync(environment).ConfigureAwait(false);

                foreach (var application in applications)
                {
                    var replicas = (await manager.GetAllReplicasAsync(environment, application).ConfigureAwait(false))
                        .Select(UrlParser.Parse)
                        .Where(u => u != null)
                        .Select(u => new TopologyReplica(u))
                        .ToList();

                    if (!replicas.Any())
                        continue;

                    result.Add(new Topology(
                        new TopologyKey(environment, application), 
                        replicas));

#if DEBUG
                    if (result.Count > 100)
                        return result;
#endif
                }
            }

            return result;
        }
    }
}