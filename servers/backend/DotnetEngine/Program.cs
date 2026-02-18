using DotnetEngine.Application.Asset.Handlers;
using DotnetEngine.Application.Asset.Ports;
using DotnetEngine.Application.Health.Handlers;
using DotnetEngine.Application.Health.Ports;
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
var mongoClient = new MongoClient(mongoConnectionString);
var mongoDatabase = mongoClient.GetDatabase("factory_mes");
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
builder.Services.AddScoped<IAssetRepository, MongoAssetRepository>();

builder.Services.AddScoped<IGetAssetsQuery, GetAssetsQueryHandler>();
builder.Services.AddScoped<IGetStatesQuery, GetStatesQueryHandler>();

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
