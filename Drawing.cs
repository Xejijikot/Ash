using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Achievements;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using static IngameScript.Program;

namespace IngameScript
{
	public static class Drawing
	{
		static Color _BLACK = new Color(0, 0, 0, 255);
		//static Color _ChosenComponent = new Color(10, 20, 30, 255);
		//static Color _ChosenComponent = new Color(0, 1, 0, 255);
		//static float _largeLcd = 240f, _smallLsd = 1080f;
		static float dz = 0.25f - 0.005f;
		static DisplayDef displayDef = new DisplayDef();
		static Vector3D cord_lcd;
		static PlaneD plane;
		static Vector3D point_on_lcd;
		static Vector3D delta;
		static MatrixD m;
		static MatrixD mTrans;
		static Vector3D vectorHudLocal;
		static Vector2 marker;
		static RectangleF rect;
		static float mult;

		public static Dictionary<string, DisplayDef> DisplayDefinitions = new Dictionary<string, DisplayDef>()
		{
			{"TextPanel/TransparentLCDLarge", new DisplayDef() {Delta = 1.25f, Multiplier = 240f, canvasOrientation = new Vector3(1, 0, 0), fullBlock = false }},
			{"TextPanel/HoloLCDLarge", new DisplayDef() {Delta = 0.75f, Multiplier = 240f, canvasOrientation = new Vector3(1, 0, 0), fullBlock = false }},
			{"TextPanel/LargeFullBlockLCDPanel", new DisplayDef() {Delta = 1.25f, Multiplier = 240f, canvasOrientation = new Vector3(0, 0, 1), fullBlock = true }},
			{"TextPanel/TransparentLCDSmall", new DisplayDef() {Delta = 0.25f - 0.005f, Multiplier = 1080f, canvasOrientation = new Vector3(1, 0, 0), fullBlock = false }},
			{"TextPanel/SmallFullBlockLCDPanel", new DisplayDef() {Delta = 0.25f, Multiplier = 1080f, canvasOrientation = new Vector3(0, 0, 1), fullBlock = true }},
			{"TextPanel/HoloLCDSmall", new DisplayDef() {Delta = 0.03f, Multiplier = 1080f, canvasOrientation = new Vector3(1, 0, 0.151f), fullBlock = false }},
		};
		static bool PrepeareCoords(IMyTextPanel surface, Vector3D obspos, Vector3D viewvector)
		{
			displayDef.SetDisplayDef(GetDisplayInfo(surface));
			mult = displayDef.Multiplier;
			dz = displayDef.Delta;
			cord_lcd = surface.GetPosition() + (surface.WorldMatrix.Forward * displayDef.canvasOrientation.X + surface.WorldMatrix.Up * displayDef.canvasOrientation.Y + surface.WorldMatrix.Right * displayDef.canvasOrientation.Z) * dz;
			if (!displayDef.fullBlock)
				plane = new PlaneD(cord_lcd, surface.WorldMatrix.Forward);
			else
				plane = new PlaneD(cord_lcd, surface.WorldMatrix.Right);
			point_on_lcd = plane.Intersection(ref obspos, ref viewvector);
			delta = point_on_lcd - cord_lcd;
			m = surface.WorldMatrix;
			mTrans = MatrixD.Transpose(m);
			vectorHudLocal = Vector3D.TransformNormal(delta, mTrans);
			if (!displayDef.fullBlock)
				marker = new Vector2((float)vectorHudLocal.X, -(float)vectorHudLocal.Y);
			else
				marker = new Vector2(-(float)vectorHudLocal.Z, -(float)vectorHudLocal.Y);
			rect = new RectangleF(
						(surface.TextureSize - surface.SurfaceSize) / 2f,
						surface.SurfaceSize
						);
			if (Math.Abs(marker.X * mult) > (surface.TextureSize.X / 2) || Math.Abs(marker.Y * mult) > (surface.TextureSize.Y / 2))
				return false;
			return true;
		}

		public static void SetupDrawSurface(IMyTextSurface surface)
		{
			surface.ScriptBackgroundColor = Color.Black;
			surface.ContentType = ContentType.SCRIPT;
			surface.Script = "";
		}


