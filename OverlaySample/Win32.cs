using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OverlaySample
{
	public static class Win32
	{
		#region Definitions
		public const int GWL_EXSTYLE = -20;

		public const int WS_EX_LAYERED = 0x80000;

		public const int WS_EX_TRANSPARENT = 0x20;

		public const int LWA_ALPHA = 0x2;

		/*
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        */

		public const int GW_HWNDPREV = 3;
		public const int KEY_PRESSED = 0x8000;

		public const UInt32 SWP_NOSIZE = 0x0001;
		public const UInt32 SWP_NOMOVE = 0x0002;
		public const UInt32 SWP_SHOWWINDOW = 0x0040;

		#endregion
		

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint GetOffsets(IntPtr hDriver, int numOffset, uint ModBase, uint ModSize);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr GetNewWindow(uint PID);
		

		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);
		
		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);


		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CreateFile([MarshalAs(UnmanagedType.LPTStr)] string filename,
												[MarshalAs(UnmanagedType.U4)] FileAccess access,
												[MarshalAs(UnmanagedType.U4)] FileShare share,
												IntPtr securityAttributes, // optional SET TO ZERO
												[MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
												[MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
												IntPtr templateFile);

		[DllImport("user32.dll")]
		public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);


		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);


		[DllImport("Kernel32")]
		public extern static Boolean CloseHandle(IntPtr handle);


		[DllImport("user32.dll")]
		public static extern int GetKeyState(int KeyStates);

		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		[DllImport("user32.dll")]
		public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


		[DllImport("user32.dll")]
		public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("dwmapi.dll")]
		public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMargins);




		public struct Margins
		{
			public int Left, Right, Top, Bottom;
		}
	}
}
