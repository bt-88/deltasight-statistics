// See https://aka.ms/new-console-template for more information

using DeltaSight.Statistics;

Console.WriteLine("Enter a numeric value and press enter to add it to the sample");

var stats = new SampleStatistics();

while (true)
{
    var input = Console.ReadLine();

    if (!double.TryParse(input, out var value))
    {
        Console.WriteLine($"### Error: The input '{input}' is invalid. Please try again.");
        continue;
    }

    stats = stats.Add(value);
    
    Console.WriteLine($"### '{value}' was added to the sample: The new stats are\n\t{stats}");
}