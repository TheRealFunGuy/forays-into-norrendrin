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
	}
	public static class MagicItem{
	}
}

