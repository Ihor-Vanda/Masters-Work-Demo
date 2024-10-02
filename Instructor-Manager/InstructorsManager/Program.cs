using InstructorsManager.Clients;
using InstructorsManager.Repository;
using InstructorsManager.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Reading settings fo MongoDB from appsettings.json
var mongoDBSettings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDBSettings>() ?? throw new InvalidOperationException("MongoDB settings are not configured properly.");

builder.Services.AddSingleton(mongoDBSettings);

builder.Services.AddSingleton<MongoDBRepository>();
builder.Services.AddScoped<IRepository, InstructorRepository>();

builder.Services.AddHttpClient<CourseServiceClient>();

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
