using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class KeyInferenceTests
{
    [Fact]
    public void Infer_CMajorNotes_ShouldReturnCMajor()
    {
        var notes = new List<Note>
        {
            Note.Parse("C4"), Note.Parse("E4"), Note.Parse("G4"), // C Major triad
            Note.Parse("F4"), Note.Parse("A4"), Note.Parse("B4")  // F Major / G7 notes
        };

        var result = KeyInferrer.Infer(notes);

        Assert.Equal("C Major", result.ToString());
    }

    [Fact]
    public void Infer_AMinorNotes_ShouldReturnAMinorDueToFirstNoteBonus()
    {
        var notes = new List<Note>
        {
            Note.Parse("A4"), Note.Parse("C5"), Note.Parse("E5"), // A minor triad
            Note.Parse("D5"), Note.Parse("F5"), Note.Parse("G5"), Note.Parse("B4")
        };

        var result = KeyInferrer.Infer(notes);

        // A Minor and C Major have same pitch classes. 
        // With the first note bonus, A Minor should now win.
        Assert.Equal("A Minor", result.ToString());
    }

    [Fact]
    public void Infer_GMajorNotes_ShouldReturnGMajor()
    {
        var notes = new List<Note>
        {
            Note.Parse("G4"), Note.Parse("B4"), Note.Parse("D5"), // G triad
            Note.Parse("C5"), Note.Parse("E5"), Note.Parse("F#5"), Note.Parse("A4")
        };

        var result = KeyInferrer.Infer(notes);

        Assert.Equal("G Major", result.ToString());
    }

    [Fact]
    public void Infer_EbMajorNotes_ShouldReturnEbMajor()
    {
        var notes = new List<Note>
        {
            Note.Parse("Eb4"), Note.Parse("G4"), Note.Parse("Bb4"), // Eb triad
            Note.Parse("Ab4"), Note.Parse("C5"), Note.Parse("D5"), Note.Parse("F4")
        };

        var result = KeyInferrer.Infer(notes);

        Assert.Equal("Eb Major", result.ToString());
    }

    [Fact]
    public void Infer_EmptyNotes_ShouldReturnC4Major()
    {
        var result = KeyInferrer.Infer(new List<Note>());
        Assert.Equal("C Major", result.ToString());
    }

    [Fact]
    public void Infer_AMinorWithDominant_ShouldReturnAMinorOrHarmonicMinor()
    {
        var notes = new List<List<Note>>
        {
            new List<Note> { Note.Parse("A4"), Note.Parse("C5"), Note.Parse("E5") }, // Am
            new List<Note> { Note.Parse("E4"), Note.Parse("G#4"), Note.Parse("B4"), Note.Parse("D5") } // E7 (Dominant)
        };

        var result = KeyInferrer.Infer(notes);

        // With functional dominant bonus and G# present, Harmonic Minor is a very strong fit.
        // We accept A Harmonic Minor as it's musically accurate for this progression.
        Assert.Equal("A Harmonic Minor", result.ToString());
    }

    [Fact]
    public void Infer_CMajorWithSecondaryDominant_ShouldReturnCMajor()
    {
        var notes = new List<List<Note>>
        {
            new List<Note> { Note.Parse("C4"), Note.Parse("E4"), Note.Parse("G4") }, // C
            new List<Note> { Note.Parse("F4"), Note.Parse("A4"), Note.Parse("C5") }, // F
            new List<Note> { Note.Parse("G4"), Note.Parse("B4"), Note.Parse("D5") }, // G
            new List<Note> { Note.Parse("D4"), Note.Parse("F#4"), Note.Parse("A4"), Note.Parse("C5") } // D7 (Secondary Dominant)
        };

        var result = KeyInferrer.Infer(notes);

        // F# is chromatic in C major but the dominant bonus for D7 -> G and the F natural should make C Major win
        Assert.Equal("C Major", result.ToString());
    }
    
    [Fact]
    public void Infer_DMinorWithCSharp_ShouldPreferHarmonicMinor()
    {
        // D E F G A Bb C#
        var notes = new List<Note>
        {
            Note.Parse("D4"), Note.Parse("E4"), Note.Parse("F4"), Note.Parse("G4"), 
            Note.Parse("A4"), Note.Parse("Bb4"), Note.Parse("C#5")
        };

        var result = KeyInferrer.Infer(notes);

        Assert.Equal("D Harmonic Minor", result.ToString());
    }

    [Fact]
    public void Infer_DDorian_ShouldPreferDorian()
    {
        // D E F G A B C
        var notes = new List<Note>
        {
            Note.Parse("D4"), Note.Parse("E4"), Note.Parse("F4"), Note.Parse("G4"), 
            Note.Parse("A4"), Note.Parse("B4"), Note.Parse("C5")
        };

        var result = KeyInferrer.Infer(notes);
        Assert.Equal("D Dorian", result.ToString());
    }
}
