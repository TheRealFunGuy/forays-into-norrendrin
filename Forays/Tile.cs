using System;
using System.Collections.Generic;
namespace Forays{
	public class Tile : PhysicalObject{
		public TileType type{get;set;}
		public bool passable{get;set;}
		public bool opaque{get;set;}
		public bool seen{get;set;}
		public int light_value{get;set;}
		public TileType? toggles_into;
		public Item inv{get;set;}
		
		private static Dictionary<TileType,Tile> proto= new Dictionary<TileType, Tile>();
		public static Tile Prototype(TileType type){ return proto[type]; }
		private static int ROWS = Global.ROWS;
		private static int COLS = Global.COLS;
		//public static Map M{get;set;} //inherited
		public static Buffer B{get;set;} //needed here?
		public static Actor player{get;set;}
		static Tile(){
			proto[TileType.FLOOR] = new Tile(TileType.FLOOR,"floor",'.',Color.White,true,false,null);
			proto[TileType.WALL] = new Tile(TileType.WALL,"wall",'#',Color.Gray,false,true,null);
			proto[TileType.DOOR_C] = new Tile(TileType.DOOR_C,"closed door",'+',Color.DarkYellow,false,true,TileType.DOOR_O);
			proto[TileType.DOOR_O] = new Tile(TileType.DOOR_O,"open door",'-',Color.DarkYellow,true,false,TileType.DOOR_C);
			proto[TileType.STAIRS] = new Tile(TileType.STAIRS,"stairway",'>',Color.White,true,false,null);
			proto[TileType.CHEST] = new Tile(TileType.CHEST,"treasure chest",'~',Color.Yellow,true,false,null);
			proto[TileType.FIREPIT] = new Tile(TileType.FIREPIT,"fire pit",'0',Color.Red,true,false,null);
			proto[TileType.STALAGMITE] = new Tile(TileType.STALAGMITE,"stalagmite",'^',Color.White,false,true,TileType.FLOOR);
			proto[TileType.GRENADE] = new Tile(TileType.GRENADE,"SPECIAL",',',Color.Red,true,false,null); //special treatment
			//trap ideas: quickfire trap: burst of fire that ignites stuff, then expands(like quickfire) for several turns.
				//you'll probably have to run while on fire, instead of putting it out
			//not an actual trap, but room mimic will be awesome.
			//also not an actual trap, but arena rooms will be cool too. perhaps you'll see the opponent, in stasis.
				//"Touch the [tile]?(Y/N) "   if you touch it, you're stuck in the arena until one of you dies.
			//stun trap. much less nasty than paralysis or even confusion.
			//definitely need braziers with radius 1 light
			//orcish grenade trap. drops 1d2 grenades up to 1 tile away.
		}
		public Tile(Tile t,int r,int c){
			type = t.type;
			name = t.name;
			a_name = t.a_name;
			the_name = t.the_name;
			symbol = t.symbol;
			color = t.color;
			passable = t.passable;
			opaque = t.opaque;
			seen = false;
			light_value = 0;
			toggles_into = t.toggles_into;
			inv = null;
			row = r;
			col = c;
		}
		public Tile(TileType type_,string name_,char symbol_,Color color_,bool passable_,bool opaque_,TileType? toggles_into_){
			type = type_;
			name = name_;
			the_name = "the " + name;
			switch(name[0]){
			case 'a':
			case 'e':
			case 'i':
			case 'o':
			case 'u':
			case 'A':
			case 'E':
			case 'I':
			case 'O':
			case 'U':
				a_name = "an " + name;
				break;
			default:
				a_name = "a " + name;
				break;
			}
			symbol = symbol_;
			color = color_;
			passable = passable_;
			opaque = opaque_;
			seen = false;
			light_value = 0;
			toggles_into = toggles_into_;
			inv = null;
		}
		public override string ToString(){
			switch(type){
			case TileType.FLOOR:
				return ".";
			case TileType.WALL:
				return "#";
			case TileType.DOOR_C:
				return "+";
			case TileType.DOOR_O:
				return "-";
			case TileType.STAIRS:
				return ">";
			case TileType.CHEST:
				return "~";
			case TileType.FIREPIT:
				return "0";
			case TileType.GRENADE:
				return ",";
			default:
				return ".";
			}
		}
		public static Tile Create(TileType type,int r,int c){
			Tile t = null;
			if(M.tile[r,c] == null){
				t = new Tile(proto[type],r,c);
				M.tile[r,c] = t; //bounds checking here?
			}
			return t;
		}
		public void Toggle(PhysicalObject toggler){
			if(toggles_into != null){
				Toggle(toggler,toggles_into.Value);
			}
		}
		public void Toggle(PhysicalObject toggler,TileType toggle_to){
			bool lighting_update = false; //todo: when a mob opens a seen door, it'll be visible, so add
			List<Actor> actors = new List<Actor>(); // a message: "You hear a door opening. " - and that should be enough!
			for(int i=row-1;i<=row+1;++i){
				for(int j=col-1;j<=col+1;++j){
					if(M.tile[i,j].IsLit()){
						lighting_update = true;
					}
				}
			}
			if(lighting_update){
				for(int i=row-Global.MAX_LIGHT_RADIUS;i<=row+Global.MAX_LIGHT_RADIUS;++i){
					for(int j=col-Global.MAX_LIGHT_RADIUS;j<=col+Global.MAX_LIGHT_RADIUS;++j){
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(M.actor[i,j] != null && M.actor[i,j].light_radius > 0){
								actors.Add(M.actor[i,j]);
								M.actor[i,j].UpdateRadius(M.actor[i,j].light_radius,0);
							}
						}
					}
				}
			}

