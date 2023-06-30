using System;
using System.IO;
using System.Text;

using SleepHunter.Common;
using SleepHunter.IO.Process;

namespace SleepHunter.Models
{
    public sealed class ClientState : UpdatableObject
    {
        private const string ActivePanelKey = @"ActivePanel";
        private const string InventoryExpandedKey = @"InventoryExpanded";
        private const string MinimizedModeKey = @"MinimizedMode";
        private const string DialogOpenKey = @"DialogOpen";
        private const string SenseOpenKey = @"SenseOpen";
        private const string UserChattingKey = @"UserChatting";

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private string versionKey;
        private InterfacePanel activePanel;
        private bool isInventoryExpanded;
        private bool isMinimizedMode;
        private bool isDialogOpen;
        private bool isSenseOpen;
        private bool isUserChatting;

        public Player Owner { get; init; }

        public string VersionKey
        {
            get => versionKey;
            set => SetProperty(ref versionKey, value);
        }

        public InterfacePanel ActivePanel
        {
            get => activePanel;
            set => SetProperty(ref activePanel, value);
        }

        public bool IsInventoryExpanded
        {
            get => isInventoryExpanded;
            set => SetProperty(ref isInventoryExpanded, value);
        }

        public bool IsMinimizedMode
        {
            get => isMinimizedMode;
            set => SetProperty(ref isMinimizedMode, value);
        }

        public bool IsDialogOpen
        {
            get => isDialogOpen;
            set => SetProperty(ref isDialogOpen, value);
        }

        public bool IsSenseOpen
        {
            get => isSenseOpen;
            set => SetProperty(ref isSenseOpen, value);
        }

        public bool IsUserChatting
        {
            get => isUserChatting;
            set => SetProperty(ref isUserChatting, value);
        }

        public ClientState(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);
        }

        protected override void OnUpdate()
        {
            var version = Owner.Version;

            if (version == null)
            {
                ResetDefaults();
                return;
            }

            var activePanelVariable = version.GetVariable(ActivePanelKey);
            var inventoryExpandedVariable = version.GetVariable(InventoryExpandedKey);
            var minimizedModeVariable = version.GetVariable(MinimizedModeKey);
            var dialogOpenVariable = version.GetVariable(DialogOpenKey);
            var senseOpenVariable = version.GetVariable(SenseOpenKey);
            var userChattingVariable = version.GetVariable(UserChattingKey);

            if (activePanelVariable != null && activePanelVariable.TryReadByte(reader, out var activePanelByte))
                ActivePanel = (InterfacePanel)activePanelByte;
            else
                ActivePanel = InterfacePanel.Unknown;

            if (inventoryExpandedVariable != null && inventoryExpandedVariable.TryReadBoolean(reader, out var isInventoryExpanded))
                IsInventoryExpanded = isInventoryExpanded;
            else
                IsInventoryExpanded = false;

            if (minimizedModeVariable != null && minimizedModeVariable.TryReadBoolean(reader, out var isMinimizedMode))
                IsMinimizedMode = isMinimizedMode;
            else
                IsMinimizedMode = false;

            if (dialogOpenVariable != null && dialogOpenVariable.TryReadBoolean(reader, out var isDialogOpen))
                IsDialogOpen = isDialogOpen;
            else
                IsDialogOpen = false;

            if (senseOpenVariable != null && senseOpenVariable.TryReadBoolean(reader, out isSenseOpen))
                IsSenseOpen = isSenseOpen;
            else
                IsSenseOpen = false;

            if (userChattingVariable != null && userChattingVariable.TryReadBoolean(reader, out var isUserChatting))
                IsUserChatting = isUserChatting;
            else
                IsUserChatting = false;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                reader?.Dispose();
                stream?.Dispose();
            }

            base.Dispose(isDisposing);
        }

        private void ResetDefaults()
        {
            ActivePanel = InterfacePanel.Unknown;
            IsInventoryExpanded = false;
            IsMinimizedMode = false;
            IsDialogOpen = false;
            IsUserChatting = false;
        }
    }
}
