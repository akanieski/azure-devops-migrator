using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsMigrator.Migrators.Transformation
{
    public interface ITransformation
    {
        string Display { get; }
        bool Apply(WorkItem sourceWit, WorkItem targetWit);
    }
}
