using System.Text.Json.Serialization;

var appBuilder = WebApplication.CreateBuilder(args);
//appBuilder.Logging.ClearProviders();

// Add services to the container.
appBuilder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // This converter tells the serializer to handle all enums as strings
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
appBuilder.Services.AddOpenApi();

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
    app.UseCors("AllowLocalhost3000");
}

app.MapControllers();
app.Run();