using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
    partial class Program : MyGridProgram
    {
        const string _debugLCDTag = "Отладка";
        const int ReInitTime = 360;
        readonly Color _interfaceColor = new Color(179, 237, 255, 255);
        readonly Color _targetColor = new Color(0, 255, 0);
        readonly Color _ballisticColor = new Color(0, 150, 0);
        readonly Color _weaponColor = new Color(255, 0, 0);
        readonly Color _powerColor = new Color(255, 255, 0);
        readonly Color _propulsionColor = new Color(0, 0, 255);
        public static int _unlockTime = 180;
        const int timeToUpdateSettings = 5;

        string _updateInfo, _statusInfo, _debuginfo = "";
        static MyIni languageIni = new MyIni();
        bool b = languageIni.TryParse(Languages.storage);

        List<IMyTerminalBlock> _allBlocks = new List<IMyTerminalBlock>();
        List<IMyUserControllableGun> _allGuns = new List<IMyUserControllableGun>();
        List<IMyUserControllableGun> _myGuns = new List<IMyUserControllableGun>();
        List<IMySmallGatlingGun> _gatlings = new List<IMySmallGatlingGun>();
        List<IMySmallMissileLauncher> _mLaunchers = new List<IMySmallMissileLauncher>();
        List<IMyMotorStator> _allRotors = new List<IMyMotorStator>();
        List<IMyMotorStator> _rotorsE = new List<IMyMotorStator>();
        List<IMyCameraBlock> _myCameras = new List<IMyCameraBlock>();
        List<IMyCameraBlock> _radarCameras = new List<IMyCameraBlock>();
        List<IMyCameraBlock> _allCameras = new List<IMyCameraBlock>();
        List<IMyTextPanel> _textPanels = new List<IMyTextPanel>();
        List<IMyShipController> _shipControllers = new List<IMyShipController>();
        List<IMyGyro> _myGyro = new List<IMyGyro>();
        List<IMyLargeTurretBase> _turrets = new List<IMyLargeTurretBase>();
        List<IMyTurretControlBlock> _TCs = new List<IMyTurretControlBlock>();

        IMyBroadcastListener _missilesListener;

        IMyTextPanel _debugLCD;
        IMyBlockGroup _FCSGroup;
        IMyMotorStator _rotorA;
        IMyShipController _myShipController;
        IMyShipController _activeShipController;
        Turret _turret = new Turret();
        HullGuidance Hull = new HullGuidance();

        Radar _radar;
        TurretRadar _turretRadar = new TurretRadar();
        //Положения наблюдателя
        Dictionary<int, MyTuple<string, float, float>> _observerInSC = new Dictionary<int, MyTuple<string, float, float>>
        {
            {0, new MyTuple<string, float, float>("SCOCKPIT", 0.46f, 0.28f) },
            {1, new MyTuple<string, float, float>("FCOCKPIT", 0.46f, 0.28f) },
            {2, new MyTuple<string, float, float>("SCSEAT", 0.46f, 0.28f) },
            {3, new MyTuple<string, float, float>("LCOCKPIT", 0.5f, 0.12f) },
            {4, new MyTuple<string, float, float>("CSEAT", 0.5f, 0.12f) },
            {5, new MyTuple<string, float, float>("CUSTOM", 0.5f, 0.12f) },
        };
        //характеристики орудия
        Dictionary<int, MyTuple<string, float, float, float>> _weaponDict = new Dictionary<int, MyTuple<string, float, float, float>>
        {
            {0, new MyTuple<string, float, float, float>("GUTLING", 400, 800, 1/11.67f) },
            {1, new MyTuple<string, float, float, float>("AUTOCANON", 400, 800, 1/2.5f) },
            {2, new MyTuple<string, float, float, float>("ASSAULT", 500, 1400, 6) },
            {3, new MyTuple<string, float, float, float>("ARTY", 500, 2000, 12) },
            {4, new MyTuple<string, float, float, float>("SRAILGUN", 1000, 1400, 20) },
            {5, new MyTuple<string, float, float, float>("LRAILGUN", 2000, 2000, 60) },
            {6, new MyTuple<string, float, float, float>("CUSTOM", 0, 0, 0) },
        };
        //Сохранение данных
        readonly MyIni _myIni = new MyIni();
        const string INI_SECTION_NAMES = "Names", INI_LANGUAGE = "Language", INI_GROUP_NAME_TAG = "Group name tag", INI_AZ_ROTOR_NAME_TAG = "Azimuth Rotor name tag", INI_EL_ROTOR_NAME_TAG = "Elevation Rotor name tag", INI_MAIN_COCKPIT_NAME_TAG = "Main Cockpit name tag",
            INI_SECTION_RADAR = "Radar", INI_INITIAL_RANGE = "Initial Range",
            INI_SECTION_CONTROLS = "Controls", INI_EL_MULT = "Elevation Rotor Multiplier", INI_AZ_MULT = "Azimuth Rotor Multiplier", INI_YAW_MULT = "Yaw Gyro Multiplier", INI_PITCH_MULT = "Pitch Gyro Multiplier", INI_INTERACTIVE_MOD = "Interactive Mod",
            INI_SECTION_WEAPON = "Weapon", INI_NUMBER_OF_WEAPON = "Number of weapon", INI_WEAPON_SHOOT_VELOCITY = "Projectile velocity", INI_WEAPON_FIRE_RANGE = "Shot range", INI_WEAPON_RELOAD_TIME = "Reload Time",
            INI_SECTION_COCKPIT = "Cockpit", INI_NUMBER_OF_COCKPIT = "Number of cockpit", INI_COEF_UP = "Observer position - up", INI_COEF_BACK = "Observer position - back",
            INI_SECTION_TARGETS = "Targets", INI_ENEMY = "Enemy", INI_NEUTRAL = "Neutral", INI_ALLIE = "Allie", INI_DISPLAYED_TARGET = "Displayed Target",
            INI_SECTION_DEFAULTS = "Defaults", INI_AZIMUTH_ANGLE = "Azimuth default angle", INI_ELEVATION_ANGLE = "Elevation default angle";

        string _language = "English", _FCSTag = "Ash", _azimuthRotorTag = "Azimuth", _elevationRotorTag = "Elevation", _mainCockpitTag = "Main";

        float elevationSpeedMult = 0.001f, azimuthSpeedMult = 0.001f, yawMult = 0.001f, pitchMult = 0.001f,
            _myWeaponShotVelocity = 400, _myWeaponRangeToFire = 800, _myWeaponReloadTime = 1 / 2.5f,
            _obsCoefUp = 0, _obsCoefBack = 0,
            _initialRange = 2000,
            _azimuthDefaultAngle = 0, _elevationDefaultAngle = 0;

        bool isTurret = false, canAutoTarget = false, stabilization = true, settingsMode = false, autotarget = false, aimAssist = false, isVehicle = false, getTarget = false, drawTank = true, interactive = true, block = false, centering = false;
        long Tick = 0;
        bool allie = false, enemy = true, neutral = true;
        IMyMotorStator _mainElRotor;

        float horizont = 0, vertical = 0, xMove = 0, yMove = 0, menuMove = 0, Y_button;

        int menuTimer = 0, menuTab = 0, menuline = 0, myCockpit = 0, myWeapon = 0, _targetingPoint = 1;
        public Program()
        {

            _missilesListener = IGC.RegisterBroadcastListener(Me.EntityId.ToString());
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }
        #region Main
        public void Main(string argument, UpdateType updateSource)
        {
            if (menuTimer > 0)
                menuTimer--;
            _statusInfo = "\nSystem status:\n";
            if ((Tick % ReInitTime) == 0)
            {
                UpdateBlocks(ref _updateInfo);
            }
            //_debugLCD.WriteText("");

            //Update radar
            _activeShipController = null;
            switch (argument)
            {
                case "settings":
                    if (settingsMode)
                    {
                        menuTab = 0;
                        menuline = 0;
                        settingsMode = false;
                        SC();
                    }
                    else
                        settingsMode = true;
                    break;
                case "action":
                    Action();
                    break;
                case "switch_lock":
                    if (_radar.Searching)
                    {
                        _radar.DropLock();
                    }
                    else
                        _radar.Searching = true;
                    break;
                case "switch_aimAssist":
                    aimAssist = !aimAssist;
                    autotarget = false;
                    break;
                case "use_gatling":
                    myWeapon = 0;
                    SC();
                    break;
                case "use_autoCanon":
                    myWeapon = 1;
                    SC();
                    break;
                case "use_assaultCanon":
                    myWeapon = 2;
                    SC();
                    break;
                case "use_artillery":
                    myWeapon = 3;
                    SC();
                    break;
                case "use_smallRail":
                    myWeapon = 4;
                    SC();
                    break;
                case "use_largeRail":
                    myWeapon = 5;
                    SC();
                    break;
                case "switch_aiMode":
                    if (aimAssist && autotarget)
                    {
                        aimAssist = false;
                        autotarget = false;
                    }
                    else
                    {
                        if (!aimAssist)
                            aimAssist = true;
                        else
                            autotarget = !autotarget;
                    }
                    break;
                case "switch_stab":
                    stabilization = !stabilization;
                    break;
                case "block":
                    Block();
                    break;
                case "centering":
                    centering = !centering;
                    break;
                default:
                    break;
            }
            _radar.Update(ref _debuginfo, Tick, _unlockTime, _initialRange);
            _statusInfo += $"{Languages.Translate(_language, "SEARCHING")}: {_radar.Searching}\n";
            if (_radar.lockedtarget != null)
            {
                EnemyTargetedInfo newTarget;
                newTarget = _turretRadar.Update(ref _debuginfo, Tick, _radar.lockedtarget);
                _radar.UpdateTarget(newTarget);
                _statusInfo += $"{Languages.Translate(_language, "LOCKED")}: {_radar.lockedtarget.Type} \n";
            }
            else
            {
                _turretRadar.Update(Tick);
            }
            //Getting status for active cockpit
            if (_myShipController != null)
                _activeShipController = _myShipController;
            else
                foreach (var cockpit in _shipControllers)
                {
                    if (cockpit.IsUnderControl)
                    {
                        _activeShipController = cockpit;
                        break;
                    }
                }
            //information from cockpit
            if (_activeShipController != null)
            {
                horizont = _activeShipController.RotationIndicator.Y;
                vertical = -_activeShipController.RotationIndicator.X;
                xMove = _activeShipController.MoveIndicator.X;
                yMove = _activeShipController.MoveIndicator.Z;
                menuMove = _activeShipController.RollIndicator;
                Y_button = _activeShipController.MoveIndicator.Y;
            }

            //Menu command
            if ((xMove != 0 || yMove != 0 || menuMove != 0) && settingsMode)
            {
                    if (menuTimer == 0) //Можем ли считывать действия
                    {
                        //Раздел меню
                        if (menuMove != 0)
                        {
                            menuline = 0;
                            menuTab += (int)Math.Round(menuMove);
                            if (menuTab > 2)
                            {
                                menuTab = 0;
                            }
                            if (menuTab < 0)
                            {
                                menuTab = 2;
                            }
                        }
                        switch (menuTab)
                        {
                            //Для "Базовых"
                            case 0:
                                if (yMove != 0)
                                {
                                    menuline += (int)Math.Round(yMove);
                                    if (menuline > 2)
                                    {
                                        menuline = 0;
                                    }
                                    else if (menuline < 0)
                                    {
                                        menuline = 2;
                                    }
                                }
                                if (xMove != 0)
                                {
                                    switch (menuline)
                                    {
                                        case 0:
                                            if (_language == "Russian")
                                                _language = "English";
                                            else
                                                _language = "Russian";
                                            break;
                                        case 1:
                                            myCockpit += (int)Math.Round(xMove);
                                            if (myCockpit > (_observerInSC.Count() - 1))
                                                myCockpit = 0;
                                            else if (myCockpit < 0)
                                                myCockpit = _observerInSC.Count() - 1;
                                            break;
                                        case 2:
                                            myWeapon += (int)Math.Round(xMove);
                                            if (myWeapon > (_weaponDict.Count() - 1))
                                                myWeapon = 0;
                                            else if (myWeapon < 0)
                                                myWeapon = _weaponDict.Count() - 1;
                                            break;
                                    }
                                }
                                break;
                            case 1:     //Для "Расширенных"
                                if (yMove != 0)
                                {
                                    menuline += (int)Math.Round(yMove);
                                    if (menuline > 3)
                                    {
                                        menuline = 0;
                                    }
                                    else if (menuline < 0)
                                    {
                                        menuline = 3;
                                    }
                                }
                                if (xMove != 0)
                                {
                                    switch (menuline)
                                    {
                                        case 0:
                                            myCockpit = 5;
                                            _obsCoefUp += 0.01f * (float)Math.Round(xMove);
                                            break;
                                        case 1:
                                            myCockpit = 5;
                                            _obsCoefBack += 0.01f * (float)Math.Round(xMove);
                                            break;
                                        case 2:
                                            myWeapon = 6;
                                            _myWeaponShotVelocity += 50 * (float)Math.Round(xMove);
                                            break;
                                        case 3:
                                            myWeapon = 6;
                                            _myWeaponRangeToFire += 100 * (float)Math.Round(xMove);
                                            break;
                                    }
                                }
                                break;
                            case 2:     //"Радар"
                                if (yMove != 0)
                                {
                                    menuline += (int)Math.Round(yMove);
                                    if (menuline > 4)
                                    {
                                        menuline = 0;
                                    }
                                    else if (menuline < 0)
                                    {
                                        menuline = 4;
                                    }
                                }
                                if (xMove != 0)
                                {
                                    switch (menuline)
                                    {
                                        case 0:
                                            _targetingPoint += (int)Math.Round(xMove);
                                            if (_targetingPoint > 2)
                                                _targetingPoint = 0;
                                            else if (_targetingPoint < 0)
                                                _targetingPoint = 2;
                                            break;
                                        case 1:
                                            _initialRange += 500f * (float)Math.Round(xMove);
                                            if (_initialRange < 500)
                                                _initialRange = 500;
                                            break;
                                        case 2:
                                            enemy = !enemy;
                                            break;
                                        case 3:
                                            neutral = !neutral;
                                            break;
                                        case 4:
                                            allie = !allie;
                                            break;
                                    }
                                    _radar.SetTargets(allie, neutral, enemy);
                                }
                                break;
                        }
                        SC();
                    }
                menuTimer = timeToUpdateSettings;
            }
            else if (!settingsMode && (menuMove!= 0 || Y_button!= 0))
            {
                if (interactive && menuTimer == 0)
                {
                    if (menuMove > 0)
                    {
                        Action();
                    }
                    if (menuMove < 0)
                    {
                        Block();
                    }
                    if (Y_button < 0)
                    {
                        centering = !centering;
                    }
                }
                menuTimer = timeToUpdateSettings;
            }
            //Getting information about system from dictionary
            //Cockpit
            if (myCockpit != 5)
            {
                MyTuple<string, float, float> obsInfo;
                _observerInSC.TryGetValue(myCockpit, out obsInfo);
                _obsCoefUp = obsInfo.Item2;
                _obsCoefBack = obsInfo.Item3;
            }
            //Weapon
            if (menuTab == 0)
            {
                if (myWeapon != 6)
                {
                    MyTuple<string, float, float, float> weaponInfo;
                    _weaponDict.TryGetValue(myWeapon, out weaponInfo);
                    _myWeaponShotVelocity = weaponInfo.Item2;
                    _myWeaponRangeToFire = weaponInfo.Item3;
                    _myWeaponReloadTime = weaponInfo.Item4;
                }
            }

            //Calculating intercept vector
            Vector3D? obs = null;
            Vector3D? obsForward = null;
            Vector3D? Intersept = null;
            Vector3D? BallicticPoint = null;
            Vector3D? ShootDirection = null;
            Vector3D MyPos;
            Drawing.GetObserverPos(ref obs, ref obsForward, _obsCoefUp, _obsCoefBack, _activeShipController, _myCameras);

            if (isTurret)
            {
                ShootDirection = _turret.referenceBlock.WorldMatrix.Forward;
                MyPos = _turret.referenceBlock.GetPosition();
            }
            else if (isVehicle)
            {
                if (_activeShipController != null)
                    MyPos = _activeShipController.GetPosition();
                else MyPos = Me.GetPosition();
            }
            else
            {
                if (obs != null)
                    MyPos = obs.GetValueOrDefault();
                else
                    MyPos = Me.GetPosition();
            }
            if (getTarget)
                if (obs != null)
                {
                    GetClosedTarget(_turretRadar.GetTargets(), obs.GetValueOrDefault(), obsForward.GetValueOrDefault(), ref _radar, Tick);
                    getTarget = false;
                }
            if (ShootDirection == null)
            {
                if (_myShipController != null) { ShootDirection = _myShipController.WorldMatrix.Forward; }
                else if (_shipControllers.Count > 0)
                    ShootDirection = _shipControllers[0].WorldMatrix.Forward;
                else ShootDirection = Me.WorldMatrix.Forward;
            }
            if (_radar.lockedtarget != null)
            {
                if (_shipControllers.Count > 0)
                {
                    EnemyTargetedInfo Target = _radar.lockedtarget;
                    Vector3D MySpeed = _shipControllers[0].GetShipVelocities().LinearVelocity;
                    Vector3D gravity = _shipControllers[0].GetNaturalGravity();
                    Intersept = MyMath.FindInterceptGVector(MyPos, MySpeed, Target, gravity, _myWeaponShotVelocity, _targetingPoint, false);
                    Vector3D prSpeed = ShootDirection.GetValueOrDefault() * _myWeaponShotVelocity;
                    BallicticPoint = MyMath.FindBallisticPoint(MyPos, MySpeed, Target, gravity, prSpeed, _targetingPoint);
                }
            }
            else if (_radar.pointOfLock != null)
                if (_shipControllers.Count > 0)
                {
                    Vector3D MySpeed = _shipControllers[0].GetShipVelocities().LinearVelocity;
                    Vector3D gravity = _shipControllers[0].GetNaturalGravity();
                    Vector3D prSpeed = ShootDirection.GetValueOrDefault() * _myWeaponShotVelocity;
                    BallicticPoint = MyMath.FindBallisticPoint(MyPos, MySpeed, _radar.pointOfLock.GetValueOrDefault(), gravity, prSpeed);
                }
            //Drawing
            


            //Turret
            if (canAutoTarget)
            {
                if (isTurret)
                {
                    if (aimAssist)
                    {
                        if (Intersept != null)
                        {
                            if (autotarget)
                            {
                                _turret.Status(ref _statusInfo, _language, _azimuthRotorTag, _elevationRotorTag);
                                _turret.Update(Intersept.GetValueOrDefault(), true, 0, 0, false);
                            }
                            else
                            {
                                _turret.Status(ref _statusInfo, _language, _azimuthRotorTag, _elevationRotorTag);
                                _turret.Update(Intersept.GetValueOrDefault(), false, azimuthSpeedMult * horizont, elevationSpeedMult * vertical, false);
                            }
                            centering = false;
                        }
                        else
                        {
                            _turret.Status(ref _statusInfo, _language, _azimuthRotorTag, _elevationRotorTag);
                            _turret.Update(azimuthSpeedMult * horizont, elevationSpeedMult * vertical, ref centering, _azimuthDefaultAngle, _elevationDefaultAngle, stabilization);
                        };
                    }
                    else
                    {

                        _turret.Status(ref _statusInfo, _language, _azimuthRotorTag, _elevationRotorTag);
                        _turret.Update(azimuthSpeedMult * horizont, elevationSpeedMult * vertical, ref centering, _azimuthDefaultAngle, _elevationDefaultAngle, stabilization);
                    }
                }
                //Vehicle
                else if (isVehicle)
                {
                    if (aimAssist)
                    {
                        if (Intersept == null)
                        {
                            Hull.Drop(_myGyro);
                        }
                        else
                        {

                            if (_myShipController != null)
                            {
                                if (autotarget)
                                    Hull.Control(ref _debuginfo, _myShipController, Intersept.Value, _myShipController.RollIndicator, _myGyro, false);
                                else
                                {
                                    Hull.Control(ref _debuginfo, _myShipController, Intersept.Value, _myShipController.RollIndicator, _myGyro, false, false, yawMult, pitchMult);
                                }
                                //_debugLCD.WriteText(_debuginfo, true);
                            }
                            else
                            {
                                IMyShipController activeSC = null;
                                foreach (var sc in _shipControllers)
                                {
                                    if (sc.IsUnderControl)
                                    {
                                        activeSC = sc;
                                        break;
                                    }
                                }
                                if (activeSC != null)
                                {
                                    if (autotarget)
                                        Hull.Control(ref _debuginfo, activeSC, Intersept.Value, activeSC.RollIndicator, _myGyro, false);
                                    else
                                    {
                                        Hull.Control(ref _debuginfo, activeSC, Intersept.Value, activeSC.RollIndicator, _myGyro, false, false, yawMult, pitchMult);
                                    }
                                    //_debugLCD.WriteText(_debuginfo, true);
                                }
                                else
                                    Hull.Drop(_myGyro);
                            }
                        }
                    }
                    else
                        Hull.Drop(_myGyro);
                }
            }

            //Drawing
            if (obs != null)
            {
                foreach (var lcd in _textPanels)
                {
                    Drawing.SetupDrawSurface(lcd);
                    var frame = lcd.DrawFrame();
                    lcd.ContentType = ContentType.TEXT_AND_IMAGE;
                    lcd.ContentType = ContentType.SCRIPT;
                    DrawingInfo defaultI = new DrawingInfo(obsForward.GetValueOrDefault(), frame, lcd, obs.GetValueOrDefault(), _interfaceColor);
                    defaultI.Target = _radar.lockedtarget;
                    DrawingInfo DI = defaultI;
                    if (settingsMode)
                    {
                        switch (menuTab)
                        {
                            case 0:
                                Drawing.SettingsInterface(DI, ref _observerInSC, ref _weaponDict, menuline, myCockpit, myWeapon, _language, 1.0f, isTurret, isVehicle);
                                break;
                            case 1:
                                Drawing.SettingsInterface(DI, menuline, _obsCoefUp, _obsCoefBack, _myWeaponShotVelocity, _myWeaponRangeToFire, _language, 1.0f, isTurret, isVehicle);
                                break;
                            case 2:
                                Drawing.SettingsInterface(DI, menuline, _targetingPoint, _initialRange, enemy, neutral, allie, _language, 1.0f, isTurret, isVehicle);
                                break;
                        }

                    }
                    else
                    {
                        foreach (var target in _turretRadar.GetTargets())
                        {
                            DI.Target = target;
                            DI.color = _targetColor;
                            if (_radar.lockedtarget != null)
                            {
                                if (target.EntityId != _radar.lockedtarget.EntityId)
                                    Drawing.DrawTurretTarget(DI);
                            }
                            else
                                Drawing.DrawTurretTarget(DI);
                        }
                        DI = defaultI;
                        if (_radar.lockedtarget != null)
                        {
                            
                                DI.Target = _radar.lockedtarget;
                                Drawing.DrawTarget(ref _debuginfo, DI, _targetingPoint);
                                if (_radar.lockedtarget.TargetSubsystems.Count != 0)
                                    Drawing.DrawSubsystemType(ref _debuginfo, DI, _weaponColor, _propulsionColor, _powerColor);
                                foreach (var subsystem in _radar.lockedtarget.TargetSubsystems)
                                {
                                    Drawing.DrawSubsystem(ref _debuginfo, DI, subsystem, _weaponColor, _propulsionColor, _powerColor);
                                }
                        }
                        if (Intersept != null)
                        {
                            DI.point = Intersept.Value;
                            DI.color = _targetColor;
                            Drawing.DrawInterceptVector(DI);
                            DI = defaultI;
                        }
                        if (BallicticPoint != null)
                        {
                            DI.point = BallicticPoint.Value;
                            DI.color = _ballisticColor;
                            Drawing.DrawBallisticPoint(DI, _interfaceColor, Intersept == null);
                            DI = defaultI;
                        }
                        double distance = 0;
                        float losing = 1f;
                        bool searching = _radar.Searching;
                        if (_radar.lockedtarget != null)
                        {
                            distance = (obs - _radar.lockedtarget.HitPosition).GetValueOrDefault().Length();
                            losing = (float)(_unlockTime - _radar.counter) / _unlockTime;
                            searching = false;
                        }
                        DI.point = obsForward.GetValueOrDefault();
                        DI.color = _interfaceColor;
                        TankInfo tankInfo = new TankInfo(0, false, 0, block, centering);
                        if (isTurret)
                        {
                            if (drawTank)
                            {
                                tankInfo.turretRotation = (float)MyMath.CalculateRotorDeviationAngle(obsForward.Value, _turret.turretMatrix);
                                tankInfo.hullRotation = (float)MyMath.CalculateRotorDeviationAngle(obsForward.Value, _activeShipController.WorldMatrix);
                                tankInfo.drawTank = true;
                            }
                        }
                        Drawing.BattleInterface(DI, _language, searching, tankInfo, 1.0f, distance, losing, isTurret, isVehicle, autotarget, aimAssist);
                        //_debugLCD.WriteText(_debuginfo, true);
                        _debuginfo = "";
                    }
                    frame.Dispose();
                }

            }
            Echo($"Before next update {(ReInitTime - (Tick % ReInitTime)) / 60} seconds");
            Echo(_updateInfo + _statusInfo);
            Tick++;
        }
        #endregion
        void GetClosedTarget(List<EnemyTargetedInfo> targets, Vector3D mypos, Vector3D obsDir, ref Radar radar, long tick)
        {
            if (targets.Count > 0)
            {
                double minAngle = MathHelper.Pi;
                foreach (EnemyTargetedInfo target in targets)
                {
                    Vector3D dir = target.Position - mypos;
                    if (minAngle > MyMath.VectorAngleBetween(dir, obsDir))
                    {
                        minAngle = MyMath.VectorAngleBetween(dir, obsDir);
                        radar.GetTarget(target, tick);
                    }
                }
            }
        }
        #region update_blocks
        bool UpdateBlocks(ref string updateInfo)
        {
            LoadIniConfig();
            updateInfo = "";
            updateInfo += $"Language: {_language}\n";
            canAutoTarget = false;
            _allBlocks.Clear();
            //weapons
            _allGuns.Clear();
            _myGuns.Clear();
            _gatlings.Clear();
            _mLaunchers.Clear();
            //radar
            _myCameras.Clear();
            _textPanels.Clear();
            _radarCameras.Clear();
            _turrets.Clear();
            _TCs.Clear();
            //autoAIM
            _shipControllers.Clear();
            _allRotors.Clear();
            _rotorsE.Clear();
            _myShipController = null;
            _mainElRotor = null;
            _rotorA = null;
            _myGyro.Clear();

            _debugLCD = GridTerminalSystem.GetBlockWithName(_debugLCDTag) as IMyTextPanel;
            GridTerminalSystem.GetBlocksOfType(_shipControllers);
            GridTerminalSystem.GetBlocksOfType(_allRotors);
            GridTerminalSystem.GetBlocksOfType(_gatlings);
            GridTerminalSystem.GetBlocksOfType(_mLaunchers);
            foreach (var weapon in _gatlings)
            {
                _allGuns.Add(weapon as IMyUserControllableGun);
            }
            foreach (var weapon in _mLaunchers)
            {
                _allGuns.Add(weapon as IMyUserControllableGun);
            }

            isTurret = false;
            isVehicle = false;

            //Cameras on my grid

            GridTerminalSystem.GetBlocksOfType(_allCameras);
            foreach (var camera in _allCameras)
            {
                if (camera.IsSameConstructAs(Me))
                {
                    _myCameras.Add(camera);
                }

            }

            //blocks in group
            _FCSGroup = GridTerminalSystem.GetBlockGroupWithName(_FCSTag);
            if (_FCSGroup == null)
            {
                updateInfo += $"\n{languageIni.Get(_language, "GnF").ToString("Не найдена группа блоков!")}\n{languageIni.Get(_language, "NAME").ToString("Имя группы:")} \"{_FCSTag}\"\n";
            }
            else
            {
                _FCSGroup.GetBlocks(_allBlocks);
                foreach (var block in _allBlocks)
                {

                    if (SystemHelper.AddToListIfType(block, _textPanels))
                        continue;

                    if (block.IsSameConstructAs(Me))
                    {
                        if (SystemHelper.AddToListIfType(block, _TCs))
                            continue;
                        if (SystemHelper.AddToListIfType(block, _turrets))
                            continue;
                        if (SystemHelper.AddToListIfType(block, _radarCameras))
                            continue;
                    }
                    if (SystemHelper.AddToListIfType(block, _myGyro))
                        continue;
                }
            }
            if (_turrets.Count == 0 && _TCs.Count == 0)
            {
                GridTerminalSystem.GetBlocksOfType(_TCs);
                GridTerminalSystem.GetBlocksOfType(_turrets);
                _turretRadar.UpdateBlocks(_turrets, _TCs, false);
            }
            else
                _turretRadar.UpdateBlocks(_turrets, _TCs);
            if (_radar == null)
            {
                _radar = new Radar(_radarCameras);
                _radar.SetTargets(allie, neutral, enemy);
            }
            else
            {
                _radar.radarCameras = _radarCameras;
                _radar.countOfCameras = _radarCameras.Count;
                _radar.SetTargets(allie, neutral, enemy);
            }
            if (_radarCameras.Count < 2)
            {
                updateInfo += $"\n{Languages.Translate(_language, "CANTLOCK")}\n";
            }

            updateInfo += $"\n{Languages.Translate(_language, "LASTUPDATE")}\n" +
                $"{Languages.Translate(_language, "RADARCAMERAS")} " + _radarCameras.Count +
                $"\n{Languages.Translate(_language, "TEXTPANELS")} " + _textPanels.Count;

            //Initialize cocpit
            if (_shipControllers.Count == 1)
            {
                _myShipController = _shipControllers[0];
            }
            else
            {
                foreach (var block in _allBlocks)
                {
                    SystemHelper.AddBlockIfType(block, out _myShipController);
                }

            }
            if (_myShipController != null)
                updateInfo += $"\n{Languages.Translate(_language, "MAINCOCKPIT")} {_myShipController.CustomName}\n";
            //Try to create autorgetnig system
            //First, try to create turret from FCS group, like classic scenario (in MART for example)
            isTurret = false;
            bool added = false;
            foreach (var block in _allBlocks)
            {
                if (block.CustomName.Contains(_azimuthRotorTag))
                    if (SystemHelper.AddBlockIfType(block, out _rotorA))
                        continue;
                if (block.CustomName.Contains(_elevationRotorTag))
                    if (SystemHelper.AddToListIfType(block, _rotorsE))
                        continue;
            }
            if (_rotorA != null && _rotorsE.Count != 0)
            {
                foreach (var rotor in _rotorsE)
                {
                    foreach (var gun in _allGuns)
                    {
                        if (rotor.TopGrid == gun.CubeGrid)
                        {
                            if (rotor.CustomName.Contains(_mainCockpitTag))
                                _mainElRotor = rotor;
                            if (_mainElRotor == null)  //Нужен именно тот ротор, на котором есть пушки, в качестве главного
                                _mainElRotor = rotor;
                            if (!_myGuns.Contains(gun))
                                _myGuns.Add(gun);
                        }

                    }
                }
                if (_myGuns.Count == 0)
                {
                    updateInfo += $"{Languages.Translate(_language, "TURRETGROUPBLOCKS")}\n" +
                        $"{Languages.Translate(_language, "FAIL")}\n" +
                    $"{Languages.Translate(_language, "NOGUNS")} \"{_elevationRotorTag}\"\n";
                    _rotorsE.Clear();
                    _rotorA = null;
                    _myGuns.Clear();
                }
                else
                {
                    _turret.UpdateBlocks(_rotorA, _rotorsE, _mainElRotor, _myGuns, _radarCameras, _myGyro);
                    updateInfo += $"{Languages.Translate(_language, "TURRETGROUPBLOCKS")}\n" +
                        $"{Languages.Translate(_language, "SUCCESS")}\n";
                    isTurret = true;
                    canAutoTarget = true;
                }
            }
            else
            {

                updateInfo += $"{Languages.Translate(_language, "TURRETGROUPBLOCKS")}\n" +
                    $"{Languages.Translate(_language, "FAIL")}\n" +
                    $"{Languages.Translate(_language, "NOROTORS")}\n";
            }
            //Auto set Turret?
            if (!isTurret)
            {
                foreach (var rotor in _allRotors)
                {
                    added = false;
                    if (rotor.TopGrid == Me.CubeGrid)
                    {
                        _rotorA = rotor;
                        continue;
                    }
                    else if (rotor.CubeGrid == Me.CubeGrid)
                    {
                        //может на роторе есть пушка
                        foreach (var gun in _allGuns)
                        {
                            if (rotor.TopGrid == gun.CubeGrid)
                            {
                                if (rotor.CustomName.Contains(_mainCockpitTag))
                                {
                                    _rotorsE.Add(rotor);
                                    _mainElRotor = rotor;
                                    added = true;
                                }
                                if (_mainElRotor == null && !added)
                                {
                                    _rotorsE.Add(rotor);
                                    _mainElRotor = rotor;
                                    added = true;
                                }
                                else if (!added)
                                {
                                    added = true;
                                    _rotorsE.Add(rotor);
                                }
                                if (!_myGuns.Contains(gun))
                                    _myGuns.Add(gun);
                            }
                        }
                        //может на роторе есть камеры из радара
                        if (!added)
                        {
                            foreach (var camera in _radarCameras)
                            {
                                if (rotor.TopGrid == camera.CubeGrid)
                                {
                                    if (_mainElRotor == null && !added)
                                    {
                                        _rotorsE.Add(rotor);
                                        _mainElRotor = rotor;
                                        added = true;
                                        break;
                                    }
                                    else if (!added)
                                    {
                                        added = true;
                                        _rotorsE.Add(rotor);
                                        break;
                                    }
                                }

                            }
                        }

                    }
                }
                if (_rotorA != null && _mainElRotor != null)
                {
                    _turret.UpdateBlocks(_rotorA, _rotorsE, _mainElRotor, _myGuns, _radarCameras, _myGyro);
                    updateInfo += $"{Languages.Translate(_language, "AUTOTURRETSUCCESS")}\n";
                    isTurret = true;//so we are in turret
                    canAutoTarget = true;
                }
                else
                    updateInfo += $"{Languages.Translate(_language, "AUTOTURRETFAIL")}\n";
            }
            //if not turret, may be veacle?
            if (!isTurret)
            {
                if (_myGyro.Count == 0)
                    GridTerminalSystem.GetBlocksOfType(_myGyro);
                if (_shipControllers.Count == 0)
                {

                    updateInfo += $"{Languages.Translate(_language, "AUTOHULLFAIL")}: " +
                        $"{Languages.Translate(_language, "NOCOCKPITS")}\n";
                }
                else
                {
                    if (_myGyro.Count == 0)
                        updateInfo += $"{Languages.Translate(_language, "AUTOHULLFAIL")}: " +
                        $"{Languages.Translate(_language, "NOGYRO")}\n";
                    else
                    {
                        canAutoTarget = true;
                        isVehicle = true;
                        updateInfo += $"{Languages.Translate(_language, "AUTOHULLSUCCESS")}: \n" +
                        $"{Languages.Translate(_language, "GYROS")}: {_myGyro.Count}\n";
                    }
                }
            }
            return true;
        }
        void Action()
        {
            if (_radar.lockedtarget == null && _turretRadar.GetTargets().Count != 0)
            {
                getTarget = true;
                aimAssist = true;
            }
            else
            {
                _radar.DropLock();
                aimAssist = false;
            }
        }
        void Block()
        {
            if (!isTurret)
                return;
            block = !block;
            _turret.Block(block);
        }
        void LoadIniConfig()
        {
            _myIni.Clear();
            bool parsed = _myIni.TryParse(Me.CustomData);
            if (!parsed)
            {
                SC();
                return;
            }
            _FCSTag = _myIni.Get(INI_SECTION_NAMES, INI_GROUP_NAME_TAG).ToString(_FCSTag);
            _language = _myIni.Get(INI_SECTION_NAMES, INI_LANGUAGE).ToString(_language);
            _azimuthRotorTag = _myIni.Get(INI_SECTION_NAMES, INI_AZ_ROTOR_NAME_TAG).ToString(_azimuthRotorTag);
            _elevationRotorTag = _myIni.Get(INI_SECTION_NAMES, INI_EL_ROTOR_NAME_TAG).ToString(_elevationRotorTag);
            _mainCockpitTag = _myIni.Get(INI_SECTION_NAMES, INI_MAIN_COCKPIT_NAME_TAG).ToString(_mainCockpitTag);
            _initialRange = (float)_myIni.Get(INI_SECTION_RADAR, INI_INITIAL_RANGE).ToDouble(_initialRange);
            elevationSpeedMult = (float)_myIni.Get(INI_SECTION_CONTROLS, INI_EL_MULT).ToDouble(elevationSpeedMult);
            azimuthSpeedMult = (float)_myIni.Get(INI_SECTION_CONTROLS, INI_AZ_MULT).ToDouble(azimuthSpeedMult);
            yawMult = (float)_myIni.Get(INI_SECTION_CONTROLS, INI_YAW_MULT).ToDouble(yawMult);
            pitchMult = (float)_myIni.Get(INI_SECTION_CONTROLS, INI_PITCH_MULT).ToDouble(pitchMult);
            interactive = _myIni.Get(INI_SECTION_CONTROLS, INI_INTERACTIVE_MOD).ToBoolean(interactive);
            myWeapon = _myIni.Get(INI_SECTION_WEAPON, INI_NUMBER_OF_WEAPON).ToInt32(myWeapon);
            _myWeaponRangeToFire = (float)_myIni.Get(INI_SECTION_WEAPON, INI_WEAPON_FIRE_RANGE).ToDouble(_myWeaponRangeToFire);
            _myWeaponShotVelocity = (float)_myIni.Get(INI_SECTION_WEAPON, INI_WEAPON_SHOOT_VELOCITY).ToDouble(_myWeaponShotVelocity);
            _myWeaponReloadTime = (float)_myIni.Get(INI_SECTION_WEAPON, INI_WEAPON_RELOAD_TIME).ToDouble(_myWeaponShotVelocity);
            myCockpit = _myIni.Get(INI_SECTION_COCKPIT, INI_NUMBER_OF_COCKPIT).ToInt32(myCockpit);
            _obsCoefUp = (float)_myIni.Get(INI_SECTION_COCKPIT, INI_COEF_UP).ToDouble(_myWeaponShotVelocity);
            _obsCoefBack = (float)_myIni.Get(INI_SECTION_COCKPIT, INI_COEF_BACK).ToDouble(_myWeaponShotVelocity);
            _azimuthDefaultAngle = (float)_myIni.Get(INI_SECTION_DEFAULTS, INI_AZIMUTH_ANGLE).ToDouble(_azimuthDefaultAngle);
            _elevationDefaultAngle = (float)_myIni.Get(INI_SECTION_DEFAULTS, INI_ELEVATION_ANGLE).ToDouble(_elevationDefaultAngle);
            allie = _myIni.Get(INI_SECTION_TARGETS, INI_ALLIE).ToBoolean(allie);
            neutral = _myIni.Get(INI_SECTION_TARGETS, INI_NEUTRAL).ToBoolean(neutral);
            enemy = _myIni.Get(INI_SECTION_TARGETS, INI_ENEMY).ToBoolean(enemy);
            _targetingPoint = _myIni.Get(INI_SECTION_TARGETS, INI_DISPLAYED_TARGET).ToInt32(_targetingPoint);
            SC();
        }

        void SC()
        {
            _myIni.Clear();
            _myIni.Set(INI_SECTION_NAMES, INI_GROUP_NAME_TAG, _FCSTag);
            _myIni.Set(INI_SECTION_NAMES, INI_LANGUAGE, _language);
            _myIni.Set(INI_SECTION_NAMES, INI_AZ_ROTOR_NAME_TAG, _azimuthRotorTag);
            _myIni.Set(INI_SECTION_NAMES, INI_EL_ROTOR_NAME_TAG, _elevationRotorTag);
            _myIni.Set(INI_SECTION_NAMES, INI_MAIN_COCKPIT_NAME_TAG, _mainCockpitTag);
            _myIni.Set(INI_SECTION_RADAR, INI_INITIAL_RANGE, _initialRange);
            _myIni.Set(INI_SECTION_CONTROLS, INI_EL_MULT, elevationSpeedMult);
            _myIni.Set(INI_SECTION_CONTROLS, INI_AZ_MULT, azimuthSpeedMult);
            _myIni.Set(INI_SECTION_CONTROLS, INI_YAW_MULT, yawMult);
            _myIni.Set(INI_SECTION_CONTROLS, INI_PITCH_MULT, pitchMult);
            _myIni.Set(INI_SECTION_CONTROLS, INI_INTERACTIVE_MOD, interactive);
            _myIni.Set(INI_SECTION_WEAPON, INI_NUMBER_OF_WEAPON, myWeapon);
            _myIni.Set(INI_SECTION_WEAPON, INI_WEAPON_FIRE_RANGE, _myWeaponRangeToFire);
            _myIni.Set(INI_SECTION_WEAPON, INI_WEAPON_SHOOT_VELOCITY, _myWeaponShotVelocity);
            _myIni.Set(INI_SECTION_WEAPON, INI_WEAPON_RELOAD_TIME, _myWeaponReloadTime);
            _myIni.Set(INI_SECTION_COCKPIT, INI_NUMBER_OF_COCKPIT, myCockpit);
            _myIni.Set(INI_SECTION_COCKPIT, INI_COEF_UP, _obsCoefUp);
            _myIni.Set(INI_SECTION_COCKPIT, INI_COEF_BACK, _obsCoefBack);
            _myIni.Set(INI_SECTION_DEFAULTS, INI_AZIMUTH_ANGLE, _azimuthDefaultAngle);
            _myIni.Set(INI_SECTION_DEFAULTS, INI_ELEVATION_ANGLE, _elevationDefaultAngle);
            _myIni.Set(INI_SECTION_TARGETS, INI_ALLIE, allie);
            _myIni.Set(INI_SECTION_TARGETS, INI_NEUTRAL, neutral);
            _myIni.Set(INI_SECTION_TARGETS, INI_ENEMY, enemy);
            _myIni.Set(INI_SECTION_TARGETS, INI_DISPLAYED_TARGET, _targetingPoint);
            Me.CustomData = _myIni.ToString();
        }
        #endregion

    }
}
