using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Macronana
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private Process targetProcess;
        private bool macroRunning = false;
        private bool stopMacro = false;

        private const int VK_F9 = 0x78; // F9 key
        private const int VK_F8 = 0x77; // F8 key
        private const int SW_RESTORE = 9;
        private const int WM_HOTKEY = 0x0312;
        const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        const uint MOUSEEVENTF_LEFTUP = 0x04;

        public Form1()
        {
            InitializeComponent();

            // Register F9 hotkey (ID = 1) with MOD_NONE (no modifier key required)
            RegisterHotKey(this.Handle, 1, 0, VK_F9);

            // Register F8 hotkey (ID = 2) with MOD_NONE (no modifier key required)
            RegisterHotKey(this.Handle, 2, 0, VK_F8);
        }

        private IntPtr FindWindowHandle(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                return processes[0].MainWindowHandle;
            }
            return IntPtr.Zero;
        }

        private void ActivateWindow(IntPtr hWnd)
        {
            if (hWnd != IntPtr.Zero)
            {
                // Check if window is minimized, if yes, restore it
                if (IsIconic(hWnd))
                {
                    ShowWindow(hWnd, SW_RESTORE);
                }

                // Bring the window to the foreground
                SetForegroundWindow(hWnd);
            }
        }

        private void HandleF9KeyPressed()
        {
            // Stop macro logic
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => btnStop_Click(this, EventArgs.Empty)));
            }
            else
            {
                btnStop_Click(this, EventArgs.Empty);
            }
        }

        private void HandleF8KeyPressed()
        {
            // Start macro logic
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => btnStart_Click(this, EventArgs.Empty)));
            }
            else
            {
                btnStart_Click(this, EventArgs.Empty);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                switch (id)
                {
                    case 1: // ID for ESC hotkey
                        HandleF9KeyPressed();
                        break;
                    case 2: // ID for HOME hotkey
                        HandleF8KeyPressed();
                        break;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Unregister F9 hotkey
            UnregisterHotKey(this.Handle, 1);

            // Unregister F8 hotkey
            UnregisterHotKey(this.Handle, 2);

            base.OnFormClosing(e);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!macroRunning)
            {
                // Read inputs from textboxes
                string processName = "Banana";
                IntPtr hWnd = FindWindowHandle(processName);
                if (hWnd != IntPtr.Zero)
                {
                    ActivateWindow(hWnd);

                    int yOffset = 50;
                    int clickInterval = 1;

                    // Find process by name
                    targetProcess = FindProcessByName(processName + ".exe");
                    if (targetProcess == null)
                    {
                        MessageBox.Show($"Process '{processName}.exe' not found!");
                        return;
                    }

                    // Start the macro
                    stopMacro = false;
                    macroRunning = true;

                    Task.Run(() => RunMacroInBackground(targetProcess.MainWindowHandle, yOffset, clickInterval));
                    lblStatus.Text = "Macro running...";
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            stopMacro = true;
            macroRunning = false;
        }

            lblStatus.Text = "Macro stopped.";
        private void RunMacroInBackground(IntPtr hWnd, int yOffset, int clickInterval)
        {
            while (!stopMacro)
            {
                // Activate the window
                SetForegroundWindow(hWnd);

                // Get window position
                RECT rect;
                if (!GetWindowRect(hWnd, out rect))
                {
                    MessageBox.Show("Failed to get window rectangle!");
                    return;
                }

                // Calculate center coordinates of the window
                int centerX = (rect.Left + rect.Right) / 2;
                int centerY = (rect.Top + rect.Bottom) / 2 + yOffset;

                // Move cursor to the center of the window
                SetCursorPos(centerX, centerY);

                // Simulate left mouse button click
                SimulateMouseClick();

                Thread.Sleep(clickInterval); // Wait before sending the next click
            }
        }

        private void SimulateMouseClick()
        {
            // Simulate left mouse button down
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            // Simulate left mouse button up
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        private Process FindProcessByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(processName));
            if (processes.Length > 0)
                return processes[0];
            return null;
        }
    }
}
