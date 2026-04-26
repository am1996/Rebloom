using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<Rebloom.Data.AppDbContext>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=rebloom.db"));
builder.Services.AddScoped<Rebloom.Services.IEmailService, Rebloom.Services.EmailService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || true)
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapControllers();

// Ensure database created
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<Rebloom.Data.AppDbContext>();
db.Database.EnsureCreated();

app.Run();

