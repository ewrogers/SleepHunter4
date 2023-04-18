using System.Text;
using System.Windows.Input;

namespace SleepHunter.Macro
{
    internal sealed class Hotkey
    {
        private string atomName;
        private ModifierKeys modifiers;
        private Key key;
        private int id = -1;

        public string AtomName
        {
            get { return atomName; }
            set { atomName = value; }
        }

        public ModifierKeys Modifiers
        {
            get { return modifiers; }
            private set { modifiers = value; }
        }

        public Key Key
        {
            get { return key; }
            private set { key = value; }
        }

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public bool IsActive
        {
            get { return id >= 0; }
        }

        public Hotkey(ModifierKeys modifiers, Key key)
        {
            this.modifiers = modifiers;
            this.key = key;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (modifiers.HasFlag(ModifierKeys.Control))
            {
                if (sb.Length > 0)
                    sb.Append("+");

                sb.Append("Control");
            }

            if (modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (sb.Length > 0)
                    sb.Append("+");

                sb.Append("Alt");
            }

            if (modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (sb.Length > 0)
                    sb.Append("+");

                sb.Append("Shift");
            }

            if (sb.Length > 0)
                sb.Append("+");

            sb.Append(GetKeyFriendlyName(key));

            return sb.ToString();
        }

        public static bool IsFunctionKey(Key key)
        {
            return key == Key.F1 ||
               key == Key.F2 ||
               key == Key.F3 ||
               key == Key.F4 ||
               key == Key.F5 ||
               key == Key.F6 ||
               key == Key.F7 ||
               key == Key.F8 ||
               key == Key.F9 ||
               key == Key.F10 ||
               key == Key.F11 ||
               key == Key.F12;
        }

        public static string GetKeyFriendlyName(Key key)
        {
            switch (key)
            {
                case Key.Oem1:
                    return "Semi-Colon";
                case Key.Oem3:
                    return "Quote";
                case Key.Oem6:
                    return "]";
                case Key.OemOpenBrackets:
                    return "[";
                case Key.OemQuotes:
                    return @"\";
                case Key.OemQuestion:
                    return "/";
                case Key.Oem5:
                    return "Tilde";

                case Key.D0:
                    return "0";
                case Key.D1:
                    return "1";
                case Key.D2:
                    return "2";
                case Key.D3:
                    return "3";
                case Key.D4:
                    return "4";
                case Key.D5:
                    return "5";
                case Key.D6:
                    return "6";
                case Key.D7:
                    return "7";
                case Key.D8:
                    return "8";
                case Key.D9:
                    return "9";

                case Key.NumPad0:
                    return "Num0";
                case Key.NumPad1:
                    return "Num1";
                case Key.NumPad2:
                    return "Num2";
                case Key.NumPad3:
                    return "Num3";
                case Key.NumPad4:
                    return "Num4";
                case Key.NumPad5:
                    return "Num5";
                case Key.NumPad6:
                    return "Num6";
                case Key.NumPad7:
                    return "Num7";
                case Key.NumPad8:
                    return "Num8";
                case Key.NumPad9:
                    return "Num9";

                case Key.OemMinus:
                    return "Minus";
                case Key.OemPlus:
                    return "Plus";
                case Key.Divide:
                    return "Divide";
                case Key.Multiply:
                    return "Multiply";
                case Key.OemPeriod:
                    return "Period";
                case Key.OemComma:
                    return "Comma";

                case Key.Back:
                    return "Backspace";
                case Key.Capital:
                    return "CapsLock";

                default:
                    return key.ToString();
            }
        }
    }
}
