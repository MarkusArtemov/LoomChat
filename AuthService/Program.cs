using Serilog;
using Microsoft.EntityFrameworkCore;
using De.Hsfl.LoomChat.Auth.Persistence;
using De.Hsfl.LoomChat.Auth.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.WriteTo.Console();
});

var connString = builder.Configuration.GetConnectionString("DefaultConnection");

// Register the database context
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseNpgsql(connString);
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<JwtUtils>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply EF Core migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    Console.WriteLine("Migrating Database: " + db);
    db.Database.Migrate();
}

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
