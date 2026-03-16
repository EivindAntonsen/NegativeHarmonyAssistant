namespace NegativeHarmonyAssistant.Tests;

public class BugReproductionTests
{
    [Fact]
    public void FSharpInCMajorKey_ShouldResultIn_ReasonableOctaves()
    {
        // The user reported F# mapping to Dm (A4, F4, D4) but then A mapping to Bm (F#6, D6, B5)
        // This suggests inconsistent axis calculation or note/chord parsing issues.
        
        var inputChord = "F#";
        var key = "E major";
        
        var chord = Chord.Parse(inputChord);
        var (mappedNotes, context) = HarmonyMapper.MapNegativeWithContext(chord.Notes, key);
        
        // Assert that mapped octaves are within a reasonable range (e.g., around octave 4)
        foreach (var note in mappedNotes)
        {
            Assert.True(note.Octave >= 2 && note.Octave <= 6, $"Octave {note.Octave} is out of expected range for input F# in {key}. Note: {note}");
        }
    }

    [Fact]
    public void ChordA_InCMajorKey_ShouldResultIn_ReasonableOctaves()
    {
        var inputChord = "A";
        var key = "E major";
        
        var chord = Chord.Parse(inputChord);
        var (mappedNotes, context) = HarmonyMapper.MapNegativeWithContext(chord.Notes, key);
        
        foreach (var note in mappedNotes)
        {
            Assert.True(note.Octave >= 2 && note.Octave <= 6, $"Octave {note.Octave} is out of expected range for input A in {key}. Note: {note}");
        }
    }

    [Fact]
    public void ProgressionFSharp_A_C_ShouldResultIn_ConsistentOctaves()
    {
        var inputChords = new[] { "F#", "A", "C" };
        var key = "E major";
        
        var chords = inputChords.Select(c => Chord.Parse(c)).ToList();
        var axisSum = HarmonyMapper.CalculateAxisSum(chords[0].Notes, key);
        
        var results = new List<List<Note>>();
        foreach (var chord in chords)
        {
            var (mappedNotes, _) = HarmonyMapper.MapNegativeWithContext(chord.Notes, key, axisSum);
            results.Add(mappedNotes);
        }
        
        // F# maps to Dm (A4, F4, D4)
        // A maps to Bm (F#4, D4, B3)
        // C maps to G#m (D#5, B4, G#4)
        
        // Assert that Bm's octaves are consistent with F#'s Dm octaves
        var bMajorNotes = results[1]; // A maps to Bm
        foreach (var note in bMajorNotes)
        {
            Assert.True(note.Octave >= 3 && note.Octave <= 5, $"Octave {note.Octave} of Bm is inconsistent. Note: {note}");
        }
    }
}
