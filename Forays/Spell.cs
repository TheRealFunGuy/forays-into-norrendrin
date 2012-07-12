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
			case SpellType.IMMOLATE:
			case SpellType.FORCE_PALM:
				return 2;
			case SpellType.FREEZE:
				return 3;
			case SpellType.BLINK:
			case SpellType.SCORCH:
				return 4;
			case SpellType.BLOODSCENT:
				return 5;
			case SpellType.LIGHTNING_BOLT:
				return 6;
			case SpellType.SHADOWSIGHT:
			case SpellType.VOLTAIC_SURGE:
				return 7;
			case SpellType.MAGIC_HAMMER:
				return 8;
			case SpellType.RETREAT:
				return 9;
			case SpellType.GLACIAL_BLAST:
				return 10;
			case SpellType.PASSAGE:
				return 11;
			case SpellType.FLASHFIRE:
				return 13;
			case SpellType.SONIC_BOOM:
				return 15;
			case SpellType.COLLAPSE:
				return 16;
			case SpellType.FORCE_BEAM:
				return 17;
			case SpellType.AMNESIA:
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
			case SpellType.SCORCH:
				return "Scorch";
			case SpellType.BLOODSCENT:
				return "Bloodscent";
			case SpellType.LIGHTNING_BOLT:
				return "Lightning bolt";
			case SpellType.VOLTAIC_SURGE:
				return "Voltaic surge";
			case SpellType.MAGIC_HAMMER:
				return "Magic hammer";
			case SpellType.GLACIAL_BLAST:
				return "Glacial blast";
			case SpellType.FLASHFIRE:
				return "Flashfire";
			case SpellType.COLLAPSE:
				return "Collapse";
			case SpellType.AMNESIA:
				return "Amnesia";
			case SpellType.SHINE:
				return "Shine";
			case SpellType.SONIC_BOOM:
				return "Sonic boom";
			case SpellType.FORCE_PALM:
				return "Force palm";
			case SpellType.BLINK:
				return "Blink";
			case SpellType.IMMOLATE:
				return "Immolate";
			case SpellType.FREEZE:
				return "Freeze";
			case SpellType.SHADOWSIGHT:
				return "Shadowsight";
			case SpellType.RETREAT:
				return "Retreat";
			case SpellType.PASSAGE:
				return "Passage";
			case SpellType.FORCE_BEAM:
				return "Force beam";
			case SpellType.BLIZZARD:
				return "Blizzard";
			case SpellType.BLESS:
				return "Bless";
			case SpellType.MINOR_HEAL:
				return "Minor heal";
			case SpellType.HOLY_SHIELD:
				return "Holy shield";
			default:
				return "unknown spell";
			}
		}
		public static string Description(SpellType spell){
			switch(spell){
			case SpellType.SHINE:
				return "Doubles your torch's radius     ";
			case SpellType.IMMOLATE:
				return "Throws flame to ignite an enemy ";
			case SpellType.FORCE_PALM:
				return "1d6 damage, range 1, knockback  ";
			case SpellType.FREEZE:
				return "Encases an enemy in ice         ";
			case SpellType.BLINK:
				return "Teleports you a short distance  ";
			case SpellType.SCORCH:
				return "2d6 fire damage, ranged         ";
			case SpellType.BLOODSCENT:
				return "Tracks one nearby living enemy  ";
			case SpellType.LIGHTNING_BOLT:
				return "2d6 electric, leaps between foes";
			case SpellType.SHADOWSIGHT:
				return "Grants better vision in the dark";
			case SpellType.VOLTAIC_SURGE:
				return "3d6 electric, radius 2 burst    ";
			case SpellType.MAGIC_HAMMER:
				return "4d6 damage, range 1, stun       ";
			case SpellType.RETREAT:
				return "Marks a spot, then returns to it";
			case SpellType.GLACIAL_BLAST:
				return "3d6 cold damage, ranged         ";
			case SpellType.PASSAGE:
				return "Move to the other side of a wall";
			case SpellType.FLASHFIRE:
				return "3d6 fire damage, ranged radius 2";
			case SpellType.SONIC_BOOM:
				return "3d6 magic damage, can stun foes ";
			case SpellType.COLLAPSE:
				return "4d6, breaks walls, leaves rubble";
			case SpellType.FORCE_BEAM:
				return "Three 1d6 beams knock foes back ";
			case SpellType.AMNESIA:
				return "An enemy forgets your presence  ";
			case SpellType.BLIZZARD:
				return "5d6 radius 5 burst, freezes foes";
			case SpellType.BLESS:
				return "Increases Combat skill briefly  ";
			case SpellType.MINOR_HEAL:
				return "Heals 4d6 damage                ";
			case SpellType.HOLY_SHIELD:
				return "Attackers take 2d6 magic damage ";
			default:
				return "Unknown.                        ";
			}
		}
	}
}

