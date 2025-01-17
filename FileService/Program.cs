using De.Hsfl.LoomChat.File.Options;
using De.Hsfl.LoomChat.File.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.WriteTo.Console();
});

// Read relative path from appsettings.json
var storageRelative = builder.Configuration.GetValue<string>("StorageRoot")  ?? "data";

// Build full path
var storageFullPath = Path.Combine(builder.Environment.ContentRootPath, storageRelative);

// Create directory if not exists
Directory.CreateDirectory(storageFullPath);

// Register FileStorageOptions to DI
builder.Services.AddSingleton<FileStorageOptions>(new FileStorageOptions
{
    StoragePath = storageFullPath
});

var connString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register the database context
builder.Services.AddDbContext<FileDbContext>(options =>
{
    options.UseNpgsql(connString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
