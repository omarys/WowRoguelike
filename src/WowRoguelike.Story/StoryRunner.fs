namespace WowRoguelike.Story

open System
open System.IO
open System.Reflection
open Ink.Runtime

/// What a story hands back at the points the host must act. Text lines are
/// batched until one of the terminal events below.
type StoryEvent =
  | Text of string
  | Encounter of string
  | QuestComplete of string
  | Choice of (int * string) list
  | End

/// A thin wrapper over Ink.Runtime.Story that advances only until the next
/// thing the host drives, instead of reading the whole story through in one
/// go. The two external functions the .ink script calls are bound here:
///
///   start_encounter(boss_id)   -> Encounter boss_id
///   complete_quest(quest_id)   -> QuestComplete quest_id
///
/// The Story object is a mutable interpreter and does not belong in the
/// deterministic Core; this project sits beside it, consumed by the host.
type StoryRunner(inkJson: string) =
  let story = Story(inkJson)
  let mutable pendingEncounter: string option = None
  let mutable pendingQuest: string option = None

  /// A line a handoff stole from the *next* phase: `story.Continue()` fires the
  /// external function and, in the same call, carries on through the divert to
  /// return the first line of what follows. That line must not be shown before
  /// the handoff, so it is held here and surfaced on the next Continue().
  let mutable heldText: string option = None

  do
    story.BindExternalFunction(
      "start_encounter",
      Func<string, obj>(fun id ->
        pendingEncounter <- Some id
        box ()))

    story.BindExternalFunction(
      "complete_quest",
      Func<string, obj>(fun id ->
        pendingQuest <- Some id
        box ()))

  member _.CanContinue = story.canContinue

  member _.CurrentChoices : (int * string) list =
    story.currentChoices |> Seq.mapi (fun i c -> i, c.text) |> Seq.toList

  /// Advance, collecting every line of text and stopping at the first point
  /// the host must handle: an encounter, a quest flag, a choice, or the end.
  member _.Continue() : StoryEvent list =
    let events = ResizeArray<StoryEvent>()

    match heldText with
    | Some t ->
      heldText <- None
      events.Add(StoryEvent.Text t)
    | None -> ()

    while
      story.canContinue
      && pendingEncounter.IsNone
      && pendingQuest.IsNone
      && story.currentChoices.Count = 0
      do
      let text = story.Continue().Trim()

      if pendingEncounter.IsSome || pendingQuest.IsSome then
        if not (String.IsNullOrWhiteSpace text) then
          heldText <- Some text
      elif not (String.IsNullOrWhiteSpace text) then
        events.Add(StoryEvent.Text text)

    match pendingEncounter with
    | Some id ->
      pendingEncounter <- None
      events.Add(StoryEvent.Encounter id)
    | None ->
      match pendingQuest with
      | Some id ->
        pendingQuest <- None
        events.Add(StoryEvent.QuestComplete id)
      | None ->
        if story.currentChoices.Count > 0 then
          events.Add(StoryEvent.Choice(story.currentChoices |> Seq.mapi (fun i c -> i, c.text) |> Seq.toList))
        elif not story.canContinue then
          events.Add(StoryEvent.End)

    Seq.toList events

  member _.Choose(index: int) =
    if index >= 0 && index < story.currentChoices.Count then
      story.ChooseChoiceIndex index
    else
      invalidArg "index" (sprintf "choice %d out of range (0..%d)" index (story.currentChoices.Count - 1))

  /// Load a compiled .ink.json from this assembly's embedded resources, else
  /// from disk (so the harness works whether or not resources were embedded).
  static member LoadJson(fileName: string) : string =
    let asm = Assembly.GetExecutingAssembly()

    let resource =
      asm.GetManifestResourceNames()
      |> Array.tryFind (fun n -> n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))

    match resource with
    | Some res ->
      use stream = asm.GetManifestResourceStream res
      use reader = new StreamReader(stream)
      reader.ReadToEnd()
    | None ->
      let candidates =
        [ fileName
          Path.Combine("Scripts", fileName)
          Path.Combine("src", "WowRoguelike.Story", "Scripts", fileName)
          Path.Combine("..", "..", "src", "WowRoguelike.Story", "Scripts", fileName) ]

      match candidates |> List.tryFind File.Exists with
      | Some p -> File.ReadAllText p
      | None -> failwithf "Could not locate %s as an embedded resource or file" fileName
