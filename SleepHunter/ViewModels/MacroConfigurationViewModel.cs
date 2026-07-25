using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Services.Configuration;

namespace SleepHunter.ViewModels
{
    public sealed partial class MacroConfigurationViewModel :
        ObservableObject
    {
        private readonly Func<SpellQueueRotation> fallbackRotation;
        private readonly IMacroConfigurationReader reader;
        private readonly Func<
            MacroCommand,
            CancellationToken,
            ValueTask> sendCommand;

        public MacroConfigurationViewModel(
            IMacroConfigurationReader reader,
            Func<SpellQueueRotation> fallbackRotation,
            Func<
                MacroCommand,
                CancellationToken,
                ValueTask> sendCommand)
        {
            this.reader = reader ??
                throw new ArgumentNullException(nameof(reader));
            this.fallbackRotation = fallbackRotation ??
                throw new ArgumentNullException(nameof(fallbackRotation));
            this.sendCommand = sendCommand ??
                throw new ArgumentNullException(nameof(sendCommand));
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        public partial Exception LastError { get; private set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasWarnings))]
        [NotifyPropertyChangedFor(nameof(Warnings))]
        public partial MacroConfigurationLoadResult LatestLoad
        {
            get;
            private set;
        }

        public bool HasError => LastError is not null;

        public bool HasWarnings => Warnings.Length > 0;

        public ImmutableArray<MacroConfigurationWarning> Warnings =>
            LatestLoad?.Warnings ??
            ImmutableArray<MacroConfigurationWarning>.Empty;

        public Task ApplyAsync(
            MacroConfigurationLoadResult loaded,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(loaded);
            return ApplyCoreAsync(loaded, cancellationToken);
        }

        [RelayCommand]
        private async Task LoadAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            LastError = null;

            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
                var loaded = await reader
                    .LoadAsync(filePath, cancellationToken);
                await ApplyCoreAsync(loaded, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LastError = exception;
            }
        }

        private async Task ApplyCoreAsync(
            MacroConfigurationLoadResult loaded,
            CancellationToken cancellationToken)
        {
            LastError = null;

            try
            {
                var configuration = loaded.Configuration;
                var rotation =
                    configuration.SpellRotation ??
                    fallbackRotation();
                if (!Enum.IsDefined(rotation))
                {
                    throw new InvalidOperationException(
                        "The fallback spell queue rotation is not supported.");
                }

                await sendCommand(
                    new ReplaceQueuesCommand(
                        configuration.Spells,
                        rotation,
                        configuration.Skills,
                        configuration.Flowers),
                    cancellationToken);
                LatestLoad = loaded;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LastError = exception;
            }
        }
    }
}
