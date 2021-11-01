using Microsoft.Win32.SafeHandles;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

//using System.Drawing;           // NOTE: Project + Add Reference required
//using System.Windows.Forms;     // NOTE: Project + Add Reference required

namespace Generic {
	#pragma warning disable CA1416//just shut the FUCK up
	public static class ConsoleExtensions {
        public static void WaitForEnter(string message) {
            Console.Write(message);
			Console.CursorVisible = true;

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
			while(keyInfo.Key != ConsoleKey.Enter)
				keyInfo = Console.ReadKey(true);
        }

		public static void ClearLine(int line) {
			Console.SetCursorPosition(0, line);
			Console.Write(new string(' ', Console.WindowWidth));//causes CursorTop to increment
			Console.SetCursorPosition(0, line);
		}

		public static bool RefreshWindow(CharInfo[] buffer) {
			SafeFileHandle h = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

			if (h.IsInvalid) return false;
			else {
				SmallRect rect = new SmallRect() { Left = 0, Top = 0, Right = (ushort)Console.WindowWidth, Bottom = (ushort)Console.WindowHeight };
				return WriteConsoleOutput(h, buffer,
				  new Coord() { X = (ushort)Console.WindowWidth, Y = (ushort)Console.WindowHeight },
				  new Coord() { X = 0, Y = 0 },
				  ref rect);
			}
		}

		public static void HideScrollbars() {
			int
				width = Console.WindowWidth,
				height = Console.WindowHeight;
			Console.SetWindowSize(width - 2, height);
			Console.SetBufferSize(width, height);

			IntPtr stdHandle = GetStdHandle(-11);
			ConsoleScreenBufferInfoEx bufferInfo = new ConsoleScreenBufferInfoEx();
			bufferInfo.cbSize = (uint)Marshal.SizeOf(bufferInfo);
			GetConsoleScreenBufferInfoEx(stdHandle, ref bufferInfo);
			++bufferInfo.srWindow.Right;
			++bufferInfo.srWindow.Bottom;
			SetConsoleScreenBufferInfoEx(stdHandle, ref bufferInfo);
		}
		
/*
		// P/Invoke declarations
		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT rc);
		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);


        public static IntPtr HWND_BOTTOM = (IntPtr)1;
        public static IntPtr HWND_TOP = (IntPtr)0;
		public static void SetWindowPosition()
		{
			IntPtr hWin = GetConsoleWindow();
			RECT rc;
			GetWindowRect(hWin, out rc);
			Screen scr = Screen.FromPoint(new Point(rc.left, rc.top));
			int left = scr.WorkingArea.Left + (scr.WorkingArea.Width - (rc.right - rc.left)) / 2;
			int top = scr.WorkingArea.Top + (scr.WorkingArea.Height - (rc.bottom - rc.top)) / 2;
			MoveWindow(hWin, left, top, rc.right - rc.left, rc.bottom - rc.top, false);
			//SetWindowPos(Handle, IntPtr.Zero, x, y, 0, 0, SWP_NOZORDER | SWP_NOACTIVATE);
		}
*/
		//public static void MoveWindow() {
		//	// Get this console window's hWnd (window handle).
		//	IntPtr hWnd = GetConsoleWindow();

		//	// Get information about the monitor (display) that the window is (mostly) displayed on.
		//	// The .rcWork field contains the monitor's work area, i.e., the usable space excluding
		//	// the taskbar (and "application desktop toolbars" - see https://msdn.microsoft.com/en-us/library/windows/desktop/ms724947(v=vs.85).aspx)
		//	var mi = MONITORINFO.Default;
		//	GetMonitorInfo(MonitorFromWindow(hWnd, MONITOR_DEFAULTTOPRIMARY), ref mi);

		//	// Get information about this window's current placement.
		//	var wp = WINDOWPLACEMENT.Default;
		//	GetWindowPlacement(hWnd, ref wp);

