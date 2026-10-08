using System.Net;
using System.Net.Http.Json;
using TaskBoard.Application.Boards;
using TaskBoard.Application.Cards;
using TaskBoard.Application.Labels;
using TaskBoard.Application.Tasks;

namespace TaskBoard.Tests.Integration;

// Kişisel kullanım özellikleri: şablonlar, etiketler, alt görevler, tamamlandı, arşiv, Görevlerim.
[Collection(ApiCollection.Name)]
public class PersonalFeatureTests(TaskBoardApiFactory api)
{
    [Fact]
    public async Task Template_creates_its_columns_and_labels()
    {
        var user = await api.RegisterAsync();

        var board = await user.CreateBoardAsync("Uygulama", template: "software");

        Assert.Equal(["Backlog", "Yapılacak", "Yapılıyor", "Test", "Bitti"], board.Columns.Select(c => c.Name));
        Assert.Equal(["Bug", "İyileştirme", "Özellik", "Teknik borç"], board.Labels.Select(l => l.Name).Order());
    }

    [Fact]
    public async Task Unknown_template_returns_400()
    {
        var user = await api.RegisterAsync();

        var response = await user.Client.PostAsJsonAsync("/api/boards", new CreateBoardRequest("X", null, "yok-boyle-sablon"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Checklist_progress_appears_on_the_card()
    {
        var user = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();
        var card = await user.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Sunum"));

        var items = new List<ChecklistItemDto>();
        foreach (var text in new[] { "Taslak", "Görseller", "Prova" })
            items.Add(await (await user.Client.PostAsJsonAsync($"/api/cards/{card.Id}/checklist", new AddChecklistItemRequest(text)))
                .ReadAsync<ChecklistItemDto>());
        await user.Client.PatchAsJsonAsync($"/api/checklist/{items[0].Id}", new UpdateChecklistItemRequest(IsDone: true));
        await user.Client.PatchAsJsonAsync($"/api/checklist/{items[1].Id}", new UpdateChecklistItemRequest(IsDone: true));

        var summary = (await user.GetBoardAsync(board.Id)).Columns[0].Cards.Single();
        Assert.Equal((2, 3), (summary.ChecklistDone, summary.ChecklistTotal));

        var detail = await user.Client.GetFromJsonAsync<CardDetailDto>($"/api/cards/{card.Id}", TestApi.Json);
        Assert.Equal(["Taslak", "Görseller", "Prova"], detail!.Checklist.Select(i => i.Text));
    }

    [Fact]
    public async Task Only_labels_of_the_same_board_can_be_attached()
    {
        var user = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();
        var otherBoard = await user.CreateBoardAsync("Diğer", template: "school");
        var card = await user.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Kart"));
        var label = await (await user.Client.PostAsJsonAsync($"/api/boards/{board.Id}/labels", new SaveLabelRequest("Acil iş", "red")))
            .ReadAsync<LabelDto>();

        var foreign = await user.Client.PutAsJsonAsync($"/api/cards/{card.Id}/labels",
            new SetCardLabelsRequest([otherBoard.Labels[0].Id]));
        var own = await user.Client.PutAsJsonAsync($"/api/cards/{card.Id}/labels", new SetCardLabelsRequest([label.Id]));

        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, own.StatusCode);
        Assert.Equal([label.Id], (await user.GetBoardAsync(board.Id)).Columns[0].Cards.Single().LabelIds);
    }

    [Fact]
    public async Task Label_names_are_unique_per_board_case_insensitively()
    {
        var user = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();
        await user.Client.PostAsJsonAsync($"/api/boards/{board.Id}/labels", new SaveLabelRequest("Okul", "indigo"));

        var duplicate = await user.Client.PostAsJsonAsync($"/api/boards/{board.Id}/labels", new SaveLabelRequest("okul", "green"));
        var badColor = await user.Client.PostAsJsonAsync($"/api/boards/{board.Id}/labels", new SaveLabelRequest("Ev", "#ff0000"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badColor.StatusCode);
    }

    [Fact]
    public async Task Archived_card_is_hidden_and_restored_to_the_end_of_its_column()
    {
        var user = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();
        var column = board.Columns[0].Id;
        var a = await user.CreateCardAsync(column, new CreateCardRequest("A"));
        await user.CreateCardAsync(column, new CreateCardRequest("B"));

        await user.Client.PutAsJsonAsync($"/api/cards/{a.Id}/archive", new SetCardArchivedRequest(true));
        Assert.Equal(["B"], (await user.GetBoardAsync(board.Id)).Columns[0].Cards.Select(c => c.Title));
        var archived = await user.Client.GetFromJsonAsync<List<ArchivedCardDto>>($"/api/boards/{board.Id}/archived-cards", TestApi.Json);
        Assert.Equal(["A"], archived!.Select(c => c.Title));

        await user.Client.PutAsJsonAsync($"/api/cards/{a.Id}/archive", new SetCardArchivedRequest(false));
        Assert.Equal(["B", "A"], (await user.GetBoardAsync(board.Id)).Columns[0].Cards.Select(c => c.Title));
    }

    [Fact]
    public async Task My_tasks_shows_open_work_i_am_responsible_for()
    {
        var owner = await api.RegisterAsync();
        var member = await api.RegisterAsync();
        var board = await owner.CreateBoardAsync();
        await api.AddMemberAsync(owner, board.Id, member);
        var column = board.Columns[0].Id;
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);

        await owner.CreateCardAsync(column, new CreateCardRequest("Sahipsiz tarihli", DueDate: tomorrow));
        await owner.CreateCardAsync(column, new CreateCardRequest("Sahipsiz tarihsiz"));
        await owner.CreateCardAsync(column, new CreateCardRequest("Bana atanmış tarihsiz", AssigneeId: owner.Id));
        await owner.CreateCardAsync(column, new CreateCardRequest("Mehmet'e atanmış", DueDate: tomorrow, AssigneeId: member.Id));
        var done = await owner.CreateCardAsync(column, new CreateCardRequest("Bitmiş", DueDate: tomorrow));
        await owner.Client.PutAsJsonAsync($"/api/cards/{done.Id}/complete", new SetCardCompletedRequest(true));

        var ownerTasks = await owner.Client.GetFromJsonAsync<List<MyTaskDto>>("/api/me/tasks", TestApi.Json);
        var memberTasks = await member.Client.GetFromJsonAsync<List<MyTaskDto>>("/api/me/tasks", TestApi.Json);

        // Tarihli olanlar önce, tarihsizler sonda.
        Assert.Equal(["Sahipsiz tarihli", "Bana atanmış tarihsiz"], ownerTasks!.Select(t => t.Title));
        Assert.Equal(["Mehmet'e atanmış"], memberTasks!.Select(t => t.Title));
    }
}
