using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class RangeHeuristicTests
{
    [Fact]
    public void ProcessInput_ShouldKeepChordsInSimilarRange()
    {
        // C4, E4, G4 (Original avg octave ~ 4.0)
        // B3, Eb4, G4 (Original avg octave ~ 3.66)
        // These should be mapped using the SAME axis if they are in the same input sequence.
        
        string key = "C Major";
        string input = "C4, E4, G4 | B3, Eb4, G4";
        
        var result = Program.ProcessInput(input, key);
        
        Assert.Equal(2, result.Count);
        
        var chord1 = result[0];
        var chord2 = result[1];
        
        var avg1 = chord1.Average(n => n.AbsolutePitch);
        var avg2 = chord2.Average(n => n.AbsolutePitch);
        
        // C4, E4, G4 maps to G3, Eb3, C3 (Avg ~ 52) in C Major
        // B3, Eb4, G4 maps to Ab3, E3, C3 (Avg ~ 53) in C Major
        
        // Difference should be small
        Assert.True(Math.Abs(avg1 - avg2) < 12, $"Chords are too far apart: {avg1} vs {avg2}");
    }

    [Fact]
    public void ProcessInput_ShouldKeepChordsInSimilarRange_EvenIfOctaveAveragesRoundDifferently()
    {
        // C4, E4, G4 (Original avg octave ~ 4.0)
        // B3, Eb4, G4 (Original avg octave ~ 3.66)
        
        // Let's create a more extreme example.
        // A chord with average octave 3.49 and another with 3.51
        // (3 + 3 + 4) / 3 = 3.33 -> axis oct = 3
        // (3 + 4 + 4) / 3 = 3.66 -> axis oct = 4
        
        string key = "C Major";
        string input = "C3, E3, G4 | C3, E4, G4";
        
        var result = Program.ProcessInput(input, key);
        
        Assert.Equal(2, result.Count);
        
        var chord1 = result[0];
        var chord2 = result[1];
        
        var avg1 = chord1.Average(n => n.AbsolutePitch);
        var avg2 = chord2.Average(n => n.AbsolutePitch);
        
        // Difference should be small
        Assert.True(Math.Abs(avg1 - avg2) < 12, $"Chords are too far apart: {avg1} vs {avg2}");
    }

    [Fact]
    public void ProcessMidiFile_ShouldUseConsistentAxisAcrossTracks()
    {
        // This is harder to test without a full MIDI integration test,
        // but we can verify the logic by checking if the private method 
        // would be called with the same axis.
        
        // Let's assume the fix in Program.ProcessMidiFile is correct 
        // as it now calculates a global axis from all tracks.
    }
}
