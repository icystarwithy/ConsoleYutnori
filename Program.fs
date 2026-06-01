// ============================================================
//  F# 콘솔 윷놀이 게임
//  - 사용자 vs 컴퓨터
//  - yut.rule / yut.engine 의 Python 로직을 F#으로 재구현
// ============================================================

open System

// ──────────────────────────────────────────────────────────────
// 1. 규칙 (yut.rule)
// ──────────────────────────────────────────────────────────────

let FINISHED = 30
let N_MALS   = 4

/// 윷 점수 → 이름
let yutscoreName (s: int) =
    match s with
    | 1  -> "도(Do)"
    | 2  -> "개(Gae)"
    | 3  -> "걸(Geol)"
    | 4  -> "윷(Yut)"
    | 5  -> "모(Mo)"
    | -1 -> "백도(Backdo)"
    | _  -> "?"

/// 윷/모이면 한 번 더 던짐
let needsThrowAgain (s: int) = s >= 4

/// 모든 말이 FINISHED 이면 게임 종료
let gameFinished (pos: int[]) = Array.min pos = FINISHED

/// 기본 다음 위치 맵
let private defaultNext =
    dict [
        0,1;  1,2;  2,3;  3,4;  4,5;  5,6;  6,7;  7,8;  8,9;  9,10
        10,18; 11,12; 12,15; 13,14; 14,15; 15,16; 16,17; 17,22
        18,19; 19,20; 20,21; 21,22; 22,25; 23,24; 24,29
        25,26; 26,27; 27,28; 28,29; 29,30; 30,30
    ]

/// 재귀적으로 다음 위치 계산 (prev=-1 이면 시작점)
let rec private nextPositionRec (prev: int) (cur: int) (yutscore: int) (shortcut: bool) : int =
    if yutscore = 0 then cur
    elif yutscore = -1 then
        // 백도
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
            // defaultNext 역방향
            defaultNext
            |> Seq.filter (fun kv -> kv.Value = cur)
            |> Seq.map (fun kv -> kv.Key)
            |> Seq.sort
            |> Seq.last
    else
        // 지름길 진입 조건
        if cur = 15 && (prev = -1 || prev = 12) then
            nextPositionRec cur 23 (yutscore-1) shortcut
        elif cur = 10 && prev = -1 then
            nextPositionRec cur 11 (yutscore-1) shortcut
        elif cur = 5 && prev = -1 then
            nextPositionRec cur 13 (yutscore-1) shortcut
        else
            let nxt = if defaultNext.ContainsKey(cur) then defaultNext.[cur] else 30
            nextPositionRec cur nxt (yutscore-1) shortcut

/// 공개 API
let nextPosition (curPos: int) (yutscore: int) (shortcut: bool) : int =
    nextPositionRec -1 curPos yutscore shortcut

/// 확률 테이블 (인덱스 순서: do, gae, geol, yut, mo, backdo)
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

/// 확률에 따라 윷 하나를 던짐
let private throwOnce () : int =
    let r = rng.NextDouble()
    let mutable acc = 0.0
    let mutable result = 1
    for (score, prob) in yutscoreProbs do
        if r >= acc && r < acc + prob then result <- score
        acc <- acc + prob
    result

/// 윷 던지기 (윷/모이면 한 번 더)
let randomCast () : int list =
    let mutable outcome = [ throwOnce() ]
    while needsThrowAgain (List.last outcome) do
        outcome <- outcome @ [ throwOnce() ]
    outcome

/// 이동 처리
/// 반환: (legal, myPositions, enemyPositions, numMalsCaught)
let makeMove (myPos: int[]) (enemyPos: int[]) (malToMove: int) (yutscore: int) (shortcut: bool)
    : bool * int[] * int[] * int =

    let curp = myPos.[malToMove]

    // 이미 완주한 말을 움직이려 할 때
    if curp = FINISHED then
        false, myPos, enemyPos, 0
    // 백도인데 출발점(0)에 있고 진행 중인 말이 있을 때
    elif curp = 0 && yutscore = -1 && Array.exists (fun p -> p > 0 && p < FINISHED) myPos then
        false, myPos, enemyPos, 0
    else
        let nextp = nextPosition curp yutscore shortcut

        // 같은 위치의 말들을 함께 이동 (출발점(0)은 혼자만)
        let malsToMove =
            if curp = 0 then [| malToMove |]
            else [| 0..N_MALS-1 |] |> Array.filter (fun i -> myPos.[i] = curp)

        if nextp = FINISHED || nextp = 0 then
            let newMyPos = Array.mapi (fun i p -> if Array.contains i malsToMove then nextp else p) myPos
            true, newMyPos, enemyPos, 0
        else
            let caught = Array.filter (fun ep -> ep = nextp) enemyPos |> Array.length
            let newMyPos   = Array.mapi (fun i p -> if Array.contains i malsToMove then nextp else p) myPos
            let newEnemyPos = Array.map (fun ep -> if ep = nextp then 0 else ep) enemyPos
            true, newMyPos, newEnemyPos, caught


