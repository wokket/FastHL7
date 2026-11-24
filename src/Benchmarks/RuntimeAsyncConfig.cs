using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Benchmarks;

public class RuntimeAsyncConfig : ManualConfig
{
    private const string SuppressExperimentalAsyncHelpersWarning = "/p:NoWarn=SYSLIB5007";


    public RuntimeAsyncConfig()
    {
        AddColumnProvider(DefaultColumnProviders.Instance);
        AddLogger(ConsoleLogger.Default);
        
#if NET11_0
        AddJob(Job.ShortRun
            .WithRuntime(CoreRuntime.Core11_0)
            .WithMsBuildArguments("/p:Features=runtime-async=on")
            .WithId("RuntimeAsyncOn"));
        AddJob(Job.ShortRun
            .WithRuntime(CoreRuntime.Core11_0)
            .WithMsBuildArguments("/p:UseRuntimeAsync=false", "/p:Features=")
            .WithId("RuntimeAsyncOff"));
#else
        AddJob(Job.ShortRun
            .WithMsBuildArguments(SuppressExperimentalAsyncHelpersWarning,
                "/p:UseRuntimeAsync=false", "/p:Features="));
#endif
    }
}