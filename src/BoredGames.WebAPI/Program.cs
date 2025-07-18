using System.Text.Json.Serialization;
using BoredGames;

var appBuilder = WebApplication.CreateBuilder(args);
//appBuilder.Logging.ClearProviders();

// Add services to the container.
appBuilder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // This converter tells the serializer to handle all enums as strings
        options.JsonSerializerOptions.TypeInfoResolver = new GameSnapshotTypeResolver();
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

appBuilder.Services.AddEndpointsApiExplorer();
appBuilder.Services.AddSwaggerGen(options =>
{
    // Add this line to tell Swagger to also use string enums
    options.AddServer(new Microsoft.OpenApi.Models.OpenApiServer { Url = "http://localhost:5000" });
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });
});

appBuilder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

var app = appBuilder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowLocalhost3000");
}

app.MapControllers();

// Handle application shutdown
app.Services.GetRequiredService<IHostApplicationLifetime>()
    .ApplicationStopping.Register(RoomManager.StopService);

app.Run();