using System.Text;
using De.Hsfl.LoomChat.File.Persistence;
using De.Hsfl.LoomChat.File.Options;
using De.Hsfl.LoomChat.File.Services;
using De.Hsfl.LoomChat.File.Hubs; // <-- Wichtig für FileHub
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR; // SignalR
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Controllers + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Serilog ---
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.WriteTo.Console();
});

// --- JWT Auth ---
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

// --- FileStorage-Option anlegen ---
var storageRelative = builder.Configuration.GetValue<string>("StorageRoot") ?? "data";
var storageFullPath = Path.Combine(builder.Environment.ContentRootPath, storageRelative);
Directory.CreateDirectory(storageFullPath);

builder.Services.AddSingleton<FileStorageOptions>(new FileStorageOptions
{
    StoragePath = storageFullPath
});

// --- DB-Context ---
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FileDbContext>(options =>
{
    options.UseNpgsql(connString);
});

// --- SignalR registrieren ---
builder.Services.AddSignalR();

// --- FileService (nutzt IHubContext<FileHub>) ---
builder.Services.AddScoped<FileService>();

var app = builder.Build();

// --- ggf. Migrationen automatisiert anstoßen ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FileDbContext>();
    db.Database.Migrate();
}

// --- Swagger, nur im Development ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- Auth/Authorization ---
app.UseAuthentication();
app.UseAuthorization();

// --- Mappe Controller-Routen ---
app.MapControllers();

// --- Mappe den FileHub für Echtzeit-Kommunikation ---
app.MapHub<FileHub>("/fileHub");

app.Run();
