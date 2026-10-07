using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure;

public static class DatabaseMigration
{
    // Bekleyen migration'ları uygular. Docker ve testlerde "Database:MigrateOnStartup=true" ile açılır;
    // geliştirmede migration'ları "dotnet ef database update" ile elle uygulamayı tercih ediyoruz.
    //
    // Not: API birden fazla kopya halinde aynı anda açılırsa hepsi migration denemesin diye,
    // gerçek ortamda migration'ı ayrı bir adımda (ör. CI/CD'de) çalıştırmak daha güvenlidir.
    public static void MigrateDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}
