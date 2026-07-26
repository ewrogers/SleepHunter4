using System;
using System.Buffers.Binary;
using System.Collections.Generic;
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
        private const string UserChattingKey = @"UserChatting";
        private const string EventPaneEntriesKey = @"EventPaneEntries";
        private const string EventPaneCountKey = @"EventPaneCount";
        private const string EventPaneCapacityKey = @"EventPaneCapacity";
        private const int EventPaneRecordSize = 12;
        private const int MaximumEventPaneCount = 4096;

        private static readonly HashSet<string> ChatInputPaneClasses = new(StringComparer.Ordinal)
        {
            "ChatInputPane",
            "TellInputPane",
            "TellReceiverInputPane",
            "BlockListenInputPane"
        };

        private readonly Stream stream;
        private readonly BinaryReader reader;

        private InterfacePanel activePanel;
        private bool isInventoryExpanded;
        private bool isMinimizedMode;
        private bool isUserChatting;

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
            var layout = Owner.Layout;

            if (layout == null)
            {
                ResetDefaults();
                return;
            }

            var activePanelVariable = layout.GetVariable(ActivePanelKey);
            var inventoryExpandedVariable = layout.GetVariable(InventoryExpandedKey);
            var minimizedModeVariable = layout.GetVariable(MinimizedModeKey);
            var userChattingVariable = layout.GetVariable(UserChattingKey);

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

            if (TryReadChatInputState(layout, out var isUserChatting) ||
                userChattingVariable != null && userChattingVariable.TryReadBoolean(reader, out isUserChatting))
            {
                IsUserChatting = isUserChatting;
            }
            else
            {
                IsUserChatting = false;
            }
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
            IsUserChatting = false;
        }

        private bool TryReadChatInputState(
            Settings.ClientLayout layout,
            out bool isUserChatting)
        {
            isUserChatting = false;

            if (!layout.TryGetVariable(EventPaneEntriesKey, out var entriesVariable) ||
                !layout.TryGetVariable(EventPaneCountKey, out var countVariable) ||
                !layout.TryGetVariable(EventPaneCapacityKey, out var capacityVariable) ||
                !countVariable.TryReadInt32(reader, out var count) ||
                !capacityVariable.TryReadInt32(reader, out var capacity) ||
                count < 0 ||
                capacity < count ||
                capacity > MaximumEventPaneCount)
            {
                return false;
            }

            if (count == 0)
                return true;

            if (!entriesVariable.TryDereferenceValue(reader, out var entriesAddress) ||
                !RuntimeMemoryReader.TryReadBytes(
                    reader,
                    entriesAddress,
                    checked(count * EventPaneRecordSize),
                    out var entries))
            {
                return false;
            }

            if (!countVariable.TryReadInt32(reader, out var currentCount) ||
                currentCount != count ||
                !entriesVariable.TryDereferenceValue(reader, out var currentEntriesAddress) ||
                currentEntriesAddress != entriesAddress)
            {
                return false;
            }

            for (var index = 0; index < count; index++)
            {
                var paneAddress = BinaryPrimitives.ReadUInt32LittleEndian(
                    entries.AsSpan(index * EventPaneRecordSize, sizeof(uint)));
                if (!RuntimeMemoryReader.TryReadRttiClassName(reader, paneAddress, out var paneClass) ||
                    !ChatInputPaneClasses.Contains(paneClass) ||
                    !RuntimeMemoryReader.TryReadBytes(reader, paneAddress + 0x130, 1, out var visible))
                {
                    continue;
                }

                if (visible[0] != 0)
                {
                    isUserChatting = true;
                    return true;
                }
            }

            return true;
        }
    }
}
