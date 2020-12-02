using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOpsMigrator.Migrators
{
    public interface IMigrator
    {
        Task ExecuteAsync(CancellationToken token);
        void InitializeEndpoints();
        Task<int?> GetPlannedCount(CancellationToken token);

        event StatusChangedEventHandler StatusChanged;
    }
    public enum EntityType
    {
        WorkItem,
        AreaPath,
        Iteration
    }
}
