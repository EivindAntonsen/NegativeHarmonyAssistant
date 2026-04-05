using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using Xunit;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace NegativeHarmonyAssistant.Tests;

public class MidiTrackLimitTests
{
    [Fact]
    public void AnalyzeFile_WithManyTracks_ShouldExtractAllTracks()
    {
        var midiFile = new MidiFile();
        int trackCount = 20;

        for (int i = 0; i < trackCount; i++)
        {
            var trackChunk = new TrackChunk();
            // Each track has one note on a unique channel (looping 0-15, skipping 9)
            int channel = i % 16;
            if (channel == 9) channel = (channel + 1) % 16;

            var note = new Melanchall.DryWetMidi.Interaction.Note((SevenBitNumber)(60 + i), 480, 0)
            {
                Channel = (FourBitNumber)channel
            };
            
            using (var nm = trackChunk.ManageNotes()) nm.Objects.Add(new[] { note });
            midiFile.Chunks.Add(trackChunk);
        }

        var tempFile = Path.GetTempFileName() + ".mid";
        try
        {
            midiFile.Write(tempFile);
            var result = MidiProcessor.AnalyzeFile(tempFile);

            Assert.True(result.Success);
            Assert.Equal(trackCount, result.Tracks.Count);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ExportFile_WithInstruments_ShouldPreserveInstruments()
    {
        var tracks = new List<List<List<Note>>>();
        var note1 = Note.FromAbsolutePitch(60);
        var noteWithMetadata1 = new Note
        {
            NoteName = note1.NoteName,
            Accidental = note1.Accidental,
            Octave = note1.Octave,
            OriginalTime = 0,
            OriginalDuration = 480,
            OriginalVelocity = 100,
            OriginalChannel = 0,
            OriginalInstrument = 1 // Piano
        };
        tracks.Add(new List<List<Note>> { new List<Note> { noteWithMetadata1 } });

        var note2 = Note.FromAbsolutePitch(64);
        var noteWithMetadata2 = new Note
        {
            NoteName = note2.NoteName,
            Accidental = note2.Accidental,
            Octave = note2.Octave,
            OriginalTime = 0,
            OriginalDuration = 480,
            OriginalVelocity = 100,
            OriginalChannel = 1,
            OriginalInstrument = 25 // Guitar
        };
        tracks.Add(new List<List<Note>> { new List<Note> { noteWithMetadata2 } });

        var tempFile = Path.GetTempFileName() + ".mid";
        try
        {
            MidiProcessor.ExportFile(tempFile, tracks);
            
            var result = MidiFile.Read(tempFile);
            var trackChunks = result.GetTrackChunks().ToList();
            
            Assert.Equal(2, trackChunks.Count);
            
            var programChanges1 = trackChunks[0].Events.OfType<ProgramChangeEvent>().ToList();
            Assert.Single(programChanges1);
            Assert.Equal(1, (int)programChanges1[0].ProgramNumber);

            var programChanges2 = trackChunks[1].Events.OfType<ProgramChangeEvent>().ToList();
            Assert.Single(programChanges2);
            Assert.Equal(25, (int)programChanges2[0].ProgramNumber);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
