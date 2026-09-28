using Attendance.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:4173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseCors("WebClient");

app.MapGet("/health", () => Results.Ok(new
{
    service = "AnujHRMS API",
    status = "healthy",
    utc = DateTime.UtcNow
}));

app.MapControllers();

app.Run();
