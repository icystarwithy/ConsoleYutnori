
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

The output should show a version beginning with `10.`.

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

The computer automatically selects a move using a simple greedy strategy:

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

## Board Representation

The board is displayed as a text-based grid showing:

* Board positions
* Player pieces (`a1`, `a2`, `a3`, `a4`)
* Computer pieces (`A1`, `A2`, `A3`, `A4`)

Example:

```text
[10: ][ 9: ][ 8: ]      [ 7: ][ 6: ][ 5: ]
[18: ][11: ]            [13: ][ 4: ]
[19: ]      [12: ]    [14: ]      [ 3:a1]
           [15: ]
[20: ]      [16: ]    [23: ]      [ 2: ]
[21: ][17: ]            [24: ][ 1:A1]
[22: ][25: ][26: ]      [27: ][28: ][29: ]
```

---

## Project Structure

```text
ConsoleYutnori/
├── ConsoleYutnori.fsproj
├── Program.fs
└── README.md
```

### Main Components

| Component      | Responsibility                                       |
| -------------- | ---------------------------------------------------- |
| Rule Logic     | Piece movement, shortcuts, finishing, captures       |
| Random Cast    | Simulates Yut throws using probability distributions |
| Board Renderer | Displays the Yutnori board and piece locations       |
| User Input     | Handles move selection and validation                |
| Computer AI    | Chooses moves using a greedy strategy                |
| Game Loop      | Controls turns, victory checks, and replay           |

---

## Rules Summary

* The player controls four pieces.
* The computer controls four pieces.
* All pieces start at position 0.
* All pieces must reach the finish position.
* Yut and Mo grant additional throws.
* Capturing grants a bonus throw.
* Shortcut paths may be used when available.
* Back Do is supported.
* Stacked pieces move together.
* The first player to finish all four pieces wins.

---

## Example Session

```text
Turn #0 - Player

Rolling Yut...

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