		public static void DrawTarget(ref string debugInfo, DrawingInfo dI, int targetingPoint = 0, bool form = true)
		{
			if (dI.Target != null)
			{
				Vector3D target = dI.Target.Position;
				if (targetingPoint == 0)
					if (dI.Target.TargetedPoint != null)
						target = dI.Target.TargetedPoint.GetValueOrDefault();
				if (targetingPoint == 2)
					if (dI.Target.HitPosition != null)
						target = dI.Target.HitPosition.GetValueOrDefault();
				Vector3D viewvector = target - dI.obspos;
				if (!PrepeareCoords(dI.surface, dI.obspos, viewvector))
					return;
				//debugInfo += "GPS: " + surface.CustomName + ":" + point_on_lcd.X + ":" + point_on_lcd.Y + ":" + point_on_lcd.Z + ":#FF75C9F1:\n";
				Vector3D lcdToTarget = target - cord_lcd;
				if (viewvector.Length() > lcdToTarget.Length())
				{
					//debugInfo += $"{marker}\n";
					if (form)
						DrawBoxCorners(dI.c, dI.frame, rect.Center + (marker * mult));
					else
						DrawPoint(dI.c, dI.frame, rect.Center + (marker * mult));

				}
			}

		}
		public static void DrawSubsystem(ref string debugInfo, DrawingInfo dI, TargetSubsystem subsystem, Color w, Color p, Color e)
		{
			Vector3D target = subsystem.GetPosition(dI.Target);
			Vector3D viewvector = target - dI.obspos;
			if (!PrepeareCoords(dI.surface, dI.obspos, viewvector))
				return;
			//debugInfo += "GPS: " + surface.CustomName + ":" + point_on_lcd.X + ":" + point_on_lcd.Y + ":" + point_on_lcd.Z + ":#FF75C9F1:\n";
			Vector3D lcdToTarget = target - cord_lcd;
			Color c;
			switch (subsystem.subsystemType)
			{
				case "Weapons":
					c = w; break;
				case "Propulsion":
					c = p; break;
				default:
					c = e; break;
			}
			if (viewvector.Length() > lcdToTarget.Length())
			{
				//debugInfo += $"{marker}\n";
				DrawPoint(c, dI.frame, rect.Center + (marker * mult));
			}

		}
		public static void DrawTurretTarget(DrawingInfo dI)
		{
			Vector3D targetpos = dI.Target.Position;
			Vector3D viewvector = targetpos - dI.obspos;
			if (!PrepeareCoords(dI.surface, dI.obspos, viewvector))
				return;
			//debugInfo += "GPS: " + surface.CustomName + ":" + point_on_lcd.X + ":" + point_on_lcd.Y + ":" + point_on_lcd.Z + ":#FF75C9F1:\n";
			Vector3D lcdToTarget = targetpos - cord_lcd;
			if (viewvector.Length() > lcdToTarget.Length())
			{
				//debugInfo += $"{marker}\n";
				DrawPoint(dI.c, dI.frame, rect.Center + (marker * mult));
			}
		}
		public static void DrawInterceptVector(DrawingInfo dI)
		{
			Vector3D viewvector = dI.point - dI.obspos;
			if (!PrepeareCoords(dI.surface, dI.obspos, viewvector))
				return;
			Vector3D lcdToTarget = dI.point - cord_lcd;
			if (viewvector.Length() > lcdToTarget.Length())
			{

				//DrawSpritesRectangle(c, frame, rect.Center + (marker * mult));
				//DrawSpriteX(c, frame, rect.Center + (marker * mult));
				DrawCircle(dI.c, dI.frame, rect.Center + (marker * mult));
			}
		}
		public static void DrawBallisticPoint(DrawingInfo dI, Color interfaceColor, bool distanceB = false)
		{
			Vector3D viewvector = dI.point - dI.obspos;
			if (!PrepeareCoords(dI.surface, dI.obspos, viewvector))
				return;
			Vector3D lcdToTarget = dI.point - cord_lcd;
			if (viewvector.Length() > lcdToTarget.Length())
			{
				DrawSpriteX(dI.c, dI.frame, rect.Center + (marker * mult));
				if (distanceB)
				{
					double distance = (dI.point - dI.obspos).Length();
					DrawDistance(interfaceColor, dI.frame, rect.Center + (marker * mult), distance, 0.7f);
				}
			}
		}
		public static void BattleInterface(DrawingInfo dI, string Language, bool searching, TankInfo tankInfo, DWI dwi, float scale = 1f, double distance = 0, float locked = 1.0f, bool isTurret = false, bool isVeachle = false, bool autoAim = false, bool aimAssist = false)
		{
			Vector3D viewvector = dI.point;
			if (!PrepeareCoords(dI.surface, dI.obspos, viewvector))
				return;
			//debugInfo += "GPS: " + surface.CustomName + ":" + cord_lcd.X + ":" + cord_lcd.Y + ":" + cord_lcd.Z + ":#FF75C9F1:\n";
			DrawInterface(dI, Language, isTurret, isVeachle);
			//string aiStatus = "";
			Vector2 statusPos = new Vector2(-40, 60);
			Vector2 infoPos = new Vector2(-33, 40);
			Vector2 wInfoPos = new Vector2(-200, 78);
			DrawSight(dI.c, dI.frame, rect.Center + (marker * mult));
			if (searching)
				DrawXSight(dI.c, dI.frame, rect.Center + (marker * mult));
			if (distance != 0)
				DrawDistance(dI.c, dI.frame, rect.Center + (marker * mult), distance, 0.7f);
			if (locked < 0.9f)
				LosingTarget(dI.c, dI.frame, rect.Center + (marker * mult), locked, 0.7f);
			if (aimAssist)
				DrawLockedMode(dI.c, Language, dI.frame, rect.Center + statusPos + (marker * mult), autoAim, 0.7f);
			if (dwi.draw)
				DrawWeaponInfo(dI.c, Language, dwi, dI.frame, rect.Center + wInfoPos + (marker * mult), 0.7f);
			if (tankInfo.drawTank)
			{
				Vector2 tankPos = new Vector2(150, 150);
				DrawHull(dI.c, dI.frame, tankPos + rect.Center + (marker * mult), tankInfo.hullRotation);
				DrawTurret(dI.c, dI.frame, tankPos + rect.Center + (marker * mult), tankInfo.turretRotation);
				DrawTurretInfo(dI.c, dI.frame, tankPos + infoPos + rect.Center + (marker * mult), tankInfo.block, tankInfo.centered);
			}
			else
			{
				if (isTurret)
					DrawTurretInfo(dI.c, dI.frame, infoPos + rect.Center + (marker * mult), tankInfo.block, tankInfo.centered);
			}
		}
		static void DrawInterface(DrawingInfo dI, string Language, bool isTurret, bool isVeachle)
		{
			string aiMode = "";
			//string aiStatus = "";
			Vector2 logoPos = new Vector2(-200, -200);
			Vector2 modPos = new Vector2(100, -200);
			Vector2 centerPos = rect.Center + logoPos + (marker * mult);
			DrawLogo(dI.c, Language, dI.frame, centerPos, 0.7f);
			if (isTurret)
			{
				aiMode = Languages.Translate(Language, "TURRET");
			}
			else if (isVeachle)
			{
				aiMode = Languages.Translate(Language, "HULL");
			}
			DrawAiMode(dI.c, dI.frame, aiMode, rect.Center + modPos + (marker * mult), 0.7f);
		}
		static void LosingTarget(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float percent, float scale = 1f)
		{
			//frame.Add(new MySprite(SpriteType.TEXT, "Срыв\nзахвата!", new Vector2(112f, -100f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 0.7f * scale)); // text1
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(120f, 0f) * scale + centerPos, new Vector2(16f, 100f) * scale, c, null, TextAlignment.CENTER, 0f));
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(120f, 0f) * scale + centerPos, new Vector2(14f, 98f) * scale, _BLACK, null, TextAlignment.CENTER, 0f));
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(120f, 0f + (48 * (1 - percent))) * scale + centerPos, new Vector2(12f, 96f * percent) * scale, c, null, TextAlignment.CENTER, 0f));
		}
		static void DrawDistance(Color c, MySpriteDrawFrame frame, Vector2 centerPos, double distance, float scale = 1f)
		{
			if (distance < 1000)
				frame.Add(new MySprite(SpriteType.TEXT, $"{Math.Round(distance, 0)} м", new Vector2(-20f, 40f) + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
			else
				frame.Add(new MySprite(SpriteType.TEXT, $"{Math.Round(distance / 1000, 2)} км", new Vector2(-20f, 40f) + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
		}
		static void DrawSight(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
		{
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(0f, 0f) * scale + centerPos,
				Size = new Vector2(1f, 1f) * scale,
				Color = c,
				RotationOrScale = 0f
			});
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(0f, 10f) * scale + centerPos,
				Size = new Vector2(2f, 5f) * scale,
				Color = c,
				RotationOrScale = 0f
			});
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(10f, 0f) * scale + centerPos,
				Size = new Vector2(5f, 2f) * scale,
				Color = c,
				RotationOrScale = 0f
			});
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(-10f, 0f) * scale + centerPos,
				Size = new Vector2(5f, 2f) * scale,
				Color = c,
				RotationOrScale = 0f
			});
		}
		static void DrawLogo(Color c, string Language, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
		{
			string scriptName = Languages.Translate(Language, "SCRIPT_NAME");
			frame.Add(new MySprite(0, "Triangle", new Vector2(145f, 17.5f) * scale + centerPos, new Vector2(50f, 42f) * scale, c, null, TextAlignment.CENTER, 3.1416f));
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos, new Vector2(156f, 36f) * scale, c, null, TextAlignment.CENTER, 0f));
			frame.Add(new MySprite(0, "Triangle", new Vector2(145f, 17.5f) * scale + centerPos, new Vector2(46f, 39f) * scale, _BLACK, null, TextAlignment.CENTER, 3.1416f));
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos, new Vector2(154f, 34f) * scale, _BLACK, null, TextAlignment.CENTER, 0f));
			frame.Add(new MySprite(SpriteType.TEXT, scriptName, new Vector2(5f, 0f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
		}
		static void DrawAiMode(Color c, MySpriteDrawFrame frame, string text, Vector2 centerPos, float scale = 1f)
		{
			frame.Add(new MySprite(0, "Triangle", new Vector2(-10f, 17.5f) * scale + centerPos, new Vector2(50f, 42f) * scale, c, null, TextAlignment.CENTER, 3.1416f));
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos, new Vector2(156f, 36f) * scale, c, null, TextAlignment.CENTER, 0f));
			frame.Add(new MySprite(0, "Triangle", new Vector2(-10f, 17.5f) * scale + centerPos, new Vector2(46f, 39f) * scale, _BLACK, null, TextAlignment.CENTER, 3.1416f));
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos, new Vector2(153f, 34f) * scale, _BLACK, null, TextAlignment.CENTER, 0f));
			frame.Add(new MySprite(SpriteType.TEXT, text, new Vector2(5f, 0f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
		}
		static void DrawLockedMode(Color c, string Language, MySpriteDrawFrame frame, Vector2 centerPos, bool autotarget, float scale = 1f)
		{
			string auto;
			string aim;
			if (autotarget)
			{
				aim = Languages.Translate(Language, "AIM");
				auto = Languages.Translate(Language, "AUTO");
				frame.Add(new MySprite(SpriteType.TEXT, aim, new Vector2(0f, 0f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale));
				frame.Add(new MySprite(SpriteType.TEXT, auto, new Vector2(-35f, 25f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale));
				frame.Add(new MySprite(0, "Circle", new Vector2(-11f, 15f) * scale + centerPos, new Vector2(7f, 7f) * scale, c, null, TextAlignment.CENTER, 0f));
			}
			else
			{
				aim = Languages.Translate(Language, "TRACKING");
				auto = Languages.Translate(Language, "ASSIST");
				frame.Add(new MySprite(SpriteType.TEXT, aim, new Vector2(0f, 0f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale));
				frame.Add(new MySprite(SpriteType.TEXT, auto, new Vector2(-5f, 25f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale));
				frame.Add(new MySprite(0, "Circle", new Vector2(-11f, 15f) * scale + centerPos, new Vector2(7f, 7f) * scale, c, null, TextAlignment.CENTER, 0f));
			}
		}
		static void DrawWeaponInfo(Color c, string Language, DWI dwi, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
		{
			string weapon = Languages.Translate(Language, "WEAPON") + ":";
			string name = dwi.name;
			string weapomType = Languages.Translate(Language, dwi.weaponDef.type);
			frame.Add(new MySprite(SpriteType.TEXT, weapon, new Vector2(0f, 0f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale));
			frame.Add(new MySprite(SpriteType.TEXT, name, new Vector2(-11f, 25f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale));
			frame.Add(new MySprite(SpriteType.TEXT, weapomType, new Vector2(-11f, 50f) * scale + centerPos, null, c, "DEBUG", TextAlignment.LEFT, 1f * scale));
			frame.Add(new MySprite(0, "Circle", new Vector2(-11f, 15f) * scale + centerPos, new Vector2(7f, 7f) * scale, c, null, TextAlignment.CENTER, 0f));
		}
		static void DrawXSight(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)

		{
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(20f, 20f) * scale + centerPos, new Vector2(1f, 8f) * scale, c, null, TextAlignment.CENTER, -0.7854f)); // sprite1
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(20f, -20f) * scale + centerPos, new Vector2(1f, 8f) * scale, c, null, TextAlignment.CENTER, 0.7854f)); // sprite6
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(-20f, -20f) * scale + centerPos, new Vector2(1f, 8f) * scale, c, null, TextAlignment.CENTER, -0.7854f)); // sprite7
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(-20f, 20f) * scale + centerPos, new Vector2(1f, 8f) * scale, c, null, TextAlignment.CENTER, 0.7854f)); // sprite8
		}
		static void DrawBox(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
		{
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(0f, 0f) * scale + centerPos,
				Size = new Vector2(20f, 20f) * scale,
				Color = c,
				RotationOrScale = 0f
			}); // sprite1
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(0f, 0f) * scale + centerPos,
				Size = new Vector2(18f, 18f) * scale,
				Color = _BLACK,
				RotationOrScale = 0f
			}); // sprite2
		}
		static void DrawPoint(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
		{
			frame.Add(new MySprite(0, "Circle", new Vector2(0f, 0f) * scale + centerPos, new Vector2(5f, 5f) * scale, c, null, TextAlignment.CENTER, 0f)); // sprite1
		}
		static void DrawBoxCorners(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
		{
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(0f, 0f) * scale + centerPos,
				Size = new Vector2(30f, 30f) * scale,
				Color = c,
				RotationOrScale = 0f
			}); // sprite1
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(0f, 0f) * scale + centerPos,
				Size = new Vector2(28f, 28f) * scale,
				Color = _BLACK,
				RotationOrScale = 0f
			}); // sprite2
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(0f, 0f) * scale + centerPos,
				Size = new Vector2(20f, 30f) * scale,
				Color = new Color(0, 0, 0, 255),
				RotationOrScale = 0f
			}); // sprite2
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "SquareSimple",
				Position = new Vector2(0f, 0f) * scale + centerPos,
				Size = new Vector2(30f, 20f) * scale,
				Color = new Color(0, 0, 0, 255),
				RotationOrScale = 0f
			}); // sprite3
		}
		static void DrawCircle(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
		{
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "Circle",
				Position = new Vector2(0f, 0f) * scale + centerPos,
				Size = new Vector2(20f, 20f) * scale,
				Color = c,
				RotationOrScale = 0f
			}); // sprite1
			frame.Add(new MySprite()
			{
				Type = 0,
				Alignment = TextAlignment.CENTER,
				Data = "Circle",
				Position = new Vector2(0f, 0f) * scale + centerPos,
				Size = new Vector2(18f, 18f) * scale,
				Color = _BLACK,
				RotationOrScale = 0f
			}); // sprite2
		}
		public static bool GetObserverPos(ref Vector3D? k, ref Vector3D? forwardDirection, float up, float backward, IMyShipController cockpit = null, List<IMyCameraBlock> all_cams = null)
		{
			//List<IMyCameraBlock> all_cams = new List<IMyCameraBlock>();
			//GridTerminalSystem.GetBlocksOfType(all_cams, camera => camera.IsActive);
			IMyCameraBlock cam = null;
			foreach (var viewCam in all_cams)
			{
				if (viewCam.IsActive)
				{
					cam = viewCam;
					break;
				}
			}
			if (cam != null)
			{
				k = cam.WorldMatrix.Translation + cam.WorldMatrix.Forward * (cam.CubeGrid.GridSize / 2f - 0.005f);
				forwardDirection = cam.WorldMatrix.Forward;
				return true;
				//j = new RayD(k.Value, cam.WorldMatrix.Forward);
			}
			else if (cockpit != null && cockpit.IsUnderControl)
			{
				k = cockpit.GetPosition() + cockpit.WorldMatrix.Up * (up) + cockpit.WorldMatrix.Backward * (backward);
				forwardDirection = cockpit.WorldMatrix.Forward;
				return true;
				//j = new RayD(k.Value, cockpit.WorldMatrix.Forward);
			}
			else
				return false;
		}
		public static void DrawSpriteX(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f, float rotation = 0f)
		{
			float sin = (float)Math.Sin(rotation);
			float cos = (float)Math.Cos(rotation);
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, c, null, TextAlignment.CENTER, 0.7854f + rotation)); // sprite1
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) * scale + centerPos, new Vector2(10f, 2f) * scale, c, null, TextAlignment.CENTER, 0.7854f + rotation)); // sprite2
			DrawPoint(_BLACK, frame, centerPos, 1.5f);
			/*DrawPoint(c, frame, centerPos + new Vector2(4f, 4f), 0.7f);
			DrawPoint(c, frame, centerPos + new Vector2(-4f, 4f), 0.7f);
			DrawPoint(c, frame, centerPos + new Vector2(4f, -4f), 0.7f);
			DrawPoint(c, frame, centerPos + new Vector2(-4f, -4f), 0.7f);
			*/
		}

		public static void DrawSpritesRectangle(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
		{
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(4f, 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, c, null, TextAlignment.CENTER, 0f)); // right
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(-4f, 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, c, null, TextAlignment.CENTER, 0f)); // left
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(0f, -4f) * scale + centerPos, new Vector2(10f, 2f) * scale, c, null, TextAlignment.CENTER, 0f)); // down
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(0f, 4f) * scale + centerPos, new Vector2(10f, 2f) * scale, c, null, TextAlignment.CENTER, 0f)); // top
		}

		public static void DrawSpritesRectangleRotateAndScale(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f, float rotation = 0f)
		{
			float sin = (float)Math.Sin(rotation);
			float cos = (float)Math.Cos(rotation);
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * 4f - sin * 0f, sin * 4f + cos * 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, c, null, TextAlignment.CENTER, 0f + rotation)); // right
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * -4f - sin * 0f, sin * -4f + cos * 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, c, null, TextAlignment.CENTER, 0f + rotation)); // left
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * 0f - sin * -4f, sin * 0f + cos * -4f) * scale + centerPos, new Vector2(10f, 2f) * scale, c, null, TextAlignment.CENTER, 0f + rotation)); // down
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * 0f - sin * 4f, sin * 0f + cos * 4f) * scale + centerPos, new Vector2(10f, 2f) * scale, c, null, TextAlignment.CENTER, 0f + rotation)); // top
		}
		public static void DrawTurret(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float rotation = 0f)
		{
			float sin = (float)Math.Sin(rotation);
			float cos = (float)Math.Cos(rotation);
			frame.Add(new MySprite(0, "Circle", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) + centerPos, new Vector2(25f, 25f), c, null, TextAlignment.CENTER, 0f + rotation)); // sprite1
			frame.Add(new MySprite(0, "Circle", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) + centerPos, new Vector2(23f, 23f), _BLACK, null, TextAlignment.CENTER, 0f + rotation)); // sprite1
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * 0f - sin * -39f, sin * 0f + cos * -39f) + centerPos, new Vector2(5f, 51f), _BLACK, null, TextAlignment.CENTER, 0f + rotation)); // sprite3
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * 0f - sin * -39f, sin * 0f + cos * -39f) + centerPos, new Vector2(3f, 49f), c, null, TextAlignment.CENTER, 0f + rotation)); // sprite2
		}
		public static void DrawHull(Color c, MySpriteDrawFrame frame, Vector2 centerPos, float rotation = 0f)
		{
			float sin = (float)Math.Sin(rotation);
			float cos = (float)Math.Cos(rotation);
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) + centerPos, new Vector2(40f, 60f), c, null, TextAlignment.CENTER, 0f + rotation)); // sprite4
			frame.Add(new MySprite(0, "SquareSimple", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) + centerPos, new Vector2(38f, 58f), _BLACK, null, TextAlignment.CENTER, 0f + rotation)); // sprite4
			frame.Add(new MySprite(0, "AH_BoreSight", new Vector2(cos * 0f - sin * -40f, sin * 0f + cos * -40f) + centerPos, new Vector2(40f, 40f), c, null, TextAlignment.CENTER, -1.5708f + rotation)); // sprite5
		}
		public static void DrawTurretInfo(Color c, MySpriteDrawFrame frame, Vector2 centerPos, bool block, bool centered)
		{
			string locked = "◉ Block";
			string сentering = "↻ Centering";
			if (block)
				frame.Add(new MySprite(SpriteType.TEXT, locked, centerPos, null, c, "DEBUG", TextAlignment.LEFT, 0.7f));
			if (centered)
				frame.Add(new MySprite(SpriteType.TEXT, сentering, centerPos + new Vector2(-16, 19), null, c, "DEBUG", TextAlignment.LEFT, 0.7f));
		}
		static DisplayDef GetDisplayInfo(IMyTextPanel surface)
		{
			DisplayDef displayD;
			string key = SystemHelper.GetKey(surface);
			if (DisplayDefinitions.TryGetValue(key, out displayD))
				return displayD;
			if (surface.CubeGrid.GridSizeEnum == MyCubeSize.Large)
				return new DisplayDef() { Delta = 1.25f, Multiplier = 240f, canvasOrientation = new Vector3I(1, 0, 0), fullBlock = false };
			else
				return new DisplayDef() { Delta = 0.25f - 0.005f, Multiplier = 1080f, canvasOrientation = new Vector3I(1, 0, 0), fullBlock = false };
		}
		public static void AddExceptionMessage(StringBuilder builder, Exception e)
		{
			builder
				.AppendLine("\n-------Crash message-------")
				.Append(e.GetType().ToString())
				.Append(": ")
				.AppendLine(e.Message);
			{
				var innerException = e.InnerException;
				while (innerException != null)
				{
					builder
						.Append("\n-----Inner exception-----\n")
						.Append(innerException.GetType().ToString())
						.Append(": ")
						.Append(innerException.Message);
					innerException = innerException.InnerException;
				}
			}

			builder
				.Append("\n\n-------Stack trace-------\n")
				.Append(e.StackTrace);
			{
				var innerException = e.InnerException;
				while (innerException != null)
				{
					builder
						.Append("\n-----Inner exception-----\n")
						.Append(innerException.StackTrace);
					innerException = innerException.InnerException;
				}
			}
		}
	}
	public class DrawingInfo
	{
		public EnemyTargetedInfo Target;
		public Vector3D point;
		public MySpriteDrawFrame frame;
		public IMyTextPanel surface;
		public Vector3D obspos;
		public Color c;
		public DrawingInfo(Vector3D point, MySpriteDrawFrame frame, IMyTextPanel surface, Vector3D obspos, Color c)
		{
			this.point = point;
			this.frame = frame;
			this.surface = surface;
			this.obspos = obspos;
			this.c = c;
		}
		public DrawingInfo(EnemyTargetedInfo target, MySpriteDrawFrame frame, IMyTextPanel surface, Vector3D obspos, Color c)
		{
			this.Target = target;
			this.frame = frame;
			this.surface = surface;
			this.obspos = obspos;
			this.c = c;
		}
	}
	public class TankInfo
	{
		public bool block, centered;
		public float turretRotation;
		public bool drawTank;
		public float hullRotation;
		public TankInfo(float turretRotation, bool drawTank, float hullRotation, bool block, bool centered)
		{
			this.block = block;
			this.centered = centered;
			this.turretRotation = turretRotation;
			this.drawTank = drawTank;
			this.hullRotation = hullRotation;

		}
	}
	public class DWI //Drawing Weapon Info
	{
		public bool draw;
		public string name;
		public WeaponDef weaponDef;
	}
	public class DisplayDef
	{
		public float Delta, Multiplier;
		public Vector3 canvasOrientation;
		public bool fullBlock;
		public void SetDisplayDef(DisplayDef displayDef)
		{
			Delta = displayDef.Delta;
			Multiplier = displayDef.Multiplier;
			canvasOrientation = displayDef.canvasOrientation;
			fullBlock = displayDef.fullBlock;
		}
	}

}
