using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Eventing.Reader;
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
using VRageRender.Messages;

namespace IngameScript
{
	public class Turret
	{
		const double TURNGYROCONST = 0.0035;
		const float SOFTNAVCONST = 0.8f;
		IMyMotorStator rotorA;
		List<IMyMotorStator> rotorsE;
		IMyMotorStator MainElRotor;
		List<IMyUserControllableGun> weapons;
		List<IMyCameraBlock> radarCameras;
		List<IMyGyro> _turretGyros = new List<IMyGyro>();
		List<IMyGyro> _weaponGyros = new List<IMyGyro>();
		Vector3D turretFrontVec;
		Vector3D lastInterceptVector;
		float MultiplierElevation;
		public IMyTerminalBlock referenceBlock = null;
		public MatrixD turretMatrix { get; private set; }
		MatrixD weaponMatrix;
		MatrixD lastWeaponMatrix;
		MatrixD lastTurretMatrix;
		MatrixD lastRotorAMatrix;
		MatrixD lastRotorEMatrix;
		static MyIni languageIni = new MyIni();
		bool firstRunAim = true;
		double yawDeltaInput = 0;
		double yawInputSpeed = 0;
		double pitchDeltaInput = 0;
		double pitchInputSpeed = 0;
		double? lastSpeedYaw = null, lastSpeedPitch = null;
		double? maneuvrabilityYaw = null;
		double? maneuvrabilityPitch = null;
		bool _firstUpdate = true;
		bool fullDriveYaw = false, fullDrivePitch = false;
		bool block = false;

