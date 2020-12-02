using AzureDevOpsMigrator.Models;
using AzureDevOpsMigrator.Services;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//using Git = LibGit2Sharp;

namespace AzureDevOpsMigrator.Migrators
{
    public enum GitMappingState
    {
        Unknown,
        TargetInSync,
        TargetBehind,
        TargetAhead,
        TargetRepoNeedsCreation,
        TargetRefNeedsCreation
    }
    /*
    public interface IGitMigrator
    {
        Task RefreshMappings(CancellationToken token);
    }
    public class GitMigrator : BaseMigrator<GitMigrator>, IMigrator, IGitMigrator
    {
        private int _totalCount = 0;
        private int _processedCount = 0;
        public GitMigrator(MigrationConfig config, ILogger<GitMigrator> log, IEndpointServiceResolver endpointResolver) :
            base(config, log, endpointResolver)
        {

        }

        public async Task ExecuteAsync(System.Threading.CancellationToken token)
        {
            _log.LogInformation($"Fetching up to date mapping status..");

            await RefreshMappings(token);
            var work = _config.GitRepoMappings.Where(r => r.Enabled).GroupBy(r => r.SourceRepoId).Where(g => g.Count() > 0);

            _log.LogInformation($"Found {work.Count()} repos to synchronize and/or migrate..");


            var workQueue = new Queue<IGrouping<Guid, GitRepoMapping>>(work);
            _totalCount = workQueue.Count;
            _processedCount = 0;
            ConcurrentBag<Exception> haltedExceptions = new ConcurrentBag<Exception>();
            var tasks = new ConcurrentDictionary<Guid, Task>();

            do
            {
                token.ThrowIfCancellationRequested();
                if (tasks.Count() < _config.MaxDegreeOfParallelism)
                {
                    // Pull a record off the queue for processing
                    var currentGroup = workQueue.Dequeue();

                    // Add that processing task to the tasks list
                    tasks.TryAdd(currentGroup.Key, Task.Run(async () =>
                    {
                        try
                        {
                            var workItem = await _processWorkItem(currentGroup.Key, token);

                            _processedCount += 1;
                            if (!tasks.TryRemove(currentGroup.Key, out Task t))
                            {
                                throw new GitProcessingException("Git migrator failed to manage task parallelism properly.", , exceptionClass: ExceptionClass.Minor);
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

            } while (workQueue.Count > 0 && haltedExceptions.Count == 0);

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

        private async Task _processRepoMapping(GitRepoMapping map)
        {

        }

        private async Task<GitRepository> _buildTargetRepo(string repoName, CancellationToken token)
        {
            var targetRepo = await _targetEndpoint.CreateGitRepository(_config.TargetEndpointConfig.ProjectName, repoName, token);
            
            // TODO: Add additional repo metadata like policies and security

            return targetRepo;
        }
        private async Task _processRepo(GitRepo repoMap, CancellationToken token)
        {
            var needsTargetRepo = false;
            var repoPath = Path.Combine(_config.WorkingFolder, "git", _config.SourceEndpointConfig.ProjectName, repoMap.SourceRepoName);
            var sourceUrl = $"{_config.SourceEndpointConfig.EndpointUri}/{_config.SourceEndpointConfig.ProjectName}/_git/{repoMap.SourceRepoName}";
            var targetUrl = $"{_config.TargetEndpointConfig.EndpointUri}/{_config.TargetEndpointConfig.ProjectName}/_git/{repoMap.TargetRepoName}";
            Git.Handlers.CredentialsHandler sourceCredsProvider = (_url, _user, _cred) => new Git.UsernamePasswordCredentials { Username = "Migrator", Password = _config.SourceEndpointConfig.PersonalAccessToken };
            Git.Handlers.CredentialsHandler targetCredsProvider = (_url, _user, _cred) => new Git.UsernamePasswordCredentials { Username = "Migrator", Password = _config.TargetEndpointConfig.PersonalAccessToken };

            if (!Directory.Exists(repoPath)) {
                Directory.CreateDirectory(repoPath);
                repoPath = Git.Repository.Clone(sourceUrl, repoPath, new Git.CloneOptions()
                {
                    CredentialsProvider = sourceCredsProvider
                    // TODO: Add progress updates using CloneOptions.OnProgress
                });
            }
        
            using (var repo = new Git.Repository(repoPath)) 
            {
                GitRepository targetRepo = null;

                if (!repoMap.TargetRepoId.HasValue)
                {
                    targetRepo = await _buildTargetRepo(repoMap.TargetRepoName, token);
                } 
                else
                {
                    targetRepo = await _targetEndpoint.GetGitRepository(repoMap.TargetRepoId.Value, token);
                }

                if (targetRepo != null)
                {

                    // We've got a repo in our working folder.. lets make sure it has both remotes
                    if (!repo.Network.Remotes.Any(r => r.Name == "target"))
                    {
                        repo.Network.Remotes.Add("target", targetUrl);
                    }

                    var logMessage = "";
                    Git.Commands.Fetch(repo, "target", repo.Network.Remotes["target"].FetchRefSpecs.Select(x => x.Specification), new Git.FetchOptions()
                    {

                        CredentialsProvider = targetCredsProvider,
                        // TODO: Add progress updates using FetchOptions.OnProgress
                    }, logMessage);

                    if (!string.IsNullOrEmpty(logMessage))
                    {
                        _log.LogInformation(logMessage);
                    }

                    logMessage = "";
                    Git.Commands.Fetch(repo, "origin", repo.Network.Remotes["origin"].FetchRefSpecs.Select(x => x.Specification), new Git.FetchOptions()
                    {

                        CredentialsProvider = sourceCredsProvider,
                        // TODO: Add progress updates using FetchOptions.OnProgress
                    }, logMessage);

                    if (!string.IsNullOrEmpty(logMessage))
                    {
                        _log.LogInformation(logMessage);
                    }

                }
                else
                {
                    throw new GitProcessingException($"Could not locate target repo {repoMap.TargetRepoName} with id <{(repoMap.TargetRepoId.HasValue ? repoMap.TargetRepoId.Value.ToString() : "not set")}>.", repoMap.SourceRepoName, repoMap.SourceRepoId, "");
                }

            }
        }

        public async Task RefreshMappings(CancellationToken token)
        {
            var sourceRepos = await _sourceEndpoint.GetGitRepositories(_config.SourceEndpointConfig.ProjectName, token);
            var targetRepos = await _targetEndpoint.GetGitRepositories(_config.TargetEndpointConfig.ProjectName, token);
            var sourceSet = new Dictionary<GitRepo, IEnumerable<GitRef>>();
            var targetSet = new Dictionary<GitRepository, IEnumerable<GitRef>>();

            foreach (var repo in sourceRepos)
            {
                var header = _config.GitRepoMappings.FirstOrDefault(g => g.SourceRepoId == repo.Id);
                if (header == null)
                {
                    new GitRepo()
                    {
                        SourceRepoId = repo.Id,
                        SourceRepoName = repo.Name,
                        GitRepoMappings = new System.Collections.ObjectModel.ObservableCollection<GitRepoMapping>(),
                        TargetRepoName = repo.Name
                    };
                    _config.GitRepoMappings.Add(header);
                }
                sourceSet.Add(header, await _sourceEndpoint.GetGitRefs(repo.ProjectReference.Name, repo.Id, token));
            }
            foreach (var repo in targetRepos)
            {
                targetSet.Add(repo, await _targetEndpoint.GetGitRefs(repo.ProjectReference.Name, repo.Id, token));
            }

            foreach (var srcRepo in _config.GitRepoMappings)
            {
                foreach (var srcRef in sourceSet[srcRepo]) 
                {
                    GitRepoMapping map = null;
                    foreach (var _ in srcRepo.GitRepoMappings)
                    {
                        if (_.SourceRepoId == srcRepo.SourceRepoId && _.Reference.Name == srcRef.Name)
                        {
                            // At this point I've found the source map
                            map = _;
                        }
                    }
                    if (map == null)
                    {
                        map = new GitRepoMapping()
                        {
                            SourceRepoId = srcRepo.SourceRepoId,
                            SourceRepoName = srcRepo.SourceRepoName,
                            Enabled = true,
                            Reference = srcRef
                        };
                        srcRepo.GitRepoMappings.Add(map);
                    }

                    map.State = GitMappingState.Unknown;

                    // Now lets make sure to map to repo on target
                    if (!map.TargetRepoId.HasValue)
                    {
                        foreach (var repo in targetSet)
                        {
                            if (repo.Key.Name.Equals(string.IsNullOrEmpty (srcRepo.TargetRepoName) ? srcRepo.SourceRepoName : srcRepo.TargetRepoName, StringComparison.OrdinalIgnoreCase))
                            {
                                map.TargetRepoId = repo.Key.Id;
                            }
                        }
                    }

                    if (map.TargetRepoId.HasValue)
                    {
                        // Now lets update the state of the map
                        bool foundTargetRepo = false;
                        
                        // Iterate through all target repos and refs to find the exact ref from the source
                        foreach (var repo in targetSet)
                        {
                            if (repo.Key.Id == map.TargetRepoId)
                            {
                                // Found the right repo
                                GitRef foundTgtRef = null;
                                foreach (var tgtRef in repo.Value)
                                {
                                    if (tgtRef.Name == srcRef.Name)
                                    {
                                        // Found the right ref
                                        foundTgtRef = tgtRef;
                                        break;
                                    }
                                }

                                // Found a target ref.. now we need to analyze the commits to see if ahead, behind, or matching
                                if (foundTgtRef != null)
                                {
                                    // We found the target ref and objectId in the target repo
                                    if (foundTgtRef.ObjectId == srcRef.ObjectId)
                                    {
                                        map.State = GitMappingState.TargetInSync;
                                    }
                                    else
                                    {
                                        // Look at the commits for the given ref..
                                        //     if the target has commits the source doesn't.. then we set state to TargetConflict
                                        //     otherwise the target must just be behind the source.. then we set state to TargetBehind
                                        var sourceCommits = await _sourceEndpoint.GetCommitRefs(srcRepo.SourceRepoId, token, branch: foundTgtRef.Name);
                                        var targetCommits = await _targetEndpoint.GetCommitRefs(repo.Key.Id, token, branch: foundTgtRef.Name);

                                        if (targetCommits.Any(tgtCommit => !sourceCommits.Any(srcCommit => srcCommit.CommitId == tgtCommit.CommitId)))
                                        {
                                            map.State = GitMappingState.TargetAhead;
                                        }
                                        else
                                        {
                                            map.State = GitMappingState.TargetBehind;
                                        }
                                    }
                                }
                                else
                                {
                                    map.State = GitMappingState.TargetRefNeedsCreation;
                                }
                                foundTargetRepo = true;
                                break;
                            }
                        }

                        if (!foundTargetRepo)
                        {
                            map.State = GitMappingState.TargetRepoNeedsCreation;
                        }
                        
                    } 
                    else
                    {
                        // The map to the target repo has not been established
                        map.State = GitMappingState.TargetRepoNeedsCreation;
                    }

                }

            }

        }

        public Task<int?> GetPlannedCount(CancellationToken token) => Task.FromResult((int?)_config.GitRepoMappings.Count(m => m.Enabled));

    }*/
    public class GitProcessingException : Exception
    {
        public ExceptionClass ExceptionClass { get; private set; }
        public string RepoName { get; private set; }
        public Guid RepoId { get; private set; }
        public string RefName { get; private set; }
        public GitProcessingException(string message, string repoName, Guid repoid, string refName, Exception innerException = null, ExceptionClass exceptionClass = ExceptionClass.Major) :
            base(message, innerException)
        {
            RepoName = repoName;
            RepoId = repoid;
            RefName = refName;
            ExceptionClass = exceptionClass;
        }
    }
}
