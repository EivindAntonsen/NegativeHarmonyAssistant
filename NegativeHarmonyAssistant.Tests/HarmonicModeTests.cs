using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class HarmonicModeTests
{
    [Fact]
    public void Parse_D_HarmonicMinor_ShouldSucceed()
    {
        var context = KeyContext.Parse("D Harmonic Minor");
        Assert.Equal(NoteName.D, context.Tonic.NoteName);
        Assert.Equal(Mode.HarmonicMinor, context.Mode);
    }

    [Fact]
    public void DHarmonicMinor_DiatonicScale_ShouldHaveCorrectNotes()
    {
        var context = KeyContext.Parse("D Harmonic Minor");
        var intervals = Mode.HarmonicMinor.GetIntervals();
        var notes = intervals.Select(i => Note.FromAbsolutePitch(context.Tonic.AbsolutePitch + i, context)).ToList();
        
        // D, E, F, G, A, Bb, C#
        Assert.Equal("D4", notes[0].ToString());
        Assert.Equal("E4", notes[1].ToString());
        Assert.Equal("F4", notes[2].ToString());
        Assert.Equal("G4", notes[3].ToString());
        Assert.Equal("A4", notes[4].ToString());
        Assert.Equal("Bb4", notes[5].ToString());
        Assert.Equal("C#5", notes[6].ToString());
    }

    [Fact]
    public void CHarmonicMinor_To_CHarmonicMajor_NegativeMapping()
    {
        // C Harmonic Minor: C D Eb F G Ab B
        // Axis C-G: C <-> G, D <-> F, Eb <-> E, F <-> D, G <-> C, Ab <-> B, B <-> Ab
        // Result: G F E D C B Ab -> C D E F G Ab B (C Harmonic Major)
        
        var inputNotes = Note.ParseSequence(["C4", "Eb4", "G4", "B4"]); // Cm(maj7)
        var (mappedNotes, context) = HarmonyMapper.MapNegativeWithContext(inputNotes, "C Harmonic Minor");
        
        Assert.Equal(Mode.HarmonicMajor, context.Mode);
        
        var resultChord = Chord.Identify(mappedNotes, context);
        // G, E, C, Ab -> Abmaj7#5 or Cmaj7b6 or something?
        // Ab C E G -> Abmaj7#5
        Assert.Equal("Abmaj7#5", resultChord);
    }

    [Fact]
    public void DHarmonicMinor_V_Chord_ShouldBe_A_Major()
    {
        var context = KeyContext.Parse("D Harmonic Minor");
        var notes = Note.ParseSequence(["A4", "C#5", "E5"]);
        var name = Chord.Identify(notes, context);
        Assert.Equal("A", name);
    }
}
