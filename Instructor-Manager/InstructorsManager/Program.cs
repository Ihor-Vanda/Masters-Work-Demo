using InstructorsManager.RabbitMQ;
using InstructorsManager.Repository;
using InstructorsManager.Settings;
using ModifiedCB;
using ModifiedCB.Settings;
using Polly;
using Polly.Extensions.Http;
using Prometheus;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Reading settings fo MongoDB from appsettings.json
var mongoDBSettings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDBSettings>() ?? throw new InvalidOperationException("MongoDB settings are not configured properly.");

builder.Services.AddSingleton(mongoDBSettings);

builder.Services.AddSingleton<MongoDBRepository>();
builder.Services.AddScoped<IRepository, InstructorRepository>();

var circuitBreakerSettings = builder.Configuration.GetSection("CircuitBreakerSettings").Get<CircuitBreakerSettings>() ?? throw new InvalidOperationException("CircuitBreaker settings are not configured properly.");
builder.Services.AddSingleton(circuitBreakerSettings);

builder.Services.AddHttpClient<HttpCommunication>();
builder.Services.AddTransient<HttpCommunication>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();

    var circuitBreakerSettings = sp.GetRequiredService<CircuitBreakerSettings>();

    var circuitBreaker = new CircuitBreakerWithRetry(
        circuitBreakerSettings.FailureThreshold,
        TimeSpan.FromSeconds(circuitBreakerSettings.ResetTimeoutSeconds),
        circuitBreakerSettings.RetryAttempts,
        TimeSpan.FromMilliseconds(circuitBreakerSettings.RetryDelayMilliseconds));

    return new HttpCommunication(httpClient, circuitBreaker);
});

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
            rabbitMqCommunication),
        _ => throw new InvalidOperationException($"Unsupported operation mode: {circuitBreakerSettings.OperationMode}")
    };
});

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

app.MapControllers();

app.UseRouting();

app.UseAuthorization();

app.UseHttpMetrics(); // Це додасть стандартні метрики HTTP запитів

app.UseEndpoints(endpoints =>
{
    _ = endpoints.MapControllers();
    _ = endpoints.MapMetrics(); // Це відкриє метрики за /metrics
});

app.Run();