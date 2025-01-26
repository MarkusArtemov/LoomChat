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

var builder = WebApplication.CreateBuilder(args);

// Serilog konfigurieren (Log-Ausgabe)
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .MinimumLevel.Debug()
        .WriteTo.Console();
});

// Connection-String laden (z. B. PostgreSQL)
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    options.UseNpgsql(connString);
});

// AutoMapper-Profil einscannen
builder.Services.AddAutoMapper(typeof(ChatMappingProfile).Assembly);

// Registriere deine Services
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<PollService>(); 


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

        // SignalR-Token auch über QueryString akzeptieren
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
builder.Services.AddSwaggerGen(); // optional für Swagger

var app = builder.Build();

// Automatisch EF-Migrationen ausführen (optional)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    db.Database.Migrate();
}

// Development-spezifische Features
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

// Auth & Authorization in der Middleware-Pipeline
app.UseAuthentication();
app.UseAuthorization();

// Controllers freischalten
app.MapControllers();

// SignalR-Hubs
app.MapHub<ChatHub>("/chatHub");
app.MapHub<PollHub>("/pollHub");  

// Start der Anwendung
app.Run();
