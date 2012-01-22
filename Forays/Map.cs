/*Copyright (c) 2011  Derrick Creamer
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
	public class Map{
		public Tile[,] tile{get;set;} //note for dungeon generator overhaul: make sure there's something to hide behind.
		public Actor[,] actor{get;set;} //i could make a tilearray/actorarray class if i wanted to use a pos as an index
		public int current_level{get; private set;}
		private bool[,] danger_sensed{get;set;}
		private List<Tile> alltiles = new List<Tile>();
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
		public void UpdateDangerValues(){
			danger_sensed = new bool[ROWS,COLS];
			foreach(Actor a in AllActors()){
				if(a != player){
					foreach(Tile t in AllTiles()){
						if(danger_sensed[t.row,t.col] == false && t.passable && !t.opaque){
							if(a.CanSee(t)){
								int value = (player.Stealth(t.row,t.col) * a.DistanceFrom(t) * 10) - 5 * a.player_visibility_duration;
								if(value < 100 || a.player_visibility_duration < 0){
									danger_sensed[t.row,t.col] = true;
								}
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
					alltiles.Add(tile[i,j]);
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
						//todo: change this
						//List<Tile> area = new List<Tile>();
						//area.Add(tile[i,j]);
						//Q.Add(new Event(area,100,EventType.CHECK_FOR_HIDDEN));
						//
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
			if(hidden.Count > 0){
				Q.Add(new Event(hidden,100,EventType.CHECK_FOR_HIDDEN));
			}
		}
		public void Draw(){
			Console.CursorVisible = false;
			for(int i=0;i<ROWS;++i){ //if(ch.c == '#'){ ch.c = Encoding.GetEncoding(437).GetChars(new byte[] {177})[0]; }
				for(int j=0;j<COLS;++j){ //^--top secret, mostly because it doesn't work well - 
					Screen.WriteMapChar(i,j,VisibleColorChar(i,j)); //redrawing leaves gaps for some reason.
				}
			}
			Screen.ResetColors();
			//Console.CursorVisible = true;
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
			Screen.ResetColors();
			//Console.CursorVisible = true;
		}
		public colorchar VisibleColorChar(int r,int c){
			colorchar ch;
			ch.bgcolor = Color.Black;
			if(player.CanSee(r,c)){
				tile[r,c].seen = true;
				if(actor[r,c] != null && player.CanSee(actor[r,c])){
					ch.c = actor[r,c].symbol;
					ch.color = actor[r,c].color;
					if(actor[r,c] == player && player.HasAttr(AttrType.DANGER_SENSE_ON)
					&& danger_sensed != null && danger_sensed[r,c] && player.LightRadius() == 0){
						ch.color = Color.Red;
					}
				}
				else{
					if(tile[r,c].inv != null){
						ch.c = tile[r,c].inv.symbol;
						ch.color = tile[r,c].inv.color;
					}
					else{
						ch.c = tile[r,c].symbol;
						ch.color = tile[r,c].color;
						if(ch.c=='.' || ch.c=='#'){
							if(tile[r,c].IsLit()){
								ch.color = Color.Yellow;
							}
							else{
								ch.color = Color.DarkCyan;
							}
						}
						if(player.HasAttr(AttrType.DANGER_SENSE_ON) && danger_sensed != null
						&& danger_sensed[r,c] && player.LightRadius() == 0){
							ch.color = Color.Red;
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
						ch.c = tile[r,c].symbol;
						ch.color = tile[r,c].color;
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
		public void SpawnMob(){
			List<ActorType> types = new List<ActorType>();
			while(types.Count == 0){
				foreach(ActorType atype in Enum.GetValues(typeof(ActorType))){
					if(atype != ActorType.PLAYER){
						int i = 1 + Math.Abs(Actor.Prototype(atype).level - (current_level+1)/2);
						if(i <= 3 && Global.Roll(i) == i){ //level-based check
							if(Global.Roll(Actor.Rarity(atype)) == Actor.Rarity(atype)){
								types.Add(atype);
							}
						}
					}
				}
			}
			SpawnMob(types[Global.Roll(types.Count)-1]);
		}
		public void SpawnMob(ActorType type){
			int number = 1;
			if(Actor.Prototype(type).HasAttr(AttrType.SMALL_GROUP)){
				number = Global.Roll(2)+1;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.MEDIUM_GROUP)){
				number = Global.Roll(2)+2;
			}
			if(Actor.Prototype(type).HasAttr(AttrType.LARGE_GROUP)){
				number = Global.Roll(3)+4;
			}
			List<Tile> group_tiles = new List<Tile>();
			for(int i=0;i<number;++i){
				if(i == 0){
					for(int j=0;j<9999;++j){
						int rr = Global.Roll(ROWS-2);
						int rc = Global.Roll(COLS-2);
						if(tile[rr,rc].passable && actor[rr,rc] == null){
							Actor.Create(type,rr,rc);
							if(number > 1){
								group_tiles.Add(tile[rr,rc]);
							}
							break;
						}
					}
				}
				else{
					for(int j=0;j<9999;++j){
						if(group_tiles.Count == 0){ //no space left!
							return;
						}
						Tile t = group_tiles[Global.Roll(group_tiles.Count)-1];
						List<Tile> empty_neighbors = new List<Tile>();
						foreach(Tile neighbor in t.TilesAtDistance(1)){
							if(neighbor.passable && neighbor.actor() == null){
								empty_neighbors.Add(neighbor);
							}
						}
						if(empty_neighbors.Count > 0){
							t = empty_neighbors[Global.Roll(empty_neighbors.Count)-1];
							Actor.Create(type,t.row,t.col);
							group_tiles.Add(t);
							break;
						}
						else{
							group_tiles.Remove(t);
						}
					}
				}
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
							Q.KillEvents(null,EventType.ANY_EVENT);
							//
							Q.KillEvents(actor[i,j],EventType.ANY_EVENT);
						}
						actor[i,j] = null;
					}
					tile[i,j].inv = null;
					tile[i,j] = null;
				}
			}
			alltiles.Clear();
			DungeonGen.Dungeon dungeon = new DungeonGen.Dungeon();
			char[,] charmap = dungeon.Generate();
			//
			for(bool done=false;!done;){ //todo: update this. currently just generates one firepit
				int rr = Global.Roll(ROWS-2);
				int rc = Global.Roll(COLS-2);
				if(charmap[rr,rc] == '.'){
					bool floors = true;
					Tile temp = new Tile(Tile.Prototype(TileType.FLOOR),rr,rc); //i wouldn't need this if my design was better =D
					foreach(pos p in temp.PositionsAtDistance(1)){
						if(charmap[p.row,p.col] != '.'){
							floors = false;
						}
					}
					if(floors){
						charmap[rr,rc] = '0';
						done = true;
					}
				}
			}
//			for(bool done=false;!done;){ //todo: update this. currently just generates one chest
			for(int i=0;i<10;++i){
				int tries = 0;
				for(bool done=false;!done && tries < 100;++tries){
				int rr = Global.Roll(ROWS-2);
				int rc = Global.Roll(COLS-2);
				if(charmap[rr,rc] == '.'){
					bool floors = true;
					pos temp = new pos(rr,rc);
					foreach(pos p in temp.PositionsAtDistance(1)){
						if(charmap[p.row,p.col] != '.'){
							floors = false;
						}
					}
					if(floors){
						if(i == 0){
							charmap[rr,rc] = '~';
						}
						else{
							if(i == 1){
								charmap[rr,rc] = '>';
							}
							else{
								charmap[rr,rc] = '^';
							}
						}
						done = true;
					}
				}}
			}
			//
			List<Tile> hidden = new List<Tile>(); //todo make sure this is right
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					switch(charmap[i,j]){
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
					case '0':
						Tile.Create(TileType.FIREPIT,i,j);
						break;
					case '~':
						Tile.Create(TileType.CHEST,i,j);
						break;
					case '^':
						Tile.Create((TileType)(Global.Roll(6)+9),i,j);
						tile[i,j].name = "floor";
						tile[i,j].the_name = "the floor";
						tile[i,j].a_name = "a floor";
						tile[i,j].symbol = '.';
						tile[i,j].color = Color.White;
						hidden.Add(tile[i,j]);
						break;
					case 'H':
						Tile.Create(TileType.HIDDEN_DOOR,i,j);
						hidden.Add(tile[i,j]);
						break;
					default:
						Tile.Create(TileType.FLOOR,i,j);
						break;
					}
					alltiles.Add(tile[i,j]);
				}
			}
			player.ResetForNewLevel();
			foreach(Tile t in AllTiles()){
				if(t.type == TileType.FIREPIT){
					foreach(Tile tt in t.TilesWithinDistance(1)){
						tt.light_value++;
					}
				}
			}
			if(true){ //todo will become if(coinflip) or something, i dunno
				bool done = false;
				for(int tries=0;!done && tries<9999;++tries){
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
						for(int i=2;i<=8;i+=2){
							Tile t = tile[rr,rc].TileInDirection(i).TileInDirection(i);
							bool good_dir = true;
							while(good_dir && t != null && t.type == TileType.WALL){
								if(t.TileInDirection(t.RotateDirection(i,false,2)).type != TileType.WALL){
									good_dir = false;
								}
								if(t.TileInDirection(t.RotateDirection(i,true,2)).type != TileType.WALL){
									good_dir = false;
								}
								t = t.TileInDirection(i);
							}
							if(good_dir && t != null){
								dirs.Add(i);
							}
						}
						if(dirs.Count > 0){
							//todo: remove some directions randomly
							foreach(int i in dirs){
								Tile t = tile[rr,rc].TileInDirection(i);
								while(t.type == TileType.WALL){
									t.TransformTo(TileType.FLOOR); //todo: make some of these traps, too
									t = t.TileInDirection(i);
								}
								t.TileInDirection(t.RotateDirection(i,true,4)).TransformTo(TileType.HIDDEN_DOOR);
								hidden.Add(t.TileInDirection(t.RotateDirection(i,true,4)));
							}
							foreach(Tile t in tile[rr,rc].TilesAtDistance(1)){
								t.TransformTo((TileType)(Global.Roll(6)+9));
								t.name = "floor";
								t.the_name = "the floor";
								t.a_name = "a floor";
								t.symbol = '.';
								t.color = Color.White;
								hidden.Add(t);
							}
							tile[rr,rc].TransformTo(TileType.CHEST);
							done = true;
						}
					}
				}
			}
			if(hidden.Count > 0){
				Q.Add(new Event(hidden,100,EventType.CHECK_FOR_HIDDEN));
			}
			//todo: spawn some items
			for(int i=Global.Roll(2,2)+3;i>0;--i){
				SpawnMob();
			}
			if(current_level == 20){
				Q.Add(new Event(1000,EventType.BOSS_ARRIVE));
			}
			//todo: find a spot for the player that no monsters can see
			//if there is no such spot, place the player randomly
			for(bool done=false;!done;){
				int rr = Global.Roll(ROWS-2);
				int rc = Global.Roll(COLS-2);
				if(tile[rr,rc].passable && actor[rr,rc] == null){
					int light = player.light_radius;
					player.light_radius = 0;
					player.Move(rr,rc);
					player.UpdateRadius(0,light,true);
					done = true;
				}
			}
		}
	}
}

