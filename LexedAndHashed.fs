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

type StationDataFixed = {
    mutable Min : int
    mutable Max : int
    mutable Sum : int
    mutable Count : int
}

let private utf8 = System.Text.Encoding.UTF8

type ChunkProcessor(chunkPtr: nativeptr<byte>, chunkLength: int64) =
    let stations = Dictionary<int, StationDataFixed>(1024)
    let stationNames = Dictionary<int, string>(1024)
    member this.Length = chunkLength
    member this.Stations =
        stations
        |> Seq.map (fun kv -> stationNames.[kv.Key], kv.Value)
    member this.Run() =
        let mutable count: int64 = 0
        let mutable entry: StationDataFixed = {
            Min = 0
            Max = 0
            Sum = 0
            Count = 0
        }
        let mutable p = chunkPtr
        let mutable index: int64 = 0
        while index < chunkLength do
            // 1. Read and hash station name
            let mutable nameLength = 0
            let mutable nameHash = 0
            let mutable b = NativePtr.read p
            while b <> ';'B do
                nameHash <- nameHash * 311 + int b
                nameLength <- nameLength + 1
                b <- NativePtr.get p nameLength
            // 2. Read temperature
            let tempPtr = NativePtr.add p (nameLength + 1)
            let mutable tempLength = 0
            b <- NativePtr.read tempPtr
            let isNeg = b = '-'B
            if isNeg then
                tempLength <- tempLength + 1
                b <- NativePtr.get tempPtr tempLength
            let mutable temp = 0
            while b <> '.'B do
                tempLength <- tempLength + 1
                temp <- temp * 10 + int (b - '0'B)
                b <- NativePtr.get tempPtr tempLength
            let dec = NativePtr.get tempPtr (tempLength + 1)
            tempLength <- tempLength + 2
            temp <- temp * 10 + int (dec - '0'B)
            if isNeg then
                temp <- -temp    
            // 3. Update station data
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
                stationNames.Add(nameHash, utf8.GetString (p, nameLength))
            count <- count + 1L
            // 4. Skip to next line
            let newlineOffset = nameLength + tempLength + 2
            p <- NativePtr.add p newlineOffset
            index <- index + int64 newlineOffset

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
        |> Seq.sortBy fst
    let mutable head = "{"
    for station in sortedStations do
        let name, e = station
        printf $"%s{head}%s{name}=%.1f{float e.Min * 0.1}/%.1f{float e.Sum * 0.1 / float e.Count}/%.1f{float e.Max * 0.1}"
        head <- ", "
    printfn "}"
