using System.Windows;

namespace SleepHunter.Models
{
    public static class InterfacePanelExtender
    {
        public static bool IsSameAs(this InterfacePanel panel, InterfacePanel target)
        {
            return panel == target || (panel.IsWorldPanel() && target.IsWorldPanel());
        }

        public static bool IsTemuairPanel(this InterfacePanel panel)
        {
            return !IsMedeniaPanel(panel) && !IsWorldPanel(panel);
        }

        public static bool IsMedeniaPanel(this InterfacePanel panel)
        {
            return (panel == InterfacePanel.MedeniaSkills || panel == InterfacePanel.MedeniaSpells);
        }

        public static bool IsWorldPanel(this InterfacePanel panel)
        {
            return (panel == InterfacePanel.WorldSkills || panel == InterfacePanel.WorldSpells);
        }

        public static bool IsSharedPanel(this InterfacePanel panel, InterfacePanel target)
        {
            if (panel == InterfacePanel.TemuairSkills && target == InterfacePanel.MedeniaSkills)
                return true;

            if (panel == InterfacePanel.MedeniaSkills && target == InterfacePanel.TemuairSkills)
                return true;

            if (panel == InterfacePanel.TemuairSpells && target == InterfacePanel.MedeniaSpells)
                return true;

            if (panel == InterfacePanel.MedeniaSpells && target == InterfacePanel.TemuairSpells)
                return true;

            if (panel == InterfacePanel.Chat && target == InterfacePanel.ChatHistory)
                return true;

            if (panel == InterfacePanel.ChatHistory && target == InterfacePanel.Chat)
                return true;

            if (panel == InterfacePanel.Stats && target == InterfacePanel.Modifiers)
                return true;

            if (panel == InterfacePanel.Modifiers && target == InterfacePanel.Stats)
                return true;

            return false;
        }

        public static bool IsSkillPanel(this InterfacePanel panel)
        {
            return (panel == InterfacePanel.TemuairSkills ||
               panel == InterfacePanel.MedeniaSkills ||
               panel == InterfacePanel.WorldSkills);
        }

        public static bool IsSpellPanel(this InterfacePanel panel)
        {
            return (panel == InterfacePanel.TemuairSpells ||
               panel == InterfacePanel.MedeniaSpells ||
               panel == InterfacePanel.WorldSpells);
        }

        public static bool IsTemuairToMedenia(this InterfacePanel panel, InterfacePanel target)
        {
            return (panel == InterfacePanel.TemuairSkills && target == InterfacePanel.MedeniaSkills) ||
               (panel == InterfacePanel.TemuairSpells && target == InterfacePanel.MedeniaSpells);
        }

        public static bool IsMedeniaToTemuair(this InterfacePanel panel, InterfacePanel target)
        {
            return (panel == InterfacePanel.MedeniaSkills && target == InterfacePanel.TemuairSkills) ||
               (panel == InterfacePanel.MedeniaSpells && target == InterfacePanel.TemuairSpells);
        }

        public static Point ToPoint(this InterfacePanel panel)
        {
            var pt = new Point(545, 0);

            switch (panel)
            {
                case InterfacePanel.Inventory:
                    pt.Y = 340;
                    break;

                case InterfacePanel.TemuairSkills:
                    pt.Y = 360;
                    break;

                case InterfacePanel.MedeniaSkills:
                    goto case InterfacePanel.TemuairSkills;

                case InterfacePanel.TemuairSpells:
                    pt.Y = 390;
                    break;

                case InterfacePanel.MedeniaSpells:
                    goto case InterfacePanel.TemuairSpells;

                case InterfacePanel.Chat:
                    pt.Y = 410;
                    break;

                case InterfacePanel.ChatHistory:
                    goto case InterfacePanel.Chat;

                case InterfacePanel.Stats:
                    pt.Y = 435;
                    break;

                case InterfacePanel.Modifiers:
                    goto case InterfacePanel.Stats;

                case InterfacePanel.WorldSkills:
                    pt.Y = 460;
                    break;

                case InterfacePanel.WorldSpells:
                    goto case InterfacePanel.WorldSkills;

                default:
                    pt = new Point(-1, -1);
                    break;
            }

            return pt;
        }

        public static Point ToSlotPoint(this InterfacePanel panel, int slot, bool isExpandedInventory = false, int iconWidth = 35, int iconHeight = 35)
        {
            Point pt;

            if (isExpandedInventory)
                pt = new Point(110, 285);
            else
                pt = new Point(110, 350);

            slot %= (Inventory.InventoryCount + 1);

            var rowSize = panel.IsWorldPanel() ? 6 : 12;
            var rowOffset = 0;
            var columnOffset = (panel == InterfacePanel.WorldSpells) ? 6 : 0;

            var row = ((slot - 1) / rowSize) + rowOffset;
            var column = ((slot - 1) % rowSize) + columnOffset;

            pt.Offset(column * iconWidth, row * iconHeight);

            return pt;
        }
    }
}
