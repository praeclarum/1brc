// 1 Billion Row Challenge in F#

let test = try System.Boolean.Parse(System.Environment.GetEnvironmentVariable("TEST"))
           with _ -> false
let measurementsPath =
    if test then "/Users/fak/Projects/1brc-main/src/test/resources/samples/measurements-20.txt"
    else "/Users/fak/Projects/1brc-main/measurements.txt"

// Baseline.run measurementsPath
// MemoryMapped.run measurementsPath
LexedAndHashed.run measurementsPath
