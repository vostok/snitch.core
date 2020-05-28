using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Commons.Helpers.Network;
using Vostok.Commons.Threading;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.Core.Models;

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
        private readonly ConcurrentDictionary<(string host, int port), List<TopologyReplicaMeta>> topologies;
        private readonly ConcurrentDictionary<string, string> hosts;
        private readonly CcTopologiesProvider ccTopologiesProvider;
        private readonly SdTopologiesProvider sdTopologiesProvider;
        private readonly DnsResolver dnsResolver;
        private volatile Task updateCacheTask;

        public TopologiesResolver(TopologiesResolverSettings settings, ILog log)
        {
            this.settings = settings;
            this.log = log = log.ForContext<TopologiesResolver>();
            topologies = new ConcurrentDictionary<(string host, int port), List<TopologyReplicaMeta>>();
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
        public IEnumerable<TopologyKey> Resolve([NotNull] Uri url, [CanBeNull] string environment, [CanBeNull] string service)
        {
            if (state == NotStarted)
                throw new InvalidOperationException("Warmup should be called first.");

            var replica = new TopologyReplica(ResolveHost(url.DnsSafeHost), url.Port, url.AbsolutePath);
            if (!topologies.TryGetValue((replica.Host, replica.Port), out var candidate) || !candidate.Any())
            {
                if (service != null)
                    return new[] {new TopologyKey(environment ?? TopologyKey.DefaultEnvironment, service)};

                return Enumerable.Empty<TopologyKey>();
            }

            var filteredByService = candidate.Where(c => c.Key.Service == service).ToList();
            if (filteredByService.Any())
                candidate = filteredByService;

            var filteredByEnvironment = candidate.Where(c => c.Key.Environment == environment).ToList();
            if (filteredByEnvironment.Any())
                candidate = filteredByEnvironment;

            var filteredByPath = candidate.Where(c => url.AbsolutePath.StartsWith(c.Replica.Path)).ToList();
            if (filteredByPath.Any())
            {
                var maxMatch = filteredByPath.Max(c => c.Replica.Path.Length);
                filteredByPath = filteredByPath.Where(c => c.Replica.Path.Length == maxMatch).ToList();
                candidate = filteredByPath;
            }
            else
            {
                // Note(kungurtsev): no url with same path.
                return Enumerable.Empty<TopologyKey>();
            }

            var filteredBySource = candidate.Where(c => c.Source == TopologyReplicaMeta.SdSource).ToList();
            if (filteredBySource.Any())
                candidate = filteredBySource;

            return candidate.Select(c => c.Key).Distinct();
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
            Dictionary<(string host, int port), List<TopologyReplicaMeta>> BuildMapping(List<Topology> topologies, int source)
            {
                var mapping = new Dictionary<(string host, int port), List<TopologyReplicaMeta>>();

                foreach (var topology in topologies)
                {
                    foreach (var replica in topology.Replicas)
                    {
                        var key = (replica.Host, replica.Port);
                        if (!mapping.ContainsKey(key))
                            mapping[key] = new List<TopologyReplicaMeta>();
                        mapping[key].Add(new TopologyReplicaMeta(topology.Key, replica, source));
                    }
                }

                return mapping;
            }

            var sw = Stopwatch.StartNew();
            var ccTopologies = ccTopologiesProvider.Get();
            var ccMapping = BuildMapping(ccTopologies, TopologyReplicaMeta.CcSource);
            log.Info("Resolved {Count} topologies from ClusterConfig in {Elapsed}.", ccTopologies.Count, sw.Elapsed.ToPrettyString());

            sw.Restart();
            var sdTopologies = sdTopologiesProvider.GetAsync().GetAwaiter().GetResult();
            var sdMapping = BuildMapping(sdTopologies, TopologyReplicaMeta.SdSource);
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
                var host = t.Key.host;
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

        internal struct TopologyReplicaMeta
        {
            public const int SdSource = 0;
            public const int CcSource = 1;

            public TopologyKey Key;
            public TopologyReplica Replica;
            public int Source;

            public TopologyReplicaMeta(TopologyKey key, TopologyReplica replica, int source)
            {
                Key = key;
                Replica = replica;
                Source = source;
            }

            public override string ToString() =>
                $"{nameof(Key)}: {Key}, {nameof(Replica)}: {Replica}, {nameof(Source)}: {Source}";
        }
    }
}