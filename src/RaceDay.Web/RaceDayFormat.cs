namespace RaceDay.Web;

public static class RaceDayFormat
{
    public static string Ordinal(int number)
    {
        var absolute = Math.Abs(number);
        var lastTwo = absolute % 100;
        if (lastTwo is >= 11 and <= 13)
        {
            return number + "th";
        }

        return (absolute % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }

    public static string FinishTime(TimeSpan time)
    {
        var hours = (int)time.TotalHours;
        return hours > 0
            ? $"{hours}:{time.Minutes:00}:{time.Seconds:00}"
            : $"{time.Minutes:00}:{time.Seconds:00}";
    }

    public static string Placement(int position, int finisherCount)
    {
        var place = Ordinal(position);
        return finisherCount > 0
            ? $"{place} out of {finisherCount} finishers"
            : place;
    }
}
