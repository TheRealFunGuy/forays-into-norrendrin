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
using Utilities;
namespace Forays{
	public class Tile : PhysicalObject{
		public TileType type{get;set;}
		public bool passable{get;set;}
		public bool opaque{get{ return internal_opaque || features.Contains(FeatureType.FOG); } set{ internal_opaque = value; }}
		private bool internal_opaque; //no need to ever access this directly
		public bool seen{get;set;}
		public bool revealed_by_light{get;set;}
		public bool solid_rock{get;set;} //used for walls that will never be seen, to speed up LOS checks
		public int light_value{
			get{
				return internal_light_value;
			}
			set{
				internal_light_value = value;
				if(value > 0 && type == TileType.BLAST_FUNGUS && !M.wiz_dark){
					B.Add("The blast fungus starts to smolder in the light. ",this);
					Toggle(null);
					if(inv == null){ //should always be true
						Item.Create(ConsumableType.BLAST_FUNGUS,row,col);
						inv.other_data = 3;
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
		private List<FeatureType> feature_priority = new List<FeatureType>{FeatureType.GRENADE,FeatureType.FIRE,FeatureType.SPORES,FeatureType.POISON_GAS,FeatureType.PIXIE_DUST,FeatureType.TELEPORTAL,FeatureType.STABLE_TELEPORTAL,FeatureType.FOG,FeatureType.WEB,FeatureType.TROLL_BLOODWITCH_CORPSE,FeatureType.TROLL_CORPSE,FeatureType.BONES,FeatureType.INACTIVE_TELEPORTAL,FeatureType.OIL,FeatureType.SLIME,FeatureType.FORASECT_EGG};
		
		private static Dictionary<TileType,Tile> proto= new Dictionary<TileType, Tile>();
		public static Tile Prototype(TileType type){ return proto[type]; }
		private static Dictionary<FeatureType,PhysicalObject> proto_feature = new Dictionary<FeatureType, PhysicalObject>();
		public static PhysicalObject Feature(FeatureType type){ return proto_feature[type]; }
		static Tile(){
			Define(TileType.FLOOR,"floor",'.',Color.White,true,false,null);
			Define(TileType.WALL,"wall",'#',Color.Gray,false,true,null);
			Define(TileType.DOOR_C,"closed door",'+',Color.DarkYellow,false,true,TileType.DOOR_O);
			Define(TileType.DOOR_O,"open door",'-',Color.DarkYellow,true,false,TileType.DOOR_C);
			Define(TileType.STAIRS,"stairway",'>',Color.White,true,false,null);
			proto[TileType.STAIRS].revealed_by_light = true;
			Define(TileType.CHEST,"treasure chest",'=',Color.DarkYellow,true,false,null);
			Define(TileType.FIREPIT,"fire pit",'0',Color.Red,true,false,null);
			proto[TileType.FIREPIT].light_radius = 1;
			proto[TileType.FIREPIT].revealed_by_light = true;
			Define(TileType.STALAGMITE,"stalagmite",'^',Color.White,false,true,TileType.FLOOR);
			Define(TileType.FIRE_TRAP,"fire trap",'^',Color.RandomFire,true,false,TileType.FLOOR);
			Define(TileType.LIGHT_TRAP,"sunlight trap",'^',Color.Yellow,true,false,TileType.FLOOR);
			Define(TileType.TELEPORT_TRAP,"teleport trap",'^',Color.Magenta,true,false,TileType.FLOOR);
			Define(TileType.SLIDING_WALL_TRAP,"sliding wall trap",'^',Color.DarkCyan,true,false,TileType.FLOOR);
			Define(TileType.GRENADE_TRAP,"grenade trap",'^',Color.DarkGray,true,false,TileType.FLOOR);
			Define(TileType.SHOCK_TRAP,"shock trap",'^',Color.RandomLightning,true,false,TileType.FLOOR);
			Define(TileType.ALARM_TRAP,"alarm trap",'^',Color.White,true,false,TileType.FLOOR);
			Define(TileType.DARKNESS_TRAP,"darkness trap",'^',Color.Blue,true,false,TileType.FLOOR);
			Define(TileType.POISON_GAS_TRAP,"poison gas trap",'^',Color.Green,true,false,TileType.FLOOR);
			Define(TileType.BLINDING_TRAP,"blinding trap",'^',Color.DarkMagenta,true,false,TileType.FLOOR);
			Define(TileType.ICE_TRAP,"ice trap",'^',Color.RandomIce,true,false,TileType.FLOOR);
			Define(TileType.PHANTOM_TRAP,"phantom trap",'^',Color.Cyan,true,false,TileType.FLOOR);
			Define(TileType.SCALDING_OIL_TRAP,"scalding oil trap",'^',Color.DarkYellow,true,false,TileType.FLOOR);
			Define(TileType.HIDDEN_DOOR,"wall",'#',Color.Gray,false,true,TileType.DOOR_C);
			Define(TileType.RUBBLE,"pile of rubble",':',Color.Gray,false,false,TileType.FLOOR);
			Define(TileType.COMBAT_SHRINE,"shrine of combat",'_',Color.DarkRed,true,false,TileType.RUINED_SHRINE);
			Define(TileType.DEFENSE_SHRINE,"shrine of defense",'_',Color.White,true,false,TileType.RUINED_SHRINE);
			Define(TileType.MAGIC_SHRINE,"shrine of magic",'_',Color.Magenta,true,false,TileType.RUINED_SHRINE);
			Define(TileType.SPIRIT_SHRINE,"shrine of spirit",'_',Color.Yellow,true,false,TileType.RUINED_SHRINE);
			Define(TileType.STEALTH_SHRINE,"shrine of stealth",'_',Color.Blue,true,false,TileType.RUINED_SHRINE);
			Define(TileType.RUINED_SHRINE,"ruined shrine",'_',Color.DarkGray,true,false,null);
			Define(TileType.SPELL_EXCHANGE_SHRINE,"spell exchange shrine",'_',Color.DarkMagenta,true,false,TileType.RUINED_SHRINE);
			Define(TileType.FIRE_GEYSER,"fire geyser",'~',Color.Red,true,false,null);
			Prototype(TileType.FIRE_GEYSER).revealed_by_light = true;
			Define(TileType.STATUE,"statue",'5',Color.Gray,false,false,null);
			Define(TileType.POOL_OF_RESTORATION,"pool of restoration",'0',Color.Cyan,true,false,TileType.FLOOR);
			proto[TileType.POOL_OF_RESTORATION].revealed_by_light = true;
			Define(TileType.FOG_VENT,"fog vent",'~',Color.Gray,true,false,null);
			Prototype(TileType.FOG_VENT).revealed_by_light = true;
			Define(TileType.POISON_GAS_VENT,"poison gas vent",'~',Color.DarkGreen,true,false,null);
			Prototype(TileType.POISON_GAS_VENT).revealed_by_light = true;
			Define(TileType.STONE_SLAB,"stone slab",'#',Color.White,false,true,null);
			proto[TileType.STONE_SLAB].revealed_by_light = true;
			Define(TileType.CHASM,"chasm",'\'',Color.DarkBlue,true,false,null);
			Define(TileType.BREACHED_WALL,"floor",'.',Color.RandomBreached,true,false,TileType.WALL);
			Define(TileType.CRACKED_WALL,"cracked wall",'#',Color.DarkGreen,false,true,TileType.FLOOR);
			proto[TileType.CRACKED_WALL].revealed_by_light = true;
			Define(TileType.BRUSH,"brush",'"',Color.DarkYellow,true,false,TileType.FLOOR);
			proto[TileType.BRUSH].a_name = "brush";
			proto[TileType.BRUSH].revealed_by_light = true;
			Define(TileType.WATER,"shallow water",'~',Color.Blue,true,false,null);
			proto[TileType.WATER].a_name = "shallow water";
			Prototype(TileType.WATER).revealed_by_light = true;
			Define(TileType.ICE,"ice",'~',Color.Cyan,true,false,null);
			proto[TileType.ICE].a_name = "ice";
			proto[TileType.ICE].revealed_by_light = true;
			Define(TileType.POPPY_FIELD,"poppy field",'"',Color.Red,true,false,TileType.FLOOR);
			proto[TileType.POPPY_FIELD].revealed_by_light = true;
			Define(TileType.GRAVEL,"gravel",',',Color.DarkGray,true,false,null);
			proto[TileType.GRAVEL].revealed_by_light = true;
			Define(TileType.JUNGLE,"thick jungle",'&',Color.DarkGreen,true,true,null); //unused
			Define(TileType.BLAST_FUNGUS,"blast fungus",'"',Color.DarkRed,true,false,TileType.FLOOR);
			proto[TileType.BLAST_FUNGUS].revealed_by_light = true;
			Define(TileType.GLOWING_FUNGUS,"glowing fungus",',',Color.RandomGlowingFungus,true,false,null);
			Prototype(TileType.GLOWING_FUNGUS).revealed_by_light = true;
			Define(TileType.TOMBSTONE,"tombstone",'+',Color.Gray,true,false,null);
			proto[TileType.TOMBSTONE].revealed_by_light = true;
			Define(TileType.GRAVE_DIRT,"grave dirt",'\'',Color.DarkYellow,true,false,null);
			proto[TileType.GRAVE_DIRT].a_name = "grave dirt";
			proto[TileType.GRAVE_DIRT].revealed_by_light = true;
			Define(TileType.BARREL,"barrel of oil",'0',Color.DarkYellow,false,false,TileType.FLOOR);
			Prototype(TileType.BARREL).revealed_by_light = true;
			Define(TileType.STANDING_TORCH,"standing torch",'|',Color.RandomTorch,false,false,TileType.FLOOR);
			Prototype(TileType.STANDING_TORCH).light_radius = 1;
			Prototype(TileType.STANDING_TORCH).revealed_by_light = true;
			Define(TileType.VINE,"vine",';',Color.DarkGreen,true,false,null);
			Prototype(TileType.VINE).revealed_by_light = true;
			Define(TileType.POISON_BULB,"poison bulb",'%',Color.Green,false,false,null);
			Prototype(TileType.POISON_BULB).revealed_by_light = true;
			Define(TileType.WAX_WALL,"wax wall",'#',Color.DarkYellow,false,true,null);
			Prototype(TileType.WAX_WALL).revealed_by_light = true;

			Define(FeatureType.GRENADE,"grenade",',',Color.Red);
			Define(FeatureType.TROLL_CORPSE,"troll corpse",'%',Color.DarkGreen);
			Define(FeatureType.TROLL_BLOODWITCH_CORPSE,"troll bloodwitch corpse",'%',Color.DarkRed);
			Define(FeatureType.POISON_GAS,"cloud of poison gas",'*',Color.DarkGreen);
			Define(FeatureType.FOG,"cloud of fog",'*',Color.Gray);
			Define(FeatureType.SLIME,"slime",',',Color.Green);
			proto_feature[FeatureType.SLIME].a_name = "slime";
			Define(FeatureType.TELEPORTAL,"teleportal",'8',Color.White);
			Define(FeatureType.INACTIVE_TELEPORTAL,"inactive teleportal",'8',Color.Gray);
			Define(FeatureType.STABLE_TELEPORTAL,"stable teleportal",'8',Color.Magenta);
			Define(FeatureType.FIRE,"fire",'&',Color.RandomFire);
			proto_feature[FeatureType.FIRE].a_name = "fire";
			proto_feature[FeatureType.FIRE].light_radius = 1; //this isn't actually functional
			Define(FeatureType.OIL,"oil",',',Color.DarkYellow);
			proto_feature[FeatureType.OIL].a_name = "oil";
			Define(FeatureType.BONES,"pile of bones",'%',Color.White);
			Define(FeatureType.WEB,"web",';',Color.White);
			Define(FeatureType.PIXIE_DUST,"cloud of pixie dust",'*',Color.RandomGlowingFungus); //might need to change this name
			Define(FeatureType.FORASECT_EGG,"forasect egg",'%',Color.DarkGray);
			Define(FeatureType.SPORES,"cloud of spores",'*',Color.DarkYellow);
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
				if(R.CoinFlip()){
					color = Color.Yellow;
				}
				if(R.OneIn(20)){
					color = Color.Green;
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
			if(Is(TileType.STAIRS,TileType.CHEST,TileType.FIREPIT,TileType.POOL_OF_RESTORATION)){
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
			int i = R.Roll(13) + 7;
			return (TileType)i;
		}
		public string Name(bool consider_low_light){
			if(revealed_by_light){
				consider_low_light = false;
			}
			if(!consider_low_light || IsLit()){
				return name;
			}
			else{
				if(IsKnownTrap()){
					return "trap";
				}
				if(IsShrine() || type == TileType.RUINED_SHRINE){
					return "shrine";
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
				if(IsKnownTrap()){
					return "a trap";
				}
				if(IsShrine() || type == TileType.RUINED_SHRINE){
					return "a shrine";
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
				if(IsKnownTrap()){
					return "the trap";
				}
				if(IsShrine() || type == TileType.RUINED_SHRINE){
					return "the shrine";
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
			foreach(FeatureType ft in feature_priority){
				if(ft == FeatureType.OIL){ //special hack - important tile types (like stairs and traps) get priority over oil & slime
					if(IsKnownTrap() || IsShrine() || Is(TileType.CHEST,TileType.RUINED_SHRINE,TileType.STAIRS,TileType.BLAST_FUNGUS)){
						return symbol;
					}
				}
				if(Is(ft)){
					if(ft == FeatureType.FIRE){
						if(type == TileType.BARREL){
							return '0';
						}
						if(type == TileType.FIRE_GEYSER){
							return '~';
						}
					}
					if(ft == FeatureType.OIL && type == TileType.WATER){
						return '~';
					}
					return Tile.Feature(ft).symbol;
				}
			}
			return symbol;
		}
		public Color FeatureColor(){
			foreach(FeatureType ft in feature_priority){
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
			if(item.type == ConsumableType.BLAST_FUNGUS && (Is(TileType.WATER) || Is(FeatureType.SLIME))){
				B.Add("The blast fungus is doused. ",this);
				return true;
			}
			if(inv == null && !Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS)){
				if((IsBurning() || (Is(TileType.FIREPIT) && !Is(FeatureType.SLIME))) && (item.NameOfItemType() == "scroll" || item.type == ConsumableType.BANDAGES)){
					B.Add(item.TheName(true) + " burns up! ",this);
					if(Is(TileType.FIREPIT)){
						AddFeature(FeatureType.FIRE);
					}
				}
				else{
					item.row = row;
					item.col = col;
					if(item.light_radius > 0){
						item.UpdateRadius(0,item.light_radius);
					}
					inv = item;
				}
				return true;
			}
			else{
				if(!Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS) && inv.type == item.type && !inv.do_not_stack && !item.do_not_stack){
					inv.quantity += item.quantity;
					return true;
				}
				else{
					foreach(Tile t in M.ReachableTilesByDistance(row,col,false,TileType.DOOR_C,TileType.RUBBLE,TileType.STONE_SLAB)){
						if(item.type == ConsumableType.BLAST_FUNGUS && (t.Is(TileType.WATER) || t.Is(FeatureType.SLIME))){
							B.Add("The blast fungus is doused. ",t);
							return true;
						}
						if(t.passable && t.inv == null && !t.Is(TileType.BLAST_FUNGUS,TileType.CHEST,TileType.STAIRS)){
							if((t.IsBurning() || (t.Is(TileType.FIREPIT) && !t.Is(FeatureType.SLIME))) && item.NameOfItemType() == "scroll" || item.type == ConsumableType.BANDAGES){
								B.Add(item.TheName(true) + " burns up! ",t);
								if(t.Is(TileType.FIREPIT)){
									t.AddFeature(FeatureType.FIRE);
								}
							}
							else{
								item.row = t.row;
								item.col = t.col;
								if(item.light_radius > 0){
									item.UpdateRadius(0,item.light_radius);
								}
								t.inv = item;
							}
							return true;
						}
					}
					return false;
				}
			}
		}
		public void Bump(int direction_of_motion){
			switch(type){
			case TileType.BARREL:
			{
				if(TileInDirection(direction_of_motion).passable){
					B.Add("The barrel tips over and smashes. ",this);
					TurnToFloor();
					List<Tile> cone = GetCone(direction_of_motion,2,true).Where(x=>x.passable && HasLOE(x));
					List<Tile> added = new List<Tile>();
					foreach(Tile t in cone){
						foreach(int dir in U.FourDirections){
							if(R.CoinFlip() && t.TileInDirection(dir).passable){
								added.AddUnique(t.TileInDirection(dir));
							}
						}
					}
					cone.AddRange(added);
					cone.Remove(this);
					foreach(Tile t in cone){
						t.AddFeature(FeatureType.OIL);
					}
					if(Is(FeatureType.FIRE)){
						RemoveFeature(FeatureType.FIRE);
						TileInDirection(direction_of_motion).ApplyEffect(DamageType.FIRE);
					}
				}
				break;
			}
			case TileType.STANDING_TORCH:
				if(TileInDirection(direction_of_motion).passable){
					B.Add("The torch tips over. ",this);
					TurnToFloor();
					TileInDirection(direction_of_motion).AddFeature(FeatureType.FIRE);
					//TileInDirection(direction_of_motion).ApplyEffect(DamageType.FIRE);
				}
				break;
			case TileType.POISON_BULB:
			{
				B.Add("The poison bulb bursts. ",this);
				TurnToFloor();
				List<Tile> area = AddGaseousFeature(FeatureType.POISON_GAS,8);
				if(area.Count > 0){
					Q.RemoveTilesFromEventAreas(area,EventType.POISON_GAS);
					Q.Add(new Event(area,200,EventType.POISON_GAS));
				}
				break;
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
			bool original_passable = passable;
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
							else{
								if(M.tile[i,j].Is(FeatureType.FIRE)){
									light_sources.Add(M.tile[i,j]);
									M.tile[i,j].UpdateRadius(1,0);
								}
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
						if(o is Tile && o.light_radius == 0 && (o as Tile).Is(FeatureType.FIRE)){
							o.UpdateRadius(0,1);
						}
						else{
							o.UpdateRadius(0,o.light_radius);
						}
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
				}
				if(type == TileType.DOOR_O && original_type == TileType.DOOR_C){
					if(player.CanSee(this)){
						B.Add(toggler.TheName(true) + " opens the door. ");
					}
				}
			}
			if(toggler != null){
				if(original_type == TileType.RUBBLE){
					B.Add(toggler.YouVisible("scatter") + " the rubble. ",this);
				}
			}
			if(!passable && original_passable){
				if(features.Contains(FeatureType.STABLE_TELEPORTAL)){
					Event e = Q.FindTargetedEvent(this,EventType.TELEPORTAL);
					if(e != null){
						foreach(Tile t in e.area){
							Event e2 = Q.FindTargetedEvent(t,EventType.TELEPORTAL);
							if(e2 != null && t.features.Contains(FeatureType.STABLE_TELEPORTAL)){
								e2.area.Remove(this);
								if(e2.area.Count == 0){
									t.RemoveFeature(FeatureType.STABLE_TELEPORTAL);
									t.AddFeature(FeatureType.INACTIVE_TELEPORTAL);
									e2.dead = true;
								}
							}
						}
					}
				}
				foreach(FeatureType ft in new List<FeatureType>(features)){
					RemoveFeature(ft);
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
			Toggle(null,TileType.FLOOR);
			/*bool lighting_update = false;
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
			}*/
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
			if(click){
				if(actor() == player || (actor() == null && player.CanSee(this))){
					B.Add("*CLICK* ",this);
					B.PrintAll();
				}
				else{
					if(actor() != null && player.CanSee(this) && player.CanSee(actor())){
						B.Add("You hear a *CLICK* from under " + actor().the_name + ". ");
						B.PrintAll();
					}
					else{
						if(DistanceFrom(player) <= 12){
							B.Add("You hear a *CLICK* nearby. ");
							B.PrintAll();
						}
						else{
							B.Add("You hear a *CLICK* in the distance. ");
							B.PrintAll();
						}
					}
				}
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
				int count = R.OneIn(10)? 3 : 2;
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
			case TileType.SLIDING_WALL_TRAP:
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
					int dir = dirs[R.Roll(dirs.Count)-1];
					Tile first = this;
					while(first.type != TileType.WALL){
						first = first.TileInDirection(dir);
					}
					first.TileInDirection(dir).TurnToFloor();
					ActorType ac = ActorType.SKELETON;
					if(M.current_level >= 2 && R.CoinFlip()){
						ac = ActorType.ZOMBIE;
					}
					if(M.current_level >= 5 && R.OneIn(10)){
						ac = ActorType.STONE_GOLEM;
					}
					if(M.current_level >= 4 && R.PercentChance(1)){
						ac = ActorType.MECHANICAL_KNIGHT;
					}
					if(M.current_level >= 8 && R.PercentChance(1)){
						ac = ActorType.CORPSETOWER_BEHEMOTH;
					}
					if(M.current_level >= 8 && R.PercentChance(1)){
						ac = ActorType.MACHINE_OF_WAR;
					}
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
					actor().attrs[AttrType.TELEPORTING] = R.Roll(4);
					Q.KillEvents(actor(),AttrType.TELEPORTING); //should be replaced by refreshduration eventually. works the same way, though.
					Q.Add(new Event(actor(),actor().DurationOfMagicalEffect(R.Roll(10)+25)*100,AttrType.TELEPORTING,actor().YouFeel() + " more stable. ",actor()));
				}
				else{
					B.Add("An unstable energy crackles for a moment, then dissipates. ",this);
				}
				Toggle(actor());
				break;
			case TileType.SHOCK_TRAP:
			{
				int old_radius = light_radius;
				UpdateRadius(old_radius,3,true);
				if(actor_here){
					if(player.CanSee(actor())){
						B.Add("Electricity zaps " + actor().the_name + ". ",this);
					}
					if(actor().TakeDamage(DamageType.ELECTRIC,DamageClass.PHYSICAL,R.Roll(3,6),null,"a shock trap")){
						B.Add(actor().YouAre() + " stunned! ",actor());
						actor().RefreshDuration(AttrType.STUNNED,actor().DurationOfMagicalEffect(R.Roll(6)+7)*100,(actor().YouAre() + " no longer stunned. "),actor());
						if(actor() == player){
							Help.TutorialTip(TutorialTopic.Stunned);
						}
					}
				}
				else{
					B.Add("Arcs of electricity appear and sizzle briefly. ",this); //apply electricity, once wands have been added
				}
				M.Draw();
				UpdateRadius(3,old_radius,true);
				Toggle(actor());
				break;
			}
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
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even brighter for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
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
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even darker for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
				}
				Toggle(actor());
				break;
			case TileType.FIRE_TRAP:
			{
				if(actor_here){
					B.Add("A column of flame engulfs " + actor().TheName(true) + "! ",this);
					actor().ApplyBurning();
				}
				else{
					B.Add("A column of flame appears! ",this);
				}
				AddFeature(FeatureType.FIRE);
				Toggle(actor());
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
					if(a.type != ActorType.GIANT_BAT && a.type != ActorType.BLOOD_MOTH && a.type != ActorType.CARNIVOROUS_BRAMBLE
					&& a.type != ActorType.LASHER_FUNGUS && a.type != ActorType.PHASE_SPIDER){
						a.FindPath(this);
					}
				}
				Toggle(actor());
				break;
			case TileType.BLINDING_TRAP:
				if(actor_here){
					B.Add("A dart flies out and strikes " + actor().TheName(true) + ". ",this);
					if(!actor().HasAttr(AttrType.NONLIVING,AttrType.BLINDSIGHT)){
						B.Add(actor().YouAre() + " blind! ",actor());
						actor().RefreshDuration(AttrType.BLIND,(R.Roll(3,6) + 6) * 100,actor().YouAre() + " no longer blinded. ",actor());
					}
					else{
						B.Add("It doesn't affect " + actor().the_name + ". ",actor());
					}
				}
				else{
					B.Add("A dart flies out and hits the floor. ",this);
				}
				Toggle(actor());
				break;
			case TileType.ICE_TRAP:
				if(actor_here){
					if(!actor().IsBurning()){
						if(player.CanSee(this)){
							B.Add("The air suddenly freezes, encasing " + actor().TheName(true) + " in ice. ");
						}
						actor().attrs[AttrType.FROZEN] = 35;
						actor().attrs[AttrType.SLIMED] = 0;
						actor().attrs[AttrType.OIL_COVERED] = 0;
						if(actor() == player){
							Help.TutorialTip(TutorialTopic.Frozen);
						}
					}
					else{
						if(player.CanSee(this)){
							if(player.CanSee(actor())){
								B.Add("Ice crystals form in the air around " + actor().the_name + " but quickly vanish. ");
							}
							else{
								B.Add("Ice crystals form in the air but quickly vanish. ");
							}
						}
					}
				}
				else{
					B.Add("Ice crystals form in the air but quickly vanish. ");
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
						if(player.HasLOS(a)){ //don't print a message if you're just detecting monsters
							B.Add("A ghostly image rises! ",a);
						}
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
				int num = R.Roll(5) + 7;
				List<Tile> new_area = AddGaseousFeature(FeatureType.POISON_GAS,num);
				if(new_area.Count > 0){
					B.Add("Poisonous gas fills the area! ",this);
					Q.Add(new Event(new_area,300,EventType.POISON_GAS));
				}
				Toggle(actor());
				break;
			}
			case TileType.SCALDING_OIL_TRAP:
			{
				if(actor_here){
					B.Add("Scalding oil pours over " + actor().TheName(true) + "! ",this);
					if(actor().TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),null,"a scalding oil trap")){
						if(!actor().HasAttr(AttrType.BURNING,AttrType.SLIMED) && !IsBurning()){
							actor().attrs[AttrType.OIL_COVERED] = 1;
							B.Add(actor().YouAre() + " covered in oil. ",actor());
							if(actor() == player){
								Help.TutorialTip(TutorialTopic.Oiled);
							}
						}
					}
				}
				else{
					B.Add("Scalding oil pours over the floor. ",this);
				}
				List<Tile> covered_in_oil = new List<Tile>{this};
				List<Tile> added = new List<Tile>();
				for(int i=0;i<2;++i){
					foreach(Tile t in covered_in_oil){
						foreach(int dir in U.FourDirections){
							Tile neighbor = t.TileInDirection(dir);
							if(neighbor.DistanceFrom(this) == 1 && R.OneIn(3) && neighbor.passable && !covered_in_oil.Contains(neighbor)){
								added.AddUnique(neighbor);
							}
						}
					}
					covered_in_oil.AddRange(added);
				}
				foreach(Tile t in covered_in_oil){
					t.AddFeature(FeatureType.OIL);
				}
				Toggle(actor());
				break;
			}
			default:
				break;
			}
		}
		public void OpenChest(){
			if(type == TileType.CHEST){
				ConsumableType item = Item.RandomChestItem();
				if(item == ConsumableType.MAGIC_TRINKET){
					List<MagicTrinketType> valid = new List<MagicTrinketType>();
					foreach(MagicTrinketType trinket in Enum.GetValues(typeof(MagicTrinketType))){
						if(trinket != MagicTrinketType.NO_MAGIC_TRINKET && trinket != MagicTrinketType.NUM_MAGIC_TRINKETS && !player.magic_trinkets.Contains(trinket)){
							valid.Add(trinket);
						}
					}
					if(valid.Count > 0){
						MagicTrinketType trinket = valid.Random();
						if(trinket == MagicTrinketType.BRACERS_OF_ARROW_DEFLECTION || trinket == MagicTrinketType.BOOTS_OF_GRIPPING){
							B.Add("You find " + MagicTrinket.Name(trinket) + "! ");
						}
						else{
							B.Add("You find a " + MagicTrinket.Name(trinket) + "! ");
						}
						player.magic_trinkets.Add(trinket);
					}
					else{
						B.Add("The chest is empty! ");
					}
				}
				else{
					bool no_room = false;
					if(player.InventoryCount() >= Global.MAX_INVENTORY_SIZE){
						no_room = true;
					}
					Item i = Item.Create(Item.RandomItem(),player);
					if(i != null){
						i.revealed_by_light = true;
						B.Add("You find " + Item.Prototype(i.type).AName() + ". ");
						if(no_room){
							B.Add("Your pack is too full to pick it up. ");
						}
					}
				}
				if(color == Color.Yellow){
					B.Add("There's something else in the chest! ");
					color = Color.DarkYellow;
				}
				else{
					TurnToFloor();
				}
			}
		}
		public bool IsLit(){ //default is player as viewer
			return IsLit(player.row,player.col,false);
		}
		public bool IsLit(int viewer_row,int viewer_col,bool ignore_wizlite_wizdark){
			if(solid_rock){
				return false;
			}
			if(!ignore_wizlite_wizdark){
				if(M.wiz_lite){
					return true;
				}
				if(M.wiz_dark){
					return false;
				}
			}
			if(light_value > 0 || type == TileType.GLOWING_FUNGUS){
				return true;
			}
			if(opaque){
				foreach(Tile t in NonOpaqueNeighborsBetween(viewer_row,viewer_col)){
					if(t.IsLit()){
						return true;
					}
				}
				if(M.actor[viewer_row,viewer_col] != null && M.actor[viewer_row,viewer_col].LightRadius() > 0){
					if(M.actor[viewer_row,viewer_col].LightRadius() >= DistanceFrom(viewer_row,viewer_col)){
						if(M.actor[viewer_row,viewer_col].HasBresenhamLineOfSight(row,col)){
							return true;
						}
					}
				}
			}
			return false;
		}
		public bool IsLitFromAnywhere(){ return IsLitFromAnywhere(opaque); }
		public bool IsLitFromAnywhere(bool considered_opaque){
			if(solid_rock){
				return false;
			}
			if(M.wiz_lite){
				return true;
			}
			if(M.wiz_dark){
				return false;
			}
			if(light_value > 0){
				return true;
			}
			if(considered_opaque){
				foreach(Tile t in TilesAtDistance(1)){
					if(t.light_value > 0){
						return true;
					}
				}
				foreach(Actor a in ActorsWithinDistance(Global.MAX_LIGHT_RADIUS)){
					if(a.LightRadius() > 0 && a.LightRadius() >= a.DistanceFrom(this) && a.HasBresenhamLineOfSight(row,col)){
						return true;
					}
				}
			}
			return false;
		}
		public bool IsTrap(){
			switch(type){
			case TileType.FIRE_TRAP:
			case TileType.GRENADE_TRAP:
			case TileType.LIGHT_TRAP:
			case TileType.SLIDING_WALL_TRAP:
			case TileType.TELEPORT_TRAP:
			case TileType.SHOCK_TRAP:
			case TileType.ALARM_TRAP:
			case TileType.DARKNESS_TRAP:
			case TileType.BLINDING_TRAP:
			case TileType.ICE_TRAP:
			case TileType.PHANTOM_TRAP:
			case TileType.POISON_GAS_TRAP:
			case TileType.SCALDING_OIL_TRAP:
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
		public bool IsDoorType(bool count_hidden_doors_as_passable){ //things that aren't passable but shouldn't block certain pathfinding routines
			switch(type){
			case TileType.DOOR_C:
			case TileType.RUBBLE:
			case TileType.STONE_SLAB:
				return true;
			case TileType.HIDDEN_DOOR:
				if(count_hidden_doors_as_passable){
					return true;
				}
				break;
			}
			return false;
		}
		public bool BlocksConnectivityOfMap(){
			if(passable || IsDoorType(true)){
				return false;
			}
			return true;
		}
		public bool IsFlammableTerrainType(){ //used for terrain that turns to floor when it burns
			switch(type){
			case TileType.BRUSH:
			case TileType.POPPY_FIELD:
			case TileType.POISON_BULB:
			case TileType.VINE:
				return true;
			}
			return false;
		}
		public bool IsCurrentlyFlammable(){
			if(Is(FeatureType.FIRE,FeatureType.POISON_GAS)){
				return false;
			}
			if(Is(FeatureType.WEB,FeatureType.OIL)){
				return true;
			}
			switch(type){
			case TileType.WATER:
			case TileType.POOL_OF_RESTORATION:
				return false;
			case TileType.BRUSH:
			case TileType.POPPY_FIELD:
			case TileType.VINE:
			case TileType.POISON_BULB:
			case TileType.BARREL:
				return true;
			}
			if(Is(FeatureType.TROLL_CORPSE,FeatureType.TROLL_BLOODWITCH_CORPSE)){
				return true;
			}
			if(inv != null){
				if(inv.type == ConsumableType.BANDAGES || inv.NameOfItemType() == "scroll"){
					return true;
				}
			}
			return false;
		}
		public bool ConductsElectricity(){
			if(IsShrine() || Is(TileType.CHEST,TileType.RUINED_SHRINE,TileType.WATER,TileType.POOL_OF_RESTORATION,TileType.STANDING_TORCH)){
				return true;
			}
			return false;
		}
		delegate int del(int i);
		public List<Tile> NeighborsBetween(int r,int c){ //list of tiles next to this one that are between you and it
			del Clamp = x => x<-1? -1 : x>1? 1 : x; //clamps to a value between -1 and 1
			int dy = r - row;
			int dx = c - col;
			List<Tile> result = new List<Tile>();
			if(dy==0 && dx==0){
				return result; //return the empty set
			}
			int newrow = row+Clamp(dy);
			int newcol = col+Clamp(dx);
			result.Add(M.tile[newrow,newcol]);
			if(Math.Abs(dy) < Math.Abs(dx) && dy!=0){
				newrow -= Clamp(dy);
				result.Add(M.tile[newrow,newcol]);
			}
			if(Math.Abs(dx) < Math.Abs(dy) && dx!=0){
				newcol -= Clamp(dx);
				result.Add(M.tile[newrow,newcol]);
			}
			return result;
		}
		public List<Tile> NonOpaqueNeighborsBetween(int r,int c){ //list of non-opaque tiles next to this one that are between you and it
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
		public List<Tile> AddGaseousFeature(FeatureType f,int num){
			List<Tile> area = new List<Tile>();
			Tile current = this;
			for(int i=0;i<num;++i){
				if(!current.Is(f)){
					current.AddFeature(f);
					area.Add(current);
				}
				else{
					for(int tries=0;tries<50;++tries){
						List<Tile> open = new List<Tile>();
						foreach(Tile t in current.TilesAtDistance(1)){
							if(t.passable){
								open.Add(t);
								if(!t.Is(f)){
									open.Add(t); //3x as likely if it can expand there
									open.Add(t);
								}
							}
						}
						/*foreach(int dir in U.FourDirections){
							if(current.TileInDirection(dir).passable){
								open.Add(current.TileInDirection(dir));
							}
						}*/
						if(open.Count > 0){
							Tile possible = open.Random();
							if(!possible.Is(f)){
								possible.AddFeature(f);
								area.Add(possible);
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
			return area;
		}
		public void ApplyEffect(DamageType effect){
			switch(effect){
			case DamageType.FIRE:
			{
				if(Is(FeatureType.FIRE,FeatureType.POISON_GAS)){
					return;
				}
				if(Is(FeatureType.FOG)){
					RemoveOpaqueFeature(FeatureType.FOG);
				}
				if(Is(FeatureType.OIL) || Is(TileType.BARREL)){
					features.Remove(FeatureType.OIL);
					if(!features.Contains(FeatureType.FIRE)){
						UpdateRadius(light_radius,1);
						features.Add(FeatureType.FIRE);
						Fire.AddBurningObject(this);
					}
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(FeatureType.WEB)){
					features.Remove(FeatureType.WEB);
					if(!features.Contains(FeatureType.FIRE)){
						UpdateRadius(light_radius,1);
						features.Add(FeatureType.FIRE);
						Fire.AddBurningObject(this);
					}
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(FeatureType.SPORES)){
					features.Remove(FeatureType.SPORES);
					if(!features.Contains(FeatureType.FIRE)){
						UpdateRadius(light_radius,1);
						features.Add(FeatureType.FIRE);
						Fire.AddBurningObject(this);
					}
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(TileType.ICE)){
					Toggle(null,TileType.WATER);
				}
				else{
					if(Is(TileType.WATER,TileType.POOL_OF_RESTORATION) || Is(FeatureType.SLIME)){
						return;
					}
				}
				if(Is(FeatureType.TROLL_CORPSE)){
					features.Remove(FeatureType.TROLL_CORPSE);
					B.Add("The troll corpse burns to ashes! ",this);
					if(!features.Contains(FeatureType.FIRE)){
						UpdateRadius(light_radius,1);
						features.Add(FeatureType.FIRE);
						Fire.AddBurningObject(this);
					}
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){
					features.Remove(FeatureType.TROLL_BLOODWITCH_CORPSE);
					B.Add("The troll bloodwitch corpse burns to ashes! ",this);
					if(!features.Contains(FeatureType.FIRE)){
						UpdateRadius(light_radius,1);
						features.Add(FeatureType.FIRE);
						Fire.AddBurningObject(this);
					}
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(inv != null && (inv.NameOfItemType() == "scroll" || inv.type == ConsumableType.BANDAGES)){
					B.Add(inv.TheName(true) + " burns up! ",this);
					inv = null;
					if(!features.Contains(FeatureType.FIRE)){
						UpdateRadius(light_radius,1);
						features.Add(FeatureType.FIRE);
						Fire.AddBurningObject(this);
					}
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(IsFlammableTerrainType()){
					if(Is(TileType.POISON_BULB)){
						B.Add("The poison bulb bursts. ",this);
						TurnToFloor();
						List<Tile> area = AddGaseousFeature(FeatureType.POISON_GAS,8);
						if(area.Count > 0){
							Q.RemoveTilesFromEventAreas(area,EventType.POISON_GAS);
							Q.Add(new Event(area,200,EventType.POISON_GAS));
						}
						if(Is(FeatureType.POISON_GAS)){
							break;
						}
					}
					else{
						TurnToFloor();
					}
					if(!features.Contains(FeatureType.FIRE)){
						UpdateRadius(light_radius,1);
						features.Add(FeatureType.FIRE);
						Fire.AddBurningObject(this);
					}
					if(actor() != null){
						actor().ApplyBurning();
					}
				}
				if(Is(TileType.WAX_WALL)){
					TurnToFloor();
					foreach(Tile neighbor in TilesAtDistance(1)){
						neighbor.solid_rock = false;
					}
					color = Color.DarkYellow;
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
				if(inv != null){
					if(inv.NameOfItemType() == "potion"){
						if(inv.quantity > 1){
							B.Add(inv.TheName(true) + " break! ",this);
						}
						else{
							B.Add(inv.TheName(true) + " breaks! ",this);
						}
						inv = null;
					}
				}
				break;
			}
			case DamageType.NORMAL:
			{
				if(inv != null){
					if(inv.NameOfItemType() == "potion"){
						if(inv.quantity > 1){
							B.Add(inv.TheName(true) + " break! ",this);
						}
						else{
							B.Add(inv.TheName(true) + " breaks! ",this);
						}
						inv = null;
					}
					else{
						if(inv.NameOfItemType() == "orb"){
							if(inv.quantity > 1){
								B.Add(inv.TheName(true) + " break! ",this);
							}
							else{
								B.Add(inv.TheName(true) + " breaks! ",this);
							}
							Item i = inv;
							inv = null;
							i.Use(null,new List<Tile>{this});
						}
					}
				}
				if(type == TileType.CRACKED_WALL){
					Toggle(null,TileType.FLOOR);
					foreach(Tile neighbor in TilesAtDistance(1)){
						neighbor.solid_rock = false;
					}
				}
				if(type == TileType.HIDDEN_DOOR){
					Toggle(null);
					Toggle(null);
				}
				if(type == TileType.DOOR_C){
					Toggle(null);
				}
				if(IsTrap()){
					TriggerTrap(); //?
				}
				if(Is(TileType.RUBBLE)){
					Toggle(null);
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
				case FeatureType.SPORES:
					if(IsBurning()){
						return;
					}
					if(Is(FeatureType.POISON_GAS)){
						RemoveFeature(FeatureType.POISON_GAS);
					}
					features.Add(FeatureType.SPORES);
					break;
				case FeatureType.POISON_GAS:
					if(Is(FeatureType.SPORES)){
						return;
					}
					if(Is(FeatureType.FIRE)){
						RemoveFeature(FeatureType.FIRE);
						Fire.burning_objects.Remove(this);
						if(name == "floor" && type != TileType.BREACHED_WALL){
							if(R.OneIn(4)){
								color = Color.Gray;
							}
							else{
								color = Color.DarkGray;
							}
						}
						if(actor() != null && actor().IsBurning()){
							actor().RefreshDuration(AttrType.BURNING,0);
						}
					}
					features.Add(FeatureType.POISON_GAS);
					break;
				case FeatureType.OIL:
					if(actor() != null && actor().HasAttr(AttrType.BURNING)){
						actor().ApplyBurning();
						AddFeature(FeatureType.FIRE);
						//ApplyEffect(DamageType.FIRE);
					}
					if(Is(FeatureType.SLIME,FeatureType.FIRE) || Is(TileType.CHASM,TileType.BRUSH,TileType.POPPY_FIELD,TileType.GRAVE_DIRT,TileType.GRAVEL,TileType.BLAST_FUNGUS,TileType.GLOWING_FUNGUS,TileType.JUNGLE,TileType.VINE,TileType.TOMBSTONE)){
						return;
					}
					if(type == TileType.FIREPIT){
						if(!features.Contains(FeatureType.FIRE)){
							UpdateRadius(light_radius,1);
							features.Add(FeatureType.FIRE);
							Fire.AddBurningObject(this);
						}
						if(actor() != null){
							actor().ApplyBurning();
						}
					}
					else{
						features.Add(FeatureType.OIL);
					}
					break;
				case FeatureType.FIRE:
					ApplyEffect(DamageType.FIRE);
					if(!Is(FeatureType.FIRE,FeatureType.POISON_GAS,FeatureType.SLIME) && !Is(TileType.POOL_OF_RESTORATION,TileType.WATER)){
						if(light_radius == 0){
							UpdateRadius(0,1);
						}
						features.Add(FeatureType.FIRE);
						Fire.AddBurningObject(this);
						if(actor() != null){
							actor().ApplyBurning();
						}
					}
					break;
				case FeatureType.SLIME:
					if(Is(TileType.ICE,TileType.WATER,TileType.POOL_OF_RESTORATION,TileType.CHASM,TileType.BRUSH,TileType.POPPY_FIELD,TileType.GRAVE_DIRT,TileType.GRAVEL,TileType.BLAST_FUNGUS,TileType.GLOWING_FUNGUS,TileType.JUNGLE,TileType.VINE,TileType.TOMBSTONE)){
						return;
					}
					if(Is(FeatureType.FIRE)){
						RemoveFeature(FeatureType.FIRE);
					}
					if(Is(FeatureType.OIL)){
						RemoveFeature(FeatureType.OIL);
					}
					features.Add(FeatureType.SLIME);
					break;
				case FeatureType.TROLL_CORPSE:
				case FeatureType.TROLL_BLOODWITCH_CORPSE:
					if(Is(FeatureType.FIRE) || (Is(TileType.FIREPIT) && !Is(FeatureType.SLIME))){
						B.Add(proto_feature[f].the_name + " burns to ashes! ",this);
					}
					else{
						features.Add(f);
					}
					break;
				case FeatureType.WEB:
					if(!Is(FeatureType.FIRE)){
						features.Add(FeatureType.WEB);
					}
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
				case FeatureType.FIRE:
					UpdateRadius(1,Prototype(type).light_radius);
					features.Remove(f);
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
								else{
									if(M.tile[i,j].Is(FeatureType.FIRE)){
										light_sources.Add(M.tile[i,j]);
										M.tile[i,j].UpdateRadius(1,0);
									}
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
							if(o is Tile && o.light_radius == 0 && (o as Tile).Is(FeatureType.FIRE)){
								o.UpdateRadius(0,1);
							}
							else{
								o.UpdateRadius(0,o.light_radius);
							}
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
								else{
									if(M.tile[i,j].Is(FeatureType.FIRE)){
										light_sources.Add(M.tile[i,j]);
										M.tile[i,j].UpdateRadius(1,0);
									}
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
							if(o is Tile && o.light_radius == 0 && (o as Tile).Is(FeatureType.FIRE)){
								o.UpdateRadius(0,1);
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
}

