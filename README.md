# DeltaSight.Statistics
Efficient tracking of the statistical descriptors (Mean, St. Deviation, Variance) of a numeric sample.
Manipulation of the descriptors is done *in one pass* with help of Welford's algorithm for updating the *variance* (and *standard deviation*).

Useful if you need to update the *running* statistics of a very large sample in *one pass*, such as with streaming data.

## Features
* Fast one pass algorithm for:
   - Mean
   - Variance
   - PopulationVariance
   - StDeviation
   - PopulationStDeviation
   - Sum
   - Count
   - SumSquaredError
* Immutable
* JSON Serializable/Deserializable
* Fluent style API

## How to use
### SampleStatistics
```csharp
// Start adding/removing from an empty sample
var stats = SampleStatistics.Empty
  .Add(1, 2, 3)
  .Add(5d, 4L) // Adds 5d, 4 times
  .Remove(1d);
  
// Get some stats
var mean = stats.Mean;
var sse = stats.SumSquaredError;
var variance = stats.Variance;
  
// Start from an IEnumerable
var stats2 = new [] {1d, 2d, 3d}.CreateStatistics();
 
// Combine two samples
var combined = stats + stats2; // Combines both samples into one

// Multiply a sample (converts X to m*X)
var multiplied = combines * Math.PI;
 
// Equality
var equals = SampleStatistics.From(Math.PI) == SampleStatistics.From(Math.PI); // true

// To Json
var json = JsonSerializer.Serialize(multiplied); // Json serialization supported

// From Json
var fromJson = JsonSerializer.Deserialize<SampleStatistics>(json); // Json deserialization supported

// [MemberNotNullWhen] implementation
if (!stats2.IsEmpty())
{
   Console.WriteLine(stats2.Mean.Value); // Does not warn about `Mean` being possibly null
}
```
## Benchmark
```
BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.2.1 (21D62) [Darwin 21.3.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT


|                  Method |       Mean |     Error |    StdDev |
|------------------------ |-----------:|----------:|----------:|
| SampleStatistics_Create |  52.025 ns | 0.0956 ns | 0.0798 ns |
|    SampleStatistics_Add |   8.456 ns | 0.1044 ns | 0.0925 ns |
|         Baseline_Create |  54.649 ns | 0.9422 ns | 0.8353 ns |
|            Baseline_Add | 103.958 ns | 1.3625 ns | 1.2078 ns |
```
