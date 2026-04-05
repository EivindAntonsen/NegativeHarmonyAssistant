using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class NegativeHarmonyStructuralTests
{
    [Fact]
    public void MapNegative_WithPreserveStructure_ShouldMaintainMelodicDirection()
    {
        // C4, D4, E4 in C Major
        // C4 maps to G4 (axis C-G)
        // D4 maps to F
        // E4 maps to Eb
        
        var notes = new List<Note> 
        { 
            Note.Parse("C4"), 
            Note.Parse("D4"), 
            Note.Parse("E4") 
        };
        
        // Traditional reflection: G4, F4, Eb4 (Descending)
        // Note: MapNegative sorts results by pitch, so we'll check the content
        var traditional = HarmonyMapper.MapNegative(notes, "C Major", preserveStructure: false);
        var traditionalPitches = traditional.Select(n => n.AbsolutePitch).ToList();
        Assert.Contains(67, traditionalPitches); // G4
        Assert.Contains(65, traditionalPitches); // F4
        Assert.Contains(63, traditionalPitches); // Eb4
        Assert.Equal(63, traditional[0].AbsolutePitch); // Lowest note should be first due to sorting
        
        // Structural: G4, F5, Eb6 (Ascending)
        // Structural does NOT sort, to preserve melody order.
        var structural = HarmonyMapper.MapNegative(notes, "C Major", preserveStructure: true);
        Assert.Equal(67, structural[0].AbsolutePitch); // G4
        Assert.True(structural[1].AbsolutePitch >= structural[0].AbsolutePitch, "D4 should map to something >= G4");
        Assert.Equal(77, structural[1].AbsolutePitch); // F5 (closest F >= 67)
        Assert.True(structural[2].AbsolutePitch >= structural[1].AbsolutePitch, "E4 should map to something >= F5");
        Assert.Equal(87, structural[2].AbsolutePitch); // Eb6 (closest Eb >= 77)
    }

    [Fact]
    public void MapNegative_WithPreserveStructure_Descending_ShouldMaintainDirection()
    {
        // G4, F4, E4 in C Major
        // G4 maps to C4
        // F4 maps to D4
        // E4 maps to Eb4
        
        var notes = new List<Note> 
        { 
            Note.Parse("G4"), 
            Note.Parse("F4"), 
            Note.Parse("E4") 
        };
        
        // Traditional: C4, D4, Eb4 (Ascending)
        var traditional = HarmonyMapper.MapNegative(notes, "C Major", preserveStructure: false);
        Assert.Equal(60, traditional[0].AbsolutePitch); // C4
        Assert.Equal(62, traditional[1].AbsolutePitch); // D4
        Assert.Equal(63, traditional[2].AbsolutePitch); // Eb4
        
        // Structural: C4, D3, Eb2 (Descending)
        var structural = HarmonyMapper.MapNegative(notes, "C Major", preserveStructure: true);
        Assert.Equal(60, structural[0].AbsolutePitch); // C4
        Assert.True(structural[1].AbsolutePitch <= structural[0].AbsolutePitch, "F4 should map to something <= C4");
        Assert.Equal(50, structural[1].AbsolutePitch); // D3 (closest D <= 60)
        Assert.True(structural[2].AbsolutePitch <= structural[1].AbsolutePitch, "E4 should map to something <= D3");
        Assert.Equal(39, structural[2].AbsolutePitch); // Eb2 (closest Eb <= 50)
    }
    
    [Fact]
    public void MapNegative_WithPreserveStructure_Chords_ShouldBeInversions()
    {
        // C4, E4, G4 (C Major chord)
        // Handled as a sequence in HarmonyMapper.MapNegative if preserveStructure is true
        // C4 -> G4
        // E4 -> Eb5 (closest Eb >= G4)
        // G4 -> C6 (closest C >= Eb5)
        
        var notes = new List<Note> 
        { 
            Note.Parse("C4"), 
            Note.Parse("E4"), 
            Note.Parse("G4") 
        };
        
        var structural = HarmonyMapper.MapNegative(notes, "C Major", preserveStructure: true);
        
        Assert.Equal(67, structural[0].AbsolutePitch); // G4
        Assert.Equal(75, structural[1].AbsolutePitch); // Eb5
        Assert.Equal(84, structural[2].AbsolutePitch); // C6
        
        // Identify resulting chord
        var resultName = Chord.Identify(structural);
        Assert.Contains("Cm", resultName); // G, Eb, C is a Cm chord (inversion)
    }
}
