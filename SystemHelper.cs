using EmptyKeys.UserInterface.Generated.DataTemplatesContracts_Bindings;
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
using System.Text.RegularExpressions;
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

	static class SystemHelper
	{
		public static bool AddBlockIfType<T>(IMyTerminalBlock block, out T orig) where T : class, IMyTerminalBlock
		{
			T typedBlock = block as T;
			orig = typedBlock;
			if (typedBlock == null)
				return false;
			return true;
		}
		public static bool AddToListIfType<T>(IMyTerminalBlock block, List<T> list) where T : class, IMyTerminalBlock
		{
			T typedBlock;
			return AddToListIfType(block, list, out typedBlock);
		}
		public static bool AddToListIfType<T>(IMyTerminalBlock block, List<T> list, out T typedBlock) where T : class, IMyTerminalBlock
		{
			typedBlock = block as T;
			if (typedBlock != null)
			{
				list.Add(typedBlock);
				return true;
			}
			return false;
		}
		public static string GetKey(IMyTerminalBlock block)
		{
			string wType = block.BlockDefinition.TypeIdString;
			wType = wType.Substring(wType.IndexOf('_') + 1);
			string key = wType + "/" + block.BlockDefinition.SubtypeName;
			return key;
		}
		public static List<string> SplitString(string str)
		{
			List<string> results = new List<string>();
			var builder = new StringBuilder();
			bool quotation = false;
			//lcd.WriteText("Start\n");
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];
				if (!quotation)
				{
					if (c != ' ' && c != '"')
					{
						//lcd.WriteText($"new char \"{c}\"\n", true);
						builder.Append(c);
					}
					if (c == ' ' && builder.Length != 0)
					{
						//lcd.WriteText("new Word\n", true);
						results.Add(builder.ToString());
						builder = new StringBuilder();
					}
					if (c == '"')
					{
						//lcd.WriteText("quotation\n", true);
						quotation = true;
					}
				}
				else
				{
					if (c != '"')
					{
						builder.Append(c);
					}
					else
					{
						quotation = false;
						results.Add(builder.ToString());
						builder = new StringBuilder();
					}
				}
				if (i + 1 == str.Length && builder.Length != 0)
				{
					results.Add(builder.ToString());
					continue;
				}
			}
			return results;

		}
	}
}
