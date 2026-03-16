# Negative Harmony Assistant

A sophisticated command-line tool for musicians to explore **Negative Harmony**. It allows you to map notes and chord progressions across the Tonic-Dominant axis, providing diatonically correct results and intelligent chord identification.

## Features

- **Musical Intelligence**: Automatically calculates the negative harmony axis for any key and maps notes while respecting musical context (e.g., C Major maps to G Phrygian).
- **Flexible Input Styles**:
  - **Notes with Octaves**: `C4, Eb4, G4`
  - **Ascending Note Sequences**: `C, E, G, C` (automatically calculates octaves in ascending order).
  - **Chord Names**: `C major`, `Am7`, `F#dim`, `G7`, `Cm7b5`, etc.
- **Progression Support**: Use the `|` delimiter to process entire harmonic movements (e.g., `Dm7 | G7 | Cmaj7`).
- **Interactive Console UI**: A friendly, persistent interface for rapid experimentation.
- **Advanced Toggles**:
  - **Condense Chords**: Bring spread-out reflections into close-voiced arrangements.
  - **Omit Duplicates**: Simplify chords by removing redundant pitch classes.
- **Visual Alignment**: Beautifully formatted table output with vertical alignment for easy reading of complex mappings.

## Usage

### Interactive Mode
Simply run the executable without arguments to enter the interactive mode:
```bash
dotnet run
```
Follow the prompts to set your key and start mapping!

### One-Shot Mode
Pass the notes and the key as arguments for a quick mapping:
```bash
dotnet run "C, E, G, B" "C Major"
```

## CI/CD and Releases
This project uses GitHub Actions for Continuous Integration and Continuous Deployment.
- **CI**: Every push or pull request to `main` triggers a build and a full test suite run.
- **CD**: Pushing a tag starting with `v` (e.g., `v1.0.0`) automatically builds, tests, and publishes self-contained binaries for Windows, Linux, and macOS to the GitHub Releases page.

## Supported Chords
The tool identifies and parses a wide variety of chord structures:
- **Triads**: Major, Minor, Diminished, Augmented, Sus4, Sus2.
- **7th Chords**: Major 7th, Minor 7th, Dominant 7th, Diminished 7th, Half-Diminished (`m7b5`).

## Development Workflow

To ensure a high level of stability, the project uses a standard **Trunk-Based Development** model:

1.  **Feature Branches**: Create a branch for any new feature or bugfix (e.g., `feat/my-new-feature`).
2.  **Pull Request**: Merge your branch into `main` via a PR. The **CI/CD pipeline** automatically runs all 26 unit tests.
3.  **Release**: When you are ready to publish a new version, simply create and push a git tag:
    ```bash
    git tag v1.0.0
    git push origin v1.0.0
    ```
    GitHub Actions will then build, test, and attach portable binaries for Windows, Linux, and macOS to a new release on the GitHub project page.

## Technical Details
- Built with **.NET 10** and **C# 14**.
- Uses an axis-based reflection logic (Tonic-Dominant).
- Diatonic naming engine ensures that results use the correct accidentals for the target negative key.
- Includes a comprehensive **xUnit** test suite for harmonic verification.

## Development
To run the tests:
```bash
dotnet test
```

---
*Created for musicians who want to flip their perspective.*
