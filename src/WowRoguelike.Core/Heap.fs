namespace WowRoguelike.Core

/// A binary min-heap, array-backed, hand-rolled.
///
/// This exists to be measured against `System.Collections.Generic.PriorityQueue`
/// (Q2c: implement twice, benchmark both). The hand-rolled version is what A*
/// actually uses, so its cost is on the critical path rather than theoretical.
type MinHeap<'p, 'v when 'p: comparison>() =

  /// One slot per entry, no implicit ordering guarantees beyond the heap
  /// property. Rebuilt by sift-up on push and sift-down on pop, so both are
  /// O(log n) and nothing here scans.
  let items = ResizeArray<'p * 'v>()

  let swap (i: int) (j: int) =
    let t = items.[i]
    items.[i] <- items.[j]
    items.[j] <- t

  member _.Count = items.Count
  member _.IsEmpty = items.Count = 0

  member _.Push(priority: 'p, value: 'v) =
    items.Add(priority, value)
    let mutable i = items.Count - 1
    let mutable climbing = true

    while climbing && i > 0 do
      let parent = (i - 1) / 2

      if fst (items.[i]) < fst (items.[parent]) then
        swap i parent
        i <- parent
      else
        climbing <- false

  member _.Pop() : ('p * 'v) option =
    if items.Count = 0 then
      None
    else
      let top = items.[0]
      let last = items.Count - 1
      items.[0] <- items.[last]
      items.RemoveAt last

      let mutable i = 0
      let mutable sinking = true

      while sinking do
        let left = 2 * i + 1
        let right = 2 * i + 2
        let mutable best = i

        if left < items.Count && fst (items.[left]) < fst (items.[best]) then
          best <- left

        if right < items.Count && fst (items.[right]) < fst (items.[best]) then
          best <- right

        if best = i then
          sinking <- false
        else
          swap i best
          i <- best

      Some top
