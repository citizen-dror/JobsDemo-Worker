using JobQueueSystem.Core.Data;
using JobQueueSystem.Core.Interfaces;
using JobQueueSystem.Core.Models;
using JobQueueSystem.QueueService.Services;
using JobQueueSystem.QueueService.SignalR;
using JobQueueSystem.WorkerNode.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Fix for SignalR in worker services
builder.Services.AddSingleton<Microsoft.AspNetCore.Hosting.IApplicationLifetime>(sp =>
    (Microsoft.AspNetCore.Hosting.IApplicationLifetime)sp.GetRequiredService<IHostApplicationLifetime>());


// Add DbContext with connection string from appsettings.json
builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("JobDbConnection"))
);

builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("WorkerSettings"));
builder.Services.AddHttpClient(); // Registers IHttpClientFactory

builder.Services.AddScoped<IWorkerApiClient, WorkerApiClient>();
builder.Services.AddScoped<IJobProcessor, JobProcessor>();
builder.Services.AddScoped<JobDistributor>(); 
builder.Services.AddHostedService<JobQueueService>(); 
builder.Services.AddSingleton<IJobUpdateHub, JobUpdateService>();

// Add SignalR services
builder.Services.AddSignalR();

var host = builder.Build();

host.Run();