			TransformTo(toggle_to);

			if(lighting_update){
				foreach(Actor a in actors){
					a.UpdateRadius(0,a.light_radius);
				}
			}
		}
		public void TransformTo(TileType type_){
			name=Prototype(type_).name;
			a_name=Prototype(type_).a_name;
			the_name=Prototype(type_).the_name;
			symbol=Prototype(type_).symbol;
			color=Prototype(type_).color;
			type=Prototype(type_).type;
			passable=Prototype(type_).passable;
			opaque=Prototype(type_).opaque;
			toggles_into=Prototype(type_).toggles_into;
			if(opaque){
				light_value = 0;
			}
		}
		public void TurnToFloor(){
			bool lighting_update = false;
			List<Actor> actors = new List<Actor>();
			if(opaque){
				for(int i=row-1;i<=row+1;++i){
					for(int j=col-1;j<=col+1;++j){
						if(M.tile[i,j].IsLit()){
							lighting_update = true;
						}
					}
				}
			}
			if(lighting_update){
				for(int i=row-Global.MAX_LIGHT_RADIUS;i<=row+Global.MAX_LIGHT_RADIUS;++i){
					for(int j=col-Global.MAX_LIGHT_RADIUS;j<=col+Global.MAX_LIGHT_RADIUS;++j){
						if(M.BoundsCheck(i,j)){
							if(M.actor[i,j] != null && M.actor[i,j].light_radius > 0){
								actors.Add(M.actor[i,j]);
								M.actor[i,j].UpdateRadius(M.actor[i,j].light_radius,0);
							}
						}
					}
				}
			}
			
			TransformTo(TileType.FLOOR); //todo: recalculate pathing? what else?
			
			if(lighting_update){
				foreach(Actor a in actors){
					a.UpdateRadius(0,a.light_radius);
				}
			}
		}
		public void OpenChest(){
			if(type == TileType.CHEST){
				if(Global.Roll(1,10) == 10){
					List<int> upgrades = new List<int>();
					if(Global.Roll(1,2) == 2 && !player.weapons.Contains(WeaponType.FLAMEBRAND)){
						upgrades.Add(0);
					}
					if(Global.Roll(1,2) == 2 && !player.weapons.Contains(WeaponType.MACE_OF_FORCE)){
						upgrades.Add(1);
					}
					if(Global.Roll(1,2) == 2 && !player.weapons.Contains(WeaponType.VENOMOUS_DAGGER)){
						upgrades.Add(2);
					}
					if(Global.Roll(1,2) == 2 && !player.weapons.Contains(WeaponType.STAFF_OF_MAGIC)){
						upgrades.Add(3);
					}
					if(Global.Roll(1,2) == 2 && !player.weapons.Contains(WeaponType.HOLY_LONGBOW)){
						upgrades.Add(4);
					}
					if(Global.Roll(1,3) == 3 && !player.armors.Contains(ArmorType.ELVEN_LEATHER)){
						upgrades.Add(5);
					}
					if(Global.Roll(1,3) == 3 && !player.armors.Contains(ArmorType.CHAINMAIL_OF_ARCANA)){
						upgrades.Add(6);
					}
					if(Global.Roll(1,3) == 3 && !player.armors.Contains(ArmorType.FULL_PLATE_OF_RESISTANCE)){
						upgrades.Add(7);
					}
					if(Global.Roll(1,2) == 2 && !player.magic_items.Contains(MagicItemType.PENDANT_OF_LIFE)){
						upgrades.Add(8);
					}
					if(Global.Roll(1,3) == 3 && !player.magic_items.Contains(MagicItemType.RING_OF_RESISTANCE)){
						upgrades.Add(9);
					}
					if(Global.Roll(1,2) == 2 && !player.magic_items.Contains(MagicItemType.RING_OF_PROTECTION)){
						upgrades.Add(10);
					}
					if(Global.Roll(1,2) == 2 && !player.magic_items.Contains(MagicItemType.CLOAK_OF_DISAPPEARANCE)){
						upgrades.Add(11);
					}
					if(upgrades.Count == 0){
						OpenChest();
						return;
					}
					int upgrade = upgrades[Global.Roll(1,upgrades.Count)-1];
					switch(upgrade){
					case 0: //flamebrand
						player.weapons.Find(WeaponType.SWORD).Value = WeaponType.FLAMEBRAND;
						if(Weapon.BaseWeapon(player.weapons.First.Value) == WeaponType.SWORD){
							player.UpdateOnEquip(WeaponType.SWORD,WeaponType.FLAMEBRAND);
						}
						break;
					case 1: //mace of force
						player.weapons.Find(WeaponType.MACE).Value = WeaponType.MACE_OF_FORCE;
						if(Weapon.BaseWeapon(player.weapons.First.Value) == WeaponType.MACE){
							player.UpdateOnEquip(WeaponType.MACE,WeaponType.MACE_OF_FORCE);
						}
						break;
					case 2: //venomous dagger
						player.weapons.Find(WeaponType.DAGGER).Value = WeaponType.VENOMOUS_DAGGER;
						if(Weapon.BaseWeapon(player.weapons.First.Value) == WeaponType.DAGGER){
							player.UpdateOnEquip(WeaponType.DAGGER,WeaponType.VENOMOUS_DAGGER);
						}
						break;
					case 3: //staff of magic
						player.weapons.Find(WeaponType.STAFF).Value = WeaponType.STAFF_OF_MAGIC;
						if(Weapon.BaseWeapon(player.weapons.First.Value) == WeaponType.STAFF){
							player.UpdateOnEquip(WeaponType.STAFF,WeaponType.STAFF_OF_MAGIC);
						}
						break;
					case 4: //holy longbow
						player.weapons.Find(WeaponType.BOW).Value = WeaponType.HOLY_LONGBOW;
						if(Weapon.BaseWeapon(player.weapons.First.Value) == WeaponType.BOW){
							player.UpdateOnEquip(WeaponType.BOW,WeaponType.HOLY_LONGBOW);
						}
						break;
					case 5: //elven leather
						player.armors.Find(ArmorType.LEATHER).Value = ArmorType.ELVEN_LEATHER;
						if(Armor.BaseArmor(player.armors.First.Value) == ArmorType.LEATHER){
							player.UpdateOnEquip(ArmorType.LEATHER,ArmorType.ELVEN_LEATHER);
						}
						break;
					case 6: //chainmail of arcana
						player.armors.Find(ArmorType.CHAINMAIL).Value = ArmorType.CHAINMAIL_OF_ARCANA;
						if(Armor.BaseArmor(player.armors.First.Value) == ArmorType.CHAINMAIL){
							player.UpdateOnEquip(ArmorType.CHAINMAIL,ArmorType.CHAINMAIL_OF_ARCANA);
						}
						break;
					case 7: //full plate of resistance
						player.armors.Find(ArmorType.FULL_PLATE).Value = ArmorType.FULL_PLATE_OF_RESISTANCE;
						if(Armor.BaseArmor(player.armors.First.Value) == ArmorType.FULL_PLATE){
							player.UpdateOnEquip(ArmorType.FULL_PLATE,ArmorType.FULL_PLATE_OF_RESISTANCE);
						}
						break;
					case 8: //pendant of life
						player.magic_items.AddLast(MagicItemType.PENDANT_OF_LIFE);
						break;
					case 9: //ring of resistance
						player.magic_items.AddLast(MagicItemType.RING_OF_RESISTANCE);
						break;
					case 10: //ring of protection
						player.magic_items.AddLast(MagicItemType.RING_OF_PROTECTION);
						break;
					case 11: //cloak of disappearance
						player.magic_items.AddLast(MagicItemType.CLOAK_OF_DISAPPEARANCE);
						break;
					default:
						break;
					}
					if(upgrade <= 4){
						B.Add("You find a " + Weapon.Name((WeaponType)(upgrade+5)) + "! ");
					}
					else{
						if(upgrade <= 7){
							B.Add("You find " + Armor.Name((ArmorType)(upgrade-2)) + "! ");
						}
						else{
							B.Add("You find a " + MagicItem.Name((MagicItemType)(upgrade-8)) + "! ");
						}
					}
				}
				else{
					Item i = Item.Create(Item.RandomItem(),player);
					if(i != null){
						B.Add("You find " + i.AName() + ". ");
					}
				}
				TurnToFloor();
			}
		}
		public bool IsLit(){ //default is player as viewer
			return IsLit(player.row,player.col);
		}
		public bool IsLit(int viewer_row,int viewer_col){
			if(light_value > 0){
				return true;
			}
			if(opaque){
				foreach(Tile t in NeighborsBetween(viewer_row,viewer_col)){
					if(t.light_value > 0){
						return true;
					}
				}
				if(M.actor[viewer_row,viewer_col] != null && M.actor[viewer_row,viewer_col].light_radius > 0){
					if(M.actor[viewer_row,viewer_col].light_radius >= DistanceFrom(viewer_row,viewer_col)){
						if(M.actor[viewer_row,viewer_col].HasBresenhamLine(row,col)){
							return true;
						}
					}
				}
			}
			return false;
		}
		delegate int del(int i);
		public List<Tile> NeighborsBetween(int r,int c){ //list of non-opaque tiles next to this one that are between you and it
			del Clamp = x => x<-1? -1 : x>1? 1 : x; //clamps to a value between -1 and 1
			int dy = r - row;
			int dx = c - col;
			List<Tile> result = new List<Tile>();
			if(dy==0 && dx==0){
				return result; //return the empty set
			}
			int newrow = row+Clamp(dy);
			int newcol = col+Clamp(dx);
			if(!M.tile[newrow,newcol].opaque){
				result.Add(M.tile[newrow,newcol]);
			}
			if(Math.Abs(dy) < Math.Abs(dx) && dy!=0){
				newrow -= Clamp(dy);
				if(!M.tile[newrow,newcol].opaque){
					result.Add(M.tile[newrow,newcol]);
				}
			}
			if(Math.Abs(dx) < Math.Abs(dy) && dx!=0){
				newcol -= Clamp(dx);
				if(!M.tile[newrow,newcol].opaque){
					result.Add(M.tile[newrow,newcol]);
				}
			}
			return result;
		}
	}
}

