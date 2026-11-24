using BenchmarkDotNet.Running;
using Benchmarks;

// Run benchmarks on each target framework with `dotnet run -c Release -f <tfm>`.
// Mllp benchmarks on net11.0 automatically compare runtime-async enabled and disabled.

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

//BenchmarkRunner.Run<MllpReaderBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<MllpWriterBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<ParseMessageBench>();
//BenchmarkRunner.Run<DateTimeParseBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<EscapingBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<HexStringBenchmarks>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<QueryBench>(/*new DebugInProcessConfig()*/);