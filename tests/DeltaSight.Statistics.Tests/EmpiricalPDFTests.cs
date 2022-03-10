using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace DeltaSight.Statistics.Tests;

public class EmpiricalPDFTests
{
    [Fact]
    public void FromUnsorted()
    {
        var pdf = EmpiricalPDF<double>.FromUnsorted(new Dictionary<double, double>
        {
            { 1d, 0.2 },
            { 20d, 0.2},
            { 3d, 0.6}
        });
        
        pdf.Probabilities.Keys.ElementAt(0).ShouldBe(1d);
        pdf.Probabilities.Keys.ElementAt(1).ShouldBe(3d);
        pdf.Probabilities.Keys.ElementAt(2).ShouldBe(20d);
    }
    
    [Fact]
    public void FromSorted()
    {
        var pdf = EmpiricalPDF<double>.FromSorted(new Dictionary<double, double>
        {
            { 1d, 0.2 },
            { 2d, 0.2},
            { 3d, 0.6}
        });
        
        pdf.Probabilities.Keys.ElementAt(0).ShouldBe(1d);
        pdf.Probabilities.Keys.ElementAt(1).ShouldBe(2d);
        pdf.Probabilities.Keys.ElementAt(2).ShouldBe(3d);
    }

    [Fact]
    public void PDF()
    {
        var pdf = EmpiricalPDF<double>.FromSorted(new Dictionary<double, double>
        {
            {1d, 0.2},
            {2d, 0.2},
            {3d, 0.6}
        });
        
        pdf.PrEquals(20).ShouldBe(0d);
        pdf.PrEquals(1.5).ShouldBe(0d);
        pdf.PrEquals(1d).ShouldBe(0.2);
    }

    [Fact]
    public void FromSorted_WithUnsortedDensities_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = EmpiricalPDF<double>.FromSorted(new Dictionary<double, double>
            {
                {1d, 0.2},
                {20d, 0.2},
                {3d, 0.6}
            }, true);
        });
    }
}