// ──────────────────────────────────────────────────────────────
// 2. 보드 출력
// ──────────────────────────────────────────────────────────────

// 보드 그리드 (Python의 GRID와 동일, -1은 빈 칸)
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
    printfn "  │              윷놀이 판                          │"
    printfn "  └─────────────────────────────────────────────────┘"
    for row in GRID do
        printf "  "
        for pos in row do
            if pos = -1 then
                printf "       "
            else
                let myMals   = [| 0..N_MALS-1 |] |> Array.filter (fun i -> p1Pos.[i] = pos) |> Array.map (fun i -> sprintf "a%d" (i+1)) |> String.concat ""
                let enmMals  = [| 0..N_MALS-1 |] |> Array.filter (fun i -> p2Pos.[i] = pos) |> Array.map (fun i -> sprintf "A%d" (i+1)) |> String.concat ""
                let label = myMals + enmMals
                printf "[%2d:%-3s]" pos label
        printfn ""
    printfn ""
    // 출발/도착 정보
    let myAt0  = [| 0..N_MALS-1 |] |> Array.filter (fun i -> p1Pos.[i] = 0)  |> Array.length
    let myFin  = [| 0..N_MALS-1 |] |> Array.filter (fun i -> p1Pos.[i] = FINISHED) |> Array.length
    let enAt0  = [| 0..N_MALS-1 |] |> Array.filter (fun i -> p2Pos.[i] = 0)  |> Array.length
    let enFin  = [| 0..N_MALS-1 |] |> Array.filter (fun i -> p2Pos.[i] = FINISHED) |> Array.length
    printfn "  ▶ 나(a)   : 출발대기=%d  완주=%d  진행중=%s" myAt0 myFin (p1Pos |> Array.mapi (fun i p -> sprintf "a%d@%d" (i+1) p) |> String.concat " ")
    printfn "  ▶ 컴퓨터(A): 출발대기=%d  완주=%d  진행중=%s" enAt0 enFin (p2Pos |> Array.mapi (fun i p -> sprintf "A%d@%d" (i+1) p) |> String.concat " ")
    printfn ""


// ──────────────────────────────────────────────────────────────
// 3. 컴퓨터 AI (그리디 전략)
// ──────────────────────────────────────────────────────────────

/// 컴퓨터 행동 결정
/// 우선순위: 1) 완주 2) 잡기 3) 지름길 활용 4) 가장 뒤처진 말 이동
let computerAction (myPos: int[]) (enemyPos: int[]) (availableScores: int list) : int * int * bool =
    let tryScore score =
        // 유효한 말 목록
        let candidateMals =
            if score = -1 then
                // 백도: 진행 중인 말만
                let running = [| 0..N_MALS-1 |] |> Array.filter (fun i -> myPos.[i] > 0 && myPos.[i] < FINISHED)
                if running.Length = 0 then
                    [| 0..N_MALS-1 |] |> Array.filter (fun i -> myPos.[i] < FINISHED)
                else running
            else
                [| 0..N_MALS-1 |] |> Array.filter (fun i -> myPos.[i] < FINISHED)

        if candidateMals.Length = 0 then None
        else
        // 각 말에 대해 shortcut true/false 시도
        let options =
            candidateMals
            |> Array.collect (fun mal -> [| (mal, true); (mal, false) |])
            |> Array.choose (fun (mal, sc) ->
                let legal, newMy, newEn, caught = makeMove myPos enemyPos mal score sc
                if legal then Some (mal, sc, newMy, newEn, caught) else None)

        if options.Length = 0 then None
        else
        // 1순위: 완주 수 최대화
        let finishCount (pos: int[]) = Array.filter (fun p -> p = FINISHED) pos |> Array.length
        let best =
            options
            |> Array.sortWith (fun (_, _, m1, e1, c1) (_, _, m2, e2, c2) ->
                let f1 = finishCount m1 - finishCount myPos
                let f2 = finishCount m2 - finishCount myPos
                if f1 <> f2 then compare f2 f1   // 완주 많은 게 좋음
                else if c1 <> c2 then compare c2 c1  // 잡기 많은 게 좋음
                else
                    // 전진 거리 최대화 (가장 앞선 내 말 위치 기준)
                    let lead1 = Array.max m1
                    let lead2 = Array.max m2
                    compare lead2 lead1)
        let (mal, sc, _, _, _) = best.[0]
        Some (mal, score, sc)

    // availableScores 중 첫 번째로 유효한 것 선택
    availableScores
    |> List.tryPick tryScore
    |> Option.defaultWith (fun () ->
        // fallback: 첫 번째 말, 첫 번째 점수, shortcut=true
        let firstScore = List.head availableScores
        let firstMal   = [| 0..N_MALS-1 |] |> Array.tryFind (fun i -> myPos.[i] < FINISHED) |> Option.defaultValue 0
        firstMal, firstScore, true)


