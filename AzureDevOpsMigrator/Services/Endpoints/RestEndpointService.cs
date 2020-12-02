using AzureDevOpsMigrator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System.IO;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using WITComment = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.Comment;

namespace AzureDevOpsMigrator.EndpointServices
{
    public class RestEndpointService: IEndpointService
    {
        private ILogger<RestEndpointService> _log;
        private string _endpointUrl;
        private string _endpointPat;
        private GitHttpClient _gitClient;
        private TfvcHttpClient _tfsClient;
        private WorkItemTrackingHttpClient _witClient;
        private WorkItemTrackingProcessHttpClient _witProcessClient;
        private ProjectHttpClient _projectsClient;

        public RestEndpointService(ILogger<RestEndpointService> log)
        {
            _log = log;
        }

        public void Initialize(string endpointUrl, string pat)
        {
            if (_endpointUrl != endpointUrl || _endpointPat != pat)
            {
                _endpointUrl = endpointUrl;
                _endpointPat = pat;
                var creds = new VssBasicCredential(string.Empty, pat);
                _gitClient = new GitHttpClient(new Uri(endpointUrl), creds);
                _tfsClient = new TfvcHttpClient(new Uri(endpointUrl), creds);
                _witClient = new WorkItemTrackingHttpClient(new Uri(endpointUrl), creds);
                _projectsClient = new ProjectHttpClient(new Uri(endpointUrl), creds);
                _witProcessClient = new WorkItemTrackingProcessHttpClient(new Uri(endpointUrl), creds);

            }
        }
        private void CheckInitialized()
        {
            if (_projectsClient == null || _witClient == null)
                throw new InvalidOperationException("Rest endpoint not initialized.");
        }

