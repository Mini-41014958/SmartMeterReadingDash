using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using SmartMeterReadingDash.Models.Dashboard;
using SmartMeterReadingDash.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Oracle connection service
builder.Services.AddScoped<OracleCon>();
builder.Services.AddScoped<Dashboard>();
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();
var jwtSettings = builder.Configuration
    .GetSection("Jwt")
    .Get<JWTSettings>();

if (jwtSettings == null ||
    string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException(
        "JWT configuration is missing."
    );
}

builder.Services.Configure<JWTSettings>(
    builder.Configuration.GetSection("Jwt")
);

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        jwtSettings.Key
                    )
                ),

            ValidateIssuer = true,

            ValidIssuer =
                jwtSettings.Issuer,

            ValidateAudience = true,

            ValidAudience =
                jwtSettings.Audience,

            ValidateLifetime = true,

            ClockSkew =
                TimeSpan.FromMinutes(1)
        };


    // =========================================================
    // READ JWT FROM COOKIE
    // =========================================================

    options.Events =
        new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token =
                    context.Request.Cookies[
                        "SmartMeterAuth"
                    ];

                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();


app.MapControllers();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.Run();
