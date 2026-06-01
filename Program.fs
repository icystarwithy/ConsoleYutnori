// ============================================================
//  F# Console Yutnori Game
//  - Player vs Computer
//  - Reimplementation of the Python logic from yut.rule / yut.engine
// ============================================================

open System

// ──────────────────────────────────────────────────────────────
// 1. Rules (yut.rule)
// ──────────────────────────────────────────────────────────────

let FINISHED = 30
let N_MALS   = 4

/// Yut score -> name
let yutscoreName (s: int) =
    match s with
    | 1  -> "Do"
    | 2  -> "Gae"
    | 3  -> "Geol"
    | 4  -> "Yut"
    | 5  -> "Mo"
    | -1 -> "Back Do"
    | _  -> "?"

/// Extra throw for Yut/Mo
let needsThrowAgain (s: int) = s >= 4

/// Game ends when all pieces reach FINISHED
let gameFinished (pos: int[]) = Array.min pos = FINISHED

/// Default next-position map
let private defaultNext =
    dict [
        0,1;  1,2;  2,3;  3,4;  4,5;  5,6;  6,7;  7,8;  8,9;  9,10
        10,18; 11,12; 12,15; 13,14; 14,15; 15,16; 16,17; 17,22
        18,19; 19,20; 20,21; 21,22; 22,25; 23,24; 24,29
        25,26; 26,27; 27,28; 28,29; 29,30; 30,30
    ]

/// Recursively compute the next position (prev=-1 means starting point)
let rec private nextPositionRec (prev: int) (cur: int) (yutscore: int) (shortcut: bool) : int =
    if yutscore = 0 then cur
    elif yutscore = -1 then
        // Back Do
        if cur = FINISHED then FINISHED
        elif cur = 0 then 0
        elif cur = 1 then 29
        elif cur = 15 then (if shortcut then 14 else 12)
        elif cur = 22 then (if shortcut then 17 else 21)
        elif cur = 29 then (if shortcut then 24 else 28)
        elif cur = 23 then 15
        elif cur = 11 then 10
        elif cur = 13 then 5
        else
            // Reverse lookup of defaultNext
            defaultNext
            |> Seq.filter (fun kv -> kv.Value = cur)
            |> Seq.map (fun kv -> kv.Key)
            |> Seq.sort
            |> Seq.last
    else
        // Shortcut entry conditions
        if cur = 15 && (prev = -1 || prev = 12) then
            nextPositionRec cur 23 (yutscore-1) shortcut
        elif cur = 10 && prev = -1 then
            nextPositionRec cur 11 (yutscore-1) shortcut
        elif cur = 5 && prev = -1 then
            nextPositionRec cur 13 (yutscore-1) shortcut
        else
            let nxt = if defaultNext.ContainsKey(cur) then defaultNext.[cur] else 30
            nextPositionRec cur nxt (yutscore-1) shortcut

/// Public API
let nextPosition (curPos: int) (yutscore: int) (shortcut: bool) : int =
    nextPositionRec -1 curPos yutscore shortcut

/// Probability table (order: do, gae, geol, yut, mo, backdo)
let private yutscoreProbs =
    // binom(4, 0.6): pmf(1)=0.1536, pmf(2)=0.3456, pmf(3)=0.3456, pmf(4)=0.1296, pmf(0)=0.0256
    // do(1)   : 0.1536 * 0.75 = 0.1152
    // backdo(-1): 0.1536 * 0.25 = 0.0384
    // gae(2)  : 0.3456
    // geol(3) : 0.3456
    // yut(4)  : 0.1296
    // mo(5)   : 0.0256
    [| (1, 0.1152); (2, 0.3456); (3, 0.3456); (4, 0.1296); (5, 0.0256); (-1, 0.0384) |]

let private rng = Random()

