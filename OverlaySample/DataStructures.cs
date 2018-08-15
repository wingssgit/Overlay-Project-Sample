using System;
using System.Runtime.InteropServices;

namespace OverlaySample
{
	public enum WeaponType
	{
		Unknown = -1,
		Knife = 0,
		Pistol = 1,
		Smg = 2,
		Rifle = 3,
		Sniper = 4,
		Shotgun = 5,
		Heavy = 6,
		Grenade = 7,
		Tazer = 8,
		C4 = 9
	}
	

	[StructLayout(LayoutKind.Sequential)]
	public class Player
	{
		public uint BaseAddress;
		public int index;
		public int health;
		public Vector3 LocationHead;
		public int distance;
		public int bSpotted;
		public int bDormant;
		
	}
	

	[StructLayout(LayoutKind.Sequential)]
	public struct Vector3
	{
		public float X;
		public float Y;
		public float Z;

		public Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}

		public double Length
		{
			get
			{
				return Math.Sqrt((this.X * this.X) + (this.Y * this.Y) + (this.Z * this.Z));
			}
		}

		#region Operators
		public static Vector3 operator +(Vector3 vecA, Vector3 vecB) => new Vector3(vecA.X + vecB.X, vecA.Y + vecB.Y, vecA.Z + vecB.Z);
		public static Vector3 operator -(Vector3 vecA, Vector3 vecB) => new Vector3(vecA.X - vecB.X, vecA.Y - vecB.Y, vecA.Z - vecB.Z);
		public static Vector3 operator *(Vector3 vecA, Vector3 vecB) => new Vector3(vecA.X * vecB.X, vecA.Y * vecB.Y, vecA.Z * vecB.Z);
		
		#endregion
	}






	
}
