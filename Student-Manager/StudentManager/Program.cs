using System.Net;
using ModifiedCB;
using ModifiedCB.CircuitBreaker;
using ModifiedCB.Settings;
using Prometheus;
using RabbitMQ.Client;
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

var circuitBreakerSettings = builder.Configuration.GetSection("CircuitBreakerSettings").Get<CircuitBreakerSettings>() ?? throw new InvalidOperationException("CircuitBreaker settings are not configured properly.");
builder.Services.AddSingleton(circuitBreakerSettings);

builder.Services.AddHttpClient<HttpCommunication>();
builder.Services.AddTransient<HttpCommunication>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var circuitBreakerSettings = sp.GetRequiredService<CircuitBreakerSettings>();

    var retryAttemp = circuitBreakerSettings.RetryAttempts;
    var retryTimeout = circuitBreakerSettings.RetryDelayMilliseconds;

    var circuitBreaker = new CircuitBreakerWithCallBack(
        circuitBreakerSettings.FailureThreshold,
        TimeSpan.FromSeconds(circuitBreakerSettings.ResetTimeoutSeconds));

    circuitBreaker.AddErrorCallback(HttpStatusCode.BadRequest, () => Console.WriteLine("Get Bad request from target service"));
    circuitBreaker.AddErrorCallback(HttpStatusCode.NotFound, () => Console.WriteLine("Get Not found from target service"));

    return new HttpCommunication(httpClient, circuitBreaker, retryAttemp, TimeSpan.FromMilliseconds(retryTimeout));
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