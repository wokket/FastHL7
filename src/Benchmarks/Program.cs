using BenchmarkDotNet.Running;

// Run using `dotnet run -c release -f net9.0 --runtimes net9.0 net10.0`

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

//BenchmarkRunner.Run<MllpReaderBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<MllpWriterBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<ParseMessageBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<DateTimeParseBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<EscapingBench>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<HexStringBenchmarks>(/*new DebugInProcessConfig()*/);
//BenchmarkRunner.Run<QueryBench>(/*new DebugInProcessConfig()*/);