using InfinityMercsApp.AotSmokeTests.Smoke;
using InfinityMercsApp.AotSmokeTests.Support;

// Runs the same checks whether compiled ahead of time or not; CI compares the two reports line for line.
var repo = RepoFolder.Find(args.FirstOrDefault());
var checks = new SmokeChecks();

ValidationSmoke.Run(checks);
await ArmyStorageSmoke.RunAsync(checks, repo);
await RecordFilesSmoke.RunAsync(checks);
await ContentSmoke.RunAsync(checks, repo);
await ApiClientSmoke.RunAsync(checks);

return checks.Report(Console.Out);
