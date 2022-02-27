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

### Compared to MathNet.Numerics.Statistics.RunningStatistics
RunningStatistics lacks:
- Remove API
- Support for adding a value more than once
- Immutability
- (Proper) Json serialization support


## How to use
### SampleStatistics
```csharp
// Start adding/removing from an empty sample
var stats = SampleStatistics.From(1, 2, 3)
  .Add(5d, 4L) // Adds 5d, 4 times
  .Remove(1d);
  
// Get some stats
var mean = stats.Mean;
var sse = stats.SumSquaredError;
var variance = stats.Variance;
  
// IEnumerable extension
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
## Benchmarks
```
|                        Method |       Mean |     Error |    StdDev |
|------------------------------ |-----------:|----------:|----------:|
|      SampleStatistics_Compute |  57.002 ns | 0.3728 ns | 0.3113 ns |
|               MathNet_Compute |  76.545 ns | 0.3206 ns | 0.2999 ns |
|              Baseline_Compute |  54.018 ns | 0.0681 ns | 0.0604 ns |
| SampleStatistics_AddFiveTimes |   7.800 ns | 0.0438 ns | 0.0410 ns |
|          MathNet_AddFiveTimes |  37.874 ns | 0.1875 ns | 0.1566 ns |
|         Baseline_AddFiveTimes | 292.260 ns | 1.2068 ns | 1.1288 ns |
|          SampleStatistics_Add |   7.828 ns | 0.0314 ns | 0.0293 ns |
|                   MathNet_Add |   6.741 ns | 0.0576 ns | 0.0450 ns |
|                  Baseline_Add | 102.016 ns | 0.2428 ns | 0.2152 ns |
```
```
BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.2.1 (21D62) [Darwin 21.3.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), Arm64 RyuJIT
```
