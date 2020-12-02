using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;

namespace AzureDevOpsMigrator.Migrators.Transformation
{
    public class BaseTransformation : ITransformation
    {
        public virtual string Display => throw new NotImplementedException();

        public virtual bool Apply(WorkItem sourceWit, WorkItem targetWit)
        {
            throw new NotImplementedException();
        }
    }
}
