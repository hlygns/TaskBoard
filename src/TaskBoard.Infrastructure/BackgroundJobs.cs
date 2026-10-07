using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Notifications;

namespace TaskBoard.Infrastructure;

public static class BackgroundJobs
{
    // Uygulama her açıldığında düzenli işleri (yeniden) tanımlar; AddOrUpdate olduğu için çoğalmazlar.
    public static void ScheduleRecurringJobs(this IServiceProvider services)
    {
        var jobs = services.GetRequiredService<IRecurringJobManager>();
        var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul") };

        jobs.AddOrUpdate<DueDateReminderJob>("due-date-reminders", job => job.RunAsync(CancellationToken.None),
            Cron.Hourly(), options);

        jobs.AddOrUpdate<DailyDigestJob>("daily-digest", job => job.RunAsync(CancellationToken.None),
            Cron.Daily(8), options);

        jobs.AddOrUpdate<ActivityCleanupJob>("activity-cleanup", job => job.RunAsync(CancellationToken.None),
            Cron.Weekly(DayOfWeek.Sunday, 3), options);
    }
}
