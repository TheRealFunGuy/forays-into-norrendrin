using System;
using System.Collections.Generic;
using System.IO;
//
//hmm, about a 'connect these 2 rooms' method...
//start with a corridor coming from each one that doesn't hit anything (or nothing but corridors)
//the 2 endpoints of these corridors are your starting points. pick one at random.
//make another corridor from that point, either toward the other room, or in the y-direction of
// the other start point (assuming the rooms are at approximately the same y position)
/*
cEEEEc
E....E
E....E
E...1E
cEEEEc

			   
			   X

						cEEEc
						E...E
						E.2.E
						cEEEc
so, of course i'll need to pick proper 'r'oom tiles to avoid corner collisions. now, i think i can do
this with a weighted roll.
what if i start with 2 endpoints(1 and 2)...then i'll alternate between them, weighting it toward X.
so like, what about a 50% chance of automatically going toward X(whichever axis it's farther away on).
and maybe a 25% chance of going in a direction that isn't opposite of the first one.
to clarify: from 1, there'd be a 50% chance of direction 6, 25% of 8, and 25% of 2.
(or i could say screw this, and connect A to B by pathfinding around rooms and 'columns')*/


namespace DungeonGen{
	public class MainClass{
		static void Main(string[] args){
		//	Console.TreatControlCAsInput = true;
			Console.CursorVisible = false;
			StreamWriter file = new StreamWriter("dungeons.txt",true);
			Dungeon d = new Dungeon();
			bool done = false;
			int count = 1;
			bool show_converted = false;
			d.GenerateInitial();
			while(!done){
				if(show_converted){
					d.DrawConverted();
				}
				else{
					d.Draw();
				}
				Console.SetCursorPosition(20,22);
				Console.Write("generate 'C'orridor; generate 'R'oom; 'G'enerate room/corridor; remove 'D'iagonals;");
				Console.SetCursorPosition(20,23);
				Console.Write("remove 'U'nconnected; remove dead 'E'nds; re'J'ect map if floors < count;");
				Console.SetCursorPosition(20,24);
				Console.Write("1:toggle allow_all_corner_connections ("+d.allow_all_corner_connections+"); 2:toggle rooms_overwrite_corridors ("+d.rooms_overwrite_corridors+");  ");
				Console.SetCursorPosition(20,25);
				Console.Write("3:toggle show_converted ("+show_converted+"); reject ma'P' if too empty;  ");
				Console.SetCursorPosition(20,26);
				Console.Write("ESC: End program; 'S'ave to file; Z:Reset map; X:Clear map; choose cou'N't: " + count + "              ");
				ConsoleKeyInfo command = Console.ReadKey(true);
				switch(command.Key){
				case ConsoleKey.C:
					{
					for(int i=0;i<count;++i){
						d.GenerateCorridor(d.Roll(4));
					}
					break;
					}
				case ConsoleKey.R:
					{
					for(int i=0;i<count;++i){
						d.GenerateRoom();
					}
					break;
					}
				case ConsoleKey.G:
					{
					for(int i=0;i<count;++i){
						if(d.CoinFlip()){
							d.GenerateCorridor();
						}
						else{
							d.GenerateRoom();
						}
					}
					break;
					}
				case ConsoleKey.D:
					{
					d.RemoveDiagonals();
					break;
					}
				case ConsoleKey.U:
					{
					d.RemoveUnconnected();
					break;
					}
				case ConsoleKey.E:
					{
					d.RemoveDeadEnds();
					break;
					}
				case ConsoleKey.V:
					{
					d.Convert();
					break;
					}
				case ConsoleKey.J:
					{
					//if(d.NumberOfFloors() < count || d.HasLargeUnusedSpaces()){
					if(d.NumberOfFloors() < count){
						d.Clear();
					}
					break;
					}
				case ConsoleKey.P:
					{
					if(d.HasLargeUnusedSpaces()){
						d.Clear();
					}
					break;
					}
				case ConsoleKey.S:
					{
					string s;
					for(int i=0;i<Dungeon.H;++i){
						s = "";
						for(int j=0;j<Dungeon.W;++j){
							if(show_converted){
								s = s + d.ConvertedChar(d.map[i,j]);
							}
							else{
								s = s + d.map[i,j];
							}
						}
						file.WriteLine(s);
					}
					file.WriteLine();
					file.WriteLine();
					break;
					}
				case ConsoleKey.X:
					d.Clear();
					break;
				case ConsoleKey.Z:
					d.Clear();
					d.GenerateInitial();
					break;
				case ConsoleKey.N:
					{
					Console.SetCursorPosition(102,26);
					Console.CursorVisible = true;
					count = int.Parse(Console.ReadLine());
					Console.CursorVisible = false;
					break;
					}
				case ConsoleKey.D1:
					d.allow_all_corner_connections = !d.allow_all_corner_connections;
					break;
				case ConsoleKey.D2:
					d.rooms_overwrite_corridors = !d.rooms_overwrite_corridors;
					break;
				case ConsoleKey.D3:
					show_converted = !show_converted;
					break;
				case ConsoleKey.Escape:
					done = true;
					break;
				default:
					break;
				}
			}
			if(show_converted){
				d.DrawConverted();
			}
			else{
				d.Draw();
			}
			file.Close();
			Console.SetCursorPosition(0,28);
			Console.CursorVisible = true;
		}
	}
	public struct pos{
		public int r;
		public int c;
		public pos(int r_,int c_){ r = r_; c = c_; }
	}
	public class Dungeon{
		public const int H = 22;
		public const int W = 66;
		public char[,] map = new char[H,W];
		public Random r = new Random();
		public bool allow_all_corner_connections = false;
		public bool rooms_overwrite_corridors = true;
		public bool corridor_chains_overlap_themselves = false;
////////////////
public char[,] Generate(){
	while(true){
		GenerateInitial();
		RemoveDiagonals();
		RemoveDeadEnds();
		RemoveUnconnected();
		if(NumberOfFloors() < 320 || HasLargeUnusedSpaces()){
			Clear();
		}
		else{
			Convert();
			break;
		}
	}
	return map;
}
public Dungeon(){
	for(int i=0;i<H;++i){
		for(int j=0;j<W;++j){
			map[i,j] = '#';
		}
	}
}
public char Map(pos p){ return map[p.r,p.c]; }
public void Draw(){
	for(int i=0;i<H;++i){
		for(int j=0;j<W;++j){
			Console.SetCursorPosition(25+j,i);
			Console.Write(map[i,j]);
		}
	}
}
public void DrawConverted(){
	for(int i=0;i<H;++i){
		for(int j=0;j<W;++j){
			Console.SetCursorPosition(25+j,i);
			Console.Write(ConvertedChar(map[i,j]));
		}
	}
}
public int NumberOfFloors(){
	int total = 0;
	for(int i=0;i<H;++i){
		for(int j=0;j<W;++j){
			if(ConvertedChar(map[i,j]) == '.'){
				total++;
			}
		}
	}
	return total;
}
public void Clear(){
	for(int i=0;i<H;++i){
		for(int j=0;j<W;++j){
			map[i,j] = '#';
		}
	}
}
public bool HasLargeUnusedSpaces(){ //couldn't think of a good name
for(int i=1;i<H-1;++i){
	for(int j=1;j<W-1;++j){
		bool good = true;
		int width = -1;
		if(W-j-1 < 15){
			good = false;
		}
		else{
			for(int k=0;k<W-j-1;++k){
				if(ConvertedChar(map[i,j+k]) != '#'){
					if(k < 15){
						good = false;
					}
					break;
				}
				else{
					width = k+1;
				}
			}
		}
		for(int lines = 1;lines<H-i-1 && good;++lines){
			if(lines * width >= 300){
				return true;
			}
			for(int k=0;k<W-j-1;++k){
				if(ConvertedChar(map[i+lines,j+k]) != '#'){
					if(k < 15){
						good = false;
					}
					else{
						if(k+1 < width){
							width = k+1;
						}
					}
					break;
				}
			}
		}
	}
}
return false;
}
public void Convert(){
	for(int i=0;i<H;++i){
		for(int j=0;j<W;++j){
			switch(map[i,j]){
			case 'h':
			case 'v':
			case 'i':
			case 'E':
			case 'c':
			case 'r':
				map[i,j] = '.';
				break;
			}
		}
	}
}
public char ConvertedChar(char ch){
	switch(ch){
	case 'h':
	case 'v':
	case 'i':
	case 'E':
	case 'c':
	case 'r':
	case '.':
		return '.';
	case '#':
	default:
		return '#';
	}
}
public bool IsCorridor(char ch){
	switch(ch){
	case 'h':
	case 'v':
	case 'i':
		return true;
	default:
		return false;
	}
}
public int RotateDir(int dir,bool clockwise){ return RotateDir(dir,clockwise,1); }
public int RotateDir(int dir,bool clockwise,int times){
	if(dir == 5){ return 5; }
	for(int i=0;i<times;++i){
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
			return 0;
		}
	}
	return dir;
}
public pos PosInDir(int r,int c,int dir){ return PosInDir(new pos(r,c),dir); }
public pos PosInDir(pos p,int dir){
	switch(dir){
	case 7:
		return new pos(p.r-1,p.c-1);
	case 8:
		return new pos(p.r-1,p.c);
	case 9:
		return new pos(p.r-1,p.c+1);
	case 4:
		return new pos(p.r,p.c-1);
	case 5:
		return p;
	case 6:
		return new pos(p.r,p.c+1);
	case 1:
		return new pos(p.r+1,p.c-1);
	case 2:
		return new pos(p.r+1,p.c);
	case 3:
		return new pos(p.r+1,p.c+1);
	default:
		return new pos(-1,-1);
	}
}
public void RemoveDiagonals(){
	List<pos> walls = new List<pos>();
	for(int i=1;i<H-2;++i){
		for(int j=1;j<W-2;++j){
			if(ConvertedChar(map[i,j]) == '.' && ConvertedChar(map[i,j+1]) == '#'){
				if(ConvertedChar(map[i+1,j]) == '#' && ConvertedChar(map[i+1,j+1]) == '.'){
					walls.Add(new pos(i,j+1));
					walls.Add(new pos(i+1,j));
				}
			}
			if(ConvertedChar(map[i,j]) == '#' && ConvertedChar(map[i,j+1]) == '.'){
				if(ConvertedChar(map[i+1,j]) == '.' && ConvertedChar(map[i+1,j+1]) == '#'){
					walls.Add(new pos(i,j));
					walls.Add(new pos(i+1,j+1));
				}
			}
			while(walls.Count > 0){
				pos p = walls[Roll(walls.Count)-1];
				walls.Remove(p);
				char[] rotated = new char[8];
				for(int ii=0;ii<8;++ii){
					rotated[ii] = Map(PosInDir(p.r,p.c,RotateDir(8,true,ii)));
				}
				int successive_walls = 0;
				for(int ii=5;ii<8;++ii){
					if(ConvertedChar(rotated[ii]) == '#'){
						successive_walls++;
					}
					else{
						successive_walls = 0;
					}
				}
				for(int ii=0;ii<8;++ii){
					if(ConvertedChar(rotated[ii]) == '#'){
						successive_walls++;
					}
					else{
						successive_walls = 0;
					}
					if(successive_walls == 4){
						map[p.r,p.c] = 'i';
						if(IsLegal(p.r,p.c)){
							walls.Clear();
						}
						else{
							map[p.r,p.c] = '#';
						}
						break;
					}
				}
			}
		}
	}
}
public void RemoveDeadEnds(){
	bool changed = true;
	while(changed){
		changed = false;
		for(int i=0;i<H;++i){
			for(int j=0;j<W;++j){
				if(ConvertedChar(map[i,j]) == '.'){
					int total=0;
					if(ConvertedChar(map[i+1,j]) == '#'){ ++total; }
					if(ConvertedChar(map[i-1,j]) == '#'){ ++total; }
					if(ConvertedChar(map[i,j+1]) == '#'){ ++total; }
					if(ConvertedChar(map[i,j-1]) == '#'){ ++total; }
					if(total >= 3){
						map[i,j] = '#';
						changed = true;
					}
				}
			}
		}
	}
}
public void RemoveUnconnected(){
	int[,] num = new int[H,W];
	for(int i=0;i<H;++i){
		for(int j=0;j<W;++j){
			if(ConvertedChar(map[i,j]) == '.'){
				num[i,j] = 0;
			}
			else{
				num[i,j] = -1;
			}
		}
	}
	int count = 0;
	for(int i=0;i<H;++i){
		for(int j=0;j<W;++j){
			if(num[i,j] == 0){
				count++;
				num[i,j] = count;
				bool changed = true;
				while(changed){
					changed = false;
					for(int s=0;s<H;++s){
						for(int t=0;t<W;++t){
							if(num[s,t] == count){
								for(int ds=-1;ds<=1;++ds){
									for(int dt=-1;dt<=1;++dt){
										if(num[s+ds,t+dt] == 0){
											num[s+ds,t+dt] = count;
											changed = true;
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
	int biggest_area = -1;
	int size_of_biggest_area = 0;
	for(int k=1;k<=count;++k){
		int size = 0;
		for(int i=0;i<H;++i){
			for(int j=0;j<W;++j){
				if(num[i,j] == k){
					size++;
				}
			}
		}
		if(size > size_of_biggest_area){
			size_of_biggest_area = size;
			biggest_area = k;
		}
	}
	for(int i=0;i<H;++i){
		for(int j=0;j<W;++j){
			if(num[i,j] != biggest_area){
				map[i,j] = '#';
			}
		}
	}
}
public bool BoundsCheck(int r,int c){
	if(r>0 && r<H-1 && c>0 && c<W-1){
		return true;
	}
	return false;
}
public bool IsLegal(int r,int c){
	if(r == 0 || r == H-1 || c == 0 || c == W-1){
		return true;
	}
	bool result = true;
	switch(map[r,c]){
	case 'r': //no special rules yet. actually must be surrounded by {E,r,c}
		break;
	case 'E':
		{ //if i implement multi-part rooms, i'll simply add an 'internal corner' tile
		int roomdir = 0; //so that edges will STILL always border 'c'/'E'/'I'
		if(map[r-1,c] == 'r'){ //or 'n' instead of 'I', or whatever.
			roomdir = 8;
		}
		if(map[r+1,c] == 'r'){
			roomdir = 2;
		}
		if(map[r,c-1] == 'r'){
			roomdir = 4;
		}
		if(map[r,c+1] == 'r'){
			roomdir = 6;
		}
		if(roomdir == 0){
			return false; //no room found, error
		}
		char[] rotated = new char[8];
		rotated[0] = 'r';
		for(int i=1;i<8;++i){
			rotated[i] = Map(PosInDir(r,c,RotateDir(roomdir,true,i)));
		}
		if(rotated[1] != 'r' && rotated[1] != 'E'){ return false; }
		if(rotated[7] != 'r' && rotated[7] != 'E'){ return false; }
		if(rotated[2] != 'c' && rotated[2] != 'E'){ return false; }
		if(rotated[6] != 'c' && rotated[6] != 'E'){ return false; }
		if(!(rotated[4] == '#' || (rotated[3] == '#' && rotated[5] == '#'))){ return false; }
		break;
		}
	case 'c':
		{
		int roomdir = 0; 
		if(map[r-1,c-1] == 'r'){
			roomdir = 7;
		}
		if(map[r-1,c+1] == 'r'){
			roomdir = 9;
		}
		if(map[r+1,c-1] == 'r'){
			roomdir = 1;
		}
		if(map[r+1,c+1] == 'r'){
			roomdir = 3;
		}
		if(roomdir == 0){
			return false; //no room found, error
		}
		char[] rotated = new char[8];
		rotated[0] = 'r';
		for(int i=1;i<8;++i){
			rotated[i] = Map(PosInDir(r,c,RotateDir(roomdir,true,i)));
		}
		if(rotated[1] != 'E'){ return false; }
		if(rotated[7] != 'E'){ return false; }
		if(allow_all_corner_connections){
			if(rotated[2] != '#' && rotated[3] != '#'){ return false; }
			if(rotated[6] != '#' && rotated[5] != '#'){ return false; }
			if(rotated[4] != '#'){
				if(rotated[3] != '#' && rotated[5] != '#'){ return false; }
				if(rotated[3] == '#' && rotated[5] == '#'){ return false; }
			}
		}
		else{
			if(rotated[3] != '#' || rotated[4] != '#' || rotated[5] != '#'){
				return false;
			}
		}
		break;
		}
	case 'i':
		{
		char[] rotated = new char[8];
		for(int i=0;i<8;++i){
			rotated[i] = Map(PosInDir(r,c,RotateDir(8,true,i)));
		}
		int successive_floors = 0;
		if(ConvertedChar(rotated[6]) == '.'){
			successive_floors++;
		}
		if(ConvertedChar(rotated[7]) == '.'){
			successive_floors++;
		}
		else{
			successive_floors = 0;
		}
		for(int i=0;i<8;++i){
			if(ConvertedChar(rotated[i]) == '.'){
				successive_floors++;
			}
			else{
				successive_floors = 0;
			}
			if(successive_floors == 3){
				return false;
			}
		}
		break;
		}
	case 'X': //in case there is a need to block off an area
		for(int i=1;i<=8;++i){
			int dir = i;
			if(dir == 5){ dir = 9; }
			if(ConvertedChar(Map(PosInDir(r,c,dir))) != '#'){
				return false;
			}
		}
		break;
	default:
		break;
	}
	return result;
}
public int Roll(int dice,int sides){
	int total = 0;
	for(int i=0;i<dice;++i){
		total += r.Next(1,sides+1);
	}
	return total;
}
public int Roll(int sides){
	return r.Next(1,sides+1);
}
public bool CoinFlip(){
	return r.Next(1,3) == 2;
}
public bool GenerateRoom(){ return GenerateRoom(Roll(H-2),Roll(W-2)); }
public bool GenerateRoom(int rr,int rc){
	int dir = (Roll(4)*2)-1;
	if(dir == 5){ dir = 9; }
	return GenerateRoom(rr,rc,dir);
}
public bool GenerateRoom(int rr,int rc,int dir){
	//int height = Roll(4) + Roll(2,2);
	//int width = Roll(2,3) + Roll(2,2);
	int height = Roll(6)+2;
	int width = Roll(8)+2;
	int h_offset = 0;
	int w_offset = 0;
	if(height % 2 == 0){
		h_offset = Roll(2) - 1;
	}
	if(width % 2 == 0){
		w_offset = Roll(2) - 1;
	}
	switch(dir){
	case 7:
		rr -= height-1;
		rc -= width-1;
		break;
	case 9:
		rr -= height-1;
		break;
	case 1:
		rc -= width-1;
		break;
	case 8:
		rr -= height-1;
		rc -= (width/2) - w_offset;
		break;
	case 2:
		rc -= (width/2) - w_offset;
		break;
	case 4:
		rr -= (height/2) - h_offset;
		rc -= width-1;
		break;
	case 6:
		rr -= (height/2) - h_offset;
		break;
	}
	dir = 3; //does nothing at the moment
	bool inbounds = true;
	for(int i=rr;i<rr+height && inbounds;++i){
		for(int j=rc;j<rc+width;++j){
			if(!BoundsCheck(i,j)){
				inbounds = false;
				break;
			}
		}
	}
	if(inbounds){
		char[,] submap = new char[height,width];
		for(int i=0;i<height;++i){
			for(int j=0;j<width;++j){
				submap[i,j] = map[i+rr,j+rc];
			}
		}
		bool good = true;
		for(int i=0;i<height && good;++i){
			for(int j=0;j<width && good;++j){
				bool place_here = false;
				switch(map[i+rr,j+rc]){
				case 'h':
				case 'v':
				case 'i':
					if(rooms_overwrite_corridors){
						place_here = true;
					}
					else{
						good = false;
					}
					break;
				case 'E':
				case 'c':
				case 'r':
					good = false;
					break;
				case 'X':
					good = false;
					break;
				default:
					place_here = true;
					break;
				}
				if(place_here){
					int total = 0;
					if(i == 0){ ++total; }
					if(i == height-1){ ++total; }
					if(j == 0){ ++total; }
					if(j == width-1){ ++total; }
					switch(total){
					case 0:
						map[i+rr,j+rc] = 'r';
						break;
					case 1:
						map[i+rr,j+rc] = 'E';
						break;
					case 2:
						map[i+rr,j+rc] = 'c';
						break;
					default:
						map[i+rr,j+rc] = '$'; //error
						break;
					}
				}
			}
		}
		for(int i=-1;i<height+1 && good;++i){ 
			for(int j=-1;j<width+1 && good;++j){
				if(!IsLegal(i+rr,j+rc)){
					good = false;
				}
			}
		}
//Draw();
//Console.ReadKey(true);
		if(!good){ //if this addition is illegal...
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					map[i+rr,j+rc] = submap[i,j];
				}
			}
		}
		else{
			return true;
		}
	}
	return false;
}
public bool GenerateCorridor(){ return GenerateCorridor(Roll(H-2),Roll(W-2),1,Roll(4)*2); }
public bool GenerateCorridor(int count){ return GenerateCorridor(Roll(H-2),Roll(W-2),count,Roll(4)*2); }
public bool GenerateCorridor(int rr,int rc){ return GenerateCorridor(rr,rc,1,Roll(4)*2); }
public bool GenerateCorridor(int rr,int rc,int count){ return GenerateCorridor(rr,rc,count,Roll(4)*2); }
public bool GenerateCorridor(int rr,int rc,int count,int dir){
	bool result = false;
	pos endpoint = new pos(rr,rc);
	pos potential_endpoint;
	List<pos> chain = null;
	if(count > 1){
		chain = new List<pos>();
	}
	int tries = 0;
	while(count > 0 && tries < 100){ //assume there's no room for a corridor if it fails 25 times in a row
////
	tries++;
	rr = endpoint.r;
	rc = endpoint.c;
	potential_endpoint = endpoint;
	if(chain != null && chain.Count > 0){ //reroll direction ONLY after the first part of the chain.
		dir = Roll(4)*2;
	}
	int length = Roll(5)+2;
	//int length = Roll(5) + Roll(6) + 1;
	//int length = Roll(15)+2;
	//int length = (Roll(2)-1)*Roll(2,3) + Roll(10) + 2;
	if(CoinFlip()){ length += 8; }
	switch(dir){
	case 8: //make them all point either down..
		dir = 2;
		rr -= length-1;
		potential_endpoint.r = rr;
		break;
	case 2:
		potential_endpoint.r += length-1;
		break;
	case 4: //..or right
		dir = 6;
		rc -= length-1;
		potential_endpoint.c = rc;
		break;
	case 6:
		potential_endpoint.c += length-1;
		break;
	}
	switch(dir){
	case 2:
		{
		bool valid_position = true;
		for(int i=rr;i<rr+length;++i){
			if(!BoundsCheck(i,rc)){
				valid_position = false;
				break;
			}
			if(corridor_chains_overlap_themselves == false){
				if(chain != null && chain.Count > 0 && i != endpoint.r && chain.Contains(new pos(i,rc))){
					valid_position = false;
					break;
				}
			}
		}
		if(valid_position){
			char[] submap = new char[length+2];
			for(int i=0;i<length+2;++i){
				submap[i] = map[i+rr-1,rc];
			}
			bool good = true;
			for(int i=0;i<length;++i){
				if(map[i+rr,rc] == 'h' || map[i+rr,rc-1] == 'h' || map[i+rr,rc+1] == 'h'){
					map[i+rr,rc] = 'i';
				}
				else{
					switch(map[i+rr,rc]){
					case 'i':
					case 'E':
					case 'r':
						break;
					case 'c':
						if(allow_all_corner_connections == false){
							good = false;
						}
						break;
					case 'X':
						good = false;
						break;
					default:
						map[i+rr,rc] = 'v';
						break;
					}
				}
			}
			if(good && map[rr-1,rc] == 'h'){ map[rr-1,rc] = 'i'; }
			if(good && map[rr+length,rc] == 'h'){ map[rr+length,rc] = 'i'; }
			for(int i=rr-1;i<rr+length+1 && good;++i){ //note that it doesn't check the bottom or right, since
				for(int j=rc-1;j<rc+2;++j){ //they are checked by the others
					if(i != rr+length && j != rc+1){
						if(ConvertedChar(map[i,j]) == '.'){
							if(ConvertedChar(map[i,j+1]) == '.'
								&& ConvertedChar(map[i+1,j]) == '.'
								&& ConvertedChar(map[i+1,j+1]) == '.'){
								good = false;
								break;
							}
						}
					}
					if(!IsLegal(i,j)){
						good = false;
						break;
					}
				}
			}
/*Draw();
if(chain != null){
	foreach(pos p in chain){
		Console.SetCursorPosition(25+p.c,p.r);
		if(ConvertedChar(map[p.r,p.c]) == '.'){
			Console.Write('X');
		}
		else{
			Console.Write('x');
		}
	}
}
Console.ReadKey(true);*/
			if(!good){ //if this addition is illegal...
				for(int i=0;i<length+2;++i){
					map[i+rr-1,rc] = submap[i];
				}
			}
			else{
				count--;
				tries = 0;
				if(chain != null){
					if(chain.Count == 0){
						chain.Add(endpoint);
					}
					for(int i=rr;i<rr+length;++i){
						pos p = new pos(i,rc);
						if(!(p.Equals(endpoint))){
							chain.Add(p);
						}
					}
				}
				endpoint = potential_endpoint;
				result = true;
			}
		}
		break;
		}
	case 6:
		{
		bool valid_position = true;
		for(int j=rc;j<rc+length;++j){
			if(!BoundsCheck(rr,j)){
				valid_position = false;
				break;
			}
			if(corridor_chains_overlap_themselves == false){
				if(chain != null && chain.Count > 0 && j != endpoint.c && chain.Contains(new pos(rr,j))){
					valid_position = false;
					break;
				}
			}
		}
		if(valid_position){
			char[] submap = new char[length+2];
			for(int j=0;j<length+2;++j){
				submap[j] = map[rr,j+rc-1];
			}
			bool good = true;
			for(int j=0;j<length;++j){
				if(map[rr,j+rc] == 'v' || map[rr-1,j+rc] == 'v' || map[rr+1,j+rc] == 'v'){
					map[rr,j+rc] = 'i';
				}
				else{
					switch(map[rr,j+rc]){
					case 'i':
					case 'E':
					case 'r':
						break;
					case 'c':
						if(allow_all_corner_connections == false){
							good = false;
						}
						break;
					case 'X':
						good = false;
						break;
					default:
						map[rr,j+rc] = 'h';
						break;
					}
				}
			}
			if(good && map[rr,rc-1] == 'v'){ map[rr,rc-1] = 'i'; }
			if(good && map[rr,rc+length] == 'v'){ map[rr,rc+length] = 'i'; }
			for(int i=rr-1;i<rr+2 && good;++i){ //note that it doesn't check the bottom or right, since
				for(int j=rc-1;j<rc+length+1;++j){ //they are checked by the others
					if(i != rr+1 && j != rc+length){
						if(IsCorridor(map[i,j])){
							if(IsCorridor(map[i,j+1])
								&& IsCorridor(map[i+1,j])
								&& IsCorridor(map[i+1,j+1])){
								good = false;
								break;
							}
						}
					}
					if(!IsLegal(i,j)){
						good = false;
						break;
					}
				}
			}
/*Draw();
if(chain != null){
	foreach(pos p in chain){
		Console.SetCursorPosition(25+p.c,p.r);
		if(ConvertedChar(map[p.r,p.c]) == '.'){
			Console.Write('X');
		}
		else{
			Console.Write('x');
		}
	}
}
Console.ReadKey(true);*/
			if(!good){ //if this addition is illegal...
				for(int j=0;j<length+2;++j){
					map[rr,j+rc-1] = submap[j];
				}
			}
			else{
				count--;
				tries = 0;
				if(chain != null){
					if(chain.Count == 0){
						chain.Add(endpoint);
					}
					for(int j=rc;j<rc+length;++j){
						pos p = new pos(rr,j);
						if(!(p.Equals(endpoint))){
							chain.Add(p);
						}
					}
				}
				endpoint = potential_endpoint;
				result = true;
			}
		}
		break;
		}
	}
////
	}
	return result;
}
public void GenerateInitial(){
////
//StreamWriter file;
//try{
	//file = new StreamWriter("debugoutput.txt");
	for(int i=20;i<20;++i){
		GenerateRoom();
	}
	for(int i=250;i<250;++i){
		GenerateCorridor(Roll(4));
	}
	int pointrows = 2;
	int pointcols = 4;
	List<pos> points = new List<pos>();
	for(int i=1;i<=pointrows;++i){
		for(int j=1;j<=pointcols;++j){
			points.Add(new pos((H*i)/(pointrows+1),(W*j)/(pointcols+1)));
		}
	}
	foreach(pos p in points){
		map[p.r,p.c] = 'X';
	}
	bool corners = false;
	for(int remaining=Roll(4);points.Count > remaining || !corners;){
		pos p = points[Roll(points.Count)-1];
		map[p.r,p.c] = '#'; //remove the X
		while(!GenerateRoom(p.r,p.c)){ }
		points.Remove(p);
		if(points.Contains(new pos(H/(pointrows+1),W/(pointcols+1))) == false
		&& points.Contains(new pos((H*pointrows)/(pointrows+1),(W*pointcols)/(pointcols+1))) == false){
			corners = true;
		}
		if(points.Contains(new pos(H/(pointrows+1),(W*pointcols)/(pointcols+1))) == false
		&& points.Contains(new pos((H*pointrows)/(pointrows+1),W/(pointcols+1))) == false){
			corners = true;
		}
		/*foreach(pos point in points){
			Console.SetCursorPosition(25+point.c,point.r);
			Console.Write("@");
			Console.SetCursorPosition(25+point.c,point.r);
			Console.ReadKey(true);
			Console.Write('X');
		}*/
	}
	for(int count=100;count<200;++count){
		int rr = -1;
		int rc = -1;
		int dir = 0;
		for(int i=0;i<9999 && dir == 0;++i){
			rr = Roll(H-4) + 1;
			rc = Roll(W-4) + 1;
			if(map[rr,rc] == '#'){
				int total = 0;
				int lastdir = 0;
				if(ConvertedChar(map[rr-1,rc]) == '.'){ ++total; lastdir = 8; }
				if(ConvertedChar(map[rr+1,rc]) == '.'){ ++total; lastdir = 2; }
				if(ConvertedChar(map[rr,rc-1]) == '.'){ ++total; lastdir = 4; }
				if(ConvertedChar(map[rr,rc+1]) == '.'){ ++total; lastdir = 6; }
				if(total == 1){
					dir = lastdir;
				}
			}
		}
		if(dir != 0){
			bool connecting_to_room = false;
			if(ConvertedChar(Map(PosInDir(PosInDir(rr,rc,dir),dir))) == '.'){
				for(int s=0;s<2;++s){
					bool clockwise = (s==0)? false : true;
					if(ConvertedChar(Map(PosInDir(PosInDir(rr,rc,dir),RotateDir(dir,clockwise)))) == '.'
					&& ConvertedChar(Map(PosInDir(rr,rc,RotateDir(dir,clockwise)))) == '.'){
						connecting_to_room = true;
					}
				}
			}
			int extra_chance_of_corridor = 0;
			if(connecting_to_room){
				extra_chance_of_corridor = 6;
			}
			if(Roll(1,10)+extra_chance_of_corridor > 7){ //corridor
				GenerateCorridor(rr,rc,Roll(4),dir);
			}
			else{
				GenerateRoom(rr,rc,dir);
			}
		}
	}
//}
//finally{
//	file.Close();
//}
////
}
////////////////
	}
}
