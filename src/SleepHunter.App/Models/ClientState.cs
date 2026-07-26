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

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private InterfacePanel activePanel;
        private bool isInventoryExpanded;
        private bool isMinimizedMode;

        public Player Owner { get; init; }

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

        public ClientState(Player owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));

            stream = owner.Accessor.GetStream();
            reader = new BinaryReader(stream, Encoding.ASCII);
        }

        protected override void OnUpdate()
        {
            var layout = Owner.Layout;

            if (layout == null)
            {
                ResetDefaults();
                return;
            }

            var activePanelVariable = layout.GetVariable(ActivePanelKey);
            var inventoryExpandedVariable = layout.GetVariable(InventoryExpandedKey);
            var minimizedModeVariable = layout.GetVariable(MinimizedModeKey);

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
        }
    }
}
