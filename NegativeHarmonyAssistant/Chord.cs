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
        var match = System.Text.RegularExpressions.Regex.Match(input, @"^([A-G][#|b]*)\s*(.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
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
        "aug" or "+" => [0, 4, 8],
        "sus4" => [0, 5, 7],
        "sus2" => [0, 2, 7],
        _ => throw new ArgumentException($"Unsupported chord formula: {formula}")
    };

    public static string Identify(List<Note> notes, KeyContext? context = null)
    {
        if (notes.Count < 3) return "Unknown";
        
        var pClasses = notes.Select(n => n.PitchClass).Distinct().OrderBy(p => p).ToList();
        
        for (var i = 0; i < pClasses.Count; i++)
        {
            var root = pClasses[i];
            var intervals = pClasses.Select(p => (p - root + 12) % 12).OrderBy(p => p).ToList();
            
            var rootNote = Note.FromAbsolutePitch(root + 60, context); 
            var rootName = rootNote.ToString().Replace(rootNote.Octave.ToString(), "");
            
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
                [0, 4, 8] => $"{rootName}aug",
                _ => null
            };

            if (name is not null) return name;
        }
        
        return "Unknown";
    }
}
