using Microsoft.AspNetCore.Identity;
using TaskBoard.Domain.Entities;
using IPasswordHasher = TaskBoard.Application.Common.Interfaces.IPasswordHasher;

namespace TaskBoard.Infrastructure.Authentication;

// ASP.NET Core Identity'nin hasher'ını kullanıyoruz: PBKDF2 + rastgele salt + 100.000 iterasyon.
// Şifre doğrulamayı kasıtlı olarak yavaşlatır, böylece kaba kuvvet saldırıları pahalılaşır.
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
