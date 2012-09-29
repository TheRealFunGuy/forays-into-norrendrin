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
		public static bool IsDamaging(SpellType spell){
			switch(spell){
			case SpellType.BLIZZARD:
			case SpellType.COLLAPSE:
			case SpellType.FLASHFIRE:
			case SpellType.FORCE_BEAM:
			case SpellType.FORCE_PALM:
			case SpellType.GLACIAL_BLAST:
			case SpellType.LIGHTNING_BOLT:
			case SpellType.MAGIC_HAMMER:
			case SpellType.SCORCH:
			case SpellType.SONIC_BOOM:
			case SpellType.VOLTAIC_SURGE:
				return true;
			}
			return false;
		}
		public static colorstring Description(SpellType spell){
			switch(spell){
			case SpellType.SHINE:
				return new colorstring("  Doubles your torch's radius     ",Color.Gray);
			case SpellType.IMMOLATE:
				return new colorstring("  Throws flame to ignite an enemy ",Color.Gray);
			case SpellType.FORCE_PALM:
				return new colorstring("  1d6 damage, range 1, knockback  ",Color.Gray);
			case SpellType.FREEZE:
				return new colorstring("  Encases an enemy in ice         ",Color.Gray);
			case SpellType.BLINK:
				return new colorstring("  Teleports you a short distance  ",Color.Gray);
			case SpellType.SCORCH:
				return new colorstring("  2d6 fire damage, ranged         ",Color.Gray);
			case SpellType.BLOODSCENT:
				return new colorstring("  Tracks one nearby living enemy  ",Color.Gray);
			case SpellType.LIGHTNING_BOLT:
				return new colorstring("  2d6 electric, leaps between foes",Color.Gray);
			case SpellType.SHADOWSIGHT:
				return new colorstring("  Grants better vision in the dark",Color.Gray);
			case SpellType.VOLTAIC_SURGE:
				return new colorstring("  3d6 electric, radius 2 burst    ",Color.Gray);
			case SpellType.MAGIC_HAMMER:
				return new colorstring("  4d6 damage, range 1, stun       ",Color.Gray);
			case SpellType.RETREAT:
				return new colorstring("  Marks a spot, then returns to it",Color.Gray);
			case SpellType.GLACIAL_BLAST:
				return new colorstring("  3d6 cold damage, ranged         ",Color.Gray);
			case SpellType.PASSAGE:
				return new colorstring("  Move to the other side of a wall",Color.Gray);
			case SpellType.FLASHFIRE:
				return new colorstring("  3d6 fire damage, ranged radius 2",Color.Gray);
			case SpellType.SONIC_BOOM:
				return new colorstring("  3d6 magic damage, can stun foes ",Color.Gray);
			case SpellType.COLLAPSE:
				return new colorstring("  4d6, breaks walls, leaves rubble",Color.Gray);
			case SpellType.FORCE_BEAM:
				return new colorstring("  Three 1d6 beams knock foes back ",Color.Gray);
			case SpellType.AMNESIA:
				return new colorstring("  An enemy forgets your presence  ",Color.Gray);
			case SpellType.BLIZZARD:
				return new colorstring("  5d6 radius 5 burst, freezes foes",Color.Gray);
			case SpellType.BLESS:
				return new colorstring("  Increases Combat skill briefly  ",Color.Gray);
			case SpellType.MINOR_HEAL:
				return new colorstring("  Heals 4d6 damage                ",Color.Gray);
			case SpellType.HOLY_SHIELD:
				return new colorstring("  Attackers take 2d6 magic damage ",Color.Gray);
			default:
				return new colorstring("  Unknown.                        ",Color.Gray);
			}
		}
		public static colorstring DescriptionWithIncreasedDamage(SpellType spell){
			switch(spell){
			case SpellType.FORCE_PALM:
				return new colorstring("  2d6",Color.Yellow," damage, range 1, knockback  ",Color.Gray);
			case SpellType.SCORCH:
				return new colorstring("  3d6",Color.Yellow," fire damage, ranged         ",Color.Gray);
			case SpellType.LIGHTNING_BOLT:
				return new colorstring("  3d6",Color.Yellow," electric, leaps between foes",Color.Gray);
			case SpellType.VOLTAIC_SURGE:
				return new colorstring("  4d6",Color.Yellow," electric, radius 2 burst    ",Color.Gray);
			case SpellType.MAGIC_HAMMER:
				return new colorstring("  5d6",Color.Yellow," damage, range 1, stun       ",Color.Gray);
			case SpellType.GLACIAL_BLAST:
				return new colorstring("  4d6",Color.Yellow," cold damage, ranged         ",Color.Gray);
			case SpellType.FLASHFIRE:
				return new colorstring("  4d6",Color.Yellow," fire damage, ranged radius 2",Color.Gray);
			case SpellType.SONIC_BOOM:
				return new colorstring("  4d6",Color.Yellow," magic damage, can stun foes ",Color.Gray);
			case SpellType.COLLAPSE:
				return new colorstring("  5d6",Color.Yellow,", breaks walls, leaves rubble",Color.Gray);
			case SpellType.FORCE_BEAM:
				return new colorstring("  Three ",Color.Gray,"2d6",Color.Yellow," beams knock foes back ",Color.Gray);
			case SpellType.BLIZZARD:
				return new colorstring("  6d6",Color.Yellow," radius 5 burst, freezes foes",Color.Gray);
			default:
				return Description(spell);
			}
		}
	}
}

