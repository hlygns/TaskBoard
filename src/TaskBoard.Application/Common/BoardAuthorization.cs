using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Common;

// Panoya erişim kuralları tek yerde. Pano, sütun, kart ve yorum servisleri hep bunu kullanır.
public static class BoardAuthorization
{
    // Üye olmayan kullanıcıya 403 yerine 404 dönüyoruz: böyle bir panonun var olduğunu bile öğrenmesin.
    public static async Task<BoardRole> EnsureMemberAsync(
        this IAppDbContext db, Guid boardId, Guid userId, CancellationToken ct)
    {
        var role = await db.BoardMembers
            .Where(m => m.BoardId == boardId && m.UserId == userId)
            .Select(m => (BoardRole?)m.Role)
            .SingleOrDefaultAsync(ct);

        return role ?? throw new NotFoundException("Pano bulunamadı.");
    }

    public static async Task EnsureOwnerAsync(
        this IAppDbContext db, Guid boardId, Guid userId, CancellationToken ct)
    {
        var role = await db.EnsureMemberAsync(boardId, userId, ct);

        if (role != BoardRole.Owner)
            throw new ForbiddenException("Bu işlem için pano sahibi olmalısınız.");
    }
}
