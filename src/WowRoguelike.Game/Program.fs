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
type private ShellRequest =
  | Pause
  | Resume
  | SetSpeed of float
  | TickOnce
  | ShowParty
  | ShowState
  | ShowTargets of EntityId * string
  | Issue of CommandKind

/// What a click on a rendered button asks for.
type private UiAction =
  | TogglePause
  | StepOnce
  | Deselect
  | ChooseAbility of EntityId * string
  | CancelTargeting

module private Parse =

  /// Abilities are matched by a punctuation-free key so that "Lesser Heal" can
  /// be typed as `lesserheal` and "Druid's Slumber" as `druidsslumber`.
  let key (name: string) =
    name
    |> Seq.filter Char.IsLetterOrDigit
    |> Seq.map Char.ToLowerInvariant
    |> Seq.toArray
    |> String

  let private idOf (s: string) =
    match Int32.TryParse s with
    | true, v -> Some(EntityId v)
    | _ -> None

  /// Parsed against the live World on the render thread, so the Core is only
  /// ever read from one thread.
  let parse (w: World) (line: string) : ShellRequest option =
    let t = line.Trim().Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)

    let abilityName (actorId: EntityId) (k: string) =
      match SimState.tryEntity actorId w with
      | Some e -> e.Abilities |> List.tryFind (fun a -> key a.Name = k) |> Option.map (fun a -> a.Name)
      | None -> None

    match t with
    | [||] -> None
    | [| "pause" |] -> Some Pause
    | [| "resume" |] -> Some Resume
    | [| "step" |] -> Some TickOnce
    | [| "party" |] -> Some ShowParty
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
    | [| "targets"; actor; abilityKey |] ->
      match idOf actor with
      | Some id -> abilityName id abilityKey |> Option.map (fun name -> ShowTargets(id, name))
      | None -> None
    | [| actor; target; abilityKey |] ->
      match idOf actor, idOf target with
      | Some a, Some tg ->
        abilityName a abilityKey
        |> Option.map (fun name -> Issue(UseAbility(a, name, tg)))
      | _ -> None
    | _ -> None

/// A hand-rolled 5x7 pixel font, so the shell gets text with no content
/// pipeline, no font asset and no dependency. Each glyph is 5 bytes: byte[c] is
/// column c, bit r (LSB first) is row r. Unknown characters render as a block.
module private Font5x7 =

  let block = [| 127uy; 127uy; 127uy; 127uy; 127uy |]

  let glyphs : Map<char, byte[]> =
    Map.ofList
      [ 'A', [| 126uy; 9uy; 9uy; 9uy; 126uy |]
        'B', [| 127uy; 69uy; 69uy; 69uy; 58uy |]
        'C', [| 62uy; 65uy; 65uy; 65uy; 34uy |]
        'D', [| 127uy; 65uy; 65uy; 65uy; 62uy |]
        'E', [| 127uy; 69uy; 69uy; 69uy; 65uy |]
        'F', [| 127uy; 5uy; 5uy; 5uy; 1uy |]
        'G', [| 62uy; 65uy; 73uy; 73uy; 58uy |]
        'H', [| 127uy; 8uy; 8uy; 8uy; 127uy |]
        'I', [| 65uy; 65uy; 127uy; 65uy; 65uy |]
        'J', [| 32uy; 64uy; 65uy; 63uy; 1uy |]
        'K', [| 127uy; 28uy; 8uy; 34uy; 65uy |]
        'L', [| 127uy; 64uy; 64uy; 64uy; 64uy |]
        'M', [| 127uy; 2uy; 12uy; 2uy; 127uy |]
        'N', [| 127uy; 2uy; 4uy; 8uy; 127uy |]
        'O', [| 62uy; 65uy; 65uy; 65uy; 62uy |]
        'P', [| 127uy; 9uy; 9uy; 9uy; 6uy |]
        'Q', [| 62uy; 65uy; 81uy; 33uy; 94uy |]
        'R', [| 127uy; 9uy; 9uy; 25uy; 102uy |]
        'S', [| 70uy; 73uy; 73uy; 73uy; 49uy |]
        'T', [| 1uy; 1uy; 127uy; 1uy; 1uy |]
        'U', [| 63uy; 64uy; 64uy; 64uy; 63uy |]
        'V', [| 31uy; 32uy; 64uy; 32uy; 31uy |]
        'W', [| 127uy; 32uy; 24uy; 32uy; 127uy |]
        'X', [| 99uy; 20uy; 8uy; 20uy; 99uy |]
        'Y', [| 3uy; 4uy; 120uy; 4uy; 3uy |]
        'Z', [| 97uy; 81uy; 73uy; 69uy; 67uy |]
        '0', [| 62uy; 81uy; 73uy; 69uy; 62uy |]
        '1', [| 0uy; 66uy; 127uy; 64uy; 0uy |]
        '2', [| 66uy; 97uy; 81uy; 73uy; 70uy |]
        '3', [| 33uy; 65uy; 69uy; 75uy; 49uy |]
        '4', [| 24uy; 20uy; 18uy; 127uy; 16uy |]
        '5', [| 39uy; 69uy; 69uy; 69uy; 57uy |]
        '6', [| 62uy; 69uy; 69uy; 69uy; 56uy |]
        '7', [| 1uy; 1uy; 121uy; 5uy; 3uy |]
        '8', [| 54uy; 73uy; 73uy; 73uy; 54uy |]
        '9', [| 6uy; 73uy; 73uy; 73uy; 54uy |]
        ' ', [| 0uy; 0uy; 0uy; 0uy; 0uy |]
        '.', [| 0uy; 96uy; 96uy; 0uy; 0uy |]
        ',', [| 0uy; 112uy; 48uy; 0uy; 0uy |]
        '-', [| 8uy; 8uy; 8uy; 8uy; 8uy |]
        ':', [| 0uy; 54uy; 54uy; 0uy; 0uy |] ]

  let ofChar (c: char) = glyphs.TryFind c |> Option.defaultValue block

