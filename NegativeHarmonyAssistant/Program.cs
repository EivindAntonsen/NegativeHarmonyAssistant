using System.Text.RegularExpressions;

namespace NegativeHarmonyAssistant;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length >= 1 && (args[0].EndsWith(".mid", StringComparison.OrdinalIgnoreCase) || args[0].EndsWith(".midi", StringComparison.OrdinalIgnoreCase)))
        {
            var midiPath = args[0];
            string? keyFromArgs = args.Length >= 2 ? args[1] : null;

            if (string.IsNullOrEmpty(keyFromArgs))
            {
                Console.Write("Enter Key (e.g., 'C Major', 'Eb Minor'): ");
                keyFromArgs = Console.ReadLine()?.Trim();
            }

            if (string.IsNullOrEmpty(keyFromArgs))
            {
                Console.WriteLine("Key is required for MIDI processing.");
                return;
            }

            ProcessMidiFile(midiPath, keyFromArgs);
            return;
        }

        if (args.Length >= 2)
        {
            var condense = args.Contains("--condense");
            var preserve = args.Contains("--preserve");
            var notes = args[0];
            var key = args[1];
            RunOnce(notes, key, condense, preserve);
            return;
        }

        RunInteractive();
    }

    private static void ProcessMidiFile(string filePath, string keyInput, bool condense = true, bool omitDuplicates = true, bool preserveStructure = false)
    {
        Console.WriteLine($"Processing MIDI file: {filePath}");
        var result = MidiProcessor.AnalyzeFile(filePath);
        
        if (!result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Message);
            Console.ResetColor();
            return;
        }

        Console.WriteLine(result.Message);
        Console.WriteLine("----------------------------------------");

        // Calculate a global axis based on the entire MIDI file (all tracks) to prevent large jumps
        int? globalAxisSum = null;
        var allMidiNotes = result.Tracks!.SelectMany(t => t).SelectMany(c => c).ToList();
        if (allMidiNotes.Any())
        {
            globalAxisSum = HarmonyMapper.CalculateAxisSum(allMidiNotes, keyInput);
        }

        var negativeTracks = new List<List<List<Note>>>();
        
        foreach (var track in result.Tracks!)
        {
            var negativeProgression = new List<List<Note>>();
            foreach (var chord in track)
            {
                // Process each chord group individually to preserve metadata accurately
                var chordInput = string.Join(", ", chord.Select(n => n.ToString(true)));
                
                // We bypass ProcessInput's own axis calculation by calling it with already-parsed notes 
                // OR we could modify ProcessInput to accept a customAxisSum.
                // Actually, let's call the mapping directly or make ProcessInput more flexible.
                // But ProcessInput does a lot of other things (naming, re-spelling, octave shifting).
                
                // Let's modify ProcessInput signature or use an internal version.
                var processedGroups = ProcessInputInternal(chordInput, keyInput, condense, omitDuplicates, preserveStructure, globalAxisSum);
                if (processedGroups.Any())
                {
                    var mappedChord = processedGroups[0];
                    
                    // Safety check: If metadata is somehow missing but we have original notes, try to fill it.
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
                                    OriginalChannel = originalNote.OriginalChannel,
                                    OriginalInstrument = originalNote.OriginalInstrument
                                };
                            }
                        }
                    }

                    negativeProgression.Add(mappedChord);
                }
            }
            negativeTracks.Add(negativeProgression);
        }

        var suffix = "_negative";
        if (condense) suffix += "_condensed";
        if (omitDuplicates) suffix += "_omitted";
        if (preserveStructure) suffix += "_preserved";
        
        var outputFileName = Path.GetFileNameWithoutExtension(filePath) + suffix + ".mid";
        var outputPath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", outputFileName);
        
        try
        {
            MidiProcessor.ExportFile(outputPath, negativeTracks, result.TimeDivision, result.TimeSignatureEvents);
            Console.WriteLine($"\nNegative harmony MIDI exported to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFailed to export MIDI: {ex.Message}");
        }
    }

    private static void RunOnce(string notesInput, string keyInput, bool condense = false, bool preserveStructure = false)
    {
        try
        {
            ProcessInput(notesInput, keyInput, condense, preserveStructure: preserveStructure);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void RunInteractive()
    {
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

            Console.Write("Preserve structural direction? (experimental) (y/n): ");
            var preserveStructureInput = Console.ReadLine()?.Trim().ToLower();
            if (preserveStructureInput == "?") continue;
            if (preserveStructureInput is "exit" or "q") break;
            var preserveStructure = preserveStructureInput == "y" || preserveStructureInput == "yes";

            Console.WriteLine($"\nSelected Key: {keyInput} (Condense: {(condense ? "Yes" : "No")}, Omit Duplicates: {(omitDuplicates ? "Yes" : "No")}, Preserve Structure: {(preserveStructure ? "Yes" : "No")})");
            Console.WriteLine("----------------------------------------");

            while (true)
            {
                Console.Write("Enter Notes/Chords or MIDI file path (or 'k' to change settings): ");
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
                    if (notesInput.EndsWith(".mid", StringComparison.OrdinalIgnoreCase) || notesInput.EndsWith(".midi", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessMidiFile(notesInput, keyInput!, condense, omitDuplicates, preserveStructure);
                    }
                    else
                    {
                        ProcessInput(notesInput, keyInput!, condense, omitDuplicates, preserveStructure);
                    }
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

    public static List<List<Note>> ProcessInput(string notesInput, string keyInput, bool condense = false, bool omitDuplicates = false, bool preserveStructure = false)
    {
        return ProcessInputInternal(notesInput, keyInput, condense, omitDuplicates, preserveStructure);
    }

    public static List<List<Note>> ProcessNotes(List<List<Note>> notes, string keyInput, bool condense = false, bool omitDuplicates = false, bool preserveStructure = false, int? customAxisSum = null)
    {
        return ProcessInputInternal(notes, keyInput, condense, omitDuplicates, preserveStructure, customAxisSum);
    }

    private static List<List<Note>> ProcessInputInternal(string notesInput, string keyInput, bool condense = false, bool omitDuplicates = false, bool preserveStructure = false, int? customAxisSum = null)
    {
        var hasExplicitOctaves = Regex.IsMatch(notesInput, @"\d");
        var groups = notesInput.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (groups.Length is 0) return [];

        var initialKeyContext = KeyContext.Parse(keyInput);
        var currentKeyContext = initialKeyContext;

        var chordGroups = new List<List<Note>>();
        var originalNames = new List<string>();
        var keyContextsPerGroup = new List<KeyContext>();
        var modulationsPerGroup = new List<string?>();

        var firstGroupElements = groups[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        // Strip modulation for style detection
        var firstElement = firstGroupElements.FirstOrDefault();
        if (firstElement != null)
        {
            var modMatch = Regex.Match(firstElement, @"^\[(.*?)\]\s*(.*)$");
            if (modMatch.Success) firstElement = modMatch.Groups[2].Value;
        }
        var isChordMode = firstElement != null && IsChordHeuristic(firstElement);

        foreach (var group in groups)
        {
            var elements = group.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (elements.Length == 0) continue;

            // Check for modulation in the first element of the group
            var firstInGroup = elements[0];
            var modulationMatch = Regex.Match(firstInGroup, @"^\[(.*?)\]\s*(.*)$");
            if (modulationMatch.Success)
            {
                var newKeyStr = modulationMatch.Groups[1].Value;
                modulationsPerGroup.Add($"[{newKeyStr}]");
                currentKeyContext = KeyContext.Parse(newKeyStr);
                elements[0] = modulationMatch.Groups[2].Value;
                // If there was only modulation and no note/chord in the first element, skip it
                if (string.IsNullOrWhiteSpace(elements[0]))
                {
                    elements = elements.Skip(1).ToArray();
                }
            }
            else
            {
                modulationsPerGroup.Add(null);
            }

            keyContextsPerGroup.Add(currentKeyContext);

            if (isChordMode)
            {
                if (elements.Length == 0)
                {
                    chordGroups.Add([]);
                    originalNames.Add("");
                    continue;
                }

                if (elements.Length > 1 || !IsChordHeuristic(elements[0]))
                    throw new ArgumentException($"Style inconsistency: Group '{group}' is not recognized as a single chord, but the first group was.");
                
                var chordName = elements[0];
                var chord = Chord.Parse(chordName);
                var diatonicallyCorrectNotes = chord.Notes.Select(n => Note.FromAbsolutePitch(n.AbsolutePitch, currentKeyContext)).ToList();
                
                if (omitDuplicates)
                {
                    diatonicallyCorrectNotes = diatonicallyCorrectNotes.GroupBy(n => n.PitchClass).Select(g => g.First()).ToList();
                }
                
                chordGroups.Add(diatonicallyCorrectNotes);
                
                var identifiedName = Chord.Identify(diatonicallyCorrectNotes, currentKeyContext);
                originalNames.Add(identifiedName == "Unknown" ? chordName : identifiedName);
            }
            else
            {
                var inputNotes = ParseSequenceWithContext(elements, chordGroups.LastOrDefault()?.LastOrDefault());
                
                // Re-spell original notes according to the identified chord structure,
                // OR according to the current key context if no chord is identified.
                var (originalChordName, originalRoot) = Chord.IdentifyWithRoot(inputNotes, currentKeyContext);
                if (originalChordName != "Unknown" && originalRoot != null)
                {
                    inputNotes = Chord.ReSpell(inputNotes, currentKeyContext);
                }
                else
                {
                    inputNotes = inputNotes.Select(n => Note.FromAbsolutePitch(n.AbsolutePitch, currentKeyContext)).ToList();
                }

                if (omitDuplicates)
                {
                    inputNotes = inputNotes.GroupBy(n => n.PitchClass).Select(g => g.First()).ToList();
                }
                
                chordGroups.Add(inputNotes);
                
                var identifiedName = Chord.Identify(inputNotes, currentKeyContext);
                originalNames.Add(identifiedName == "Unknown" ? "" : identifiedName); 
            }
        }

        return ProcessInputInternal(chordGroups, keyInput, condense, omitDuplicates, preserveStructure, customAxisSum, hasExplicitOctaves, originalNames, keyContextsPerGroup, modulationsPerGroup, isChordMode);
    }

    private static List<List<Note>> ProcessInputInternal(List<List<Note>> chordGroups, string keyInput, bool condense = false, bool omitDuplicates = false, bool preserveStructure = false, int? customAxisSum = null, bool hasExplicitOctaves = true, List<string>? originalNames = null, List<KeyContext>? keyContextsPerGroup = null, List<string?>? modulationsPerGroup = null, bool isChordMode = false)
    {
        if (originalNames == null) originalNames = chordGroups.Select(g => "").ToList();
        if (keyContextsPerGroup == null) keyContextsPerGroup = chordGroups.Select(g => KeyContext.Parse(keyInput)).ToList();
        if (modulationsPerGroup == null) modulationsPerGroup = chordGroups.Select(g => (string?)null).ToList();

        // Calculate a global axis based on the entire progression to prevent large jumps
        int? globalAxisSum = customAxisSum;
        if (!globalAxisSum.HasValue && chordGroups.Any())
        {
            var allNotes = chordGroups.SelectMany(g => g).ToList();
            if (allNotes.Any())
            {
                globalAxisSum = HarmonyMapper.CalculateAxisSum(allNotes, keyInput);
            }
        }

        var processedResults = new List<(string NegName, List<Note> NegNotes)>();
        for (var i = 0; i < chordGroups.Count; i++)
        {
            var originalNotes = chordGroups[i];
            var context = keyContextsPerGroup[i];
            
            // We use the context-specific axis calculation unless it's the main key,
            // but for a single sequence, we should stick to the global axis if possible.
            // If modulation happened, we might need to adjust, but for now let's try global.
            var accStr = context.Tonic.Accidental switch
            {
                Accidental.DoubleSharp => "##",
                Accidental.Sharp => "#",
                Accidental.DoubleFlat => "bb",
                Accidental.Flat => "b",
                _ => ""
            };
            var keyStr = $"{context.Tonic.NoteName}{accStr} {context.Mode}";
            
            var (mappedNotes, negContext) = HarmonyMapper.MapNegativeWithContext(
                originalNotes, 
                keyStr, 
                customAxisSum: globalAxisSum,
                preserveStructure: preserveStructure);
            
            // Re-apply naming based on negative context to ensure correct spelling of chromatic notes
            mappedNotes = mappedNotes.Select(n => {
                var m = Note.FromAbsolutePitch(n.AbsolutePitch, negContext);
                return new Note {
                    NoteName = m.NoteName,
                    Accidental = m.Accidental,
                    Octave = m.Octave,
                    OriginalTime = n.OriginalTime,
                    OriginalDuration = n.OriginalDuration,
                    OriginalVelocity = n.OriginalVelocity,
                    OriginalChannel = n.OriginalChannel,
                    OriginalInstrument = n.OriginalInstrument
                };
            }).ToList();
            
            // Further refine spelling based on identified chord structure (e.g., prefer E# over F in C# Major)
            mappedNotes = Chord.ReSpell(mappedNotes, negContext);

            // Simplify double sharps/flats for readability
            mappedNotes = mappedNotes.Select(n => n.Simplify()).ToList();

            // Match the output average octave to the input average octave to preserve register
            var inputAvgOctave = originalNotes.Average(n => n.Octave);
            var mappedAvgOctave = mappedNotes.Average(n => n.Octave);
            var octaveShift = (int)Math.Round(inputAvgOctave - mappedAvgOctave);
            
            if (octaveShift != 0)
            {
                mappedNotes = mappedNotes.Select(n => {
                    var m = Note.FromAbsolutePitch(n.AbsolutePitch + octaveShift * 12, negContext);
                    return new Note {
                        NoteName = m.NoteName,
                        Accidental = m.Accidental,
                        Octave = m.Octave,
                        OriginalTime = n.OriginalTime,
                        OriginalDuration = n.OriginalDuration,
                        OriginalVelocity = n.OriginalVelocity,
                        OriginalChannel = n.OriginalChannel,
                        OriginalInstrument = n.OriginalInstrument
                    };
                }).ToList();
            }
            
            if (condense)
            {
                mappedNotes = Note.Condense(mappedNotes, negContext);
            }

            // Apply instrument range limits
            if (mappedNotes.Any())
            {
                var instrument = mappedNotes.First().OriginalInstrument;
                if (instrument.HasValue)
                {
                    (int? minPitch, int? maxPitch) = instrument.Value switch
                    {
                        >= 24 and <= 31 => (23, 88), // Guitars (Acoustic, Electric) -> B0 (MIDI 23) to E6 (MIDI 88)
                        >= 32 and <= 39 => (23, 67), // Basses -> B0 (MIDI 23) to G4 (MIDI 67)
                        _ => ((int?)null, (int?)null)
                    };

                    if (minPitch.HasValue)
                    {
                        var lowestCurrent = mappedNotes.Min(n => n.AbsolutePitch);
                        if (lowestCurrent < minPitch.Value)
                        {
                            var shift = (int)Math.Ceiling((minPitch.Value - lowestCurrent) / 12.0);
                            mappedNotes = mappedNotes.Select(n => {
                                var m = Note.FromAbsolutePitch(n.AbsolutePitch + shift * 12, negContext);
                                return new Note {
                                    NoteName = m.NoteName,
                                    Accidental = m.Accidental,
                                    Octave = m.Octave,
                                    OriginalTime = n.OriginalTime,
                                    OriginalDuration = n.OriginalDuration,
                                    OriginalVelocity = n.OriginalVelocity,
                                    OriginalChannel = n.OriginalChannel,
                                    OriginalInstrument = n.OriginalInstrument
                                };
                            }).ToList();
                        }
                    }

                    if (maxPitch.HasValue)
                    {
                        var highestCurrent = mappedNotes.Max(n => n.AbsolutePitch);
                        if (highestCurrent > maxPitch.Value)
                        {
                            var shift = (int)Math.Ceiling((highestCurrent - maxPitch.Value) / 12.0);
                            mappedNotes = mappedNotes.Select(n => {
                                var m = Note.FromAbsolutePitch(n.AbsolutePitch - shift * 12, negContext);
                                return new Note {
                                    NoteName = m.NoteName,
                                    Accidental = m.Accidental,
                                    Octave = m.Octave,
                                    OriginalTime = n.OriginalTime,
                                    OriginalDuration = n.OriginalDuration,
                                    OriginalVelocity = n.OriginalVelocity,
                                    OriginalChannel = n.OriginalChannel,
                                    OriginalInstrument = n.OriginalInstrument
                                };
                            }).ToList();
                        }
                    }
                }
            }

            if (omitDuplicates)
            {
                mappedNotes = mappedNotes.GroupBy(n => n.PitchClass).Select(g => g.First()).ToList();
            }
            
            var negativeChordName = Chord.Identify(mappedNotes, negContext);
            var negativeName = negativeChordName == "Unknown" ? "" : negativeChordName;
            processedResults.Add((negativeName, mappedNotes));
        }

        var inputParts = new List<string>();
        for (int i = 0; i < chordGroups.Count; i++)
        {
            var part = "";
            if (modulationsPerGroup[i] != null) part += modulationsPerGroup[i] + " ";
            var name = originalNames[i];
            part += (string.IsNullOrEmpty(name) || name == "Unknown" || (isChordMode && name == chordGroups[i].FirstOrDefault()?.ToString(hasExplicitOctaves))) 
                ? string.Join(", ", chordGroups[i].Select(note => note.ToString(hasExplicitOctaves))) 
                : name;
            inputParts.Add(part);
        }
        var inputStr = string.Join(" | ", inputParts);
        Console.WriteLine($"Original: {inputStr}");
        Console.WriteLine($"Initial Key: {keyInput}");
        Console.WriteLine("\nNegative Harmony Mapping:");

        var maxOriginalNameWidth = originalNames.Max(n => (string.IsNullOrEmpty(n) || n == "Unknown" ? "" : $"({n}) ").Length);
        var maxNegativeNameWidth = processedResults.Max(r => (string.IsNullOrEmpty(r.NegName) || r.NegName == "Unknown" ? "" : $"({r.NegName}) ").Length);

        var origNameColWidth = maxOriginalNameWidth > 0 ? maxOriginalNameWidth + 3 : 0;
        var negNameColWidth = maxNegativeNameWidth > 0 ? maxNegativeNameWidth + 3 : 0;

        var maxNotesInGroup = Math.Max(chordGroups.Count > 0 ? chordGroups.Max(g => g.Count) : 0, 
                                       processedResults.Count > 0 ? processedResults.Max(r => r.NegNotes.Count) : 0);
        var originalNoteWidths = new int[maxNotesInGroup];
        var negativeNoteWidths = new int[maxNotesInGroup];

        for (var j = 0; j < maxNotesInGroup; j++)
        {
            originalNoteWidths[j] = chordGroups.Any(g => g.Count > j) 
                ? chordGroups.Where(g => g.Count > j).Max(g => (g[j].ToString(hasExplicitOctaves) + (j < g.Count - 1 ? ", " : "")).Length)
                : 0;
            negativeNoteWidths[j] = processedResults.Any(r => r.NegNotes.Count > j)
                ? processedResults.Where(r => r.NegNotes.Count > j).Max(r => (r.NegNotes[j].ToString(hasExplicitOctaves) + (j < r.NegNotes.Count - 1 ? ", " : "")).Length)
                : 0;
        }

        const string arrow = "   =>   ";

        var rows = new List<string>();
        for (var i = 0; i < chordGroups.Count; i++)
        {
            var origName = originalNames[i];
            var origNotes = chordGroups[i];
            var (negName, negNotes) = processedResults[i];

            var origNamePart = string.IsNullOrEmpty(origName) || origName == "Unknown"
                ? new string(' ', origNameColWidth) 
                : $"({origName}) ".PadRight(origNameColWidth);
            
            var origNotesPart = "";
            for (var j = 0; j < origNotes.Count; j++)
            {
                var noteStr = origNotes[j].ToString(hasExplicitOctaves) + (j < origNotes.Count - 1 ? ", " : "");
                origNotesPart += noteStr.PadRight(originalNoteWidths[j]);
            }
            origNotesPart += new string(' ', originalNoteWidths.Sum() - originalNoteWidths.Take(origNotes.Count).Sum());

            var negNamePart = string.IsNullOrEmpty(negName) || negName == "Unknown"
                ? new string(' ', negNameColWidth) 
                : $"({negName}) ".PadRight(negNameColWidth);

            var negNotesPart = "";
            for (var j = 0; j < negNotes.Count; j++)
            {
                var noteStr = negNotes[j].ToString(hasExplicitOctaves) + (j < negNotes.Count - 1 ? ", " : "");
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
        for (var i = 0; i < rows.Count; i++)
        {
            if (modulationsPerGroup[i] != null)
            {
                Console.WriteLine($"\n--- Modulation to {modulationsPerGroup[i]} ---");
            }
            Console.WriteLine(rows[i]);
        }

        var finalProgression = new List<string>();
        for (var i = 0; i < chordGroups.Count; i++)
        {
            var (negName, negNotes) = processedResults[i];
            var negativeNotesStr = string.Join(", ", negNotes.Select(n => n.ToString(hasExplicitOctaves)));
            finalProgression.Add(string.IsNullOrEmpty(negName) ? $"[{negativeNotesStr}]" : negName);
        }
        Console.WriteLine($"\nResulting Progression: {string.Join(" - ", finalProgression)}");
        return processedResults.Select(r => r.NegNotes).ToList();
    }

    private static List<Note> ParseSequenceWithContext(string[] inputs, Note? lastNoteInPreviousGroup)
    {
        var result = new List<Note>();
        var previous = lastNoteInPreviousGroup;

        foreach (var input in inputs)
        {
            var match = Regex.Match(input.Trim(), @"^([A-G])(##|#|bb|b)?(\d+)?$", RegexOptions.IgnoreCase);
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
                    var nextPitch = currentPitch;
                    
                    // Style 2 expects that notes are chosen as the nearest pitch from the previous note.
                    // This can however lead to octave creep in long sequences if they are mostly ascending.
                    // BUT, if the sequence is mostly diatonic, it should stay within a reasonable range.
                    // Let's refine the "nearest" logic to actually pick the nearest absolute pitch.
                    
                    var possiblePitches = new[] { 
                        currentPitch + (targetPitchClass - currentPitch % 12 + 12) % 12, // UP or same
                        currentPitch + (targetPitchClass - currentPitch % 12 - 12) % 12  // DOWN or same
                    };
                    
                    // Choose the one that is closer to currentPitch
                    if (Math.Abs(possiblePitches[0] - currentPitch) <= Math.Abs(possiblePitches[1] - currentPitch))
                    {
                        nextPitch = possiblePitches[0];
                    }
                    else
                    {
                        nextPitch = possiblePitches[1];
                    }
                    
                    // If we're hitting boundaries (Octave 0 or 8), we should cap it.
                    if (nextPitch < 12) // Below C0 (12)
                    {
                        nextPitch += 12;
                    }
                    else if (nextPitch > 107) // Above B8 (107)
                    {
                        nextPitch -= 12;
                    }
                    
                    // Determine the letter name for the next note
                    var targetNoteName = tempNote.NoteName;
                    var basePC = GetBasePitchClass(targetNoteName);
                    var pc = nextPitch % 12;
                    if (pc < 0) pc += 12;
                    
                    var diff = pc - basePC;
                    while (diff > 6) diff -= 12;
                    while (diff < -6) diff += 12;
                    
                    var finalNote = new Note
                    {
                        NoteName = targetNoteName,
                        Accidental = diff == 0 ? null : (Accidental)diff,
                        Octave = (nextPitch / 12) - 1
                    };
                    
                    result.Add(finalNote);
                    previous = finalNote;
                }
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

    public static bool IsChordHeuristic(string input)
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
