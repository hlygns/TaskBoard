using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Notifications;
using Testcontainers.PostgreSql;

namespace TaskBoard.Tests.Integration;

// Gerçek API'yi bellek içinde, Docker'da açılan geçici bir PostgreSQL ile çalıştırır.
// Tüm entegrasyon testleri aynı örneği paylaşır (bkz. ApiCollection); her test kendi
// kullanıcılarını rastgele e-postalarla oluşturduğu için birbirini etkilemez.
public class TaskBoardApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17").Build();

    public FakeEmailService Emails { get; } = new();

    public Task InitializeAsync() => _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _database.GetConnectionString());
        builder.UseSetting("Jwt:Secret", "integration-test-secret-0123456789-abcdefgh");
        builder.UseSetting("Database:MigrateOnStartup", "true");

        // Redis ayarı verilmediği için bellek içi presence ve cache'siz çalışır.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
        });
    }

    // Refresh token cookie'si "Secure" olduğu için istemci https adresiyle konuşmalı.
    public HttpClient CreateHttpsClient(bool handleCookies = true) => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = handleCookies
    });
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<TaskBoardApiFactory>
{
    public const string Name = "api";
}

// Mail göndermek yerine kaydeder; testler davet token'ını ve hatırlatmaları buradan okur.
public class FakeEmailService : IEmailService
{
    public ConcurrentQueue<(string ToEmail, string Token)> Invitations { get; } = new();
    public ConcurrentQueue<DueDateReminderEmail> Reminders { get; } = new();
    public ConcurrentQueue<DailyDigestEmail> Digests { get; } = new();

    public Task SendBoardInvitationAsync(
        string toEmail, string inviterName, string boardName, string invitationToken, CancellationToken ct = default)
    {
        Invitations.Enqueue((toEmail, invitationToken));
        return Task.CompletedTask;
    }

    public Task SendDueDateReminderAsync(DueDateReminderEmail email, CancellationToken ct = default)
    {
        Reminders.Enqueue(email);
        return Task.CompletedTask;
    }

    public Task SendDailyDigestAsync(DailyDigestEmail email, CancellationToken ct = default)
    {
        Digests.Enqueue(email);
        return Task.CompletedTask;
    }

    public string LastInvitationTokenFor(string email) =>
        Invitations.Last(i => i.ToEmail == email.ToLowerInvariant()).Token;
}
