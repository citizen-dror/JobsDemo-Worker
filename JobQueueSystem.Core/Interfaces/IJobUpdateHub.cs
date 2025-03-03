using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobQueueSystem.Core.Interfaces
{
    /// <summary>
    /// SignalR hub interface that defines methods for broadcasting updates
    /// </summary>
    public interface IJobUpdateHub
    {
        Task BroadcastJobStatusUpdates(List<int> jobIds);
        Task BroadcastWorkerStatusUpdates(List<string> workerIds);
        Task SendJobProgress(int jobId, int progress);
        Task SendJobError(int jobId, string errorMessage);
        Task BroadcastQueueStatus(int pendingJobsCount, int activeJobsCount);
    }

    /// <summary>
    /// Alert severity levels for system notifications
    /// </summary>
    public enum AlertLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
