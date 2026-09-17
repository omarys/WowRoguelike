namespace WowRoguelike.Game

open System
open System.Collections.Concurrent
open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open WowRoguelike.Core

/// What the player can ask for. The speed controls never touch the Core — they
/// decide how many times the Shell calls `step`, which is what keeps a replay
/// exact at any speed (ADR-0001).
type private Input =
  | Pause
  | Resume
  | SetSpeed of float
  | StepOnce
  | ShowRoster
  | ShowState
  | Issue of CommandKind

module private Parse =

  /// Abilities are matched by a space-free key so that "Lesser Heal" can be
  /// typed as `lesserheal`.
  let key (name: string) = name.Replace(" ", "").ToLowerInvariant()

  let private idOf (s: string) =
    match Int32.TryParse s with
    | true, v -> Some(EntityId v)
    | _ -> None

  /// Parsed against the live World on the render thread, so the Core is only
  /// ever read from one thread.
  let parse (w: World) (line: string) : Input option =
    let t = line.Trim().Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)

    let abilityName (actorId: EntityId) (k: string) =
      match Sim.tryEntity actorId w with
      | Some e -> e.Abilities |> List.tryFind (fun a -> key a.Name = k) |> Option.map (fun a -> a.Name)
      | None -> None

    match t with
    | [||] -> None
    | [| "pause" |] -> Some Pause
    | [| "resume" |] -> Some Resume
    | [| "step" |] -> Some StepOnce
    | [| "roster" |] -> Some ShowRoster
    | [| "state" |] -> Some ShowState
    | [| "speed"; n |] ->
      match Double.TryParse n with
      | true, v -> Some(SetSpeed v)
      | _ -> None
    | [| actor; "stop" |] -> idOf actor |> Option.map (fun id -> Issue(Stop id))
    | [| actor; "move"; x; y |] ->
      match idOf actor, Int32.TryParse x, Int32.TryParse y with
      | Some id, (true, px), (true, py) -> Some(Issue(MoveTo(id, { X = px; Y = py })))
      | _ -> None
    | [| actor; target; abilityKey |] ->
      match idOf actor, idOf target with
      | Some a, Some tg ->
        abilityName a abilityKey
        |> Option.map (fun name -> Issue(UseAbility(a, name, tg)))
      | _ -> None
    | _ -> None

