using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Activities;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Boards;
using TaskBoard.Application.Cards;
using TaskBoard.Application.Columns;
using TaskBoard.Application.Invitations;
using TaskBoard.Application.Labels;
using TaskBoard.Application.Tasks;

namespace TaskBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IColumnService, ColumnService>();
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<IMyTasksService, MyTasksService>();

        return services;
    }
}
