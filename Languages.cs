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
			"SETTINGS=Настройки\n" +
			"SIMPLE=Базовые\n" +
			"ADVANCED=Расширенные\n" +
			"RADAR=Радар\n" +
			"COCKPIT=Кокпит\n" +
			"WEAPON=Орудие\n" +
			"SCOCKPIT=Малый@кокпит\n" +
			"FCOCKPIT=Кокпит@Истребителя\n" +
			"SCSEAT=Малое кресло@пилота\n" +
			"LCOCKPIT=Большой@кокпит\n" +
			"CSEAT=Кресло@пилота\n" +
			"CUSTOM=Кастомный\n" +
			"GUTLING=Пулемет\n" +
			"AUTOCANON=Автопушка\n" +
			"ASSAULT=Штурмовая\n" +
			"ARTY=Артиллерия\n" +
			"SRAILGUN=РельсаМ\n" +
			"LRAILGUN=РельсаБ\n" +
			"ENEMY=Враждебные\n" +
			"NEUTRAL=Нейтральные\n" +
			"ALLIE=Союзные\n" +
            "AIM=Наведение\n" +
            "AUTO=Автоматическое\n" +
			"TRACKING=Помощь в\n" +
            "ASSIST=наведении\n" +
            "PROJVEL=Cкорость@снаряда\n" +
			"SHOTRANGE=Дальность@выстрела\n" +
            "TARGETINGPOINT=Точка@прицеливания\n" +
            "INITIALRANGE=Дальность@поиска\n" +
			"POINT0=Точка@захвата\n" +
            "POINT1=Центр@Цели\n" +
            "POINT2=Реальный@захват\n" +

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
			"SETTINGS=Settings\n" +
			"SIMPLE=Simple\n" +
			"ADVANCED=Advanced\n" +
            "RADAR=Radar\n" +
			"COCKPIT=Cockpit\n" +
			"WEAPON=Weapon\n" +
			"SCOCKPIT=Small@cockpit\n" +
			"FCOCKPIT=Fighter@cockpit\n" +
			"SCSEAT=Small control@seat\n" +
			"LCOCKPIT=Large@cockpit\n" +
			"CSEAT=Control@seat\n" +
			"CUSTOM=Custom\n" +
			"GUTLING=Gutling\n" +
			"AUTOCANON=Autocanon\n" +
			"ASSAULT=Assault\n" +
			"ARTY=Artillery\n" +
			"SRAILGUN=S Railgun\n" +
			"LRAILGUN=L RAILGUN\n" +
			"ENEMY=Enemy\n" +
			"NEUTRAL=Neutral\n" +
			"ALLIE=Allie\n" +
            "AIM=Auto Aim\n" +
			"AUTO= \n" +
            "TRACKING=Aim Assist\n" +
            "ASSIST=\n" +
            "PROJVEL=Projectile@velocity\n" +
			"SHOTRANGE=Shoot@range\n" +
            "TARGETINGPOINT=Aiming@point\n" +
            "INITIALRANGE=Lock@Range\n" +
            "POINT0=First@Lock\n" +
            "POINT1=Center of@Target\n" +
            "POINT2=Real@lock\n";


		static MyIni languageIni = new MyIni();
		static bool b = languageIni.TryParse(storage);
		public static string Translate(string language, string name)
		{
			string s = languageIni.Get(language, name).ToString("Translation error");

			return s.Replace("@", "\n");
		}
	}
}