// ──────────────────────────────────────────────────────────────
// 4. 사용자 입력 처리
// ──────────────────────────────────────────────────────────────

let readInt (prompt: string) (minVal: int) (maxVal: int) =
    let mutable result = minVal - 1
    while result < minVal || result > maxVal do
        printf "%s" prompt
        let line = Console.ReadLine()
        match Int32.TryParse(line) with
        | true, v when v >= minVal && v <= maxVal -> result <- v
        | _ -> printfn "  ⚠ %d ~ %d 사이의 숫자를 입력하세요." minVal maxVal
    result

/// 사용자 행동 선택
let userAction (myPos: int[]) (enemyPos: int[]) (availableScores: int list) : int * int * bool =
    printfn "  사용 가능한 윷 점수:"
    availableScores |> List.iteri (fun i s -> printfn "    [%d] %s (%d)" (i+1) (yutscoreName s) s)

    // 윷 점수 선택
    let chosenScore =
        if availableScores.Length = 1 then
            let s = availableScores.[0]
            printfn "  사용할 윷 점수: %s (%d)" (yutscoreName s) s
            s
        else
            let scoreIdx =
                readInt
                    (sprintf "  사용할 윷 점수 번호 (1~%d): " availableScores.Length)
                    1
                    availableScores.Length

            availableScores.[scoreIdx - 1]

    // 이동 가능한 말 목록
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
        printfn "  이동 가능한 말이 없습니다."
        0, chosenScore, true
    else
        printfn "  이동할 말:"

        candidateMals
        |> Array.iteri (fun i malIdx ->
            let pos = myPos.[malIdx]
            printfn "    [%d] 말 %d  (현재 위치: %d → 다음 위치: %d)"
                (i + 1)
                (malIdx + 1)
                pos
                (nextPosition pos chosenScore true))

        let chosenMal =
            if candidateMals.Length = 1 then
                let m = candidateMals.[0]
                printfn "  이동할 말: %d" (m + 1)
                m
            else
                let malChoiceIdx =
                    readInt
                        (sprintf "  이동할 말 번호 (1~%d): " candidateMals.Length)
                        1
                        candidateMals.Length

                candidateMals.[malChoiceIdx - 1]

        let curPos = myPos.[chosenMal]

        // 지름길 선택 여부 (해당하는 경우만 물어봄)
        let shortcutPositions = [5; 10; 15; 22; 29]

        let useShortcut =
            if chosenScore = -1 && List.contains curPos [15; 22; 29] then
                printfn "  지름길을 사용하시겠습니까? (y/n)"
                let ans = Console.ReadLine().Trim().ToLower()
                ans = "y" || ans = "yes"

            elif chosenScore > 0 && List.contains curPos shortcutPositions then
                let withSC = nextPosition curPos chosenScore true
                let withoutSC = nextPosition curPos chosenScore false

                if withSC <> withoutSC then
                    printfn
                        "  지름길을 사용하시겠습니까? (위치 %d → 지름길:%d / 일반:%d) (y/n)"
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
// 5. 게임 루프
// ──────────────────────────────────────────────────────────────

