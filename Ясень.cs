using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
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
using VRage.Game.Utils;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string _debugLCDTag = "Отладка";
        const string _IGCAsh_to_AxisTag = "XJI_ML_Axis";
        const string _IGCAshTag = "XJI_FCS_Ash";
        const int ReInitTime = 360;
        //Color _interfaceColor = new Color(179, 237, 255, 255);
        Color _interfaceColor = new Color(0, 100, 0, 255);
        Color _targetColor = new Color(0, 255, 0);
        Color _ballisticColor = new Color(0, 150, 0);
        Color _weaponColor = new Color(255, 0, 0);
        Color _powerColor = new Color(255, 255, 0);
        Color _propulsionColor = new Color(0, 0, 255);
        public static int _unlockTime = 180;
        const int timeToUpdateButtons = 5;

        string _updateInfo, _statusInfo, _debuginfo = "";
        static MyIni languageIni = new MyIni();

        List<IMyTerminalBlock> _allBlocks = new List<IMyTerminalBlock>();
        List<IMyUserControllableGun> _allGuns = new List<IMyUserControllableGun>();
        List<IMyUserControllableGun> _myGuns = new List<IMyUserControllableGun>();
        List<IMySmallGatlingGun> _gatlings = new List<IMySmallGatlingGun>();
        List<IMySmallMissileLauncher> _mLaunchers = new List<IMySmallMissileLauncher>();
        List<IMyMotorStator> _allRotors = new List<IMyMotorStator>();
        List<IMyMotorStator> _rotorsE = new List<IMyMotorStator>();
        List<IMyCameraBlock> _radarCameras = new List<IMyCameraBlock>();    //камеры для рейкаста
        List<IMyCameraBlock> _allActiveCameras = new List<IMyCameraBlock>(); //все камеры в группе
        List<IMyCameraBlock> _myCameras = new List<IMyCameraBlock>();       //камеры на моем гриде
        List<IMyCameraBlock> _allCameras = new List<IMyCameraBlock>();      //все камеры вообще
        List<IMyTextPanel> _textPanels = new List<IMyTextPanel>();
        List<IMyShipController> _shipControllers = new List<IMyShipController>();
        List<IMyGyro> _myGyro = new List<IMyGyro>();
        List<IMyLargeTurretBase> _turrets = new List<IMyLargeTurretBase>();
        List<IMyTurretControlBlock> _TCs = new List<IMyTurretControlBlock>();

        IMyTextPanel _debugLCD;
        IMyBlockGroup _FCSGroup;
        IMyMotorStator _rotorA;
        IMyShipController _myShipController;
        IMyShipController _activeShipController;
        IMyTerminalBlock _referenceBlock;
        //Подсистемы
        Turret _turret = new Turret();
        HullGuidance Hull = new HullGuidance();
        Radar _radar;
        TurretRadar _turretRadar = new TurretRadar();
        BroadcastModule _communicator;
        //Положения наблюдателя
        public static Dictionary<string, CockpitDef> CockpitDefinitions = new Dictionary<string, CockpitDef>()
        {
            {"Cockpit/SmallBlockCockpit", new CockpitDef() {Up = 0.46f, Back = 0.28f} },
            {"Cockpit/SmallBlockCapCockpit", new CockpitDef() {Up = 0.46f, Back = 0.1f} },
            {"Cockpit/OpenCockpitSmall", new CockpitDef() {Up = 0.5f, Back = 0.19f} },
            {"Cockpit/SmallBlockStandingCockpit", new CockpitDef() {Up = 0.707f, Back = -0.2f } },
            {"Cockpit/LargeBlockCockpitSeat", new CockpitDef() {Up = 0.5f, Back = 0.19f} },
        };
        //характеристики орудия
        public static Dictionary<string, WeaponDef> WeaponDefinitions = new Dictionary<string, WeaponDef>()
        {
            {"SmallGatlingGun/", new WeaponDef() {type = "Gatling", Range = 800, StartSpeed = 400, ReloadTime = 1/11.67f} },
            {"SmallGatlingGun/SmallGatlingGunWarfare2", new WeaponDef() {type = "Gatling", Range = 800, StartSpeed = 400, ReloadTime = 1/11.67f} },
            {"SmallGatlingGun/SmallBlockAutocannon", new WeaponDef() {type = "Autocanon", Range = 800, StartSpeed = 400, ReloadTime = 1/2.5f} },
            {"SmallMissileLauncherReload/SmallBlockMediumCalibreGun", new WeaponDef() {type = "Assault canon", Range = 1400, StartSpeed = 500, ReloadTime = 6} },
            {"SmallMissileLauncherReload/SmallRailgun", new WeaponDef() {type = "Small Railgun", Range = 1400, StartSpeed = 1000, ReloadTime = 20} },
            {"SmallMissileLauncher/LargeBlockLargeCalibreGun", new WeaponDef() {type = "Artillery", Range = 2000, StartSpeed = 500, ReloadTime = 12} },
            {"SmallMissileLauncherReload/LargeRailgun", new WeaponDef() {type = "Large Railgun", Range = 2000, StartSpeed = 2000, ReloadTime = 60} },
        };
        CockpitDef _cockpitInfo = new CockpitDef();
        WeaponDef _weaponInfo = new WeaponDef();
        string _myIniWeapon = "", _customWeapon = "false";
        //Сохранение данных
        readonly MyIni _myIni = new MyIni();
        const string INI_SECTION_NAMES = "Names", INI_LANGUAGE = "Language", INI_GROUP_NAME_TAG = "Group name tag", INI_AZ_ROTOR_NAME_TAG = "Azimuth Rotor name tag", INI_EL_ROTOR_NAME_TAG = "Elevation Rotor name tag", INI_MAIN_COCKPIT_NAME_TAG = "Name tag \"Main\"", INI_SIGHT_NAME_TAG = "Sight name tag",
            INI_SECTION_DISPLAY = "Display", INI_UNLIMITED_FPS = "FPS Limit", INI_SHOW_WEAPON_INFO = "Show Weapon Info", INI_SHOW_TANK = "Show Tank", INI_COLOR_INTERFACE = "Interface c", INI_COLOR_TARGET = "Target c", INI_COLOR_BALLISTC = "Ballistic point c", INI_COLOR_TARGET_WEAPON = "Target weapons c", INI_COLOR_TARGET_POWER = "Target power c", INI_COLOR_TARGET_PROPULSION = "Target propulsions c",
            INI_SECTION_RADAR = "Radar", INI_INITIAL_RANGE = "Initial Range",
            INI_SECTION_CONTROLS = "Controls", INI_EL_MULT = "Elevation Rotor Multiplier", INI_AZ_MULT = "Azimuth Rotor Multiplier", INI_YAW_MULT = "Yaw Gyro Multiplier", INI_PITCH_MULT = "Pitch Gyro Multiplier", INI_INTERACTIVE_MOD = "Interactive Mode",
            INI_SECTION_WEAPON = "Weapon", INI_MY_WEAPON = "My weapon", INI_CUSTOM_SETTINGS = "Custom settings", INI_WEAPON_SHOOT_VELOCITY = "Projectile velocity", INI_WEAPON_FIRE_RANGE = "Shot range", INI_WEAPON_RELOAD_TIME = "Reload Time",
            INI_SECTION_COCKPIT = "Cockpit", INI_COEF_UP = "Observer position - up", INI_COEF_BACK = "Observer position - back",
            INI_SECTION_TARGETS = "Targets", INI_ENEMY = "Enemy", INI_NEUTRAL = "Neutral", INI_ALLIE = "Allie", INI_DISPLAYED_TARGET = "Displayed Target",
            INI_SECTION_DEFAULTS = "Defaults", INI_AZIMUTH_ANGLE = "Azimuth default angle", INI_ELEVATION_ANGLE = "Elevation default angle";

        string _language = "English", _FCSTag = "Ash", _azimuthRotorTag = "Azimuth", _elevationRotorTag = "Elevation", _mainTag = "Main", _sightNameTag = "SIGHT";

        float elevationSpeedMult = 0.001f, azimuthSpeedMult = 0.001f, yawMult = 0.001f, pitchMult = 0.001f,
            _myWeaponShotVelocity = 400, _myWeaponRangeToFire = 800, _myWeaponReloadTime = 1 / 2.5f,
            _defObsCoefUp = 0.46f, _defObsCoefBack = 0.28f,
            _initialRange = 2000,
            _azimuthDefaultAngle = 0, _elevationDefaultAngle = 0;
        bool isTurret = false, canAutoTarget = false, stabilization = true, autotarget = false, aimAssist = false, isVehicle = false, getTarget = false, _showWeaponInfo = true, drawTank = true, interactive = false, block = false, centering = false, _fpsLimit = false;
        long Tick = 0;
        bool allie = false, enemy = true, neutral = true;
        IMyMotorStator _mainElRotor;

        float horizont = 0, vertical = 0, menuMove = 0, Y_button;

        int menuTimer = 0, _targetingPoint = 0;
        public Program()
        {
            _communicator = new BroadcastModule(IGC, _IGCAsh_to_AxisTag, _IGCAshTag);
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
            //_debugLCD.WriteText(SystemHelper.GetKey(_debugLCD));
            //Update radar
            _activeShipController = null;
            switch (argument)
            {
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
            CommandHandler(argument);
            _radar.Update(ref _debuginfo, Tick, _unlockTime, _initialRange);
            _statusInfo += $"Radar - searching: {_radar.Searching}\n";
            if (_radar.lockedtarget != null)
            {
                EnemyTargetedInfo newTarget;
                newTarget = _turretRadar.Update(ref _debuginfo, Tick, _radar.lockedtarget);
                _radar.UpdateTarget(newTarget);
                _statusInfo += $"Target locked: {_radar.lockedtarget.Type} \n";
            }
            else
            {
                _turretRadar.Update(Tick);
            }
            //Broadcasting
            _communicator.GetMessageFromAxis(_IGCAsh_to_AxisTag, _radar.lockedtarget, _referenceBlock);
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
                /*
				xMove = _activeShipController.MoveIndicator.X;
				yMove = _activeShipController.MoveIndicator.Z;*/
                menuMove = _activeShipController.RollIndicator;
                Y_button = _activeShipController.MoveIndicator.Y;
                _cockpitInfo = GetCockpitInfo(_activeShipController);
            }
            //Menu command
            if (menuMove != 0 || Y_button != 0)
            {
                if (interactive && menuTimer == 0 && isTurret)
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
                menuTimer = timeToUpdateButtons;
            }
            //Getting information about system from dictionary
            //Weapon

            //Calculating intercept vector
            Vector3D? obs = null;
            Vector3D? obsForward = null;
            Vector3D? Intersept = null;
            Vector3D? BallicticPoint = null;
            Vector3D? ShootDirection = null;
            Vector3D MyPos;
            Drawing.GetObserverPos(ref obs, ref obsForward, _cockpitInfo.Up, _cockpitInfo.Back, _activeShipController, _myCameras);

            if (isTurret)
            {
                ShootDirection = _turret.referenceBlock.WorldMatrix.Forward;
                MyPos = _turret.referenceBlock.GetPosition();
            }
            else if (isVehicle)
            {
                if (_referenceBlock != null)
                    MyPos = _referenceBlock.GetPosition();
                else if (_activeShipController != null)
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
                    Intersept = MyMath.FindInterceptGVector(MyPos, MySpeed, Target, gravity, _weaponInfo.StartSpeed, _targetingPoint, false);
                    Vector3D prSpeed = ShootDirection.GetValueOrDefault() * _weaponInfo.StartSpeed;
                    BallicticPoint = MyMath.FindBallisticPoint(MyPos, MySpeed, Target, gravity, prSpeed, _targetingPoint);
                }
            }
            else if (_radar.pointOfLock != null)
                if (_shipControllers.Count > 0)
                {
                    Vector3D MySpeed = _shipControllers[0].GetShipVelocities().LinearVelocity;
                    Vector3D gravity = _shipControllers[0].GetNaturalGravity();
                    Vector3D prSpeed = ShootDirection.GetValueOrDefault() * _weaponInfo.StartSpeed;
                    BallicticPoint = MyMath.FindBallisticPoint(MyPos, MySpeed, _radar.pointOfLock.GetValueOrDefault(), gravity, prSpeed);
                }
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
            if (obs != null && (!_fpsLimit | (Tick % 10) == 0))
            {
                foreach (var lcd in _textPanels)
                {
                    if (!_fpsLimit)
                        lcd.ContentType = ContentType.TEXT_AND_IMAGE;
                    Drawing.SetupDrawSurface(lcd);
                    var frame = lcd.DrawFrame();
                    DrawingInfo DI = new DrawingInfo(obsForward.GetValueOrDefault(), frame, lcd, obs.GetValueOrDefault(), _targetColor)
                    {
                        Target = _radar.lockedtarget
                    };
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
                    DI.c = _interfaceColor;
                    TankInfo tankInfo = new TankInfo(0, false, 0, block, centering);
                    DWI dWI = new DWI() { weaponDef = _weaponInfo, draw = _showWeaponInfo, name = _referenceBlock.CustomName };
                    if (isTurret)
                    {
                        if (drawTank)
                        {
                            tankInfo.turretRotation = (float)MyMath.CalculateRotorDeviationAngle(obsForward.Value, _turret.turretMatrix);
                            tankInfo.hullRotation = (float)MyMath.CalculateRotorDeviationAngle(obsForward.Value, _activeShipController.WorldMatrix);
                            tankInfo.drawTank = true;
                        }
                    }
                    Drawing.BattleInterface(DI, _language, searching, tankInfo, dWI, 1.0f, distance, losing, isTurret, isVehicle, autotarget, aimAssist);
                    foreach (var target in _turretRadar.GetTargets())
                    {
                        DI.Target = target;
                        DI.c = _targetColor;
                        if (_radar.lockedtarget != null)
                        {
                            if (target.EntityId != _radar.lockedtarget.EntityId)
                                Drawing.DrawTurretTarget(DI);
                        }
                        else
                            Drawing.DrawTurretTarget(DI);
                    }
                    _debugLCD.WriteText("4");
                    if (_radar.lockedtarget != null)
                    {

                        DI.Target = _radar.lockedtarget;
                        DI.c = _targetColor;
                        Drawing.DrawTarget(ref _debuginfo, DI, _targetingPoint);
                        foreach (var subsystem in _radar.lockedtarget.TargetSubsystems)
                        {
                            Drawing.DrawSubsystem(ref _debuginfo, DI, subsystem, _weaponColor, _propulsionColor, _powerColor);
                        }
                    }
                    if (Intersept != null)
                    {
                        DI.point = Intersept.Value;
                        if (!((DI.point - MyPos).Length() > _weaponInfo.Range))
                        {
                            DI.c = _targetColor;
                            Drawing.DrawInterceptVector(DI);
                        }
                    }
                    if (BallicticPoint != null)
                    {
                        DI.point = BallicticPoint.Value;
                        if (!((DI.point - MyPos).Length() > _weaponInfo.Range))
                        {
                            DI.c = _ballisticColor;
                            Drawing.DrawBallisticPoint(DI, _interfaceColor, Intersept == null);
                        }
                    }
                    //_debugLCD.WriteText(_debuginfo, true);
                    _debuginfo = "";
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
            _debugLCD = GridTerminalSystem.GetBlockWithName(_debugLCDTag) as IMyTextPanel;

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
            _allActiveCameras.Clear();
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
            //FindReferenceBlock
            if (_referenceBlock == null)
            {
                FindReferenceBlock();
            }
            if (_referenceBlock == null)
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
                        if (SystemHelper.AddToListIfType(block, _allActiveCameras))
                            continue;
                    }
                    if (SystemHelper.AddToListIfType(block, _myGyro))
                        continue;
                }
            }
            foreach (var camera in _allActiveCameras)
                if (!camera.CustomName.Contains(_sightNameTag))
                    _radarCameras.Add(camera);
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
                updateInfo += $"\nNot enought cameras in the radar\n";
            }

            updateInfo += $"\nLast update:\n" +
                $"Cameras in radar - " + _radarCameras.Count +
                $"\nText panels - " + _textPanels.Count + "\n";

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
                updateInfo += $"\nMain cockpit - \"{_myShipController.CustomName}\"\n";
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
                            if (rotor.CustomName.Contains(_mainTag))
                                _mainElRotor = rotor;
                            if (_mainElRotor == null)  //Нужен именно тот ротор, на котором есть пушки, в качестве главного
                                _mainElRotor = rotor;
                            if (!_myGuns.Contains(gun))
                                _myGuns.Add(gun);
                        }

                    }
                    foreach (var camera in _allActiveCameras)
                    {
                        if (rotor.TopGrid == camera.CubeGrid)
                        {
                            if (rotor.CustomName.Contains(_mainTag))
                                _mainElRotor = rotor;
                            if (_mainElRotor == null)
                                _mainElRotor = rotor;
                        }
                    }
                }
                if (_myGuns.Count == 0)
                {
                    updateInfo += $"\nTrying to create a turret from blocks in a group...\n" +
                        $"Failure\n" +
                    $"Not found weapons on rotors \"{_elevationRotorTag}\"\n";
                    _rotorsE.Clear();
                    _rotorA = null;
                    _myGuns.Clear();
                }
                else
                {
                    _turret.UpdateBlocks(_rotorA, _rotorsE, _mainElRotor, _myGuns, _allActiveCameras, _myGyro);
                    updateInfo += $"Trying to create a turret from blocks in a group...\n" +
                        $"Success\n";
                    isTurret = true;
                    canAutoTarget = true;
                }
            }
            else
            {

                updateInfo += $"Trying to create a turret from blocks in a group..." +
                    $"Failure\n" +
                    $"Not enought rotors in the group\n";
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
                                if (rotor.CustomName.Contains(_mainTag))
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
                            foreach (var camera in _allActiveCameras)
                            {
                                if (rotor.TopGrid == camera.CubeGrid)
                                {
                                    if (rotor.CustomName.Contains(_mainTag))
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
                    _turret.UpdateBlocks(_rotorA, _rotorsE, _mainElRotor, _myGuns, _allActiveCameras, _myGyro);
                    updateInfo += $"Successful auto-transition to turret mode, all components found\n";
                    isTurret = true;//so we are in turret
                    canAutoTarget = true;
                }
                else
                    updateInfo += $"Auto-transition to turret mode failed\n";
            }
            //if not turret, may be veacle?
            if (!isTurret)
            {
                if (_myGyro.Count == 0)
                    GridTerminalSystem.GetBlocksOfType(_myGyro);
                if (_shipControllers.Count == 0)
                {

                    updateInfo += $"Transition to hull-guided mode failed:\n" +
                        $"No cockpits\n";
                }
                else
                {
                    if (_myGyro.Count == 0)
                        updateInfo += $"Transition to hull-guided mode failed:\n" +
                        $"No gyros\n";
                    else
                    {
                        canAutoTarget = true;
                        isVehicle = true;
                        updateInfo += $"Transition to hull-guided mode failed:\n" +
                        $"Gyroscopes: {_myGyro.Count}\n";
                    }
                }
            }

            UpdateWeaponInfo();
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
        void FindReferenceBlock()
        {
            if (!String.IsNullOrEmpty(_myIniWeapon))
            {
                if (TryGetWeaponFromName(_myIniWeapon))
                    return;
            }
            _debugLCD.WriteText($"{_allActiveCameras.Count}");
            if (isTurret)
            {
                _referenceBlock = _turret.referenceBlock;
            }
            if (_allGuns.Count != 0)
                foreach (var weapon in _allGuns)
                {
                    if (_referenceBlock == null)
                        _referenceBlock = weapon;
                    if (weapon.CustomName.Contains(_mainTag))
                    {
                        _referenceBlock = weapon;
                        return;
                    }
                }
            if (_allActiveCameras.Count != 0 && _referenceBlock == null)
                foreach (var camera in _allActiveCameras)
                {
                    if (_referenceBlock == null)
                        _referenceBlock = camera;
                    if (camera.CustomName.Contains(_mainTag))
                    {
                        _referenceBlock = camera;
                        return;
                    }
                }
        }
        void UpdateWeaponInfo()
        {
            FindWeaponInfo();
            SC();
        }
        void FindWeaponInfo()
        {
            if (_customWeapon != "false" && _customWeapon != "true")
            {
                if (WeaponDefinitions.TryGetValue(_customWeapon, out _weaponInfo))
                {
                    return;
                }
            }
            if (_customWeapon == "false")    //autho configuration
            {
                if (_referenceBlock != null)
                {
                    if (_referenceBlock as IMyUserControllableGun != null)
                    {
                        _weaponInfo = GetWeaponInfo(_referenceBlock);
                        return;
                    }
                }
                if (isTurret)
                {
                    if (_turret.referenceBlock as IMyUserControllableGun != null)
                    {
                        _weaponInfo = GetWeaponInfo(_turret.referenceBlock);
                        return;
                    }
                }
                if (_allGuns.Count != 0)
                    _weaponInfo = GetWeaponInfo(_allGuns[0]);
                else
                {
                    _weaponInfo = new WeaponDef() { type = "CUSTOM", Range = _initialRange, StartSpeed = _myWeaponShotVelocity, ReloadTime = _myWeaponReloadTime };
                }
                return;
            }
            else if (_customWeapon == "true")
            {
                _weaponInfo = new WeaponDef() { type = "CUSTOM", Range = _initialRange, StartSpeed = _myWeaponShotVelocity, ReloadTime = _myWeaponReloadTime };
            }/*
			else
			{
				if (TryGetWeaponFromName(_myIniWeapon))
					return;
				if (WeaponDefinitions.TryGetValue(_myIniWeapon, out _weaponInfo))
				{
					return;
				}
				else
					_weaponInfo = new WeaponDef() { type = "CUSTOM", Range = _initialRange, StartSpeed = _myWeaponShotVelocity, ReloadTime = _myWeaponReloadTime };
			}*/
        }
        bool TryGetWeaponFromName(string name)
        {
            foreach (var weapon in _allGuns)
                if (weapon.CustomName == name)
                {
                    if (isTurret)
                    {
                        if (!_turret.TrySetRef(weapon))
                            continue;
                    }
                    _referenceBlock = weapon;
                    _weaponInfo = GetWeaponInfo(weapon);
                    return true;
                }
            foreach (var camera in _allActiveCameras)
                if (camera.CustomName == name)
                {
                    if (isTurret)
                    {
                        if (!_turret.TrySetRef(camera))
                            continue;
                    }
                    _referenceBlock = camera;
                    _weaponInfo = GetWeaponInfo(camera); //вернет дефолт значение
                    return true;
                }
            return false;
        }
        CockpitDef GetCockpitInfo(IMyTerminalBlock block)
        {
            CockpitDef cockpitDef;
            if (!(block is IMyCockpit)) return null;
            string key = SystemHelper.GetKey(block);
            if (CockpitDefinitions.TryGetValue(key, out cockpitDef))
                return cockpitDef;
            else return new CockpitDef() { Up = _defObsCoefUp, Back = _defObsCoefBack };
        }
        WeaponDef GetWeaponInfo(IMyTerminalBlock block)
        {
            WeaponDef weaponInfo;
            string key = SystemHelper.GetKey(block);
            if (WeaponDefinitions.TryGetValue(key, out weaponInfo))
                return weaponInfo;
            else if (WeaponDefinitions.TryGetValue(_customWeapon, out weaponInfo))
                return weaponInfo;
            else
                return new WeaponDef() { type = "CUSTOM", Range = _initialRange, StartSpeed = _myWeaponShotVelocity, ReloadTime = _myWeaponReloadTime };
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
            _mainTag = _myIni.Get(INI_SECTION_NAMES, INI_MAIN_COCKPIT_NAME_TAG).ToString(_mainTag);
            _sightNameTag = _myIni.Get(INI_SECTION_NAMES, INI_SIGHT_NAME_TAG).ToString(_sightNameTag);
            _fpsLimit = _myIni.Get(INI_SECTION_DISPLAY, INI_UNLIMITED_FPS).ToBoolean(_fpsLimit);
            drawTank = _myIni.Get(INI_SECTION_DISPLAY, INI_SHOW_TANK).ToBoolean(drawTank);
            _showWeaponInfo = _myIni.Get(INI_SECTION_DISPLAY, INI_SHOW_WEAPON_INFO).ToBoolean(_showWeaponInfo);
            IniToColor(_myIni.Get(INI_SECTION_DISPLAY, INI_COLOR_INTERFACE), ref _interfaceColor);
            IniToColor(_myIni.Get(INI_SECTION_DISPLAY, INI_COLOR_TARGET), ref _targetColor);
            IniToColor(_myIni.Get(INI_SECTION_DISPLAY, INI_COLOR_BALLISTC), ref _ballisticColor);
            IniToColor(_myIni.Get(INI_SECTION_DISPLAY, INI_COLOR_TARGET_WEAPON), ref _weaponColor);
            IniToColor(_myIni.Get(INI_SECTION_DISPLAY, INI_COLOR_TARGET_POWER), ref _powerColor);
            IniToColor(_myIni.Get(INI_SECTION_DISPLAY, INI_COLOR_TARGET_PROPULSION), ref _propulsionColor);
            _initialRange = (float)_myIni.Get(INI_SECTION_RADAR, INI_INITIAL_RANGE).ToDouble(_initialRange);
            elevationSpeedMult = (float)_myIni.Get(INI_SECTION_CONTROLS, INI_EL_MULT).ToDouble(elevationSpeedMult);
            azimuthSpeedMult = (float)_myIni.Get(INI_SECTION_CONTROLS, INI_AZ_MULT).ToDouble(azimuthSpeedMult);
            yawMult = (float)_myIni.Get(INI_SECTION_CONTROLS, INI_YAW_MULT).ToDouble(yawMult);
            pitchMult = (float)_myIni.Get(INI_SECTION_CONTROLS, INI_PITCH_MULT).ToDouble(pitchMult);
            interactive = _myIni.Get(INI_SECTION_CONTROLS, INI_INTERACTIVE_MOD).ToBoolean(interactive);
            _myWeaponRangeToFire = (float)_myIni.Get(INI_SECTION_WEAPON, INI_WEAPON_FIRE_RANGE).ToDouble(_myWeaponRangeToFire);
            _myIniWeapon = _myIni.Get(INI_SECTION_WEAPON, INI_MY_WEAPON).ToString(_myIniWeapon);
            _customWeapon = _myIni.Get(INI_SECTION_WEAPON, INI_CUSTOM_SETTINGS).ToString(_customWeapon);
            _myWeaponShotVelocity = (float)_myIni.Get(INI_SECTION_WEAPON, INI_WEAPON_SHOOT_VELOCITY).ToDouble(_myWeaponShotVelocity);
            _myWeaponReloadTime = (float)_myIni.Get(INI_SECTION_WEAPON, INI_WEAPON_RELOAD_TIME).ToDouble(_myWeaponReloadTime);
            _defObsCoefUp = (float)_myIni.Get(INI_SECTION_COCKPIT, INI_COEF_UP).ToDouble(_defObsCoefUp);
            _defObsCoefBack = (float)_myIni.Get(INI_SECTION_COCKPIT, INI_COEF_BACK).ToDouble(_defObsCoefBack);
            _azimuthDefaultAngle = (float)_myIni.Get(INI_SECTION_DEFAULTS, INI_AZIMUTH_ANGLE).ToDouble(_azimuthDefaultAngle);
            _elevationDefaultAngle = (float)_myIni.Get(INI_SECTION_DEFAULTS, INI_ELEVATION_ANGLE).ToDouble(_elevationDefaultAngle);
            allie = _myIni.Get(INI_SECTION_TARGETS, INI_ALLIE).ToBoolean(allie);
            neutral = _myIni.Get(INI_SECTION_TARGETS, INI_NEUTRAL).ToBoolean(neutral);
            enemy = _myIni.Get(INI_SECTION_TARGETS, INI_ENEMY).ToBoolean(enemy);
            _targetingPoint = _myIni.Get(INI_SECTION_TARGETS, INI_DISPLAYED_TARGET).ToInt32(_targetingPoint);
        }

        void SC()
        {
            _myIni.Clear();
            _myIni.Set(INI_SECTION_NAMES, INI_GROUP_NAME_TAG, _FCSTag);
            _myIni.Set(INI_SECTION_NAMES, INI_LANGUAGE, _language);
            _myIni.Set(INI_SECTION_NAMES, INI_AZ_ROTOR_NAME_TAG, _azimuthRotorTag);
            _myIni.Set(INI_SECTION_NAMES, INI_EL_ROTOR_NAME_TAG, _elevationRotorTag);
            _myIni.Set(INI_SECTION_NAMES, INI_MAIN_COCKPIT_NAME_TAG, _mainTag);
            _myIni.Set(INI_SECTION_NAMES, INI_SIGHT_NAME_TAG, _sightNameTag);
            _myIni.Set(INI_SECTION_DISPLAY, INI_UNLIMITED_FPS, _fpsLimit);
            _myIni.Set(INI_SECTION_DISPLAY, INI_SHOW_WEAPON_INFO, _showWeaponInfo);
            _myIni.Set(INI_SECTION_DISPLAY, INI_SHOW_TANK, drawTank);
            _myIni.Set(INI_SECTION_DISPLAY, INI_COLOR_INTERFACE, ColorToString(_interfaceColor));
            _myIni.Set(INI_SECTION_DISPLAY, INI_COLOR_TARGET, ColorToString(_targetColor));
            _myIni.Set(INI_SECTION_DISPLAY, INI_COLOR_BALLISTC, ColorToString(_ballisticColor));
            _myIni.Set(INI_SECTION_DISPLAY, INI_COLOR_TARGET_WEAPON, ColorToString(_weaponColor));
            _myIni.Set(INI_SECTION_DISPLAY, INI_COLOR_TARGET_POWER, ColorToString(_powerColor));
            _myIni.Set(INI_SECTION_DISPLAY, INI_COLOR_TARGET_PROPULSION, ColorToString(_propulsionColor));
            _myIni.Set(INI_SECTION_RADAR, INI_INITIAL_RANGE, _initialRange);
            _myIni.Set(INI_SECTION_CONTROLS, INI_EL_MULT, elevationSpeedMult);
            _myIni.Set(INI_SECTION_CONTROLS, INI_AZ_MULT, azimuthSpeedMult);
            _myIni.Set(INI_SECTION_CONTROLS, INI_YAW_MULT, yawMult);
            _myIni.Set(INI_SECTION_CONTROLS, INI_PITCH_MULT, pitchMult);
            _myIni.Set(INI_SECTION_CONTROLS, INI_INTERACTIVE_MOD, interactive);
            _myIni.Set(INI_SECTION_WEAPON, INI_MY_WEAPON, _myIniWeapon);
            _myIni.Set(INI_SECTION_WEAPON, INI_CUSTOM_SETTINGS, _customWeapon);
            _myIni.Set(INI_SECTION_WEAPON, INI_WEAPON_FIRE_RANGE, _myWeaponRangeToFire);
            _myIni.Set(INI_SECTION_WEAPON, INI_WEAPON_SHOOT_VELOCITY, _myWeaponShotVelocity);
            _myIni.Set(INI_SECTION_WEAPON, INI_WEAPON_RELOAD_TIME, _myWeaponReloadTime);
            _myIni.Set(INI_SECTION_COCKPIT, INI_COEF_UP, _defObsCoefUp);
            _myIni.Set(INI_SECTION_COCKPIT, INI_COEF_BACK, _defObsCoefBack);
            _myIni.Set(INI_SECTION_DEFAULTS, INI_AZIMUTH_ANGLE, _azimuthDefaultAngle);
            _myIni.Set(INI_SECTION_DEFAULTS, INI_ELEVATION_ANGLE, _elevationDefaultAngle);
            _myIni.Set(INI_SECTION_TARGETS, INI_ALLIE, allie);
            _myIni.Set(INI_SECTION_TARGETS, INI_NEUTRAL, neutral);
            _myIni.Set(INI_SECTION_TARGETS, INI_ENEMY, enemy);
            _myIni.Set(INI_SECTION_TARGETS, INI_DISPLAYED_TARGET, _targetingPoint);
            Me.CustomData = _myIni.ToString();
        }
        void CommandHandler(string command)
        {
            if (String.IsNullOrEmpty(command))
                return;
            List<string> commandSplit = SystemHelper.SplitString(command);
            if (commandSplit[0] == "use")
            {
                for (int i = 1; i < commandSplit.Count(); i++)
                {
                    switch (commandSplit[i])
                    {
                        case "-p":
                            string p = commandSplit.ElementAtOrDefault(i + 1);
                            if (!String.IsNullOrEmpty(p))
                            {
                                DefaultPresets(p);
                            }
                            break;
                        case "-n":
                            string n = commandSplit.ElementAtOrDefault(i + 1);
                            if (!String.IsNullOrEmpty(n))
                            {
                                if (TryGetWeaponFromName(n))
                                {
                                    _myIniWeapon = n;
                                    SC();
                                }
                            }
                            break;
                        case "-v":
                            string v = commandSplit.ElementAtOrDefault(i + 1);
                            if (!String.IsNullOrEmpty(v))
                            {
                                float vel;
                                if (float.TryParse(v, out vel))
                                {
                                    _customWeapon = "true";
                                    _myWeaponShotVelocity = vel;
                                    FindWeaponInfo();
                                    SC();
                                }
                            }
                            break;
                        case "-d":
                            _myIniWeapon = "";
                            _customWeapon = "false";
                            _referenceBlock = null;
                            FindReferenceBlock();
                            FindWeaponInfo();
                            SC();
                            break;
                        default: break;
                    }
                }
            }
        }
        void DefaultPresets(string p)
        {
            switch (p)
            {
                case "custom":
                    _customWeapon = "true";
                    FindWeaponInfo();
                    SC();
                    break;
                case "gatling":
                    _customWeapon = "SmallGatlingGun/";
                    FindWeaponInfo();
                    SC();
                    break;
                case "autoCanon":
                    _customWeapon = "SmallGatlingGun/SmallBlockAutocannon";
                    FindWeaponInfo();
                    SC();
                    break;
                case "assaultCanon":
                    _customWeapon = "SmallMissileLauncherReload/SmallBlockMediumCalibreGun";
                    FindWeaponInfo();
                    SC();
                    break;
                case "artillery":
                    _customWeapon = "SmallMissileLauncher/LargeBlockLargeCalibreGun";
                    FindWeaponInfo();
                    SC();
                    break;
                case "smallRail":
                    _customWeapon = "SmallMissileLauncherReload/SmallRailgun";
                    FindWeaponInfo();
                    SC();
                    break;
                case "largeRail":
                    _customWeapon = "SmallMissileLauncherReload/LargeRailgun";
                    FindWeaponInfo();
                    SC();
                    break;
                case "defaul":
                    _customWeapon = "false";
                    UpdateWeaponInfo();
                    SC();
                    break;
                default: break;
            }
        }
        void SetWeaponParam(string key)
        {
            if (WeaponDefinitions.TryGetValue(key, out _weaponInfo))
            {
                _initialRange = (float)_weaponInfo.Range;
                _myWeaponShotVelocity = (float)_weaponInfo.StartSpeed;
                _myWeaponReloadTime = (float)_weaponInfo.ReloadTime;
            }
        }
        bool IniToColor(MyIniValue val, ref Color c)
        {
            string rgbString = val.ToString("");
            string[] rgbSplit = rgbString.Split(','); int r = 0, g = 0, b = 0, a = 0;
            if (rgbSplit.Length != 4 || !int.TryParse(rgbSplit[0].Trim(), out r) || !int.TryParse(rgbSplit[1].Trim(), out g) || !int.TryParse(rgbSplit[2].Trim(), out b))
            {
                return false;
            }
            bool hasAlpha = int.TryParse(rgbSplit[3].Trim(), out a);
            if (!hasAlpha)
            {
                a = 255;
            }
            r = MathHelper.Clamp(r, 0, 255);
            g = MathHelper.Clamp(g, 0, 255);
            b = MathHelper.Clamp(b, 0, 255);
            a = MathHelper.Clamp(a, 0, 255);
            c = new Color(r, g, b, a);
            return true;
        }
        string ColorToString(Color Value)
        {
            return string.Format("{0}, {1}, {2}, {3}", Value.R, Value.G, Value.B, Value.A);
        }
        #endregion

    }
    public class WeaponDef
    {
        public double Range, StartSpeed;
        public double ReloadTime;
        public string type;
    }

    public class CockpitDef
    {
        public float Up, Back;
    }
}
