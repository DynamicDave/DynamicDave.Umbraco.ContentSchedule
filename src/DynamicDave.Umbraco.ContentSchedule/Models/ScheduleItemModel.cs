namespace DynamicDave.Umbraco.ContentSchedule.Models;

public sealed class ScheduleItemModel
{
    public required Guid Key { get; init; }
    public required string Name { get; init; }
    public required string Action { get; init; }   // "publish" | "unpublish"
    public string? Culture { get; init; }
    public required DateTime ScheduledAt { get; init; } // UTC
    public required string Status { get; init; }   // "scheduled" | "overdue"
}

public sealed class ScheduleCountsModel
{
    public required int Today { get; init; }
    public required int Next7Days { get; init; }
    public required int Next30Days { get; init; }
    public required int Overdue { get; init; }
}

public sealed class ScheduleItemsResponse
{
    public required IReadOnlyList<ScheduleItemModel> Items { get; init; }
    public required ScheduleCountsModel Counts { get; init; }
}
