using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using SleepHunter.Models;

namespace SleepHunter.Macro
{
    public static class PlayerInterfaceExtender
    {
        public static void Disarm(this Player client)
        {
            if (!client.Equipment.IsEmpty(EquipmentSlot.Weapon | EquipmentSlot.Shield))
                WindowAutomator.SendKeystroke(client.Process.WindowHandle, '`');
        }

        public static bool DisarmAndWait(this Player client, TimeSpan timeout, CancellationToken token = default)
        {
            Disarm(client);
            return WaitForEquipmentEmpty(client, EquipmentSlot.Weapon | EquipmentSlot.Shield, timeout, token);
        }

        public static void Assail(this Player client)
            => WindowAutomator.SendKeystroke(client.Process.WindowHandle, WindowAutomator.VK_SPACE);

        public static void CancelDialog(this Player client)
            => WindowAutomator.SendKeystroke(client.Process.WindowHandle, WindowAutomator.VK_ESCAPE);

        public static bool UseItemAndWait(this Player client, string itemName, TimeSpan timeout,
            out bool didRequireSwitch, CancellationToken token = default)
        {
            didRequireSwitch = false;

            itemName = itemName.Trim();

            var slot = client.Inventory.FindItemSlot(itemName);

            return slot >= 0 && UseItemAndWait(client, slot, timeout, out didRequireSwitch, token);
        }

        public static bool UseItemAndWait(this Player client, int slot, TimeSpan timeout, out bool didRequireSwitch,
            CancellationToken token = default)
        {
            didRequireSwitch = false;

            if (slot < 0 || token.IsCancellationRequested)
                return false;

            var isInExpandedInventory = slot > 34;

            if (!client.SwitchToPanelAndWait(InterfacePanel.Inventory, timeout, out didRequireSwitch, true, token))
                return false;

            bool inventoryReady;
            inventoryReady = isInExpandedInventory
                ? client.ExpandInventoryAndWait(timeout, token)
                : client.CollapseInventoryAndWait(timeout, token);

            if (!inventoryReady)
                return false;

            client.DoubleClickSlot(InterfacePanel.Inventory, slot, isInExpandedInventory);
            return true;
        }

        public static bool EquipItemAndWait(this Player client, string itemName, EquipmentSlot equipmentSlot,
            TimeSpan timeout, out bool didRequireSwitch)
        {
            return UseItemAndWait(client, itemName, timeout, out didRequireSwitch) &&
                   WaitForEquipment(client, itemName, equipmentSlot, timeout);
        }

