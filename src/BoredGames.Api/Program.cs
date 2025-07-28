using System.Text.Json.Serialization;
using BoredGames.Services;
using BoredGames;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
//builder.Logging.ClearProviders();

// Add .NET services here
builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(ConfigureSwagger)
    .AddCors(ConfigureCors)
    .AddControllers()
    .AddJsonOptions(ConfigureJsonOptions);

// Add application services here
builder.Services
    .AddGameConfigs()
    .AddSingleton<RoomManager>()
    .AddHostedService<RoomCleanupService>()
    .AddSingleton<PlayerConnectionManager>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<RoomManager>();
    Console.WriteLine("RoomManager initialized.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowLocalhost3000");
}

app.MapControllers();

app.Run();
return;

void ConfigureCors(CorsOptions options)
{
    options.AddPolicy("AllowLocalhost3000", cpb => cpb.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
}

void ConfigureSwagger(SwaggerGenOptions options)
{
    // Add this line to tell Swagger to also use string enums
    options.AddServer(new Microsoft.OpenApi.Models.OpenApiServer { Url = "http://localhost:5000" });
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });
}

void ConfigureJsonOptions(JsonOptions options)
{
    options.JsonSerializerOptions.TypeInfoResolver = new GameTypeInfoResolver();
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
}