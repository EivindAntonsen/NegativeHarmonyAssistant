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
    public void D_F_A_C_InCMinorKey_ShouldResultIn_Sorted_G3_Bb3_D4_F4()
    {
        var inputNotes = Note.ParseSequence(["D", "F", "A", "C"]);
        var (mappedNotes, context) = HarmonyMapper.MapNegativeWithContext(inputNotes, "C minor");

        Assert.Equal("G3", mappedNotes[0].ToString());
        Assert.Equal("Bb3", mappedNotes[1].ToString());
        Assert.Equal("D4", mappedNotes[2].ToString());
        Assert.Equal("F4", mappedNotes[3].ToString());
    }

    [Fact]
    public void Ab4_C5_Eb5_G5_InCMinorKey_ShouldResultIn_SortedOutput()
    {
        var inputNotes = Note.ParseSequence(["Ab4", "C5", "Eb5", "G5"]);
        var (mappedNotes, context) = HarmonyMapper.MapNegativeWithContext(inputNotes, "C minor");

        // The user's example resulted in B5, G5, E5, C5.
        // We want them to be sorted ascendingly: C5, E5, G5, B5.
        
        Assert.Equal("C5", mappedNotes[0].ToString());
        Assert.Equal("E5", mappedNotes[1].ToString());
        Assert.Equal("G5", mappedNotes[2].ToString());
        Assert.Equal("B5", mappedNotes[3].ToString());
    }

    [Fact]
    public void SpreadNotes_ShouldResultIn_CondensedOutput()
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
    public void E_Minor_Bb_F_D_Should_Map_To_Structural_Mirror()
    {
        // E Minor: Tonic E(4), Dominant B(11). Axis 7.5.
        // Bb4 (70) -> (159 - 70) = 89 (F6/E#6).
        // F5 (77) -> (159 - 77) = 82 (Bb5/A#5).
        // D6 (86) -> (159 - 86) = 73 (Db5/C#5).
        
        var inputNotes = Note.ParseSequence(["Bb4", "F5", "D6"]);
        var (mappedNotes, context) = HarmonyMapper.MapNegativeWithContext(inputNotes, "E Minor");
        
        // Re-apply naming as Program.ProcessInput does
        var finalNotes = mappedNotes.Select(n => Note.FromAbsolutePitch(n.AbsolutePitch, context)).ToList();
        
        // Final notes sorted by absolute pitch:
        // PC 1 (Db5/C#5), PC 10 (Bb5/A#5), PC 5 (F6/E#6)
        Assert.Equal(1, finalNotes[0].PitchClass);
        Assert.Equal(10, finalNotes[1].PitchClass);
        Assert.Equal(5, finalNotes[2].PitchClass);
        
        // Spelling should be in B Mixolydian (B, C#, D#, E, F#, G#, A).
        // PC 1 is Diatonic (C#).
        Assert.Equal("C#5", finalNotes[0].ToString());
    }
}
