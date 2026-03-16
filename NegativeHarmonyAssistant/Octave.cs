namespace NegativeHarmonyAssistant;

public readonly record struct Octave
{
    public int Value { get; }
    public Octave(int value)
    {
        if (value is < 0 or > 8)
            throw new ArgumentOutOfRangeException(nameof(value), "Octave must be between 0 and 8.");
        Value = value;
    }
    public static implicit operator int(Octave o) => o.Value;
    public static implicit operator Octave(int v) => new(v);
    public override string ToString() => Value.ToString();
}
