namespace NegativeHarmonyAssistant;

public class HarmonyMapper
{
    public static (List<Note> MappedNotes, KeyContext NegativeKeyContext) MapNegativeWithContext(IEnumerable<Note> notes, string keyString, int? customAxisSum = null)
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
            var avgOctave = (int)Math.Round(noteList.Average(n => n.Octave));
            tonicBaseForContext = (avgOctave + 1) * 12 + tonicPC;
            var dominantBase = tonicBaseForContext + 7;
            axisSum = tonicBaseForContext + dominantBase;
        }

        var negativeMode = originalKey.Mode.GetNegativeMode();
        var dominantNote = Note.FromAbsolutePitch(tonicBaseForContext + 7, preferSharps: originalKey.PreferSharps);
        var negativeKeyContext = new KeyContext(dominantNote, negativeMode);

        var finalPreferSharps = negativeKeyContext.PreferSharps;

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
                OriginalChannel = note.OriginalChannel
            };
        })
        .OrderBy(n => n.AbsolutePitch)
        .ToList();
        return (mapped, negativeKeyContext);
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

    public static List<Note> MapNegative(IEnumerable<Note> notes, string keyString)
    {
        return MapNegativeWithContext(notes, keyString).MappedNotes;
    }
}
