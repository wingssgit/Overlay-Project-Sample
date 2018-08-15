using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverlaySample
{

	//All offsets are generated dynamically via a separate module, the following declarations are old/unnecessary
	public static class Offsets
	{
		public static uint dwEntityList = 0;//Retrieved via pattern scan
		public static uint dwLocalPlayer = 0;//Retrieved via pattern scan
		public static uint dwViewMatrix = 0;//Retrieved via pattern scan


		public static uint IndexOffset = 0x64;
		public static uint BoneMatrix = 0x2698;
		public static uint TeamNum = 0xF0;
		public static uint Health = 0xFC;
		public static uint VecPunchAngle = 0x301C;
		public static uint Dormant = 0xE9;
		public static uint SpottedByMask = 0x97C;
		public static uint VecVelocity = 0x110;
		public static uint VecOrigin = 0x134;

		public static uint ItemDefinitionIndex = 0x2F9A;
		public static uint ActiveWeapon = 0x2EE8;
		public static uint Clip1 = 0x3234;
		public static uint ShotsFired = 0xA2C0;


	}
}
 