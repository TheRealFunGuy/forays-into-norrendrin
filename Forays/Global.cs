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
using Utilities;
namespace Forays{
	public static class Global{
		public const string VERSION = "version 0.7.0 ";
		public static bool LINUX = false;
		public const int SCREEN_H = 25;
		public const int SCREEN_W = 80;
		public const int ROWS = 22;
		public const int COLS = 66;
		public const int MAP_OFFSET_ROWS = 3;
		public const int MAP_OFFSET_COLS = 13;
		public const int MAX_LIGHT_RADIUS = 12; //the maximum POSSIBLE light radius. used in light calculations.
		public const int MAX_INVENTORY_SIZE = 20;
		public static bool GAME_OVER = false;
		public static bool BOSS_KILLED = false;
		public static bool QUITTING = false;
		public static bool SAVING = false;
		public static string KILLED_BY = "debugged to death";
		public static List<string> quickstartinfo = null;
		public static Dictionary<OptionType,bool> Options = new Dictionary<OptionType, bool>();
		public static bool Option(OptionType option){
			bool result = false;
			Options.TryGetValue(option,out result);
			return result;
		}
		public static int RandomDirection(){
			int result = R.Roll(8);
			if(result == 5){
				result = 9;
			}
			return result;
		}
		public static void FlushInput(){
			while(Console.KeyAvailable){
				Console.ReadKey(true);
			}
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
									return -1;
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
			int cursor = Console.CursorLeft;
			Screen.WriteString(Console.CursorTop,cursor,"".PadRight(max_length));
			while(!done){
				Console.SetCursorPosition(cursor,Console.CursorTop);
				command = Console.ReadKey(true);
				if((command.KeyChar >= '!' && command.KeyChar <= '~') || command.KeyChar == ' '){
					if(s.Length < max_length){
						s = s + command.KeyChar;
						Screen.WriteChar(Console.CursorTop,cursor,command.KeyChar);
						++cursor;
					}
				}
				else{
					if(command.Key == ConsoleKey.Backspace && s.Length > 0){
						s = s.Substring(0,s.Length-1);
						--cursor;
						Screen.WriteChar(Console.CursorTop,cursor,' ');
						Console.SetCursorPosition(cursor,Console.CursorTop);
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
		public static string GenerateCharacterName(){
			List<string> vowel = new List<string>{"a","e","i","o","u","ei","a","e","i","o","u","a","e","i","o","u","a","e","i","o","a","e","o"};
			List<string> end_vowel = new List<string>{"a","e","i","o","u","io","ia","a","e","i","o","a","e","i","o","a","e","o","a","e","o"};
			List<string> consonant = new List<string>{"k","s","t","n","h","m","y","r","w","g","d","p","b","f","l","v","z","ch","br","cr","dr","fr","gr","kr","pr","tr","th","sc","sh","sk","sl","sm","sn","sp","st","s","t","n","m","r","g","d","p","b","l","k","s","t","n","m","d","p","b","l"};
			List<string> end_consonant = new List<string>{"k","s","t","n","m","r","g","d","p","b","l","z","ch","th","sh","sk","sp","st","k","s","t","n","m","r","n","d","p","b","l","k","s","t","n","m","r","d","p","l","sk","th","st","d","m","s"};
			string result = "";
			if(R.OneIn(5)){
				if(R.CoinFlip()){
					result = vowel.Random() + consonant.Random() + vowel.Random() + consonant.Random() + vowel.Random() + end_consonant.Random();
				}
				else{
					result = vowel.Random() + consonant.Random() + vowel.Random() + consonant.Random() + end_vowel.Random();
				}
			}
			else{
				if(R.CoinFlip()){
					result = consonant.Random() + vowel.Random() + consonant.Random() + vowel.Random() + consonant.Random() + vowel.Random() + end_consonant.Random();
				}
				else{
					result = consonant.Random() + vowel.Random() + consonant.Random() + vowel.Random() + consonant.Random() + end_vowel.Random();
				}
			}
			result = result.Substring(0,1).ToUpper() + result.Substring(1);
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
			s = "";
			while(s.Length < 2 || s.Substring(0,2) != "--"){
				s = file.ReadLine();
				if(s.Length >= 2 && s.Substring(0,2) == "--"){
					break;
				}
				string[] tokens = s.Split(' ');
				if(tokens[0].Length == 1){
					char c = Char.ToUpper(tokens[0][0]);
					if(c == 'F' || c == 'T'){
						TutorialTopic topic = (TutorialTopic)Enum.Parse(typeof(TutorialTopic),tokens[1],true);
						if(c == 'F' || Global.Option(OptionType.ALWAYS_RESET_TIPS)){
							Help.displayed[topic] = false;
						}
						else{
							Help.displayed[topic] = true;
						}
					}
				}
			}
		}
		public static void SaveOptions(){
			StreamWriter file = new StreamWriter("options.txt",false);
			file.WriteLine("Options:");
			file.WriteLine("Any line that starts with [TtFf] and a space MUST be one of the valid options(or, in the 2nd part, one of the valid tutorial tips):");
			file.WriteLine("last_target autopickup no_roman_numerals hide_old_messages hide_commands never_display_tips always_reset_tips");
			foreach(OptionType op in Enum.GetValues(typeof(OptionType))){
				if(Options[op]){
					file.Write("t ");
				}
				else{
					file.Write("f ");
				}
				file.WriteLine(Enum.GetName(typeof(OptionType),op).ToLower());
			}
			file.WriteLine("-- Tracking which tutorial tips have been displayed:");
			foreach(TutorialTopic topic in Enum.GetValues(typeof(TutorialTopic))){
				if(Help.displayed[topic]){
					file.Write("t ");
				}
				else{
					file.Write("f ");
				}
				file.WriteLine(Enum.GetName(typeof(TutorialTopic),topic).ToLower());
			}
			file.WriteLine("--");
			file.Close();
		}
		public delegate int IDMethod(PhysicalObject o);
		public static void SaveGame(Buffer B,Map M,Queue Q){ //games are loaded in Main.cs
			FileStream file = new FileStream("forays.sav",FileMode.CreateNew);
			BinaryWriter b = new BinaryWriter(file);
			Dictionary<PhysicalObject,int> id = new Dictionary<PhysicalObject, int>();
			int next_id = 1;
			IDMethod GetID = delegate(PhysicalObject o){
				if(o == null){
					return 0;
				}
				if(!id.ContainsKey(o)){
					id.Add(o,next_id);
					++next_id;
				}
				return id[o];
			};
			b.Write(Actor.player_name);
			b.Write(M.current_level);
			for(int i=0;i<20;++i){
				b.Write((int)M.level_types[i]);
			}
			b.Write(M.wiz_lite);
			b.Write(M.wiz_dark);
			//skipping danger_sensed
			b.Write(Actor.feats_in_order.Count);
			foreach(FeatType ft in Actor.feats_in_order){
				b.Write((int)ft);
			}
			b.Write(Actor.spells_in_order.Count);
			foreach(SpellType sp in Actor.spells_in_order){
				b.Write((int)sp);
			}
			List<List<Actor>> groups = new List<List<Actor>>();
			b.Write(M.AllActors().Count);
			foreach(Actor a in M.AllActors()){
				b.Write(GetID(a));
				b.Write(a.row);
				b.Write(a.col);
				b.Write(a.name);
				b.Write(a.the_name);
				b.Write(a.a_name);
				b.Write(a.symbol);
				b.Write((int)a.color);
				b.Write((int)a.type);
				b.Write(a.maxhp);
				b.Write(a.curhp);
				b.Write(a.speed);
				b.Write(a.light_radius);
				b.Write(GetID(a.target));
				b.Write(a.inv.Count);
				foreach(Item i in a.inv){
					b.Write(i.name);
					b.Write(i.the_name);
					b.Write(i.a_name);
					b.Write(i.symbol);
					b.Write((int)i.color);
					b.Write((int)i.type);
					b.Write(i.quantity);
					b.Write(i.ignored);
				}
				for(int i=0;i<13;++i){
					b.Write((int)a.F[i]);
				}
				b.Write(a.attrs.d.Count);
				foreach(AttrType at in a.attrs.d.Keys){
					b.Write((int)at);
					b.Write(a.attrs[at]);
				}
				b.Write(a.skills.d.Count);
				foreach(SkillType st in a.skills.d.Keys){
					b.Write((int)st);
					b.Write(a.skills[st]);
				}
				b.Write(a.feats.d.Count);
				foreach(FeatType ft in a.feats.d.Keys){
					b.Write((int)ft);
					b.Write(a.feats[ft]);
				}
				b.Write(a.spells.d.Count);
				foreach(SpellType sp in a.spells.d.Keys){
					b.Write((int)sp);
					b.Write(a.spells[sp]);
				}
				b.Write(a.exhaustion);
				b.Write(a.time_of_last_action);
				b.Write(a.recover_time);
				b.Write(a.path.Count);
				foreach(pos p in a.path){
					b.Write(p.row);
					b.Write(p.col);
				}
				b.Write(GetID(a.target_location));
				b.Write(a.player_visibility_duration);
				if(a.group != null){
					groups.AddUnique(a.group);
				}
				b.Write(a.weapons.Count);
				/*foreach(WeaponType w in a.weapons){
					b.Write((int)w);
				}
				b.Write(a.armors.Count);
				foreach(ArmorType ar in a.armors){
					b.Write((int)ar);
				}*/
				b.Write(a.magic_trinkets.Count);
				foreach(MagicTrinketType m in a.magic_trinkets){
					b.Write((int)m);
				}
			}
			b.Write(groups.Count);
			foreach(List<Actor> group in groups){
				b.Write(group.Count);
				foreach(Actor a in group){
					b.Write(GetID(a));
				}
			}
			b.Write(M.AllTiles().Count);
			foreach(Tile t in M.AllTiles()){
				b.Write(GetID(t));
				b.Write(t.row);
				b.Write(t.col);
				b.Write(t.name);
				b.Write(t.the_name);
				b.Write(t.a_name);
				b.Write(t.symbol);
				b.Write((int)t.color);
				b.Write((int)t.type);
				b.Write(t.passable);
				b.Write(t.opaque);
				b.Write(t.seen);
				b.Write(t.solid_rock);
				b.Write(t.light_value);
				if(t.toggles_into.HasValue){
					b.Write(true);
					b.Write((int)t.toggles_into.Value);
				}
				else{
					b.Write(false);
				}
				if(t.inv != null){
					b.Write(true);
					b.Write(t.inv.name);
					b.Write(t.inv.the_name);
					b.Write(t.inv.a_name);
					b.Write(t.inv.symbol);
					b.Write((int)t.inv.color);
					b.Write((int)t.inv.type);
					b.Write(t.inv.quantity);
					b.Write(t.inv.ignored);
				}
				else{
					b.Write(false);
				}
				b.Write(t.features.Count);
				foreach(FeatureType f in t.features){
					b.Write((int)f);
				}
			}
			b.Write(Q.turn);
			b.Write(Actor.tiebreakers.Count);
			foreach(Actor a in Actor.tiebreakers){
				b.Write(GetID(a));
			}
			b.Write(Q.list.Count);
			foreach(Event e in Q.list){
				b.Write(GetID(e.target));
				if(e.area == null){
					b.Write(0);
				}
				else{
					b.Write(e.area.Count);
					foreach(Tile t in e.area){
						b.Write(GetID(t));
					}
				}
				b.Write(e.delay);
				b.Write((int)e.type);
				b.Write((int)e.attr);
				b.Write(e.value);
				b.Write(e.msg);
				if(e.msg_objs == null){
					b.Write(0);
				}
				else{
					b.Write(e.msg_objs.Count);
					foreach(PhysicalObject o in e.msg_objs){
						b.Write(GetID(o));
					}
				}
				b.Write(e.time_created);
				b.Write(e.dead);
				b.Write(e.tiebreaker);
			}
			for(int i=0;i<20;++i){
				b.Write(B.Printed(i));
			}
			b.Close();
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
	public static class Extensions{
		/*public static int NumberOfConsecutiveAdjacentPositionsWhere(this pos p,U.BooleanPositionDelegate condition){
			int max_count = 0;
			int count = 0;
			for(int times=0;times<2;++times){
				for(int i=0;i<8;++i){
					if(condition(p.PosInDir(8.RotateDir(true,i)))){
						++count;
					}
					else{
						if(count > max_count){
							max_count = count;
						}
						count = 0;
					}
				}
				if(count == 8){
					return 8;
				}
			}
			return max_count;
		}*/
		public static T Last<T>(this List<T> l){ //note that this doesn't work the way I wanted it to - 
			if(l.Count == 0){ // you can't assign to list.Last()
				return default(T);
			}
			return l[l.Count-1];
		}
		public static List<string> GetWordWrappedList(this string s,int max_length){ //max_length MUST be longer than any single word in the string
			List<string> result = new List<string>();
			while(s.Length > max_length){
				for(int i=max_length;i>=0;--i){
					if(s.Substring(i,1) == " "){
						result.Add(s.Substring(0,i));
						s = s.Substring(i+1);
						break;
					}
				}
			}
			result.Add(s);
			return result;
		}
		public static string ConcatenateListWithCommas(this List<string> ls){
			//"one" returns "one"
			//"one" "two" returns "one and two"
			//"one" "two" "three" returns "one, two, and three", and so on
			if(ls.Count == 1){
				return ls[0];
			}
			if(ls.Count == 2){
				return ls[0] + " and " + ls[1];
			}
			if(ls.Count > 2){
				string result = "";
				for(int i=0;i<ls.Count;++i){
					if(i == ls.Count - 1){
						result = result + "and " + ls[i];
					}
					else{
						result = result + ls[i] + ", ";
					}
				}
				return result;
			}
			return "";
		}
		public static string PadToMapSize(this string s){
			return s.PadRight(Global.COLS);
		}
		public static colorstring GetColorString(this string s){ return GetColorString(s,Color.Gray); }
		public static colorstring GetColorString(this string s,Color color){
			if(s.Contains("[")){
				string temp = s;
				colorstring result = new colorstring();
				while(temp.Contains("[")){
					int open = temp.IndexOf('[');
					int close = temp.IndexOf(']');
					if(close == -1){
						result.strings.Add(new cstr(temp,color));
						temp = "";
					}
					else{
						int hyphen = temp.IndexOf('-');
						if(hyphen != -1 && hyphen > open && hyphen < close){
							result.strings.Add(new cstr(temp.Substring(0,open+1),color));
							//result.strings.Add(new cstr(temp.Substring(open+1,(close-open)-1),Color.Cyan));
							result.strings.Add(new cstr(temp.Substring(open+1,(hyphen-open)-1),Color.Cyan));
							result.strings.Add(new cstr("-",color));
							result.strings.Add(new cstr(temp.Substring(hyphen+1,(close-hyphen)-1),Color.Cyan));
							result.strings.Add(new cstr("]",color));
							temp = temp.Substring(close+1);
						}
						else{
							result.strings.Add(new cstr(temp.Substring(0,open+1),color));
							result.strings.Add(new cstr(temp.Substring(open+1,(close-open)-1),Color.Cyan));
							result.strings.Add(new cstr("]",color));
							temp = temp.Substring(close+1);
						}
					}
				}
				if(temp != ""){
					result.strings.Add(new cstr(temp,color));
				}
				return result;
			}
			else{
				return new colorstring(s,color);
			}
		}
		public static List<colorstring> GetColorStrings(this List<string> l){
			List<colorstring> result = new List<colorstring>();
			foreach(string s in l){
				result.Add(s.GetColorString());
			}
			return result;
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
		public static List<Tile> ToFirstObstruction(this List<Tile> line){ //impassable tile OR actor
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
		public static List<Tile> From(this List<Tile> line,PhysicalObject o){
			List<Tile> result = new List<Tile>();
			bool found = false;
			foreach(Tile t in line){
				if(o.row == t.row && o.col == t.col){
					found = true;
				}
				if(found){
					result.Add(t);
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
	}
}
