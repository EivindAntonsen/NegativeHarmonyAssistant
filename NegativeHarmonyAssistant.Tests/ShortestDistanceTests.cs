using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;
using System;

namespace NegativeHarmonyAssistant.Tests;

public class ShortestDistanceTests
{
    [Fact]
    public void ShortestDistance_ShouldKeepNotesCloseToOriginal()
    {
        // C4 (60) in C Major
        // Tonic C4 (60), Dominant G4 (67). AxisSum = 127.
        // Reflected C4 = 127 - 60 = 67 (G4). Distance = 7.
        // Reflected PC is G (7).
        // Closest G to 60 is G3 (55) [dist 5] or G4 (67) [dist 7].
        // So Shortest Distance should pick G3 (55).
        
        var notes = new List<Note> { Note.Parse("C4") };
        var (mapped, _) = HarmonyMapper.MapNegativeWithContext(notes, "C Major", shortestDistance: true);
        
        Assert.Single(mapped);
        // Note: HarmonyMapper returns mapped notes, but Program.ProcessInput does further re-spelling and simplification.
        // G3 (55) is indeed G3.
        Assert.Equal("G3", mapped[0].ToString());
    }

    [Fact]
    public void StandardReflection_ShouldFollowAxis()
    {
        var notes = new List<Note> { Note.Parse("C4") };
        var (mapped, _) = HarmonyMapper.MapNegativeWithContext(notes, "C Major", shortestDistance: false);
        
        Assert.Single(mapped);
        Assert.Equal("G4", mapped[0].ToString());
    }

    [Fact]
    public void ShortestDistance_ThroughProgramProcessInput_ShouldWork()
    {
        // Testing that the Program level doesn't override the shortest distance
        var resultGroups = Program.ProcessInput("C4", "C Major", shortestDistance: true);
        var mappedNote = resultGroups[0][0];
        
        Assert.Equal("G3", mappedNote.ToString());
    }

    [Fact]
    public void StandardReflection_ThroughProgramProcessInput_ShouldShiftOctaveByDefault()
    {
        // Standard reflection maps C4 to G4.
        // avgOctave of C4 is 4. avgOctave of G4 is 4.
        // octaveShift = 4 - 4 = 0.
        // So it stays G4.
        var resultGroups = Program.ProcessInput("C4", "C Major", shortestDistance: false);
        var mappedNote = resultGroups[0][0];
        
        Assert.Equal("G4", mappedNote.ToString());
    }
}
