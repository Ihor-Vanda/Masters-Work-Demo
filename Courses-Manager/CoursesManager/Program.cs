using CoursesManager.RabbitMQ;
using CoursesManager.Repository;
using CoursesManager.Settings;
using Polly;
using Polly.Extensions.Http;
using ModifiedCB;
using ModifiedCB.Settings;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

var mongoDBSettings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDBSettings>() ?? throw new InvalidOperationException("MongoDB settings are not configured properly.");
builder.Services.AddSingleton(mongoDBSettings);

builder.Services.AddSingleton<MongoDBRepository>();
builder.Services.AddScoped<IRepository, CourseRepository>();

var circuitBreakerSettings = builder.Configuration.GetSection("CircuitBreakerSettings").Get<CircuitBreakerSettings>() ?? throw new InvalidOperationException("CircuitBreaker settings are not configured properly.");
builder.Services.AddSingleton(circuitBreakerSettings);

builder.Services.AddHttpClient<HttpCommunication>()
                .AddPolicyHandler(GetCircuitBreakerPolicy(circuitBreakerSettings));

builder.Services.AddHostedService<RabbitMQConsumer>();

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = circuitBreakerSettings.RabbitMq.HostName,
        Port = circuitBreakerSettings.RabbitMq.Port,
        UserName = circuitBreakerSettings.RabbitMq.UserName,
        Password = circuitBreakerSettings.RabbitMq.Password
    };
    return factory.CreateConnection();
});
builder.Services.AddSingleton<RabbitMqCommunication>();

builder.Services.AddSingleton<ICommunicationStrategy>(sp =>
{
    var httpCommunication = sp.GetRequiredService<HttpCommunication>();
    var rabbitMqCommunication = sp.GetRequiredService<RabbitMqCommunication>();

    var circuitBreakerSettings = sp.GetRequiredService<CircuitBreakerSettings>();

    return circuitBreakerSettings.OperationMode switch
    {
        "HttpOnly" => httpCommunication, // Тільки HTTP
        "RabbitMqOnly" => rabbitMqCommunication, // Тільки RabbitMQ
        "Combined" => new CBCommunication(
            httpCommunication,
            rabbitMqCommunication,
            TimeSpan.FromMilliseconds(circuitBreakerSettings.RetryDelayMilliseconds),
            circuitBreakerSettings.FailureThreshold,
            TimeSpan.FromSeconds(circuitBreakerSettings.ResetTimeoutSeconds)),
        _ => throw new InvalidOperationException($"Unsupported operation mode: {circuitBreakerSettings.OperationMode}")
    };
});

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


IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(CircuitBreakerSettings circuitBreakerSettings)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: circuitBreakerSettings.RetryAttempts, // Використовуємо кількість спроб з конфігурації
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromMilliseconds(circuitBreakerSettings.RetryDelayMilliseconds) // Використовуємо затримку з конфігурації
        )
        .WrapAsync(
            HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: circuitBreakerSettings.FailureThreshold, // Кількість невдач до відкриття
                durationOfBreak: TimeSpan.FromSeconds(circuitBreakerSettings.ResetTimeoutSeconds),  // Тривалість паузи після збою
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
