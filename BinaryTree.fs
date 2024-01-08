// 1 Billion Row Challenge in F#
// One thread per 256 MB chunk

module BinaryTree

open System
open System.Buffers.Text
open System.Collections.Generic
open System.IO
open Microsoft.FSharp.Core
open Microsoft.FSharp.NativeInterop

open Baseline
open LexedAndHashed

#nowarn "9"

type StationNode =
    { NameByte: byte
      mutable Left: StationNode ValueOption
      mutable Right: StationNode ValueOption
      mutable NextTree: StationNode ValueOption
      mutable TempMin: int
      mutable TempMax: int
      mutable TempSum: int
      mutable TempCount: int }

let inline newNode (nameByte: byte) =
    { NameByte = nameByte
      Left = ValueNone
      Right = ValueNone
      NextTree = ValueNone
      TempMin = 0
      TempMax = 0
      TempSum = 0
      TempCount = 0 }
    
let rec getNode (nameByte: byte) (node: StationNode) =
    if nameByte = node.NameByte then
        node
    elif nameByte < node.NameByte then
        match node.Left with
        | ValueNone ->
            let newNode = newNode nameByte
            node.Left <- ValueSome newNode
            newNode
        | ValueSome leftNode ->
            getNode nameByte leftNode
    else
        match node.Right with
        | ValueNone ->
            let newNode = newNode nameByte
            node.Right <- ValueSome newNode
            newNode
        | ValueSome rightNode ->
            getNode nameByte rightNode
            
let getNextNode (nameByte: byte) (parentNode: StationNode) =
    match parentNode.NextTree with
    | ValueNone ->
        let newNode = newNode nameByte
        parentNode.NextTree <- ValueSome newNode
        newNode
    | ValueSome nextNode ->
        getNode nameByte nextNode
            
let rootNode = newNode 'K'B
let addStation (name: string) (temp: int) =
    let nameBytes = System.Text.Encoding.UTF8.GetBytes name
    let mutable node = getNode nameBytes.[0] rootNode
    for i in 1 .. nameBytes.Length - 1 do
        node <- getNextNode nameBytes.[i] node
    let count = node.TempCount + 1
    if count = 1 then
        node.TempMin <- temp
        node.TempMax <- temp
        node.TempSum <- temp
        node.TempCount <- 1
    else
        node.TempMin <- min node.TempMin temp
        node.TempMax <- max node.TempMax temp
        node.TempSum <- node.TempSum + temp
        node.TempCount <- count

addStation "KABQ" -10
addStation "KABQ" 25
addStation "KAB" 30
addStation "Woo" 40
addStation "Foo" 40

rootNode


type ChunkTreeProcessor(chunkPtr: nativeptr<byte>, chunkLength: int64) =
    let rootNode = newNode 'K'B
    let mutable stations: StationNode ValueOption = ValueNone
    let stationNames = Dictionary<int, string>(1024)
    member this.Length = chunkLength
    // member this.Stations =
    //     stations
    //     |> Seq.map (fun kv -> stationNames.[kv.Key], kv.Value)
    member this.Run() =
        let mutable count: int64 = 0
        let mutable p = chunkPtr
        let mutable index: int64 = 0
        while index < chunkLength do
            // 1. Read and hash station name
            let mutable nameLength = 1
            let mutable b = NativePtr.read p
            let mutable nameNode = getNode b rootNode
            b <- NativePtr.get p nameLength
            while b <> ';'B do
                nameNode <- getNextNode b nameNode
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
            let scount = nameNode.TempCount + 1
            if scount = 1 then
                nameNode.TempMin <- temp
                nameNode.TempMax <- temp
                nameNode.TempSum <- temp
                nameNode.TempCount <- 1
            else
                nameNode.TempMin <- min nameNode.TempMin temp
                nameNode.TempMax <- max nameNode.TempMax temp
                nameNode.TempSum <- nameNode.TempSum + temp
                nameNode.TempCount <- scount
            count <- count + 1L
            // 4. Skip to next line
            let newlineOffset = nameLength + tempLength + 2
            p <- NativePtr.add p newlineOffset
            index <- index + int64 newlineOffset

let mergeResults (results : ResizeArray<ChunkTreeProcessor>) : Dictionary<string, StationDataFixed> =
    let merged = Dictionary<string, StationDataFixed>(1024)
    for result in results do
        // for name, e in result.Stations do
        //     if merged.ContainsKey name then
        //         let e2 = merged.[name]
        //         e2.Count <- e2.Count + e.Count
        //         e2.Sum <- e2.Sum + e.Sum
        //         e2.Min <- min e2.Min e.Min
        //         e2.Max <- max e2.Max e.Max
        //     else
        //         merged.Add(name, e)
        ()
    printfn $"NUM STATIONS: %d{merged.Count}"
    merged

let run (measurementsPath : string) =
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    let mmap = MemoryMappedFiles.MemoryMappedFile.CreateFromFile(measurementsPath, FileMode.Open)
    let mmapA = mmap.CreateViewAccessor()
    let mutable filePtr: nativeptr<byte> = NativePtr.nullPtr<byte>
    mmapA.SafeMemoryMappedViewHandle.AcquirePointer(&filePtr)
    let fileLength = mmapA.Capacity
    
    let chunks = Multithreaded.chunkify filePtr fileLength (fun p l -> ChunkTreeProcessor(p, l))
    
    let threads =
        chunks
        |> Seq.map (fun c -> new System.Threading.Thread(fun () -> c.Run()))
        |> Array.ofSeq
    for thread in threads do
        thread.Start()
        #if DEBUG
        thread.Join()
        #endif
    for thread in threads do
        thread.Join()
    
    let sortedStations =
        mergeResults chunks
        |> Seq.map (fun kv -> kv.Key, kv.Value)
        |> Seq.sortBy fst
    let mutable head = "{"
    for station in sortedStations do
        let name, e = station
        printf $"%s{head}%s{name}=%.1f{float e.Min * 0.1}/%.1f{float e.Sum * 0.1 / float e.Count}/%.1f{float e.Max * 0.1}"
        head <- ", "
    printfn "}"
    stopwatch.Stop ()
    printfn $"ELAPSED: {stopwatch}"
