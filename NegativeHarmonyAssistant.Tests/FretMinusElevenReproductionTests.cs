using Xunit;
using NegativeHarmonyAssistant;
using System.Linq;
using System.Collections.Generic;

namespace NegativeHarmonyAssistant.Tests;

public class FretMinusElevenReproductionTests
{
    [Fact]
    public void Reproduction_FretMinusEleven_ShouldShiftUp()
    {
        // Scenario: A note results in MIDI 12 (C0) for a guitar (min 23).
        // If imported to Guitar Pro, it shows as fret -11 on a B0 string.
        
        var key = "C Major";
        int axisSum = 127; 
        
        // We want to force a result that is exactly 12 (C0) after all heuristics.
        // Original: B2 (47). Axis 127. Reflected: 127-47 = 80 (Ab5).
        // AvgOctave: 2 - 5 = -3.
        // 80 - 3*12 = 44 (Ab2).
        
        // Let's try to get 12.
        // If we have an input at octave 0, it might work.
        // Input: C0 (12). Reflected G7 (115).
        // AvgOctave: 0 - 8 = -8.
        // 115 - 8*12 = 19 (G0).
        // G0 (19) is < 23.
        // Shifting up by ceil((23-19)/12) = 1 octave: 19 + 12 = 31 (G1).
        
        var note = new Note {
            NoteName = NoteName.C,
            Octave = 0,
            OriginalInstrument = 25 // Guitar
        };
        var chordGroups = new List<List<Note>> { new List<Note> { note } };
        
        var results = Program.ProcessNotes(chordGroups, key, customAxisSum: axisSum);
        var mappedNote = results[0][0];
        
        Assert.True(mappedNote.AbsolutePitch >= 23, $"Note {mappedNote} (pitch {mappedNote.AbsolutePitch}) should be >= 23. It was {mappedNote.AbsolutePitch}.");
        Assert.Equal(31, mappedNote.AbsolutePitch); // G1
    }

    [Fact]
    public void Reproduction_BassFretMinusSeven_ShouldShiftUp()
    {
        // Bass (32-39): B0 (23) to G4 (67).
        // Let's try to get a note below B0 for bass.
        var key = "C Major";
        int axisSum = 127; 
        
        var note = new Note {
            NoteName = NoteName.E,
            Octave = 0,
            OriginalInstrument = 32 // Bass
        };
        // E0 (16). 127 - 16 = 111 (Eb7).
        // AvgOctave: 0 - 8 = -8.
        // 111 - 8*12 = 15 (Eb0).
        // Eb0 (15) < 23.
        // Shift up: 15 + 12 = 27 (Eb1).
        
        var chordGroups = new List<List<Note>> { new List<Note> { note } };
        var results = Program.ProcessNotes(chordGroups, key, customAxisSum: axisSum);
        var mappedNote = results[0][0];
        
        Assert.True(mappedNote.AbsolutePitch >= 23, $"Note {mappedNote} (pitch {mappedNote.AbsolutePitch}) should be >= 23. It was {mappedNote.AbsolutePitch}.");
        Assert.Equal(27, mappedNote.AbsolutePitch); // Eb1
    }
}
