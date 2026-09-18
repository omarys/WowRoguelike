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
      Expanded: int }

  /// Walk the cameFrom chain back to the start. Returns the tiles *after* the
  /// start through the goal, which is what a mover wants: it is already standing
  /// on the first tile.
  let private reconstruct (cameFrom: Map<Pos, Pos>) (goal: Pos) =
    let rec loop acc p =
      match cameFrom |> Map.tryFind p with
      | Some prev -> loop (p :: acc) prev
      | None -> p :: acc

    loop [] goal |> List.tail

  /// The one search. A* and Dijkstra differ only in the heuristic, which is why
  /// they are the same function.
  let private search
    (heuristic: Pos -> Pos -> int)
    (g: Grid)
    (blocked: (Pos -> bool))
    (start: Pos)
    (goal: Pos)
    : Result =
    if start = goal then
      { Path = []
        Expanded = 0 }
    else
      // The goal is always enterable, so a route to a tile somebody is standing
      // on still exists and the mover can walk up to it.
      let passable p = p = goal || (Grid.isFloor g p && not (blocked p))

      let openList = MinHeap<int, Pos>()
      openList.Push(heuristic start goal, start)

      let mutable best = Map.ofList [ start, 0 ]
      let mutable cameFrom = Map.empty
      let mutable closed = Set.empty
      let mutable expanded = 0
      let mutable result = None

      while result.IsNone && not openList.IsEmpty do
        match openList.Pop() with
        | None -> ()
        | Some(_, current) ->
          if Set.contains current closed then
            // A stale entry: a shorter route to this tile was already expanded.
            ()
          elif current = goal then
            result <- Some(reconstruct cameFrom goal)
          else
            closed <- Set.add current closed
            expanded <- expanded + 1
            let gCurrent = best |> Map.find current

            for n in neighbours passable current do
              if not (Set.contains n closed) then
                let tentative = gCurrent + stepCost current n

                match best |> Map.tryFind n with
                | Some known when known <= tentative -> ()
                | _ ->
                  best <- best |> Map.add n tentative
                  cameFrom <- cameFrom |> Map.add n current
                  // Re-pushing an improved node is cheaper than supporting
                  // decrease-key, and the stale-entry check above covers it.
                  openList.Push(tentative + heuristic n goal, n)

      { Path = result |> Option.defaultValue []
        Expanded = expanded }

  let astar (g: Grid) (blocked: (Pos -> bool)) (start: Pos) (goal: Pos) : Result =
    search octile g blocked start goal

  /// The same search with no heuristic. Kept because comparing its expansion
  /// count against A*'s is the entire lesson about why a heuristic is worth
  /// having.
  let dijkstra (g: Grid) (blocked: (Pos -> bool)) (start: Pos) (goal: Pos) : Result =
    search (fun _ _ -> 0) g blocked start goal

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
      Expanded = steps }

  /// Walk a Bresenham line from `a` towards `b`, testing every tile strictly
  /// between them. Reports false as soon as something blocks.
  ///
  /// Both endpoints are excluded, so an entity never blocks its own line —
  /// otherwise anything standing in a doorway would be permanently unhittable.
  /// A diagonal step additionally requires both of its orthogonal neighbours to
  /// be open, matching `neighbours`, so sight does not slip between two
  /// diagonally adjacent pillars either.
  let private lineClear (passable: Pos -> bool) (a: Pos) (b: Pos) =
    let dx = abs (b.X - a.X)
    let dy = abs (b.Y - a.Y)
    let sx = sign (b.X - a.X)
    let sy = sign (b.Y - a.Y)
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
        if not (passable { X = x; Y = y }) then
          ok <- false
        elif
          x <> previousX
          && y <> previousY
          && (not (passable { X = x; Y = previousY })
              || not (passable { X = previousX; Y = y }))
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
    let passable p = Grid.isFloor g p
    a = b || (lineClear passable a b && lineClear passable b a)
