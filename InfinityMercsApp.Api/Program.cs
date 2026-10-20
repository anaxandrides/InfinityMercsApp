using Azure.Identity;
using InfinityMercsApp.Api.ArmyData;
using InfinityMercsApp.Api.Cosmos;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// In Azure "ArmyStorage" holds a serviceUri and the Function app's managed identity signs in; locally it is a
// connection string to Azurite.
builder.Services.AddAzureClients(clients =>
{
    clients.AddBlobServiceClient(builder.Configuration.GetSection("ArmyStorage"));
    clients.UseCredential(new DefaultAzureCredential());
});

builder.Services.AddHttpClient<IArmyPayloadSource, CorvusBelliPayloadSource>();
builder.Services
    .AddSingleton(TimeProvider.System)
    .AddSingleton(_ => CosmosClients.Create(builder.Configuration))
    .AddSingleton<IArmyPayloadStore, BlobArmyPayloadStore>()
    .AddSingleton<IArmySnapshotStore, CosmosArmySnapshotStore>()
    .AddTransient<ArmyMirror>();

builder.Build().Run();
