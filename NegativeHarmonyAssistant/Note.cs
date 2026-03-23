using System.Text.RegularExpressions;

namespace NegativeHarmonyAssistant;

public class Note
{
    public NoteName NoteName { get; init; }
    public Accidental? Accidental { get; init; }
    public Octave Octave { get; init; }
    public long? OriginalTime { get; init; }
    public long? OriginalDuration { get; init; }
    public byte? OriginalVelocity { get; init; }
    public byte? OriginalChannel { get; init; }

    public int PitchClass
    {
        get
        {
            var pc = NoteName switch
            {
                NoteName.C => 0,
                NoteName.D => 2,
                NoteName.E => 4,
                NoteName.F => 5,
                NoteName.G => 7,
                NoteName.A => 9,
                NoteName.B => 11,
                _ => throw new ArgumentOutOfRangeException(nameof(NoteName))
            };
            return (pc + (int)(Accidental ?? 0) + 12) % 12;
        }
    }

    public int AbsolutePitch => (Octave + 1) * 12 + PitchClass;

    public static Note Parse(string input, int? defaultOctave = null)
    {
        input = input.Trim();
        var match = Regex.Match(input, @"^([A-G])(##|#|bb|b)?(\d)?$", RegexOptions.IgnoreCase);
        if (!match.Success)
            throw new ArgumentException($"Invalid note format: {input}. Expected format like 'C3' or 'Eb4'.");

        var name = Enum.Parse<NoteName>(match.Groups[1].Value, true);
        var accidental = match.Groups[2].Value switch
        {
            "##" => NegativeHarmonyAssistant.Accidental.DoubleSharp,
            "#" => NegativeHarmonyAssistant.Accidental.Sharp,
            "bb" => NegativeHarmonyAssistant.Accidental.DoubleFlat,
            "b" => NegativeHarmonyAssistant.Accidental.Flat,
            _ => (Accidental?)null
        };
        
        var octaveValue = string.IsNullOrEmpty(match.Groups[3].Value) 
            ? (defaultOctave ?? 4) 
            : int.Parse(match.Groups[3].Value);
        
        return new Note
        {
            NoteName = name,
            Accidental = accidental,
            Octave = octaveValue
        };
    }

