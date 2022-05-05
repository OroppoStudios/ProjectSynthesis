using System.Runtime.InteropServices;

namespace Warner
	{
	public static class StickyKeys
		{
		#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

		private static Stickykey originalStickyKeys;
		private static FilterKey originalFilterKeys;
		private static Stickykey originalToggleKeys;
		private static bool disabled;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct Stickykey
			{
			public uint cbSize;
			public uint dwFlags;
			}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct FilterKey
			{
			public uint cbSize;
			public uint dwFlags;
			public uint iWaitMSec;
			public uint iDelayMSec;
			public uint iRepeatMSec;
			public uint iBounceMSec;
			}

		private const uint SPI_GETFILTERKEYS = 0x0032;
		private const uint SPI_SETFILTERKEYS = 0x0033;
		private const uint SPI_GETTOGGLEKEYS = 0x0034;
		private const uint SPI_SETTOGGLEKEYS = 0x0035;
		private const uint SPI_GETSTICKYKEYS = 0x003A;
		private const uint SPI_SETSTICKYKEYS = 0x003B;
		private const uint SKF_STICKYKEYSON = 0x00000001;
		private const uint TKF_TOGGLEKEYSON = 0x00000001;
		private const uint SKF_CONFIRMHOTKEY = 0x00000008;
		private const uint SKF_HOTKEYACTIVE = 0x00000004;
		private const uint TKF_CONFIRMHOTKEY = 0x00000008;
		private const uint TKF_HOTKEYACTIVE = 0x00000004;
		private const uint FKF_CONFIRMHOTKEY = 0x00000008;
		private const uint FKF_HOTKEYACTIVE = 0x00000004;
		private const uint stickyKeySize = sizeof(uint)*2;
		private const uint filterKeySize = sizeof(uint)*6;


		[DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = false)]
		private static extern bool SystemParametersInfo(uint action, uint param, ref Stickykey vparam, uint init);

		[DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = false)]
		private static extern bool SystemParametersInfo(uint action, uint param, ref FilterKey vparam, uint init);


		public static void disable()
			{
			if (disabled)
				return;

			disabled = true;

			//backup original
			originalStickyKeys.cbSize = stickyKeySize;
			originalToggleKeys.cbSize = stickyKeySize;
			originalFilterKeys.cbSize = filterKeySize;

			SystemParametersInfo(SPI_GETSTICKYKEYS, stickyKeySize, ref originalStickyKeys, 0);
			SystemParametersInfo(SPI_GETTOGGLEKEYS, stickyKeySize, ref originalToggleKeys, 0);
			SystemParametersInfo(SPI_GETFILTERKEYS, filterKeySize, ref originalFilterKeys, 0);

			//disable them
			Stickykey stickyKey = originalStickyKeys;
			stickyKey.dwFlags &= ~SKF_HOTKEYACTIVE;
			stickyKey.dwFlags &= ~SKF_CONFIRMHOTKEY;
			SystemParametersInfo(SPI_SETSTICKYKEYS, stickyKeySize, ref stickyKey, 0);

			Stickykey toggleKey = originalToggleKeys;           
			toggleKey.dwFlags &= ~TKF_HOTKEYACTIVE;
			toggleKey.dwFlags &= ~TKF_CONFIRMHOTKEY;
			SystemParametersInfo(SPI_SETTOGGLEKEYS, stickyKeySize, ref toggleKey, 0);

			FilterKey filterKey = originalFilterKeys;
			filterKey.dwFlags &= ~FKF_HOTKEYACTIVE;
			filterKey.dwFlags &= ~FKF_CONFIRMHOTKEY;
			SystemParametersInfo(SPI_SETFILTERKEYS, filterKeySize, ref filterKey, 0);
			}


		public static void restore()
			{
			if (!disabled)
				return;

			SystemParametersInfo(SPI_SETSTICKYKEYS, stickyKeySize, ref originalStickyKeys, 0);
			SystemParametersInfo(SPI_SETTOGGLEKEYS, stickyKeySize, ref originalToggleKeys, 0);
			SystemParametersInfo(SPI_SETFILTERKEYS, filterKeySize, ref originalFilterKeys, 0);
			}

		#else
		public static void disable()
			{
			}

		public static void restore()
			{

			}
		#endif
		}
	}