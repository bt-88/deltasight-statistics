namespace DeltaSight.Statistics;

public class StatisticsTrackerException : Exception
{
    public StatisticsTrackerException(string message, Exception ex) : base(message, ex)
    {
        
    }
}