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
	public class Buffer{
		private int max_length;
		private string str,str2;
		private string[] log;
		private int position;
		public static Map M{get;set;}
		public static Actor player{get;set;}
		public Buffer(Game g){
			max_length=Global.COLS; //because the message window runs along the top of the map
			str = "";
			str2 = "";
			log=new string[20];
			for(int i=0;i<20;++i){
				log[i] = "";
			}
			position=0;
			M = g.M;
			player = g.player;
		}
		public void Add(string s,params PhysicalObject[] objs){ //if there's at least one object, the player must be able to
			bool add = false;
			if(objs != null && objs.Length > 0){ //see at least one of them. if not, no message is added. 
				foreach(PhysicalObject obj in objs){
					if(obj == player || player.CanSee(obj)){
						add = true;
						break;
					}
				}
			}
			else{
				add = true;
			}
			if(add){
				if(Char.IsLetter(s[0])){
					char[] c = s.ToCharArray();
					c[0] = Char.ToUpper(s[0]);
					s = new string(c);
				}
				str = str + s;
				while(str.Length > max_length){
					for(int i=max_length-7;i>=0;--i){
						if(str.Substring(i,1)==" "){
							str2 = str.Substring(i+1);
							str = str.Substring(0,i+1);
							break;
						}
					}
					Screen.ResetColors();
					Print(false);
				}
			}
		}
		public void DisplayNow(string s){
			Console.CursorVisible = false;
			Console.SetCursorPosition(Global.MAP_OFFSET_COLS,1);
			Console.Write(s.PadRight(Global.COLS));
			Console.SetCursorPosition(Global.MAP_OFFSET_COLS + s.Length,1);
			//Console.CursorVisible = true;
		}
		public void DisplayNow(){ //displays whatever is in the buffer. used before animations.
			Console.CursorVisible = false;
			Screen.ResetColors();
			Console.SetCursorPosition(Global.MAP_OFFSET_COLS,1);
			Console.Write(str.PadRight(Global.COLS));
			//Console.SetCursorPosition(Global.MAP_OFFSET_COLS + str.Length,1);
			//Console.CursorVisible = true;
		}
		public void Print(bool special_message){
			Console.CursorVisible = false;
			if(str != ""){
				if(str != "You regenerate. " && player.HasAttr(AttrType.RUNNING)){
					player.attrs[AttrType.RUNNING] = 0;
				}
				int last = position-1;
				if(last == -1){ last = 19; }
				string prev = log[last];
				string count = "1";
				int pos = prev.LastIndexOf(" (x");
				if(pos != -1){
					count = prev.Substring(pos+3);
					count = count.Substring(0,count.Length-1);
					prev = prev.Substring(0,pos+1);
				}
				if(prev == str){
					log[last] = prev + "(x" + (Convert.ToInt32(count)+1).ToString() + ")";
				}
				else{
					log[position] = str;
					++position;
					if(position == 20){ position = 0; }
				}
				Console.SetCursorPosition(Global.MAP_OFFSET_COLS,1);
				Console.Write("".PadRight(Global.COLS));
				Console.SetCursorPosition(Global.MAP_OFFSET_COLS,1);
				Console.Write(str);
				if(str2 != "" || special_message == true){
					int cursor = Console.CursorLeft;
					if(Screen.MapChar(0,0).c == '-'){ //hack
						M.RedrawWithStrings();
					}
					else{
						M.Draw();
					}
					Console.SetCursorPosition(cursor,1);
					Screen.ForegroundColor = ConsoleColor.Yellow;
					Console.Write("[more]");
					Screen.ForegroundColor = ConsoleColor.Gray;
					Console.CursorVisible = true; //untested
					Console.ReadKey();
				}
				str = str2;
				str2 = "";
			}
			else{
				Console.SetCursorPosition(Global.MAP_OFFSET_COLS,1);
				Console.Write("".PadRight(Global.COLS));
			}
			//Console.CursorVisible = true;
		}
		public void PrintAll(){
			Screen.ResetColors();
			if(str != ""){
				if(str.Length > max_length-7){
					for(int i=max_length-7;i>=0;--i){
						if(str.Substring(i,1)==" "){
							str2 = str.Substring(i+1);
							str = str.Substring(0,i+1);
							break;
						}
					}
					Print(true);
					Print(true);
				}
				else{
					Print(true);
				}
			}
		}
		public string Printed(int num){ return log[(position+num)%20]; }
		public List<string> GetMessages(){
			List<string> result = new List<string>();
			for(int i=0;i<20;++i){
				result.Add(Printed(i));
			}
			return result;
		}
		public void AddDependingOnLastPartialMessage(string s){ //   =|
			if(!str.EndsWith(s,true,null)){
				Add(s);
			}
		}
		public void AddIfEmpty(string s){
			if(str.Length == 0){
				Add(s);
			}
		}
	}
}

