using Chat.API.Extensions;
using Chat.Infrastructure.Extensions;
using Microsoft.OpenApi.Models;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();  // Logs to console
    logging.AddDebug();    // Logs to debug output window
    logging.SetMinimumLevel(LogLevel.Information);
});


// Add services to the container.
builder.Services.AddControllers();

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Infrastructure services (WebPubSub, CosmosDB)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Chat API", Version = "v1" });

    // Add JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});
// Add CORS policy for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("Authorization"));
});

var app = builder.Build();

// Test metric at startup
var startupCounter = Metrics.CreateCounter("chat_startup_counter", "Counter incremented at startup");
startupCounter.Inc();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.UseHttpsRedirection();

// Use CORS before auth
app.UseCors("AllowAll");

app.UseHttpMetrics(); // Must come after UseRouting but before UseEndpoints
app.UseMetricServer(); // Exposes the /metrics endpoint

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();