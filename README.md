# 1️⃣🐝🏎️ The One Billion Row Challenge - .NET Edition

> The One Billion Row Challenge (1BRC [Original Java Challenge](https://github.com/gunnarmorling/1brc)) is a fun exploration of how far modern .NET can be pushed for aggregating one billion rows from a text file.
> Grab all your (virtual) threads, reach out to SIMD, optimize your GC, or pull any other trick, and create the fastest implementation for solving this task!

## Results

Tested on a 3GHz 10-core Xeon W iMac Pro 2017.

| #  | Result (m:s.ms) | Language | Implementation                                                                                                            | Runtime      | Submitter     |
|----|-----------------|----------|---------------------------------------------------------------------------------------------------------------------------|--------------|---------------|
| 1. | 00:02.68        | F#       | [Multithreaded.fs](https://github.com/praeclarum/1brc/blob/main/Multithreaded.fs)                                         | net8/osx-x64 | [Frank Krueger](https://github.com/praeclarum)|
| 2. | 00:02.69        | C#       | [buybackoff/1brc](https://github.com/buybackoff/1brc)                                                                     | net8/osx-x64 | [Victor Baybekov](https://github.com/buybackoff)|
| 3. | 00:03.62        | C#       | [pedrosakuma/1brc](https://github.com/pedrosakuma/1brc)                                                                   | net8/osx-x64 | [Pedro Travi](https://github.com/pedrosakuma)|
| 4. | 00:04.80        | C#       | [Vake93/1brc](https://github.com/Vake93/1brc)                                                                   | net8/osx-x64 | [Vishvaka Ranasinghe](https://github.com/Vake93)|
| 5. | 00:06.04        | C#       | [hexawyz/OneBillionRows](https://github.com/hexawyz/OneBillionRows)                                                       | net8/osx-x64 | [Fabien Barbier](https://github.com/hexawyz)|
| 6. | 00:06.55        | C#       | [bbronisz/1brc](https://github.com/bbronisz/1brc)                                                       | net8/osx-x64 | [Beniamin](https://github.com/bbronisz)|
| 7. | 00:30.81        | C#       | [F0b0s/1brc](https://github.com/F0b0s/1brc)                                                                               | net8/osx-x64 | [Sergey Popov](https://github.com/F0b0s)|
| 8. | 00:51.76        | F#       | [LexedAndHashed.fs](https://github.com/praeclarum/1brc/blob/main/LexedAndHashed.fs)                                       | net8/osx-x64 | [Frank Krueger](https://github.com/praeclarum)|
| 9. | 02:53.86        | C#       | [KristofferStrube/Blazor1brc](https://github.com/KristofferStrube/Blazor1brc)                                             | net8/wasm    | [Kristoffer Strube](https://github.com/KristofferStrube)|
| 10.| 03:17.70        | Java     | [Java Baseline](https://github.com/gunnarmorling/onebrc/blob/main/src/main/java/dev/morling/onebrc/CalculateAverage.java) | java21       | [Gunnar Morling](https://github.com/gunnarmorling)|
| 11.| 03:18.26        | F#       | [Baseline.fs](https://github.com/praeclarum/1brc/blob/main/Baseline.fs)                                                   | net8/osx-x64 | [Frank Krueger](https://github.com/praeclarum)|

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
