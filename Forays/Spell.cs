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
			case SpellType.FORCE_PALM:
			case SpellType.DETECT_MOVEMENT:
			case SpellType.RADIANCE:
				return 1;
			case SpellType.MERCURIAL_SPHERE:
			case SpellType.GREASE:
			case SpellType.BLINK:
			case SpellType.FREEZE:
				return 2;
			case SpellType.SCORCH:
			case SpellType.LIGHTNING_BOLT:
			case SpellType.MAGIC_HAMMER:
			case SpellType.PORTAL:
				return 3;
			case SpellType.PASSAGE:
			case SpellType.GLACIAL_BLAST:
			case SpellType.AMNESIA:
			case SpellType.SHADOWSIGHT:
				return 4;
			case SpellType.BLIZZARD:
			case SpellType.COLLAPSE:
			case SpellType.FIRE_BLITZ:
			case SpellType.PLACEHOLDER:
				return 5;
			default:
				return 5;
			}
		}
		public static string Name(SpellType spell){
			switch(spell){
			case SpellType.SHINE:
				return "Shine";
			case SpellType.FORCE_PALM:
				return "Force palm";
			case SpellType.DETECT_MOVEMENT:
				return "Detect movement";
			case SpellType.RADIANCE:
				return "Radiance";
			case SpellType.MERCURIAL_SPHERE:
				return "Mercurial sphere";
			case SpellType.GREASE:
				return "Grease";
			case SpellType.BLINK:
				return "Blink";
			case SpellType.FREEZE:
				return "Freeze";
			case SpellType.SCORCH:
				return "Scorch";
			case SpellType.LIGHTNING_BOLT:
				return "Lightning bolt";
			case SpellType.MAGIC_HAMMER:
				return "Magic hammer";
			case SpellType.PORTAL:
				return "Portal";
			case SpellType.PASSAGE:
				return "Passage";
			case SpellType.GLACIAL_BLAST:
				return "Glacial blast";
			case SpellType.AMNESIA:
				return "Amnesia";
			case SpellType.SHADOWSIGHT:
				return "Shadowsight";
			case SpellType.BLIZZARD:
				return "Blizzard";
			case SpellType.COLLAPSE:
				return "Collapse";
			case SpellType.FIRE_BLITZ:
				return "Fire blitz";
			case SpellType.PLACEHOLDER:
				return "PLACEHOLDER";
			default:
				return "unknown spell";
			}
		}
		public static bool IsDamaging(SpellType spell){
			switch(spell){
			case SpellType.BLIZZARD:
			case SpellType.COLLAPSE:
			case SpellType.FIRE_BLITZ:
			case SpellType.FORCE_PALM:
			case SpellType.GLACIAL_BLAST:
			case SpellType.LIGHTNING_BOLT:
			case SpellType.MAGIC_HAMMER:
			case SpellType.SCORCH:
			case SpellType.MERCURIAL_SPHERE:
			case SpellType.RADIANCE:
			case SpellType.PLACEHOLDER: //todo!
				return true;
			}
			return false;
		}
		public static colorstring Description(SpellType spell){
			switch(spell){
			case SpellType.SHINE:
				return new colorstring("  Doubles your torch's radius     ",Color.Gray);
			case SpellType.FORCE_PALM:
				return new colorstring("  1d6 damage, range 1, knockback  ",Color.Gray);
			case SpellType.DETECT_MOVEMENT:
				return new colorstring("  PLACEHOLDER TODO                ",Color.Gray);
			case SpellType.RADIANCE:
				return new colorstring("  PLACEHOLDER TODO                ",Color.Gray);
			case SpellType.MERCURIAL_SPHERE:
				return new colorstring("  PLACEHOLDER TODO                ",Color.Gray);
			case SpellType.GREASE:
				return new colorstring("  PLACEHOLDER TODO                ",Color.Gray);
			case SpellType.BLINK:
				return new colorstring("  Teleports you a short distance  ",Color.Gray);
			case SpellType.FREEZE:
				return new colorstring("  Encases an enemy in ice         ",Color.Gray);
			case SpellType.SCORCH:
				return new colorstring("  2d6 fire damage, ranged         ",Color.Gray); //todo change ALL the descriptions!
			case SpellType.LIGHTNING_BOLT:
				return new colorstring("  2d6 electric, leaps between foes",Color.Gray);
			case SpellType.MAGIC_HAMMER:
				return new colorstring("  4d6 damage, range 1, stun       ",Color.Gray);
			case SpellType.PORTAL:
				return new colorstring("  PLACEHOLDER TODO                ",Color.Gray);
			case SpellType.PASSAGE:
				return new colorstring("  Move to the other side of a wall",Color.Gray);
			case SpellType.GLACIAL_BLAST:
				return new colorstring("  3d6 cold damage, ranged         ",Color.Gray);
			case SpellType.AMNESIA:
				return new colorstring("  An enemy forgets your presence  ",Color.Gray);
			case SpellType.SHADOWSIGHT:
				return new colorstring("  Grants better vision in the dark",Color.Gray);
			case SpellType.BLIZZARD:
				return new colorstring("  5d6 radius 5 burst, freezes foes",Color.Gray);
			case SpellType.FIRE_BLITZ:
				return new colorstring("  placeholder todo                ",Color.Gray);
			case SpellType.COLLAPSE:
				return new colorstring("  4d6, breaks walls, leaves rubble",Color.Gray);
			case SpellType.PLACEHOLDER:
				return new colorstring("  PLACEHOLDER TODO                ",Color.Gray);
			default:
				return new colorstring("  Unknown.                        ",Color.Gray);
			}
		}
		public static colorstring DescriptionWithIncreasedDamage(SpellType spell){
			switch(spell){
			case SpellType.FORCE_PALM:
				return new colorstring("  2d6",Color.Yellow," damage, range 1, knockback  ",Color.Gray); //todo!
			case SpellType.SCORCH:
				return new colorstring("  3d6",Color.Yellow," fire damage, ranged         ",Color.Gray);
			case SpellType.LIGHTNING_BOLT:
				return new colorstring("  3d6",Color.Yellow," electric, leaps between foes",Color.Gray);
			case SpellType.MAGIC_HAMMER:
				return new colorstring("  5d6",Color.Yellow," damage, range 1, stun       ",Color.Gray);
			case SpellType.GLACIAL_BLAST:
				return new colorstring("  4d6",Color.Yellow," cold damage, ranged         ",Color.Gray);
			case SpellType.COLLAPSE:
				return new colorstring("  5d6",Color.Yellow,", breaks walls, leaves rubble",Color.Gray);
			case SpellType.FIRE_BLITZ:
				return new colorstring("  Three ",Color.Gray,"2d6",Color.Yellow," beams knock foes back ",Color.Gray);
			case SpellType.BLIZZARD:
				return new colorstring("  6d6",Color.Yellow," radius 5 burst, freezes foes",Color.Gray);
			default:
				return Description(spell);
			}
		}
	}
}

