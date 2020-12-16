using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Services;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsMigrator.Migrators
{
    public interface IAreaPathMigrator : IMigrator
    {
    }
    public class AreaPathMigrator : ClassificationNodeMigrator<IAreaPathMigrator>, IAreaPathMigrator
    {
        public AreaPathMigrator(MigrationConfig config, ILogger<IAreaPathMigrator> log, IEndpointServiceResolver endpointResolver) :
            base(config, log, endpointResolver)
        { }
        protected override TreeStructureGroup _nodesType => TreeStructureGroup.Areas;
        protected override TreeNodeStructureType _nodeType => TreeNodeStructureType.Area;

        protected override bool IsFilterMatch(string path)
        {
            if (_areaFilter != _config.AreaPathFilter)
            {
                _areaFilter = _config.AreaPathFilter;
                _areaFilterMatcher = new System.Text.RegularExpressions.Regex(_config.AreaPathFilter);
            }
            return string.IsNullOrEmpty(_config.AreaPathFilter) ? true : _areaFilterMatcher.IsMatch(path);
        }

        private string _areaFilter = "";
        private System.Text.RegularExpressions.Regex _areaFilterMatcher;
    }
}
