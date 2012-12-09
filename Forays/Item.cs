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
		public ConsumableType type{get;set;}
		public int quantity{get;set;}
		public bool ignored{get;set;} //whether autoexplore and autopickup should ignore this item
		public bool do_not_stack{get;set;} //whether the item should be combined with other stacks. used for mimic items too.
		
		private static Dictionary<ConsumableType,Item> proto= new Dictionary<ConsumableType,Item>();
		public static Item Prototype(ConsumableType type){ return proto[type]; }
		//public static Map M{get;set;} //inherited
		public static Queue Q{get;set;}
		public static Buffer B{get;set;}
		public static Actor player{get;set;}
		static Item(){
			proto[ConsumableType.HEALING] = new Item(ConsumableType.HEALING,"potion~ of healing",'!',Color.DarkMagenta);
			proto[ConsumableType.REGENERATION] = new Item(ConsumableType.REGENERATION,"potion~ of regeneration",'!',Color.Green);
			proto[ConsumableType.TOXIN_IMMUNITY] = new Item(ConsumableType.TOXIN_IMMUNITY,"potion~ of toxin immunity",'!',Color.Red);
//			proto[ConsumableType.RESISTANCE] = new Item(ConsumableType.RESISTANCE,"potion~ of resistance",'!',Color.Yellow);
			proto[ConsumableType.CLARITY] = new Item(ConsumableType.CLARITY,"potion~ of clarity",'!',Color.Gray);
			proto[ConsumableType.BLINKING] = new Item(ConsumableType.BLINKING,"rune~ of blinking",'&',Color.Cyan);
			proto[ConsumableType.TELEPORTATION] = new Item(ConsumableType.TELEPORTATION,"rune~ of teleportation",'&',Color.DarkRed);
			proto[ConsumableType.PASSAGE] = new Item(ConsumableType.PASSAGE,"rune~ of passage",'&',Color.Blue);
			proto[ConsumableType.DETECT_MONSTERS] = new Item(ConsumableType.DETECT_MONSTERS,"scroll~ of detect monsters",'?',Color.White);
			proto[ConsumableType.MAGIC_MAP] = new Item(ConsumableType.MAGIC_MAP,"scroll~ of magic map",'?',Color.Gray);
			proto[ConsumableType.SUNLIGHT] = new Item(ConsumableType.SUNLIGHT,"orb~ of sunlight",'*',Color.White);
			proto[ConsumableType.DARKNESS] = new Item(ConsumableType.DARKNESS,"orb~ of darkness",'*',Color.DarkGray);
			proto[ConsumableType.PRISMATIC] = new Item(ConsumableType.PRISMATIC,"prismatic orb~",'*',Color.RandomPrismatic);
			proto[ConsumableType.FREEZING] = new Item(ConsumableType.FREEZING,"orb~ of freezing",'*',Color.RandomIce);
			proto[ConsumableType.BANDAGE] = new Item(ConsumableType.BANDAGE,"bandage~",'{',Color.White);
			Define(ConsumableType.QUICKFIRE,"orb~ of quickfire",'*',Color.RandomFire);
			Define(ConsumableType.CLOAKING,"potion~ of cloaking",'!',Color.DarkBlue);
			Define(ConsumableType.FOG,"orb~ of fog",'*',Color.Gray);
			Define(ConsumableType.TIME,"rune~ of time",'&',Color.Green);
		}
		private static void Define(ConsumableType type_,string name_,char symbol_,Color color_){
			proto[type_] = new Item(type_,name_,symbol_,color_);
		}
		public Item(){}
		public Item(ConsumableType type_,string name_,char symbol_,Color color_){
			type = type_;
			quantity = 1;
			ignored = false;
			do_not_stack = false;
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
			light_radius = 0;
		}
		public Item(Item i,int r,int c){
			type = i.type;
			quantity = 1;
			ignored = false;
			do_not_stack = false;
			name = i.name;
			a_name = i.a_name;
			the_name = i.the_name;
			symbol = i.symbol;
			color = i.color;
			row = r;
			col = c;
			light_radius = i.light_radius;
		}
		public static Item Create(ConsumableType type,int r,int c){
			Item i = null;
			if(Global.BoundsCheck(r,c)){
				if(M.tile[r,c].inv == null){
					i = new Item(proto[type],r,c);
					if(i.light_radius > 0){
						i.UpdateRadius(0,i.light_radius);
					}
					M.tile[r,c].inv = i;
				}
				else{
					if(M.tile[r,c].inv.type == type){
						M.tile[r,c].inv.quantity++;
						return M.tile[r,c].inv;
					}
				}
			}
			else{
				i = new Item(proto[type],r,c);
			}
			return i;
		}
		public static Item Create(ConsumableType type,Actor a){
			Item i = null;
			if(a.InventoryCount() < Global.MAX_INVENTORY_SIZE){
				foreach(Item held in a.inv){
					if(held.type == type){
						held.quantity++;
						return held;
					}
				}
				i = new Item(proto[type],-1,-1);
				a.inv.Add(i);
			}
			else{
				i = Create(type,a.row,a.col);
			}
			return i;
		}
		public string Name(){
			string result;
			int position;
			string qty = quantity.ToString();
			switch(quantity){
			case 0:
				return "a buggy item";
			case 1:
				result = name;
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
			case ConsumableType.TOXIN_IMMUNITY:
			case ConsumableType.CLARITY:
			case ConsumableType.TELEPORTATION:
			case ConsumableType.SUNLIGHT:
			case ConsumableType.DARKNESS:
			case ConsumableType.PRISMATIC:
			case ConsumableType.HEALING:
			case ConsumableType.REGENERATION:
			case ConsumableType.CLOAKING:
			case ConsumableType.FOG:
				//plus the potion of 'brutish strength'
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
			return list.Random();
		}
		public bool Use(Actor user){ return Use(user,null); }
		public bool Use(Actor user,List<Tile> line){
			bool used = true;
			switch(type){
			case ConsumableType.HEALING:
				user.TakeDamage(DamageType.HEAL,DamageClass.NO_TYPE,50,null); //was Roll(8,6)
				B.Add("A blue glow surrounds " + user.the_name + ". ",user);
				break;
			case ConsumableType.TOXIN_IMMUNITY:
				if(!user.HasAttr(AttrType.IMMUNE_TOXINS)){
					if(user.HasAttr(AttrType.POISONED)){
						user.attrs[AttrType.POISONED] = 0;
						B.Add(user.YouFeel() + " relieved. ",user);
					}
					user.GainAttr(AttrType.IMMUNE_TOXINS,5100,user.YouAre() + " no longer immune to toxins. ",user);
				}
				else{
					B.Add("Nothing happens. ",user);
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
				int duration = 60; //was Roll(10)+20
				Q.Add(new Event(user,duration*100,AttrType.REGENERATING));
				break;
			}
			/*case ConsumableType.RESISTANCE:
				{
				user.attrs[AttrType.RESIST_FIRE]++;
				user.attrs[AttrType.RESIST_COLD]++;
				user.attrs[AttrType.RESIST_ELECTRICITY]++;
				B.Add(user.YouFeel() + " insulated. ",user);
				int duration = Global.Roll(2,10)+5;
				Q.Add(new Event(user,duration*100,AttrType.RESIST_FIRE));
				Q.Add(new Event(user,duration*100,AttrType.RESIST_COLD));
				Q.Add(new Event(user,duration*100,AttrType.RESIST_ELECTRICITY,user.YouFeel() + " less insulated. ",user));
				if(user.HasAttr(AttrType.ON_FIRE) || user.HasAttr(AttrType.CATCHING_FIRE)
				|| user.HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){
					B.Add(user.YouAre() + " no longer on fire. ",user);
					int oldradius = user.LightRadius();
					user.attrs[AttrType.ON_FIRE] = 0;
					user.attrs[AttrType.CATCHING_FIRE] = 0;
					user.attrs[AttrType.STARTED_CATCHING_FIRE_THIS_TURN] = 0;
					if(oldradius != user.LightRadius()){
						user.UpdateRadius(oldradius,user.LightRadius());
					}
				}
				break;
				}*/
			case ConsumableType.CLARITY:
				user.ResetSpells();
				if(user.name == "you"){
					B.Add("Your mind clears. ");
				}
				else{
					B.Add(user.the_name + " seems focused. ",user);
				}
				break;
			case ConsumableType.CLOAKING:
				if(user.tile().IsLit()){
					B.Add("You would feel at home in the shadows. ");
				}
				else{
					B.Add("You fade away in the darkness. ");
				}
				user.GainAttrRefreshDuration(AttrType.SHADOW_CLOAK,(Global.Roll(41)+29)*100,"You are no longer cloaked. ",user);
				break;
			case ConsumableType.BLINKING:
				for(int i=0;i<9999;++i){
					int rr = Global.Roll(1,17) - 9;
					int rc = Global.Roll(1,17) - 9;
					if(Math.Abs(rr) + Math.Abs(rc) >= 6){
						rr += user.row;
						rc += user.col;
						if(M.BoundsCheck(rr,rc) && M.tile[rr,rc].passable && M.actor[rr,rc] == null){
							B.Add(user.You("step") + " through a rip in reality. ",M.tile[user.row,user.col],M.tile[rr,rc]);
							user.AnimateStorm(2,3,4,'*',Color.DarkMagenta);
							user.Move(rr,rc);
							M.Draw();
							user.AnimateStorm(2,3,4,'*',Color.DarkMagenta);
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
							user.AnimateStorm(3,3,10,'*',Color.Green);
							user.Move(rr,rc);
							M.Draw();
							user.AnimateStorm(3,3,10,'*',Color.Green);
							break;
						}
					}
				}
				break;
			case ConsumableType.PASSAGE:
				{
				int i = user.DirectionOfOnlyUnblocked(TileType.WALL,true);
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
			case ConsumableType.TIME:
				B.Add("Time stops for a moment. ");
				Q.turn -= 200;
				break;
			case ConsumableType.DETECT_MONSTERS:
			{
				//user.attrs[AttrType.DETECTING_MONSTERS]++;
				B.Add("The scroll reveals " + user.Your() + " foes. ",user);
				int duration = Global.Roll(20)+30;
				//Q.Add(new Event(user,duration*100,AttrType.DETECTING_MONSTERS,user.Your() + " foes are no longer revealed. ",user));
				user.GainAttrRefreshDuration(AttrType.DETECTING_MONSTERS,duration*100,user.Your() + " foes are no longer revealed. ",user);
				break;
			}
			case ConsumableType.MAGIC_MAP:
			{
				B.Add("The scroll reveals the layout of this level. ");
				Event hiddencheck = null;
				foreach(Event e in Q.list){
					if(!e.dead && e.type == EventType.CHECK_FOR_HIDDEN){
						hiddencheck = e;
						break;
					}
				}
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
							if(t.IsTrapOrVent() || t.Is(TileType.HIDDEN_DOOR)){
								if(hiddencheck != null){
									hiddencheck.area.Remove(t);
								}
							}
							if(t.IsTrapOrVent()){
								t.name = Tile.Prototype(t.type).name;
								t.a_name = Tile.Prototype(t.type).a_name;
								t.the_name = Tile.Prototype(t.type).the_name;
								t.symbol = Tile.Prototype(t.type).symbol;
								t.color = Tile.Prototype(t.type).color;
							}
							if(t.Is(TileType.HIDDEN_DOOR)){
								t.Toggle(null);
							}
						}
					}
				}
				break;
			}
			case ConsumableType.SUNLIGHT:
				if(!M.wiz_lite){
					M.wiz_lite = true;
					M.wiz_dark = false;
					B.Add("The air itself seems to shine. ");
				}
				else{
					B.Add("Nothing happens. ");
				}
				break;
			case ConsumableType.DARKNESS:
				if(!M.wiz_dark){
					M.wiz_dark = true;
					M.wiz_lite = false;
					B.Add("The air itself grows dark. ");
				}
				else{
					B.Add("Nothing happens. ");
				}
				break;
			case ConsumableType.PRISMATIC:
			{
				if(line == null){
					line = user.GetTarget(12,1);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line); //todo - consider allowing thrown items to pass over actors, because they fly in an arc
					B.Add(user.You("throw") + " the prismatic orb. ",user);
					if(first != null){
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',Color.RandomPrismatic);
					List<DamageType> dmg = new List<DamageType>();
					dmg.Add(DamageType.FIRE);
					dmg.Add(DamageType.COLD);
					dmg.Add(DamageType.ELECTRIC);
					while(dmg.Count > 0){
						DamageType damtype = dmg.Random();
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
						if(t.passable){
							foreach(Tile t2 in t.TilesWithinDistance(1)){
								if(t2.actor() != null){
									t2.actor().TakeDamage(damtype,DamageClass.MAGICAL,Global.Roll(2,6),user,"a prismatic orb");
								}
								if(damtype == DamageType.FIRE && t2.Is(FeatureType.TROLL_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_CORPSE);
									B.Add("The troll corpse burns to ashes! ",t2);
								}
								if(damtype == DamageType.FIRE && t2.Is(FeatureType.TROLL_SEER_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_SEER_CORPSE);
									B.Add("The troll seer corpse burns to ashes! ",t2);
								}
							}
						}
						else{
							foreach(Tile t2 in t.TilesWithinDistance(1)){
								if(prev != null && prev.HasBresenhamLine(t2.row,t2.col)){
									if(t2.actor() != null){
										t2.actor().TakeDamage(damtype,DamageClass.MAGICAL,Global.Roll(2,6),user,"a prismatic orb");
									}
									if(damtype == DamageType.FIRE && t2.Is(FeatureType.TROLL_CORPSE)){
										t2.features.Remove(FeatureType.TROLL_CORPSE);
										B.Add("The troll corpse burns to ashes! ",t2);
									}
									if(damtype == DamageType.FIRE && t2.Is(FeatureType.TROLL_SEER_CORPSE)){
										t2.features.Remove(FeatureType.TROLL_SEER_CORPSE);
										B.Add("The troll seer corpse burns to ashes! ",t2);
									}
								}
							}
						}
						dmg.Remove(damtype);
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.FREEZING:
			{
				if(line == null){
					line = user.GetTarget(12,3);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("throw") + " the freezing orb. ",user);
					if(first != null){
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',Color.RandomIce);
					user.AnimateExplosion(t,3,'*',Color.Cyan);
					List<Actor> targets = new List<Actor>();
					if(t.passable){
						foreach(Actor ac in t.ActorsWithinDistance(3)){
							if(t.HasLOE(ac)){
								targets.Add(ac);
							}
						}
					}
					else{
						foreach(Actor ac in t.ActorsWithinDistance(3)){
							if(prev != null && prev.HasLOE(ac)){
								targets.Add(ac);
							}
						}
					}
					while(targets.Count > 0){
						Actor ac = targets.RemoveRandom();
						B.Add(ac.YouAre() + " encased in ice. ",ac);
						ac.attrs[Forays.AttrType.FROZEN] = 25;
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.QUICKFIRE:
			{
				if(line == null){
					line = user.GetTarget(12,-1);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("throw") + " the orb of quickfire. ",user);
					if(first != null){
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',Color.RandomFire);
					if(t.passable){
						t.features.Add(FeatureType.QUICKFIRE);
						Q.Add(new Event(t,new List<Tile>{t},100,EventType.QUICKFIRE,AttrType.NO_ATTR,3,""));
					}
					else{
						prev.features.Add(FeatureType.QUICKFIRE);
						Q.Add(new Event(prev,new List<Tile>{prev},100,EventType.QUICKFIRE,AttrType.NO_ATTR,3,""));
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.FOG:
			{
				if(line == null){
					line = user.GetTarget(12,-3);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("throw") + " the orb of fog. ",user);
					if(first != null){
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',Color.Gray);
					List<Tile> area = new List<Tile>();
					List<pos> cells = new List<pos>();
					if(t.passable){
						foreach(Tile tile in t.TilesWithinDistance(3)){
							if(tile.passable && t.HasLOE(tile)){
								tile.AddOpaqueFeature(FeatureType.FOG);
								area.Add(tile);
								cells.Add(tile.p);
							}
						}
					}
					else{
						foreach(Tile tile in t.TilesWithinDistance(3)){
							if(prev != null && tile.passable && prev.HasLOE(tile)){
								tile.AddOpaqueFeature(FeatureType.FOG);
								area.Add(tile);
								cells.Add(tile.p);
							}
						}
					}
					Screen.AnimateMapCells(cells,new colorchar('*',Color.Gray));
					Q.Add(new Event(area,400,EventType.FOG));
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.BANDAGE:
				user.TakeDamage(DamageType.HEAL,DamageClass.NO_TYPE,1,null);
				if(user.HasAttr(AttrType.MAGICAL_BLOOD)){
					user.recover_time = Q.turn + 200;
				}
				else{
					user.recover_time = Q.turn + 500;
				}
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
					if(user != null){
						user.inv.Remove(this);
					}
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
				return new Damage(3,DamageType.SLASHING,DamageClass.PHYSICAL,null);
			case WeaponType.MACE:
			case WeaponType.MACE_OF_FORCE:
				return new Damage(3,DamageType.BASHING,DamageClass.PHYSICAL,null);
			case WeaponType.DAGGER:
			case WeaponType.VENOMOUS_DAGGER:
				return new Damage(2,DamageType.PIERCING,DamageClass.PHYSICAL,null);
			case WeaponType.STAFF:
			case WeaponType.STAFF_OF_MAGIC:
			case WeaponType.BOW: //bow's melee damage
			case WeaponType.HOLY_LONGBOW:
				return new Damage(1,DamageType.BASHING,DamageClass.PHYSICAL,null);
			default:
				return new Damage(0,DamageType.NONE,DamageClass.NO_TYPE,null);
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
		public static cstr StatsName(WeaponType type){
			cstr cs;
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
		public static cstr EquipmentScreenName(WeaponType type){
			cstr cs;
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
				return "Sword -- A powerful 3d6 damage slashing weapon.";
			case WeaponType.FLAMEBRAND:
				return "Flamebrand -- Deals extra fire damage.";
			case WeaponType.MACE:
				return "Mace -- A powerful 3d6 damage bashing weapon.";
			case WeaponType.MACE_OF_FORCE:
				return "Mace of force -- Chance to knock back or stun.";
			case WeaponType.DAGGER:
				return "Dagger -- 2d6 damage. Extra chance for critical hits.";
			case WeaponType.VENOMOUS_DAGGER:
				return "Venomous dagger -- Chance to poison any foe it hits.";
			case WeaponType.STAFF:
				return "Staff -- 1d6 damage. Grants a small bonus to defense.";
			case WeaponType.STAFF_OF_MAGIC:
				return "Staff of magic -- Grants a bonus to magic skill.";
			case WeaponType.BOW:
				return "Bow -- 3d6 damage at range. Less accurate than melee.";
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
		public static int StealthPenalty(ArmorType type){
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
		public static cstr StatsName(ArmorType type){
			cstr cs;
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
		public static cstr EquipmentScreenName(ArmorType type){
			cstr cs;
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
		public static cstr StatsName(MagicItemType type){
			cstr cs;
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

