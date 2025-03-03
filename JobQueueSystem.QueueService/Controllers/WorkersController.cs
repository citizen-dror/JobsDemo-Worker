using JobQueueSystem.Core.Data;
using JobQueueSystem.Core.Interfaces;
using JobQueueSystem.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobQueueSystem.QueueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //responsible for managing worker node registrations, heartbeats, and job assignments.
    public class WorkersController : ControllerBase
    {
        private readonly JobDbContext _dbContext;
        private readonly ILogger<WorkersController> _logger;
        private readonly IJobUpdateHub _jobUpdateHub;

        public WorkersController(
            JobDbContext dbContext,
            ILogger<WorkersController> logger,
            IJobUpdateHub jobUpdateHub
            )
        {
            _dbContext = dbContext;
            _logger = logger;
            _jobUpdateHub = jobUpdateHub;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkerNode>>> GetWorkers([FromQuery] WorkerStatus? status)
        {
            IQueryable<WorkerNode> query = _dbContext.WorkerNodes;

            if (status.HasValue)
            {
                query = query.Where(w => w.Status == status.Value);
            }

            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WorkerNode>> GetWorker(string id)
        {
            var worker = await _dbContext.WorkerNodes.FindAsync(id);

            if (worker == null)
            {
                return NotFound();
            }

            return worker;
        }

        [HttpPost("register")]
        public async Task<ActionResult<WorkerNode>> RegisterWorker(WorkerNode worker)
        {
            // Check if worker with this name already exists
            var existingWorker = await _dbContext.WorkerNodes
                .FirstOrDefaultAsync(w => w.Name == worker.Name && w.Status != WorkerStatus.Offline);

            if (existingWorker != null)
            {
                // If worker exists but is offline, reactivate it
                if (existingWorker.Status == WorkerStatus.Offline)
                {
                    existingWorker.Status = WorkerStatus.Idle;
                    existingWorker.LastHeartbeat = DateTime.UtcNow;
                    existingWorker.ActiveJobCount = 0;
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Reactivated worker {existingWorker.Name} ({existingWorker.Id})");

                    // Notify clients
                    await _jobUpdateHub.BroadcastWorkerStatusUpdates(new List<string> { existingWorker.Id });

                    return existingWorker;
                }

                return Conflict($"Worker with name '{worker.Name}' is already registered and active");
            }

            // Ensure worker has a valid ID
            if (string.IsNullOrEmpty(worker.Id))
            {
                worker.Id = Guid.NewGuid().ToString();
            }

            worker.LastHeartbeat = DateTime.UtcNow;
            worker.Status = WorkerStatus.Idle;

            _dbContext.WorkerNodes.Add(worker);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Registered new worker {worker.Name} ({worker.Id})");

            // Notify clients
            await _jobUpdateHub.BroadcastWorkerStatusUpdates(new List<string> { worker.Id });

            return CreatedAtAction(nameof(GetWorker), new { id = worker.Id }, worker);
        }

        [HttpPost("{id}/heartbeat")]
        public async Task<IActionResult> SendHeartbeat(string id, [FromBody] WorkerHeartbeatDto heartbeat)
        {
            var worker = await _dbContext.WorkerNodes.FindAsync(id);
            if (worker == null)
            {
                return NotFound();
            }

            var statusChanged = worker.Status != heartbeat.Status;

            worker.LastHeartbeat = DateTime.UtcNow;
            worker.Status = heartbeat.Status;
            worker.ActiveJobCount = heartbeat.ActiveJobCount;

            await _dbContext.SaveChangesAsync();

            // Only broadcast updates if status changed
            if (statusChanged)
            {
                //// Notify clients
                await _jobUpdateHub.BroadcastWorkerStatusUpdates(new List<string> { worker.Id });
            }

            return Ok();
        }

        [HttpPost("{id}/status")]
        public async Task<IActionResult> UpdateWorkerStatus(string id, [FromBody] WorkerStatusUpdateDto statusUpdate)
        {
            var worker = await _dbContext.WorkerNodes.FindAsync(id);
            if (worker == null)
            {
                return NotFound();
            }

            worker.Status = statusUpdate.Status;
            worker.LastHeartbeat = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            // Notify clients about worker status change
            await _jobUpdateHub.BroadcastWorkerStatusUpdates(new List<string> { worker.Id });

            return Ok();
        }

        [HttpPost("{id}/jobs")]
        public async Task<ActionResult<bool>> AssignJobToWorker(string id, [FromBody] Job job)
        {
            var worker = await _dbContext.WorkerNodes.FindAsync(id);
            if (worker == null)
            {
                return NotFound($"Worker with ID {id} not found");
            }

            if (worker.Status == WorkerStatus.Offline)
            {
                return BadRequest("Cannot assign job to offline worker");
            }

            if (worker.ActiveJobCount >= worker.ConcurrencyLimit)
            {
                return BadRequest("Worker is at capacity");
            }

            // The worker node service should handle the acceptance of the job
            // This endpoint is called by the job distributor to assign jobs to workers

            // Log the assignment
            _logger.LogInformation($"Assigning job {job.Id} to worker {worker.Name} ({worker.Id})");

            // For this API, we're just returning success
            // The actual job processing happens in the worker node service

            return Ok(true);
        }

        [HttpGet("{id}/jobs")]
        public async Task<ActionResult<IEnumerable<Job>>> GetWorkerJobs(string id)
        {
            var worker = await _dbContext.WorkerNodes.FindAsync(id);
            if (worker == null)
            {
                return NotFound();
            }

            var jobs = await _dbContext.Jobs
                .Where(j => j.AssignedWorker == id && j.Status == JobStatus.InProgress)
                .ToListAsync();

            return jobs;
        }
    }

    public class WorkerHeartbeatDto
    {
        public WorkerStatus Status { get; set; }
        public int ActiveJobCount { get; set; }
    }

    public class WorkerStatusUpdateDto
    {
        public WorkerStatus Status { get; set; }
    }
}
