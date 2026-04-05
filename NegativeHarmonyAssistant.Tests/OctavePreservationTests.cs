using Xunit;
using NegativeHarmonyAssistant;
using System.Collections.Generic;
using System.Linq;
using System;

namespace NegativeHarmonyAssistant.Tests;

public class OctavePreservationTests
{
    [Fact]
    public void ProcessInput_ShouldPreserveOriginalOctaveRange()
    {
        // Low D (D2 is 38, D1 is 26) on 7th string.
        // Let's assume D2.
        string key = "C Major";
        string input = "D2, F2, A2"; // Dm (Original avg octave 2.0)
        
        var result = Program.ProcessInput(input, key);
        
        var chord = result[0];
        var avgOctave = chord.Average(n => n.Octave);
        
        // C4, G4 axis (sum 127) reflects D2 (38) to 127 - 38 = 89 (F5)
        // Original avg octave 2.0 -> Reflected avg octave ~ 5.5
        // Current heuristic: if avg > 6, shift down. if avg < 2, shift up.
        // 5.5 is within 2-6, so it stays at 5.5.
        // This is exactly the 3.5 octave jump the user is complaining about.
        
        // We want the output avg octave to be close to the input avg octave.
        Assert.True(Math.Abs(avgOctave - 2.0) <= 1.0, $"Octave jump too large: input 2.0, output {avgOctave}");
    }

    [Fact]
    public void ProcessInput_ShouldNotForceHighNotesToMiddleRange()
    {
        string key = "C Major";
        string input = "C6, E6, G6"; // High C (Original avg octave 6.0)
        
        var result = Program.ProcessInput(input, key);
        
        var chord = result[0];
        var avgOctave = chord.Average(n => n.Octave);
        
        // C4, G4 axis (sum 127) reflects C6 (84) to 127 - 84 = 43 (G2)
        // Original avg octave 6.0 -> Reflected avg octave ~ 2.5
        // 2.5 is within 2-6, so it stays.
        // This is a 3.5 octave jump downwards.
        
        Assert.True(Math.Abs(avgOctave - 6.0) <= 1.0, $"Octave jump too large: input 6.0, output {avgOctave}");
    }
}
