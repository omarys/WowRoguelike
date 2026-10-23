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
  | ChooseMove of EntityId
  | CancelPending
  /// Hand a member over to the player, or give it back to the script.
  | ToggleControl of EntityId
  /// A group heading. Rendered, not clickable.
  | Heading

/// What a click is waiting on: an ability awaiting a target, or a move awaiting
/// a destination tile. Both are queued per character, the same way.
type private PendingAction =
  | Targeting of EntityId * string
  | Moving of EntityId

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
        ':', [| 0uy; 54uy; 54uy; 0uy; 0uy |]
        '\'', [| 6uy; 0uy; 0uy; 0uy; 0uy |]
        '(', [| 62uy; 65uy; 0uy; 0uy; 0uy |]
        ')', [| 0uy; 0uy; 0uy; 65uy; 62uy |] ]

  let ofChar (c: char) = glyphs.TryFind c |> Option.defaultValue block

/// The player's selection: a dungeon, then (where it has a band) the level. The
/// band is the difficulty, from .pi/research/levelscale/dungeon-bands.md.
module private Menu =

  /// Which menu the player is in.
  type Stage =
    | Closed
    | PickDungeon of int
    | PickDifficulty of int * int
    /// Inspecting the maps instead of playing them: which source, which seed.
    | Browse of int * uint64

  /// One playable dungeon and the levels it can be entered at.
  type Entry =
    { Label: string
      Bands: (string * int) list
      /// The folder under Assets/ holding this dungeon's sliced creature art;
      /// empty where the art is hand-named at the sprites root instead.
      Art: string
      /// Take the band index; the Party's level follows from the band.
      Start: int -> World }

  let private scarletStart (wing: string) (band: int) : World =
    let difficulty =
      match band with
      | 0 -> Content.Minimum
      | 1 -> Content.Median
      | _ -> Content.Maximum

    Content.scarletMonasteryAt wing difficulty 1UL

  let entries () : Entry list =
    [ { Label = "Screaming Gully"
        Bands = [ "level", 24 ]
        Art = ""
        Start = fun _ -> Content.gully 1UL }
      { Label = "Wailing Caverns"
        Bands = [ "minimum", 17; "median", 21; "maximum", 22 ]
        Art = ""
        Start = fun band -> Content.wailingCavernsAt ([ 17; 21; 22 ] |> List.item band) 1UL } ]
    @ [ for wing in Content.scarletWings ->
          { Label = "Scarlet Monastery — " + wing.Name
            Bands =
              [ "minimum", wing.Minimum
                "median", wing.Median
                "maximum", wing.Maximum ]
            Art = "sprites/scarlet-monastery"
            Start = scarletStart wing.Name } ]

