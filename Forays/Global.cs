/*Copyright (c) 2011-2012  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.IO;
using System.Collections.Generic;
namespace Forays{
	public static class Global{public static int temporary = 0;
		public const string VERSION = "version 0.6.1 ";
		public static bool LINUX = false;
		public const int SCREEN_H = 25;
		public const int SCREEN_W = 80;
		public const int ROWS = 22;
		public const int COLS = 66;
		public const int MAP_OFFSET_ROWS = 3;
		public const int MAP_OFFSET_COLS = 13;
		public const int MAX_LIGHT_RADIUS = 12; //the maximum POSSIBLE light radius. used in light calculations.
		public const int MAX_INVENTORY_SIZE = ROWS-2;
		public static bool GAME_OVER = false;
		public static bool BOSS_KILLED = false;
		public static bool QUITTING = false;
		public static List<string> quickstartinfo = null;
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
		public static bool OneIn(int num){
			int i = Roll(num);
			if(i == num){
				return true;
			}
			return false;
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
		public static string[] titlescreen =  new string[]{
"                                                                                ",
"                                                                                ",
"        #######                                                                 ",
"        #######                                                                 ",
"        ##    #                                                                 ",
"        ##                                                                      ",
"        ##  #                                                                   ",
"        #####                                                                   ",
"        #####                                                                   ",
"        ##  #   ###   # ##   ###    #   #   ###                                 ",
"        ##     #   #  ##    #   #   #   #  #                                    ",
"        ##     #   #  #     #   #    # #    ##                                  ",
"        ##     #   #  #     #   #     #       #                                 ",
"        ##      ###   #      ### ##   #    ###                                  ",
"                                     #                                          ",
"                                    #                                           ",
"                                                                                ",
"                                                                                ",
"                         I N T O     N O R R E N D R I N                        ",
"                                                                                ",
"                                                                                ",
"                                                                                ",
"                                                                  " + VERSION,
"                                                             by Derrick Creamer "};
		public static string RomanNumeral(int num){
			string result = "";
			while(num > 1000){
				result = result + "M";
				num -= 1000;
			}
			result = result + RomanPattern(num/100,'C','D','M');
			num -= (num/100)*100;
			result = result + RomanPattern(num/10,'X','L','C');
			num -= (num/10)*10;
			result = result + RomanPattern(num,'I','V','X');
			return result;
		}
		private static string RomanPattern(int num,char one,char five,char ten){
			switch(num){
			case 1:
				return "" + one;
			case 2:
				return "" + one + one;
			case 3:
				return "" + one + one + one;
			case 4:
				return "" + one + five;
			case 5:
				return "" + five;
			case 6:
				return "" + five + one;
			case 7:
				return "" + five + one + one;
			case 8:
				return "" + five + one + one + one;
			case 9:
				return "" + one + ten;
			default: //0
				return "";
			}
		}
		public static void DisplayHelp(){ DisplayHelp(Help.Overview); }
		public static void DisplayHelp(Help h){
			Console.CursorVisible = false;
			Screen.Blank();
			int num_topics = Enum.GetValues(typeof(Help)).Length;
			Screen.WriteString(5,4,"Topics:",Color.Yellow);
			for(int i=0;i<num_topics+1;++i){
				Screen.WriteString(i+7,0,"[ ]");
				Screen.WriteChar(i+7,1,(char)(i+'a'),Color.Cyan);
			}
			Screen.WriteString(num_topics+7,4,"Quit");
			Screen.WriteString(0,16,"".PadRight(61,'-'));
			Screen.WriteString(23,16,"".PadRight(61,'-'));
			List<string> text = HelpTopic(h);
			int startline = 0;
			ConsoleKeyInfo command;
			char ch;
			for(bool done=false;!done;){
				foreach(Help help in Enum.GetValues(typeof(Help))){
					if(h == help){
						Screen.WriteString(7+(int)help,4,Enum.GetName(typeof(Help),help),Color.Yellow);
					}
					else{
						Screen.WriteString(7+(int)help,4,Enum.GetName(typeof(Help),help));
					}
				}
				if(startline > 0){
					Screen.WriteString(0,77,new colorstring("[",Color.Yellow,"-",Color.Cyan,"]",Color.Yellow));
				}
				else{
					Screen.WriteString(0,77,"---");
				}
				bool more = false;
				if(startline + 22 < text.Count){
					more = true;
				}
				if(more){
					Screen.WriteString(23,77,new colorstring("[",Color.Yellow,"-",Color.Cyan,"]",Color.Yellow));
				}
				else{
					Screen.WriteString(23,77,"---");
				}
				for(int i=1;i<=22;++i){
					if(text.Count - startline < i){
						Screen.WriteString(i,16,"".PadRight(64));
					}
					else{
						Screen.WriteString(i,16,text[i+startline-1].PadRight(64));
					}
				}
				command = Console.ReadKey(true);
				ConsoleKey ck = command.Key;
				if(ck == ConsoleKey.Backspace || ck == ConsoleKey.PageUp){
					ch = (char)8;
				}
				else{
					if(ck == ConsoleKey.PageDown){
						ch = ' ';
					}
					else{
						ch = Actor.ConvertInput(command);
					}
				}
				switch(ch){
				case 'a':
					if(h != Help.Overview){
						h = Help.Overview;
						text = HelpTopic(h);
						startline = 0;
					}
					break;
				case 'b':
					if(h != Help.Skills){
						h = Help.Skills;
						text = HelpTopic(h);
						startline = 0;
						
					}
					break;
				case 'c':
					if(h != Help.Feats){
						h = Help.Feats;
						text = HelpTopic(h);
						startline = 0;
					}
					break;
				case 'd':
					if(h != Help.Spells){
						h = Help.Spells;
						text = HelpTopic(h);
						startline = 0;
					}
					break;
				case 'e':
					if(h != Help.Items){
						h = Help.Items;
						text = HelpTopic(h);
						startline = 0;
					}
					break;
				case 'f':
					if(h != Help.Commands){
						h = Help.Commands;
						text = HelpTopic(h);
						startline = 0;
					}
					break;
				case 'g':
					if(h != Help.Advanced){
						h = Help.Advanced;
						text = HelpTopic(h);
						startline = 0;
					}
					break;
				case 'h':
				case (char)27:
					done = true;
					break;
				case '8':
				case '-':
				case '_':
					if(startline > 0){
						--startline;
					}
					break;
				case '2':
				case '+':
				case '=':
					if(more){
						++startline;
					}
					break;
				case (char)8:
					if(startline > 0){
						startline -= 22;
						if(startline < 0){
							startline = 0;
						}
					}
					break;
				case ' ':
				case (char)13:
					if(text.Count > 22){
						startline += 22;
						if(startline + 22 > text.Count){
							startline = text.Count - 22;
						}
					}
					break;
				default:
					break;
				}
			}
			Screen.Blank();
		}
		public static List<string> HelpTopic(Help h){
			string path = "";
			int startline = 0;
			int num_lines = -1; //-1 means read until end
			switch(h){
			case Help.Overview:
				path = "help.txt";
				num_lines = 55;
				break;
			case Help.Commands:
				path = "help.txt";
				if(Option(OptionType.VI_KEYS)){
					startline = 85;
				}
				else{
					startline = 57;
				}
				num_lines = 26;
				break;
			case Help.Items:
				path = "item_help.txt";
				break;
			case Help.Skills:
				path = "feat_help.txt";
				num_lines = 19;
				break;
			case Help.Feats:
				path = "feat_help.txt";
				startline = 21;
				break;
			case Help.Spells:
				path = "spell_help.txt";
				break;
			case Help.Advanced:
				path = "advanced_help.txt";
				break;
			default:
				path = "feat_help.txt";
				break;
			}
			List<string> result = new List<string>();
			if(path != ""){
				StreamReader file = new StreamReader(path);
				for(int i=0;i<startline;++i){
					file.ReadLine();
				}
				for(int i=0;i<num_lines || num_lines == -1;++i){
					if(file.Peek() != -1){
						result.Add(file.ReadLine());
					}
					else{
						break;
					}
				}
				file.Close();
			}
			return result;
		}
		public static void LoadOptions(){
			StreamReader file = new StreamReader("options.txt");
			string s = "";
			while(s.Length < 2 || s.Substring(0,2) != "--"){
				s = file.ReadLine();
				if(s.Length >= 2 && s.Substring(0,2) == "--"){
					break;
				}
				string[] tokens = s.Split(' ');
				if(tokens[0].Length == 1){
					char c = Char.ToUpper(tokens[0][0]);
					if(c == 'F' || c == 'T'){
						OptionType option = (OptionType)Enum.Parse(typeof(OptionType),tokens[1],true);
						if(c == 'F'){
							Options[option] = false;
						}
						else{
							Options[option] = true;
						}
					}
				}
			}
		}
		public static void SaveOptions(){
			StreamWriter file = new StreamWriter("options.txt",false);
			file.WriteLine("Options:");
			file.WriteLine("Any line that starts with [TtFf] and a space MUST be one of the valid options:");
			file.WriteLine("last_target vi_keys open_chests no_blood_boil_message autopickup no_roman_numerals hide_old_messages");
			foreach(OptionType op in Enum.GetValues(typeof(OptionType))){
				if(Options[op]){
					file.Write("t ");
				}
				else{
					file.Write("f ");
				}
				file.WriteLine(Enum.GetName(typeof(OptionType),op).ToLower());
			}
			file.WriteLine("--");
			file.Close();
		}
		public static void Quit(){
			if(LINUX){
				Screen.Blank();
				Screen.ResetColors();
				Console.SetCursorPosition(0,0);
				Console.CursorVisible = true;
			}
			Environment.Exit(0);
		}
	}
	public enum Help{Overview,Skills,Feats,Spells,Items,Commands,Advanced};
	public class Dict<TKey,TValue>{
		public Dictionary<TKey,TValue> d;// = new Dictionary<TKey,TValue>();
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
	public struct pos{
		public int row;
		public int col;
		public pos(int r,int c){
			row = r;
			col = c;
		}
		public int DistanceFrom(PhysicalObject o){ return DistanceFrom(o.row,o.col); }
		public int DistanceFrom(pos p){ return DistanceFrom(p.row,p.col); }
		//public int DistanceFrom(ICoord o){ return DistanceFrom(o.row,o.col); }
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
		public int EstimatedEuclideanDistanceFromX10(PhysicalObject o){ return EstimatedEuclideanDistanceFromX10(o.row,o.col); }
		public int EstimatedEuclideanDistanceFromX10(pos p){ return EstimatedEuclideanDistanceFromX10(p.row,p.col); }
		public int EstimatedEuclideanDistanceFromX10(int r,int c){ // x10 so that orthogonal directions are closer than diagonals
			int dy = Math.Abs(r-row) * 10;
			int dx = Math.Abs(c-col) * 10;
			if(dx > dy){
				return dx + (dy/2);
			}
			else{
				return dy + (dx/2);
			}
		}
		public List<pos> PositionsWithinDistance(int dist){ return PositionsWithinDistance(dist,false); }
		public List<pos> PositionsWithinDistance(int dist,bool exclude_origin){
			List<pos> result = new List<pos>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(i!=row || j!=col || exclude_origin==false){
						if(Global.BoundsCheck(i,j)){
							result.Add(new pos(i,j));
						}
					}
				}
			}
			return result;
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
		public bool BoundsCheck(){
			if(row>=0 && row<Global.ROWS && col>=0 && col<Global.COLS){
				return true;
			}
			return false;
		}
	}
	public static class Extensions{
		public static T Random<T>(this List<T> l){
			return l[Global.Roll(l.Count)-1];
		}
		public static T RemoveRandom<T>(this List<T> l){
			T result = l[Global.Roll(l.Count)-1];
			l.Remove(result);
			return result;
		}
		public static void AddUnique<T>(this List<T> l,T obj){
			if(!l.Contains(obj)){
				l.Add(obj);
			}
		}
		public static T Last<T>(this List<T> l){ //note that this doesn't work the way I wanted it to - 
			if(l.Count == 0){ // you can't assign to list.Last()
				return default(T);
			}
			return l[l.Count-1];
		}
		public static string PadOuter(this string s,int totalWidth){
			return s.PadOuter(totalWidth,' ');
		}
		public static string PadOuter(this string s,int totalWidth,char paddingChar){
			if(s.Length >= totalWidth){
				return s;
			}
			int added = totalWidth - s.Length;
			string left = "";
			for(int i=0;i<(added+1)/2;++i){
				left = left + paddingChar;
			}
			string right = "";
			for(int i=0;i<added/2;++i){
				right = right + paddingChar;
			}
			return left + s + right;
		}
		public static string PadToMapSize(this string s){
			return s.PadRight(Global.COLS);
		}
		public delegate void ListDelegate<T>(T t); //this one is kinda experimental and doesn't save tooo much typing, but it's here anyway
		public static void Each<T>(this List<T> l,ListDelegate<T> del){
			foreach(T t in l){
				del(t);
			}
		}
		public static List<Tile> ToFirstSolidTile(this List<Tile> line){
			List<Tile> result = new List<Tile>();
			foreach(Tile t in line){
				result.Add(t);
				if(!t.passable){
					break;
				}
			}
			return result;
		}
		public static List<Tile> ToFirstObstruction(this List<Tile> line){ //impassible tile OR actor
			List<Tile> result = new List<Tile>();
			int idx = 0;
			foreach(Tile t in line){
				result.Add(t);
				if(idx != 0){ //skip the first, as it is assumed to be the origin
					if(!t.passable || t.actor() != null){
						break;
					}
				}
				++idx;
			}
			return result;
		}
		public static List<Tile> To(this List<Tile> line,PhysicalObject o){
			List<Tile> result = new List<Tile>();
			foreach(Tile t in line){
				result.Add(t);
				if(o.row == t.row && o.col == t.col){
					break;
				}
			}
			return result;
		}
		public static Tile LastBeforeSolidTile(this List<Tile> line){
			Tile result = null;
			foreach(Tile t in line){
				if(!t.passable){
					break;
				}
				else{
					result = t;
				}
			}
			return result;
		}
		/*public static List<ICoord> ToICoord(this List<Tile> l){
			List<ICoord> result = new List<ICoord>();
			foreach(Tile t in l){
				result.Add(t);
			}
			return result;
		}*/
	}
}
