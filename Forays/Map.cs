/*Copyright (c) 2011-2012  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace Forays{
	public enum LevelType{Standard,Cave,Ruined,Hive,Mine,Fortress,Extravagant};
	public class PosArray<T>{
		public T[,] objs;
		public T this[int row,int col]{
			get{
				return objs[row,col];
			}
			set{
				objs[row,col] = value;
			}
		}
		public T this[pos p]{
			get{
				return objs[p.row,p.col];
			}
			set{
				objs[p.row,p.col] = value;
			}
		}
		public PosArray(int rows,int cols){
			objs = new T[rows,cols];
		}
	}
	public class Map{
		public PosArray<Tile> tile = new PosArray<Tile>(ROWS,COLS);
		public PosArray<Actor> actor = new PosArray<Actor>(ROWS,COLS);
		public int current_level{get;set;}
		public List<LevelType> level_types;
		public bool wiz_lite{get{ return internal_wiz_lite; }
			set{
				internal_wiz_lite = value;
				if(value == true){
					foreach(Tile t in AllTiles()){
						if(t.Is(TileType.BLAST_FUNGUS)){
							B.Add("The blast fungus starts to smolder in the light. ",t);
							t.Toggle(null);
							if(t.inv == null){ //should always be true
								t.GetItem(Item.Create(ConsumableType.BLAST_FUNGUS,t.row,t.col));
								t.inv.quantity = 3;
								t.inv.revealed_by_light = true;
							}
							Q.Add(new Event(t.inv,100,EventType.BLAST_FUNGUS));
						}
					}
				}
			}
		}
		private bool internal_wiz_lite;
		public bool wiz_dark{get;set;}
		private Dict<ActorType,int> generated_this_level = null; //used for rejecting monsters if too many already exist on the current level
		private bool[,] danger_sensed{get;set;}
		private static List<pos> allpositions = new List<pos>();
		public PosArray<int> safetymap;
		public int[,] row_displacement = null;
		public int[,] col_displacement = null;

		public static Color darkcolor = Color.DarkCyan;
		public static Color unseencolor = Color.DarkGray;
		private const int ROWS = Global.ROWS;
		private const int COLS = Global.COLS;
		public static Actor player{get;set;}
		public static Queue Q{get;set;}
		public static Buffer B{get;set;}
		static Map(){
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					allpositions.Add(new pos(i,j));
				}
			}
		}
		public Map(Game g){
			//tile = new Tile[ROWS,COLS];
			//actor = new Actor[ROWS,COLS];
			current_level = 0;
			Map.player = g.player;
			Map.Q = g.Q;
			Map.B = g.B;
			safetymap = new PosArray<int>(Global.ROWS,Global.COLS);
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					safetymap[i,j] = -9999;
				}
			}
		}
		public bool BoundsCheck(int r,int c){
			if(r>=0 && r<ROWS && c>=0 && c<COLS){
				return true;
			}
			return false;
		}
		public List<Tile> AllTiles(){ //possible speed issues? is there anywhere that I should be using 'alltiles' directly?
			List<Tile> result = new List<Tile>(); //should i have one method that allows modification and one that doesn't?
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					result.Add(tile[i,j]);
				}
			}
			return result;
		}
		public List<Actor> AllActors(){
			List<Actor> result = new List<Actor>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(actor[i,j] != null){
						result.Add(actor[i,j]);
					}
				}
			}
			return result;
		}
		public List<pos> AllPositions(){ return allpositions; }
		public LevelType ChooseNextLevelType(LevelType current){
			List<LevelType> types = new List<LevelType>();
			foreach(LevelType l in Enum.GetValues(typeof(LevelType))){
				if(l != current){
					types.Add(l);
				}
			}
			return types.Random();
		}
		public IEnumerable<Tile> ReachableTilesByDistance(int origin_row,int origin_col,bool return_reachable_walls,params TileType[] tiles_considered_passable){
			int[,] values = new int[ROWS,COLS];
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					bool passable = tile[i,j].passable;
					foreach(TileType tt in tiles_considered_passable){
						if(tile[i,j].type == tt){
							passable = true;
							break;
						}
					}
					if(return_reachable_walls && !tile[i,j].solid_rock){
						passable = true;
					}
					if(passable){
						values[i,j] = 0;
					}
					else{
						values[i,j] = -1;
					}
				}
			}
			int minrow = 1;
			int maxrow = ROWS-2;
			int mincol = 1; //todo: make it start at 1 radius and go out from there until it hits these limits.
			int maxcol = COLS-2;
			values[origin_row,origin_col] = 1;
			int val = 1;
			bool done = false;
			List<Tile> just_added = new List<Tile>{tile[origin_row,origin_col]};
			while(!done){
				done = true;
				while(just_added.Count > 0){
					yield return just_added.RemoveRandom();
				}
				for(int i=minrow;i<=maxrow;++i){
					for(int j=mincol;j<=maxcol;++j){
						if(values[i,j] == val){
							for(int s=i-1;s<=i+1;++s){
								for(int t=j-1;t<=j+1;++t){
									if(values[s,t] == 0){
										values[s,t] = val + 1;
										done = false;
										just_added.Add(tile[s,t]);
									}
								}
							}
						}
					}
				}
				++val;
			}
		}
		public void GenerateLevelTypes(){
			level_types = new List<LevelType>{LevelType.Standard,LevelType.Standard};
			LevelType current = LevelType.Standard;
			while(level_types.Count < 20){
				int num = Global.Roll(2,2) - 1;
				current = ChooseNextLevelType(current);
				for(int i=0;i<num;++i){
					if(level_types.Count < 20){
						level_types.Add(current);
					}
				}
			}
		}
		public void UpdateDangerValues(){
			danger_sensed = new bool[ROWS,COLS];
			foreach(Actor a in AllActors()){
				if(a != player){
					foreach(Tile t in AllTiles()){
						if(danger_sensed[t.row,t.col] == false && t.passable && !t.opaque){
							if(a.CanSee(t)){
								int multiplier = a.HasAttr(AttrType.KEEN_SENSES)? 5 : 10;
								int stealth = player.TotalSkill(SkillType.STEALTH);
								if(!player.tile().IsLit()){
									stealth -= 2; //remove any bonus from the player's own tile...
								}
								if(!t.IsLit()){
									stealth += 2; //...and add any bonus from the tile in question
								}
								int value = (stealth * a.DistanceFrom(t) * multiplier) - 5 * a.player_visibility_duration;
								if(value < 100 || a.player_visibility_duration < 0){
									danger_sensed[t.row,t.col] = true;
								}
							}
						}
					}
				}
			}
		}
		public void UpdateSafetyMap2(params PhysicalObject[] sources){ //todo: remove one or the other
			List<cell> sourcelist = new List<cell>();
			foreach(PhysicalObject o in sources){
				sourcelist.Add(new cell(o.row,o.col,0));
			}
			IntLocationDelegate get_cost = (r,c) => {
				if(actor[r,c] != null){
					return 20 + (10 * actor[r,c].attrs[AttrType.TURNS_HERE]);
				}
				else{
					if(tile[r,c].Is(TileType.DOOR_C,TileType.RUBBLE)){
						return 20;
					}
					else{
						return 10;
					}
				}
			};
			PosArray<int> a = GetDijkstraMap(Global.ROWS,Global.COLS,
			                                 (s,t)=>tile[s,t].Is(TileType.WALL,TileType.HIDDEN_DOOR,TileType.STONE_SLAB,TileType.STATUE), 
			                                 get_cost,sourcelist);
			for(int i=0;i<Global.ROWS;++i){
				for(int j=0;j<Global.COLS;++j){
					if(a[i,j] != -9999){
						a[i,j] = -(a[i,j]) * 5;
					}
				}
			}
			foreach(PhysicalObject o in sources){
				a[o.row,o.col] = -9999; //now the player (or other sources) become blocking
			}
			UpdateDijkstraMap(a,get_cost);
			safetymap = a;
		}
		public void UpdateSafetyMap(params PhysicalObject[] sources){
			PriorityQueue<cell> frontier = new PriorityQueue<cell>(c => -c.value);
			for(int i=0;i<Global.ROWS;++i){
				for(int j=0;j<Global.COLS;++j){
					if(tile[i,j].Is(TileType.WALL,TileType.HIDDEN_DOOR,TileType.STONE_SLAB,TileType.STATUE)){
						safetymap[i,j] = -9999; //wall or other blocking object, including stationary monsters
					}
					else{
						safetymap[i,j] = 9999; //otherwise, cells start at a very high number
					}
				}
			}
			foreach(PhysicalObject o in sources){
				safetymap[o.row,o.col] = 0;
				frontier.Add(new cell(o.row,o.col,0));
			}
			while(frontier.list.Count > 0){
				cell c = frontier.Pop();
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(BoundsCheck(c.row+s,c.col+t)){
							int cost = 10;
							if(actor[c.row+s,c.col+t] != null){
								cost = 20 + (10 * actor[c.row+s,c.col+t].attrs[AttrType.TURNS_HERE]);
							}
							else{
								if(tile[c.row+s,c.col+t].Is(TileType.DOOR_C,TileType.RUBBLE)){
									cost = 20;
								}
							}
							if(safetymap[c.row+s,c.col+t] > c.value+cost){
								safetymap[c.row+s,c.col+t] = c.value+cost;
								frontier.Add(new cell(c.row+s,c.col+t,c.value+cost));
							}
						}
					}
				}
			}
			for(int i=0;i<Global.ROWS;++i){
				for(int j=0;j<Global.COLS;++j){
					if(safetymap[i,j] == 9999){
						safetymap[i,j] = -9999; //treat any unreachable areas as walls
					}
					if(safetymap[i,j] != -9999){
						safetymap[i,j] = -(safetymap[i,j]) * 5;
					}
				}
			}
			foreach(PhysicalObject o in sources){
				safetymap[o.row,o.col] = -9999; //now the player (or other sources) become blocking
				//frontier.Add(new cell(o.row,o.col,0));
			}
			for(int i=1;i<Global.ROWS-1;++i){
				for(int j=1;j<Global.COLS-1;++j){
					if(safetymap[i,j] != -9999){
						int v = safetymap[i,j];
						bool good = true;
						for(int s=-1;s<=1 && good;++s){
							for(int t=-1;t<=1 && good;++t){
								if(safetymap[i+s,j+t] < v && safetymap[i+s,j+t] != -9999){
									good = false;
								}
							}
						}
						if(good){
							frontier.Add(new cell(i,j,v));
						}
					}
				}
			}
			while(frontier.list.Count > 0){
				cell c = frontier.Pop();
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(BoundsCheck(c.row+s,c.col+t)){
							int cost = 10;
							if(actor[c.row+s,c.col+t] != null){
								cost = 20 + (10 * actor[c.row+s,c.col+t].attrs[AttrType.TURNS_HERE]);
							}
							else{
								if(tile[c.row+s,c.col+t].Is(TileType.DOOR_C,TileType.RUBBLE)){
									cost = 20;
								}
							}
							if(safetymap[c.row+s,c.col+t] > c.value+cost){
								safetymap[c.row+s,c.col+t] = c.value+cost;
								frontier.Add(new cell(c.row+s,c.col+t,c.value+cost));
							}
						}
					}
				}
			}
		}
		public delegate bool BooleanLocationDelegate(int row,int col);
		public delegate int IntLocationDelegate(int row,int col);
		public static PosArray<int> GetDijkstraMap(int height,int width,BooleanLocationDelegate is_blocked,IntLocationDelegate get_cost,List<cell> sources){
			PriorityQueue<cell> frontier = new PriorityQueue<cell>(c => -c.value);
			PosArray<int> map = new PosArray<int>(height,width);
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(is_blocked(i,j)){
						map[i,j] = -9999;
					}
					else{
						map[i,j] = 9999;
					}
				}
			}
			foreach(cell c in sources){
				map[c.row,c.col] = c.value;
				frontier.Add(c);
			}
			while(frontier.list.Count > 0){
				cell c = frontier.Pop();
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(c.row+s >= 0 && c.row+s < height && c.col+t >= 0 && c.col+t < width){
							int cost = get_cost(c.row+s,c.col+t);
							if(map[c.row+s,c.col+t] > c.value+cost){
								map[c.row+s,c.col+t] = c.value+cost;
								frontier.Add(new cell(c.row+s,c.col+t,c.value+cost));
							}
						}
					}
				}
			}
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(map[i,j] == 9999){
						map[i,j] = -9999; //any unreachable areas are marked unpassable
					}
				}
			}
			return map;
		}
		public static void UpdateDijkstraMap(PosArray<int> map,IntLocationDelegate get_cost){
			PriorityQueue<cell> frontier = new PriorityQueue<cell>(c => -c.value);
			int height = map.objs.GetLength(0);
			int width = map.objs.GetLength(1);
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(map[i,j] != -9999){
						int v = map[i,j];
						bool good = true;
						for(int s=-1;s<=1 && good;++s){
							for(int t=-1;t<=1 && good;++t){
								if(i+s >= 0 && i+s < height && j+t >= 0 && j+t < width){
									if(map[i+s,j+t] < v && map[i+s,j+t] != -9999){
										good = false;
									}
								}
							}
						}
						if(good){ //find local minima and add them to the frontier
							frontier.Add(new cell(i,j,v));
						}
					}
				}
			}
			while(frontier.list.Count > 0){
				cell c = frontier.Pop();
				for(int s=-1;s<=1;++s){
					for(int t=-1;t<=1;++t){
						if(c.row+s >= 0 && c.row+s < height && c.col+t >= 0 && c.col+t < width){
							int cost = get_cost(c.row+s,c.col+t);
							if(map[c.row+s,c.col+t] > c.value+cost){
								map[c.row+s,c.col+t] = c.value+cost;
								frontier.Add(new cell(c.row+s,c.col+t,c.value+cost));
							}
						}
					}
				}
			}
		}
		public void InitLevel(){ //creates an empty level surrounded by walls. used for testing purposes.
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(i==0 || j==0 || i==ROWS-1 || j==COLS-1){
						tile[i,j] = Tile.Create(TileType.WALL,i,j);
					}
					else{
						tile[i,j] = Tile.Create(TileType.FLOOR,i,j);
					}
					//alltiles.Add(tile[i,j]);
				}
			}
		}
		public void LoadLevel(string filename){
			TextReader file = new StreamReader(filename);
			char ch;
			List<Tile> hidden = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					ch = (char)file.Read();
					switch(ch){
					case '#':
						Tile.Create(TileType.WALL,i,j);
						break;
					case '.':
						Tile.Create(TileType.FLOOR,i,j);
						break;
					case '+':
						Tile.Create(TileType.DOOR_C,i,j);
						break;
					case '-':
						Tile.Create(TileType.DOOR_O,i,j);
						break;
					case '>':
						Tile.Create(TileType.STAIRS,i,j);
						break;
					case 'H':
						Tile.Create(TileType.HIDDEN_DOOR,i,j);
						hidden.Add(tile[i,j]);
						break;
					default:
						Tile.Create(TileType.FLOOR,i,j);
						break;
					}
					//alltiles.Add(tile[i,j]);
				}
				file.ReadLine();
			}
			file.Close();
			if(hidden.Count > 0){
				Event e = new Event(hidden,100,EventType.CHECK_FOR_HIDDEN);
				e.tiebreaker = 0;
				Q.Add(e);
			}
		}
		public void Draw(){
			if(Screen.MapChar(0,0).c == '-'){ //kinda hacky. there won't be an open door in the corner, so this looks for
				RedrawWithStrings(); //evidence of Select being called (& therefore, the map needing to be redrawn entirely)
			}
			else{
				Console.CursorVisible = false;
				if(player.HasAttr(AttrType.ON_FIRE) || player.HasAttr(AttrType.CATCHING_FIRE)){
					Screen.DrawMapBorder(new colorchar(Color.RandomFire,'&'));
					for(int i=1;i<ROWS-1;++i){
						for(int j=1;j<COLS-1;++j){
							Screen.WriteMapChar(i,j,VisibleColorChar(i,j));
						}
					}
				}
				else{
					for(int i=0;i<ROWS;++i){ //if(ch.c == '#'){ ch.c = Encoding.GetEncoding(437).GetChars(new byte[] {177})[0]; }
						for(int j=0;j<COLS;++j){ //^--top secret, mostly because it doesn't work well - 
							Screen.WriteMapChar(i,j,VisibleColorChar(i,j)); //redrawing leaves gaps for some reason.
						}
					}
					/*colorchar[,] scr = new colorchar[ROWS,COLS];
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							scr[i,j] = VisibleColorChar(i,j);
							if(scr[i,j].c == '#'){
								//scr[i,j].color = Color.RandomBright;
							}
						}
					}
					if(row_displacement == null){
						//row_displacement = Actor.GetDiamondSquarePlasmaFractal(ROWS,COLS);
						//col_displacement = Actor.GetDiamondSquarePlasmaFractal(ROWS,COLS);
						row_displacement = new int[ROWS,COLS];
						col_displacement = new int[ROWS,COLS];
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								//row_displacement[i,j] /= 16;
								//col_displacement[i,j] /= 16;
								row_displacement[i,j] = 0;
								col_displacement[i,j] = 0;
							}
						}
					}
					else{*/
						/*int[,] rd2 = Actor.GetDiamondSquarePlasmaFractal(ROWS,COLS);
						int[,] cd2 = Actor.GetDiamondSquarePlasmaFractal(ROWS,COLS);
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								rd2[i,j] /= 32;
								cd2[i,j] /= 32;
								row_displacement[i,j] += (rd2[i,j] < 0? -1 : rd2[i,j] > 0? 1 : 0);
								col_displacement[i,j] += (cd2[i,j] < 0? -1 : cd2[i,j] > 0? 1 : 0);
							}
						}*/
						/*for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(Global.OneIn(40)){
									if(row_displacement[i,j] < 0){
										if(Global.OneIn(10)){
											row_displacement[i,j]--;
										}
										else{
											row_displacement[i,j]++;
										}
									}
									else{
										if(row_displacement[i,j] > 0){
											if(Global.OneIn(10)){
												row_displacement[i,j]++;
											}
											else{
												row_displacement[i,j]--;
											}
										}
										else{
											if(Global.CoinFlip()){
												row_displacement[i,j]++;
											}
											else{
												row_displacement[i,j]--;
											}
										}
									}
									if(col_displacement[i,j] < 0){
										if(Global.OneIn(10)){
											col_displacement[i,j]--;
										}
										else{
											col_displacement[i,j]++;
										}
									}
									else{
										if(col_displacement[i,j] > 0){
											if(Global.OneIn(10)){
												col_displacement[i,j]++;
											}
											else{
												col_displacement[i,j]--;
											}
										}
										else{
											if(Global.CoinFlip()){
												col_displacement[i,j]++;
											}
											else{
												col_displacement[i,j]--;
											}
										}
									}
								}
							}
						}
					}
					pos p = player.p;
					actor[p] = null;
					scr[p.row,p.col] = VisibleColorChar(p.row,p.col);
					actor[p] = player;
					int total_rd = 0;
					int total_cd = 0;
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							total_rd += row_displacement[i,j];
							total_cd += col_displacement[i,j];
						}
					}
					int avg_rd = total_rd / (ROWS*COLS);
					int avg_cd = total_cd / (ROWS*COLS);
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							row_displacement[i,j] -= avg_rd;
							col_displacement[i,j] -= avg_cd;
						}
					}
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(i == p.row && j == p.col){
								Screen.WriteMapChar(i,j,'@',Color.White);
							}
							else{
								if(Global.BoundsCheck(i+row_displacement[i,j],j+col_displacement[i,j])){
									Screen.WriteMapChar(i,j,scr[i+row_displacement[i,j],j+col_displacement[i,j]]);
								}
								else{
									Screen.WriteMapChar(i,j,Screen.BlankChar());
								}
							}
						}
					}*/
					//
					//
				}
				Screen.ResetColors();
			}
		}
		public void RedrawWithStrings(){
			Console.CursorVisible = false;
			cstr s;
			s.s = "";
			s.bgcolor = Color.Black;
			s.color = Color.Black;
			int r = 0;
			int c = 0;
			if(player.HasAttr(AttrType.ON_FIRE) || player.HasAttr(AttrType.CATCHING_FIRE)){
				Screen.DrawMapBorder(new colorchar(Color.RandomFire,'&'));
				for(int i=1;i<ROWS-1;++i){
					s.s = "";
					r = i;
					c = 1;
					for(int j=1;j<COLS-1;++j){
						colorchar ch = VisibleColorChar(i,j);
						if(Screen.ResolveColor(ch.color) != s.color){
							if(s.s.Length > 0){
								Screen.WriteMapString(r,c,s);
								s.s = "";
								s.s += ch.c;
								s.color = ch.color;
								r = i;
								c = j;
							}
							else{
								s.s += ch.c;
								s.color = ch.color;
							}
						}
						else{
							s.s += ch.c;
						}
					}
					Screen.WriteMapString(r,c,s);
				}
			}
			else{
				for(int i=0;i<ROWS;++i){
					s.s = "";
					r = i;
					c = 0;
					for(int j=0;j<COLS;++j){
						colorchar ch = VisibleColorChar(i,j);
						if(Screen.ResolveColor(ch.color) != s.color){
							if(s.s.Length > 0){
								Screen.WriteMapString(r,c,s);
								s.s = "";
								s.s += ch.c;
								s.color = ch.color;
								r = i;
								c = j;
							}
							else{
								s.s += ch.c;
								s.color = ch.color;
							}
						}
						else{
							s.s += ch.c;
						}
					}
					Screen.WriteMapString(r,c,s);
				}
			}
			Screen.ResetColors();
		}
		public colorchar VisibleColorChar(int r,int c){
			colorchar ch = Screen.BlankChar();
			if(player.CanSee(r,c)){
				tile[r,c].seen = true;
				if(tile[r,c].IsLit()){
					if(tile[r,c].IsTrapOrVent() || tile[r,c].IsShrine() || tile[r,c].Is(TileType.RUINED_SHRINE,TileType.STAIRS)){
						if(tile[r,c].name != "floor"){ //don't mark traps that aren't visible yet
							tile[r,c].revealed_by_light = true;
						}
					}
					if(tile[r,c].inv != null){
						tile[r,c].inv.revealed_by_light = true;
					}
				}
				if(actor[r,c] != null && player.CanSee(actor[r,c])){
					ch.c = actor[r,c].symbol;
					ch.color = actor[r,c].color;
					if(actor[r,c] == player && player.HasFeat(FeatType.DANGER_SENSE)
					&& danger_sensed != null && danger_sensed[r,c] && player.LightRadius() == 0
					&& !wiz_lite){
						ch.color = Color.Red;
					}
					else{
						if(actor[r,c] == player && !tile[r,c].IsLit()){
							if(player.HasAttr(AttrType.SHADOW_CLOAK)){
								ch.color = Color.DarkBlue;
							}
							else{
								ch.color = darkcolor;
							}
						}
					}
				}
				else{
					if(tile[r,c].inv != null){
						ch.c = tile[r,c].inv.symbol;
						ch.color = tile[r,c].inv.color;
						if(!tile[r,c].inv.revealed_by_light && !tile[r,c].IsLit()){
							ch.color = darkcolor;
						}
					}
					else{
						if(tile[r,c].features.Count > 0){
							ch.c = tile[r,c].FeatureSymbol();
							ch.color = tile[r,c].FeatureColor();
						}
						else{
							ch.c = tile[r,c].symbol;
							ch.color = tile[r,c].color;
							/*if((ch.c=='.' && ch.color == Color.White) || (ch.c=='#' && ch.color == Color.Gray)){
								if(tile[r,c].IsLit()){
									ch.color = Color.Yellow;
								}
								else{
									ch.color = Color.DarkCyan;
								}
							}*/
							if(!tile[r,c].revealed_by_light && !tile[r,c].IsLit()){
								ch.color = darkcolor;
							}
							if(player.HasFeat(FeatType.DANGER_SENSE) && danger_sensed != null
							   && danger_sensed[r,c] && player.LightRadius() == 0
							   && !wiz_lite && !tile[r,c].IsKnownTrap() && !tile[r,c].IsShrine()){
								ch.color = Color.Red;
							}
						}
					}
				}
			}
			else{
				if(actor[r,c] != null && player.CanSee(actor[r,c])){
					ch.c = actor[r,c].symbol;
					ch.color = actor[r,c].color;
				}
				else{
					if(tile[r,c].seen){
						if(tile[r,c].inv != null){
							ch.c = tile[r,c].inv.symbol;
							if(tile[r,c].inv.revealed_by_light){
								ch.color = tile[r,c].inv.color;
							}
							else{
								ch.color = unseencolor;
							}
						}
						else{
							List<FeatureType> list = new List<FeatureType>{/*FeatureType.FUNGUS_PRIMED,FeatureType.FUNGUS_ACTIVE,*/FeatureType.TELEPORTAL,FeatureType.RUNE_OF_RETREAT,/*FeatureType.FUNGUS,*/FeatureType.SLIME};
							bool feature = false;
							foreach(FeatureType ft in list){ //some features stay visible when out of sight
								if(tile[r,c].Is(ft)){
									feature = true;
									ch.c = Tile.Feature(ft).symbol;
									ch.color = Tile.Feature(ft).color;
									break;
								}
							}
							if(!feature){
								ch.c = tile[r,c].symbol;
								if(tile[r,c].revealed_by_light){
									ch.color = tile[r,c].color;
								}
								else{
									ch.color = unseencolor;
								}
								/*if((ch.c=='.' && ch.color == Color.White) || (ch.c=='#' && ch.color == Color.Gray)){
												ch.color = Color.DarkGray;
								}*/
							}
						}
					}
					else{
						ch.c = ' ';
						ch.color = Color.Black;
					}
				}
			}
			return ch;
		}
		public void RemoveTargets(Actor a){ //cleanup of references to dead monsters
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(actor[i,j]!=null){
						actor[i,j].RemoveTarget(a);
					}
				}
			}
		}
		public Item SpawnItem(){
			ConsumableType result = Item.RandomItem();
			for(bool done=false;!done;){
				int rr = Global.Roll(ROWS-2);
				int rc = Global.Roll(COLS-2);
				Tile t = tile[rr,rc];
				if(t.passable && t.inv == null && t.type != TileType.CHEST && t.type != TileType.FIREPIT
				&& t.type != TileType.STAIRS && !t.IsShrine()){
					return Item.Create(result,rr,rc);
					//done = true;
				}
			}
			//return result;
			return null;
		}
		public ActorType MobType(){
			ActorType result = ActorType.RAT;
			bool good_result = false;
			while(!good_result){
				int level = 1;
				int monster_depth = (current_level+1) / 2; //1-10, not 1-20
				if(current_level != 1){ //depth 1 only generates level 1 monsters
					List<int> levels = new List<int>();
					for(int i=-2;i<=2;++i){
						if(monster_depth + i >= 1 && monster_depth + i <= 10){
							int j = 1 + Math.Abs(i);
							if(Global.OneIn(j)){ //current depth is considered 1 out of 1 times, depth+1 and depth-1 one out of 2 times, etc.
								levels.Add(monster_depth + i);
							}
						}
					}
					level = levels.Random();
				}
				if(monster_depth == 1){ //level 1 monsters are all equal in rarity
					result = (ActorType)(level*7 + Global.Between(-4,2));
				}
				else{
					int roll = Global.Roll(100);
					if(roll <= 3){ //3% rare
						result = (ActorType)(level*7 + 2);
					}
					else{
						if(roll <= 22){ //19% uncommon (9.5% each)
							result = (ActorType)(level*7 + Global.Between(0,1));
						}
						else{ //78% common (19.5% each)
							result = (ActorType)(level*7 + Global.Between(-4,-1));
						}
					}
				}
				if(generated_this_level[result] == 0){
					good_result = true;
				}
				else{
					if(Global.OneIn(generated_this_level[result]+1)){ // 1 in 2 for the 2nd, 1 in 3 for the 3rd, and so on
						good_result = true;
					}
				}
			}
			generated_this_level[result]++;
			return result;
		}
		/*public ActorType MobType(){
			ActorType result = ActorType.RAT;
			bool good_result = false;
			while(!good_result){
				int level = 1;
				int monster_depth = (current_level+1) / 2; //1-10, not 1-20
				if(current_level != 1){ //depth 1 only generates level 1 monsters
					List<int> levels = new List<int>();
					for(int i=-2;i<=2;++i){
						if(monster_depth + i >= 1 && monster_depth + i <= 10){
							int j = 1 + Math.Abs(i);
							if(Global.OneIn(j)){ //current depth is considered 1 out of 1 times, depth+1 and depth-1 one out of 2 times, etc.
								levels.Add(monster_depth + i);
							}
						}
					}
					level = levels.Random();
				}
				if(monster_depth == 1){
					//result = (ActorType)(level*5 + Global.Roll(5) - 3); //equal probability for the level 1 monsters
					result = (ActorType)(level*7 + Global.Between(-4,2));
				}
				else{
					int roll = Global.Roll(100);
					if(roll <= 4){ //4% rare
						result = (ActorType)(level*7 + 2);
					}
					else{
						if(roll <= 20){ //16% uncommon (8% each)
							result = (ActorType)(level*7 + Global.Between(0,1));
						}
						else{ //80% common (20% each)
							result = (ActorType)(level*7 + Global.Between(-4,-1));
						}
					}
				}
				if(current_level <= 2){ //the first 2 levels try to generate a wider variety of types
					if(generated_this_level[result] == 0){ //todo update
						good_result = true;
					}
					else{
						if(Global.OneIn(generated_this_level[result]+1)){ // 1 in 2 for the 2nd, 1 in 3 for the 3rd, and so on
							good_result = true;
						}
					}
				}
				else{
					if(generated_this_level[result] < 2){
						good_result = true;
					}
					else{
						if(Global.OneIn(generated_this_level[result])){ // 1 in 2 for the 3rd, 1 in 3 for the 4th, and so on
							good_result = true;
						}
					}
				}
			}
			generated_this_level[result]++;
			return result;
		}*/
		public Actor SpawnMob(){ return SpawnMob(MobType()); }
		public Actor SpawnMob(ActorType type){
			Actor result = null;
			if(type == ActorType.POLTERGEIST){
				for(int tries=0;tries<1000;++tries){
					int rr = Global.Roll(ROWS-4) + 1;
					int rc = Global.Roll(COLS-4) + 1;
					List<Tile> tiles = new List<Tile>();
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(3)){
						if(t.passable || t.type == TileType.DOOR_C){
							tiles.Add(t);
						}
					}
					if(tiles.Count >= 15){
						Actor.tiebreakers.Add(null); //a placeholder for the poltergeist once it manifests
						Event e = new Event(null,tiles,(Global.Roll(8)+6)*100,EventType.POLTERGEIST);
						e.tiebreaker = Actor.tiebreakers.Count - 1;
						Q.Add(e);
						//return type;
						return null;
					}
				}
				return null;
			}
			if(type == ActorType.MIMIC){
				while(true){
					int rr = Global.Roll(ROWS-2);
					int rc = Global.Roll(COLS-2);
					Tile t = tile[rr,rc];
					if(t.passable && t.inv == null && t.type != TileType.CHEST && t.type != TileType.FIREPIT
					&& t.type != TileType.STAIRS && !t.IsShrine()){
						Item item = Item.Create(Item.RandomItem(),rr,rc);
						Actor.tiebreakers.Add(null); //placeholder
						Event e = new Event(item,new List<Tile>{t},100,EventType.MIMIC,AttrType.NO_ATTR,0,"");
						e.tiebreaker = Actor.tiebreakers.Count - 1;
						Q.Add(e);
						return null;
					}
				}
			}
			if(type == ActorType.MARBLE_HORROR){
				Tile statue = AllTiles().Where(t=>t.type == TileType.STATUE).Random();
				if(statue != null){
					Q.Add(new Event(statue,100,EventType.MARBLE_HORROR));
				}
				return null;
			}
			int number = 1;
			if(Actor.Prototype(type).HasAttr(AttrType.SMALL_GROUP)){
				number = Global.Roll(2)+1;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.MEDIUM_GROUP)){
				number = Global.Roll(2)+2;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.LARGE_GROUP)){
				number = Global.Roll(2)+4;
			}
			List<Tile> group_tiles = new List<Tile>();
			List<Actor> group = null;
			if(number > 1){
				group = new List<Actor>();
			}
			for(int i=0;i<number;++i){
				if(i == 0){
					for(int j=0;j<9999;++j){
						int rr = Global.Roll(ROWS-2);
						int rc = Global.Roll(COLS-2);
						bool good = true;
						foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
							if(t.IsTrap()){
								good = false;
							}
						}
						if(good && tile[rr,rc].passable && actor[rr,rc] == null){
							result = Actor.Create(type,rr,rc,true,false);
							if(number > 1){
								group_tiles.Add(tile[rr,rc]);
								group.Add(result);
								result.group = group;
							}
							break;
						}
					}
				}
				else{
					for(int j=0;j<9999;++j){
						if(group_tiles.Count == 0){ //no space left!
							if(group.Count > 0){
								return group[0];
							}
							else{
								return result;
							}
						}
						Tile t = group_tiles.Random();
						List<Tile> empty_neighbors = new List<Tile>();
						foreach(Tile neighbor in t.TilesAtDistance(1)){
							if(neighbor.passable && !neighbor.IsTrap() && neighbor.actor() == null){
								empty_neighbors.Add(neighbor);
							}
						}
						if(empty_neighbors.Count > 0){
							t = empty_neighbors.Random();
							result = Actor.Create(type,t.row,t.col,true,false);
							group_tiles.Add(t);
							group.Add(result);
							result.group = group;
							break;
						}
						else{
							group_tiles.Remove(t);
						}
					}
				}
			}
			//return type;
			if(number > 1){
				return group[0];
			}
			else{
				return result;
			}
		}
		public void GenerateLevel(){
			if(current_level < 20){
				++current_level;
			}
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(actor[i,j] != null){
						if(actor[i,j] != player){
							actor[i,j].inv.Clear();
							actor[i,j].target = null;
							//Q.KillEvents(actor[i,j],EventType.ANY_EVENT);
							if(actor[i,j].group != null){
								actor[i,j].group.Clear();
								actor[i,j].group = null;
							}
						}
						actor[i,j] = null;
					}
					if(tile[i,j] != null){
						tile[i,j].inv = null;
					}
					tile[i,j] = null;
				}
			}
			wiz_lite = false;
			wiz_dark = false;
			generated_this_level = new Dict<ActorType, int>();
			/*Q.KillEvents(null,EventType.BLAST_FUNGUS);
			Q.KillEvents(null,EventType.RELATIVELY_SAFE);
			Q.KillEvents(null,EventType.POLTERGEIST);
			Q.KillEvents(null,EventType.MIMIC);
			Q.KillEvents(null,EventType.FIRE_GEYSER);
			Q.KillEvents(null,EventType.FIRE_GEYSER_ERUPTION);
			Q.KillEvents(null,EventType.FOG);
			Q.KillEvents(null,EventType.GRENADE);
			Q.KillEvents(null,EventType.POISON_GAS);
			Q.KillEvents(null,EventType.QUICKFIRE);
			Q.KillEvents(null,EventType.REGENERATING_FROM_DEATH);
			Q.KillEvents(null,EventType.STALAGMITE);*/
			Q.ResetForNewLevel();
			Actor.tiebreakers = new List<Actor>{player};
			//alltiles.Clear();
			DungeonGen.StandardDungeon dungeon = new DungeonGen.StandardDungeon();
			char[,] charmap = null;
			switch(level_types[current_level-1]){
			case LevelType.Standard:
				charmap = dungeon.GenerateStandard();
				break;
			case LevelType.Cave:
				charmap = dungeon.GenerateCave();
				break;
			case LevelType.Ruined:
				charmap = dungeon.GenerateRuined();
				break;
			case LevelType.Hive:
				charmap = dungeon.GenerateHive();
				break;
			case LevelType.Mine:
				charmap = dungeon.GenerateMine();
				break;
			case LevelType.Fortress:
				charmap = dungeon.GenerateFortress();
				break;
			case LevelType.Extravagant:
				charmap = dungeon.GenerateExtravagant();
				break;
			}
			/*for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					Screen.WriteMapChar(i,j,charmap[i,j]);
				}
			}
			Console.ReadKey(true);*/
			List<pos> interesting_tiles = new List<pos>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(charmap[i,j] == '$'){
						interesting_tiles.Add(new pos(i,j));
					}
				}
			}
			int attempts = 0;
			if(current_level%2 == 1){
				List<int> ints = new List<int>{0,1,2,3,4};
				while(ints.Count > 0){
					attempts = 0;
					for(bool done=false;!done;++attempts){
						int rr = Global.Roll(ROWS-4) + 1;
						int rc = Global.Roll(COLS-4) + 1;
						if(interesting_tiles.Count > 0 && (ints.Count > 1 || attempts > 1000)){
							pos p = interesting_tiles.Random();
							rr = p.row;
							rc = p.col;
							charmap[rr,rc] = '.';
						}
						pos temp = new pos(rr,rc);
						if(ints.Count > 1){
							if(charmap[rr,rc] == '.'){
								if(attempts > 1000){
									bool good = true;
									foreach(pos p in temp.PositionsWithinDistance(4)){
										char ch = charmap[p.row,p.col];
										if(ch == 'a' || ch == 'b' || ch == 'c' || ch == 'd' || ch == 'e'){
											good = false;
										}
									}
									if(good){
										List<pos> dist2 = new List<pos>();
										foreach(pos p2 in temp.PositionsAtDistance(2)){
											if(charmap[p2.row,p2.col] == '.'){
												dist2.Add(p2);
											}
										}
										if(dist2.Count > 0){
											charmap[rr,rc] = (char)(((char)ints.RemoveRandom()) + 'a');
											pos p2 = dist2.Random();
											charmap[p2.row,p2.col] = (char)(((char)ints.RemoveRandom()) + 'a');
											done = true;
											break;
										}
									}
									else{
										interesting_tiles.Remove(temp);
									}
								}
								bool floors = true;
								foreach(pos p in temp.PositionsAtDistance(1)){
									if(charmap[p.row,p.col] != '.'){
										floors = false;
									}
								}
								foreach(pos p in temp.PositionsWithinDistance(3)){
									char ch = charmap[p.row,p.col];
									if(ch == 'a' || ch == 'b' || ch == 'c' || ch == 'd' || ch == 'e'){
										floors = false;
									}
								}
								if(floors){
									if(Global.CoinFlip()){
										charmap[rr-1,rc] = (char)(((char)ints.RemoveRandom()) + 'a');
										charmap[rr+1,rc] = (char)(((char)ints.RemoveRandom()) + 'a');
									}
									else{
										charmap[rr,rc-1] = (char)(((char)ints.RemoveRandom()) + 'a');
										charmap[rr,rc+1] = (char)(((char)ints.RemoveRandom()) + 'a');
									}
									char center = ' ';
									switch(Global.Roll(3)){
									case 1:
										center = '#';
										break;
									case 2:
										center = '&';
										break;
									case 3:
										center = '0';
										break;
									}
									charmap[rr,rc] = center;
									interesting_tiles.Remove(temp);
									done = true;
									break;
								}
							}
						}
						else{
							if(charmap[rr,rc] == '.'){
								bool good = true;
								foreach(pos p in temp.PositionsWithinDistance(2)){
									char ch = charmap[p.row,p.col];
									if(ch == 'a' || ch == 'b' || ch == 'c' || ch == 'd' || ch == 'e'){
										good = false;
									}
								}
								if(good){
									if(attempts > 1000){
										charmap[rr,rc] = (char)(((char)ints.RemoveRandom()) + 'a');
										interesting_tiles.Remove(temp);
										done = true;
										break;
									}
									else{
										bool floors = true;
										foreach(pos p in temp.PositionsAtDistance(1)){
											if(charmap[p.row,p.col] != '.'){
												floors = false;
											}
										}
										if(floors){
											charmap[rr,rc] = (char)(((char)ints.RemoveRandom()) + 'a');
											interesting_tiles.Remove(temp);
											done = true;
											break;
										}
									}
								}
							}
							if(charmap[rr,rc] == '#'){
								if(charmap[rr+1,rc] != '.' && charmap[rr-1,rc] != '.' && charmap[rr,rc-1] != '.' && charmap[rr,rc+1] != '.'){
									continue; //no floors? retry.
								}
								bool no_good = false;
								foreach(pos p in temp.PositionsAtDistance(2)){
									char ch = charmap[p.row,p.col];
									if(ch == 'a' || ch == 'b' || ch == 'c' || ch == 'd' || ch == 'e'){
										no_good = true;
									}
								}
								if(no_good){
									continue;
								}
								int walls = 0;
								foreach(pos p in temp.PositionsAtDistance(1)){
									if(charmap[p.row,p.col] == '#'){
										++walls;
									}
								}
								if(walls >= 5){
									int successive_walls = 0;
									char[] rotated = new char[8];
									for(int i=0;i<8;++i){
										pos temp2;
										temp2 = temp.PositionInDirection(8.RotateDirection(true,i));
										rotated[i] = charmap[temp2.row,temp2.col];
									}
									for(int i=0;i<15;++i){
										if(rotated[i%8] == '#'){
											++successive_walls;
										}
										else{
											successive_walls = 0;
										}
										if(successive_walls == 5){
											done = true;
											charmap[rr,rc] = (char)(((char)ints.RemoveRandom()) + 'a');
											interesting_tiles.Remove(temp);
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			int num_chests = Global.Roll(2);
			if(Global.OneIn(50)){
				num_chests = 3;
			}
			for(int i=0;i<num_chests;++i){
				int tries = 0;
				for(bool done=false;!done;++tries){
					int rr = Global.Roll(ROWS-4) + 1;
					int rc = Global.Roll(COLS-4) + 1;
					if(interesting_tiles.Count > 0){
						pos p = interesting_tiles.RemoveRandom();
						rr = p.row;
						rc = p.col;
						charmap[rr,rc] = '.';
					}
					if(charmap[rr,rc] == '.'){
						bool floors = true;
						pos temp = new pos(rr,rc);
						foreach(pos p in temp.PositionsAtDistance(1)){
							if(charmap[p.row,p.col] != '.'){
								floors = false;
							}
						}
						if(floors || tries > 1000){ //after 1000 tries, place it anywhere
							charmap[rr,rc] = '=';
							done = true;
						}
					}
				}
			}
			attempts = 0;
			for(bool done=false;!done;++attempts){
				int rr = Global.Roll(ROWS-4) + 1;
				int rc = Global.Roll(COLS-4) + 1;
				if(interesting_tiles.Count > 0){
					pos p = interesting_tiles.RemoveRandom();
					rr = p.row;
					rc = p.col;
					charmap[rr,rc] = '.';
				}
				if(charmap[rr,rc] == '.'){
					bool floors = true;
					pos temp = new pos(rr,rc);
					foreach(pos p in temp.PositionsAtDistance(1)){
						if(charmap[p.row,p.col] != '.'){
							floors = false;
						}
					}
					if(floors || attempts > 1000){
						charmap[rr,rc] = '>';
						done = true;
					}
				}
			}
			if(Global.OneIn(30)){
				attempts = 0;
				for(bool done=false;!done;++attempts){
					int rr = Global.Roll(ROWS-4) + 1;
					int rc = Global.Roll(COLS-4) + 1;
					if(interesting_tiles.Count > 0){
						pos p = interesting_tiles.RemoveRandom();
						rr = p.row;
						rc = p.col;
						charmap[rr,rc] = '.';
					}
					if(charmap[rr,rc] == '.'){
						bool floors = true;
						pos temp = new pos(rr,rc);
						foreach(pos p in temp.PositionsAtDistance(1)){
							if(charmap[p.row,p.col] != '.'){
								floors = false;
							}
						}
						if(floors || attempts > 1000){
							charmap[rr,rc] = 'P';
							done = true;
						}
					}
				}
			}
			if(Global.OneIn(20) && current_level > 2){
				LevelType lt = level_types[current_level-1];
				if(lt == LevelType.Standard || lt == LevelType.Cave || lt == LevelType.Ruined || lt == LevelType.Mine){
					switch(Global.Roll(4)){
					case 1:
					case 2:
						for(attempts=0;attempts<100;++attempts){
							int rr = Global.Roll(ROWS-4) + 1;
							int rc = Global.Roll(COLS-4) + 1;
							if(charmap[rr,rc] == '.'){
								charmap[rr,rc] = 'F';
								break;
							}
						}
						break;
					case 3:
						for(attempts=0;attempts<100;++attempts){
							int rr = Global.Roll(ROWS-4) + 1;
							int rc = Global.Roll(COLS-4) + 1;
							pos p = new pos(rr,rc);
							if(charmap[rr,rc] == '.'){
								List<pos> other_pos = new List<pos>();
								foreach(pos nearby in p.PositionsWithinDistance(5,true)){
									if(charmap[nearby.row,nearby.col] == '.'){
										other_pos.Add(nearby);
									}
								}
								if(other_pos.Count > 0){
									charmap[rr,rc] = 'F';
									pos other = other_pos.Random();
									charmap[other.row,other.col] = 'F';
									break;
								}
							}
						}
						break;
					case 4:
					{
						int num = Global.Roll(5)+2;
						for(int i=0;i<num;++i){
							for(attempts=0;attempts<100;++attempts){
								int rr = Global.Roll(ROWS-4) + 1;
								int rc = Global.Roll(COLS-4) + 1;
								if(charmap[rr,rc] == '.'){
									charmap[rr,rc] = 'F';
									break;
								}
							}
						}
						break;
					}
					}
				}
			}
			int num_traps = Global.Roll(2,3);
			for(int i=0;i<num_traps;++i){
				int tries = 0;
				for(bool done=false;!done && tries < 100;++tries){
					int rr = Global.Roll(ROWS-2);
					int rc = Global.Roll(COLS-2);
					if(charmap[rr,rc] == '.'){
						charmap[rr,rc] = '^';
						done = true;
					}
				}
			}
			int percentage_of_traps_to_become_vents = 1;
			if(level_types[current_level-1] == LevelType.Cave || level_types[current_level-1] == LevelType.Hive){
				percentage_of_traps_to_become_vents = 20;
			}
			if(level_types[current_level-1] == LevelType.Mine){
				percentage_of_traps_to_become_vents = 5;
			}
			if(level_types[current_level-1] == LevelType.Extravagant || level_types[current_level-1] == LevelType.Fortress){
				percentage_of_traps_to_become_vents = 0;
			}
			List<Tile> hidden = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					//Screen.WriteMapChar(i,j,charmap[i,j]);
					switch(charmap[i,j]){
					case '#':
						Tile.Create(TileType.WALL,i,j);
						break;
					case '.':
					case '$':
						Tile.Create(TileType.FLOOR,i,j);
						break;
					case '+':
						Tile.Create(TileType.DOOR_C,i,j);
						break;
					case '-':
						Tile.Create(TileType.DOOR_O,i,j);
						break;
					case '>':
						if(current_level < 20){
							Tile.Create(TileType.STAIRS,i,j);
						}
						else{
							if(current_level == 20){
								Tile.Create(TileType.STAIRS,i,j);
								tile[i,j].color = Color.Red;
								tile[i,j].SetName("scorched stairway");
							}
							else{
								Tile.Create(TileType.FLOOR,i,j);
							}
						}
						break;
					case '&':
						Tile.Create(TileType.STATUE,i,j);
						break;
					case ':':
						Tile.Create(TileType.RUBBLE,i,j);
						break;
					case '0':
						Tile.Create(TileType.FIREPIT,i,j);
						break;
					case 'P':
						Tile.Create(TileType.HEALING_POOL,i,j);
						break;
					case 'F':
						Tile.Create(TileType.BLAST_FUNGUS,i,j);
						//tile[i,j].features.Add(FeatureType.FUNGUS);
						break;
					case '=':
						Tile.Create(TileType.CHEST,i,j);
						break;
					case '~':
						Tile.Create(TileType.FIRE_GEYSER,i,j);
						break;
					case '^':
					{
						TileType type = Tile.RandomTrap();
						if(Global.Roll(100) <= percentage_of_traps_to_become_vents){
							type = Tile.RandomVent();
						}
						Tile.Create(type,i,j);
						tile[i,j].name = "floor";
						tile[i,j].the_name = "the floor";
						tile[i,j].a_name = "a floor";
						tile[i,j].symbol = '.';
						tile[i,j].color = Color.White;
						hidden.Add(tile[i,j]);
						if(type == TileType.FIRE_GEYSER){
							int frequency = Global.Roll(21) + 4; //5-25
							int variance = Global.Roll(10) - 1; //0-9
							int variance_amount = (frequency * variance) / 10;
							int number_of_values = variance_amount*2 + 1;
							int minimum_value = frequency - variance_amount;
							if(minimum_value < 5){
								int diff = 5 - minimum_value;
								number_of_values -= diff;
								minimum_value = 5;
							}
							int delay = ((minimum_value - 1) + Global.Roll(number_of_values)) * 100;
							Q.Add(new Event(tile[i,j],delay + 200,EventType.FIRE_GEYSER,(frequency*10)+variance)); //notice the hacky way the value is stored
							Q.Add(new Event(tile[i,j],delay,EventType.FIRE_GEYSER_ERUPTION,2));
						}
						if(type == TileType.FOG_VENT){
							Q.Add(new Event(tile[i,j],100,EventType.FOG_VENT));
						}
						if(type == TileType.POISON_GAS_VENT){
							Q.Add(new Event(tile[i,j],100,EventType.POISON_GAS_VENT));
						}
						break;
					}
					case 'H':
						Tile.Create(TileType.HIDDEN_DOOR,i,j);
						hidden.Add(tile[i,j]);
						break;
					case 'a':
						Tile.Create(Forays.TileType.COMBAT_SHRINE,i,j);
						break;
					case 'b':
						Tile.Create(Forays.TileType.DEFENSE_SHRINE,i,j);
						break;
					case 'c':
						Tile.Create(Forays.TileType.MAGIC_SHRINE,i,j);
						break;
					case 'd':
						Tile.Create(Forays.TileType.SPIRIT_SHRINE,i,j);
						break;
					case 'e':
						Tile.Create(Forays.TileType.STEALTH_SHRINE,i,j);
						break;
					default:
						Tile.Create(TileType.FLOOR,i,j);
						break;
					}
					//alltiles.Add(tile[i,j]);
					tile[i,j].solid_rock = true;
				}
			}
			//Console.ReadKey(true);
			player.ResetForNewLevel();
			foreach(Tile t in AllTiles()){
				if(t.light_radius > 0){
					t.UpdateRadius(0,t.light_radius);
				}
				/*if(t.type == TileType.FIREPIT){
					foreach(Tile tt in t.TilesWithinDistance(1)){
						tt.light_value++;
					}
				}*/
			}
			int num_items = 1;
			switch(Global.Roll(5)){
			case 1:
				num_items = 0;
				break;
			case 5:
				num_items = 2;
				break;
			}
			for(int i=num_items;i>0;--i){
				SpawnItem();
			}
			bool poltergeist_spawned = false; //i'm not sure this is the right call, but for now
			bool mimic_spawned = false; // i'm limiting these guys, to avoid "empty" levels
			bool marble_horror_spawned = false;
			for(int i=Global.Roll(2,2)+3;i>0;--i){
				ActorType type = MobType();
				if(type == ActorType.POLTERGEIST){
					if(!poltergeist_spawned){
						SpawnMob(type);
						poltergeist_spawned = true;
					}
					else{
						++i; //try again..
					}
				}
				else{
					if(type == ActorType.MIMIC){
						if(!mimic_spawned){
							SpawnMob(type);
							mimic_spawned = true;
						}
						else{
							++i;
						}
					}
					else{
						if(type == ActorType.MARBLE_HORROR){
							Tile statue = AllTiles().Where(t=>t.type == TileType.STATUE).Random();
							if(!marble_horror_spawned && statue != null){
								SpawnMob(type);
								marble_horror_spawned = true;
							}
							else{
								++i;
							}
						}
						else{
							if(type == ActorType.ENTRANCER){
								if(i >= 2){ //need 2 slots here
									Actor entrancer = SpawnMob(type);
									entrancer.attrs[Forays.AttrType.WANDERING]++;
									List<Tile> tiles = new List<Tile>();
									int dist = 1;
									while(tiles.Count == 0 && dist < 100){
										foreach(Tile t in entrancer.TilesAtDistance(dist)){
											if(t.passable && !t.IsTrap() && t.actor() == null){
												tiles.Add(t);
											}
										}
										++dist;
									}
									if(tiles.Count > 0){
										ActorType thralltype = ActorType.RAT;
										bool done = false;
										while(!done){
											thralltype = MobType();
											switch(thralltype){
											case ActorType.ZOMBIE:
											case ActorType.ROBED_ZEALOT:
											case ActorType.BANSHEE:
											case ActorType.WARG:
											case ActorType.DERANGED_ASCETIC:
											case ActorType.NOXIOUS_WORM:
											case ActorType.BERSERKER:
											case ActorType.TROLL:
											case ActorType.VAMPIRE:
											case ActorType.CRUSADING_KNIGHT:
											case ActorType.SKELETAL_SABERTOOTH:
											case ActorType.OGRE:
											case ActorType.SHADOWVEIL_DUELIST:
											case ActorType.STONE_GOLEM:
											case ActorType.LUMINOUS_AVENGER:
											case ActorType.CORPSETOWER_BEHEMOTH:
												done = true;
												break;
											}
										}
										Tile t = tiles.Random();
										Actor thrall = Actor.Create(thralltype,t.row,t.col,true,true);
										if(entrancer.group == null){
											entrancer.group = new List<Actor>{entrancer};
										}
										entrancer.group.Add(thrall);
										thrall.group = entrancer.group;
										--i;
									}
								}
								else{
									++i;
								}
							}
							else{
								Actor a = SpawnMob(type);
								if(a.AlwaysWanders() || (Global.CoinFlip() && a.CanWander())){
									a.attrs[Forays.AttrType.WANDERING]++;
								}
							}
						}
					}
				}
			}
			bool[,] good_location = new bool[ROWS,COLS];
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(tile[i,j].type == TileType.FLOOR){
						good_location[i,j] = true;
					}
					else{
						good_location[i,j] = false;
					}
				}
			}
			foreach(Actor a in AllActors()){
				if(a != player){
					good_location[a.row,a.col] = false;
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(good_location[i,j] && a.HasLOS(i,j)){
								good_location[i,j] = false;
							}
						}
					}
				}
			}
			bool at_least_one_good = false;
			for(int i=0;i<ROWS && !at_least_one_good;++i){
				for(int j=0;j<COLS && !at_least_one_good;++j){
					if(good_location[i,j]){
						at_least_one_good = true;
					}
				}
			}
			if(!at_least_one_good){
				foreach(Actor a in AllActors()){
					if(a != player){
						good_location[a.row,a.col] = false;
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(good_location[i,j] && a.CanSee(i,j)){ //checking CanSee this time
									good_location[i,j] = false;
								}
							}
						}
					}
				}
			}
			List<Tile> goodtiles = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(good_location[i,j]){
						goodtiles.Add(tile[i,j]);
					}
				}
			}
			if(goodtiles.Count > 0){
				Tile t = goodtiles.Random();
				int light = player.light_radius;
				player.light_radius = 0;
				player.Move(t.row,t.col);
				player.UpdateRadius(0,light,true);
			}
			else{
				for(bool done=false;!done;){
					int rr = Global.Roll(ROWS-2);
					int rc = Global.Roll(COLS-2);
					bool good = true;
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
						if(t.IsTrap()){
							good = false;
						}
					}
					if(good && tile[rr,rc].passable && actor[rr,rc] == null){
						int light = player.light_radius;
						int fire = player.attrs[Forays.AttrType.ON_FIRE];
						player.light_radius = 0;
						player.attrs[Forays.AttrType.ON_FIRE] = 0;
						player.Move(rr,rc);
						player.UpdateRadius(0,Math.Max(light,fire),true);
						player.light_radius = light;
						player.attrs[Forays.AttrType.ON_FIRE] = fire;
						done = true;
					}
				}
			}
			if(Global.CoinFlip()){ //is 50% the best rate for hidden areas? it seems to be working well so far.
				bool done = false;
				for(int tries=0;!done && tries<500;++tries){
					int rr = Global.Roll(ROWS-4) + 1;
					int rc = Global.Roll(COLS-4) + 1;
					bool good = true;
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(2)){
						if(t.type != TileType.WALL){
							good = false;
							break;
						}
					}
					if(good){
						List<int> dirs = new List<int>();
						bool long_corridor = false;
						int connections = 0;
						for(int i=2;i<=8;i+=2){
							Tile t = tile[rr,rc].TileInDirection(i).TileInDirection(i);
							bool good_dir = true;
							int distance = -1;
							while(good_dir && t != null && t.type == TileType.WALL){
								if(t.TileInDirection(t.RotateDirection(i,false,2)).type != TileType.WALL){
									good_dir = false;
								}
								if(t.TileInDirection(t.RotateDirection(i,true,2)).type != TileType.WALL){
									good_dir = false;
								}
								t = t.TileInDirection(i);
								if(t != null && t.type == TileType.STATUE){
									good_dir = false;
								}
								++distance;
							}
							if(good_dir && t != null){
								dirs.Add(i);
								++connections;
								if(distance >= 4){
									long_corridor = true;
								}
							}
						}
						if(dirs.Count > 0){
							List<TileType> possible_traps = new List<TileType>();
							int trap_roll = Global.Roll(7);
							if(trap_roll == 1 || trap_roll == 4 || trap_roll == 5 || trap_roll == 7){
								possible_traps.Add(TileType.GRENADE_TRAP);
							}
							if(trap_roll == 2 || trap_roll == 4 || trap_roll == 6 || trap_roll == 7){
								possible_traps.Add(TileType.POISON_GAS_TRAP);
							}
							if(trap_roll == 3 || trap_roll == 5 || trap_roll == 6 || trap_roll == 7){
								possible_traps.Add(TileType.PHANTOM_TRAP);
							}
							bool stone_slabs = false; //(instead of hidden doors)
							if(Global.OneIn(4)){
								stone_slabs = true;
							}
							foreach(int i in dirs){
								Tile t = tile[rr,rc].TileInDirection(i);
								int distance = -2; //distance of the corridor between traps and secret door
								while(t.type == TileType.WALL){
									++distance;
									t = t.TileInDirection(i);
								}
								if(long_corridor && distance < 4){
									continue;
								}
								t = tile[rr,rc].TileInDirection(i);
								while(t.type == TileType.WALL){
									if(distance >= 4){
										TileType tt = TileType.FLOOR;
										if(Global.Roll(3) >= 2){
											tt = possible_traps.Random();
											hidden.Add(t);
										}
										t.TransformTo(tt);
										t.name = "floor";
										t.the_name = "the floor";
										t.a_name = "a floor";
										t.symbol = '.';
										t.color = Color.White;
										if(t.DistanceFrom(tile[rr,rc]) < distance+2){
											Tile neighbor = t.TileInDirection(t.RotateDirection(i,false,2));
											if(neighbor.TileInDirection(t.RotateDirection(i,false,1)).type == TileType.WALL
											   && neighbor.TileInDirection(t.RotateDirection(i,false,2)).type == TileType.WALL
											   && neighbor.TileInDirection(t.RotateDirection(i,false,3)).type == TileType.WALL){
												tt = TileType.FLOOR;
												if(Global.Roll(3) >= 2){
													tt = possible_traps.Random();
												}
												neighbor.TransformTo(tt);
												if(possible_traps.Contains(tt)){
													neighbor.name = "floor";
													neighbor.the_name = "the floor";
													neighbor.a_name = "a floor";
													neighbor.symbol = '.';
													neighbor.color = Color.White;
													hidden.Add(neighbor);
												}
											}
											neighbor = t.TileInDirection(t.RotateDirection(i,true,2));
											if(neighbor.TileInDirection(t.RotateDirection(i,true,1)).type == TileType.WALL
											   && neighbor.TileInDirection(t.RotateDirection(i,true,2)).type == TileType.WALL
											   && neighbor.TileInDirection(t.RotateDirection(i,true,3)).type == TileType.WALL){
												tt = TileType.FLOOR;
												if(Global.Roll(3) >= 2){
													tt = possible_traps.Random();
												}
												neighbor.TransformTo(tt);
												if(possible_traps.Contains(tt)){
													neighbor.name = "floor";
													neighbor.the_name = "the floor";
													neighbor.a_name = "a floor";
													neighbor.symbol = '.';
													neighbor.color = Color.White;
													hidden.Add(neighbor);
												}
											}
										}
									}
									else{
										TileType tt = TileType.FLOOR;
										if(Global.CoinFlip()){
											tt = Tile.RandomTrap();
											hidden.Add(t);
										}
										t.TransformTo(tt);
										if(tt != TileType.FLOOR){
											t.name = "floor";
											t.the_name = "the floor";
											t.a_name = "a floor";
											t.symbol = '.';
											t.color = Color.White;
										}
									}
									t = t.TileInDirection(i);
								}
								t = t.TileInDirection(t.RotateDirection(i,true,4));
								if(stone_slabs){
									t.TransformTo(TileType.STONE_SLAB);
									Q.Add(new Event(t,new List<Tile>{t.TileInDirection(i.RotateDirection(true,4))},100,EventType.STONE_SLAB));
								}
								else{
									t.TransformTo(TileType.HIDDEN_DOOR);
									hidden.AddUnique(t);
								}
								t = t.TileInDirection(t.RotateDirection(i,true,4));
								if(Global.CoinFlip()){
									if(t.IsTrap()){
										t.type = TileType.ALARM_TRAP;
									}
									else{
										t.TransformTo(Forays.TileType.ALARM_TRAP);
										t.name = "floor";
										t.the_name = "the floor";
										t.a_name = "a floor";
										t.symbol = '.';
										t.color = Color.White;
										hidden.AddUnique(t);
									}
								}
							}
							if(long_corridor && connections == 1){
								foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
									t.TransformTo(possible_traps.Random());
									t.name = "floor";
									t.the_name = "the floor";
									t.a_name = "a floor";
									t.symbol = '.';
									t.color = Color.White;
									hidden.Add(t);
								}
								tile[rr,rc].TileInDirection(tile[rr,rc].RotateDirection(dirs[0],true,4)).TransformTo(TileType.CHEST);
							}
							else{
								foreach(Tile t in tile[rr,rc].TilesAtDistance(1)){
									t.TransformTo(Tile.RandomTrap());
									t.name = "floor";
									t.the_name = "the floor";
									t.a_name = "a floor";
									t.symbol = '.';
									t.color = Color.White;
									hidden.Add(t);
								}
								tile[rr,rc].TransformTo(TileType.CHEST);
							}
							done = true;
						}
					}
				}
			}
			foreach(Tile t in AllTiles()){
				if(t.type != TileType.WALL){
					foreach(Tile neighbor in t.TilesAtDistance(1)){
						neighbor.solid_rock = false;
					}
				}
			}
			if(hidden.Count > 0){
				Event e = new Event(hidden,100,EventType.CHECK_FOR_HIDDEN);
				e.tiebreaker = 0;
				Q.Add(e);
			}
			if(current_level == 20){
				Event e = new Event(1066,EventType.BOSS_SIGN);
				e.tiebreaker = 0;
				Q.Add(e);
			}
			{
				Event e = new Event(10000,EventType.RELATIVELY_SAFE);
				e.tiebreaker = 0;
				Q.Add(e);
			}
			if(current_level == 1){
				B.Add("Welcome, " + Actor.player_name + "! ");
			}
			else{
				B.Add(LevelMessage());
			}
		}
		public void GenerateBossLevel(bool boss_already_on_level){
			current_level = 21;
			int boss_hp = -1;
			foreach(Event e in Q.list){
				if(e.type == EventType.BOSS_ARRIVE){
					boss_hp = e.value;
					break;
				}
			}
			foreach(Actor a in AllActors()){
				if(a.type == ActorType.FIRE_DRAKE){
					boss_hp = a.curhp;
					break;
				}
			}
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(actor[i,j] != null){
						if(actor[i,j] != player){
							actor[i,j].inv.Clear();
							actor[i,j].target = null;
							if(actor[i,j].group != null){
								actor[i,j].group.Clear();
								actor[i,j].group = null;
							}
						}
						actor[i,j] = null;
					}
					if(tile[i,j] != null){
						tile[i,j].inv = null;
					}
					tile[i,j] = null;
				}
			}
			wiz_lite = false;
			wiz_dark = false;
			LinkedList<Event> newlist = new LinkedList<Event>();
			for(LinkedListNode<Event> current = Q.list.First;current!=null;current = current.Next){
				if(current.Value.target == Event.player || current.Value.type == EventType.CEILING_COLLAPSE || current.Value.type == EventType.FLOOR_COLLAPSE){
					if(current.Value.type == EventType.FLOOR_COLLAPSE){
						current.Value.target = null;
					}
					newlist.AddLast(current.Value);
				}
			}
			Q.list = newlist; //same as Q.ResetForNewLevel, but it keeps collapse events.
			Actor.tiebreakers = new List<Actor>{player};
			DungeonGen.StandardDungeon dungeon = new DungeonGen.StandardDungeon();
			char[,] charmap = dungeon.GenerateCave();
			int num_traps = Global.Roll(1,3);
			for(int i=0;i<num_traps;++i){
				int tries = 0;
				for(bool done=false;!done && tries < 100;++tries){
					int rr = Global.Roll(ROWS-2);
					int rc = Global.Roll(COLS-2);
					if(charmap[rr,rc] == '.'){
						charmap[rr,rc] = '^';
						done = true;
					}
				}
			}
			List<Tile> hidden = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					switch(charmap[i,j]){
					case '#':
						Tile.Create(TileType.WALL,i,j);
						break;
					case '.':
					case '$':
						Tile.Create(TileType.FLOOR,i,j);
						break;
					case ':':
						Tile.Create(TileType.RUBBLE,i,j);
						break;
					case 'P':
						Tile.Create(TileType.HEALING_POOL,i,j);
						break;
					case '~':
						Tile.Create(TileType.FIRE_GEYSER,i,j);
						break;
					case '^':
					{
						TileType type = TileType.FIRE_GEYSER;
						Tile.Create(type,i,j);
						tile[i,j].name = "floor";
						tile[i,j].the_name = "the floor";
						tile[i,j].a_name = "a floor";
						tile[i,j].symbol = '.';
						tile[i,j].color = Color.White;
						hidden.Add(tile[i,j]);
						int frequency = Global.Roll(21) + 4; //5-25
						int variance = Global.Roll(10) - 1; //0-9
						int variance_amount = (frequency * variance) / 10;
						int number_of_values = variance_amount*2 + 1;
						int minimum_value = frequency - variance_amount;
						if(minimum_value < 5){
							int diff = 5 - minimum_value;
							number_of_values -= diff;
							minimum_value = 5;
						}
						int delay = ((minimum_value - 1) + Global.Roll(number_of_values)) * 100;
						Q.Add(new Event(tile[i,j],delay + 200,EventType.FIRE_GEYSER,(frequency*10)+variance)); //notice the hacky way the value is stored
						Q.Add(new Event(tile[i,j],delay,EventType.FIRE_GEYSER_ERUPTION,2));
						break;
					}
					default:
						Tile.Create(TileType.FLOOR,i,j);
						break;
					}
					tile[i,j].solid_rock = true;
				}
			}
			player.ResetForNewLevel();
			foreach(Tile t in AllTiles()){
				if(t.light_radius > 0){
					t.UpdateRadius(0,t.light_radius);
				}
			}
			List<Tile> goodtiles = AllTiles().Where(t=>t.type == TileType.FLOOR && !t.IsAdjacentTo(TileType.FIRE_GEYSER));
			if(goodtiles.Count > 0){
				Tile t = goodtiles.Random();
				int light = player.light_radius;
				int fire = player.attrs[Forays.AttrType.ON_FIRE];
				player.light_radius = 0;
				player.attrs[Forays.AttrType.ON_FIRE] = 0;
				player.Move(t.row,t.col);
				player.UpdateRadius(0,Math.Max(light,fire),true);
				player.light_radius = light;
				player.attrs[Forays.AttrType.ON_FIRE] = fire;
			}
			else{
				for(bool done=false;!done;){
					int rr = Global.Roll(ROWS-2);
					int rc = Global.Roll(COLS-2);
					bool good = true;
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
						if(t.IsTrap()){
							good = false;
						}
					}
					if(good && tile[rr,rc].passable && actor[rr,rc] == null){
						int light = player.light_radius;
						player.light_radius = 0;
						player.Move(rr,rc);
						player.UpdateRadius(0,light,true);
						done = true;
					}
				}
			}
			foreach(Tile t in AllTiles()){
				if(t.type != TileType.WALL){
					foreach(Tile neighbor in t.TilesAtDistance(1)){
						neighbor.solid_rock = false;
					}
				}
			}
			if(hidden.Count > 0){
				Event e = new Event(hidden,100,EventType.CHECK_FOR_HIDDEN);
				e.tiebreaker = 0;
				Q.Add(e);
			}
			if(boss_already_on_level){
				Tile tile = AllTiles().Where(t=>t.passable && !t.Is(TileType.CHASM) && t.actor() == null).Random();
				Actor a = Actor.Create(ActorType.FIRE_DRAKE,tile.row,tile.col,true,false);
				if(boss_hp > 0){
					a.curhp = boss_hp;
				}
			}
			else{
				Event e = new Event(null,null,(Global.Roll(20)+50)*100,EventType.BOSS_ARRIVE,AttrType.COOLDOWN_1,boss_hp,"");
				e.tiebreaker = 0;
				Q.Add(e);
			}
		}
		public string LevelMessage(){
			if(current_level == 1 || level_types[current_level-2] == level_types[current_level-1]){
				return "";
			}
			List<string> messages = new List<string>();
			switch(level_types[current_level-1]){
			case LevelType.Standard:
				messages.Add("You enter a complex of ancient rooms and hallways. ");
				messages.Add("Well-worn corridors suggest that these rooms are frequently used. ");
				break;
			case LevelType.Cave:
				messages.Add("You enter a large natural cave. ");
				messages.Add("This cavern's rough walls shine with moisture. ");
				messages.Add("A cave opens up before you. A dry, dusty scent lingers in the ancient tunnels. ");
				break;
			case LevelType.Ruined:
				messages.Add("You enter a badly damaged rubble-strewn area of the dungeon. ");
				messages.Add("Broken walls and piles of rubble cover parts of the floor here. ");
				messages.Add("This section of the dungeon has partially collapsed. ");
				break;
			case LevelType.Hive:
				messages.Add("You enter an area made up of small chambers. Some of the walls are covered in a waxy substance. ");
				messages.Add("As you enter the small chambers here, you hear a faint buzzing. It sounds like insects. ");
				break;
			case LevelType.Mine:
				messages.Add("You enter a system of mining tunnels. ");
				messages.Add("Mining tools are scattered on the ground of this level. ");
				messages.Add("You notice half-finished tunnels and mining equipment here. ");
				break;
			case LevelType.Fortress:
				messages.Add("You pass through a broken gate and enter the remnants of a fortress. ");
				messages.Add("This level looks like it was intended to be a stronghold. ");
				break;
			case LevelType.Extravagant:
				messages.Add("This area is decorated with fine tapestries, marble statues, and other luxuries. ");
				messages.Add("Patterned decorative tiles, fine rugs, and beautifully worked stone greet you upon entering this level. ");
				break;
			}
			if(current_level > 1){
				string transition = TransitionMessage(level_types[current_level-2],level_types[current_level-1]);
				if(transition != ""){
					messages.Add(transition);
				}
			}
			return messages.Random();
		}
		public string TransitionMessage(LevelType from,LevelType to){
			switch(from){
			case LevelType.Standard:
				switch(to){
				case LevelType.Cave:
					return "Rooms and corridors disappear from your surroundings as you reach a large natural cavern. ";
				case LevelType.Ruined:
					return "More corridors and rooms appear before you, but many of the walls here are shattered and broken. Rubble covers the floor. ";
				case LevelType.Hive:
					return "The rooms get smaller as you continue. A waxy substance appears on some of the walls. ";
				case LevelType.Mine:
					return "As you continue, you notice that the rooms and corridors here seem only partly finished. ";
				case LevelType.Fortress:
					return "You pass through an undefended gate. This area was obviously intended to be secure against intruders. ";
				case LevelType.Extravagant:
					return "As you continue, you notice that every corridor is extravagantly decorated and every room is magnificently furnished. ";
				}
				break;
			case LevelType.Cave:
				switch(to){
				case LevelType.Standard:
					return "Leaving the cave behind, you again encounter signs of humanoid habitation. ";
				case LevelType.Ruined:
					return "The cave leads you to ruined corridors long abandoned by their creators. ";
				case LevelType.Hive:
					return "The wide-open spaces of the cave disappear, replaced by small chambers that remind you of an insect hive. ";
				case LevelType.Mine:
					return "As you continue, the rough natural edges of the cave are broken up by artificial tunnels. You notice mining tools on the ground. ";
				case LevelType.Fortress:
					return "A smashed set of double doors leads you out of the cave. This area seems to have been well-defended, once. ";
				case LevelType.Extravagant:
					return "You encounter a beautifully crafted door in the cave wall. It leads to corridors richly decorated with tiles and tapestries. ";
				}
				break;
			case LevelType.Ruined:
				switch(to){
				case LevelType.Standard:
					return "This part of the dungeon is in much better condition. Rubble no longer covers the floor. ";
				case LevelType.Cave:
					return "You leave ruined rooms behind and enter natural cave tunnels, never touched by picks. ";
				case LevelType.Hive:
					return "It looks like this section was taken over by insects. The rubble has been cleared and used to build small chambers. ";
				case LevelType.Mine:
					return "Rubble still covers the floor here. However, this area isn't ruined - it's still being mined. ";
				case LevelType.Fortress:
					return "You no longer see crumbling walls in this section, but this fortress has clearly fallen into disuse. ";
				case LevelType.Extravagant:
					return "The rubble disappears, replaced by extravagant decorations. Whatever ruined that part of the dungeon didn't affect this area. ";
				}
				break;
			case LevelType.Hive:
				switch(to){
				case LevelType.Standard:
					return "The rooms around you begin to look more typical, created by picks instead of by thousands of insects. ";
				case LevelType.Cave:
					return "You leave the cramped chambers behind and enter a wider cave. ";
				case LevelType.Ruined:
					return "This area was clearly built by intelligent life, but nature seems to be reclaiming the ruined tunnels. ";
				case LevelType.Mine:
					return "Tools on the ground reveal that the rooms here are being made by humanoids rather than insects. ";
				case LevelType.Fortress:
					return "A wide hole in the wall leads to a fortress, abandoned by its creators. ";
				case LevelType.Extravagant:
					return "Your skin stops crawling as you leave the hives behind and enter a beautifully furnished area. ";
				}
				break;
			case LevelType.Mine:
				switch(to){
				case LevelType.Standard:
					return "You leave the mines behind and return to finished corridors and rooms. ";
				case LevelType.Cave:
					return "The half-finished tunnels disappear as natural cave walls surround you. ";
				case LevelType.Ruined:
					return "This area is collapsing and ruined. It looks much older than the mines you just left. ";
				case LevelType.Hive:
					return "As you continue, signs of humanoid construction vanish and hive walls appear. ";
				case LevelType.Fortress:
					return "You reach a section that is not only complete, but easily defensible. ";
				case LevelType.Extravagant:
					return "As you walk, incomplete tunnels turn into luxurious carpeted hallways. ";
				}
				break;
			case LevelType.Fortress:
				switch(to){
				case LevelType.Standard:
					return "You enter a section outside the main area of the fortress. ";
				case LevelType.Cave:
					return "You leave the fortress behind. The corridors open up into natural caves. ";
				case LevelType.Ruined:
					return "Unlike the fortress, this area has deteriorated immensely. ";
				case LevelType.Hive:
					return "A wide hole in the wall leads to an area filled with small chambers. You are reminded of an insect hive. ";
				case LevelType.Mine:
					return "This section might have been part of the fortress, but pickaxes are still scattered in the unfinished rooms. ";
				case LevelType.Extravagant:
					return "As you continue, the military focus of your surroundings is replaced by rich luxury. ";
				}
				break;
			case LevelType.Extravagant:
				switch(to){
				case LevelType.Standard:
					return "The marvelous luxury vanishes. These rooms look unexciting in comparison. ";
				case LevelType.Cave:
					return "Extravagance is replaced by nature as you enter a large cavern. ";
				case LevelType.Ruined:
					return "The opulence of your surroundings vanishes, replaced by ruined walls and scattered rubble. ";
				case LevelType.Hive:
					return "As you continue, the lavish decorations give way to the waxy walls of an insect hive. ";
				case LevelType.Mine:
					return "You find no comfortable excess of luxury here, just the tools of workers. ";
				case LevelType.Fortress:
					return "You enter what was once a fortress. Your new surroundings trade ornate comfort for spartan efficiency. ";
				}
				break;
			}
			return "";
		}
	}
	public delegate int SortValue<T>(T t);
	public class PriorityQueue<T>{
		public LinkedList<T> list;
		public SortValue<T> priority;
		public PriorityQueue(SortValue<T> sort_value){
			list = new LinkedList<T>();
			priority = sort_value;
		}
		public void Add(T t){
			if(list.First == null){
				list.AddFirst(t);
			}
			else{
				LinkedListNode<T> current = list.First;
				while(true){
					if(priority(t) < priority(current.Value)){
						current = current.Next;
						if(current == null){
							list.AddLast(t);
							break;
						}
					}
					else{
						list.AddBefore(current,t);
						break;
					}
				}
			}
		}
		public T Pop(){
			T result = list.First.Value;
			list.Remove(result);
			return result;
		}
	}
	public struct cell{
		public int row;
		public int col;
		public int value;
		public cell(int row_,int col_,int value_){ row = row_; col = col_; value = value_; }
	}
}

