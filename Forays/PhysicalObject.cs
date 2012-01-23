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
		public int row{get;set;}
		public int col{get;set;}
		public string name{get;set;}
		public string a_name{get;set;}
		public string the_name{get;set;}
		public char symbol{get;set;}
		public Color color{get;set;}
		
		public static Map M{get;set;}
		public PhysicalObject(){
			row=-1;
			col=-1;
			name="";
			a_name="";
			the_name="";
			symbol='%';
			color=Color.White;
		}
		public void Cursor(){
			Console.SetCursorPosition(col+Global.MAP_OFFSET_COLS,row+Global.MAP_OFFSET_ROWS);
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
		public int RotateDirection(int dir,bool clockwise){ return RotateDirection(dir,clockwise,1); }
		public int RotateDirection(int dir,bool clockwise,int num){
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
		public int DirectionOf(PhysicalObject obj){
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
		public bool HasBresenhamLine(int r,int c){
			List<Tile> line = GetBresenhamLine(r,c);
			int length = line.Count;
			if(length == 1){
				return true;
			}
			for(int i=0;i<length-1;++i){
				if(line[i].opaque){
					return false;
				}
			}
			return true;
		}
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
	}
}

