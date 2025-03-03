using Microsoft.AspNetCore.SignalR;

namespace JobQueueSystem.QueueService.SignalR
{
    /// <summary>
    /// SignalR hub for job and worker status updates
    /// </summary>
    public class JobUpdateHub : Hub
    {
        private readonly ILogger<JobUpdateHub> _logger;

        public JobUpdateHub(ILogger<JobUpdateHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Allows clients to subscribe to updates for a specific job
        /// </summary>
        public async Task SubscribeToJob(int jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"job_{jobId}");
            _logger.LogInformation($"Client {Context.ConnectionId} subscribed to job {jobId}");
        }

        /// <summary>
        /// Allows clients to unsubscribe from a specific job
        /// </summary>
        public async Task UnsubscribeFromJob(int jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job_{jobId}");
            _logger.LogInformation($"Client {Context.ConnectionId} unsubscribed from job {jobId}");
        }

        /// <summary>
        /// Allows clients to subscribe to updates for a specific worker
        /// </summary>
        public async Task SubscribeToWorker(string workerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"worker_{workerId}");
            _logger.LogInformation($"Client {Context.ConnectionId} subscribed to worker {workerId}");
        }

        /// <summary>
        /// Allows clients to unsubscribe from a specific worker
        /// </summary>
        public async Task UnsubscribeFromWorker(string workerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"worker_{workerId}");
            _logger.LogInformation($"Client {Context.ConnectionId} unsubscribed from worker {workerId}");
        }

        /// <summary>
        /// Subscribes to all job status updates
        /// </summary>
        public async Task SubscribeToAllJobs()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_jobs");
            _logger.LogInformation($"Client {Context.ConnectionId} subscribed to all jobs");
        }

        /// <summary>
        /// Unsubscribes from all job status updates
        /// </summary>
        public async Task UnsubscribeFromAllJobs()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_jobs");
            _logger.LogInformation($"Client {Context.ConnectionId} unsubscribed from all jobs");
        }

        /// <summary>
        /// Subscribes to all worker status updates
        /// </summary>
        public async Task SubscribeToAllWorkers()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_workers");
            _logger.LogInformation($"Client {Context.ConnectionId} subscribed to all workers");
        }

        /// <summary>
        /// Unsubscribes from all worker status updates
        /// </summary>
        public async Task UnsubscribeFromAllWorkers()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_workers");
            _logger.LogInformation($"Client {Context.ConnectionId} unsubscribed from all workers");
        }

        /// <summary>
        /// Handles client disconnect
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Client {Context.ConnectionId} disconnected");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
