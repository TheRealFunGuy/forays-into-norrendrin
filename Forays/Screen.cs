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
using System.Threading;
namespace Forays{
	/*public interface ICoord{ //Now uses ICoords, so it'll work with anything that has row and column
		int row{get;set;}
		int col{get;set;} //scratch that, c# doesn't make this easy enough yet.
	}*/
	public enum Color{Black,White,Gray,Red,Green,Blue,Yellow,Magenta,Cyan,DarkGray,DarkRed,DarkGreen,DarkBlue,DarkYellow,DarkMagenta,DarkCyan,RandomFire,RandomIce,RandomLightning,RandomPrismatic,RandomDark,RandomBright};
	public struct colorchar{ //todo: engine code version should be char,color
		public Color color;
		public Color bgcolor;
		public char c;
		public colorchar(char c_,Color color_,Color bgcolor_){
			color = color_;
			bgcolor = bgcolor_;
			c = c_;
		}
		public colorchar(char c_,Color color_){
			color = color_;
			bgcolor = Color.Black;
			c = c_;
		}
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
	public struct cstr{ //todo: engine code version should be string,color
		public Color color;
		public Color bgcolor;
		public string s;
		public cstr(string s_,Color color_){
			color = color_;
			bgcolor = Color.Black;
			s = s_;
		}
		public cstr(string s_,Color color_,Color bgcolor_){
			color = color_;
			bgcolor = bgcolor_;
			s = s_;
		}
		public cstr(Color color_,string s_){
			color = color_;
			bgcolor = Color.Black;
			s = s_;
		}
		public cstr(Color color_,Color bgcolor_,string s_){
			color = color_;
			bgcolor = bgcolor_;
			s = s_;
		}
	}
	public class colorstring{
		public List<cstr> strings = new List<cstr>();
		public int Length(){
			int total = 0;
			foreach(cstr s in strings){
				total += s.s.Length;
			}
			return total;
		}
		public colorstring(string s1,Color c1){
			strings.Add(new cstr(s1,c1));
		}
		public colorstring(string s1,Color c1,string s2,Color c2){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
		}
		public colorstring(string s1,Color c1,string s2,Color c2,string s3,Color c3){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
			strings.Add(new cstr(s3,c3));
		}
		public colorstring(string s1,Color c1,string s2,Color c2,string s3,Color c3,string s4,Color c4){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
			strings.Add(new cstr(s3,c3));
			strings.Add(new cstr(s4,c4));
		}
		public colorstring(string s1,Color c1,string s2,Color c2,string s3,Color c3,string s4,Color c4,string s5,Color c5){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
			strings.Add(new cstr(s3,c3));
			strings.Add(new cstr(s4,c4));
			strings.Add(new cstr(s5,c5));
		}
		public colorstring(string s1,Color c1,string s2,Color c2,string s3,Color c3,string s4,Color c4,string s5,Color c5,string s6,Color c6){
			strings.Add(new cstr(s1,c1));
			strings.Add(new cstr(s2,c2));
			strings.Add(new cstr(s3,c3));
			strings.Add(new cstr(s4,c4));
			strings.Add(new cstr(s5,c5));
			strings.Add(new cstr(s6,c6));
		}
		public colorstring(params cstr[] cstrs){
			if(cstrs != null && cstrs.Length > 0){
				foreach(cstr cs in cstrs){
					strings.Add(cs);
				}
			}
		}
		public static colorstring operator +(colorstring one,colorstring two){
			colorstring result = new colorstring();
			foreach(cstr s in one.strings){
				result.strings.Add(s);
			}
			foreach(cstr s in two.strings){
				result.strings.Add(s);
			}
			return result;
		}
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
		public static colorchar StatsChar(int r,int c){ return memory[r,c]; } //changed from r+1,c
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
		public static colorchar[,] GetCurrentScreen(){
			colorchar[,] result = new colorchar[Global.SCREEN_H,Global.SCREEN_W];
			for(int i=0;i<Global.SCREEN_H;++i){
				for(int j=0;j<Global.SCREEN_W;++j){
					result[i,j] = Char(i,j);
				}
			}
			return result;
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
		public static colorchar[,] GetCurrentRect(int row,int col,int height,int width){
			colorchar[,] result = new colorchar[height,width];
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					result[i,j] = Char(row+i,col+j);
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
				for(int j=0;j<Global.SCREEN_W;++j){
					memory[i,j].c = ' ';
					memory[i,j].color = Color.Black;
					memory[i,j].bgcolor = Color.Black;
				}
			}
		}
		public static void WriteChar(int r,int c,char ch){
			WriteChar(r,c,new colorchar(Color.Gray,ch));
		}
		public static void WriteChar(int r,int c,char ch,Color color){
			WriteChar(r,c,new colorchar(ch,color));
		}
		public static void WriteChar(int r,int c,colorchar ch){
			if(!memory[r,c].Equals(ch)){
				ch.color = ResolveColor(ch.color);
				ch.bgcolor = ResolveColor(ch.bgcolor);
				memory[r,c] = ch;
				ConsoleColor co = GetColor(ch.color);
				if(co != ForegroundColor){
					ForegroundColor = co;
				}
				co = GetColor(ch.bgcolor);
				if(co != Console.BackgroundColor || (Global.LINUX && ch.c == ' ' && ch.color == Color.Black)){//voodoo here. not sure why this is needed. (possible Mono bug)
					BackgroundColor = co;
				}
				Console.SetCursorPosition(c,r);
				Console.Write(ch.c);
			}
		}
		public static void WriteArray(int r,int c,colorchar[,] array){
			int h = array.GetLength(0);
			int w = array.GetLength(1);
			for(int i=0;i<h;++i){
				for(int j=0;j<w;++j){
					WriteChar(i+r,j+c,array[i,j]);
				}
			}
		}
		public static void WriteList(int r,int c,List<colorstring> ls){
			int line = r;
			foreach(colorstring cs in ls){
				WriteString(line,c,cs);
				++line;
			}
		}
		public static void WriteString(int r,int c,string s){ WriteString(r,c,new cstr(Color.Gray,s)); }
		public static void WriteString(int r,int c,string s,Color color){ WriteString(r,c,new cstr(s,color)); }
		public static void WriteString(int r,int c,cstr s){
			if(Global.SCREEN_W - c > s.s.Length){
				//s.s = s.s.Substring(0,; //don't move down to the next line
			}
			else{
				s.s = s.s.Substring(0,Global.SCREEN_W - c);
			}
			if(s.s.Length > 0){
				s.color = ResolveColor(s.color);
				s.bgcolor = ResolveColor(s.bgcolor);
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
				bool changed = false;
				foreach(char ch in s.s){
					cch.c = ch;
					if(!memory[r,c+i].Equals(cch)){
						memory[r,c+i] = cch;
						changed = true;
					}
					++i;
				}
				if(changed){
					Console.SetCursorPosition(c,r);
					Console.Write(s.s);
				}
			}
		}
		public static void WriteString(int r,int c,colorstring cs){
			if(cs.Length() > 0){
				int pos = c;
				foreach(cstr s1 in cs.strings){
					cstr s = new cstr(s1.s,s1.color,s1.bgcolor);
					if(s.s.Length + pos > Global.SCREEN_W){
						s.s = s.s.Substring(0,Global.SCREEN_W - pos);
					}
					s.color = ResolveColor(s.color);
					s.bgcolor = ResolveColor(s.bgcolor);
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
					bool changed = false;
					foreach(char ch in s.s){
						cch.c = ch;
						if(!memory[r,pos+i].Equals(cch)){
							memory[r,pos+i] = cch;
							changed = true;
						}
						++i;
					}
					if(changed){
						Console.SetCursorPosition(pos,r);
						Console.Write(s.s);
					}
					pos += s.s.Length;
				}
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
		public static void WriteMapChar(int r,int c,char ch){
			WriteMapChar(r,c,new colorchar(Color.Gray,ch));
		}
		public static void WriteMapChar(int r,int c,char ch,Color color){
			WriteMapChar(r,c,new colorchar(ch,color));
		}
		public static void WriteMapChar(int r,int c,colorchar ch){
			WriteChar(r+Global.MAP_OFFSET_ROWS,c+Global.MAP_OFFSET_COLS,ch);
		}
		public static void WriteMapString(int r,int c,string s){
			cstr cs;
			cs.color = Color.Gray;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteMapString(r,c,cs);
		}
		public static void WriteMapString(int r,int c,string s,Color color){
			cstr cs;
			cs.color = color;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteMapString(r,c,cs);
		}
		public static void WriteMapString(int r,int c,cstr s){
			if(Global.COLS - c > s.s.Length){
				//s.s = s.s.Substring(0); //don't move down to the next line
			}
			else{
				s.s = s.s.Substring(0,Global.COLS - c);
			}
			if(s.s.Length > 0){
				r += Global.MAP_OFFSET_ROWS;
				c += Global.MAP_OFFSET_COLS;
				s.color = ResolveColor(s.color);
				s.bgcolor = ResolveColor(s.bgcolor);
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
				bool changed = false;
				foreach(char ch in s.s){
					cch.c = ch;
					if(!memory[r,c+i].Equals(cch)){
						memory[r,c+i] = cch;
						changed = true;
					}
					++i;
				}
				if(changed){
					Console.SetCursorPosition(c,r);
					Console.Write(s.s);
				}
			}
		}
		public static void WriteMapString(int r,int c,colorstring cs){
			if(cs.Length() > 0){
				r += Global.MAP_OFFSET_ROWS;
				c += Global.MAP_OFFSET_COLS;
				int cpos = c;
				foreach(cstr s1 in cs.strings){
					cstr s = new cstr(s1.s,s1.color,s1.bgcolor);
					if(cpos-Global.MAP_OFFSET_COLS + s.s.Length > Global.COLS){
						s.s = s.s.Substring(0,Global.COLS-(cpos-Global.MAP_OFFSET_COLS));
					}
					s.color = ResolveColor(s.color);
					s.bgcolor = ResolveColor(s.bgcolor);
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
					bool changed = false;
					foreach(char ch in s.s){
						cch.c = ch;
						if(!memory[r,cpos+i].Equals(cch)){
							memory[r,cpos+i] = cch;
							changed = true;
						}
						++i;
					}
					if(changed){
						Console.SetCursorPosition(cpos,r);
						Console.Write(s.s);
					}
					cpos += s.s.Length;
				}
				/*if(cpos-Global.MAP_OFFSET_COLS < Global.COLS){
					WriteString(r,cpos,"".PadRight(Global.COLS-(cpos-Global.MAP_OFFSET_COLS)));
				}*/
			}
		}
		public static void WriteStatsChar(int r,int c,colorchar ch){ WriteChar(r,c,ch); } //was r+1,c
		public static void WriteStatsString(int r,int c,string s){
			cstr cs;
			cs.color = Color.Gray;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteStatsString(r,c,cs);
		}
		public static void WriteStatsString(int r,int c,string s,Color color){
			cstr cs;
			cs.color = color;
			cs.bgcolor = Color.Black;
			cs.s = s;
			WriteStatsString(r,c,cs);
		}
		public static void WriteStatsString(int r,int c,cstr s){
			if(12 - c > s.s.Length){
				//s.s = s.s.Substring(0); //don't move down to the next line - 12 is the width of the stats area
			}
			else{
				s.s = s.s.Substring(0,12 - c);
			}
			if(s.s.Length > 0){
				//++r; //was ++r
				s.color = ResolveColor(s.color);
				s.bgcolor = ResolveColor(s.bgcolor);
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
		public static void MapDrawWithStrings(colorchar[,] array,int row,int col,int height,int width){
			cstr s;
			s.s = "";
			s.bgcolor = Color.Black;
			s.color = Color.Black;
			int current_c = col;
			for(int i=row;i<row+height;++i){
				s.s = "";
				current_c = col;
				for(int j=col;j<col+width;++j){
					colorchar ch = array[i,j];
					if(Screen.ResolveColor(ch.color) != s.color){
						if(s.s.Length > 0){
							Screen.WriteMapString(i,current_c,s);
							s.s = "";
							s.s += ch.c;
							s.color = ch.color;
							current_c = j;
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
				Screen.WriteMapString(i,current_c,s);
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
		public static void AnimateMapCells(List<pos> cells,List<colorchar> chars){ AnimateMapCells(cells,chars,50); }
		public static void AnimateMapCells(List<pos> cells,List<colorchar> chars,int duration){
			List<colorchar> prev = new List<colorchar>();
			int idx = 0;
			foreach(pos p in cells){
				prev.Add(MapChar(p.row,p.col));
				WriteMapChar(p.row,p.col,chars[idx]);
				++idx;
			}
			Thread.Sleep(duration);
			idx = 0;
			foreach(pos p in cells){
				WriteMapChar(p.row,p.col,prev[idx]);
				++idx;
			}
		}
		public static void AnimateMapCells(List<pos> cells,colorchar ch){ AnimateMapCells(cells,ch,50); }
		public static void AnimateMapCells(List<pos> cells,colorchar ch,int duration){
			List<colorchar> prev = new List<colorchar>();
			int idx = 0;
			foreach(pos p in cells){
				prev.Add(MapChar(p.row,p.col));
				WriteMapChar(p.row,p.col,ch);
				++idx;
			}
			Thread.Sleep(duration);
			idx = 0;
			foreach(pos p in cells){
				WriteMapChar(p.row,p.col,prev[idx]);
				++idx;
			}
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
		public static void AnimateBoltProjectile(List<Tile> list,Color color){ AnimateBoltProjectile(list,color,50); }
		public static void AnimateBoltProjectile(List<Tile> list,Color color,int duration){
			Console.CursorVisible = false;
			colorchar ch;
			ch.color = color;
			ch.bgcolor = Color.Black;
			ch.c='!';
			switch(list[0].DirectionOf(list[list.Count-1])){
			case 7:
			case 3:
				ch.c = '\\';
				break;
			case 8:
			case 2:
				ch.c = '|';
				break;
			case 9:
			case 1:
				ch.c = '/';
				break;
			case 4:
			case 6:
				ch.c = '-';
				break;
			}
			list.RemoveAt(0);
			foreach(Tile t in list){
				AnimateMapCell(t.row,t.col,ch,duration);
			}
			Console.CursorVisible = true;
		}
		public static void AnimateExplosion(PhysicalObject obj,int radius,colorchar ch){
			AnimateExplosion(obj,radius,ch,50,false);
		}
		public static void AnimateExplosion(PhysicalObject obj,int radius,colorchar ch,bool single_frame){
			AnimateExplosion(obj,radius,ch,50,single_frame);
		}
		public static void AnimateExplosion(PhysicalObject obj,int radius,colorchar ch,int duration){
			AnimateExplosion(obj,radius,ch,duration,false);
		}
		public static void AnimateExplosion(PhysicalObject obj,int radius,colorchar ch,int duration,bool single_frame){
			Console.CursorVisible = false;
			colorchar[,] prev = new colorchar[radius*2+1,radius*2+1];
			for(int i=0;i<=radius*2;++i){
				for(int j=0;j<=radius*2;++j){
					if(MapBoundsCheck(obj.row-radius+i,obj.col-radius+j)){
						prev[i,j] = MapChar(obj.row-radius+i,obj.col-radius+j);
					}
				}
			}
			if(!single_frame){
				for(int i=0;i<=radius;++i){
					foreach(Tile t in obj.TilesAtDistance(i)){
						WriteMapChar(t.row,t.col,ch);
					}
					Thread.Sleep(duration);
				}
			}
			else{
				foreach(Tile t in obj.TilesWithinDistance(radius)){
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
		public static void AnimateBoltBeam(List<Tile> list,Color color){ AnimateBoltBeam(list,color,50); }
		public static void AnimateBoltBeam(List<Tile> list,Color color,int duration){
			Console.CursorVisible = false;
			colorchar ch;
			ch.color = color;
			ch.bgcolor = Color.Black;
			ch.c='!';
			switch(list[0].DirectionOf(list[list.Count-1])){
			case 7:
			case 3:
				ch.c = '\\';
				break;
			case 8:
			case 2:
				ch.c = '|';
				break;
			case 9:
			case 1:
				ch.c = '/';
				break;
			case 4:
			case 6:
				ch.c = '-';
				break;
			}
			list.RemoveAt(0);
			List<colorchar> memlist = new List<colorchar>();
			foreach(Tile t in list){
				memlist.Add(MapChar(t.row,t.col));
				WriteMapChar(t.row,t.col,ch);
				Thread.Sleep(duration);
			}
			int i = 0;
			foreach(Tile t in list){
				WriteMapChar(t.row,t.col,memlist[i++]);
			}
			Console.CursorVisible = true;
		}
		public static void AnimateBeam(List<Tile> list,colorchar ch){ AnimateBeam(list,ch,50); }
		public static void AnimateBeam(List<Tile> list,colorchar ch,int duration){
			Console.CursorVisible = false;
			list.RemoveAt(0);
			List<colorchar> memlist = new List<colorchar>();
			foreach(Tile t in list){
				memlist.Add(MapChar(t.row,t.col));
				WriteMapChar(t.row,t.col,ch);
				Thread.Sleep(duration);
			}
			int i = 0;
			foreach(Tile t in list){
				WriteMapChar(t.row,t.col,memlist[i++]);
			}
			Console.CursorVisible = true;
		}
		public static void AnimateStorm(pos origin,int radius,int num_frames,int num_per_frame,char c,Color color){
			AnimateStorm(origin,radius,num_frames,num_per_frame,new colorchar(c,color));
		}
		public static void AnimateStorm(pos origin,int radius,int num_frames,int num_per_frame,colorchar ch){
			for(int i=0;i<num_frames;++i){
				List<pos> cells = new List<pos>();
				List<pos> nearby = origin.PositionsWithinDistance(radius);
				for(int j=0;j<num_per_frame;++j){
					cells.Add(nearby.RemoveRandom());
				}
				Screen.AnimateMapCells(cells,ch);
			}
		}
		public static void DrawMapBorder(colorchar ch){
			for(int i=0;i<Global.ROWS;i+=Global.ROWS-1){
				for(int j=0;j<Global.COLS;++j){
					WriteMapChar(i,j,ch);
				}
			}
			for(int j=0;j<Global.COLS;j+=Global.COLS-1){
				for(int i=0;i<Global.ROWS;++i){
					WriteMapChar(i,j,ch);
				}
			}
			ResetColors();
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
			case Color.RandomIce:
			case Color.RandomLightning:
			case Color.RandomPrismatic:
			case Color.RandomDark:
			case Color.RandomBright:
				return GetColor(ResolveColor(c));
			default:
				return ConsoleColor.Black;
			}
		}
		public static Color ResolveColor(Color c){
			switch(c){
			case Color.RandomFire:
				switch(Global.Roll(1,3)){
				case 1:
					return Color.Red;
				case 2:
					return Color.DarkRed;
				case 3:
					return Color.Yellow;
				default:
					return Color.Black;
				}
			case Color.RandomIce:
				switch(Global.Roll(1,4)){
				case 1:
					return Color.White;
				case 2:
					return Color.Cyan;
				case 3:
					return Color.Blue;
				case 4:
					return Color.DarkBlue;
				default:
					return Color.Black;
				}
			case Color.RandomLightning:
				switch(Global.Roll(1,4)){
				case 1:
					return Color.White;
				case 2:
					return Color.Yellow;
				case 3:
					return Color.Yellow;
				case 4:
					return Color.DarkYellow;
				default:
					return Color.Black;
				}
			case Color.RandomPrismatic:
				switch(Global.Roll(3)){
				case 1:
					return Color.Red;
				case 2:
					return Color.Blue;
				case 3:
					return Color.Yellow;
				default:
					return Color.Black;
				}
			case Color.RandomDark:
				switch(Global.Roll(7)){
				case 1:
					return Color.DarkBlue;
				case 2:
					return Color.DarkCyan;
				case 3:
					return Color.DarkGray;
				case 4:
					return Color.DarkGreen;
				case 5:
					return Color.DarkMagenta;
				case 6:
					return Color.DarkRed;
				case 7:
					return Color.DarkYellow;
				default:
					return Color.Black;
				}
			case Color.RandomBright:
				switch(Global.Roll(8)){
				case 1:
					return Color.Blue;
				case 2:
					return Color.Cyan;
				case 3:
					return Color.Gray;
				case 4:
					return Color.Green;
				case 5:
					return Color.Magenta;
				case 6:
					return Color.Red;
				case 7:
					return Color.Yellow;
				case 8:
					return Color.White;
				default:
					return Color.Black;
				}
			default:
				return c;
			}
		}
	}
}
