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
using System.Reflection;
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
		public void SendLaunchMessage(string tag, EnemyTargetedInfo target, IMyTerminalBlock refBlock, bool b = false)
		{
			if (target != null)
			{
				if (b && target.inBodyPointPosition != null)
				{
					var container = new MyTuple<long, Vector3D, MatrixD, Vector3D>(target.EntityId, target.Position, target.Orientation, target.inBodyPointPosition.Value);
                    _IGC.SendBroadcastMessage(tag, container, TransmissionDistance.CurrentConstruct);
					return;
                }
				var container2 = new MyTuple<long, Vector3D>(target.EntityId, target.Position);
                _IGC.SendBroadcastMessage(tag, container2, TransmissionDistance.CurrentConstruct);
            }
			else if (refBlock != null)
				SendLaunchMessage(tag, refBlock.WorldMatrix.Forward);
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
        public void GetMessageFromAxis(string tag, EnemyTargetedInfo target, IMyTerminalBlock refBlock)
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
							SendPingAnswer(tag);
                        if ((string)Data == "PointRequest")
                            SendLaunchMessage(tag, target, refBlock, true);
                        if ((string)Data == "TargetRequest")
                            SendLaunchMessage(tag, target, refBlock);
                    }
				}
			}
		}
	}
}