		public Turret()
		{
			_turretGyros = new List<IMyGyro>();
			radarCameras = new List<IMyCameraBlock>();
			rotorsE = new List<IMyMotorStator>();
			weapons = new List<IMyUserControllableGun>();
		}
		public bool UpdateBlocks(IMyMotorStator newRotorA, List<IMyMotorStator> newRotorsE, IMyMotorStator mainElRotor, List<IMyUserControllableGun> newWeapons, List<IMyCameraBlock> cameras, List<IMyGyro> gyros)
		{
			radarCameras = cameras;
			rotorA = newRotorA;
			MainElRotor = mainElRotor;
			rotorsE = newRotorsE;
			rotorsE.Remove(mainElRotor);
			weapons = newWeapons;
			if (!CheckRefBlock())
				return false;
			_turretGyros.Clear();
			_weaponGyros.Clear();
			foreach (var g in gyros)
			{
				if (g.CubeGrid == rotorA.TopGrid)
				{
					_turretGyros.Add(g);
				}
				if (g.CubeGrid == referenceBlock.CubeGrid)
					_weaponGyros.Add(g);
			}
			turretFrontVec = referenceBlock.WorldMatrix.Forward;
			if (_firstUpdate)
			{
				_firstUpdate = false;
				lastRotorAMatrix = newRotorA.WorldMatrix;
				lastRotorEMatrix = MainElRotor.WorldMatrix;
				lastWeaponMatrix = MyMath.CreateLookAtForwardDir(referenceBlock.GetPosition(), turretFrontVec, rotorA.WorldMatrix.Up);
				lastTurretMatrix = MyMath.CreateLookAtUpDir(rotorA.Top.WorldMatrix.Translation, turretFrontVec, rotorA.WorldMatrix.Up);
			}
			MultiplierElevation = 1;
			float deltaAzimuthCos = (float)rotorA.Top.WorldMatrix.Right.Dot(MainElRotor.WorldMatrix.Up);
			Vector3D absUpVec = rotorA.WorldMatrix.Up;
			Vector3D turretSideVec = MainElRotor.WorldMatrix.Up;
			Vector3D turretFrontCrossSide = turretFrontVec.Cross(turretSideVec);
			if (turretFrontCrossSide.Dot(absUpVec) < 0)  //Выясняется что ротор по взгляду пушки левый, если да то
			{
				deltaAzimuthCos += MathHelper.Pi;
				MultiplierElevation = -1;
			}

			if (deltaAzimuthCos > 1)
				deltaAzimuthCos = 1;
			if (deltaAzimuthCos < -1)
				deltaAzimuthCos = -1;
			//deltaAzimuth = (float)Math.Acos(deltaAzimuthCos);
			return true;
		}
		public void Update(Vector3D interceptVector, bool autoAim = true, float az = 0, float el = 0, bool dir = true)
		{
			if (!dir)
				interceptVector -= referenceBlock.WorldMatrix.Translation;
			//Матрицы башни и пушки
			turretFrontVec = referenceBlock.WorldMatrix.Forward;
			weaponMatrix = MyMath.CreateLookAtForwardDir(referenceBlock.GetPosition(), turretFrontVec, rotorA.WorldMatrix.Up);
			turretMatrix = MyMath.CreateLookAtUpDir(referenceBlock.GetPosition(), turretFrontVec, rotorA.WorldMatrix.Up);
			if (block)
				return;
			//Расчет стабилизационного момента от движения по местности
			float azError = (float)MyMath.CalculateRotorDeviationAngle(rotorA.WorldMatrix.Forward, lastRotorAMatrix);
			float elError = (float)MyMath.CalculateRotorDeviationAngle(MainElRotor.WorldMatrix.Forward, lastRotorEMatrix);
			//Расчет собственной скорости башни и пушки
			double ownYaw, ownPitch;
			MyMath.CalculateYawVelocity(turretMatrix, lastTurretMatrix, out ownYaw);
			MyMath.CalculatePitchVelocity(weaponMatrix, lastWeaponMatrix, out ownPitch);
			//Ввод данных от пользователя
			double yawRotationInput = az;
			double pitchRotationInput = el;
			if (autoAim)
			{
				yawRotationInput = 0;
				pitchRotationInput = 0;
				yawDeltaInput = 0;
				pitchDeltaInput = 0;
			}
			else
			{

				if (maneuvrabilityYaw != null && Math.Abs(yawInputSpeed - yawRotationInput) > maneuvrabilityYaw)
				{
					if (yawInputSpeed - yawRotationInput > 0)
						yawInputSpeed -= maneuvrabilityYaw.Value;
					else
						yawInputSpeed += maneuvrabilityYaw.Value;
				}
				else
				{
					yawInputSpeed = yawRotationInput;
				}
				if (maneuvrabilityPitch != null && Math.Abs(pitchInputSpeed - pitchRotationInput) > maneuvrabilityPitch)
				{
					if (pitchInputSpeed - pitchRotationInput > 0)
						pitchInputSpeed -= maneuvrabilityPitch.Value;
					else
						pitchInputSpeed += maneuvrabilityPitch.Value;
				}
				else { pitchInputSpeed = pitchRotationInput; }
				yawDeltaInput += yawInputSpeed;
				pitchDeltaInput += pitchInputSpeed;
			}
			//Расчет желаемого угла наведения
			Vector3D targetVecLocTurret = Vector3D.TransformNormal(interceptVector, MatrixD.Transpose(turretMatrix));
			double wantedYaw = Math.Atan2(targetVecLocTurret.X, -targetVecLocTurret.Z);
			Vector3D targetVecLocWeapon = Vector3D.TransformNormal(interceptVector, MatrixD.Transpose(weaponMatrix));
			double xyLenght = new Vector2D(targetVecLocWeapon.X, targetVecLocWeapon.Z).Length();
			double wantedPitch = Math.Atan2(targetVecLocWeapon.Y, xyLenght);
			if (firstRunAim)
			{
				firstRunAim = false;
				lastInterceptVector = interceptVector;
			}
			//Расчет стабилизационного момента относительно движения цели
			double targetAngularVelYaw, targetAngularVelPitch;
			Vector3D lastTargetVecLocTurret = Vector3D.TransformNormal(lastInterceptVector, MatrixD.Transpose(turretMatrix));
			double lastTargetDirYaw = Math.Atan2(lastTargetVecLocTurret.X, -lastTargetVecLocTurret.Z);
			Vector3D lastTargetVecLocWeapon = Vector3D.TransformNormal(lastInterceptVector, MatrixD.Transpose(weaponMatrix));
			xyLenght = new Vector2D(lastTargetVecLocWeapon.X, lastTargetVecLocWeapon.Z).Length();
			double lastTargetDirPitch = Math.Atan2(lastTargetVecLocWeapon.Y, xyLenght);
			targetAngularVelYaw = wantedYaw - lastTargetDirYaw;
			targetAngularVelPitch = wantedPitch - lastTargetDirPitch;
			//Расчет ввода данных от пользователя
			//расчет маневренности, или передача команд управления
			double yawRotorSpeed, pitchRotorSpeed, yawGyroSpeed, pitchGyroSpeed;
			//YAW
			if (fullDriveYaw)
			{
				fullDriveYaw = false;
				maneuvrabilityYaw = Math.Abs(lastSpeedYaw.GetValueOrDefault() - ownYaw);
			}
			if (maneuvrabilityYaw == null)
			{
				if (wantedYaw > 0)
				{
					yawRotorSpeed = MathHelper.Pi / 60;
					yawGyroSpeed = ownYaw + TURNGYROCONST;
				}
				else
				{
					yawRotorSpeed = -MathHelper.Pi / 60;
					yawGyroSpeed = ownYaw - TURNGYROCONST;
				}
				fullDriveYaw = true;
			}
			else
			{
				//Расчет оптимальной угловой скорости для наведения
				double timeToStopYaw = Math.Abs((ownYaw - targetAngularVelYaw - yawRotationInput - azError) / maneuvrabilityYaw.Value);
				double avaibleDistanceYaw = wantedYaw + yawDeltaInput - ownYaw + (targetAngularVelYaw + yawRotationInput + azError) * timeToStopYaw;
				double optimalAngularVelYaw = SOFTNAVCONST * Math.Sqrt(Math.Abs(2 * maneuvrabilityYaw.Value * avaibleDistanceYaw));
				if (wantedYaw + yawDeltaInput < 0)
					optimalAngularVelYaw *= -1;
				optimalAngularVelYaw += targetAngularVelYaw + azError;
				if (Math.Abs(wantedYaw + yawDeltaInput) > maneuvrabilityYaw)//rough
				{
					yawRotorSpeed = optimalAngularVelYaw;
					yawGyroSpeed = ownYaw < optimalAngularVelYaw ? ownYaw + TURNGYROCONST : ownYaw - TURNGYROCONST;
					fullDriveYaw = true;
				}
				else//soft
				{
					yawRotorSpeed = wantedYaw + yawDeltaInput + targetAngularVelYaw + azError;
					yawGyroSpeed = wantedYaw + yawDeltaInput + targetAngularVelYaw;
					fullDriveYaw = false;
				}
			}
			//PITCH
			if (fullDrivePitch)
			{
				fullDrivePitch = false;
				maneuvrabilityPitch = Math.Abs(lastSpeedPitch.GetValueOrDefault() - ownPitch);
			}
			if (maneuvrabilityPitch == null)
			{
				if (wantedPitch > 0)
				{
					pitchRotorSpeed = MathHelper.Pi / 60;
					pitchGyroSpeed = ownPitch + TURNGYROCONST;
				}
				else
				{
					pitchRotorSpeed = -MathHelper.Pi / 60;
					pitchGyroSpeed = ownPitch - TURNGYROCONST;
				}
				fullDrivePitch = true;
			}
			else
			{
				//Расчет оптимальной угловой скорости для наведения
				double timeToStopPitch = Math.Abs((ownPitch - targetAngularVelPitch - pitchRotationInput - elError) / maneuvrabilityPitch.Value);
				double avaibleDistancePitch = wantedPitch + pitchDeltaInput - ownPitch + (targetAngularVelPitch + pitchRotationInput + elError) * timeToStopPitch;
				double optimalAngularVelPitch = SOFTNAVCONST * Math.Sqrt(Math.Abs(2 * maneuvrabilityPitch.Value * avaibleDistancePitch));
				if (wantedPitch + pitchDeltaInput < 0)
					optimalAngularVelPitch *= -1;
				optimalAngularVelPitch += targetAngularVelPitch + elError;
				if (Math.Abs(wantedPitch + pitchDeltaInput) > maneuvrabilityPitch)//rough
				{
					pitchRotorSpeed = optimalAngularVelPitch;
					pitchGyroSpeed = ownPitch < optimalAngularVelPitch ? ownPitch + TURNGYROCONST : ownPitch - TURNGYROCONST;
					fullDrivePitch = true;
				}
				else//soft
				{
					pitchRotorSpeed = wantedPitch + pitchDeltaInput + targetAngularVelPitch + elError;
					pitchGyroSpeed = wantedPitch + pitchDeltaInput + targetAngularVelPitch;
					fullDrivePitch = false;
				}
			}
			//pitchRotorSpeed = 0;
			//set speed
			rotorA.TargetVelocityRad = (float)yawRotorSpeed * 60;
			MainElRotor.TargetVelocityRad = (float)pitchRotorSpeed * 60;
			foreach (var rotor in rotorsE)
			{
				if (!rotor.Closed)
					SetSupprotRotor(rotor, turretFrontVec, (float)pitchRotorSpeed);
			}
			HullGuidance.ApplyGyroOverride(0, yawGyroSpeed * 60, 0, _turretGyros, turretMatrix);
			HullGuidance.ApplyGyroOverride(-pitchGyroSpeed * 60, yawGyroSpeed * 60, 0, _weaponGyros, turretMatrix);
			//update info
			lastInterceptVector = interceptVector;
			LastMatrix(ownYaw, ownPitch);
		}
		public void Update(float az, float el, ref bool centering, float azAngle, float elAngle, bool stab = true)
		{
			weaponMatrix = MyMath.CreateLookAtForwardDir(referenceBlock.GetPosition(), turretFrontVec, rotorA.WorldMatrix.Up);
			turretMatrix = MyMath.CreateLookAtUpDir(rotorA.Top.WorldMatrix.Translation, turretFrontVec, rotorA.WorldMatrix.Up);
			turretFrontVec = referenceBlock.WorldMatrix.Forward;
			if (block)
				return;
			if (centering)
			{
				fullDriveYaw = false; fullDrivePitch = false;
				HullGuidance.DropGyro(_turretGyros);
				HullGuidance.DropGyro(_weaponGyros);
				if (az == 0 && el == 0)
				{
					float elC = SetRotorAngle(MainElRotor, elAngle);
					SetRotorAngle(rotorA, azAngle);
					foreach (var rotor in rotorsE)
					{
						if (!rotor.Closed)
							SetSupprotRotor(rotor, turretFrontVec, elC);
					}
					return;
				}
				else
					centering = false;
			}
			yawDeltaInput = 0; pitchDeltaInput = 0;
			firstRunAim = false;
			float azError = 0;
			float elError = 0;
			if (stab)
			{
				azError = (float)MyMath.CalculateRotorDeviationAngle(rotorA.WorldMatrix.Forward, lastRotorAMatrix);
				elError = (float)MyMath.CalculateRotorDeviationAngle(MainElRotor.WorldMatrix.Forward, lastRotorEMatrix);
			}
			double ownYaw, ownPitch;
			MyMath.CalculateYawVelocity(turretMatrix, lastTurretMatrix, out ownYaw);
			MyMath.CalculatePitchVelocity(weaponMatrix, lastWeaponMatrix, out ownPitch);
			double yawRotationInput = az;
			double pitchRotationInput = el;
			//turret rotors
			float elevation = (float)(elError + pitchRotationInput);
			float azimuth = (float)(azError + yawRotationInput);
			MainElRotor.TargetVelocityRad = MultiplierElevation * elevation * 60;
			rotorA.TargetVelocityRad = azimuth * 60;
			foreach (var rotor in rotorsE)
			{
				if (!rotor.Closed)
					SetSupprotRotor(rotor, turretFrontVec, elevation);
			}
			double yawSpeed = 0, pitchSpeed = 0;
			//turret gyros
			//calculate maneuvrability
			azimuth -= azError;
			elevation -= elError;
			//YAW
			if (fullDriveYaw)
			{
				fullDriveYaw = false;
				maneuvrabilityYaw = Math.Abs(lastSpeedYaw.GetValueOrDefault() - ownYaw);
			}
			if (maneuvrabilityYaw == null)
			{
				if (azimuth != 0)
				{
					if (azimuth > 0)
					{
						fullDriveYaw = true;
						yawSpeed = ownYaw + TURNGYROCONST;
					}
					else yawSpeed = ownYaw - TURNGYROCONST;
					fullDriveYaw = true;
				}
			}
			else
			{
				if (Math.Abs(ownYaw - azimuth) > maneuvrabilityYaw)
				{
					if (ownYaw - azimuth > 0)
						yawSpeed = ownYaw - TURNGYROCONST;
					else
						yawSpeed = ownYaw + TURNGYROCONST;
					fullDriveYaw = true;
				}
				else
				{
					yawSpeed = azimuth;
				}
			}
			//PITCH
			if (fullDrivePitch)
			{
				fullDrivePitch = false;
				maneuvrabilityPitch = Math.Abs(lastSpeedPitch.GetValueOrDefault() - ownPitch);
			}
			if (maneuvrabilityPitch == null)
			{
				if (elevation != 0)
				{
					if (elevation > 0)
					{
						pitchSpeed = ownPitch + TURNGYROCONST;
					}
					else pitchSpeed = ownPitch - TURNGYROCONST;
					fullDrivePitch = true;
				}
			}
			else
			{
				if (Math.Abs(ownPitch - elevation) > maneuvrabilityPitch)
				{
					if (ownPitch - elevation > 0)
						pitchSpeed = ownPitch - TURNGYROCONST;
					else
						pitchSpeed = ownPitch + TURNGYROCONST;
					fullDrivePitch = true;
				}
				else
				{
					pitchSpeed = elevation;
				}
			}
			//turret gyro
			HullGuidance.ApplyGyroOverride(0, yawSpeed * 60, 0, _turretGyros, turretMatrix);
			//weapon gyro
			HullGuidance.ApplyGyroOverride(-pitchSpeed * 60, yawSpeed * 60, 0, _weaponGyros, turretMatrix);
			LastMatrix(ownYaw, ownPitch);
		}
		void LastMatrix(double ownYaw, double ownPitch)
		{
			lastSpeedYaw = ownYaw;
			lastSpeedPitch = ownPitch;
			lastTurretMatrix = turretMatrix;
			lastWeaponMatrix = weaponMatrix;
			lastRotorAMatrix = rotorA.WorldMatrix;
			lastRotorEMatrix = MainElRotor.WorldMatrix;
		}
		public void Block(bool b)
		{
			block = b;
			rotorA.RotorLock = b;
			MainElRotor.RotorLock = b;
			foreach (var r in rotorsE)
			{
				r.RotorLock = b;
				if (b)
					r.TargetVelocityRad = 0;
			}
			if (b)
			{
				fullDriveYaw = false; fullDrivePitch = false;
				HullGuidance.DropGyro(_turretGyros);
				HullGuidance.DropGyro(_weaponGyros);
				MainElRotor.TargetVelocityRad = 0;
				rotorA.TargetVelocityRad = 0;
				maneuvrabilityYaw = null;
				maneuvrabilityPitch = null;
				lastSpeedYaw = null; lastSpeedPitch = null;
			}
			else
			{
				lastRotorAMatrix = rotorA.WorldMatrix;
				lastRotorEMatrix = MainElRotor.WorldMatrix;
				lastWeaponMatrix = MyMath.CreateLookAtForwardDir(referenceBlock.GetPosition(), turretFrontVec, rotorA.WorldMatrix.Up);
				lastTurretMatrix = MyMath.CreateLookAtUpDir(rotorA.Top.WorldMatrix.Translation, turretFrontVec, rotorA.WorldMatrix.Up);
			}
		}

