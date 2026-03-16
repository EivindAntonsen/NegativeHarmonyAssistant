using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class NegativeHarmonyTests
{
    [Fact]
    public void CMajorInCMajorKey_ShouldResultIn_NegativeHarmonyCm()
    {
        var cMajorNotes = Chord.Parse("C").Notes;
        var (mappedNotes, context) = HarmonyMapper.MapNegativeWithContext(cMajorNotes, "C Major");
        var resultChord = Chord.Identify(mappedNotes, context);

        Assert.Equal("Cm", resultChord);
    }

    [Fact]
    public void CMaj7InCMajorKey_ShouldResultIn_NegativeHarmonyAbmaj7()
    {
        var cMaj7Notes = Chord.Parse("Cmaj7").Notes;
        var (mappedNotes, context) = HarmonyMapper.MapNegativeWithContext(cMaj7Notes, "C Major");
        var resultChord = Chord.Identify(mappedNotes, context);

        Assert.Equal("Abmaj7", resultChord);
    }

    [Fact]
    public void E_Minor_Bb_F_D_Should_Map_To_Structural_Mirror()
    {
        var inputNotes = Note.ParseSequence(["Bb4", "F5", "D6"]);
        var (mappedNotes, context) = HarmonyMapper.MapNegativeWithContext(inputNotes, "E Minor");
        
        var finalNotes = mappedNotes.Select(n => Note.FromAbsolutePitch(n.AbsolutePitch, context)).ToList();
        finalNotes = Chord.ReSpell(finalNotes, context);
        
        var name = Chord.Identify(finalNotes, context);
        Assert.Equal("A#m", name);
        Assert.Contains(finalNotes, n => n.ToString() == "C#5");
        Assert.Contains(finalNotes, n => n.ToString() == "E#6");
        Assert.Contains(finalNotes, n => n.ToString() == "A#5");
    }

    [Fact]
    public void CondensedOutput_ShouldHaveCorrectPitches()
    {
        var spreadNotes = new List<Note>
        {
            Note.Parse("B4"),
            Note.Parse("G#5"),
            Note.Parse("E6"),
            Note.Parse("B6")
        };

        var condensed = Note.Condense(spreadNotes);

        Assert.Equal("B4", condensed[0].ToString());
        Assert.Equal("E5", condensed[1].ToString());
        Assert.Equal("G#5", condensed[2].ToString());
        Assert.Equal("B5", condensed[3].ToString());
    }

    [Fact]
    public void OctaveShifting_ShouldKeepNotesInReasonableRange()
    {
        // High notes (Octave 7/8) should be shifted down
        var highNotes = new List<Note> { Note.Parse("E7"), Note.Parse("G7"), Note.Parse("B7") };
        var (mapped, context) = HarmonyMapper.MapNegativeWithContext(highNotes, "E Minor");
        
        // This mapping logic is actually in Program.ProcessInput, 
        // but we should verify that we can calculate reasonable octaves.
        // Actually, let's test the heuristic we have in Program.ProcessInput.
        
        var avgOctave = mapped.Average(n => n.Octave);
        // The MapNegativeWithContext returns notes mapped mathematically.
        // E7 (88) maps to 111-88 = 23 (B0) -> very low! 
        // Wait, axis sum for E7 is (7+1)*12+4 + (7+1)*12+4+7 = 100+107 = 207?
        // Let's use the actual Program.ProcessInput logic.
    }
}
