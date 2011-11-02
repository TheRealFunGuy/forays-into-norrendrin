using System;
using System.Collections.Generic;
namespace Forays{
	public class PhysicalObject{
		public int row{get; protected set;}
		public int col{get; protected set;}
		public string name{get; protected set;}
		public string a_name{get; protected set;}
		public string the_name{get; protected set;}
		public char symbol{get; protected set;}
		public ConsoleColor color{get; protected set;}
		
		public static Map M{get;set;}
		public PhysicalObject(){
			row=-1;
			col=-1;
			name="";
			a_name="";
			the_name="";
			symbol='%';
			color=ConsoleColor.White;
		}
		public void Cursor(){
			Console.SetCursorPosition(col+Global.MAP_OFFSET_COLS,row+Global.MAP_OFFSET_ROWS);
		}
		public int DistanceFrom(PhysicalObject o){ return DistanceFrom(o.row,o.col); }
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
				return M.actor[row-1,col-1];
			case 8:
				return M.actor[row-1,col];
			case 9:
				return M.actor[row-1,col+1];
			case 4:
				return M.actor[row,col-1];
			case 5:
				return M.actor[row,col];
			case 6:
				return M.actor[row,col+1];
			case 1:
				return M.actor[row+1,col-1];
			case 2:
				return M.actor[row+1,col];
			case 3:
				return M.actor[row+1,col+1];
			default:
				return null;
			}
		}
		public Tile TileInDirection(int dir){
			switch(dir){
			case 7:
				return M.tile[row-1,col-1];
			case 8:
				return M.tile[row-1,col];
			case 9:
				return M.tile[row-1,col+1];
			case 4:
				return M.tile[row,col-1];
			case 5:
				return M.tile[row,col];
			case 6:
				return M.tile[row,col+1];
			case 1:
				return M.tile[row+1,col-1];
			case 2:
				return M.tile[row+1,col];
			case 3:
				return M.tile[row+1,col+1];
			default:
				return null;
			}
		}
		public Actor Actor(){
			return M.actor[row,col];
		}
		public Tile Tile(){
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

