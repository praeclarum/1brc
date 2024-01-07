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
    { LastNameChar: byte
      mutable Left: StationNode ValueOption
      mutable Right: StationNode ValueOption
      mutable TempMin: int
      mutable TempMax: int
      mutable TempSum: int
      mutable TempCount: int }
    member this.GetWithLastNameChar(lastNameChar: byte) =
        match lastNameChar.CompareTo(this.LastNameChar) with
        | 0 -> ValueSome this
        | -1 -> this.Left |> ValueOption.bind (fun node -> node.GetWithLastNameChar lastNameChar)
        | _ -> this.Right |> ValueOption.bind (fun node -> node.GetWithLastNameChar lastNameChar)

let rec getNode (lastNameChar: byte) (parentNode: StationNode voption) =
    match parentNode with
    | ValueSome node ->
        node.GetWithLastNameChar lastNameChar
    | ValueNone ->
        ValueSome
            { LastNameChar = lastNameChar
              Left = ValueNone
              Right = ValueNone
              TempMin = 0
              TempMax = 0
              TempSum = 0
              TempCount = 0 }
            
let tree =
    ValueNone
    |> getNode 'A'B
    |> getNode 'B'B


type ChunkTreeProcessor(chunkPtr: nativeptr<byte>, chunkLength: int64) =
    let mutable stations: StationNode ValueOption = ValueNone
    let stationNames = Dictionary<int, string>(1024)
    member this.Length = chunkLength
    // member this.Stations =
    //     stations
    //     |> Seq.map (fun kv -> stationNames.[kv.Key], kv.Value)
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
            let mutable b = NativePtr.read p
            let mutable nameNode = 0
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

