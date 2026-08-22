using System;
using System.Collections.Generic;
using System.Linq;

namespace NegativeHarmonyAssistant;

public record InferenceResult(KeyContext Key, double Confidence, List<(KeyContext Key, double Score)> Candidates);

public static class KeyInferrer
{
    // Krumhansl-Schmuckler profiles
    private static readonly double[] MajorProfileRaw = [6.35, 2.23, 3.48, 2.33, 4.38, 4.09, 2.52, 5.19, 2.39, 3.66, 2.29, 2.88];
    private static readonly double[] MinorProfileRaw = [6.33, 2.68, 3.52, 5.38, 2.60, 3.53, 2.54, 4.75, 3.98, 2.69, 3.34, 3.17];

    // Mean-subtracted profiles to avoid bias towards Minor or certain scales
    private static readonly double[] MajorProfile = MajorProfileRaw.Select(w => w - MajorProfileRaw.Average()).ToArray();
    private static readonly double[] MinorProfile = MinorProfileRaw.Select(w => w - MinorProfileRaw.Average()).ToArray();

    private static readonly Dictionary<Mode, double[]> ModeProfiles;

    static KeyInferrer()
    {
        ModeProfiles = Enum.GetValues<Mode>()
            .Distinct()
            .ToDictionary(m => m, CreateProfile);
    }

    private static double[] CreateProfile(Mode mode)
    {
        var intervals = mode.GetIntervals();
        var isMajorish = intervals.Contains(4); // Major 3rd
        var baseProfileRaw = isMajorish ? MajorProfileRaw : MinorProfileRaw;
        var baseIntervals = (isMajorish ? Mode.Major : Mode.Minor).GetIntervals();

        var profile = (double[])baseProfileRaw.Clone();

        var outOfBaseInTarget = intervals.Except(baseIntervals).OrderBy(x => x).ToList();
        var inBaseOutOfTarget = baseIntervals.Except(intervals).OrderBy(x => x).ToList();

        for (int i = 0; i < Math.Min(outOfBaseInTarget.Count, inBaseOutOfTarget.Count); i++)
        {
            var targetPC = outOfBaseInTarget[i];
            var basePC = inBaseOutOfTarget[i];
            (profile[targetPC], profile[basePC]) = (profile[basePC], profile[targetPC]);
        }

        var avg = profile.Average();
        return profile.Select(w => w - avg).ToArray();
    }

    public static KeyContext Infer(IEnumerable<Note> notes)
    {
        return InferWithConfidence(new[] { notes }).Key;
    }

    public static KeyContext Infer(IEnumerable<IEnumerable<Note>> noteGroups)
    {
        return InferWithConfidence(noteGroups).Key;
    }

    public static InferenceResult InferWithConfidence(IEnumerable<Note> notes)
    {
        return InferWithConfidence(new[] { notes });
    }

    public static InferenceResult InferWithConfidence(IEnumerable<IEnumerable<Note>> noteGroups)
    {
        var groups = noteGroups.ToList();
        var allNotes = groups.SelectMany(g => g).ToList();
        if (!allNotes.Any()) return new InferenceResult(new KeyContext(Note.Parse("C4"), Mode.Major), 0.0, new());

        var firstNote = allNotes.FirstOrDefault();
        var pitchClassCounts = new int[12];
        foreach (var note in allNotes)
        {
            pitchClassCounts[note.PitchClass]++;
        }

        // Identify dominant chords for functional bias
        var dominantRoots = new List<(int Root, bool IsFull7th)>();
        foreach (var group in groups)
        {
            var notesInGroup = group.ToList();
            if (notesInGroup.Count < 3) continue;
            var (name, root) = Chord.IdentifyWithRoot(notesInGroup);
            if (name != "Unknown" && root != null)
            {
                // Dominant 7th (e.g. G7)
                if (name.EndsWith("7") && !name.Contains("maj") && !name.Contains("m"))
                {
                    dominantRoots.Add((root.PitchClass, true));
                }
                // Major triad (e.g. G)
                else if (!name.Contains("m") && !name.Contains("dim") && !name.Contains("aug") && !name.Contains("sus") && !name.Contains("7"))
                {
                    dominantRoots.Add((root.PitchClass, false));
                }
            }
        }

        var results = new List<(KeyContext Key, double Score)>();

        for (int tonic = 0; tonic < 12; tonic++)
        {
            foreach (var mode in ModeProfiles.Keys)
            {
                var profile = ModeProfiles[mode];
                
                var score = 0.0;
                for (int pc = 0; pc < 12; pc++)
                {
                    var count = pitchClassCounts[pc];
                    if (count > 0)
                    {
                        var relativePC = (pc - tonic + 12) % 12;
                        score += count * profile[relativePC];
                    }
                }

                // Functional Dominant Bonus: If a detected dominant chord is the V of this key
                // Applies to all modes with a perfect 5th. 
                // We reward Dominant 7ths strongly (+2.0) and Major triads moderately (+1.0)
                if (mode.GetIntervals().Contains(7))
                {
                    foreach (var (domRoot, isFull7th) in dominantRoots)
                    {
                        if (domRoot == (tonic + 7) % 12)
                        {
                            score += isFull7th ? 2.0 : 1.0;
                        }
                    }
                }

                // Tonic emphasis bonus: if the first note is the tonic
                if (firstNote != null && tonic == firstNote.PitchClass)
                {
                    score += 1.0;
                }

                // Simplicity bonus (tie-breaker)
                var simplicityBonus = tonic switch
                {
                    0 => 0.01, // C
                    7 => 0.009, // G
                    5 => 0.009, // F
                    2 => 0.008, // D
                    10 => 0.008, // Bb
                    9 => 0.007, // A
                    3 => 0.007, // Eb
                    _ => 0.0
                };
                score += simplicityBonus;
                if (mode == Mode.Major) score += 0.005;
                if (mode == Mode.Minor) score += 0.002;

                results.Add((new KeyContext(GetStandardTonic(tonic, mode), mode), score));
            }
        }

        results = results.OrderByDescending(r => r.Score).ToList();
        var best = results[0];
        
        double confidence = 1.0;
        if (results.Count > 1)
        {
            var second = results[1];
            var avg = results.Average(r => r.Score);
            var scoreRange = best.Score - avg;
            if (scoreRange > 0)
            {
                confidence = (best.Score - second.Score) / scoreRange;
            }
        }
        
        return new InferenceResult(best.Key, Math.Clamp(confidence, 0.0, 1.0), results.Take(5).ToList());
    }

    private static Note GetStandardTonic(int pitchClass, Mode mode)
    {
        var isMajorish = mode.GetIntervals().Contains(4);
        return pitchClass switch
        {
            0 => Note.Parse("C4"),
            1 => isMajorish ? Note.Parse("Db4") : Note.Parse("C#4"),
            2 => Note.Parse("D4"),
            3 => isMajorish ? Note.Parse("Eb4") : Note.Parse("D#4"),
            4 => Note.Parse("E4"),
            5 => Note.Parse("F4"),
            6 => isMajorish ? Note.Parse("Gb4") : Note.Parse("F#4"),
            7 => Note.Parse("G4"),
            8 => isMajorish ? Note.Parse("Ab4") : Note.Parse("G#4"),
            9 => Note.Parse("A4"),
            10 => isMajorish ? Note.Parse("Bb4") : Note.Parse("A#4"),
            11 => Note.Parse("B4"),
            _ => Note.Parse("C4")
        };
    }
}
