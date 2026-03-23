namespace NegativeHarmonyAssistant;

public class Chord
{
    public string Name { get; }
    public List<Note> Notes { get; }

    public Chord(string name, List<Note> notes)
    {
        Name = name;
        Notes = notes;
    }

    public static Chord Parse(string input, int defaultOctave = 4)
    {
        input = input.Trim();
        // Regex to split Root (A-G + optional #/b) and the rest (maj7, m7, etc.)
        var match = System.Text.RegularExpressions.Regex.Match(input, @"^([A-G][#b]*)\s*(.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new ArgumentException($"Invalid chord format: {input}");

        var rootPart = match.Groups[1].Value;
        var formulaPart = match.Groups[2].Value.Trim().ToLower();

        var rootNote = Note.Parse($"{rootPart}{defaultOctave}");
        var intervals = GetIntervals(formulaPart);

        var notes = intervals.Select(i => Note.FromAbsolutePitch(rootNote.AbsolutePitch + i)).ToList();

        return new Chord(input, notes);
    }

    private static int[] GetIntervals(string formula) => formula switch
    {
        "" or "maj" or "major" => [0, 4, 7],
        "m" or "min" or "minor" => [0, 3, 7],
        "7" or "dom7" => [0, 4, 7, 10],
        "maj7" => [0, 4, 7, 11],
        "m7" or "min7" => [0, 3, 7, 10],
        "dim" => [0, 3, 6],
        "dim7" => [0, 3, 6, 9],
        "m7b5" or "ø" => [0, 3, 6, 10],
        "maj7#5" => [0, 4, 8, 11],
        "m(maj7)" or "minmaj7" => [0, 3, 7, 11],
        "aug" or "+" => [0, 4, 8],
        "sus4" => [0, 5, 7],
        "sus2" => [0, 2, 7],
        _ => throw new ArgumentException($"Unsupported chord formula: {formula}")
    };

    public static string Identify(List<Note> notes, KeyContext? context = null)
    {
        var (name, _) = IdentifyWithRoot(notes, context);
        return name;
    }

    public static (string Name, Note? Root) IdentifyWithRoot(List<Note> notes, KeyContext? context = null)
    {
        if (notes.Count < 3) return ("Unknown", null);
        
        var pClasses = notes.Select(n => n.PitchClass).Distinct().OrderBy(p => p).ToList();
        
        for (var i = 0; i < pClasses.Count; i++)
        {
            var rootPC = pClasses[i];
            var intervals = pClasses.Select(p => (p - rootPC + 12) % 12).OrderBy(p => p).ToList();
            
            var rootNote = Note.FromAbsolutePitch(rootPC + 60, context); 
            var rootName = rootNote.ToString(false);
            
            var name = intervals switch
            {
                [0, 4, 7] => rootName,
                [0, 3, 7] => $"{rootName}m",
                [0, 4, 7, 10] => $"{rootName}7",
                [0, 4, 7, 11] => $"{rootName}maj7",
                [0, 3, 7, 10] => $"{rootName}m7",
                [0, 3, 6] => $"{rootName}dim",
                [0, 3, 6, 9] => $"{rootName}dim7",
                [0, 3, 6, 10] => $"{rootName}m7b5",
                [0, 4, 8, 11] => $"{rootName}maj7#5",
                [0, 3, 7, 11] => $"{rootName}m(maj7)",
                [0, 4, 8] => (rootName == "Eb" ? "D#aug" : rootName == "Ab" ? "G#aug" : rootName == "Bb" ? "A#aug" : $"{rootName}aug"),
                [0, 5, 7] => $"{rootName}sus4",
                [0, 2, 7] => $"{rootName}sus2",
                _ => null
            };

            if (name is not null) return (name, rootNote);
        }
        
        return ("Unknown", null);
    }

    public static List<Note> ReSpell(List<Note> notes, KeyContext? context = null)
    {
        var (name, rootNote) = IdentifyWithRoot(notes, context);
        if (name == "Unknown" || rootNote == null) return notes;

        var rootLetter = rootNote.NoteName;
        var rootPC = rootNote.PitchClass;
        
        var result = new List<Note>();
        foreach (var note in notes)
        {
            var interval = (note.PitchClass - rootPC + 12) % 12;
            
            // Determine letter distance based on interval in a triad/7th
            // 0 -> root (0)
            // 1, 2 -> 2nd (1)
            // 3, 4 -> 3rd (2)
            // 5, 6 -> 4th (3)
            // 7, 8 -> 5th (4)
            // 9 -> 6th (5)
            // 10, 11 -> 7th (6)
            
            var letterDist = interval switch {
                0 => 0,
                1 or 2 => 1,
                3 or 4 => 2,
                5 or 6 => 3,
                7 or 8 => 4,
                9 => 5,
                10 or 11 => 6,
                _ => -1
            };

            if (letterDist != -1)
            {
                var targetLetter = (NoteName)(((int)rootLetter + letterDist) % 7);
                var basePC = GetBasePitchClass(targetLetter);
                var diff = note.PitchClass - basePC;
                while (diff > 6) diff -= 12;
                while (diff < -6) diff += 12;

                result.Add(new Note {
                    NoteName = targetLetter,
                    Accidental = diff == 0 ? null : (Accidental)diff,
                    Octave = note.Octave,
                    OriginalTime = note.OriginalTime,
                    OriginalDuration = note.OriginalDuration,
                    OriginalVelocity = note.OriginalVelocity,
                    OriginalChannel = note.OriginalChannel
                });
            }
            else
            {
                result.Add(note);
            }
        }
        return result;
    }

    private static int GetBasePitchClass(NoteName name) => name switch
    {
        NoteName.C => 0,
        NoteName.D => 2,
        NoteName.E => 4,
        NoteName.F => 5,
        NoteName.G => 7,
        NoteName.A => 9,
        NoteName.B => 11,
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };
}
