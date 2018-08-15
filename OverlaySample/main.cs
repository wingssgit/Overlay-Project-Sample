using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using Factory = SharpDX.Direct2D1.Factory;
using Format = SharpDX.DXGI.Format;
using System.Threading;
using System.Runtime.InteropServices;

namespace OverlaySample
{
	public partial class main : Form
	{

		private Thread updateStream = null;
		private Thread windowStream = null;
		private Thread aimStream = null;
		private WindowRenderTarget device;
		private HwndRenderTargetProperties renderProperties;
		private Factory factory;
		private float[] vmatrix = new float[16];
		private static Vector2 RecoilCross = new Vector2(0, 0);
		private static Vector3 AimPunch = new Vector3(0, 0, 0);
		private static Vector3 VecPunch = new Vector3(0, 0, 0);
		private static Vector3 LastPunch = new Vector3(0, 0, 0);
		public static IntPtr nwHWND = IntPtr.Zero;
		public static float windowWidth;
		public static float windowHeight;
		private System.Drawing.Font Font = new System.Drawing.Font("Tahoma", 9, System.Drawing.FontStyle.Regular);
		public main()
		{
			InitializeComponent();
			this.Text = "";
			this.BackColor = System.Drawing.Color.Black;
			this.FormBorderStyle = FormBorderStyle.None;
			this.WindowState = FormWindowState.Minimized;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			 System.Drawing.Rectangle resolution = Screen.PrimaryScreen.Bounds;
			windowWidth = resolution.Width;
			windowHeight = resolution.Height;

			//Load external module to 'borrow' the window belonging to the process ID passed in the call to GetNewWindow
			IntPtr pDll = Win32.LoadLibrary(@"wextdll.dll");
			IntPtr pFunction = Win32.GetProcAddress(pDll, "GetNewWindow");
			Win32.GetNewWindow getWindow = (Win32.GetNewWindow)Marshal.GetDelegateForFunctionPointer(pFunction, typeof(Win32.GetNewWindow));

			nwHWND = getWindow(G.PID);

			Win32.FreeLibrary(pDll);
			Win32.SetLayeredWindowAttributes(nwHWND, 0, 255, Win32.LWA_ALPHA);

			windowStream = new Thread(new ParameterizedThreadStart(WindowToTop));
			windowStream.IsBackground = true;
			windowStream.Start();
			Initialize();
			updateStream = new Thread(new ParameterizedThreadStart(Update));
			updateStream.IsBackground = true;
			updateStream.Start();
			aimStream = new Thread(AimLoop);
			aimStream.IsBackground = true;
			aimStream.Start();

			timer1.Interval = 20;
			timer1.Start();
		}

		private void Initialize()
		{
			G.Memory = new Mx();

			factory = new Factory();
			renderProperties = new HwndRenderTargetProperties()
			{
				Hwnd = nwHWND,
				PixelSize = new Size2((int)windowWidth, (int)windowHeight),
				PresentOptions = PresentOptions.Immediately
			};
			device = new WindowRenderTarget(factory, new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied)), renderProperties)
			{
				TextAntialiasMode = TextAntialiasMode.Grayscale,
				AntialiasMode = AntialiasMode.Aliased
			};

			var margins = new Win32.Margins();
			margins.Left = -1;

