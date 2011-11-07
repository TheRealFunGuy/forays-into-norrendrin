/*Copyright (c) 2011  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
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
		public void Add(string s){
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
				Print(false);
			}
		}
		public void IfSeenAdd(PhysicalObject o1,string s){ IfSeenAdd(o1,null,s); }
		public void IfSeenAdd(PhysicalObject o1,PhysicalObject o2,string s){
			if(player.CanSee(o1) && (o2 == null || player.CanSee(o2))){
				Add(s);//todo: make sure everything uses this method properly
			}
		}
		public void DisplayNow(string s){
			Console.CursorVisible = false;
			Console.SetCursorPosition(Global.MAP_OFFSET_COLS,1);
			Console.Write(s.PadRight(Global.COLS));
			Console.SetCursorPosition(Global.MAP_OFFSET_COLS + s.Length,1);
			Console.CursorVisible = true;
		}
		public void DisplayLogtempfunction(){
			for(int i=0;i<log.Length;++i){
				Console.Error.Write("{0} ",log[i]);
			}
			Console.ReadKey();
		}
		public void Print(bool special_message){
			Console.CursorVisible = false;
			if(str != ""){
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
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write("[more]");
					Console.ForegroundColor = ConsoleColor.Gray;
					M.Draw();
					Console.ReadKey();
				}
				str = str2;
				str2 = "";
			}
			else{
				Console.SetCursorPosition(Global.MAP_OFFSET_COLS,1);
				Console.Write("".PadRight(Global.COLS));
			}
			Console.CursorVisible = true;
		}
		public void PrintAll(){
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
		public string Printed(int num){ return log[(position+num-1)%20]; }
	}
}

