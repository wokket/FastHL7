using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;


BenchmarkRunner.Run<MllpReaderBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<MllpWriterBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<ParseMessageBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<DateTimeParseBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<EscapingBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<HexStringBenchmarks>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<QueryBench>(/*new DebugInProcessConfig()*/);