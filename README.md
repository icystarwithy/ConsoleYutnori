# Console Yutnori

A command-line implementation of the traditional Korean board game **Yutnori**, built with **F# / .NET 10**.

Play against a computer opponent on a text-based Yutnori board. The game supports shortcut paths, piece stacking, capturing, bonus throws, Back Do, and replay functionality.

---

## Getting Started

### Prerequisites

* .NET 10 SDK

Verify your installation:

```bash
dotnet --version
```

### Run

```bash
dotnet run
```

### Build

```bash
dotnet build
```

---

## How to Play

### Objective

Move all four of your pieces from the starting position to the finish before the computer does.

### Yut Scores

| Result  | Value |
| ------- | ----- |
| Do      | 1     |
| Gae     | 2     |
| Geol    | 3     |
| Yut     | 4     |
| Mo      | 5     |
| Back Do | -1    |

* Rolling **Yut** or **Mo** grants an additional throw.
* Capturing an opponent's piece grants a bonus throw.
* Back Do moves a piece backward according to the game rules.

### Player Turn

1. The board is displayed.
2. The Yut sticks are thrown automatically.
3. Available Yut scores are shown.
4. Select which score to use.
5. Select which piece to move.
6. If a shortcut path is available, choose whether to use it.

### Computer Turn

The computer automatically selects a move using a greedy strategy:

1. Prefer moves that finish pieces.
2. Prefer moves that capture opponent pieces.
3. Prefer advantageous shortcut paths.
4. Otherwise move the most beneficial piece.

### Piece Stacking

Pieces occupying the same position are stacked together.

When one stacked piece moves, all stacked pieces move together.

### Capturing

If a piece lands on a position occupied by an opponent's piece:

* The opponent's piece is sent back to the start.
* The current player receives a bonus throw.

### Winning

The game ends when all four pieces of one player reach the finish position.

---

## Project Structure

```text
ConsoleYutnori/
├── ConsoleYutnori.fsproj
├── Program.fs
├── README.md
└── .gitignore
```

---

## Main Features

* User vs Computer gameplay
* Support for Do, Gae, Geol, Yut, Mo, and Back Do
* Piece stacking (group movement)
* Piece capturing
* Bonus throws
* Shortcut paths
* Automatic computer opponent
* Replay support after game completion

---

## LLM Usage

This project was developed with assistance from ChatGPT.

ChatGPT was used for:

* Generating portions of the F# source code
* Generating and improving code comments
* Debugging and correcting F# syntax issues
* Improving the structure and clarity of the README documentation

One notable issue involved the implementation of the piece stacking (group movement) rule. The initial ChatGPT-generated logic did not fully match the intended Yutnori rules because the prompt only stated that the game should follow Yutnori rules, without explicitly describing how stacked pieces should move together. As a result, the stacking logic was manually reviewed and modified to correctly reflect the intended game behavior.

All generated content was reviewed, tested, and integrated into the final implementation by the author.

---

## Example Run

```text
Turn #0 - Player

Result: Geol (3)

Available Yut Scores:
[1] Geol (3)

Move Piece:
[1] Piece 1 (0 -> 3)
[2] Piece 2 (0 -> 3)
[3] Piece 3 (0 -> 3)
[4] Piece 4 (0 -> 3)

Select Piece: 1
```

Enjoy playing Yutnori!
