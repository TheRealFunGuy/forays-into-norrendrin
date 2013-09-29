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
using SchismDungeonGenerator;
using Utilities;
using SchismExtensionMethods;
namespace Forays{
	public enum LevelType{Standard,Cave,Hive,Mine,Fortress,Slime,FogCave,Garden,Crypt};
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
					safetymap[i,j] = U.DijkstraMin;
				}
			}
		}
		public bool BoundsCheck(int r,int c){
			if(r>=0 && r<ROWS && c>=0 && c<COLS){
				return true;
			}
			return false;
		}
		public bool BoundsCheck(pos p){
			if(p.row>=0 && p.row<ROWS && p.col>=0 && p.col<COLS){
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
				int num = R.Roll(2,2) - 1;
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
				if(a != player && (a.HasAttr(AttrType.DANGER_SENSED) || player.CanSee(a))){
					a.attrs[AttrType.DANGER_SENSED] = 1;
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
					if(a[i,j] != U.DijkstraMin){
						a[i,j] = (-(a[i,j]) * 12) / 10;
					}
				}
			}
			foreach(PhysicalObject o in sources){
				a[o.row,o.col] = U.DijkstraMin; //now the player (or other sources) become blocking
			}
			UpdateDijkstraMap(a,get_cost);
			foreach(PhysicalObject o in sources){ //todo experimentally added - add a penalty for tiles adjacent to the player
				foreach(pos p in o.PositionsAtDistance(1)){
					if(a[p] != U.DijkstraMax && a[p] != U.DijkstraMin){
						a[p] += 30;
					}
				}
			}
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
						map[i,j] = U.DijkstraMin;
					}
					else{
						map[i,j] = U.DijkstraMax;
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
					if(map[i,j] == U.DijkstraMax){
						map[i,j] = U.DijkstraMin; //any unreachable areas are marked unpassable
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
					if(map[i,j] != U.DijkstraMin){
						int v = map[i,j];
						bool good = true;
						for(int s=-1;s<=1 && good;++s){
							for(int t=-1;t<=1 && good;++t){
								if(i+s >= 0 && i+s < height && j+t >= 0 && j+t < width){
									if(map[i+s,j+t] < v && map[i+s,j+t] != U.DijkstraMin){
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
				if(player.HasAttr(AttrType.BURNING) || player.HasAttr(AttrType.CATCHING_FIRE)){
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
								if(R.OneIn(40)){
									if(row_displacement[i,j] < 0){
										if(R.OneIn(10)){
											row_displacement[i,j]--;
										}
										else{
											row_displacement[i,j]++;
										}
									}
									else{
										if(row_displacement[i,j] > 0){
											if(R.OneIn(10)){
												row_displacement[i,j]++;
											}
											else{
												row_displacement[i,j]--;
											}
										}
										else{
											if(R.CoinFlip()){
												row_displacement[i,j]++;
											}
											else{
												row_displacement[i,j]--;
											}
										}
									}
									if(col_displacement[i,j] < 0){
										if(R.OneIn(10)){
											col_displacement[i,j]--;
										}
										else{
											col_displacement[i,j]++;
										}
									}
									else{
										if(col_displacement[i,j] > 0){
											if(R.OneIn(10)){
												col_displacement[i,j]++;
											}
											else{
												col_displacement[i,j]--;
											}
										}
										else{
											if(R.CoinFlip()){
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
								if(U.BoundsCheck(i+row_displacement[i,j],j+col_displacement[i,j])){
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
			if(player.HasAttr(AttrType.BURNING) || player.HasAttr(AttrType.CATCHING_FIRE)){
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
					actor[r,c].attrs[AttrType.DANGER_SENSED] = 1;
					ch.c = actor[r,c].symbol;
					ch.color = actor[r,c].color;
					if(actor[r,c] == player && player.HasFeat(FeatType.DANGER_SENSE)
					&& danger_sensed != null && danger_sensed[r,c] && player.LightRadius() == 0
					&& !wiz_lite){
						ch.color = Color.Red;
					}
					else{
						if(actor[r,c] == player && !tile[r,c].IsLit()){
							bool hidden_in_corner = false;
							if(player.HasFeat(FeatType.CORNER_CLIMB) && !player.tile().IsLit()){
								if(SchismExtensionMethods.Extensions.ConsecutiveAdjacent(player.p,x=>tile[x].Is(TileType.WALL,TileType.CRACKED_WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR,TileType.STATUE,TileType.STONE_SLAB)) >= 5){
									hidden_in_corner = true;
								}
							}
							if(player.HasAttr(AttrType.SHADOW_CLOAK) || hidden_in_corner){
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
							List<FeatureType> list = new List<FeatureType>{/*FeatureType.FUNGUS_PRIMED,FeatureType.FUNGUS_ACTIVE,*/FeatureType.TELEPORTAL,FeatureType.INACTIVE_TELEPORTAL,FeatureType.STABLE_TELEPORTAL,/*FeatureType.FUNGUS,*/FeatureType.SLIME};
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
				int rr = R.Roll(ROWS-2);
				int rc = R.Roll(COLS-2);
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
							if(R.OneIn(j)){ //current depth is considered 1 out of 1 times, depth+1 and depth-1 one out of 2 times, etc.
								levels.Add(monster_depth + i);
							}
						}
					}
					level = levels.Random();
				}
				if(monster_depth == 1){ //level 1 monsters are all equal in rarity
					result = (ActorType)(level*7 + R.Between(-4,2));
				}
				else{
					int roll = R.Roll(100);
					if(roll <= 3){ //3% rare
						result = (ActorType)(level*7 + 2);
					}
					else{
						if(roll <= 22){ //19% uncommon (9.5% each)
							result = (ActorType)(level*7 + R.Between(0,1));
						}
						else{ //78% common (19.5% each)
							result = (ActorType)(level*7 + R.Between(-4,-1));
						}
					}
				}
				if(generated_this_level[result] == 0){
					good_result = true;
				}
				else{
					if(R.OneIn(generated_this_level[result]+1)){ // 1 in 2 for the 2nd, 1 in 3 for the 3rd, and so on
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
							if(R.OneIn(j)){ //current depth is considered 1 out of 1 times, depth+1 and depth-1 one out of 2 times, etc.
								levels.Add(monster_depth + i);
							}
						}
					}
					level = levels.Random();
				}
				if(monster_depth == 1){
					//result = (ActorType)(level*5 + R.Roll(5) - 3); //equal probability for the level 1 monsters
					result = (ActorType)(level*7 + R.Between(-4,2));
				}
				else{
					int roll = R.Roll(100);
					if(roll <= 4){ //4% rare
						result = (ActorType)(level*7 + 2);
					}
					else{
						if(roll <= 20){ //16% uncommon (8% each)
							result = (ActorType)(level*7 + R.Between(0,1));
						}
						else{ //80% common (20% each)
							result = (ActorType)(level*7 + R.Between(-4,-1));
						}
					}
				}
				if(current_level <= 2){ //the first 2 levels try to generate a wider variety of types
					if(generated_this_level[result] == 0){ //todo update
						good_result = true;
					}
					else{
						if(R.OneIn(generated_this_level[result]+1)){ // 1 in 2 for the 2nd, 1 in 3 for the 3rd, and so on
							good_result = true;
						}
					}
				}
				else{
					if(generated_this_level[result] < 2){
						good_result = true;
					}
					else{
						if(R.OneIn(generated_this_level[result])){ // 1 in 2 for the 3rd, 1 in 3 for the 4th, and so on
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
					int rr = R.Roll(ROWS-4) + 1;
					int rc = R.Roll(COLS-4) + 1;
					List<Tile> tiles = new List<Tile>();
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(3)){
						if(t.passable || t.type == TileType.DOOR_C){
							tiles.Add(t);
						}
					}
					if(tiles.Count >= 15){
						Actor.tiebreakers.Add(null); //a placeholder for the poltergeist once it manifests
						Event e = new Event(null,tiles,(R.Roll(8)+6)*100,EventType.POLTERGEIST);
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
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
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
			if(type == ActorType.NOXIOUS_WORM){
				//todo:
				//get a dijkstra map with nonwalls as origins. we're looking for distance 2+
				var dijkstra = tile.GetDijkstraMap(x=>false,AllPositions().Where(y=>!tile[y].Is(TileType.WALL)));
				//for each of these, we're gonna check twice, first for horizontal matches.
				//so, now we iterate over the map, ignoring tiles too close to the edge, and checking the valid tiles.
				List<List<Tile>> valid_burrows = new List<List<Tile>>();
				List<Tile> valid = new List<Tile>();
				for(int i=3;i<ROWS-3;++i){
					for(int j=3;j<COLS-3;++j){
						if(dijkstra[i,j] >= 2 && tile[i+1,j].Is(TileType.WALL) && tile[i+2,j].Is(TileType.FLOOR) && tile[i-1,j].Is(TileType.WALL) && tile[i-2,j].Is(TileType.FLOOR)){
							valid.Add(tile[i,j]);
						}
						else{
							if(valid.Count >= 3){
								valid_burrows.Add(new List<Tile>(valid));
							}
							valid.Clear();
						}
					}
					if(valid.Count >= 3){
						valid_burrows.Add(new List<Tile>(valid));
					}
					valid.Clear();
				}
				for(int j=3;j<COLS-3;++j){
					for(int i=3;i<ROWS-3;++i){
						if(dijkstra[i,j] >= 2 && tile[i,j+1].Is(TileType.WALL) && tile[i,j+2].Is(TileType.FLOOR) && tile[i,j-1].Is(TileType.WALL) && tile[i,j-2].Is(TileType.FLOOR)){
							valid.Add(tile[i,j]);
						}
						else{
							if(valid.Count >= 3){
								valid_burrows.Add(new List<Tile>(valid));
							}
							valid.Clear();
						}
					}
					if(valid.Count >= 3){
						valid_burrows.Add(new List<Tile>(valid));
					}
					valid.Clear();
				}
				//if a valid tile has a wall above and below it, and has floors above and below those walls, it's good. increment a counter.
				//if we find 3 or more good tiles in a row, add them all to a list of lists or something.
				//...go over the whole map, then do the same for columns.
				//if there's at least one list in the list of lists, choose one at random, convert its tiles to cracked walls, and put the worm in there somewhere. done.
				if(valid_burrows.Count > 0){
					List<Tile> burrow = valid_burrows.Random();
					foreach(Tile t in burrow){
						t.Toggle(null,TileType.CRACKED_WALL);
					}
					Tile dest = burrow.Random();
					return Actor.Create(type,dest.row,dest.col,true,false);
				}
			}
			int number = 1;
			if(Actor.Prototype(type).HasAttr(AttrType.SMALL_GROUP)){
				number = R.Roll(2)+1;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.MEDIUM_GROUP)){
				number = R.Roll(2)+2;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.LARGE_GROUP)){
				number = R.Roll(2)+4;
			}
			List<Tile> group_tiles = new List<Tile>();
			List<Actor> group = null;
			if(number > 1){
				group = new List<Actor>();
			}
			for(int i=0;i<number;++i){
				if(i == 0){
					for(int j=0;j<9999;++j){
						int rr = R.Roll(ROWS-2);
						int rc = R.Roll(COLS-2);
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
		public PosArray<CellType> GenerateMap(LevelType type){
			PosArray<CellType> result = new PosArray<CellType>(ROWS,COLS);
			Dungeon d = new Dungeon(ROWS,COLS);
			switch(type){
			case LevelType.Standard:
				while(true){
					d.CreateBasicMap();
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.RemoveDeadEndCorridors();
					d.AddDoors(25);
					d.AlterRooms(6,2,1,1,0); //todo: something is broken here; unconnected rooms are possible. does moving RemoveUnconnectedAreas() to the end help?
					//d.CaveWidenRooms(30,30);
					//d.AddPillars(30);
					d.MarkInterestingLocations();
					d.ForEachRectangularRoom((start_r,start_c,end_r,end_c)=>{ //todo: this is the lit room experiment. two things: I don't think this method would work with stalagmites, but they should maybe be features anyway. And, it should be done differently so that rooms with something in them can be lit.
						bool good = true;
						for(int i=start_r;i<=end_r;++i){
							for(int j=start_c;j<=end_c;++j){
								if(!d[i,j].Is(CellType.RoomCorner,CellType.RoomEdge,CellType.RoomInterior,CellType.RoomInteriorCorner)){
									good = false;
								}
							}
						}
						if(good && R.OneIn(10)){
							for(int i=start_r+1;i<end_r;++i){
								for(int j=start_c+1;j<end_c;++j){
									d[i,j] = CellType.RoomFeature4;
								}
							}
						}
						return true;
					});
					if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
				break;
			case LevelType.Cave:
				switch(R.Roll(4)){ //three different algorithms
				case 1:
				case 2:
					while(true){
						d.FillWithRandomWalls(25);
						d.ApplyCellularAutomataXYRule(3);
						d.ConnectDiagonals();
						d.RemoveDeadEndCorridors();
						d.RemoveUnconnectedAreas();
						//todo: addfirepits() removed here. this shouldn't matter as i'll be redoing all the features anyway.
						if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
							d.Clear();
						}
						else{
							for(int i=0;i<ROWS;++i){
								for(int j=0;j<COLS;++j){
									result[i,j] = d.map[i,j];
								}
							}
							return result;
						}
					}
				case 3:
					while(true){
						d.CreateTwistyCave(40);
						d.ApplyCellularAutomataXYRule(4);
						d.ConnectDiagonals();
						d.RemoveDeadEndCorridors();
						d.RemoveUnconnectedAreas(); //todo: more rubble here?
						if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
							d.Clear();
						}
						else{
							for(int i=0;i<ROWS;++i){
								for(int j=0;j<COLS;++j){
									result[i,j] = d.map[i,j];
								}
							}
							return result;
						}
					}
				case 4:
					while(true){
						d.CreateTwistyCave(40);
						List<pos> thin_walls = d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck() && d.map[y].IsFloor()));
						while(thin_walls.Count > 0){
							pos p = thin_walls.Random();
							foreach(int dir in new int[]{8,4}){
								if(d.map[p.PosInDir(dir)] != CellType.Wall && d.map[p.PosInDir(dir.RotateDir(true,4))] != CellType.Wall){
									var dijkstra = d.map.GetDijkstraMap(x=>d[x] == CellType.Wall,new List<pos>{p.PosInDir(dir)}); //todo: this would be better as "get distance"
									if(Math.Abs(dijkstra[p.PosInDir(dir)] - dijkstra[p.PosInDir(dir.RotateDir(true,4))]) > 30){
										d.map[p] = CellType.CorridorIntersection;
										break;
									}
								}
							}
							thin_walls.Remove(p);
						}
						d.ConnectDiagonals();
						d.RemoveUnconnectedAreas();
						d.RemoveDeadEndCorridors();
						if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
							d.Clear();
						}
						else{
							for(int i=0;i<ROWS;++i){
								for(int j=0;j<COLS;++j){
									result[i,j] = d.map[i,j];
								}
							}
							return result;
						}
					}
				}
				break;
			case LevelType.Hive:
				d.RoomHeightMax = 3;
				d.RoomWidthMax = 3;
				while(true){
					int successes = 0;
					int consecutive_failures = 0;
					while(successes < 35){
						if(d.CreateRoom()){
							++successes;
							consecutive_failures = 0;
						}
						else{
							if(consecutive_failures++ >= 40){
								d.Clear();
								successes = 0;
								consecutive_failures = 0;
							}
						}
					}
					d.CaveWidenRooms(100,10);
					List<pos> thin_walls = d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck() && d.map[y].IsFloor()));
					while(!d.IsFullyConnected() && thin_walls.Count > 0){
						pos p = thin_walls.Random();
						d.map[p] = CellType.CorridorIntersection;
						foreach(pos neighbor in p.PositionsWithinDistance(2)){
							thin_walls.Remove(neighbor);
						}
					}
					d.ConnectDiagonals();
					d.RemoveDeadEndCorridors(); //todo: do a random floodfill sort of thing. all walls within 1 or 2 spaces of a floor will become wax walls, and then the wax will extend randomly as in random floodfill, too. eventually there will be actual walls.
					d.RemoveUnconnectedAreas();
					d.MarkInterestingLocations();
					//to find rooms big enough for stuff in the center:
					//var dijkstra = d.map.GetDijkstraMap(x=>d.map[x].IsWall(),d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasAdjacentWhere(y=>!d.map[y].IsWall())));
					if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){ //todo: add 'proper coverage' check here - make sure it stretches across enough of the map.
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.Mine:
			{
				d.RoomHeightMin = 4;
				d.RoomHeightMax = 10;
				d.RoomWidthMin = 4;
				d.RoomWidthMax = 12;
				ParabolaConsoleLib.Screen.Initialize(80,25);
				while(true){
					for(int i=0;i<200;++i){
						d.CreateCorridor();
					}
					for(int i=0;i<300;++i){
						d.CreateRoom();
					}
					d.RemoveUnconnectedAreas(); //aha, add a new rejection step here.
					if(!d.MakeRoomsCavelike()){
						d.Clear();
						continue;
					}
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					//todo: removed fire pits here
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
						//InteractiveGenerator i = new InteractiveGenerator(ROWS,COLS);
						//i.d = d;
						//i.DrawMap();
						//Console.ReadKey(true);
						d.Clear();
					}
					else{
						//todo: removed ruin here. add piles of rubble later.
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			}
			case LevelType.Fortress:
				while(true){
					int H = ROWS;
					int W = COLS;
					for(int i=H/2-1;i<H/2+1;++i){
						for(int j=1;j<W-1;++j){
							if(j==1 || j==W-2){
								d.map[i,j] = CellType.RoomCorner;
							}
							else{
								d.map[i,j] = CellType.RoomEdge;
							}
						}
					}
					for(int i=0;i<700;++i){
						if(R.OneIn(5)){
							d.CreateCorridor();
						}
						else{
							d.CreateRoom();
						}
					}
					bool reflect_features = R.PercentChance(80);
					if(reflect_features){
						d.AddDoors(25);
						d.AddPillars(30);
					}
					d.Reflect(true,false);
					d.ConnectDiagonals();
					d.RemoveDeadEndCorridors();
					d.RemoveUnconnectedAreas();
					if(!reflect_features){
						d.AddDoors(25);
						d.AddPillars(30);
					}
					bool door_right = false;
					bool door_left = false;
					int rightmost_door = 0;
					int leftmost_door = 999;
					/*for(int j=0;j<22;++j){
						if(SchismExtensionMethods.Extensions.IsCorridorType(d[0,0])){
						}
						if(ConvertedChar(map[H/2-2,j]) == '.' || ConvertedChar(map[H/2-2,j]) == '+'){
							door_left = true;
							if(leftmost_door == 999){
								leftmost_door = j;
							}
						}
						if(ConvertedChar(map[H/2-2,W-1-j]) == '.' || ConvertedChar(map[H/2-2,W-1-j]) == '+'){
							door_right = true;
							if(rightmost_door == 0){
								rightmost_door = W-1-j;
							}
						}
					}
					if(!door_left || !door_right){
						Clear();
						continue;
					}
					for(int j=1;j<leftmost_door-6;++j){
						map[H/2-1,j] = '#';
						map[H/2,j] = '#';
					}
					for(int j=W-2;j>rightmost_door+6;--j){
						map[H/2-1,j] = '#';
						map[H/2,j] = '#';
					}
					for(int j=1;j<W-1;++j){
						if(ConvertedChar(map[H/2-1,j]) == '.'){
							map[H/2-1,j] = '&';
							map[H/2,j] = '&';
							break;
						}
						else{
							if(ConvertedChar(map[H/2-1,j]) == '&'){
								break;
							}
						}
					}
					for(int j=W-2;j>0;--j){
						if(ConvertedChar(map[H/2-1,j]) == '.'){
							map[H/2-1,j] = '&';
							map[H/2,j] = '&';
							break;
						}
						else{
							if(ConvertedChar(map[H/2-1,j]) == '&'){
								break;
							}
						}
					}*/
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 420 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.Slime:
				while(true){
					d.CreateBasicMap();
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.AddDoors(25);
					d.CaveWidenRooms(30,30);
					d.RemoveDeadEndCorridors();
					d.AddPillars(30);
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.FogCave:
				d.RoomHeightMax = 3;
				d.RoomWidthMax = 3;
				while(true){
					int successes = 0;
					int consecutive_failures = 0;
					while(successes < 13){
						if(d.CreateRoom()){
							++successes;
							consecutive_failures = 0;
						}
						else{
							if(consecutive_failures++ >= 50){
								d.Clear();
								successes = 0;
								consecutive_failures = 0;
							}
						}
					}
					d.CaveWidenRooms(100,50);
					List<pos> thin_walls = d.map.AllPositions().Where(x=>d.map[x].IsWall() && x.HasOppositePairWhere(true,y=>y.BoundsCheck() && d.map[y].IsFloor()));
					while(!d.IsFullyConnected() && thin_walls.Count > 0){
						pos p = thin_walls.Random();
						d.map[p] = CellType.CorridorIntersection;
						foreach(pos neighbor in p.PositionsWithinDistance(1)){
							thin_walls.Remove(neighbor);
						}
					}
					d.ConnectDiagonals();
					d.RemoveDeadEndCorridors();
					d.RemoveUnconnectedAreas(); //todo: probably needs some rubble/pillars/stalagmites somewhere.
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){ //todo: add 'proper coverage' check here - make sure it stretches across enough of the map.
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.Garden:
				while(true){
					d.CreateBasicMap();
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.AddDoors(25);
					d.CaveWidenRooms(30,30);
					d.RemoveDeadEndCorridors();
					d.AddPillars(30);
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 320 || d.HasLargeUnusedSpaces(300)){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			case LevelType.Crypt:
				while(true){
					while(!d.CreateRoom(ROWS/2,R.Roll(COLS/8 - 1) + COLS/8 - 1)){} //left half
					while(!d.CreateRoom(ROWS/2,R.Roll(COLS/8 - 1) + (COLS*6 / 8) - 1)){} //right half
					d.CaveWidenRooms(100,150);
					var dijkstra = d.map.GetDijkstraMap(x=>d.map[x] == CellType.Wall,x=>d.map[x] == CellType.Wall);
					int distance_from_walls = 3;
					List<pos> central_room = d.map.PositionsWhere(x=>dijkstra[x] > distance_from_walls);
					int required_consecutive = 3;
					for(int i=0;i<ROWS;++i){ //first, check each row...
						for(int j=0;j<COLS;++j){
							List<pos> this_row = new List<pos>();
							while(j < COLS && dijkstra[i,j] > distance_from_walls){
								this_row.Add(new pos(i,j));
								++j;
							}
							if(this_row.Count < required_consecutive){
								foreach(pos p in this_row){
									central_room.Remove(p);
								}
							}
						}
					}
					for(int j=0;j<COLS;++j){ //...then each column
						for(int i=0;i<ROWS;++i){
							List<pos> this_col = new List<pos>();
							while(i < ROWS && dijkstra[i,j] > distance_from_walls){
								this_col.Add(new pos(i,j));
								++i;
							}
							if(this_col.Count < required_consecutive){
								foreach(pos p in this_col){
									central_room.Remove(p);
								}
							}
						}
					}
					central_room = d.map.GetFloodFillPositions(central_room.Where(x=>x.PositionsWithinDistance(1).All(y=>central_room.Contains(y))),false,x=>central_room.Contains(x));
					List<pos> walls = new List<pos>();
					foreach(pos p in central_room){
						d.map[p] = CellType.InterestingLocation;
						foreach(pos neighbor in p.PositionsAtDistance(1)){
							if(!central_room.Contains(neighbor)){
								d.map[neighbor] = CellType.Wall;
								walls.Add(neighbor);
							}
						}
					}
					while(true){
						List<pos> potential_doors = new List<pos>();
						foreach(pos p in walls){
							foreach(int dir in U.FourDirections){
								if(d.map[p.PosInDir(dir)] == CellType.InterestingLocation && d.map[p.PosInDir(dir.RotateDir(true,4))].IsRoomType() && d.map[p.PosInDir(dir.RotateDir(true,4))] != CellType.InterestingLocation){
									potential_doors.Add(p);
									break;
								}
							}
						}
						if(potential_doors.Count > 0){
							pos p = potential_doors.Random();
							d.map[p] = CellType.Door;
							List<pos> room = d.map.GetFloodFillPositions(p,true,x=>d.map[x] == CellType.InterestingLocation);
							foreach(pos p2 in room){
								d.map[p2] = CellType.RoomInterior;
							}
						}
						else{
							break;
						}
					}
					dijkstra = d.map.GetDijkstraMap(x=>d.map[x] == CellType.Wall,x=>d.map[x] == CellType.Wall);
					int num_chests = 0;
					d.ForEachRoom(list=>{
						if(central_room.Contains(list[0])){
							if(num_chests++ < 2){
								d[list.Random()] = CellType.Chest;
							}
							return true;
						}
						List<pos> room = list.Where(x=>dijkstra[x] > 1);
						int start_r = room.WhereLeast(x=>x.row)[0].row;
						int end_r = room.WhereGreatest(x=>x.row)[0].row;
						int start_c = room.WhereLeast(x=>x.col)[0].col;
						int end_c = room.WhereGreatest(x=>x.col)[0].col;
						List<List<pos>> offsets = new List<List<pos>>();
						for(int i=0;i<4;++i){
							offsets.Add(new List<pos>());
						}
						for(int i=start_r;i<=end_r;i+=2){
							for(int j=start_c;j<=end_c;j+=2){
								if(room.Contains(new pos(i,j))){
									offsets[0].Add(new pos(i,j));
								}
								if(i+1 <= end_r && room.Contains(new pos(i+1,j))){
									offsets[1].Add(new pos(i+1,j));
								}
								if(j+1 <= end_c && room.Contains(new pos(i,j+1))){
									offsets[2].Add(new pos(i,j+1));
								}
								if(i+1 <= end_r && j+1 <= end_c && room.Contains(new pos(i+1,j+1))){
									offsets[3].Add(new pos(i+1,j+1));
								}
							}
						}
						List<pos> tombstones = offsets.WhereGreatest(x=>x.Count).Random();
						foreach(pos p in tombstones){
							d.map[p] = CellType.Tombstone;
						}
						return true;
					});
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(d[i,j] == CellType.Door){
								pos p = new pos(i,j);
								List<pos> potential_statues = p.PositionsAtDistance(1).Where(x=>!d[x].IsWall() && !central_room.Contains(x) && p.DirectionOf(x) % 2 != 0 && !x.PositionsAtDistance(1).Any(y=>d[y].Is(CellType.Tombstone)));
								if(potential_statues.Count == 2){
									d[potential_statues[0]] = CellType.Statue;
									d[potential_statues[1]] = CellType.Statue;
								}
							}
						}
					}
					List<pos> room_one = null;
					List<pos> room_two = null;
					for(int j=0;j<COLS && room_one == null;++j){
						for(int i=0;i<ROWS;++i){
							if(d[i,j] != CellType.Wall){
								room_one = d.map.GetFloodFillPositions(new pos(i,j),false,x=>!d[x].IsWall());
								break;
							}
						}
					}
					for(int j=COLS-1;j>=0 && room_two == null;--j){
						for(int i=0;i<ROWS;++i){
							if(d[i,j] != CellType.Wall){
								room_two = d.map.GetFloodFillPositions(new pos(i,j),false,x=>!d[x].IsWall());
								break;
							}
						}
					}
					if(room_one.WhereGreatest(x=>x.col).Random().DistanceFrom(room_two.WhereLeast(x=>x.col).Random()) < 12){
						d.Clear();
						continue;
					}
					Dungeon d2 = new Dungeon(ROWS,COLS);
					int tries = 0;
					while(tries < 10){
						d2.CreateBasicMap();
						d2.ConnectDiagonals();
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(d[i,j] != CellType.Wall){
									pos p = new pos(i,j);
									foreach(pos neighbor in p.PositionsAtDistance(1)){
										d2[neighbor] = CellType.Wall;
									}
								}
							}
						}
						d2.RemoveUnconnectedAreas();
						//ParabolaConsoleLib.Screen.Initialize(25,80); InteractiveGenerator ig = new InteractiveGenerator(ROWS,COLS); ig.d = d2; ig.DrawMap(); Console.ReadKey(true);
						List<pos> room_one_walls = new List<pos>();
						List<pos> room_two_walls = new List<pos>();
						for(int i=0;i<ROWS;++i){
							for(int j=COLS-1;j>=0;--j){
								pos p = new pos(i,j);
								if(room_one.Contains(p)){
									room_one_walls.Add(p);
									break;
								}
							}
							for(int j=0;j<COLS;++j){
								pos p = new pos(i,j);
								if(room_two.Contains(p)){
									room_two_walls.Add(p);
									break;
								}
							}
						}
						List<pos> room_one_valid_connections = new List<pos>();
						List<pos> room_two_valid_connections = new List<pos>();
						foreach(pos p in room_one_walls){
							pos next = p.PosInDir(6);
							while(BoundsCheck(next) && p.DistanceFrom(next) < 7){
								if(d2[next] != CellType.Wall){
									room_one_valid_connections.Add(p.PosInDir(6));
									break;
								}
								next = next.PosInDir(6);
							}
						}
						foreach(pos p in room_two_walls){
							pos next = p.PosInDir(4);
							while(BoundsCheck(next) && p.DistanceFrom(next) < 7){
								if(d2[next] != CellType.Wall){
									room_two_valid_connections.Add(p.PosInDir(4));
									break;
								}
								next = next.PosInDir(4);
							}
						}
						if(room_one_valid_connections.Count > 0 && room_two_valid_connections.Count > 0){
							pos one = room_one_valid_connections.Random();
							while(true){
								if(d2[one] == CellType.Wall){
									d2[one] = CellType.CorridorHorizontal;
								}
								else{
									break;
								}
								one = one.PosInDir(6);
							}
							pos two = room_two_valid_connections.Random();
							while(true){
								if(d2[two] == CellType.Wall){
									d2[two] = CellType.CorridorHorizontal;
								}
								else{
									break;
								}
								two = two.PosInDir(4);
							}
							break;
						}
						else{
							d2.Clear();
						}
						++tries;
					}
					if(tries == 10){
						d.Clear();
						continue;
					}
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(d2[i,j] != CellType.Wall){
								d[i,j] = d2[i,j];
							}
						}
					}
					//ParabolaConsoleLib.Screen.Initialize(25,80); InteractiveGenerator ig = new InteractiveGenerator(ROWS,COLS); ig.d = d; ig.DrawMap(); Console.ReadKey(true);
					//d.CaveWidenRooms(100,20);
					//d.MakeCavesMoreRectangular(4);
					//d.SharpenCorners(); //todo: need "add cross room" and "add almost rectangular room" in schism.
					//d.RemoveDeadEndCorridors();
					//d.MakeCavesMoreRectangular(1 + num++ / 10);
					//d.Clear();
					//continue;
					d.ConnectDiagonals();
					d.RemoveUnconnectedAreas();
					d.RemoveDeadEndCorridors();
					d.MarkInterestingLocations();
					if(d.NumberOfFloors() < 300){
						d.Clear();
					}
					else{
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								result[i,j] = d.map[i,j];
							}
						}
						return result;
					}
				}
			}
			return null;
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
			Q.ResetForNewLevel();
			Actor.tiebreakers = new List<Actor>{player};
			PosArray<CellType> map = GenerateMap(level_types[current_level-1]);
			List<pos> interesting_tiles = new List<pos>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(map[i,j] == CellType.InterestingLocation){
						interesting_tiles.Add(new pos(i,j));
					}
				}
			}
			int attempts = 0;
			if(current_level%2 == 1){
				List<CellType> shrines = new List<CellType>{CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5};
				while(shrines.Count > 0){
					attempts = 0;
					for(bool done=false;!done;++attempts){
						int rr = R.Roll(ROWS-4) + 1;
						int rc = R.Roll(COLS-4) + 1;
						if(interesting_tiles.Count > 0 && (shrines.Count > 1 || attempts > 1000)){
							pos p = interesting_tiles.Random();
							rr = p.row;
							rc = p.col;
							map[rr,rc] = CellType.RoomInterior;
						}
						pos temp = new pos(rr,rc);
						if(shrines.Count > 1){
							if(map[rr,rc].IsFloor()){ //todo: watch for weirdness here as a result of switching to celltype
								if(attempts > 1000){
									bool good = true;
									foreach(pos p in temp.PositionsWithinDistance(4)){
										CellType ch = map[p.row,p.col];
										if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
											good = false;
										}
									}
									if(good){
										List<pos> dist2 = new List<pos>();
										foreach(pos p2 in temp.PositionsAtDistance(2)){
											if(map[p2.row,p2.col].IsFloor()){
												dist2.Add(p2);
											}
										}
										if(dist2.Count > 0){
											map[rr,rc] = shrines.RemoveRandom();
											pos p2 = dist2.Random();
											map[p2.row,p2.col] = shrines.RemoveRandom();
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
									if(!map[p.row,p.col].IsFloor()){
										floors = false;
									}
								}
								foreach(pos p in temp.PositionsWithinDistance(3)){
									CellType ch = map[p.row,p.col];
									if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
										floors = false;
									}
								}
								if(floors){
									if(R.CoinFlip()){
										map[rr-1,rc] = shrines.RemoveRandom();
										map[rr+1,rc] = shrines.RemoveRandom();
									}
									else{
										map[rr,rc-1] = shrines.RemoveRandom();
										map[rr,rc+1] = shrines.RemoveRandom();
									}
									CellType center = CellType.Wall;
									switch(R.Roll(4)){
									case 1:
										center = CellType.Pillar;
										break;
									case 2:
										center = CellType.Statue;
										break;
									case 3:
										center = CellType.FirePit;
										break;
									case 4:
										center = CellType.ShallowWater;
										break;
									}
									map[rr,rc] = center;
									interesting_tiles.Remove(temp);
									done = true;
									break;
								}
							}
						}
						else{
							if(map[rr,rc].IsFloor()){
								bool good = true;
								foreach(pos p in temp.PositionsWithinDistance(2)){
									CellType ch = map[p.row,p.col];
									if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
										good = false;
									}
								}
								if(good){
									if(attempts > 1000){
										map[rr,rc] = shrines.RemoveRandom();
										interesting_tiles.Remove(temp);
										done = true;
										break;
									}
									else{
										bool floors = true;
										foreach(pos p in temp.PositionsAtDistance(1)){
											if(!map[p.row,p.col].IsFloor()){
												floors = false;
											}
										}
										if(floors){
											map[rr,rc] = shrines.RemoveRandom();
											interesting_tiles.Remove(temp);
											done = true;
											break;
										}
									}
								}
							}
							if(map[rr,rc].IsWall()){
								if(!map[rr+1,rc].IsFloor() && !map[rr-1,rc].IsFloor() && !map[rr,rc-1].IsFloor() && !map[rr,rc+1].IsFloor()){
									continue; //no floors? retry.
								}
								bool no_good = false;
								foreach(pos p in temp.PositionsAtDistance(2)){
									CellType ch = map[p.row,p.col];
									if(ch.Is(CellType.SpecialFeature1,CellType.SpecialFeature2,CellType.SpecialFeature3,CellType.SpecialFeature4,CellType.SpecialFeature5)){
										no_good = true;
									}
								}
								if(no_good){
									continue;
								}
								int walls = 0;
								foreach(pos p in temp.PositionsAtDistance(1)){
									if(map[p.row,p.col].IsWall()){
										++walls;
									}
								}
								if(walls >= 5){
									int successive_walls = 0;
									CellType[] rotated = new CellType[8];
									for(int i=0;i<8;++i){
										pos temp2;
										temp2 = temp.PosInDir(8.RotateDir(true,i));
										rotated[i] = map[temp2.row,temp2.col];
									}
									for(int i=0;i<15;++i){
										if(rotated[i%8].IsWall()){
											++successive_walls;
										}
										else{
											successive_walls = 0;
										}
										if(successive_walls == 5){
											done = true;
											map[rr,rc] = shrines.RemoveRandom();
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
			int num_chests = R.Roll(2);
			if(R.OneIn(50)){
				num_chests = 3;
			}
			if(level_types[current_level-1] == LevelType.Crypt){
				num_chests -= map.PositionsWhere(x=>map[x] == CellType.Chest).Count;
			}
			for(int i=0;i<num_chests;++i){
				int tries = 0;
				for(bool done=false;!done;++tries){
					int rr = R.Roll(ROWS-4) + 1;
					int rc = R.Roll(COLS-4) + 1;
					if(interesting_tiles.Count > 0){
						pos p = interesting_tiles.RemoveRandom();
						rr = p.row;
						rc = p.col;
						map[rr,rc] = CellType.RoomInterior;
					}
					if(map[rr,rc].IsFloor()){
						bool floors = true;
						pos temp = new pos(rr,rc);
						foreach(pos p in temp.PositionsAtDistance(1)){
							if(!map[p.row,p.col].IsFloor()){
								floors = false;
							}
						}
						if(floors || tries > 1000){ //after 1000 tries, place it anywhere
							map[rr,rc] = CellType.Chest;
							done = true;
						}
					}
				}
			}
			attempts = 0;
			for(bool done=false;!done;++attempts){
				int rr = R.Roll(ROWS-4) + 1;
				int rc = R.Roll(COLS-4) + 1;
				if(interesting_tiles.Count > 0){
					pos p = interesting_tiles.RemoveRandom();
					rr = p.row;
					rc = p.col;
					map[rr,rc] = CellType.RoomInterior;
				}
				if(map[rr,rc].IsFloor()){
					bool floors = true;
					pos temp = new pos(rr,rc);
					foreach(pos p in temp.PositionsAtDistance(1)){
						if(!map[p.row,p.col].IsFloor()){
							floors = false;
						}
					}
					if(floors || attempts > 1000){
						map[rr,rc] = CellType.Stairs;
						done = true;
					}
				}
			}
			if(R.OneIn(30)){
				attempts = 0;
				for(bool done=false;!done;++attempts){
					int rr = R.Roll(ROWS-4) + 1;
					int rc = R.Roll(COLS-4) + 1;
					if(interesting_tiles.Count > 0){
						pos p = interesting_tiles.RemoveRandom();
						rr = p.row;
						rc = p.col;
						map[rr,rc] = CellType.RoomInterior;
					}
					if(map[rr,rc].IsFloor()){
						bool floors = true;
						pos temp = new pos(rr,rc);
						foreach(pos p in temp.PositionsAtDistance(1)){
							if(!map[p.row,p.col].IsFloor()){
								floors = false;
							}
						}
						if(floors || attempts > 1000){
							map[rr,rc] = CellType.Pool;
							done = true;
						}
					}
				}
			}
			if(R.OneIn(20) && current_level > 2){
				LevelType lt = level_types[current_level-1];
				if(lt == LevelType.Standard || lt == LevelType.Cave || lt == LevelType.Mine){
					switch(R.Roll(4)){
					case 1:
					case 2:
						for(attempts=0;attempts<100;++attempts){
							int rr = R.Roll(ROWS-4) + 1;
							int rc = R.Roll(COLS-4) + 1;
							if(map[rr,rc].IsFloor()){
								map[rr,rc] = CellType.Fungus;
								break;
							}
						}
						break;
					case 3:
						for(attempts=0;attempts<100;++attempts){
							int rr = R.Roll(ROWS-4) + 1;
							int rc = R.Roll(COLS-4) + 1;
							pos p = new pos(rr,rc);
							if(map[rr,rc].IsFloor()){
								List<pos> other_pos = new List<pos>();
								foreach(pos nearby in p.PositionsWithinDistance(5,true,false)){
									if(map[nearby].IsFloor()){
										other_pos.Add(nearby);
									}
								}
								if(other_pos.Count > 0){
									map[rr,rc] = CellType.Fungus;
									pos other = other_pos.Random();
									map[other] = CellType.Fungus;
									break;
								}
							}
						}
						break;
					case 4:
					{
						int num = R.Roll(5)+2;
						for(int i=0;i<num;++i){
							for(attempts=0;attempts<100;++attempts){
								int rr = R.Roll(ROWS-4) + 1;
								int rc = R.Roll(COLS-4) + 1;
								if(map[rr,rc].IsFloor()){
									map[rr,rc] = CellType.Fungus;
									break;
								}
							}
						}
						break;
					}
					}
				}
			}
			int num_traps = R.Roll(2,3);
			for(int i=0;i<num_traps;++i){
				int tries = 0;
				for(bool done=false;!done && tries < 100;++tries){
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
					if(map[rr,rc].IsFloor()){
						map[rr,rc] = CellType.Trap;
						done = true;
					}
				}
			}
			int percentage_of_traps_to_become_vents = 1;
			if(level_types[current_level-1] == LevelType.Cave || level_types[current_level-1] == LevelType.Hive){ //todo: reverse this - don't actually make traps nonexistent on these levels.
				percentage_of_traps_to_become_vents = 20;
			}
			if(level_types[current_level-1] == LevelType.Mine){
				percentage_of_traps_to_become_vents = 5;
			}
			if(level_types[current_level-1] == LevelType.Fortress){ //todo: removed extravagant level check here
				percentage_of_traps_to_become_vents = 0;
			}
			List<Tile> hidden = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					//Screen.WriteMapChar(i,j,map[i,j]);
					switch(map[i,j]){
					case CellType.Wall:
					case CellType.Pillar:
						Tile.Create(TileType.WALL,i,j);
						break;
					case CellType.Door:
						Tile.Create(TileType.DOOR_C,i,j);
						break;
					case CellType.Stairs:
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
					case CellType.Statue:
						Tile.Create(TileType.STATUE,i,j);
						break;
					case CellType.Rubble:
						Tile.Create(TileType.RUBBLE,i,j);
						break;
					case CellType.FirePit:
						Tile.Create(TileType.FIREPIT,i,j);
						break;
					case CellType.Pool:
						Tile.Create(TileType.HEALING_POOL,i,j);
						break;
					case CellType.Fungus:
						Tile.Create(TileType.BLAST_FUNGUS,i,j);
						//tile[i,j].features.Add(FeatureType.FUNGUS);
						break;
					case CellType.Chest:
						Tile.Create(TileType.CHEST,i,j);
						break;
					case CellType.Geyser:
						Tile.Create(TileType.FIRE_GEYSER,i,j);
						break;
					case CellType.Trap:
					{
						TileType type = Tile.RandomTrap();
						if(R.Roll(100) <= percentage_of_traps_to_become_vents){
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
							int frequency = R.Roll(21) + 4; //5-25
							int variance = R.Roll(10) - 1; //0-9
							int variance_amount = (frequency * variance) / 10;
							int number_of_values = variance_amount*2 + 1;
							int minimum_value = frequency - variance_amount;
							if(minimum_value < 5){
								int diff = 5 - minimum_value;
								number_of_values -= diff;
								minimum_value = 5;
							}
							int delay = ((minimum_value - 1) + R.Roll(number_of_values)) * 100;
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
					case CellType.Tombstone:
					{
						Tile t = Tile.Create(TileType.TOMBSTONE,i,j);
						if(R.OneIn(8)){
							Q.Add(new Event(null,new List<Tile>{t},100,EventType.TOMBSTONE_GHOST));
						}
						break;
					}
					case CellType.HiddenDoor:
						Tile.Create(TileType.HIDDEN_DOOR,i,j);
						hidden.Add(tile[i,j]);
						break;
					case CellType.SpecialFeature1:
						Tile.Create(Forays.TileType.COMBAT_SHRINE,i,j);
						break;
					case CellType.SpecialFeature2:
						Tile.Create(Forays.TileType.DEFENSE_SHRINE,i,j);
						break;
					case CellType.SpecialFeature3:
						Tile.Create(Forays.TileType.MAGIC_SHRINE,i,j);
						break;
					case CellType.SpecialFeature4:
						Tile.Create(Forays.TileType.SPIRIT_SHRINE,i,j);
						break;
					case CellType.SpecialFeature5:
						Tile.Create(Forays.TileType.STEALTH_SHRINE,i,j);
						break;
					case CellType.DeepWater:
					case CellType.ShallowWater:
						Tile.Create(TileType.WATER,i,j);
						break;
					case CellType.RoomFeature4:
						Tile.Create(TileType.FLOOR,i,j); //todo: used in the lit room experiment. this probably needs fixing!
						tile[i,j].light_radius = 1;
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
			switch(R.Roll(5)){
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
			for(int i=R.Roll(2,2)+3;i>0;--i){
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
									entrancer.attrs[AttrType.WANDERING]++;
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
											case ActorType.INFESTED_MASS_TODO_NAME: //todo update list
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
								if(a.AlwaysWanders() || (R.CoinFlip() && a.CanWanderAtLevelGen())){
									a.attrs[AttrType.WANDERING]++;
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
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
					bool good = true;
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
						if(t.IsTrap()){
							good = false;
						}
					}
					if(good && tile[rr,rc].passable && actor[rr,rc] == null){
						int light = player.light_radius;
						int fire = player.attrs[AttrType.BURNING];
						player.light_radius = 0;
						player.attrs[AttrType.BURNING] = 0;
						player.Move(rr,rc);
						player.UpdateRadius(0,Math.Max(light,fire),true);
						player.light_radius = light;
						player.attrs[AttrType.BURNING] = fire;
						done = true;
					}
				}
			}
			if(R.CoinFlip()){ //is 50% the best rate for hidden areas? it seems to be working well so far.
				bool done = false;
				for(int tries=0;!done && tries<500;++tries){
					int rr = R.Roll(ROWS-4) + 1;
					int rc = R.Roll(COLS-4) + 1;
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
								if(t.TileInDirection(i.RotateDir(false,2)).type != TileType.WALL){
									good_dir = false;
								}
								if(t.TileInDirection(i.RotateDir(true,2)).type != TileType.WALL){
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
							int trap_roll = R.Roll(7);
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
							if(R.OneIn(4)){
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
										if(R.Roll(3) >= 2){
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
											Tile neighbor = t.TileInDirection(i.RotateDir(false,2));
											if(neighbor.TileInDirection(i.RotateDir(false,1)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(false,2)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(false,3)).type == TileType.WALL){
												tt = TileType.FLOOR;
												if(R.Roll(3) >= 2){
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
											neighbor = t.TileInDirection(i.RotateDir(true,2));
											if(neighbor.TileInDirection(i.RotateDir(true,1)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(true,2)).type == TileType.WALL
											   && neighbor.TileInDirection(i.RotateDir(true,3)).type == TileType.WALL){
												tt = TileType.FLOOR;
												if(R.Roll(3) >= 2){
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
										if(R.CoinFlip()){
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
								t = t.TileInDirection(i.RotateDir(true,4));
								if(stone_slabs){
									t.TransformTo(TileType.STONE_SLAB);
									Q.Add(new Event(t,new List<Tile>{t.TileInDirection(i.RotateDir(true,4))},100,EventType.STONE_SLAB));
								}
								else{
									t.TransformTo(TileType.HIDDEN_DOOR);
									hidden.AddUnique(t);
								}
								t = t.TileInDirection(i.RotateDir(true,4));
								if(R.CoinFlip()){
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
								tile[rr,rc].TileInDirection(dirs[0].RotateDir(true,4)).TransformTo(TileType.CHEST);
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
			char[,] map = dungeon.GenerateCave();
			int num_traps = R.Roll(1,3);
			for(int i=0;i<num_traps;++i){
				int tries = 0;
				for(bool done=false;!done && tries < 100;++tries){
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
					if(map[rr,rc] == '.'){
						map[rr,rc] = '^';
						done = true;
					}
				}
			}
			List<Tile> hidden = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					switch(map[i,j]){
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
						int frequency = R.Roll(21) + 4; //5-25
						int variance = R.Roll(10) - 1; //0-9
						int variance_amount = (frequency * variance) / 10;
						int number_of_values = variance_amount*2 + 1;
						int minimum_value = frequency - variance_amount;
						if(minimum_value < 5){
							int diff = 5 - minimum_value;
							number_of_values -= diff;
							minimum_value = 5;
						}
						int delay = ((minimum_value - 1) + R.Roll(number_of_values)) * 100;
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
				int fire = player.attrs[AttrType.BURNING];
				player.light_radius = 0;
				player.attrs[AttrType.BURNING] = 0;
				player.Move(t.row,t.col);
				player.UpdateRadius(0,Math.Max(light,fire),true);
				player.light_radius = light;
				player.attrs[AttrType.BURNING] = fire;
			}
			else{
				for(bool done=false;!done;){
					int rr = R.Roll(ROWS-2);
					int rc = R.Roll(COLS-2);
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
				Event e = new Event(null,null,(R.Roll(20)+50)*100,EventType.BOSS_ARRIVE,AttrType.COOLDOWN_1,boss_hp,"");
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
			/*case LevelType.Ruined:
				messages.Add("You enter a badly damaged rubble-strewn area of the dungeon. ");
				messages.Add("Broken walls and piles of rubble cover parts of the floor here. ");
				messages.Add("This section of the dungeon has partially collapsed. ");
				break;*/
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
			/*case LevelType.Extravagant:
				messages.Add("This area is decorated with fine tapestries, marble statues, and other luxuries. ");
				messages.Add("Patterned decorative tiles, fine rugs, and beautifully worked stone greet you upon entering this level. ");
				break;*/
			default:
				messages.Add("TODO: New level types don't have messages yet. ");
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
				/*case LevelType.Ruined:
					return "More corridors and rooms appear before you, but many of the walls here are shattered and broken. Rubble covers the floor. ";
				case LevelType.Extravagant:
					return "As you continue, you notice that every corridor is extravagantly decorated and every room is magnificently furnished. ";*/
				case LevelType.Hive:
					return "The rooms get smaller as you continue. A waxy substance appears on some of the walls. ";
				case LevelType.Mine:
					return "As you continue, you notice that the rooms and corridors here seem only partly finished. ";
				case LevelType.Fortress:
					return "You pass through an undefended gate. This area was obviously intended to be secure against intruders. ";
				}
				break;
			case LevelType.Cave:
				switch(to){
				case LevelType.Standard:
					return "Leaving the cave behind, you again encounter signs of humanoid habitation. ";
				/*case LevelType.Ruined:
					return "The cave leads you to ruined corridors long abandoned by their creators. ";
				case LevelType.Extravagant:
					return "You encounter a beautifully crafted door in the cave wall. It leads to corridors richly decorated with tiles and tapestries. ";*/
				case LevelType.Hive:
					return "The wide-open spaces of the cave disappear, replaced by small chambers that remind you of an insect hive. ";
				case LevelType.Mine:
					return "As you continue, the rough natural edges of the cave are broken up by artificial tunnels. You notice mining tools on the ground. ";
				case LevelType.Fortress:
					return "A smashed set of double doors leads you out of the cave. This area seems to have been well-defended, once. ";
				}
				break;
			/*case LevelType.Ruined:
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
				break;*/
			case LevelType.Hive:
				switch(to){
				case LevelType.Standard:
					return "The rooms around you begin to look more typical, created by picks instead of by thousands of insects. ";
				case LevelType.Cave:
					return "You leave the cramped chambers behind and enter a wider cave. ";
				/*case LevelType.Ruined:
					return "This area was clearly built by intelligent life, but nature seems to be reclaiming the ruined tunnels. ";
				case LevelType.Extravagant:
					return "Your skin stops crawling as you leave the hives behind and enter a beautifully furnished area. ";*/
				case LevelType.Mine:
					return "Tools on the ground reveal that the rooms here are being made by humanoids rather than insects. ";
				case LevelType.Fortress:
					return "A wide hole in the wall leads to a fortress, abandoned by its creators. ";
				}
				break;
			case LevelType.Mine:
				switch(to){
				case LevelType.Standard:
					return "You leave the mines behind and return to finished corridors and rooms. ";
				case LevelType.Cave:
					return "The half-finished tunnels disappear as natural cave walls surround you. ";
				/*case LevelType.Ruined:
					return "This area is collapsing and ruined. It looks much older than the mines you just left. ";
				case LevelType.Extravagant:
					return "As you walk, incomplete tunnels turn into luxurious carpeted hallways. ";*/
				case LevelType.Hive:
					return "As you continue, signs of humanoid construction vanish and hive walls appear. ";
				case LevelType.Fortress:
					return "You reach a section that is not only complete, but easily defensible. ";
				}
				break;
			case LevelType.Fortress:
				switch(to){
				case LevelType.Standard:
					return "You enter a section outside the main area of the fortress. ";
				case LevelType.Cave:
					return "You leave the fortress behind. The corridors open up into natural caves. ";
				/*case LevelType.Ruined:
					return "Unlike the fortress, this area has deteriorated immensely. ";
				case LevelType.Extravagant:
					return "As you continue, the military focus of your surroundings is replaced by rich luxury. ";*/
				case LevelType.Hive:
					return "A wide hole in the wall leads to an area filled with small chambers. You are reminded of an insect hive. ";
				case LevelType.Mine:
					return "This section might have been part of the fortress, but pickaxes are still scattered in the unfinished rooms. ";
				}
				break;
			/*case LevelType.Extravagant:
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
				break;*/
			}
			return "";
		}
	}
}

