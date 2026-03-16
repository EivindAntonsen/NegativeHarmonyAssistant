using System.Text.RegularExpressions;

namespace NegativeHarmonyAssistant;

class Program
{
    public static void Main(string[] args)
    {
        if (args is [var notesInput, var keyInput, ..])
        {
            RunOnce(notesInput, keyInput);
            return;
        }

        RunInteractive();
    }

    private static void RunOnce(string notesInput, string keyInput, bool condense = false)
    {
        try
        {
            ProcessInput(notesInput, keyInput, condense);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void RunInteractive()
    {
        Console.Clear();
        Console.WriteLine("========================================");
        Console.WriteLine("    Negative Harmony Assistant v1.0");
        Console.WriteLine("========================================");
        Console.WriteLine("Welcome! This tool maps notes and chords");
        Console.WriteLine("to their negative harmony counterparts.");
        Console.WriteLine("Type 'k' or 'key' to change the settings.");
        Console.WriteLine("Type '?' at any prompt to reset back to the key selection.");
        Console.WriteLine("Type 'exit' or 'q' at any prompt to quit.\n");

        while (true)
        {
            Console.Write("Enter Key (e.g., 'C Major', 'Eb Minor'): ");
            var keyInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(keyInput) || keyInput.Equals("exit", StringComparison.OrdinalIgnoreCase) || keyInput.Equals("q", StringComparison.OrdinalIgnoreCase))
                break;
            if (keyInput == "?") continue;

            Console.Write("Condense chords? (y/n): ");
            var condenseInput = Console.ReadLine()?.Trim().ToLower();
            if (condenseInput == "?") continue;
            if (condenseInput is "exit" or "q") break;
            var condense = condenseInput == "y" || condenseInput == "yes";

            Console.Write("Omit duplicate notes? (y/n): ");
            var omitDuplicatesInput = Console.ReadLine()?.Trim().ToLower();
            if (omitDuplicatesInput == "?") continue;
            if (omitDuplicatesInput is "exit" or "q") break;
            var omitDuplicates = omitDuplicatesInput == "y" || omitDuplicatesInput == "yes";

            Console.WriteLine($"\nSelected Key: {keyInput} (Condense: {(condense ? "Yes" : "No")}, Omit Duplicates: {(omitDuplicates ? "Yes" : "No")})");
            Console.WriteLine("----------------------------------------");

            while (true)
            {
                Console.Write("Enter Notes/Chords (or 'k' to change settings): ");
                var notesInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(notesInput)) continue;

                if (notesInput.Equals("exit", StringComparison.OrdinalIgnoreCase) || notesInput.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Goodbye!");
                    return;
                }

                if (notesInput.Equals("k", StringComparison.OrdinalIgnoreCase) || notesInput.Equals("key", StringComparison.OrdinalIgnoreCase) || notesInput == "?")
                {
                    Console.WriteLine();
                    break;
                }

                Console.WriteLine();
                try
                {
                    ProcessInput(notesInput, keyInput, condense, omitDuplicates);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }
                Console.WriteLine("\n----------------------------------------");
            }
        }

        Console.WriteLine("Goodbye!");
    }

