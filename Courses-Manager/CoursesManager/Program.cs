using CoursesManager.Clients;
using CoursesManager.Repository;
using CoursesManager.Settings;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Читання конфігурації MongoDBSettings з файлу appsettings.json
var mongoDBSettings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDBSettings>();

// Перевірка, чи успішно прочитані налаштування
if (mongoDBSettings == null)
{
    throw new InvalidOperationException("MongoDB settings are not configured properly.");
}

// Реєстрація MongoDBSettings як Singleton
builder.Services.AddSingleton(mongoDBSettings);

// Реєстрація репозиторію і сервісу
builder.Services.AddSingleton<MongoDBRepository>(); // Репозиторій для MongoDB
builder.Services.AddScoped<IRepository, CourseRepository>(); // Репозиторій для курсів

builder.Services.AddHttpClient<InstructorManagerClient>()
    .AddPolicyHandler(GetCircuitBreakerPolicy());
builder.Services.AddHttpClient<StudentManagerClient>()
    .AddPolicyHandler(GetCircuitBreakerPolicy());
// builder.Services.AddHttpClient<TestManagerClient>();

builder.Services.AddControllers();

// Налаштування Swagger/OpenAPI
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