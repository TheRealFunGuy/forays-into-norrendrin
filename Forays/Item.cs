/*Copyright (c) 2011  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
namespace Forays{
	public class Item : PhysicalObject{
		public EffectType effect{get; private set;}
		public int quantity{get;set;}
		
		//public static Map M{get;set;} //inherited
		public static Queue Q{get;set;}
		public static Buffer B{get;set;}
		public static Actor player{get;set;}
		public Item(){
		}
		public string AName(){
			return "todo!";
		}
		public string TheName(){
			return "todo!";
		}
		public bool Use(Actor user){
			return true;
		}
	}
	public static class Weapon{
		public static int Damage(WeaponType type){
			switch(type){
			case WeaponType.SWORD:
			case WeaponType.FLAMEBRAND:
			case WeaponType.MACE:
			case WeaponType.MACE_OF_FORCE:
				return 3;
			case WeaponType.DAGGER:
			case WeaponType.VENOMOUS_DAGGER:
				return 2;
			case WeaponType.STAFF:
			case WeaponType.STAFF_OF_MAGIC:
			case WeaponType.BOW: //bow's melee damage
			case WeaponType.HOLY_LONGBOW:
				return 1;
			default:
				return 0;
			}
		}
		public static string Name(WeaponType type){
			switch(type){
			case WeaponType.SWORD:
				return "sword";
			case WeaponType.FLAMEBRAND:
				return "flamebrand";
			case WeaponType.MACE:
				return "mace";
			case WeaponType.MACE_OF_FORCE:
				return "mace of force";
			case WeaponType.DAGGER:
				return "dagger";
			case WeaponType.VENOMOUS_DAGGER:
				return "venomous dagger";
			case WeaponType.STAFF:
				return "staff";
			case WeaponType.STAFF_OF_MAGIC:
				return "staff of magic";
			case WeaponType.BOW:
				return "bow";
			case WeaponType.HOLY_LONGBOW:
				return "holy longbow";
			default:
				return "no weapon";
			}
		}
		public static colorstring StatsName(WeaponType type){
			colorstring cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.Gray;
			switch(type){
			case WeaponType.SWORD:
				cs.s = "Sword";
				break;
			case WeaponType.FLAMEBRAND:
				cs.s = "+Sword+";
				cs.color = Color.Red;
				break;
			case WeaponType.MACE:
				cs.s = "Mace";
				break;
			case WeaponType.MACE_OF_FORCE:
				cs.s = "+Mace+";
				cs.color = Color.Cyan;
				break;
			case WeaponType.DAGGER:
				cs.s = "Dagger";
				break;
			case WeaponType.VENOMOUS_DAGGER:
				cs.s = "+Dagger+";
				cs.color = Color.Green;
				break;
			case WeaponType.STAFF:
				cs.s = "Staff";
				break;
			case WeaponType.STAFF_OF_MAGIC:
				cs.s = "+Staff+";
				cs.color = Color.Magenta;
				break;
			case WeaponType.BOW:
				cs.s = "Bow";
				break;
			case WeaponType.HOLY_LONGBOW:
				cs.s = "+Bow+";
				cs.color = Color.Yellow;
				break;
			default:
				cs.s = "no weapon";
				break;
			}
			return cs;
		}
	}
	public static class Armor{
		public static int Protection(ArmorType type){
			switch(type){
			case ArmorType.LEATHER:
			case ArmorType.ELVEN_LEATHER:
				return 2;
			case ArmorType.CHAINMAIL:
			case ArmorType.CHAINMAIL_OF_ARCANA:
				return 4;
			case ArmorType.FULL_PLATE:
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				return 6;
			default:
				return 0;
			}
		}
		public static int AddedFailRate(ArmorType type){
			switch(type){
			case ArmorType.CHAINMAIL: //chainmail of arcana has no penalty
				return 5;
			case ArmorType.FULL_PLATE:
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				return 15;
			default:
				return 0;
			}
		}
		public static string Name(ArmorType type){
			switch(type){
			case ArmorType.LEATHER:
				return "leather";
			case ArmorType.ELVEN_LEATHER:
				return "elven leather";
			case ArmorType.CHAINMAIL:
				return "chainmail";
			case ArmorType.CHAINMAIL_OF_ARCANA:
				return "chainmail of arcana";
			case ArmorType.FULL_PLATE:
				return "full plate";
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				return "full plate of resistance";
			default:
				return "no armor";
			}
		}
		public static colorstring StatsName(ArmorType type){
			colorstring cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.Gray;
			switch(type){
			case ArmorType.LEATHER:
				cs.s = "Leather";
				break;
			case ArmorType.ELVEN_LEATHER:
				cs.s = "+Leather+";
				cs.color = Color.DarkCyan;
				break;
			case ArmorType.CHAINMAIL:
				cs.s = "Chainmail";
				break;
			case ArmorType.CHAINMAIL_OF_ARCANA:
				cs.s = "+Chainmail+";
				cs.color = Color.Magenta;
				break;
			case ArmorType.FULL_PLATE:
				cs.s = "Full plate";
				break;
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				cs.s = "+Full plate+";
				cs.color = Color.Blue;
				break;
			default:
				cs.s = "no armor";
				break;
			}
			return cs;
		}
	}
	public static class MagicItem{
	}
}

