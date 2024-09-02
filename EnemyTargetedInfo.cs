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
using VRage.Game.ObjectBuilders.VisualScripting;
using VRageMath;

namespace IngameScript
{
    public class EnemyTargetedInfo
    {
        public long EntityId { private set; get; }
        public MyDetectedEntityType Type;
        public Vector3D? TargetedPoint;         //Точка на цели, куда желаем попасть
        public Vector3D? HitPosition;           //Точка попадания рейкастом
        public Vector3D Position;               //Центр цели
        public Vector3D? inBodyPointPosition;   //Вектор в системе координат цели, от центра цели до точки желаемого попадания
        public Vector3D? DeltaPosition;         //Разница между точкой попадания рейкастом и центра цели, для стабильного захвата
        public Vector3D Velocity;
        public Vector3D? Acceleration;
        public MatrixD Orientation;
        public long LastLockTick;
        public List<TargetSubsystem> TargetSubsystems = new List<TargetSubsystem>();
        public List<TargetSubsystem> PowerSubsystems = new List<TargetSubsystem>();
        public List<TargetSubsystem> PropSubsystems = new List<TargetSubsystem>();
        public List<TargetSubsystem> WeaponSubsystems = new List<TargetSubsystem>();

        public EnemyTargetedInfo(long tick, MyDetectedEntityInfo newentityInfo, Vector3D? dir = null)
        {
            EntityId = newentityInfo.EntityId;
            Type = newentityInfo.Type;
            HitPosition = newentityInfo.HitPosition;
            Position = newentityInfo.Position;
            if (HitPosition != null)
            {
                TargetedPoint = HitPosition;
                DeltaPosition = HitPosition.GetValueOrDefault() - Position;
            }
            Orientation = newentityInfo.Orientation;
            if (TargetedPoint != null)
            {
                Vector3D worldDirection = TargetedPoint.GetValueOrDefault() - Position;
                inBodyPointPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(Orientation));
                if (dir != null)
                    TargetedPoint += dir / dir.GetValueOrDefault().Length() * 0.5;

            }
            Velocity = newentityInfo.Velocity;
            Acceleration = null;
            LastLockTick = tick;
        }
        public void UpdateTargetInfo(long tick, MyDetectedEntityInfo newEntityInfo, Vector3D? dir = null)
        {
            EntityId = newEntityInfo.EntityId;
            Type = newEntityInfo.Type;
            HitPosition = newEntityInfo.HitPosition;
            Position = newEntityInfo.Position;
            Orientation = newEntityInfo.Orientation;
            if (HitPosition != null)
            {
                DeltaPosition = HitPosition.GetValueOrDefault() - Position;
                if (TargetedPoint == null)
                {
                    TargetedPoint = HitPosition;
                    if (dir != null)
                        TargetedPoint += dir / dir.GetValueOrDefault().Length() * 0.5;
                    Vector3D worldDirection = TargetedPoint.GetValueOrDefault() - Position;
                    inBodyPointPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(Orientation));
                }
            }
            else DeltaPosition = null;
            if (inBodyPointPosition != null)
            {
                Vector3D worldDirection = Vector3D.TransformNormal(inBodyPointPosition.GetValueOrDefault(), Orientation);
                TargetedPoint = worldDirection + Position;
            }
            else TargetedPoint = null;
            if (LastLockTick - tick > 60)
                Acceleration = null;
            else
                Acceleration = (newEntityInfo.Velocity - Velocity) / (LastLockTick - tick);
            Velocity = newEntityInfo.Velocity;
            LastLockTick = tick;
        }
        public bool AddSubsystem(long tick, Vector3D worldPosition, string type)
        {
            TargetSubsystem newSubsystem = new TargetSubsystem(tick, worldPosition, this, type);
            return CheckSubsystem(tick, newSubsystem, type);
        }
        public bool AddSubsystem(long tick, MyDetectedEntityInfo newEntityInfo, string type)
        {
            TargetSubsystem newSubsystem = new TargetSubsystem(tick, newEntityInfo.HitPosition.GetValueOrDefault(), this, type);
            return CheckSubsystem(tick, newSubsystem, type);
        }
        bool CheckSubsystem(long tick, TargetSubsystem newSubsystem, string type)
        {
            float size;
            if (Type == MyDetectedEntityType.LargeGrid)
                size = 2.5f;
            else if (Type == MyDetectedEntityType.SmallGrid)
                size = 0.5f;
            else
                return false;
            foreach (var subsystem in TargetSubsystems)
            {
                Vector3D delta = subsystem.gridPosition - newSubsystem.gridPosition;
                if (delta.Length() / size < 1)
                {
                    if (subsystem.subsystemType == newSubsystem.subsystemType)
                    {
                        subsystem.lastUpdateTick = tick;
                        return true;
                    }
                    else
                        return false;
                }
            }
            TargetSubsystems.Add(newSubsystem);
            if (type == "Weapons")
                WeaponSubsystems.Add(newSubsystem);
            if (type == "Propulsion")
                PropSubsystems.Add(newSubsystem);
            if (type == "PowerSystems")
                PowerSubsystems.Add(newSubsystem);
            return true;
        }
        public MyTuple<long, MatrixD, MatrixD> CreateMessage(long tick)
        {
            long TickPassed = tick - LastLockTick;
            var pos = Position + Velocity * TickPassed / 60;
            Vector3D vector = inBodyPointPosition.GetValueOrDefault(Vector3D.Zero);
            MatrixD positions = default(MatrixD);
            positions.Right = pos;
            positions.Up = vector;
            positions.Backward = Velocity;
            var message = new MyTuple<long, MatrixD, MatrixD>(EntityId, positions, Orientation);
            return message;
        }
        public MyTuple<long, MatrixD, MatrixD, string> CreateMessage(long tick, string i)
        {
            var m = CreateMessage(tick);
            return new MyTuple<long, MatrixD, MatrixD, string>(m.Item1, m.Item2, m.Item3, i);
        }
    }
    public class TargetSubsystem
    {
        public long lastUpdateTick;
        public string subsystemType { get; }
        public Vector3D gridPosition { get; }
        public TargetSubsystem(long tick, Vector3D worldPosition, EnemyTargetedInfo Target, string type)
        {
            lastUpdateTick = tick;
            subsystemType = type;
            Vector3D worldDirection = worldPosition - Target.Position;
            gridPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(Target.Orientation));
        }
        public Vector3D GetPosition(EnemyTargetedInfo Target)
        {
            Vector3D worldDirection = Vector3D.TransformNormal(gridPosition, Target.Orientation);
            Vector3D worldPosition = worldDirection + Target.Position;
            return worldPosition;
        }
    }
}
