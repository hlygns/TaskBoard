using System.Text;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using StackExchange.Redis;
using TaskBoard.API.ErrorHandling;
using TaskBoard.API.Realtime;
using TaskBoard.API.Services;
using TaskBoard.Application;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var signalR = builder.Services.AddSignalR();
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddSingleton<IPresenceTracker, InMemoryPresenceTracker>();
}
else
{
    // Backplane: API birden fazla sunucuda çalışırsa, bir sunucudaki SignalR mesajı Redis pub/sub
    // üzerinden diğer sunuculara bağlı istemcilere de ulaşır.
    signalR.AddStackExchangeRedis(redisConnection, o =>
    {
        o.Configuration.ChannelPrefix = RedisChannel.Literal("taskboard");
        o.Configuration.AbortOnConnectFail = false;
    });
    builder.Services.AddSingleton<IPresenceTracker, RedisPresenceTracker>();
}

// Panoyu değiştiren her işlem önce cache'i temizler, sonra SignalR ile haber verir (decorator).
builder.Services.AddScoped<SignalRBoardNotifier>();
builder.Services.AddScoped<IBoardNotifier>(sp => new CacheInvalidatingBoardNotifier(
    sp.GetRequiredService<IBoardCache>(),
    sp.GetRequiredService<SignalRBoardNotifier>()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt ayarları bulunamadı.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Claim isimlerini olduğu gibi bırak ("sub" → uzun .NET URI'sine çevrilmesin).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
            // Varsayılan 5 dakikalık tolerans 15 dakikalık token için fazla.
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            // Tarayıcı WebSocket bağlantısına Authorization başlığı ekleyemez; SignalR istemcisi
            // token'ı ?access_token= ile gönderir. Bunu sadece hub adresleri için kabul ediyoruz.
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = token;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers()
    // Enum'lar JSON'da sayı yerine isimle: "role": "Owner"
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    app.Services.MigrateDatabase();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // API'yi tarayıcıdan denemek için: /scalar
    app.MapScalarApiReference();
    // Arka plan işlerini (kuyruk, düzenli işler, hatalar) görmek için: /hangfire
    // Varsayılan olarak sadece aynı makineden (localhost) erişilebilir.
    app.UseHangfireDashboard("/hangfire");
}

app.Services.ScheduleRecurringJobs();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<BoardHub>("/hubs/board");

app.Run();

// Entegrasyon testlerinde WebApplicationFactory<Program> ile uygulamayı ayağa kaldırabilmek için.
public partial class Program;
