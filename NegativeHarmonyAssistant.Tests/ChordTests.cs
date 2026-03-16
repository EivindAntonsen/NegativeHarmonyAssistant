namespace NegativeHarmonyAssistant.Tests;

public class ChordTests
{
    [Fact]
    public void Parse_C_ShouldResultIn_NotesCEG()
    {
        var chord = Chord.Parse("C");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 4);
        Assert.Contains(chord.Notes, n => n.PitchClass == 7);
        Assert.Equal(3, chord.Notes.Count);
    }

    [Fact]
    public void CMajor_ShouldResultIn_RootToThirdIntervalIs4()
    {
        var chord = Chord.Parse("C");
        
        var root = chord.Notes.First(n => n.PitchClass == 0);
        var third = chord.Notes.First(n => n.PitchClass == 4);
        
        var interval = (third.AbsolutePitch - root.AbsolutePitch + 12) % 12;
        Assert.Equal(4, interval);
    }

    [Fact]
    public void Parse_Cm_ShouldResultIn_NotesCEbG()
    {
        var chord = Chord.Parse("Cm");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 3);
        Assert.Contains(chord.Notes, n => n.PitchClass == 7);
    }

    [Fact]
    public void ParseCMinor_ShouldResultIn_NotesCEbG()
    {
        var chord = Chord.Parse("C minor");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 3);
        Assert.Contains(chord.Notes, n => n.PitchClass == 7);
    }

    [Fact]
    public void Parse_Cmaj7_ShouldResultIn_NotesCEGB()
    {
        var chord = Chord.Parse("Cmaj7");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 4);
        Assert.Contains(chord.Notes, n => n.PitchClass == 7);
        Assert.Contains(chord.Notes, n => n.PitchClass == 11);
    }

    [Fact]
    public void Parse_C7_ShouldResultIn_NotesCEGBb()
    {
        var chord = Chord.Parse("C7");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 4);
        Assert.Contains(chord.Notes, n => n.PitchClass == 7);
        Assert.Contains(chord.Notes, n => n.PitchClass == 10);
    }

    [Fact]
    public void Parse_Cm7_ShouldResultIn_NotesCEbGBb()
    {
        var chord = Chord.Parse("Cm7");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 3);
        Assert.Contains(chord.Notes, n => n.PitchClass == 7);
        Assert.Contains(chord.Notes, n => n.PitchClass == 10);
    }

    [Fact]
    public void Parse_Cdim_ShouldResultIn_NotesCEbGb()
    {
        var chord = Chord.Parse("Cdim");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 3);
        Assert.Contains(chord.Notes, n => n.PitchClass == 6);
    }

    [Fact]
    public void Parse_Cdim7_ShouldResultIn_NotesCEbGbA()
    {
        var chord = Chord.Parse("Cdim7");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 3);
        Assert.Contains(chord.Notes, n => n.PitchClass == 6);
        Assert.Contains(chord.Notes, n => n.PitchClass == 9);
    }

    [Fact]
    public void Parse_Cm7b5_ShouldResultIn_NotesCEbGbBb()
    {
        var chord = Chord.Parse("Cm7b5");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 3);
        Assert.Contains(chord.Notes, n => n.PitchClass == 6);
        Assert.Contains(chord.Notes, n => n.PitchClass == 10);
    }

    [Fact]
    public void Parse_Caug_ShouldResultIn_NotesCEGSharp()
    {
        var chord = Chord.Parse("Caug");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 4);
        Assert.Contains(chord.Notes, n => n.PitchClass == 8);
    }

    [Fact]
    public void Parse_Csus4_ShouldResultIn_NotesCFG()
    {
        var chord = Chord.Parse("Csus4");
        
        Assert.Contains(chord.Notes, n => n.PitchClass == 0);
        Assert.Contains(chord.Notes, n => n.PitchClass == 5);
        Assert.Contains(chord.Notes, n => n.PitchClass == 7);
    }

    [Fact]
    public void UnrecognizableNotes_ShouldResultIn_Unknown()
    {
        var notes = new List<Note> { Note.Parse("C4"), Note.Parse("D4"), Note.Parse("E4") };
        var name = Chord.Identify(notes);
        
        Assert.Equal("Unknown", name);
    }
}