        public async Task<GitRepository> CreateGitRepository(string projectName, string repoName, CancellationToken token) =>
            await _gitClient.CreateRepositoryAsync(new GitRepository()
            {
                Name = repoName,
                ProjectReference = new TeamProjectReference()
                {
                    Name = projectName
                }
            }, cancellationToken: token);
        public async Task<GitRepository> GetGitRepository(Guid repoId, CancellationToken token) =>
            await _gitClient.GetRepositoryAsync(repoId, cancellationToken: token);
        public async Task<GitRepository> GetGitRepository(string projectName, string repoName, CancellationToken token) =>
            (from repo in (await _gitClient.GetRepositoriesAsync(projectName, cancellationToken: token))
             where repo.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase)
             select repo).FirstOrDefault();
        public async Task<GitRepository> GetGitRepository(Guid projectId, string repoName, CancellationToken token) =>
            (from repo in (await _gitClient.GetRepositoriesAsync(projectId, cancellationToken: token))
             where repo.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase)
             select repo).FirstOrDefault();
        public async Task<GitRepository> GetGitRepository(Guid projectId, Guid repoId, CancellationToken token) =>
            await _gitClient.GetRepositoryAsync(projectId, repoId, cancellationToken: token);
        public async Task<IEnumerable<GitRepository>> GetGitRepositories(string projectName, CancellationToken token) =>
            await _gitClient.GetRepositoriesAsync(projectName, cancellationToken: token);
        public async Task<IEnumerable<GitRef>> GetGitRefs(string projectName, Guid repoId, CancellationToken token) =>
            await _gitClient.GetRefsAsync(projectName, repoId, cancellationToken: token);
        public async Task<GitRef> GetGitRef(string projectName, Guid repoId, string refName, CancellationToken token) =>
            (from gitRef in await _gitClient.GetRefsAsync(projectName, repoId, cancellationToken: token)
             where gitRef.Name.Equals(refName, StringComparison.OrdinalIgnoreCase)
             select gitRef).FirstOrDefault();
        public async Task<IEnumerable<GitCommitRef>> GetCommitRefs(Guid repoId, CancellationToken token, string branch = null) 
        {

            List<GitCommitRef> tempResults = new List<GitCommitRef>();
            List<GitCommitRef> finalSet = new List<GitCommitRef>();

            var criteria = new GitQueryCommitsCriteria();

            if (!string.IsNullOrEmpty(branch))
            {
                criteria.ItemVersion = new GitVersionDescriptor()
                {
                    Version = branch
                };
            }
            
            do
            {
                tempResults = await _gitClient.GetCommitsAsync(repoId, criteria, top: 100, skip: finalSet.Count, cancellationToken: token);
                if (tempResults.Count > 0)
                {
                    finalSet.AddRange(tempResults);
                }
            } while (tempResults.Count > 0);

            return finalSet;

        }

        public async Task UpsertClassificationNodesAsync(WorkItemClassificationNode node, string projectName, TreeStructureGroup group, CancellationToken token)
        {
            await _witClient.CreateOrUpdateClassificationNodeAsync(node, projectName, group, cancellationToken: token);
        }

        public async Task<IEnumerable<WorkItemClassificationNode>> GetAreaPaths(string projectName, CancellationToken token) =>
            (await _witClient.GetRootNodesAsync(projectName, depth: ushort.MaxValue))
            .Where(n => n.StructureType == TreeNodeStructureType.Area);

        public async Task<IEnumerable<WorkItemClassificationNode>> GetIterations(string projectName, CancellationToken token) =>
            (await _witClient.GetRootNodesAsync(projectName, depth: ushort.MaxValue, cancellationToken: token))
            .Where(n => n.StructureType == TreeNodeStructureType.Iteration);

        public async Task<IEnumerable<WorkItemClassificationNode>> GetClassificationNodes(string projectName, TreeStructureGroup type, CancellationToken token) =>
            (await _witClient.GetRootNodesAsync(projectName, depth: ushort.MaxValue, cancellationToken: token))
            .Where(n => n.StructureType == (type == TreeStructureGroup.Areas ? 
                TreeNodeStructureType.Area : TreeNodeStructureType.Iteration));

        public async Task<TeamProject> GetProject(string projectName) =>
            await _projectsClient.GetProject(projectName, null);


        public async Task<IEnumerable<TeamProjectReference>> GetAllProjects()
        {
            CheckInitialized();
            return await _projectsClient.GetProjects(top: ushort.MaxValue);
        }

        public async Task<IEnumerable<int>> GetIdsByWiql(string wiqlQuery, CancellationToken token)
        {
            CheckInitialized();
            var wiql = new Wiql();
            wiql.Query = $"SELECT * FROM WORKITEMS WHERE {wiqlQuery}";
            return (await _witClient.QueryByWiqlAsync(wiql, false, cancellationToken: token)).WorkItems.Select(i => i.Id);
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItems(IEnumerable<int> ids, CancellationToken token, WorkItemExpand expand = WorkItemExpand.None)
        {
            CheckInitialized();
            return await _witClient.GetWorkItemsAsync(ids: ids, expand: expand, cancellationToken: token);
        }

        public async Task<(int TotalCount, IEnumerable<WorkItem> Items)> GetWorkItemsAsync(string wiqlQuery, CancellationToken token, int top = 0, int skip = 0)
        {
            CheckInitialized();
            var wiql = new Wiql();
            wiql.Query = wiqlQuery;
            var result = (await _witClient.QueryByWiqlAsync(wiql, false, cancellationToken: token)).WorkItems;
            List<WorkItem> batch = await _witClient.GetWorkItemsAsync(
                    ids: result.Skip(skip).Take(top).Select(x => x.Id),
                    cancellationToken: token);
            return (result.Count(), batch);
        }

        public async Task<WorkItem> CreateWorkItem(string projectName, WorkItem workItem, CancellationToken token, 
            WorkItemExpand expand = WorkItemExpand.All)
        {
            CheckInitialized();

            var patch = new JsonPatchDocument();

            foreach (var field in workItem.Fields)
            {
                patch.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{field.Key}",
                    Value = field.Value
                });
            }

            foreach (var attachment in workItem.Relations ?? new List<WorkItemRelation>())
            {
                patch.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = attachment
                });
            }

            return await _witClient.CreateWorkItemAsync(patch, projectName, workItem.Fields["System.WorkItemType"].ToString(), 
                false, true, true, expand: expand, cancellationToken: token);
        }

        public async Task<IEnumerable<WorkItemField>> GetFields(string project, CancellationToken token) =>
            await _witClient.GetFieldsAsync(project, GetFieldsExpand.ExtensionFields, token);

        public async Task<WorkItemField> CreateField(WorkItemField field, CancellationToken token) =>
            await _witClient.CreateFieldAsync(field, cancellationToken: token);

        public async Task<PickList> GetPickList(Guid pickListId, CancellationToken token) =>
            await _witProcessClient.GetListAsync(pickListId, cancellationToken: token);

        public async Task<WITComment> AddComment(string projectName, int workItemId, string comment, CancellationToken token)
        {
            CheckInitialized();

            var request = new CommentCreate()
            {
                Text = comment
            };

            return await _witClient.AddCommentAsync(request, projectName, workItemId, cancellationToken: token);
        }

        public async Task<WorkItem> UpdateWorkItem(string projectName, WorkItem workItem, CancellationToken token, WorkItemExpand expand = WorkItemExpand.All)
        {
            CheckInitialized();

            var patch = new JsonPatchDocument();

            foreach (var field in workItem.Fields)
            {
                patch.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Replace,
                    Path = $"/fields/{field.Key}",
                    Value = field.Value
                });
            }

            foreach (var attachment in (workItem.Relations ?? new List<WorkItemRelation>()))
            {
                patch.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = attachment
                });
            }

            return await _witClient.UpdateWorkItemAsync(patch, workItem.Id.Value, false, true, true, null, cancellationToken: token);
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItemHistory(string projectName, int workItemId, CancellationToken token, WorkItemExpand expand = WorkItemExpand.None)
        {
            CheckInitialized();
            return await _witClient.GetRevisionsAsync(projectName, workItemId, top: ushort.MaxValue, expand: expand, cancellationToken: token);
        }

        public async Task<WorkItem> GetWorkItem(string projectName, int workItemId, CancellationToken token, WorkItemExpand expand = WorkItemExpand.None)
        {
            CheckInitialized();
            return await _witClient.GetWorkItemAsync(projectName, workItemId, expand: expand, cancellationToken: token);
        }

        public async Task<IEnumerable<WorkItemComment>> GetWorkItemComments(string projectName, int workItemId, int revision, CancellationToken token, WorkItemExpand expand = WorkItemExpand.None)
        {
            CheckInitialized();
            return (await _witClient.GetCommentsAsync(projectName, workItemId, 
                fromRevision: revision, top: ushort.MaxValue, cancellationToken: token)).Comments;
        }

        public async Task<PickList> CreatePickList(PickList picklist, CancellationToken token) =>
            await _witProcessClient.CreateListAsync(picklist, cancellationToken: token);

        public async Task<WorkItemField> GetField(string referenceName, CancellationToken token) =>
            await _witClient.GetFieldAsync(referenceName, cancellationToken: token);

        public async Task<WorkItem> GetWorkItemByMigrationState(string projectName, string migrationStateField, string url, CancellationToken token)
        {
            var existing = await GetIdsByWiql($"[Custom.{migrationStateField}] = '{url}'", token);

            if (existing.Count() == 0)
            {
                return null;
            }

            return await GetWorkItem(projectName, existing.OrderBy(x => x).LastOrDefault(), token, WorkItemExpand.All);
        }

        public async Task<List<WorkItem>> GetWorkItemRevisions(string projectName, int workItemId, CancellationToken token)
        {
            List<WorkItem> tempResults = new List<WorkItem>();
            List<WorkItem> finalSet = new List<WorkItem>();
            
            do
            {
                tempResults = await _witClient.GetRevisionsAsync(workItemId, top: 100, skip: finalSet.Count, expand: WorkItemExpand.All, cancellationToken: token);
                if (tempResults.Count > 0)
                {
                    finalSet.AddRange(tempResults);
                }
            } while (tempResults.Count > 0);
            
            return finalSet;
        }
            

        public async Task<WorkItem> GetWorkItemRevision(string projectName, int workItemId, int revision, CancellationToken token) => 
            await _witClient.GetRevisionAsync(workItemId, revision, WorkItemExpand.All, cancellationToken: token);

        public async Task<AttachmentReference> CreateAttachment(string projectName, Stream dataStream, string fileName, string areaPath, CancellationToken token, string uploadType = "Chunked") =>
            await _witClient.CreateAttachmentAsync(dataStream, project: projectName, fileName: fileName, uploadType: uploadType, areaPath: areaPath, cancellationToken: token);
        public async Task<Stream> GetAttachmentStream(Guid guid, string fileName, CancellationToken token) =>
            await _witClient.GetAttachmentContentAsync(guid, fileName, cancellationToken: token);
    }

    public interface IEndpointService
    {
        Task<WorkItem> CreateWorkItem(string projectName, WorkItem workItem, CancellationToken token, WorkItemExpand expand = WorkItemExpand.All);
        Task<IEnumerable<TeamProjectReference>> GetAllProjects();
        Task<IEnumerable<WorkItemClassificationNode>> GetAreaPaths(string projectName, CancellationToken token);
        Task<IEnumerable<WorkItemClassificationNode>> GetClassificationNodes(string projectName, TreeStructureGroup type, CancellationToken token);
        Task<IEnumerable<WorkItemField>> GetFields(string project, CancellationToken token);
        Task<WorkItemField> CreateField(WorkItemField field, CancellationToken token);
        Task<IEnumerable<int>> GetIdsByWiql(string wiqlQuery, CancellationToken token);
        Task<IEnumerable<WorkItemClassificationNode>> GetIterations(string projectName, CancellationToken token);
        Task<TeamProject> GetProject(string projectName);
        Task<WorkItem> GetWorkItem(string projectName, int workItemId, CancellationToken token, WorkItemExpand expand = WorkItemExpand.None);
        Task<IEnumerable<WorkItemComment>> GetWorkItemComments(string projectName, int workItemId, int revision, CancellationToken token, WorkItemExpand expand = WorkItemExpand.None);
        Task<IEnumerable<WorkItem>> GetWorkItemHistory(string projectName, int workItemId, CancellationToken token, WorkItemExpand expand = WorkItemExpand.None);
        Task<IEnumerable<WorkItem>> GetWorkItems(IEnumerable<int> ids, CancellationToken token, WorkItemExpand expand = WorkItemExpand.None);
        Task<(int TotalCount, IEnumerable<WorkItem> Items)> GetWorkItemsAsync(string wiqlQuery, CancellationToken token, int top = 0, int skip = 0);
        void Initialize(string endpointUrl, string pat);
        Task<WorkItem> UpdateWorkItem(string projectName, WorkItem workItem, CancellationToken token, WorkItemExpand expand = WorkItemExpand.All);
        Task UpsertClassificationNodesAsync(WorkItemClassificationNode node, string projectName, TreeStructureGroup group, CancellationToken token);
        Task<PickList> GetPickList(Guid picklistId, CancellationToken token);
        Task<PickList> CreatePickList(PickList picklist, CancellationToken token);
        Task<WorkItemField> GetField(string referenceName, CancellationToken token);
        Task<WorkItem> GetWorkItemByMigrationState(string projectName, string migrationStateField, string url, CancellationToken token);
        Task<List<WorkItem>> GetWorkItemRevisions(string projectName, int workItemId, CancellationToken token);
        Task<WorkItem> GetWorkItemRevision(string projectName, int workItemId, int revision, CancellationToken token);
        Task<AttachmentReference> CreateAttachment(string projectName, Stream dataStream, string fileName, string areaPath, CancellationToken token, string uploadType = "Chunked");
        Task<Stream> GetAttachmentStream(Guid guid, string fileName, CancellationToken token);
        Task<GitRepository> GetGitRepository(string projectName, string repoName, CancellationToken token);
        Task<GitRepository> GetGitRepository(Guid projectId, string repoName, CancellationToken token);
        Task<GitRepository> GetGitRepository(Guid projectId, Guid repoId, CancellationToken token);
        Task<GitRepository> GetGitRepository(Guid repoId, CancellationToken token);
        Task<GitRef> GetGitRef(string projectName, Guid repoId, string refName, CancellationToken token);
        Task<IEnumerable<GitRef>> GetGitRefs(string projectName, Guid repoId, CancellationToken token);
        Task<IEnumerable<GitRepository>> GetGitRepositories(string projectName, CancellationToken token);
        Task<IEnumerable<GitCommitRef>> GetCommitRefs(Guid repoId, CancellationToken token, string branch = null);
        Task<GitRepository> CreateGitRepository(string projectName, string repoName, CancellationToken token);
    }
}
