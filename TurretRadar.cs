using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VRageRender.Textures;

namespace IngameScript
{
	public class TurretRadar
	{
		public List<IMyLargeTurretBase> _turrets = new List<IMyLargeTurretBase>();
		public List<IMyTurretControlBlock> _TCs = new List<IMyTurretControlBlock>();
		List<EnemyTargetedInfo> _enemyTargetedInfos = new List<EnemyTargetedInfo>();
		public long? _lastChangeTick = null;
		bool _change = false;
		int cycle = 0;
		public void UpdateBlocks(List<IMyLargeTurretBase> turrets, List<IMyTurretControlBlock> turretControlBlocks, bool change = true)
		{
			_change = change;
			_turrets = turrets;
			_TCs = turretControlBlocks;
			if (_change)
			{
				foreach (var t in _turrets)
				{
					switch (cycle)
					{
						case 0:
							t.SetTargetingGroup("Weapons");
							break;
						case 1:
							t.SetTargetingGroup("Propulsion");
							break;
						default:
							t.SetTargetingGroup("PowerSystems");
							break;
					}
				}
				foreach (var t in _TCs)
				{
					switch (cycle)
					{
						case 0:
							t.SetTargetingGroup("Weapons");
							break;
						case 1:
							t.SetTargetingGroup("Propulsion");
							break;
						default:
							t.SetTargetingGroup("PowerSystems");
							break;
					}
				}
			}
		}
		public void Update(long tick, bool b = true)
		{
			if (b)
				_lastChangeTick = null;
			foreach (var t in _turrets)
			{
				bool weHaveThisTarget = false;
				if (t.HasTarget && !t.GetTargetedEntity().IsEmpty())
				{
					MyDetectedEntityInfo NewTarget = t.GetTargetedEntity();
					for (int i = 0; i < _enemyTargetedInfos.Count; i++)
					{
						if (NewTarget.EntityId == _enemyTargetedInfos[i].EntityId)
						{
							_enemyTargetedInfos[i].UpdateTargetInfo(tick, NewTarget);
							weHaveThisTarget = true;
						}
					}
					if (!weHaveThisTarget)
					{
						EnemyTargetedInfo target = new EnemyTargetedInfo(tick, NewTarget);
						_enemyTargetedInfos.Add(target);
					}
				}
			}
			foreach (var t in _TCs)
			{
				bool weHaveThisTarget = false;
				if (t.HasTarget && !t.GetTargetedEntity().IsEmpty())
				{
					MyDetectedEntityInfo NewTarget = t.GetTargetedEntity();
					for (int i = 0; i < _enemyTargetedInfos.Count; i++)
					{
						if (NewTarget.EntityId == _enemyTargetedInfos[i].EntityId)
						{
							_enemyTargetedInfos[i].UpdateTargetInfo(tick, NewTarget);
							weHaveThisTarget = true;
						}
					}
					if (!weHaveThisTarget)
					{
						EnemyTargetedInfo target = new EnemyTargetedInfo(tick, NewTarget);
						_enemyTargetedInfos.Add(target);
					}
				}
			}
			DeleteOldTargets(tick);
		}
		public EnemyTargetedInfo Update(ref string _debuginfo, long tick, EnemyTargetedInfo target)
		{
			Update(tick, false);
			if (_change)
			{
				foreach (var t in _turrets)
				{
					if (t.HasTarget && !t.GetTargetedEntity().IsEmpty())
					{
						MyDetectedEntityInfo NewSubsystem = t.GetTargetedEntity();
						if (target.EntityId == NewSubsystem.EntityId)
						{
							_debuginfo += target.AddSubsystem(tick, NewSubsystem, t.GetTargetingGroup()) + "\n";
						}
					}
				}
				foreach (var t in _TCs)
				{
					if (t.HasTarget && !t.GetTargetedEntity().IsEmpty())
					{
						MyDetectedEntityInfo NewSubsystem = t.GetTargetedEntity();
						if (target.EntityId == NewSubsystem.EntityId)
						{
							_debuginfo += target.AddSubsystem(tick, NewSubsystem, t.GetTargetingGroup()) + "\n";
						}
					}
				}
				if (_lastChangeTick == null)
				{
					_lastChangeTick = tick;
					foreach (var t in _turrets)
					{
						switch (cycle)
						{
							case 0:
								t.SetTargetingGroup("Weapons");
								break;
							case 1:
								t.SetTargetingGroup("Propulsion");
								break;
							default:
								t.SetTargetingGroup("PowerSystems");
								break;
						}
					}
					foreach (var t in _TCs)
					{
						switch (cycle)
						{
							case 0:
								t.SetTargetingGroup("Weapons");
								break;
							case 1:
								t.SetTargetingGroup("Propulsion");
								break;
							default:
								t.SetTargetingGroup("PowerSystems");
								break;
						}
					}
				}
				else
				{
					if ((_lastChangeTick - tick) % 120 == 0)
					{
						switch (cycle)
						{
							case 2:
								cycle = 0;
								break;
							default:
								cycle++;
								break;
						}
						if (_change)
						{
							foreach (var t in _turrets)
							{
								switch (cycle)
								{
									case 0:
										t.SetTargetingGroup("Weapons");
										break;
									case 1:
										t.SetTargetingGroup("Propulsion");
										break;
									default:
										t.SetTargetingGroup("PowerSystems");
										break;
								}
							}
							foreach (var t in _TCs)
							{
								switch (cycle)
								{
									case 0:
										t.SetTargetingGroup("Weapons");
										break;
									case 1:
										t.SetTargetingGroup("Propulsion");
										break;
									default:
										t.SetTargetingGroup("PowerSystems");
										break;
								}
							}
						}
					}
				}
			}
			return target;
		}
		void DeleteOldTargets(long tick)
		{
			foreach (var t in _enemyTargetedInfos)
				if (tick - t.LastLockTick > 300)
				{
					_enemyTargetedInfos.Remove(t);
					return;
				}
		}
		public List<EnemyTargetedInfo> GetTargets() { return _enemyTargetedInfos; }
	}
}
