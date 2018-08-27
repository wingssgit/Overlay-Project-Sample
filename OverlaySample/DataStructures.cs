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
	public struct ViewMatrix
	{
		public float m0;
		public float m1;
		public float m2;
		public float m3;
		public float m4;
		public float m5;
		public float m6;
		public float m7;
		public float m8;
		public float m9;
		public float m10;
		public float m11;
		public float m12;
		public float m13;
		public float m14;
		public float m15;
		
	}
	

	[StructLayout(LayoutKind.Sequential)]
	public class Player
	{
		public uint BaseAddress;
		public int Index;
		public int Health;
		public Vector3 TargetBoneLocation;
		public int Distance;
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
