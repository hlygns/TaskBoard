using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskBoard.API.Controllers;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Boards;
using TaskBoard.Application.Cards;

namespace TaskBoard.Tests.Integration;

public record TestUser(HttpClient Client, AuthResponse Auth)
{
    public Guid Id => Auth.User.Id;
    public string Email => Auth.User.Email;
}

// Testleri okunur tutmak için küçük yardımcılar.
public static class TestApi
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public const string Password = "Sifre12345";

    public static async Task<TestUser> RegisterAsync(this TaskBoardApiFactory api, string fullName = "Test Kullanıcı")
    {
        var client = api.CreateHttpsClient();
        var email = $"{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, fullName, Password));
        var auth = await response.ReadAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return new TestUser(client, auth);
    }

    public static async Task<BoardDetailDto> CreateBoardAsync(this TestUser user, string name = "Test Panosu", string? template = null)
    {
        var response = await user.Client.PostAsJsonAsync("/api/boards", new CreateBoardRequest(name, null, template));
        return await response.ReadAsync<BoardDetailDto>();
    }

    public static Task<BoardDetailDto> GetBoardAsync(this TestUser user, Guid boardId) =>
        user.Client.GetFromJsonAsync<BoardDetailDto>($"/api/boards/{boardId}", Json)!;

    public static async Task<CardSummaryDto> CreateCardAsync(this TestUser user, Guid columnId, CreateCardRequest request)
    {
        var response = await user.Client.PostAsJsonAsync($"/api/columns/{columnId}/cards", request, Json);
        return await response.ReadAsync<CardSummaryDto>();
    }

    // Davet gönderir ve davetli kişiyle kabul eder.
    public static async Task AddMemberAsync(this TaskBoardApiFactory api, TestUser owner, Guid boardId, TestUser member)
    {
        (await owner.Client.PostAsJsonAsync($"/api/boards/{boardId}/invitations", new { email = member.Email }))
            .EnsureSuccessStatusCode();
        var token = api.Emails.LastInvitationTokenFor(member.Email);
        (await member.Client.PostAsync($"/api/invitations/{token}/accept", null)).EnsureSuccessStatusCode();
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"{(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }
}
