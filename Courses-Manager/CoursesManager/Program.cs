using CoursesManager.Clients;
using CoursesManager.Repository;
using CoursesManager.Settings;

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

builder.Services.AddHttpClient<InstructorManagerClient>();
builder.Services.AddHttpClient<StudentManagerClient>();
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
