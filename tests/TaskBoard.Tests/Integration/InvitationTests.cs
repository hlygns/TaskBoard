using System.Net;
using System.Net.Http.Json;
using TaskBoard.Application.Invitations;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Tests.Integration;

[Collection(ApiCollection.Name)]
public class InvitationTests(TaskBoardApiFactory api)
{
    [Fact]
    public async Task Invitation_preview_is_visible_without_login()
    {
        var owner = await api.RegisterAsync("Hülya Güneş");
        var board = await owner.CreateBoardAsync("Pazarlama");
        await owner.Client.PostAsJsonAsync($"/api/boards/{board.Id}/invitations", new { email = "Yeni.Kisi@Test.com" });
        var token = api.Emails.LastInvitationTokenFor("yeni.kisi@test.com");

        var preview = await api.CreateHttpsClient().GetFromJsonAsync<InvitationPreviewDto>(
            $"/api/invitations/{token}", TestApi.Json);

        Assert.Equal("Pazarlama", preview!.BoardName);
        Assert.Equal("Hülya Güneş", preview.InvitedByName);
        Assert.Equal("yeni.kisi@test.com", preview.Email);
        Assert.Equal(InvitationStatus.Pending, preview.Status);
    }

    [Fact]
    public async Task Only_the_invited_email_can_accept_and_only_once()
    {
        var owner = await api.RegisterAsync();
        var invited = await api.RegisterAsync();
        var someoneElse = await api.RegisterAsync();
        var board = await owner.CreateBoardAsync();
        await owner.Client.PostAsJsonAsync($"/api/boards/{board.Id}/invitations", new { email = invited.Email });
        var token = api.Emails.LastInvitationTokenFor(invited.Email);

        // Link başkasına iletilse bile işe yaramaz.
        var forwarded = await someoneElse.Client.PostAsync($"/api/invitations/{token}/accept", null);
        var accepted = await invited.Client.PostAsync($"/api/invitations/{token}/accept", null);
        var again = await invited.Client.PostAsync($"/api/invitations/{token}/accept", null);

        Assert.Equal(HttpStatusCode.Forbidden, forwarded.StatusCode);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(BoardRole.Member, (await invited.GetBoardAsync(board.Id)).MyRole);
    }

    [Fact]
    public async Task Inviting_an_existing_member_returns_409()
    {
        var owner = await api.RegisterAsync();
        var board = await owner.CreateBoardAsync();

        var response = await owner.Client.PostAsJsonAsync($"/api/boards/{board.Id}/invitations", new { email = owner.Email });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
