using JobQueueSystem.Core.Interfaces;
using JobQueueSystem.Core.Models;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace JobQueueSystem.WorkerNode.Services
{
    /// <summary>
    /// Main job processor that executes different types of jobs based on JobType 
    /// the core execution engine within each worker node.
    /// 
    /// responsible for:
    /// Taking a job that was assigned to a worker, Executing the job based on its type
    /// Reporting progress back to the caller , Handling different types of jobs with different processing logic
    /// Returning success/failure results
    /// </summary>
    public class JobProcessor : IJobProcessor
    {
        private readonly ILogger<JobProcessor> _logger;

        public JobProcessor(ILogger<JobProcessor> logger)
        {
            _logger = logger;
        }

        public async Task<JobProcessResult> ProcessJob(Job job, IProgress<int> progress)
        {
            _logger.LogInformation($"Processing job {job.Id} ({job.JobName}) of type {job.JobType}");

            try
            {
                // Determine which job handler to use based on job type
                return job.JobType switch
                {
                    "DataProcessing" => await ProcessDataJob(job, progress),
                    "FileConversion" => await ProcessFileJob(job, progress),
                    "Notification" => await ProcessNotificationJob(job, progress),
                    "Report" => await ProcessReportJob(job, progress),
                    _ => await ProcessGenericJob(job, progress)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing job {job.Id}");
                return new JobProcessResult
                {
                    Success = false,
                    ErrorMessage = $"Job processor exception: {ex.Message}"
                };
            }
        }

        private async Task<JobProcessResult> ProcessDataJob(Job job, IProgress<int> progress)
        {
            _logger.LogInformation($"Processing data job {job.Id}");

            // Simulate data processing with progress
            for (int i = 0; i <= 100; i += 10)
            {
                if (i > 0)
                {
                    await Task.Delay(500); // Simulate work being done
                }
                progress.Report(i);
            }

            // Here you would parse the job.JobData and perform actual data processing logic

            return new JobProcessResult
            {
                Success = true,
                ResultData = new { ProcessedRecords = 150 }
            };
        }

        private async Task<JobProcessResult> ProcessFileJob(Job job, IProgress<int> progress)
        {
            _logger.LogInformation($"Processing file job {job.Id}");

            // Simulate file processing with progress
            for (int i = 0; i <= 100; i += 5)
            {
                if (i > 0)
                {
                    await Task.Delay(200); // Simulate work being done
                }
                progress.Report(i);
            }

            // Here you would parse job.JobData to get file info and perform conversion

            return new JobProcessResult
            {
                Success = true,
                ResultData = new { ConvertedFilePath = "/path/to/converted/file.pdf" }
            };
        }

        private async Task<JobProcessResult> ProcessNotificationJob(Job job, IProgress<int> progress)
        {
            _logger.LogInformation($"Processing notification job {job.Id}");

            // Notifications typically have a simpler progress pattern
            progress.Report(10);
            await Task.Delay(100);
            progress.Report(50);
            await Task.Delay(100);
            progress.Report(100);

            // Here you would parse job.JobData and send the notification

            return new JobProcessResult
            {
                Success = true,
                ResultData = new { NotificationSent = true, RecipientCount = 5 }
            };
        }

        private async Task<JobProcessResult> ProcessReportJob(Job job, IProgress<int> progress)
        {
            _logger.LogInformation($"Processing report job {job.Id}");

            // Simulate report generation with uneven progress
            int[] steps = { 10, 20, 40, 50, 60, 85, 95, 100 };

            foreach (var step in steps)
            {
                await Task.Delay(300); // Reports often have longer processing times
                progress.Report(step);
            }

            // Here you would parse job.JobData and generate the report

            return new JobProcessResult
            {
                Success = true,
                ResultData = new { ReportUrl = "/reports/generated/report_12345.xlsx" }
            };
        }

        private async Task<JobProcessResult> ProcessGenericJob(Job job, IProgress<int> progress)
        {
            _logger.LogWarning($"Processing unknown job type: {job.JobType}");

            // Simple linear progress for unknown job types
            for (int i = 0; i <= 100; i += 20)
            {
                await Task.Delay(200);
                progress.Report(i);
            }

            return new JobProcessResult
            {
                Success = true,
                ResultData = new { Message = "Generic job completed" }
            };
        }
    }
}
