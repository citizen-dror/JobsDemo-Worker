using JobQueueSystem.Core.Interfaces;
using JobQueueSystem.Core.Models;
using JobQueueSystem.WorkerNode.Services;

var builder = Host.CreateApplicationBuilder(args);

// Bind WorkerSettings from appsettings.json
builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("WorkerSettings"));
builder.Services.AddHttpClient(); // Registers IHttpClientFactory
// Register other services
builder.Services.AddScoped<IWorkerApiClient, WorkerApiClient>();
builder.Services.AddSingleton<IJobProcessor, JobProcessor>();
builder.Services.AddHostedService<WorkerNodeService>();

var host = builder.Build();
host.Run();
