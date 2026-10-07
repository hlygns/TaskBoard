using System.Net;
using System.Net.Http.Json;
using TaskBoard.Application.Cards;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Tests.Integration;

[Collection(ApiCollection.Name)]
public class BoardAccessTests(TaskBoardApiFactory api)
{
    [Fact]
    public async Task New_board_has_owner_and_default_columns()
    {
        var owner = await api.RegisterAsync();

        var board = await owner.CreateBoardAsync("Web Projesi");

        Assert.Equal(BoardRole.Owner, board.MyRole);
        Assert.Equal(["Yapılacak", "Yapılıyor", "Bitti"], board.Columns.Select(c => c.Name));
    }

    [Fact]
    public async Task Outsider_gets_404_not_403_for_board_and_cards()
    {
        var owner = await api.RegisterAsync();
        var outsider = await api.RegisterAsync();
        var board = await owner.CreateBoardAsync();
        var card = await owner.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Gizli kart"));

        // 404: yabancı, böyle bir panonun var olduğunu bile öğrenmemeli.
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.Client.GetAsync($"/api/boards/{board.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.Client.GetAsync($"/api/cards/{card.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.Client.PostAsJsonAsync(
            $"/api/columns/{board.Columns[0].Id}/cards", new CreateCardRequest("Sızma"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.Client.GetAsync($"/api/boards/{board.Id}/activities")).StatusCode);
    }

    [Fact]
    public async Task Member_can_edit_cards_but_not_manage_the_board()
    {
        var owner = await api.RegisterAsync();
        var member = await api.RegisterAsync();
        var board = await owner.CreateBoardAsync();
        await api.AddMemberAsync(owner, board.Id, member);

        var asMember = await member.GetBoardAsync(board.Id);
        Assert.Equal(BoardRole.Member, asMember.MyRole);

        // Üye kart ekleyebilir...
        await member.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Üyenin kartı"));

        // ...ama panoyu silemez, adını değiştiremez, davet gönderemez.
        Assert.Equal(HttpStatusCode.Forbidden, (await member.Client.DeleteAsync($"/api/boards/{board.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.Client.PutAsJsonAsync(
            $"/api/boards/{board.Id}", new { name = "Yeni ad" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.Client.PostAsJsonAsync(
            $"/api/boards/{board.Id}/invitations", new { email = "biri@test.com" })).StatusCode);
    }

    [Fact]
    public async Task Owner_cannot_leave_but_member_can()
    {
        var owner = await api.RegisterAsync();
        var member = await api.RegisterAsync();
        var board = await owner.CreateBoardAsync();
        await api.AddMemberAsync(owner, board.Id, member);

        var ownerLeaves = await owner.Client.DeleteAsync($"/api/boards/{board.Id}/members/{owner.Id}");
        var memberLeaves = await member.Client.DeleteAsync($"/api/boards/{board.Id}/members/{member.Id}");

        Assert.Equal(HttpStatusCode.Conflict, ownerLeaves.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, memberLeaves.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await member.Client.GetAsync($"/api/boards/{board.Id}")).StatusCode);
    }

    [Fact]
    public async Task Removed_member_is_unassigned_from_cards()
    {
        var owner = await api.RegisterAsync();
        var member = await api.RegisterAsync();
        var board = await owner.CreateBoardAsync();
        await api.AddMemberAsync(owner, board.Id, member);
        var card = await owner.CreateCardAsync(board.Columns[0].Id, new CreateCardRequest("Atanmış kart", AssigneeId: member.Id));
        Assert.Equal(member.Id, card.Assignee!.UserId);

        (await owner.Client.DeleteAsync($"/api/boards/{board.Id}/members/{member.Id}")).EnsureSuccessStatusCode();

        var reloaded = await owner.GetBoardAsync(board.Id);
        Assert.Null(reloaded.Columns[0].Cards.Single().Assignee);
    }
}
