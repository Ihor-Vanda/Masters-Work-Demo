using CoursesManager.Clients;
using CoursesManager.RabbitMQ;
using CoursesManager.Repository;
using CoursesManager.Settings;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

var mongoDBSettings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDBSettings>() ?? throw new InvalidOperationException("MongoDB settings are not configured properly.");

builder.Services.AddSingleton(mongoDBSettings);

builder.Services.AddSingleton<MongoDBRepository>();
builder.Services.AddScoped<IRepository, CourseRepository>();

builder.Services.AddHttpClient<InstructorManagerClient>()
    .AddPolicyHandler(GetCircuitBreakerPolicy());
builder.Services.AddHttpClient<StudentManagerClient>()
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHostedService<RabbitMQConsumer>();
builder.Services.AddSingleton<RabbitMQClient>();

builder.Services.AddControllers();

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
            retryCount: 3, // Retry up to 3 times
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))  // Exponential Backoff: 2^retryAttempt
                + TimeSpan.FromMilliseconds(jitterer.Next(0, 5000)) // Adding jitter: random delay up to 1 second
        )
        .WrapAsync(
            HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3, // Number of failures before opening the circuit
                durationOfBreak: TimeSpan.FromSeconds(10),  // Initial duration of break
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