        public static void ClickAt(this Player client, double x, double y, bool moveMouseBeforeClick = true)
            => WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)x, (int)y,
                moveMouseBeforeClick);

        public static void ClickSlot(this Player client, InterfacePanel panel, int slot,
            bool isExpandedInventory = false)
        {
            slot = panel.GetRelativeSlot(slot);

            var pt = panel.ToSlotPoint(slot, isExpandedInventory);
            pt = pt.ScalePoint(client.Process.WindowScaleX, client.Process.WindowScaleY);

            WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)pt.X, (int)pt.Y);
        }

        public static void DoubleClickSlot(this Player client, InterfacePanel panel, int slot,
            bool isExpandedInventory = false)
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

            if (!client.GameClient.IsInventoryExpanded)
                WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)pt.X, (int)pt.Y);
        }

        public static void CollapseInventory(this Player client)
        {
            var pt = new Point(570, 320);
            pt = pt.ScalePoint(client.Process.WindowScaleX, client.Process.WindowScaleY);

            if (client.GameClient.IsInventoryExpanded)
                WindowAutomator.SendMouseClick(client.Process.WindowHandle, MouseButton.Left, (int)pt.X, (int)pt.Y);
        }

        public static bool ExpandInventoryAndWait(this Player client, TimeSpan timeout,
            CancellationToken token = default)
        {
            ExpandInventory(client);
            return WaitForInventory(client, true, timeout, token);
        }

        public static bool CollapseInventoryAndWait(this Player client, TimeSpan timeout,
            CancellationToken token = default)
        {
            CollapseInventory(client);
            return WaitForInventory(client, false, timeout, token);
        }

        public static bool WaitForInventory(this Player client, bool isExpanded, TimeSpan timeout,
            CancellationToken token = default)
        {
            var startTime = DateTime.Now;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (client.GameClient.IsInventoryExpanded == isExpanded)
                        return true;
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var deltaTime = DateTime.Now - startTime;

                if (timeout != TimeSpan.Zero && deltaTime >= timeout)
                    break;

                Thread.Sleep(16);
            }

            return false;
        }

        public static bool WaitForEquipment(this Player client, string itemName, EquipmentSlot slot, TimeSpan timeout,
            CancellationToken token = default)
        {
            var startTime = DateTime.Now;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (client.Equipment.IsEquipped(itemName, slot))
                        return true;
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var deltaTime = DateTime.Now - startTime;

                if (timeout != TimeSpan.Zero && deltaTime >= timeout)
                    break;

                Thread.Sleep(16);
            }

            return false;
        }

        public static bool WaitForEquipmentEmpty(this Player client, EquipmentSlot slot, TimeSpan timeout,
            CancellationToken token = default)
        {
            var startTime = DateTime.Now;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (client.Equipment.IsEmpty(slot))
                        return true;
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var deltaTime = DateTime.Now - startTime;

                if (timeout == TimeSpan.Zero && deltaTime >= timeout)
                    break;

                Thread.Sleep(16);
            }

            return false;
        }

        public static void SwitchToPanel(this Player client, InterfacePanel panel, out bool didRequireSwitch,
            bool useShiftKey = true)
        {
            didRequireSwitch = false;
            var pt = panel.ToPoint();
            pt = pt.ScalePoint(client.Process.WindowScaleX, client.Process.WindowScaleY);

            var x = (int)pt.X;
            var y = (int)pt.Y;

            var hwnd = client.Process.WindowHandle;

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

        public static bool SwitchToPanelAndWait(this Player client, InterfacePanel panel, out bool didRequireSwitch,
            bool useShiftKey = true, CancellationToken token = default)
            => SwitchToPanelAndWait(client, panel, TimeSpan.Zero, out didRequireSwitch, useShiftKey, token);

        public static bool SwitchToPanelAndWait(this Player client, InterfacePanel panel, TimeSpan timeout,
            out bool didRequireSwitch, bool useShiftKey = true, CancellationToken token = default)
        {
            SwitchToPanel(client, panel, out didRequireSwitch, useShiftKey);
            return WaitForPanel(client, panel, timeout, token);
        }

        public static void WaitForPanel(this Player client, InterfacePanel panel, CancellationToken token = default)
            => WaitForPanel(client, panel, TimeSpan.Zero, token);

        public static bool WaitForPanel(this Player client, InterfacePanel panel, TimeSpan timeout,
            CancellationToken token = default)
        {
            var startTime = DateTime.Now;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var activePanel = client.GameClient.ActivePanel;

                    if (activePanel.IsSameAs(panel))
                        return true;
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var delaTime = DateTime.Now - startTime;

                if (timeout != TimeSpan.Zero && delaTime >= timeout)
                    break;

                Thread.Sleep(16);
            }

            return false;
        }

        public static void Terminate(this Player client)
            => WindowAutomator.SendCloseWindow(client.Process.WindowHandle);

        static int GetRelativeSlot(this InterfacePanel panel, int slot)
        {
            int maxSlotCount = 36;

            if (panel == InterfacePanel.Inventory)
                maxSlotCount = 60;

            if (panel.IsWorldPanel())
                maxSlotCount = 18;

            return slot % maxSlotCount;
        }

        static Point ScalePoint(this Point pt, double scaleX, double scaleY)
        {
            if (scaleX > 0 && Math.Abs(scaleX - 1) > Double.Epsilon)
                pt.X *= scaleX;

            if (scaleY > 0 && Math.Abs(scaleY - 1) > Double.Epsilon)
                pt.Y *= scaleY;

            return pt;
        }
    }
}
