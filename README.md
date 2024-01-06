# 1️⃣🐝🏎️ The One Billion Row Challenge - .NET Edition

> The One Billion Row Challenge (1BRC [Original Java Challenge](https://github.com/gunnarmorling/1brc)) is a fun exploration of how far modern .NET can be pushed for aggregating one billion rows from a text file.
> Grab all your (virtual) threads, reach out to SIMD, optimize your GC, or pull any other trick, and create the fastest implementation for solving this task!

## Results

Tested on a 3GHz 10-core Xeon W iMac Pro 2017.

| # | Result (m:s.ms) | Implementation     | Runtime | Submitter     |
|---|-----------------|--------------------|-----|---------------|
| 1.| 00:02.92        | [buybackoff/1brc](https://github.com/buybackoff/1brc)| osx-x64| [Victor Baybekov](https://github.com/buybackoff)|
| 2.| 00:03.62        | [pedrosakuma/1brc](https://github.com/pedrosakuma/1brc)| osx-x64| [Pedro Travi](https://github.com/pedrosakuma)|
| 3.| 00:30.81        | [F0b0s/1brc](https://github.com/F0b0s/1brc)| osx-x64| [Sergey Popov](https://github.com/F0b0s)|
| 4.| 00:51.76        | [LexedAndHashed.fs](https://github.com/praeclarum/1brc/blob/main/LexedAndHashed.fs)| osx-x64| [Frank Krueger](https://github.com/praeclarum)|
| 5.| 02:53.86        | [KristofferStrube/Blazor1brc](https://github.com/KristofferStrube/Blazor1brc)| wasm| [Kristoffer Strube](https://github.com/KristofferStrube)|
| 6.| 03:18.31        | [Baseline.fs](https://github.com/praeclarum/1brc/blob/main/Baseline.fs)| osx-x64| [Frank Krueger](https://github.com/praeclarum)|

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
