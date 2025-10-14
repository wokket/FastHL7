using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using FastHl7;

namespace Benchmarks;

/// <summary>
/// Benchmark for comparing local changes against the nuget version, and across .net runtimes.
///
/// To Run: ` dotnet run -c release -f net9.0 --filter "*NetRuntimeUpgradeBench*" ` 
/// </summary>
[Config(typeof(Config))]
[MemoryDiagnoser]
public class NetRuntimeUpgradeBench
{
    private static readonly string _sampleMessage = File.ReadAllText("Sample-Orm.txt");

    [Benchmark]
    public void ParseAndQueryMessage()
    {
        var msg = new Message(_sampleMessage);
        var nte = msg.GetSegment("NTE(2)");

        var noteText = nte.GetField(3);
        if (!noteText.Value.StartsWith("more text"))
        {
            throw new(); //sanity check
        }

#if LOCAL_CODE
        var unescapedText = noteText.Value.Unescape(msg.Delimiters);
#else
        var unescapedText = noteText.Value.Unescape(new(_sampleMessage));
#endif

        var receivingApp = msg.Query("MSH.5");
        if (!receivingApp.Equals("ReceivingApp", StringComparison.OrdinalIgnoreCase))
        {
            throw new($"'{receivingApp}' is not expected value"); //sanity check
        }

        var patientFirstName = msg.Query("PID.5.2");
        if (!patientFirstName.Equals("Jane", StringComparison.OrdinalIgnoreCase))
        {
            throw new(); //sanity check 
        }
    }


    private class Config : ManualConfig
    {
        public Config()
        {
            var baseJob = Job.ShortRun;

            AddJob(baseJob.WithRuntime(CoreRuntime.Core90).WithId("Net9 Nuget").AsBaseline());
            AddJob(baseJob.WithRuntime(CoreRuntime.Core10_0).WithId("Net10 Nuget"));

            AddJob(baseJob.WithRuntime(CoreRuntime.Core90).WithCustomBuildConfiguration("LOCAL_CODE")
                .WithId("Net9 Local")); // custom config to include/exclude nuget reference or target project reference locally

            AddJob(baseJob.WithRuntime(CoreRuntime.Core10_0).WithCustomBuildConfiguration("LOCAL_CODE")
                .WithId("Net10 Local")); // custom config to include/exclude nuget reference or target project reference locally
        }
    }
}