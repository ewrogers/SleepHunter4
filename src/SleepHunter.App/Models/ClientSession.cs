using System;
using CommunityToolkit.Mvvm.ComponentModel;
using SleepHunter.Services.Hotkeys;
using SleepHunter.Settings;

namespace SleepHunter.Models
{
    public sealed class ClientSession : ObservableObject
    {
        private ClientLayout layout;
        private string name;
        private Hotkey hotkey;
        private int selectedTabIndex;

        public ClientProcess Process { get; init; }

        public ClientLayout Layout
        {
            get => layout;
            set => SetProperty(ref layout, value);
        }

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public string HotkeyString => hotkey?.ToString();

        public Hotkey Hotkey
        {
            get => hotkey;
            set
            {
                if (!SetProperty(ref hotkey, value))
                    return;

                OnPropertyChanged(nameof(HotkeyString));
                OnPropertyChanged(nameof(HasHotkey));
            }
        }

        public bool HasHotkey =>
            !string.IsNullOrWhiteSpace(HotkeyString);

        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set => SetProperty(ref selectedTabIndex, value);
        }

        public ClientSession(ClientProcess process)
        {
            Process = process ??
                throw new ArgumentNullException(nameof(process));
        }

        public override string ToString() =>
            Name ?? $"Process {Process.ProcessId}";
    }
}
