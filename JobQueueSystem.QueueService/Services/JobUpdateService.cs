using JobQueueSystem.Core.Data;
using JobQueueSystem.Core.Interfaces;
using JobQueueSystem.QueueService.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JobQueueSystem.QueueService.Services
{/// <summary>
 /// Service implementing the IJobUpdateHub interface to broadcast updates to connected clients
 /// </summary>
    public class JobUpdateService : IJobUpdateHub
    {
        private readonly IHubContext<JobUpdateHub> _hubContext;
        private readonly ILogger<JobUpdateService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public JobUpdateService(
            IHubContext<JobUpdateHub> hubContext,
            ILogger<JobUpdateService> logger,
            IServiceProvider serviceProvider)
        {
            _hubContext = hubContext;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Broadcasts job status updates to subscribed clients
        /// </summary>
        public async Task BroadcastJobStatusUpdates(List<int> jobIds)
        {
            if (!jobIds.Any())
            {
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<JobDbContext>();

            try
            {
                var jobs = await dbContext.Jobs
                    .Where(j => jobIds.Contains(j.Id))
                    .ToListAsync();

                foreach (var job in jobs)
                {
                    var jobUpdate = new
                    {
                        job.Id,
                        job.JobName,
                        job.Status,
                        job.Priority,
                        job.Progress,
                        job.StartTime,
                        job.EndTime,
                        job.ErrorMessage,
                        job.AssignedWorker,
                        job.RetryCount,
                        job.ScheduledTime
                    };

                    // Send to specific job subscribers
                    await _hubContext.Clients.Group($"job_{job.Id}").SendAsync("JobUpdated", jobUpdate);

                    // Send to all jobs subscribers
                    await _hubContext.Clients.Group("all_jobs").SendAsync("JobStatusChanged", jobUpdate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting job status updates");
            }
        }

        /// <summary>
        /// Broadcasts worker status updates to subscribed clients
        /// </summary>
        public async Task BroadcastWorkerStatusUpdates(List<string> workerIds)
        {
            if (!workerIds.Any())
            {
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<JobDbContext>();

            try
            {
                var workers = await dbContext.WorkerNodes
                    .Where(w => workerIds.Contains(w.Id))
                    .ToListAsync();

                foreach (var worker in workers)
                {
                    var workerUpdate = new
                    {
                        worker.Id,
                        worker.Name,
                        worker.Status,
                        worker.LastHeartbeat,
                        worker.ActiveJobCount,
                        worker.CurrentJobId,
                        worker.ConcurrencyLimit
                    };

                    // Send to specific worker subscribers
                    await _hubContext.Clients.Group($"worker_{worker.Id}").SendAsync("WorkerUpdated", workerUpdate);

                    // Send to all workers subscribers
                    await _hubContext.Clients.Group("all_workers").SendAsync("WorkerStatusChanged", workerUpdate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting worker status updates");
            }
        }

        /// <summary>
        /// Sends job progress updates to subscribed clients
        /// </summary>
        public async Task SendJobProgress(int jobId, int progress)
        {
            try
            {
                var progressUpdate = new { JobId = jobId, Progress = progress };

                // Send to specific job subscribers
                await _hubContext.Clients.Group($"job_{jobId}").SendAsync("JobProgressUpdated", progressUpdate);

                // Send to all jobs subscribers
                await _hubContext.Clients.Group("all_jobs").SendAsync("JobProgressUpdated", progressUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending job progress for job {jobId}");
            }
        }

        /// <summary>
        /// Sends job error notifications to subscribed clients
        /// </summary>
        public async Task SendJobError(int jobId, string errorMessage)
        {
            try
            {
                var errorUpdate = new { JobId = jobId, ErrorMessage = errorMessage };

                // Send to specific job subscribers
                await _hubContext.Clients.Group($"job_{jobId}").SendAsync("JobError", errorUpdate);

                // Send to all jobs subscribers
                await _hubContext.Clients.Group("all_jobs").SendAsync("JobError", errorUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending error notification for job {jobId}");
            }
        }

        /// <summary>
        /// Broadcasts overall queue status to all connected clients
        /// </summary>
        public async Task BroadcastQueueStatus(int pendingJobsCount, int activeJobsCount)
        {
            try
            {
                var queueStatus = new
                {
                    PendingJobs = pendingJobsCount,
                    ActiveJobs = activeJobsCount,
                    Timestamp = DateTime.UtcNow
                };

                // Broadcast to all connected clients
                await _hubContext.Clients.All.SendAsync("QueueStatusUpdated", queueStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting queue status");
            }
        }
    }
}
