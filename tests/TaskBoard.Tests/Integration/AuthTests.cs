using System.Net;
using System.Net.Http.Json;
using TaskBoard.API.Controllers;
using TaskBoard.Application.Auth;

namespace TaskBoard.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthTests(TaskBoardApiFactory api)
{
    [Fact]
    public async Task Registered_user_can_call_me_with_access_token()
    {
        var user = await api.RegisterAsync("Ayşe Yılmaz");

        var me = await user.Client.GetFromJsonAsync<UserDto>("/api/auth/me", TestApi.Json);

        Assert.Equal("Ayşe Yılmaz", me!.FullName);
        Assert.Equal(user.Email, me.Email);
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var response = await api.CreateHttpsClient().GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Email_is_unique_case_insensitively()
    {
        var user = await api.RegisterAsync();

        var response = await api.CreateHttpsClient().PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(user.Email.ToUpperInvariant(), "Başka Biri", TestApi.Password));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_the_same_message()
    {
        var user = await api.RegisterAsync();
        var client = api.CreateHttpsClient();

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, "yanlis-sifre"));
        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("yok@test.com", "yanlis-sifre"));

        // Saldırgan hangi e-postaların kayıtlı olduğunu öğrenemesin (user enumeration).
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(
            Detail(await wrongPassword.Content.ReadAsStringAsync()),
            Detail(await unknownEmail.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Reusing_a_rotated_refresh_token_revokes_all_sessions()
    {
        var user = await api.RegisterAsync();
        var client = api.CreateHttpsClient(handleCookies: false);

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(user.Email, TestApi.Password));
        var first = RefreshCookie(login);

        // Normal yenileme: yeni token gelir, eskisi iptal edilir.
        var refreshed = await Refresh(client, first);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var second = RefreshCookie(refreshed);
        Assert.NotEqual(first, second);

        // Eski token tekrar kullanılırsa çalınmış sayılır...
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, first)).StatusCode);
        // ...ve kullanıcının tüm oturumları (yeni token dahil) kapanır.
        Assert.Equal(HttpStatusCode.Unauthorized, (await Refresh(client, second)).StatusCode);
    }

    private static Task<HttpResponseMessage> Refresh(HttpClient client, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"refreshToken={token}");
        return client.SendAsync(request);
    }

    private static string RefreshCookie(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("refreshToken="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        return cookie.Split(';')[0]["refreshToken=".Length..];
    }

    private static string? Detail(string problemJson) =>
        System.Text.Json.JsonDocument.Parse(problemJson).RootElement.GetProperty("detail").GetString();
}
