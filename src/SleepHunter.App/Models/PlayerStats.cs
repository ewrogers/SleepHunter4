using SleepHunter.Common;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Models
{
    public sealed class PlayerStats : ObservableObject
    {
        private int currentHealth;
        private int maximumHealth;
        private int currentMana;
        private int maximumMana;

        public int CurrentHealth
        {
            get => currentHealth;
            set => SetProperty(
                ref currentHealth,
                value,
                onChanged: (_) =>
                {
                    RaisePropertyChanged(nameof(HealthPercent));
                });
        }

        public int MaximumHealth
        {
            get => maximumHealth;
            set => SetProperty(
                ref maximumHealth,
                value,
                onChanged: (_) =>
                {
                    RaisePropertyChanged(nameof(HealthPercent));
                });
        }

        public int CurrentMana
        {
            get => currentMana;
            set => SetProperty(
                ref currentMana,
                value,
                onChanged: (_) =>
                {
                    RaisePropertyChanged(nameof(ManaPercent));
                });
        }

        public int MaximumMana
        {
            get => maximumMana;
            set => SetProperty(
                ref maximumMana,
                value,
                onChanged: (_) =>
                {
                    RaisePropertyChanged(nameof(ManaPercent));
                });
        }

        public double HealthPercent =>
            CalculatePercent(currentHealth, maximumHealth);

        public double ManaPercent =>
            CalculatePercent(currentMana, maximumMana);

        internal void Apply(VitalsSnapshot snapshot)
        {
            CurrentHealth = snapshot?.CurrentHealth ?? 0;
            MaximumHealth = snapshot?.MaximumHealth ?? 0;
            CurrentMana = snapshot?.CurrentMana ?? 0;
            MaximumMana = snapshot?.MaximumMana ?? 0;
        }

        internal void Reset() => Apply(null);

        private static double CalculatePercent(
            int current,
            int maximum)
        {
            if (maximum <= 0)
                return 0;

            if (current >= maximum)
                return 100;

            return current * 100.0 / maximum;
        }
    }
}
