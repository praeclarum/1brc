// 1 Billion Row Challenge in F#
// 1 Billion Row Challenge in F#


module Baseline

open System
open System.Collections.Generic
open System.IO

type StationDataObject = {
    mutable Min : double
    mutable Max : double
    mutable Sum : double
    mutable Count : int
}

let run (measurementsPath : string) =
    let bufferSize = 64*1024
    use measurementsStream = new FileStream(measurementsPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan)
    use measurements = new StreamReader(measurementsStream, System.Text.Encoding.UTF8, true, bufferSize)
    let stations = Dictionary<string, StationDataObject>()
    let mutable entry: StationDataObject = {
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
            entry.Min <- min entry.Min temp
            entry.Max <- max entry.Max temp
            entry.Sum <- entry.Sum + temp
            entry.Count <- entry.Count + 1
        | false ->
            stations.[station] <- {
                Min = temp
                Max = temp
                Sum = temp
                Count = 1
            }
        count <- count + 1
        line <- measurements.ReadLine()
        if (count % 50_000_000) = 0 then
            let entriesPerSecond = (float count) / stopwatch.Elapsed.TotalSeconds
            let estimatedTotalTime = TimeSpan.FromSeconds (1.0e9 / entriesPerSecond)
            printfn $"Processed %d{count} lines (est {estimatedTotalTime})"
    let sortedStations =
        stations
        |> Seq.sortBy (fun (kv : KeyValuePair<string, StationDataObject>) -> kv.Key)
    let mutable head = "{"
    for station in sortedStations do
        let e = station.Value
        printf $"%s{head}%s{station.Key}=%.1f{e.Min}/%.1f{e.Sum / double e.Count}/%.1f{e.Max}"
        head <- ", "
    printfn "}"