type RoguelikeGame() as this =
  inherit Game()

  let graphics = new GraphicsDeviceManager(this)

  let tile = 28
  let columns = 22
  let rows = 13

  /// 5x7 pixel font, baked 2x into the atlas so drawing is native-size (crisp).
  let glyphW = 5
  let glyphH = 7
  let fontScale = 2
  let cellW = (glyphW + 1) * fontScale
  let cellH = glyphH * fontScale

  /// The strip below the grid that holds the clickable controls.
  let uiTop = rows * tile + 8
  let uiHeight = 120

  /// Ceiling on ticks per rendered frame. Without it a slow frame becomes a
  /// longer frame, which is the classic spiral. Falling behind in real time is
  /// correct; dropping Ticks would break determinism (ADR-0001).
  let maxTicksPerFrame = 4

  let mutable spriteBatch: SpriteBatch = null
  let mutable pixel: Texture2D = null
  let mutable disc: Texture2D = null
  let mutable fontTex: Texture2D = null

  let mutable world = Content.gully 1UL
  let mutable ticksPerSecond = 20.0
  let mutable accumulator = 0.0
  let mutable runOnce = false
  let mutable printedLogLines = 0

  /// Raw lines from stdin, parsed on the render thread against the live World.
  let typed = new ConcurrentQueue<string>()
  let pending = ResizeArray<CommandKind>()

  // Mouse-driven command state.
  let mutable selected: EntityId option = None
  let mutable targeting: (EntityId * string) option = None
  let mutable prevMouse = Mouse.GetState()

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
    if not (SimState.alive e) then
      Color(70, 70, 75)
    elif SimState.isSleeping e then
      Color(120, 120, 30)
    elif SimState.isShifted e then
      Color(150, 60, 180)
    elif e.Faction = Party then
      match e.Role with
      | Some Tank -> Color(90, 140, 230)
      | Some Healer -> Color(90, 210, 140)
      | _ -> Color(210, 210, 120)
    else
      Color(210, 80, 80)

  let measure (text: string) = text.Length * cellW

  let buildFontTexture () =
    let count = 95
    let tex = new Texture2D(this.GraphicsDevice, cellW * count, cellH)
    let data = Array.zeroCreate<Color> (cellW * count * cellH)

    for i in 0 .. count - 1 do
      let g = Font5x7.ofChar (char (32 + i))

      for col in 0 .. glyphW - 1 do
        let bits = int g.[col]

        for row in 0 .. glyphH - 1 do
          if (bits &&& (1 <<< row)) <> 0 then
            for dy in 0 .. fontScale - 1 do
              for dx in 0 .. fontScale - 1 do
                let px = i * cellW + col * fontScale + dx
                let py = row * fontScale + dy
                data.[py * cellW * count + px] <- Color.White

    tex.SetData(data)
    tex

  let drawText (text: string) (x: int) (y: int) (colour: Color) =
    text.ToUpperInvariant()
    |> Seq.iteri (fun i c ->
      let code = int c - 32

      if code >= 0 && code < 95 then
        spriteBatch.Draw(
          fontTex,
          Rectangle(x + i * cellW, y, glyphW * fontScale, glyphH * fontScale),
          Rectangle(code * cellW, 0, glyphW * fontScale, glyphH * fontScale),
          colour))

  /// The alive entity under a click, hit-tested against its interpolated centre.
  let entityAt (mx: int) (my: int) =
    world.Entities
    |> List.tryFind (fun e ->
      SimState.alive e
      && (let fx, fy = drawnAt e
          let cx = int (fx * float tile + float tile / 2.0)
          let cy = int (fy * float tile + float tile / 2.0)
          let size = if e.Faction = Party then 22 else 20
          abs (mx - cx) <= size / 2 + 4 && abs (my - cy) <= size / 2 + 4))

  /// The buttons for this frame: the global controls, plus the selected
  /// member's abilities (or a Cancel while targeting). Wraps onto extra rows.
  let uiButtons () =
    let panelW = columns * tile + 24
    let mutable x = 8
    let mutable row = 0
    let y0 = uiTop + 6

    let add label action =
      let w = measure label + 12

      if x + w > panelW - 8 then
        x <- 8
        row <- row + 1

      let b = Rectangle(x, y0 + row * 30, w, 26), label, action
      x <- x + w + 6
      b

    let pauseLabel = if ticksPerSecond > 0.0 then "Pause" else "Resume"
    let globals = [ add pauseLabel TogglePause; add "Step" StepOnce; add "Clear" Deselect ]

    match selected with
    | None -> globals
    | Some aid ->
      match targeting with
      | Some _ -> globals @ [ add "Cancel" CancelTargeting ]
      | None ->
        match SimState.tryEntity aid world with
        | Some e -> globals @ (e.Abilities |> List.map (fun a -> add a.Name (ChooseAbility(aid, a.Name))))
        | None -> globals

  let printParty () =
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
        sprintf
          "%-3d %-18s %-7s hp %4d/%-4d res %4d/%-4d %s"
          (let (EntityId i) = e.Id in i)
          e.Name
          role
          e.Health
          e.MaxHealth
          e.Resource
          e.MaxResource
          keys
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

    world <- SimTick.step commands world

  do
    graphics.PreferredBackBufferWidth <- columns * tile + 24
    graphics.PreferredBackBufferHeight <- uiTop + uiHeight
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
    fontTex <- buildFontTexture ()

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

    // Mouse: a click is the press edge, so holding the button does not repeat.
    let mouse = Mouse.GetState()

    if mouse.LeftButton = ButtonState.Pressed && prevMouse.LeftButton = ButtonState.Released then
      let mx, my = mouse.X, mouse.Y

      match uiButtons () |> List.tryFind (fun (r, _, _) -> r.Contains(mx, my)) with
      | Some (_, _, action) ->
        match action with
        | TogglePause ->
          if ticksPerSecond > 0.0 then ticksPerSecond <- 0.0 else ticksPerSecond <- 20.0
        | StepOnce -> runOnce <- true
        | Deselect ->
          selected <- None
          targeting <- None
        | ChooseAbility(aid, ability) -> targeting <- Some(aid, ability)
        | CancelTargeting -> targeting <- None
      | None ->
        match entityAt mx my with
        | Some e ->
          match targeting with
          | Some (aid, ability) ->
            if SimState.validTargets aid ability world |> List.exists (fun t -> t.Id = e.Id) then
              pending.Add(UseAbility(aid, ability, e.Id))

            targeting <- None
          | None -> if e.Faction = Party then selected <- Some e.Id
        | None ->
          let tx, ty = mx / tile, my / tile

          if ty < rows && Grid.isFloor world.Grid { X = tx; Y = ty } then
            match selected with
            | Some aid -> pending.Add(MoveTo(aid, { X = tx; Y = ty }))
            | None -> ()

    prevMouse <- mouse

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
      | Some TickOnce -> runOnce <- true
      | Some ShowParty -> printParty ()
      | Some ShowState ->
        Console.WriteLine()
        Console.WriteLine(Dump.world world)
      | Some(ShowTargets(actorId, abilityName)) ->
        Console.WriteLine()
        Console.WriteLine(sprintf "valid targets for %s:" abilityName)

        SimState.validTargets actorId abilityName world
        |> List.iter (fun t ->
          Console.WriteLine(
            sprintf "  %-3d %-18s hp %4d/%-4d" (let (EntityId i) = t.Id in i) t.Name t.Health t.MaxHealth))

        Console.WriteLine()
      | Some(Issue kind) -> pending.Add kind
      | None -> Console.WriteLine(sprintf "? %s" line)

    if runOnce then
      runOnce <- false
      advance ()
    elif ticksPerSecond > 0.0 then
      // Fixed-timestep accumulator: speed scales how much real time one Tick
      // consumes, and pause is simply zero Ticks per second.
      accumulator <- accumulator + gameTime.ElapsedGameTime.TotalSeconds * ticksPerSecond

      let mutable advanced = 0

      while accumulator >= 1.0 && advanced < maxTicksPerFrame do
        accumulator <- accumulator - 1.0
        advance ()
        advanced <- advanced + 1

      if accumulator > float maxTicksPerFrame then
        accumulator <- 0.0

    flushNewLogLines ()
    base.Update(gameTime)

  override _.Draw(gameTime: GameTime) =
    this.GraphicsDevice.Clear(Color(12, 12, 18))

    let validIds =
      match targeting with
      | Some (aid, ability) ->
        SimState.validTargets aid ability world |> List.map (fun t -> t.Id) |> Set.ofList
      | None -> Set.empty

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

      if SimState.alive e then
        // Halo for the selected member and for valid targeting candidates.
        match selected with
        | Some sid when sid = e.Id -> spriteBatch.Draw(disc, Rectangle(cx - 16, cy - 16, 32, 32), Color(120, 200, 255) * 0.55f)
        | _ ->
          if validIds.Contains e.Id then
            let halo = if e.Faction = Party then Color(110, 240, 150) else Color(240, 110, 110)
            spriteBatch.Draw(disc, Rectangle(cx - 16, cy - 16, 32, 32), halo * 0.55f)

        spriteBatch.Draw(
          disc,
          Rectangle(cx - size / 2, cy - size / 2, size, size),
          colour
        )

        bar (cx - 14) (cy - size / 2 - 7) 28 4 (float e.Health / float e.MaxHealth)
          (if e.Faction = Party then Color(90, 220, 110) else Color(220, 90, 90))

        if e.MaxResource > 0 then
          bar (cx - 14) (cy - size / 2 - 11) 28 2 (float e.Resource / float e.MaxResource) (Color(90, 150, 230))

        if e.Casting.IsSome then
          bar (cx - 14) (cy + size / 2 + 2) 28 3 1.0 (Color(240, 200, 90))

        if e.Faction = Party then
          drawText e.Name (cx - measure e.Name / 2) (cy + 16) (Color(220, 220, 230))
      else
        spriteBatch.Draw(pixel, Rectangle(cx - 6, cy - 6, 12, 12), colour)

    // The clickable strip.
    spriteBatch.Draw(pixel, Rectangle(0, uiTop, columns * tile + 24, uiHeight), Color(20, 22, 30))
    let m = Mouse.GetState()

    for (rect, label, _) in uiButtons () do
      let hover = rect.Contains(m.X, m.Y)
      spriteBatch.Draw(pixel, rect, if hover then Color(70, 78, 96) else Color(42, 46, 60))
      drawText label (rect.X + 6) (rect.Y + 5) (if hover then Color.White else Color(210, 210, 220))

    let nameOf aid =
      SimState.tryEntity aid world |> Option.map (fun e -> e.Name) |> Option.defaultValue "?"

    let hint =
      match selected, targeting with
      | Some _, Some (_, ability) -> sprintf "Choose a target for %s — click a highlighted unit" ability
      | Some aid, None -> sprintf "%s selected — click a tile to move, or an ability button" (nameOf aid)
      | None, _ -> "Click a party member to command them"

    drawText hint 8 (uiTop + uiHeight - 22) (Color(160, 160, 170))

    spriteBatch.End()
    base.Draw(gameTime)

module Program =

  [<EntryPoint>]
  let main _ =
    use game = new RoguelikeGame()

    Console.WriteLine "WowRoguelike — Screaming Gully (Wailing Caverns, first encounter)"
    Console.WriteLine ""
    Console.WriteLine "Mouse: click a party member, then an ability button, then a target."
    Console.WriteLine "       Click an empty floor tile to move the selected member."
    Console.WriteLine "Typed commands still work:"
    Console.WriteLine "  <actor> move <x> <y>          walk"
    Console.WriteLine "  <actor> <target> <abilitykey> use an ability, e.g. `3 8 kick`"
    Console.WriteLine "  pause | resume | step | speed <n> | party | state | targets <actor> <abilitykey>"

    game.Run()
    0
