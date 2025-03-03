using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobQueueSystem.Core.Models
{
    public class WorkerNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public WorkerStatus Status { get; set; } = WorkerStatus.Idle;
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
        public int CurrentJobId { get; set; }
        public int ConcurrencyLimit { get; set; } = 1;
        public int ActiveJobCount { get; set; } = 0;
    }

    public enum WorkerStatus
    {
        Idle = 0,
        Busy = 1,
        Offline = 2
    }
}