/// Throw one Yut result according to probability
let private throwOnce () : int =
    let r = rng.NextDouble()
    let mutable acc = 0.0
    let mutable result = 1
    for (score, prob) in yutscoreProbs do
        if r >= acc && r < acc + prob then result <- score
        acc <- acc + prob
    result

/// Throw Yut sticks (Yut/Mo grants an extra throw)
let randomCast () : int list =
    let mutable outcome = [ throwOnce() ]
    while needsThrowAgain (List.last outcome) do
        outcome <- outcome @ [ throwOnce() ]
    outcome

/// Move a piece
/// Returns: (legal, myPositions, enemyPositions, numMalsCaught)
let makeMove (myPos: int[]) (enemyPos: int[]) (malToMove: int) (yutscore: int) (shortcut: bool)
    : bool * int[] * int[] * int =

    let curp = myPos.[malToMove]

    // Attempting to move a finished piece
    if curp = FINISHED then
        false, myPos, enemyPos, 0

    // Back Do from the start while another piece is already in play
    elif curp = 0 && yutscore = -1 && Array.exists (fun p -> p > 0 && p < FINISHED) myPos then
        false, myPos, enemyPos, 0

    else
        let nextp = nextPosition curp yutscore shortcut

        // Move all stacked pieces together (except at the start position)
        let malsToMove =
            if curp = 0 then [| malToMove |]
            else [| 0..N_MALS-1 |] |> Array.filter (fun i -> myPos.[i] = curp)

        if nextp = FINISHED || nextp = 0 then
            let newMyPos = Array.mapi (fun i p -> if Array.contains i malsToMove then nextp else p) myPos
            true, newMyPos, enemyPos, 0
        else
            let caught = Array.filter (fun ep -> ep = nextp) enemyPos |> Array.length
            let newMyPos = Array.mapi (fun i p -> if Array.contains i malsToMove then nextp else p) myPos
            let newEnemyPos = Array.map (fun ep -> if ep = nextp then 0 else ep) enemyPos
            true, newMyPos, newEnemyPos, caught

// ──────────────────────────────────────────────────────────────
// 2. Board Display
// ──────────────────────────────────────────────────────────────

// Board grid (same as Python's GRID, -1 means empty space)
let private GRID =
    [|
        [| 10;  9;  8; -1;  7;  6;  5 |]
        [| 18; 11; -1; -1; -1; 13;  4 |]
        [| 19; -1; 12; -1; 14; -1;  3 |]
        [| -1; -1; -1; 15; -1; -1; -1 |]
        [| 20; -1; 16; -1; 23; -1;  2 |]
        [| 21; 17; -1; -1; -1; 24;  1 |]
        [| 22; 25; 26; -1; 27; 28; 29 |]
    |]

let printBoard (p1Pos: int[]) (p2Pos: int[]) =
    printfn ""
    printfn "  ┌─────────────────────────────────────────────────┐"
    printfn "  │                 Yutnori Board                   │"
    printfn "  └─────────────────────────────────────────────────┘"

    for row in GRID do
        printf "  "
        for pos in row do
            if pos = -1 then
                printf "       "
            else
                let myMals =
                    [| 0..N_MALS-1 |]
                    |> Array.filter (fun i -> p1Pos.[i] = pos)
                    |> Array.map (fun i -> sprintf "a%d" (i+1))
                    |> String.concat ""

                let enmMals =
                    [| 0..N_MALS-1 |]
                    |> Array.filter (fun i -> p2Pos.[i] = pos)
                    |> Array.map (fun i -> sprintf "A%d" (i+1))
                    |> String.concat ""

                let label = myMals + enmMals
                printf "[%2d:%-3s]" pos label

        printfn ""

    printfn ""

    // Start / finish information
    let myAt0 =
        [| 0..N_MALS-1 |]
        |> Array.filter (fun i -> p1Pos.[i] = 0)
        |> Array.length

    let myFin =
        [| 0..N_MALS-1 |]
        |> Array.filter (fun i -> p1Pos.[i] = FINISHED)
        |> Array.length

    let enAt0 =
        [| 0..N_MALS-1 |]
        |> Array.filter (fun i -> p2Pos.[i] = 0)
        |> Array.length

    let enFin =
        [| 0..N_MALS-1 |]
        |> Array.filter (fun i -> p2Pos.[i] = FINISHED)
        |> Array.length

    printfn
        "  ▶ Player (a)   : Waiting=%d  Finished=%d  Positions=%s"
        myAt0
        myFin
        (p1Pos
         |> Array.mapi (fun i p -> sprintf "a%d@%d" (i+1) p)
         |> String.concat " ")

    printfn
        "  ▶ Computer (A) : Waiting=%d  Finished=%d  Positions=%s"
        enAt0
        enFin
        (p2Pos
         |> Array.mapi (fun i p -> sprintf "A%d@%d" (i+1) p)
         |> String.concat " ")

    printfn ""

