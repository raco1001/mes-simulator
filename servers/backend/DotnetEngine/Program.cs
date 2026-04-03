using DotnetEngine.Application.Alert.Handlers;
using DotnetEngine.Application.Alert.Ports.Driving;
using DotnetEngine.Application.Alert.Ports.Driven;
using DotnetEngine.Application.Asset.Handlers;
using DotnetEngine.Application.Asset.Ports.Driving;
using DotnetEngine.Application.Asset.Ports.Driven;
using DotnetEngine.Application.Health.Handlers;
using DotnetEngine.Application.Health.Ports;
using DotnetEngine.Application.Relationship.Handlers;
using DotnetEngine.Application.Relationship.Ports.Driving;
using DotnetEngine.Application.Relationship.Ports.Driven;
using DotnetEngine.Application.Simulation.Handlers;
using DotnetEngine.Application.Simulation.Workers;
using DotnetEngine.Application.Simulation.Ports.Driven;
using DotnetEngine.Application.Simulation.Ports.Driving;
using DotnetEngine.Application.Simulation;
using DotnetEngine.Application.Simulation.Rules;
using DotnetEngine.Application.Simulation.Simulators;
using DotnetEngine.Application.ObjectType.Handlers;
using DotnetEngine.Application.ObjectType.Ports.Driving;
using DotnetEngine.Application.ObjectType.Ports.Driven;
using DotnetEngine.Application.LinkType.Handlers;
using DotnetEngine.Application.LinkType.Ports.Driving;
using DotnetEngine.Application.LinkType.Ports.Driven;
using DotnetEngine.Application.Recommendation.Ports.Driven;
using DotnetEngine.Infrastructure.Alert;
using DotnetEngine.Infrastructure.Kafka;
using DotnetEngine.Infrastructure.Mongo;
using DotnetEngine.Infrastructure.Recommendation;
using DotnetEngine.Infrastructure.Simulation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
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

ConventionRegistry.Register(
    "BsonConventions",
    new ConventionPack
    {
        new CamelCaseElementNameConvention(),
        new EnumRepresentationConvention(BsonType.String),
    },
    _ => true
);

builder.Services.AddSingleton<IMongoClient>(
    new MongoClient(mongoConnectionString));

builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase("factory_mes");
});

builder.Services.AddScoped<IAssetRepository, MongoAssetRepository>();
builder.Services.AddScoped<IObjectTypeSchemaRepository, MongoObjectTypeSchemaRepository>();
builder.Services.AddScoped<ILinkTypeSchemaRepository, MongoLinkTypeSchemaRepository>();
builder.Services.AddScoped<IRelationshipRepository, MongoRelationshipRepository>();
builder.Services.AddScoped<ISimulationRunRepository, MongoSimulationRunRepository>();
builder.Services.AddScoped<IEventRepository, MongoEventRepository>();

builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection(KafkaOptions.SectionName));
builder.Services.AddScoped<IEventPublisher, KafkaEventPublisher>();
builder.Services.AddScoped<IRecommendationAppliedPublisher, KafkaRecommendationAppliedPublisher>();
builder.Services.AddScoped<IEngineStateApplier, EngineStateApplier>();
builder.Services.AddHttpClient<IPipelineRecommendationClient, PipelineRecommendationClient>(client =>
{
    var baseUrl = builder.Configuration["Pipeline:BaseUrl"] ?? "http://localhost:8000";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<IAlertStore, MongoAlertStore>();
builder.Services.AddSingleton<IAlertNotifier, SseAlertChannel>();
builder.Services.AddScoped<IGetAlertsQuery, GetAlertsQueryHandler>();
builder.Services.AddHostedService<KafkaAlertConsumerService>();

builder.Services.AddScoped<IPropagationRule, SuppliesRule>();
builder.Services.AddScoped<IPropagationRule, ContainsRule>();
builder.Services.AddScoped<IPropagationRule, ConnectedToRule>();
builder.Services.AddSingleton<IPropertySimulator, ConstantSimulator>();
builder.Services.AddSingleton<IPropertySimulator, SettableSimulator>();
builder.Services.AddSingleton<IPropertySimulator, RateSimulator>();
builder.Services.AddSingleton<IPropertySimulator, AccumulatorSimulator>();
builder.Services.AddSingleton<IPropertySimulator, DerivedSimulator>();

builder.Services.AddScoped<IGetAssetsQuery, GetAssetsQueryHandler>();
builder.Services.AddScoped<IGetStatesQuery, GetStatesQueryHandler>();
builder.Services.AddScoped<ICreateAssetCommand, CreateAssetCommandHandler>();
builder.Services.AddScoped<IUpdateAssetCommand, UpdateAssetCommandHandler>();
builder.Services.AddScoped<IDeleteAssetCommand, DeleteAssetCommandHandler>();
builder.Services.AddScoped<IGetObjectTypeSchemasQuery, GetObjectTypeSchemasQueryHandler>();
builder.Services.AddScoped<IGetObjectTypeSchemaQuery, GetObjectTypeSchemaQueryHandler>();
builder.Services.AddScoped<ICreateObjectTypeSchemaCommand, CreateObjectTypeSchemaCommandHandler>();
builder.Services.AddScoped<IUpdateObjectTypeSchemaCommand, UpdateObjectTypeSchemaCommandHandler>();
builder.Services.AddScoped<IDeleteObjectTypeSchemaCommand, DeleteObjectTypeSchemaCommandHandler>();
builder.Services.AddScoped<IGetLinkTypeSchemasQuery, GetLinkTypeSchemasQueryHandler>();
builder.Services.AddScoped<IGetLinkTypeSchemaQuery, GetLinkTypeSchemaQueryHandler>();
builder.Services.AddScoped<ICreateLinkTypeSchemaCommand, CreateLinkTypeSchemaCommandHandler>();
builder.Services.AddScoped<IUpdateLinkTypeSchemaCommand, UpdateLinkTypeSchemaCommandHandler>();

builder.Services.AddScoped<IGetRelationshipsQuery, GetRelationshipsQueryHandler>();
builder.Services.AddScoped<ICreateRelationshipCommand, CreateRelationshipCommandHandler>();
builder.Services.AddScoped<IUpdateRelationshipCommand, UpdateRelationshipCommandHandler>();
builder.Services.AddScoped<IDeleteRelationshipCommand, DeleteRelationshipCommandHandler>();

builder.Services.AddSingleton<ISimulationNotifier, SseSimulationChannel>();
builder.Services.AddScoped<IRunSimulationCommand, RunSimulationCommandHandler>();
builder.Services.AddScoped<IWhatIfSimulationQuery, WhatIfSimulationQueryHandler>();
builder.Services.AddScoped<IStartContinuousRunCommand, StartContinuousRunCommandHandler>();
builder.Services.AddScoped<IStopSimulationRunCommand, StopSimulationRunCommandHandler>();
builder.Services.AddScoped<IReplayRunCommand, ReplayRunCommandHandler>();
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
