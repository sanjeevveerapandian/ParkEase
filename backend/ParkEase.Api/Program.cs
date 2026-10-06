using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ParkEase.Api.Authorization;
using ParkEase.Api.Data;
using ParkEase.Api.Services;
using ParkEase.Api.Services.Notifications;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection("Booking"));
builder.Services.Configure<SmsOptions>(builder.Configuration.GetSection("Sms"));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IParkingLotService, ParkingLotService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IReportService, ReportService>();

// One NotificationService instance serves both the dispatch and the driver-inbox interfaces.
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<INotificationService>(sp => sp.GetRequiredService<NotificationService>());
builder.Services.AddScoped<INotificationInboxService>(sp => sp.GetRequiredService<NotificationService>());

builder.Services.AddHttpClient<IEmailSender, BrevoEmailSender>();

// SMS provider is chosen by config (Sms:Provider). Without gateway credentials the simulated
// "Log" provider is used, so the flow works end to end and messages still show up in the app.
var smsOptions = builder.Configuration.GetSection("Sms").Get<SmsOptions>() ?? new SmsOptions();
if (smsOptions.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase) && smsOptions.Twilio.IsConfigured)
{
    builder.Services.AddHttpClient<ISmsSender, TwilioSmsSender>();
}
else
{
    if (smsOptions.Provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
        Console.WriteLine("WARNING: Sms:Provider is 'Twilio' but Sms:Twilio credentials are incomplete — falling back to simulated SMS.");
    builder.Services.AddSingleton<ISmsSender, LogSmsSender>();
}

builder.Services.AddHostedService<BookingMaintenanceWorker>();

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.DriverOnly, p => p.RequireRole("Driver"));
    options.AddPolicy(AuthPolicies.OperatorOnly, p => p.RequireRole("Operator"));
    options.AddPolicy(AuthPolicies.AdminOnly, p => p.RequireRole("Admin"));
    options.AddPolicy(AuthPolicies.OperatorOrAdmin, p => p.RequireRole("Operator", "Admin"));
    options.AddPolicy(AuthPolicies.AnyAuthenticatedUser, p => p.RequireAuthenticatedUser());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ParkEase API",
        Version = "v1",
        Description = "Full-stack Smart Parking Management System — real-time slot booking, " +
                       "role-based access (Driver/Operator/Admin), automated billing and email notifications.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Sanjeev Veerapandian",
            Email = "sanjeevveerapandian@gmail.com"
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        policy.WithOrigins(origins is { Length: > 0 } ? origins : new[] { "http://localhost:5173" })
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseMiddleware<ParkEase.Api.Middleware.GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");


if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // In Development the schema is brought up to date automatically, so a fresh clone (or a
    // pulled schema change) just works. Elsewhere, run `dotnet ef database update` deliberately.
    if (app.Environment.IsDevelopment())
        db.Database.Migrate();

    DbSeeder.Seed(db);
}

app.Run();