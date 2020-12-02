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
    }
}
