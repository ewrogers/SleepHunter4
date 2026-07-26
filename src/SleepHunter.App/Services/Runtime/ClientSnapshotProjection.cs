using System;
using System.ComponentModel;
using SleepHunter.Models;
using SleepHunter.ViewModels;

namespace SleepHunter.Services.Runtime
{
    public sealed class ClientSnapshotProjection : IDisposable
    {
        private readonly Player player;
        private readonly ClientRuntimeViewModel runtime;
        private bool isDisposed;

        public ClientSnapshotProjection(
            Player player,
            ClientRuntimeViewModel runtime)
        {
            this.player = player ??
                throw new ArgumentNullException(nameof(player));
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));

            runtime.PropertyChanged += OnRuntimePropertyChanged;
            ApplyLatestSnapshot();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            runtime.PropertyChanged -= OnRuntimePropertyChanged;
            isDisposed = true;
        }

        private void OnRuntimePropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (string.Equals(
                    e.PropertyName,
                    nameof(ClientRuntimeViewModel.LatestCapture),
                    StringComparison.Ordinal))
            {
                ApplyLatestSnapshot();
            }
        }

        private void ApplyLatestSnapshot()
        {
            var snapshot = runtime.LatestSnapshot;
            if (snapshot is not null)
                player.ApplySnapshot(snapshot);
        }
    }
}
