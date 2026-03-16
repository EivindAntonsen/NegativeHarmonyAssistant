namespace NegativeHarmonyAssistant;

public class KeyContext
{
    public Note Tonic { get; }
    public Mode Mode { get; }
    public Dictionary<int, (NoteName Name, Accidental? Accidental)> PitchClassToDiatonicName { get; }
    public bool PreferSharps { get; }

    public KeyContext(Note tonic, Mode mode)
    {
        Tonic = tonic;
        Mode = mode;
        PitchClassToDiatonicName = GenerateDiatonicMap(tonic, mode);
        PreferSharps = !PitchClassToDiatonicName.Values.Any(v => v.Accidental is Accidental.Flat or Accidental.DoubleFlat);
    }

    public static KeyContext Parse(string keyString)
    {
        var parts = keyString.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tonicPart = parts[0];
        var tonic = Note.Parse($"{tonicPart}4");
        var mode = Mode.Major;

        if (parts is [_, var modeStr, ..])
        {
            mode = modeStr.ToLower() switch
            {
                "maj" or "major" or "ionian" => Mode.Major,
                "m" or "min" or "minor" or "aeolian" => Mode.Minor,
                "dorian" => Mode.Dorian,
                "phrygian" => Mode.Phrygian,
                "lydian" => Mode.Lydian,
                "mixolydian" => Mode.Mixolydian,
                "locrian" => Mode.Locrian,
                _ => Mode.Major
            };
        }

        return new KeyContext(tonic, mode);
    }

    private static Dictionary<int, (NoteName, Accidental?)> GenerateDiatonicMap(Note tonic, Mode mode)
    {
        var map = new Dictionary<int, (NoteName, Accidental?)>();
        var intervals = mode.GetIntervals();
        var currentLetter = tonic.NoteName;

        for (var i = 0; i < intervals.Length; i++)
        {
            var targetPC = (tonic.PitchClass + intervals[i]) % 12;
            if (targetPC < 0) targetPC += 12;

            // Find the accidental needed for this letter to match targetPC
            var basePC = GetBasePitchClass(currentLetter);
            var diff = targetPC - basePC;
            // Handle wrapping around the octave
            while (diff > 6) diff -= 12;
            while (diff < -6) diff += 12;

            var accidental = (Accidental?)diff;
            if (diff == 0) accidental = null;

            map[targetPC] = (currentLetter, accidental);

            // Move to the next letter name (A -> B -> C -> ...)
            currentLetter = (NoteName)(((int)currentLetter + 1) % 7);
        }

        return map;
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
