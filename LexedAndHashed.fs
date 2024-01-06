// 1 Billion Row Challenge in F#
// Lexer and Hashed Station Names

module LexedAndHashed

open System
open System.Buffers.Text
open System.Collections.Generic
open System.IO
open Microsoft.FSharp.NativeInterop

open Baseline

#nowarn "9"

let private utf8 = System.Text.Encoding.UTF8

type ChunkProcessor(chunkPtr: nativeptr<byte>, chunkLength: int64) =
    let stations = Dictionary<int, StationDataObject>()
    let stationNames = Dictionary<int, string>()
    member this.Stations = stations
    member this.StationNames = stationNames
    member this.Run() =
        let mutable count: int64 = 0
        let mutable printedCount: int64 = 0
        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        let mutable entry: StationDataObject = {
            Min = 0.0
            Max = 0.0
            Sum = 0.0
            Count = 0
        }
        let mutable p = chunkPtr
        let mutable index: int64 = 0
        while index < chunkLength do
            let mutable namePtr = p
            let mutable nameLength = 0
            let mutable nameHash = 0
            let mutable b = NativePtr.read namePtr
            while b <> ';'B do
                nameHash <- nameHash * 33 + int b
                nameLength <- nameLength + 1
                b <- NativePtr.get namePtr nameLength
    
            let mutable tempPtr = NativePtr.add p (nameLength + 1)
            let mutable tempLength = 0
            let mutable temp: double = 0.0
            b <- NativePtr.read tempPtr
            let isNeg = b = '-'B
            if isNeg then
                tempLength <- tempLength + 1
                b <- NativePtr.get tempPtr tempLength
            while b <> '.'B do
                tempLength <- tempLength + 1
                temp <- temp * 10.0 + double (b - '0'B)
                b <- NativePtr.get tempPtr tempLength
            let dec = NativePtr.get tempPtr (tempLength + 1)
            tempLength <- tempLength + 2
            temp <- temp + double (dec - '0'B) * 0.1
            if isNeg then
                temp <- -temp
    
            let newlineOffset = nameLength + tempLength + 2
            p <- NativePtr.add p newlineOffset
            index <- index + int64 newlineOffset
            
            // let debugName = utf8.GetString (namePtr, nameLength)
            // let debugTemp = utf8.GetString (tempPtr, tempLength)
            // printfn "%A = %A" debugName debugTemp
    
            // printfn "%s = %.1f" name temp
            if stations.TryGetValue(nameHash, &entry) then
                entry.Min <- min entry.Min temp
                entry.Max <- max entry.Max temp
                entry.Sum <- entry.Sum + temp
                entry.Count <- entry.Count + 1
            else
                stations.Add(nameHash, {
                    Min = temp
                    Max = temp
                    Sum = temp
                    Count = 1
                })
                stationNames.Add(nameHash, utf8.GetString (namePtr, nameLength))
            count <- count + 1L
                    
            if (count - printedCount) >= 50_000_000L then
                printedCount <- count
                let entriesPerSecond = (float count) / stopwatch.Elapsed.TotalSeconds
                let estimatedTotalTime = TimeSpan.FromSeconds (1.0e9 / entriesPerSecond)
                printfn "Processed %d lines (index=%A) (est %O)" count index estimatedTotalTime

let run (measurementsPath : string) =
    let mmap = MemoryMappedFiles.MemoryMappedFile.CreateFromFile(measurementsPath, FileMode.Open)
    let mmapA = mmap.CreateViewAccessor()
    let mutable filePtr: nativeptr<byte> = NativePtr.nullPtr<byte>
    mmapA.SafeMemoryMappedViewHandle.AcquirePointer(&filePtr)
    let fileLength = mmapA.Capacity
    let processor = ChunkProcessor(filePtr, fileLength)
    processor.Run()
    let sortedStations =
        processor.Stations
        |> Seq.map (fun (kv : KeyValuePair<int, StationDataObject>) -> processor.StationNames.[kv.Key], kv.Value)
        |> Seq.sortBy fst
    let mutable head = "{"
    for station in sortedStations do
        let name, e = station
        printf "%s%s=%.1f/%.1f/%.1f" head name e.Min (e.Sum / double e.Count) e.Max
        head <- ", "
    printfn "}"
