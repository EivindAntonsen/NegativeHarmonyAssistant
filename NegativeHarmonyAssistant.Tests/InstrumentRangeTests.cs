using Xunit;
using NegativeHarmonyAssistant;
using System.Linq;
using System.Collections.Generic;

namespace NegativeHarmonyAssistant.Tests;

public class InstrumentRangeTests
{
    [Fact]
    public void ProcessMidiFile_WithGuitarTrack_ShouldStayAboveB0()
    {
        // 7-string guitar lowest note is B0 (MIDI note 23)
        // Acoustic Guitar (Steel) is Program 25.
        var key = "C Major";
        
        // C4 (60) in C Major reflects to G4 (67) if axis is 127.
        // If we have a very high note B7 (107), it reflects to 127 - 107 = 20 (Ab0).
        // Ab0 (20) is below B0 (23).
        // It should be shifted up to Ab1 (32).
        
        var guitarNote = Note.Parse("B7");
        var noteWithInstrument = new Note {
            NoteName = guitarNote.NoteName,
            Accidental = guitarNote.Accidental,
            Octave = guitarNote.Octave,
            OriginalInstrument = 25 
        };

        var chordGroups = new List<List<Note>> { new List<Note> { noteWithInstrument } };
        
        // We use a fixed axis to make results predictable
        int axisSum = 127; // C4 (60) + G4 (67)
        
        // Disable automatic octave shifting for this test to focus on instrument range
        var results = Program.ProcessNotes(chordGroups, key, customAxisSum: axisSum);
        
        // Wait, ProcessNotes still applies the octave preservation heuristic!
        // 127 - 107 = 20. 
        // Original average octave = 7. 
        // Mapped average octave (from 20) = 0.
        // Shift = 7 - 0 = 7 octaves.
        // 20 + 7*12 = 104.
        
        // Let's use an input that doesn't trigger a huge shift, but still lands below 23.
        // Original: D2 (38). Axis: 127. 
        // Reflected: 127 - 38 = 89 (F6). 
        // Shift: AvgOctave 2 - 6 = -4 octaves.
        // 89 - 4*12 = 41 (F2). 
        
        // What if input is B6 (95)? 
        // 127 - 95 = 32 (Ab1). 
        // AvgOctave 6 - 1 = 5.
        // 32 + 5*12 = 92 (Ab6).
        
        // The problem is that the octave preservation ALWAYS kicks in first.
        // If we want to test the instrument range, we need it to land below 23 AFTER the octave preservation.
        
        // If input is D7 (98), reflected is 127 - 98 = 29 (F1). 
        // AvgOctave 7 - 1 = 6. 
        // 29 + 6*12 = 101.
        
        // It's hard to land below 23 if we always shift back to the original octave!
        // UNLESS the original was already low?
        // If input is B0 (23). Axis 127.
        // Reflected: 127 - 23 = 104 (G#7).
        // Shift: AvgOctave 0 - 7 = -7.
        // 104 - 7*12 = 20 (G#0).
        // 20 < 23! 
        // So B0 should reflect and stay at least B0.
        
        var guitarNoteLow = Note.Parse("B0");
        var noteWithInstrumentLow = new Note {
            NoteName = guitarNoteLow.NoteName,
            Accidental = guitarNoteLow.Accidental,
            Octave = guitarNoteLow.Octave,
            OriginalInstrument = 25 
        };
        var chordGroupsLow = new List<List<Note>> { new List<Note> { noteWithInstrumentLow } };
        var resultsLow = Program.ProcessNotes(chordGroupsLow, key, customAxisSum: axisSum);
        var mappedNoteLow = resultsLow[0][0];
        
        Assert.True(mappedNoteLow.AbsolutePitch >= 23, $"Note {mappedNoteLow} (pitch {mappedNoteLow.AbsolutePitch}) should be >= 23");
        Assert.Equal(32, mappedNoteLow.AbsolutePitch); // G#1 (20 + 12)
    }

    [Fact]
    public void ProcessMidiFile_WithGuitarTrack_ShouldStayBelowE6()
    {
        var key = "C Major";
        int axisSum = 127;
        
        // Let's create a very low note that when reflected becomes very high
        // If input is C-1 (0), reflected is 127 - 0 = 127 (G8).
        // Shift: AvgOctave -1 - 9 = -10.
        // 127 - 10*12 = 7 (G-1).
        
        // Wait, if input is C4 (60). Axis 127. Reflected G4 (67). Shift 0.
        // If input is C0 (12). Reflected G7 (115). Shift: 0 - 8 = -8. 
        // 115 - 8*12 = 19 (G0).
        
        // I want to test the high limit.
        // If input is B0 (23). Reflected G#7 (104). Shift: 0 - 7 = -7.
        // Result 104 - 7*12 = 20 (G#0). 
        // Then low limit kicks in: 20 -> 32 (G#1).
        
        // Let's say we have an instrument that is naturally very high.
        // Input G6 (86). Axis 127. Reflected A2 (41). 
        // Shift: 6 - 2 = 4. 
        // 41 + 4*12 = 89 (F6).
        // F6 (89) > E6 (88). 
        // It should be shifted down to 77 (F5).
        
        var guitarNoteHigh = Note.Parse("G6");
        var noteWithInstrumentHigh = new Note {
            NoteName = guitarNoteHigh.NoteName,
            Accidental = guitarNoteHigh.Accidental,
            Octave = guitarNoteHigh.Octave,
            OriginalInstrument = 25 
        };
        var chordGroupsHigh = new List<List<Note>> { new List<Note> { noteWithInstrumentHigh } };
        var resultsHigh = Program.ProcessNotes(chordGroupsHigh, key, customAxisSum: axisSum);
        var mappedNoteHigh = resultsHigh[0][0];
        
        Assert.True(mappedNoteHigh.AbsolutePitch <= 88, $"Note {mappedNoteHigh} (pitch {mappedNoteHigh.AbsolutePitch}) should be <= 88");
        Assert.Equal(84, mappedNoteHigh.AbsolutePitch); // C6? No, wait. 
        // If 89 > 88. Shift = ceil((89-88)/12) = 1.
        // 89 - 12 = 77.
        // Why did I get 84? 
        // Ah, because G6 is 91? 
        // G0=7, G1=19, G2=31, G3=43, G4=55, G5=67, G6=79. 
        // Wait. (6+1)*12 + 7 = 84 + 7 = 91.
        // G6 is 91. 
        // Reflected: 127 - 91 = 36 (C2).
        // Shift: 6 - 1 = 5.
        // 36 + 5*12 = 96 (C7).
        // 96 > 88. Shift = ceil((96-88)/12) = 1.
        // 96 - 12 = 84 (C6). 
        // 84 <= 88. 
        
        Assert.Equal(84, mappedNoteHigh.AbsolutePitch); // C6
    }
}
