namespace TaskBoard.Application.Common.Exceptions;

// İş kuralı hataları. API katmanındaki GlobalExceptionHandler bunları HTTP durum kodlarına çevirir.
public abstract class AppException(string message) : Exception(message);

public class BadRequestException(string message) : AppException(message);

public class NotFoundException(string message) : AppException(message);

public class ConflictException(string message) : AppException(message);

public class UnauthorizedException(string message) : AppException(message);

public class ForbiddenException(string message) : AppException(message);
