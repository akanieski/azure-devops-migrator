using Newtonsoft.Json;
using System.Collections.ObjectModel;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System;
using AzureDevOpsMigrator.Migrators;

namespace AzureDevOpsMigrator.Models
{
    public class MigrationConfig
    {
        public string Name { get; set; }
        public EndpointConfig SourceEndpointConfig { get; set; }
        public EndpointConfig TargetEndpointConfig { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, ItemTypeNameHandling = TypeNameHandling.Objects, TypeNameHandling = TypeNameHandling.Objects)]
        public ObservableCollection<object> Transformations { get; set; } = new ObservableCollection<object>();
        public string SourceQuery { get; set; } = "";
        public int MaxDegreeOfParallelism { get; set; } = 3;
        public bool FixHyperlinks { get; set; } = true;
        public ExecutionOptions Execution { get; set; } = new ExecutionOptions();
        public bool IncludeWorkItemHistory { get; set; } = false;
        public PreMigrationChecks PreMigrationChecks { get; set; } = new PreMigrationChecks();
        public bool CreateFieldsInTarget { get; set; } = true;
        public string MigrationStateField { get; set; } = "MigrationState";
        public bool MigrateHistory { get; set; } = true;
        public bool MigrateAttachments { get; set; } = true;

        public ObservableCollection<GitRepo> GitRepoMappings { get; set; } = new ObservableCollection<GitRepo>();
    }
    public class GitRepo
    {
        public Guid SourceRepoId { get; set; }
        public string SourceRepoName { get; set; }
        public string TargetRepoName { get; set; }
        public Guid? TargetRepoId { get; set; }
        public bool Enabled { get; set; }
        public ObservableCollection<GitRepoMapping> GitRepoMappings { get; set; } = new ObservableCollection<GitRepoMapping>();

    }
    public class GitRepoMapping
    {
        public Guid SourceRepoId { get; set; }
        public string SourceRepoName { get; set; }
        public Guid? TargetRepoId { get; set; }

        public GitRef Reference { get; set; }
        public bool Enabled { get; set; }

        [JsonIgnore]
        public GitMappingState State { get; set; } = GitMappingState.Unknown;
    }

    public class PreMigrationChecks
    {
        public bool CompareFields { get; set; } = true;
    }
    public class ExecutionOptions
    {
        public bool WorkItemsMigratorEnabled { get; set; } = true;
        public bool AreaPathMigratorEnabled { get; set; } = true;
        public bool IterationsMigratorEnabled { get; set; } = true;

    }
    public class EndpointConfig
    {
        public string ProjectName { get; set; }
        public string EndpointUri { get; set; }
        public string PersonalAccessToken { get; set; }
        public string EndpointType { get; set; } = "RestEndpointService";
    }
}
