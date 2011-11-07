using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace Forays{
	public class Map{
		public Tile[,] tile{get;set;} //note for dungeon generator overhaul: make sure there's something to hide behind.
		public Actor[,] actor{get;set;} //i could make a tilearray/actorarray class if i wanted to use a pos as an index
		public int current_level{get; private set;}
		private static List<Tile> alltiles = new List<Tile>();
		private static List<pos> allpositions = new List<pos>();
		
		private const int ROWS = Global.ROWS; //hax lol
		private const int COLS = Global.COLS;
		public static Actor player{get;set;}
		public static Queue Q{get;set;}
		static Map(){
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					allpositions.Add(new pos(i,j));
				}
			}
		}
		public Map(Game g){
			tile = new Tile[ROWS,COLS];
			actor = new Actor[ROWS,COLS];
			current_level = 1;
			Map.player = g.player;
			Map.Q = g.Q;
		}
		public bool BoundsCheck(int r,int c){
			if(r>=0 && r<ROWS && c>=0 && c<COLS){
				return true;
			}
			return false;
		}
		public List<Tile> AllTiles(){ return alltiles; }
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
		public void InitLevel(){ //creates an empty level surrounded by walls. used for testing purposes.
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(i==0 || j==0 || i==ROWS-1 || j==COLS-1){
						tile[i,j] = Tile.Create(TileType.WALL,i,j);
					}
					else{
						tile[i,j] = Tile.Create(TileType.FLOOR,i,j);
					}
					alltiles.Add(tile[i,j]);
				}
			}
		}
		public void LoadLevel(string filename){
			TextReader file = new StreamReader(filename);
			char ch;
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
					default:
						Tile.Create(TileType.FLOOR,i,j);
						break;
					}
					alltiles.Add(tile[i,j]);
				}
				file.ReadLine();
			}
			file.Close();
		}
		public void Draw(){
			Console.CursorVisible = false;
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					colorchar ch;
					ch.bgcolor = Color.Black;
					if(player.CanSee(i,j)){
						tile[i,j].seen = true;
						if(actor[i,j] != null && player.CanSee(actor[i,j])){
							ch.c = actor[i,j].symbol;
							ch.color = actor[i,j].color;
						}
						else{
							if(tile[i,j].inv != null){
								ch.c = tile[i,j].inv.symbol;
								ch.color = tile[i,j].inv.color;
							}
							else{
								ch.c = tile[i,j].symbol;
								ch.color = tile[i,j].color;
								if(ch.c=='.' || ch.c=='#'){
									if(tile[i,j].IsLit()){
										ch.color = Color.Yellow;
									}
									else{
										ch.color = Color.DarkCyan;
									}
								}
							}
						}
					}
					else{
						if(actor[i,j] != null && player.CanSee(actor[i,j])){
							ch.c = actor[i,j].symbol;
							ch.color = actor[i,j].color;
						}
						else{
							if(tile[i,j].seen){
								ch.c = tile[i,j].symbol;
								ch.color = tile[i,j].color;
								if(ch.c=='.' || ch.c=='#'){
									ch.color = Color.White;
								}
							}
							else{
								ch.c = ' ';
								ch.color = Color.Black;
							}
						}
					}
					//if(ch.c == '#'){ ch.c = Encoding.GetEncoding(437).GetChars(new byte[] {177})[0]; }
					//^--top secret, mostly because it doesn't work well - redrawing leaves gaps for some reason.
					Screen.WriteMapChar(i,j,ch);
				}
			}
			Console.ResetColor();
			Console.CursorVisible = true;
		}
		public void RedrawWithStrings(){
			Console.CursorVisible = false;
			colorstring s;
			s.s = "";
			s.bgcolor = Color.Black;
			s.color = Color.Black;
			int r = 0;
			int c = 0;
			for(int i=0;i<ROWS;++i){
				s.s = "";
				r = i;
				c = 0;
				for(int j=0;j<COLS;++j){
					colorchar ch;
					ch.bgcolor = Color.Black;
					if(player.CanSee(i,j)){
						tile[i,j].seen = true;
						if(actor[i,j] != null && player.CanSee(actor[i,j])){
							ch.c = actor[i,j].symbol;
							ch.color = actor[i,j].color;
						}
						else{
							if(tile[i,j].inv != null){
								ch.c = tile[i,j].inv.symbol;
								ch.color = tile[i,j].inv.color;
							}
							else{
								ch.c = tile[i,j].symbol;
								ch.color = tile[i,j].color;
								if(ch.c=='.' || ch.c=='#'){
									if(tile[i,j].IsLit()){
										ch.color = Color.Yellow;
									}
									else{
										ch.color = Color.DarkCyan;
									}
								}
							}
						}
					}
					else{
						if(actor[i,j] != null && player.CanSee(actor[i,j])){
							ch.c = actor[i,j].symbol;
							ch.color = actor[i,j].color;
						}
						else{
							if(tile[i,j].seen){
								ch.c = tile[i,j].symbol;
								ch.color = tile[i,j].color;
								if(ch.c=='.' || ch.c=='#'){
									ch.color = Color.White;
								}
							}
							else{
								ch.c = ' ';
								ch.color = Color.Black;
							}
						}
					}
					if(ch.color != s.color){
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
			Console.ResetColor();
			Console.CursorVisible = true;
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
		public void GenerateLevel(){
			if(current_level < 10){
				++current_level;
			}
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(actor[i,j] != null){
						actor[i,j].inv.Clear();
						actor[i,j].target = null;
						Q.KillEvents(actor[i,j],EventType.ANY_EVENT);
						actor[i,j] = null;
					}
					tile[i,j].inv = null;
					tile[i,j] = null;
				}
			}
			alltiles.Clear();
			//generate layout
			//update player for new level, including reset of player's target
			//spawn some items and mobs
			if(current_level == 10){
				Q.Add(new Event(1000,EventType.BOSS_ARRIVE));
			}
			//find a spot for the player that no monsters can see
			//if there is no such spot, place the player randomly
		}//remember to add new tiles to alltiles
	}
}