type RoguelikeGame(?browse: int * uint64) as this =
  inherit Game()

  let graphics = new GraphicsDeviceManager(this)

  /// Must match TILE and SPRITE in tools/slice_tileset.py: the PNGs are cut to
  /// the size they are drawn at, so nothing is resampled at runtime. A sprite
  /// is taller than its tile and stands on it, overhanging the row above.
  let tile = 40
  let spriteSize = 48
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
  let uiHeight = 264

  /// The combat log box below the control strip. The window is taller than the
  /// board so the log gets its own room rather than covering it.
  let logHeight = 188

  /// Members the player has taken over: their AI orders are suppressed until
  /// they are handed back. Held here rather than in the World because it is an
  /// input policy, not simulation state — a replay records the commands it
  /// produced, and they are the same commands either way.
  let mutable manual = Set.empty<EntityId>

  /// The side panel to the right of the grid: the party roster on top, the
  /// queue below.
  let panelX = columns * tile + 8
  let panelW = 320
  let rosterTop = 24
  let rosterRowH = 42
  let queueTop = 242

  /// Ceiling on ticks per rendered frame. Without it a slow frame becomes a
  /// longer frame, which is the classic spiral. Falling behind in real time is
  /// correct; dropping Ticks would break determinism (ADR-0001).
  let maxTicksPerFrame = 4

  let mutable spriteBatch: SpriteBatch = null
  let mutable pixel: Texture2D = null
  let mutable disc: Texture2D = null
  let mutable fontTex: Texture2D = null

  /// Art sliced from the tileset by tools/slice_tileset.py, keyed by file name.
  /// A missing file is just absent: drawing falls back to the plain shapes.
  let mutable textures: Map<string, Texture2D> = Map.empty

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
  let mutable pendingAction: PendingAction option = None
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

  /// The camera is a window on the grid: a Scarlet Monastery wing is a hundred
  /// tiles tall and the view is thirteen, so the map has to scroll. It centres on
  /// the selection, else on the Warrior, and clamps so the grid's edge never
  /// shows as void.
  let mutable camX = 0
  let mutable camY = 0

  let followCamera () =
    let target =
      selected
      |> Option.bind (fun id -> SimState.tryEntity id world)
      |> Option.orElse (
        world.Entities
        |> List.tryFind (fun e -> e.Faction = Party && e.Name = "Warrior")
      )

    match target with
    | Some e ->
      camX <- max 0 (min (world.Grid.Width - columns) (e.Pos.X - columns / 2))
      camY <- max 0 (min (world.Grid.Height - rows) (e.Pos.Y - rows / 2))
    | None -> ()

  /// An entity's position on screen, in tiles, once the camera is applied.
  let screenAt (e: Entity) =
    let fx, fy = drawnAt e
    (fx - float camX, fy - float camY)

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

  /// Every PNG under Assets/, keyed by its path below Assets/ without extension
  /// ("sprites/classes/warrior"). Bare file names would collide: fifty of them
  /// are reused across dungeons and the Party, and the map would keep whichever
  /// one the filesystem happened to list last.
  let loadTextures () : Map<string, Texture2D> =
    let root = IO.Path.Combine(AppContext.BaseDirectory, "Assets")

    if not (IO.Directory.Exists root) then
      Map.empty
    else
      IO.Directory.GetFiles(root, "*.png", IO.SearchOption.AllDirectories)
      |> Array.map (fun path ->
        let tex = Texture2D.FromFile(this.GraphicsDevice, path)
        // PNGs hold straight alpha; SpriteBatch's default blend expects
        // premultiplied, or keyed edges draw as bright fringes.
        let data = Array.zeroCreate<Color> (tex.Width * tex.Height)
        tex.GetData data

        for i in 0 .. data.Length - 1 do
          let c = data.[i]
          let a = int c.A
          data.[i] <- Color(int c.R * a / 255, int c.G * a / 255, int c.B * a / 255, a)

        tex.SetData data

        let key =
          path.Substring(root.Length + 1).Replace('\\', '/').Replace(".png", "")

        key, tex)
      |> Map.ofArray

  /// Creature art for the dungeon in play, by creature name. A sheet names its
  /// creatures by slot (mob-1-1 ...) and carries no creature names, so each
  /// distinct name takes a slot in a stable order: the same creature always
  /// wears the same art within a run.
  let mutable creatureArt: Map<string, string> = Map.empty

  let bindCreatureArt (dungeon: string) (w: World) : Map<string, string> =
    let slots =
      textures
      |> Map.toList
      |> List.map fst
      |> List.filter (fun k -> k.StartsWith(dungeon + "/mob-"))
      |> List.sort

    let names =
      w.Entities
      |> List.filter (fun e -> e.Faction <> Party)
      |> List.map (fun e -> e.Name)
      |> List.distinct
      |> List.sort

    if slots.IsEmpty then
      Map.empty
    else
      names |> List.mapi (fun i n -> n, slots.[i % slots.Length]) |> Map.ofList

  /// Which sliced sprite stands for an entity. Serpent Form swaps the caster
  /// for the snake; names with no art fall back to a disc.
  let spriteKey (e: Entity) =
    let named =
      match e.Name with
      | "Warrior" -> "sprites/classes/warrior"
      | "Priest" -> "sprites/classes/priest"
      | "Rogue" -> "sprites/classes/rogue"
      | "Mage" -> "sprites/classes/mage"
      | "Druid"
      | "Naralex" -> "sprites/classes/druid"
      | "Paladin" -> "sprites/classes/paladin"
      | "Hunter" -> "sprites/classes/hunter"
      | "Warlock" -> "sprites/classes/warlock"
      | "Shaman" -> "sprites/classes/shaman"
      | "Raptor" -> (let (EntityId i) = e.Id in if i % 2 = 0 then "sprites/raptor0" else "sprites/raptor1")
      | "Druid of the Fang" -> "sprites/fang_druid"
      | "Lady Anacondra" -> "sprites/anacondra"
      | "Lord Cobrahn" -> "sprites/cobra_lord"
      | "Lord Pythas" -> "sprites/pythas"
      | "Lord Serpentis" -> "sprites/serpentis"
      | "Mutanus the Devourer" -> "sprites/mutanus"
      | n when n.Contains "Turtle" -> "sprites/deviate_turtle"
      | _ -> ""

    if SimState.isShifted e then
      "sprites/serpent"
    elif named <> "" then
      named
    else
      creatureArt.TryFind e.Name |> Option.defaultValue ""

  /// Where each entity's art was drawn last frame. A walk to the left turns a
  /// sprite even while it holds a target standing behind it.
  let mutable lastSeenX: Map<EntityId, float> = Map.empty

  /// The art faces right; flip it to face the way it is walking, else its
  /// target, else the default (the party faces right, hostiles face left).
  let facesLeft (e: Entity) =
    let nowX, _ = drawnAt e

    match lastSeenX.TryFind e.Id with
    | Some was when abs (nowX - was) > 0.02 -> nowX < was
    | _ ->
      match e.Target |> Option.bind (fun t -> SimState.tryEntity t world) with
      | Some t when t.Pos.X <> e.Pos.X -> t.Pos.X < e.Pos.X
      | _ -> e.Faction <> Party

  /// The rectangle an entity's art actually covers: a sprite is taller than its
  /// tile and stands on it, while the disc fallback sits lower. Drawing and
  /// hit-testing both measure from here, so a click lands on what is on screen.
  let artRect (e: Entity) =
    let fx, fy = screenAt e
    let cx = int (fx * float tile + float tile / 2.0)
    let feet = int (fy * float tile) + tile - 3

    match textures.TryFind(spriteKey e) with
    | Some _ -> Rectangle(cx - spriteSize / 2, feet - spriteSize, spriteSize, spriteSize)
    | None ->
      let size = if e.Faction = Party then 22 else 20
      Rectangle(cx - size / 2, feet - size - 6, size, size)

  /// A stable per-tile number, so floor variety and decorations hold still.
  let tileHash (x: int) (y: int) = ((x * 73856093) ^^^ (y * 19349663)) &&& 0x7fffffff

  /// Fill `area` with the dark cobble texture, then a mossy two-tone rim.
  let panel (area: Rectangle) =
    match textures.TryFind "tiles/panel" with
    | Some t ->
      let mutable y = area.Y

      while y < area.Bottom do
        let h = min t.Height (area.Bottom - y)
        let mutable x = area.X

        while x < area.Right do
          let w = min t.Width (area.Right - x)
          spriteBatch.Draw(t, Rectangle(x, y, w, h), Nullable(Rectangle(0, 0, w, h)), Color(92, 92, 100))
          x <- x + w

        y <- y + h
    | None -> spriteBatch.Draw(pixel, area, Color(18, 20, 28))

    let edge (colour: Color) (inset: int) =
      let r = Rectangle(area.X + inset, area.Y + inset, area.Width - 2 * inset, area.Height - 2 * inset)
      spriteBatch.Draw(pixel, Rectangle(r.X, r.Y, r.Width, 1), colour)
      spriteBatch.Draw(pixel, Rectangle(r.X, r.Bottom - 1, r.Width, 1), colour)
      spriteBatch.Draw(pixel, Rectangle(r.X, r.Y, 1, r.Height), colour)
      spriteBatch.Draw(pixel, Rectangle(r.Right - 1, r.Y, 1, r.Height), colour)

    edge (Color(8, 8, 10)) 0
    edge (Color(78, 112, 58)) 1
    edge (Color(30, 40, 28)) 2

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

  /// Text with a 1px drop shadow, for labels over the textured board.
  let drawTextShadow (text: string) (x: int) (y: int) (colour: Color) =
    drawText text (x + 1) (y + 1) (Color.Black * 0.85f)
    drawText text x y colour

  /// Break text into lines of at most `maxChars`, on word boundaries.
  let wrapWords (maxChars: int) (text: string) : string list =
    let words = text.Split([| ' ' |], StringSplitOptions.RemoveEmptyEntries)
    let lines = ResizeArray<string>()
    let mutable current = ""

    for w in words do
      let candidate = if current = "" then w else current + " " + w

      if candidate.Length <= maxChars then
        current <- candidate
      else
        if current <> "" then lines.Add current
        current <- w

    if current <> "" then lines.Add current
    Seq.toList lines

  /// Draw `text` wrapped at word boundaries to fit `maxWidth` pixels.
  let drawTextWrapped (text: string) (x: int) (y: int) (maxWidth: int) (colour: Color) =
    let maxChars = max 1 (maxWidth / cellW)

    wrapWords maxChars text
    |> List.iteri (fun i line -> drawText line x (y + i * (cellH + 2)) colour)

  /// The alive entity under a click: the union of the tile it stands on and the
  /// art drawn above it, so the overhanging head is reachable without taking the
  /// click away from the tile below.
  let entityAt (mx: int) (my: int) =
    let tileRect (e: Entity) =
      let fx, fy = screenAt e
      Rectangle(int (fx * float tile), int (fy * float tile), tile, tile)

    world.Entities
    |> List.tryFind (fun e ->
      SimState.alive e && (Rectangle.Union(tileRect e, artRect e)).Contains(mx, my))

  let nameOf (id: EntityId) =
    SimState.tryEntity id world |> Option.map (fun e -> e.Name) |> Option.defaultValue "?"

  let shorten (maxChars: int) (text: string) =
    if text.Length <= maxChars then text
    else text.Substring(0, max 0 (maxChars - 2)) + ".."

  let actorOf (kind: CommandKind) : EntityId =
    match kind with
    | UseAbility(actor, _, _) -> actor
    | MoveTo(actor, _) -> actor
    | Stop actor -> actor

  /// A queued order as (what it is, who it is on).
  let describeQueued (kind: CommandKind) : string * string =
    match kind with
    | UseAbility(actor, ability, target) -> (sprintf "%s %s" (nameOf actor) ability, sprintf "on %s" (nameOf target))
    | MoveTo(actor, dest) -> (sprintf "%s move" (nameOf actor), sprintf "to %d,%d" dest.X dest.Y)
    | Stop actor -> (sprintf "%s stop" (nameOf actor), "")

  /// The rows shown: the selected member's queued orders only, or everyone's
  /// when nothing is selected. Each entry is its rectangle and its index in
  /// `pending`, so a click cancels the right one.
  let queueRows () =
    let rowH = 34
    let visible = max 1 ((uiTop - queueTop) / rowH)

    pending
    |> Seq.indexed
    |> Seq.filter (fun (_, kind) -> selected = None || Some(actorOf kind) = selected)
    |> Seq.truncate visible
    |> Seq.mapi (fun row (i, _) -> Rectangle(panelX + 8, queueTop + row * rowH, panelW - 16, rowH - 4), i)
    |> Seq.toList

  /// What the selected member does with no order queued: swing at an engaged
  /// enemy in reach, which is what "auto-attack when attacked" means.
  let defaultAction (aid: EntityId) : string =
    match SimState.tryEntity aid world with
    | Some e ->
      match e.AutoAttack with
      | Some attack ->
        SimState.aliveHostiles world
        |> List.filter (fun t -> t.Engaged && SimState.canReach attack.Range e t world)
        |> List.sortBy (fun t -> Pos.chebyshev e.Pos t.Pos)
        |> List.tryHead
        |> Option.map (fun t -> sprintf "auto-attacks %s" t.Name)
        |> Option.defaultValue "idle, no target in reach"
      | None -> "no auto-attack"
    | None -> "no orders"

  /// The buttons for this frame: the global controls, then the selected
  /// member's control toggle, Move and abilities — the abilities filed under
  /// what they do, one heading per group. Wraps onto rows.
  let uiButtons () =
    let panelW = this.GraphicsDevice.Viewport.Width
    let mutable x = 8
    let mutable row = 0
    let y0 = uiTop + 6
    let rowH = 26

    let newRow () =
      x <- 8
      row <- row + 1

    let add label action =
      let w = measure label + 12

      if x + w > panelW - 8 then
        newRow ()

      let b = Rectangle(x, y0 + row * rowH, w, 22), label, action
      x <- x + w + 6
      b

    let pauseLabel = if ticksPerSecond > 0.0 then "Pause" else "Resume"
    let globals = [ add pauseLabel TogglePause; add "Step" StepOnce; add "Clear" Deselect ]

    match selected with
    | None -> globals
    | Some aid ->
      match pendingAction with
      | Some _ -> globals @ [ add "Cancel" CancelPending ]
      | None ->
        match SimState.tryEntity aid world with
        | Some e ->
          let controlLabel = if manual.Contains aid then "Return to AI" else "Take control"

          let ordered =
            [ AbilityGroup.Damage, "DAMAGE"
              AbilityGroup.DamageOverTime, "DAMAGE OVER TIME"
              AbilityGroup.Heal, "HEAL"
              AbilityGroup.HealOverTime, "HEAL OVER TIME"
              AbilityGroup.Defence, "DEFENCE"
              AbilityGroup.Utility, "UTILITY" ]

          // The control toggle and Move lead, then one row per group of
          // abilities that is not empty.
          newRow ()

          let controls =
            [ add controlLabel (ToggleControl aid); add "Move" (ChooseMove aid) ]

          let groups =
            ordered
            |> List.collect (fun (g, heading) ->
              e.Abilities
              |> List.filter (fun a -> Ability.group a = g)
              |> fun abilities ->
                if List.isEmpty abilities then
                  []
                else
                  newRow ()

                  add heading Heading
                  :: (abilities |> List.map (fun a -> add a.Name (ChooseAbility(aid, a.Name)))))

          globals @ controls @ groups
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

    let playerOrders kinds =
      kinds
      |> List.map (fun kind ->
        { ApplyAt = world.Tick + ticks 1
          Kind = kind })

    // The player's orders win; the script covers whoever was left idle this Tick
    // and stays off anyone the player has taken over.
    let commands = PartyAi.supplementExcept manual (playerOrders due) world

    world <- SimTick.step commands world

  do
    graphics.PreferredBackBufferWidth <- panelX + panelW
    graphics.PreferredBackBufferHeight <- uiTop + uiHeight + logHeight
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

  let menuItems = Menu.entries ()

  /// Every map the game can draw, for inspection: the generated Dungeon, both
  /// hand-traced Caverns levels, and each Scarlet wing. The art folder is the
  /// one a playable entry uses, so a browsed map wears the creatures it wears
  /// in play. The Scarlet floorplans are hand-traced and ignore the seed; the
  /// other three re-roll with it.
  let browseSources : (string * string * (uint64 -> World)) list =
    [ yield ("generated dungeon (72x40)", "", (fun seed -> Content.dungeon seed))
      yield ("Screaming Gully", "", (fun seed -> Content.gully seed))
      yield ("Wailing Caverns (stitched)", "", (fun seed -> Content.wailingCaverns seed))

      for wing in Content.scarletWings do
        yield
          ("Scarlet Monastery — " + wing.Name,
           "sprites/scarlet-monastery",
           (fun seed -> Content.scarletMonasteryAt wing.Name Content.Median seed)) ]

  /// A fresh run starts in the menu, so the dungeon and its level are chosen
  /// rather than assumed.
  let mutable menu = Menu.PickDungeon 0

  /// Keys already down last frame, so a held key does not scroll at frame rate.
  let mutable heldKeys = Set.empty<Keys>

  let pressed (keyboard: KeyboardState) (key: Keys) =
    keyboard.IsKeyDown key && not (heldKeys.Contains key)

  let rememberKeys (keyboard: KeyboardState) =
    heldKeys <-
      [ for key in Enum.GetValues<Keys>() do
          if keyboard.IsKeyDown key then key ]
      |> Set.ofList

  let startEntry (index: int) (band: int) =
    let entry = menuItems |> List.item index
    world <- entry.Start band
    creatureArt <- bindCreatureArt entry.Art world
    selected <- None
    pendingAction <- None
    pending.Clear()
    printedLogLines <- 0
    accumulator <- 0.0
    menu <- Menu.Closed
    Console.WriteLine(sprintf "[%s — level %d]" entry.Label (snd entry.Bands.[band]))

  /// Show a browsed map in the normal view: the renderer, camera and tileset all
  /// work off `world`, so browsing is a matter of putting a different World in
  /// it, not of drawing a second time.
  let showBrowse (source: int) (seed: uint64) =
    let source = max 0 (min (browseSources.Length - 1) source)
    let seed = max 1UL seed
    let _, art, start = browseSources |> List.item source
    world <- start seed
    creatureArt <- bindCreatureArt art world
    selected <- None
    pendingAction <- None
    pending.Clear()
    menu <- Menu.Browse(source, seed)

  let updateMenu (keyboard: KeyboardState) =
    let up = pressed keyboard Keys.Up || pressed keyboard Keys.W
    let down = pressed keyboard Keys.Down || pressed keyboard Keys.S

    let confirm =
      pressed keyboard Keys.Enter || pressed keyboard Keys.Space

    match menu with
    | Menu.Closed -> ()
    | Menu.PickDungeon i ->
      if down then menu <- Menu.PickDungeon((i + 1) % menuItems.Length)
      elif up then menu <- Menu.PickDungeon((i + menuItems.Length - 1) % menuItems.Length)
      elif confirm then menu <- Menu.PickDifficulty(i, 0)
      elif pressed keyboard Keys.B then showBrowse 0 1UL
      elif pressed keyboard Keys.Escape then menu <- Menu.Closed
    | Menu.PickDifficulty(i, band) ->
      let bands = menuItems.[i].Bands

      if down then menu <- Menu.PickDifficulty(i, (band + 1) % bands.Length)
      elif up then menu <- Menu.PickDifficulty(i, (band + bands.Length - 1) % bands.Length)
      elif confirm then startEntry i band
      elif pressed keyboard Keys.Escape then menu <- Menu.PickDungeon i
    | Menu.Browse(source, seed) ->
      // Left/right re-rolls the seed, up/down changes map, because the thing
      // being inspected changes most often with the seed.
      if pressed keyboard Keys.Right then showBrowse source (seed + 1UL)
      elif pressed keyboard Keys.Left then showBrowse source (max 1UL (seed - 1UL))
      elif down then showBrowse (source + 1) seed
      elif up then showBrowse (source - 1) seed
      elif confirm then
        // Take the browsed map into play, exactly as if it had been chosen.
        printedLogLines <- 0
        accumulator <- 0.0
        menu <- Menu.Closed
      elif pressed keyboard Keys.Escape then menu <- Menu.PickDungeon 0

  /// The menu is an opaque card: the dungeon list first, then the band, which is
  /// the difficulty.
  let drawMenu () =
    let width = min (columns * tile - 40) 560
    let height = 60 + 9 * 26
    let x = (columns * tile - width) / 2
    let y = 30

    spriteBatch.Draw(pixel, Rectangle(x, y, width, height), Color(10, 12, 16) * 0.96f)
    spriteBatch.Draw(pixel, Rectangle(x, y, width, 2), Color(190, 214, 160))

    let rows =
      match menu with
      | Menu.Closed -> []
      | Menu.Browse _ -> []
      | Menu.PickDungeon i ->
        [ yield "CHOOSE A DUNGEON", Color(190, 214, 160), false
          for index, entry in List.indexed menuItems do
            let detail =
              match entry.Bands with
              | [ _, level ] -> sprintf "level %d" level
              | _ -> "three bands"

            yield sprintf "  %s   (%s)" entry.Label detail, Color.White, index = i
          yield "", Color.White, false
          yield "Arrows move  -  Enter selects  -  B browses maps  -  Esc back", Color(140, 146, 130), false ]
      | Menu.PickDifficulty(i, band) ->
        let entry = menuItems.[i]

        [ yield entry.Label.ToUpperInvariant(), Color(190, 214, 160), false
          for index, (label, level) in List.indexed entry.Bands do
            yield sprintf "  %-8s level %d" label level, Color.White, index = band
          yield "", Color.White, false
          yield "The band is the difficulty: the lowest level it opens at up", Color(140, 146, 130), false
          yield "to the level of its highest boss.", Color(140, 146, 130), false
          yield "Arrows move  -  Enter starts  -  Esc back", Color(140, 146, 130), false ]

    rows
    |> List.iteri (fun row (text, colour, highlight) ->
      let rowY = y + 18 + row * 26

      if highlight then
        spriteBatch.Draw(pixel, Rectangle(x + 6, rowY - 4, width - 12, 24), Color(120, 200, 255) * 0.18f)

      drawTextShadow text (x + 14) rowY colour)

  /// Browsing draws the map, not a card over it: one strip at the top so the
  /// floorplan stays visible while the keys stay on screen.
  let drawBrowseHud (source: int) (seed: uint64) =
    let label, _, _ = browseSources |> List.item source

    let text =
      sprintf
        "%s    seed %d    map %d/%d        Left/Right seed    Up/Down map    Enter play    Esc back"
        label
        seed
        (source + 1)
        browseSources.Length

    spriteBatch.Draw(pixel, Rectangle(0, 0, columns * tile, 22), Color(10, 12, 16) * 0.92f)
    drawTextShadow (shorten (max 1 ((columns * tile - 16) / cellW)) text) 8 6 (Color(190, 214, 160))

  override _.Initialize() =
    // Set here rather than in the constructor: the window exists by now.
    this.Window.AllowUserResizing <- true
    base.Initialize()

  override _.LoadContent() =
    spriteBatch <- new SpriteBatch(this.GraphicsDevice)
    pixel <- new Texture2D(this.GraphicsDevice, 1, 1)
    pixel.SetData([| Color.White |])
    disc <- makeDisc 32
    fontTex <- buildFontTexture ()
    textures <- loadTextures ()

    // Opened straight into the browser from the command line. Textures have to
    // be loaded first: the creature art is bound from them.
    match browse with
    | Some (source, seed) -> showBrowse source seed
    | None -> ()

  member this.StepWorld (keyboard: KeyboardState) (gameTime: GameTime) =
    if keyboard.IsKeyDown(Keys.Escape) then
      menu <- Menu.PickDungeon 0

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

      match
        queueRows () |> List.tryFind (fun (r, _) -> r.Contains(mx, my)),
        uiButtons () |> List.tryFind (fun (r, _, _) -> r.Contains(mx, my))
        with
      | Some (_, i), _ -> pending.RemoveAt i
      | None, Some (_, _, action) ->
        match action with
        | TogglePause ->
          if ticksPerSecond > 0.0 then ticksPerSecond <- 0.0 else ticksPerSecond <- 20.0
        | StepOnce -> runOnce <- true
        | Deselect ->
          selected <- None
          pendingAction <- None
        | ChooseAbility(aid, ability) -> pendingAction <- Some(Targeting(aid, ability))
        | ChooseMove aid -> pendingAction <- Some(Moving aid)
        | ToggleControl aid ->
          if manual.Contains aid then
            manual <- manual.Remove aid
            Console.WriteLine(sprintf "[%s returned to the AI]" (nameOf aid))
          else
            manual <- manual.Add aid
            pendingAction <- None
            Console.WriteLine(sprintf "[%s under manual control]" (nameOf aid))
        | CancelPending -> pendingAction <- None
        | Heading -> ()
      | None, None ->
        match entityAt mx my with
        | Some e ->
          match pendingAction with
          | Some (Targeting(aid, ability)) ->
            if SimState.validTargets aid ability world |> List.exists (fun t -> t.Id = e.Id) then
              pending.Add(UseAbility(aid, ability, e.Id))

            pendingAction <- None
          | _ -> if e.Faction = Party then selected <- Some e.Id
        | None ->
          let tx, ty = mx / tile + camX, my / tile + camY

          if
            ty < world.Grid.Height
            && tx < world.Grid.Width
            && Grid.isFloor world.Grid { X = tx; Y = ty }
          then
            match pendingAction, selected with
            | Some (Moving aid), _ ->
              pending.Add(MoveTo(aid, { X = tx; Y = ty }))
              pendingAction <- None
            | None, Some aid -> pending.Add(MoveTo(aid, { X = tx; Y = ty }))
            | _ -> ()

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

  override _.Update(gameTime: GameTime) =
    let keyboard = Keyboard.GetState()

    if menu <> Menu.Closed then updateMenu keyboard else this.StepWorld keyboard gameTime

    rememberKeys keyboard
    base.Update(gameTime)

  override _.Draw(gameTime: GameTime) =
    this.GraphicsDevice.Clear(Color(10, 10, 14))
    followCamera ()

    let validIds =
      match pendingAction with
      | Some (Targeting(aid, ability)) ->
        SimState.validTargets aid ability world |> List.map (fun t -> t.Id) |> Set.ofList
      | _ -> Set.empty

    spriteBatch.Begin()

    let decorations = [|
      "sprites/mushroom_green"
      "sprites/mushroom_blue"
      "sprites/mushroom_teal"
      "sprites/fern"
    |]

    for y in camY .. min (camY + rows - 1) (world.Grid.Height - 1) do
      for x in camX .. min (camX + columns - 1) (world.Grid.Width - 1) do
        let p = { X = x; Y = y }
        let dest = Rectangle((x - camX) * tile, (y - camY) * tile, tile, tile)
        let h = tileHash x y

        if Grid.isWall world.Grid p then
          // Rock with no floor beside it is deep inside the wall: shade it so
          // the cave edge reads.
          let exposed =
            [ -1, 0; 1, 0; 0, -1; 0, 1 ]
            |> List.exists (fun (dx, dy) -> Grid.isFloor world.Grid { X = x + dx; Y = y + dy })

          match textures.TryFind "tiles/wall" with
          | Some t -> spriteBatch.Draw(t, dest, (if exposed then Color(200, 200, 205) else Color(80, 80, 92)))
          | None -> spriteBatch.Draw(pixel, Rectangle(dest.X, dest.Y, tile - 1, tile - 1), Color(46, 44, 40))
        else
          match textures.TryFind(if h % 3 = 0 then "tiles/floor1" else "tiles/floor0") with
          | Some t ->
            // Mirroring by the tile hash hides the repeat.
            let flip = if (h >>> 3) % 2 = 0 then SpriteEffects.None else SpriteEffects.FlipHorizontally
            spriteBatch.Draw(t, dest, Nullable(), Color(190, 190, 190), 0.0f, Vector2.Zero, flip, 0.0f)
          | None -> spriteBatch.Draw(pixel, Rectangle(dest.X, dest.Y, tile - 1, tile - 1), Color(24, 26, 34))

          if (h >>> 5) % 100 < 7 then
            match textures.TryFind decorations.[(h >>> 12) % decorations.Length] with
            | Some d ->
              let ox = (h >>> 16) % (tile - d.Width + 1)
              spriteBatch.Draw(d, Rectangle(dest.X + ox, dest.Bottom - d.Height - 2, d.Width, d.Height), Color.White * 0.9f)
            | None -> ()

    // Back to front: the dead first, then by row, so a lower sprite overlaps
    // the one standing behind it.
    let drawOrder =
      world.Entities |> List.sortBy (fun e -> SimState.alive e, snd (drawnAt e))

    let mouse = Mouse.GetState()
    let hover = entityAt mouse.X mouse.Y |> Option.map (fun e -> e.Id)

    for e in drawOrder do
      let fx, fy = screenAt e

      let cx = int (fx * float tile + float tile / 2.0)
      let feet = int (fy * float tile) + tile - 3
      let top = feet - spriteSize
      let art = textures.TryFind(spriteKey e)
      let flip = if facesLeft e then SpriteEffects.FlipHorizontally else SpriteEffects.None
      lastSeenX <- Map.add e.Id fx lastSeenX
      let box = artRect e

      let drawArt (tint: Color) =
        match art with
        | Some t -> spriteBatch.Draw(t, box, Nullable(), tint, 0.0f, Vector2.Zero, flip, 0.0f)
        | None -> spriteBatch.Draw(disc, box, entityColour e)

      if SimState.alive e then
        spriteBatch.Draw(disc, Rectangle(cx - 15, feet - 5, 30, 10), Color.Black * 0.5f)

        // A ring on the ground for the selected member and for valid targets.
        let ring =
          match selected with
          | Some sid when sid = e.Id -> Some(Color(120, 200, 255))
          | _ when validIds.Contains e.Id -> Some(if e.Faction = Party then Color(110, 240, 150) else Color(240, 110, 110))
          | _ -> None

        ring |> Option.iter (fun c -> spriteBatch.Draw(disc, Rectangle(cx - 19, feet - 8, 38, 16), c * 0.75f))

        let tint =
          if SimState.isSleeping e then Color(170, 170, 110)
          else Color.White

        drawArt tint

        bar (cx - 16) (top - 4) 32 4 (float e.Health / float e.MaxHealth)
          (if e.Faction = Party then Color(90, 220, 110) else Color(220, 90, 90))

        if e.MaxResource > 0 then
          bar (cx - 16) (top - 7) 32 2 (float e.Resource / float e.MaxResource) (Color(90, 150, 230))

        if e.Casting.IsSome then
          bar (cx - 16) (top - 11) 32 3 1.0 (Color(240, 200, 90))

        // What is on it: one chip per aura, so a slept, stunned or sundered
        // enemy reads at a glance. Green helps the holder, amber hurts it.
        let auras = e.Auras

        if not auras.IsEmpty then
          let chipW = 6
          let span = auras.Length * chipW + (auras.Length - 1) * 2

          auras
          |> List.iteri (fun i a ->
            let tint =
              if Aura.isHelpful a then Color(110, 240, 150) else Color(235, 165, 70)

            spriteBatch.Draw(pixel, Rectangle(cx - span / 2 + i * (chipW + 2), top - 16, chipW, 4), tint))

        if e.Faction = Party then
          drawTextShadow e.Name (cx - measure e.Name / 2) (feet + 2) (Color(220, 220, 230))

        // The chips are a tally, not a name: spell the auras out for the one
        // under the mouse, which is the only entity they cannot be misread on.
        if hover = Some e.Id && not auras.IsEmpty then
          let text =
            auras
            |> List.map (fun a -> sprintf "%s %ds" (Aura.label a) (Aura.remainingSeconds a))
            |> String.concat "  "

          let y = if e.Faction = Party then feet + 18 else feet + 2
          drawTextShadow text (cx - measure text / 2) y (Color(255, 235, 180))
      else
        drawArt (Color(70, 70, 70) * 0.55f)

    // The side panel: the party's health and mana, then the queue.
    panel (Rectangle(panelX, 0, panelW, uiTop))
    let m = mouse
    let rowChars = max 1 ((panelW - 24) / cellW)

    drawTextShadow "PARTY" (panelX + 8) 8 (Color(190, 214, 160))
    let party = world.Entities |> List.filter (fun e -> e.Faction = Party)
    let portrait = rosterRowH - 6
    let barX = panelX + 8 + portrait + 6
    let barW = 84
    let textX = barX + barW + 6
    let nameChars = max 1 ((panelX + panelW - barX - 8) / cellW)

    for i, e in party |> List.indexed do
      let y = rosterTop + i * rosterRowH

      if selected = Some e.Id then
        spriteBatch.Draw(pixel, Rectangle(panelX + 4, y - 2, panelW - 8, rosterRowH - 2), Color(120, 200, 255) * 0.18f)

      let frame = Rectangle(panelX + 8, y, portrait, portrait)
      spriteBatch.Draw(pixel, frame, Color(78, 112, 58))
      spriteBatch.Draw(pixel, Rectangle(frame.X + 1, frame.Y + 1, frame.Width - 2, frame.Height - 2), Color(14, 16, 14))

      match textures.TryFind(spriteKey e) with
      | Some t ->
        let tint = if SimState.alive e then Color.White else Color(80, 80, 80)
        // The sprite's upper part is the face; crop to it for a bust portrait.
        spriteBatch.Draw(t, Rectangle(frame.X + 1, frame.Y + 1, frame.Width - 2, frame.Height - 2), Nullable(Rectangle(8, 0, 32, 32)), tint)
      | None -> spriteBatch.Draw(disc, Rectangle(frame.X + 6, frame.Y + 6, portrait - 12, portrait - 12), entityColour e)

      drawTextShadow (shorten nameChars e.Name) barX (y + 1) (Color(224, 224, 232))
      let hpPct = if e.MaxHealth > 0 then e.Health * 100 / e.MaxHealth else 0
      bar barX (y + 15) barW 8 (float e.Health / float (max 1 e.MaxHealth)) (Color(90, 220, 110))
      drawText (sprintf "%d/%d %d%%" e.Health e.MaxHealth hpPct) textX (y + 13) (Color(210, 240, 210))
      let mpPct = if e.MaxResource > 0 then e.Resource * 100 / e.MaxResource else 0
      bar barX (y + 27) barW 8 (float e.Resource / float (max 1 e.MaxResource)) (Color(90, 150, 230))
      drawText (sprintf "%d/%d %d%%" e.Resource e.MaxResource mpPct) textX (y + 25) (Color(210, 220, 245))

    let title =
      match selected with
      | Some aid -> sprintf "queue %s" (nameOf aid)
      | None -> "queue all"

    drawTextShadow (shorten rowChars title) (panelX + 8) (queueTop - 16) (Color(190, 214, 160))
    let rows = queueRows ()

    for (rect, i) in rows do
      let hover = rect.Contains(m.X, m.Y)
      spriteBatch.Draw(pixel, rect, if hover then Color(110, 52, 48) * 0.9f else Color(12, 14, 12) * 0.75f)
      spriteBatch.Draw(pixel, Rectangle(rect.X, rect.Y, 2, rect.Height), if hover then Color(220, 110, 90) else Color(78, 112, 58))
      let line1, line2 = describeQueued pending.[i]
      drawText (shorten rowChars line1) (rect.X + 6) (rect.Y + 3) (Color(224, 224, 232))

      if line2 <> "" then
        drawText (shorten rowChars line2) (rect.X + 6) (rect.Y + 17) (Color(150, 150, 165))

    if List.isEmpty rows then
      let text =
        match selected with
        | Some aid -> defaultAction aid
        | None -> "nothing queued"

      drawTextWrapped text (panelX + 12) (queueTop + 2) (panelW - 24) (Color(130, 136, 128))

    // The clickable strip.
    let panelW = this.GraphicsDevice.Viewport.Width
    panel (Rectangle(0, uiTop, panelW, uiHeight))

    for (rect, label, action) in uiButtons () do
      let hover = action <> Heading && rect.Contains(m.X, m.Y)

      if action = Heading then
        drawText label rect.X (rect.Y + 4) (Color(150, 176, 130))
      else
        spriteBatch.Draw(pixel, rect, if hover then Color(120, 160, 84) else Color(60, 84, 46))

        spriteBatch.Draw(
          pixel,
          Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2),
          if hover then Color(58, 78, 44) else Color(26, 30, 24)
        )

        spriteBatch.Draw(pixel, Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), Color.White * 0.08f)
        drawTextShadow label (rect.X + 6) (rect.Y + 4) (if hover then Color.White else Color(214, 220, 200))

    let hint =
      match selected, pendingAction with
      | Some _, Some (Targeting(_, ability)) -> sprintf "Choose a target for %s - click a highlighted unit" ability
      | Some aid, Some (Moving _) -> sprintf "%s - click a destination tile" (nameOf aid)
      | Some aid, None when manual.Contains aid ->
        sprintf "%s under manual control - it will not act on its own until handed back" (nameOf aid)
      | Some aid, None -> sprintf "%s selected - Move, an ability, or click a tile to walk" (nameOf aid)
      | None, _ -> "Click a party member to command them"

    drawTextWrapped hint 8 (uiTop + uiHeight - 20) (panelW - 16) (Color(160, 160, 170))

    // The combat log. `world.Log` is newest-first, so reverse it to read the
    // way a log should: oldest visible line at the top, newest at the bottom.
    let logTop = uiTop + uiHeight
    let bodyTop = logTop + 24
    let lineH = cellH + 2
    let lines = max 1 ((this.GraphicsDevice.Viewport.Height - bodyTop - 6) / lineH)
    let logChars = max 1 ((panelW - 16) / cellW)

    panel (Rectangle(0, logTop, panelW, this.GraphicsDevice.Viewport.Height - logTop))
    drawTextShadow "COMBAT LOG" 8 (logTop + 6) (Color(190, 214, 160))

    world.Log
    |> List.truncate lines
    |> List.rev
    |> List.iteri (fun i line ->
      drawText (shorten logChars line) 8 (bodyTop + i * lineH) (Color(198, 198, 208)))

    if List.isEmpty world.Log then
      drawText "nothing has happened yet" 8 bodyTop (Color(120, 120, 138))

    if menu <> Menu.Closed then
      match menu with
      | Menu.Browse(source, seed) -> drawBrowseHud source seed
      | _ -> drawMenu ()

    spriteBatch.End()
    base.Draw(gameTime)

