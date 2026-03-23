using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Tools;

namespace NegativeHarmonyAssistant;

public class MidiProcessor
{
    public record MidiAnalysisResult(
        bool Success,
        string Message,
        List<List<Note>>? ChordProgression = null,
        TimeDivision? TimeDivision = null
    );

    public static MidiAnalysisResult AnalyzeFile(string filePath)
    {
        try
        {
            var midiFile = MidiFile.Read(filePath);
            var tempoMap = midiFile.GetTempoMap();
            var timeDivision = midiFile.TimeDivision;
            
            // Extract notes from all tracks, but we might want to filter
            var notes = midiFile.GetNotes();

            if (!notes.Any())
            {
                return new MidiAnalysisResult(false, "Found no notes in the MIDI file.");
            }

            // Filter out percussion channel (usually channel 9 in 0-indexed)
            var musicalNotes = notes.Where(n => n.Channel != 9).ToList();
            
            if (!musicalNotes.Any())
            {
                return new MidiAnalysisResult(false, "Midi track instrument is not supported (only non-drum tracks are supported).");
            }

            // Group notes by start time to find "stacked" chords
            var groupedByTime = musicalNotes
                .GroupBy(n => n.Time)
                .OrderBy(g => g.Key)
                .ToList();

            var progression = new List<List<Note>>();
            bool foundAnyChord = false;

            foreach (var group in groupedByTime)
            {
                var chordNotes = group
                    .Select(n => {
                        var note = Note.FromAbsolutePitch(n.NoteNumber);
                        return new Note {
                            NoteName = note.NoteName,
                            Accidental = note.Accidental,
                            Octave = note.Octave,
                            OriginalTime = n.Time,
                            OriginalDuration = n.Length,
                            OriginalVelocity = n.Velocity,
                            OriginalChannel = n.Channel
                        };
                    })
                    .ToList();

                progression.Add(chordNotes);
                if (chordNotes.Count >= 3)
                {
                    foundAnyChord = true;
                }
            }

            if (!foundAnyChord && progression.Any(p => p.Count > 0))
            {
                // If we found notes but none formed a chord (>= 3 notes at once)
                // we should check if they are just melodies
                if (progression.All(p => p.Count < 3))
                {
                    return new MidiAnalysisResult(false, "Found notes, but unable to find chords (the notes don't start at the same time).", progression, timeDivision);
                }
            }

            if (!progression.Any())
            {
                return new MidiAnalysisResult(false, "Found no chord progression.");
            }

            return new MidiAnalysisResult(true, "Found chord progression and extracted chords.", progression, timeDivision);
        }
        catch (Exception ex)
        {
            return new MidiAnalysisResult(false, $"Error processing MIDI file: {ex.Message}");
        }
    }

    public static void ExportFile(string filePath, List<List<Note>> progression, TimeDivision? timeDivision = null)
    {
        var midiFile = new MidiFile();
        if (timeDivision != null)
        {
            midiFile.TimeDivision = timeDivision;
        }
        var trackChunk = new TrackChunk();

        var allNotes = progression.SelectMany(c => c).ToList();
        
        if (allNotes.Any(n => n.OriginalTime.HasValue))
        {
            var midiNotes = allNotes.Select(n => new Melanchall.DryWetMidi.Interaction.Note(
                (SevenBitNumber)n.AbsolutePitch,
                n.OriginalDuration ?? 480,
                n.OriginalTime ?? 0)
            {
                Velocity = (SevenBitNumber)(n.OriginalVelocity ?? 100),
                Channel = (FourBitNumber)(n.OriginalChannel ?? 0)
            }).ToList();

            using (var notesManager = trackChunk.ManageNotes())
            {
                notesManager.Objects.Add(midiNotes);
            }
        }
        else
        {
            // Fallback for non-MIDI inputs
            long currentTime = 0;
            foreach (var chord in progression)
            {
                var chordNotes = chord.Select(n => new Melanchall.DryWetMidi.Interaction.Note(
                    (SevenBitNumber)n.AbsolutePitch,
                    480,
                    currentTime)
                {
                    Velocity = (SevenBitNumber)100
                }).ToList();

                using (var notesManager = trackChunk.ManageNotes())
                {
                    notesManager.Objects.Add(chordNotes);
                }
                currentTime += 480;
            }
        }

        midiFile.Chunks.Add(trackChunk);
        midiFile.Write(filePath, true);
    }
}
