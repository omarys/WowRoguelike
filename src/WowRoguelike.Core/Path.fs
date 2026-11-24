namespace WowRoguelike.Core

/// Pathfinding over the Grid, with no knowledge of Entities.
///
/// The passability test is passed in rather than read from a World, so this
/// module depends only on Domain and Heap. That keeps the search itself free of
/// game rules and makes it directly testable.
///
/// Costs are integers in tenths of a tile: 10 orthogonal, 14 diagonal. No
/// floating point, per ADR-0002, so the heuristic is admissible exactly rather
/// than approximately.
module Path =

  [<Literal>]
  let private Straight = 10

  [<Literal>]
  let private Diagonal = 14

  /// Octile distance: admissible for 8-way movement at these costs. The standard
  /// form is `D*(dx+dy) + (D2-2D)*min`; since `dx+dy = max + min`, that reduces
  /// to `D*max + (2D - (2D - D2))*min` — i.e. a min-coefficient of `D2 - D`, which
  /// is 4 for 10/14 and keeps the whole thing in integers.
  let octile (a: Pos) (b: Pos) =
    let dx = abs (a.X - b.X)
    let dy = abs (a.Y - b.Y)
    Straight * max dx dy + (Diagonal - Straight) * min dx dy

  let private stepCost (a: Pos) (b: Pos) =
    if a.X <> b.X && a.Y <> b.Y then Diagonal else Straight

  /// 8-way neighbours, without cutting corners: a diagonal move requires both of
  /// its orthogonal neighbours to be open, so entities cannot slip between two
  /// diagonally adjacent pillars.
  let neighbours (passable: Pos -> bool) (p: Pos) =
    [ for dy in -1..1 do
        for dx in -1..1 do
          if dx <> 0 || dy <> 0 then
            let n = Pos.move dx dy p

            if
              passable n
              && (dx = 0
                  || dy = 0
                  || (passable (Pos.move dx 0 p) && passable (Pos.move 0 dy p)))
            then
              yield n ]

  type Result =
    { /// Tiles from just after the start through the goal. Empty if unreachable.
      Path: Pos list
      /// Nodes pulled off the open list. For A* versus Dijkstra this is the
      /// whole point: the heuristic's value is measured here and nowhere else.
      /// For `greedy` it counts steps taken, since there is no open list.
      Expanded: int
      /// True when the search hit `nodeBudget` before reaching the goal, so
      /// `Path` is a best-effort route to the closest tile it managed to look
      /// at rather than the shortest one.
      Truncated: bool }

  /// Walk the cameFrom chain back to the start. Returns the tiles *after* the
  /// start through the goal, which is what a mover wants: it is already standing
  /// on the first tile.
  /// Walk the flat cameFrom array back from the goal, returning the tiles after
  /// the start through the goal. A cameFrom of -1 marks the start.
  let private walkBack (cameFrom: int[]) (width: int) (goalIndex: int) =
    if goalIndex < 0 then
      []
    else
      let mutable acc = []
      let mutable current = goalIndex

      while cameFrom.[current] >= 0 do
        acc <- { X = current % width; Y = current / width } :: acc
        current <- cameFrom.[current]

      acc

  /// Ceiling on expansions per search. A route that does not exist, or one much
  /// longer than a room, otherwise expands the whole map — 14 ms measured — and
  /// every entity that cannot reach its goal retries that forever, which is what
  /// made a stalled dungeon run cost minutes of CPU. Past the ceiling the search
  /// returns the route to the closest tile it looked at, so a mover still makes
  /// progress towards a distant door instead of standing still.
  [<Literal>]
  let private nodeBudget = 1200

  /// Per-thread reusable scratch buffers for A*/Dijkstra, generation-stamped so
  /// each search starts in O(1) with zero array allocations.
  type private SearchScratch() =
    let mutable seenStamp = Array.zeroCreate<int> 0
    let mutable closedStamp = Array.zeroCreate<int> 0
    let mutable best = Array.zeroCreate<int> 0
    let mutable cameFrom = Array.zeroCreate<int> 0
    let mutable stamp = 0
    let openList = MinHeap<int, int>()

    member _.Acquire(size: int) =
      if seenStamp.Length < size then
        let cap = max size (max 256 (seenStamp.Length * 2))
        seenStamp <- Array.zeroCreate<int> cap
        closedStamp <- Array.zeroCreate<int> cap
        best <- Array.zeroCreate<int> cap
        cameFrom <- Array.zeroCreate<int> cap
        stamp <- 1
      elif stamp = System.Int32.MaxValue then
        System.Array.Clear(seenStamp, 0, seenStamp.Length)
        System.Array.Clear(closedStamp, 0, closedStamp.Length)
        stamp <- 1
      else
        stamp <- stamp + 1

      openList.Clear()
      stamp, seenStamp, closedStamp, best, cameFrom, openList

    [<System.ThreadStatic; DefaultValue>]
    static val mutable private instance: SearchScratch

    static member Get() =
      let s = SearchScratch.instance

      if obj.ReferenceEquals(s, null) then
        let fresh = SearchScratch()
        SearchScratch.instance <- fresh
        fresh
      else
        s

  let private getScratch () = SearchScratch.Get()

  let inline private octileXY (ax: int) (ay: int) (bx: int) (by: int) =
    let dx = abs (ax - bx)
    let dy = abs (ay - by)
    let hi = if dx >= dy then dx else dy
    let lo = if dx >= dy then dy else dx
    Straight * hi + (Diagonal - Straight) * lo

  /// The one search. A* and Dijkstra differ only in the heuristic, which is why
  /// they are the same function.
  ///
  /// State lives in generation-stamped flat arrays in a thread-local scratch
  /// buffer, avoiding O(W*H) allocation and initialization on every call.
  let private searchWith
    (budget: int)
    (useOctile: bool)
    (g: Grid)
    (blockedIdx: int -> int -> int -> bool)
    (start: Pos)
    (goal: Pos)
    : Result =
    if start = goal then
      { Path = []
        Expanded = 0
        Truncated = false }
    else
      let width = g.Width
      let height = g.Height
      let walls = g.Walls
      let goalX = goal.X
      let goalY = goal.Y
      let goalIndex = goalY * width + goalX
      let startIndex = start.Y * width + start.X

      let stamp, seenStamp, closedStamp, best, cameFrom, openList =
        (getScratch ()).Acquire(width * height)

      let startH = if useOctile then octileXY start.X start.Y goalX goalY else 0
      seenStamp.[startIndex] <- stamp
      best.[startIndex] <- 0
      cameFrom.[startIndex] <- -1
      openList.Push(startH, startIndex)

      let mutable expanded = 0
      let mutable found = false
      let mutable closest = startIndex
      let mutable closestH = startH
      let mutable poppedPriority = 0
      let mutable current = 0

      let inline isPassable (x: int) (y: int) (idx: int) =
        x >= 0
        && y >= 0
        && x < width
        && y < height
        && not walls.[idx]
        && ((x = goalX && y = goalY) || not (blockedIdx x y idx))

      let inline relax (nx: int) (ny: int) (nIndex: int) (gNext: int) (cur: int) =
        if closedStamp.[nIndex] <> stamp then
          if seenStamp.[nIndex] <> stamp || gNext < best.[nIndex] then
            seenStamp.[nIndex] <- stamp
            best.[nIndex] <- gNext
            cameFrom.[nIndex] <- cur
            let hNext = if useOctile then octileXY nx ny goalX goalY else 0
            openList.Push(gNext + hNext, nIndex)

      while not found && expanded < budget && openList.TryPop(&poppedPriority, &current) do
        if closedStamp.[current] = stamp then
          ()
        elif current = goalIndex then
          found <- true
        else
          closedStamp.[current] <- stamp
          expanded <- expanded + 1
          let cx = current % width
          let cy = current / width
          let gCurrent = best.[current]
          let h = if useOctile then octileXY cx cy goalX goalY else 0

          if h < closestH then
            closestH <- h
            closest <- current

          // Pre-check the 4 orthogonal neighbours once; diagonal moves require
          // both adjacent orthogonals to be passable (no corner-cutting).
          let nIdx = current - width
          let sIdx = current + width
          let wIdx = current - 1
          let eIdx = current + 1

          let okN = isPassable cx (cy - 1) nIdx
          let okS = isPassable cx (cy + 1) sIdx
          let okW = isPassable (cx - 1) cy wIdx
          let okE = isPassable (cx + 1) cy eIdx

          let gStraight = gCurrent + Straight
          let gDiag = gCurrent + Diagonal

          // Visit in exact row-major (-1..1, -1..1) order to preserve tie-breaking.
          if okW && okN then
            let idx = nIdx - 1
            if isPassable (cx - 1) (cy - 1) idx then
              relax (cx - 1) (cy - 1) idx gDiag current

          if okN then
            relax cx (cy - 1) nIdx gStraight current

          if okE && okN then
            let idx = nIdx + 1
            if isPassable (cx + 1) (cy - 1) idx then
              relax (cx + 1) (cy - 1) idx gDiag current

          if okW then
            relax (cx - 1) cy wIdx gStraight current

          if okE then
            relax (cx + 1) cy eIdx gStraight current

          if okW && okS then
            let idx = sIdx - 1
            if isPassable (cx - 1) (cy + 1) idx then
              relax (cx - 1) (cy + 1) idx gDiag current

          if okS then
            relax cx (cy + 1) sIdx gStraight current

          if okE && okS then
            let idx = sIdx + 1
            if isPassable (cx + 1) (cy + 1) idx then
              relax (cx + 1) (cy + 1) idx gDiag current

      // A drained open list means the goal is genuinely unreachable, and an
      // empty path is how every caller spells that. Only a search the budget cut
      // short is allowed to hand back a partial route.
      { Path = walkBack cameFrom width (if found then goalIndex elif expanded >= budget then closest else -1)
        Expanded = expanded
        Truncated = not found && expanded >= budget }

  /// One search at the fast budget, and one escalated retry when the fast budget
  /// cut it short **without** a best-effort route to hand back.
  let private search
    (useOctile: bool)
    (g: Grid)
    (blockedIdx: int -> int -> int -> bool)
    (start: Pos)
    (goal: Pos)
    : Result =
    let quick = searchWith nodeBudget useOctile g blockedIdx start goal

    if not quick.Truncated || not (List.isEmpty quick.Path) then
      quick
    else
      searchWith (g.Width * g.Height) useOctile g blockedIdx start goal

  let internal astarByIndex (g: Grid) (blockedIdx: int -> bool) (start: Pos) (goal: Pos) : Result =
    search true g (fun _ _ idx -> blockedIdx idx) start goal

  let astar (g: Grid) (blocked: (Pos -> bool)) (start: Pos) (goal: Pos) : Result =
    search true g (fun x y _ -> blocked { X = x; Y = y }) start goal

  /// The same search with no heuristic. Kept because comparing its expansion
  /// count against A*'s is the entire lesson about why a heuristic is worth
  /// having.
  let dijkstra (g: Grid) (blocked: (Pos -> bool)) (start: Pos) (goal: Pos) : Result =
    search false g (fun x y _ -> blocked { X = x; Y = y }) start goal

  /// Total cost of a path, in the same units A* minimises.
  let pathCost (start: Pos) (path: Pos list) =
    (start :: path) |> List.pairwise |> List.sumBy (fun (a, b) -> stepCost a b)

  /// The greedy stepper slice 1 shipped: take the diagonal-or-axis step that
  /// reduces distance and never look ahead. Kept as the baseline A* is measured
  /// against (Q2c), because it is the cheapest possible answer and it is wrong
  /// in a specific, demonstrable way.
  let greedy (g: Grid) (blocked: (Pos -> bool)) (start: Pos) (goal: Pos) : Result =
    let passable q = Grid.isFloor g q && not (blocked q)
    let mutable current = start
    let mutable steps = 0
    let mutable trail = []
    let mutable stuck = false

    // Greedy can cycle, so it needs a ceiling. A* does not.
    let limit = g.Width * g.Height

    while not stuck && current <> goal && steps < limit do
      let dx = sign (goal.X - current.X)
      let dy = sign (goal.Y - current.Y)

      let candidates =
        [ { X = current.X + dx; Y = current.Y + dy }
          { X = current.X + dx; Y = current.Y }
          { X = current.X; Y = current.Y + dy } ]

      match candidates |> List.tryFind passable with
      | None -> stuck <- true
      | Some next ->
        trail <- next :: trail
        current <- next
        steps <- steps + 1

    { Path = (if current = goal then List.rev trail else [])
      Expanded = steps
      Truncated = current <> goal }

  /// Walk a Bresenham line from `a` towards `b`, testing every tile strictly
  /// between them. Reports false as soon as something blocks.
  ///
  /// Both endpoints are excluded, so an entity never blocks its own line —
  /// otherwise anything standing in a doorway would be permanently unhittable.
  /// A diagonal step additionally requires both of its orthogonal neighbours to
  /// be open, matching `neighbours`, so sight does not slip between two
  /// diagonally adjacent pillars either.
  let private lineClear (g: Grid) (a: Pos) (b: Pos) =
    let width = g.Width
    let height = g.Height
    let walls = g.Walls

    let inline passable (px: int) (py: int) =
      px >= 0 && py >= 0 && px < width && py < height && not walls.[py * width + px]

    let dx = abs (b.X - a.X)
    let dy = abs (b.Y - a.Y)
    let sx = if b.X > a.X then 1 elif b.X < a.X then -1 else 0
    let sy = if b.Y > a.Y then 1 elif b.Y < a.Y then -1 else 0
    let mutable err = dx - dy
    let mutable x = a.X
    let mutable y = a.Y
    let mutable ok = true
    // Bresenham terminates by construction; the guard is here because this
    // runs inside the tick and a hang is worse than a wrong answer.
    let mutable guard = 0
    let limit = dx + dy + 2

    while ok && (x <> b.X || y <> b.Y) && guard < limit do
      guard <- guard + 1
      let previousX = x
      let previousY = y
      let e2 = 2 * err

      if e2 > -dy then
        err <- err - dy
        x <- x + sx

      if e2 < dx then
        err <- err + dx
        y <- y + sy

      let arrived = x = b.X && y = b.Y

      if not arrived then
        if not (passable x y) then
          ok <- false
        elif
          x <> previousX
          && y <> previousY
          && (not (passable x previousY) || not (passable previousX y))
        then
          ok <- false

    ok

  /// Point-to-point line of sight. Both directions must be clear, which makes
  /// symmetry a property of the construction rather than something to hope for:
  /// naive Bresenham picks a different cell on exact diagonal ties depending on
  /// which end you start from, so testing one direction alone gives you a caster
  /// who can see you while you cannot see it back.
  ///
  /// Entities deliberately do not block sight, so only walls are consulted.
  let lineOfSight (g: Grid) (a: Pos) (b: Pos) =
    a = b || (lineClear g a b && lineClear g b a)