		//	// Calculate the window's new position: lower left corner.
		//	// !! Inexplicably, on W10, work-area coordinates (0,0) appear to be (7,7) pixels 
		//	// !! away from the true edge of the screen / taskbar.
		//	int fudgeOffset = 7;
		//	wp.NormalPosition = new RECT() {
		//		Left = -fudgeOffset,
		//		Top = mi.rcWork.Bottom - (wp.NormalPosition.Bottom - wp.NormalPosition.Top),
		//		Right = (wp.NormalPosition.Right - wp.NormalPosition.Left),
		//		Bottom = fudgeOffset + mi.rcWork.Bottom
		//	};

		//	// Place the window at the new position.
		//	SetWindowPlacement(hWnd, ref wp);
		//}

		#region extern
		public const int SWP_NOZORDER = 0x4;
		public const int SWP_NOACTIVATE = 0x10;

		[DllImport("user32.dll")]
		static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

		public const int MONITOR_DEFAULTTOPRIMARY = 1;

		[DllImport("user32.dll")]
		static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
		

		[DllImport("user32")]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

		[StructLayout(LayoutKind.Sequential)]
		struct MONITORINFO
		{
			public uint cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public uint dwFlags;
			public static MONITORINFO Default
			{
				get { MONITORINFO inst= new MONITORINFO(); inst.cbSize = (uint)Marshal.SizeOf(inst); return inst; }
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct RECT
		{
			public int Left, Top, Right, Bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct POINT
		{
			public int x, y;
		}

		public static IntPtr Handle
		{
			get
			{
				//Initialize();
				return GetConsoleWindow();
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

		public const uint SW_RESTORE= 9;

		[StructLayout(LayoutKind.Sequential)]
		struct WINDOWPLACEMENT
		{
			public uint Length;
			public uint Flags;
			public uint ShowCmd;
			public POINT MinPosition;
			public POINT MaxPosition;
			public RECT NormalPosition;
			public static WINDOWPLACEMENT Default
			{
				get
				{
					WINDOWPLACEMENT instance = new WINDOWPLACEMENT();
					instance.Length = (uint) Marshal.SizeOf(instance);
					return instance;
				}
			}
		}


		[DllImport("Kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		static extern SafeFileHandle CreateFile(
			string fileName,
			[MarshalAs(UnmanagedType.U4)] uint fileAccess,
			[MarshalAs(UnmanagedType.U4)] uint fileShare,
			IntPtr securityAttributes,
			[MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
			[MarshalAs(UnmanagedType.U4)] int flags,
			IntPtr template);

		[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		static extern bool WriteConsoleOutput(
		  SafeFileHandle hConsoleOutput,
		  CharInfo[] lpBuffer,
		  Coord dwBufferSize,
		  Coord dwBufferCoord,
		  ref SmallRect lpWriteRegion);

		[StructLayout(LayoutKind.Sequential)]
		public struct Coord {
			public ushort X;
			public ushort Y;

			public Coord(ushort X, ushort Y) {
				this.X = X;
				this.Y = Y;
			}
		};

		[StructLayout(LayoutKind.Explicit, CharSet=CharSet.Unicode)]
		public struct CharUnion {
			[FieldOffset(0)] public char UnicodeChar;
			[FieldOffset(0)] public byte AsciiChar;
		}

		[StructLayout(LayoutKind.Explicit, CharSet=CharSet.Unicode)]
		public struct CharInfo {
			[FieldOffset(0)] public CharUnion Char;
			[FieldOffset(2)] public ushort Attributes;

			public CharInfo(char character, ConsoleColor? foreground = null, ConsoleColor? background = null) {
				this.Char = new CharUnion() { UnicodeChar = character };
				this.Attributes = (ushort)((int)(foreground ?? 0) | (((int)(background ?? 0)) << 4));
			}
			public CharInfo(byte character, ConsoleColor? foreground = null, ConsoleColor? background = null) {
				this.Char = new CharUnion() { AsciiChar = character };
				this.Attributes = (ushort) ((int)(foreground ?? 0) | (((int)(background ?? 0))<<4));
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SmallRect {
			public ushort Left;
			public ushort Top;
			public ushort Right;
			public ushort Bottom;
		}
		//copied from
		//https://social.msdn.microsoft.com/Forums/vstudio/en-US/1aa43c6c-71b9-42d4-aa00-60058a85f0eb/c-console-window-disable-resize?forum=csharpgeneral
		public const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;
        public const int SC_MINIMIZE = 0xF020;
        public const int SC_MAXIMIZE = 0xF030;
        public const int SC_SIZE = 0xF000;
        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
		public static void DisableAllResizing() {
			IntPtr handle = GetConsoleWindow();
            IntPtr sysMenu = GetSystemMenu(handle, false);

            if (handle != IntPtr.Zero)
            {
                //DeleteMenu(sysMenu, SC_CLOSE, MF_BYCOMMAND);
                //DeleteMenu(sysMenu, SC_MINIMIZE, MF_BYCOMMAND);
                DeleteMenu(sysMenu, SC_MAXIMIZE, MF_BYCOMMAND);
                DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);
            }

			Console.CursorVisible = false;
		}

		//copied from
		//https://stackoverflow.com/a/58381272/2799848
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GetConsoleScreenBufferInfoEx(
			IntPtr hConsoleOutput,
			ref ConsoleScreenBufferInfoEx ConsoleScreenBufferInfo);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleScreenBufferInfoEx(
			IntPtr hConsoleOutput,
			ref ConsoleScreenBufferInfoEx ConsoleScreenBufferInfoEx);

		[StructLayout(LayoutKind.Sequential)]
		private struct ConsoleScreenBufferInfoEx
		{
			public uint cbSize;
			public Coord dwSize;
			public Coord dwCursorPosition;
			public short wAttributes;
			public SmallRect srWindow;
			public Coord dwMaximumWindowSize;
			public ushort wPopupAttributes;
			public bool bFullscreenSupported;

			public Colorref black, darkBlue, darkGreen, darkCyan, darkRed, darkMagenta, darkYellow, gray, darkGray, blue, green, cyan, red, magenta, yellow, white;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Colorref
		{
			public uint ColorDWORD;
		}
		#endregion extern

		//Copied from
		//https://stackoverflow.com/a/28372546/2799848
		//#region Native methods
		//[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		//static extern SafeFileHandle CreateFile(
		//    string fileName,
		//    [MarshalAs(UnmanagedType.U4)] uint fileAccess,
		//    [MarshalAs(UnmanagedType.U4)] uint fileShare,
		//    IntPtr securityAttributes,
		//    [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
		//    [MarshalAs(UnmanagedType.U4)] int flags,
		//    IntPtr template);
		//[StructLayout(LayoutKind.Sequential)]
		//public struct Coord
		//{
		//    public short X;
		//    public short Y;

		//    public Coord(short X, short Y)
		//    {
		//        this.X = X;
		//        this.Y = Y;
		//    }
		//};
		//[DllImport("kernel32.dll", SetLastError = true)]
		//    static extern bool WriteConsoleOutputCharacter(
		//    SafeFileHandle hConsoleOutput,
		//    string lpCharacter,
		//    int nLength,
		//    Coord dwWriteCoord,
		//    ref int lpumberOfCharsWritten);
		//#endregion
		//public static void DrawDirect(int x, int y, char c, ConsoleColor? color = null, bool excludeCorner = false) {
  //          bool isCorner = false;
  //          if (!excludeCorner
  //          || !(isCorner = x == Console.WindowWidth - 1 && y == Console.WindowHeight - 1)) {
  //              Console.SetCursorPosition(x, y);
  //              if (color.HasValue) Console.ForegroundColor = color.Value;
  //              Console.Write(c);
  //              if (isCorner) Console.SetWindowPosition(0 , 0);//window bounce :(
  //          }
               
  //          /*
  //          // The handle to the output buffer of the console
  //          SafeFileHandle consoleHandle = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

  //          // Draw with this native method because this method does NOT move the cursor.
  //          int n = 0;
  //          WriteConsoleOutputCharacter(consoleHandle, c.ToString(), 1, new Coord((short)x, (short)y), ref n);
  //          */
  //      }
	}
}