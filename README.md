# DeltaSight.Statistics
Efficient tracking of the statistical descriptors (Mean, St. Deviation, Variance) of a numeric sample.
Manipulation of the descriptors is done *in one pass* with help of Welford's algoirithm for updating the *variance* (and *standard deviation*).

Useful if you need to update the *running* statistics of a very large sample in *one pass*. For instance when processing streaming data.

## Features
* Fast one pass algorithm for:
   - Mean
   - Variance
   - PopulationVariance
   - StDeviation
   - PopulationStDeviation
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