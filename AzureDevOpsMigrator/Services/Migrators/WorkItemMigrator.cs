using AzureDevOpsMigrator.EndpointServices;
using AzureDevOpsMigrator.Migrators.Transformation;
using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Services;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;

namespace AzureDevOpsMigrator.Migrators
{
    public interface IWorkItemMigrator : IMigrator
    {
        Task<WorkItemField[]> CompareFields(CancellationToken token);
        Task MigrateFields(string projectName, WorkItemField[] fieldsDiff, CancellationToken token);
        Task SetupMigrationField(CancellationToken token);
    }
    public class WorkItemMigrator : BaseMigrator<IWorkItemMigrator>, IWorkItemMigrator
    {
        private int _processedCount = 0;
        private int _totalCount = 0;
        private TeamProject _sourceProject { get; set; }
        private TeamProject _targetProject { get; set; }
        public WorkItemMigrator(MigrationConfig config, ILogger<IWorkItemMigrator> log, IEndpointServiceResolver endpointResolver): 
            base(config, log, endpointResolver) { }

        public async Task ExecuteAsync(CancellationToken token)
        {
            _sourceProjectFields = await _sourceEndpoint.GetFields(_config.SourceEndpointConfig.ProjectName, token);
            _sourceProject = await _sourceEndpoint.GetProject(_config.SourceEndpointConfig.ProjectName);
            _targetProject = await _targetEndpoint.GetProject(_config.TargetEndpointConfig.ProjectName);

            var result = new Queue<int>(await _sourceEndpoint.GetIdsByWiql(_config.SourceQuery, token));
            _totalCount = result.Count;
            _processedCount = 0;
            ConcurrentBag<Exception> haltedExceptions = new ConcurrentBag<Exception>();
            var tasks = new ConcurrentDictionary<int, Task>();

            do
            {
                token.ThrowIfCancellationRequested();
                if (tasks.Count() < _config.MaxDegreeOfParallelism)
                {
                    // Pull a record off the queue for processing
                    var currentId = result.Dequeue();

                    // Add that processing task to the tasks list
                    tasks.TryAdd(currentId, Task.Run(async () =>
                    {
                        try
                        {
                            var workItem = await _processWorkItem(currentId, token);

                            _processedCount += 1;
                            if (!tasks.TryRemove(currentId, out Task t))
                            {
                                throw new WorkItemProcessingException("Work item migrator failed to manage task parallelism properly.", currentId, exceptionClass: ExceptionClass.Minor);
                            }
                        }
                        catch (WorkItemProcessingException ex)
                        {
                            if (ex.ExceptionClass == ExceptionClass.Minor)
                            {
                                _log.LogWarning(ex.Message);
                                _log.LogError(ex.ToString());
                            }
                            else
                            {
                                _log.LogInformation($"Failed to process work item {currentId}.");
                                _log.LogError(ex.ToString());
                                haltedExceptions.Add(new WorkItemProcessingException("Failed to process work item.", currentId, ex));
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.LogInformation($"Failed to process work item {currentId}.");
                            _log.LogError(ex.ToString());
                            haltedExceptions.Add(new WorkItemProcessingException("Failed to process work item.", currentId, ex));
                        }
                    }));
                }
                await Task.Delay(100); // Artificial throttling to not hammer CPU cycles

            } while (result.Count > 0 && haltedExceptions.Count == 0);

            if (haltedExceptions.Count > 0)
            {
                _log.LogWarning("Received halt command on work item processing. Gracefully stopping processing by allowing remaining work item processing tasks to finish or fail.");
            }

            await Task.WhenAll(tasks.Values);

            if (haltedExceptions.Count > 0)
            {
                throw new MigrationException("Work item processing failed.");
            }
        }

        private string[] _fieldsNotToCopy = new string[]
        {
            "System.Id",
            "System.AreaId",
            "System.TeamProject",
            "System.Rev",
            "System.IterationId",
            "System.CommentCount"
        };
        private IEnumerable<WorkItemField> _sourceProjectFields;

        public async Task SetupMigrationField(CancellationToken token)
        {
            var existing = await _targetEndpoint.GetField("Custom.MigrationState", token);
            if (existing == null)
            {
                await _targetEndpoint.CreateField(new WorkItemField()
                {
                    ReferenceName = $"Custom.{_config.MigrationStateField}",
                    Name = $"{_config.MigrationStateField}",
                    Description = "Used to store migration state of the work item between two instances of Azure DevOps",
                    IsQueryable = true,
                    CanSortBy = true,
                    Type = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.FieldType.String,
                    Usage = FieldUsage.WorkItem,
                    SupportedOperations = new List<WorkItemFieldOperation>()
                    {
                        new WorkItemFieldOperation()
                        {
                            Name = "=",
                            ReferenceName = "SupportedOperations.Equals"
                        },
                        new WorkItemFieldOperation()
                        {
                            Name = "Contains",
                            ReferenceName = "SupportedOperations.Contains"
                        },
                        new WorkItemFieldOperation()
                        {
                            Name = "Does Not Contain",
                            ReferenceName = "SupportedOperations.NotContains"
                        }
                    }
                }, token);
                _log.LogInformation("Migration state field created.");
            }
            else
            {
                _log.LogInformation("Migration state field exists.");
            }
        }

        private (WorkItem Target, int TransformationCount) _buildWorkItemRevision(WorkItem source, WorkItem target)
        {
            target.Rev = source.Rev;
            foreach (var field in source.Fields.Where(srcField => !_fieldsNotToCopy.Contains(srcField.Key)))
            {
                var projectField = _sourceProjectFields.FirstOrDefault(projectField => projectField.ReferenceName.Equals(field.Key));
                if (projectField != null && !projectField.ReadOnly)
                {
                    if (!target.Fields.ContainsKey(field.Key))
                    {
                        target.Fields.Add(field);
                    }
                    else
                    {
                        target.Fields[field.Key] = field.Value;
                    }
                }
            }

            var splitAreaPath = target.Fields["System.AreaPath"].ToString().Split('\\').Skip(1);
            var splitIterationPath = target.Fields["System.IterationPath"].ToString().Split('\\').Skip(1);
            target.Fields["System.AreaPath"] = _config.TargetEndpointConfig.ProjectName + (splitAreaPath.Count() > 0 ? "\\" + string.Join('\\', splitAreaPath) : "");
            target.Fields["System.IterationPath"] = _config.TargetEndpointConfig.ProjectName + (splitIterationPath.Count() > 0 ? "\\"+ string.Join('\\', splitIterationPath) : "");

            var url = source.Url.Contains("/revisions/") ? source.Url.Substring(0, source.Url.Substring(0, source.Url.LastIndexOf('/')).LastIndexOf('/')) : source.Url;
            if (target.Fields.ContainsKey($"Custom.{_config.MigrationStateField}"))
            {
                target.Fields[$"Custom.{_config.MigrationStateField}"] = url;
            }
            else
            {
                target.Fields.Add($"Custom.{_config.MigrationStateField}", url);
            }

            var transformsCount = 0;
            foreach (ITransformation transform in _config.Transformations)
            {
                if (transform.Apply(source, target))
                {
                    transformsCount += 1;
                }
            }

            return (target, transformsCount);
        }

        private async Task<WorkItem> _processWorkItem(int currentId, CancellationToken token)
        {
            var currentRevision = await _sourceEndpoint.GetWorkItem(_config.SourceEndpointConfig.ProjectName, currentId, token, WorkItemExpand.All);

            // Check if the current revision is found in target project
            WorkItem existing = await _targetEndpoint.GetWorkItemByMigrationState(_config.TargetEndpointConfig.ProjectName, _config.MigrationStateField, currentRevision.Url, token);

            if (existing?.Rev < currentRevision.Rev)
            {
                // It needs updating in the target
                var result = await _fastForward(currentRevision, existing, existing.Rev.Value, token, existing.Id);
                ReportCount(1, SyncState.Update, EntityType.WorkItem);
                return result;
            }
            else if (existing?.Rev == currentRevision.Rev)
            {
                // It matches in the target
                _log.LogInformation($"Work item #{currentId} already synced to work item #{existing.Id}:{existing.Rev}.");
                ReportCount(1, SyncState.Matching, EntityType.WorkItem);
                return existing;
            }
            else if (existing?.Rev > currentRevision.Rev)
            {
                throw new WorkItemProcessingException("Work item exists in target, but the target has been modified since the last migration.", currentId, exceptionClass: ExceptionClass.Major);
            }
            else
            {
                // It's a straight create operation
                var result = await _fastForward(currentRevision, null, 0, token);
                ReportCount(1, SyncState.Create, EntityType.WorkItem);
                return result;
            }
        }

        private async Task _migrateAttachments(WorkItem source, WorkItem target, CancellationToken token)
        {
            if (source.Relations != null && source.Relations.Count > 0)
            {
                foreach (var relation in source.Relations)
                {
                    target.Relations = target.Relations ?? new List<WorkItemRelation>();

                    if (relation.Rel.StartsWith("System.LinkTypes") ||
                        relation.Rel.StartsWith("Microsoft.VSTS.Common", StringComparison.OrdinalIgnoreCase))
                    {

                        var relatedWorkItem = await _targetEndpoint.GetWorkItemByMigrationState(_targetProject.Name, _config.MigrationStateField, relation.Url, token);

                        if (relatedWorkItem == null)
                        {
                            _log.LogWarning($"\t\t Relation of type [{relation.Rel}] linked to {relation.Url} not found in target.");
                        }
                        else
                        {
                            var existingLinkInTarget = target.Relations.FirstOrDefault(t =>
                                t.Rel == relation.Rel &&
                                t.Url.Equals(relatedWorkItem.Url, StringComparison.OrdinalIgnoreCase));
                            if (existingLinkInTarget != null)
                            {

                            }
                            else
                            {
                                target.Relations.Add(new WorkItemRelation()
                                {
                                    Url = relatedWorkItem.Url,
                                    Rel = relation.Rel,
                                    Title = relation.Title,
                                    Attributes = relation.Attributes
                                });
                            }
                        }
                    }
                    else if (relation.Rel.Equals("AttachedFile", StringComparison.OrdinalIgnoreCase))
                    {
                        var fileName = relation.Attributes["name"].ToString();
                        var id = Guid.Parse(relation.Url.Split('/').Last());

                        var existingAttachmentInTarget = target.Relations.FirstOrDefault(t =>
                            t.Rel == "AttachedFile" &&
                            t.Attributes["name"].ToString().Equals(relation.Attributes["name"].ToString(), StringComparison.OrdinalIgnoreCase));
                        if (existingAttachmentInTarget != null)
                        {

                        }
                        else
                        {
                            var stream = await _sourceEndpoint.GetAttachmentStream(id, fileName, token);
                            var attachmentRef = await _targetEndpoint.CreateAttachment(_config.TargetEndpointConfig.ProjectName, stream, fileName, "", token, uploadType: "Chunked");
                            IDictionary<string, object> attrs = new Dictionary<string, object>();

                            if (relation.Attributes.ContainsKey("name"))
                            {
                                attrs.Add("name", relation.Attributes["name"]);
                            }

                            if (relation.Attributes.ContainsKey("comment"))
                            {
                                attrs.Add("comment", relation.Attributes["comment"]);
                            }

                            _log.LogInformation($"Attachment {fileName} on Work item #{source.Id.Value}:{source.Rev} has been migrated.");
                            target.Relations.Add(new WorkItemRelation()
                            {
                                Rel = "AttachedFile",
                                Url = attachmentRef.Url,
                                Attributes = relation.Attributes
                            });
                        }
                    }
                    else if (relation.Rel == "ArtifactLink" && relation.Url.StartsWith("vstfs:///Git/Commit/"))
                    {
                        var tempRelation = new WorkItemRelation()
                        {
                            Rel = relation.Rel,
                            Title = relation.Title,
                            Attributes = new Dictionary<string, object>(),
                            Url = relation.Url
                        };
                        relation.Attributes.Copy(tempRelation.Attributes);

                        var split = relation.Url.Replace("vstfs:///Git/Commit/", "").Split("%2F", StringSplitOptions.RemoveEmptyEntries);
                        var projectId = Guid.Parse(split[0]);
                        var repoId = Guid.Parse(split[1]);
                        var commitId = split[2];

                        var sourceRepo = await _sourceEndpoint.GetGitRepository(projectId, repoId, token);
                        if (sourceRepo != null)
                        {
                            var targetRepo = await _targetEndpoint.GetGitRepository(_targetProject.Id, sourceRepo.Name, token);
                            if (targetRepo != null)
                            {
                                tempRelation.Url = tempRelation.Url
                                    .Replace(projectId.ToString(), _targetProject.Id.ToString())
                                    .Replace(repoId.ToString(), targetRepo.Id.ToString());
                            }
                        }

                        var existingLinkInTarget = target.Relations.FirstOrDefault(t =>
                            t.Rel == tempRelation.Rel &&
                            t.Url.Equals(tempRelation.Url, StringComparison.OrdinalIgnoreCase));
                        if (existingLinkInTarget != null)
                        {

                        }
                        else
                        {
                            target.Relations.Add(tempRelation);
                        }
                    }
                    else
                    {

                        var existingLinkInTarget = target.Relations.FirstOrDefault(t =>
                            t.Rel == relation.Rel &&
                            t.Url.Equals(relation.Url, StringComparison.OrdinalIgnoreCase));
                        if (existingLinkInTarget != null)
                        {

                        }
                        else
                        {
                            target.Relations.Add(relation);
                        }
                    }
                }
            }
        }

        private async Task<WorkItem> _fastForward(WorkItem sourceWorkItem, WorkItem currentTarget, int currentRevision, CancellationToken token, int? currentId = null)
        {
            WorkItem updatedTarget = currentTarget;
            var history = from rev in await _sourceEndpoint.GetWorkItemRevisions(
                            sourceWorkItem.Fields["System.TeamProject"].ToString(),
                            sourceWorkItem.Id.Value, token)
                          where !_config.MigrateHistory || rev.Rev > currentRevision
                          orderby rev.Rev ascending
                          select rev;
            var transformCount = 0;
            if (_config.MigrateHistory)
            {
                foreach (var revision in history)
                {
                    (updatedTarget, transformCount) = _buildWorkItemRevision(revision, updatedTarget ?? new WorkItem()
                    {
                        Id = currentId,
                        Rev = revision.Rev
                    });

                    if (_config.MigrateAttachments)
                    {
                        await _migrateAttachments(revision, updatedTarget, token);
                    }

                    if (currentId == null && revision.Rev == 1)
                    {
                        updatedTarget = await _targetEndpoint.CreateWorkItem(_config.TargetEndpointConfig.ProjectName, updatedTarget, token, WorkItemExpand.All);
                        currentId = updatedTarget.Id;
                    }
                    else
                    {
                        updatedTarget = await _targetEndpoint.UpdateWorkItem(_config.TargetEndpointConfig.ProjectName, updatedTarget, token, WorkItemExpand.All);
                    }
                    _log.LogInformation($"Work item #{sourceWorkItem.Id.Value}:{revision.Rev} has been synced to work item #{updatedTarget.Id}:{updatedTarget.Rev} with {transformCount} transformations.");
                }
            }
            else
            {
                var mostRecent = history.OrderBy(h => h.Rev).Last();
                (updatedTarget, transformCount) = _buildWorkItemRevision(mostRecent, new WorkItem()
                {
                    Rev = mostRecent.Rev
                });

                if (_config.MigrateAttachments)
                {
                    await _migrateAttachments(mostRecent, updatedTarget, token);
                }

                updatedTarget = await _targetEndpoint.CreateWorkItem(_config.TargetEndpointConfig.ProjectName, updatedTarget, token, WorkItemExpand.All);
                _log.LogInformation($"Work item #{sourceWorkItem.Id.Value}:{sourceWorkItem.Rev} has been synced to work item #{updatedTarget.Id}:{updatedTarget.Rev} with {transformCount} transformations.");
            }
            return updatedTarget;
        }

        public async Task<int?> GetPlannedCount(CancellationToken token) => !_config.Execution.WorkItemsMigratorEnabled ? 
            await Task.FromResult<int?>(null) : (await _sourceEndpoint.GetIdsByWiql(_config.SourceQuery, token)).Count();

        public async Task<WorkItemField[]> CompareFields(CancellationToken token)
        {
            var sourceFields = await _sourceEndpoint.GetFields(_config.SourceEndpointConfig.ProjectName, token);
            var targetFields = await _targetEndpoint.GetFields(_config.TargetEndpointConfig.ProjectName, token);
            return sourceFields.Where(src => !targetFields.Any(tgt => 
                tgt.Name.Equals(src.Name, StringComparison.OrdinalIgnoreCase))).ToArray();
        }

        public async Task MigrateFields(string projectName, WorkItemField[] fields, CancellationToken token)
        {
            foreach (var field in fields)
            {
                // Check for each individual field directly.. there is some scenario where fields in source are not found in target
                // yet when you attempt to create them you find they are indeed already created. -- Andrew K
                var existing = await _targetEndpoint.GetField(field.ReferenceName, token);
                if (existing == null)
                {
                    if (field.IsPicklist)
                    {
                        var picklist = await _sourceEndpoint.GetPickList(field.PicklistId.Value, token);
                        PickList targetList = await _targetEndpoint.CreatePickList(picklist, token);
                        field.PicklistId = targetList.Id;
                        _log.LogInformation($"Created picklist [{picklist.Name}]");
                    }
                    await _targetEndpoint.CreateField(field, token);
                    _log.LogInformation($"Created field [{field.ReferenceName}]");
                }
            }
        }
    }

    public enum ExceptionClass
    {
        Minor,
        Major
    }
    public class WorkItemProcessingException : Exception
    {
        public ExceptionClass ExceptionClass { get; private set; }
        public int SourceWorkItemId { get; private set; }
        public WorkItemProcessingException(string message, int sourceWorkItemId, Exception innerException = null, ExceptionClass exceptionClass = ExceptionClass.Major) :
            base(message, innerException)
        {
            SourceWorkItemId = sourceWorkItemId;
            ExceptionClass = exceptionClass;
        }
    }
    public class MigrationException : Exception
    {
        public MigrationException(string message,  Exception innerException = null) :
            base(message, innerException)
        {
        }
    }
}
