# DeltaSight.Statistics
Efficient tracking of the statistical descriptors (Mean, St. Deviation, Variance) of a numeric sample.
Manipulation of the descriptors is done *in line* with help of Welford's single pass algoirithm for updating the *variance* (and *standard deviation*).

## Code examples
### SampleStatistics
```csharp
var stats = SampleStatistics.Empty // Returns an empty sample
  .Add(1, 2, 3) // Adds 1, 2, 3
  .Add(5d, 4L) // Adds 5d, 4 times
  .Remove(1d); // Removes 1d
  
 var otherStats = SampleStatistics.Empty.Add(1, Math.PI); // Creates a sample with { 1, Pi }
 
 var combined = stats + otherStats; // Combines both samples into one
 
 var multiplied = combined.Multiply(Math.PI); // Multiplies the 'combined' sample by Pi
 
 var equals = SampleStatistics.Empty.Add(1) == SampleStatistics.Empty.Add(2); // true
 
 var json = JsonSerializer.Serialize(multiplied); // Json serialization supported
 var fromJson = JsonSerializer.Deserialize<SampleStatistics>(json); // Json deserialization supported
```
