using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Services;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsMigrator.Migrators
{
    public interface IIterationMigrator : IMigrator
    {
    }

    public class IterationMigrator : ClassificationNodeMigrator<IIterationMigrator>, IIterationMigrator
    {
        public IterationMigrator(MigrationConfig config, ILogger<IIterationMigrator> log, IEndpointServiceResolver endpointResolver) :
            base(config, log, endpointResolver)
        { }
        protected override TreeStructureGroup _nodesType => TreeStructureGroup.Iterations;
        protected override TreeNodeStructureType _nodeType => TreeNodeStructureType.Iteration;

        protected override bool IsFilterMatch(string path)
        {
            if (_iterationFilter != _config.IterationFilter)
            {
                _iterationFilter = _config.IterationFilter;
                _iterationFilterMatcher = new System.Text.RegularExpressions.Regex(_config.IterationFilter);
            }
            return string.IsNullOrEmpty(_config.IterationFilter) ? true : _iterationFilterMatcher.IsMatch(path);
        }

        private string _iterationFilter = "";
        private System.Text.RegularExpressions.Regex _iterationFilterMatcher;
    }
}
