// 1 Billion Row Challenge in F#
// Baseline + MemoryMapped File using Pointers

module MemoryMapped

open System
open System.Buffers.Text
open System.Collections.Generic
open System.IO
open Microsoft.FSharp.NativeInterop

open Baseline

#nowarn "9"

let inline pbytesIndexOf (pbytes : nativeptr<byte>) (length: int64) (v : byte) : int64 =
    let mutable i: int64 = 0
    let mutable run = true
    while run && i < length do
        if NativePtr.get pbytes (int i) = v then
            run <- false
        else
            i <- i + 1L
    if run then -1 else i

let inline parseTemp (pbytes : nativeptr<byte>) : double =
    // Find the '.'
    let mutable doti: int = 0
    while NativePtr.get pbytes doti <> '.'B do
        doti <- doti + 1
    let endi = doti + 2
    let length = endi + 1
    let span = new ReadOnlySpan<byte>(NativePtr.toVoidPtr pbytes, int length)
    let mutable result: double = 0.0
    Utf8Parser.TryParse(span, &result) |> ignore
    result

let utf8 = System.Text.Encoding.UTF8

let run (measurementsPath : string) =
    let mmap = MemoryMappedFiles.MemoryMappedFile.CreateFromFile(measurementsPath, FileMode.Open)
    let mmapA = mmap.CreateViewAccessor()
    let mutable filePtr: nativeptr<byte> = NativePtr.nullPtr<byte>
    mmapA.SafeMemoryMappedViewHandle.AcquirePointer(&filePtr)
    let fileLength = mmapA.Capacity
    let stations = Dictionary<string, StationData>()
    let mutable count = 0
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    let mutable entry: StationData = {
        Min = 0.0
        Max = 0.0
        Sum = 0.0
        Count = 0
    }
    let mutable p = filePtr
    let mutable plength = fileLength
    while plength > 0 do
        match pbytesIndexOf p plength '\n'B with
        | -1L ->
            printfn "FAILED TO FIND NEWLINE"
            plength <- 0
        | newlineIndex when newlineIndex > 0 ->
            let lineLength: int64 = newlineIndex
            match pbytesIndexOf p lineLength ';'B with
            | -1L ->
                printfn "FAILED TO FIND SEMICOLON"
                plength <- 0
            | semicolonIndex ->
                let name = utf8.GetString(p, int semicolonIndex)
                let temp = parseTemp (NativePtr.add p (int semicolonIndex + 1))
                match stations.TryGetValue(name, &entry) with
                | true ->
                    stations.[name] <- {
                        Min = min entry.Min temp
                        Max = max entry.Max temp
                        Sum = entry.Sum + temp
                        Count = entry.Count + 1
                    }
                | false ->
                    stations.Add(name, {
                        Min = temp
                        Max = temp
                        Sum = temp
                        Count = 1
                    })
                count <- count + 1
                p <- NativePtr.add p (int newlineIndex + 1)
                plength <- plength - (newlineIndex + 1L)
        | _ ->
            plength <- 0
            
        if count > 0 && (count % 10000000) = 0 then
            let entriesPerSecond = (float count) / stopwatch.Elapsed.TotalSeconds
            let estimatedTotalTime = TimeSpan.FromSeconds (1.0e9 / entriesPerSecond)
            printfn "Processed %d lines (plength=%A) (est %O)" count plength estimatedTotalTime
    let sortedStations =
        stations
        |> Seq.sortBy (fun (kv : KeyValuePair<string, StationData>) -> kv.Key)
    let mutable head = "{"
    for station in sortedStations do
        let e = station.Value
        printf "%s%s=%.1f/%.1f/%.1f" head station.Key e.Min (e.Sum / double e.Count) e.Max
        head <- ", "
    printfn "}"
