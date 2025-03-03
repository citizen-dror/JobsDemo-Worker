using Microsoft.Extensions.Logging;

namespace JobQueueSystem.Core.Models
{
    public class JobExecutionLog
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? WorkerId { get; set; }
    }

}
