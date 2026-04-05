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
        List<List<List<Note>>>? Tracks = null, // Track -> List of Note Groups (Chords/Melodies)
        TimeDivision? TimeDivision = null,
        IEnumerable<MidiEvent>? TimeSignatureEvents = null
    );

    public static MidiAnalysisResult AnalyzeFile(string filePath)
    {
        try
        {
            var midiFile = MidiFile.Read(filePath);
            var timeDivision = midiFile.TimeDivision;
            var timeSignatureEvents = midiFile.GetTrackChunks()
                .SelectMany(c => c.Events)
                .OfType<TimeSignatureEvent>()
                .Select(e => (MidiEvent)e.Clone())
                .ToList();

            var tracks = new List<List<List<Note>>>();

            foreach (var trackChunk in midiFile.GetTrackChunks())
            {
                var events = trackChunk.Events;
                var notes = trackChunk.GetNotes();

                if (!notes.Any())
                {
                    continue;
                }

                // Filter out percussion channel (usually channel 9 in 0-indexed)
                // Also common to filter out channel 15/16 for some FX, but let's stick to channel 9 for now as per previous requirement.
                var musicalNotes = notes.Where(n => n.Channel != 9).ToList();

                if (!musicalNotes.Any())
                {
                    continue;
                }

                // Group notes by start time to find "stacked" chords
                var groupedByTime = musicalNotes
                    .GroupBy(n => n.Time)
                    .OrderBy(g => g.Key)
                    .ToList();

                var trackProgression = new List<List<Note>>();

                foreach (var group in groupedByTime)
                {
                    // Find the last ProgramChangeEvent that occurred at or before this group's time
                    // DryWetMidi Events in TrackChunk are ordered by Absolute Time if handled correctly,
                    // but DeltaTime is what's stored. GetNotes() provides absolute time.
                    // We should use an event manager or calculate absolute times for events.
                    long accumulatedTime = 0;
                    ProgramChangeEvent? lastProgramChange = null;
                    foreach (var e in events)
                    {
                        accumulatedTime += e.DeltaTime;
                        if (accumulatedTime > group.Key) break;
                        if (e is ProgramChangeEvent pce) lastProgramChange = pce;
                    }

                    var chordNotes = group
                        .Select(n =>
                        {
                            var note = Note.FromAbsolutePitch(n.NoteNumber);
                            return new Note
                            {
                                NoteName = note.NoteName,
                                Accidental = note.Accidental,
                                Octave = note.Octave,
                                OriginalTime = n.Time,
                                OriginalDuration = n.Length,
                                OriginalVelocity = n.Velocity,
                                OriginalChannel = n.Channel,
                                OriginalInstrument = lastProgramChange?.ProgramNumber
                            };
                        })
                        .ToList();

                    trackProgression.Add(chordNotes);
                }

                if (trackProgression.Any())
                {
                    tracks.Add(trackProgression);
                }
            }

            if (!tracks.Any())
            {
                return new MidiAnalysisResult(false, "Found no musical notes in any MIDI track.");
            }

            return new MidiAnalysisResult(true, "Found notes and extracted sequence from multiple tracks.", tracks, timeDivision, timeSignatureEvents);
        }
        catch (Exception ex)
        {
            return new MidiAnalysisResult(false, $"Error processing MIDI file: {ex.Message}");
        }
    }

    public static void ExportFile(string filePath, List<List<List<Note>>> tracks, TimeDivision? timeDivision = null, IEnumerable<MidiEvent>? timeSignatureEvents = null)
    {
        var midiFile = new MidiFile();
        if (timeDivision != null)
        {
            midiFile.TimeDivision = timeDivision;
        }

        if (timeSignatureEvents != null && timeSignatureEvents.Any())
        {
            var tempoTrack = new TrackChunk();
            foreach (var tsEvent in timeSignatureEvents)
            {
                tempoTrack.Events.Add(tsEvent.Clone());
            }
            midiFile.Chunks.Add(tempoTrack);
        }

        foreach (var trackProgression in tracks)
        {
            var trackChunk = new TrackChunk();
            var allNotes = trackProgression.SelectMany(c => c).ToList();

            if (allNotes.Any(n => n.OriginalInstrument.HasValue))
            {
                var instrumentGroups = allNotes
                    .GroupBy(n => new { n.OriginalInstrument, n.OriginalChannel })
                    .ToList();

                foreach (var group in instrumentGroups)
                {
                    if (group.Key.OriginalInstrument.HasValue)
                    {
                        trackChunk.Events.Add(new ProgramChangeEvent((SevenBitNumber)group.Key.OriginalInstrument.Value)
                        {
                            Channel = (FourBitNumber)(group.Key.OriginalChannel ?? 0),
                            DeltaTime = 0
                        });
                    }
                }
            }

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
                // Fallback for non-MIDI inputs or missing metadata
                long currentTime = 0;
                foreach (var chord in trackProgression)
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
        }

        midiFile.Write(filePath, true);
    }
}
