using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobQueueSystem.Core.Models
{
    public class WorkerSettings
    {
        public string WorkerName { get; set; } = $"Worker-{Guid.NewGuid().ToString().Substring(0, 8)}";
        public string QueueServiceUrl { get; set; } = "http://localhost:5000";
        public int ConcurrencyLimit { get; set; } = 2;
        public int HeartbeatIntervalSeconds { get; set; } = 30;
    }
}
