![DeltaSight](https://github.com/bt-88/deltasight-statistics/blob/master/images/Logo_Light_Wide.png)

# DeltaSight.Statistics
Provides efficient tracking of statistical descriptors of a changing or running value sample.
Manipulation of the descriptors efficient because it is done *in one pass* as much as possible.

## Features
* SimpleStatisticsTracker: A fast one pass tracker with *Add* and *Remove* API for
   - Mean
   - Variance
   - PopulationVariance
   - StDeviation
   - PopulationStDeviation
   - Sum
   - Count
* AdvancedStatisticsTracker: Tracks the following additional descriptors
   - Minimum
   - Maximum
   - GreatestCommonDivisor
   - Probabilities (i.e., the probability densities)
* JSON serialization and deserialization

## How to use
Please see the example project under */examples*.
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
