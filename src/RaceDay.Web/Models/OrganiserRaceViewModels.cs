using RaceDay.Web.Api;

namespace RaceDay.Web.Models;

public sealed class EnrolmentListViewModel
{
    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public List<EnrolmentModel> Enrolments { get; set; } = [];
}

public sealed class ResultEntryViewModel
{
    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public List<ResultEntryRow> Rows { get; set; } = [];
}

public sealed class ResultEntryRow
{
    public int EnrolmentId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? ResultId { get; set; }

    public string FinishTime { get; set; } = "01:00:00";

    public int FinishingPosition { get; set; } = 1;

    public int FinisherCount { get; set; }
}
