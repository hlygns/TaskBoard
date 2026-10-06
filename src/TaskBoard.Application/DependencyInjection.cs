using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Boards;
using TaskBoard.Application.Invitations;

namespace TaskBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IInvitationService, InvitationService>();

        return services;
    }
}
