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
    }
}
