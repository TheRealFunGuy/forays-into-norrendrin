using System;
using System.Collections.Generic;
namespace Forays{
	public struct colorchar{
		public ConsoleColor color;
		public ConsoleColor bgcolor;
		public char c;
	}
	public struct colorstring{
		public ConsoleColor color;
		public ConsoleColor bgcolor;
		public string s;
	}
	public static class Screen{
		private static colorchar[,] memory;
		public static colorchar Char(int r,int c){ return memory[r,c]; }
		public static colorchar MapChar(int r,int c){ return memory[r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS]; }
		public static colorchar StatsChar(int r,int c){ return memory[r+1,c+1]; }
		static Screen(){
			memory = new colorchar[Global.SCREEN_H,Global.SCREEN_W];
			for(int i=0;i<Global.SCREEN_H;++i){
				for(int j=0;j<Global.SCREEN_W;++j){
					memory[i,j].c = ' ';
					memory[i,j].color = ConsoleColor.Black;
					memory[i,j].bgcolor = ConsoleColor.Black;
				}
			}
		}
		public static colorchar[,] GetCurrentMap(){
			colorchar[,] result = new colorchar[Global.ROWS,Global.COLS];
			for(int i=0;i<Global.ROWS;++i){
				for(int j=0;j<Global.COLS;++j){
					result[i,j] = MapChar(i,j);
				}
			}
			return result;
		}
		public static void WriteMapChar(int r,int c,colorchar ch){
			r += Global.MAP_OFFSET_ROWS;
			c += Global.MAP_OFFSET_COLS;
			if(!memory[r,c].Equals(ch)){
				memory[r,c] = ch;
				if(ch.color != Console.ForegroundColor){
					Console.ForegroundColor = ch.color;
				}
				if(ch.bgcolor != Console.BackgroundColor){
					Console.BackgroundColor = ch.bgcolor;
				}
				Console.SetCursorPosition(c,r);
				Console.Write(ch.c);
			}
		}
		public static void WriteMapString(int r,int c,string s){
			colorstring cs;
			cs.color = ConsoleColor.Gray;
			cs.bgcolor = ConsoleColor.Black;
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
				if(Console.ForegroundColor != s.color){
					Console.ForegroundColor = s.color;
				}
				if(Console.BackgroundColor != s.bgcolor){
					Console.BackgroundColor = s.bgcolor;
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
		public static void WriteStatsChar(int r,int c,colorchar ch){
			++r;
			++c;
			if(!memory[r,c].Equals(ch)){
				memory[r,c] = ch;
				if(ch.color != Console.ForegroundColor){
					Console.ForegroundColor = ch.color;
				}
				if(ch.bgcolor != Console.BackgroundColor){
					Console.BackgroundColor = ch.bgcolor;
				}
				Console.SetCursorPosition(c,r);
				Console.Write(ch.c);
			}
		}
		public static void WriteStatsString(int r,int c,string s){
			colorstring cs;
			cs.color = ConsoleColor.Gray;
			cs.bgcolor = ConsoleColor.Black;
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
				++c;
				colorchar cch;
				cch.color = s.color;
				cch.bgcolor = s.bgcolor;
				if(Console.ForegroundColor != s.color){
					Console.ForegroundColor = s.color;
				}
				if(Console.BackgroundColor != s.bgcolor){
					Console.BackgroundColor = s.bgcolor;
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
	}
}
