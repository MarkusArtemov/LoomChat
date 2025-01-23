using System.Text;
using De.Hsfl.LoomChat.File.Persistence;
using De.Hsfl.LoomChat.File.Options;
using De.Hsfl.LoomChat.File.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.WriteTo.Console();
});

// JWT Auth
var secret = builder.Configuration["Jwt:Secret"];
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// File storage
var storageRelative = builder.Configuration.GetValue<string>("StorageRoot") ?? "data";
var storageFullPath = Path.Combine(builder.Environment.ContentRootPath, storageRelative);
Directory.CreateDirectory(storageFullPath);

builder.Services.AddSingleton<FileStorageOptions>(new FileStorageOptions
{
    StoragePath = storageFullPath
});

// DB context
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FileDbContext>(options =>
{
    options.UseNpgsql(connString);
});

// FileService
builder.Services.AddScoped<FileService>();

var app = builder.Build();

// Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use authentication/authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
