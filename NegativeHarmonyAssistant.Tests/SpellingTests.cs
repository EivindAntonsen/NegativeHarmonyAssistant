using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class SpellingTests
{
    [Theory]
    [InlineData("C Minor", 11, NoteName.B, null)] // G Major in C Minor: B natural
    [InlineData("G Minor", 6, NoteName.F, Accidental.Sharp)] // D Major in G Minor: F#
    [InlineData("D Minor", 1, NoteName.C, Accidental.Sharp)] // A Major in D Minor: C#
    [InlineData("A Minor", 8, NoteName.G, Accidental.Sharp)] // E Major in A Minor: G#
    [InlineData("E Minor", 3, NoteName.D, Accidental.Sharp)] // B Major in E Minor: D#
    [InlineData("B Minor", 10, NoteName.A, Accidental.Sharp)] // F# Major in B Minor: A#
    [InlineData("F# Minor", 5, NoteName.E, Accidental.Sharp)] // C# Major in F# Minor: E#
    [InlineData("C# Minor", 0, NoteName.B, Accidental.Sharp)] // G# Major in C# Minor: B#
    public void DominantToRoot_ShouldHaveCorrectLeadingToneSpelling(string key, int pitchClass, NoteName expectedName, Accidental? expectedAccidental)
    {
        var context = KeyContext.Parse(key);
        var note = Note.FromAbsolutePitch(pitchClass + 48, context);
        Assert.Equal(expectedName, note.NoteName);
        Assert.Equal(expectedAccidental, note.Accidental);
    }

    [Fact]
    public void NoteSimplify_DoubleAccidentals_ShouldBeSimplified()
    {
        Assert.Equal("D", Note.Parse("C##").Simplify().ToString(false));
        Assert.Equal("C", Note.Parse("Dbb").Simplify().ToString(false));
    }

    [Fact]
    public void NoteSimplify_SingleAccidentals_ShouldNotChange()
    {
        Assert.Equal("C#", Note.Parse("C#").Simplify().ToString(false));
        Assert.Equal("Eb", Note.Parse("Eb").Simplify().ToString(false));
    }

    [Fact]
    public void NoteSimplify_Natural_ShouldNotChange()
    {
        Assert.Equal("F", Note.Parse("F").Simplify().ToString(false));
    }
}