// ──────────────────────────────────────────────────────────────
// 3. Computer AI (Greedy Strategy)
// ──────────────────────────────────────────────────────────────

/// Determines the computer's action
/// Priority:
/// 1) Finish a piece
/// 2) Capture opponent pieces
/// 3) Use shortcuts
/// 4) Move the most advanced piece
let computerAction (myPos: int[]) (enemyPos: int[]) (availableScores: int list) : int * int * bool =
    let tryScore score =
        // List of movable pieces
        let candidateMals =
            if score = -1 then
                // Backdo: only pieces currently on the board
                let running =
                    [| 0..N_MALS-1 |]
                    |> Array.filter (fun i -> myPos.[i] > 0 && myPos.[i] < FINISHED)

                if running.Length = 0 then
                    [| 0..N_MALS-1 |]
                    |> Array.filter (fun i -> myPos.[i] < FINISHED)
                else
                    running
            else
                [| 0..N_MALS-1 |]
                |> Array.filter (fun i -> myPos.[i] < FINISHED)

        if candidateMals.Length = 0 then None
        else

        // Try each piece with and without shortcut
        let options =
            candidateMals
            |> Array.collect (fun mal -> [| (mal, true); (mal, false) |])
            |> Array.choose (fun (mal, sc) ->
                let legal, newMy, newEn, caught =
                    makeMove myPos enemyPos mal score sc

                if legal then
                    Some (mal, sc, newMy, newEn, caught)
                else
                    None)

        if options.Length = 0 then None
        else

        // Priority 1: maximize the number of finished pieces
        let finishCount (pos: int[]) =
            Array.filter (fun p -> p = FINISHED) pos |> Array.length

        let best =
            options
            |> Array.sortWith (fun (_, _, m1, _, c1) (_, _, m2, _, c2) ->
                let f1 = finishCount m1 - finishCount myPos
                let f2 = finishCount m2 - finishCount myPos

                if f1 <> f2 then
                    compare f2 f1   // More finished pieces is better
                elif c1 <> c2 then
                    compare c2 c1   // More captures is better
                else
                    // Maximize forward progress (furthest piece position)
                    let lead1 = Array.max m1
                    let lead2 = Array.max m2
                    compare lead2 lead1)

        let (mal, sc, _, _, _) = best.[0]
        Some (mal, score, sc)

    // Select the first valid move among available scores
    availableScores
    |> List.tryPick tryScore
    |> Option.defaultWith (fun () ->
        // Fallback: first movable piece, first score, shortcut enabled
        let firstScore = List.head availableScores

        let firstMal =
            [| 0..N_MALS-1 |]
            |> Array.tryFind (fun i -> myPos.[i] < FINISHED)
            |> Option.defaultValue 0

        firstMal, firstScore, true)

// ──────────────────────────────────────────────────────────────
// 4. User Input Handling
// ──────────────────────────────────────────────────────────────

