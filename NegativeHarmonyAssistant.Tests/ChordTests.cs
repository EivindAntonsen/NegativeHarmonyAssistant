using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class ChordTests
{
    [Theory]
    [InlineData("", 0, 4, 7)] // Major
    [InlineData("maj", 0, 4, 7)]
    [InlineData("major", 0, 4, 7)]
    [InlineData("m", 0, 3, 7)] // Minor
    [InlineData("min", 0, 3, 7)]
    [InlineData("minor", 0, 3, 7)]
    [InlineData("7", 0, 4, 7, 10)] // Dominant 7
    [InlineData("dom7", 0, 4, 7, 10)]
    [InlineData("maj7", 0, 4, 7, 11)] // Major 7
    [InlineData("m7", 0, 3, 7, 10)] // Minor 7
    [InlineData("min7", 0, 3, 7, 10)]
    [InlineData("dim", 0, 3, 6)] // Diminished
    [InlineData("dim7", 0, 3, 6, 9)] // Diminished 7
    [InlineData("m7b5", 0, 3, 6, 10)] // Half-diminished
    [InlineData("ø", 0, 3, 6, 10)]
    [InlineData("aug", 0, 4, 8)] // Augmented
    [InlineData("+", 0, 4, 8)]
    [InlineData("sus4", 0, 5, 7)] // Sus4
    [InlineData("sus2", 0, 2, 7)] // Sus2
    public void Parse_AllChordFormulas_ShouldHaveCorrectPitches(string formula, params int[] expectedIntervals)
    {
        var root = "C";
        var chord = Chord.Parse($"{root}{formula}");
        var rootPC = 0; // C
        
        foreach (var interval in expectedIntervals)
        {
            var expectedPC = (rootPC + interval) % 12;
            Assert.Contains(chord.Notes, n => n.PitchClass == expectedPC);
        }
        Assert.Equal(expectedIntervals.Length, chord.Notes.Count);
    }

    [Theory]
    [InlineData("C", "C")]
    [InlineData("Cm", "Cm")]
    [InlineData("C7", "C7")]
    [InlineData("Cmaj7", "Cmaj7")]
    [InlineData("Cm7", "Cm7")]
    [InlineData("Cdim", "Cdim")]
    [InlineData("Cdim7", "Cdim7")]
    [InlineData("Cm7b5", "Cm7b5")]
    [InlineData("Caug", "Caug")]
    [InlineData("Csus4", "Csus4")]
    [InlineData("Csus2", "Csus2")]
    public void Identify_AllChordTypes_ShouldReturnCorrectName(string input, string expectedName)
    {
        var chord = Chord.Parse(input);
        var identifiedName = Chord.Identify(chord.Notes);
        Assert.Equal(expectedName, identifiedName);
    }

    [Fact]
    public void Identify_ShouldReturnUnknown_ForUnrecognizableNotes()
    {
        var notes = new List<Note> { Note.Parse("C4"), Note.Parse("D4"), Note.Parse("E4") };
        var name = Chord.Identify(notes);
        Assert.Equal("Unknown", name);
    }

    [Fact]
    public void Identify_ShouldNotReturnOctave()
    {
        var notes = new List<Note> { Note.Parse("E4"), Note.Parse("G4"), Note.Parse("B4") };
        var name = Chord.Identify(notes);
        Assert.Equal("Em", name);
        Assert.DoesNotContain("4", name);
    }

    [Fact]
    public void ReSpell_CSharpMajor_ShouldPrefer_ESharp()
    {
        var notes = new List<Note> {
            Note.Parse("C#4"),
            Note.Parse("F4"),  // Enharmonic to E#4
            Note.Parse("G#4")
        };
        var respelled = Chord.ReSpell(notes);
        Assert.Contains(respelled, n => n.ToString() == "C#4");
        Assert.Contains(respelled, n => n.ToString() == "E#4");
        Assert.Contains(respelled, n => n.ToString() == "G#4");
    }

    [Fact]
    public void ReSpell_DbMajor_ShouldPrefer_F()
    {
        var notes = new List<Note> {
            Note.Parse("Db4"),
            Note.Parse("E#4"), // Enharmonic to F4
            Note.Parse("Ab4")
        };
        var context = KeyContext.Parse("Db Major");
        var respelled = Chord.ReSpell(notes, context);
        Assert.Contains(respelled, n => n.ToString() == "Db4");
        Assert.Contains(respelled, n => n.ToString() == "F4");
        Assert.Contains(respelled, n => n.ToString() == "Ab4");
    }
}
