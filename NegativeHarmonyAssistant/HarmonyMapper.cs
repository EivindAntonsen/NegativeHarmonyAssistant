namespace NegativeHarmonyAssistant;

public class HarmonyMapper
{
    public static (List<Note> MappedNotes, KeyContext NegativeKeyContext) MapNegativeWithContext(IEnumerable<Note> notes, string keyString, int? customAxisSum = null, bool preserveStructure = false)
    {
        var noteList = notes.ToList();
        if (noteList is []) return ([], null!);

        var originalKey = KeyContext.Parse(keyString);
        var tonicPC = originalKey.Tonic.PitchClass;
        
        int axisSum;
        int tonicBaseForContext;

        if (customAxisSum.HasValue)
        {
            axisSum = customAxisSum.Value;
            tonicBaseForContext = (int)Math.Round((axisSum - 7) / 2.0);
        }
        else
        {
            // By reflecting across (Tonic + Dominant) at the average octave of the input,
            // we ensure that the reflection's range is centered around the input's range.
            // Example: If input is around octave 4, axis is (C4 + G4) = 60 + 67 = 127.
            // C4 (60) maps to 127 - 60 = 67 (G4).
            // E4 (64) maps to 127 - 64 = 63 (Eb4).
            // G4 (67) maps to 127 - 67 = 60 (C4).
            // This preserves the register naturally.
            var avgOctave = (int)Math.Round(noteList.Average(n => n.Octave));
            tonicBaseForContext = (avgOctave + 1) * 12 + tonicPC;
            var dominantBase = tonicBaseForContext + 7;
            axisSum = tonicBaseForContext + dominantBase;
        }

        var negativeMode = originalKey.Mode.GetNegativeMode();
        var dominantNote = Note.FromAbsolutePitch(tonicBaseForContext + 7, preferSharps: originalKey.PreferSharps);
        var negativeKeyContext = new KeyContext(dominantNote, negativeMode);

        var finalPreferSharps = negativeKeyContext.PreferSharps;

        if (!preserveStructure)
        {
            var mapped = noteList.Select(note => {
                var m = Note.FromAbsolutePitch(axisSum - note.AbsolutePitch, negativeKeyContext, finalPreferSharps);
                return new Note
                {
                    NoteName = m.NoteName,
                    Accidental = m.Accidental,
                    Octave = m.Octave,
                    OriginalTime = note.OriginalTime,
                    OriginalDuration = note.OriginalDuration,
                    OriginalVelocity = note.OriginalVelocity,
                    OriginalChannel = note.OriginalChannel,
                    OriginalInstrument = note.OriginalInstrument
                };
            })
            .OrderBy(n => n.AbsolutePitch)
            .ToList();
            return (mapped, negativeKeyContext);
        }
        else
        {
            // Preserve Structural Direction/Melody
            // Map pitch classes but keep the same relative movement (contour)
            var result = new List<Note>();
            Note? lastOriginal = null;
            Note? lastMapped = null;

            foreach (var note in noteList)
            {
                var negativePC = (axisSum - note.AbsolutePitch + 1200) % 12;
                
                if (lastMapped == null || lastOriginal == null)
                {
                    // First note - use traditional reflection for initial placement
                    var m = Note.FromAbsolutePitch(axisSum - note.AbsolutePitch, negativeKeyContext, finalPreferSharps);
                    var newNote = new Note
                    {
                        NoteName = m.NoteName,
                        Accidental = m.Accidental,
                        Octave = m.Octave,
                        OriginalTime = note.OriginalTime,
                        OriginalDuration = note.OriginalDuration,
                        OriginalVelocity = note.OriginalVelocity,
                        OriginalChannel = note.OriginalChannel,
                        OriginalInstrument = note.OriginalInstrument
                    };
                    result.Add(newNote);
                    lastMapped = newNote;
                }
                else
                {
                    // Calculate target pitch based on direction
                    int originalDiff = note.AbsolutePitch - lastOriginal.AbsolutePitch;
                    int targetPitch;

                    if (originalDiff == 0)
                    {
                        targetPitch = lastMapped.AbsolutePitch; // No change
                    }
                    else if (originalDiff > 0)
                    {
                        // Moving UP - find the closest pitch with negativePC that is >= lastMapped
                        targetPitch = lastMapped.AbsolutePitch + (negativePC - lastMapped.AbsolutePitch % 12 + 12) % 12;
                        // If it's the same pitch but original was moving up, we might want to force an octave up?
                        // Actually, if pitch class changes, it will be >. If same pitch class, it will be same.
                        // User said "melodies might not start from the root necessarily", 
                        // "chords would probably have to become inversions".
                    }
                    else // originalDiff < 0
                    {
                        // Moving DOWN - find the closest pitch with negativePC that is <= lastMapped
                        targetPitch = lastMapped.AbsolutePitch - (lastMapped.AbsolutePitch % 12 - negativePC + 12) % 12;
                    }

                    var m = Note.FromAbsolutePitch(targetPitch, negativeKeyContext, finalPreferSharps);
                    var newNote = new Note
                    {
                        NoteName = m.NoteName,
                        Accidental = m.Accidental,
                        Octave = m.Octave,
                        OriginalTime = note.OriginalTime,
                        OriginalDuration = note.OriginalDuration,
                        OriginalVelocity = note.OriginalVelocity,
                        OriginalChannel = note.OriginalChannel,
                        OriginalInstrument = note.OriginalInstrument
                    };
                    result.Add(newNote);
                    lastMapped = newNote;
                }
                lastOriginal = note;
            }
            // Preserve the original order (relevant for melodies)
            return (result, negativeKeyContext);
        }
    }

    public static int CalculateAxisSum(IEnumerable<Note> notes, string keyString)
    {
        var noteList = notes.ToList();
        if (noteList is []) return 0;

        var originalKey = KeyContext.Parse(keyString);
        var avgOctave = (int)Math.Round(noteList.Average(n => n.Octave));
        var tonicBase = (avgOctave + 1) * 12 + originalKey.Tonic.PitchClass;
        var dominantBase = tonicBase + 7;
        return tonicBase + dominantBase;
    }

    public static List<Note> MapNegative(IEnumerable<Note> notes, string keyString, bool preserveStructure = false)
    {
        return MapNegativeWithContext(notes, keyString, preserveStructure: preserveStructure).MappedNotes;
    }
}
