/*Copyright (c) 2011-2012  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
namespace Forays{
	public static class Spell{
		public static int Level(SpellType spell){
			switch(spell){
			case SpellType.SHINE:
				return 1;
			case SpellType.MAGIC_MISSILE:
				return 2;
			case SpellType.DETECT_MONSTERS:
				return 2;
			case SpellType.FORCE_PALM:
				return 3;
			case SpellType.BLINK:
				return 4;
			case SpellType.IMMOLATE:
				return 4;
			case SpellType.SHOCK:
				return 5;
			case SpellType.BURNING_HANDS:
				return 5;
			case SpellType.FREEZE:
				return 6;
			case SpellType.NIMBUS:
				return 6;
			case SpellType.SONIC_BOOM:
				return 7;
			case SpellType.ARC_LIGHTNING:
				return 8;
			case SpellType.ICY_BLAST:
				return 9;
			case SpellType.SHADOWSIGHT:
				return 10;
			case SpellType.RETREAT:
				return 11;
			case SpellType.FIREBALL:
				return 12;
			case SpellType.PASSAGE:
				return 12;
			case SpellType.FORCE_BEAM:
				return 15;
			case SpellType.DISINTEGRATE:
				return 18;
			case SpellType.BLIZZARD:
				return 20;
			case SpellType.BLESS:
				return 3;
			case SpellType.MINOR_HEAL:
				return 7;
			case SpellType.HOLY_SHIELD:
				return 9;
			default:
				return 20;
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
			case SpellType.SHOCK:
				return "Shock";
			case SpellType.BURNING_HANDS:
				return "Burning hands";
			case SpellType.FREEZE:
				return "Freeze";
			case SpellType.NIMBUS:
				return "Nimbus";
			case SpellType.SONIC_BOOM:
				return "Sonic boom";
			case SpellType.ARC_LIGHTNING:
				return "Arc lightning";
			case SpellType.ICY_BLAST:
				return "Icy blast";
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
		public static string Description(SpellType spell){
			switch(spell){
			case SpellType.SHINE:
				return "Light radius is doubled.";
			case SpellType.MAGIC_MISSILE:
				return "Basic offensive spell.  ";
			case SpellType.DETECT_MONSTERS:
				return "Sense nearby enemies.   ";
			case SpellType.FORCE_PALM:
				return "Can knock foes back.    ";
			case SpellType.BLINK:
				return "Teleport a few spaces.  ";
			case SpellType.IMMOLATE:
				return "Ignites an enemy.       ";
			case SpellType.SHOCK:
				return "Electric attack spell.  ";
			case SpellType.BURNING_HANDS:
				return "Range 1 attack spell.   ";
			case SpellType.FREEZE:
				return "Immobilizes an enemy.   ";
			case SpellType.NIMBUS:
				return "Range 1 damage each turn";
			case SpellType.SONIC_BOOM:
				return "Can stun an enemy.      ";
			case SpellType.ARC_LIGHTNING:
				return "Radius 1 attack spell.  ";
			case SpellType.ICY_BLAST:
				return "Cold attack spell.      ";
			case SpellType.SHADOWSIGHT:
				return "Grants darkvision.      ";
			case SpellType.RETREAT:
				return "Return to a safe place. ";
			case SpellType.FIREBALL:
				return "Ranged radius 2 spell.  ";
			case SpellType.PASSAGE:
				return "Move through a wall.    ";
			case SpellType.FORCE_BEAM:
				return "Knocks foes back.       ";
			case SpellType.DISINTEGRATE:
				return "Can destroy terrain.    ";
			case SpellType.BLIZZARD:
				return "Radius 5. Immobilizes.  ";
			case SpellType.BLESS:
				return "Increases Combat skill. ";
			case SpellType.MINOR_HEAL:
				return "Heals minor wounds.     ";
			case SpellType.HOLY_SHIELD:
				return "Attackers take damage.  ";
			default:
				return "Unknown.                ";
			}
		}
		public static string Damage(SpellType spell){
			switch(spell){
			case SpellType.MAGIC_MISSILE:
			case SpellType.FORCE_PALM:
			case SpellType.IMMOLATE:
			case SpellType.FREEZE:
			case SpellType.NIMBUS:
				return "1d6";
			case SpellType.SHOCK:
			case SpellType.SONIC_BOOM:
				return "2d6";
			case SpellType.BURNING_HANDS:
			case SpellType.ARC_LIGHTNING:
			case SpellType.ICY_BLAST:
			case SpellType.FIREBALL:
			case SpellType.FORCE_BEAM:
				return "3d6";
			case SpellType.DISINTEGRATE:
				return "8d6";
			case SpellType.BLIZZARD:
				return "5d6";
			default:
				return "   ";
			}
		}
	}
}

