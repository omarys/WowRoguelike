namespace WowRoguelike.Core

/// A binary min-heap, array-backed, hand-rolled.
///
/// This exists to be measured against `System.Collections.Generic.PriorityQueue`
/// (Q2c: implement twice, benchmark both). The hand-rolled version is what A*
/// actually uses, so its cost is on the critical path rather than theoretical.
///
/// Priorities and values live in two parallel arrays rather than one array of
/// pairs. A single `ResizeArray<'p * 'v>` allocates a reference tuple on every
/// push and copies tuples on every swap, which measured 12.9x slower than the
/// BCL heap at n=100000 — more than the whole Map/Set removal that preceded it.
type MinHeap<'p, 'v when 'p: comparison>() =

  let priorities = ResizeArray<'p>()
  let values = ResizeArray<'v>()

  /// Both arrays must move together, or priorities stop describing the values.
  let swap (i: int) (j: int) =
    let priority = priorities.[i]
    priorities.[i] <- priorities.[j]
    priorities.[j] <- priority
    let value = values.[i]
    values.[i] <- values.[j]
    values.[j] <- value

  member _.Count = priorities.Count
  member _.IsEmpty = priorities.Count = 0

  member _.Push(priority: 'p, value: 'v) =
    priorities.Add priority
    values.Add value

    let mutable i = priorities.Count - 1
    let mutable climbing = true

    while climbing && i > 0 do
      let parent = (i - 1) / 2

      if priorities.[i] < priorities.[parent] then
        swap i parent
        i <- parent
      else
        climbing <- false

  member _.Pop() : ('p * 'v) option =
    if priorities.Count = 0 then
      None
    else
      let topPriority = priorities.[0]
      let topValue = values.[0]
      let last = priorities.Count - 1
      priorities.[0] <- priorities.[last]
      values.[0] <- values.[last]
      priorities.RemoveAt last
      values.RemoveAt last

      let mutable i = 0
      let mutable sinking = true

      while sinking do
        let left = 2 * i + 1
        let right = 2 * i + 2
        let mutable best = i

        if left < priorities.Count && priorities.[left] < priorities.[best] then
          best <- left

        if right < priorities.Count && priorities.[right] < priorities.[best] then
          best <- right

        if best = i then
          sinking <- false
        else
          swap i best
          i <- best

      Some(topPriority, topValue)
