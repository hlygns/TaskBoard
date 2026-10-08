using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Labels;

public record LabelDto(Guid Id, string Name, string Color);

public record SaveLabelRequest([Required, MaxLength(40)] string Name, [Required] string Color);

public interface ILabelService
{
    Task<LabelDto> CreateAsync(Guid boardId, SaveLabelRequest request, CancellationToken ct = default);
    Task<LabelDto> UpdateAsync(Guid labelId, SaveLabelRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid labelId, CancellationToken ct = default);
}

// Etiketler panoya aittir; panonun her üyesi oluşturup düzenleyebilir.
public class LabelService(IAppDbContext db, ICurrentUser currentUser, IBoardNotifier notifier) : ILabelService
{
    // Arayüzdeki sabit renk paleti; serbest renk kodu kabul etmiyoruz.
    public static readonly string[] Colors =
        ["slate", "red", "orange", "amber", "green", "teal", "sky", "indigo", "violet", "pink"];

    public async Task<LabelDto> CreateAsync(Guid boardId, SaveLabelRequest request, CancellationToken ct = default)
    {
        await db.EnsureMemberAsync(boardId, currentUser.Id, ct);
        var (name, color) = Validate(request);
        await EnsureUniqueNameAsync(boardId, name, null, ct);

        var label = new Label { BoardId = boardId, Name = name, Color = color };
        db.Labels.Add(label);
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.BoardUpdated), ct);

        return new LabelDto(label.Id, label.Name, label.Color);
    }

    public async Task<LabelDto> UpdateAsync(Guid labelId, SaveLabelRequest request, CancellationToken ct = default)
    {
        var label = await GetForMemberAsync(labelId, ct);
        var (name, color) = Validate(request);
        await EnsureUniqueNameAsync(label.BoardId, name, labelId, ct);

        label.Name = name;
        label.Color = color;
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(label.BoardId, new BoardEvent(BoardEvent.BoardUpdated), ct);

        return new LabelDto(label.Id, label.Name, label.Color);
    }

    public async Task DeleteAsync(Guid labelId, CancellationToken ct = default)
    {
        var label = await GetForMemberAsync(labelId, ct);

        // Kartlardaki bağlantıları (card_labels) veritabanı ON DELETE CASCADE ile siler.
        db.Labels.Remove(label);
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(label.BoardId, new BoardEvent(BoardEvent.BoardUpdated), ct);
    }

    private async Task<Label> GetForMemberAsync(Guid labelId, CancellationToken ct)
    {
        var label = await db.Labels.SingleOrDefaultAsync(l => l.Id == labelId, ct)
            ?? throw new NotFoundException("Etiket bulunamadı.");
        await db.EnsureMemberAsync(label.BoardId, currentUser.Id, ct);
        return label;
    }

    private async Task EnsureUniqueNameAsync(Guid boardId, string name, Guid? exceptId, CancellationToken ct)
    {
        var lower = name.ToLower();
        var exists = await db.Labels.AnyAsync(l => l.BoardId == boardId && l.Id != exceptId && l.Name.ToLower() == lower, ct);
        if (exists)
            throw new ConflictException($"\"{name}\" adında bir etiket zaten var.");
    }

    private static (string Name, string Color) Validate(SaveLabelRequest request)
    {
        if (!Colors.Contains(request.Color))
            throw new BadRequestException("Geçersiz etiket rengi.");
        return (request.Name.Trim(), request.Color);
    }
}
