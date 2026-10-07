using System.Net;
using System.Net.Http.Json;
using TaskBoard.Application.Activities;
using TaskBoard.Application.Cards;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Tests.Integration;

[Collection(ApiCollection.Name)]
public class CardTests(TaskBoardApiFactory api)
{
    [Fact]
    public async Task Moves_between_and_within_columns_are_persisted()
    {
        var user = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();
        var (todo, doing) = (board.Columns[0].Id, board.Columns[1].Id);
        var a = await user.CreateCardAsync(todo, new CreateCardRequest("A"));
        await user.CreateCardAsync(todo, new CreateCardRequest("B"));
        var c = await user.CreateCardAsync(todo, new CreateCardRequest("C"));

        await Move(user, c.Id, doing, 0);   // C → Yapılıyor
        await Move(user, a.Id, todo, 1);    // A → B'nin arkasına

        var reloaded = await user.GetBoardAsync(board.Id);
        Assert.Equal(["B", "A"], reloaded.Columns[0].Cards.Select(x => x.Title));
        Assert.Equal(["C"], reloaded.Columns[1].Cards.Select(x => x.Title));
    }

    [Fact]
    public async Task Card_cannot_be_moved_to_another_boards_column()
    {
        var user = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();
        var otherBoard = await user.CreateBoardAsync("Diğer pano");
        var card = await user.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Kart"));

        var response = await user.Client.PutAsJsonAsync($"/api/cards/{card.Id}/move",
            new MoveCardRequest(otherBoard.Columns[0].Id, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Only_board_members_can_be_assigned()
    {
        var user = await api.RegisterAsync();
        var outsider = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();

        var response = await user.Client.PostAsJsonAsync($"/api/columns/{board.Columns[0].Id}/cards",
            new CreateCardRequest("Kart", AssigneeId: outsider.Id), TestApi.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Activity_history_keeps_card_title_after_the_card_is_deleted()
    {
        var user = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();
        var card = await user.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Logo revizyonu"));
        await Move(user, card.Id, board.Columns[2].Id, 0);
        await user.Client.PostAsJsonAsync($"/api/cards/{card.Id}/comments", new AddCommentRequest("Bitti sayılır"));
        (await user.Client.DeleteAsync($"/api/cards/{card.Id}")).EnsureSuccessStatusCode();

        var activities = (await user.Client.GetFromJsonAsync<List<ActivityDto>>(
            $"/api/boards/{board.Id}/activities", TestApi.Json))!;

        // En yeni en üstte.
        Assert.Equal(
            [ActivityType.CardDeleted, ActivityType.CommentAdded, ActivityType.CardMoved, ActivityType.CardCreated, ActivityType.BoardCreated],
            activities.Select(a => a.Type));

        var moved = activities.Single(a => a.Type == ActivityType.CardMoved);
        Assert.Equal("Logo revizyonu", moved.Data["cardTitle"]);
        Assert.Equal("Yapılacak", moved.Data["fromColumn"]);
        Assert.Equal("Bitti", moved.Data["toColumn"]);
        Assert.Equal("Logo revizyonu", activities[0].Data["cardTitle"]);
    }

    [Fact]
    public async Task Activity_history_is_paged_with_a_before_cursor()
    {
        var user = await api.RegisterAsync();
        var board = await user.CreateBoardAsync();
        for (var i = 1; i <= 5; i++)
            await user.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest($"Kart {i}"));

        var firstPage = await user.Client.GetFromJsonAsync<List<ActivityDto>>(
            $"/api/boards/{board.Id}/activities?limit=4", TestApi.Json);
        var cursor = Uri.EscapeDataString(firstPage![^1].CreatedAt.ToString("O"));
        var secondPage = await user.Client.GetFromJsonAsync<List<ActivityDto>>(
            $"/api/boards/{board.Id}/activities?limit=4&before={cursor}", TestApi.Json);

        // 5 kart + "pano oluşturuldu" = 6 kayıt → 4 + 2, tekrar ya da atlama yok.
        Assert.Equal(4, firstPage.Count);
        Assert.Equal(2, secondPage!.Count);
        Assert.Empty(firstPage.Select(a => a.Id).Intersect(secondPage.Select(a => a.Id)));
    }

    private static async Task Move(TestUser user, Guid cardId, Guid columnId, int index) =>
        (await user.Client.PutAsJsonAsync($"/api/cards/{cardId}/move", new MoveCardRequest(columnId, index)))
            .EnsureSuccessStatusCode();
}
