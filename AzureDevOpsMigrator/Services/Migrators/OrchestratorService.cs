﻿using AzureDevOpsMigrator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Collections.Generic;

namespace AzureDevOpsMigrator.Migrators
{
    public class MigrationPlan
    {
        public int? IterationsCount { get; set; } = null;
        public int? AreaPathsCount { get; set; } = null;
        public int? WorkItemsCount { get; set; } = null;
    }
    public interface IOrchestratorService
    {
        Task ExecuteAsync(MigrationPlan plan, CancellationToken token);
        Task<MigrationPlan> BuildMigrationPlan(CancellationToken token);

        event StatusChangedEventHandler StatusChanged;

    }
    public class OrchestratorService : IOrchestratorService
    {
        private ILogger<IOrchestratorService> _log;
        private MigrationConfig _config;
        private IIterationMigrator _iterationMigrator;
        private IAreaPathMigrator _areaPathMigrator;
        private IWorkItemMigrator _workItemMigrator;
        
        public event StatusChangedEventHandler StatusChanged;
        private MigrationPlan _currentPlan;
        private int _current = 0;
        private object _currentCounterLock = new object();

        public OrchestratorService(MigrationConfig config, IWorkItemMigrator workItemMigrator, 
            IAreaPathMigrator areaPathMigrator, IIterationMigrator iterationMigrator, 
            ILogger<IOrchestratorService> log)
        {
            _log = log;
            _config = config;
            _iterationMigrator = iterationMigrator;
            _areaPathMigrator = areaPathMigrator;
            _workItemMigrator = workItemMigrator;

            _log.LogInformation("Initializing source and target endpoints");
            _iterationMigrator.InitializeEndpoints();
            _areaPathMigrator.InitializeEndpoints();
            _workItemMigrator.InitializeEndpoints();

            _workItemMigrator.StatusChanged += Migrator_StatusChanged;
            _iterationMigrator.StatusChanged += Migrator_StatusChanged;
            _areaPathMigrator.StatusChanged += Migrator_StatusChanged;
        }

        private void Migrator_StatusChanged(object source, (int Increment, SyncState State, EntityType Type) eventArgs)
        {
            lock (_currentCounterLock)
            {
                _current += eventArgs.Increment;
            }
            StatusChanged.Invoke(this, eventArgs);
        }

        public async Task<MigrationPlan> BuildMigrationPlan(CancellationToken token)
        {
            var plan = new MigrationPlan() { };

            InitializeEndpoints();

            var exceptions = new List<Exception>();

            await Task.WhenAll(
                Task.Run(async () =>
                {
                    try
                    {
                        return plan.IterationsCount = _config.Execution.IterationsMigratorEnabled ? await _iterationMigrator.GetPlannedCount(token) : 0;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        return -1;
                    }
                }),
                Task.Run(async () =>
                {
                    try
                    {
                        return plan.AreaPathsCount = _config.Execution.AreaPathMigratorEnabled ? await _areaPathMigrator.GetPlannedCount(token) : 0;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        return -1;
                    }
                }),
                Task.Run(async () =>
                {
                    try
                    {
                        return plan.WorkItemsCount = _config.Execution.WorkItemsMigratorEnabled ? await _workItemMigrator.GetPlannedCount(token) : 0;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        return -1;
                    }
                }));

            if (exceptions.Count > 0)
            {
                throw new MigrationException($"Failed to generate migration plan.", exceptions.First());
            }

            return plan;
        }


        public async Task ExecuteAsync(MigrationPlan plan, CancellationToken token)
        {
            lock (_currentCounterLock)
            {
                _current = 0;
            }

            try
            {
                _log.LogInformation("Starting pre migration checks");
                #region Check Fields ...
                if (_config.PreMigrationChecks.CompareFields)
                {
                    _log.LogInformation("Scanning for fields that are missing in target project.");
                    var fieldsDiff = await _workItemMigrator.CompareFields(token);
                    if (fieldsDiff.Length > 0)
                    {
                        if (_config.CreateFieldsInTarget)
                        {
                            await _workItemMigrator.MigrateFields(_config.TargetEndpointConfig.ProjectName, fieldsDiff, token);
                        } 
                        else
                        {
                            _log.LogInformation("The following fields are missing from the target project:");
                            foreach (var f in fieldsDiff)
                            {
                                _log.LogInformation($"\t\t{f.ReferenceName} <{Enum.GetName(typeof(FieldType), f.Type)}>");
                            }
                            throw new MigrationException("Cannot migrate due to missing fields in target.");
                        }

                    }
                    else
                    {
                        _log.LogInformation("All fields are present and accounted for in target project.");
                    }
                }
                if (_config.CreateFieldsInTarget)
                {
                    await _workItemMigrator.SetupMigrationField(token);
                }
                #endregion

                _currentPlan = plan;
                _log.LogInformation("Starting migrations");

                if (_config.Execution.AreaPathMigratorEnabled)
                {
                    await _areaPathMigrator.ExecuteAsync(token);
                }
                else
                {
                    _log.LogInformation("Skipping area path migrations..");
                }
                if (_config.Execution.IterationsMigratorEnabled)
                {
                    await _iterationMigrator.ExecuteAsync(token);
                }
                else
                {
                    _log.LogInformation("Skipping iteration migrations..");
                }


                if (_config.Execution.WorkItemsMigratorEnabled)
                {
                    await _workItemMigrator.ExecuteAsync(token);
                } 
                else
                {
                    _log.LogInformation("Skipping work item migrations..");
                }
            }
            catch (OperationCanceledException opCanceledEx)
            {
                _log.LogInformation("Migration Canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _log.LogInformation($"Migration Failed!");
                _log.LogError($"{ex}");
                throw;
            }


            _log.LogInformation("Migration Complete.");
        }

        private void InitializeEndpoints()
        {
            _workItemMigrator.InitializeEndpoints();
            _iterationMigrator.InitializeEndpoints();
            _areaPathMigrator.InitializeEndpoints();
        }
    }

    public delegate void StatusChangedEventHandler(object source, (int Increment, SyncState State, EntityType Type) eventArgs);
}
