using JobQueueSystem.Core.Data;
using JobQueueSystem.Core.Interfaces;
using JobQueueSystem.QueueService.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// Add DbContext with connection string from appsettings.json
builder.Services.AddDbContext<JobDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("JobDbConnection"))
);

builder.Services.AddSingleton<IJobUpdateHub, JobUpdateService>();
builder.Services.AddHostedService<JobQueueService>();

var host = builder.Build();
host.Run();
