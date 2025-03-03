using JobQueueSystem.Core.Data;
using JobQueueSystem.Core.Interfaces;
using JobQueueSystem.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobQueueSystem.QueueService.Services
{
    /// <summary>
    /// Distribute Jobs to Workers by Workers capacity and jobs priority
    /// </summary>
    public class JobDistributor
    {
        private readonly ILogger<JobDistributor> _logger;
        private readonly JobDbContext _dbContext;
        private readonly IWorkerApiClient _workerApiClient;

        public JobDistributor(
            ILogger<JobDistributor> logger,
            JobDbContext dbContext,
            IWorkerApiClient workerApiClient
            )
        {
            _logger = logger;
            _dbContext = dbContext;
            _workerApiClient = workerApiClient;
        }

        public async Task DistributeJobs(List<Job> pendingJobs, List<WorkerNode> availableWorkers)
        {
            if (!pendingJobs.Any() || !availableWorkers.Any())
            {
                return;
            }

            // Group workers by how many more jobs they can handle
            var workersByCapacity = availableWorkers
                .OrderBy(w => w.ActiveJobCount)
                .GroupBy(w => w.ConcurrencyLimit - w.ActiveJobCount)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Sort jobs by priority (highest first)
            var prioritizedJobs = pendingJobs
                .OrderByDescending(j => j.Priority)
                .ThenBy(j => j.ScheduledTime ?? DateTime.MinValue)
                .ToList();

            foreach (var job in prioritizedJobs)
            {
                // Find a worker with available capacity
                WorkerNode selectedWorker = null;

                // Try to find a worker with capacity starting from the ones with most available slots
                foreach (var capacity in workersByCapacity.Keys.OrderByDescending(k => k))
                {
                    if (workersByCapacity[capacity].Any())
                    {
                        selectedWorker = workersByCapacity[capacity].First();

                        // Move worker to appropriate capacity group or remove if full
                        workersByCapacity[capacity].Remove(selectedWorker);
                        int newCapacity = capacity - 1;
                        if (newCapacity > 0)
                        {
                            if (!workersByCapacity.ContainsKey(newCapacity))
                            {
                                workersByCapacity[newCapacity] = new List<WorkerNode>();
                            }
                            workersByCapacity[newCapacity].Add(selectedWorker);
                        }

                        break;
                    }
                }

                if (selectedWorker == null)
                {
                    // No workers available with capacity
                    _logger.LogInformation($"No workers available with capacity for job {job.Id}");
                    break;
                }

                // Assign the job to the worker
                job.Status = JobStatus.InProgress;
                job.AssignedWorker = selectedWorker.Id;
                job.StartTime = DateTime.UtcNow;

                selectedWorker.Status = WorkerStatus.Busy;
                selectedWorker.ActiveJobCount++;
                selectedWorker.CurrentJobId = job.Id;

                _logger.LogInformation($"Assigning job {job.Id} ({job.JobName}) to worker {selectedWorker.Name}");

                // Send the job to the worker
                bool assignmentSuccessful = await _workerApiClient.AssignJobToWorker(selectedWorker.Id, job);

                if (!assignmentSuccessful)
                {
                    _logger.LogWarning($"Failed to assign job {job.Id} to worker {selectedWorker.Name}. Will try again later.");
                    job.Status = JobStatus.Pending;
                    job.AssignedWorker = null;
                    job.StartTime = null;

                    selectedWorker.ActiveJobCount--;
                    selectedWorker.Status = selectedWorker.ActiveJobCount > 0 ? WorkerStatus.Busy : WorkerStatus.Idle;
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
