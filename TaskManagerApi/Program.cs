using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System.Text;
using TaskManagerApi.Services;
using TaskManagerApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ----------------- MongoDB -----------------
var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

var mongoUrl = builder.Configuration["MongoDB:ConnectionString"];
var databaseName = builder.Configuration["MongoDB:DatabaseName"];
var mongoClient = new MongoClient(mongoUrl);
var database = mongoClient.GetDatabase(databaseName);

builder.Services.AddSingleton<IMongoDatabase>(database);
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<TaskService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<FileService>();

// ----------------- JWT -----------------
var jwtSecret = builder.Configuration["JWT:SecretKey"] ?? "your-secret-key-change-in-production";
var jwtKey = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ----------------- Authorization -----------------
builder.Services.AddAuthorization();

// ----------------- CORS -----------------
var corsOrigins = builder.Configuration["CORS:Origins"]?.Split(',') ?? new[] { "*" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length == 1 && corsOrigins[0] == "*")
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// ----------------- Controllers & Swagger -----------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ----------------- Swagger -----------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Manager API v1");
    c.RoutePrefix = string.Empty; // Swagger доступен по /
});

// ----------------- Middleware -----------------
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapControllers();

// ----------------- Динамический порт для Render -----------------
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
