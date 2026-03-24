# Negative Harmony Assistant
 
![Build Status](https://github.com/EivindAntonsen/NegativeHarmonyAssistant/actions/workflows/ci-cd.yml/badge.svg)
![Test Coverage](https://img.shields.io/badge/Coverage-100%25-brightgreen)
![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)
 
A command-line tool for musicians to explore **Negative Harmony**. It maps notes and chord progressions across the Tonic-Dominant axis, providing diatonically correct results and intelligent chord identification.

## Key Features

- **Axis-Based Reflection**: Automatically calculates the negative harmony axis for any key and maps notes while respecting musical context (e.g., C Major maps to G Phrygian).
- **Intelligent Chord Mapping**: 
  - Input chords by name (e.g., `Cmaj7`, `Am7`, `F#dim`).
  - Input raw note sequences (e.g., `C4, Eb4, G4`).
  - Automatic identification of the resulting negative chords.
- **Modulation Support**: Process chord progressions with key changes using the `[Key]` syntax (e.g., `Cmaj7 | [G Major] D7 | Gmaj7`).
- **Flexible Input Styles**:
  - **Notes with Octaves**: `C4, Eb4, G4`
  - **Ascending Note Sequences**: `C, E, G, C` (automatically calculates octaves).
  - **Delimited Progressions**: Use `|` for harmonic movements (e.g., `Dm7 | G7 | Cmaj7`).
- **Interactive UI**: A persistent console interface for rapid experimentation.
- **MIDI Support**: 
  - **Import**: Analyze MIDI files to extract chord progressions.
  - **Export**: Automatically generate a new MIDI file containing the negative harmony version of the input.
- **Diatonic Precision**: An advanced naming engine ensures results use correct accidentals for the target negative key.

## Installation

You can choose to either download a [release](https://github.com/EivindAntonsen/NegativeHarmonyAssistant/releases) for your operating system, or you can download the code and build the application yourself. The release section contains builds for windows, mac and unix systems.

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/NegativeHarmonyAssistant.git
   cd NegativeHarmonyAssistant
   ```
2. Build the project:
   ```bash
   dotnet build
   ```

## Usage

### 1. Interactive Mode
Run the tool without arguments to enter interactive mode:
```bash
dotnet run --project NegativeHarmonyAssistant
```
You will be prompted to:
- Select a **Key** (e.g., `C Major`, `Eb Minor`).
- Set **Options** (Condense chords, Omit duplicates, retain original contour).
- Enter **Notes/Chords** or **Progressions**.
- Or specify a file location for a midi file to read. Will not convert drum tracks.

### 2. One-Shot Mode
Map a specific sequence directly from your terminal:
```bash
dotnet run --project NegativeHarmonyAssistant "C, E, G, B" "C Major"
```

### 3. MIDI Processing
Process a MIDI file to extract its chord progression and export the negative harmony version:
```bash
dotnet run --project NegativeHarmonyAssistant "path/to/your.mid" "C Major"
```
The tool will:
1. Extract notes from the MIDI file (excluding drum tracks).
2. Group notes by start time into a chord progression.
3. Calculate and display the negative harmony progression in the console.
4. Export a new MIDI file (e.g., `your_negative.mid`) with the mapped notes.

## Advanced Options

- **Condense Chords**: Automatically brings spread-out reflections into close-voiced arrangements (e.g., voicing a negative chord within a single octave).
- **Omit Duplicates**: Removes redundant pitch classes (e.g., converting a 5-note voicing with doubled root into a 4-note chord).

## Supported Chords

The tool identifies and parses a wide variety of structures:
- **Triads**: Major, Minor, Diminished, Augmented, Sus4, Sus2.
- **7th Chords**: Dominant 7th, Major 7th, Minor 7th, Diminished 7th, Half-Diminished (`m7b5` or `ø`).

## How it Works

Negative harmony is based on the reflection of the chromatic scale across an axis. This tool uses the **Tonic-Dominant axis**:
- In **C Major**, the axis lies halfway between **C** (Tonic) and **G** (Dominant).
- Notes are reflected across this axis:
  - `C` ↔ `G`
  - `E` ↔ `Eb`
  - `G` ↔ `C`
- The tool handles the complex task of re-spelling these reflections in a musically meaningful way according to the target scale.

## Development

### Running Tests
Ensure everything is working correctly:
```bash
dotnet test
```

### CI/CD
- **CI**: Every push to `main` triggers a build and full test suite.
- **CD**: Pushing a tag (e.g., `v1.0.0`) automatically generates binaries for Windows, Linux, and macOS.
