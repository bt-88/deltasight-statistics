using System;
using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace DeltaSight.Statistics.Tests;

public class EmpiricalCDFTests
{
    [Fact]
    public void PrLessThanOrEqual()
    {
        var cdf = EmpiricalCDF<double>.FromSorted(new Dictionary<double, double>
        {
            { 1d, 0.2 },
            { 3d, 0.2},
            { 20d, 0.6}
        });
        
        cdf.PrLessThanOrEqual(double.MaxValue).ShouldBe(1d, 1e-6);
        cdf.PrLessThanOrEqual(double.MinValue).ShouldBe(0d, 1e-6);
        cdf.PrLessThanOrEqual(15).ShouldBe(0.4, 1e-6);
        cdf.PrLessThanOrEqual(19.9).ShouldBe(0.4, 1e-6);
        cdf.PrLessThanOrEqual(.1).ShouldBe(0d, 1e-6);
        cdf.PrLessThanOrEqual(1d).ShouldBe(0.2, 1e-6);
        cdf.PrLessThanOrEqual(1.1).ShouldBe(0.2, 1e-6);
        cdf.PrLessThanOrEqual(3d).ShouldBe(0.4, 1e-6);
        cdf.PrLessThanOrEqual(20d).ShouldBe(1d, 1e-6);
    }

    [Fact]
    public void FromSorted_WithUnsorted_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = EmpiricalCDF<double>.FromSorted(new Dictionary<double, double>
            {
                {1d, 0.2},
                {20d, 0.2},
                {3d, 0.6}
            });
        });
    }

    [Fact]
    public void FromSorted_WithIncompleteEntries_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = EmpiricalCDF<double>.FromSorted(new Dictionary<double, double>
            {
                {1d, 0.2}
            });
        });
    }
    
    [Fact]
    public void FromSorted_WithIncompatibleEntries_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = EmpiricalCDF<double>.FromSorted(new Dictionary<double, double>
            {
                {1d, 0.8},
                {2d, 1.2}
            });
        });
    }
}