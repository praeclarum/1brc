# 1️⃣🐝🏎️ The One Billion Row Challenge - .NET Edition

> The One Billion Row Challenge (1BRC [Original Java Challenge](https://github.com/gunnarmorling/1brc)) is a fun exploration of how far modern .NET can be pushed for aggregating one billion rows from a text file.
> Grab all your (virtual) threads, reach out to SIMD, optimize your GC, or pull any other trick, and create the fastest implementation for solving this task!

## Results

| # | Result (m:s.ms) | Implementation     | SDK | Submitter     |
|---|-----------------|--------------------|-----|---------------|
| 1.|        01:32.63| [LexedAndHashed.fs](https://github.com/praeclarum/1brc/blob/main/LexedAndHashed.fs)| 8.0.100| [Frank Krueger](https://github.com/praeclarum)|
| 2.|        03:18.31| [Baseline.fs](https://github.com/praeclarum/1brc/blob/main/Baseline.fs)| 8.0.100| [Frank Krueger](https://github.com/praeclarum)|

## Running

```bash
dotnet run measurements-20.txt
```

### macOS Intel 64-bit Optimized

```bash
dotnet publish -c Release -r osx-x64 --self-contained
time bin/Release/net8.0/osx-x64/publish/1brc measurements.txt
```

## Profiling

```bash
dotnet-trace collect --duration 00:00:10 -- bin/Release/net8.0/osx-x64/publish/1brc measurements.txt
dotnet-trace convert 1brc_yyyymmdd_hhmmss.nettrace --format Speedscope
```

Drag the resulting json file on to [https://www.speedscope.app](https://www.speedscope.app)
