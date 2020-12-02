using AzureDevOpsMigrator.EndpointServices;
using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Services;
using Microsoft.Extensions.Logging;
using System;

namespace AzureDevOpsMigrator.Migrators
{
    public class BaseMigrator<T>
    {
        protected ILogger<T> _log;
        protected MigrationConfig _config;
        protected IEndpointService _sourceEndpoint;
        protected IEndpointService _targetEndpoint;
        private IEndpointServiceResolver _endpointResolver;

        public event StatusChangedEventHandler StatusChanged;

        public BaseMigrator(MigrationConfig config, ILogger<T> log, IEndpointServiceResolver endpointResolver)
        {
            _log = log;
            _config = config;
            _endpointResolver = endpointResolver;
            InitializeEndpoints();
        }
        internal void ReportCount(int increment, SyncState state, EntityType type) => StatusChanged?.Invoke(this, (increment, state, type));
        public void InitializeEndpoints()
        {
            _sourceEndpoint = _sourceEndpoint ?? _endpointResolver.Resolve(_config.SourceEndpointConfig);
            _targetEndpoint = _targetEndpoint ?? _endpointResolver.Resolve(_config.TargetEndpointConfig);
        }
    }
}
