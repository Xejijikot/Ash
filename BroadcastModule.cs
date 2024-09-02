using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Achievements;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
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
	public class BroadcastModule
	{
		IMyBroadcastListener _mListener, _aListener;
		IMyIntergridCommunicationSystem _IGC;
		public BroadcastModule(IMyIntergridCommunicationSystem IGC, string _IGCAxisTag, string _IGCAshTag)
		{
			_IGC = IGC;
			_mListener = _IGC.RegisterBroadcastListener(_IGCAxisTag);
			_aListener = _IGC.RegisterBroadcastListener(_IGCAshTag);
		}
		public void SendLaunchMessage(long tick, string tag, string i, EnemyTargetedInfo target, IMyTerminalBlock refBlock)
		{
			if (target != null)
			{
				var container = target.CreateMessage(tick, i);
				_IGC.SendBroadcastMessage(tag, container, TransmissionDistance.CurrentConstruct);
				foreach (var s in target.TargetSubsystems)
				{
					var info = new MyTuple<long, string, Vector3D>(target.EntityId, s.subsystemType, s.gridPosition);
                    _IGC.SendBroadcastMessage(tag, info, TransmissionDistance.CurrentConstruct);
                }
			}
			else if (refBlock != null)
				SendLaunchMessage(tag, refBlock.WorldMatrix.Forward);
		}
		public void SendInfo(long tick, string tag, EnemyTargetedInfo target)
		{
            if (target != null)
            {
                var container = target.CreateMessage(tick);
                _IGC.SendBroadcastMessage(tag, container, TransmissionDistance.CurrentConstruct);
            }
        }
		public void SendLaunchMessage(string tag, Vector3D Dir)
		{
			Vector3D container = Dir;
			_IGC.SendBroadcastMessage(tag, container, TransmissionDistance.CurrentConstruct);
		}
		public void SendPingAnswer(string tag)
		{
			_IGC.SendBroadcastMessage(tag, "answer", TransmissionDistance.CurrentConstruct);
		}
		public void SendMyPos(string tag, long id, Vector3D pos, string name)
		{
			var container = new MyTuple<long, Vector3D, Vector3D, string>(id, pos, Vector3D.Zero, name);
            _IGC.SendBroadcastMessage(tag, container, TransmissionDistance.AntennaRelay);
        }
		public void GetMessageFromAxis(long tick, ref long axisTick, string tag, EnemyTargetedInfo target, IMyTerminalBlock refBlock, LinkedList<UnitInfo> missiles)
		{
			while (_mListener.HasPendingMessage)
			{
				MyIGCMessage myIGCMessage = _mListener.AcceptMessage();
				var Data = myIGCMessage.Data;
				if (myIGCMessage.Tag == tag)
				{ // This is our tag
					if (Data is MyTuple<int, string>)
					{

					}
					if (Data is string)
					{
						if ((string)Data == "ping")
						{
							axisTick = tick;
                            SendPingAnswer(tag);
						}
                    }
                    if (Data is MyTuple<string, string>)
                    {
						var m = (MyTuple<string, string>)Data;
                        if (m.Item1 == "Launch")
                        {
                            axisTick = tick;
                            SendLaunchMessage(tick, tag, m.Item2, target, refBlock);
                        }
                    }
                    if (Data is MyTuple<long, long, Vector3D, Vector3D>)
					{
						var d = (MyTuple<long, long, Vector3D, Vector3D>)Data;
						bool b = false;
						foreach (var m in missiles)
						{
                            if (m.id == d.Item1)
							{
								m.Update(tick, d.Item2, d.Item3, d.Item4);
								b = true;
								break;
							}
						}
						if (!b)
                            missiles.AddFirst(new UnitInfo { id = d.Item1 , tick = tick, tId = d.Item2, pos = d.Item3, target = d.Item4});
                    }
				}
			}
            var s = missiles.First;
			while (s != null)
			{
				var s1 = s.Next;
				if (tick - s.Value.tick> 5)
                    missiles.Remove(s);
				s = s1;
            }
        }
		public void GetMessageFromAsh(long tick, long id, string tag, LinkedList<AllieUnitInfo> allies)
		{
            while (_aListener.HasPendingMessage)
            {
                MyIGCMessage myIGCMessage = _aListener.AcceptMessage();
                var Data = myIGCMessage.Data;
                if (myIGCMessage.Tag == tag && myIGCMessage.Source != id)
                { // This is our tag
                    if (Data is MyTuple<long, Vector3D, Vector3D, string>)
                    {
                        var d = (MyTuple<long, Vector3D, Vector3D, string>)Data;
                        bool b = false;
                        foreach (var a in allies)
                        {
                            if (a.id == d.Item1)
                            {
                                a.Update(tick, 0, d.Item2, d.Item3);
								a.name = d.Item4;
                                b = true;
                                break;
                            }
                        }
                        if (!b)
                            allies.AddFirst(new AllieUnitInfo { id = d.Item1, tick = tick, pos = d.Item2, target = Vector3D.Zero, name = d.Item4, tId = 0 });
                    }
                }
            }
            var s = allies.First;
            while (s != null)
            {
                var s1 = s.Next;
                if (tick - s.Value.tick > 5)
                    allies.Remove(s);
                s = s1;
            }
        }
	}
	public class UnitInfo
	{
        public long id, tick, tId;
        public Vector3D pos, target;
		public void Update(long tick, long tId, Vector3D p, Vector3D t)
		{
			pos = p; target = t; this.tick = tick; this.tId = tId;
		}
	}
	public class AllieUnitInfo : UnitInfo
	{
		public string name;

    }

	//Target info structure for messages
	/*public  class MessageStruct
	{
		public long EntityId { get; private set; }
        public MyDetectedEntityType Type { get; private set; }
        public Vector3D Position { get; private set; }               //Центр цели
        public Vector3D inBodyPointPosition { get; private set; }   //Вектор в системе координат цели, от центра цели до точки желаемого попадания
        public Vector3D Velocity { get; private set; }
        public MatrixD Orientation { get; private set; }
        public int LastLockTickDelta { get; private set; }
        public List<subsystemMessageStruct> subsystems { get; private set; }
		public MessageStruct(long entityId, MyDetectedEntityType type, Vector3D position, Vector3D inBodyPointPosition, Vector3D velocity, MatrixD orientation, int lastLockTickDelta, List<subsystemMessageStruct> subsystems)
        {
            EntityId = entityId;
            Type = type;
            Position = position;
            this.inBodyPointPosition = inBodyPointPosition;
            Velocity = velocity;
            Orientation = orientation;
            LastLockTickDelta = lastLockTickDelta;
            this.subsystems = subsystems;
        }
    }
	public struct subsystemMessageStruct
	{
		public string subsystemType;
		public Vector3D gridPosition;
	}*/
}
