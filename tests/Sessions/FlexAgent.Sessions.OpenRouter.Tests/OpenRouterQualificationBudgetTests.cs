using FlexAgent.Sessions.OpenRouter;

namespace FlexAgent.Sessions.OpenRouter.Tests;

public sealed class OpenRouterQualificationBudgetTests
{
    [Fact]
    public void Reservations_are_persistent_and_stop_at_the_approved_limit()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "budget");

        var firstProcess = new OpenRouterQualificationBudget(path);
        Assert.True(firstProcess.TryReserve(out var first));
        Assert.Equal(1, first);

        var restartedProcess = new OpenRouterQualificationBudget(path);
        for (var expected = 2; expected <= OpenRouterLiveQualification.MaxInferenceRequests; expected++)
        {
            Assert.True(restartedProcess.TryReserve(out var reserved));
            Assert.Equal(expected, reserved);
        }

        Assert.False(restartedProcess.TryReserve(out var exhausted));
        Assert.Equal(OpenRouterLiveQualification.MaxInferenceRequests, exhausted);
    }

    [Fact]
    public void Current_count_can_be_read_without_creating_or_incrementing_state()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "budget");
        var budget = new OpenRouterQualificationBudget(path);

        Assert.False(budget.TryRead(out var missing));
        Assert.Equal(0, missing);
        Assert.False(File.Exists(path));

        Assert.True(budget.TryReserve(out var first));
        Assert.Equal(1, first);
        var afterReserve = File.ReadAllBytes(path);

        Assert.True(budget.TryRead(out var current));
        Assert.Equal(1, current);
        Assert.Equal(afterReserve, File.ReadAllBytes(path));
    }

    [Fact]
    public void Corrupt_or_incompatible_state_fails_closed_without_replacing_it()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "budget");
        File.WriteAllText(path, "not-a-budget");
        var original = File.ReadAllBytes(path);

        var budget = new OpenRouterQualificationBudget(path);

        Assert.False(budget.TryReserve(out var reserved));
        Assert.Equal(0, reserved);
        Assert.Equal(original, File.ReadAllBytes(path));
    }

    [Fact]
    public void Symbolic_link_state_fails_closed()
    {
        Assert.SkipUnless(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD(),
            "Qualification budget symlink checks apply on the current target.");
        using var directory = new TemporaryDirectory();
        var target = Path.Combine(directory.Path, "target");
        var link = Path.Combine(directory.Path, "budget");
        File.WriteAllText(target, "openrouter_qualification_budget.v1\n0\n12\n");
        File.CreateSymbolicLink(link, target);

        var budget = new OpenRouterQualificationBudget(link);

        Assert.False(budget.TryReserve(out var reserved));
        Assert.Equal(0, reserved);
        Assert.Equal("openrouter_qualification_budget.v1\n0\n12\n", File.ReadAllText(target));
        Assert.False(budget.TryRead(out var current));
        Assert.Equal(0, current);
        Assert.Equal("openrouter_qualification_budget.v1\n0\n12\n", File.ReadAllText(target));
    }

    [Fact]
    public void Dangling_symbolic_link_state_fails_closed_without_creating_its_target()
    {
        Assert.SkipUnless(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD(),
            "Qualification budget symlink checks apply on the current target.");
        using var directory = new TemporaryDirectory();
        var target = Path.Combine(directory.Path, "missing-target");
        var link = Path.Combine(directory.Path, "budget");
        File.CreateSymbolicLink(link, target);

        var budget = new OpenRouterQualificationBudget(link);

        Assert.False(budget.TryReserve(out var reserved));
        Assert.Equal(0, reserved);
        Assert.False(File.Exists(target));
    }

    [Fact]
    public void Symbolic_link_budget_directory_fails_closed()
    {
        Assert.SkipUnless(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD(),
            "Qualification budget symlink checks apply on the current target.");
        using var directory = new TemporaryDirectory();
        var targetDirectory = Path.Combine(directory.Path, "target");
        Directory.CreateDirectory(targetDirectory);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
        {
            File.SetUnixFileMode(
                targetDirectory,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        var linkedDirectory = Path.Combine(directory.Path, "linked");
        Directory.CreateSymbolicLink(linkedDirectory, targetDirectory);

        var budget = new OpenRouterQualificationBudget(Path.Combine(linkedDirectory, "budget"));

        Assert.False(budget.TryReserve(out var reserved));
        Assert.Equal(0, reserved);
        Assert.False(File.Exists(Path.Combine(targetDirectory, "budget")));
    }

    [Fact]
    public void Expected_reservation_fails_closed_when_the_count_has_moved()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "budget");
        var budget = new OpenRouterQualificationBudget(path);
        for (var i = 0; i < 6; i++)
        {
            Assert.True(budget.TryReserve(out _));
        }

        Assert.False(budget.TryReserveExpected(5, out var stale));
        Assert.Equal(6, stale);
        Assert.True(budget.TryRead(out var unchanged));
        Assert.Equal(6, unchanged);

        Assert.True(budget.TryReserveExpected(6, out var next));
        Assert.Equal(7, next);
    }

    [Fact]
    public async Task Two_instances_cannot_both_reserve_the_same_expected_count()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "budget");
        var seed = new OpenRouterQualificationBudget(path);
        for (var i = 0; i < 6; i++)
        {
            Assert.True(seed.TryReserve(out _));
        }

        var first = new OpenRouterQualificationBudget(path);
        var second = new OpenRouterQualificationBudget(path);
        var started = new ManualResetEventSlim(false);
        var results = new (bool Succeeded, int Reserved)[2];
        var cancellation = TestContext.Current.CancellationToken;
        var firstWorker = Task.Run(
            () =>
            {
                started.Wait(cancellation);
                results[0] = (first.TryReserveExpected(6, out var reserved), reserved);
            },
            cancellation);
        var secondWorker = Task.Run(
            () =>
            {
                started.Wait(cancellation);
                results[1] = (second.TryReserveExpected(6, out var reserved), reserved);
            },
            cancellation);
        started.Set();
        await Task.WhenAll(firstWorker, secondWorker);

        Assert.Equal(1, results.Count(result => result.Succeeded));
        Assert.Contains(results, result => result is { Succeeded: true, Reserved: 7 });
        Assert.True(seed.TryRead(out var current));
        Assert.Equal(7, current);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            var directory = Directory.CreateTempSubdirectory("flex-agent-openrouter-budget-");
            Path = directory.FullName;
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD())
            {
                File.SetUnixFileMode(
                    Path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
