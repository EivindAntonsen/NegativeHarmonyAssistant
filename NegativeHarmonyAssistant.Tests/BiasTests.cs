using Xunit;
using System.Collections.Generic;
using NegativeHarmonyAssistant;

namespace NegativeHarmonyAssistant.Tests;

public class BiasTests
{
    [Fact]
    public void Infer_DDorianWithGChord_ShouldStillBeDDorian()
    {
        // Notes from D Dorian scale
        // Chords: Dm, G (characteristic Dorian IV chord)
        var noteGroups = new List<List<Note>>
        {
            new List<Note> { Note.Parse("D4"), Note.Parse("F4"), Note.Parse("A4") }, // Dm
            new List<Note> { Note.Parse("G4"), Note.Parse("B4"), Note.Parse("D5") }, // G (Major triad)
            new List<Note> { Note.Parse("D4"), Note.Parse("E4"), Note.Parse("F4"), Note.Parse("G4"), Note.Parse("A4"), Note.Parse("B4"), Note.Parse("C5") }
        };

        var result = KeyInferrer.Infer(noteGroups);

        // Current implementation will likely return C Major because G Major chord rewards C Major +2.0
        // and C Major is a relative key with higher simplicity bonus.
        Assert.Equal("D Dorian", result.ToString());
    }

    [Fact]
    public void Infer_AMinorWithOnlyAmTriad_ShouldBeAMinor()
    {
        // If we just play A minor triad.
        var notes = new List<Note> { Note.Parse("A4"), Note.Parse("C5"), Note.Parse("E5") };
        var result = KeyInferrer.Infer(notes);
        
        // A minor triad is also in C Major.
        // C Major has a slightly higher simplicity bias (+0.015 total).
        // A Minor has a lower simplicity bias (+0.009 total).
        // But First Note Bonus (+1.0) for A should make it win.
        Assert.Equal("A Minor", result.ToString());
    }
}