type RoguelikeGame() as this =
  inherit Game()

  let graphics = new GraphicsDeviceManager(this)

  let tile = 28
  let columns = 22
  let rows = 13

  /// Ceiling on ticks per rendered frame. Without it a slow frame becomes a
  /// longer frame, which is the classic spiral. Falling behind in real time is
  /// correct; dropping Ticks would break determinism (ADR-0001).
  let maxStepsPerFrame = 4

  let mutable spriteBatch: SpriteBatch = null
  let mutable pixel: Texture2D = null
  let mutable disc: Texture2D = null

  let mutable world = Content.gully 1UL
  let mutable ticksPerSecond = 20.0
  let mutable accumulator = 0.0
  let mutable runOnce = false
  let mutable printedLogLines = 0

  /// Raw lines from stdin, parsed on the render thread against the live World.
  let typed = new ConcurrentQueue<string>()
  let pending = ResizeArray<CommandKind>()

  let makeDisc (radius: int) : Texture2D =
    let d = radius * 2
    let tex = new Texture2D(this.GraphicsDevice, d, d)
    let data = Array.zeroCreate<Color> (d * d)
    let centre = float radius - 0.5

    for y in 0 .. d - 1 do
      for x in 0 .. d - 1 do
        let dx = float x - centre
        let dy = float y - centre

        if dx * dx + dy * dy <= float radius * float radius then
          data.[y * d + x] <- Color.White

    tex.SetData(data)
    tex

  let bar (x: int) (y: int) (width: int) (height: int) (filled: float) (colour: Color) =
    spriteBatch.Draw(pixel, Rectangle(x, y, width, height), Color.Black * 0.6f)

    let w = int (float width * max 0.0 (min 1.0 filled))

    if w > 0 then
      spriteBatch.Draw(pixel, Rectangle(x, y, w, height), colour)

  /// Where the entity should be drawn this instant, interpolating between the
  /// tile it is leaving and the tile it is entering. The Core knows nothing
  /// about this (Q27a).
  let drawnAt (e: Entity) =
    match e.Destination with
    | Some dest when e.MoveTicksTotal > ticks 0 ->
      let progress =
        1.0 - (float e.MoveTicksLeft / float e.MoveTicksTotal)

      (float e.Pos.X + (float dest.X - float e.Pos.X) * progress,
       float e.Pos.Y + (float dest.Y - float e.Pos.Y) * progress)
    | _ -> (float e.Pos.X, float e.Pos.Y)

  let entityColour (e: Entity) =
    if not (Sim.alive e) then
      Color(70, 70, 75)
    elif Sim.isSleeping e then
      Color(120, 120, 30)
    elif Sim.isShifted e then
      Color(150, 60, 180)
    elif e.Faction = Party then
      match e.Role with
      | Some Tank -> Color(90, 140, 230)
      | Some Healer -> Color(90, 210, 140)
      | _ -> Color(210, 210, 120)
    else
      Color(210, 80, 80)

  let printRoster () =
    Console.WriteLine()
    Console.WriteLine("id  name               role    hp        abilities (key to type)")

    for e in world.Entities do
      let role =
        match e.Role with
        | Some r -> string r
        | None -> if e.Faction = Party then "-" else "mob"

      let keys =
        e.Abilities
        |> List.map (fun a -> sprintf "%s=%s" (Parse.key a.Name) a.Name)
        |> String.concat "  "

      Console.WriteLine(
        sprintf "%-3d %-18s %-7s %4d/%-4d %s" (let (EntityId i) = e.Id in i) e.Name role e.Health e.MaxHealth keys
      )

    Console.WriteLine()

  let flushNewLogLines () =
    let total = List.length world.Log

    if total > printedLogLines then
      let fresh = total - printedLogLines

      world.Log
      |> List.truncate fresh
      |> List.rev
      |> List.iter Console.WriteLine

      printedLogLines <- total

    // The Log is excluded from the fingerprint precisely so it can be trimmed
    // here without affecting replay (Q25a).
    if total > 2000 then
      world <- { world with Log = world.Log |> List.truncate 500 }
      printedLogLines <- 500

  /// One Tick, taking whatever the player queued since the last one.
  let advance () =
    let due = List.ofSeq pending

    pending.Clear()

    let commands =
      due
      |> List.map (fun kind ->
        { ApplyAt = world.Tick + ticks 1
          Kind = kind })

    world <- Sim.step commands world

  do
    graphics.PreferredBackBufferWidth <- columns * tile + 24
    graphics.PreferredBackBufferHeight <- rows * tile + 8
    this.Content.RootDirectory <- "Content"
    this.IsMouseVisible <- true

    let reader =
      new System.Threading.Thread(fun () ->
        let mutable line = Console.ReadLine()

        while line <> null do
          typed.Enqueue line
          line <- Console.ReadLine())

    reader.IsBackground <- true
    reader.Start()

  override _.Initialize() = base.Initialize()

  override _.LoadContent() =
    spriteBatch <- new SpriteBatch(this.GraphicsDevice)
    pixel <- new Texture2D(this.GraphicsDevice, 1, 1)
    pixel.SetData([| Color.White |])
    disc <- makeDisc 32

  override _.Update(gameTime: GameTime) =
    let keyboard = Keyboard.GetState()

    if keyboard.IsKeyDown(Keys.Escape) then
      this.Exit()

    if keyboard.IsKeyDown(Keys.Space) then
      if ticksPerSecond > 0.0 then
        ticksPerSecond <- 0.0
        Console.WriteLine "[paused]"
      else
        ticksPerSecond <- 20.0
        Console.WriteLine "[resumed at 20Hz]"

    if keyboard.IsKeyDown(Keys.OemPeriod) then
      runOnce <- true

    // Drain stdin. Parsed here, on the one thread that owns the World.
    let mutable line = Unchecked.defaultof<string>

    while typed.TryDequeue(&line) do
      match Parse.parse world line with
      | Some Pause ->
        ticksPerSecond <- 0.0
        Console.WriteLine "[paused]"
      | Some Resume ->
        ticksPerSecond <- 20.0
        Console.WriteLine "[resumed at 20Hz]"
      | Some(SetSpeed v) ->
        ticksPerSecond <- max 0.0 (min 20.0 v)
        Console.WriteLine(sprintf "[speed %.1fHz]" ticksPerSecond)
      | Some StepOnce -> runOnce <- true
      | Some ShowRoster -> printRoster ()
      | Some ShowState ->
        Console.WriteLine()
        Console.WriteLine(Dump.world world)
      | Some(Issue kind) -> pending.Add kind
      | None -> Console.WriteLine(sprintf "? %s" line)

    if runOnce then
      runOnce <- false
      advance ()
    elif ticksPerSecond > 0.0 then
      // Fixed-timestep accumulator: speed scales how much real time one Tick
      // consumes, and pause is simply zero Ticks per second.
      accumulator <- accumulator + gameTime.ElapsedGameTime.TotalSeconds * ticksPerSecond

      let mutable steps = 0

      while accumulator >= 1.0 && steps < maxStepsPerFrame do
        accumulator <- accumulator - 1.0
        advance ()
        steps <- steps + 1

      if accumulator > float maxStepsPerFrame then
        accumulator <- 0.0

    flushNewLogLines ()
    base.Update(gameTime)

  override _.Draw(gameTime: GameTime) =
    this.GraphicsDevice.Clear(Color(12, 12, 18))
    spriteBatch.Begin()

    for y in 0 .. rows - 1 do
      for x in 0 .. columns - 1 do
        let p = { X = x; Y = y }

        let colour =
          if Grid.isWall world.Grid p then Color(46, 44, 40) else Color(24, 26, 34)

        spriteBatch.Draw(pixel, Rectangle(x * tile, y * tile, tile - 1, tile - 1), colour)

    for e in world.Entities do
      let fx, fy = drawnAt e

      let cx = int (fx * float tile + float tile / 2.0)
      let cy = int (fy * float tile + float tile / 2.0)
      let size = if e.Faction = Party then 22 else 20
      let colour = entityColour e

      if Sim.alive e then
        spriteBatch.Draw(
          disc,
          Rectangle(cx - size / 2, cy - size / 2, size, size),
          colour
        )

        bar (cx - 14) (cy - size / 2 - 7) 28 4 (float e.Health / float e.MaxHealth)
          (if e.Faction = Party then Color(90, 220, 110) else Color(220, 90, 90))

        if e.Casting.IsSome then
          bar (cx - 14) (cy + size / 2 + 2) 28 3 1.0 (Color(240, 200, 90))
      else
        spriteBatch.Draw(pixel, Rectangle(cx - 6, cy - 6, 12, 12), colour)

    spriteBatch.End()
    base.Draw(gameTime)

module Program =

  [<EntryPoint>]
  let main _ =
    use game = new RoguelikeGame()

    Console.WriteLine "WowRoguelike — Screaming Gully (Wailing Caverns, first encounter)"
    Console.WriteLine ""
    Console.WriteLine "Typed commands apply at the next Tick."
    Console.WriteLine "  <actor> move <x> <y>          walk"
    Console.WriteLine "  <actor> stop                  stop, drop the cast"
    Console.WriteLine "  <actor> <target> <abilitykey> use an ability, e.g. `3 8 kick`"
    Console.WriteLine "  pause | resume | step         speed control, 0 to 20Hz (space, period)"
    Console.WriteLine "  speed <n>                     set ticks per second, 0..20"
    Console.WriteLine "  roster | state                list entities | dump canonical state"

    game.Run()
    0
