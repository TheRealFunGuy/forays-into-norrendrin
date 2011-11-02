using System;
using System.Collections.Generic;
namespace Forays{
	public struct colorchar{
		public ConsoleColor color;
		public ConsoleColor bgcolor;
		public char c;
	}
	public static class Screen{
		private static colorchar[,] memory;
		public static colorchar Char(int r,int c){ return memory[r,c]; }
		public static colorchar MapChar(int r,int c){ return memory[r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS]; }
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
/*		public static void WriteMapChar(int r,int c,char ch){
			WriteMapChar(r,c,new colorchar{ color = ConsoleColor.White, bgcolor = ConsoleColor.Black, c = ch });
		}*/
		public static void WriteMapChar(int r,int c,colorchar ch){
			r += Global.MAP_OFFSET_ROWS;
			c += Global.MAP_OFFSET_COLS;
			if(!memory[r,c].Equals(ch)){
				memory[r,c] = ch;
				Console.SetCursorPosition(c,r);
				if(ch.color == Console.ForegroundColor && ch.bgcolor == Console.BackgroundColor){
					Console.Write(ch.c);
				}
				else{
					if(ch.bgcolor == Console.BackgroundColor){
						ConsoleColor oldcolor = Console.ForegroundColor;
						Console.ForegroundColor = ch.color;
						Console.Write(ch.c);
						Console.ForegroundColor = oldcolor;
					}
					else{
						if(ch.color == Console.ForegroundColor){
							ConsoleColor oldcolor = Console.BackgroundColor;
							Console.BackgroundColor = ch.bgcolor;
							Console.Write(ch.c);
							Console.BackgroundColor = oldcolor;
						}
						else{
							ConsoleColor oldcolor = Console.ForegroundColor;
							ConsoleColor oldbgcolor = Console.BackgroundColor;
							Console.ForegroundColor = ch.color;
							Console.BackgroundColor = ch.bgcolor;
							Console.Write(ch.c);
							Console.ForegroundColor = oldcolor;
							Console.BackgroundColor = oldbgcolor;
						}
					}
				}
			}
		}
		public static void WriteMapString(int r,int c,string s){
			if(Global.COLS - c > s.Length){
				s = s.Substring(0); //don't move down to the next line
			}
			else{
				s = s.Substring(0,Global.COLS - c);
			}
			r += Global.MAP_OFFSET_ROWS;
			c += Global.MAP_OFFSET_COLS;
			ConsoleColor oldcolor = Console.ForegroundColor;
			ConsoleColor oldbgcolor = Console.BackgroundColor;
			colorchar cch;
			cch.color = ConsoleColor.Gray;
			cch.bgcolor = ConsoleColor.Black;
			if(s.Length > 0){
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.BackgroundColor = ConsoleColor.Black;
			}
			int i = 0;
			foreach(char ch in s){
				cch.c = ch;
				if(!memory[r,c+i].Equals(cch)){
					memory[r,c+i] = cch;
				}
				++i;
			}
			Console.SetCursorPosition(c,r);
			Console.Write(s);
			if(s.Length > 0){
				Console.ForegroundColor = oldcolor;
				Console.BackgroundColor = oldbgcolor;
			}
		}
/*		public static void WriteChar(string s){ WriteChar(s[0]); }
		public static void WriteChar(char ch){
			colorchar cch;
			cch.c = ch;
			cch.color = ConsoleColor.White;
			WriteChar(cch);
		}
		public static void WriteChar(colorchar ch){
			int i = Console.CursorTop;
			int j = Console.CursorLeft;
			if(!memory[i,j].Equals(ch)){
				memory[i,j] = ch;
				ConsoleColor oldcolor = Console.ForegroundColor;
				Console.ForegroundColor = ch.color;
				Console.Write(ch.c);
				Console.ForegroundColor = oldcolor;
			}
			else{
				if(j == Global.SCREEN_W - 1){
					if(i == Global.SCREEN_H - 1){
						Console.SetCursorPosition(0,0); //loop around, why not?
					}
					else{
						Console.SetCursorPosition(0,i+1);
					}
				}
				else{
					Console.SetCursorPosition(j+1,i);
				}
			}
		}
		public static void WriteChar(int r,int c,colorchar ch){
			if(!memory[r,c].Equals(ch)){
				memory[r,c] = ch;
				Console.SetCursorPosition(c,r);
				ConsoleColor oldcolor = Console.ForegroundColor;
				Console.ForegroundColor = ch.color;
				Console.Write(ch.c);
				Console.ForegroundColor = oldcolor;
			}
			else{
				if(c == Global.SCREEN_W - 1){
					if(r == Global.SCREEN_H - 1){
						Console.SetCursorPosition(0,0); //loop around, why not?
					}
					else{
						Console.SetCursorPosition(0,r+1);
					}
				}
				else{
					Console.SetCursorPosition(c+1,r);
				}
			}
		}*/
	}
}
