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
	public static class Global{
		public static bool LINUX = false;
		public const int SCREEN_H = 25;
		public const int SCREEN_W = 80;
		public const int ROWS = 22;
		public const int COLS = 66;
		public const int MAP_OFFSET_ROWS = 2;
		public const int MAP_OFFSET_COLS = 13;
		public const int MAX_LIGHT_RADIUS = 12; //the maximum POSSIBLE light radius. used in light calculations.
		public const int MAX_INVENTORY_SIZE = ROWS-2;
		public static bool GAME_OVER = false;
		public static Dictionary<OptionType,bool> Options = new Dictionary<OptionType, bool>();
		public static bool Option(OptionType option){
			bool result = false;
			Options.TryGetValue(option,out result);
			return result;
		}
		public static Random r = new Random();
		public static void SetSeed(int seed){ r = new Random(seed); }
		public static int Roll(int dice,int sides){
			int total = 0;
			for(int i=0;i<dice;++i){
				total += r.Next(1,sides+1); //Next's maxvalue is exclusive, thus the +1
			}
			return total;
		}
		public static int Roll(int sides){
			int total = 0;
			total += r.Next(1,sides+1); //Next's maxvalue is exclusive, thus the +1
			return total;
		}
		public static bool CoinFlip(){
			if(r.Next(1,3) == 2){ //returns 1 or 2...
				return true;
			}
			else{
				return false;
			}
		}
		public static int RandomDirection(){
			int result = r.Next(1,8);
			if(result == 5){
				result = 9;
			}
			return result;
		}
		public static bool BoundsCheck(int r,int c){
			if(r>=0 && r<ROWS && c>=0 && c<COLS){
				return true;
			}
			return false;
		}
		public static int EnterInt(){ return EnterInt(4); }
		public static int EnterInt(int max_length){
			string s = "";
			ConsoleKeyInfo command;
			Console.CursorVisible = true;
			bool done = false;
			int pos = Console.CursorLeft;
			Screen.WriteString(Console.CursorTop,pos,"".PadRight(max_length));
			while(!done){
				Console.SetCursorPosition(pos,Console.CursorTop);
				command = Console.ReadKey(true);
				if(command.KeyChar >= '0' && command.KeyChar <= '9'){
					if(s.Length < max_length){
						s = s + command.KeyChar;
						Screen.WriteChar(Console.CursorTop,pos,command.KeyChar);
						++pos;
					}
				}
				else{
					if(command.Key == ConsoleKey.Backspace && s.Length > 0){
						s = s.Substring(0,s.Length-1);
						--pos;
						Screen.WriteChar(Console.CursorTop,pos,' ');
						Console.SetCursorPosition(pos,Console.CursorTop);
					}
					else{
						if(command.Key == ConsoleKey.Escape){
							return 0;
						}
						else{
							if(command.Key == ConsoleKey.Enter){
								if(s.Length == 0){
									return 0;
								}
								done = true;
							}
						}
					}
				}
			}
			return Convert.ToInt32(s);
		}
		public static string EnterString(){ return EnterString(COLS-1); }
		public static string EnterString(int max_length){
			string s = "";
			ConsoleKeyInfo command;
			Console.CursorVisible = true;
			bool done = false;
			int pos = Console.CursorLeft;
			Screen.WriteString(Console.CursorTop,pos,"".PadRight(max_length));
			while(!done){
				Console.SetCursorPosition(pos,Console.CursorTop);
				command = Console.ReadKey(true);
				if((command.KeyChar >= '!' && command.KeyChar <= '~') || command.KeyChar == ' '){
					if(s.Length < max_length){
						s = s + command.KeyChar;
						Screen.WriteChar(Console.CursorTop,pos,command.KeyChar);
						++pos;
					}
				}
				else{
					if(command.Key == ConsoleKey.Backspace && s.Length > 0){
						s = s.Substring(0,s.Length-1);
						--pos;
						Screen.WriteChar(Console.CursorTop,pos,' ');
						Console.SetCursorPosition(pos,Console.CursorTop);
					}
					else{
						if(command.Key == ConsoleKey.Escape){
							return "";
						}
						else{
							if(command.Key == ConsoleKey.Enter){
								if(s.Length == 0){
									return "";
								}
								done = true;
							}
						}
					}
				}
			}
			return s;
		}
	}
	public class Dict<TKey,TValue>{
		private Dictionary<TKey,TValue> d;// = new Dictionary<TKey,TValue>();
		public TValue this[TKey key]{
			get{
				return d.ContainsKey(key)? d[key] : default(TValue);
			}
			set{
				d[key] = value;
			}
		}
		public Dict(){ d = new Dictionary<TKey,TValue>(); }
		public Dict(Dict<TKey,TValue> d2){ d = new Dictionary<TKey, TValue>(d2.d); }
	}
	public struct pos{ //eventually, this might become part of PhysicalObject. if so, i'll create a property for 'row' and 'col'
		public int row; //so the change will be entirely transparent.
		public int col;
		public pos(int r,int c){
			row = r;
			col = c;
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
		public List<pos> PositionsAtDistance(int dist){
			List<pos> result = new List<pos>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(DistanceFrom(i,j) == dist && Global.BoundsCheck(i,j)){
						result.Add(new pos(i,j));
					}
				}
			}
			return result;
		}
	}
}
