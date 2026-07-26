using SleepHunter.Services.Clients;
using SleepHunter.Settings;
using SleepHunter.Tests.Support;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.ViewModels;

public sealed class ClientLaunchViewModelTests
{
    [Test]
    public void ShouldEnableLaunchOnlyWhenLayoutIsAvailable()
    {
        var viewModel = CreateViewModel(
            new RecordingClientLaunchService(),
            new RecordingClientLaunchInteraction(),
            new UserSettings(),
            new ClientLayout(),
            new TestLogger());

        Assert.That(
            viewModel.LaunchClientCommand.CanExecute(null),
            Is.False);

        viewModel.IsLayoutAvailable = true;

        Assert.That(
            viewModel.LaunchClientCommand.CanExecute(null),
            Is.True);
    }

    [Test]
    public void ShouldSnapshotCurrentSettingsAndLaunch()
    {
        var launcher = new RecordingClientLaunchService();
        var settings = new UserSettings
        {
            ClientPath = "client.exe",
            AllowMultipleInstances = true,
            ImprovedAutoFollowMinimumDistance = 4
        };
        var layout = new ClientLayout();
        var viewModel = CreateViewModel(
            launcher,
            new RecordingClientLaunchInteraction(),
            settings,
            layout,
            new TestLogger());
        viewModel.IsLayoutAvailable = true;

        viewModel.LaunchClientCommand.Execute(null);
        settings.ClientPath = "changed.exe";

        Assert.Multiple(() =>
        {
            Assert.That(
                launcher.Options?.ExecutablePath,
                Is.EqualTo("client.exe"));
            Assert.That(
                launcher.Options
                    ?.ImprovedAutoFollowMinimumDistance,
                Is.EqualTo(4));
            Assert.That(launcher.Layout, Is.SameAs(layout));
            Assert.That(launcher.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldReportLaunchFailure()
    {
        var expected = new InvalidOperationException(
            "launch failed");
        var launcher = new RecordingClientLaunchService
        {
            Exception = expected
        };
        var interaction =
            new RecordingClientLaunchInteraction();
        var logger = new TestLogger();
        var viewModel = CreateViewModel(
            launcher,
            interaction,
            new UserSettings(),
            new ClientLayout(),
            logger);
        viewModel.IsLayoutAvailable = true;

        viewModel.LaunchClientCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(
                interaction.Exception,
                Is.SameAs(expected));
            Assert.That(
                logger.Errors,
                Has.Member(
                    "Unable to launch a new client"));
            Assert.That(
                logger.Exceptions,
                Has.Member(expected));
        });
    }

    private static ClientLaunchViewModel CreateViewModel(
        IClientLaunchService launcher,
        IClientLaunchInteraction interaction,
        UserSettings settings,
        ClientLayout layout,
        TestLogger logger) =>
        new(
            launcher,
            interaction,
            () => settings,
            () => layout,
            logger);

    private sealed class RecordingClientLaunchService :
        IClientLaunchService
    {
        public int CallCount { get; private set; }

        public Exception? Exception { get; init; }

        public ClientLayout? Layout { get; private set; }

        public ClientLaunchOptions? Options { get; private set; }

        public void Launch(
            ClientLaunchOptions options,
            ClientLayout layout)
        {
            CallCount++;
            Options = options;
            Layout = layout;

            if (Exception is not null)
                throw Exception;
        }
    }

    private sealed class RecordingClientLaunchInteraction :
        IClientLaunchInteraction
    {
        public Exception? Exception { get; private set; }

        public void ShowError(Exception exception) =>
            Exception = exception;
    }
}
