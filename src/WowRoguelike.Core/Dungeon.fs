namespace WowRoguelike.Core

/// Rooms-and-corridors generation (Q21a): scatter non-overlapping rectangular
/// rooms, join them with L-shaped corridors, then pick the boss room as the one
/// farthest from the entrance by *path length* rather than by straight line.
///
/// A rectangular room is not a stylistic choice. The fights are authored against
/// room-shaped space — Lady Anacondra's four spawn positions are four corners of
/// a room — so a generator that produces caves would produce rooms the encounters
/// no longer fit. That is also why cellular automata is not the first algorithm
/// here, and why BSP is the natural second: same output class, different
/// structure, so the two are genuinely comparable.
///
/// The Grid is the one place Q8 sanctions mutation, and generation is where that
/// happens. Everything after it treats the Grid as read-only for the Run.
module Dungeon =

  let width = 46
  let height = 26

  let minRoomSize = 6
  let maxRoomSize = 11
  let targetRooms = 6
  let private placementAttempts = 300

  /// Wall-to-wall gap kept between rooms, so a corridor reads as a corridor.
  let private roomMargin = 1

  /// Two tiles wide. At one tile a five-member Party cannot pass itself, which
  /// makes every corridor a deadlock rather than a route.
  let private corridorWidth = 2

  /// Breadth-first flood fill over floor tiles, 4-way, returning the path
  /// distance to every tile. -1 means a wall or an unreachable tile.
  ///
  /// This is used twice and deliberately so: to choose the boss room, and to
  /// prove the layout is connected. One algorithm, and the second use is an
  /// assertion about the first.
  let floodFill (g: Grid) (start: Pos) : int[] =
    let distances = Array.create (g.Width * g.Height) -1
    let index (p: Pos) = p.Y * g.Width + p.X
    let queue = System.Collections.Generic.Queue<Pos>()

    if Grid.isFloor g start then
      distances.[index start] <- 0
      queue.Enqueue start

    while queue.Count > 0 do
      let p = queue.Dequeue()
      let next = distances.[index p] + 1

      for (dx, dy) in [ (-1, 0); (1, 0); (0, -1); (0, 1) ] do
        let q = Pos.move dx dy p

        if Grid.isFloor g q && distances.[index q] < 0 then
          distances.[index q] <- next
          queue.Enqueue q

    distances

  /// Every floor tile is reachable from `start`. The layout is connected by
  /// construction, so this is the test that says the construction is right —
  /// rather than regenerate-until-connected, which turns a bug into a hang.
  let isConnected (g: Grid) (start: Pos) =
    let distances = floodFill g start

    Grid.tiles g
    |> Seq.filter (Grid.isFloor g)
    |> Seq.forall (fun p -> distances.[p.Y * g.Width + p.X] >= 0)

  let private carveCorridor (g: Grid) (from: Pos) (target: Pos) =
    let carve (p: Pos) =
      for dy in 0 .. corridorWidth - 1 do
        for dx in 0 .. corridorWidth - 1 do
          let q = { X = p.X + dx; Y = p.Y + dy }

          if Grid.inBounds g q then
            g.Walls.[q.Y * g.Width + q.X] <- false

    let stepX = sign (target.X - from.X)
    let stepY = sign (target.Y - from.Y)
    let mutable p = from

    // Horizontal then vertical. Enough because every room is joined to the one
    // before it, so the chain is a path and not a tree that needs solving.
    while p.X <> target.X do
      carve p
      p <- { p with X = p.X + stepX }

    while p.Y <> target.Y do
      carve p
      p <- { p with Y = p.Y + stepY }

    carve p

  let private carveRoom (g: Grid) (r: Room) =
    for y in r.Top .. r.Bottom do
      for x in r.Left .. r.Right do
        g.Walls.[y * g.Width + x] <- false

  /// Generate a floorplan. Draws exclusively from the state it is handed, and
  /// returns the advanced state, so a Run's generation is reproducible from its
  /// seed and cannot disturb the Combat stream (ADR-0002).
  let generate (seed: RngState) : Layout * RngState =
    let g = Grid.createSolid width height
    let mutable rng = seed

    let draw lo hi =
      let value, advanced = Rng.roll lo hi rng
      rng <- advanced
      value

    let placed = ResizeArray<Room>()
    let mutable attempts = 0

    while placed.Count < targetRooms && attempts < placementAttempts do
      attempts <- attempts + 1
      let w = draw minRoomSize maxRoomSize
      let h = draw minRoomSize maxRoomSize
      let left = draw 1 (width - w - 2)
      let top = draw 1 (height - h - 2)

      let candidate =
        { Left = left
          Top = top
          Right = left + w - 1
          Bottom = top + h - 1 }

      if not (placed |> Seq.exists (Room.tooClose roomMargin candidate)) then
        placed.Add candidate

    // Placement can only fail by running out of attempts, which would leave a
    // Dungeon with no rooms at all. Fall back to one room rather than crash.
    if placed.Count = 0 then
      placed.Add
        { Left = width / 4
          Top = height / 4
          Right = width * 3 / 4
          Bottom = height * 3 / 4 }

    let rooms = List.ofSeq placed

    for r in rooms do
      carveRoom g r

    // Join each room to the previous one, then add one link between rooms that
    // are not neighbours, so the Dungeon has a side route rather than a single
    // chain that dead-ends everywhere.
    let links =
      (rooms |> List.pairwise)
      @ (if List.length rooms > 3 then
           [ (List.item 1 rooms, List.last rooms) ]
         else
           [])

    for (a, b) in links do
      carveCorridor g (Room.centre a) (Room.centre b)

    let entrance = Room.centre (List.head rooms)
    let distances = floodFill g entrance

    // Farthest by path length. A room's centre is always floor, so this cannot
    // read a wall's -1.
    let bossRoom =
      rooms
      |> List.maxBy (fun r ->
        let c = Room.centre r
        distances.[c.Y * width + c.X])

    { Grid = g
      Rooms = rooms
      Entrance = entrance
      BossRoom = bossRoom },
    rng

  /// Generate a floorplan from a Run seed. Convenience for callers that only
  /// need the layout.
  let layout (seed: uint64) = fst (generate (Rng.stream seed Rng.GenerationStream))

  /// One-line summary of a floorplan, for tests and reports.
  let describe (layout: Layout) =
    sprintf
      "%dx%d, %d rooms, entrance %d,%d, boss room %d,%d %dx%d"
      width
      height
      (List.length layout.Rooms)
      layout.Entrance.X
      layout.Entrance.Y
      layout.BossRoom.Left
      layout.BossRoom.Top
      (Room.width layout.BossRoom)
      (Room.height layout.BossRoom)

  /// An ASCII rendering of a floorplan, with the entrance and boss room marked.
  /// This is the debugging tool that makes a bad dungeon obvious in one glance
  /// rather than after an hour of failing tests.
  let render (layout: Layout) =
    let sb = System.Text.StringBuilder()

    for y in 0 .. layout.Grid.Height - 1 do
      for x in 0 .. layout.Grid.Width - 1 do
        let p = { X = x; Y = y }

        let c =
          if p = layout.Entrance then '@'
          elif Room.contains layout.BossRoom p then 'B'
          elif Grid.isWall layout.Grid p then '#'
          else '.'

        sb.Append c |> ignore

      sb.AppendLine() |> ignore

    sb.ToString()
