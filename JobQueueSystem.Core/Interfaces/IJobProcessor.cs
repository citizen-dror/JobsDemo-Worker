using JobQueueSystem.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobQueueSystem.Core.Interfaces
{
    /// <summary>
    /// Interface defining the job processor operations
    /// </summary>
    public interface IJobProcessor
    {
        Task<JobProcessResult> ProcessJob(Job job, IProgress<int> progress);
    }

    /// <summary>
    /// Result of job processing
    /// </summary>
    public class JobProcessResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public object? ResultData { get; set; }
    }
}
