using DeltaSight.Statistics;
using DeltaSight.Statistics.Abstractions;

Console.WriteLine("### Choose a statistics tracker: [S]imple, [A]dvanced");

var tracker = ProvideTracker(Console.ReadLine());

while (true)
{
    try
    {
        if (tracker.IsEmpty())
        {
            Console.WriteLine("### Your sample is empty");
        }

        Console.WriteLine("### Choose an operation: [A]dd, [R]emove");

        var operation = Console.ReadLine();

        if (!IsOperationValid(operation))
        {
            Console.WriteLine($"### Invalid operation '{operation}'. Please try again.");
            continue;
        }

        Console.WriteLine("### Enter a numeric value (or a set of values, seperated by `;`)");

        var input = Console.ReadLine();

        var values = ConvertToValues(input);

        if (values is null || values.Length == 0)
        {
            Console.WriteLine($"### Error: The input '{input}' is invalid. Please try again.");
            continue;
        }

        ApplyOperation(tracker, operation, values);

        var snapshot = tracker.TakeSnapshot();

        Console.WriteLine(
            $"### '{input}' was processed (operation: '{operation}'). The stats snapshot is\n\t{snapshot}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("### Error: " + ex.Message);
    }
}

static bool IsOperationValid(string? operation)
{
    return operation is not null && new[] {"a", "r"}.Contains(operation.ToLower());
}

static void ApplyOperation(IStatisticsTracker<IStatisticsSnapshot> stats, string? operation, IEnumerable<double> values)
{
    switch (operation?.ToLower())
    {
        case "a":
            stats.Add(values);
            break;
        case "r":
            stats.Remove(values);
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(operation));
    }
}

static IStatisticsTracker<IStatisticsSnapshot> ProvideTracker(string? operation)
{
    return operation?.ToLower() switch
    {
        "a" => new AdvancedStatisticsTracker(),
        "s" => new SimpleStatisticsTracker(),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}

static double[]? ConvertToValues(string? input)
{
    return input?
        .Split(";", StringSplitOptions.RemoveEmptyEntries)
        .Select(x => double.TryParse(x, out var value) ? value : double.NaN)
        .Where(x => !double.IsNaN(x))
        .ToArray();
}