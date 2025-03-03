using Azure;
using JobQueueSystem.Core.Data;
using JobQueueSystem.Core.Interfaces;
using JobQueueSystem.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobQueueSystem.QueueService.Controllers
{
    /// <summary>
    /// API endpoints that allow clients to interact with the job queue
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly JobDbContext _dbContext;
        private readonly ILogger<JobsController> _logger;
        private readonly IJobUpdateHub _jobUpdateHub;

        public JobsController(
            JobDbContext dbContext,
            ILogger<JobsController> logger,
            IJobUpdateHub jobUpdateHub
            )
        {
            _dbContext = dbContext;
            _logger = logger;
            _jobUpdateHub = jobUpdateHub;
        }

        // GET: api/jobs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetJobs(
            [FromQuery] JobStatus? status,
            [FromQuery] JobPriority? priority,
            [FromQuery] string? search,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            IQueryable<Job> query = _dbContext.Jobs;

            // Apply filters
            if (status.HasValue)
            {
                query = query.Where(j => j.Status == status.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(j => j.Priority == priority.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(j => j.JobName.Contains(search));
            }

            if (startDate.HasValue)
            {
                query = query.Where(j => j.StartTime >= startDate.Value || j.ScheduledTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(j => j.EndTime <= endDate.Value);
            }

            // Count total items for pagination
            var totalItems = await query.CountAsync();

            // Apply pagination
            var jobs = await query
                .OrderByDescending(j => j.Priority)
                .ThenByDescending(j => j.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Add pagination headers
            Response.Headers.Add("X-Total-Count", totalItems.ToString());
            Response.Headers.Add("X-Page-Count", Math.Ceiling((double)totalItems / pageSize).ToString());

            return jobs;
        }

        // GET: api/jobs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Job>> GetJob(int id)
        {
            var job = await _dbContext.Jobs.FindAsync(id);

            if (job == null)
            {
                return NotFound();
            }

            return job;
        }

        // GET: api/jobs/5/logs
        [HttpGet("{id}/logs")]
        public async Task<ActionResult<IEnumerable<JobExecutionLog>>> GetJobLogs(int id)
        {
            var job = await _dbContext.Jobs.FindAsync(id);

            if (job == null)
            {
                return NotFound();
            }

            var logs = await _dbContext.JobExecutionLogs
                .Where(l => l.JobId == id)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return logs;
        }

        // POST: api/jobs
        [HttpPost]
        public async Task<ActionResult<Job>> CreateJob(JobCreateDto jobDto)
        {
            // Map DTO to job entity
            var job = new Job
            {
                JobName = jobDto.JobName,
                Priority = jobDto.Priority,
                JobType = jobDto.JobType,
                JobData = jobDto.JobData,
                MaxRetries = jobDto.MaxRetries ?? 3,
                ScheduledTime = jobDto.ScheduledTime
            };

            // Validate job type
            if (string.IsNullOrWhiteSpace(job.JobType))
            {
                return BadRequest("Job type is required");
            }

            // Determine initial status based on scheduled time
            job.Status = job.ScheduledTime != null && job.ScheduledTime > DateTime.UtcNow
                ? JobStatus.Scheduled
                : JobStatus.Pending;

            _dbContext.Jobs.Add(job);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Created job {job.Id} ({job.JobName}) with priority {job.Priority}");

            // Log job creation
            var log = new JobExecutionLog
            {
                JobId = job.Id,
                LogLevel = LogLevel.Information,
                Message = $"Job created with priority {job.Priority}" +
                         (job.ScheduledTime.HasValue ? $", scheduled for {job.ScheduledTime}" : "")
            };
            _dbContext.JobExecutionLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            // Notify clients about the new job
            await _jobUpdateHub.BroadcastJobStatusUpdates(new List<int> { job.Id });

            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
        }

        // PUT: api/jobs/5/cancel
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelJob(int id)
        {
            var job = await _dbContext.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            // Only pending, scheduled or in-progress jobs can be cancelled
            if (job.Status != JobStatus.Pending &&
                job.Status != JobStatus.Scheduled &&
                job.Status != JobStatus.InProgress)
            {
                return BadRequest($"Cannot cancel job with status {job.Status}");
            }

            job.Status = JobStatus.Cancelled;

            // Log job cancellation
            var log = new JobExecutionLog
            {
                JobId = job.Id,
                LogLevel = LogLevel.Information,
                Message = $"Job cancelled by user"
            };
            _dbContext.JobExecutionLogs.Add(log);

            await _dbContext.SaveChangesAsync();

            // Notify clients about the job status change
            await _jobUpdateHub.BroadcastJobStatusUpdates(new List<int> { job.Id });

            return NoContent();
        }

        // PUT: api/jobs/5/priority
        [HttpPut("{id}/priority")]
        public async Task<IActionResult> UpdateJobPriority(int id, [FromBody] JobPriorityUpdateDto updateDto)
        {
            var job = await _dbContext.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            // Only pending or scheduled jobs can have priority changed
            if (job.Status != JobStatus.Pending && job.Status != JobStatus.Scheduled)
            {
                return BadRequest($"Cannot change priority of job with status {job.Status}");
            }

            var oldPriority = job.Priority;
            job.Priority = updateDto.Priority;

            // Log priority change
            var log = new JobExecutionLog
            {
                JobId = job.Id,
                LogLevel = LogLevel.Information,
                Message = $"Job priority changed from {oldPriority} to {updateDto.Priority}"
            };
            _dbContext.JobExecutionLogs.Add(log);

            await _dbContext.SaveChangesAsync();

            // Notify clients about the job update
            await _jobUpdateHub.BroadcastJobStatusUpdates(new List<int> { job.Id });

            return NoContent();
        }

        // PUT: api/jobs/5/reschedule
        [HttpPut("{id}/reschedule")]
        public async Task<IActionResult> RescheduleJob(int id, [FromBody] JobRescheduleDto rescheduleDto)
        {
            var job = await _dbContext.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            // Only pending, scheduled, or failed jobs can be rescheduled
            if (job.Status != JobStatus.Pending &&
                job.Status != JobStatus.Scheduled &&
                job.Status != JobStatus.Failed)
            {
                return BadRequest($"Cannot reschedule job with status {job.Status}");
            }

            var oldScheduledTime = job.ScheduledTime;
            job.ScheduledTime = rescheduleDto.ScheduledTime;
            job.Status = JobStatus.Scheduled;

            if (job.Status == JobStatus.Failed)
            {
                // Reset retry count if rescheduling a failed job
                job.RetryCount = 0;
                job.ErrorMessage = null;
            }

            // Log rescheduling
            var log = new JobExecutionLog
            {
                JobId = job.Id,
                LogLevel = LogLevel.Information,
                Message = $"Job rescheduled from {oldScheduledTime?.ToString() ?? "immediate"} to {rescheduleDto.ScheduledTime?.ToString() ?? "immediate"}"
            };
            _dbContext.JobExecutionLogs.Add(log);

            await _dbContext.SaveChangesAsync();

            // Notify clients about the job update
            await _jobUpdateHub.BroadcastJobStatusUpdates(new List<int> { job.Id });

            return NoContent();
        }

        // POST: api/jobs/bulk-cancel
        [HttpPost("bulk-cancel")]
        public async Task<IActionResult> BulkCancelJobs([FromBody] BulkJobsDto bulkDto)
        {
            if (bulkDto.JobIds == null || !bulkDto.JobIds.Any())
            {
                return BadRequest("No job IDs provided");
            }

            var jobs = await _dbContext.Jobs
                .Where(j => bulkDto.JobIds.Contains(j.Id) &&
                       (j.Status == JobStatus.Pending || j.Status == JobStatus.Scheduled))
                .ToListAsync();

            if (!jobs.Any())
            {
                return NotFound("No eligible jobs found to cancel");
            }

            foreach (var job in jobs)
            {
                job.Status = JobStatus.Cancelled;

                // Log job cancellation
                _dbContext.JobExecutionLogs.Add(new JobExecutionLog
                {
                    JobId = job.Id,
                    LogLevel = LogLevel.Information,
                    Message = "Job cancelled as part of bulk operation"
                });
            }

            await _dbContext.SaveChangesAsync();

            // Notify clients about all job updates
            await _jobUpdateHub.BroadcastJobStatusUpdates(jobs.Select(j => j.Id).ToList());

            return Ok(new { CancelledCount = jobs.Count });
        }

        // GET: api/jobs/stats
        [HttpGet("stats")]
        public async Task<ActionResult<JobStats>> GetJobStats([FromQuery] DateTime? since)
        {
            var query = _dbContext.Jobs.AsQueryable();

            if (since.HasValue)
            {
                query = query.Where(j => j.StartTime >= since.Value || j.ScheduledTime >= since.Value);
            }

            var stats = new JobStats
            {
                TotalJobs = await query.CountAsync(),
                CompletedJobs = await query.CountAsync(j => j.Status == JobStatus.Completed),
                FailedJobs = await query.CountAsync(j => j.Status == JobStatus.Failed),
                PendingJobs = await query.CountAsync(j => j.Status == JobStatus.Pending),
                ScheduledJobs = await query.CountAsync(j => j.Status == JobStatus.Scheduled),
                InProgressJobs = await query.CountAsync(j => j.Status == JobStatus.InProgress),
                CancelledJobs = await query.CountAsync(j => j.Status == JobStatus.Cancelled),
                RetryingJobs = await query.CountAsync(j => j.Status == JobStatus.Retrying),

                ByPriority = new Dictionary<JobPriority, int>
                {
                    { JobPriority.Regular, await query.CountAsync(j => j.Priority == JobPriority.Regular) },
                    { JobPriority.High, await query.CountAsync(j => j.Priority == JobPriority.High) },
                },

                AverageCompletionTimeSeconds = await query
                    .Where(j => j.Status == JobStatus.Completed && j.StartTime.HasValue && j.EndTime.HasValue)
                    .Select(j => (j.EndTime.Value - j.StartTime.Value).TotalSeconds)
                    .DefaultIfEmpty(0)
                    .AverageAsync()
            };

            return stats;
        }
    }

    // Data Transfer Objects
    public class JobCreateDto
    {
        public string JobName { get; set; } = string.Empty;
        public JobPriority Priority { get; set; } = JobPriority.Regular;
        public string JobType { get; set; } = string.Empty;
        public string? JobData { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public int? MaxRetries { get; set; }
    }

    public class JobPriorityUpdateDto
    {
        public JobPriority Priority { get; set; }
    }

    public class JobRescheduleDto
    {
        public DateTime? ScheduledTime { get; set; }
    }

    public class BulkJobsDto
    {
        public List<int> JobIds { get; set; } = new List<int>();
    }

    public class JobStats
    {
        public int TotalJobs { get; set; }
        public int CompletedJobs { get; set; }
        public int FailedJobs { get; set; }
        public int PendingJobs { get; set; }
        public int ScheduledJobs { get; set; }
        public int InProgressJobs { get; set; }
        public int CancelledJobs { get; set; }
        public int RetryingJobs { get; set; }
        public Dictionary<JobPriority, int> ByPriority { get; set; } = new Dictionary<JobPriority, int>();
        public double AverageCompletionTimeSeconds { get; set; }
    }
}
