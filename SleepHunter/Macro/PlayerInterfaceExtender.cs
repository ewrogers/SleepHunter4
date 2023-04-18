using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;

using SleepHunter.Models;

namespace SleepHunter.Macro
{
    internal static class PlayerInterfaceExtender
    {
        public static void Disarm(this Player client)
        {
            client.Update(PlayerFieldFlags.Equipment);

            if (!client.Equipment.IsEmpty(EquipmentSlot.Weapon | EquipmentSlot.Shield))
                WindowAutomator.SendKeystroke(client.Process.WindowHandle, '`');
        }

        public static bool DisarmAndWait(this Player client, TimeSpan timeout)
        {
            Disarm(client);
            return WaitForEquipmentEmpty(client, EquipmentSlot.Weapon | EquipmentSlot.Shield, timeout);
        }

        public static void Assail(this Player client)
        {
            WindowAutomator.SendKeystroke(client.Process.WindowHandle, WindowAutomator.VK_SPACE);
        }

        public static void CancelDialog(this Player client)
        {
            WindowAutomator.SendKeystroke(client.Process.WindowHandle, WindowAutomator.VK_ESCAPE);
        }

        public static bool UseItemAndWait(this Player client, string itemName, TimeSpan timeout, out bool didRequireSwitch)
        {
            didRequireSwitch = false;

            itemName = itemName.Trim();

            client.Update(PlayerFieldFlags.Inventory);
            var slot = client.Inventory.FindItemSlot(itemName);

            if (slot < 0)
                return false;

            return UseItemAndWait(client, slot, timeout, out didRequireSwitch);
        }

        public static bool UseItemAndWait(this Player client, int slot, TimeSpan timeout, out bool didRequireSwitch)
        {
            didRequireSwitch = false;

            if (slot < 0)
                return false;

            var isInExpandedInventory = slot > 34;

            if (!client.SwitchToPanelAndWait(InterfacePanel.Inventory, timeout, out didRequireSwitch))
                return false;

            var inventoryReady = false;
            if (isInExpandedInventory)
                inventoryReady = client.ExpandInventoryAndWait(timeout);
            else
                inventoryReady = client.CollapseInventoryAndWait(timeout);

            if (!inventoryReady)
                return false;

            client.DoubleClickSlot(InterfacePanel.Inventory, slot, isInExpandedInventory);
            return true;
        }

        public static bool EquipItemAndWait(this Player client, string itemName, EquipmentSlot equipmentSlot, TimeSpan timeout, out bool didRequireSwitch)
        {
            return UseItemAndWait(client, itemName, timeout, out didRequireSwitch) &&
               WaitForEquipment(client, itemName, equipmentSlot, timeout);
        }

