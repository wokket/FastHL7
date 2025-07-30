using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Benchmarks;

BenchmarkRunner.Run<ParseMessageBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<DateTimeParseBench>(/*new DebugInProcessConfig()*/);