module Program =

  /// `--browse [map] [seed]` opens the map browser at that map and seed.
  let private browseArgs (args: string list) : (int * uint64) option =
    match args |> List.tryFindIndex ((=) "--browse") with
    | None -> None
    | Some i ->
      let intAt j fallback =
        args
        |> List.tryItem (i + j)
        |> Option.bind (fun s ->
          match System.Int32.TryParse s with
          | true, v -> Some v
          | _ -> None)
        |> Option.defaultValue fallback

      let seed =
        args
        |> List.tryItem (i + 2)
        |> Option.bind (fun s ->
          match System.UInt64.TryParse s with
          | true, v -> Some v
          | _ -> None)
        |> Option.defaultValue 1UL

      Some(intAt 1 0, seed)

  [<EntryPoint>]
  let main (argv: string[]) =
    let args = argv |> Array.toList
    use game = new RoguelikeGame(?browse = browseArgs args)

    Console.WriteLine "WowRoguelike — Screaming Gully (Wailing Caverns, first encounter)"
    Console.WriteLine ""
    Console.WriteLine "Mouse: click a party member, then an ability button, then a target."
    Console.WriteLine "       Click an empty floor tile to move the selected member."
    Console.WriteLine "Typed commands still work:"
    Console.WriteLine "  <actor> move <x> <y>          walk"
    Console.WriteLine "  <actor> <target> <abilitykey> use an ability, e.g. `3 8 kick`"
    Console.WriteLine "  pause | resume | step | speed <n> | party | state | targets <actor> <abilitykey>"
    Console.WriteLine ""
    Console.WriteLine "  --browse [map] [seed]         open the map browser: Left/Right seeds, Up/Down maps"

    game.Run()
    0
