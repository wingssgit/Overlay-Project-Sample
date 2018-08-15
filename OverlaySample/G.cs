using System;
using System.Collections.Generic;
using SharpDX;

namespace OverlaySample
{
	public class G
	{
		public static uint clientDLL { get; set; }
		public static uint clientSize { get; set; }
		public static uint PID { get; set; }
		public static uint PlayerList { get; set; }
		public static int MyTeam { get; set; }
		public static int Clip1 { get; set; }
		public static int ShotsFired { get; set; }
		public static int WeaponID { get; set; }
		public static uint pWeapon { get; set; }
		public static WeaponType MyWeaponType { get; set; }
		public static uint pLocalPlayer { get; set; }
		public static IntPtr Driver { get; set; }
		
		public static int MyIndex { get; set; }
		public static Vector3 MyLocation { get; set; }
		public static List<Player> Players { get; set; }
		public static Mx Memory { get; set; }
	}
}
