using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Cards;
using TaskBoard.Application.Notifications;

namespace TaskBoard.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ReminderJobTests(TaskBoardApiFactory api)
{
    [Fact]
    public async Task Reminder_goes_to_assignee_or_board_owner_and_only_once()
    {
        var owner = await api.RegisterAsync();
        var member = await api.RegisterAsync();
        var board = await owner.CreateBoardAsync();
        await api.AddMemberAsync(owner, board.Id, member);
        var column = board.Columns[0].Id;
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);

        await owner.CreateCardAsync(column, new CreateCardRequest("Mehmet'in işi", DueDate: tomorrow, AssigneeId: member.Id));
        // Atanmamış kart: tek başına kullanımda kimse kendini atamaz; hatırlatma pano sahibine gider.
        await owner.CreateCardAsync(column, new CreateCardRequest("Sahipsiz iş", DueDate: tomorrow));
        await owner.CreateCardAsync(column, new CreateCardRequest("Gelecek hafta", DueDate: tomorrow.AddDays(6), AssigneeId: member.Id));
        var done = await owner.CreateCardAsync(column, new CreateCardRequest("Bitmiş iş", DueDate: tomorrow));
        var archived = await owner.CreateCardAsync(column, new CreateCardRequest("Arşivdeki iş", DueDate: tomorrow));
        await owner.Client.PutAsJsonAsync($"/api/cards/{done.Id}/complete", new SetCardCompletedRequest(true));
        await owner.Client.PutAsJsonAsync($"/api/cards/{archived.Id}/archive", new SetCardArchivedRequest(true));

        await RunReminderJob();

        var toMember = Assert.Single(api.Emails.Reminders, r => r.ToEmail == member.Email);
        var toOwner = Assert.Single(api.Emails.Reminders, r => r.ToEmail == owner.Email);
        Assert.Equal(["Mehmet'in işi"], toMember.Cards.Select(c => c.CardTitle));
        // Tamamlanan ve arşivlenen kartlar hatırlatılmaz.
        Assert.Equal(["Sahipsiz iş"], toOwner.Cards.Select(c => c.CardTitle));

        // İş saatte bir çalışır; aynı kart için ikinci kez mail gitmemeli.
        await RunReminderJob();
        Assert.Single(api.Emails.Reminders, r => r.ToEmail == member.Email);
        Assert.Single(api.Emails.Reminders, r => r.ToEmail == owner.Email);
    }

    private async Task RunReminderJob()
    {
        using var scope = api.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DueDateReminderJob>().RunAsync(CancellationToken.None);
    }
}
