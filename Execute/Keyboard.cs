using System.Runtime.InteropServices;

namespace AutoClick.Execute
{
    internal class Keyboard
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern ushort VkKeyScan(char ch);

        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_SHIFT = 0x10;

        public static void KeyPress(byte keyCode)
        {
            keybd_event(keyCode, 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
            keybd_event(keyCode, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        }

        public static void Type(string text, int interval = 10)
        {
            foreach (char c in text)
            {
                bool shiftDown = false;
                bool isSpecial = IsSpecialCharacter(c);

                if (char.IsUpper(c) || isSpecial)
                {
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
                    shiftDown = true;
                }

                ushort keyCode = VkKeyScan(c);
                byte key = (byte)(keyCode & 0xFF);
                byte scan = (byte)((keyCode >> 8) & 0xFF);

                keybd_event(key, scan, KEYEVENTF_KEYDOWN, IntPtr.Zero);
                Thread.Sleep(interval);
                keybd_event(key, scan, KEYEVENTF_KEYUP, IntPtr.Zero);

                if (shiftDown)
                {
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
                    Thread.Sleep(interval);
                }
            }
        }

        private static bool IsSpecialCharacter(char c)
        {
            // Define which characters are considered "special"
            string specialCharacters = "!@#$%^&*()";

            return specialCharacters.Contains(c);
        }
    }
}
