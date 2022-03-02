using System;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace DeltaSight.Statistics.Tests;

public class SimpleStatisticsTrackerTests
{
    [Fact]
    public void Remove_FromEmpty_ShouldThrow()
    {
        Assert.Throws<StatisticsTrackerException>(() =>
        {
            var tracker = new SimpleStatisticsTracker();
            
            tracker.Remove(1d);
        });
    }

    [Fact]
    public void Remove_WithTooHighQuantity_ShouldThrow()
    {
        Assert.Throws<StatisticsTrackerException>(
            () =>
            {
                var tracker = SimpleStatisticsTracker.From(10d);
                
                tracker.Remove(10d, 2);
            });
    }

    [Fact]
    public void AddAndRemove_WithSameQuantity_ShouldBeEmpty()
    {
        var tracker = new SimpleStatisticsTracker();
        tracker.Add(Math.PI, 10);
        tracker.Remove(Math.PI, 10);
        tracker.IsEmpty().ShouldBeTrue();

        tracker
            .TakeSnapshot()
            .ShouldBeNull();
    }

    [Fact]
    public void Empty_TakeSnapshot_ShouldBeNull()
    {
        new SimpleStatisticsTracker()
            .TakeSnapshot()
            .ShouldBeNull();
    }
    
    [Fact]
    public void Add_WithCountGreaterThanOne()
    {
        var tracker = SimpleStatisticsTracker.From(1d);
        
        tracker.Add(2d, 5L);
        
        tracker.IsEmpty().ShouldBeFalse();

        var stats = tracker.TakeSnapshot();

        stats.ShouldNotBeNull();
        stats.Sum.ShouldBe(11d, 1e-2);
        stats.Count.ShouldBe(6L);
        stats.Mean.ShouldBe(1.83, 1e-2);
        stats.Variance.ShouldBe(0.17, 1e-2);
        stats.PopulationVariance.ShouldBe(0.14, 1e-2);
        
        tracker.Remove(2d);

        tracker.IsEmpty().ShouldBeFalse();
        
        var stats2 = tracker.TakeSnapshot();

        stats2.ShouldNotBeNull();
        stats2.Sum.ShouldBe(9d, 1e-2);
        stats2.Count.ShouldBe(5L);
        stats2.Mean.ShouldBe(1.8, 1e-2);
        stats2.Variance.ShouldBe(0.2, 1e-2);
        stats2.PopulationVariance.ShouldBe(0.16, 1e-2);
    }

    [Fact]
    public void Combine()
    {
        var tracker1 = SimpleStatisticsTracker.From(1, 2, 3);
        var tracker2 = SimpleStatisticsTracker.From(2, 4, 5, 6);

        var tracker = tracker1.Combine(tracker2);

        var stats = tracker.TakeSnapshot();

        stats.ShouldNotBeNull();
        stats.Sum.ShouldBe(23d, 1e-2);
        stats.Count.ShouldBe(7L);
        stats.Mean.ShouldBe(3.29, 1e-2);
        stats.Variance.ShouldBe(3.24, 1e-2);
        stats.PopulationVariance.ShouldBe(2.78, 1e-2);
    }

    [Fact]
    public void Equality()
    {
        var values = new[] {1d, 2d, 3d, 4d, 10d};

        var tracker1 = values.TrackSimpleStatistics();
        var tracker2 = new SimpleStatisticsTracker(values);
        
        tracker1.Equals(tracker2).ShouldBeTrue();
        ReferenceEquals(tracker1, tracker2).ShouldBeFalse();

        var snap1 = tracker1.TakeSnapshot();
        var snap2 = tracker2.TakeSnapshot();

        snap1.ShouldNotBeNull();
        snap2.ShouldNotBeNull();
        
        snap1.Equals(snap2).ShouldBeTrue();
        ReferenceEquals(snap1, snap2).ShouldBeFalse();
    }
    
    [Fact]
    public void AddAndRemove_ShouldHaveZeroVariance()
    {
        var tracker = SimpleStatisticsTracker.From(2d, 2d, 4d);
        
        tracker.Remove(4);
        tracker.IsEmpty().ShouldBeFalse();

        var stats = tracker.TakeSnapshot();

        stats.ShouldNotBeNull();
        stats.Mean.ShouldBe(2);
        stats.Variance.ShouldBe(0d, 1e-2);
        stats.StandardDeviation.ShouldBe(0d, 1e-2);
    }

    [Fact]
    public void AddAndRemove_ShouldNotHaveZeroVariance()
    {
        var tracker = SimpleStatisticsTracker.From(3, 3, 4, 1);
        var stats = tracker.TakeSnapshot();

        stats.ShouldNotBeNull();
        stats.Variance.ShouldBe(1.58, 1e-2);
        stats.StandardDeviation.ShouldBe(1.26, 1e-2);

        tracker.Remove(1);

        stats = tracker.TakeSnapshot();
        
        stats.ShouldNotBeNull();
        stats.Variance.ShouldBe(.33, 1e-2);
        stats.StandardDeviation.ShouldBe(0.58, 1e-2);
    }

    [Fact]
    public void Serialize()
    {
        JsonSerializer.Serialize(SimpleStatisticsTracker.From(1, 2, 3))
            .ShouldBe("{\"Sum\":6,\"SSE\":2,\"N\":3}");
    }

    [Fact]
    public void Deserialize()
    {
        var tracker = JsonSerializer.Deserialize<SimpleStatisticsTracker>("{\"N\":3,\"Sum\":6,\"SSE\":2}");

        var stats = tracker?.TakeSnapshot();

        stats.ShouldNotBeNull();
        stats.Count.ShouldBe(3);
        stats.Mean.ShouldBe(2d, 1e-2);
        stats.Sum.ShouldBe(6d, 1e-2);
        stats.SumSquaredError.ShouldBe(2d, 1e-2);
        stats.StandardDeviation.ShouldBe(1d, 1e-2);
        stats.PopulationStandardDeviation.ShouldBe(0.82, 1e-2);
        stats.Variance.ShouldBe(1d, 1e-2);
        stats.PopulationVariance.ShouldBe(.66, 1e-2);
    }

    [Fact]
    public void Multiply()
    {
        var tracker = SimpleStatisticsTracker.From(2, 4, 6).Multiply(3);
        
        tracker.IsEmpty().ShouldBeFalse();

        var stats = tracker.TakeSnapshot();

        stats.ShouldNotBeNull();
        stats.Sum.ShouldBe(36d, 1e-2);
        stats.Mean.ShouldBe(12d, 1e-2);
        stats.Count.ShouldBe(3L);
        stats.Variance.ShouldBe(36d, 1e-2); // 4 * 3^2
        stats.PopulationVariance.ShouldBe(24d, 1e-2);
        stats.StandardDeviation.ShouldBe(6d, 1e-2);
        stats.PopulationStandardDeviation.ShouldBe(4.90, 1e-2);
    }
}