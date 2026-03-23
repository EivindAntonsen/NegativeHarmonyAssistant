namespace NegativeHarmonyAssistant;

public enum Mode
{
    Ionian, Major = Ionian,
    Dorian,
    Phrygian,
    Lydian,
    Mixolydian,
    Aeolian, Minor = Aeolian,
    Locrian,
    HarmonicMinor,
    HarmonicMajor
}

public static class ModeExtensions
{
    public static int[] GetIntervals(this Mode mode) => mode switch
    {
        Mode.Ionian => [0, 2, 4, 5, 7, 9, 11],
        Mode.Dorian => [0, 2, 3, 5, 7, 9, 10],
        Mode.Phrygian => [0, 1, 3, 5, 7, 8, 10],
        Mode.Lydian => [0, 2, 4, 6, 7, 9, 11],
        Mode.Mixolydian => [0, 2, 4, 5, 7, 9, 10],
        Mode.Aeolian => [0, 2, 3, 5, 7, 8, 10],
        Mode.Locrian => [0, 1, 3, 5, 6, 8, 10],
        Mode.HarmonicMinor => [0, 2, 3, 5, 7, 8, 11],
        Mode.HarmonicMajor => [0, 2, 4, 5, 7, 8, 11],
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    public static Mode GetNegativeMode(this Mode mode) => mode switch
    {
        Mode.Ionian => Mode.Phrygian,
        Mode.Dorian => Mode.Dorian,
        Mode.Phrygian => Mode.Ionian,
        Mode.Lydian => Mode.Locrian,
        Mode.Mixolydian => Mode.Aeolian,
        Mode.Aeolian => Mode.Mixolydian,
        Mode.Locrian => Mode.Lydian,
        Mode.HarmonicMinor => Mode.HarmonicMajor,
        Mode.HarmonicMajor => Mode.HarmonicMinor,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
