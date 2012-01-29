/*Copyright (c) 2011-2012  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
using System.Threading;
namespace Forays{
	public class Item : PhysicalObject{
		public ConsumableType type{get; private set;}
		public int quantity{get;set;}
		
		private static Dictionary<ConsumableType,Item> proto= new Dictionary<ConsumableType,Item>();
		public static Item Prototype(ConsumableType type){ return proto[type]; }
		//public static Map M{get;set;} //inherited
		public static Queue Q{get;set;}
		public static Buffer B{get;set;}
		public static Actor player{get;set;}
		static Item(){
			proto[ConsumableType.HEALING] = new Item(ConsumableType.HEALING,"potion~ of healing",'!',Color.DarkMagenta);
			proto[ConsumableType.REGENERATION] = new Item(ConsumableType.REGENERATION,"potion~ of regeneration",'!',Color.Green);
			proto[ConsumableType.CURE_POISON] = new Item(ConsumableType.CURE_POISON,"potion~ of cure poison",'!',Color.Red);
			proto[ConsumableType.RESISTANCE] = new Item(ConsumableType.RESISTANCE,"potion~ of resistance",'!',Color.Yellow);
			proto[ConsumableType.CLARITY] = new Item(ConsumableType.CLARITY,"potion~ of clarity",'!',Color.Gray);
			proto[ConsumableType.PHASING] = new Item(ConsumableType.PHASING,"rune~ of phasing",'&',Color.Cyan);
			proto[ConsumableType.TELEPORTATION] = new Item(ConsumableType.TELEPORTATION,"rune~ of teleportation",'&',Color.DarkRed);
			proto[ConsumableType.PASSAGE] = new Item(ConsumableType.PASSAGE,"rune~ of passage",'&',Color.Blue);
			proto[ConsumableType.DETECT_MONSTERS] = new Item(ConsumableType.DETECT_MONSTERS,"scroll~ of detect monsters",'?',Color.White);
			proto[ConsumableType.MAGIC_MAP] = new Item(ConsumableType.MAGIC_MAP,"scroll~ of magic map",'?',Color.Gray);
			proto[ConsumableType.WIZARDS_LIGHT] = new Item(ConsumableType.WIZARDS_LIGHT,"orb~ of wizard's light",'*',Color.White);
			proto[ConsumableType.PRISMATIC_ORB] = new Item(ConsumableType.PRISMATIC_ORB,"prismatic orb~",'*',Color.RandomPrismatic);
			proto[ConsumableType.BANDAGE] = new Item(ConsumableType.BANDAGE,"bandage~",'~',Color.White);
		}
		public Item(ConsumableType type_,string name_,char symbol_,Color color_){
			type = type_;
			quantity = 1;
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
			row = -1;
			col = -1;
		}
		public Item(Item i,int r,int c){
			type = i.type;
			quantity = 1;
			name = i.name;
			a_name = i.a_name;
			the_name = i.the_name;
			symbol = i.symbol;
			color = i.color;
			row = r;
			col = c;
		}
		public static Item Create(ConsumableType type,int r,int c){
			Item i = null;
			if(M.tile[r,c].inv == null){
				i = new Item(proto[type],r,c);
				M.tile[r,c].inv = i;
			}
			else{
				if(M.tile[r,c].inv.type == type){
					M.tile[r,c].inv.quantity++;
					return M.tile[r,c].inv;
				}
			}
			return i;
		}
		public static Item Create(ConsumableType type,Actor a){
			Item i = null;
			foreach(Item held in a.inv){
				if(held.type == type){
					held.quantity++;
					return held;
				}
			}
			if(a.inv.Count < Global.MAX_INVENTORY_SIZE){
				i = new Item(proto[type],-1,-1);
				a.inv.Add(i);
			}
			else{
				i = Create(type,a.row,a.col);
			}
			return i;
		}
		public string AName(){
			string result;
			int position;
			string qty = quantity.ToString();
			switch(quantity){
			case 0:
				return "a buggy item";
			case 1:
				result = a_name;
				position = result.IndexOf('~');
				if(position != -1){
					result = result.Substring(0,position) + result.Substring(position+1);
				}
				return result;
			default:
				result = name;
				position = result.IndexOf('~');
				if(position != -1){
					result = qty + ' ' + result.Substring(0,position) + 's' + result.Substring(position+1);
				}
				return result;
			}
		}
		public string TheName(){
			string result;
			int position;
			string qty = quantity.ToString();
			switch(quantity){
			case 0:
				return "the buggy item";
			case 1:
				result = the_name;
				position = result.IndexOf('~');
				if(position != -1){
					result = result.Substring(0,position) + result.Substring(position+1);
				}
				return result;
			default:
				result = name;
				position = result.IndexOf('~');
				if(position != -1){
					result = qty + ' ' + result.Substring(0,position) + 's' + result.Substring(position+1);
				}
				return result;
			}
		}
		public static int Rarity(ConsumableType type){
			switch(type){
			case ConsumableType.RESISTANCE:
			case ConsumableType.CLARITY:
			case ConsumableType.TELEPORTATION:
			case ConsumableType.PASSAGE:
			case ConsumableType.MAGIC_MAP:
			case ConsumableType.WIZARDS_LIGHT:
			case ConsumableType.PRISMATIC_ORB:
				return 2;
			default:
				return 1;
			}
		}
		public static ConsumableType RandomItem(){
			List<ConsumableType> list = new List<ConsumableType>();
			foreach(ConsumableType item in Enum.GetValues(typeof(ConsumableType))){
				if(Item.Rarity(item) == 1){
					list.Add(item);
				}
				else{
					if(Global.Roll(1,Item.Rarity(item)) == Item.Rarity(item)){
						list.Add(item);
					}
				}
			}
			return list[Global.Roll(1,list.Count)-1];
		}
		public bool Use(Actor user){
			bool used = true;
			switch(type){
			case ConsumableType.HEALING:
				user.TakeDamage(DamageType.HEAL,Global.Roll(8,6),null);
				B.Add("A blue glow surrounds " + user.the_name + ". ",user);
				break;
			case ConsumableType.CURE_POISON:
				if(user.HasAttr(AttrType.POISONED)){
					user.attrs[AttrType.POISONED] = 0;
					B.Add(user.YouFeel() + " relieved. ",user);
				}
				else{
					B.Add(user.YouFeel() + " no different. ",user);
				}
				break;
			case ConsumableType.REGENERATION:
				{
				user.attrs[AttrType.REGENERATING]++;
				if(user.name == "you"){
					B.Add("Your blood tingles. ",user);
				}
				else{
					B.Add(user.the_name + " looks energized. ",user);
				}
				int duration = Global.Roll(1,10)+20;
				Q.Add(new Event(user,duration*100,AttrType.REGENERATING));
				break;
				}
			case ConsumableType.RESISTANCE:
				{
				user.attrs[AttrType.RESIST_FIRE]++;
				user.attrs[AttrType.RESIST_COLD]++;
				user.attrs[AttrType.RESIST_ELECTRICITY]++;
				B.Add(user.YouFeel() + " insulated. ",user);
				int duration = Global.Roll(2,10)+5;
				Q.Add(new Event(user,duration*100,AttrType.RESIST_FIRE));
				Q.Add(new Event(user,duration*100,AttrType.RESIST_COLD));
				Q.Add(new Event(user,duration*100,AttrType.RESIST_ELECTRICITY,user.YouFeel() + " less insulated. ",user));
				break;
				}
			case ConsumableType.CLARITY:
				user.ResetSpells();
				if(user.name == "you"){
					B.Add("Your mind clears. ");
				}
				else{
					B.Add(user.the_name + " seems focused. ",user);
				}
				break;
			case ConsumableType.PHASING:
				for(int i=0;i<9999;++i){
					int rr = Global.Roll(1,17) - 9;
					int rc = Global.Roll(1,17) - 9;
					if(Math.Abs(rr) + Math.Abs(rc) >= 6){
						rr += user.row;
						rc += user.col;
						if(M.BoundsCheck(rr,rc) && M.tile[rr,rc].passable && M.actor[rr,rc] == null){
							B.Add(user.You("step") + " through a rip in reality. ",M.tile[user.row,user.col],M.tile[rr,rc]);
							user.Move(rr,rc);
							break;
						}
					}
				}
				break;
			case ConsumableType.TELEPORTATION:
				for(int i=0;i<9999;++i){
					int rr = Global.Roll(1,Global.ROWS-2);
					int rc = Global.Roll(1,Global.COLS-2);
					if(Math.Abs(rr-user.row) >= 10 || Math.Abs(rc-user.col) >= 10 || (Math.Abs(rr-user.row) >= 7 && Math.Abs(rc-user.col) >= 7)){
						if(M.BoundsCheck(rr,rc) && M.tile[rr,rc].passable && M.actor[rr,rc] == null){
							B.Add(user.You("jump") + " through a rift in reality. ",M.tile[user.row,user.col],M.tile[rr,rc]);
							user.Move(rr,rc);
							break;
						}
					}
				}
				break;
			case ConsumableType.PASSAGE:
				{
				if(user.HasAttr(AttrType.IMMOBILIZED)){
					B.Add("You can't use this item while immobilized. ");
					used = false;
					break;
				}
				int i = user.DirectionOfOnly(TileType.WALL,true);
				if(i == 0){
					B.Add("This item requires an adjacent wall. ");
					used = false;
					break;
				}
				else{
					i = user.GetDirection(true,false);
					Tile t = user.TileInDirection(i);
					if(t != null){
						if(t.type == TileType.WALL){
							Console.CursorVisible = false;
							colorchar ch = new colorchar(Color.Cyan,'!');
							switch(user.DirectionOf(t)){
							case 8:
							case 2:
								ch.c = '|';
								break;
							case 4:
							case 6:
								ch.c = '-';
								break;
							}
							List<Tile> tiles = new List<Tile>();
							List<colorchar> memlist = new List<colorchar>();
							while(!t.passable){
								if(t.row == 0 || t.row == Global.ROWS-1 || t.col == 0 || t.col == Global.COLS-1){
									break;
								}
								tiles.Add(t);
								memlist.Add(Screen.MapChar(t.row,t.col));
								Screen.WriteMapChar(t.row,t.col,ch);
								Thread.Sleep(35);
								t = t.TileInDirection(i);
							}
							if(t.passable && M.actor[t.row,t.col] == null){
								if(M.tile[user.row,user.col].inv != null){
									Screen.WriteMapChar(user.row,user.col,new colorchar(user.tile().inv.color,user.tile().inv.symbol));
								}
								else{
									Screen.WriteMapChar(user.row,user.col,new colorchar(user.tile().color,user.tile().symbol));
								}
								Screen.WriteMapChar(t.row,t.col,new colorchar(user.color,user.symbol));
								int j = 0;
								foreach(Tile tile in tiles){
									Screen.WriteMapChar(tile.row,tile.col,memlist[j++]);
									Thread.Sleep(35);
								}
								B.Add(user.You("travel") + " through the passage. ",user,t);
								user.Move(t.row,t.col);
							}
							else{
								int j = 0;
								foreach(Tile tile in tiles){
									Screen.WriteMapChar(tile.row,tile.col,memlist[j++]);
									Thread.Sleep(35);
								}
								B.Add("The passage is blocked. ",user);
							}
						}
						else{
							B.Add("This item requires an adjacent wall. ");
							used = false;
							break;
						}
					}
					else{
						used = false;
					}
				}
				break;
				}
			case ConsumableType.DETECT_MONSTERS:
				user.attrs[AttrType.DETECTING_MONSTERS]++;
				B.Add("The scroll reveals " + user.Your() + " foes. ",user);
				int duration = Global.Roll(2,50)+50;
				Q.Add(new Event(user,duration*100,AttrType.DETECTING_MONSTERS,user.Your() + " foes are no longer revealed. ",user));
				break;
			case ConsumableType.MAGIC_MAP:
				B.Add("The scroll reveals the layout of this level. ");
				foreach(Tile t in M.AllTiles()){
					if(t.type != TileType.FLOOR){
						bool good = false;
						foreach(Tile neighbor in t.TilesAtDistance(1)){
							if(neighbor.type != TileType.WALL){
								good = true;
							}
						}
						if(good){
							t.seen = true;
						}
					}
				}
				break;
			case ConsumableType.WIZARDS_LIGHT:
				if(!Global.Option(OptionType.WIZLIGHT_CAST)){
					Global.Options[OptionType.WIZLIGHT_CAST] = true;
					B.Add("The air itself seems to shine. ");
				}
				else{
					B.Add("Nothing happens. ");
				}
				break;
			case ConsumableType.PRISMATIC_ORB:
				{
				Tile t = user.GetTarget();
				if(t != null){
					Actor first = user.FirstActorInLine(t);
					B.Add(user.You("throw") + " the prismatic orb. ",user);
					if(first != null){
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					List<DamageType> dmg = new List<DamageType>();
					dmg.Add(DamageType.FIRE);
					dmg.Add(DamageType.COLD);
					dmg.Add(DamageType.ELECTRIC);
					while(dmg.Count > 0){
						DamageType damtype = dmg[Global.Roll(1,dmg.Count)-1];
						colorchar ch = new colorchar(Color.Black,'*');
						switch(damtype){
						case DamageType.FIRE:
							ch.color = Color.RandomFire;
							break;
						case DamageType.COLD:
							ch.color = Color.RandomIce;
							break;
						case DamageType.ELECTRIC:
							ch.color = Color.RandomLightning;
							break;
						}
						B.DisplayNow();
						Screen.AnimateExplosion(t,1,ch,100);
						foreach(Actor a in t.ActorsWithinDistance(1)){
							a.TakeDamage(damtype,DamageClass.MAGICAL,Global.Roll(2,6),user);
						}
						dmg.Remove(damtype);
					}
				}
				else{
					used = false;
				}
				break;
				}
			case ConsumableType.BANDAGE:
				user.TakeDamage(DamageType.HEAL,1,null);
				if(user.name == "you"){
					B.Add("You apply a bandage. ");
				}
				else{
					B.Add(user.the_name + " applies a bandage. ",user);
				}
				break;
			default:
				used = false;
				break;
			}
			if(used){
				if(quantity > 1){
					--quantity;
				}
				else{
					user.inv.Remove(this);
				}
			}
			return used;
		}
	}
	public static class Weapon{
		public static Damage Damage(WeaponType type){
			switch(type){
			case WeaponType.SWORD:
			case WeaponType.FLAMEBRAND:
				return new Damage(3,false,DamageType.SLASHING,DamageClass.PHYSICAL,null);
			case WeaponType.MACE:
			case WeaponType.MACE_OF_FORCE:
				return new Damage(3,false,DamageType.BASHING,DamageClass.PHYSICAL,null);
			case WeaponType.DAGGER:
			case WeaponType.VENOMOUS_DAGGER:
				return new Damage(2,false,DamageType.PIERCING,DamageClass.PHYSICAL,null);
			case WeaponType.STAFF:
			case WeaponType.STAFF_OF_MAGIC:
			case WeaponType.BOW: //bow's melee damage
			case WeaponType.HOLY_LONGBOW:
				return new Damage(1,false,DamageType.BASHING,DamageClass.PHYSICAL,null);
			default:
				return new Damage(0,false,DamageType.NONE,DamageClass.NO_TYPE,null);
			}
		}
		public static WeaponType BaseWeapon(WeaponType type){
			switch(type){
			case WeaponType.SWORD:
			case WeaponType.FLAMEBRAND:
				return WeaponType.SWORD;
			case WeaponType.MACE:
			case WeaponType.MACE_OF_FORCE:
				return WeaponType.MACE;
			case WeaponType.DAGGER:
			case WeaponType.VENOMOUS_DAGGER:
				return WeaponType.DAGGER;
			case WeaponType.STAFF:
			case WeaponType.STAFF_OF_MAGIC:
				return WeaponType.STAFF;
			case WeaponType.BOW:
			case WeaponType.HOLY_LONGBOW:
				return WeaponType.BOW;
			default:
				return WeaponType.NO_WEAPON;
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
		public static colorstring EquipmentScreenName(WeaponType type){
			colorstring cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.Gray;
			switch(type){
			case WeaponType.SWORD:
				cs.s = "Sword";
				break;
			case WeaponType.FLAMEBRAND:
				cs.s = "Flamebrand";
				cs.color = Color.Red;
				break;
			case WeaponType.MACE:
				cs.s = "Mace";
				break;
			case WeaponType.MACE_OF_FORCE:
				cs.s = "Mace of force";
				cs.color = Color.Cyan;
				break;
			case WeaponType.DAGGER:
				cs.s = "Dagger";
				break;
			case WeaponType.VENOMOUS_DAGGER:
				cs.s = "Venomous dagger";
				cs.color = Color.Green;
				break;
			case WeaponType.STAFF:
				cs.s = "Staff";
				break;
			case WeaponType.STAFF_OF_MAGIC:
				cs.s = "Staff of magic";
				cs.color = Color.Magenta;
				break;
			case WeaponType.BOW:
				cs.s = "Bow";
				break;
			case WeaponType.HOLY_LONGBOW:
				cs.s = "Holy longbow";
				cs.color = Color.Yellow;
				break;
			default:
				cs.s = "no weapon";
				break;
			}
			return cs;
		}
		public static string Description(WeaponType type){
			switch(type){
			case WeaponType.SWORD:
				return "Sword -- A high-damage slashing weapon.";
			case WeaponType.FLAMEBRAND:
				return "Flamebrand -- Deals extra fire damage.";
			case WeaponType.MACE:
				return "Mace -- A high-damage bashing weapon.";
			case WeaponType.MACE_OF_FORCE:
				return "Mace of force -- Chance to knock back or stun.";
			case WeaponType.DAGGER:
				return "Dagger -- Medium damage. Extra chance for critical hits.";
			case WeaponType.VENOMOUS_DAGGER:
				return "Venomous dagger -- Chance to poison any foe it hits.";
			case WeaponType.STAFF:
				return "Staff -- Low damage. Grants a small bonus to defense.";
			case WeaponType.STAFF_OF_MAGIC:
				return "Staff of magic -- Grants a bonus to magic skill.";
			case WeaponType.BOW:
				return "Bow -- A high-damage ranged weapon.";
			case WeaponType.HOLY_LONGBOW:
				return "Holy longbow - Deals extra damage to undead and demons.";
			default:
				return "no weapon";
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
		public static ArmorType BaseArmor(ArmorType type){
			switch(type){
			case ArmorType.LEATHER:
			case ArmorType.ELVEN_LEATHER:
				return ArmorType.LEATHER;
			case ArmorType.CHAINMAIL:
			case ArmorType.CHAINMAIL_OF_ARCANA:
				return ArmorType.CHAINMAIL;
			case ArmorType.FULL_PLATE:
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				return ArmorType.FULL_PLATE;
			default:
				return ArmorType.NO_ARMOR;
			}
		}
		public static int AddedFailRate(ArmorType type){ //balance check: should these be higher?
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
		public static int StealthPenalty(ArmorType type){ //balance check: should these be higher?
			switch(type){
			case ArmorType.CHAINMAIL:
			case ArmorType.CHAINMAIL_OF_ARCANA:
				return 1;
			case ArmorType.FULL_PLATE:
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				return 3;
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
		public static colorstring EquipmentScreenName(ArmorType type){
			colorstring cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.Gray;
			switch(type){
			case ArmorType.LEATHER:
				cs.s = "Leather";
				break;
			case ArmorType.ELVEN_LEATHER:
				cs.s = "Elven leather";
				cs.color = Color.DarkCyan;
				break;
			case ArmorType.CHAINMAIL:
				cs.s = "Chainmail";
				break;
			case ArmorType.CHAINMAIL_OF_ARCANA:
				cs.s = "Chainmail of arcana";
				cs.color = Color.Magenta;
				break;
			case ArmorType.FULL_PLATE:
				cs.s = "Full plate";
				break;
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				cs.s = "Full plate of resistance";
				cs.color = Color.Blue;
				break;
			default:
				cs.s = "no armor";
				break;
			}
			return cs;
		}
		public static string Description(ArmorType type){
			switch(type){
			case ArmorType.LEATHER:
				return "Leather -- Light armor. Provides some basic protection.";
			case ArmorType.ELVEN_LEATHER:
				return "Elven leather -- Grants a bonus to stealth skill.";
			case ArmorType.CHAINMAIL:
				return "Chainmail -- Good protection. Noisy and hard to cast in.";
			case ArmorType.CHAINMAIL_OF_ARCANA:
				return "Chainmail of arcana -- Bonus to magic. No cast penalty.";
			case ArmorType.FULL_PLATE:
				return "Full plate -- The thickest, noisiest, and bulkiest armor.";
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				return "Full plate of resistance -- Grants resistance to elements.";
			default:
				return "no armor";
			}
		}
	}
	public static class MagicItem{
		public static colorstring StatsName(MagicItemType type){
			colorstring cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.DarkGreen;
			switch(type){
			case MagicItemType.RING_OF_PROTECTION:
				cs.s = "Ring (prot)";
				break;
			case MagicItemType.RING_OF_RESISTANCE:
				cs.s = "Ring (res)";
				break;
			case MagicItemType.PENDANT_OF_LIFE:
				cs.s = "Pendant";
				break;
			case MagicItemType.CLOAK_OF_DISAPPEARANCE:
				cs.s = "Cloak";
				break;
			default:
				cs.s = "No item";
				break;
			}
			return cs;
		}
		public static string Name(MagicItemType type){
			switch(type){
			case MagicItemType.PENDANT_OF_LIFE:
				return "pendant of life";
			case MagicItemType.RING_OF_PROTECTION:
				return "ring of protection";
			case MagicItemType.RING_OF_RESISTANCE:
				return "ring of resistance";
			case MagicItemType.CLOAK_OF_DISAPPEARANCE:
				return "cloak of disappearance";
			default:
				return "no item";
			}
		}
		public static string[] Description(MagicItemType type){
			switch(type){
			case MagicItemType.PENDANT_OF_LIFE:
				return new string[]{"Pendant of life -- Prevents a lethal attack from","finishing you, but only works once."};
			case MagicItemType.RING_OF_PROTECTION:
				return new string[]{"Ring of protection -- Grants a small bonus to","defense."};
			case MagicItemType.RING_OF_RESISTANCE:
				return new string[]{"Ring of resistance -- Grants resistance to cold,","fire, and electricity."};
			case MagicItemType.CLOAK_OF_DISAPPEARANCE:
				return new string[]{"Cloak of disappearance -- When your health falls,","gives you a chance to escape to safety."};
			default:
				return new string[]{"no","item"};
			}
		}
	}
}