        public static void ClickAt(this Player client, double x, double y, bool moveMouseBeforeClick = true)
        {
            WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)x, (int)y, moveMouseBeforeClick);
        }

        public static void ClickSlot(this Player client, InterfacePanel panel, int slot, bool isExpandedInventory = false)
        {
            slot = panel.GetRelativeSlot(slot);

            var pt = panel.ToSlotPoint(slot, isExpandedInventory);
            pt = pt.ScalePoint(client.Process.WindowScaleX, client.Process.WindowScaleY);

            WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)pt.X, (int)pt.Y);
        }

        public static void DoubleClickSlot(this Player client, InterfacePanel panel, int slot, bool isExpandedInventory = false)
        {
            slot = panel.GetRelativeSlot(slot);

            var pt = panel.ToSlotPoint(slot, isExpandedInventory);
            pt = pt.ScalePoint(client.Process.WindowScaleX, client.Process.WindowScaleY);

            WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)pt.X, (int)pt.Y);
            WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)pt.X, (int)pt.Y);
        }

        public static void ExpandInventory(this Player client)
        {
            var pt = new Point(570, 320);
            pt = pt.ScalePoint(client.Process.WindowScaleX, client.Process.WindowScaleY);

            client.Update(PlayerFieldFlags.GameClient);

            if (!client.GameClient.IsInventoryExpanded)
                WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)pt.X, (int)pt.Y);
        }

        public static void CollapseInventory(this Player client)
        {
            var pt = new Point(570, 320);
            pt = pt.ScalePoint(client.Process.WindowScaleX, client.Process.WindowScaleY);

            client.Update(PlayerFieldFlags.GameClient);

            if (client.GameClient.IsInventoryExpanded)
                WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)pt.X, (int)pt.Y);
        }

        public static bool ExpandInventoryAndWait(this Player client, TimeSpan timeout)
        {
            ExpandInventory(client);
            return WaitForInventory(client, true, timeout);
        }

        public static bool CollapseInventoryAndWait(this Player client, TimeSpan timeout)
        {
            CollapseInventory(client);
            return WaitForInventory(client, false, timeout);
        }

        public static bool WaitForInventory(this Player client, bool isExpanded, TimeSpan timeout)
        {
            var startTime = DateTime.Now;

            while (true)
            {
                client.Update(PlayerFieldFlags.GameClient);

                if (client.GameClient.IsInventoryExpanded == isExpanded)
                    return true;

                var deltaTime = DateTime.Now - startTime;

                if (timeout != TimeSpan.Zero && deltaTime >= timeout)
                    break;

                Thread.Sleep(1);
            }

            return false;
        }

        public static bool WaitForEquipment(this Player client, string itemName, EquipmentSlot slot, TimeSpan timeout)
        {
            var startTime = DateTime.Now;

            while (true)
            {
                client.Update(PlayerFieldFlags.Equipment);

                if (client.Equipment.IsEquipped(itemName, slot))
                    return true;

                var deltaTime = DateTime.Now - startTime;

                if (timeout != TimeSpan.Zero && deltaTime >= timeout)
                    break;

                Thread.Sleep(1);
            }

            return false;
        }

        public static bool WaitForEquipmentEmpty(this Player client, EquipmentSlot slot, TimeSpan timeout)
        {
            var startTime = DateTime.Now;

            while (true)
            {
                client.Update(PlayerFieldFlags.Equipment);

                if (client.Equipment.IsEmpty(slot))
                    return true;

                var deltaTime = DateTime.Now - startTime;

                if (timeout == TimeSpan.Zero && deltaTime >= timeout)
                    break;

                Thread.Sleep(1);
            }

            return false;
        }

        public static void SwitchToPanel(this Player client, InterfacePanel panel, out bool didRequireSwitch, bool useShiftKey = true)
        {
            didRequireSwitch = false;
            var pt = panel.ToPoint();
            pt = pt.ScalePoint(client.Process.WindowScaleX, client.Process.WindowScaleY);

            var x = (int)pt.X;
            var y = (int)pt.Y;

            var hwnd = client.Process.WindowHandle;

            client.Update(PlayerFieldFlags.GameClient);
            var currentPanel = client.GameClient.ActivePanel;

            if (currentPanel.IsSameAs(panel))
                return;

            didRequireSwitch = true;

            // Tem Skills/Spells to Medenia
            if (currentPanel.IsTemuairToMedenia(panel))
            {
                if (useShiftKey)
                    WindowAutomator.SendShiftKeyDown(hwnd);

                WindowAutomator.SendMouseClick(hwnd, MouseButton.Left, x, y);

                if (useShiftKey)
                    WindowAutomator.SendShiftKeyUp(hwnd);
            }
            // Med Skills/Spells to Temuair
            else if (currentPanel.IsMedeniaToTemuair(panel))
            {
                WindowAutomator.SendMouseClick(hwnd, MouseButton.Left, x, y);
            }
            // Switching to Medenia from non-Temuair
            else if (panel.IsMedeniaPanel())
            {
                if (useShiftKey)
                    WindowAutomator.SendShiftKeyDown(hwnd);

                WindowAutomator.SendMouseClick(hwnd, MouseButton.Left, x, y);

                if (!useShiftKey)
                    WindowAutomator.SendMouseClick(hwnd, MouseButton.Left, x, y);

                if (useShiftKey)
                    WindowAutomator.SendShiftKeyUp(hwnd);
            }
            // Switching to Temauir from non-Medenia
            else
            {
                WindowAutomator.SendMouseClick(hwnd, MouseButton.Left, x, y);
            }
        }

        public static bool SwitchToPanelAndWait(this Player client, InterfacePanel panel, out bool didRequireSwitch, bool useShiftKey = true)
        {
            return SwitchToPanelAndWait(client, panel, TimeSpan.Zero, out didRequireSwitch, useShiftKey);
        }

        public static bool SwitchToPanelAndWait(this Player client, InterfacePanel panel, TimeSpan timeout, out bool didRequireSwitch, bool useShiftKey = true)
        {
            SwitchToPanel(client, panel, out didRequireSwitch, useShiftKey);
            return WaitForPanel(client, panel, timeout);
        }

        public static void WaitForPanel(this Player client, InterfacePanel panel)
        {
            WaitForPanel(client, panel, TimeSpan.Zero);
        }

        public static bool WaitForPanel(this Player client, InterfacePanel panel, TimeSpan timeout)
        {
            var startTime = DateTime.Now;

            while (true)
            {
                client.Update(PlayerFieldFlags.GameClient);

                var activePanel = client.GameClient.ActivePanel;

                if (activePanel.IsSameAs(panel))
                    return true;

                var delaTime = DateTime.Now - startTime;

                if (timeout != TimeSpan.Zero && delaTime >= timeout)
                    break;

                Thread.Sleep(1);
            }

            return false;
        }

        public static void Terminate(this Player client)
        {
            WindowAutomator.SendCloseWindow(client.Process.WindowHandle);
        }

        private static int GetRelativeSlot(this InterfacePanel panel, int slot)
        {
            int maxSlotCount = 36;

            if (panel == InterfacePanel.Inventory)
                maxSlotCount = 60;

            if (panel.IsWorldPanel())
                maxSlotCount = 18;

            return slot % maxSlotCount;
        }

        private static Point ScalePoint(this Point pt, double scaleX, double scaleY)
        {
            if (scaleX > 0 && scaleX != 1)
                pt.X *= scaleX;

            if (scaleY > 0 && scaleY != 1)
                pt.Y *= scaleY;

            return pt;
        }
    }
}
