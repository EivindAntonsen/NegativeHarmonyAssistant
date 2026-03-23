using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using Xunit;
using System.IO;
using System.Linq;

namespace NegativeHarmonyAssistant.Tests;

public class MidiProcessorTests
{
    [Fact]
    public void AnalyzeFile_NonExistentFile_ShouldReturnFailure()
    {
        var result = MidiProcessor.AnalyzeFile("nonexistent.mid");
        Assert.False(result.Success);
        Assert.Contains("Error processing MIDI file", result.Message);
    }

    [Fact]
    public void AnalyzeFile_WithValidMidi_ShouldExtractChords()
    {
        // Create a simple MIDI file in memory
        var midiFile = new MidiFile();
        var trackChunk = new TrackChunk();

        // C Major chord (C4, E4, G4) starting at time 0
        trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100));
        trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)64, (SevenBitNumber)100));
        trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)67, (SevenBitNumber)100));
        
        trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0) { DeltaTime = 100 });
        trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)64, (SevenBitNumber)0));
        trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)67, (SevenBitNumber)0));

        midiFile.Chunks.Add(trackChunk);

        var tempFile = Path.GetTempFileName() + ".mid";
        try
        {
            midiFile.Write(tempFile);

            var result = MidiProcessor.AnalyzeFile(tempFile);

            Assert.True(result.Success);
            Assert.NotNull(result.ChordProgression);
            Assert.Single(result.ChordProgression);
            var firstChord = result.ChordProgression[0];
            Assert.Equal(3, firstChord.Count);
            
            var pitchClasses = firstChord.Select(n => n.PitchClass).ToList();
            Assert.Contains(0, pitchClasses); // C
            Assert.Contains(4, pitchClasses); // E
            Assert.Contains(7, pitchClasses); // G
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ExportFile_ShouldCreateValidMidiFile()
    {
        var tempFile = Path.GetTempFileName() + ".mid";
        var progression = new List<List<Note>>
        {
            new List<Note> { Note.FromAbsolutePitch(60), Note.FromAbsolutePitch(64), Note.FromAbsolutePitch(67) }
        };

        try
        {
            MidiProcessor.ExportFile(tempFile, progression);
            Assert.True(File.Exists(tempFile));

            var result = MidiProcessor.AnalyzeFile(tempFile);
            Assert.True(result.Success);
            Assert.Single(result.ChordProgression);
            Assert.Equal(3, result.ChordProgression[0].Count);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void AnalyzeFile_MelodyOnly_ShouldReturnFailureWithNotes()
    {
        var midiFile = new MidiFile();
        var trackChunk = new TrackChunk();

        // C4 then E4 then G4
        trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100));
        trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0) { DeltaTime = 100 });
        trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)64, (SevenBitNumber)100));
        trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)64, (SevenBitNumber)0) { DeltaTime = 100 });
        trackChunk.Events.Add(new NoteOnEvent((SevenBitNumber)67, (SevenBitNumber)100));
        trackChunk.Events.Add(new NoteOffEvent((SevenBitNumber)67, (SevenBitNumber)0) { DeltaTime = 100 });

        midiFile.Chunks.Add(trackChunk);

        var tempFile = Path.GetTempFileName() + ".mid";
        try
        {
            midiFile.Write(tempFile);

            var result = MidiProcessor.AnalyzeFile(tempFile);

            Assert.False(result.Success);
            Assert.Equal("Found notes, but unable to find chords (the notes don't start at the same time).", result.Message);
            Assert.NotNull(result.ChordProgression);
            Assert.Equal(3, result.ChordProgression.Count);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ExportFile_ShouldPreserveOriginalTimingAndVelocity()
    {
        var tempFile = Path.GetTempFileName() + ".mid";
        var exportFile = Path.GetTempFileName() + ".mid";
        
        var midiFile = new MidiFile();
        var trackChunk = new TrackChunk();

        // C chord (60, 64, 67) at time 100, duration 200, velocity 80, channel 3
        var notes = new[] { 60, 64, 67 }.Select(p => new Melanchall.DryWetMidi.Interaction.Note((SevenBitNumber)p, 200, 100)
        {
            Velocity = (SevenBitNumber)80,
            Channel = (FourBitNumber)3
        });
        using (var notesManager = trackChunk.ManageNotes())
        {
            notesManager.Objects.Add(notes);
        }
        midiFile.Chunks.Add(trackChunk);

        try
        {
            midiFile.Write(tempFile);

            var analysis = MidiProcessor.AnalyzeFile(tempFile);
            Assert.True(analysis.Success, analysis.Message);
            
            MidiProcessor.ExportFile(exportFile, analysis.ChordProgression);
            
            var reAnalysis = MidiProcessor.AnalyzeFile(exportFile);
            Assert.True(reAnalysis.Success);
            
            var exportedNote = reAnalysis.ChordProgression[0][0];
            Assert.Equal(60, exportedNote.AbsolutePitch);
            Assert.Equal((long)100, exportedNote.OriginalTime);
            Assert.Equal((long)200, exportedNote.OriginalDuration);
            Assert.Equal((byte)80, exportedNote.OriginalVelocity);
            Assert.Equal((byte)3, exportedNote.OriginalChannel);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(exportFile)) File.Delete(exportFile);
        }
    }

    [Fact]
    public void ExportFile_ExistingFile_ShouldOverwrite()
    {
        var tempFile = Path.GetTempFileName() + ".mid";
        var progression = new List<List<Note>>
        {
            new List<Note> { Note.FromAbsolutePitch(60), Note.FromAbsolutePitch(64), Note.FromAbsolutePitch(67) }
        };

        try
        {
            // Create a dummy file first
            File.WriteAllText(tempFile, "This is not a MIDI file");
            Assert.True(File.Exists(tempFile));

            // Export to the same path
            MidiProcessor.ExportFile(tempFile, progression);

            // Verify it's now a valid MIDI file
            var result = MidiProcessor.AnalyzeFile(tempFile);
            Assert.True(result.Success);
            Assert.Single(result.ChordProgression);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void AnalyzeFile_ShouldPreserveNoteDurationsInChordProgression()
    {
        var midiFile = new MidiFile();
        var trackChunk = new TrackChunk();

        // Chord 1: C4, E4, G4 at time 0, duration 480 (quarter note)
        var notes1 = new[] { 60, 64, 67 }.Select(p => new Melanchall.DryWetMidi.Interaction.Note((SevenBitNumber)p, 480, 0));
        // Chord 2: F4, A4, C5 at time 480, duration 960 (half note)
        var notes2 = new[] { 65, 69, 72 }.Select(p => new Melanchall.DryWetMidi.Interaction.Note((SevenBitNumber)p, 960, 480));

        using (var notesManager = trackChunk.ManageNotes())
        {
            notesManager.Objects.Add(notes1);
            notesManager.Objects.Add(notes2);
        }
        midiFile.Chunks.Add(trackChunk);

        var tempFile = Path.GetTempFileName() + ".mid";
        try
        {
            midiFile.Write(tempFile);

            var result = MidiProcessor.AnalyzeFile(tempFile);

            Assert.True(result.Success);
            Assert.Equal(2, result.ChordProgression.Count);

            // Verify durations
            Assert.All(result.ChordProgression[0], n => Assert.Equal(480, n.OriginalDuration));
            Assert.All(result.ChordProgression[1], n => Assert.Equal(960, n.OriginalDuration));

            // Verify times
            Assert.All(result.ChordProgression[0], n => Assert.Equal(0, n.OriginalTime));
            Assert.All(result.ChordProgression[1], n => Assert.Equal(480, n.OriginalTime));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Program_ProcessMidiFile_ShouldPreserveDurationsAfterMapping()
    {
        var midiFile = new MidiFile();
        var trackChunk = new TrackChunk();

        // One chord: C4, E4, G4 at time 0, duration 480
        var notes1 = new[] { 60, 64, 67 }.Select(p => new Melanchall.DryWetMidi.Interaction.Note((SevenBitNumber)p, 480, 0));

        using (var notesManager = trackChunk.ManageNotes())
        {
            notesManager.Objects.Add(notes1);
        }
        midiFile.Chunks.Add(trackChunk);

        var tempFile = Path.Combine(Directory.GetCurrentDirectory(), "test_input.mid");
        var outputFile = Path.Combine(Directory.GetCurrentDirectory(), "test_input_negative.mid");
        
        try
        {
            midiFile.Write(tempFile);

            // Simulate what happens in Program.ProcessMidiFile
            var analysis = MidiProcessor.AnalyzeFile(tempFile);
            Assert.True(analysis.Success);

            var keyInput = "D Harmonic Minor";
            var progression = analysis.ChordProgression!;
            
            var negativeProgression = new List<List<Note>>();
            foreach (var chord in progression)
            {
                var chordInput = string.Join(", ", chord.Select(n => n.ToString(true)));
                // We use Program.ProcessInput just like in Program.ProcessMidiFile
                // BUT WE ENABLE CONDENSE
                var processedGroups = Program.ProcessInput(chordInput, keyInput, true, false);
                if (processedGroups.Any())
                {
                    var mappedChord = processedGroups[0];
                    // Manual restoration logic currently in Program.cs (simplified check)
                    if (mappedChord.Count == chord.Count)
                    {
                        for (int i = 0; i < mappedChord.Count; i++)
                        {
                            if (!mappedChord[i].OriginalTime.HasValue)
                            {
                                var mappedNote = mappedChord[i];
                                var originalNote = chord[i];
                                mappedChord[i] = new Note
                                {
                                    NoteName = mappedNote.NoteName,
                                    Accidental = mappedNote.Accidental,
                                    Octave = mappedNote.Octave,
                                    OriginalTime = originalNote.OriginalTime,
                                    OriginalDuration = originalNote.OriginalDuration,
                                    OriginalVelocity = originalNote.OriginalVelocity,
                                    OriginalChannel = originalNote.OriginalChannel
                                };
                            }
                        }
                    }
                    negativeProgression.Add(mappedChord);
                }
            }

            MidiProcessor.ExportFile(outputFile, negativeProgression);

            // Re-read exported file and check durations
            var reAnalysis = MidiProcessor.AnalyzeFile(outputFile);
            Assert.True(reAnalysis.Success);
            
            // It might be 1 or 2 depending on how DryWetMidi groups things, 
            // but we mostly care about the notes themselves.
            var allNotes = reAnalysis.ChordProgression.SelectMany(c => c).ToList();
            Assert.Equal(3, allNotes.Count);
            
            Assert.All(allNotes, n => Assert.Equal(480, n.OriginalDuration));
            Assert.All(allNotes, n => Assert.Equal(0, n.OriginalTime));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(outputFile)) File.Delete(outputFile);
        }
    }
    [Fact]
    public void ExportFile_ShouldPreserveTimeDivision()
    {
        var tempFile = Path.GetTempFileName() + ".mid";
        var exportFile = Path.GetTempFileName() + ".mid";
        
        var midiFile = new MidiFile();
        // Set a non-standard time division
        midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision(960);
        var trackChunk = new TrackChunk();

        // Three notes to form a chord
        var notes = new[] { 60, 64, 67 }.Select(p => new Melanchall.DryWetMidi.Interaction.Note((SevenBitNumber)p, 960, 0));
        using (var notesManager = trackChunk.ManageNotes())
        {
            notesManager.Objects.Add(notes);
        }
        midiFile.Chunks.Add(trackChunk);

        try
        {
            midiFile.Write(tempFile);

            var analysis = MidiProcessor.AnalyzeFile(tempFile);
            if (!analysis.Success)
            {
                 throw new Exception($"Midi analysis failed: {analysis.Message}");
            }
            Assert.True(analysis.Success);
            Assert.Equal((short)960, ((TicksPerQuarterNoteTimeDivision)analysis.TimeDivision).TicksPerQuarterNote);
            
            MidiProcessor.ExportFile(exportFile, analysis.ChordProgression, analysis.TimeDivision);
            
            var reMidiFile = MidiFile.Read(exportFile);
            Assert.Equal((short)960, ((TicksPerQuarterNoteTimeDivision)reMidiFile.TimeDivision).TicksPerQuarterNote);
            
            var reAnalysis = MidiProcessor.AnalyzeFile(exportFile);
            Assert.Equal(960, reAnalysis.ChordProgression[0][0].OriginalDuration);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(exportFile)) File.Delete(exportFile);
        }
    }
}
