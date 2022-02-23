// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Running;
using DeltaSight.Statistics.Benchmarks;

Console.WriteLine("Hello, World!");

BenchmarkRunner.Run<SampleStatisticsBenchmarks>();