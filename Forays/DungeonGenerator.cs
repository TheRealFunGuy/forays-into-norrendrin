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
			StandardDungeon d = new StandardDungeon();
			bool done = false;
			int count = 1;
			bool show_converted = false;
			d.CreateBasicMap();
			while(!done){
				if(show_converted){
					d.DrawConverted();
				}
				else{
					d.Draw();
				}
				//Console.SetCursorPosition(0,22);
				//Console.Write("generate 'C'orridor; generate 'R'oom; 'G'enerate room/corridor; remove 'D'iagonals;");
				//Console.SetCursorPosition(0,23);
				//Console.Write("remove 'U'nconnected; remove dead 'E'nds; re'J'ect map if floors < count;");
				//Console.SetCursorPosition(0,24);
				//Console.Write("1:toggle allow_all_corner_connections ("+d.allow_all_corner_connections+"); 2:toggle rooms_overwrite_corridors ("+d.rooms_overwrite_corridors+");  ");
				//Console.SetCursorPosition(0,25);
				//Console.Write("3:toggle show_converted ("+show_converted+"); reject ma'P' if too empty;  ");
				//Console.SetCursorPosition(0,26);
				//Console.Write("ESC: End program; 'S'ave to file; Z:Reset map; X:Clear map; choose cou'N't: " + count + "              ");
				Console.SetCursorPosition(67,0);
				Console.Write("q: corridor");
				Console.SetCursorPosition(67,1);
				Console.Write("w: room");
				Console.SetCursorPosition(67,2);
				Console.Write("e: room / cor");
				Console.SetCursorPosition(67,4);
				if(d.allow_all_corner_connections){
					Console.ForegroundColor = ConsoleColor.Green;
				}
				else{
					Console.ForegroundColor = ConsoleColor.Red;
				}
				Console.Write("1: corner");
				Console.SetCursorPosition(67,5);
				Console.Write(" connections?");
				Console.SetCursorPosition(67,6);
				if(d.rooms_overwrite_corridors){
					Console.ForegroundColor = ConsoleColor.Green;
				}
				else{
					Console.ForegroundColor = ConsoleColor.Red;
				}
				Console.Write("2: rooms");
				Console.SetCursorPosition(67,7);
				Console.Write(" overwrite");
				Console.SetCursorPosition(67,8);
				Console.Write(" corridors?");
				Console.SetCursorPosition(67,9);
				if(show_converted){
					Console.ForegroundColor = ConsoleColor.Green;
				}
				else{
					Console.ForegroundColor = ConsoleColor.Red;
				}
				Console.Write("3: display");
				Console.SetCursorPosition(67,10);
				Console.Write(" converted?");
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.SetCursorPosition(67,12);
				Console.Write("a: remove");
				Console.SetCursorPosition(67,13);
				Console.Write(" diagonals");
				Console.SetCursorPosition(67,14);
				Console.Write("s: remove");
				Console.SetCursorPosition(67,15);
				Console.Write(" unconnected");
				Console.SetCursorPosition(67,16);
				Console.Write("d: remove");
				Console.SetCursorPosition(67,17);
				Console.Write(" dead ends");
				Console.SetCursorPosition(67,19);
				Console.Write("z: reset map");
				Console.SetCursorPosition(67,20);
				Console.Write("x: fill map");
				Console.SetCursorPosition(67,21);
				Console.Write(" with walls");
				Console.SetCursorPosition(0,22);
				Console.Write("  c: reject map if too empty   v: reject map if floors < count");
				Console.SetCursorPosition(0,23);
				Console.Write("  ESC: end program   b: save to file   n: choose count: " + count + "     ");
				Console.SetCursorPosition(0,24);
				Console.Write("  Rooms: hjkl; Corridors: 67890;   {0} {1} {2} {3} ;  {4} {5} {6} {7} {8}    ",d.room_height_min,d.room_height_max,d.room_width_min,d.room_width_max,
				              d.corridor_length_min,d.corridor_length_max,d.corridor_chain_length_min,d.corridor_chain_length_max,d.corridor_length_addition);
				ConsoleKeyInfo command = Console.ReadKey(true);
				switch(command.Key){
				case ConsoleKey.Q:
					{
					for(int i=0;i<count;++i){
						d.CreateCorridor(Dungeon.Roll(d.corridor_chain_length_max - (d.corridor_chain_length_min-1)) + (d.corridor_chain_length_min-1));
					}
					break;
					}
				case ConsoleKey.W:
					{
					for(int i=0;i<count;++i){
						d.CreateRoom();
					}
					break;
					}
				case ConsoleKey.E:
				{
					for(int i=0;i<count;++i){
						if(Dungeon.CoinFlip()){
							d.CreateCorridor(Dungeon.Roll(d.corridor_chain_length_max - (d.corridor_chain_length_min-1)) + (d.corridor_chain_length_min-1));
						}
						else{
							d.CreateRoom();
						}
					}
					break;
				}
				case ConsoleKey.T:
					d.Convert();
					break;
				case ConsoleKey.Y:
					d.ConvertToShowFloorType();
					break;
				case ConsoleKey.A:
					{
					d.RemoveDiagonals();
					break;
					}
				case ConsoleKey.S:
					{
					d.RemoveUnconnected();
					break;
					}
				case ConsoleKey.D:
					{
					d.RemoveDeadEnds();
					break;
					}
				case ConsoleKey.V:
					{
					//if(d.NumberOfFloors() < count || d.HasLargeUnusedSpaces()){
					if(d.NumberOfFloors() < count){
						d.Clear();
					}
					break;
					}
				case ConsoleKey.C:
					{
					if(d.HasLargeUnusedSpaces()){
						d.Clear();
					}
					break;
					}
				case ConsoleKey.B:
					{
					string s;
					for(int i=0;i<StandardDungeon.H;++i){
						s = "";
						for(int j=0;j<StandardDungeon.W;++j){
							if(show_converted){
								s = s + StandardDungeon.ConvertedChar(d.map[i,j]);
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
					d.CreateBasicMap();
					break;
				case ConsoleKey.N:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						count = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.O:
					d.CreateRandomWalls(count);
					break;
				case ConsoleKey.P:
					d.ApplyCellularAutomataFourFiveRule();
					break;
				case ConsoleKey.I:
					d.ApplyCellularAutomataXYRule(3);
					break;
				case ConsoleKey.G:
					d.ApplyCaveModification();
					break;
				case ConsoleKey.F:
					d.AddPillars(count);
					break;
				case ConsoleKey.M:
					d.MarkInterestingLocations();
					break;
				case ConsoleKey.R:
					switch(count % 10){
					case 1:
						d.Reflect(true,false);
						break;
					case 2:
						d.Reflect(false,true);
						break;
					case 3:
						d.Reflect(true,true);
						break;
					}
					break;
				case ConsoleKey.H:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						d.room_height_min = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.J:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						d.room_height_max = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.K:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						d.room_width_min = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.L:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						d.room_width_max = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.D6:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						d.corridor_length_min = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.D7:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						d.corridor_length_max = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.D8:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						d.corridor_chain_length_min = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.D9:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						d.corridor_chain_length_max = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.D0:
				{
					Console.SetCursorPosition(56,23);
					Console.Write("          ");
					Console.SetCursorPosition(56,23);
					Console.CursorVisible = true;
					try{
						d.corridor_length_addition = int.Parse(Console.ReadLine());
					}
					catch(System.FormatException){
						//do nothing
					}
					Console.CursorVisible = false;
					break;
				}
				case ConsoleKey.U:
					d.AddLake();
					break;
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
			Console.SetCursorPosition(0,21);
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
		public static Random r = new Random();
		public Dungeon(){
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					map[i,j] = '#';
				}
			}
		}
		public char Map(pos p){ return map[p.r,p.c]; }
		public static int RotateDir(int dir,bool clockwise){ return RotateDir(dir,clockwise,1); }
		public static int RotateDir(int dir,bool clockwise,int times){
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
		public static int EstimatedEuclideanDistanceFromX10(pos p1,pos p2){ return EstimatedEuclideanDistanceFromX10(p1.r,p1.c,p2.r,p2.c); }
		public static int EstimatedEuclideanDistanceFromX10(int r1,int c1,int r2,int c2){ // x10 so that orthogonal directions are closer than diagonals
			int dy = Math.Abs(r1-r2) * 10;
			int dx = Math.Abs(c1-c2) * 10;
			if(dx > dy){
				return dx + (dy/2);
			}
			else{
				return dy + (dx/2);
			}
		}
		public static pos PosInDir(int r,int c,int dir){ return PosInDir(new pos(r,c),dir); }
		public static pos PosInDir(pos p,int dir){
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
		public bool BoundsCheck(int r,int c){
			if(r>0 && r<H-1 && c>0 && c<W-1){
				return true;
			}
			return false;
		}
		public static bool BoundsCheck(int r,int c,int H,int W){
			if(r>0 && r<H-1 && c>0 && c<W-1){
				return true;
			}
			return false;
		}
		public static int Roll(int dice,int sides){
			int total = 0;
			for(int i=0;i<dice;++i){
				total += r.Next(1,sides+1);
			}
			return total;
		}
		public static int Roll(int sides){
			return r.Next(1,sides+1);
		}
		public static bool CoinFlip(){
			return r.Next(1,3) == 2;
		}
	}
	public class StandardDungeon : Dungeon{
		/*public const int H = 22;
		public const int W = 66;
		public char[,] map = new char[H,W];
		public Random r = new Random();*/
		public bool allow_all_corner_connections = false;
		public bool rooms_overwrite_corridors = true;
		//public bool corridor_chains_overlap_themselves = false;
		public bool rooms_over_rooms = false;
		public int room_height_min = 3;
		public int room_height_max = 8;
		public int room_width_min = 3;
		public int room_width_max = 10;
		public int corridor_length_min = 3;
		public int corridor_length_max = 7; //note that this might not be the actual max: corridor_length_addition is added half the time.
		public int corridor_length_addition = 8;
		public int corridor_chain_length_min = 1;
		public int corridor_chain_length_max = 4;
		public char[,] GenerateStandard(){
			ResetToDefaults();
			while(true){
				CreateBasicMap();
				RemoveDiagonals();
				RemoveDeadEnds();
				RemoveUnconnected();
				AddDoors();
				AddPillars(30);
				/*int num_lakes = Roll(3);
				for(int i=0;i<num_lakes;++i){
					AddLake();
				}*/
				MarkInterestingLocations();
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
		public char[,] GenerateCave(){
			ResetToDefaults();
			while(true){
				CreateRandomWalls(25);
				ApplyCellularAutomataXYRule(3);
				RemoveDiagonals();
				RemoveDeadEnds();
				RemoveUnconnected();
				AddFirePits();
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
		public char[,] GenerateRuined(){
			ResetToDefaults();
			while(true){
				CreateBasicMap();
				RemoveDiagonals();
				RemoveDeadEnds();
				RemoveUnconnected();
				AddDoors();
				AddPillars(30);
				MarkInterestingLocations();
				if(NumberOfFloors() < 320 || HasLargeUnusedSpaces()){
					Clear();
				}
				else{
					for(int i=0;i<5;++i){
						ApplyRuin();
					}
					Convert();
					break;
				}
			}
			return map;
		}
		public char[,] GenerateHive(){
			ResetToDefaults();
			room_height_max = 3;
			room_width_max = 3;
			corridor_length_min = 4;
			corridor_length_max = 4;
			corridor_length_addition = 4;
			while(true){
				CreateBasicMap();
				for(int i=0;i<700;++i){
					if(CoinFlip()){
						CreateCorridor(Roll(corridor_chain_length_max - (corridor_chain_length_min-1)) + (corridor_chain_length_min-1));
					}
					else{
						CreateRoom();
					}
				}
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
		public char[,] GenerateMine(){
			ResetToDefaults();
			room_height_min = 4;
			room_height_max = 10;
			room_width_min = 4;
			room_width_max = 12;
			while(true){
				CreateBasicMap();
				RemoveUnconnected();
				if(!ApplyCaveModification()){
					Clear();
					continue;
				}
				RemoveDiagonals();
				RemoveUnconnected();
				AddFirePits();
				MarkInterestingLocations();
				if(NumberOfFloors() < 420 || HasLargeUnusedSpaces()){
					Clear();
				}
				else{
					if(Roll(10) == 10){
						ApplyRuin();
					}
					Convert();
					break;
				}
			}
			return map;
		}
		public char[,] GenerateFortress(){
			ResetToDefaults();
			while(true){ // it could require a certain number of room tiles if corridor-heavy fortresses are too common
				for(int i=H/2-1;i<H/2+1;++i){
					for(int j=1;j<W-1;++j){
						if(j==1 || j==W-2){
							map[i,j] = 'c';
						}
						else{
							map[i,j] = 'E';
						}
					}
				}
				for(int i=0;i<700;++i){
					if(Roll(5) == 5){
						CreateCorridor(Roll(corridor_chain_length_max - (corridor_chain_length_min-1)) + (corridor_chain_length_min-1));
					}
					else{
						CreateRoom();
					}
				}
				bool reflect_features = Roll(5) <= 4;
				if(reflect_features){
					AddDoors();
					AddPillars(30);
				}
				Reflect(true,false);
				RemoveDiagonals();
				RemoveDeadEnds();
				RemoveUnconnected();
				if(!reflect_features){
					AddDoors();
					AddPillars(30);
				}
				bool door_right = false;
				bool door_left = false;
				int rightmost_door = 0;
				int leftmost_door = 999;
				for(int j=0;j<22;++j){
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
				}
				MarkInterestingLocations();
				if(NumberOfFloors() < 420 || HasLargeUnusedSpaces()){
					Clear();
				}
				else{
					/*for(int i=0;i<H;++i){
						for(int j=0;j<W;++j){
							Forays.Screen.WriteMapChar(i,j,map[i,j]);
						}
					}
					Console.ReadKey(true);*/
					Convert();
					break;
				}
			}
			return map;
		}
		public char[,] GenerateExtravagant(){
			ResetToDefaults();
			while(true){
				CreateBasicMap();
				RemoveDiagonals();
				RemoveDeadEnds();
				RemoveUnconnected();
				AddDoors();
				AddPillars(100);
				MarkInterestingLocations();
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
		public StandardDungeon(){
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					map[i,j] = '#';
				}
			}
		}
		public void ResetToDefaults(){
			allow_all_corner_connections = false;
			rooms_overwrite_corridors = true;
			rooms_over_rooms = false;
			room_height_min = 3;
			room_height_max = 8;
			room_width_min = 3;
			room_width_max = 10;
			corridor_length_min = 3;
			corridor_length_max = 7;
			corridor_length_addition = 8;
			corridor_chain_length_min = 1;
			corridor_chain_length_max = 4;
		}
		//public char Map(pos p){ return map[p.r,p.c]; }
		public void Draw(){
			Console.CursorVisible = false;
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					Console.SetCursorPosition(j,i);
					Console.Write(map[i,j]);
				}
			}
		}
		public void DrawConverted(){
			Console.CursorVisible = false;
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					Console.SetCursorPosition(j,i);
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
		public bool HasLargeUnusedSpaces(){
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
					case 'h': //horizontal corridor
					case 'v': //vertical corridor
					case 'i': //corridor intersection
					case 'E': //room edge
					case 'c': //room corner
					case 'N': //internal room corner
					case 'r': //room
						map[i,j] = '.';
						break;
					case 'P':
						map[i,j] = '#';
						break;
					}
				}
			}
		}
		public void ConvertToShowFloorType(){
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(map[i,j] == '.'){
						map[i,j] = FindFloorType(i,j);
					}
				}
			}
		}
		public static char ConvertedChar(char ch){
			switch(ch){
			case 'h':
			case 'v':
			case 'i':
			case 'E':
			case 'c':
			case 'N':
			case 'r':
			case '$':
			case '.':
				return '.';
			case '+':
				return '+';
			case ':':
				return ':';
			case '&':
				return '&';
			case '0':
				return '0';
			case '~':
				return '~';
			case '#':
			case 'P':
			default:
				return '#';
			}
		}
		public bool IsCorridor(char ch){
			switch(ch){
			case 'h':
			case 'v':
			case 'i':
			case '+':
				return true;
			default:
				return false;
			}
		}
		public bool IsCorridor(int r,int c){ return IsCorridor(new pos(r,c)); }
		public bool IsCorridor(pos p){
			if(!IsRoom(p) && ConvertedChar(Map(p)) != '#'){
				return true;
			}
			return false;
		}
		public bool IsRoom(char ch){
			switch(ch){
			case 'r':
			case 'E':
			case 'c':
			case 'N':
			case 'P':
			case '0':
			case '&': //todo: statues only appear in rooms for now. This might change.
			case '$':
				return true;
			default:
				return false;
			}
		}
		public bool IsRoomOrGenericFloor(char ch){
			if(IsRoom(ch) || ch == '.'){
				return true;
			}
			return false;
		}
		public bool IsRoom(int r,int c){ return IsRoom(new pos(r,c)); }
		public bool IsRoom(pos p){
			if(!IsRoomOrGenericFloor(Map(p))){
				return false;
			}
			for(int i=2;i<=8;i+=2){
				if(IsRoomOrGenericFloor(Map(PosInDir(p,i))) && IsRoomOrGenericFloor(Map(PosInDir(p,RotateDir(i,true,1))))
				&& IsRoomOrGenericFloor(Map(PosInDir(p,RotateDir(i,true,2))))){
					return true;
				}
			}
			return false;
		}
		public char FindFloorType(int r,int c){
			pos p = new pos(r,c);
			if(ConvertedChar(Map(p)) != '.'){
				return ConvertedChar(Map(p));
			}
			if(IsRoom(r,c)){
				//int num_walls = ForEachDirection(r,c,ch => ConvertedChar(ch)=='#',true);
				int num_walls = 0;
				for(int i=1;i<=8;++i){
					int dir = i;
					if(dir == 5){
						dir = 9;
					}
					if(!IsRoom(PosInDir(p,dir))){
						++num_walls;
					}
				}
				if(num_walls == 0){
					return 'r';
				}
				int num_dirs_with_walls = 0;
				for(int i=2;i<=8;i+=2){
					if(!IsRoom(PosInDir(p,i)) && !IsRoom(PosInDir(p,RotateDir(i,true,1)))
					&& !IsRoom(PosInDir(p,RotateDir(i,false,1)))){
						num_dirs_with_walls++;
					}
				}
				if(num_walls == 3 && num_dirs_with_walls == 1){
					return 'E';
				}
				if(num_walls == 5 && num_dirs_with_walls == 2){
					return 'c';
				}
			}
			else{
				if(ConvertedChar(Map(PosInDir(p,8))) == '#' && ConvertedChar(Map(PosInDir(p,2))) == '#'){
					return 'h';
				}
				if(ConvertedChar(Map(PosInDir(p,4))) == '#' && ConvertedChar(Map(PosInDir(p,6))) == '#'){
					return 'v';
				}
				if(ConvertedChar(Map(PosInDir(p,1))) == '#' && ConvertedChar(Map(PosInDir(p,9))) == '#'){
					return 'i';
				}
				if(ConvertedChar(Map(PosInDir(p,7))) == '#' && ConvertedChar(Map(PosInDir(p,3))) == '#'){
					return 'i';
				}
			}
			return 'N';
		}
		/*public int RotateDir(int dir,bool clockwise){ return RotateDir(dir,clockwise,1); }
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
		}*/
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
					if(walls.Count > 0){
						pos wall0 = walls[0];
						pos wall1 = walls[1];
						while(walls.Count > 0){
							pos p = walls[Roll(walls.Count)-1];
							walls.Remove(p);
							int direction_of_other_wall = 0;
							char[] rotated = new char[8];
							for(int ii=0;ii<8;++ii){
								rotated[ii] = Map(PosInDir(p,RotateDir(8,true,ii)));
								pos other_wall = new pos(-1,-1);
								if(p.r == wall0.r && p.c == wall0.c){
									other_wall = wall1;
								}
								else{
									other_wall = wall0;
								}
								if(PosInDir(p,RotateDir(8,true,ii)).r == other_wall.r && PosInDir(p,RotateDir(8,true,ii)).c == other_wall.c){
									direction_of_other_wall = RotateDir(8,true,ii);
								}
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
								if((successive_walls == 4) || (ConvertedChar(Map(PosInDir(p,RotateDir(direction_of_other_wall,true,3)))) == '#'
								&& ConvertedChar(Map(PosInDir(p,RotateDir(direction_of_other_wall,true,4)))) == '#'
								&& ConvertedChar(Map(PosInDir(p,RotateDir(direction_of_other_wall,true,5)))) == '#')){
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
		}
		public void RemoveDeadEnds(){
			bool changed = true;
			while(changed){
				changed = false;
				for(int i=0;i<H;++i){
					for(int j=0;j<W;++j){
						if(ConvertedChar(map[i,j]) == '.' || ConvertedChar(map[i,j]) == '+'){
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
					if(ConvertedChar(map[i,j]) == '.' || map[i,j] == '&' || map[i,j] == ':' || map[i,j] == 'P' || map[i,j] == '+'){
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
		public bool IsConnected(){
			int[,] num = new int[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(ConvertedChar(map[i,j]) == '.' || map[i,j] == '&' || map[i,j] == ':' || map[i,j] == 'P' || map[i,j] == '+'){
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
			if(count > 1){
				return false;
			}
			return true;
		}
		public void AddLake(){
			int radius = Roll(6);
			int size = radius*2 + 1;
			for(int tries=0;tries<100;++tries){
				int rr = Roll(H-((1+radius)*2)) + radius;
				int rc = Roll(W-((1+radius)*2)) + radius;
				int top = rr - radius;
				int left = rc - radius;
				char[,] submap = new char[size,size];
				int radiusx10 = radius * 10;
				bool reject = false;
				for(int i=0;i<size && !reject;++i){
					for(int j=0;j<size && !reject;++j){
						if(map[i+top,j+left] == 'W'){
							reject = true;
						}
					}
				}
				if(reject){
					continue;
				}
				for(int i=0;i<size;++i){
					for(int j=0;j<size;++j){
						submap[i,j] = map[i+top,j+left];
						if(EstimatedEuclideanDistanceFromX10(rr,rc,i+top,j+left) <= radiusx10){
							map[i+top,j+left] = '#';
						}
					}
				}
				if(IsConnected()){
					for(int i=0;i<size;++i){
						for(int j=0;j<size;++j){
							if(EstimatedEuclideanDistanceFromX10(rr,rc,i+top,j+left) <= radiusx10){
								map[i+top,j+left] = 'W';
							}
						}
					}
					return;
				}
				else{
					for(int i=0;i<size;++i){
						for(int j=0;j<size;++j){
							map[i+top,j+left] = submap[i,j];
						}
					}
				}
			}
		}
		public void AddDoors(){
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(ConvertedChar(map[i,j]) == '.'){
						if((map[i-1,j] == '#' && map[i+1,j] == '#') || (map[i,j-1] == '#' && map[i,j+1] == '#')){ //walls on opposite sides
							bool potential_door = false;
							for(int k=2;k<=8;k+=2){
								if(ConvertedChar(Map(PosInDir(i,j,k))) == '.' && ConvertedChar(Map(PosInDir(PosInDir(i,j,k),k))) == '.'){
									if(ConvertedChar(Map(PosInDir(PosInDir(i,j,k),RotateDir(k,false,2)))) == '.'
									&& ConvertedChar(Map(PosInDir(PosInDir(i,j,k),RotateDir(k,false,1)))) == '.'){
										potential_door = true;
									}
									if(ConvertedChar(Map(PosInDir(PosInDir(i,j,k),RotateDir(k,true,2)))) == '.'
									&& ConvertedChar(Map(PosInDir(PosInDir(i,j,k),RotateDir(k,true,1)))) == '.'){
										potential_door = true;
									}
								}
								if(Map(PosInDir(i,j,k)) == '+'){
									potential_door = false;
									break;
								}
							}
							if(potential_door && Roll(4) == 4){
								map[i,j] = '+';
							}
						}
					}
				}
			}
		}
		public void AddFirePits(){
			int num_firepits = 0;
			switch(Roll(5)){
			case 1:
				num_firepits = 1;
				break;
			case 2:
				num_firepits = Roll(4)+1;
				break;
			}
			for(int i=0;i<num_firepits;++i){
				int tries = 0;
				for(bool done=false;!done && tries < 100;++tries){
					int rr = Roll(H-4) + 1;
					int rc = Roll(W-4) + 1;
					if(ConvertedChar(map[rr,rc]) == '.'){
						bool floors = true;
						pos temp = new pos(rr,rc);
						for(int ii=1;ii<=8;++ii){
							int dir = ii;
							if(dir == 5){
								dir = 9;
							}
							if(ConvertedChar(Map(PosInDir(temp,dir))) != '.'){
								floors = false;
								break;
							}
						}
						if(floors){
							map[rr,rc] = '0';
							done = true;
						}
					}
				}
			}
		}
		/*public bool BoundsCheck(int r,int c){
			if(r>0 && r<H-1 && c>0 && c<W-1){
				return true;
			}
			return false;
		}*/
		public bool IsLegal(int r,int c){
			if(r == 0 || r == H-1 || c == 0 || c == W-1){
				return true;
			}
			bool result = true;
			switch(map[r,c]){
			case 'r': //no special rules yet. actually must be surrounded by {E,r,c,N}
				break;
			case 'E':
			{
				int roomdir = 0;
				if(map[r-1,c] == 'r'){
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
					//return false; //no room found, error - disabled for the sake of special tiny rooms with h/w of 2
					char[] rotated = new char[8];
					for(int i=0;i<8;++i){
						rotated[i] = Map(PosInDir(r,c,RotateDir(8,true,i)));
					}
					int successive_corridors = 0;
					if(IsCorridor(rotated[7])){
						successive_corridors++;
					}
					for(int i=0;i<8;++i){
						if(IsCorridor(rotated[i])){
							successive_corridors++;
						}
						else{
							successive_corridors = 0;
						}
						if(successive_corridors == 2){
							return false;
						}
					}
					int successive_room_tiles = 0;
					if(IsRoom(rotated[5])){
						successive_room_tiles++;
					}
					if(IsRoom(rotated[6])){
						successive_room_tiles++;
					}
					else{
						successive_room_tiles = 0;
					}
					if(IsRoom(rotated[7])){
						successive_room_tiles++;
					}
					else{
						successive_room_tiles = 0;
					}
					for(int i=0;i<8;++i){
						if(IsRoom(rotated[i])){
							successive_room_tiles++;
						}
						else{
							successive_room_tiles = 0;
						}
						if(successive_room_tiles == 5){
							return true;
						}
					}
				}
				else{
					char[] rotated = new char[8];
					rotated[0] = 'r';
					for(int i=1;i<8;++i){
						rotated[i] = Map(PosInDir(r,c,RotateDir(roomdir,true,i)));
					}
					if(rotated[1] != 'r' && rotated[1] != 'E'){ return false; }
					if(rotated[7] != 'r' && rotated[7] != 'E'){ return false; }
					if(rotated[2] != 'c' && rotated[2] != 'E' && rotated[2] != 'N'){ return false; }
					if(rotated[6] != 'c' && rotated[6] != 'E' && rotated[6] != 'N'){ return false; }
					if(!(rotated[4] == '#' || (rotated[3] == '#' && rotated[5] == '#'))){ return false; }
				}
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
			case 'N':
				break;
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
			case 'h':
				if(r > H-3 || Map(PosInDir(PosInDir(r,c,2),2)) == 'h'){
					return false;
				}
				if(r < 2 || Map(PosInDir(PosInDir(r,c,8),8)) == 'h'){
					return false;
				}
				break;
			case 'v':
				if(c > W-3 || Map(PosInDir(PosInDir(r,c,6),6)) == 'v'){
					return false;
				}
				if(c < 2 || Map(PosInDir(PosInDir(r,c,4),4)) == 'v'){
					return false;
				}
				break;
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
		/*public int Roll(int dice,int sides){
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
		}*/
		public bool CreateRoom(){ return CreateRoom(Roll(H-2),Roll(W-2)); }
		public bool CreateRoom(int rr,int rc){
			int dir = (Roll(4)*2)-1;
			if(dir == 5){ dir = 9; }
			return CreateRoom(rr,rc,dir);
		}
		public bool CreateRoom(int rr,int rc,int dir){
			//int height = Roll(6)+2; //these 2 lines are still accurate, but were generalized for testing purposes below.
			//int width = Roll(8)+2;
			int height = Roll(room_height_max - (room_height_min-1)) + (room_height_min-1);
			int width = Roll(room_width_max - (room_width_min-1)) + (room_width_min-1);
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
						case 'N':
						case 'r':
							if(rooms_over_rooms){
								place_here = true;
							}
							else{
								good = false;
							}
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
		public bool CreateCorridor(){ return CreateCorridor(Roll(H-2),Roll(W-2),1,Roll(4)*2); }
		public bool CreateCorridor(int count){ return CreateCorridor(Roll(H-2),Roll(W-2),count,Roll(4)*2); }
		public bool CreateCorridor(int rr,int rc){ return CreateCorridor(rr,rc,1,Roll(4)*2); }
		public bool CreateCorridor(int rr,int rc,int count){ return CreateCorridor(rr,rc,count,Roll(4)*2); }
		public bool CreateCorridor(int rr,int rc,int count,int dir){
			bool result = false;
			pos endpoint = new pos(rr,rc);
			pos potential_endpoint;
			List<pos> chain = null;
			if(count > 1){
				chain = new List<pos>();
			}
			int tries = 0;
			while(count > 0 && tries < 100){ //assume there's no room for a corridor if it fails 25 times in a row
				tries++;
				rr = endpoint.r;
				rc = endpoint.c;
				potential_endpoint = endpoint;
				if(chain != null && chain.Count > 0){ //reroll direction ONLY after the first part of the chain.
					dir = Roll(4)*2;
				}
				//int length = Roll(5)+2;	//again, these 2 lines are still accurate, but have been generalized for
				//if(CoinFlip()){ length += 8; } //testing purposes below:
				int length = Roll(corridor_length_max - (corridor_length_min-1)) + (corridor_length_min-1);
				if(CoinFlip()){ length += corridor_length_addition; }
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
						if(true/*corridor_chains_overlap_themselves == false*/){
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
						if(true/*corridor_chains_overlap_themselves == false*/){
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
			}
			return result;
		}
		public void CreateBasicMap(){
			/*for(int i=20;i<20;++i){
				CreateRoom();
			}
			for(int i=250;i<250;++i){
				CreateCorridor(Roll(corridor_chain_length_max - (corridor_chain_length_min-1)) + (corridor_chain_length_min-1));
			}*/
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
				//while(!CreateRoom(p.r,p.c)){ }
				for(int tries=0;tries<500;++tries){
					if(CreateRoom(p.r,p.c)){
						break;
					}
				}
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
			foreach(pos p in points){
				if(map[p.r,p.c] == 'X'){
					map[p.r,p.c] = '#';
				}
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
						CreateCorridor(rr,rc,Roll(corridor_chain_length_max - (corridor_chain_length_min-1)) + (corridor_chain_length_min-1),dir);
					}
					else{
						CreateRoom(rr,rc,dir);
					}
				}
			}
		}
		public delegate bool BooleanDelegate(char ch);
		public int ForEachDirection(int r,int c,BooleanDelegate condition,bool OOB_automatically_passes){
			int result = 0;
			for(int i=1;i<=8;++i){
				int dir = i;
				if(dir == 5){
					dir = 9;
				}
				if(BoundsCheck(r,c)){
					if(condition(Map(PosInDir(r,c,dir)))){
						++result;
					}
				}
				else{
					if(OOB_automatically_passes){
						++result;
					}
				}
			}
			return result;
		}
		public int ForEachOrthDirection(int r,int c,BooleanDelegate condition,bool OOB_automatically_passes){
			int result = 0;
			for(int i=2;i<=8;i+=2){
				if(BoundsCheck(r,c)){
					if(condition(Map(PosInDir(r,c,i)))){
						++result;
					}
				}
				else{
					if(OOB_automatically_passes){
						++result;
					}
				}
			}
			return result;
		}
		public void CreateRandomWalls(int percentage_of_walls){
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(Roll(100) <= percentage_of_walls){
						map[i,j] = '#';
					}
					else{
						map[i,j] = '.';
					}
				}
			}
		}
		public void ApplyCellularAutomataFourFiveRule(){
			/*char[,] result = new char[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					int num_walls = ForEachDirection(i,j,ch => ConvertedChar(ch) == '#',true);
					if(num_walls >= 5 || (num_walls >= 4 && ConvertedChar(map[i,j]) == '#')){
						result[i,j] = '#';
					}
					else{
						result[i,j] = '.';
					}
				}
			}
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					map[i,j] = result[i,j];
				}
			}*/
			ApplyCellularAutomataXYRule(5);
		}
		public void ApplyCellularAutomataXYRule(int target_number_of_walls){
			char[,] result = new char[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					int num_walls = ForEachDirection(i,j,ch => ConvertedChar(ch) == '#',true);
					if(num_walls >= target_number_of_walls || (num_walls >= target_number_of_walls-1 && ConvertedChar(map[i,j]) == '#')){
						result[i,j] = '#';
					}
					else{
						result[i,j] = '.';
					}
				}
			}
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					map[i,j] = result[i,j];
				}
			}
		}
		public void ApplyRuin(){
			char[,] result = new char[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					result[i,j] = map[i,j];
				}
			}
			for(int i=1;i<H-1;++i){
				for(int j=1;j<W-1;++j){
					if(ConvertedChar(map[i,j]) == '#'){
						int num_walls = ForEachDirection(i,j,ch=>ConvertedChar(ch)=='#' || ConvertedChar(ch)=='&',true);
						if(num_walls < 8 && Roll(20) == 20){
							if(Roll(2) == 2){
								result[i,j] = '.';
							}
							else{
								result[i,j] = ':';
								for(int k=1;k<=8;++k){
									int dir = k;
									if(dir == 5){
										dir = 9;
									}
									if(ConvertedChar(Map(PosInDir(i,j,dir))) == '.' && Roll(10) == 10){
										pos p = PosInDir(i,j,dir);
										result[p.r,p.c] = ':';
									}
								}
							}
						}
					}
					else{
						int num_walls = ForEachDirection(i,j,ch=>ConvertedChar(ch)=='#',true);
						if(num_walls == 0 && Roll(100) == 100){
							if(Roll(6) == 6){
								result[i,j] = ':';
							}
							result[i,j] = ':';
							for(int k=1;k<=8;++k){
								int dir = k;
								if(dir == 5){
									dir = 9;
								}
								if(ConvertedChar(Map(PosInDir(i,j,dir))) == '.' && Roll(6) == 6){
									pos p = PosInDir(i,j,dir);
									result[p.r,p.c] = ':';
								}
							}
						}
					}
				}
			}
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					map[i,j] = result[i,j];
				}
			}
		}
		public void Reflect(bool horizontal_axis,bool vertical_axis){
			int half_h = H/2;
			int half_w = W/2;
			if(horizontal_axis){
				if(vertical_axis){
					for(int i=0;i<half_h;++i){
						for(int j=0;j<half_w;++j){
							map[H-1-i,j] = map[i,j];
							map[i,W-1-j] = map[i,j];
							map[H-1-i,W-1-j] = map[i,j];
						}
					}
				}
				else{
					for(int i=0;i<half_h;++i){
						for(int j=0;j<W;++j){
							map[H-1-i,j] = map[i,j];
						}
					}
				}
			}
			else{
				if(vertical_axis){
					for(int i=0;i<H;++i){
						for(int j=0;j<half_w;++j){
							map[i,W-1-j] = map[i,j];
						}
					}
				}
			}
		}
		public delegate bool RoomDelegate(int start_r,int start_c,int end_r,int end_c);
		public bool ForEachRoom(RoomDelegate action){
			int[,] rooms = new int[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(IsRoom(i,j)){
						rooms[i,j] = 0;
					}
					else{
						rooms[i,j] = -1;
					}
				}
			}
			int next_room_number = 1;
			for(int i=0;i<H-3;++i){
				for(int j=0;j<W-3;++j){
					if(rooms[i,j] == -1 && rooms[i+1,j+1] == 0 && rooms[i+2,j+2] == 0){ //checks 2 spaces down and right
						rooms[i+1,j+1] = next_room_number;
						for(bool done=false;!done;){
							done = true;
							for(int s=i+1;s<H-1;++s){
								for(int t=j+1;t<W-1;++t){
									if(rooms[s,t] == next_room_number){
										for(int u=s-1;u<=s+1;++u){
											for(int v=t-1;v<=t+1;++v){
												if(u != s || v != t){
													if(rooms[u,v] == 0){
														rooms[u,v] = next_room_number;
														done = false;
													}
												}
											}
										}
									}
								}
							}
						}
						++next_room_number;
					}
				}
			}
			for(int k=1;k<next_room_number;++k){
				int start_r = 999;
				int start_c = 999;
				int end_r = -1;
				int end_c = -1;
				for(int i=1;i<H-1;++i){
					for(int j=1;j<W-1;++j){
						if(rooms[i,j] == k){
							if(i < start_r){
								start_r = i;
							}
							if(i > end_r){
								end_r = i;
							}
							if(j < start_c){
								start_c = j;
							}
							if(j > end_c){
								end_c = j;
							}
						}
					}
				}
				if(!action(start_r,start_c,end_r,end_c)){
					return false;
				}
			}
			return true;
		}
		public void MarkInterestingLocations(){
			ForEachRoom((start_r,start_c,end_r,end_c) => {
				int height = end_r - start_r + 1;
				int width = end_c - start_c + 1;
				if(height % 2 == 1 || width % 2 == 1){
					List<pos> exits = new List<pos>();
					for(int i=start_r;i<=end_r;++i){
						for(int j=start_c;j<=end_c;++j){
							if(i == start_r || j == start_c || i == end_r || j == end_c){
								if(IsCorridor(map[i-1,j]) || IsCorridor(map[i,j-1])
								|| IsCorridor(map[i+1,j]) || IsCorridor(map[i,j+1])){
									exits.Add(new pos(i,j));
								}
							}
						}
					}
					int half_r = (start_r + end_r)/2;
					int half_c = (start_c + end_c)/2;
					int half_r_offset = (start_r + end_r + 1)/2;
					int half_c_offset = (start_c + end_c + 1)/2;
					List<pos> centers = new List<pos>();
					centers.Add(new pos(half_r,half_c));
					if(half_r != half_r_offset){
						centers.Add(new pos(half_r_offset,half_c));
					}
					else{ //these can't both be true because the dimension can't be even X even
						if(half_c != half_c_offset){
							centers.Add(new pos(half_r,half_c_offset));
						}
					}
					List<pos> in_middle_row_or_column = new List<pos>();
					if(width % 2 == 1){
						for(int i=start_r;i<=end_r;++i){
							in_middle_row_or_column.Add(new pos(i,half_c));
						}
					}
					if(height % 2 == 1){
						for(int j=start_c;j<=end_c;++j){
							bool good = true;
							foreach(pos p in in_middle_row_or_column){
								if(p.r == half_r && p.c == j){
									good = false;
									break;
								}
							}
							if(good){
								in_middle_row_or_column.Add(new pos(half_r,j));
							}
						}
					}
					List<pos> rejected = new List<pos>();
					foreach(pos p in in_middle_row_or_column){
						int floors = ForEachDirection(p.r,p.c,ch => ConvertedChar(ch) == '.',true);
						if(ConvertedChar(Map(p)) != '.' || floors != 8){
							rejected.Add(p);
						}
					}
					foreach(pos p in rejected){
						in_middle_row_or_column.Remove(p);
					}
					rejected.Clear();
					foreach(pos exit in exits){
						int greatest_distance = 0;
						foreach(pos center in centers){
							if(EstimatedEuclideanDistanceFromX10(exit,center) > greatest_distance){
								greatest_distance = EstimatedEuclideanDistanceFromX10(exit,center);
							}
						}
						foreach(pos potential in in_middle_row_or_column){
							if(EstimatedEuclideanDistanceFromX10(exit,potential) <= greatest_distance){
								rejected.Add(potential);
							}
						}
					}
					foreach(pos p in rejected){
						in_middle_row_or_column.Remove(p);
					}
					if(in_middle_row_or_column.Count > 0){
						int greatest_total_distance = 0;
						List<pos> positions_with_greatest_distance = new List<pos>();
						foreach(pos potential in in_middle_row_or_column){
							int total_distance = 0;
							foreach(pos exit in exits){
								total_distance += EstimatedEuclideanDistanceFromX10(potential,exit);
							}
							if(total_distance > greatest_total_distance){
								greatest_total_distance = total_distance;
								positions_with_greatest_distance.Clear();
								positions_with_greatest_distance.Add(potential);
							}
							else{
								if(total_distance == greatest_total_distance){
									positions_with_greatest_distance.Add(potential);
								}
							}
						}
						foreach(pos p in positions_with_greatest_distance){
							map[p.r,p.c] = '$';
						}
					}
					else{
						if(height % 2 == 1 && width % 2 == 1){
							int floors = ForEachDirection(half_r,half_c,ch => ConvertedChar(ch) == '.',true);
							if(ConvertedChar(map[half_r,half_c]) == '.' && floors == 8){
								map[half_r,half_c] = '$';
							}
						}
					}
				}
				return true;
			});
		}
		public enum PillarArrangement{Single,Full,Corners,Row,StatueCorners,StatueEdges};
		public void AddPillars(int percent_chance_per_room){
			ForEachRoom((start_r,start_c,end_r,end_c) => {
				if(Roll(100) <= percent_chance_per_room){
					int height = end_r - start_r + 1;
					int width = end_c - start_c + 1;
					if(height > 3 || width > 3){
						List<PillarArrangement> layouts = new List<PillarArrangement>();
						if(height % 2 == 1 && width % 2 == 1){
							layouts.Add(PillarArrangement.Single);
						}
						if((height % 2 == 1 || width % 2 == 1) && height != 4 && width != 4){
							layouts.Add(PillarArrangement.Row);
						}
						if(height >= 5 && width >= 5){
							layouts.Add(PillarArrangement.Corners);
						}
						if(height > 2 && width > 2 && height != 4 && width != 4){
							layouts.Add(PillarArrangement.Full);
						}
						if((width % 2 == 1 && width >= 5) || (height % 2 == 1 && height >= 5)){
							layouts.Add(PillarArrangement.StatueEdges);
						}
						//if((height == 4 && width % 2 == 0) || (width == 4 && height % 2 == 0)){
						if(layouts.Count == 0 || CoinFlip()){ //otherwise they're too common
							layouts.Add(PillarArrangement.StatueCorners);
						}
						if(layouts.Count > 0){
							char pillar = ' ';
							switch(Roll(4)){
							case 1:
							case 2:
								pillar = 'P';
								break;
							case 3:
								pillar = '&';
								break;
							case 4:
								pillar = '0';
								break;
							}
							switch(layouts[Roll(layouts.Count)-1]){
							case PillarArrangement.Single:
								map[(start_r + end_r)/2,(start_c + end_c)/2] = 'P';
								break;
							case PillarArrangement.Row:
							{
								bool vertical;
								if(width % 2 == 1 && height % 2 == 0){
									vertical = true;
								}
								else{
									if(height % 2 == 1 && width % 2 == 0){
										vertical = false;
									}
									else{
										vertical = CoinFlip();
									}
								}
								if(vertical){
									if(height % 2 == 1){
										for(int i=start_r+1;i<=end_r-1;i+=2){
											map[i,(start_c + end_c)/2] = pillar;
										}
									}
									else{
										int offset = 0;
										if(height % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_r+1+offset;i<(start_r + end_r)/2;i+=2){
											map[i,(start_c + end_c)/2] = pillar;
										}
										for(int i=end_r-1-offset;i>(start_r + end_r)/2+1;i-=2){
											map[i,(start_c + end_c)/2] = pillar;
										}
									}
								}
								else{
									if(width % 2 == 1){
										for(int i=start_c+1;i<=end_c-1;i+=2){
											map[(start_r + end_r)/2,i] = pillar;
										}
									}
									else{
										int offset = 0;
										if(width % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_c+1+offset;i<(start_c + end_c)/2;i+=2){
											map[(start_r + end_r)/2,i] = pillar;
										}
										for(int i=end_c-1-offset;i>(start_c + end_c)/2+1;i-=2){
											map[(start_r + end_r)/2,i] = pillar;
										}
									}
								}
								break;
							}
							case PillarArrangement.Corners:
							{
								int v_offset = 0;
								int h_offset = 0;
								if(height % 4 == 0){
									v_offset = Roll(2) - 1;
								}
								if(width % 4 == 0){
									h_offset = Roll(2) - 1;
								}
								map[start_r + 1 + v_offset,start_c + 1 + h_offset] = pillar;
								map[start_r + 1 + v_offset,end_c - 1 - h_offset] = pillar;
								map[end_r - 1 - v_offset,start_c + 1 + h_offset] = pillar;
								map[end_r - 1 - v_offset,end_c - 1 - h_offset] = pillar;
								break;
							}
							case PillarArrangement.Full:
							{
								int v_offset = 0;
								int h_offset = 0;
								if(height % 4 == 0){
									v_offset = Roll(2) - 1;
								}
								if(width % 4 == 0){
									h_offset = Roll(2) - 1;
								}
								int half_r = (start_r + end_r)/2;
								int half_c = (start_c + end_c)/2;
								int half_r_offset = (start_r + end_r + 1)/2;
								int half_c_offset = (start_c + end_c + 1)/2;
								for(int i=start_r+1+v_offset;i<half_r;i+=2){
									for(int j=start_c+1+h_offset;j<half_c;j+=2){
										map[i,j] = pillar;
									}
								}
								for(int i=start_r+1+v_offset;i<half_r;i+=2){
									for(int j=end_c-1-h_offset;j>half_c_offset;j-=2){
										map[i,j] = pillar;
									}
								}
								for(int i=end_r-1-v_offset;i>half_r_offset;i-=2){
									for(int j=start_c+1+h_offset;j<half_c;j+=2){
										map[i,j] = pillar;
									}
								}
								for(int i=end_r-1-v_offset;i>half_r_offset;i-=2){
									for(int j=end_c-1-h_offset;j>half_c_offset;j-=2){
										map[i,j] = pillar;
									}
								}
								if((width+1) % 4 == 0){
									if(height % 2 == 1){
										for(int i=start_r+1;i<=end_r-1;i+=2){
											map[i,half_c] = pillar;
										}
									}
									else{
										int offset = 0;
										if(height % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_r+1+offset;i<half_r;i+=2){
											map[i,half_c] = pillar;
										}
										for(int i=end_r-1-offset;i>half_r_offset;i-=2){
											map[i,half_c] = pillar;
										}
									}
								}
								if((height+1) % 4 == 0){
									if(width % 2 == 1){
										for(int i=start_c+1;i<=end_c-1;i+=2){
											map[half_r,i] = pillar;
										}
									}
									else{
										int offset = 0;
										if(width % 4 == 0){
											offset = Roll(2) - 1;
										}
										for(int i=start_c+1+offset;i<half_c;i+=2){
											map[half_r,i] = pillar;
										}
										for(int i=end_c-1-offset;i>half_c_offset;i-=2){
											map[half_r,i] = pillar;
										}
									}
								}
								break;
							}
							case PillarArrangement.StatueCorners:
								map[start_r,start_c] = '&';
								map[start_r,end_c] = '&';
								map[end_r,start_c] = '&';
								map[end_r,end_c] = '&';
								break;
							case PillarArrangement.StatueEdges:
							{
								map[start_r,start_c] = '&';
								map[start_r,end_c] = '&';
								map[end_r,start_c] = '&';
								map[end_r,end_c] = '&';
								if(width % 2 == 1 && width > 3){
									int half_c = (start_c + end_c)/2;
									int corridors = ForEachOrthDirection(start_r,half_c,ch => IsCorridor(ch),false);
									if(corridors == 0){
										map[start_r,half_c] = '&';
									}
									corridors = ForEachOrthDirection(end_r,half_c,ch => IsCorridor(ch),false);
									if(corridors == 0){
										map[end_r,half_c] = '&';
									}
								}
								if(height % 2 == 1 && height > 3){
									int half_r = (start_r + end_r)/2;
									int corridors = ForEachOrthDirection(half_r,start_c,ch => IsCorridor(ch),false);
									if(corridors == 0){
										map[half_r,start_c] = '&';
									}
									corridors = ForEachOrthDirection(half_r,end_c,ch => IsCorridor(ch),false);
									if(corridors == 0){
										map[half_r,end_c] = '&';
									}
								}
								break;
							}
							default:
								break;
							}
						}
					}
				}
				return true;
			});
		}
		public bool ApplyCaveModification(){ //todo: this needs a counterpart that makes rooms *bigger* and cavelike.
			return ForEachRoom((start_r,start_c,end_r,end_c) => {
				char[,] room = new char[(end_r-start_r)+3,(end_c-start_c)+3]; //includes borders
				List<pos> exits = new List<pos>();
				for(int i=1;i<room.GetLength(0)-1;++i){
					for(int j=1;j<room.GetLength(1)-1;++j){
						if(i == 1 || j == 1 || i == room.GetLength(0)-2 || j == room.GetLength(1)-2){
							if(IsCorridor(map[start_r+i-2,start_c+j-1]) || IsCorridor(map[start_r+i-1,start_c+j-2])
							|| IsCorridor(map[start_r+i,start_c+j-1]) || IsCorridor(map[start_r+i-1,start_c+j])){
								exits.Add(new pos(i,j));
							}
						}
					}
				}
				int tries = 0;
				while(true && tries < 500){
					CreateRandomWalls(room,25);
					ApplyCellularAutomataXYRule(room,3);
					RemoveDiagonals(room);
					RemoveDeadEnds(room);
					RemoveUnconnected(room);
					bool exits_open = true;
					foreach(pos p in exits){
						if(ConvertedChar(room[p.r,p.c]) != '.'){
							exits_open = false;
						}
					}
					if(exits_open){
						for(int i=start_r;i<=end_r;++i){
							for(int j=start_c;j<=end_c;++j){
								map[i,j] = room[(i-start_r)+1,(j-start_c)+1];
							}
						}
						break;
					}
					++tries;
					if(tries > 50){
						return false;
					}
				}
				return true;
			});
			//return true;
		}
		/*public delegate void ArrayDelegate(char[,] map);
		public void ForEachRoom(ArrayDelegate action){
		}*/
		public static int ForEachDirection(char[,] map,int r,int c,BooleanDelegate condition,bool OOB_automatically_passes){
			int result = 0;
			for(int i=1;i<=8;++i){
				int dir = i;
				if(dir == 5){
					dir = 9;
				}
				if(BoundsCheck(r,c,map.GetLength(0),map.GetLength(1))){
					pos p = PosInDir(r,c,dir);
					if(condition(map[p.r,p.c])){
						++result;
					}
				}
				else{
					if(OOB_automatically_passes){
						++result;
					}
				}
			}
			return result;
		}
		public static void RemoveDiagonals(char[,] map){
			List<pos> walls = new List<pos>();
			for(int i=1;i<map.GetLength(0)-2;++i){
				for(int j=1;j<map.GetLength(1)-2;++j){
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
						pos p = walls[Dungeon.Roll(walls.Count)-1];
						walls.Remove(p);
						char[] rotated = new char[8];
						for(int ii=0;ii<8;++ii){
							pos p2 = PosInDir(p.r,p.c,RotateDir(8,true,ii));
							rotated[ii] = map[p2.r,p2.c];
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
								if(StandardDungeon.IsLegal(map,p.r,p.c)){
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
		public static void RemoveDeadEnds(char[,] map){
			bool changed = true;
			while(changed){
				changed = false;
				for(int i=0;i<map.GetLength(0);++i){
					for(int j=0;j<map.GetLength(1);++j){
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
		public static void RemoveUnconnected(char[,] map){
			int[,] num = new int[map.GetLength(0),map.GetLength(1)];
			for(int i=0;i<map.GetLength(0);++i){
				for(int j=0;j<map.GetLength(1);++j){
					if(ConvertedChar(map[i,j]) == '.' ||  map[i,j] == '&' || map[i,j] == ':'){
						num[i,j] = 0;
					}
					else{
						num[i,j] = -1;
					}
				}
			}
			int count = 0;
			for(int i=0;i<map.GetLength(0);++i){
				for(int j=0;j<map.GetLength(1);++j){
					if(num[i,j] == 0){
						count++;
						num[i,j] = count;
						bool changed = true;
						while(changed){
							changed = false;
							for(int s=0;s<map.GetLength(0);++s){
								for(int t=0;t<map.GetLength(1);++t){
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
				for(int i=0;i<map.GetLength(0);++i){
					for(int j=0;j<map.GetLength(1);++j){
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
			for(int i=0;i<map.GetLength(0);++i){
				for(int j=0;j<map.GetLength(1);++j){
					if(num[i,j] != biggest_area){
						map[i,j] = '#';
					}
				}
			}
		}
		public static bool IsLegal(char[,] map,int r,int c){ return IsLegal(map,r,c,false); }
		public static bool IsLegal(char[,] map,int r,int c,bool allow_all_corner_connections){
			if(r == 0 || r == map.GetLength(0)-1 || c == 0 || c == map.GetLength(1)-1){
				return true;
			}
			bool result = true;
			switch(map[r,c]){
			case 'r': //no special rules yet. actually must be surrounded by {E,r,c,N}
				break;
			case 'E':
			{
				int roomdir = 0;
				if(map[r-1,c] == 'r'){
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
					pos p = PosInDir(r,c,RotateDir(roomdir,true,i));
					rotated[i] = map[p.r,p.c];
				}
				if(rotated[1] != 'r' && rotated[1] != 'E'){ return false; }
				if(rotated[7] != 'r' && rotated[7] != 'E'){ return false; }
				if(rotated[2] != 'c' && rotated[2] != 'E' && rotated[2] != 'N'){ return false; }
				if(rotated[6] != 'c' && rotated[6] != 'E' && rotated[6] != 'N'){ return false; }
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
					pos p = PosInDir(r,c,RotateDir(roomdir,true,i));
					rotated[i] = map[p.r,p.c];
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
			case 'N':
				break;
			case 'i':
			{
				char[] rotated = new char[8];
				for(int i=0;i<8;++i){
					pos p = PosInDir(r,c,RotateDir(8,true,i));
					rotated[i] = map[p.r,p.c];
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
			case 'h':
			{
				pos p = PosInDir(PosInDir(r,c,2),2);
				if(r > H-3 || map[p.r,p.c] == 'h'){
					return false;
				}
				p = PosInDir(PosInDir(r,c,8),8);
				if(r < 2 || map[p.r,p.c] == 'h'){
					return false;
				}
				break;
			}
			case 'v':
			{
				pos p = PosInDir(PosInDir(r,c,6),6);
				if(c > W-3 || map[p.r,p.c] == 'v'){
					return false;
				}
				p = PosInDir(PosInDir(r,c,4),4);
				if(c < 2 || map[p.r,p.c] == 'v'){
					return false;
				}
				break;
			}
			case 'X': //in case there is a need to block off an area
				for(int i=1;i<=8;++i){
					int dir = i;
					if(dir == 5){ dir = 9; }
					pos p = PosInDir(r,c,dir);
					if(ConvertedChar(map[p.r,p.c]) != '#'){
						return false;
					}
				}
				break;
			default:
				break;
			}
			return result;
		}
		public static void CreateRandomWalls(char[,] map,int percentage_of_walls){
			for(int i=0;i<map.GetLength(0);++i){
				for(int j=0;j<map.GetLength(1);++j){
					if(Roll(100) <= percentage_of_walls){
						map[i,j] = '#';
					}
					else{
						map[i,j] = '.';
					}
				}
			}
		}
		public static void ApplyCellularAutomataXYRule(char[,] map,int target_number_of_walls){
			char[,] result = new char[map.GetLength(0),map.GetLength(1)];
			for(int i=0;i<map.GetLength(0);++i){
				for(int j=0;j<map.GetLength(1);++j){
					int num_walls = ForEachDirection(map,i,j,ch => ConvertedChar(ch) == '#',true);
					if(num_walls >= target_number_of_walls || (num_walls >= target_number_of_walls-1 && ConvertedChar(map[i,j]) == '#')){
						result[i,j] = '#';
					}
					else{
						result[i,j] = '.';
					}
				}
			}
			for(int i=0;i<map.GetLength(0);++i){
				for(int j=0;j<map.GetLength(1);++j){
					map[i,j] = result[i,j];
				}
			}
		}
		public static int NumberOfFloors(char[,] map){
			int total = 0;
			for(int i=0;i<map.GetLength(0);++i){
				for(int j=0;j<map.GetLength(1);++j){
					if(ConvertedChar(map[i,j]) == '.'){
						total++;
					}
				}
			}
			return total;
		}
	}
	//public class CaveDungeon : Dungeon{ //looks like the inheritance was a mistake. it's all going in the primary class.
	//}
}
