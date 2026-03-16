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
        var mappedNotes = HarmonyMapper.MapNegative(inputNotes, "C minor");

        Assert.Equal("G3", mappedNotes[0].ToString());
        Assert.Equal("Bb3", mappedNotes[1].ToString());
        Assert.Equal("D4", mappedNotes[2].ToString());
        Assert.Equal("F4", mappedNotes[3].ToString());
    }

    [Fact]
    public void Ab4_C5_Eb5_G5_InCMinorKey_ShouldResultIn_SortedOutput()
    {
        var inputNotes = Note.ParseSequence(["Ab4", "C5", "Eb5", "G5"]);
        var mappedNotes = HarmonyMapper.MapNegative(inputNotes, "C minor");

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
    public void DuplicateNotes_ShouldBeOmitted_WhenGroupingByPitchClass()
    {
        var notes = new List<Note>
        {
            Note.Parse("C4"),
            Note.Parse("E4"),
            Note.Parse("G4"),
            Note.Parse("C5")
        };

        var result = notes.GroupBy(n => n.PitchClass).Select(g => g.First()).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("C4", result[0].ToString());
        Assert.Equal("E4", result[1].ToString());
        Assert.Equal("G4", result[2].ToString());
    }
}
