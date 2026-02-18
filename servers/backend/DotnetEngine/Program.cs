using DotnetEngine.Application.Health.Handlers;
using DotnetEngine.Application.Health.Ports;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IGetHealthQuery, GetHealthQueryHandler>();

var app = builder.Build();

app.MapControllers();

app.Run();

// WebApplicationFactory에서 어셈블리를 참조하기 위해 노출
public partial class Program { }
