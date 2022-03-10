using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace DeltaSight.Statistics.Tests;

public class AdvancedStatisticsTrackerTests
{
    [Fact]
    public void Remove_FromEmpty_ShouldThrow()
    {
        Assert.Throws<StatisticsTrackerException>(() =>
        {
            var tracker = new AdvancedStatisticsTracker();
            
            tracker.Remove(1d);
        });
    }

    [Fact]
    public void Remove_WithTooHighQuantity_ShouldThrow()
    {
        Assert.Throws<StatisticsTrackerException>(
            () =>
            {
                var tracker = AdvancedStatisticsTracker.From(10d);
                
                tracker.Remove(10d, 2);
            });
    }

    [Fact]
    public void Add_WithInfiniteDecimals()
    {
        var tracker = AdvancedStatisticsTracker.From(Math.PI);
        
        tracker.IntegerMultiplier.ShouldBe(10000); // 10000 is max
    }
    
    [Fact]
    public void AddAndRemove_WithSameQuantity_ShouldBeEmpty()
    {
        var tracker = new AdvancedStatisticsTracker();
        tracker.Add(1d, 10);
        tracker.Remove(1d, 10);
        tracker.IsEmpty().ShouldBeTrue();

        var stats = tracker.TakeSnapshot();
        
        stats.ShouldBe(AdvancedStatistics.Empty);
    }

    [Fact]
    public void Empty_TakeSnapshot_ShouldBeNull()
    {
        new AdvancedStatisticsTracker().TakeSnapshot().ShouldBe(AdvancedStatistics.Empty);
    }
    
    [Fact]
    public void AddAndRemove_WithCountGreaterThanOne()
    {
        var tracker = AdvancedStatisticsTracker.From(20d);
        
        tracker.IsEmpty().ShouldBeFalse();
        tracker.GreatestCommonDivisor.ShouldNotBeNull();
        
        tracker.GreatestCommonDivisor.ShouldBe(20L);
        tracker.IntegerMultiplier.ShouldBe(1L);
        
        tracker.Add(4d, 5L);
        
        tracker.GreatestCommonDivisor.ShouldBe(4L);
        tracker.IntegerMultiplier.ShouldBe(1L);

        tracker.IntegerMultipliers.ShouldNotBeNull();
        tracker.IntegerMultipliers.ShouldContain(new KeyValuePair<long, long>(1L, 6L));

        tracker.Frequencies.ShouldNotBeNull();
        tracker.Frequencies.ShouldContain(new KeyValuePair<double, long>(20d, 1L));
        tracker.Frequencies.ShouldContain(new KeyValuePair<double, long>(4d, 5L));
        
        tracker.Remove(4d, 5L);
        
        tracker.GreatestCommonDivisor.ShouldBe(20L);
        tracker.IntegerMultiplier.ShouldBe(1L);
        
        tracker.IntegerMultipliers.ShouldNotBeNull();
        tracker.IntegerMultipliers.ShouldContain(new KeyValuePair<long, long>(1L, 1L));

        tracker.Frequencies.ShouldNotBeNull();
        tracker.Frequencies.ShouldContain(new KeyValuePair<double, long>(20d, 1L));
    }

    [Fact]
    public void AddAndRemove_WithDecimalValues()
    {
        var tracker = AdvancedStatisticsTracker.From(0.05, 0.2, 2, 20, 400, 8000);

        tracker.GreatestCommonDivisor.ShouldNotBeNull();
        tracker.GreatestCommonDivisor.ShouldBe(5L);
        tracker.IntegerMultiplier.ShouldBe(100L);
        
        tracker.Remove(0.05);
        
        tracker.GreatestCommonDivisor.ShouldBe(2L);
        tracker.IntegerMultiplier.ShouldBe(10L);
        
        tracker.Add(0.009);
        tracker.GreatestCommonDivisor.ShouldBe(1L);
        tracker.IntegerMultiplier.ShouldBe(1000L);
        
        tracker.Remove(new [] {0.2, 0.009 });

        tracker.GreatestCommonDivisor.ShouldBe(2L);
        tracker.IntegerMultiplier.ShouldBe(1L);
    }
    
    [Fact]
    public void Equality()
    {
        var values = new[] {1d, 2d, 3d, 4d, 10d};

        var tracker1 = values.TrackAdvancedStatistics();
        var tracker2 = new AdvancedStatisticsTracker(values);
        
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
    public void Multiply()
    {
        var tracker = AdvancedStatisticsTracker.From(2, 4, 6).Multiply(3) as AdvancedStatisticsTracker;

        tracker.ShouldNotBeNull();
        tracker.IsEmpty().ShouldBeFalse();
        tracker.GreatestCommonDivisor.ShouldBe(6L);
        tracker.IntegerMultiplier.ShouldBe(1L);

    }

    [Fact]
    public void TakeSnapshot_ProbabilitiesShouldSumTo1()
    {
        var snapshot = new AdvancedStatisticsTracker(Enumerable.Range(0, 99).Select(x => Random.Shared.NextDouble() * 10d))
            .TakeSnapshot();

        snapshot.ShouldNotBeNull();
        snapshot.Probabilities.Values.Sum().ShouldBe(1d, 1e-10);
    }
    
    [Fact]
    public void SerializeAndSerialize()
    {
        var tracker = AdvancedStatisticsTracker.From(1, 2, 3);
        var json = JsonSerializer.Serialize(tracker);

        var deserTracker = JsonSerializer.Deserialize<AdvancedStatisticsTracker>(json);

        tracker.Equals(deserTracker).ShouldBeTrue();

        var json2 = JsonSerializer.Serialize(deserTracker);
        
        json.ShouldBe(json2);
    }
        
}