using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverlaySample
{
	public class Scan
	{
		private byte[] g_ModBuffer { get; set; }

		private uint g_pModBase { get; set; }

		public Scan(uint ModBaseAddr, byte[] ModuleBuffer, uint SizeOfBuffer)
		{
			g_ModBuffer = new byte[SizeOfBuffer];
			g_ModBuffer = ModuleBuffer;
			g_pModBase = ModBaseAddr;
		}

		private bool PatternCheck(int nOffset, byte[] arrPattern)
		{
			for (int i = 0; i < arrPattern.Length; i++)
			{
				if (arrPattern[i] == 0x0)
					continue;

				if (arrPattern[i] != this.g_ModBuffer[nOffset + i])
					return false;
			}

			return true;
		}

		public uint FindPattern(string szPattern)
		{
			if (g_ModBuffer == null || g_pModBase == 0)
				return 0;

			byte[] bytePattern = PatternToBytes(szPattern);

			for (int nModuleIndex = 0; nModuleIndex < g_ModBuffer.Length; nModuleIndex++)
			{
				if (this.g_ModBuffer[nModuleIndex] != bytePattern[0])
					continue;

				if (PatternCheck(nModuleIndex, bytePattern))
				{
					return g_pModBase + (uint)nModuleIndex;
				}
			}
			return 0;
		}

		public uint FindPattern(byte[] bytePattern)
		{
			if (g_ModBuffer == null || g_pModBase == 0)
				return 0;


			for (int nModuleIndex = 0; nModuleIndex < g_ModBuffer.Length; nModuleIndex++)
			{
				if (this.g_ModBuffer[nModuleIndex] != bytePattern[0])
					continue;

				if (PatternCheck(nModuleIndex, bytePattern))
				{
					return g_pModBase + (uint)nModuleIndex;
				}
			}
			return 0;
		}

		private byte[] PatternToBytes(string szPattern)
		{
			List<byte> patternbytes = new List<byte>();

			foreach (var szByte in szPattern.Split(' '))
				patternbytes.Add(szByte == "?" ? (byte)0x0 : Convert.ToByte(szByte, 16));

			return patternbytes.ToArray();
		}


	}
}
