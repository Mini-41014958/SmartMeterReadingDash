using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SmartMeterReadingDash.Models.Dashboard;
using SmartMeterReadingDash.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options => options.SuppressXFrameOptionsHeader = true);

// Application Services
builder.Services.AddScoped<OracleCon>();
builder.Services.AddScoped<Dashboard>();
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<UserAccess>();
builder.Services.AddScoped<ReportGenService>();
builder.Services.AddScoped<EmailService>();


var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JWTSettings>(); 

if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.Key) || string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException("JWT configuration is missing or incomplete.");
}

builder.Services.Configure<JWTSettings>( builder.Configuration.GetSection("Jwt"));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey( Encoding.UTF8.GetBytes(jwtSettings.Key)),

                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromMinutes(1)
            };

        options.Events =
            new JwtBearerEvents
            {
                OnMessageReceived =
                    context =>
                    {
                        var token = context.Request.Cookies[ "SmartMeterAuth" ];

                        if (!string.IsNullOrWhiteSpace( token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },

                OnChallenge =
                    context =>
                    {
                        context.HandleResponse();

                        var request =  context.Request;

                        var response = context.Response;

                        if (request.Path.StartsWithSegments(  "/api",  StringComparison.OrdinalIgnoreCase))
                        {
                            response.StatusCode =  StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }

                        if (request.Path.StartsWithSegments("/Account/Login", StringComparison.OrdinalIgnoreCase))
                        {
                            response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }

                        response.Redirect( $"{request.PathBase}/Account/Login");

                        return Task.CompletedTask;
                    }
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.Use(async (context, next) =>
{
    // Remove the old X-Frame-Options header
    context.Response.Headers.Remove("X-Frame-Options");

    // Correctly allow the exact origin (Scheme + Host + Port ONLY)
    context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'self' http://10.213.225.35:81");

    await next();
});

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);


app.Run();