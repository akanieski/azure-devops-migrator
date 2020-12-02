using AzureDevOpsMigrator.EndpointServices;
using AzureDevOpsMigrator.Models;
using System;
using System.Collections.Generic;

namespace AzureDevOpsMigrator.Services
{
    public interface IEndpointServiceResolver
    {
        IEndpointService Resolve(EndpointConfig config);
    }
    public class EndpointServiceResolver : IEndpointServiceResolver
    {
        private IServiceProvider _serviceProvider;
        private Dictionary<string, IEndpointService> _cache = new Dictionary<string, IEndpointService>();
        public EndpointServiceResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IEndpointService Resolve(EndpointConfig config)
        {
            var key = config.EndpointUri + config.PersonalAccessToken;
            switch (config.EndpointType)
            {
                case "RestEndpointService":
                case "AzureDevOpsMigrator.EndpointServices.RestEndpointService":
                    if (_cache.ContainsKey(key))
                    {
                        return _cache[key];
                    }
                    else
                    {
                        _cache.Add(key, _serviceProvider.GetService(typeof(RestEndpointService)) as IEndpointService);
                        _cache[key].Initialize(config.EndpointUri, config.PersonalAccessToken);
                        return _cache[key];
                    }
                default:
                    throw new Exception($"Unknown endpoint type {config.EndpointType}.");
            }
        }
    }
}