    public static List<Note> ParseSequence(string[] inputs)
    {
        var result = new List<Note>();
        Note? previous = null;

        foreach (var input in inputs)
        {
            var match = Regex.Match(input.Trim(), @"^([A-G])(##|#|bb|b)?(\d+)?$", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new ArgumentException($"Invalid note format in sequence: {input}");

            if (match.Groups[3].Value is [var octChar] && int.TryParse(octChar.ToString(), out var octVal))
            {
                var note = Parse(input);
                result.Add(note);
                previous = note;
            }
            else
            {
                if (previous == null)
                {
                    var note = Parse(input, 4);
                    result.Add(note);
                    previous = note;
                }
                else
                {
                    var tempNote = Parse(input, 0);
                    var targetPitchClass = tempNote.PitchClass;
                    var currentPitch = previous.AbsolutePitch;
                    
                    var possiblePitches = new[] { 
                        currentPitch + (targetPitchClass - currentPitch % 12 + 12) % 12, // UP or same
                        currentPitch + (targetPitchClass - currentPitch % 12 - 12) % 12  // DOWN or same
                    };
                    
                    int nextPitch;
                    if (Math.Abs(possiblePitches[0] - currentPitch) <= Math.Abs(possiblePitches[1] - currentPitch))
                    {
                        nextPitch = possiblePitches[0];
                    }
                    else
                    {
                        nextPitch = possiblePitches[1];
                    }
                    
                    var note = FromAbsolutePitch(nextPitch);
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

    public static Note FromAbsolutePitch(int absolutePitch, KeyContext? context = null, bool? preferSharps = null)
    {
        // Cap absolute pitch to stay within valid octave range (0 to 8)
        // Octave 0 starts at C0 (pitch 12). Octave 8 ends at B8 (pitch 119).
        if (absolutePitch < 12) absolutePitch = 12 + (absolutePitch % 12 + 12) % 12;
        if (absolutePitch > 119) absolutePitch = 108 + (absolutePitch % 12);

        var pc = absolutePitch % 12;
        if (pc < 0) pc += 12;
        var octaveValue = (int)Math.Floor(absolutePitch / 12.0) - 1;

        if (context?.PitchClassToDiatonicName.TryGetValue(pc, out var named) == true)
        {
            return new Note
            {
                NoteName = named.Name,
                Accidental = named.Accidental,
                Octave = octaveValue
            };
        }

        if (context != null)
        {
            // Find the nearest diatonic note
            var diatonicPcs = context.PitchClassToDiatonicName.Keys.ToList();
            var nearest = diatonicPcs
                .Select(dpc => new { dpc, dist = (pc - dpc + 12) % 12 })
                .Select(x => new { x.dpc, dist = x.dist > 6 ? x.dist - 12 : x.dist })
                .OrderBy(x => Math.Abs(x.dist))
                .ThenByDescending(x =>
                {
                    // If equidistant (dist is 1 or -1), we must choose based on musical context.
                    // For dominant chords in minor keys, we prefer the sharpened leading tone.
                    // E.g., in G minor, target F# (11) is between F (10) and G (0).
                    // dist to F is 1, dist to G is -1.
                    // If we want F#, we prefer positive dist (sharpened).
                    
                    // In flat keys (PreferSharps = false), we usually prefer flats.
                    // BUT for the leading tone specifically, we prefer sharp.
                    
                    // Leading tone to the root: target = root - 1.
                    // Leading tone to the dominant: target = dominant - 1.
                    
                    // Heuristic: If it's the leading tone to any diatonic note, prefer sharpened.
                    var targetNext = (pc + 1) % 12;
                    if (context.PitchClassToDiatonicName.ContainsKey(targetNext))
                    {
                        // Special case: if targetNext is the tonic, it's definitely a leading tone
                        if (targetNext == context.Tonic.PitchClass)
                            return x.dist;
                        
                        // If it's the leading tone to the dominant (5th degree)
                        var intervals = context.Mode.GetIntervals();
                        var dominantPC = (context.Tonic.PitchClass + intervals[4]) % 12; // 5th degree
                        if (targetNext == dominantPC)
                            return x.dist;
                    }
                    
                    // What about the minor 3rd in G Mixolydian (Bb vs A#)?
                    // G Mixolydian: G(0), A(2), B(4), C(5), D(7), E(9), F(10).
                    // Target Bb/A# (3). 3+1 = 4. 4 is B (in map).
                    // So it currently returns x.dist -> A#.
                    // BUT Bb is NOT a leading tone to B in the context of the minor 3rd.
                    
                    // If PreferSharps is false (e.g. key has flats), we should prefer flats (x.dist negative).
                    // G Mixolydian has NO accidentals in scale. 
                    // Current PreferSharps calculation in KeyContext:
                    // PreferSharps = !PitchClassToDiatonicName.Values.Any(v => v.Accidental is Accidental.Flat or Accidental.DoubleFlat);
                    // For G Mixolydian, this is TRUE.
                    
                    // BUT HarmonyMapper has logic to force PreferSharps to FALSE if original was FALSE and negative has NO accidentals.
                    
                    /* From HarmonyMapper.cs:
                    var finalPreferSharps = negativeKeyContext.PreferSharps;
                    if (!originalKey.PreferSharps && negativeKeyContext.PitchClassToDiatonicName.Values.All(v => v.Accidental == null))
                    {
                        finalPreferSharps = false;
                    }
                    */
                    
                    // HOWEVER, Note.FromAbsolutePitch uses context.PreferSharps which is the one from KeyContext.
                    // The one from HarmonyMapper is passed as a parameter `preferSharps`.
                    
                    var actualPreferSharps = preferSharps ?? context.PreferSharps;
                    return actualPreferSharps ? x.dist : -x.dist;
                })
                .First();

            var baseNote = context.PitchClassToDiatonicName[nearest.dpc];
            var finalAccidental = (int)(baseNote.Accidental ?? 0) + nearest.dist;
            return new Note
            {
                NoteName = baseNote.Name,
                Accidental = finalAccidental == 0 ? null : (Accidental)finalAccidental,
                Octave = octaveValue
            };
        }

        var actualPreferSharps = preferSharps ?? context?.PreferSharps ?? true;
        var result = pc switch
        {
            0 => (NoteName.C, (Accidental?)null),
            1 => actualPreferSharps ? (NoteName.C, NegativeHarmonyAssistant.Accidental.Sharp) : (NoteName.D, NegativeHarmonyAssistant.Accidental.Flat),
            2 => (NoteName.D, (Accidental?)null),
            3 => actualPreferSharps ? (NoteName.D, NegativeHarmonyAssistant.Accidental.Sharp) : (NoteName.E, NegativeHarmonyAssistant.Accidental.Flat),
            4 => (NoteName.E, (Accidental?)null),
            5 => (NoteName.F, (Accidental?)null),
            6 => actualPreferSharps ? (NoteName.F, NegativeHarmonyAssistant.Accidental.Sharp) : (NoteName.G, NegativeHarmonyAssistant.Accidental.Flat),
            7 => (NoteName.G, (Accidental?)null),
            8 => actualPreferSharps ? (NoteName.G, NegativeHarmonyAssistant.Accidental.Sharp) : (NoteName.A, NegativeHarmonyAssistant.Accidental.Flat),
            9 => (NoteName.A, (Accidental?)null),
            10 => actualPreferSharps ? (NoteName.A, NegativeHarmonyAssistant.Accidental.Sharp) : (NoteName.B, NegativeHarmonyAssistant.Accidental.Flat),
            11 => (NoteName.B, (Accidental?)null),
            _ => throw new ArgumentOutOfRangeException(nameof(pc))
        };

        return new Note
        {
            NoteName = result.Item1,
            Accidental = result.Item2,
            Octave = octaveValue
        };
    }

    public override string ToString()
    {
        return ToString(true);
    }

    public string ToString(bool includeOctave)
    {
        var accStr = Accidental switch
        {
            NegativeHarmonyAssistant.Accidental.DoubleSharp => "##",
            NegativeHarmonyAssistant.Accidental.Sharp => "#",
            NegativeHarmonyAssistant.Accidental.DoubleFlat => "bb",
            NegativeHarmonyAssistant.Accidental.Flat => "b",
            _ => ""
        };
        var octStr = includeOctave ? Octave.ToString() : "";
        return $"{NoteName}{accStr}{octStr}";
    }

    public Note Simplify()
    {
        if (Accidental is not (NegativeHarmonyAssistant.Accidental.DoubleSharp or NegativeHarmonyAssistant.Accidental.DoubleFlat))
            return this;

        var pc = PitchClass;
        // Simplify double sharps/flats to nearest single accidental or natural
        var simplified = FromAbsolutePitch(AbsolutePitch);
        return new Note
        {
            NoteName = simplified.NoteName,
            Accidental = simplified.Accidental,
            Octave = simplified.Octave,
            OriginalTime = this.OriginalTime,
            OriginalDuration = this.OriginalDuration,
            OriginalVelocity = this.OriginalVelocity,
            OriginalChannel = this.OriginalChannel
        };
    }

    public static List<Note> Condense(List<Note> notes, KeyContext? context = null)
    {
        if (notes is []) return [];
        var ordered = notes.OrderBy(n => n.AbsolutePitch).ToList();
        var anchor = ordered[0];
        var result = new List<Note> { anchor };

        for (var i = 1; i < ordered.Count; i++)
        {
            var targetPC = ordered[i].PitchClass;
            var nextPitch = anchor.AbsolutePitch + 1;
            while (nextPitch % 12 != targetPC) nextPitch++;
            
            var condensedNote = FromAbsolutePitch(nextPitch, context);
            // Preserving original naming if possible
            result.Add(new Note
            {
                NoteName = ordered[i].NoteName,
                Accidental = ordered[i].Accidental,
                Octave = condensedNote.Octave,
                OriginalTime = ordered[i].OriginalTime,
                OriginalDuration = ordered[i].OriginalDuration,
                OriginalVelocity = ordered[i].OriginalVelocity,
                OriginalChannel = ordered[i].OriginalChannel
            });
        }

        return result.OrderBy(n => n.AbsolutePitch).ToList();
    }
}
