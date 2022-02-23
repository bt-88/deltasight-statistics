using System;
using System.Linq;
using System.Collections.Generic;
using DeltaSight.Statistics;

var stats = new SampleStatistics();

while (true)
{
    if (stats.IsEmpty())
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

    var values = input?
        .Split(";", StringSplitOptions.RemoveEmptyEntries)
        .Select(x => double.TryParse(x, out var value) ? value : double.NaN)
        .Where(x => !double.IsNaN(x))
        .ToArray();
    
    if (values is null || values.Length == 0)
    {
        Console.WriteLine($"### Error: The input '{input}' is invalid. Please try again.");
        continue;
    }

    stats = ApplyOperation(stats, operation, values);

    Console.WriteLine($"### '{input}' was processed (operation: '{operation}'). The new stats are\n\t{stats}");
}

static bool IsOperationValid(string? operation)
{
    return operation is not null && new[] {"a", "r"}.Contains(operation.ToLower());
}

static SampleStatistics ApplyOperation(SampleStatistics stats, string? operation, IEnumerable<double> values)
{
    return operation?.ToLower() switch
    {
        "a" => stats.Add(values),
        "r" => stats.Remove(values),
        _ => stats
    };
}