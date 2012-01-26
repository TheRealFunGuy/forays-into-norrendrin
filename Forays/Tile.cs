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
		public static Buffer B{get;set;}
		public static Queue Q{get;set;}
		public static Actor player{get;set;}
		static Tile(){
			proto[TileType.FLOOR] = new Tile(TileType.FLOOR,"floor",'.',Color.White,true,false,null);
			proto[TileType.WALL] = new Tile(TileType.WALL,"wall",'#',Color.Gray,false,true,null);
			proto[TileType.DOOR_C] = new Tile(TileType.DOOR_C,"closed door",'+',Color.DarkYellow,false,true,TileType.DOOR_O);
			proto[TileType.DOOR_O] = new Tile(TileType.DOOR_O,"open door",'-',Color.DarkYellow,true,false,TileType.DOOR_C);
			proto[TileType.STAIRS] = new Tile(TileType.STAIRS,"stairway",'>',Color.White,true,false,null);
			proto[TileType.CHEST] = new Tile(TileType.CHEST,"treasure chest",'~',Color.DarkYellow,true,false,null);
			proto[TileType.FIREPIT] = new Tile(TileType.FIREPIT,"fire pit",'0',Color.Red,true,false,null);
			proto[TileType.STALAGMITE] = new Tile(TileType.STALAGMITE,"stalagmite",'^',Color.White,false,true,TileType.FLOOR);
			proto[TileType.GRENADE] = new Tile(TileType.GRENADE,"grenade(dud)",',',Color.Red,true,false,null); //special treatment
			proto[TileType.QUICKFIRE] = new Tile(TileType.QUICKFIRE,"quickfire",'&',Color.RandomFire,true,false,TileType.FLOOR);
			proto[TileType.QUICKFIRE_TRAP] = new Tile(TileType.QUICKFIRE_TRAP,"quickfire trap",'^',Color.RandomFire,true,false,TileType.FLOOR);
			proto[TileType.LIGHT_TRAP] = new Tile(TileType.LIGHT_TRAP,"light trap",'^',Color.Yellow,true,false,TileType.FLOOR);
			proto[TileType.TELEPORT_TRAP] = new Tile(TileType.TELEPORT_TRAP,"teleport trap",'^',Color.Magenta,true,false,TileType.FLOOR);
			proto[TileType.UNDEAD_TRAP] = new Tile(TileType.UNDEAD_TRAP,"sliding wall trap",'^',Color.DarkCyan,true,false,TileType.FLOOR);
			proto[TileType.GRENADE_TRAP] = new Tile(TileType.GRENADE_TRAP,"grenade trap",'^',Color.DarkGray,true,false,TileType.FLOOR);
			proto[TileType.STUN_TRAP] = new Tile(TileType.STUN_TRAP,"stun trap",'^',Color.Red,true,false,TileType.FLOOR);
			proto[TileType.HIDDEN_DOOR] = new Tile(TileType.HIDDEN_DOOR,"wall",'#',Color.Gray,false,true,TileType.DOOR_C);
			//mimic
			//not an actual trap, but arena rooms, too. perhaps you'll see the opponent, in stasis.
				//"Touch the [tile]?(Y/N) "   if you touch it, you're stuck in the arena until one of you dies.
			//poison gas
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
		public bool GetItem(Item item){
			if(inv == null){
				inv = item;
				item.row = row;
				item.col = col;
				return true;
			}
			else{
				if(inv.type == item.type){
					inv.quantity += item.quantity;
					return true;
				}
				else{
					for(int i=1;i<COLS;++i){
						List<Tile> tiles = TilesAtDistance(i);
						while(tiles.Count > 0){
							Tile t = tiles[Global.Roll(tiles.Count)-1];
							if(t.passable && t.inv == null){
								t.inv = item;
								item.row = t.row;
								item.col = t.col;
								return true;
							}
							tiles.Remove(t);
						}
					}
				}
			}
			return false;
		}
		public void Toggle(PhysicalObject toggler){
			if(toggles_into != null){
				Toggle(toggler,toggles_into.Value);
			}
		}
		public void Toggle(PhysicalObject toggler,TileType toggle_to){
			bool lighting_update = false;
			List<Actor> actors = new List<Actor>();
			if(opaque != Prototype(toggle_to).opaque){
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
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(M.actor[i,j] != null && M.actor[i,j].LightRadius() > 0){
								actors.Add(M.actor[i,j]);
								M.actor[i,j].UpdateRadius(M.actor[i,j].LightRadius(),0);
							}
						}
					}
				}
			}

			TransformTo(toggle_to);

			if(lighting_update){
				foreach(Actor a in actors){
					a.UpdateRadius(0,a.LightRadius());
				}
			}
			if(toggler != null && toggler != player){
				if(type == TileType.DOOR_C){
					if(player.CanSee(this)){
						B.Add(toggler.the_name + " closes " + the_name + ". ");
					}
					else{
						if(seen || player.DistanceFrom(this) <= 12){
							B.Add("You hear a door closing. ");
						}
					}
				}
				if(type == TileType.DOOR_O){
					if(player.CanSee(this)){
						B.Add(toggler.the_name + " opens " + the_name + ". ");
					}
					else{
						if(seen || player.DistanceFrom(this) <= 12){
							B.Add("You hear a door opening. ");
						}
					}
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
			
			TransformTo(TileType.FLOOR);
			
			if(lighting_update){
				foreach(Actor a in actors){
					a.UpdateRadius(0,a.light_radius);
				}
			}
		}
		public void TriggerTrap(){
			if(actor().type == ActorType.FIRE_DRAKE){
				B.Add(actor().the_name + " smashes " + the_name + ". ",this);
				TransformTo(TileType.FLOOR);
				return;
			}
			B.Add("*CLICK* ",this);
			B.PrintAll();
			switch(type){
			case TileType.GRENADE_TRAP:
			{
				B.Add("Grenades fall from the ceiling above " + actor().the_name + "! ",this);
				bool nade_here = false;
				List<Tile> valid = new List<Tile>();
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.passable && t.type != TileType.GRENADE){
						valid.Add(t);
					}
				}
				int count = Global.Roll(10) == 10? 3 : 2;
				for(;count>0 & valid.Count > 0;--count){
					Tile t = valid[Global.Roll(valid.Count)-1];
					if(t == this){
						nade_here = true;
					}
					if(t.actor() != null){
						if(t.actor() == player){
							B.Add("One lands under you! ");
						}
						else{
							B.Add("One lands under " + t.actor().the_name + ". ",t.actor());
						}
					}
					else{
						if(t.inv != null){
							B.Add("It lands under " + t.inv.the_name + ". ",t);
						}
					}
					TileType oldtype = t.type;
					t.TransformTo(TileType.GRENADE);
					if(t == this){
						t.toggles_into = TileType.FLOOR;
						t.passable = true;
						t.opaque = false;
					}
					else{
						t.toggles_into = oldtype;
						t.passable = Tile.Prototype(oldtype).passable;
						t.opaque = Tile.Prototype(oldtype).opaque;
					}
					switch(oldtype){
					case TileType.FLOOR:
						t.the_name = "the grenade on the floor";
						t.a_name = "a grenade on a floor";
						break;
					case TileType.STAIRS:
						t.the_name = "the grenade on the stairway";
						t.a_name = "a grenade on a stairway";
						break;
					case TileType.DOOR_O:
						t.the_name = "the grenade in the open door";
						t.a_name = "a grenade in an open door";
						break;
					default:
						t.the_name = "the grenade and " + Tile.Prototype(oldtype).the_name;
						t.a_name = "a grenade and " + Tile.Prototype(oldtype).a_name;
						break;
					}
					valid.Remove(t);
					if(actor() == player){ //this hack demonstrates the sort of tweak i might need to do to my timing system
						Q.Add(new Event(t,101,EventType.GRENADE));
					}
					else{
						Q.Add(new Event(t,100,EventType.GRENADE));
					}
				}
				if(!nade_here){
					Toggle(actor());
				}
				break;
			}
			case TileType.UNDEAD_TRAP:
			{
				List<int> dirs = new List<int>();
				for(int i=2;i<=8;i+=2){
					Tile t = this;
					bool good = true;
					while(t.type != TileType.WALL){
						t = t.TileInDirection(i);
						if(t.opaque && t.type != TileType.WALL){
							good = false;
							break;
						}
						if(DistanceFrom(t) > 6){
							good = false;
							break;
						}
					}
					if(good && t.row > 0 && t.row < ROWS-1 && t.col > 0 && t.col < COLS-1){
						t = t.TileInDirection(i);
					}
					else{
						good = false;
					}
					if(good && t.row > 0 && t.row < ROWS-1 && t.col > 0 && t.col < COLS-1){
						foreach(Tile tt in t.TilesWithinDistance(1)){
							if(tt.type != TileType.WALL){
								good = false;
							}
						}
					}
					else{
						good = false;
					}
					if(good){
						dirs.Add(i);
					}
				}
				if(dirs.Count == 0){
					B.Add("Nothing happens. ",this);
				}
				else{
					int dir = dirs[Global.Roll(dirs.Count)-1];
					Tile first = this;
					while(first.type != TileType.WALL){
						first = first.TileInDirection(dir);
					}
					first.TileInDirection(dir).TurnToFloor();
					ActorType ac = Global.CoinFlip()? ActorType.SKELETON : ActorType.ZOMBIE;
					Actor.Create(ac,first.TileInDirection(dir).row,first.TileInDirection(dir).col);
					first.TurnToFloor();
					//first.ActorInDirection(dir).target_location = this;
					//first.ActorInDirection(dir).player_visibility_duration = -1;
					first.ActorInDirection(dir).FindPath(TileInDirection(dir));
					if(player.CanSee(first)){
						B.Add("The wall slides away. ");
					}
					else{
						if(DistanceFrom(player) <= 6){
							B.Add("You hear rock sliding on rock. ");
						}
					}
				}
				Toggle(actor());
				break;
			}
			case TileType.TELEPORT_TRAP:
				B.Add("An unstable energy covers " + actor().the_name + ". ",actor());
				actor().attrs[AttrType.TELEPORTING] = Global.Roll(4);
				Q.KillEvents(actor(),AttrType.TELEPORTING);
				Q.Add(new Event(actor(),actor().DurationOfMagicalEffect(Global.Roll(10)+25)*100,AttrType.TELEPORTING,actor().YouFeel() + " more stable. ",actor()));
				Toggle(actor());
				break;
			case TileType.STUN_TRAP:
				B.Add("A disorienting flash assails " + actor().the_name + ". ",this);
				actor().attrs[AttrType.STUNNED]++;
				Q.Add(new Event(actor(),actor().DurationOfMagicalEffect(Global.Roll(5)+7)*100,AttrType.STUNNED,actor().YouFeel() + " less disoriented. ",actor()));
				Toggle(actor());
				break;
			case TileType.LIGHT_TRAP:
				B.Add("You hear a high-pitched ringing sound. "); //was "high-pitched thrumming sound"
				if(actor() == player || player.DistanceFrom(this) <= 4){ //kinda hacky until other things can MakeNoise
					player.MakeNoise();
				}
				if(player.HasLOS(row,col)){
					B.Add("A wave of light washes out from above " + actor().the_name + "! ");
				}
				else{
					B.Add("A wave of light washes over the area! ");
				}
				Global.Options[OptionType.WIZLIGHT_CAST] = true;
				Toggle(actor());
				break;
			case TileType.QUICKFIRE_TRAP:
				B.Add("Fire pours over " + actor().the_name + " and starts to spread! ",this);
				foreach(Actor a in ActorsWithinDistance(1)){
					if(!a.HasAttr(AttrType.RESIST_FIRE) && !a.HasAttr(AttrType.CATCHING_FIRE) && !a.HasAttr(AttrType.ON_FIRE)
					&& !a.HasAttr(AttrType.IMMUNE_FIRE) && !a.HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){
						if(a == actor()){							// to work properly, 
							a.attrs[AttrType.STARTED_CATCHING_FIRE_THIS_TURN] = 1; //this would need to determine what actor's turn it is
						} //therefore, hack
						else{
							a.attrs[AttrType.CATCHING_FIRE] = 1;
						}
						B.Add(a.You("start") + " to catch fire. ",a);
					}
				}
				TransformTo(TileType.QUICKFIRE);
				toggles_into = TileType.FLOOR;
				passable = true;
				opaque = false;
				List<Tile> newarea = new List<Tile>();
				newarea.Add(this);
				if(actor() == player){ //hack
					Q.Add(new Event(this,newarea,101,EventType.QUICKFIRE,AttrType.NO_ATTR,3,""));
				}
				else{
					Q.Add(new Event(this,newarea,100,EventType.QUICKFIRE,AttrType.NO_ATTR,3,""));
				}
				break;
			default:
				break;
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
						B.Add("You find " + Item.Prototype(i.type).AName() + ". ");
					}
				}
				TurnToFloor();
			}
		}
		public bool IsLit(){ //default is player as viewer
			return IsLit(player.row,player.col);
		}
		public bool IsLit(int viewer_row,int viewer_col){
			if(Global.Option(OptionType.WIZLIGHT_CAST)){
				return true;
			}
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
		public bool IsTrap(){
			switch(type){
			case TileType.QUICKFIRE_TRAP:
			case TileType.GRENADE_TRAP:
			case TileType.LIGHT_TRAP:
			case TileType.UNDEAD_TRAP:
			case TileType.TELEPORT_TRAP:
			case TileType.STUN_TRAP:
				return true;
			default:
				return false;
			}
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

