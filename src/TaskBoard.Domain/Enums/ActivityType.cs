namespace TaskBoard.Domain.Enums;

public enum ActivityType
{
    BoardCreated = 1,
    MemberJoined = 2,
    MemberRemoved = 3,
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