    private static void ProcessInput(string notesInput, string keyInput, bool condense = false, bool omitDuplicates = false)
    {
        var groups = notesInput.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (groups.Length is 0) return;

        var originalKeyContext = KeyContext.Parse(keyInput);

        var firstGroupElements = groups[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var isChordMode = firstGroupElements is [var first] && IsChordHeuristic(first);

        var chordGroups = new List<List<Note>>();
        var originalNames = new List<string>();

        foreach (var group in groups)
        {
            var elements = group.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            
            if (isChordMode)
            {
                if (elements is not [var chordName] || !IsChordHeuristic(chordName))
                    throw new ArgumentException($"Style inconsistency: Group '{group}' is not recognized as a single chord, but the first group was.");
                
                var chord = Chord.Parse(chordName);
                var diatonicallyCorrectNotes = chord.Notes.Select(n => Note.FromAbsolutePitch(n.AbsolutePitch, originalKeyContext)).ToList();
                
                if (omitDuplicates)
                {
                    diatonicallyCorrectNotes = diatonicallyCorrectNotes.GroupBy(n => n.PitchClass).Select(g => g.First()).ToList();
                }
                
                chordGroups.Add(diatonicallyCorrectNotes);
                
                var identifiedName = Chord.Identify(diatonicallyCorrectNotes, originalKeyContext);
                originalNames.Add(identifiedName == "Unknown" ? chordName : identifiedName);
            }
            else
            {
                var inputNotes = ParseSequenceWithContext(elements, null);
                
                if (omitDuplicates)
                {
                    inputNotes = inputNotes.GroupBy(n => n.PitchClass).Select(g => g.First()).ToList();
                }
                
                chordGroups.Add(inputNotes);
                
                var identifiedName = Chord.Identify(inputNotes, originalKeyContext);
                originalNames.Add(identifiedName == "Unknown" ? "" : identifiedName); 
            }
        }

        var axisSum = HarmonyMapper.CalculateAxisSum(chordGroups.SelectMany(g => g), keyInput);

        var processedResults = new List<(string NegName, List<Note> NegNotes)>();
        foreach (var originalNotes in chordGroups)
        {
            var (mappedNotes, negContext) = HarmonyMapper.MapNegativeWithContext(originalNotes, keyInput, axisSum);
            
            if (condense)
            {
                mappedNotes = Note.Condense(mappedNotes, negContext);
            }

            if (omitDuplicates)
            {
                mappedNotes = mappedNotes.GroupBy(n => n.PitchClass).Select(g => g.First()).ToList();
            }
            
            var negativeChordName = Chord.Identify(mappedNotes, negContext);
            var negativeName = negativeChordName == "Unknown" ? "" : negativeChordName;
            processedResults.Add((negativeName, mappedNotes));
        }

        var inputStr = string.Join(" | ", originalNames.Select((n, i) => string.IsNullOrEmpty(n) ? string.Join(", ", chordGroups[i].Select(note => note.ToString())) : n));
        Console.WriteLine($"Original: {inputStr}");
        Console.WriteLine($"Key: {keyInput}");
        Console.WriteLine("\nNegative Harmony Mapping:");

        var maxOriginalNameWidth = originalNames.Max(n => n.Length);
        var maxNegativeNameWidth = processedResults.Max(r => r.NegName.Length);

        var origNameColWidth = maxOriginalNameWidth > 0 ? maxOriginalNameWidth + 3 : 0;
        var negNameColWidth = maxNegativeNameWidth > 0 ? maxNegativeNameWidth + 3 : 0;

        var maxNotesInGroup = Math.Max(chordGroups.Count > 0 ? chordGroups.Max(g => g.Count) : 0, 
                                       processedResults.Count > 0 ? processedResults.Max(r => r.NegNotes.Count) : 0);
        var originalNoteWidths = new int[maxNotesInGroup];
        var negativeNoteWidths = new int[maxNotesInGroup];

        for (var j = 0; j < maxNotesInGroup; j++)
        {
            originalNoteWidths[j] = chordGroups.Any(g => g.Count > j) 
                ? chordGroups.Where(g => g.Count > j).Max(g => (g[j].ToString() + (j < g.Count - 1 ? ", " : "")).Length)
                : 0;
            negativeNoteWidths[j] = processedResults.Any(r => r.NegNotes.Count > j)
                ? processedResults.Where(r => r.NegNotes.Count > j).Max(r => (r.NegNotes[j].ToString() + (j < r.NegNotes.Count - 1 ? ", " : "")).Length)
                : 0;
        }

        const string arrow = "   =>   ";

        var rows = new List<string>();
        for (var i = 0; i < chordGroups.Count; i++)
        {
            var origName = originalNames[i];
            var origNotes = chordGroups[i];
            var (negName, negNotes) = processedResults[i];

            var origNamePart = string.IsNullOrEmpty(origName) 
                ? new string(' ', origNameColWidth) 
                : $"({origName}) ".PadRight(origNameColWidth);
            
            var origNotesPart = "";
            for (var j = 0; j < origNotes.Count; j++)
            {
                var noteStr = origNotes[j].ToString() + (j < origNotes.Count - 1 ? ", " : "");
                origNotesPart += noteStr.PadRight(originalNoteWidths[j]);
            }
            origNotesPart += new string(' ', originalNoteWidths.Sum() - originalNoteWidths.Take(origNotes.Count).Sum());

            var negNamePart = string.IsNullOrEmpty(negName) 
                ? new string(' ', negNameColWidth) 
                : $"({negName}) ".PadRight(negNameColWidth);

            var negNotesPart = "";
            for (var j = 0; j < negNotes.Count; j++)
            {
                var noteStr = negNotes[j].ToString() + (j < negNotes.Count - 1 ? ", " : "");
                negNotesPart += noteStr.PadRight(negativeNoteWidths[j]);
            }

            rows.Add(origNamePart + origNotesPart + arrow + negNamePart + negNotesPart);
        }

        var maxRowLength = rows.Count > 0 ? rows.Max(r => r.Length) : 0;
        var headerOrigName = "Original".PadRight(origNameColWidth);
        var headerOrigNotes = "Notes".PadRight(originalNoteWidths.Sum());
        var headerNegName = "Negative".PadRight(negNameColWidth);
        
        Console.WriteLine(headerOrigName + headerOrigNotes + arrow + headerNegName + "Notes");
        Console.WriteLine(new string('-', Math.Max(maxRowLength, 40)));
        foreach (var row in rows) Console.WriteLine(row);

        var finalProgression = new List<string>();
        for (var i = 0; i < chordGroups.Count; i++)
        {
            var (negName, negNotes) = processedResults[i];
            var negativeNotesStr = string.Join(", ", negNotes.Select(n => n.ToString()));
            finalProgression.Add(string.IsNullOrEmpty(negName) ? $"[{negativeNotesStr}]" : negName);
        }
        Console.WriteLine($"\nResulting Progression: {string.Join(" - ", finalProgression)}");
    }

    private static List<Note> ParseSequenceWithContext(string[] inputs, Note? lastNoteInPreviousGroup)
    {
        var result = new List<Note>();
        var previous = lastNoteInPreviousGroup;

        foreach (var input in inputs)
        {
            var match = Regex.Match(input.Trim(), @"^([A-G])(##|#|bb|b)?(\d)?$", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new ArgumentException($"Invalid note format in sequence: {input}");

            var hasOctave = !string.IsNullOrEmpty(match.Groups[3].Value);
            
            if (hasOctave)
            {
                var note = Note.Parse(input);
                result.Add(note);
                previous = note;
            }
            else
            {
                if (previous == null)
                {
                    var note = Note.Parse(input, 4);
                    result.Add(note);
                    previous = note;
                }
                else
                {
                    var tempNote = Note.Parse(input, 0); 
                    var targetPitchClass = tempNote.PitchClass;
                    var currentPitch = previous.AbsolutePitch;
                    var nextPitch = currentPitch + 1;
                    
                    while (nextPitch % 12 != targetPitchClass)
                    {
                        nextPitch++;
                    }
                    
                    var note = Note.FromAbsolutePitch(nextPitch);
                    var finalNote = new Note
                    {
                        NoteName = tempNote.NoteName,
                        Accidental = tempNote.Accidental,
                        Octave = note.Octave
                    };
                    
                    result.Add(finalNote);
                    previous = finalNote;
                }
            }
        }

        return result;
    }

    private static bool IsChordHeuristic(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.Length == 0) return false;
        
        // Let's identify things that are DEFINITELY chords
        var chordSuffixes = new[] { "m", "7", "dim", "aug", "sus", "maj", "min", "major", "minor", "+", "ø" };
        var lowerInput = trimmed.ToLower();
        if (chordSuffixes.Any(s => lowerInput.Contains(s))) return true;

        // If it's a single letter or single letter + accidentals, it's a note (Style 2)
        var noteOnlyRegex = new Regex(@"^[A-G](##|#|bb|b)?$", RegexOptions.IgnoreCase);
        if (noteOnlyRegex.IsMatch(trimmed)) return false;

        // If it's a note with octave, it's NOT a chord (it's Style 1)
        var noteWithOctaveRegex = new Regex(@"^[A-G](##|#|bb|b)?[0-8]$", RegexOptions.IgnoreCase);
        if (noteWithOctaveRegex.IsMatch(trimmed)) return false;

        return true;
    }
}
