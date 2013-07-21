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
		public bool opaque{get{ return internal_opaque || features.Contains(FeatureType.FOG); } set{ internal_opaque = value; }}
		private bool internal_opaque; //no need to ever access this directly
		public bool seen{get;set;}
		public bool revealed_by_light{get;set;}
		public bool solid_rock{get;set;} //used for walls that will never be seen, to speed up LOS checks
		public int light_value{get{ return internal_light_value; }
			set{
				internal_light_value = value;
				if(value > 0 && type == TileType.BLAST_FUNGUS){
					/*Q.Add(new Event(this,200,EventType.BLAST_FUNGUS));
					B.Add("The blast fungus starts to smolder in the light. ",this);
					features.Remove(FeatureType.FUNGUS);
					features.Add(FeatureType.FUNGUS_ACTIVE);*/
					B.Add("The blast fungus starts to smolder in the light. ",this);
					Toggle(null);
					if(inv == null){ //should always be true
						GetItem(Item.Create(ConsumableType.BLAST_FUNGUS,row,col));
						inv.quantity = 3;
						inv.revealed_by_light = true;
					}
					Q.Add(new Event(inv,100,EventType.BLAST_FUNGUS));
				}
			}
		}
		private int internal_light_value; //no need to ever access this directly, either
		public int direction_exited{get;set;} //used to improve AI's handling of corners
		public TileType? toggles_into;
		public Item inv{get;set;}
		public List<FeatureType> features = new List<FeatureType>();
		
		private static Dictionary<TileType,Tile> proto= new Dictionary<TileType, Tile>();
		public static Tile Prototype(TileType type){ return proto[type]; }
		private static Dictionary<FeatureType,PhysicalObject> proto_feature = new Dictionary<FeatureType, PhysicalObject>();
		public static PhysicalObject Feature(FeatureType type){ return proto_feature[type]; }
		private static int ROWS = Global.ROWS;
		private static int COLS = Global.COLS;
		//public static Map M{get;set;} //inherited
		//public static Buffer B{get;set;}
		//public static Queue Q{get;set;}
		//public static Actor player{get;set;}
		static Tile(){
			Define(TileType.FLOOR,"floor",'.',Color.White,true,false,null);
			Define(TileType.WALL,"wall",'#',Color.Gray,false,true,null);
			Define(TileType.DOOR_C,"closed door",'+',Color.DarkYellow,false,true,TileType.DOOR_O);
			Define(TileType.DOOR_O,"open door",'-',Color.DarkYellow,true,false,TileType.DOOR_C);
			Define(TileType.STAIRS,"stairway",'>',Color.White,true,false,null);
			Define(TileType.CHEST,"treasure chest",'=',Color.DarkYellow,true,false,null);
			Define(TileType.FIREPIT,"fire pit",'0',Color.Red,true,false,null);
			proto[TileType.FIREPIT].light_radius = 1;
			proto[TileType.FIREPIT].revealed_by_light = true;
			Define(TileType.STALAGMITE,"stalagmite",'^',Color.White,false,true,TileType.FLOOR);
			Define(TileType.QUICKFIRE_TRAP,"quickfire trap",'^',Color.RandomFire,true,false,TileType.FLOOR);
			Define(TileType.LIGHT_TRAP,"light trap",'^',Color.Yellow,true,false,TileType.FLOOR);
			Define(TileType.TELEPORT_TRAP,"teleport trap",'^',Color.Magenta,true,false,TileType.FLOOR);
			Define(TileType.UNDEAD_TRAP,"sliding wall trap",'^',Color.DarkCyan,true,false,TileType.FLOOR);
			Define(TileType.GRENADE_TRAP,"grenade trap",'^',Color.DarkGray,true,false,TileType.FLOOR);
			Define(TileType.STUN_TRAP,"stun trap",'^',Color.Red,true,false,TileType.FLOOR);
			Define(TileType.ALARM_TRAP,"alarm trap",'^',Color.White,true,false,TileType.FLOOR);
			Define(TileType.DARKNESS_TRAP,"darkness trap",'^',Color.Blue,true,false,TileType.FLOOR);
			Define(TileType.POISON_GAS_TRAP,"poison gas trap",'^',Color.Green,true,false,TileType.FLOOR);
			Define(TileType.DIM_VISION_TRAP,"dim vision trap",'^',Color.DarkMagenta,true,false,TileType.FLOOR);
			Define(TileType.ICE_TRAP,"ice trap",'^',Color.RandomIce,true,false,TileType.FLOOR);
			Define(TileType.PHANTOM_TRAP,"phantom trap",'^',Color.Cyan,true,false,TileType.FLOOR);
			Define(TileType.HIDDEN_DOOR,"wall",'#',Color.Gray,false,true,TileType.DOOR_C);
			Define(TileType.RUBBLE,"pile of rubble",':',Color.Gray,false,true,TileType.FLOOR);
			Define(TileType.COMBAT_SHRINE,"shrine of combat",'_',Color.DarkRed,true,false,TileType.RUINED_SHRINE);
			Define(TileType.DEFENSE_SHRINE,"shrine of defense",'_',Color.White,true,false,TileType.RUINED_SHRINE);
			Define(TileType.MAGIC_SHRINE,"shrine of magic",'_',Color.Magenta,true,false,TileType.RUINED_SHRINE);
			Define(TileType.SPIRIT_SHRINE,"shrine of spirit",'_',Color.Yellow,true,false,TileType.RUINED_SHRINE);
			Define(TileType.STEALTH_SHRINE,"shrine of stealth",'_',Color.Blue,true,false,TileType.RUINED_SHRINE);
			Define(TileType.RUINED_SHRINE,"ruined shrine",'_',Color.DarkGray,true,false,null);
			Define(TileType.SPELL_EXCHANGE_SHRINE,"spell exchange shrine",'_',Color.DarkMagenta,true,false,TileType.RUINED_SHRINE);
			Define(TileType.FIRE_GEYSER,"fire geyser",'~',Color.Red,true,false,null);
			Define(TileType.STATUE,"statue",'2',Color.Gray,false,false,null);
			Define(TileType.HEALING_POOL,"healing pool",'0',Color.Cyan,true,false,TileType.FLOOR);
			proto[TileType.HEALING_POOL].revealed_by_light = true;
			Define(TileType.FOG_VENT,"fog vent",'~',Color.Gray,true,false,null);
			Define(TileType.POISON_GAS_VENT,"gas vent",'~',Color.DarkGreen,true,false,null);
			Define(TileType.STONE_SLAB,"stone slab",'#',Color.White,false,true,null);
			proto[TileType.STONE_SLAB].revealed_by_light = true;
			Define(TileType.CHASM,"chasm",'7',Color.DarkBlue,true,false,null);
			Define(TileType.BREACHED_WALL,"floor",'.',Color.RandomBreached,true,false,TileType.WALL);
			Define(TileType.CRACKED_WALL,"cracked wall",'#',Color.DarkYellow,false,true,TileType.FLOOR);
			proto[TileType.CRACKED_WALL].revealed_by_light = true;
			Define(TileType.BRUSH,"brush",'"',Color.DarkYellow,true,false,TileType.FLOOR);
			proto[TileType.BRUSH].a_name = "brush";
			proto[TileType.BRUSH].revealed_by_light = true;
			Define(TileType.WATER,"shallow water",'~',Color.DarkCyan,true,false,null);
			proto[TileType.WATER].a_name = "shallow water";
			Define(TileType.ICE,"ice",'.',Color.Cyan,true,false,null);
			proto[TileType.ICE].a_name = "ice";
			proto[TileType.ICE].revealed_by_light = true;
			Define(TileType.POPPY_FIELD,"poppy field",'"',Color.Red,true,false,TileType.FLOOR);
			proto[TileType.POPPY_FIELD].revealed_by_light = true;
			Define(TileType.GRAVEL,"gravel",',',Color.DarkGray,true,false,null);
			proto[TileType.GRAVEL].revealed_by_light = true;
			Define(TileType.JUNGLE,"thick jungle",'&',Color.DarkGreen,true,true,null);
			Define(TileType.BLAST_FUNGUS,"blast fungus",'"',Color.DarkRed,true,false,TileType.FLOOR);
			proto[TileType.BLAST_FUNGUS].revealed_by_light = true;
			Define(TileType.GLOWING_FUNGUS,"glowing fungus",',',Color.RandomGlowingFungus,true,false,null);
			Prototype(TileType.GLOWING_FUNGUS).light_radius = 1;
			Define(TileType.TOMBSTONE,"tombstone",'+',Color.Gray,true,false,null);
			Define(TileType.GRAVE_DIRT,"grave dirt",',',Color.DarkYellow,true,false,null);
			proto[TileType.GRAVE_DIRT].a_name = "grave dirt";
			proto[TileType.GRAVE_DIRT].revealed_by_light = true;

			Define(FeatureType.GRENADE,"grenade",',',Color.Red);
			Define(FeatureType.QUICKFIRE,"quickfire",'&',Color.RandomFire);
			proto_feature[FeatureType.QUICKFIRE].a_name = "quickfire";
			Define(FeatureType.TROLL_CORPSE,"troll corpse",'%',Color.DarkGreen);
			Define(FeatureType.TROLL_SEER_CORPSE,"troll seer corpse",'%',Color.Cyan);
			Define(FeatureType.RUNE_OF_RETREAT,"rune of retreat",'&',Color.RandomDRGB);
			Define(FeatureType.POISON_GAS,"cloud of poison gas",'*',Color.DarkGreen);
			Define(FeatureType.FOG,"cloud of fog",'*',Color.Gray);
			Define(FeatureType.SLIME,"slime",',',Color.Green);
			proto_feature[FeatureType.SLIME].a_name = "slime";
			//Define(FeatureType.FUNGUS,"blast fungus",'"',Color.DarkRed);
			//Define(FeatureType.FUNGUS_ACTIVE,"blast fungus(active)",'"',Color.Red);
			//Define(FeatureType.FUNGUS_PRIMED,"blast fungus(exploding)",'"',Color.Yellow);
			Define(FeatureType.TELEPORTAL,"teleportal",'8',Color.White);
			Define(FeatureType.FIRE,"fire",'&',Color.RandomFire);
			proto_feature[FeatureType.FIRE].a_name = "fire";
			Define(FeatureType.OIL,"oil",',',Color.DarkYellow);
			proto_feature[FeatureType.OIL].a_name = "oil";

			//not an actual trap, but arena rooms, too. perhaps you'll see the opponent, in stasis.
				//"Touch the [tile]?(Y/N) "   if you touch it, you're stuck in the arena until one of you dies.
		}
		private static void Define(TileType type_,string name_,char symbol_,Color color_,bool passable_,bool opaque_,TileType? toggles_into_){
			proto[type_] = new Tile(type_,name_,symbol_,color_,passable_,opaque_,toggles_into_);
		}
		private static void Define(FeatureType type_,string name_,char symbol_,Color color_){
			proto_feature[type_] = new PhysicalObject(name_,symbol_,color_);
		}
		public Tile(){}
		public Tile(Tile t,int r,int c){
			type = t.type;
			name = t.name;
			a_name = t.a_name;
			the_name = t.the_name;
			symbol = t.symbol;
			color = t.color;
			if(t.type == TileType.BRUSH){
				if(Global.CoinFlip()){
					t.color = Color.Yellow;
				}
				if(Global.OneIn(20)){
					t.color = Color.Green;
				}
			}
			passable = t.passable;
			opaque = t.opaque;
			seen = false;
			revealed_by_light = t.revealed_by_light;
			solid_rock = false;
			light_value = 0;
			toggles_into = t.toggles_into;
			inv = null;
			row = r;
			col = c;
			light_radius = t.light_radius;
			direction_exited = 0;
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
			solid_rock = false;
			revealed_by_light = false;
			if(Is(TileType.STAIRS,TileType.CHEST,TileType.FIREPIT,TileType.HEALING_POOL)){
				revealed_by_light = true;
			}
			light_value = 0;
			toggles_into = toggles_into_;
			inv = null;
			light_radius = 0;
			direction_exited = 0;
		}
		public override string ToString(){
			return symbol.ToString();
		}
		public static Tile Create(TileType type,int r,int c){
			Tile t = null;
			if(M.tile[r,c] == null){
				t = new Tile(proto[type],r,c);
				M.tile[r,c] = t; //bounds checking here?
			}
			return t;
		}
		public static TileType RandomTrap(){
			int i = Global.Roll(12) + 7;
			return (TileType)i;
		}
		public static TileType RandomVent(){
			switch(Global.Roll(3)){
			case 1:
				return TileType.FIRE_GEYSER;
			case 2:
				return TileType.FOG_VENT;
			case 3:
			default:
				return TileType.POISON_GAS_VENT;
			}
		}
		public string Name(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			if(!consider_low_light || IsLit()){
				return name;
			}
			else{
				if(IsTrap()){
					return "trap";
				}
				if(IsShrine() || type == TileType.RUINED_SHRINE){
					return "shrine";
				}
				switch(type){
				case TileType.FIRE_GEYSER:
				case TileType.FOG_VENT:
				case TileType.POISON_GAS_VENT:
					return "crack in the floor";
				}
				return name;
			}
		}
		public string AName(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			if(!consider_low_light || IsLit()){
				return a_name;
			}
			else{
				if(IsTrap()){
					return "a trap";
				}
				if(IsShrine() || type == TileType.RUINED_SHRINE){
					return "a shrine";
				}
				switch(type){
				case TileType.FIRE_GEYSER:
				case TileType.FOG_VENT:
				case TileType.POISON_GAS_VENT:
					return "a crack in the floor";
				}
				return a_name;
			}
		}
		public string TheName(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			if(!consider_low_light || IsLit()){
				return the_name;
			}
			else{
				if(IsTrap()){
					return "the trap";
				}
				if(IsShrine() || type == TileType.RUINED_SHRINE){
					return "the shrine";
				}
				switch(type){
				case TileType.FIRE_GEYSER:
				case TileType.FOG_VENT:
				case TileType.POISON_GAS_VENT:
					return "the crack in the floor";
				}
				return the_name;
			}
		}
		public bool Is(TileType t){
			if(type == t){
				return true;
			}
			return false;
		}
		public bool Is(FeatureType t){
			foreach(FeatureType feature in features){
				if(feature == t){
					return true;
				}
			}
			return false;
		}
		public bool Is(params TileType[] types){
			foreach(TileType t in types){
				if(type == t){
					return true;
				}
			}
			return false;
		}
		public bool Is(params FeatureType[] types){
			foreach(FeatureType t1 in types){
				foreach(FeatureType t2 in features){
					if(t1 == t2){
						return true;
					}
				}
			}
			return false;
		}
		public char FeatureSymbol(){
			List<FeatureType> list = new List<FeatureType>{FeatureType.GRENADE,FeatureType.FIRE,FeatureType.QUICKFIRE,FeatureType.POISON_GAS,FeatureType.TELEPORTAL,FeatureType.FOG,FeatureType.TROLL_SEER_CORPSE,FeatureType.TROLL_CORPSE,FeatureType.RUNE_OF_RETREAT,FeatureType.OIL,FeatureType.SLIME};
			foreach(FeatureType ft in list){
				if(ft == FeatureType.OIL){ //special hack - important tile types (like stairs and traps) get priority over oil & slime
					if(IsKnownTrap() || IsShrine() || Is(TileType.CHEST,TileType.RUINED_SHRINE,TileType.STAIRS,TileType.BLAST_FUNGUS)){
						return symbol;
					}
				}
				if(Is(ft)){
					return Tile.Feature(ft).symbol;
				}
			}
			return symbol;
		}
		public Color FeatureColor(){
			List<FeatureType> list = new List<FeatureType>{FeatureType.GRENADE,FeatureType.FIRE,FeatureType.QUICKFIRE,FeatureType.POISON_GAS,FeatureType.TELEPORTAL,FeatureType.FOG,FeatureType.TROLL_SEER_CORPSE,FeatureType.TROLL_CORPSE,FeatureType.RUNE_OF_RETREAT,FeatureType.OIL,FeatureType.SLIME};
			foreach(FeatureType ft in list){
				if(ft == FeatureType.OIL){ //special hack - important tile types (like stairs and traps) get priority over oil & slime
					if(IsKnownTrap() || IsShrine() || Is(TileType.CHEST,TileType.RUINED_SHRINE,TileType.STAIRS,TileType.BLAST_FUNGUS)){
						return color;
					}
				}
				if(Is(ft)){
					return Tile.Feature(ft).color;
				}
			}
			return color;
		}
		public string Preposition(){
			switch(type){
			case TileType.FLOOR:
			case TileType.STAIRS:
				return " on ";
			case TileType.DOOR_O:
				return " in ";
			default:
				return " and ";
			}
		}
		public bool GetItem(Item item){
			if(inv == null && !Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS)){
				item.row = row;
				item.col = col;
				if(item.light_radius > 0){
					item.UpdateRadius(0,item.light_radius);
				}
				inv = item;
				return true;
			}
			else{
				if(!Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS) && inv.type == item.type && !inv.do_not_stack && !item.do_not_stack){
					inv.quantity += item.quantity;
					return true;
				}
				else{
					foreach(Tile t in M.ReachableTilesByDistance(row,col,false)){
						if(t.passable && t.inv == null && !t.Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS)){
							item.row = t.row;
							item.col = t.col;
							if(item.light_radius > 0){
								item.UpdateRadius(0,item.light_radius);
							}
							t.inv = item;
							return true;
						}
					}
					return false;
				}
			}
		}
		public void Toggle(Actor toggler){
			if(toggles_into != null){
				Toggle(toggler,toggles_into.Value);
			}
		}
		public void Toggle(Actor toggler,TileType toggle_to){
			bool lighting_update = false;
			List<PhysicalObject> light_sources = new List<PhysicalObject>();
			TileType original_type = type;
			if(opaque != Prototype(toggle_to).opaque){
				for(int i=row-1;i<=row+1;++i){
					for(int j=col-1;j<=col+1;++j){
						if(M.tile[i,j].IsLit(player.row,player.col,true)){
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
								light_sources.Add(M.actor[i,j]);
								M.actor[i,j].UpdateRadius(M.actor[i,j].LightRadius(),0);
							}
							if(M.tile[i,j].inv != null && M.tile[i,j].inv.light_radius > 0){
								light_sources.Add(M.tile[i,j].inv);
								M.tile[i,j].inv.UpdateRadius(M.tile[i,j].inv.light_radius,0);
							}
							if(M.tile[i,j].light_radius > 0){
								light_sources.Add(M.tile[i,j]);
								M.tile[i,j].UpdateRadius(M.tile[i,j].light_radius,0);
							}
						}
					}
				}
			}

			TransformTo(toggle_to);

			if(lighting_update){
				foreach(PhysicalObject o in light_sources){
					if(o is Actor){
						Actor a = o as Actor;
						a.UpdateRadius(0,a.LightRadius());
					}
					else{
						o.UpdateRadius(0,o.light_radius);
					}
				}
			}
			if(Prototype(type).revealed_by_light == true){
				revealed_by_light = true;
			}
			if(toggler != null && toggler != player){
				if(type == TileType.DOOR_C && original_type == TileType.DOOR_O){
					if(player.CanSee(this)){
						B.Add(toggler.TheName(true) + " closes the door. ");
					}
					else{
						if(seen || player.DistanceFrom(this) <= 6){
							B.Add("You hear a door closing. ");
						}
					}
				}
				if(type == TileType.DOOR_O && original_type == TileType.DOOR_C){
					if(player.CanSee(this)){
						B.Add(toggler.TheName(true) + " opens the door. ");
					}
					else{
						if(seen || player.DistanceFrom(this) <= 6){
							B.Add("You hear a door opening. ");
						}
					}
				}
			}
			if(toggler != null){
				if(original_type == TileType.RUBBLE){
					B.Add(toggler.YouVisible("scatter") + " the rubble. ",this);
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
			if(light_radius != Prototype(type_).light_radius){
				UpdateRadius(light_radius,Prototype(type_).light_radius);
			}
			light_radius = Prototype(type_).light_radius;
			revealed_by_light = false;
		}
		public void TurnToFloor(){
			bool lighting_update = false;
			List<PhysicalObject> light_sources = new List<PhysicalObject>();
			if(opaque){
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.IsLit(player.row,player.col,true)){
						lighting_update = true;
					}
				}
			}
			if(lighting_update){
				for(int i=row-Global.MAX_LIGHT_RADIUS;i<=row+Global.MAX_LIGHT_RADIUS;++i){
					for(int j=col-Global.MAX_LIGHT_RADIUS;j<=col+Global.MAX_LIGHT_RADIUS;++j){
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(M.actor[i,j] != null && M.actor[i,j].LightRadius() > 0){
								light_sources.Add(M.actor[i,j]);
								M.actor[i,j].UpdateRadius(M.actor[i,j].LightRadius(),0);
							}
							if(M.tile[i,j].inv != null && M.tile[i,j].inv.light_radius > 0){
								light_sources.Add(M.tile[i,j].inv);
								M.tile[i,j].inv.UpdateRadius(M.tile[i,j].inv.light_radius,0);
							}
							if(M.tile[i,j].light_radius > 0){
								light_sources.Add(M.tile[i,j]);
								M.tile[i,j].UpdateRadius(M.tile[i,j].light_radius,0);
							}
						}
					}
				}
			}
			
			TransformTo(TileType.FLOOR);
			
			if(lighting_update){
				foreach(PhysicalObject o in light_sources){
					if(o is Actor){
						Actor a = o as Actor;
						a.UpdateRadius(0,a.LightRadius());
					}
					else{
						o.UpdateRadius(0,o.light_radius);
					}
				}
			}
		}
		public void TriggerTrap(){ TriggerTrap(true); }
		public void TriggerTrap(bool click){
			bool actor_here = (actor() != null);
			if(actor_here && actor().type == ActorType.FIRE_DRAKE){
				if(name == "floor"){
					B.Add(actor().the_name + " smashes " + Tile.Prototype(type).a_name + ". ",this);
				}
				else{
					B.Add(actor().the_name + " smashes " + the_name + ". ",this);
				}
				TransformTo(TileType.FLOOR);
				return;
			}
			if(click && player.CanSee(this)){
				B.Add("*CLICK* ",this);
				B.PrintAll();
			}
			switch(type){
			case TileType.GRENADE_TRAP:
			{
				if(actor_here && player.CanSee(actor())){
					B.Add("Grenades fall from the ceiling above " + actor().the_name + "! ",this);
				}
				else{
					B.Add("Grenades fall from the ceiling! ",this);
				}
				List<Tile> valid = new List<Tile>();
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.passable && !t.Is(FeatureType.GRENADE)){
						valid.Add(t);
					}
				}
				int count = Global.OneIn(10)? 3 : 2;
				for(;count>0 & valid.Count > 0;--count){
					Tile t = valid.Random();
					if(t.actor() != null){
						if(t.actor() == player){
							B.Add("One lands under you! ");
						}
						else{
							if(player.CanSee(this)){
								B.Add("One lands under " + t.actor().the_name + ". ",t.actor());
							}
						}
					}
					else{
						if(t.inv != null){ //todo: this could also check for any features that hide grenades.
							B.Add("One lands under " + t.inv.TheName() + ". ",t);
						}
					}
					t.features.Add(FeatureType.GRENADE);
					valid.Remove(t);
					Q.Add(new Event(t,100,EventType.GRENADE));
				}
				Toggle(actor());
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
					ActorType ac = Global.CoinFlip()? ActorType.SKELETON : ActorType.ZOMBIE; //todo: change this based on depth?
					Actor.Create(ac,first.TileInDirection(dir).row,first.TileInDirection(dir).col,true,true);
					first.TurnToFloor();
					foreach(Tile t in first.TileInDirection(dir).TilesWithinDistance(1)){
						t.solid_rock = false;
					}
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
				if(actor_here){
					B.Add("An unstable energy covers " + actor().TheName(true) + ". ",actor());
					actor().attrs[AttrType.TELEPORTING] = Global.Roll(4);
					Q.KillEvents(actor(),AttrType.TELEPORTING); //should be replaced by refreshduration eventually. works the same way, though.
					Q.Add(new Event(actor(),actor().DurationOfMagicalEffect(Global.Roll(10)+25)*100,AttrType.TELEPORTING,actor().YouFeel() + " more stable. ",actor()));
				}
				else{
					B.Add("An unstable energy crackles for a moment, then dissipates. ",this);
				}
				Toggle(actor());
				break;
			case TileType.STUN_TRAP:
				if(actor_here && player.CanSee(actor())){
					B.Add("A disorienting flash assails " + actor().the_name + ". ",this); //todo: change message to "A (disorienting) light flashes! You are stunned." or so?
				}
				else{
					B.Add("You notice a flash of light. ",this);
				}
				if(actor_here){
					actor().RefreshDuration(AttrType.STUNNED,actor().DurationOfMagicalEffect(Global.Roll(10)+7)*100,(actor().YouFeel() + " less disoriented. "),(this.actor()));
				}
				Toggle(actor());
				break;
			case TileType.LIGHT_TRAP:
				if(M.wiz_lite == false){
					if(actor_here && player.HasLOS(row,col) && !actor().IsHiddenFrom(player)){
						B.Add("A wave of light washes out from above " + actor().the_name + "! ");
					}
					else{
						B.Add("A wave of light washes over the area! ");
					}
					M.wiz_lite = true;
					M.wiz_dark = false;
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((Global.Roll(2,20) + 200) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even brighter for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((Global.Roll(2,20) + 200) * 100,EventType.NORMAL_LIGHTING));
				}
				Toggle(actor());
				break;
			case TileType.DARKNESS_TRAP:
				if(M.wiz_dark == false){
					if(actor_here && player.CanSee(actor())){
						B.Add("A surge of darkness radiates out from above " + actor().the_name + "! ");
						if(player.light_radius > 0){
							B.Add("Your light is extinguished! ");
						}
					}
					else{
						B.Add("A surge of darkness radiates over the area! ");
						if(player.light_radius > 0){
							B.Add("Your light is extinguished! ");
						}
					}
					M.wiz_dark = true;
					M.wiz_lite = false;
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((Global.Roll(2,20) + 200) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even darker for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((Global.Roll(2,20) + 200) * 100,EventType.NORMAL_LIGHTING));
				}
				Toggle(actor());
				break;
			case TileType.QUICKFIRE_TRAP:
			{
				if(actor_here){
					B.Add("Fire pours over " + actor().TheName(true) + " and starts to spread! ",this);
					Actor a = actor();
					if(!a.HasAttr(AttrType.RESIST_FIRE) && !a.HasAttr(AttrType.CATCHING_FIRE) && !a.HasAttr(AttrType.ON_FIRE)
					&& !a.HasAttr(AttrType.IMMUNE_FIRE) && !a.HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){
						if(a == actor()){							// to work properly, 
							a.attrs[AttrType.STARTED_CATCHING_FIRE_THIS_TURN] = 1; //this would need to determine what actor's turn it is
						} //therefore, hack
						else{
							a.attrs[AttrType.CATCHING_FIRE] = 1;
						}
						if(player.CanSee(a.tile())){
							B.Add(a.You("start") + " to catch fire. ",a);
						}
					}
				}
				features.Add(FeatureType.QUICKFIRE);
				Toggle(actor());
				List<Tile> newarea = new List<Tile>();
				newarea.Add(this);
				Q.Add(new Event(this,newarea,100,EventType.QUICKFIRE,AttrType.NO_ATTR,3,""));
				break;
			}
			case TileType.ALARM_TRAP:
				if(actor() == player){
					B.Add("A high-pitched ringing sound reverberates from above you. ");
				}
				else{
					if(actor_here && player.CanSee(actor())){
						B.Add("A high-pitched ringing sound reverberates from above " + actor().the_name + ". ");
					}
					else{
						B.Add("You hear a high-pitched ringing sound. ");
					}
				}
				foreach(Actor a in ActorsWithinDistance(12,true)){
					if(a.type != ActorType.LARGE_BAT && a.type != ActorType.BLOOD_MOTH && a.type != ActorType.CARNIVOROUS_BRAMBLE
					&& a.type != ActorType.LASHER_FUNGUS && a.type != ActorType.PHASE_SPIDER){
						a.FindPath(this);
					}
				}
				Toggle(actor());
				break;
			case TileType.DIM_VISION_TRAP:
				if(actor_here){
					B.Add("A dart flies out and strikes " + actor().the_name + ". ",actor());
				}
				else{
					B.Add("A dart flies out and breaks on the floor. ",this);
				}
				if(actor() == player){
					B.Add("Your vision becomes weaker! ");
					actor().RefreshDuration(AttrType.DIM_VISION,actor().DurationOfMagicalEffect(Global.Roll(10) + 20) * 100,"Your vision returns to normal. ");
				}
				else{
					if(actor_here && !actor().HasAttr(AttrType.NONLIVING) && !actor().HasAttr(Forays.AttrType.BLINDSIGHT)
					&& actor().type != ActorType.BLOOD_MOTH){
						if(player.CanSee(actor())){
							B.Add(actor().the_name + " seems to have trouble seeing. ");
						}
						actor().RefreshDuration(AttrType.DIM_VISION,actor().DurationOfMagicalEffect(Global.Roll(10) + 20) * 100);
					}
				}
				Toggle(actor());
				break;
			case TileType.ICE_TRAP:
				if(actor_here){
					if(player.CanSee(this)){
						B.Add("The air suddenly freezes, encasing " + actor().TheName(true) + " in ice. ");
					}
					actor().attrs[AttrType.FROZEN] = 35;
				}
				Toggle(actor());
				break;
			case TileType.PHANTOM_TRAP:
			{
				Tile open = TilesWithinDistance(3).Where(t => t.passable && t.actor() == null && t.HasLOE(this)).Random();
				if(open != null){
					Actor a = Actor.CreatePhantom(open.row,open.col);
					if(a != null){
						a.attrs[AttrType.PLAYER_NOTICED]++;
						a.player_visibility_duration = -1;
						B.Add("A ghostly image rises! ",a);
					}
					else{
						B.Add("Nothing happens. ",this);
					}
				}
				else{
					B.Add("Nothing happens. ",this);
				}
				Toggle(actor());
				break;
			}
			case TileType.POISON_GAS_TRAP:
			{
				Tile current = this;
				int num = Global.Roll(5) + 7;
				List<Tile> new_area = new List<Tile>();
				for(int i=0;i<num;++i){
					if(!current.Is(FeatureType.POISON_GAS)){
						current.features.Add(FeatureType.POISON_GAS);
						new_area.Add(current);
					}
					else{
						for(int tries=0;tries<50;++tries){
							List<Tile> open = new List<Tile>();
							foreach(Tile t in current.TilesAtDistance(1)){
								if(t.passable){
									open.Add(t);
								}
							}
							if(open.Count > 0){
								Tile possible = open.Random();
								if(!possible.Is(FeatureType.POISON_GAS)){
									possible.features.Add(FeatureType.POISON_GAS);
									new_area.Add(possible);
									break;
								}
								else{
									current = possible;
								}
							}
							else{
								break;
							}
						}
					}
				}
				if(new_area.Count > 0){
					B.Add("Poisonous gas fills the area! ",this);
					Q.Add(new Event(new_area,300,EventType.POISON_GAS));
				}
				Toggle(actor());
				break;
			}
			default:
				break;
			}
		}
		/*public void TriggerTrap(){
			if(actor().type == ActorType.FIRE_DRAKE){
				if(name == "floor"){
					B.Add(actor().the_name + " smashes " + Tile.Prototype(type).a_name + ". ",this);
				}
				else{
					B.Add(actor().the_name + " smashes " + the_name + ". ",this);
				}
				TransformTo(TileType.FLOOR);
				return;
			}
			if(player.CanSee(this)){
				B.Add("*CLICK* ",this);
				B.PrintAll();
			}
			switch(type){
			case TileType.GRENADE_TRAP:
			{
				if(player.CanSee(actor())){
					B.Add("Grenades fall from the ceiling above " + actor().the_name + "! ",this);
				}
				else{
					B.Add("Grenades fall from the ceiling! ",this);
				}
				List<Tile> valid = new List<Tile>();
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.passable && !t.Is(FeatureType.GRENADE)){
						valid.Add(t);
					}
				}
				int count = Global.OneIn(10)? 3 : 2;
				for(;count>0 & valid.Count > 0;--count){
					Tile t = valid.Random();
					if(t.actor() != null){
						if(t.actor() == player){
							B.Add("One lands under you! ");
						}
						else{
							if(player.CanSee(this)){
								B.Add("One lands under " + t.actor().the_name + ". ",t.actor());
							}
						}
					}
					else{
						if(t.inv != null){
							B.Add("One lands under " + t.inv.TheName() + ". ",t);
						}
					}
					t.features.Add(FeatureType.GRENADE);
					valid.Remove(t);
					Q.Add(new Event(t,100,EventType.GRENADE));
				}
				Toggle(actor());
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
					Actor.Create(ac,first.TileInDirection(dir).row,first.TileInDirection(dir).col,true,true);
					first.TurnToFloor();
					foreach(Tile t in first.TileInDirection(dir).TilesWithinDistance(1)){
						t.solid_rock = false;
					}
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
				B.Add("An unstable energy covers " + actor().TheName(true) + ". ",actor());
				actor().attrs[AttrType.TELEPORTING] = Global.Roll(4);
				Q.KillEvents(actor(),AttrType.TELEPORTING); //should be replaced by refreshduration eventually. works the same way, though.
				Q.Add(new Event(actor(),actor().DurationOfMagicalEffect(Global.Roll(10)+25)*100,AttrType.TELEPORTING,actor().YouFeel() + " more stable. ",actor()));
				Toggle(actor());
				break;
			case TileType.STUN_TRAP:
				if(player.CanSee(actor())){
					B.Add("A disorienting flash assails " + actor().the_name + ". ",this);
				}
				else{
					B.Add("You notice a flash of light. ",this);
				}
				actor().GainAttrRefreshDuration(AttrType.STUNNED,actor().DurationOfMagicalEffect(Global.Roll(10)+7)*100,(actor().YouFeel() + " less disoriented. "),(this.actor()));
				Toggle(actor());
				break;
			case TileType.LIGHT_TRAP:
				if(M.wiz_lite == false){
					if(player.HasLOS(row,col) && !actor().IsHiddenFrom(player)){
						B.Add("A wave of light washes out from above " + actor().the_name + "! ");
					}
					else{
						B.Add("A wave of light washes over the area! ");
					}
					M.wiz_lite = true;
					M.wiz_dark = false;
				}
				else{
					B.Add("Nothing happens. ",this);
				}
				Toggle(actor());
				break;
			case TileType.DARKNESS_TRAP:
				if(M.wiz_dark == false){
					if(player.CanSee(actor())){
						B.Add("A surge of darkness radiates out from above " + actor().the_name + "! ");
						if(player.light_radius > 0){
							B.Add("Your light is extinguished! ");
						}
					}
					else{
						B.Add("A surge of darkness extinguishes all light in the area! ");
					}
					M.wiz_dark = true;
					M.wiz_lite = false;
				}
				else{
					B.Add("Nothing happens. ",this);
				}
				Toggle(actor());
				break;
			case TileType.QUICKFIRE_TRAP:
			{
				B.Add("Fire pours over " + actor().TheName(true) + " and starts to spread! ",this);
				Actor a = actor();
				if(!a.HasAttr(AttrType.RESIST_FIRE) && !a.HasAttr(AttrType.CATCHING_FIRE) && !a.HasAttr(AttrType.ON_FIRE)
				&& !a.HasAttr(AttrType.IMMUNE_FIRE) && !a.HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){
					if(a == actor()){							// to work properly, 
						a.attrs[AttrType.STARTED_CATCHING_FIRE_THIS_TURN] = 1; //this would need to determine what actor's turn it is
					} //therefore, hack
					else{
						a.attrs[AttrType.CATCHING_FIRE] = 1;
					}
					if(player.CanSee(a.tile())){
						B.Add(a.You("start") + " to catch fire. ",a);
					}
				}
				features.Add(FeatureType.QUICKFIRE);
				Toggle(actor());
				List<Tile> newarea = new List<Tile>();
				newarea.Add(this);
				Q.Add(new Event(this,newarea,100,EventType.QUICKFIRE,AttrType.NO_ATTR,3,""));
				break;
			}
			case TileType.ALARM_TRAP:
				if(actor() == player){
					B.Add("A high-pitched ringing sound reverberates from above you. ");
				}
				else{
					if(player.CanSee(actor())){
						B.Add("A high-pitched ringing sound reverberates from above " + actor().the_name + ". ");
					}
					else{
						B.Add("You hear a high-pitched ringing sound. ");
					}
				}
				foreach(Actor a in ActorsWithinDistance(12,true)){
					if(a.type != ActorType.LARGE_BAT && a.type != ActorType.BLOOD_MOTH && a.type != ActorType.CARNIVOROUS_BRAMBLE
					&& a.type != ActorType.LASHER_FUNGUS && a.type != ActorType.PHASE_SPIDER){
						a.FindPath(this);
					}
				}
				Toggle(actor());
				break;
			case TileType.DIM_VISION_TRAP:
				B.Add("A dart strikes " + actor().the_name + ". ",actor());
				if(actor() == player){
					B.Add("Your vision becomes weaker! ");
					actor().GainAttrRefreshDuration(AttrType.DIM_VISION,actor().DurationOfMagicalEffect(Global.Roll(10) + 20) * 100,"Your vision returns to normal. ");
				}
				else{
					if(!actor().HasAttr(AttrType.IMMUNE_TOXINS) && !actor().HasAttr(AttrType.UNDEAD) && !actor().HasAttr(Forays.AttrType.BLINDSIGHT)
					&& actor().type != ActorType.BLOOD_MOTH && actor().type != ActorType.PHASE_SPIDER){
						if(player.CanSee(actor())){
							B.Add(actor().the_name + " seems to have trouble seeing. ");
						}
						actor().GainAttrRefreshDuration(AttrType.DIM_VISION,actor().DurationOfMagicalEffect(Global.Roll(10) + 20) * 100);
					}
				}
				Toggle(actor());
				break;
			case TileType.ICE_TRAP:
				if(player.CanSee(this)){
					B.Add("The air suddenly freezes, encasing " + actor().TheName(true) + " in ice. ");
				}
				actor().attrs[AttrType.FROZEN] = 25;
				Toggle(actor());
				break;
			case TileType.PHANTOM_TRAP:
			{
				Tile open = TilesWithinDistance(3).Where(t => t.passable && t.actor() == null && t.HasLOE(this)).Random();
				if(open != null){
					Actor a = Actor.CreatePhantom(open.row,open.col);
					if(a != null){
						a.attrs[AttrType.PLAYER_NOTICED]++;
						a.player_visibility_duration = -1;
						B.Add("A ghostly image rises! ",a);
					}
					else{
						B.Add("Nothing happens. ",this);
					}
				}
				else{
					B.Add("Nothing happens. ",this);
				}
				Toggle(actor());
				break;
			}
			case TileType.POISON_GAS_TRAP:
			{
				Tile current = this;
				int num = Global.Roll(5) + 7;
				List<Tile> new_area = new List<Tile>();
				for(int i=0;i<num;++i){
					if(!current.Is(FeatureType.POISON_GAS)){
						current.features.Add(FeatureType.POISON_GAS);
						new_area.Add(current);
					}
					else{
						for(int tries=0;tries<50;++tries){
							List<Tile> open = new List<Tile>();
							foreach(Tile t in current.TilesAtDistance(1)){
								if(t.passable){
									open.Add(t);
								}
							}
							if(open.Count > 0){
								Tile possible = open.Random();
								if(!possible.Is(FeatureType.POISON_GAS)){
									possible.features.Add(FeatureType.POISON_GAS);
									new_area.Add(possible);
									break;
								}
								else{
									current = possible;
								}
							}
							else{
								break;
							}
						}
					}
				}
				if(new_area.Count > 0){
					B.Add("Poisonous gas fills the area! ",this);
					Q.Add(new Event(new_area,300,EventType.POISON_GAS));
				}
				Toggle(actor());
				break;
			}
			default:
				break;
			}
		}*/
		public void OpenChest(){
			if(type == TileType.CHEST){
				if(Global.Roll(1,10) == 10){
					List<int> upgrades = new List<int>();
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
					case 8: //pendant of life
						player.magic_items.Add(MagicItemType.PENDANT_OF_LIFE);
						break;
					case 9: //ring of resistance
						player.magic_items.Add(MagicItemType.RING_OF_RESISTANCE);
						break;
					case 10: //ring of protection
						player.magic_items.Add(MagicItemType.RING_OF_PROTECTION);
						break;
					case 11: //cloak of disappearance
						player.magic_items.Add(MagicItemType.CLOAK_OF_DISAPPEARANCE);
						break;
					default:
						break;
					}
					if(player.magic_items.Count == 1){
						player.equipped_magic_item_idx = 0;
					}
					if(upgrade <= 4){
					}
					else{
						if(upgrade <= 7){
						}
						else{
							B.Add("You find a " + MagicItem.Name((MagicItemType)(upgrade-8)) + "! ");
						}
					}
				}
				else{
					bool no_room = false;
					if(player.InventoryCount() >= Global.MAX_INVENTORY_SIZE){
						no_room = true;
					}
					Item i = Item.Create(Item.RandomItem(),player);
					if(i != null){
						B.Add("You find " + Item.Prototype(i.type).AName() + ". ");
						if(no_room){
							B.Add("Your pack is too full to pick it up. ");
						}
					}
				}
				TurnToFloor();
			}
		}
		public bool IsLit(){ //default is player as viewer
			return IsLit(player.row,player.col,false);
		}
		public bool IsLit(int viewer_row,int viewer_col,bool ignore_wizlite_wizdark){
			if(!ignore_wizlite_wizdark){
				if(M.wiz_lite){
					return true;
				}
				if(M.wiz_dark){
					return false;
				}
			}
			if(light_value > 0){
				return true;
			}
			if(features.Contains(FeatureType.QUICKFIRE) || features.Contains(FeatureType.FIRE)){
				return true;
			}
			if(opaque){
				foreach(Tile t in NeighborsBetween(viewer_row,viewer_col)){
					if(t.light_value > 0){
						return true;
					}
				}
				if(M.actor[viewer_row,viewer_col] != null && M.actor[viewer_row,viewer_col].LightRadius() > 0){
					if(M.actor[viewer_row,viewer_col].LightRadius() >= DistanceFrom(viewer_row,viewer_col)){
						if(M.actor[viewer_row,viewer_col].HasBresenhamLine(row,col)){
							return true;
						}
					}
				}
			}
			return false;
		}
		public bool IsLitFromAnywhere(){ return IsLitFromAnywhere(opaque); }
		public bool IsLitFromAnywhere(bool considered_opaque){
			if(M.wiz_lite){
				return true;
			}
			if(M.wiz_dark){
				return false;
			}
			if(light_value > 0){
				return true;
			}
			if(features.Contains(FeatureType.QUICKFIRE)){
				return true;
			}
			if(considered_opaque){
				foreach(Tile t in TilesAtDistance(1)){
					if(t.light_value > 0){
						return true;
					}
				}
				foreach(Actor a in ActorsWithinDistance(Global.MAX_LIGHT_RADIUS)){
					if(a.LightRadius() > 0 && a.LightRadius() >= a.DistanceFrom(this) && a.HasBresenhamLine(row,col)){
						return true;
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
			case TileType.ALARM_TRAP:
			case TileType.DARKNESS_TRAP:
			case TileType.DIM_VISION_TRAP:
			case TileType.ICE_TRAP:
			case TileType.PHANTOM_TRAP:
			case TileType.POISON_GAS_TRAP:
				return true;
			default:
				return false;
			}
		}
		public bool IsTrapOrVent(){
			return IsTrap() || type == TileType.FIRE_GEYSER || type == TileType.FOG_VENT || type == TileType.POISON_GAS_VENT;
		}
		public bool IsKnownTrap(){
			if(IsTrap() && name != "floor"){
				return true;
			}
			return false;
		}
		public bool IsShrine(){
			switch(type){
			case TileType.COMBAT_SHRINE:
			case TileType.DEFENSE_SHRINE:
			case TileType.MAGIC_SHRINE:
			case TileType.SPIRIT_SHRINE:
			case TileType.STEALTH_SHRINE:
			case TileType.SPELL_EXCHANGE_SHRINE:
				return true;
			default:
				return false;
			}
		}
		public bool ConductsElectricity(){
			if(IsShrine() || type == TileType.CHEST || type == TileType.RUINED_SHRINE){
				return true;
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
		public void ApplyEffect(DamageType effect){
			switch(effect){
			case DamageType.FIRE:
			{
				if(Is(FeatureType.OIL)){
					features.Remove(FeatureType.OIL);
					features.Add(FeatureType.FIRE);
				}
				if(Is(TileType.WATER)){
					return;
				}
				if(Is(FeatureType.TROLL_CORPSE)){
					//todo
					B.Add("TODO: troll corpse burns. ");
				}
				if(Is(FeatureType.TROLL_SEER_CORPSE)){
					//todo
					B.Add("TODO: troll seer corpse burns. ");
				}
				if(inv != null && inv.NameOfItemType() == "scroll"){
					B.Add("TODO: scroll burns. ");
					//todo
				}
				break;
			}
			case DamageType.ELECTRIC:
			{
				break;
			}
			case DamageType.COLD:
			{
				if(Is(FeatureType.SLIME)){
					features.Remove(FeatureType.SLIME);
				}
				if(Is(TileType.WATER)){
					Toggle(null,TileType.ICE);
				}
				break;
			}
			case DamageType.NORMAL:
			{
				if(inv != null){
					if(inv.NameOfItemType() == "potion"){
						B.Add("TODO: potion breaks. ");
					}
					else{ //todo
						if(inv.NameOfItemType() == "orb"){
							B.Add("TODO: orb breaks. ");
						}
					}
				}
				if(type == TileType.CRACKED_WALL){
					B.Add("TODO: cracked wall falls. ");
				}
				if(type == TileType.HIDDEN_DOOR){
					B.Add("TODO: hidden door opened. ");
				}
				if(type == TileType.DOOR_C){
					B.Add("TODO: door opened. ");
				}
				break;
			}
			}
		}
		public void AddFeature(FeatureType f){
			if(!features.Contains(f)){
				switch(f){
				case FeatureType.FOG:
					if(!Is(FeatureType.FIRE)){
						AddOpaqueFeature(FeatureType.FOG);
					}
					break;
				case FeatureType.OIL:
					if(Is(FeatureType.SLIME,FeatureType.FIRE) || Is(TileType.ICE,TileType.WATER,TileType.HEALING_POOL,TileType.CHASM,TileType.BRUSH,TileType.POPPY_FIELD)){ //todo: all other brushlike tiles that don't take oil or slime go here
						return;
					}
					if(type == TileType.FIREPIT){
						features.Add(FeatureType.FIRE);
					}
					else{
						features.Add(FeatureType.OIL);
					}
					break;
				case FeatureType.FIRE:
					if(Is(FeatureType.SLIME)){
						return;
					}
					if(Is(FeatureType.FOG)){
						RemoveOpaqueFeature(FeatureType.FOG);
					}
					if(Is(TileType.WATER,TileType.HEALING_POOL)){
						return;
					}
					if(type == TileType.ICE){
						Toggle(null,TileType.WATER);
					}
					if(Is(FeatureType.TROLL_CORPSE)){
						B.Add("TODO: troll corpse burns. ");
					}
					if(Is(FeatureType.TROLL_SEER_CORPSE)){
						B.Add("TODO: troll corpse burns. ");
					}
					if(type == TileType.POPPY_FIELD){ //or a general check for burnable terrain, todo
					}
					//yeah, and if there's no oil or burnable terrain, no fire gets placed anyway, i think
					break;
				case FeatureType.SLIME:
					if(Is(TileType.ICE,TileType.WATER,TileType.HEALING_POOL,TileType.CHASM,TileType.BRUSH,TileType.POPPY_FIELD)){//todo! all other brushlike stuff
						return;
					}
					if(Is(FeatureType.FIRE)){
						features.Remove(FeatureType.FIRE);
					}
					if(Is(FeatureType.OIL)){
						features.Remove(FeatureType.OIL);
					}
					features.Add(FeatureType.SLIME);
					break;
				default:
					features.Add(f);
					break;
				}
			}
		}
		public void RemoveFeature(FeatureType f){
			if(features.Contains(f)){
				switch(f){
				case FeatureType.FOG:
					RemoveOpaqueFeature(FeatureType.FOG);
					break;
				default:
					features.Remove(f);
					break;
				}
			}
		}
		private void AddOpaqueFeature(FeatureType f){
			if(!features.Contains(f)){
				bool lighting_update = false;
				List<PhysicalObject> light_sources = new List<PhysicalObject>();
				for(int i=row-1;i<=row+1;++i){
					for(int j=col-1;j<=col+1;++j){
						if(M.tile[i,j].IsLit(player.row,player.col,true)){
							lighting_update = true;
						}
					}
				}
				if(lighting_update){
					for(int i=row-Global.MAX_LIGHT_RADIUS;i<=row+Global.MAX_LIGHT_RADIUS;++i){
						for(int j=col-Global.MAX_LIGHT_RADIUS;j<=col+Global.MAX_LIGHT_RADIUS;++j){
							if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
								if(M.actor[i,j] != null && M.actor[i,j].LightRadius() > 0){
									light_sources.Add(M.actor[i,j]);
									M.actor[i,j].UpdateRadius(M.actor[i,j].LightRadius(),0);
								}
								if(M.tile[i,j].inv != null && M.tile[i,j].inv.light_radius > 0){
									light_sources.Add(M.tile[i,j].inv);
									M.tile[i,j].inv.UpdateRadius(M.tile[i,j].inv.light_radius,0);
								}
								if(M.tile[i,j].light_radius > 0){
									light_sources.Add(M.tile[i,j]);
									M.tile[i,j].UpdateRadius(M.tile[i,j].light_radius,0);
								}
							}
						}
					}
				}
	
				features.Add(f);
	
				if(lighting_update){
					foreach(PhysicalObject o in light_sources){
						if(o is Actor){
							Actor a = o as Actor;
							a.UpdateRadius(0,a.LightRadius());
						}
						else{
							o.UpdateRadius(0,o.light_radius);
						}
					}
				}
			}
		}
		private void RemoveOpaqueFeature(FeatureType f){
			if(features.Contains(f)){
				bool lighting_update = false;
				List<PhysicalObject> light_sources = new List<PhysicalObject>();
				for(int i=row-1;i<=row+1;++i){
					for(int j=col-1;j<=col+1;++j){
						if(M.tile[i,j].IsLit(player.row,player.col,true)){
							lighting_update = true;
						}
					}
				}
				if(lighting_update){
					for(int i=row-Global.MAX_LIGHT_RADIUS;i<=row+Global.MAX_LIGHT_RADIUS;++i){
						for(int j=col-Global.MAX_LIGHT_RADIUS;j<=col+Global.MAX_LIGHT_RADIUS;++j){
							if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
								if(M.actor[i,j] != null && M.actor[i,j].LightRadius() > 0){
									light_sources.Add(M.actor[i,j]);
									M.actor[i,j].UpdateRadius(M.actor[i,j].LightRadius(),0);
								}
								if(M.tile[i,j].inv != null && M.tile[i,j].inv.light_radius > 0){
									light_sources.Add(M.tile[i,j].inv);
									M.tile[i,j].inv.UpdateRadius(M.tile[i,j].inv.light_radius,0);
								}
								if(M.tile[i,j].light_radius > 0){
									light_sources.Add(M.tile[i,j]);
									M.tile[i,j].UpdateRadius(M.tile[i,j].light_radius,0);
								}
							}
						}
					}
				}
	
				features.Remove(f);
	
				if(lighting_update){
					foreach(PhysicalObject o in light_sources){
						if(o is Actor){
							Actor a = o as Actor;
							a.UpdateRadius(0,a.LightRadius());
						}
						else{
							o.UpdateRadius(0,o.light_radius);
						}
					}
				}
			}
		}
	}
}

