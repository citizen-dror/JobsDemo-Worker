using JobQueueSystem.Core.Interfaces;
using JobQueueSystem.Core.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JobQueueSystem.WorkerNode.Services
{
    //communication between the worker nodes and the job queue service
    public class WorkerApiClient : IWorkerApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WorkerApiClient> _logger;
        private readonly WorkerSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public WorkerApiClient(
            IHttpClientFactory httpClientFactory,
            ILogger<WorkerApiClient> logger,
            IOptions<WorkerSettings> settings)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _settings = settings.Value;

            // Configure the base address from settings
            _httpClient.BaseAddress = new Uri(_settings.QueueServiceUrl);
        }

        public async Task<Core.Models.WorkerNode> RegisterWorker(Core.Models.WorkerNode worker)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/workers/register", worker, _jsonOptions);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Core.Models.WorkerNode>(_jsonOptions);
                }

                _logger.LogError($"Failed to register worker. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while registering worker");
                return null;
            }
        }

        public async Task SendHeartbeat(string workerId, WorkerStatus status, int activeJobCount)
        {
            try
            {
                var heartbeat = new
                {
                    WorkerId = workerId,
                    Status = status,
                    ActiveJobCount = activeJobCount,
                    Timestamp = DateTime.UtcNow
                };

                var response = await _httpClient.PostAsJsonAsync("api/workers/heartbeat", heartbeat, _jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to send heartbeat. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending heartbeat");
            }
        }

        public async Task UpdateWorkerStatus(string workerId, WorkerStatus status)
        {
            try
            {
                var statusUpdate = new
                {
                    Status = status
                };

                var response = await _httpClient.PutAsJsonAsync($"api/workers/{workerId}/status", statusUpdate, _jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to update worker status. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while updating worker status");
            }
        }

        public async Task<bool> AssignJobToWorker(string workerId, Job job)
        {
            try
            {
                // This method is typically called by the queue service, not by workers
                // But it's included for completeness
                var response = await _httpClient.PostAsJsonAsync($"api/workers/{workerId}/jobs", job, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while assigning job {job.Id} to worker");
                return false;
            }
        }

        public async Task UpdateJobStatus(Job job)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/jobs/{job.Id}/status", job, _jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to update job status. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while updating status for job {job.Id}");
            }
        }

        public async Task UpdateJobProgress(int jobId, int progress)
        {
            try
            {
                var progressUpdate = new
                {
                    Progress = progress
                };

                var response = await _httpClient.PutAsJsonAsync($"api/jobs/{jobId}/progress", progressUpdate, _jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to update job progress. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while updating progress for job {jobId}");
            }
        }

        public async Task<Job> GetJob(int jobId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Job>($"api/jobs/{jobId}", _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while getting job {jobId}");
                return null;
            }
        }

        public async Task<Job[]> GetPendingJobsForWorker(string workerId, int maxJobs)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Job[]>($"api/workers/{workerId}/pendingjobs?maxJobs={maxJobs}", _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while getting pending jobs");
                return Array.Empty<Job>();
            }
        }
    }
}
