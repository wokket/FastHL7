using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace Benchmarks;

public class RuntimeAsyncConfig : ManualConfig
{
    public RuntimeAsyncConfig()
    {
#if NET11_0
#if RUNTIME_ASYNC_OFF
        AddJob(Job.ShortRun
            .WithToolchain(InProcessNoEmitToolchain.Instance)
            .WithId("RuntimeAsyncOff"));
#else
        AddJob(Job.ShortRun
            .WithToolchain(InProcessNoEmitToolchain.Instance)
            .WithId("RuntimeAsyncOn"));
#endif
#else
        AddJob(Job.ShortRun);
#endif
    }
}
