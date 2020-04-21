using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Commons.Threading;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.Core.Models;
using Vostok.Commons.Helpers.Network;

namespace Vostok.Snitch.Core.Topologies
{
    [PublicAPI]
    public class TopologiesResolver : IDisposable
    {
        private const int NotStarted = 0;
        private const int Running = 1;
        private const int Disposed = 2;
        private readonly AtomicInt state = new AtomicInt(NotStarted);
        private readonly AsyncManualResetEvent updateCacheSignal = new AsyncManualResetEvent(true);

        private readonly TopologiesResolverSettings settings;
        private readonly ILog log;
        private readonly ConcurrentDictionary<TopologyReplica, List<TopologyKey>> topologies;
        private readonly ConcurrentDictionary<string, string> hosts;
        private readonly CcTopologiesProvider ccTopologiesProvider;
        private readonly SdTopologiesProvider sdTopologiesProvider;
        private volatile Task updateCacheTask;
        private readonly DnsResolver dnsResolver;

        public TopologiesResolver(TopologiesResolverSettings settings, ILog log)
        {
            this.settings = settings;
            this.log = log = log.ForContext<TopologiesResolver>();
            topologies = new ConcurrentDictionary<TopologyReplica, List<TopologyKey>>();
            hosts = new ConcurrentDictionary<string, string>();
            ccTopologiesProvider = new CcTopologiesProvider(settings.ClusterConfigClient, log);
            sdTopologiesProvider = new SdTopologiesProvider(settings.ServiceDiscoveryClient, settings.EnvironmentsWhitelist, log);

            dnsResolver = new DnsResolver(settings.DnsCacheTtl, settings.DnsResolveTimeout);
        }

        public void Warmup()
        {
            if (!state.TryIncreaseTo(Running))
                throw new InvalidOperationException("Already warmed up.");

            UpdateCacheTaskIteration();

            updateCacheTask = Task.Run(UpdateCacheTask);
        }

        [NotNull]
        public string ResolveHost([NotNull] string host) =>
            hosts.TryGetValue(host, out var result) ? result : host;

        [NotNull]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public IEnumerable<TopologyKey> Resolve([NotNull] Uri url, [CanBeNull] string environment, [CanBeNull] string service)
        {
            if (state == NotStarted)
                throw new InvalidOperationException("Warmup should be called first.");

            environment = environment ?? TopologyKey.DefaultEnvironment;

            var replica = new TopologyReplica(ResolveHost(url.DnsSafeHost), url.Port, url.AbsolutePath);
            if (!topologies.TryGetValue(replica, out var nonFiltered))
            {
                if (service != null)
                    return new[] {new TopologyKey(environment, service)};

                return Enumerable.Empty<TopologyKey>();
            }

            var filteredByService = nonFiltered.Where(k => k.Service == service);
            if (!filteredByService.Any())
                return nonFiltered;

            var filteredByEnvironment = filteredByService.Where(k => k.Environment == environment);
            if (!filteredByEnvironment.Any())
                return filteredByService;

            return filteredByEnvironment;
        }

        public void Dispose()
        {
            if (state.TryIncreaseTo(Disposed))
            {
                updateCacheSignal.Set();

                updateCacheTask?.GetAwaiter().GetResult();
            }
        }

        private async Task UpdateCacheTask()
        {
            while (state == Running)
            {
                UpdateCacheTaskIteration();
                await updateCacheSignal.WaitAsync().WaitAsync(settings.IterationPeriod).ConfigureAwait(false);
                updateCacheSignal.Reset();
            }
        }

        private void UpdateCacheTaskIteration()
        {
            try
            {
                UpdateTopologies();
                UpdateHosts();
            }
            catch (Exception exception)
            {
                log.Error(exception, "Failed cache update iteration.");
            }
        }

        private void UpdateTopologies()
        {
            // ReSharper disable once ParameterHidesMember
            Dictionary<TopologyReplica, HashSet<TopologyKey>> BuildMapping(List<Topology> topologies)
            {
                var mapping = new Dictionary<TopologyReplica, HashSet<TopologyKey>>();

                foreach (var topology in topologies)
                {
                    foreach (var replica in topology.Replicas)
                    {
                        if (!mapping.ContainsKey(replica))
                            mapping[replica] = new HashSet<TopologyKey>();
                        mapping[replica].Add(topology.Key);
                    }
                }

                return mapping;
            }

            var sw = Stopwatch.StartNew();
            var ccTopologies = ccTopologiesProvider.Get();
            var ccMapping = BuildMapping(ccTopologies);
            log.Info("Resolved {Count} topologies from ClusterConfig in {Elapsed}.", ccTopologies.Count, sw.Elapsed.ToPrettyString());
            
            sw.Restart();
            var sdTopologies = sdTopologiesProvider.GetAsync().GetAwaiter().GetResult();
            var sdMapping = BuildMapping(sdTopologies);
            log.Info("Resolved {Count} topologies from ServiceDiscovery in {Elapsed}.", sdTopologies.Count, sw.Elapsed.ToPrettyString());

            var mergedMapping = sdMapping.ToDictionary(x => x.Key, x => x.Value);
            foreach (var pair in ccMapping)
            {
                if (!mergedMapping.ContainsKey(pair.Key))
                    mergedMapping.Add(pair.Key, pair.Value);
                else
                {
                    foreach (var x in pair.Value)
                    {
                        mergedMapping[pair.Key].Add(x);
                    }
                }
            }

            foreach (var pair in mergedMapping)
            {
                topologies[pair.Key] = pair.Value.ToList();
            }
            
            log.Info("Resolved {Count} topology keys.", topologies.Count);
        }

        private void UpdateHosts()
        {
            var used = new HashSet<string>();
            var sw = Stopwatch.StartNew();
            
            foreach (var t in topologies)
            {
                var host = t.Key.Host;
                if (used.Contains(host))
                    continue;
                used.Add(host);
                
                var ips = dnsResolver.Resolve(host, true);

                foreach (var ip in ips)
                {
                    hosts[ip.ToString()] = host;
                }
            }

            log.Info("Resolved {Count} host addresses in {Elapsed}.", hosts.Count, sw.Elapsed.ToPrettyString());
        }
    }
}