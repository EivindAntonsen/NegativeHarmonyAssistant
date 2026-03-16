namespace NegativeHarmonyAssistant.Tests;

public class InputStyleTests
{
    [Fact]
    public void Style1_WithOctaves_ShouldParseCorrectly()
    {
        var inputs = new[] { "C2", "E2", "G2", "C3" };
        var notes = Note.ParseSequence(inputs);
        
        Assert.Equal(4, notes.Count);
        Assert.Equal(2, (int)notes[0].Octave);
        Assert.Equal(2, (int)notes[1].Octave);
        Assert.Equal(2, (int)notes[2].Octave);
        Assert.Equal(3, (int)notes[3].Octave);
    }

    [Fact]
    public void Style2_WithoutOctaves_ShouldAscend()
    {
        var inputs = new[] { "C", "E", "G", "C" };
        var notes = Note.ParseSequence(inputs);
        
        Assert.Equal(4, notes.Count);
        Assert.Equal(4, (int)notes[0].Octave);
        Assert.Equal(4, (int)notes[1].Octave);
        Assert.Equal(4, (int)notes[2].Octave);
        Assert.Equal(5, (int)notes[3].Octave);
        
        Assert.True(notes[1].AbsolutePitch > notes[0].AbsolutePitch);
        Assert.True(notes[2].AbsolutePitch > notes[1].AbsolutePitch);
        Assert.True(notes[3].AbsolutePitch > notes[2].AbsolutePitch);
    }

    [Fact]
    public void Style2_MixedOctaves_ShouldHandleTransitions()
    {
        var inputs = new[] { "G3", "C", "E" };
        var notes = Note.ParseSequence(inputs);
        
        Assert.Equal(3, notes.Count);
        Assert.Equal(3, (int)notes[0].Octave);
        Assert.Equal(4, (int)notes[1].Octave); // First C after G3
        Assert.Equal(4, (int)notes[2].Octave); // First E after C4
    }

    [Fact]
    public void Style2_SameNote_ShouldGoToNextOctave()
    {
        var inputs = new[] { "C", "C" };
        var notes = Note.ParseSequence(inputs);
        
        Assert.Equal(2, notes.Count);
        Assert.Equal(4, (int)notes[0].Octave);
        Assert.Equal(5, (int)notes[1].Octave);
    }
}
