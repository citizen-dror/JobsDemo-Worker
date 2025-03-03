using JobQueueSystem.WorkerNode.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerNodeService>();

var host = builder.Build();
host.Run();
