using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.ViewModels.Presentation
{
    public sealed class ActiveSpellEffectViewModel : ObservableObject
    {
        private bool isEmpty;
        private int iconIndex;
        private ImageSource icon;
        private SpellEffectDurationStage durationStage;

        internal ActiveSpellEffectViewModel(int slot)
        {
            Slot = slot;
            isEmpty = true;
        }

        public int Slot { get; }

        public bool IsEmpty
        {
            get => isEmpty;
            private set
            {
                if (!SetProperty(ref isEmpty, value))
                    return;

                OnPropertyChanged(nameof(DurationStep));
                OnPropertyChanged(nameof(ToolTipText));
            }
        }

        public int IconIndex
        {
            get => iconIndex;
            private set
            {
                if (SetProperty(ref iconIndex, value))
                    OnPropertyChanged(nameof(ToolTipText));
            }
        }

        public ImageSource Icon
        {
            get => icon;
            private set => SetProperty(ref icon, value);
        }

        public SpellEffectDurationStage DurationStage
        {
            get => durationStage;
            private set
            {
                if (!SetProperty(ref durationStage, value))
                    return;

                OnPropertyChanged(nameof(DurationStep));
                OnPropertyChanged(nameof(ToolTipText));
            }
        }

        public int DurationStep =>
            IsEmpty ? 0 : (int)DurationStage;

        public string ToolTipText =>
            IsEmpty
                ? string.Empty
                : $"Effect {IconIndex} ({DurationStage})";

        internal void Apply(
            ActiveSpellEffectSnapshot snapshot,
            ImageSource resolvedIcon)
        {
            IsEmpty = false;
            IconIndex = snapshot.Icon;
            Icon = resolvedIcon;
            DurationStage = snapshot.DurationStage;
        }

        internal void Reset()
        {
            IsEmpty = true;
            IconIndex = 0;
            Icon = null;
            DurationStage = default;
        }
    }
}
