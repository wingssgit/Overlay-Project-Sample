using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace OverlaySample
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// 
		/// </summary>
		[STAThread]
		static void Main()
		{
			uint highestPID = 0;
			string overlayProcess = "NVIDIA Share"; //Process to borrow overlay window from
			Process[] ProcessList = Process.GetProcessesByName(overlayProcess);
			if (ProcessList.Length > 0)
			{
				foreach (Process foundProc in ProcessList.ToList())
				{
					if (foundProc.Id > highestPID)
						highestPID = (uint)foundProc.Id;
				}
			}
			else
			{
				MessageBox.Show($"Could not find process: {overlayProcess}");
				Environment.Exit(0);
			}

			G.PID = highestPID;

			G.Driver = IntPtr.Zero;
			G.Memory = new Mx();
			if (!M.Init())
			{
				MessageBox.Show($"Initialization failed.");
				Environment.Exit(0);
			}

			byte[] modImage = M.Read(G.clientDLL, G.clientSize);
			Scan Scanner = new Scan(G.clientDLL, modImage, G.clientSize);
			uint foundAddr = Scanner.FindPattern("A3 ? ? ? ? C7 05 ? ? ? ? ? ? ? ? E8 ? ? ? ? 59 C3 6A");
			uint localPlayerAdd = M.Read<uint>(foundAddr + 1) + 16;
			Offsets.dwLocalPlayer = localPlayerAdd - G.clientDLL;

			foundAddr = Scanner.FindPattern("BB ? ? ? ? 83 FF 01 0F 8C ? ? ? ? 3B F8");
			G.PlayerList = M.Read<uint>(foundAddr + 1);
			Offsets.dwEntityList = G.PlayerList - G.clientDLL;


			foundAddr = Scanner.FindPattern("0F 10 05 ? ? ? ? 8D 85 ? ? ? ? B9");
			uint viewMatrixAdd = M.Read<uint>(foundAddr + 3) + 176;
			Offsets.dwViewMatrix = viewMatrixAdd - G.clientDLL;

			G.PlayerList = G.clientDLL + Offsets.dwEntityList;

			G.Players = new List<Player>();
			

			G.WeaponID = 0;
			G.ShotsFired = 0;
			G.Clip1 = 0;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new main());
		}
	}
}
