// 1 Billion Row Challenge in F#
// Lexer and Hashed Station Names

module Lexed

open System
open System.Buffers.Text
open System.Collections.Generic
open System.IO
open Microsoft.FSharp.NativeInterop

open Baseline

#nowarn "9"

let private utf8 = System.Text.Encoding.UTF8

type ParseState =
    | ReadingName = 0
    | ReadingTemperature = 1

let run (measurementsPath : string) =
    let mmap = MemoryMappedFiles.MemoryMappedFile.CreateFromFile(measurementsPath, FileMode.Open)
    let mmapA = mmap.CreateViewAccessor()
    let mutable filePtr: nativeptr<byte> = NativePtr.nullPtr<byte>
    mmapA.SafeMemoryMappedViewHandle.AcquirePointer(&filePtr)
    let fileLength = mmapA.Capacity
    let stations = Dictionary<string, StationData>()
    let mutable count: int64 = 0
    let mutable printedCount: int64 = 0
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    let mutable entry: StationData = {
        Min = 0.0
        Max = 0.0
        Sum = 0.0
        Count = 0
    }
    let mutable state = ParseState.ReadingName
    let mutable p = filePtr
    let mutable index: int64 = 0
    let mutable nameHash = 0
    let mutable namePtr = p
    let mutable nameLength = 0
    let mutable tempPtr = p
    let mutable tempLength = 0
    let mutable temp: double = 0.0
    while index < fileLength do
        let b = NativePtr.read p
        
        match state, b with
        | ParseState.ReadingName, ';'B ->
            p <- NativePtr.add p 1
            index <- index + 1L
            state <- ParseState.ReadingTemperature
            tempPtr <- p
            tempLength <- 0
        | ParseState.ReadingName, _ ->
            nameHash <- nameHash * 33 + int b
            nameLength <- nameLength + 1
            p <- NativePtr.add p 1
            index <- index + 1L
        | ParseState.ReadingTemperature, '\n'B ->
            let name = utf8.GetString (namePtr, nameLength)
            Utf8Parser.TryParse(ReadOnlySpan<byte>(NativePtr.toVoidPtr tempPtr, tempLength), &temp) |> ignore
            // printfn "%s = %.1f" name temp
            if stations.TryGetValue(name, &entry) then
                stations.[name] <- {
                    Min = min entry.Min temp
                    Max = max entry.Max temp
                    Sum = entry.Sum + temp
                    Count = entry.Count + 1
                }
            else
                stations.Add(name, {
                    Min = temp
                    Max = temp
                    Sum = temp
                    Count = 1
                })
            p <- NativePtr.add p 1
            index <- index + 1L
            count <- count + 1L
            state <- ParseState.ReadingName
            namePtr <- p
            nameHash <- 0
            nameLength <- 0
        | _, _ ->
            p <- NativePtr.add p 1
            index <- index + 1L
            tempLength <- tempLength + 1
                
        if (count - printedCount) >= 10_000_000L then
            printedCount <- count
            let entriesPerSecond = (float count) / stopwatch.Elapsed.TotalSeconds
            let estimatedTotalTime = TimeSpan.FromSeconds (1.0e9 / entriesPerSecond)
            printfn "Processed %d lines (index=%A) (est %O)" count index estimatedTotalTime
    let sortedStations =
        stations
        |> Seq.sortBy (fun (kv : KeyValuePair<string, StationData>) -> kv.Key)
    let mutable head = "{"
    for station in sortedStations do
        let e = station.Value
        printf "%s%s=%.1f/%.1f/%.1f" head station.Key e.Min (e.Sum / double e.Count) e.Max
        head <- ", "
    printfn "}"
