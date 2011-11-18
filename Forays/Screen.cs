/*Copyright (c) 2011  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
using System.Threading;
namespace Forays{
	public enum Color{Black,White,Gray,Red,Green,Blue,Yellow,Magenta,Cyan,DarkGray,DarkRed,DarkGreen,DarkBlue,DarkYellow,DarkMagenta,DarkCyan,RandomFire,RandomIce,RandomLightning};
	public struct colorchar{
		public Color color;
		public Color bgcolor;
		public char c;
		public colorchar(Color color_,Color bgcolor_,char c_){
			color = color_;
			bgcolor = bgcolor_;
			c = c_;
		}
		public colorchar(Color color_,char c_){
			color = color_;
			bgcolor = Color.Black;
			c = c_;
		}
	}
	public struct colorstring{
		public Color color;
		public Color bgcolor;
		public string s;
	}
	public static class Screen{
		private static colorchar[,] memory;
		private static bool terminal_bold = false; //for linux terminals
		private static readonly string bold_on = (char)27 + "[1m"; //VT100 codes, sweet
		private static readonly string bold_off = (char)27 + "[m";
		public static ConsoleColor ForegroundColor{
			get{
				if(Global.LINUX && terminal_bold){
					return Console.ForegroundColor+8;
				}
				return Console.ForegroundColor;
			}
			set{
				if(Global.LINUX && (int)value >= 8){
					Console.ForegroundColor = value - 8;
					if(!terminal_bold){
						terminal_bold = true;
						Console.Write(bold_on);
					}
				}
				else{
					if(Global.LINUX && terminal_bold){
						Console.Write(bold_off);
						terminal_bold = false;
					}
					Console.ForegroundColor = value;
				}
			}
		}
		public static ConsoleColor BackgroundColor{
			get{
				return Console.BackgroundColor;
			}
			set{
				if(Global.LINUX && (int)value >= 8){
					Console.BackgroundColor = value - 8;
				}
				else{
					Console.BackgroundColor = value;
				}
			}
		}
		public static colorchar Char(int r,int c){ return memory[r,c]; }
		public static colorchar MapChar(int r,int c){ return memory[r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS]; }
		public static colorchar StatsChar(int r,int c){ return memory[r+1,c]; }
		static Screen(){
			memory = new colorchar[Global.SCREEN_H,Global.SCREEN_W];
			for(int i=0;i<Global.SCREEN_H;++i){
				for(int j=0;j<Global.SCREEN_W;++j){
					memory[i,j].c = ' ';
					memory[i,j].color = Color.Black;
					memory[i,j].bgcolor = Color.Black;
				}
			}
			BackgroundColor = Console.BackgroundColor;
			ForegroundColor = Console.ForegroundColor;
		}
		public static colorchar BlankChar(){ return new colorchar(Color.Black,' '); }
		public static colorchar[,] GetCurrentMap(){
			colorchar[,] result = new colorchar[Global.ROWS,Global.COLS];
			for(int i=0;i<Global.ROWS;++i){
				for(int j=0;j<Global.COLS;++j){
					result[i,j] = MapChar(i,j);
				}
			}
			return result;
		}
		public static bool BoundsCheck(int r,int c){
			if(r>=0 && r<Global.SCREEN_H && c>=0 && c<Global.SCREEN_W){
				return true;
			}
			return false;
		}
		public static bool MapBoundsCheck(int r,int c){
			if(r>=0 && r<Global.ROWS && c>=0 && c<Global.COLS){
				return true;
			}
			return false;
		}
		public static void Blank(){
			Console.CursorVisible = false;
			for(int i=0;i<Global.SCREEN_H;++i){
				Console.SetCursorPosition(0,i);
				Console.Write("".PadRight(Global.SCREEN_W));
			}
			//Console.CursorVisible = true; //this works for Main's call, but it's a bit hacky.	
		}
		public static void WriteChar(int r,int c,colorchar ch){
			if(!memory[r,c].Equals(ch)){
				if(ch.c == ' '){
					ch.color = ch.bgcolor;
				}
				memory[r,c] = ch;
				ConsoleColor co = GetColor(ch.color);
				if(co != ForegroundColor){
					ForegroundColor = co;
				}
				co = GetColor(ch.bgcolor);
				if(co != Console.BackgroundColor || ch.c == ' '){//voodoo here. not sure why this is needed.
					BackgroundColor = co;
				}
				Console.SetCursorPosition(c,r);
				Console.Write(ch.c);
			}
		}
		public static void ResetColors(){
			if(ForegroundColor != ConsoleColor.Gray){
				ForegroundColor = ConsoleColor.Gray;
			}
			if(BackgroundColor != ConsoleColor.Black){
				BackgroundColor = ConsoleColor.Black;
			}
		}
		public static void WriteMapChar(int r,int c,colorchar ch){
			WriteChar(r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS,ch);
		}
		public static void WriteMapString(int r,int c,string s){
			colorstring cs;
			cs.color = Color.Gray;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteMapString(r,c,cs);
		}
		public static void WriteMapString(int r,int c,colorstring s){
			if(Global.COLS - c > s.s.Length){
				s.s = s.s.Substring(0); //don't move down to the next line
			}
			else{
				s.s = s.s.Substring(0,Global.COLS - c);
			}
			if(s.s.Length > 0){
				r += Global.MAP_OFFSET_ROWS;
				c += Global.MAP_OFFSET_COLS;
				colorchar cch;
				cch.color = s.color;
				cch.bgcolor = s.bgcolor;
				ConsoleColor co = GetColor(s.color);
				if(ForegroundColor != co){
					ForegroundColor = co;
				}
				co = GetColor(s.bgcolor);
				if(BackgroundColor != co){
					BackgroundColor = co;
				}
				int i = 0;
				foreach(char ch in s.s){
					cch.c = ch;
					if(!memory[r,c+i].Equals(cch)){
						memory[r,c+i] = cch;
					}
					++i;
				}
				Console.SetCursorPosition(c,r);
				Console.Write(s.s);
			}
		}
		public static void WriteStatsChar(int r,int c,colorchar ch){ WriteChar(r+1,c,ch); }
		public static void WriteStatsString(int r,int c,string s){
			colorstring cs;
			cs.color = Color.Gray;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteStatsString(r,c,cs);
		}
		public static void WriteStatsString(int r,int c,colorstring s){
			if(12 - c > s.s.Length){
				s.s = s.s.Substring(0); //don't move down to the next line - 12 is the width of the stats area
			}
			else{
				s.s = s.s.Substring(0,12 - c);
			}
			if(s.s.Length > 0){
				++r;
				//++c;
				colorchar cch;
				cch.color = s.color;
				cch.bgcolor = s.bgcolor;
				ConsoleColor co = GetColor(s.color);
				if(ForegroundColor != co){
					ForegroundColor = co;
				}
				co = GetColor(s.bgcolor);
				if(BackgroundColor != co){
					BackgroundColor = co;
				}
				int i = 0;
				foreach(char ch in s.s){
					cch.c = ch;
					if(!memory[r,c+i].Equals(cch)){
						memory[r,c+i] = cch;
					}
					++i;
				}
				Console.SetCursorPosition(c,r);
				Console.Write(s.s);
			}
		}
		public static void AnimateCell(int r,int c,colorchar ch,int duration){
			colorchar prev = memory[r,c];
			WriteChar(r,c,ch);
			Thread.Sleep(duration);
			WriteChar(r,c,prev);
		}
		public static void AnimateMapCell(int r,int c,colorchar ch){ AnimateMapCell(r,c,ch,50); }
		public static void AnimateMapCell(int r,int c,colorchar ch,int duration){
			AnimateCell(r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS,ch,duration);
		}
		public static void AnimateProjectile(List<Tile> list,colorchar ch){ AnimateProjectile(list,ch,50); }
		public static void AnimateProjectile(List<Tile> list,colorchar ch,int duration){
			Console.CursorVisible = false;
			list.RemoveAt(0);
			foreach(Tile t in list){
				AnimateMapCell(t.row,t.col,ch,duration);
			}
			Console.CursorVisible = true;
		}
		public static void AnimateExplosion(PhysicalObject obj,int radius,colorchar ch,int duration){
			Console.CursorVisible = false;
			colorchar[,] prev = new colorchar[radius*2+1,radius*2+1];
			for(int i=0;i<=radius*2;++i){
				for(int j=0;j<=radius*2;++j){
					if(MapBoundsCheck(obj.row-radius+i,obj.col-radius+j)){
						prev[i,j] = MapChar(obj.row-radius+i,obj.col-radius+j);
					}
				}
			}
			for(int i=0;i<=radius;++i){
				foreach(Tile t in obj.TilesAtDistance(i)){
					WriteMapChar(t.row,t.col,ch);
				}
				Thread.Sleep(duration);
			}
			for(int i=0;i<=radius*2;++i){
				for(int j=0;j<=radius*2;++j){
					if(MapBoundsCheck(obj.row-radius+i,obj.col-radius+j)){
						WriteMapChar(obj.row-radius+i,obj.col-radius+j,prev[i,j]);
					}
				}
			}
			Console.CursorVisible = true;
		}
		public static ConsoleColor GetColor(Color c){
			switch(c){
			case Color.Black:
				return ConsoleColor.Black;
			case Color.White:
				return ConsoleColor.White;
			case Color.Gray:
				return ConsoleColor.Gray;
			case Color.Red:
				return ConsoleColor.Red;
			case Color.Green:
				return ConsoleColor.Green;
			case Color.Blue:
				return ConsoleColor.Blue;
			case Color.Yellow:
				return ConsoleColor.Yellow;
			case Color.Magenta:
				return ConsoleColor.Magenta;
			case Color.Cyan:
				return ConsoleColor.Cyan;
			case Color.DarkGray:
				return ConsoleColor.DarkGray;
			case Color.DarkRed:
				return ConsoleColor.DarkRed;
			case Color.DarkGreen:
				return ConsoleColor.DarkGreen;
			case Color.DarkBlue:
				return ConsoleColor.DarkBlue;
			case Color.DarkYellow:
				return ConsoleColor.DarkYellow;
			case Color.DarkMagenta:
				return ConsoleColor.DarkMagenta;
			case Color.DarkCyan:
				return ConsoleColor.DarkCyan;
			case Color.RandomFire:
				switch(Global.Roll(1,3)){
				case 1:
					return ConsoleColor.Red;
				case 2:
					return ConsoleColor.DarkRed;
				case 3:
					return ConsoleColor.Yellow;
				default:
					return ConsoleColor.Black;
				}
			case Color.RandomIce:
				switch(Global.Roll(1,4)){
				case 1:
					return ConsoleColor.White;
				case 2:
					return ConsoleColor.Cyan;
				case 3:
					return ConsoleColor.Blue;
				case 4:
					return ConsoleColor.DarkBlue;
				default:
					return ConsoleColor.Black;
				}
			case Color.RandomLightning:
				switch(Global.Roll(1,4)){
				case 1:
					return ConsoleColor.White;
				case 2:
					return ConsoleColor.Yellow;
				case 3:
					return ConsoleColor.Yellow;
				case 4:
					return ConsoleColor.DarkYellow;
				default:
					return ConsoleColor.Black;
				}
			default:
				return ConsoleColor.Black;
			}
		}
	}
}
