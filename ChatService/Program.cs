using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using De.Hsfl.LoomChat.Chat.Persistence;
using De.Hsfl.LoomChat.Chat.Services;
using De.Hsfl.LoomChat.Chat.Hubs;
using De.Hsfl.LoomChat.Chat.Mappings;
using De.Hsfl.LoomChat.Common.dtos;

var builder = WebApplication.CreateBuilder(args);

// Serilog konfigurieren
builder.Host.UseSerilog((context, loggerConfig) =>
{
    // Mindest-Level, hier z.B. Debug oder Info, je nach Bedarf
    loggerConfig
        .MinimumLevel.Debug()
        .WriteTo.Console();
});

// EF Core
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    options.UseNpgsql(connString);
});

// AutoMapper - scans assembly for profiles
builder.Services.AddAutoMapper(typeof(ChatMappingProfile).Assembly);

// Services
builder.Services.AddScoped<ChatService>();

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not found in config.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        // SignalR-Token auch aus QueryString akzeptieren
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// REST + SignalR
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Automatisch Migrationen ausf�hren
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    db.Database.Migrate();
}

// Middlewares
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map SignalR Hub
app.MapHub<ChatHub>("/chatHub");

// Weil SIgnalR mit nginx nicht funktioniert hat
int publicPort = int.Parse(Environment.GetEnvironmentVariable("PUBLIC_PORT") ?? "8080");
app.MapGet("/port", () => Results.Ok(new PortResponse(publicPort)));

app.Run();
