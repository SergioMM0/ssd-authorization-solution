using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ssd_authorization_solution;
using ssd_authorization_solution.Services;
using ssd_authorization_solution.Util;

// Ensure this matches the correct namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AppDb");
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<DbSeeder>();

var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);
var jwtSettings = jwtSettingsSection.Get<JwtSettings>();

// Register JwtSettings in DI
builder.Services.AddSingleton(jwtSettings);

// Register JwtTokenService
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddControllers();

// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Authentication configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EditorPolicy", policy => policy.RequireRole("Editor"));
    options.AddPolicy("WriterPolicy", policy => policy.RequireRole("Writer", "Journalist"));
    options.AddPolicy("RegisteredUserPolicy", policy => policy.RequireRole("Subscriber", "RegisteredUser"));
});

// Identity configuration
builder.Services.AddIdentityApiEndpoints<IdentityUser>(options => { options.SignIn.RequireConfirmedAccount = false; })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync().Wait();
}

// Swagger setup
app.UseSwagger();
app.UseSwaggerUI();

// Middleware pipeline
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapIdentityApi<IdentityUser>();

app.Run();
