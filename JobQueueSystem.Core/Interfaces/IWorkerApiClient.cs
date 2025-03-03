using JobQueueSystem.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobQueueSystem.Core.Interfaces
{
    //communication between the worker nodes and the job queue service
    public interface IWorkerApiClient
    {
        // Worker registration and status
        Task<WorkerNode> RegisterWorker(WorkerNode worker);
        Task SendHeartbeat(string workerId, WorkerStatus status, int activeJobCount);
        Task UpdateWorkerStatus(string workerId, WorkerStatus status);

        // Job management
        Task<bool> AssignJobToWorker(string workerId, Job job);
        Task UpdateJobStatus(Job job);
        Task UpdateJobProgress(int jobId, int progress);

        // Job retrieval
        Task<Job> GetJob(int jobId);
        Task<Job[]> GetPendingJobsForWorker(string workerId, int maxJobs);
    }
}
