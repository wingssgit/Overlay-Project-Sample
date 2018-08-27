using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace OverlaySample
{
	public static class M
	{
		public static T Read<T>(uint Address) where T : struct => G.Memory.Read<T>(Address);
		public static byte[] Read(uint Address, uint size) => G.Memory.Read(Address, (int)size);
		public static bool Init() => G.Memory.Init();
	}
	public class Mx
	{
		//Driver Stuff
		private System.Object lockThis = new System.Object();
		private const uint IO_READ = 0x221C0C;
		private const uint IO_MOD = 0x221C10;
		

		private struct KReadRequest
		{
			public uint address;
			public uint size;
			public IntPtr bytes;
		}
		
		private struct KModRequest
		{
			public uint BaseAddress;
			public uint ModuleSize;
		}
		
		public bool Init()
		{
			G.Driver = Win32.CreateFile("\\\\.\\cdport64", FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
			
			if (G.Driver == null || G.Driver == IntPtr.Zero)
			{
				MessageBox.Show("Failed getting driver handle.");
				Environment.Exit(0);
				return false;
			}
			

			Thread.Sleep(250);
			G.clientDLL = GetBaseAddress();

			if (G.clientDLL != 0)
			{
				//Load external library to dump game offsets & free library once finished to minimize detection potential
				IntPtr pDll = Win32.LoadLibrary(@"Offsets.dll");
				IntPtr pFunction = Win32.GetProcAddress(pDll, "GetOffsets");
				Win32.GetOffsets getOffsets = (Win32.GetOffsets)Marshal.GetDelegateForFunctionPointer(pFunction, typeof(Win32.GetOffsets));

				uint[] offsets = new uint[16];
				for (int i = 0; i < offsets.Length; i++)
				{
					if (offsets[i] == 0)
					{
						MessageBox.Show($"Failed to find offset[{i}]. Update required.");
						Win32.FreeLibrary(pDll);
						return false;
					}
					offsets[i] = getOffsets(G.Driver, i, G.clientDLL, G.clientSize);

				}
				Win32.FreeLibrary(pDll);

				Offsets.dwEntityList = offsets[0];
				Offsets.dwLocalPlayer = offsets[1];
				Offsets.dwViewMatrix = offsets[2];
				Offsets.IndexOffset = offsets[3];
				Offsets.BoneMatrix = offsets[4];
				Offsets.TeamNum = offsets[5];
				Offsets.Health = offsets[6];
				Offsets.VecPunchAngle = offsets[7];
				Offsets.Dormant = offsets[8];
				Offsets.SpottedByMask = offsets[9];
				Offsets.VecVelocity = offsets[10];
				Offsets.VecOrigin = offsets[11];
				Offsets.ItemDefinitionIndex = offsets[12];
				Offsets.ActiveWeapon = offsets[13];
				Offsets.Clip1 = offsets[14];
				Offsets.ShotsFired = offsets[15];
				

				return true;
			}
			else
			{
				MessageBox.Show("Client.dll not found.");
				return false;
			}
				
		}
		

		private uint GetBaseAddress()
		{
			if (G.Driver == null || G.Driver == IntPtr.Zero)
			{
				MessageBox.Show("Bad driver handle, exiting..");
				Environment.Exit(0);
			}
			KModRequest ReadRequest = new KModRequest();
			ReadRequest.BaseAddress = 0x100000;
			ReadRequest.ModuleSize = 0;
			uint sizeRequest = (uint)Marshal.SizeOf(typeof(KModRequest));

			byte[] reqbytes = new byte[sizeRequest];

			IntPtr ptr = Marshal.AllocHGlobal((int)sizeRequest);

			Marshal.StructureToPtr(ReadRequest, ptr, true);

			uint NULL;
			if (Win32.DeviceIoControl(G.Driver, IO_MOD, ptr, sizeRequest, ptr, sizeRequest, out NULL, IntPtr.Zero))
			{
				var readReq = (KModRequest)Marshal.PtrToStructure(ptr, typeof(KModRequest));
				Marshal.FreeHGlobal(ptr);
				G.clientSize = readReq.ModuleSize;
				return readReq.BaseAddress;
			}
			else
			{
				MessageBox.Show("IoCtl failed: 1");
				return 0;
			}

		}

		public byte[] Read(uint Address, int size_t)
		{
			if (G.Driver == null || G.Driver == IntPtr.Zero)
			{
				MessageBox.Show("Bad driver handle, exiting..");
				Environment.Exit(0);
			}
			byte[] buffer = new byte[size_t];
			var p_buffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			KReadRequest ReadRequest = new KReadRequest();
			ReadRequest.address = Address;
			ReadRequest.size = (uint)size_t;
			ReadRequest.bytes = p_buffer.AddrOfPinnedObject();

			uint sizeRequest = (uint)Marshal.SizeOf(typeof(KReadRequest));
			IntPtr ptr = Marshal.AllocHGlobal((int)sizeRequest);
			Marshal.StructureToPtr(ReadRequest, ptr, true);
			lock (lockThis)
			{
				if (Win32.DeviceIoControl(G.Driver, IO_READ, ptr, sizeRequest, ptr, sizeRequest, out uint NULL, IntPtr.Zero))
				{
					Marshal.FreeHGlobal(ptr);
					p_buffer.Free();
					return buffer;
				}
				else
				{
					MessageBox.Show("IoCtl failed: 2");
					return new byte[0];
				}
					
			}
		}

		private static T GetStructure<T>(byte[] bytes)
		{
			var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			var structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
			handle.Free();
			return structure;
		}
		
		public T Read<T>(uint Address)
		{
			var size = Marshal.SizeOf(typeof(T));
			var data = Read(Address, size);
			return GetStructure<T>(data);
		}
	}
}
