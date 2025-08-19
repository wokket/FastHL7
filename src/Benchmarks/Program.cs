using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

//BenchmarkRunner.Run<ParseMessageBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<DateTimeParseBench>(/*new DebugInProcessConfig()*/);
BenchmarkRunner.Run<EscapingBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<HexStringBenchmarks>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<QueryBench>(/*new DebugInProcessConfig()*/);