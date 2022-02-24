using System;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace DeltaSight.Statistics.Tests;

public class SampleStatisticsTests
{
    [Fact]
    public void Remove_FromEmpty_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() => SampleStatistics.Empty.Remove(1d));
    }

    [Fact]
    public void Remove_WithTooHighQuantity_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(
            () => SampleStatistics.Empty.Add(1d).Remove(1d, 2L));
    }

    [Fact]
    public void AddAndRemove_WithSameQuantity_ShouldBeEmpty()
    {
        SampleStatistics
            .Empty
            .Add(Math.PI, 10)
            .Remove(Math.PI, 10)
            .IsEmpty()
            .ShouldBeTrue();
    }
    
    [Fact]
    public void Add_WithCountGreaterThanOne()
    {
        var stats = SampleStatistics
            .From(1d)
            .Add(2d, 5L);

        stats.IsEmpty().ShouldBeFalse();
        stats.Sum.ShouldBe(11d, 1e-2);
        stats.Count.ShouldBe(6L);
        stats.Mean!.Value.ShouldBe(1.83, 1e-2);
        stats.Variance!.Value.ShouldBe(0.17, 1e-2);
        stats.PopulationVariance!.Value.ShouldBe(0.14, 1e-2);
        
        var stats2 = stats.Remove(2d);

        stats2.IsEmpty().ShouldBeFalse();
        stats2.Sum.ShouldBe(9d, 1e-2);
        stats2.Count.ShouldBe(5L);
        stats2.Mean!.Value.ShouldBe(1.8, 1e-2);
        stats2.Variance!.Value.ShouldBe(0.2, 1e-2);
        stats2.PopulationVariance!.Value.ShouldBe(0.16, 1e-2);
    }

    [Fact]
    public void Add()
    {
        var stats = new double[] {1, 2, 3}.CreateStatistics()
                    + new double[] {2, 4, 5, 6 }.CreateStatistics();

        stats.IsEmpty().ShouldBeFalse();
        stats.Sum.ShouldBe(23d, 1e-2);
        stats.Count.ShouldBe(7L);
        stats.Mean!.Value.ShouldBe(3.29, 1e-2);
        stats.Variance!.Value.ShouldBe(3.24, 1e-2);
        stats.PopulationVariance!.Value.ShouldBe(2.78, 1e-2);
    }

    [Fact]
    public void AddAndRemove_ShouldHaveZeroVariance()
    {
        var stats = SampleStatistics
            .Empty
            .Add(2, 2, 4)
            .Remove(4);
        
        stats.IsEmpty().ShouldBeFalse();
        stats.Mean.ShouldBe(2);
        stats.Variance!.Value.ShouldBe(0d, 1e-2);
        stats.StandardDeviation!.Value.ShouldBe(0d, 1e-2);
    }

    [Fact]
    public void AddAndRemove_ShouldNotHaveZeroVariance()
    {
        var stats = new double[] {3, 3, 4, 1}.CreateStatistics();
        
        stats.IsEmpty().ShouldBeFalse();
        stats.Variance!.Value.ShouldBe(1.58, 1e-2);
        stats.StandardDeviation!.Value.ShouldBe(1.26, 1e-2);

        stats = stats.Remove(1);

        stats.IsEmpty().ShouldBeFalse();
        stats.Variance!.Value.ShouldBe(.33, 1e-2);
        stats.StandardDeviation!.Value.ShouldBe(0.58, 1e-2);
    }

    [Fact]
    public void Equality()
    {
        new [] {1d, 2d, 3d}.CreateStatistics()
            .Equals(new [] {1d, 3d, 2d}.CreateStatistics())
            .ShouldBeTrue();
    }
    
    [Fact]
    public void ShouldSerializeAndDeserialize()
    {
        var stats = new[] {1d, 2d, 3d}.CreateStatistics();

        var json = JsonSerializer.Serialize(stats);

        var stats2 = JsonSerializer.Deserialize<SampleStatistics>(json);
        
        (stats == stats2).ShouldBeTrue();

        var json2 = JsonSerializer.Serialize(stats2);
        
        (json == json2).ShouldBeTrue();
    }

    [Fact]
    public void Multiply()
    {
        var stats = new [] {2d, 4d, 6d}
            .CreateStatistics()
            .Multiply(3);
        
        stats.IsEmpty().ShouldBeFalse();
        
        stats.Sum.ShouldBe(36d, 1e-2);
        stats.Mean!.Value.ShouldBe(12d, 1e-2);
        stats.Count.ShouldBe(3L);
        stats.Variance!.Value.ShouldBe(36d, 1e-2); // 4 * 3^2
        stats.PopulationVariance!.Value.ShouldBe(24d, 1e-2);
        stats.StandardDeviation!.Value.ShouldBe(6d, 1e-2);
        stats.PopulationStandardDeviation!.Value.ShouldBe(4.90, 1e-2);
    }
}