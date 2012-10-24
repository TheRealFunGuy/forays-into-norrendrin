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
	public class Map{
		public class PosArray<T>{
			private T[,] objs;
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
		//public Tile[,] tile{get;set;} 
		//public Actor[,] actor{get;set;} 
		public PosArray<Tile> tile = new PosArray<Tile>(ROWS,COLS);
		public PosArray<Actor> actor = new PosArray<Actor>(ROWS,COLS);
		public int current_level{get;set;}
		public bool wiz_lite{get{ return internal_wiz_lite; }
			set{
				internal_wiz_lite = value;
				if(value == true){
					foreach(Tile t in AllTiles()){
						if(t.Is(FeatureType.FUNGUS)){
							Q.Add(new Event(t,200,EventType.BLAST_FUNGUS));
							Actor.B.Add("The blast fungus starts to smolder in the light. ",t);
							t.features.Remove(FeatureType.FUNGUS);
							t.features.Add(FeatureType.FUNGUS_ACTIVE);
						}
					}
				}
			}
		}
		private bool internal_wiz_lite;
		public bool wiz_dark{get;set;}
		private bool[,] danger_sensed{get;set;}
		//private List<Tile> alltiles = new List<Tile>();
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
			//tile = new Tile[ROWS,COLS];
			//actor = new Actor[ROWS,COLS];
			current_level = 0;
			Map.player = g.player;
			Map.Q = g.Q;
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
		public void UpdateDangerValues(){
			danger_sensed = new bool[ROWS,COLS];
			foreach(Actor a in AllActors()){
				if(a != player){
					foreach(Tile t in AllTiles()){
						if(danger_sensed[t.row,t.col] == false && t.passable && !t.opaque){
							if(a.CanSee(t)){
								int multiplier = a.HasAttr(AttrType.KEEN_SENSES)? 5 : 10;
								int value = (player.Stealth(t.row,t.col) * a.DistanceFrom(t) * multiplier) - 5 * a.player_visibility_duration;
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
			colorchar ch;
			ch.bgcolor = Color.Black;
			if(player.CanSee(r,c)){
				tile[r,c].seen = true;
				if(actor[r,c] != null && player.CanSee(actor[r,c])){
					ch.c = actor[r,c].symbol;
					ch.color = actor[r,c].color;
					if(actor[r,c] == player && player.HasAttr(AttrType.SHADOW_CLOAK) && !player.tile().IsLit()){
						ch.color = Color.DarkBlue;
					}
					if(actor[r,c] == player && player.HasFeat(FeatType.DANGER_SENSE) //was danger_sense_on
					&& danger_sensed != null && danger_sensed[r,c] && player.LightRadius() == 0
					&& !wiz_lite){
						ch.color = Color.Red;
					}
				}
				else{
					if(tile[r,c].inv != null){
						ch.c = tile[r,c].inv.symbol;
						ch.color = tile[r,c].inv.color;
					}
					else{
						if(tile[r,c].features.Count > 0){
							ch.c = tile[r,c].FeatureSymbol();
							ch.color = tile[r,c].FeatureColor();
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
							if(player.HasFeat(FeatType.DANGER_SENSE) && danger_sensed != null //was danger_sense_on
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
							ch.color = tile[r,c].inv.color;
						}
						else{
							if(tile[r,c].Is(FeatureType.RUNE_OF_RETREAT)){ //some features stay visible when out of sight
								ch.c = Tile.Feature(FeatureType.RUNE_OF_RETREAT).symbol;
								ch.color = Tile.Feature(FeatureType.RUNE_OF_RETREAT).color;
							}
							else{
								if(tile[r,c].Is(FeatureType.FUNGUS)){
									ch.c = Tile.Feature(FeatureType.FUNGUS).symbol;
									ch.color = Tile.Feature(FeatureType.FUNGUS).color;
								}
								else{
									if(tile[r,c].Is(FeatureType.FUNGUS_ACTIVE)){
										ch.c = Tile.Feature(FeatureType.FUNGUS_ACTIVE).symbol;
										ch.color = Tile.Feature(FeatureType.FUNGUS_ACTIVE).color;
									}
									else{
										if(tile[r,c].Is(FeatureType.FUNGUS_PRIMED)){
											ch.c = Tile.Feature(FeatureType.FUNGUS_PRIMED).symbol;
											ch.color = Tile.Feature(FeatureType.FUNGUS_PRIMED).color;
										}
										else{
											ch.c = tile[r,c].symbol;
											ch.color = tile[r,c].color;
											if(ch.c=='.' || ch.c=='#'){
												ch.color = Color.DarkGray;
											}
										}
									}
								}
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
			/*List<ActorType> types = new List<ActorType>();
			while(types.Count == 0){
				foreach(ActorType atype in Enum.GetValues(typeof(ActorType))){
					if(atype != ActorType.PLAYER){
						int i = 1 + Math.Abs(Actor.Prototype(atype).level - (current_level+1)/2);
						if(i <= 3 && Global.OneIn(i)){ //level-based check
							if(Global.Roll(Actor.Rarity(atype)) == Actor.Rarity(atype)){ //note that Roll(0) apparently returns 1. hmm.
								types.Add(atype); // at least that has the nice side effect of not generating 0-rarity monsters.
							}
						}
					}
				}
			}
			return types.Random();*/
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
				return (ActorType)(level*5 + Global.Roll(5) - 3); //equal probability for the level 1 monsters
			}
			if(Global.OneIn(5)){ //rarer types
				if(Global.CoinFlip()){
					return (ActorType)(level*5 + 1);
				}
				else{
					return (ActorType)(level*5 + 2);
				}
			}
			else{
				int roll = Global.Roll(3);
				if(roll == 1){
					return (ActorType)(level*5 - 2);
				}
				else{
					if(roll == 2){
						return (ActorType)(level*5 - 1);
					}
					else{
						return (ActorType)(level*5);
					}
				}
			}
		}
		public Actor SpawnMob(){ return SpawnMob(MobType()); }
		public Actor SpawnMob(ActorType type){
			Actor result = null;
			if(type == ActorType.POLTERGEIST){
				while(true){
					int rr = Global.Roll(ROWS-4) + 1;
					int rc = Global.Roll(COLS-4) + 1;
					List<Tile> tiles = new List<Tile>();
					foreach(Tile t in tile[rr,rc].TilesWithinDistance(3)){
						if(t.passable){
							tiles.Add(t);
						}
					}
					if(tiles.Count >= 15){
						Actor.tiebreakers.Add(null); //a placeholder for the poltergeist once it manifests
						Event e = new Event(null,tiles,(Global.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,0,"");
						e.tiebreaker = Actor.tiebreakers.Count - 1;
						Q.Add(e);
						//return type;
						return null;
					}
				}
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
							Q.KillEvents(actor[i,j],EventType.ANY_EVENT);
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
			Q.KillEvents(null,EventType.BLAST_FUNGUS);
			Q.KillEvents(null,EventType.RELATIVELY_SAFE);
			Q.KillEvents(null,EventType.POLTERGEIST);
			Actor.tiebreakers = new List<Actor>{player};
			//alltiles.Clear();
			DungeonGen.Dungeon dungeon = new DungeonGen.Dungeon();
			char[,] charmap = dungeon.Generate();
			int attempts = 0;
			for(bool done=false;!done;++attempts){
				int rr = Global.Roll(ROWS-4) + 1;
				int rc = Global.Roll(COLS-4) + 1;
				if(charmap[rr,rc] == '.'){
					bool floors = true;
					pos temp = new pos(rr,rc);
					foreach(pos p in temp.PositionsAtDistance(1)){
						if(charmap[p.row,p.col] != '.'){
							floors = false;
						}
					}
					if(floors){
						charmap[rr,rc] = '>';
						done = true;
					}
				}
				if(attempts > 500){ Actor.B.Add("Trying to place stairs.... "); }
			}
			if(current_level%2 == 1){
				List<int> ints = new List<int>{0,1,2,3,4};
				while(ints.Count > 0){
					bool done = false;
					while(!done){
						int rr = Global.Roll(ROWS-4) + 1;
						int rc = Global.Roll(COLS-4) + 1;
						if(ints.Count > 1){
							if(charmap[rr,rc] == '.'){
								bool floors = true;
								pos temp = new pos(rr,rc);
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
									charmap[rr,rc] = '#';
									done = true;
								}
							}
						}
						else{
							if(charmap[rr,rc] == '#'){
								if(charmap[rr+1,rc] != '.' && charmap[rr-1,rc] != '.' && charmap[rr,rc-1] != '.' && charmap[rr,rc+1] != '.'){
									continue; //no floors? retry.
								}
								int walls = 0;
								pos temp = new pos(rr,rc);
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
										temp2 = temp.PositionInDirection(Global.RotateDirection(8,true,i));
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
					if(charmap[rr,rc] == '.'){
						bool floors = true;
						pos temp = new pos(rr,rc);
						foreach(pos p in temp.PositionsAtDistance(1)){
							if(charmap[p.row,p.col] != '.'){
								floors = false;
							}
						}
						if(floors){
							charmap[rr,rc] = '=';
							done = true;
						}
					}
					if(tries > 500){ Actor.B.Add("Trying to place chests.... "); }
				}
			}
			int num_firepits = 0;
			switch(Global.Roll(5)){
			case 1:
				num_firepits = 1;
				break;
			case 2:
				num_firepits = Global.Roll(4)+1;
				break;
			}
			for(int i=0;i<num_firepits;++i){
				int tries = 0;
				for(bool done=false;!done && tries < 100;++tries){
					int rr = Global.Roll(ROWS-4) + 1;
					int rc = Global.Roll(COLS-4) + 1;
					if(charmap[rr,rc] == '.'){
						bool floors = true;
						pos temp = new pos(rr,rc);
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
			List<Tile> hidden = new List<Tile>();
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
						if(current_level < 20){
							Tile.Create(TileType.STAIRS,i,j);
						}
						else{
							Tile.Create(TileType.FLOOR,i,j);
						}
						break;
					case '0':
						Tile.Create(TileType.FIREPIT,i,j);
						break;
					case '=':
						Tile.Create(TileType.CHEST,i,j);
						break;
					case '~':
						Tile.Create(TileType.FIRE_GEYSER,i,j);
						break;
					case '^':
						Tile.Create(Tile.RandomTrap(),i,j);
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
						player.light_radius = 0;
						player.Move(rr,rc);
						player.UpdateRadius(0,light,true);
						done = true;
					}
				}
			}
			if(Global.CoinFlip()){ //is 50% the best rate for hidden areas? hmm
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
											tt = TileType.GRENADE_TRAP;
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
													tt = TileType.GRENADE_TRAP;
												}
												neighbor.TransformTo(tt);
												if(tt == TileType.GRENADE_TRAP){
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
													tt = TileType.GRENADE_TRAP;
												}
												neighbor.TransformTo(tt);
												if(tt == TileType.GRENADE_TRAP){
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
								t.TransformTo(TileType.HIDDEN_DOOR);
								if(!hidden.Contains(t)){
									hidden.Add(t);
								}
							}
							if(long_corridor && connections == 1){
								foreach(Tile t in tile[rr,rc].TilesWithinDistance(1)){
									t.TransformTo(TileType.GRENADE_TRAP);
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
				Event e = new Event(1066,EventType.BOSS_ARRIVE);
				e.tiebreaker = 0;
				Q.Add(e);
			}
			{
			Event e = new Event(10000,EventType.RELATIVELY_SAFE);
			e.tiebreaker = 0;
			Q.Add(e);
			}
		}
	}
}

