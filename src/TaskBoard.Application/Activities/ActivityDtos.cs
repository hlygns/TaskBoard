using TaskBoard.Application.Cards;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Activities;

// Data: kayıt anındaki adlar, ör. { "cardTitle": "Logo", "fromColumn": "Yapılacak", "toColumn": "Bitti" }.
// Cümleyi istemci kurar ("Hülya 'Logo' kartını Bitti'ye taşıdı"); böylece dil/çeviri arayüzde kalır.
public record ActivityDto(
    Guid Id,
    ActivityType Type,
    MemberRefDto Actor,
    Dictionary<string, string?> Data,
    DateTime CreatedAt);
