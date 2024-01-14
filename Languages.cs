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

	public static class Languages
	{
		public static string storage =
			"[Russian]\n" +
			"SCRIPT_NAME=СУО \"Ясень\"\n" +
			"TURRET=Башня\n" +
			"HULL=Корпус\n" +
			"GnF=Не найдена группа блоков!\n" +
			"NAME=Имя группы:\n" +
			"CANTLOCK=Недостаточно камер в радаре\n" +
			"LASTUPDATE=Последнее обновление блоков:\n" +
			"RADARCAMERAS=Камер в радаре -\n" +
			"TEXTPANELS=Панелей индикации -\n" +
			"MAINCOCKPIT=Главный кокпит -\n" +
			"SUCCESS=Успех\n" +
			"FAIL=Провал\n" +
			"TURRETGROUPBLOCKS=Попытка создать турель из блоков группы...\n" +
			"NOGUNS=Нет пушек на роторах\n" +
			"NOROTORS=Не достаточно роторов в группе\n" +
			"AUTOTURRETSUCCESS=Автоматический переход в режим турели - успех, все составляющие найдены\n" +
			"AUTOTURRETFAIL=Автоматический переход в режим турели не удался\n" +
			"AUTOHULLFAIL=Попытка перейти в режим наведения корпусом неуспешна\n" +
			"AUTOHULLSUCCESS=Активировано наведение корпусом\n" +
			"NOCOCKPITS=Нет кокпитов\n" +
			"NOGYRO=Нет гироскопов\n" +
			"GYROS=Гироскопов\n" +
			"SEARCHING=Радар - поиск целей:\n" +
			"LOCKED=Цель захвачена\n" +
			"ROTOR=Ротор\n" +
			"MAINEROTOR=Ведущий подъемный ротор\n" +
			"ALLROTORS=Подъемных роторов всего\n" +
			"ALLWEAPONS=Всего орудий\n" +
			"WEAPON=Орудие\n" +
			"CUSTOM=Кастомные@установки\n" +
			"GATLING=Гатлинг\n" +
			"AUTOCANON=Автопушка\n" +
			"ASSAULT=Штурм\n" +
			"ARTY=Артиллерия\n" +
			"SRAILGUN=Малый Рельсотрон\n" +
			"LRAILGUN=Большой Рельсотрон\n" +
			"AIM=Наведение\n" +
			"AUTO=Автоматическое\n" +
			"TRACKING=Помощь в\n" +
			"ASSIST=наведении\n" +

			"[English]\n" +
			"SCRIPT_NAME=FCS \"Ash\"\n" +
			"TURRET=Turret\n" +
			"HULL=Hull\n" +
			"GnF=Group not found!\n" +
			"NAME=Name of Group:\n" +
			"CANTLOCK=Not enought cameras in the radar\n" +
			"LASTUPDATE=Last update:\n" +
			"RADARCAMERAS=Cameras in radar -\n" +
			"TEXTPANELS=Text panels -\n" +
			"MAINCOCKPIT=Main cockpit -\n" +
			"SUCCESS=Success\n" +
			"FAIL=Failure\n" +
			"TURRETGROUPBLOCKS=Trying to create a turret from blocks in a group...\n" +
			"NOGUNS=Not found weapons on rotors\n" +
			"NOROTORS=Not enought rotors in the group\n" +
			"AUTOTURRETSUCCESS=Successful auto-transition to turret mode, all components found\n" +
			"AUTOTURRETFAIL=Auto-transition to turret mode failed\n" +
			"AUTOHULLFAIL=Transition to hull-guided mode failed\n" +
			"AUTOHULLSUCCESS=Hull aiming activated\n" +
			"NOCOCKPITS=No cockpits\n" +
			"NOGYRO=No gyros\n" +
			"GYROS=Gyroscopes\n" +
			"SEARCHING=Radar - searching\n" +
			"LOCKED=Target locked\n" +
			"ROTOR=Rotor\n" +
			"MAINEROTOR=Main elevation rotor\n" +
			"ALLROTORS=Total elevation rotors\n" +
			"ALLWEAPONS=Total weapons\n" +
            "WEAPON=Weapon\n" +
            "CUSTOM=Custom settings\n" +
			"GATLING=Gatling\n" +
			"AUTOCANON=Autocanon\n" +
			"ASSAULT=Assault canon\n" +
			"ARTY=Artillery\n" +
			"SRAILGUN=Small Railgun\n" +
			"LRAILGUN=Large Railgun\n" +
			"AIM=Auto Aim\n" +
			"AUTO= \n" +
			"TRACKING=Aim Assist\n" +
			"ASSIST=\n";


		static MyIni languageIni = new MyIni();
		static bool b = languageIni.TryParse(storage);
		public static string Translate(string language, string name)
		{
			string s = languageIni.Get(language, name).ToString("Translation error");

			return s.Replace("@", "\n");
		}
	}
}
