using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class MathematicalCorrectnessTests
{
    [Theory]
    [InlineData("C", "G")]    // C (0) -> G (7)
    [InlineData("C#", "F#")] // C# (1) -> F# (6)
    [InlineData("Db", "Gb")] // Db (1) -> Gb (6)
    [InlineData("D", "F")]    // D (2) -> F (5)
    [InlineData("D#", "E")]   // D# (3) -> E (4)
    [InlineData("Eb", "E")]   // Eb (3) -> E (4)
    [InlineData("E", "Eb")]   // E (4) -> Eb (3)
    [InlineData("F", "D")]    // F (5) -> D (2)
    [InlineData("F#", "C#")]  // F# (6) -> C# (1)
    [InlineData("Gb", "Db")]  // Gb (6) -> Db (1)
    [InlineData("G", "C")]    // G (7) -> C (0)
    [InlineData("G#", "B")]   // G# (8) -> B (11)
    [InlineData("Ab", "B")]   // Ab (8) -> B (11)
    [InlineData("A", "Bb")]   // A (9) -> Bb (10)
    [InlineData("A#", "A")]   // A# (10) -> A (9)
    [InlineData("Bb", "A")]   // Bb (10) -> A (9)
    [InlineData("B", "Ab")]   // B (11) -> Ab (8)
    public void CMajor_NoteMappings_ShouldBeMathematicallyCorrect(string inputNote, string expectedNegativeNote)
    {
        // Axis in C Major is C/G (0, 7)
        // Reflection: (0+7) - Note
        
        var notes = new List<Note> { Note.Parse(inputNote + "4") };
        var (mapped, context) = HarmonyMapper.MapNegativeWithContext(notes, "C Major");
        
        var result = mapped[0].ToString(false);
        // We use PitchClass because the exact spelling might vary based on context, but let's see if the defaults match
        Assert.Equal(Note.Parse(expectedNegativeNote + "4").PitchClass, mapped[0].PitchClass);
    }
    
    [Theory]
    [InlineData("C Major", "C", "G")]
    [InlineData("G Major", "G", "D")]
    [InlineData("E Minor", "E", "B")]
    public void Tonic_ShouldAlwaysMapToDominant(string key, string tonic, string dominant)
    {
        var notes = new List<Note> { Note.Parse(tonic + "4") };
        var (mapped, context) = HarmonyMapper.MapNegativeWithContext(notes, key);
        
        Assert.Equal(Note.Parse(dominant + "4").PitchClass, mapped[0].PitchClass);
    }
}
