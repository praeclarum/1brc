// 1 Billion Row Challenge in F#

[<EntryPoint>]
let main argv =
    let measurementsPath =
        argv
        |> Seq.filter (fun arg -> arg.EndsWith(".txt", System.StringComparison.OrdinalIgnoreCase))
        |> Seq.tryHead
        |> Option.defaultValue "measurements-20.txt"
    
    // Baseline.run measurementsPath
    LexedAndHashed.run measurementsPath
    0
