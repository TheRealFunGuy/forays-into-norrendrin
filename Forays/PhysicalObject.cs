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
	public class PhysicalObject{
		public pos p;
		public int row{
			get{
				return p.row;
			}
			set{
				p.row = value;
			}
		}
		public int col{
			get{
				return p.col;
			}
			set{
				p.col = value;
			}
		}
		public string name{get;set;}
		public string a_name{get;set;}
		public string the_name{get;set;}
		public char symbol{get;set;}
		public Color color{get;set;}
		public int light_radius{get;set;}
		
		public static Map M{get;set;}
		public PhysicalObject(){
			row=-1;
			col=-1;
			name="";
			a_name="";
			the_name="";
			symbol='%';
			color=Color.White;
			light_radius = 0;
		}
		public PhysicalObject(string name_,char symbol_,Color color_){
			row = -1;
			col = -1;
			SetName(name_);
			symbol = symbol_;
			color = color_;
			light_radius = 0;
		}
		public void SetName(string new_name){
			name = new_name;
			the_name = "the " + name;
			a_name = "a " + name;
			if(name=="you"){
				the_name = "you";
				a_name = "you";
			}
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
			}
		}
		public void Cursor(){
			Console.SetCursorPosition(col+Global.MAP_OFFSET_COLS,row+Global.MAP_OFFSET_ROWS);
		}
		public void UpdateRadius(int from,int to){ UpdateRadius(from,to,false); }
		public void UpdateRadius(int from,int to,bool change){
			if(from > 0){
				for(int i=row-from;i<=row+from;++i){
					for(int j=col-from;j<=col+from;++j){
						if(i>0 && i<Global.ROWS-1 && j>0 && j<Global.COLS-1){
							if(!M.tile[i,j].opaque && HasBresenhamLine(i,j)){
								M.tile[i,j].light_value--;
							}
						}
					}
				}
			}
			if(to > 0){
				for(int i=row-to;i<=row+to;++i){
					for(int j=col-to;j<=col+to;++j){
						if(i>0 && i<Global.ROWS-1 && j>0 && j<Global.COLS-1){
							if(!M.tile[i,j].opaque && HasBresenhamLine(i,j)){
								M.tile[i,j].light_value++;
							}
						}
					}
				}
			}
			if(change){
				light_radius = to;
			}
		}
		public string YouAre(){
			if(name == "you"){
				return "you are";
			}
			else{
				return the_name + " is";
			}
		}
		public string Your(){
			if(name == "you"){
				return "your";
			}
			else{
				return the_name + "'s";
			}
		}
		public string You(string s){ return You(s,false); }
		public string You(string s,bool ends_in_es){
			if(name == "you"){
				return "you " + s;
			}
			else{
				if(ends_in_es){
					return the_name + " " + s + "es";
				}
				else{
					return the_name + " " + s + "s";
				}
			}
		}
		virtual public string YouVisible(string s){ return YouVisible(s,false); }
		virtual public string YouVisible(string s,bool ends_in_es){ //same as You(). overridden by Actor.
			if(name == "you"){
				return "you " + s;
			}
			else{
				if(ends_in_es){
					return the_name + " " + s + "es";
				}
				else{
					return the_name + " " + s + "s";
				}
			}
		}
		public string YouFeel(){
			if(name == "you"){
				return "you feel";
			}
			else{
				return the_name + " looks";
			}
		}
		public int DistanceFrom(PhysicalObject o){ return DistanceFrom(o.row,o.col); }
		public int DistanceFrom(pos p){ return DistanceFrom(p.row,p.col); }
		public int DistanceFrom(int r,int c){
			int dy = Math.Abs(r-row);
			int dx = Math.Abs(c-col);
			if(dx > dy){
				return dx;
			}
			else{
				return dy;
			}
		}
		public int EstimatedEuclideanDistanceFromX10(PhysicalObject o){ return EstimatedEuclideanDistanceFromX10(o.row,o.col); }
		public int EstimatedEuclideanDistanceFromX10(pos p){ return EstimatedEuclideanDistanceFromX10(p.row,p.col); }
		public int EstimatedEuclideanDistanceFromX10(int r,int c){ // x10 so that orthogonal directions are closer than diagonals
			int dy = Math.Abs(r-row) * 10;
			int dx = Math.Abs(c-col) * 10;
			if(dx > dy){
				return dx + (dy/2);
			}
			else{
				return dy + (dx/2);
			}
		}
		public Actor ActorInDirection(int dir){
			switch(dir){
			case 7:
				if(M.BoundsCheck(row-1,col-1)){
					return M.actor[row-1,col-1];
				}
				break;
			case 8:
				if(M.BoundsCheck(row-1,col)){
					return M.actor[row-1,col];
				}
				break;
			case 9:
				if(M.BoundsCheck(row-1,col+1)){
					return M.actor[row-1,col+1];
				}
				break;
			case 4:
				if(M.BoundsCheck(row,col-1)){
					return M.actor[row,col-1];
				}
				break;
			case 5:
				if(M.BoundsCheck(row,col)){
					return M.actor[row,col];
				}
				break;
			case 6:
				if(M.BoundsCheck(row,col+1)){
					return M.actor[row,col+1];
				}
				break;
			case 1:
				if(M.BoundsCheck(row+1,col-1)){
					return M.actor[row+1,col-1];
				}
				break;
			case 2:
				if(M.BoundsCheck(row+1,col)){
					return M.actor[row+1,col];
				}
				break;
			case 3:
				if(M.BoundsCheck(row+1,col+1)){
					return M.actor[row+1,col+1];
				}
				break;
			default:
				return null;
			}
			return null;
		}
		public Tile TileInDirection(int dir){
			switch(dir){
			case 7:
				if(M.BoundsCheck(row-1,col-1)){
					return M.tile[row-1,col-1];
				}
				break;
			case 8:
				if(M.BoundsCheck(row-1,col)){
					return M.tile[row-1,col];
				}
				break;
			case 9:
				if(M.BoundsCheck(row-1,col+1)){
					return M.tile[row-1,col+1];
				}
				break;
			case 4:
				if(M.BoundsCheck(row,col-1)){
					return M.tile[row,col-1];
				}
				break;
			case 5:
				if(M.BoundsCheck(row,col)){
					return M.tile[row,col];
				}
				break;
			case 6:
				if(M.BoundsCheck(row,col+1)){
					return M.tile[row,col+1];
				}
				break;
			case 1:
				if(M.BoundsCheck(row+1,col-1)){
					return M.tile[row+1,col-1];
				}
				break;
			case 2:
				if(M.BoundsCheck(row+1,col)){
					return M.tile[row+1,col];
				}
				break;
			case 3:
				if(M.BoundsCheck(row+1,col+1)){
					return M.tile[row+1,col+1];
				}
				break;
			default:
				return null;
			}
			return null;
		}
		public Actor FirstActorInLine(PhysicalObject obj){ return FirstActorInLine(obj,1); }
		public Actor FirstActorInLine(PhysicalObject obj,int num){
			if(obj == null){
				return null;
			}
			int count = 0;
			List<Tile> line = GetBestLine(obj.row,obj.col);
			line.RemoveAt(0);
			foreach(Tile t in line){
				if(!t.passable){
					return null;
				}
				if(M.actor[t.row,t.col] != null){
					++count;
					if(count == num){
						return M.actor[t.row,t.col];
					}
				}
			}
			return null;
		}
		public Actor FirstActorInLine(List<Tile> line){ return FirstActorInLine(line,1); }
		public Actor FirstActorInLine(List<Tile> line,int num){
			if(line == null){
				return null;
			}
			int count = 0;
			int idx = 0; //note that the first position is thrown out, as it is assumed to be the origin of the line
			foreach(Tile t in line){
				if(idx != 0){
					if(!t.passable){
						return null;
					}
					if(M.actor[t.row,t.col] != null){
						++count;
						if(count == num){
							return M.actor[t.row,t.col];
						}
					}
				}
				++idx;
			}
			return null;
		}
		public Actor FirstActorInExtendedLine(PhysicalObject obj){ return FirstActorInExtendedLine(obj,1,-1); }
		public Actor FirstActorInExtendedLine(PhysicalObject obj,int max_distance){ return FirstActorInExtendedLine(obj,1,max_distance); }
		public Actor FirstActorInExtendedLine(PhysicalObject obj,int num,int max_distance){
			if(obj == null){
				return null;
			}
			int count = 0;
			List<Tile> line = GetBestExtendedLine(obj.row,obj.col);
			line.RemoveAt(0);
			foreach(Tile t in line){
				if(!t.passable){
					return null;
				}
				if(max_distance != -1 && DistanceFrom(t) > max_distance){
					return null;
				}
				if(M.actor[t.row,t.col] != null){
					++count;
					if(count == num){
						return M.actor[t.row,t.col];
					}
				}
			}
			return null;
		}
		public Tile FirstSolidTileInLine(PhysicalObject obj){ return FirstSolidTileInLine(obj,1); }
		public Tile FirstSolidTileInLine(PhysicalObject obj,int num){
			if(obj == null){
				return null;
			}
			int count = 0;
			List<Tile> line = GetBestLine(obj.row,obj.col);
			line.RemoveAt(0);
			foreach(Tile t in line){
				if(!t.passable){
					++count;
					if(count == num){
						return t;
					}
				}
			}
			return null;
		}
		public int RotateDirection(int dir,bool clockwise){ return RotateDirection(dir,clockwise,1); }
		public int RotateDirection(int dir,bool clockwise,int num){
			if(num < 0){
				num = -(num);
				clockwise = !clockwise;
			}
			for(int i=0;i<num;++i){
				switch(dir){
				case 7:
					dir = clockwise?8:4;
					break;
				case 8:
					dir = clockwise?9:7;
					break;
				case 9:
					dir = clockwise?6:8;
					break;
				case 4:
					dir = clockwise?7:1;
					break;
				case 5:
					break;
				case 6:
					dir = clockwise?3:9;
					break;
				case 1:
					dir = clockwise?4:2;
					break;
				case 2:
					dir = clockwise?1:3;
					break;
				case 3:
					dir = clockwise?2:6;
					break;
				default:
					dir = 0;
					break;
				}
			}
			return dir;
		}
		public int DirectionOf(PhysicalObject obj){ return DirectionOf(obj.p); }
		public int DirectionOf(pos obj){
			int dy = Math.Abs(obj.row - row);
			int dx = Math.Abs(obj.col - col);
			if(dy == 0){
				if(col < obj.col){
					return 6;
				}
				if(col > obj.col){
					return 4;
				}
				else{
					if(dx == 0){
						return 5;
					}
				}
			}
			if(dx == 0){
				if(row > obj.row){
					return 8;
				}
				else{
					if(row < obj.row){
						return 2;
					}
				}
			}
			if(row+col == obj.row+obj.col){ //slope is -1
				if(row > obj.row){
					return 9;
				}
				else{
					if(row < obj.row){
						return 1;
					}
				}
			}
			if(row-col == obj.row-obj.col){ //slope is 1
				if(row > obj.row){
					return 7;
				}
				else{
					if(row < obj.row){
						return 3;
					}
				}
			}
			// calculate all other dirs here
			/*.................flipped y
........m........
.......l|n.......
........|........
.....k..|..o.....
......\.|./......
...j...\|/...p...
..i-----@-----a.1
...h.../|\...b.2.
....../.|.\.B.3..
.....g..|..c.4...
........|...5....
.......f|d.......
........e........

@-------------...
|\;..b.2.........
|.\.B.3..........
|..\.4;..........
|...\...;........
|....\....;6.....
|.....\.....;....
|......\.....5;..
	rise:	run:	ri/ru:	angle(flipped y):
b:	1	5	1/5		(obviously the dividing line should be 22.5 degrees here)
d:	5	1	5		67.5
f:	5	-1	-5		112.5
h:	1	-5	-1/5		157.5
j:	-1	-5	1/5		202.5
l:	-5	-1	5		247.5
n:	-5	1	-5		292.5
p:	-1	5	-1/5		337.5
algorithm for determining direction...			(for b)		(for 4)		(for 6)		(for 5)		(for B)
first, determine 'major' direction - NSEW		E		E		E		E		E
then, determine 'minor' direction - diagonals		SE		SE		SE		SE		SE
find the ratio of d-major/d(other dir) (both positive)	1/5		3/5		5/11		7/13		2/4
compare this number to 1/2:  if less than 1/2, major.	
	if more than 1/2, minor.
	if exactly 1/2, tiebreaker.
							major(E)	minor(SE)	major(E)	minor(SE)	tiebreak


*/
			int primary; //orthogonal
			int secondary; //diagonal
			int dprimary = Math.Min(dy,dx);
			int dsecondary = Math.Max(dy,dx);
			if(row < obj.row){ //down
				if(col < obj.col){ //right
					secondary = 3;
					if(dx > dy){ //slope less than 1
						primary = 6;
					}
					else{ //slope greater than 1
						primary = 2;
					}
				}
				else{ //left
					secondary = 1;
					if(dx > dy){ //slope less than 1
						primary = 4;
					}
					else{ //slope greater than 1
						primary = 2;
					}
				}
			}
			else{ //up
				if(col < obj.col){ //right
					secondary = 9;
					if(dx > dy){ //slope less than 1
						primary = 6;
					}
					else{ //slope greater than 1
						primary = 8;
					}
				}
				else{ //left
					secondary = 7;
					if(dx > dy){ //slope less than 1
						primary = 4;
					}
					else{ //slope greater than 1
						primary = 8;
					}
				}
			}
			int tiebreaker = primary;
			float ratio = (float)dprimary / (float)dsecondary;
			if(ratio < 0.5f){
				return primary;
			}
			else{
				if(ratio > 0.5f){
					return secondary;
				}
				else{
					return tiebreaker;
				}
			}
		}
		public int DirectionOfOnlyUnblocked(TileType tiletype){ return DirectionOfOnlyUnblocked(tiletype,false); }
		public int DirectionOfOnlyUnblocked(TileType tiletype,bool orth){//if there's only 1 unblocked tile of this kind, return its dir
			int total=0;
			int dir=0;
			for(int i=1;i<=9;++i){
				if(i != 5){
					if(TileInDirection(i).type == tiletype && ActorInDirection(i) == null && TileInDirection(i).inv == null){
						if(!orth || i%2==0){
							++total;
							dir = i;
						}
					}
				}
				/*else{
					if(tile().type == tiletype && !orth){
						++total;
						dir = i;
					}
				}*/
			}
			if(total > 1){
				return -1;
			}
			else{
				if(total == 1){
					return dir;
				}
				else{
					return 0;
				}
			}
		}
		public Actor actor(){
			return M.actor[row,col];
		}
		public Tile tile(){
			return M.tile[row,col];
		}
		public List<Actor> ActorsWithinDistance(int dist){ return ActorsWithinDistance(dist,false); }
		public List<Actor> ActorsWithinDistance(int dist,bool exclude_origin){
			List<Actor> result = new List<Actor>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(i!=row || j!=col || exclude_origin==false){
						if(M.BoundsCheck(i,j) && M.actor[i,j] != null){
							result.Add(M.actor[i,j]);
						}
					}
				}
			}
			return result;
		}
		public List<Actor> ActorsAtDistance(int dist){
			List<Actor> result = new List<Actor>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(DistanceFrom(i,j) == dist && M.BoundsCheck(i,j) && M.actor[i,j] != null){
						result.Add(M.actor[i,j]);
					}
				}
			}
			return result;
		}
		public List<Tile> TilesWithinDistance(int dist){ return TilesWithinDistance(dist,false); }
		public List<Tile> TilesWithinDistance(int dist,bool exclude_origin){
			List<Tile> result = new List<Tile>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(i!=row || j!=col || exclude_origin==false){
						if(M.BoundsCheck(i,j)){
							result.Add(M.tile[i,j]);
						}
					}
				}
			}
			return result;
		}
		public List<Tile> TilesAtDistance(int dist){
			List<Tile> result = new List<Tile>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(DistanceFrom(i,j) == dist && M.BoundsCheck(i,j)){
						result.Add(M.tile[i,j]);
					}
				}
			}
			return result;
		}
		public List<pos> PositionsWithinDistance(int dist){ return PositionsWithinDistance(dist,false); }
		public List<pos> PositionsWithinDistance(int dist,bool exclude_origin){
			List<pos> result = new List<pos>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(i!=row || j!=col || exclude_origin==false){
						if(M.BoundsCheck(i,j)){
							result.Add(new pos(i,j));
						}
					}
				}
			}
			return result;
		}
		public List<pos> PositionsAtDistance(int dist){
			List<pos> result = new List<pos>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(DistanceFrom(i,j) == dist && M.BoundsCheck(i,j)){
						result.Add(new pos(i,j));
					}
				}
			}
			return result;
		}
		public bool IsAdjacentTo(TileType type){ return IsAdjacentTo(type,false); } //didn't need an Actor (or Item) version yet
		public bool IsAdjacentTo(TileType type,bool consider_origin){
			foreach(Tile t in TilesWithinDistance(1,!consider_origin)){
				if(t.type == type){
					return true;
				}
			}
			return false;
		}
		public bool IsAdjacentTo(FeatureType type){ return IsAdjacentTo(type,false); } //didn't need an Actor (or Item) version yet
		public bool IsAdjacentTo(FeatureType type,bool consider_origin){
			foreach(Tile t in TilesWithinDistance(1,!consider_origin)){
				if(t.features.Contains(type)){
					return true;
				}
			}
			return false;
		}
		public bool HasLOS(PhysicalObject o){ return HasLOS(o.row,o.col); }
		public bool HasLOS(int r,int c){
			int y1 = row;
			int x1 = col;
			int y2 = r;
			int x2 = c;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			if(dx<=1 && dy<=1){ //everything adjacent
				return true;
			}
			if(HasBresenhamLine(r,c)){ //basic LOS check
				return true;
			}
			if(M.tile[r,c].opaque){ //for walls, check nearby tiles
				foreach(Tile t in M.tile[r,c].NeighborsBetween(row,col)){
					if(HasBresenhamLine(t.row,t.col)){
						return true;
					}
				}
			}
			return false;
		}
		public bool HasBresenhamLine(int r,int c){
			List<Tile> line = GetBestLine(r,c);
			int length = line.Count;
			if(length == 1){
				return true;
			}
			for(int i=1;i<length-1;++i){ //todo: experimentally changed i=0 to i=1, to skip the first tile.
				if(line[i].opaque){ // this should allow actors to see out of solid tiles.
					return false;
				}
			}
			return true;
		}
		public List<Tile> GetBestLine(PhysicalObject o){ return GetBestLine(o.row,o.col); }
		public List<Tile> GetBestLine(int r,int c){
			List<Tile> list = GetBresenhamLine(r,c);
			List<Tile> list2 = GetAlternateBresenhamLine(r,c);
			for(int i=0;i<list.Count;++i){
				if(list2[i].opaque){
					return list;
				}
				if(list[i].opaque){
					return list2;
				}
			}
			return list;
		}
		public List<Tile> GetBestExtendedLine(PhysicalObject o){ return GetBestExtendedLine(o.row,o.col); }
		public List<Tile> GetBestExtendedLine(int r,int c){
			List<Tile> list = GetExtendedBresenhamLine(r,c);
			List<Tile> list2 = GetAlternateExtendedBresenhamLine(r,c);
			for(int i=0;i<list.Count;++i){
				if(list2[i].opaque){
					return list;
				}
				if(list[i].opaque){
					return list2;
				}
			}
			return list;
		}
		public List<Tile> GetBresenhamLine(PhysicalObject o){ return GetBresenhamLine(o.row,o.col); }
		public List<Tile> GetBresenhamLine(int r,int c){ //bresenham (inverted y)
			int y2 = r;
			int x2 = c;
			int y1 = row;
			int x1 = col;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			int er = 0;
			List<Tile> list = new List<Tile>();
			if(dy==0){
				if(dx==0){
					list.Add(M.tile[row,col]);
					return list;
				}
				for(;x1<x2;++x1){ //right
					list.Add(M.tile[y1,x1]);
				}
				for(;x1>x2;--x1){ //left
					list.Add(M.tile[y1,x1]);
				}
				list.Add(M.tile[r,c]);
				return list;
			}
			if(dx==0){
				for(;y1>y2;--y1){ //up
					list.Add(M.tile[y1,x1]);
				}
				for(;y1<y2;++y1){ //down
					list.Add(M.tile[y1,x1]);
				}
				list.Add(M.tile[r,c]);
				return list;
			}
			if(y1+x1==y2+x2){ //slope is -1
				for(;x1<x2;++x1){ //up-right
					list.Add(M.tile[y1,x1]);
					--y1;
				}
				for(;x1>x2;--x1){ //down-left
					list.Add(M.tile[y1,x1]);
					++y1;
				}
				list.Add(M.tile[r,c]);
				return list;
			}
			if(y1-x1==y2-x2){ //slope is 1
				for(;x1<x2;++x1){ //down-right
					list.Add(M.tile[y1,x1]);
					++y1;
				}
				for(;x1>x2;--x1){ //up-left
					list.Add(M.tile[y1,x1]);
					--y1;
				}
				list.Add(M.tile[r,c]);
				return list;
			}
			if(y1<y2){ //down
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<x2;++x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 >= dx){
								++y1;
								er -= dx;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
					else{ //slope greater than 1
						for(;y1<y2;++y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 >= dy){
								++x1;
								er -= dy;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>x2;--x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 >= dx){
								++y1;
								er -= dx;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
					else{ //slope greater than 1
						for(;y1<y2;++y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 >= dy){
								--x1;
								er -= dy;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
				}
			}
			else{ //up
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<x2;++x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 >= dx){
								--y1;
								er -= dx;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
					else{ //slope greater than 1
						for(;y1>y2;--y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 >= dy){
								++x1;
								er -= dy;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>x2;--x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 >= dx){
								--y1;
								er -= dx;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
					else{ //slope greater than 1
						for(;y1>y2;--y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 >= dy){
								--x1;
								er -= dy;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
				}
			}
		}
		public List<Tile> GetExtendedBresenhamLine(PhysicalObject o){ return GetExtendedBresenhamLine(o.row,o.col); }
		public List<Tile> GetExtendedBresenhamLine(int r,int c){ //extends to edge of map
			int y2 = r;
			int x2 = c;
			int y1 = row;
			int x1 = col;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			int er = 0;
			int COLS = Global.COLS; //for laziness
			int ROWS = Global.ROWS;
			List<Tile> list = new List<Tile>();
			if(dy==0){
				if(dx==0){
					list.Add(M.tile[row,col]);
					return list;
				}
				if(x1<x2){
					for(;x1<=COLS-1;++x1){ //right
						list.Add(M.tile[y1,x1]);
					}
				}
				else{
					for(;x1>=0;--x1){ //left
						list.Add(M.tile[y1,x1]);
					}
				}
				return list;
			}
			if(dx==0){
				if(y1>y2){
					for(;y1>=0;--y1){ //up
						list.Add(M.tile[y1,x1]);
					}
				}
				else{
					for(;y1<=ROWS-1;++y1){ //down
						list.Add(M.tile[y1,x1]);
					}
				}
				return list;
			}
			if(y1+x1==y2+x2){ //slope is -1
				if(x1<x2){
					for(;x1<=COLS-1 && y1>=0;++x1){ //up-right
						list.Add(M.tile[y1,x1]);
						--y1;
					}
				}
				else{
					for(;x1>=0 && y1<=ROWS-1;--x1){ //down-left
						list.Add(M.tile[y1,x1]);
						++y1;
					}
				}
				return list;
			}
			if(y1-x1==y2-x2){ //slope is 1
				if(x1<x2){
					for(;x1<=COLS-1 && y1<=ROWS-1;++x1){ //down-right
						list.Add(M.tile[y1,x1]);
						++y1;
					}
				}
				else{
					for(;x1>=0 && y1>=0;--x1){ //up-left
						list.Add(M.tile[y1,x1]);
						--y1;
					}
				}
				return list;
			}
			if(y1<y2){ //down
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<=COLS-1 && y1<=ROWS-1;++x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 >= dx){
								++y1;
								er -= dx;
							}
						}
						return list;
					}
					else{ //slope greater than 1
						for(;y1<=ROWS-1 && x1<=COLS-1;++y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 >= dy){
								++x1;
								er -= dy;
							}
						}
						return list;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>=0 && y1<=ROWS-1;--x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 >= dx){
								++y1;
								er -= dx;
							}
						}
						return list;
					}
					else{ //slope greater than 1
						for(;y1<=ROWS-1 && x1>=0;++y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 >= dy){
								--x1;
								er -= dy;
							}
						}
						return list;
					}
				}
			}
			else{ //up
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<=COLS-1 && y1>=0;++x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 >= dx){
								--y1;
								er -= dx;
							}
						}
						return list;
					}
					else{ //slope greater than 1
						for(;y1>=0 && x1<=COLS-1;--y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 >= dy){
								++x1;
								er -= dy;
							}
						}
						return list;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>=0 && y1>=0;--x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 >= dx){
								--y1;
								er -= dx;
							}
						}
						return list;
					}
					else{ //slope greater than 1
						for(;y1>=0 && x1>=0;--y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 >= dy){
								--x1;
								er -= dy;
							}
						}
						return list;
					}
				}
			}
		}
		public List<Tile> GetAlternateBresenhamLine(PhysicalObject o){ return GetAlternateBresenhamLine(o.row,o.col); }
		public List<Tile> GetAlternateBresenhamLine(int r,int c){ //bresenham (inverted y)
			int y2 = r;
			int x2 = c;
			int y1 = row;
			int x1 = col;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			int er = 0;
			List<Tile> list = new List<Tile>();
			if(dy==0){
				if(dx==0){
					list.Add(M.tile[row,col]);
					return list;
				}
				for(;x1<x2;++x1){ //right
					list.Add(M.tile[y1,x1]);
				}
				for(;x1>x2;--x1){ //left
					list.Add(M.tile[y1,x1]);
				}
				list.Add(M.tile[r,c]);
				return list;
			}
			if(dx==0){
				for(;y1>y2;--y1){ //up
					list.Add(M.tile[y1,x1]);
				}
				for(;y1<y2;++y1){ //down
					list.Add(M.tile[y1,x1]);
				}
				list.Add(M.tile[r,c]);
				return list;
			}
			if(y1+x1==y2+x2){ //slope is -1
				for(;x1<x2;++x1){ //up-right
					list.Add(M.tile[y1,x1]);
					--y1;
				}
				for(;x1>x2;--x1){ //down-left
					list.Add(M.tile[y1,x1]);
					++y1;
				}
				list.Add(M.tile[r,c]);
				return list;
			}
			if(y1-x1==y2-x2){ //slope is 1
				for(;x1<x2;++x1){ //down-right
					list.Add(M.tile[y1,x1]);
					++y1;
				}
				for(;x1>x2;--x1){ //up-left
					list.Add(M.tile[y1,x1]);
					--y1;
				}
				list.Add(M.tile[r,c]);
				return list;
			}
			if(y1<y2){ //down
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<x2;++x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
					else{ //slope greater than 1
						for(;y1<y2;++y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>x2;--x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
					else{ //slope greater than 1
						for(;y1<y2;++y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
				}
			}
			else{ //up
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<x2;++x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
					else{ //slope greater than 1
						for(;y1>y2;--y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>x2;--x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
					else{ //slope greater than 1
						for(;y1>y2;--y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						list.Add(M.tile[r,c]);
						return list;
					}
				}
			}
		}
		public List<Tile> GetAlternateExtendedBresenhamLine(PhysicalObject o){ return GetAlternateExtendedBresenhamLine(o.row,o.col); }
		public List<Tile> GetAlternateExtendedBresenhamLine(int r,int c){ //extends to edge of map
			int y2 = r;
			int x2 = c;
			int y1 = row;
			int x1 = col;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			int er = 0;
			int COLS = Global.COLS; //for laziness
			int ROWS = Global.ROWS;
			List<Tile> list = new List<Tile>();
			if(dy==0){
				if(dx==0){
					list.Add(M.tile[row,col]);
					return list;
				}
				if(x1<x2){
					for(;x1<=COLS-1;++x1){ //right
						list.Add(M.tile[y1,x1]);
					}
				}
				else{
					for(;x1>=0;--x1){ //left
						list.Add(M.tile[y1,x1]);
					}
				}
				return list;
			}
			if(dx==0){
				if(y1>y2){
					for(;y1>=0;--y1){ //up
						list.Add(M.tile[y1,x1]);
					}
				}
				else{
					for(;y1<=ROWS-1;++y1){ //down
						list.Add(M.tile[y1,x1]);
					}
				}
				return list;
			}
			if(y1+x1==y2+x2){ //slope is -1
				if(x1<x2){
					for(;x1<=COLS-1 && y1>=0;++x1){ //up-right
						list.Add(M.tile[y1,x1]);
						--y1;
					}
				}
				else{
					for(;x1>=0 && y1<=ROWS-1;--x1){ //down-left
						list.Add(M.tile[y1,x1]);
						++y1;
					}
				}
				return list;
			}
			if(y1-x1==y2-x2){ //slope is 1
				if(x1<x2){
					for(;x1<=COLS-1 && y1<=ROWS-1;++x1){ //down-right
						list.Add(M.tile[y1,x1]);
						++y1;
					}
				}
				else{
					for(;x1>=0 && y1>=0;--x1){ //up-left
						list.Add(M.tile[y1,x1]);
						--y1;
					}
				}
				return list;
			}
			if(y1<y2){ //down
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<=COLS-1 && y1<=ROWS-1;++x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						return list;
					}
					else{ //slope greater than 1
						for(;y1<=ROWS-1 && x1<=COLS-1;++y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						return list;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>=0 && y1<=ROWS-1;--x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						return list;
					}
					else{ //slope greater than 1
						for(;y1<=ROWS-1 && x1>=0;++y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						return list;
					}
				}
			}
			else{ //up
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<=COLS-1 && y1>=0;++x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						return list;
					}
					else{ //slope greater than 1
						for(;y1>=0 && x1<=COLS-1;--y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						return list;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>=0 && y1>=0;--x1){
							list.Add(M.tile[y1,x1]);
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						return list;
					}
					else{ //slope greater than 1
						for(;y1>=0 && x1>=0;--y1){
							list.Add(M.tile[y1,x1]);
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						return list;
					}
				}
			}
		}
		public bool HasLOE(PhysicalObject o){ return HasLOE(o.row,o.col); }
		public bool HasLOE(int r,int c){
			int y1 = row;
			int x1 = col;
			int y2 = r;
			int x2 = c;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			if(dx<=1 && dy<=1){ //everything adjacent
				return true;
			}
			if(HasBresenhamLineOfEffect(r,c)){ //basic LOE check
				return true;
			}
			if(!M.tile[r,c].passable){ //for walls, check nearby tiles
				foreach(Tile t in M.tile[r,c].NeighborsBetween(row,col)){
					if(HasBresenhamLineOfEffect(t.row,t.col)){
						return true;
					}
				}
			}
			return false;
		}
		public bool HasBresenhamLineOfEffect(int r,int c){
			List<Tile> line = GetBestLineOfEffect(r,c);
			int length = line.Count;
			if(length == 1){
				return true;
			}
			for(int i=1;i<length-1;++i){ //todo: experimentally changed i=0 to i=1, to skip the first tile.
				if(!line[i].passable){ // this should allow actors to fire out of solid tiles.
					return false;
				}
			}
			return true;
		}
		public List<Tile> GetBestLineOfEffect(PhysicalObject o){ return GetBestLineOfEffect(o.row,o.col); }
		public List<Tile> GetBestLineOfEffect(int r,int c){
			List<Tile> list = GetBresenhamLine(r,c);
			List<Tile> list2 = GetAlternateBresenhamLine(r,c);
			for(int i=0;i<list.Count;++i){
				if(!list2[i].passable){
					return list;
				}
				if(!list[i].passable){
					return list2;
				}
			}
			return list;
		}
		public List<Tile> GetBestExtendedLineOfEffect(PhysicalObject o){ return GetBestExtendedLineOfEffect(o.row,o.col); }
		public List<Tile> GetBestExtendedLineOfEffect(int r,int c){
			List<Tile> list = GetExtendedBresenhamLine(r,c);
			List<Tile> list2 = GetAlternateExtendedBresenhamLine(r,c);
			for(int i=0;i<list.Count;++i){
				if(!list2[i].passable){
					return list;
				}
				if(!list[i].passable){
					return list2;
				}
			}
			return list;
		}
	}
}

