namespace WowRoguelike.Core

open System
open System.Collections.Generic
open System.Runtime.CompilerServices

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

  let comparer = Comparer<'p>.Default
  let mutable priorities = Array.zeroCreate<'p> 16
  let mutable values = Array.zeroCreate<'v> 16
  let mutable count = 0

  let grow () =
    let nextCap = priorities.Length * 2
    let nextP = Array.zeroCreate<'p> nextCap
    let nextV = Array.zeroCreate<'v> nextCap
    Array.Copy(priorities, nextP, count)
    Array.Copy(values, nextV, count)
    priorities <- nextP
    values <- nextV

  member _.Count = count
  member _.IsEmpty = count = 0

  member _.Clear() =
    if count > 0 then
      if RuntimeHelpers.IsReferenceOrContainsReferences<'p>() then
        Array.Clear(priorities, 0, count)

      if RuntimeHelpers.IsReferenceOrContainsReferences<'v>() then
        Array.Clear(values, 0, count)

      count <- 0

  member _.Push(priority: 'p, value: 'v) =
    if count = priorities.Length then
      grow ()

    let mutable i = count
    count <- count + 1
    let ps = priorities
    let vs = values

    while i > 0 do
      let parent = (i - 1) >>> 1
      let parentP = ps.[parent]

      if comparer.Compare(priority, parentP) < 0 then
        ps.[i] <- parentP
        vs.[i] <- vs.[parent]
        i <- parent
      else
        ps.[i] <- priority
        vs.[i] <- value
        i <- -1

    if i = 0 then
      ps.[0] <- priority
      vs.[0] <- value

  member _.TryPop(priority: outref<'p>, value: outref<'v>) : bool =
    if count = 0 then
      false
    else
      let ps = priorities
      let vs = values
      priority <- ps.[0]
      value <- vs.[0]
      let last = count - 1
      count <- last

      if last > 0 then
        let moveP = ps.[last]
        let moveV = vs.[last]
        let mutable i = 0
        let mutable sinking = true

        while sinking do
          let left = (i <<< 1) + 1

          if left >= last then
            sinking <- false
          else
            let mutable best = i
            let mutable bestP = moveP
            let leftP = ps.[left]

            if comparer.Compare(leftP, bestP) < 0 then
              best <- left
              bestP <- leftP

            let right = left + 1

            if right < last then
              let rightP = ps.[right]

              if comparer.Compare(rightP, bestP) < 0 then
                best <- right

            if best = i then
              sinking <- false
            else
              ps.[i] <- ps.[best]
              vs.[i] <- vs.[best]
              i <- best

        ps.[i] <- moveP
        vs.[i] <- moveV

      true

  member this.Pop() : ('p * 'v) option =
    let mutable p = Unchecked.defaultof<'p>
    let mutable v = Unchecked.defaultof<'v>

    if this.TryPop(&p, &v) then
      Some(p, v)
    else
      None
