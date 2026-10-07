using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Notifications;
using TaskBoard.Infrastructure.Authentication;
using TaskBoard.Infrastructure.Caching;
using TaskBoard.Infrastructure.Email;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("ConnectionStrings:Database ayarı bulunamadı.");

        services.AddDbContext<AppDbContext>(options => options
            // İç içe koleksiyon çeken sorgular (pano → sütunlar → kartlar) tek dev JOIN yerine
            // her koleksiyon için ayrı SQL ile çalışsın; satır tekrarı (kartezyen patlama) olmasın.
            .UseNpgsql(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.Secret.Length >= 32, "Jwt:Secret en az 32 karakter olmalı.")
            .ValidateOnStart();

        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton(TimeProvider.System);

        AddEmail(services, configuration);
        AddBackgroundJobs(services, connectionString);
        AddRedis(services, configuration.GetConnectionString("Redis"));

        return services;
    }

    private static void AddRedis(IServiceCollection services, string? redisConnection)
    {
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IBoardCache, NoBoardCache>();
            return;
        }

        // Tüm uygulama tek bir Redis bağlantısını paylaşır (StackExchange.Redis önerisi).
        // AbortOnConnectFail=false: Redis kapalıyken de uygulama açılır, Redis gelince kendisi bağlanır.
        var options = ConfigurationOptions.Parse(redisConnection);
        options.AbortOnConnectFail = false;
        options.ConnectTimeout = 2000;
        var multiplexer = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);

        services.AddStackExchangeRedisCache(o =>
        {
            o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(multiplexer);
            o.InstanceName = "taskboard:";
        });
        services.AddSingleton<IBoardCache, RedisBoardCache>();
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        var smtp = configuration.GetSection(SmtpOptions.SectionName);
        services.Configure<SmtpOptions>(smtp);

        // SMTP sunucusu ayarlıysa gerçek gönderim, değilse terminale yazma.
        if (string.IsNullOrWhiteSpace(smtp["Host"]))
            services.AddTransient<IEmailSender, LoggingEmailSender>();
        else
            services.AddTransient<IEmailSender, SmtpEmailSender>();

        services.AddScoped<IEmailService, EmailService>();
    }

    private static void AddBackgroundJobs(IServiceCollection services, string connectionString)
    {
        // Hangfire işleri aynı PostgreSQL'de, ayrı "hangfire" şemasında saklar (tabloları kendisi oluşturur).
        // İşler veritabanında durduğu için API yeniden başlasa da kaybolmaz; başarısız olanlar tekrar denenir.
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        services.AddScoped<DueDateReminderJob>();
        services.AddScoped<DailyDigestJob>();
        services.AddScoped<ActivityCleanupJob>();
    }
}
