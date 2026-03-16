using System.Text.RegularExpressions;

namespace NegativeHarmonyAssistant;

public class Note
{
    public NoteName NoteName { get; init; }
    public Accidental? Accidental { get; init; }
    public Octave Octave { get; init; }

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
            var match = Regex.Match(input.Trim(), @"^([A-G])(##|#|bb|b)?(\d)?$", RegexOptions.IgnoreCase);
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
                    var nextPitch = previous.AbsolutePitch + 1;
                    
                    while (nextPitch % 12 != targetPitchClass)
                    {
                        nextPitch++;
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
        var accStr = Accidental switch
        {
            NegativeHarmonyAssistant.Accidental.DoubleSharp => "##",
            NegativeHarmonyAssistant.Accidental.Sharp => "#",
            NegativeHarmonyAssistant.Accidental.DoubleFlat => "bb",
            NegativeHarmonyAssistant.Accidental.Flat => "b",
            _ => ""
        };
        return $"{NoteName}{accStr}{Octave}";
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
                Octave = condensedNote.Octave
            });
        }

        return result.OrderBy(n => n.AbsolutePitch).ToList();
    }
}
