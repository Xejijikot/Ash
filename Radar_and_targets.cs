using Sandbox.Game.EntityComponents;
using Sandbox.Game.WorldEnvironment.Modules;
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
	public class Radar
	{
		public List<IMyCameraBlock> radarCameras = new List<IMyCameraBlock>();
		public EnemyTargetedInfo lockedtarget { private set; get; }
		long lastRadarLockTick = 0;
		public Vector3D? pointOfLock;
		public int countOfCameras;
		public bool Searching = false;
		public int counter = 0;
		const float STABLELOCK = 1.1f;      //Для накопления ресурса радара для предупреждения срыва захвата
		const float NEWTARGET = 0.25f;		//Для поиска новой цели перед срывом захвата
		bool enemy = true;
		bool neutral = false;
		bool allie = false;
		public Radar(List<IMyCameraBlock> radar)
		{
			radarCameras = radar;
			foreach (var camera in radarCameras)
				camera.EnableRaycast = true;
			lockedtarget = null;
			countOfCameras = radarCameras.Count;
		}
		public void SetTargets(bool allieIn, bool neutralIn, bool enemyIn)
		{
			enemy = enemyIn;
			neutral = neutralIn;
			allie = allieIn;
		}
		public bool UpdateTarget(EnemyTargetedInfo newInfo)
		{
			if (lockedtarget != null)
				if (lockedtarget.EntityId == newInfo.EntityId && lockedtarget.LastLockTick < newInfo.LastLockTick)
				{
					lockedtarget.Position = newInfo.Position;
					lockedtarget.LastLockTick = newInfo.LastLockTick;
					lockedtarget.TargetSubsystems = newInfo.TargetSubsystems;
					lockedtarget.PowerSubsystems = newInfo.PowerSubsystems;
					lockedtarget.PropSubsystems = newInfo.PropSubsystems;
					lockedtarget.WeaponSubsystems = newInfo.WeaponSubsystems;
                    return true;
				}
            return false;
		}
        public bool TryLock(long tick, double InitialRange = 2000)
		{
			MyDetectedEntityInfo newDetectedInfo;
			long TickPassed = tick - lastRadarLockTick;
			if (TickPassed > InitialRange * 0.03 / countOfCameras)
			{
				var lockcam = GetCameraWithMaxRange(radarCameras);
				if (lockcam == null)
					return false;
				if (lockcam.CanScan(InitialRange))
				{
					newDetectedInfo = lockcam.Raycast(InitialRange, 0, 0);
					if (!newDetectedInfo.IsEmpty())
					{
						if (newDetectedInfo.Type == MyDetectedEntityType.SmallGrid || newDetectedInfo.Type == MyDetectedEntityType.LargeGrid)
						{
							if ((newDetectedInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies) && enemy
								|| (newDetectedInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral) && neutral
								|| (newDetectedInfo.Relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership) && neutral
                                || (newDetectedInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Friends) && allie
								|| (newDetectedInfo.Relationship == MyRelationsBetweenPlayerAndBlock.Owner) && allie)
							{
								lockedtarget = new EnemyTargetedInfo(tick, newDetectedInfo, lockcam.WorldMatrix.Forward);
								lastRadarLockTick = tick;
								counter = 0;
                                pointOfLock = null;
                            }
                            else
                                pointOfLock = newDetectedInfo.HitPosition;
                        }
						else
							pointOfLock = newDetectedInfo.HitPosition;
                    }
				}
			}
			return true;
		}
		public bool Update(ref string debuginfo, long tick, int unlockTime, double initialRange = 2000)
		{
			counter++;
			if (countOfCameras < 2)
				return false;
			if (lockedtarget != null)
			{
				long TickPassed = tick - lastRadarLockTick;
				Vector3D shift = lockedtarget.Velocity * TickPassed / 60;
				if (shift.Length() < 0.002)
					shift = Vector3D.Zero;											//Цели на земле имеют небольшую скорость, направленную от земли, как баг

				if (TickPassed > (lockedtarget.Position + shift - radarCameras[0].GetPosition()).Length() * 0.03 / radarCameras.Count * STABLELOCK)     //Проверка радара на готовность лока
				{
					if (radarCameras == null)
						return false;
					IMyCameraBlock c = GetCameraWithMaxRange(radarCameras);
					if (c == null)
						return false;
					double TargetDistance = (lockedtarget.Position + shift - c.GetPosition()).Length() + 10d;
					MyDetectedEntityInfo DetectedEntity;
					if (c.AvailableScanRange >= TargetDistance)                                                                             //Все условия выполнены, попытка захвата
					{
						Vector3D point;
						Vector3D dir;
						Vector3D locDir;
						Vector3D camPos = c.GetPosition();
						if (lockedtarget.DeltaPosition != null)
						{
							point = lockedtarget.Position + shift + lockedtarget.DeltaPosition.GetValueOrDefault();				//Сперва - примерно туда же где лочили раньше     
							dir = point - camPos;
							locDir = Vector3D.TransformNormal(dir, MatrixD.Transpose(c.WorldMatrix));
							DetectedEntity = c.Raycast(dir.Length() + 10, locDir);													//На 10 глубже для стабильного захвата
							if (DetectedEntity.EntityId == lockedtarget.EntityId)
								goto UpdateInfo;
							if(counter > unlockTime * NEWTARGET)
								if(CheckForNewTarget(DetectedEntity, tick, c.WorldMatrix.Forward))
                                    goto UpdateInfo;

                        }
						if (lockedtarget.TargetedPoint != null)													                //Если не вышло - в точку в которую хотим попасть
						{
							point = lockedtarget.inBodyPointPosition.GetValueOrDefault() + shift;
							dir = point - camPos;
							locDir = Vector3D.TransformNormal(dir, MatrixD.Transpose(c.WorldMatrix));
							DetectedEntity = c.Raycast(dir.Length() + 10, locDir);
							if (DetectedEntity.EntityId == lockedtarget.EntityId)
							{
								goto UpdateInfo;
                            }
                            if (counter > unlockTime * NEWTARGET)
                                if (CheckForNewTarget(DetectedEntity, tick, c.WorldMatrix.Forward))
                                    goto UpdateInfo;
                        }
						point = lockedtarget.Position + shift;																	//Если не вышло - в центр структуры
						dir = point - camPos;
						locDir = Vector3D.TransformNormal(dir, MatrixD.Transpose(c.WorldMatrix));
						DetectedEntity = c.Raycast(dir.Length() + 10, locDir);

						if (DetectedEntity.EntityId == lockedtarget.EntityId)
							goto UpdateInfo;
                        if (counter > unlockTime * NEWTARGET)
                            if (CheckForNewTarget(DetectedEntity, tick, c.WorldMatrix.Forward))
                                goto UpdateInfo;
                    }
					else
						return false;
					UpdateInfo:
					if (DetectedEntity.EntityId == lockedtarget.EntityId)
					{
						lastRadarLockTick = tick;
						lockedtarget.UpdateTargetInfo(tick, DetectedEntity, c.WorldMatrix.Forward);
						counter = 0;
					}
				}
				if (counter >= unlockTime)
				{
					counter = 0;
					lockedtarget = null;
				}
			}
			else if (Searching)
			{
				TryLock(tick, initialRange);
			}
			return true;
		}
		public void GetTarget(EnemyTargetedInfo target, long tick)
		{
			Searching = true;
			lockedtarget = target;
			lastRadarLockTick = tick;
        }
		bool CheckForNewTarget(MyDetectedEntityInfo newEntity, long tick, Vector3D viewvec)
		{
            if (newEntity.Type == MyDetectedEntityType.SmallGrid || newEntity.Type == MyDetectedEntityType.LargeGrid)
                if ((newEntity.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies) && enemy
                    || (newEntity.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral) && neutral
                    || (newEntity.Relationship == MyRelationsBetweenPlayerAndBlock.Friends) && allie
                    || (newEntity.Relationship == MyRelationsBetweenPlayerAndBlock.Owner) && allie)
                {
                    lockedtarget = new EnemyTargetedInfo(tick, newEntity, viewvec);
                    lastRadarLockTick = tick;
                    counter = 0;
					return true;
                }
			return false;
        }
		public void DropLock()
		{
			pointOfLock = null;
			lockedtarget = null;
			Searching = false;
		}
		IMyCameraBlock GetCameraWithMaxRange(List<IMyCameraBlock> cameras)
		{
			double maxRange = 0;
			IMyCameraBlock maxRangeCamera = null;
			foreach (var c in cameras)
			{
				if (c.AvailableScanRange > maxRange)
				{
					maxRangeCamera = c;
					maxRange = maxRangeCamera.AvailableScanRange;
				}
			}
			return maxRangeCamera;
		}



	}

}
