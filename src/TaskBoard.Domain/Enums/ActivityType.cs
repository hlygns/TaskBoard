namespace TaskBoard.Domain.Enums;

// Veritabanında yazı olarak saklandığı için yeni değer eklemek migration gerektirmez.
public enum ActivityType
{
    BoardCreated = 1,
    MemberJoined = 2,
    MemberRemoved = 3,
    BoardUpdated = 4,
    MemberLeft = 5,
    MemberInvited = 6,
    ColumnCreated = 10,
    ColumnRenamed = 11,
    ColumnMoved = 12,
    ColumnDeleted = 13,
    CardCreated = 20,
    CardUpdated = 21,
    CardMoved = 22,
    CardAssigned = 23,
    CardDeleted = 24,
    CommentAdded = 30
}