let printTitle () =
    printfn ""
    printfn "  ╔══════════════════════════════════════╗"
    printfn "  ║       🎲  F# 콘솔 윷놀이 게임       ║"
    printfn "  ║   사용자(a) vs 컴퓨터(A)             ║"
    printfn "  ╚══════════════════════════════════════╝"
    printfn ""

let printTurnBanner (turn: int) (isUser: bool) =
    printfn "  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    printfn "  턴 #%d  ―  %s의 차례" turn (if isUser then "👤 사용자" else "🖥  컴퓨터")
    printfn "  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

let runGame () =
    printTitle ()

    let mutable userPos     = Array.create N_MALS 0
    let mutable computerPos = Array.create N_MALS 0
    let mutable turn = 0
    let mutable winner = -1   // -1: 진행 중, 0: 사용자 승, 1: 컴퓨터 승

    while winner = -1 do
        let isUserTurn = (turn % 2 = 0)
        printTurnBanner turn isUserTurn

        // 보드 출력
        printBoard userPos computerPos

        // 현재 플레이어 관점
        let myPos, enemyPos =
            if isUserTurn then userPos, computerPos
            else computerPos, userPos

        // 윷 던지기
        printf "  🎲 윷을 던집니다... [Press Enter]"
        if not isUserTurn then
            System.Threading.Thread.Sleep(600)
        else
            Console.ReadLine() |> ignore  // 엔터 대기
        printfn ""

        let mutable castOutcome = randomCast()
        let mutable availableScores = castOutcome
        printfn "  결과: %s" (castOutcome |> List.map yutscoreName |> String.concat " + ")

        let mutable myCurPos    = Array.copy myPos
        let mutable enemyCurPos = Array.copy enemyPos
        let mutable extraTurn   = false

        while availableScores <> [] do
            // 행동 선택
            let (malIdx, scoreUsed, shortcut) =
                if isUserTurn then
                    userAction myCurPos enemyCurPos availableScores
                else
                    let (m, s, sc) = computerAction myCurPos enemyCurPos availableScores
                    printfn "  🖥  컴퓨터: 말 %d을(를) '%s'로 이동 (지름길: %b)" (m+1) (yutscoreName s) sc
                    System.Threading.Thread.Sleep(800)
                    m, s, sc

            let (legal, newMy, newEnemy, caught) =
                makeMove myCurPos enemyCurPos malIdx scoreUsed shortcut

            if not legal then
                printfn "  ❌ 불법 이동! 상대방이 승리합니다."
                winner <- if isUserTurn then 1 else 0
                availableScores <- []
            else
                myCurPos    <- newMy
                enemyCurPos <- newEnemy

                if caught > 0 then
                    printfn "  💥 %d개의 상대 말을 잡았습니다! 윷을 한 번 더 던집니다." caught

                availableScores <- List.filter (fun s -> s <> scoreUsed) availableScores

                // 잡기 보너스 (윷/모 제외)
                if caught > 0 && not (needsThrowAgain scoreUsed) then
                    printfn "  🎲 보너스 윷 던지기!"
                    if not isUserTurn then System.Threading.Thread.Sleep(600)
                    let bonus = randomCast()
                    printfn "  보너스 결과: %s" (bonus |> List.map yutscoreName |> String.concat " + ")
                    availableScores <- availableScores @ bonus

                // 완주 확인
                if gameFinished myCurPos then
                    winner <- if isUserTurn then 0 else 1
                    availableScores <- []

        // 포지션 업데이트
        if isUserTurn then
            userPos     <- myCurPos
            computerPos <- enemyCurPos
        else
            computerPos <- myCurPos
            userPos     <- enemyCurPos

        turn <- turn + 1
        printfn ""

    // 게임 종료
    printBoard userPos computerPos
    printfn ""
    if winner = 0 then
        printfn "  🎉🎉  사용자 승리! 축하합니다! 🎉🎉"
    else
        printfn "  🖥  컴퓨터 승리! 다음에 다시 도전하세요."
    printfn ""


// ──────────────────────────────────────────────────────────────
// 6. 진입점
// ──────────────────────────────────────────────────────────────

let mutable playAgain = true
while playAgain do
    runGame()
    printf "  다시 플레이하시겠습니까? (y/n): "
    let ans = Console.ReadLine().Trim().ToLower()
    playAgain <- ans = "y" || ans = "yes"

printfn "  게임을 종료합니다. 감사합니다!"