		public void Status(ref string statusInfo, string language, string azimuthTag, string elevationTag)
		{
			statusInfo += $"\nRotor \"{azimuthTag}\": {rotorA.CustomName}\n" +
				$"Main elevation rotor \"{elevationTag}\": {MainElRotor.CustomName}\n" +
				$"Count of elevation rotors: {rotorsE.Count + 1}\n" +
				$"Count of weapons: {weapons.Count}\n";
		}

        bool SetSupprotRotor(IMyMotorStator rotor, Vector3D direction, float mainRotTurnSpeed)
		{
			float localMultiplierElevation = MultiplierElevation;
			if (rotor.WorldMatrix.Up.Dot(MainElRotor.WorldMatrix.Up) < 0)  //Если ротор сонаправлен, то оставляем коэф, иначе меняем
			{
				localMultiplierElevation = -localMultiplierElevation;
			}
			Vector3D? frontVec = null;
			foreach (var gun in weapons)
				if (gun.CubeGrid == rotor.TopGrid)
				{
					frontVec = gun.WorldMatrix.Forward;
					break;
				}
			if (frontVec == null)
			{
				foreach (var camera in radarCameras)
					if (camera.CubeGrid == rotor.TopGrid)
					{
						frontVec = camera.WorldMatrix.Forward;
						break;
					}
			}
			if (frontVec == null)
				return false;
			Vector3D TargetVectorLoc = MyMath.VectorTransform(direction, rotor.WorldMatrix.GetOrientation());
			Vector3D GunVectorLoc = MyMath.VectorTransform(frontVec.GetValueOrDefault(), rotor.WorldMatrix.GetOrientation());
			double targetAngleLoc = Math.Atan2(-TargetVectorLoc.X, TargetVectorLoc.Z);
			double myAngleLoc = Math.Atan2(-GunVectorLoc.X, GunVectorLoc.Z);
			float Elevation = (float)(0.1 * (targetAngleLoc - myAngleLoc) + localMultiplierElevation * mainRotTurnSpeed);
			rotor.TargetVelocityRad = Elevation * 60;
			return true;
		}
		public bool TrySetRef(IMyTerminalBlock reference)
		{
			if (weapons.Contains(reference))
			{
				referenceBlock = reference;
				return true;
			}
			if (radarCameras.Contains(reference))
			{
				referenceBlock = reference;
				return true;
			}
			return false;
		}

