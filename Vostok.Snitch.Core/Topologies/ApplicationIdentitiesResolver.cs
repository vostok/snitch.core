using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Commons.Threading;
using Vostok.Commons.Time;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.Core.Helpers;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Core.Topologies
{
    [PublicAPI]
    public class ApplicationIdentitiesResolver : IDisposable
    {
        private const int NotStarted = 0;
        private const int Running = 1;
        private const int Disposed = 2;
        private readonly AtomicInt state = new AtomicInt(NotStarted);
        private readonly AsyncManualResetEvent updateCacheSignal = new AsyncManualResetEvent(true);

        private readonly ApplicationIdentitiesResolverSettings settings;
        private readonly ILog log;
        private readonly SdTopologiesProvider sdTopologiesProvider;
        private readonly ConcurrentDictionary<(string environment, string service), IVostokApplicationIdentity> identities;
        private readonly ConcurrentDictionary<string, string> parentEnvironment;
        private volatile Task updateCacheTask;

        public ApplicationIdentitiesResolver(ApplicationIdentitiesResolverSettings settings, ILog log)
        {
            this.settings = settings;
            this.log = log = log.ForContext<ApplicationIdentitiesResolver>();

            sdTopologiesProvider = new SdTopologiesProvider(settings.ServiceDiscoveryClient, settings.EnvironmentsWhitelist, log);
            identities = new ConcurrentDictionary<(string environment, string service), IVostokApplicationIdentity>();
            parentEnvironment = new ConcurrentDictionary<string, string>();
        }

        public void Warmup()
        {
            if (!state.TryIncreaseTo(Running))
                throw new InvalidOperationException("Already warmed up.");

            UpdateCacheTaskIterationAsync().GetAwaiter().GetResult();

            updateCacheTask = Task.Run(UpdateCacheTask);
        }

        [NotNull]
        public IVostokApplicationIdentity Resolve([CanBeNull] string environment, [CanBeNull] string service)
        {
            if (state == NotStarted)
                throw new InvalidOperationException("Warmup should be called first.");

            var originEnvironment = environment = environment ?? TopologyKey.DefaultEnvironment;
            service = service ?? "unknown";

            var (realService, suffix) = NamesHelper.GetRealServiceName(service);
            IVostokApplicationIdentity result = null;

            for (var deep = 0; deep < 10 && environment != null; deep++)
            {
                if (identities.TryGetValue((environment, realService), out result))
                {
                    originEnvironment = environment;
                    break;
                }

                if (parentEnvironment.TryGetValue(environment, out var p))
                    environment = p;
            }

            return result != null
                ? NamesHelper.AddServiceNameSuffix(result, suffix)
                : new ApplicationIdentity(
                    NamesHelper.GenerateProjectName(service, settings.ProjectsWhitelist?.Invoke()),
                    null,
                    $"{originEnvironment}-{settings.UnknownEnvironmentSuffix}",
                    service,
                    "instance");
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
                await UpdateCacheTaskIterationAsync().ConfigureAwait(false);
                await updateCacheSignal.WaitAsync().WaitAsync(settings.IterationPeriod).ConfigureAwait(false);
                updateCacheSignal.Reset();
            }
        }

        private async Task UpdateCacheTaskIterationAsync()
        {
            try
            {
                await UpdateIdentitiesAsync().ConfigureAwait(false);
                await UpdateEnvironmentsAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                log.Error(exception, "Failed cache update iteration.");
            }
        }

        private async Task UpdateIdentitiesAsync()
        {
            var sw = Stopwatch.StartNew();
            var sdTopologies = await sdTopologiesProvider.GetAsync().ConfigureAwait(false);
            log.Info("Resolved {Count} topologies from ServiceDiscovery in {Elapsed}.", sdTopologies.Count, sw.Elapsed.ToPrettyString());

            foreach (var topology in sdTopologies)
            {
                var replicas = await settings.ServiceDiscoveryClient.GetAllReplicasAsync(topology.Key.Environment, topology.Key.Service).ConfigureAwait(false);

                if (!replicas.Any())
                    continue;

                foreach (var r in replicas)
                {
                    var replica = await settings.ServiceDiscoveryClient.GetReplicaAsync(topology.Key.Environment, topology.Key.Service, r).ConfigureAwait(false);
                    if (replica?.Properties == null)
                        continue;

                    replica.Properties.TryGetValue(WellKnownApplicationIdentityProperties.Project, out var project);
                    replica.Properties.TryGetValue(WellKnownApplicationIdentityProperties.Subproject, out var subproject);
                    replica.Properties.TryGetValue(WellKnownApplicationIdentityProperties.Environment, out var environment);
                    replica.Properties.TryGetValue(WellKnownApplicationIdentityProperties.Application, out var application);
                    replica.Properties.TryGetValue(WellKnownApplicationIdentityProperties.Instance, out var instance);

                    if (project != null && environment != null && application != null && instance != null)
                    {
                        identities[(topology.Key.Environment, topology.Key.Service)] = new ApplicationIdentity(project, subproject, environment, application, instance);
                        break;
                    }
                }

                if (!identities.ContainsKey((topology.Key.Environment, topology.Key.Service)))
                    identities[(topology.Key.Environment, topology.Key.Service)] = null;
            }

            log.Info("Resolved {Count} identities in {Elapsed}.", identities.Count, sw.Elapsed.ToPrettyString());
        }

        private async Task UpdateEnvironmentsAsync()
        {
            var sw = Stopwatch.StartNew();
            var environments = await settings.ServiceDiscoveryClient.GetAllEnvironmentsAsync().ConfigureAwait(false);

            foreach (var e in environments)
            {
                var environment = await settings.ServiceDiscoveryClient.GetEnvironmentAsync(e).ConfigureAwait(false);

                if (environment != null)
                    parentEnvironment[environment.Environment] = environment.ParentEnvironment;
            }

            log.Info("Resolved {Count} environments in {Elapsed}.", environments.Count, sw.Elapsed.ToPrettyString());
        }
    }
}