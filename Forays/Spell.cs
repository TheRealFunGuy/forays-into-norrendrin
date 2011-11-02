using System;
namespace Forays{
	public static class Spell{
		public static int Level(SpellType spell){
			switch(spell){
			case SpellType.SHINE:
				return 5;
			case SpellType.MAGIC_MISSILE:
				return 10;
			case SpellType.DETECT_MONSTERS:
				return 10;
			case SpellType.FORCE_PALM:
				return 15;
			case SpellType.BLINK:
				return 20;
			case SpellType.IMMOLATE:
				return 20;
			case SpellType.ICY_BLAST:
				return 25;
			case SpellType.BURNING_HANDS:
				return 25;
			case SpellType.FREEZE:
				return 30;
			case SpellType.SONIC_BOOM:
				return 35;
			case SpellType.ARC_LIGHTNING:
				return 40;
			case SpellType.PLACEHOLDER:
				return 42;
			case SpellType.SHOCK:
				return 45;
			case SpellType.SHADOWSIGHT:
				return 50;
			case SpellType.RETREAT:
				return 55;
			case SpellType.FIREBALL:
				return 60;
			case SpellType.PASSAGE:
				return 60;
			case SpellType.FORCE_BEAM:
				return 75;
			case SpellType.DISINTEGRATE:
				return 90;
			case SpellType.BLIZZARD:
				return 100;
			case SpellType.BLESS:
				return 15;
			case SpellType.MINOR_HEAL:
				return 35;
			case SpellType.HOLY_SHIELD:
				return 45;
			default:
				return 100;
			}
		}
		public static string Name(SpellType spell){
			switch(spell){
			case SpellType.SHINE:
				return "Shine";
			case SpellType.MAGIC_MISSILE:
				return "Magic missile";
			case SpellType.DETECT_MONSTERS:
				return "Detect monsters";
			case SpellType.FORCE_PALM:
				return "Force palm";
			case SpellType.BLINK:
				return "Blink";
			case SpellType.IMMOLATE:
				return "Immolate";
			case SpellType.ICY_BLAST:
				return "Icy blast";
			case SpellType.BURNING_HANDS:
				return "Burning hands";
			case SpellType.FREEZE:
				return "Freeze";
			case SpellType.SONIC_BOOM:
				return "Sonic boom";
			case SpellType.ARC_LIGHTNING:
				return "Arc lightning";
			case SpellType.PLACEHOLDER:
				return "Placeholder";
			case SpellType.SHOCK:
				return "Shock";
			case SpellType.SHADOWSIGHT:
				return "Shadowsight";
			case SpellType.RETREAT:
				return "Retreat";
			case SpellType.FIREBALL:
				return "Fireball";
			case SpellType.PASSAGE:
				return "Passage";
			case SpellType.FORCE_BEAM:
				return "Force beam";
			case SpellType.DISINTEGRATE:
				return "Disintegrate";
			case SpellType.BLIZZARD:
				return "Blizzard";
			case SpellType.BLESS:
				return "Bless";
			case SpellType.MINOR_HEAL:
				return "Minor heal";
			case SpellType.HOLY_SHIELD:
				return "Holy shield";
			default:
				return "an unknown spell";
			}
		}
	}
}

