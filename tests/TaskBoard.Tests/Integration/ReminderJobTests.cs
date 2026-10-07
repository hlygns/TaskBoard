using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Cards;
using TaskBoard.Application.Notifications;

namespace TaskBoard.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ReminderJobTests(TaskBoardApiFactory api)
{
    [Fact]
    public async Task Due_date_reminder_is_sent_once_and_only_for_cards_due_soon()
    {
        var user = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        await user.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Yarın teslim", DueDate: tomorrow, AssigneeId: user.Id));
        await user.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Gelecek hafta", DueDate: tomorrow.AddDays(6), AssigneeId: user.Id));
        await user.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Atanmamış", DueDate: tomorrow));
        // "Bitti" sütunundaki kart tamamlanmış sayılır, hatırlatılmaz.
        await user.CreateCardAsync(board.Columns[2].Id, new CreateCardRequest("Zaten bitti", DueDate: tomorrow, AssigneeId: user.Id));

        await RunReminderJob();
        var reminders = api.Emails.Reminders.Where(r => r.ToEmail == user.Email).ToList();

        var reminder = Assert.Single(reminders);
        Assert.Equal(["Yarın teslim"], reminder.Cards.Select(c => c.CardTitle));

        // İş saatte bir çalışır; aynı kart için ikinci kez mail gitmemeli.
        await RunReminderJob();
        Assert.Single(api.Emails.Reminders, r => r.ToEmail == user.Email);
    }

    private async Task RunReminderJob()
    {
        using var scope = api.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DueDateReminderJob>().RunAsync(CancellationToken.None);
    }
}
