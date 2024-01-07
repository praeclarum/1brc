module Multithreaded

open System
open System.Buffers.Text
open System.Collections.Generic
open System.IO
open Microsoft.FSharp.NativeInterop

open Baseline
open LexedAndHashed

#nowarn "9"

let chunkify (chunkPtr : nativeptr<byte>) (length : int64) : ResizeArray<ChunkProcessor> =
    let idealChunkLength = 256 * 1024 * 1024
    let chunks = ResizeArray<ChunkProcessor>()
    let mutable offset = 0L
    let mutable p = chunkPtr
    while offset < length do
        let mutable chunkLength = min (length - offset) (int64 idealChunkLength - 256L) |> int
        let mutable pNextChunk = NativePtr.add p chunkLength
        offset <- offset + int64 chunkLength
        // Find next '\n'
        while (offset < length) && (NativePtr.read pNextChunk <> '\n'B) do
            pNextChunk <- NativePtr.add pNextChunk 1
            chunkLength <- chunkLength + 1
            offset <- offset + 1L
        // Move beyond the '\n'
        if offset < length then
            pNextChunk <- NativePtr.add pNextChunk 1
            chunkLength <- chunkLength + 1
            offset <- offset + 1L
        // Output previous chunk
        chunks.Add(ChunkProcessor(p, chunkLength))
        p <- pNextChunk
    if true then
        let totalChunkLength = chunks |> Seq.sumBy (fun c -> c.Length)
        if totalChunkLength <> length then
            failwithf $"Chunk lengths don't add up: {totalChunkLength} <> {length}"
    chunks

let mergeResults (results : ResizeArray<ChunkProcessor>) : Dictionary<string, StationDataObject> =
    let merged = Dictionary<string, StationDataObject>(1024)
    for result in results do
        for name, e in result.Stations do
            if merged.ContainsKey name then
                let e2 = merged.[name]
                e2.Count <- e2.Count + e.Count
                e2.Sum <- e2.Sum + e.Sum
                e2.Min <- min e2.Min e.Min
                e2.Max <- max e2.Max e.Max
            else
                merged.Add(name, e)
    printfn $"NUM STATIONS: %d{merged.Count}"
    merged

let run (measurementsPath : string) =
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    let mmap = MemoryMappedFiles.MemoryMappedFile.CreateFromFile(measurementsPath, FileMode.Open)
    let mmapA = mmap.CreateViewAccessor()
    let mutable filePtr: nativeptr<byte> = NativePtr.nullPtr<byte>
    mmapA.SafeMemoryMappedViewHandle.AcquirePointer(&filePtr)
    let fileLength = mmapA.Capacity
    
    let chunks = chunkify filePtr fileLength
    System.Threading.Tasks.Parallel.ForEach(chunks, fun (chunk : ChunkProcessor) ->
        chunk.Run())
    |> ignore    
    
    let sortedStations =
        mergeResults chunks
        |> Seq.map (fun kv -> kv.Key, kv.Value)
        |> Seq.sortBy fst
    let mutable head = "{"
    for station in sortedStations do
        let name, e = station
        printf $"%s{head}%s{name}=%.1f{e.Min}/%.1f{e.Sum / double e.Count}/%.1f{e.Max}"
        head <- ", "
    printfn "}"
    stopwatch.Stop ()
    printfn $"ELAPSED: {stopwatch}"