			margins.Top = 0;
			margins.Right = 0;
			margins.Bottom = 0;
			Win32.DwmExtendFrameIntoClientArea(nwHWND, ref margins);
		}

		private void Update(object sender)
		{
			var brushWhite = new SolidColorBrush(device, new RawColor4(1, 1, 1, 1));
			var brushWhiteF = new SolidColorBrush(device, new RawColor4(1, 1, 1, 0.6f));
			var brushDarkRed = new SolidColorBrush(device, new RawColor4(0.62f, 0, 0, 0.6f));
			var brushBlack = new SolidColorBrush(device, new RawColor4(0, 0, 0, 1));
			var brushBlackF = new SolidColorBrush(device, new RawColor4(0, 0, 0, 0.6f));
			var brushBlue = new SolidColorBrush(device, new RawColor4(0.14f, 0.58f, 1, 1));
			var brushOrange = new SolidColorBrush(device, new RawColor4(1, 0.49f, 0, 1));
			var brushGray = new SolidColorBrush(device, new RawColor4(0.5f, 0.5f, 0.5f, 1));
			var brushGrayF = new SolidColorBrush(device, new RawColor4(0.3f, 0.3f, 0.3f, 1));
			var brushLtGray = new SolidColorBrush(device, new RawColor4(0.65f, 0.65f, 0.65f, 1));
			RawColor4 black = new RawColor4(0, 0, 0, 1);
			RawColor4 white = new RawColor4(1, 1, 1, 1);
			RawColor4 gray = new RawColor4(0.3f, 0.3f, 0.3f, 1);
			var brushGreen = new SolidColorBrush(device, RawColorFromColor(Color.Green));
			var brushRed = new SolidColorBrush(device, RawColorFromColor(Color.Red));
			var brushYellow = new SolidColorBrush(device, RawColorFromColor(Color.Yellow));
			var brushPurple = new SolidColorBrush(device, RawColorFromColor(Color.Purple));

			var fontFactory = new SharpDX.DirectWrite.Factory();
			var font = new SharpDX.DirectWrite.TextFormat(fontFactory, "Tahoma", 12);

			while (true)
			{
				
					
				device.BeginDraw();
				device.Clear(null);

				//Read local player info
				GetLocalInfo(G.PlayerList);
				GetWeaponInfo();

				//Get ViewMatrix
				for (int j = 0; j < 16; j++)
					vmatrix[j] = M.Read<float>(G.clientDLL + Offsets.dwViewMatrix + ((uint)j * 0x4));

				//Get Punch Angle
				VecPunch = M.Read<Vector3>(G.pLocalPlayer + Offsets.VecPunchAngle);

				//Generate screen coordinates for Recoil Crosshair
				GetRecoilCoords(out Vector2 RecoilCross);
				if (VecPunch.X != 0 || VecPunch.Y != 0)
				{
					if (Config.visuals)
						DrawMarker(RecoilCross.X, RecoilCross.Y, 10, brushRed);
				}

				//Reset Target list
				List<Player> Players = new List<Player>();

				for (int i = 0; i < 32; i++)
				{
					Player player = new Player();

					//Read player info
					uint pPlayer = M.Read<uint>(G.PlayerList + (uint)i * 0x10);
					
					int team = M.Read<int>(pPlayer + Offsets.TeamNum);
					if (team == G.MyTeam) continue;

					int health = M.Read<int>(pPlayer + Offsets.Health);
					if (health < 1) continue;

					int dormant = M.Read<int>(pPlayer + Offsets.Dormant);
					if (dormant == 1) continue;

					int spotted_by = M.Read<int>(pPlayer + Offsets.SpottedByMask);
					int bSpotted = spotted_by & (1 << G.MyIndex - 1);

					Vector3 playerLocation = M.Read<Vector3>(pPlayer + Offsets.VecOrigin);

					

					if (WorldToScreen(playerLocation, out Vector2 screenpos2))
					{
						uint headBone = 8;
						Vector3 plr_bone = GetBonePos(headBone, pPlayer);

						Vector3 vDelta = playerLocation - G.MyLocation;
						int distance = (int)(vDelta.Length / 10);
						if (Config.visuals)
							DrawText(distance.ToString() + 'm', (int)screenpos2.X, (int)screenpos2.Y - 5, brushYellow, brushBlack, fontFactory, font);
						//Set Player info & add to player list
						player.BaseAddress = pPlayer;
						player.index = i;
						player.health = health;
						player.LocationHead = plr_bone;
						player.distance = distance;
						player.bSpotted = bSpotted;
						player.bDormant = dormant;
						Players.Add(player);
						
						if (WorldToScreen(plr_bone, out Vector2 screenpos))
						{
							//Center of head screen position
							//DrawMarker((int)screenpos.X, (int)screenpos.Y, 14, brushBlue, brushBlack);
						}

						Vector3 bottomHead = new Vector3(plr_bone.X, plr_bone.Y, plr_bone.Z - 4f);
						Vector3 topHead = new Vector3(plr_bone.X, plr_bone.Y, plr_bone.Z + 3f);
						if (WorldToScreen(bottomHead, out Vector2 screenpos3))
						{
							if (WorldToScreen(topHead, out Vector2 screenpos4))
							{
								float height = Math.Abs(screenpos2.Y - screenpos4.Y);
								float width = height / 2f;
								if (Config.visuals)
									DrawVertHBar(screenpos4.X - ((width / 2) - 2), screenpos4.Y - 1, height, health, brushGreen);
								float headHeight = Math.Abs(screenpos3.Y - screenpos4.Y);
								float headWidth = headHeight / 2f;
								if (Config.visuals)
									DrawRect(screenpos.X - headWidth, screenpos.Y - headWidth, headHeight, headHeight, brushBlue);
							}
						}
					}
					
				}

				if (Players.Count > 0)
					G.Players = Players;
				else
					G.Players = new List<Player>();
				
				//Draw Crosshair
				if (Config.visuals)
					DrawCross((int)(windowWidth / 2) + 1, (int)(windowHeight / 2) + 1, 20, brushGreen);

				//Draw Menu
				if (Config.Menu)
					DrawMenu(400, 400, brushWhite, brushLtGray, brushDarkRed, fontFactory, white, font);

				//Recoil Control
				if (IsKeyDown(Config.ControlRecoilKey) && G.ShotsFired > 1) //0x4C = L key
				{
					ControlRecoil();
				}
				LastPunch = VecPunch;

				device.EndDraw();
				Thread.Sleep(10);
			}
		}

		private Vector3 PredictCoords(Vector3 PlayerLocation, Vector3 PlayerVelocity, float ping)
		{
			float impactTime = ping / 1000;
			Vector3 impactTimeVec = new Vector3(impactTime, impactTime, impactTime);

			return PlayerLocation + (PlayerVelocity * impactTimeVec);
		}


		private void AimLoop(object sender)
		{
			uint ticks = 0;
			uint LastSwitchTick = 0;
			bool bLastTargetValid = false;
			bool bWasAimKeyUp = true;

			Player LastTarget = new Player();
			LastTarget.BaseAddress = 0x1000;
			LastTarget.index = -1;
			LastTarget.health = 100;
			LastTarget.LocationHead = new Vector3(0, 0, 0);
			LastTarget.distance = 1000;
			LastTarget.bSpotted = 0;
			LastTarget.bDormant = 0;

			
			while (true)
			{
				
				
				if (IsKeyDown(Config.AimKey) && G.Players.Count > 0 && G.MyWeaponType != WeaponType.Knife && G.MyWeaponType != WeaponType.Grenade && G.MyWeaponType != WeaponType.Unknown)//Ctrl = 0x11 Shift = 0x10
				{

					List<Player> AimTarget = new List<Player>();
					float bestDist = Config.fov;



					foreach (var player in G.Players.ToList())
					{
						
						if (player.health > 0 && player.BaseAddress != 0 && player.bDormant == 0)
						{
							float flDist = DistanceFromCrosshair(player.LocationHead);

							if (G.MyWeaponType == WeaponType.Rifle || G.MyWeaponType == WeaponType.Smg)
								if (G.ShotsFired > 1)
									flDist = DistanceFromRecoilCrosshair(player.LocationHead);
							
							if (flDist < 0) continue; //target not on screen

							if (flDist < bestDist)
							{
								AimTarget = new List<Player>();
								bestDist = flDist;
								AimTarget.Add(player);
							}
						}
					}
					
					if (AimTarget.Count > 0)
					{
						//Check if target is different, if so wait plausible human reaction time
						if (AimTarget[0].BaseAddress != LastTarget.BaseAddress && bLastTargetValid && !bWasAimKeyUp)
						{
							//Delay target switch
							uint tickoffset = (uint)Config.SwitchTargetDelayMS / 5;
							if (ticks < (LastSwitchTick + tickoffset))
							{
								LastSwitchTick = ticks;
								ticks += tickoffset;

								Thread.Sleep(Config.SwitchTargetDelayMS);
								bLastTargetValid = false;
							}
							else
								LastSwitchTick = ticks;
						}
						else
						{
							Vector3 velocity = M.Read<Vector3>(AimTarget[0].BaseAddress + Offsets.VecVelocity);
							velocity.Z = 0; //Disregard Z velocity due to death animation causing wonky Z velocity readings resulting in crazy predictions
							

							//Predicting for ~13ms to prevent crosshair from lagging behind moving targets
							float delay = Config.smooth * 13; 
							Vector3 predictedLocation = PredictCoords(AimTarget[0].LocationHead, velocity, delay);

							if (WorldToScreen(predictedLocation, out Vector2 screenPos))
							{
								
								float smoothing = Config.smooth;
								Vector2 ScreenCenter = new Vector2((windowWidth / 2) + 1, (windowHeight / 2) + 1);
								Vector2 RecoilCross = ScreenCenter;

								 
								if (G.MyWeaponType == WeaponType.Rifle || G.MyWeaponType == WeaponType.Smg)
								{
									if (G.ShotsFired >= 2)
									{
										AimPunch = M.Read<Vector3>(G.pLocalPlayer + Offsets.VecPunchAngle);
										GetAimRecoil(out RecoilCross);
									}
									else if (Config.trigger)
									{
										AimPunch = M.Read<Vector3>(G.pLocalPlayer + Offsets.VecPunchAngle);
										GetAimRecoil(out RecoilCross);
									}
								}
								
								
								if (Math.Abs(RecoilCross.X - ScreenCenter.X) >= 1f || Math.Abs(RecoilCross.Y - ScreenCenter.Y) >= 1f)
								{
									ScreenCenter = RecoilCross;
								}

								float absdistX = Math.Abs(screenPos.X - ScreenCenter.X);
								float absdistY = Math.Abs(screenPos.Y - ScreenCenter.Y);
								float smFactorX = 1;//extra smoothing factor for testing purposes
								float smFactorY = 1;//extra smoothing factor for testing purposes

								int moveX = 0;
								int moveY = 0;
								//Determine if distance to move is less than smoothing factor, if so we move by 1 pixel otherwise aim will be clunky/inaccurate
								if (absdistX < Config.smooth)
								{
									moveX = (int)(Math.Round(screenPos.X - ScreenCenter.X)) < 0 ? -1 : 1;
								}
								else
								{
									moveX = (int)(Math.Round(screenPos.X - ScreenCenter.X) / (smoothing * smFactorX));
								}

								if (absdistY < Config.smooth)
								{
									moveY = (int)(Math.Round(screenPos.Y - ScreenCenter.Y)) < 0 ? -1 : 1;
								}
								else
								{
									moveY = (int)(Math.Round(screenPos.Y - ScreenCenter.Y) / (smoothing * smFactorY));
								}
								
								//Aim at target via mouse movement
								if (Math.Abs(moveX) >= 1 || Math.Abs(moveY) >= 1)//Only move mouse if distance is 1 pixel or more
									Win32.mouse_event(0x0001, moveX, moveY, 0, 0);
								else if(Config.trigger)//Fire only if no further movement required & trigger bot enabled
								{
									Win32.mouse_event(0x0002, 0, 0, 0, 0);//Send Left Mouse Down input
									Thread.Sleep(10);//Wait like a normal person
									Win32.mouse_event(0x0004, 0, 0, 0, 0);//Send Left Mouse Up input
								}

								bLastTargetValid = true;
								LastTarget = AimTarget[0];


							}
						}

					}
					bWasAimKeyUp = false;
				}
				else
					bWasAimKeyUp = true;
				ticks++;
				Thread.Sleep(5);
			}
		}

		public WeaponType GetWeaponType(int WeaponId)
		{
			switch (WeaponId)
			{
				case 1:
					//Deagle
					return WeaponType.Pistol;
				case 2:
					//Duals
					return WeaponType.Pistol;
				case 3:
					//Five Seven
					return WeaponType.Pistol;
				case 4:
					//Glock
					return WeaponType.Pistol;
				case 7:
					//AK47
					return WeaponType.Rifle;
				case 8:
					//AUG
					return WeaponType.Rifle;
				case 9:
					//AWP
					return WeaponType.Sniper;
				case 10:
					//Famas
					return WeaponType.Rifle;
				case 11:
					//G3 SG1
					return WeaponType.Sniper;
				case 13:
					//Galil
					return WeaponType.Rifle;
				case 14:
					//M249
					return WeaponType.Heavy;
				case 16:
					//M4A1
					return WeaponType.Rifle;
				case 17:
					//Mac 10
					return WeaponType.Smg;
				case 19:
					//P90
					return WeaponType.Smg;
				case 24:
					//UMP45
					return WeaponType.Smg;
				case 25:
					//XM1014
					return WeaponType.Shotgun;
				case 26:
					//Bizon
					return WeaponType.Smg;
				case 27:
					//Mag-7
					return WeaponType.Shotgun;
				case 28:
					//Negev
					return WeaponType.Heavy;
				case 29:
					//Sawed Off
					return WeaponType.Shotgun;
				case 30:
					//Tech 9
					return WeaponType.Pistol;
				case 31:
					//Taser
					return WeaponType.Tazer;
				case 32:
					//P2000
					return WeaponType.Pistol;
				case 33:
					//MP7
					return WeaponType.Smg;
				case 34:
					//MP9
					return WeaponType.Smg;
				case 35:
					//Nova
					return WeaponType.Shotgun;
				case 36:
					//P250
					return WeaponType.Pistol;
				case 38:
					//SCAR 20
					return WeaponType.Sniper;
				case 39:
					//SG556
					return WeaponType.Rifle;
				case 40:
					//SSG08
					return WeaponType.Sniper;
				case 42:
					//Knife
					return WeaponType.Knife;
				case 43:
					//Flashbang
					return WeaponType.Grenade;
				case 44:
					//Grenade
					return WeaponType.Grenade;
				case 45:
					//Smoke
					return WeaponType.Grenade;
				case 46:
					//Molotov
					return WeaponType.Grenade;
				case 47:
					//Decoy
					return WeaponType.Grenade;
				case 48:
					//Molotov
					return WeaponType.Grenade;
				case 49:
					//C4
					return WeaponType.C4;
				case 59:
					//Knife
					return WeaponType.Knife;
				case 60:
					//M4A1
					return WeaponType.Rifle;
				case 61:
					//USP
					return WeaponType.Pistol;
				case 63:
					//CZ75
					return WeaponType.Pistol;
				case 64:
					//Revolver
					return WeaponType.Pistol;
				case 500:
					//Bayonet
					return WeaponType.Knife;
				case 505:
					//Switchblade
					return WeaponType.Knife;
				case 506:
					//Gutknife
					return WeaponType.Knife;
				case 507:
					//Karambit
					return WeaponType.Knife;
				case 508:
					//M9 Bayonet
					return WeaponType.Knife;
				case 509:
					//Huntsman
					return WeaponType.Knife;
				case 512:
					//Falchion
					return WeaponType.Knife;
				case 515:
					//Butterfly
					return WeaponType.Knife;
				case 516:
					//Another Knife
					return WeaponType.Knife;
				default:
					//Unknown Weapon
					return WeaponType.Unknown;
			}
		}

		public float DistanceFromRecoilCrosshair(Vector3 TargetLocation)
		{
			Vector2 Crosshair = new Vector2((windowWidth / 2) + 1, (windowHeight / 2) + 1);
			float absdistX = Math.Abs(Crosshair.X - RecoilCross.X);
			float absdistY = Math.Abs(Crosshair.Y - RecoilCross.Y);

			if (absdistX > 1 || absdistY > 1)
				if (RecoilCross.X != 0 && RecoilCross.Y != 0)
					Crosshair = RecoilCross;

			Vector2 ScreenLocation = new Vector2(0f, 0f);
			if (WorldToScreen(TargetLocation, out ScreenLocation))
			{
				float X = ScreenLocation.X > Crosshair.X ? ScreenLocation.X - Crosshair.X : Crosshair.X - ScreenLocation.X;
				float Y = ScreenLocation.Y > Crosshair.Y ? ScreenLocation.Y - Crosshair.Y : Crosshair.Y - ScreenLocation.Y;
				return (float)Math.Sqrt((X * X) + (Y * Y));
			}
			else
			{
				return -1;
			}

		}

		public float DistanceFromCrosshair(Vector3 TargetLocation)
		{
			Vector2 Crosshair = new Vector2((windowWidth / 2) + 1, (windowHeight / 2) + 1);
			Vector2 ScreenLocation = new Vector2(0f, 0f);
			if (WorldToScreen(TargetLocation, out ScreenLocation))
			{
				float X = ScreenLocation.X > Crosshair.X ? ScreenLocation.X - Crosshair.X : Crosshair.X - ScreenLocation.X;
				float Y = ScreenLocation.Y > Crosshair.Y ? ScreenLocation.Y - Crosshair.Y : Crosshair.Y - ScreenLocation.Y;
				return (float)Math.Sqrt((X * X) + (Y * Y));
			}
			else
			{
				return -1;
			}
		}

		private void GetLocalInfo(uint plistAddress)
		{
			uint player = M.Read<uint>(plistAddress);
			if (player == 0) return;

			G.MyLocation = M.Read<Vector3>(player + Offsets.VecOrigin);

			uint pLocalPlayer = G.clientDLL + Offsets.dwLocalPlayer;
			uint localplayer = M.Read<uint>(pLocalPlayer);
			G.pLocalPlayer = localplayer;
			G.MyTeam = M.Read<int>(localplayer + Offsets.TeamNum);
			G.MyIndex = M.Read<int>(localplayer + Offsets.IndexOffset);
		}

		private void GetWeaponInfo()
		{
			uint wpnptr = M.Read<uint>(G.pLocalPlayer + Offsets.ActiveWeapon);
			uint wpnptr1 = wpnptr & 0xfff;
			G.pWeapon = M.Read<uint>(G.clientDLL + Offsets.dwEntityList + (wpnptr1 - 1) * 0x10);
			G.WeaponID = M.Read<int>(G.pWeapon + Offsets.ItemDefinitionIndex);
			G.MyWeaponType = GetWeaponType(G.WeaponID);
			G.Clip1 = M.Read<int>(G.pWeapon + Offsets.Clip1);
			G.ShotsFired = M.Read<int>(G.pLocalPlayer + Offsets.ShotsFired);

		}

		private Vector3 GetBonePos(uint bone, uint playerAddress)
		{
			uint plr_boneMatrix = M.Read<uint>(playerAddress + Offsets.BoneMatrix);
			float[] bonepos = new float[3];
			bonepos[0] = M.Read<float>(plr_boneMatrix + 0x30 * bone + 0x0C);
			bonepos[1] = M.Read<float>(plr_boneMatrix + 0x30 * bone + 0x1C);
			bonepos[2] = M.Read<float>(plr_boneMatrix + 0x30 * bone + 0x2C);

			return new Vector3(bonepos[0], bonepos[1], bonepos[2]);
		}
		bool WorldToScreen(Vector3 pos, out Vector2 screenpos)
		{
			float w = 0.0f;

			screenpos.X = vmatrix[0] * pos.X + vmatrix[1] * pos.Y + vmatrix[2] * pos.Z + vmatrix[3];
			screenpos.Y = vmatrix[4] * pos.X + vmatrix[5] * pos.Y + vmatrix[6] * pos.Z + vmatrix[7];

			w = vmatrix[12] * pos.X + vmatrix[13] * pos.Y + vmatrix[14] * pos.Z + vmatrix[15];

			if (w < 0.01f)
				return false;

			screenpos.X *= (1.0f / w);
			screenpos.Y *= (1.0f / w);

			int width = (int)windowWidth;
			int height = (int)windowHeight;

			float x = width / 2;
			float y = height / 2;

			x += 0.5f * screenpos.X * width + 0.5f;
			y -= 0.5f * screenpos.Y * height + 0.5f;

			screenpos.X = x;
			screenpos.Y = y;

			if (screenpos.X > width || screenpos.X < 0 || screenpos.Y > height || screenpos.Y < 0)
				return false;

			return true;
		}



		//Recoil
		public void GetRecoilCoords(out Vector2 recoil)
		{
			

			float pX = VecPunch.X / Config.RecoilOffsetXhair; 
			float pY = -(VecPunch.Y / Config.RecoilOffsetXhair);

			recoil.X = pY + (windowWidth / 2) + 1;
			recoil.Y = pX + (windowHeight / 2) + 1;
		}

		public void ControlRecoil()
		{

			Vector3 punch = VecPunch - LastPunch;

			float pX = punch.X / Config.RecoilOffset;
			float pY = punch.Y / Config.RecoilOffset;
			
			Win32.mouse_event(0x0001, (int)pY, (int)-pX, 0, 0);
			
		}

		public void GetAimRecoil(out Vector2 recoil)
		{
			
			float pX = AimPunch.X / Config.RecoilOffsetXhair;
			float pY = -(AimPunch.Y / Config.RecoilOffsetXhair);

			recoil.X = pY + (windowWidth / 2) + 1;
			recoil.Y = pX + (windowHeight / 2) + 1;
		}

		public bool IsKeyDown(int key)
		{
			return Convert.ToBoolean(Win32.GetKeyState(key) & Win32.KEY_PRESSED);
		}

		private int waitInputTime = 5;
		private bool isKeyDown = false;
		private int waitTime = 0;
		private int count = 0;
		private void UpdateLongKeys()
		{
			if (isKeyDown == true)
			{
				waitTime++;
				if (waitTime == waitInputTime)
				{
					waitTime = 0;
					isKeyDown = false;
				}
			}
			else
			{
				if (IsKeyDown(0x71)) //F2 0x71
				{
					Win32.CloseHandle(G.Driver);
					Application.Exit();
					isKeyDown = true;
				}
				else if (IsKeyDown(0x70)) //F1
				{
					Config.visuals = false;
					Config.Menu = false;
					isKeyDown = true;
				}
				else if (IsKeyDown(0x74)) //F5
				{
					Config.Menu = !Config.Menu;
					isKeyDown = true;
				}
				else if (IsKeyDown(0x26)) // UP
				{
					currentMenuIndex = currentMenuIndex == 0 ? 5 : currentMenuIndex - 1;
					isKeyDown = true;
				}
				else if (IsKeyDown(0x27)) // RIGHT
				{
					switch (currentMenuIndex)
					{
						case 0:
							Config.visuals = !Config.visuals;
							break;
						case 1:
							Config.trigger = !Config.trigger;
							break;
						case 2:
							Config.RecoilOffsetXhair = Config.RecoilOffsetXhair  + 0.001f;
							break;
						case 3:
							Config.RecoilOffset = Config.RecoilOffset + 0.0005f;
							break;
						case 4:
							Config.smooth = Config.smooth + 0.5f;
							break;
						case 5:
							Config.fov = Config.fov < 1000 ? Config.fov + 10 : 1000;
							break;
						

					}

					isKeyDown = true;
				}
				else if (IsKeyDown(0x28)) // DOWN
				{
					currentMenuIndex = currentMenuIndex == 5 ? 0 : currentMenuIndex + 1;
					isKeyDown = true;
				}
				else if (IsKeyDown(0x25)) // Left
				{

					switch (currentMenuIndex)
					{
						case 0:
							Config.visuals = !Config.visuals;
							break;
						case 1:
							Config.trigger = !Config.trigger;
							break;
						case 2:
							Config.RecoilOffsetXhair = Config.RecoilOffsetXhair > 0.001f ? Config.RecoilOffsetXhair - 0.001f : 0.001f;
							break;
						case 3:
							Config.RecoilOffset = Config.RecoilOffset > 0.001f ? Config.RecoilOffset - 0.0005f : 0.001f;
							break;
						case 4:
							Config.smooth = Config.smooth > 1 ? Config.smooth - 0.5f : 1;
							break;
						case 5:
							Config.fov = Config.fov > 10 ? Config.fov - 10 : 10;
							break;

					}

					isKeyDown = true;
				}

			}
		}




		delegate IntPtr GetWindowHandleDelegate();

		private IntPtr GetWindowHandle()
		{

			if (this.InvokeRequired == true)
			{

				return (IntPtr)this.Invoke((GetWindowHandleDelegate)delegate () {

					return GetWindowHandle();

				});
			}

			return this.Handle;
		}

		//Bring Window to Foreground without using TopMost flag
		private void WindowToTop(object sender)
		{
			IntPtr overhwnd = nwHWND; 

			while (true)
			{
				IntPtr hwnd2 = Win32.GetForegroundWindow();
				if (hwnd2 != overhwnd)
				{
					IntPtr hwnd3 = Win32.GetWindow(hwnd2, Win32.GW_HWNDPREV);
					Win32.SetWindowPos(overhwnd, hwnd3, 0, 0, 0, 0, Win32.SWP_SHOWWINDOW | Win32.SWP_NOSIZE | Win32.SWP_NOMOVE | 0x4000);
				}

				Thread.Sleep(1000);
			}
		}
		

		public SharpDX.DirectWrite.TextLayout TextLayout(string szText, SharpDX.DirectWrite.Factory factory, SharpDX.DirectWrite.TextFormat font) => new SharpDX.DirectWrite.TextLayout(factory, szText, font, float.MaxValue, float.MaxValue);
		
		private void DrawVertHBar(float X, float Y, float Height, float health, SharpDX.Direct2D1.Brush fillBrush)
		{

			float X2, Y2;
			float W = 2;
			float hbar = health / 100;
			hbar = hbar * Height;
			X2 = X + W;
			Y2 = Y + Height;
			Y = Y2 - hbar;

			device.FillRectangle(new RawRectangleF(X, Y, X2, Y2), fillBrush);

		}
		

		public void DrawText(string szText, int x, int y, SharpDX.Direct2D1.Brush textColor, SharpDX.Direct2D1.Brush shadowColor, SharpDX.DirectWrite.Factory fontFactory, SharpDX.DirectWrite.TextFormat font)
		{
			var tempTextLayout = TextLayout(szText, fontFactory, font);

			device.DrawTextLayout(new RawVector2(x + 1, y), tempTextLayout, shadowColor, DrawTextOptions.NoSnap);
			device.DrawTextLayout(new RawVector2(x - 1, y), tempTextLayout, shadowColor, DrawTextOptions.NoSnap);
			device.DrawTextLayout(new RawVector2(x, y + 1), tempTextLayout, shadowColor, DrawTextOptions.NoSnap);
			device.DrawTextLayout(new RawVector2(x, y - 1), tempTextLayout, shadowColor, DrawTextOptions.NoSnap);
			device.DrawTextLayout(new RawVector2(x, y), tempTextLayout, textColor, DrawTextOptions.NoSnap);

			tempTextLayout.Dispose();
		}


		private int currentMenuIndex = 3;
		public void DrawMenu(int X, int Y, Brush SelectedColor, Brush UnselectedColor, Brush titleColor, SharpDX.DirectWrite.Factory fontFactory, RawColor4 background, SharpDX.DirectWrite.TextFormat font)
		{



			string[] menuArray = new string[]
			{
				String.Format("Visuals: {0}", Config.visuals ? "(1)" : "(0)"),
				String.Format("Trigger Bot: {0}", Config.trigger ? "(1)" : "(0)"),
				String.Format("Recoil Xhair: ({0})", Config.RecoilOffsetXhair),
				String.Format("Recoil Ctrl: ({0})", Config.RecoilOffset),
				String.Format("Smoothing: ({0})", Config.smooth),
				String.Format("Aim FOV: ({0})", Config.fov / 10)
			};

			string title = "Cole's Sample Project Menu";
			System.Drawing.Size nameSize = TextRenderer.MeasureText(title, Font);
			DrawFillRect(X - 2, Y - 2, nameSize.Width, 4 + (nameSize.Height * (menuArray.Count() + 1)), background);
			DrawFillRect(X - 2, Y + (nameSize.Height * (currentMenuIndex + 1)) + 1, nameSize.Width, nameSize.Height, background);
			DrawText(title, X, Y, titleColor, fontFactory, font);
			DrawText(menuArray[0], X, Y + (nameSize.Height * 1), currentMenuIndex == 0 ? SelectedColor : UnselectedColor, fontFactory, font);
			DrawText(menuArray[1], X, Y + (nameSize.Height * 2), currentMenuIndex == 1 ? SelectedColor : UnselectedColor, fontFactory, font);
			DrawText(menuArray[2], X, Y + (nameSize.Height * 3), currentMenuIndex == 2 ? SelectedColor : UnselectedColor, fontFactory, font);
			DrawText(menuArray[3], X, Y + (nameSize.Height * 4), currentMenuIndex == 3 ? SelectedColor : UnselectedColor, fontFactory, font);
			DrawText(menuArray[4], X, Y + (nameSize.Height * 5), currentMenuIndex == 4 ? SelectedColor : UnselectedColor, fontFactory, font);
			DrawText(menuArray[5], X, Y + (nameSize.Height * 6), currentMenuIndex == 5 ? SelectedColor : UnselectedColor, fontFactory, font);


		}

		public void DrawText(string szText, int x, int y, SharpDX.Direct2D1.Brush textColor, SharpDX.DirectWrite.Factory fontFactory, SharpDX.DirectWrite.TextFormat font)
		{
			var tempTextLayout = TextLayout(szText, fontFactory, font);

			device.DrawTextLayout(new RawVector2(x + 1, y), tempTextLayout, new SolidColorBrush(device, RGB(0, 0, 0)), DrawTextOptions.NoSnap);
			device.DrawTextLayout(new RawVector2(x - 1, y), tempTextLayout, new SolidColorBrush(device, RGB(0, 0, 0)), DrawTextOptions.NoSnap);
			device.DrawTextLayout(new RawVector2(x, y + 1), tempTextLayout, new SolidColorBrush(device, RGB(0, 0, 0)), DrawTextOptions.NoSnap);
			device.DrawTextLayout(new RawVector2(x, y - 1), tempTextLayout, new SolidColorBrush(device, RGB(0, 0, 0)), DrawTextOptions.NoSnap);
			device.DrawTextLayout(new RawVector2(x, y), tempTextLayout, textColor, DrawTextOptions.NoSnap);

			tempTextLayout.Dispose();
		}


		private void DrawFillRect(float X, float Y, float W, float H, RawColor4 color)
		{
			float X2, Y2;
			X2 = X + W;
			Y2 = Y + H;

			SolidColorBrush background = brush(color.R, color.G, color.B, 50f);
			SolidColorBrush outline = brush(color.R, color.G, color.B, color.A);

			device.FillRectangle(new RawRectangleF(X, Y, X2, Y2), background);
			device.DrawRectangle(new RawRectangleF(X, Y, X2, Y2), outline);
		}

		private SolidColorBrush brush(float R, float G, float B, float A)
		{
			float r, g, b, a;
			r = R / 255.0f;
			g = G / 255.0f;
			b = B / 255.0f;
			a = A / 255.0f;
			RawColor4 color = new RawColor4(r, g, b, a);
			return new SolidColorBrush(device, color);
		}
		private RawColor4 RGB(float R, float G, float B)
		{
			float r, g, b;
			r = R / 255.0f;
			g = G / 255.0f;
			b = B / 255.0f;
			return new RawColor4(r, g, b, 1);
		}

		private void DrawCornerBox(float X, float Y, float W, float H, int size, Brush color)
		{
			float BRx, BRy, TLx, TLy, TRx, TRy, BLx, BLy;
			TLx = X;
			TLy = Y;
			BRx = X + W;
			BRy = Y + H;
			TRx = BRx;
			TRy = Y;
			BLx = X;
			BLy = BRy;

			device.DrawLine(new RawVector2(TLx, TLy), new RawVector2(TLx + size, TLy), color); //Top Left Horizontal
			device.DrawLine(new RawVector2(TLx, TLy), new RawVector2(TLx, TLy + size), color); //Top Left Vertical
			device.DrawLine(new RawVector2(TRx, TRy), new RawVector2(TRx - size, TRy), color); //Top Right Horizontal
			device.DrawLine(new RawVector2(TRx, TRy), new RawVector2(TRx, TRy + size), color); //Top Right Vertical
			device.DrawLine(new RawVector2(BLx, BLy), new RawVector2(BLx + size, BLy), color); //Bottom Left Horizontal
			device.DrawLine(new RawVector2(BLx, BLy), new RawVector2(BLx, BLy - size), color); //Bottom Left Vertical
			device.DrawLine(new RawVector2(BRx, BRy), new RawVector2(BRx - size, BRy), color); //Bottom Right Horizontal
			device.DrawLine(new RawVector2(BRx, BRy), new RawVector2(BRx, BRy - size), color); //Bottom Right Vertical
		}

		private void DrawCircle(float X, float Y, float R, Brush color)
		{

			device.DrawEllipse(new Ellipse(new RawVector2(X, Y), R, R), color, 1.0f);
		}
		private void DrawCircle(float X, float Y, float R, Brush color, Brush shadowColor)
		{
			device.DrawEllipse(new Ellipse(new RawVector2(X, Y), R, R), shadowColor, 3.0f);
			device.DrawEllipse(new Ellipse(new RawVector2(X, Y), R, R), color, 1.0f);
		}
		private void DrawFillRect(float X, float Y, float W, float H, Brush color, Brush shadowColor)
		{
			float X1 = X - (W / 2);
			float X2 = X + (W / 2);
			float Y1 = Y - (H / 2);
			float Y2 = Y + (H / 2);
			
			device.FillRoundedRectangle(new RoundedRectangle() { RadiusX = W + 1, RadiusY = H + 1, Rect = new RawRectangleF(X1 - 1, Y1 - 1, X2 + 1, Y2 + 1) }, shadowColor);
			device.FillRoundedRectangle(new RoundedRectangle() { RadiusX = W, RadiusY = H, Rect = new RawRectangleF(X1, Y1, X2, Y2) }, color);
		}

		private void DrawRect(float X, float Y, float W, float H, Brush color)
		{
			float X2, Y2;
			X2 = X + W;
			Y2 = Y + H;

			device.DrawRectangle(new RawRectangleF(X, Y, X2, Y2), color, 1.0f, null);
		}

		private void DrawMarker(float X, float Y, int size, Brush color)
		{

			device.DrawLine(new Vector2(X - size / 2, Y), new Vector2(X + size / 2, Y), color);
			device.DrawLine(new Vector2(X, Y - size / 2), new Vector2(X, Y + size / 2), color);
		}

		private void DrawCross(int X, int Y, int size, Brush color)
		{
			//Left
			device.DrawLine(new Vector2(X - size / 2, Y), new Vector2(X - 5, Y), color);
			//Top
			device.DrawLine(new Vector2(X, Y - size / 2), new Vector2(X, Y - 5), color);

			//Right
			device.DrawLine(new Vector2(X + size / 2, Y), new Vector2(X + 5, Y), color);
			//Bottom
			device.DrawLine(new Vector2(X, Y + size / 2), new Vector2(X, Y + 5), color);
		}

		private void DrawMarker(int X, int Y, int size, Brush color, Brush shadow)
		{

			//shadow
			device.DrawLine(new Vector2(X - size / 2, Y + 1), new Vector2(X + size / 2, Y + 1), shadow);
			device.DrawLine(new Vector2(X + 1, Y - size / 2), new Vector2(X + 1, Y + size / 2), shadow);

			device.DrawLine(new Vector2(X - size / 2, Y), new Vector2(X + size / 2, Y), color);
			device.DrawLine(new Vector2(X, Y - size / 2), new Vector2(X, Y + size / 2), color);
		}
		
		public RawColor4 RawColorFromColor(Color color) => new RawColor4(color.R, color.G, color.B, color.A);
		
		private void main_FormClosing(object sender, FormClosingEventArgs e)
		{
			//Win32.CloseHandle(G.Driver);
			//Application.Exit();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			UpdateLongKeys();
		}
	}
}
