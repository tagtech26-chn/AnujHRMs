var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    service = "AnujHRMS API",
    status = "healthy",
    utc = DateTime.UtcNow
}));

app.MapControllers();

app.Run();