		/*static void TurnRotor(IMyMotorStator rotor, float angleDiff)
		{
			if (!rotor.Closed)
			{
				angleDiff %= MathHelper.TwoPi;
				if (angleDiff > MathHelper.Pi)
					angleDiff = -MathHelper.TwoPi + angleDiff;
				else if (angleDiff < -MathHelper.Pi)
					angleDiff = MathHelper.TwoPi + angleDiff;

				float rotorVelocity = PROPGAIN * angleDiff;
				rotor.TargetVelocityRPM = rotorVelocity;
			}
		}*/
		static float SetRotorAngle(IMyMotorStator rotor, float degreeAngle)
		{
			if (!rotor.Closed)
			{
				float radAngle = (float)(degreeAngle / 180 * MathHelper.Pi);
				float currAngle = rotor.Angle;
				float angleDiff = radAngle - currAngle;

				angleDiff %= MathHelper.TwoPi;
				if (angleDiff > MathHelper.Pi)
					angleDiff = -MathHelper.TwoPi + angleDiff;
				else if (angleDiff < -MathHelper.Pi)
					angleDiff = MathHelper.TwoPi + angleDiff;
				float Elevation = (float)(0.1 * angleDiff);
				rotor.TargetVelocityRad = Elevation * 30;
				return Elevation;
			}
			return 0;
		}
		bool CheckRefBlock()
		{
			if (referenceBlock == null)
				return FindRefBlock();
			if (referenceBlock.Closed)
				return FindRefBlock();
			return false;
		}
		bool FindRefBlock()
		{
			foreach (var weapon in weapons)
			{
				if (weapon.CubeGrid == MainElRotor.TopGrid)
				{
					referenceBlock = weapon;
					break;
				}
			}
			if (referenceBlock == null)
			{
				referenceBlock = radarCameras[0];
			}
			if (referenceBlock == null)
				return false;
			return true;
		}

	}
}
