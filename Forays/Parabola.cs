/*Copyright (c) 2013 Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
namespace ParabolaConsoleLib{
	public static class Screen{
		private static colorchar[,] memory;
		private static int H;
		private static int W;
		public static bool linux;
		public static ConsoleColor defaultcolor = ConsoleColor.Gray;
		public static bool wordwrap = false;
		private static bool terminal_bold = false; //for use on linux terminals
		private static readonly string bold_on = (char)27 + "[1m"; //VT100 codes, sweet
		private static readonly string bold_off = (char)27 + "[m";
		public static ConsoleColor ForegroundColor{
			get{
				if(linux && terminal_bold){
					return Console.ForegroundColor+8;
				}
				return Console.ForegroundColor;
			}
			set{
				if(linux && (int)value >= 8){
					Console.ForegroundColor = value - 8;
					if(!terminal_bold){
						terminal_bold = true;
						Console.Write(bold_on);
					}
				}
				else{
					if(linux && terminal_bold){
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
				if(linux && (int)value >= 8){
					Console.BackgroundColor = value - 8;
				}
				else{
					Console.BackgroundColor = value;
				}
			}
		}
		public static void Initialize(int height,int width){
			int os = (int)Environment.OSVersion.Platform;
			if(os == 4 || os == 6 ||  os == 128){
				linux = true;
			}
			else{
				linux = false;
			}
			H = height;
			W = width;
			memory = new colorchar[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					memory[i,j].c = ' ';
					memory[i,j].color = ConsoleColor.Black;
					memory[i,j].bgcolor = ConsoleColor.Black;
				}
			}
			BackgroundColor = Console.BackgroundColor;
			ForegroundColor = Console.ForegroundColor;
			if(linux){
				Clear();
			}
		}
		public static void ResetColors(){
			if(ForegroundColor != defaultcolor){
				ForegroundColor = defaultcolor;
			}
			if(BackgroundColor != ConsoleColor.Black){
				BackgroundColor = ConsoleColor.Black;
			}
		}
		public static void Redraw(){ //this method does NOT need to be called unless something has messed up the screen.
			Console.CursorVisible = false;
			for(int i=0;i<H;++i){    //if you call this every update, the screen will flicker!
				for(int j=0;j<W;++j){
					if(i != H-1 || j != W-1){
						if(memory[i,j].color != ForegroundColor){
							ForegroundColor = memory[i,j].color;
						}
						if(memory[i,j].bgcolor != Console.BackgroundColor || (linux && memory[i,j].c == ' ' && memory[i,j].color == ConsoleColor.Black)){//voodoo here. not sure why this is needed. possible Mono bug.
							BackgroundColor = memory[i,j].bgcolor;
						}
						Console.SetCursorPosition(j,i);
						Console.Write(memory[i,j].c);
					}
				}
			}
		}
		public static colorchar Char(int r,int c){ return memory[r,c]; }
		public static colorchar BlankChar(){ return new colorchar(' ',ConsoleColor.Black); }
		public static colorchar[,] GetCurrentScreen(){
			colorchar[,] result = new colorchar[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					result[i,j] = Char(i,j);
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
			if(r>=0 && r<H && c>=0 && c<W){
				return true;
			}
			return false;
		}
		public static void Clear(){ Clear(0,0,H,W); }
		public static void Clear(int start_row,int start_column,int height,int width){
			Console.CursorVisible = false;
			for(int i=0;i<height;++i){
				Console.SetCursorPosition(start_column,start_row+i);
				Console.Write("".PadRight(width));
				for(int j=0;j<width;++j){
					memory[start_row+i,start_column+j].c = ' ';
					memory[start_row+i,start_column+j].color = ConsoleColor.Black;
					memory[start_row+i,start_column+j].bgcolor = ConsoleColor.Black;
				}
			}
		}
		public static void Write(int r,int c,char ch){
			Write(r,c,new colorchar(ch,defaultcolor));
		}
		public static void Write(int r,int c,char ch,ConsoleColor color){
			Write(r,c,new colorchar(ch,color));
		}
		public static void Write(int r,int c,colorchar ch){
			if(!memory[r,c].Equals(ch)){
				memory[r,c] = ch;
				if(ch.color != ForegroundColor){
					ForegroundColor = ch.color;
				} //note: I changed Console.BackgroundColor to BackgroundColor on the next line. hope it didn't break anything.
				if(ch.bgcolor != BackgroundColor || (linux && ch.c == ' ' && ch.color == ConsoleColor.Black)){//voodoo here. not sure why this is needed. (possible Mono bug)
					BackgroundColor = ch.bgcolor;
				}
				Console.SetCursorPosition(c,r);
				Console.Write(ch.c);
			}
		}
		public static void Write(int r,int c,colorchar[,] array){
			int h = array.GetLength(0);
			int w = array.GetLength(1);
			for(int i=0;i<h;++i){
				for(int j=0;j<w;++j){
					Write(i+r,j+c,array[i,j]);
				}
			}
		}
		public static void Write(int r,int c,List<colorstring> ls){
			int line = r;
			foreach(colorstring cs in ls){
				Write(line,c,cs);
				++line;
			}
		}
		public static void Write(int r,int c,string s){ Write(r,c,new colorstringpart(s,defaultcolor),0,W,H-1); }
		public static void Write(int r,int c,string s,ConsoleColor color){ Write(r,c,new colorstringpart(s,color),0,W,H-1); }
		public static void Write(int r,int c,colorstringpart s){ Write(r,c,s,0,W,H-1); }
		public static void Write(int r,int c,colorstringpart s,int left_border,int width,int bottom_border){
			colorstringpart wrapped = new colorstringpart("",defaultcolor);
			bool carriage_return = false;
			int available_space = (left_border + width) - c;
			if(r == H-1){
				--available_space; //this prevents Parabola from printing to the bottom right corner, which would scroll the screen.
			}
			if(s.s.Length > 0){
				colorchar cch;
				cch.color = s.color;
				cch.bgcolor = s.bgcolor;
				if(ForegroundColor != s.color){
					ForegroundColor = s.color;
				}
				if(BackgroundColor != s.bgcolor){
					BackgroundColor = s.bgcolor;
				}
				string print_string = s.s;
				int i = 0;
				bool changed = false;
				foreach(char ch in s.s){
					if(ch == '\r' || ch == '\n'){
						print_string = s.s.Substring(0,i);
						wrapped.s = s.s.Substring(i+1);
						wrapped.bgcolor = s.bgcolor;
						wrapped.color = s.color;
						if(ch == '\r'){
							carriage_return = true;
						}
						break;
					}
					else{
						if(i >= available_space){
							if(wordwrap){
								wrapped.s = s.s.Substring(i);
								wrapped.bgcolor = s.bgcolor;
								wrapped.color = s.color;
							}
							print_string = s.s.Substring(0,i);
							break;
						}
						else{
							cch.c = ch;
							if(!memory[r,c+i].Equals(cch)){
								memory[r,c+i] = cch;
								changed = true;
							}
							++i;
						}
					}
				}
				if(changed){
					Console.SetCursorPosition(c,r);
					Console.Write(print_string);
				}
			}
			if(wrapped.s.Length > 0){
				if(carriage_return){
					Write(r,left_border,wrapped,left_border,width,bottom_border);
				}
				else{
					if(r < bottom_border){
						Write(r+1,left_border,wrapped,left_border,width,bottom_border);
					}
				}
			}
		}
		public static void Write(int r,int c,colorstring cs){ Write(r,c,cs,0,W,H-1); }
		public static void Write(int r,int c,colorstring cs,int left_border,int width,int bottom_border){
			if(cs.Length() > 0){
				int row = r;
				int col = c;
				foreach(colorstringpart s1 in cs.strings){
					colorstringpart s = new colorstringpart(s1.s,s1.color,s1.bgcolor);
					colorstringpart wrapped = new colorstringpart("",defaultcolor);
					bool carriage_return = false;
					int available_space = (left_border + width) - col;
					if(row == H-1){
						--available_space; //this prevents Parabola from printing to the bottom right corner, which would scroll the screen.
					}
					colorchar cch;
					cch.color = s.color;
					cch.bgcolor = s.bgcolor;
					if(ForegroundColor != s.color){
						ForegroundColor = s.color;
					}
					if(BackgroundColor != s.bgcolor){
						BackgroundColor = s.bgcolor;
					}
					string print_string = s.s;
					int i = 0;
					bool changed = false;
					bool last_char_was_NL_or_CR = false;
					foreach(char ch in s.s){
						if(ch == '\r' || ch == '\n'){
							print_string = s.s.Substring(0,i);
							wrapped.s = s.s.Substring(i+1);
							if(wrapped.s.Length == 0){
								last_char_was_NL_or_CR = true;
							}
							wrapped.bgcolor = s.bgcolor;
							wrapped.color = s.color;
							if(ch == '\r'){
								carriage_return = true;
							}
							break;
						}
						else{
							if(i >= available_space){
								if(wordwrap){
									wrapped.s = s.s.Substring(i);
									wrapped.bgcolor = s.bgcolor;
									wrapped.color = s.color;
								}
								print_string = s.s.Substring(0,i);
								break;
							}
							else{
								cch.c = ch;
								if(!memory[row,col+i].Equals(cch)){
									memory[row,col+i] = cch;
									changed = true;
								}
								++i;
							}
						}
					}
					if(changed){
						Console.SetCursorPosition(col,row);
						Console.Write(print_string);
					}
					col += print_string.Length;
					if(wrapped.s.Length > 0){
						if(carriage_return){
							Write(row,left_border,wrapped,left_border,width,bottom_border);
							col = left_border + wrapped.s.Length;
						}
						else{
							if(row < bottom_border){
								++row;
								Write(row,left_border,wrapped,left_border,width,bottom_border);
								col = left_border + wrapped.s.Length;
							}
						}
					}
					else{
						if(last_char_was_NL_or_CR){
							if(carriage_return){
								col = left_border;
							}
							else{
								if(row < bottom_border){
									++row;
									col = left_border;
								}
							}
						}
					}
					if(col >= left_border + width){
						if(wordwrap){
							++row;
							col = left_border;
							if(row > bottom_border){
								break;
							}
						}
						else{
							break;
						}
					}
				}
			}
		}
		public static void DrawRectangularPartOfArray(colorchar[,] array,int row,int col,int height,int width){
			colorstringpart s;
			s.s = "";
			s.bgcolor = ConsoleColor.Black;
			s.color = ConsoleColor.Black;
			int current_c = col;
			for(int i=row;i<row+height;++i){
				s.s = "";
				current_c = col;
				for(int j=col;j<col+width;++j){
					colorchar ch = array[i,j];
					if(ch.color != s.color){
						if(s.s.Length > 0){
							Screen.Write(i,current_c,s);
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
				Screen.Write(i,current_c,s);
			}
		}
	}
	public class Panel{
		private int row;
		private int col;
		private int H;
		private int W;
		public ConsoleColor defaultcolor = ConsoleColor.Gray;
		public Panel(int start_row,int start_column,int height,int width){
			row = start_row;
			col = start_column;
			H = height;
			W = width;
		}
		public void Redraw(){ //this method does NOT need to be called unless something has messed up the screen.
			Console.CursorVisible = false;    //if you call Redraw() every update, the screen will flicker!
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					if(Screen.Char(row+i,col+j).color != Screen.ForegroundColor){
						Screen.ForegroundColor = Screen.Char(row+i,col+j).color;
					}
					if(Screen.Char(row+i,col+j).bgcolor != Screen.BackgroundColor || (Screen.linux && Screen.Char(row+i,col+j).c == ' ' && Screen.Char(row+i,col+j).color == ConsoleColor.Black)){//voodoo here. not sure why this is needed. possible Mono bug.
						Screen.BackgroundColor = Screen.Char(row+i,col+j).bgcolor;
					}
					Console.SetCursorPosition(j,i);
					Console.Write(Screen.Char(row+i,col+j).c);
				}
			}
		}
		public colorchar Char(int r,int c){ return Screen.Char(row+r,col+c); }
		public colorchar[,] GetCurrentPanel(){
			colorchar[,] result = new colorchar[H,W];
			for(int i=0;i<H;++i){
				for(int j=0;j<W;++j){
					result[i,j] = Screen.Char(row+i,col+j);
				}
			}
			return result;
		}
		public bool BoundsCheck(int r,int c){
			if(r>=0 && r<H && c>=0 && c<W){
				return true;
			}
			return false;
		}
		public void Clear(){ Screen.Clear(row,col,H,W); }
		public void Write(int r,int c,char ch){
			Screen.Write(row+r,col+c,new colorchar(ch,defaultcolor));
		}
		public void Write(int r,int c,char ch,ConsoleColor color){
			Screen.Write(row+r,col+c,new colorchar(ch,color));
		}
		public void Write(int r,int c,colorchar ch){
			Screen.Write(row+r,col+c,ch);
		}
		public void Write(int r,int c,colorchar[,] array){
			Screen.Write(row+r,col+c,array);
		}
		public void Write(int r,int c,List<colorstring> ls){
			Screen.Write(row+r,col+c,ls);
		}
		public void Write(int r,int c,string s){ Screen.Write(row+r,col+c,new colorstringpart(s,defaultcolor),col,W,(row+H)-1); }
		public void Write(int r,int c,string s,ConsoleColor color){ Screen.Write(row+r,col+c,new colorstringpart(s,color),col,W,(row+H)-1); }
		public void Write(int r,int c,colorstringpart s){
			Screen.Write(row+r,col+c,s,col,W,(row+H)-1);
		}
		public void Write(int r,int c,colorstring cs){
			Screen.Write(row+r,col+c,cs,col,W,(row+H)-1);
		}
		public void DrawRectangularPartOfArray(colorchar[,] array,int row,int col,int height,int width){
			Screen.DrawRectangularPartOfArray(array,this.row+row,this.col+col,height,width);
		}
	}
	public struct colorchar{
		public ConsoleColor color;
		public ConsoleColor bgcolor;
		public char c;
		public colorchar(char c_,ConsoleColor color_){
			color = color_;
			bgcolor = ConsoleColor.Black;
			c = c_;
		}
		public colorchar(char c_,ConsoleColor color_,ConsoleColor bgcolor_){
			color = color_;
			bgcolor = bgcolor_;
			c = c_;
		}
	}
	public struct colorstringpart{
		public ConsoleColor color;
		public ConsoleColor bgcolor;
		public string s;
		public colorstringpart(string s_,ConsoleColor color_){
			color = color_;
			bgcolor = ConsoleColor.Black;
			s = s_;
		}
		public colorstringpart(string s_,ConsoleColor color_,ConsoleColor bgcolor_){
			color = color_;
			bgcolor = bgcolor_;
			s = s_;
		}
	}
	public class colorstring{
		public List<colorstringpart> strings = new List<colorstringpart>();
		public int Length(){
			int total = 0;
			foreach(colorstringpart s in strings){
				total += s.s.Length;
			}
			return total;
		}
		public colorstring(string s1,ConsoleColor c1){ //constructors, from 1 to 10...any more and you can pass in a list of colorstringparts.
			strings.Add(new colorstringpart(s1,c1));
		}
		public colorstring(string s1,ConsoleColor c1,string s2,ConsoleColor c2){
			strings.Add(new colorstringpart(s1,c1));
			strings.Add(new colorstringpart(s2,c2));
		}
		public colorstring(string s1,ConsoleColor c1,string s2,ConsoleColor c2,string s3,ConsoleColor c3){
			strings.Add(new colorstringpart(s1,c1));
			strings.Add(new colorstringpart(s2,c2));
			strings.Add(new colorstringpart(s3,c3));
		}
		public colorstring(string s1,ConsoleColor c1,string s2,ConsoleColor c2,string s3,ConsoleColor c3,string s4,ConsoleColor c4){
			strings.Add(new colorstringpart(s1,c1));
			strings.Add(new colorstringpart(s2,c2));
			strings.Add(new colorstringpart(s3,c3));
			strings.Add(new colorstringpart(s4,c4));
		}
		public colorstring(string s1,ConsoleColor c1,string s2,ConsoleColor c2,string s3,ConsoleColor c3,string s4,ConsoleColor c4,string s5,ConsoleColor c5){
			strings.Add(new colorstringpart(s1,c1));
			strings.Add(new colorstringpart(s2,c2));
			strings.Add(new colorstringpart(s3,c3));
			strings.Add(new colorstringpart(s4,c4));
			strings.Add(new colorstringpart(s5,c5));
		}
		public colorstring(string s1,ConsoleColor c1,string s2,ConsoleColor c2,string s3,ConsoleColor c3,string s4,ConsoleColor c4,string s5,ConsoleColor c5,string s6,ConsoleColor c6){
			strings.Add(new colorstringpart(s1,c1));
			strings.Add(new colorstringpart(s2,c2));
			strings.Add(new colorstringpart(s3,c3));
			strings.Add(new colorstringpart(s4,c4));
			strings.Add(new colorstringpart(s5,c5));
			strings.Add(new colorstringpart(s6,c6));
		}
		public colorstring(string s1,ConsoleColor c1,string s2,ConsoleColor c2,string s3,ConsoleColor c3,string s4,ConsoleColor c4,string s5,ConsoleColor c5,string s6,ConsoleColor c6,string s7,ConsoleColor c7){
			strings.Add(new colorstringpart(s1,c1));
			strings.Add(new colorstringpart(s2,c2));
			strings.Add(new colorstringpart(s3,c3));
			strings.Add(new colorstringpart(s4,c4));
			strings.Add(new colorstringpart(s5,c5));
			strings.Add(new colorstringpart(s6,c6));
			strings.Add(new colorstringpart(s7,c7));
		}
		public colorstring(string s1,ConsoleColor c1,string s2,ConsoleColor c2,string s3,ConsoleColor c3,string s4,ConsoleColor c4,string s5,ConsoleColor c5,string s6,ConsoleColor c6,string s7,ConsoleColor c7,string s8,ConsoleColor c8){
			strings.Add(new colorstringpart(s1,c1));
			strings.Add(new colorstringpart(s2,c2));
			strings.Add(new colorstringpart(s3,c3));
			strings.Add(new colorstringpart(s4,c4));
			strings.Add(new colorstringpart(s5,c5));
			strings.Add(new colorstringpart(s6,c6));
			strings.Add(new colorstringpart(s7,c7));
			strings.Add(new colorstringpart(s8,c8));
		}
		public colorstring(string s1,ConsoleColor c1,string s2,ConsoleColor c2,string s3,ConsoleColor c3,string s4,ConsoleColor c4,string s5,ConsoleColor c5,string s6,ConsoleColor c6,string s7,ConsoleColor c7,string s8,ConsoleColor c8,string s9,ConsoleColor c9){
			strings.Add(new colorstringpart(s1,c1));
			strings.Add(new colorstringpart(s2,c2));
			strings.Add(new colorstringpart(s3,c3));
			strings.Add(new colorstringpart(s4,c4));
			strings.Add(new colorstringpart(s5,c5));
			strings.Add(new colorstringpart(s6,c6));
			strings.Add(new colorstringpart(s7,c7));
			strings.Add(new colorstringpart(s8,c8));
			strings.Add(new colorstringpart(s9,c9));
		}
		public colorstring(string s1,ConsoleColor c1,string s2,ConsoleColor c2,string s3,ConsoleColor c3,string s4,ConsoleColor c4,string s5,ConsoleColor c5,string s6,ConsoleColor c6,string s7,ConsoleColor c7,string s8,ConsoleColor c8,string s9,ConsoleColor c9,string s10,ConsoleColor c10){
			strings.Add(new colorstringpart(s1,c1));
			strings.Add(new colorstringpart(s2,c2));
			strings.Add(new colorstringpart(s3,c3));
			strings.Add(new colorstringpart(s4,c4));
			strings.Add(new colorstringpart(s5,c5));
			strings.Add(new colorstringpart(s6,c6));
			strings.Add(new colorstringpart(s7,c7));
			strings.Add(new colorstringpart(s8,c8));
			strings.Add(new colorstringpart(s9,c9));
			strings.Add(new colorstringpart(s10,c10));
		}
		public colorstring(params colorstringpart[] colorstringparts){
			if(colorstringparts != null && colorstringparts.Length > 0){
				foreach(colorstringpart cs in colorstringparts){
					strings.Add(cs);
				}
			}
		}
		public static colorstring operator +(colorstring one,colorstring two){
			colorstring result = new colorstring();
			foreach(colorstringpart s in one.strings){
				result.strings.Add(s);
			}
			foreach(colorstringpart s in two.strings){
				result.strings.Add(s);
			}
			return result;
		}
	}
}

