using System;
using System.Collections.Generic;
namespace Forays{
	public class Tile : PhysicalObject{
		public TileType type{get; private set;}
		public bool passable{get; private set;}
		public bool opaque{get; private set;}
		public bool seen{get;set;}
		public int light_value{get;set;}
		private TileType? toggles_into;
		public Item inv{get;set;}
		
		private static Dictionary<TileType,Tile> proto= new Dictionary<TileType, Tile>();
		public static Tile Prototype(TileType type){ return proto[type]; }
		private static int ROWS = Global.ROWS;
		private static int COLS = Global.COLS;
		//public static Map M{get;set;} //inherited
		public static Buffer B{get;set;} //needed here?
		public static Actor player{get;set;}
		static Tile(){
			proto[TileType.FLOOR] = new Tile(TileType.FLOOR,"floor",'.',ConsoleColor.White,true,false,null);
			proto[TileType.WALL] = new Tile(TileType.WALL,"wall",'#',ConsoleColor.Gray,false,true,null);
			proto[TileType.DOOR_C] = new Tile(TileType.DOOR_C,"closed door",'+',ConsoleColor.DarkYellow,false,true,TileType.DOOR_O);
			proto[TileType.DOOR_O] = new Tile(TileType.DOOR_O,"open door",'-',ConsoleColor.DarkGreen,true,false,TileType.DOOR_C);
			proto[TileType.STAIRS] = new Tile(TileType.STAIRS,"stairway",'>',ConsoleColor.White,true,false,null);
			proto[TileType.CHEST] = new Tile(TileType.CHEST,"treasure chest",'~',ConsoleColor.Yellow,true,false,TileType.FLOOR);
			//trap ideas: quickfire trap: burst of fire that ignites stuff, then expands(like quickfire) for several turns.
				//you'll probably have to run while on fire, instead of putting it out
			//not an actual trap, but room mimic will be awesome.
			//also not an actual trap, but arena rooms will be cool too. perhaps you'll see the opponent, in stasis.
				//"Touch the [tile]?(Y/N) "   if you touch it, you're stuck in the arena until one of you dies.
			//stun trap. much less nasty than paralysis or even confusion.
		}
		/*public Tile(){ //is this constructor necessary?
			type=TileType.FLOOR;
			passable = true;
			opaque = false;
			seen = false;
			lit = 0;
			toggles_into = null;
			inv = null;
		}*/
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
		public Tile(TileType type_,string name_,char symbol_,ConsoleColor color_,bool passable_,bool opaque_,TileType? toggles_into_){
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
		public void Toggle(PhysicalObject toggler){ //todo: when a mob opens a seen door, it will be visible, so add
			if(toggles_into != null){	//	a message: "You hear a door opening. " - and that should be enough!
				bool lighting_update = false;
				List<Actor> actors = new List<Actor>();
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

				TransformTo(toggles_into.Value);

				if(lighting_update){
					foreach(Actor a in actors){
						a.UpdateRadius(0,a.light_radius);
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
		public void Destroy(){ //levels a tile, leaving only floor.
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