let readInt (prompt: string) (minVal: int) (maxVal: int) =
    let mutable result = minVal - 1
    while result < minVal || result > maxVal do
        printf "%s" prompt
        let line = Console.ReadLine()

        match Int32.TryParse(line) with
        | true, v when v >= minVal && v <= maxVal -> result <- v
        | _ ->
            printfn
                "  ⚠ Please enter a number between %d and %d."
                minVal
                maxVal

    result

/// Selects the player's action
let userAction (myPos: int[]) (enemyPos: int[]) (availableScores: int list) : int * int * bool =
    printfn "  Available Yut scores:"

    availableScores
    |> List.iteri (fun i s ->
        printfn "    [%d] %s (%d)" (i + 1) (yutscoreName s) s)

    // Select a Yut score
    let chosenScore =
        if availableScores.Length = 1 then
            let s = availableScores.[0]
            printfn "  Selected Yut score: %s (%d)" (yutscoreName s) s
            s
        else
            let scoreIdx =
                readInt
                    (sprintf "  Select a Yut score (1~%d): " availableScores.Length)
                    1
                    availableScores.Length

            availableScores.[scoreIdx - 1]

    // List of movable pieces
    let candidateMals =
        if chosenScore = -1 then
            let running =
                [| 0 .. N_MALS - 1 |]
                |> Array.filter (fun i -> myPos.[i] > 0 && myPos.[i] < FINISHED)

            if running.Length = 0 then
                [| 0 .. N_MALS - 1 |]
                |> Array.filter (fun i -> myPos.[i] < FINISHED)
            else
                running
        else
            [| 0 .. N_MALS - 1 |]
            |> Array.filter (fun i -> myPos.[i] < FINISHED)

    if candidateMals.Length = 0 then
        printfn "  No movable pieces available."
        0, chosenScore, true
    else
        printfn "  Select a piece to move:"

        candidateMals
        |> Array.iteri (fun i malIdx ->
            let pos = myPos.[malIdx]

            printfn
                "    [%d] Piece %d  (Current: %d → Next: %d)"
                (i + 1)
                (malIdx + 1)
                pos
                (nextPosition pos chosenScore true))

        let chosenMal =
            if candidateMals.Length = 1 then
                let m = candidateMals.[0]
                printfn "  Selected piece: %d" (m + 1)
                m
            else
                let malChoiceIdx =
                    readInt
                        (sprintf "  Select a piece (1~%d): " candidateMals.Length)
                        1
                        candidateMals.Length

                candidateMals.[malChoiceIdx - 1]

        let curPos = myPos.[chosenMal]

        // Ask about shortcut usage only when applicable
        let shortcutPositions = [5; 10; 15; 22; 29]

        let useShortcut =
            if chosenScore = -1 && List.contains curPos [15; 22; 29] then
                printfn "  Use the shortcut? (y/n)"
                let ans = Console.ReadLine().Trim().ToLower()
                ans = "y" || ans = "yes"

            elif chosenScore > 0 && List.contains curPos shortcutPositions then
                let withSC = nextPosition curPos chosenScore true
                let withoutSC = nextPosition curPos chosenScore false

                if withSC <> withoutSC then
                    printfn
                        "  Use the shortcut? (Position %d → Shortcut:%d / Normal:%d) (y/n)"
                        curPos
                        withSC
                        withoutSC

                    let ans = Console.ReadLine().Trim().ToLower()
                    ans = "y" || ans = "yes"
                else
                    true
            else
                true

        chosenMal, chosenScore, useShortcut

// ──────────────────────────────────────────────────────────────
// 5. Game Loop
// ──────────────────────────────────────────────────────────────

let printTitle () =
    printfn ""
    printfn "  ╔══════════════════════════════════════╗"
    printfn "  ║        🎲  F# Console Yutnori       ║"
    printfn "  ║      Player(a) vs Computer(A)       ║"
    printfn "  ╚══════════════════════════════════════╝"
    printfn ""

