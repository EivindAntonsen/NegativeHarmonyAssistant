using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class ParsingTests
{
    [Fact]
    public void Style1_WithOctaves_ShouldParseCorrectly()
    {
        var inputs = new[] { "C2", "E2", "G2", "C3" };
        var notes = Note.ParseSequence(inputs);
        Assert.Equal(4, notes.Count);
        Assert.Equal(2, (int)notes[0].Octave);
        Assert.Equal(3, (int)notes[3].Octave);
    }

    [Fact]
    public void Style2_WithoutOctaves_ShouldUseNearestPitch()
    {
        // Start at C4 (default)
        // E should be E4 (up 4 semitones vs down 8)
        // G should be G4 (up 3 vs down 9)
        // C should be C5 (up 5 vs down 7 - Tie goes UP in our logic)
        var inputs = new[] { "C", "E", "G", "C" };
        var notes = Note.ParseSequence(inputs);
        Assert.Equal(4, notes.Count);
        Assert.Equal(4, (int)notes[0].Octave);
        Assert.Equal(4, (int)notes[1].Octave);
        Assert.Equal(4, (int)notes[2].Octave);
        Assert.Equal(5, (int)notes[3].Octave);
    }

    [Fact]
    public void Style2_MixedOctaves_ShouldHandleTransitions()
    {
        var inputs = new[] { "G3", "C", "E" };
        var notes = Note.ParseSequence(inputs);
        Assert.Equal(3, notes.Count);
        Assert.Equal(3, (int)notes[0].Octave);
        Assert.Equal(4, (int)notes[1].Octave); // Nearest C after G3 is C4
        Assert.Equal(4, (int)notes[2].Octave); 
    }

    [Fact]
    public void Style2_SameNote_ShouldStayInSameOctave()
    {
        var inputs = new[] { "C", "C" };
        var notes = Note.ParseSequence(inputs);
        Assert.Equal(2, notes.Count);
        Assert.Equal(4, (int)notes[0].Octave);
        Assert.Equal(4, (int)notes[1].Octave);
    }

    [Theory]
    [InlineData("Dm", NoteName.D, null, Mode.Minor)]
    [InlineData("C#maj", NoteName.C, Accidental.Sharp, Mode.Major)]
    [InlineData("Eb Minor", NoteName.E, Accidental.Flat, Mode.Minor)]
    [InlineData("G", NoteName.G, null, Mode.Major)]
    [InlineData("F Ionian", NoteName.F, null, Mode.Major)]
    [InlineData("A Aeolian", NoteName.A, null, Mode.Minor)]
    [InlineData("B Dorian", NoteName.B, null, Mode.Dorian)]
    public void KeyContext_Parse_ShouldHandleVariousFormats(string input, NoteName expectedTonic, Accidental? expectedAcc, Mode expectedMode)
    {
        var context = KeyContext.Parse(input);
        Assert.Equal(expectedTonic, context.Tonic.NoteName);
        Assert.Equal(expectedAcc, context.Tonic.Accidental);
        Assert.Equal(expectedMode, context.Mode);
    }

    [Theory]
    [InlineData("C", false)]
    [InlineData("Cm", true)]
    [InlineData("C7", true)]
    [InlineData("dim", true)]
    [InlineData("aug", true)]
    [InlineData("Cmaj7", true)]
    [InlineData("Csus4", true)]
    [InlineData("C2", false)] // Note with octave
    [InlineData("C#", false)] // Note with accidental
    [InlineData("C#4", false)]
    [InlineData("UnknownChord", true)] // Anything not matching note regex is treated as chord
    public void IsChordHeuristic_ShouldCorrectIdentifyChords(string input, bool expected)
    {
        var result = Program.IsChordHeuristic(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("C, E, G | [F Major] F, A, C", "C Major", "Modulation to [F Major]")]
    [InlineData("E, G, B | [G Major] G, B, D", "E Minor", "Modulation to [G Major]")]
    public void ProcessInput_WithModulations_ShouldIncludeTagsInOutput(string input, string initialKey, string expectedModulation)
    {
        var output = CaptureOutput(() => Program.ProcessInput(input, initialKey));
        Assert.Contains(expectedModulation, output);
    }

    private string CaptureOutput(System.Action action)
    {
        var originalOut = System.Console.Out;
        using var sw = new System.IO.StringWriter();
        System.Console.SetOut(sw);
        try
        {
            action();
            return sw.ToString();
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }
}
