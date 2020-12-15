using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Services;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureDevOpsMigrator.Migrators
{
    public abstract partial class ClassificationNodeMigrator<T> : BaseMigrator<T>, IMigrator
    {
        protected virtual TreeNodeStructureType _nodeType { get; }
        protected virtual TreeStructureGroup _nodesType { get; }
        private int _processedCount = 0;
        private int _totalCount = 0;
        

        public ClassificationNodeMigrator(MigrationConfig config, ILogger<T> log, IEndpointServiceResolver endpointResolver) : 
            base(config, log, endpointResolver) { }

        public async Task ExecuteAsync(System.Threading.CancellationToken token)
        {
            _log.LogInformation($"Scanning for {Enum.GetName(typeof(TreeNodeStructureType),_nodeType)} nodes.");

            IEnumerable<WorkItemClassificationNode> sourcePaths = null, targetPaths = null;
            var sourceFetchTask = Task.Run(async () => 
            sourcePaths = await _sourceEndpoint.GetClassificationNodes(_config.SourceEndpointConfig.ProjectName, _nodesType, token));

            var targetFetchTask = Task.Run(async () => 
            targetPaths = await _targetEndpoint.GetClassificationNodes(_config.TargetEndpointConfig.ProjectName, _nodesType, token));

            await Task.WhenAll(sourceFetchTask, targetFetchTask);

            _log.LogInformation($"Source and Target nodes have been collected. Flattening for easier processing");

            var flattenedSourcePaths = new List<(string Path, WorkItemClassificationNode Node)>();
            FlattenNodes(sourcePaths, ref flattenedSourcePaths);
            flattenedSourcePaths = flattenedSourcePaths.OrderBy(s => s.Path).Select(p => (string.Join('\\', p.Path.Split(@"\").Skip(3)), p.Node)).ToList();

            var flattenedTargetPaths = new List<(string Path, WorkItemClassificationNode Node)>();
            FlattenNodes(targetPaths, ref flattenedTargetPaths);
            flattenedTargetPaths = flattenedTargetPaths.OrderBy(s => s.Path).Select(p => (string.Join('\\', p.Path.Split(@"\").Skip(3)), p.Node)).ToList();

            _log.LogInformation($"{flattenedSourcePaths.Count} source nodes flattened");
            _log.LogInformation($"{flattenedTargetPaths.Count} target nodes flattened");

            // Now that we have a nice flat list of nodes let's get a list of nodes that need to be created

            var upsertedNodes = new List<(SyncState State, WorkItemClassificationNode Node)>();
            foreach (var sourceNode in flattenedSourcePaths)
            {
                token.ThrowIfCancellationRequested();


                // look up modified path in target
                var existing = flattenedTargetPaths
                    .Select(x => new { Path = x.Path, Node = x.Node})
                    .FirstOrDefault(p => p.Path.Equals(sourceNode.Path, System.StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    // Create a new area path in target
                    var updatedPath = @"\" + _config.TargetEndpointConfig.ProjectName + (_nodeType == TreeNodeStructureType.Area ? @"\Area\" : @"\Iteration\") + sourceNode.Path;
                    upsertedNodes.Add((SyncState.Create, new WorkItemClassificationNode()
                    {
                        Path = updatedPath,
                        Name = sourceNode.Path.Split(@"\").Last(),
                        StructureType = sourceNode.Node.StructureType,
                        Attributes = sourceNode.Node.Attributes,
                        HasChildren = sourceNode.Node.HasChildren
                    }));
                } 
                else
                {
                    bool changes = false;
                    // Let's see if attributes match if not then we update
                    if (sourceNode.Node.Attributes != null)
                    {
                        foreach (var sourcePair in sourceNode.Node.Attributes)
                        {
                            if (existing.Node.Attributes != null)
                            {
                                if (existing.Node.Attributes.ContainsKey(sourcePair.Key) && !existing.Node.Attributes[sourcePair.Key].Equals(sourcePair.Value))
                                {
                                    changes = true;
                                    existing.Node.Attributes[sourcePair.Key] = sourcePair.Value;
                                }
                                else if (!existing.Node.Attributes.ContainsKey(sourcePair.Key))
                                {
                                    changes = true;
                                    existing.Node.Attributes.Add(sourcePair);
                                }
                            }
                        }
                    }
                    upsertedNodes.Add((changes ? SyncState.Update : SyncState.Matching, existing.Node));
                }
            }

            foreach (var node in upsertedNodes)
            {
                token.ThrowIfCancellationRequested();
                EntityType type = _nodeType == TreeNodeStructureType.Area ? EntityType.AreaPath : EntityType.Iteration;
                switch (node.State)
                {
                    case SyncState.Create:
                        await _targetEndpoint.UpsertClassificationNodesAsync(node.Node, _config.TargetEndpointConfig.ProjectName, _nodesType, token);
                        _log.LogInformation($"Source: {node.Node.Path} ... Created");
                        ReportCount(1, node.State, type);
                        break;
                    case SyncState.Update:
                        await _targetEndpoint.UpsertClassificationNodesAsync(node.Node, _config.TargetEndpointConfig.ProjectName, _nodesType, token);
                        _log.LogInformation($"Source: {node.Node.Path} ... Updated");
                        ReportCount(1, node.State, type);
                        break;
                    case SyncState.Matching:
                        _log.LogInformation($"Source: {node.Node.Path} ... In Sync");
                        ReportCount(1, node.State, type);
                        break;
                }
            }

        }

        private void FlattenNodes(IEnumerable<WorkItemClassificationNode> nodes, ref List<(string Path, WorkItemClassificationNode Node)> paths)
        {
            foreach (var node in nodes)
            {
                paths.Add((node.Path, node));
                if (node.Children?.Count() > 0)
                {
                    FlattenNodes(node.Children, ref paths);
                }
            }
        }

        public async Task<int?> GetPlannedCount(CancellationToken token) {
            if ((_nodeType == TreeNodeStructureType.Area && !_config.Execution.AreaPathMigratorEnabled) ||
                (_nodeType == TreeNodeStructureType.Iteration && !_config.Execution.IterationsMigratorEnabled))
            {
                return null;
            }
            var paths = new List<(string Path, WorkItemClassificationNode Node)>();
            FlattenNodes(
                await _sourceEndpoint.GetClassificationNodes(_config.SourceEndpointConfig.ProjectName, _nodesType, token), 
                ref paths);
            return paths.Count;
        }
    }
}