let printTurnBanner (turn: int) (isUser: bool) =
    printfn "  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    printfn "  Turn #%d  ―  %s's turn" turn (if isUser then "👤 Player" else "🖥 Computer")
    printfn "  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

let runGame () =
    printTitle ()

    let mutable userPos     = Array.create N_MALS 0
    let mutable computerPos = Array.create N_MALS 0
    let mutable turn = 0
    let mutable winner = -1   // -1: game in progress, 0: player wins, 1: computer wins

    while winner = -1 do
        let isUserTurn = (turn % 2 = 0)
        printTurnBanner turn isUserTurn

        // Display board
        printBoard userPos computerPos

        // Current player's perspective
        let myPos, enemyPos =
            if isUserTurn then userPos, computerPos
            else computerPos, userPos

        // Throw Yut sticks
        printf "  🎲 Throwing Yut... [Press Enter]"
        if not isUserTurn then
            System.Threading.Thread.Sleep(600)
        else
            Console.ReadLine() |> ignore
        printfn ""

        let mutable castOutcome = randomCast()
        let mutable availableScores = castOutcome
        printfn "  Result: %s" (castOutcome |> List.map yutscoreName |> String.concat " + ")

        let mutable myCurPos    = Array.copy myPos
        let mutable enemyCurPos = Array.copy enemyPos
        let mutable extraTurn   = false

        while availableScores <> [] do

            // Select action
            let (malIdx, scoreUsed, shortcut) =
                if isUserTurn then
                    userAction myCurPos enemyCurPos availableScores
                else
                    let (m, s, sc) =
                        computerAction myCurPos enemyCurPos availableScores

                    printfn
                        "  🖥 Computer: Move piece %d using '%s' (shortcut: %b)"
                        (m + 1)
                        (yutscoreName s)
                        sc

                    System.Threading.Thread.Sleep(800)
                    m, s, sc

            let (legal, newMy, newEnemy, caught) =
                makeMove myCurPos enemyCurPos malIdx scoreUsed shortcut

            if not legal then
                printfn "  ❌ Illegal move! The opponent wins."
                winner <- if isUserTurn then 1 else 0
                availableScores <- []

            else
                myCurPos <- newMy
                enemyCurPos <- newEnemy

                if caught > 0 then
                    printfn
                        "  💥 Captured %d opponent piece(s)! Throw Yut again."
                        caught

                availableScores <-
                    List.filter (fun s -> s <> scoreUsed) availableScores

                // Capture bonus (excluding Yut/Mo)
                if caught > 0 && not (needsThrowAgain scoreUsed) then
                    printfn "  🎲 Bonus Yut throw!"

                    if not isUserTurn then
                        System.Threading.Thread.Sleep(600)

                    let bonus = randomCast()

                    printfn
                        "  Bonus result: %s"
                        (bonus |> List.map yutscoreName |> String.concat " + ")

                    availableScores <- availableScores @ bonus

                // Check victory condition
                if gameFinished myCurPos then
                    winner <- if isUserTurn then 0 else 1
                    availableScores <- []

        // Update positions
        if isUserTurn then
            userPos <- myCurPos
            computerPos <- enemyCurPos
        else
            computerPos <- myCurPos
            userPos <- enemyCurPos

        turn <- turn + 1
        printfn ""

    // Game over
    printBoard userPos computerPos
    printfn ""

    if winner = 0 then
        printfn "  🎉🎉 Player Wins! Congratulations! 🎉🎉"
    else
        printfn "  🖥 Computer Wins! Better luck next time."

    printfn ""


// ──────────────────────────────────────────────────────────────
// 6. Entry Point
// ──────────────────────────────────────────────────────────────

let mutable playAgain = true

while playAgain do
    runGame()

    printf "  Play again? (y/n): "
    let ans = Console.ReadLine().Trim().ToLower()

    playAgain <- ans = "y" || ans = "yes"

printfn "  Exiting game. Thank you for playing!"