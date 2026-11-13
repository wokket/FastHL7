using BenchmarkDotNet.Running;

// Run benchmarks on each target framework with `dotnet run -c Release -f <tfm>`.
// For net11.0 Mllp comparisons, run once normally (RuntimeAsyncOn), then run:
// `dotnet build -c Release -f net11.0 -p:UseRuntimeAsync=false`
// `dotnet run -c Release -f net11.0 --no-build` (RuntimeAsyncOff).
// The net11.0 variants use BenchmarkDotNet's in-process toolchain because 0.15.8 doesn't recognize .NET 11.

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

//BenchmarkRunner.Run<MllpReaderBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<MllpWriterBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<ParseMessageBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<DateTimeParseBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<EscapingBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<HexStringBenchmarks>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<QueryBench>(/*new DebugInProcessConfig()*/);