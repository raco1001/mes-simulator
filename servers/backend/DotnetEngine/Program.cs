using DotnetEngine.Application.Asset.Handlers;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Health.Handlers;
using DotnetEngine.Application.Health.Ports;
using DotnetEngine.Application.Relationship.Handlers;
using DotnetEngine.Application.Relationship.Ports.Driving;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Handlers;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Application.Simulation.Rules;
using DotnetEngine.Infrastructure.Kafka;
using DotnetEngine.Infrastructure.Mongo;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IGetHealthQuery, GetHealthQueryHandler>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins("http://localhost:5173", "https://your-production-domain.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB")
    ?? "mongodb://admin:admin123@localhost:27017/factory_mes?authSource=admin";

builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(mongoConnectionString));

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("factory_mes");
});

builder.Services.AddScoped<IAssetRepository, MongoAssetRepository>();
builder.Services.AddScoped<IRelationshipRepository, MongoRelationshipRepository>();
builder.Services.AddScoped<ISimulationRunRepository, MongoSimulationRunRepository>();
builder.Services.AddScoped<IEventRepository, MongoEventRepository>();

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddScoped<IEventPublisher, KafkaEventPublisher>();

builder.Services.AddScoped<IPropagationRule, SuppliesRule>();
builder.Services.AddScoped<IPropagationRule, ContainsRule>();
builder.Services.AddScoped<IPropagationRule, ConnectedToRule>();

builder.Services.AddScoped<IGetAssetsQuery, GetAssetsQueryHandler>();
builder.Services.AddScoped<IGetStatesQuery, GetStatesQueryHandler>();
builder.Services.AddScoped<ICreateAssetCommand, CreateAssetCommandHandler>();
builder.Services.AddScoped<IUpdateAssetCommand, UpdateAssetCommandHandler>();

builder.Services.AddScoped<IGetRelationshipsQuery, GetRelationshipsQueryHandler>();
builder.Services.AddScoped<ICreateRelationshipCommand, CreateRelationshipCommandHandler>();
builder.Services.AddScoped<IUpdateRelationshipCommand, UpdateRelationshipCommandHandler>();
builder.Services.AddScoped<IDeleteRelationshipCommand, DeleteRelationshipCommandHandler>();

builder.Services.AddScoped<IRunSimulationCommand, RunSimulationCommandHandler>();
builder.Services.AddScoped<IStartContinuousRunCommand, StartContinuousRunCommandHandler>();
builder.Services.AddScoped<IStopSimulationRunCommand, StopSimulationRunCommandHandler>();
builder.Services.AddHostedService<SimulationEngineService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Factory MES API",
        Version = "v1",
        Description = "Factory MES 시스템 REST API"
    });
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Factory MES API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.MapControllers();

app.Run();

public partial class Program { }
