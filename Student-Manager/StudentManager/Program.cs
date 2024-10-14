using Polly;
using Polly.Extensions.Http;
using StudentManager.Clients;
using StudentManager.RabbitMQ;
using StudentManager.Repository;
using StudentManager.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Reading settings fo MongoDB from appsettings.json
var mongoDBSettings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDBSettings>() ?? throw new InvalidOperationException("MongoDB settings are not configured properly.");

builder.Services.AddSingleton(mongoDBSettings);

builder.Services.AddSingleton<MongoDBRepository>();
builder.Services.AddScoped<IRepository, StudentRepository>();

builder.Services.AddHttpClient<CourseServiceClient>()
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHostedService<RabbitMQConsumer>();
builder.Services.AddSingleton<RabbitMQClient>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    Random jitterer = new();

    return HttpPolicyExtensions
        .HandleTransientHttpError() // Handles transient errors such as 5xx and timeout
        .WaitAndRetryAsync( // Exponential backoff with jitter
            retryCount: 2, // Retry up to 3 times
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(2)
                + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)) // Adding jitter: random delay up to 1 second
        )
        .WrapAsync(
            HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 2, // Number of failures before opening the circuit
                durationOfBreak: TimeSpan.FromSeconds(30),  // Initial duration of break
                onBreak: (result, breakDelay) =>
                {
                    Console.WriteLine($"Circuit breaker triggered! Delay: {breakDelay}, Exception: {result.Exception?.Message}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                }
            )
        );
}