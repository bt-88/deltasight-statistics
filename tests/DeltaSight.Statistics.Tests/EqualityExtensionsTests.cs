using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace DeltaSight.Statistics.Tests;

public class EqualityExtensionsTests
{
    [Fact]
    public void ContentEquals_WithEmptyDictionaries()
    {
        var a = new Dictionary<string, string>();
        var b = new Dictionary<string, string>();
        
        a.ContentEquals(b).ShouldBeTrue();
        
        b.Add("jow","dudes");
        
        a.ContentEquals(b).ShouldBeFalse();
    }

    [Fact]
    public void ContentEquals_WithDifferentSorting_ShouldBeTrue()
    {
        var a = new Dictionary<string, string>
        {
            {"foo", "bar "},
            {"bar", "baz"}
        };
        
        var b = new Dictionary<string, string>
        {
            {"bar", "baz"},
            {"foo", "bar "}
        };
        
        a.ContentEquals(b).ShouldBeTrue();
    }
    
    [Fact]
    public void ContentEquals_WithOverlappingEntries()
    {
        var a = new Dictionary<string, string>
        {
            {"foo", "bar "}
        };
        
        var b = new Dictionary<string, string>
        {
            {"bar", "baz"},
            {"foo", "bar "}
        };
        
        a.ContentEquals(b).ShouldBeFalse();
    }
    
    [Fact]
    public void ContentEquals_WithNullEntryValues()
    {
        var a = new Dictionary<string, string?>
        {
            {"foo", null}
        };
        
        var b = new Dictionary<string, string?>
        {
            {"foo", null}
        };
        
        a.ContentEquals(b).ShouldBeTrue();
    }
    
    [Fact]
    public void ContentEquals_WithNull()
    {
        Dictionary<string, string>? a = null;
        Dictionary<string, string>? b = null;
        
        a.ContentEquals(b).ShouldBeTrue();
    }
}