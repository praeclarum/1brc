// 1 Billion Row Challenge in F#
// 1 Billion Row Challenge in F#


module Baseline

open System
open System.Collections.Generic
open System.IO

[<Struct>]
type StationData = {
    Min : double
    Max : double
    Sum : double
    Count : int
}

let run (measurementsPath : string) =
    let bufferSize = 64*1024
    use measurementsStream = new FileStream(measurementsPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan)
    use measurements = new StreamReader(measurementsStream, System.Text.Encoding.UTF8, true, bufferSize)
    let stations = Dictionary<string, StationData>()
    let mutable entry: StationData = {
        Min = 0.0
        Max = 0.0
        Sum = 0.0
        Count = 0
    }
    let mutable count = 0
    let mutable line = measurements.ReadLine()
    let stopwatch = System.Diagnostics.Stopwatch.StartNew()
    while line <> null do
        let parts = line.Split(';')
        let station = parts.[0]
        let temp = double(parts.[1])
        match stations.TryGetValue(station, &entry) with
        | true ->
            stations.[station] <- {
                Min = min entry.Min temp
                Max = max entry.Max temp
                Sum = entry.Sum + temp
                Count = entry.Count + 1
            }
        | false ->
            stations.[station] <- {
                Min = temp
                Max = temp
                Sum = temp
                Count = 1
            }
        count <- count + 1
        line <- measurements.ReadLine()
        if (count % 10000000) = 0 then
            let entriesPerSecond = (float count) / stopwatch.Elapsed.TotalSeconds
            let estimatedTotalTime = TimeSpan.FromSeconds (1.0e9 / entriesPerSecond)
            printfn "Processed %d lines (est %O)" count estimatedTotalTime
    let sortedStations =
        stations
        |> Seq.sortBy (fun (kv : KeyValuePair<string, StationData>) -> kv.Key)
    let mutable head = "{"
    for station in sortedStations do
        let e = station.Value
        printf "%s%s=%.1f/%.1f/%.1f" head station.Key e.Min (e.Sum / double e.Count) e.Max
        head <- ", "
    printfn "}"
