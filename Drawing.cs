using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
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

namespace IngameScript
{
    public static class Drawing
    {
        static Color _BLACK = new Color(0, 0, 0, 255);
        static Color _ChosenComponent = new Color(10, 20, 30, 255);
        //static Color _ChosenComponent = new Color(0, 1, 0, 255);
        static float _largeLcd = 240f, _smallLsd = 1080f;
        static float dz = 0.25f - 0.005f;
        static bool large = false;
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
        static bool PrepeareCoords(IMyTextPanel surface, Vector3D obspos, Vector3D viewvector)
        {
            mult = _smallLsd;
            if (large)
                mult = _largeLcd;
            large = false;
            if (surface.CubeGrid.GridSizeEnum == MyCubeSize.Large)
            {
                dz = 1.25f;
                large = true;
            }
            else
            {
                dz = 0.25f - 0.005f;
                large = false;
            }
            cord_lcd = surface.GetPosition() + surface.WorldMatrix.Forward * dz;
            plane = new PlaneD(cord_lcd, surface.WorldMatrix.Forward);
            point_on_lcd = plane.Intersection(ref obspos, ref viewvector);
            delta = point_on_lcd - cord_lcd;
            m = surface.WorldMatrix;
            mTrans = MatrixD.Transpose(m);
            vectorHudLocal = Vector3D.TransformNormal(delta, mTrans);
            marker = new Vector2((float)vectorHudLocal.X, -(float)vectorHudLocal.Y);
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


        public static void DrawTarget(ref string debugInfo, DrawingInfo dI, int targetingPoint = 0)
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
                    float size = _smallLsd;
                    DrawBox(dI.color, dI.frame, rect.Center + (marker * mult));
                }
            }

        }
        public static void DrawSubsystemType(ref string debugInfo, DrawingInfo dI, Color w, Color p, Color e)
        {
            Vector3D viewvector = dI.point;
            if (!PrepeareCoords(dI.surface, dI.obspos, viewvector))
                return;
            string weapon = "Weapons: " + dI.Target.WeaponSubsystems.Count;
            string prop = "Propulsion: " + dI.Target.PropSubsystems.Count;
            string power = "PowerSystems: " + dI.Target.PowerSubsystems.Count;
            float step = 20f;
            dI.frame.Add(new MySprite(SpriteType.TEXT, weapon, new Vector2(-220f, 150f) + rect.Center, null, w, "DEBUG", TextAlignment.LEFT, 0.7f));
            dI.frame.Add(new MySprite(SpriteType.TEXT, prop, new Vector2(-220f, 150f + step) + rect.Center, null, p, "DEBUG", TextAlignment.LEFT, 0.7f));
            dI.frame.Add(new MySprite(SpriteType.TEXT, power, new Vector2(-220f, 150f + step * 2) + rect.Center, null, e, "DEBUG", TextAlignment.LEFT, 0.7f));
            /*switch (subsystem.subsystemType)
            {
                case "Weapons":
                    color = w; break;
                case "Propulsion":
                    color = p; break;
                default:
                    color = e; break;
            }*/
        }
        public static void DrawSubsystem(ref string debugInfo, DrawingInfo dI, TargetSubsystem subsystem, Color w, Color p, Color e)
        {
            Vector3D target = subsystem.GetPosition(dI.Target);
            Vector3D viewvector = target - dI.obspos;
            if (!PrepeareCoords(dI.surface, dI.obspos, viewvector))
                return;
            //debugInfo += "GPS: " + surface.CustomName + ":" + point_on_lcd.X + ":" + point_on_lcd.Y + ":" + point_on_lcd.Z + ":#FF75C9F1:\n";
            Vector3D lcdToTarget = target - cord_lcd;
            Color color;
            switch (subsystem.subsystemType)
            {
                case "Weapons":
                    color = w; break;
                case "Propulsion":
                    color = p; break;
                default:
                    color = e; break;
            }
            if (viewvector.Length() > lcdToTarget.Length())
            {
                //debugInfo += $"{marker}\n";
                DrawPoint(color, dI.frame, rect.Center + (marker * mult));
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
                DrawPassiveTarget(dI.color, dI.frame, rect.Center + (marker * mult));
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
                if (large)
                {
                    //DrawSpritesRectangle(color, frame, rect.Center + (marker * _largeLcd));
                    //DrawSpriteX(color, frame, rect.Center + (marker * _largeLcd));
                    DrawCircle(dI.color, dI.frame, rect.Center + (marker * _largeLcd));
                }
                else
                {
                    //DrawSpritesRectangle(color, frame, rect.Center + (marker * _smallLsd));
                    //DrawSpriteX(color, frame, rect.Center + (marker * _smallLsd));
                    DrawCircle(dI.color, dI.frame, rect.Center + (marker * _smallLsd));
                }
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
                DrawSpriteX(dI.color, dI.frame, rect.Center + (marker * mult));
                if (distanceB)
                {
                    double distance = (dI.point - dI.obspos).Length();
                    DrawDistance(interfaceColor, dI.frame, rect.Center + (marker * mult), distance, 0.7f);
                }
            }
        }
        public static void BattleInterface(DrawingInfo dI, string Language, bool searching, TankInfo tankInfo, float scale = 1f, double distance = 0, float locked = 1.0f, bool isTurret = false, bool isVeachle = false, bool autoAim = false, bool aimAssist = false)
        {
            Vector3D viewvector = dI.point;
            PrepeareCoords(dI.surface, dI.obspos, viewvector);
            //debugInfo += "GPS: " + surface.CustomName + ":" + cord_lcd.X + ":" + cord_lcd.Y + ":" + cord_lcd.Z + ":#FF75C9F1:\n";
            DrawInterface(dI, Language, isTurret, isVeachle);
            //string aiStatus = "";
            Vector2 statusPos = new Vector2(-40, 60);
            DrawSight(dI.color, dI.frame, rect.Center + (marker * mult));
            if (searching)
                DrawXSight(dI.color, dI.frame, rect.Center + (marker * mult));
            if (distance != 0)
                DrawDistance(dI.color, dI.frame, rect.Center + (marker * mult), distance, 0.7f);
            if (locked < 0.9f)
                LosingTarget(dI.color, dI.frame, rect.Center + (marker * mult), locked, 0.7f);
            if (aimAssist)
                DrawLockedMode(dI.color, Language, dI.frame, rect.Center + statusPos + (marker * mult), autoAim, 0.7f);
            if (tankInfo.drawTank)
            {
                Vector2 tankPos = new Vector2(150, 150);
                DrawHull(dI.color, dI.frame, tankPos + rect.Center + (marker * mult), tankInfo.hullRotation);
                DrawTurret(dI.color, dI.frame, tankPos + rect.Center + (marker * mult), tankInfo.turretRotation);
            }
            
        }
        public static void SettingsInterface(DrawingInfo dI, ref Dictionary<int, MyTuple<string, float, float>> obsDict, ref Dictionary<int, MyTuple<string, float, float, float>> weaponDict, int nubmerOfLine, int numberOfCockpit, int numberOfWeapon, string Language, float scale = 1f, bool isTurret = false, bool isVeachle = false)
        {
            Vector3D viewvector = dI.point;
            PrepeareCoords(dI.surface, dI.obspos, viewvector);
            DrawInterface(dI, Language, isTurret, isVeachle);
            Vector2 settingsPos = new Vector2(0, -160);
            DrawSettings(dI.color, Language, dI.frame, rect.Center + settingsPos, 0, 0.7f);
            float step = 100f;
            Vector2 moduleCoord = new Vector2(0f, -50f);
            MyTuple<string, float, float> obsInfo;
            obsDict.TryGetValue(numberOfCockpit, out obsInfo);
            MyTuple<string, float, float, float> weaponInfo;
            weaponDict.TryGetValue(numberOfWeapon, out weaponInfo);
            string cocpitType = Languages.Translate(Language, obsInfo.Item1);
            string weaponType = Languages.Translate(Language, weaponInfo.Item1);
            DrawModule0(dI.color, Language, dI.frame, rect.Center + moduleCoord, nubmerOfLine, cocpitType, weaponType, step, 0.7f);
        }
        public static void SettingsInterface(DrawingInfo dI, int nubmerOfLine, float obsUp, float obsBack, float weaponVel, float weaponRange, string Language, float scale = 1f, bool isTurret = false, bool isVeachle = false)
        {
            Vector3D viewvector = dI.point;
            PrepeareCoords(dI.surface, dI.obspos, viewvector);
            DrawInterface(dI, Language, isTurret, isVeachle);
            Vector2 settingsPos = new Vector2(0, -160);
            DrawSettings(dI.color, Language, dI.frame, rect.Center + settingsPos + (marker * mult), 1, 0.7f);
            float step = 70f;
            Vector2 moduleCoord = new Vector2(0f, -50f);
            DrawModule1(dI.color, Language, dI.frame, rect.Center + moduleCoord, nubmerOfLine, obsUp, obsBack, weaponVel, weaponRange, step, 0.7f);
        }
        public static void SettingsInterface(DrawingInfo dI, int nubmerOfLine, int targetingPoint, float lockRange, bool enemy, bool neutral, bool allie, string Language, float scale = 1f, bool isTurret = false, bool isVeachle = false)
        {
            Vector3D viewvector = dI.point;
            PrepeareCoords(dI.surface, dI.obspos, viewvector);
            DrawInterface(dI, Language, isTurret, isVeachle);
            Vector2 settingsPos = new Vector2(0, -160);
            DrawSettings(dI.color, Language, dI.frame, rect.Center + settingsPos + (marker * mult), 2, 0.7f);
            float step = 70f;
            Vector2 moduleCoord = new Vector2(0f, -50f);
            DrawModule2(dI.color, Language, dI.frame, rect.Center + moduleCoord, nubmerOfLine, targetingPoint, lockRange, enemy, neutral, allie, step, 0.7f);
        }
        static void DrawInterface(DrawingInfo dI, string Language, bool isTurret, bool isVeachle)
        {
            string aiMode = "";
            //string aiStatus = "";
            Vector2 logoPos = new Vector2(-200, -200);
            Vector2 modPos = new Vector2(100, -200);
            Vector2 centerPos = rect.Center + logoPos + (marker * mult);
            DrawLogo(dI.color, Language, dI.frame, centerPos, 0.7f);
            if (isTurret)
            {
                aiMode = Languages.Translate(Language, "TURRET");
            }
            else if (isVeachle)
            {
                aiMode = Languages.Translate(Language, "HULL");
            }
            DrawAiMode(dI.color, dI.frame, aiMode, rect.Center + modPos + (marker * mult), 0.7f);
        }
        static void LosingTarget(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float percent, float scale = 1f)
        {
            //frame.Add(new MySprite(SpriteType.TEXT, "Срыв\nзахвата!", new Vector2(112f, -100f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 0.7f * scale)); // text1
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(120f, 0f) * scale + centerPos, new Vector2(16f, 100f) * scale, color, null, TextAlignment.CENTER, 0f));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(120f, 0f) * scale + centerPos, new Vector2(14f, 98f) * scale, _BLACK, null, TextAlignment.CENTER, 0f));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(120f, 0f + (48 * (1 - percent))) * scale + centerPos, new Vector2(12f, 96f * percent) * scale, color, null, TextAlignment.CENTER, 0f));
        }
        static void DrawDistance(Color color, MySpriteDrawFrame frame, Vector2 centerPos, double distance, float scale = 1f)
        {
            if (distance < 1000)
                frame.Add(new MySprite(SpriteType.TEXT, $"{Math.Round(distance, 0)} м", new Vector2(-20f, 40f) + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
            else
                frame.Add(new MySprite(SpriteType.TEXT, $"{Math.Round(distance / 1000, 2)} км", new Vector2(-20f, 40f) + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
        }
        static void DrawSight(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
        {
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(0f, 0f) * scale + centerPos,
                Size = new Vector2(1f, 1f) * scale,
                Color = color,
                RotationOrScale = 0f
            });
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(0f, 10f) * scale + centerPos,
                Size = new Vector2(2f, 5f) * scale,
                Color = color,
                RotationOrScale = 0f
            });
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(10f, 0f) * scale + centerPos,
                Size = new Vector2(5f, 2f) * scale,
                Color = color,
                RotationOrScale = 0f
            });
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(-10f, 0f) * scale + centerPos,
                Size = new Vector2(5f, 2f) * scale,
                Color = color,
                RotationOrScale = 0f
            });
        }
        static void DrawLogo(Color color, string Language, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
        {
            string scriptName = Languages.Translate(Language, "SCRIPT_NAME");
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(145f, 17.5f) * scale + centerPos, new Vector2(50f, 42f) * scale, color, null, TextAlignment.CENTER, 3.1416f));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos, new Vector2(156f, 36f) * scale, color, null, TextAlignment.CENTER, 0f));
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(145f, 17.5f) * scale + centerPos, new Vector2(46f, 39f) * scale, _BLACK, null, TextAlignment.CENTER, 3.1416f));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos, new Vector2(154f, 34f) * scale, _BLACK, null, TextAlignment.CENTER, 0f));
            frame.Add(new MySprite(SpriteType.TEXT, scriptName, new Vector2(5f, 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
        }
        static void DrawAiMode(Color color, MySpriteDrawFrame frame, string text, Vector2 centerPos, float scale = 1f)
        {
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(-10f, 17.5f) * scale + centerPos, new Vector2(50f, 42f) * scale, color, null, TextAlignment.CENTER, 3.1416f));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos, new Vector2(156f, 36f) * scale, color, null, TextAlignment.CENTER, 0f));
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(-10f, 17.5f) * scale + centerPos, new Vector2(46f, 39f) * scale, _BLACK, null, TextAlignment.CENTER, 3.1416f));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos, new Vector2(153f, 34f) * scale, _BLACK, null, TextAlignment.CENTER, 0f));
            frame.Add(new MySprite(SpriteType.TEXT, text, new Vector2(5f, 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
        }
        static void DrawSettings(Color color, string Language, MySpriteDrawFrame frame, Vector2 centerPos, int module = 0, float scale = 1f)
        {
            //"Настройки"
            Vector2 englD = new Vector2(0, 0);
            if (Language == "English")
                englD = new Vector2(20, 0);
            string settings = Languages.Translate(Language, "SETTINGS");
            Vector2 delta = new Vector2(-65, -27);
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(145f, 12f) * scale + centerPos + delta * scale, new Vector2(50f, 42f) * scale, color, null, TextAlignment.CENTER, 0f)); // sprite3Copy
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(-10f, 12f) * scale + centerPos + delta * scale, new Vector2(50f, 42f) * scale, color, null, TextAlignment.CENTER, 0f)); // sprite3
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos + delta * scale, new Vector2(156f, 36f) * scale, color, null, TextAlignment.CENTER, 0f)); // sprite1
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(145f, 12f) * scale + centerPos + delta * scale, new Vector2(46f, 39f) * scale, _BLACK, null, TextAlignment.CENTER, 0f)); // sprite4Copy
            frame.Add(new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(-10f, 12f) * scale + centerPos + delta * scale, new Vector2(46f, 39f) * scale, _BLACK, null, TextAlignment.CENTER, 0f)); // sprite4
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(68f, 15f) * scale + centerPos + delta * scale, new Vector2(154f, 34f) * scale, _BLACK, null, TextAlignment.CENTER, 0f)); // sprite2
            frame.Add(new MySprite(SpriteType.TEXT, settings, new Vector2(5f, 0f) * scale + centerPos + delta * scale, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
                                                                                                                                                                               //Выбор раздела
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 38f) * scale + centerPos, new Vector2(400f, 30f) * scale, new Color(255, 255, 255, 255), null, TextAlignment.CENTER, 0f)); // фон рамки
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 38f) * scale + centerPos, new Vector2(398f, 28f) * scale, _BLACK, null, TextAlignment.CENTER, 0f));//рамка
            string simple = Languages.Translate(Language, "SIMPLE");
            string advanced = Languages.Translate(Language, "ADVANCED");
            string radar = Languages.Translate(Language, "RADAR");
            switch (module)
            {
                case 0:
                    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-142f, 38f) * scale + centerPos, new Vector2(114f, 28f) * scale, _ChosenComponent, null, TextAlignment.CENTER, 0f));
                    break;
                case 1:
                    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(3, 38f) * scale + centerPos, new Vector2(176, 28f) * scale, _ChosenComponent, null, TextAlignment.CENTER, 0f));
                    break;
                case 2:
                    frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(144, 38f) * scale + centerPos, new Vector2(110f, 28f) * scale, _ChosenComponent, null, TextAlignment.CENTER, 0f));
                    break;
            }
            frame.Add(new MySprite(SpriteType.TEXT, simple, new Vector2(-190f, 21f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1 
            frame.Add(new MySprite(SpriteType.TEXT, advanced, (new Vector2(-77f, 21f) + englD) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text2
            frame.Add(new MySprite(SpriteType.TEXT, radar, new Vector2(100f, 21f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
        }
        static void DrawModule0(Color color, string Language, MySpriteDrawFrame frame, Vector2 centerPos, int component, string cocpitType, string weaponType, float step, float scale = 1f)
        {
            //"Настройки"
            string cockpit = Languages.Translate(Language, "COCKPIT");
            string weapon = Languages.Translate(Language, "WEAPON");
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(24f, 16f + (step * component)) * scale + centerPos, new Vector2(475f, 60f) * scale, _ChosenComponent, null, TextAlignment.CENTER, 0f)); // фон рамки
            frame.Add(new MySprite(SpriteType.TEXT, "Language", new Vector2(-180f, 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, Language, new Vector2(100f, 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, cockpit, new Vector2(-180f, 0f + step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, cocpitType, new Vector2(100f, -15f + step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, weapon, new Vector2(-180f, 0f + 2 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, weaponType, new Vector2(100f, 0f + 2 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
        }
        static void DrawModule1(Color color, string Language, MySpriteDrawFrame frame, Vector2 centerPos, int component, float obsUp, float obsBack, float weaponVel, float weaponRange, float step, float scale = 1f)
        {
            string projvel = Languages.Translate(Language, "PROJVEL"), shootrange = Languages.Translate(Language, "SHOTRANGE");
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(24f, 16f + (step * component)) * scale + centerPos, new Vector2(475f, 60f) * scale, _ChosenComponent, null, TextAlignment.CENTER, 0f)); // фон рамки
            frame.Add(new MySprite(SpriteType.TEXT, "Cockpit - Up", new Vector2(-180f, 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, $"{obsUp}", new Vector2(100f, 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, "Cockpit - Back", new Vector2(-180f, 0f + step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, $"{obsBack}", new Vector2(100f, 0f + step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, projvel, new Vector2(-180f, -15f + 2 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
            frame.Add(new MySprite(SpriteType.TEXT, $"{weaponVel}", new Vector2(100f, 0f + 2 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text2
            frame.Add(new MySprite(SpriteType.TEXT, shootrange, new Vector2(-180f, -15f + 3 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text1
            frame.Add(new MySprite(SpriteType.TEXT, $"{weaponRange}", new Vector2(100f, 0f + 3 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale)); // text2
        }
        static void DrawModule2(Color color, string Language, MySpriteDrawFrame frame, Vector2 centerPos, int component, int targetingPoint, float lockRange, bool enemyB, bool neutralB, bool allieB, float step, float scale = 1f)
        {
            string target = Languages.Translate(Language, "TARGETINGPOINT");
            string point = Languages.Translate(Language, "POINT" + targetingPoint);
            string range = Languages.Translate(Language, "INITIALRANGE");
            string enemy = Languages.Translate(Language, "ENEMY");
            string neutral = Languages.Translate(Language, "NEUTRAL");
            string allie = Languages.Translate(Language, "ALLIE");
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(24f, 16f + (step * component)) * scale + centerPos, new Vector2(475f, 60f) * scale, _ChosenComponent, null, TextAlignment.CENTER, 0f)); // фон рамки
            frame.Add(new MySprite(SpriteType.TEXT, target, new Vector2(-180f, -15f + 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, point, new Vector2(100f, -15f + 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, range, new Vector2(-180f, -15f + step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, $"{lockRange}m", new Vector2(100f, 0f + step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, enemy, new Vector2(-180f, 0f + 2 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, $"{enemyB}", new Vector2(100f, 0f + 2 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, neutral, new Vector2(-180f, 0f + 3 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, $"{neutralB}", new Vector2(100f, 0f + 3 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, allie, new Vector2(-180f, 0f + 4 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
            frame.Add(new MySprite(SpriteType.TEXT, $"{allieB}", new Vector2(100f, 0f + 4 * step) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
        }

        static void DrawLockedMode(Color color, string Language, MySpriteDrawFrame frame, Vector2 centerPos, bool autotarget, float scale = 1f)
        {
            string auto;
            string aim;
            if (autotarget)
            {
                aim = Languages.Translate(Language, "AIM");
                auto = Languages.Translate(Language, "AUTO");
                frame.Add(new MySprite(SpriteType.TEXT, aim, new Vector2(0f, 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
                frame.Add(new MySprite(SpriteType.TEXT, auto, new Vector2(-35f, 25f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
                frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(-11f, 15f) * scale + centerPos, new Vector2(7f, 7f) * scale, color, null, TextAlignment.CENTER, 0f));
            }
            else
            {
                aim = Languages.Translate(Language, "TRACKING");
                auto = Languages.Translate(Language, "ASSIST");
                frame.Add(new MySprite(SpriteType.TEXT, aim, new Vector2(0f, 0f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
                frame.Add(new MySprite(SpriteType.TEXT, auto, new Vector2(-5f, 25f) * scale + centerPos, null, color, "DEBUG", TextAlignment.LEFT, 1f * scale));
                frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(-11f, 15f) * scale + centerPos, new Vector2(7f, 7f) * scale, color, null, TextAlignment.CENTER, 0f));
            }
        }
        static void DrawXSight(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)

        {
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(20f, 20f) * scale + centerPos, new Vector2(1f, 8f) * scale, color, null, TextAlignment.CENTER, -0.7854f)); // sprite1
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(20f, -20f) * scale + centerPos, new Vector2(1f, 8f) * scale, color, null, TextAlignment.CENTER, 0.7854f)); // sprite6
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-20f, -20f) * scale + centerPos, new Vector2(1f, 8f) * scale, color, null, TextAlignment.CENTER, -0.7854f)); // sprite7
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-20f, 20f) * scale + centerPos, new Vector2(1f, 8f) * scale, color, null, TextAlignment.CENTER, 0.7854f)); // sprite8
        }

        static void DrawBox(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
        {
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(0f, 0f) * scale + centerPos,
                Size = new Vector2(20f, 20f) * scale,
                Color = color,
                RotationOrScale = 0f
            }); // sprite1
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(0f, 0f) * scale + centerPos,
                Size = new Vector2(18f, 18f) * scale,
                Color = _BLACK,
                RotationOrScale = 0f
            }); // sprite2
        }
        static void DrawPoint(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
        {
            frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(0f, 0f) * scale + centerPos, new Vector2(5f, 5f) * scale, color, null, TextAlignment.CENTER, 0f)); // sprite1
        }
        static void DrawPassiveTarget(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
        {
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(0f, 0f) * scale + centerPos,
                Size = new Vector2(20f, 20f) * scale,
                Color = color,
                RotationOrScale = 0f
            }); // sprite1
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(0f, 0f) * scale + centerPos,
                Size = new Vector2(18f, 18f) * scale,
                Color = _BLACK,
                RotationOrScale = 0f
            }); // sprite2
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(0f, 0f) * scale + centerPos,
                Size = new Vector2(12f, 20f) * scale,
                Color = new Color(0, 0, 0, 255),
                RotationOrScale = 0f
            }); // sprite2
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "SquareSimple",
                Position = new Vector2(0f, 0f) * scale + centerPos,
                Size = new Vector2(20f, 12f) * scale,
                Color = new Color(0, 0, 0, 255),
                RotationOrScale = 0f
            }); // sprite3
        }
        static void DrawCircle(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
        {
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
                Alignment = TextAlignment.CENTER,
                Data = "Circle",
                Position = new Vector2(0f, 0f) * scale + centerPos,
                Size = new Vector2(20f, 20f) * scale,
                Color = color,
                RotationOrScale = 0f
            }); // sprite1
            frame.Add(new MySprite()
            {
                Type = SpriteType.TEXTURE,
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
        public static void DrawSpriteX(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f, float rotation = 0f)
        {
            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, color, null, TextAlignment.CENTER, 0.7854f + rotation)); // sprite1
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) * scale + centerPos, new Vector2(10f, 2f) * scale, color, null, TextAlignment.CENTER, 0.7854f + rotation)); // sprite2
        }

        public static void DrawSpritesRectangle(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f)
        {
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(4f, 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, color, null, TextAlignment.CENTER, 0f)); // right
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(-4f, 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, color, null, TextAlignment.CENTER, 0f)); // left
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, -4f) * scale + centerPos, new Vector2(10f, 2f) * scale, color, null, TextAlignment.CENTER, 0f)); // down
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(0f, 4f) * scale + centerPos, new Vector2(10f, 2f) * scale, color, null, TextAlignment.CENTER, 0f)); // top
        }

        public static void DrawSpritesRectangleRotateAndScale(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float scale = 1f, float rotation = 0f)
        {
            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * 4f - sin * 0f, sin * 4f + cos * 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, color, null, TextAlignment.CENTER, 0f + rotation)); // right
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * -4f - sin * 0f, sin * -4f + cos * 0f) * scale + centerPos, new Vector2(2f, 10f) * scale, color, null, TextAlignment.CENTER, 0f + rotation)); // left
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * 0f - sin * -4f, sin * 0f + cos * -4f) * scale + centerPos, new Vector2(10f, 2f) * scale, color, null, TextAlignment.CENTER, 0f + rotation)); // down
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * 0f - sin * 4f, sin * 0f + cos * 4f) * scale + centerPos, new Vector2(10f, 2f) * scale, color, null, TextAlignment.CENTER, 0f + rotation)); // top
        }
        public static void DrawTurret(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float rotation = 0f)
        {
            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);
            frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) + centerPos, new Vector2(25f, 25f), color, null, TextAlignment.CENTER, 0f + rotation)); // sprite1
            frame.Add(new MySprite(SpriteType.TEXTURE, "Circle", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) + centerPos, new Vector2(23f, 23f), _BLACK, null, TextAlignment.CENTER, 0f + rotation)); // sprite1
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * 0f - sin * -39f, sin * 0f + cos * -39f) + centerPos, new Vector2(5f, 51f), _BLACK, null, TextAlignment.CENTER, 0f + rotation)); // sprite3
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * 0f - sin * -39f, sin * 0f + cos * -39f) + centerPos, new Vector2(3f, 49f), color, null, TextAlignment.CENTER, 0f + rotation)); // sprite2
        }
        public static void DrawHull(Color color, MySpriteDrawFrame frame, Vector2 centerPos, float rotation = 0f)
        {
            float sin = (float)Math.Sin(rotation);
            float cos = (float)Math.Cos(rotation);
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) + centerPos, new Vector2(40f, 60f), color, null, TextAlignment.CENTER, 0f + rotation)); // sprite4
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cos * 0f - sin * 0f, sin * 0f + cos * 0f) + centerPos, new Vector2(38f, 58f), _BLACK, null, TextAlignment.CENTER, 0f + rotation)); // sprite4
            frame.Add(new MySprite(SpriteType.TEXTURE, "AH_BoreSight", new Vector2(cos * 0f - sin * -40f, sin * 0f + cos * -40f) + centerPos, new Vector2(40f, 40f), color, null, TextAlignment.CENTER, - 1.5708f + rotation)); // sprite5
        }
    }
    public class DrawingInfo
    {
        public EnemyTargetedInfo Target;
        public Vector3D point;
        public MySpriteDrawFrame frame;
        public IMyTextPanel surface;
        public Vector3D obspos;
        public Color color;
        public DrawingInfo(Vector3D point, MySpriteDrawFrame frame, IMyTextPanel surface, Vector3D obspos, Color color)
        {
            this.point = point;
            this.frame = frame;
            this.surface = surface;
            this.obspos = obspos;
            this.color = color;
        }
        public DrawingInfo(EnemyTargetedInfo target, MySpriteDrawFrame frame, IMyTextPanel surface, Vector3D obspos, Color color)
        {
            this.Target = target;
            this.frame = frame;
            this.surface = surface;
            this.obspos = obspos;
            this.color = color;
        }
    }
    public class TankInfo
    {
        public float turretRotation;
        public bool drawTank;
        public float hullRotation;
        public TankInfo(float turretRotation, bool drawTank, float hullRotation)
        {
            this.turretRotation = turretRotation;
            this.drawTank = drawTank;
            this.hullRotation = hullRotation;
        }
    }

}
