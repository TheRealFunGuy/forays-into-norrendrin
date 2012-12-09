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
using System.Threading;
namespace Forays{
	public enum HelpTopic{Overview,Skills,Feats,Spells,Items,Commands,Advanced,Tips};
	public enum TutorialTopic{Movement,Attacking,Torch,Resistance,Fire,Recovery,RangedAttacks,Feats,Armor,HealingPool,Consumables};
	public static class Help{
		public static Dict<TutorialTopic,bool> displayed = new Dict<TutorialTopic,bool>();
		public static void DisplayHelp(){ DisplayHelp(HelpTopic.Overview); }
		public static void DisplayHelp(HelpTopic h){
			Console.CursorVisible = false;
			Screen.Blank();
			int num_topics = Enum.GetValues(typeof(HelpTopic)).Length;
			Screen.WriteString(5,4,"Topics:",Color.Yellow);
			for(int i=0;i<num_topics+1;++i){
				Screen.WriteString(i+7,0,"[ ]");
				Screen.WriteChar(i+7,1,(char)(i+'a'),Color.Cyan);
			}
			Screen.WriteString(num_topics+7,4,"Quit");
			Screen.WriteString(0,16,"".PadRight(61,'-'));
			Screen.WriteString(23,16,"".PadRight(61,'-'));
			List<string> text = HelpText(h);
			int startline = 0;
			ConsoleKeyInfo command;
			char ch;
			for(bool done=false;!done;){
				foreach(HelpTopic help in Enum.GetValues(typeof(HelpTopic))){
					if(h == help){
						Screen.WriteString(7+(int)help,4,Enum.GetName(typeof(HelpTopic),help),Color.Yellow);
					}
					else{
						Screen.WriteString(7+(int)help,4,Enum.GetName(typeof(HelpTopic),help));
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
					Screen.WriteString(23,77,new colorstring("[",Color.Yellow,"+",Color.Cyan,"]",Color.Yellow));
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
					if(h != HelpTopic.Overview){
						h = HelpTopic.Overview;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'b':
					if(h != HelpTopic.Skills){
						h = HelpTopic.Skills;
						text = HelpText(h);
						startline = 0;
						
					}
					break;
				case 'c':
					if(h != HelpTopic.Feats){
						h = HelpTopic.Feats;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'd':
					if(h != HelpTopic.Spells){
						h = HelpTopic.Spells;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'e':
					if(h != HelpTopic.Items){
						h = HelpTopic.Items;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'f':
					if(h != HelpTopic.Commands){
						h = HelpTopic.Commands;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'g':
					if(h != HelpTopic.Advanced){
						h = HelpTopic.Advanced;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'h':
					if(h != HelpTopic.Tips){
						h = HelpTopic.Tips;
						text = HelpText(h);
						startline = 0;
					}
					break;
				case 'i':
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
		public static List<string> HelpText(HelpTopic h){
			string path = "";
			int startline = 0;
			int num_lines = -1; //-1 means read until end
			switch(h){
			case HelpTopic.Overview:
				path = "help.txt";
				num_lines = 54;
				break;
			case HelpTopic.Commands:
				path = "help.txt";
				/*if(Option(OptionType.VI_KEYS)){
					startline = 85;
				}
				else{*/
				startline = 56;
				//}
				num_lines = 26;
				break;
			case HelpTopic.Items:
				path = "item_help.txt";
				break;
			case HelpTopic.Skills:
				path = "feat_help.txt";
				num_lines = 19;
				break;
			case HelpTopic.Feats:
				path = "feat_help.txt";
				startline = 21;
				break;
			case HelpTopic.Spells:
				path = "spell_help.txt";
				break;
			case HelpTopic.Advanced:
				path = "advanced_help.txt";
				break;
			default:
				path = "feat_help.txt";
				break;
			}
			List<string> result = new List<string>();
			if(h == HelpTopic.Tips){ //these aren't read from a file
				result.Add("Viewing all tutorial tips:");
				result.Add("");
				result.Add("");
				result.Add("");
				foreach(TutorialTopic topic in Enum.GetValues(typeof(TutorialTopic))){
					foreach(string s in TutorialText(topic)){
						result.Add(s);
					}
					result.Add("");
					result.Add("");
					result.Add("");
				}
				return result;
			}
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
		public static Color NextColor(Color c){
			if(c == Color.DarkCyan){
				return Color.White;
			}
			else{
				return (Color)(1+(int)c);
			}
		}
		public static string[] TutorialText(TutorialTopic topic){
			switch(topic){
			case TutorialTopic.Movement:
				return new string[]{
					"Moving around",
					"",
					"Use the numpad [1-9] to move. Press",
					"[5] to wait.",
					"",
					"If you have no numpad, you can use",
					"the arrow keys or [hjkl] to move,",
					"using [yubn] for diagonal moves.",
					"",
					"This tip won't appear again. If you",
					"wish to view all tips, you can find",
					"them by pressing [?] for help."};
			case TutorialTopic.Attacking:
				return new string[]{
					"Attacking enemies",
					"",
					"To make a melee attack, simply try to",
					"move toward an adjacent monster."};
			case TutorialTopic.Torch:
				return new string[]{
					"Using your torch",
					"",
					"You carry a torch that illuminates",
					"your surroundings, but its light makes",
					"your presence obvious to enemies.",
					"",
					"To put your torch away (or bring it",
					"back out), press [t].",
					"",
					"You won't be able to see quite as far without",
					"your torch (and you'll have a harder time",
					"spotting hidden things), but you'll be able",
					"to sneak around without automatically",
					"alerting monsters."};
			case TutorialTopic.Resistance:
				return new string[]{
					"Resisted!",
					"",
					"Some monsters take half damage from certain",
					"attack types. If a monster resists one of your",
					"attacks, you can switch to a different",
					"weapon by pressing [e] to access the",
					"equipment screen.",
					"",
					"For example, skeletons resist several types of",
					"damage, but are fully vulnerable to maces."};
			case TutorialTopic.RangedAttacks:
				return new string[]{
					"Ranged attacks",
					"",
					"There are some monsters that are best dispatched",
					"at a safe distance. You can switch to your bow",
					"by pressing [e] to access the equipment screen.",
					"",
					"Once you've readied your bow, press [s] to shoot."};
			case TutorialTopic.Feats:
				return new string[]{
					"Feats",
					"",
					"Feats are special abilities",
					"you can learn at shrines.",
					"",
					"You need to put ALL of the required",
					"points into a feat before you can",
					"use it."};
			case TutorialTopic.Armor:
				return new string[]{
					"Armor",
					"",
					"Armor helps you to avoid taking damage from",
					"attacks, but heavy armor also interferes with",
					"both stealth and magic spells.",
					"",
					"If you don't need stealth or magic, wear",
					"full plate for the best protection."};
			case TutorialTopic.Fire:
				return new string[]{
					"You're on fire!",
					"",
					"You'll take damage each turn",
					"until you put it out.",
					"",
					"Stand still by pressing [.] and",
					"you'll try to put out the fire."};
			case TutorialTopic.Recovery:
				return new string[]{
					"Recovering health",
					"",
					"Take advantage of your natural recovery. Your",
					"health will slowly return until your HP reaches",
					"a multiple of 10 (so if your health is 74/100,",
					"it'll go back up to 80/100, and then stop).",
					"",
					"If that isn't enough, you can restore more HP by",
					"resting. Press [r], and if you're undisturbed for",
					"10 turns, you'll regain half of your missing HP",
					"(and restore your magic reserves, if applicable).",
					"",
					"You can rest only once per dungeon level, but your",
					"natural recovery always works."};
			case TutorialTopic.HealingPool:
				return new string[]{
					"Healing pools",
					"",
					"Perhaps a relative of wishing wells, healing",
					"pools are a rare feature of the dungeon that",
					"can fully restore your health.",
					"",
					"To activate a healing pool, drop in an item",
					"by pressing [d]."};
			case TutorialTopic.Consumables:
				return new string[]{
					"Using consumable items",
					"",
					"Sometimes death is unavoidable.",
					"",
					"However, consumable items can",
					"get you out of some desperate",
					"situations.",
					"",
					"When all hope seems lost, be sure to",
					"check your inventory."};
			default:
				return new string[0]{};
			}
		}
		private static List<colorstring> BoxAnimationFrame(int height,int width){
			Color box_edge_color = Color.Blue;
			Color box_corner_color = Color.Yellow;
			List<colorstring> box = new List<colorstring>();
			box.Add(new colorstring("+",box_corner_color,"".PadRight(width-2,'-'),box_edge_color,"+",box_corner_color));
			for(int i=0;i<height-2;++i){
				box.Add(new colorstring("|",box_edge_color,"".PadRight(width-2),Color.Gray,"|",box_edge_color));
			}
			box.Add(new colorstring("+",box_corner_color,"".PadRight(width-2,'-'),box_edge_color,"+",box_corner_color));
			return box;
		}
		private static int FrameWidth(int previous_height,int previous_width){
			return previous_width - (previous_width * 2 / previous_height); //2 lines are removed, so the width loses 2/height to keep similar dimensions
		}
		public static void TutorialTip(TutorialTopic topic){
			if(Global.Option(OptionType.NEVER_DISPLAY_TIPS) || displayed[topic]){
				return;
			}
			Color box_edge_color = Color.Blue;
			Color box_corner_color = Color.Yellow;
			Color first_line_color = Color.Yellow;
			Color text_color = Color.Gray;
			string[] text = TutorialText(topic);
			int stringwidth = 27; // length of "[Press any key to continue]"
			foreach(string s in text){
				if(s.Length > stringwidth){
					stringwidth = s.Length;
				}
			}
			stringwidth += 4; //2 blanks on each side
			int boxwidth = stringwidth + 2;
			int boxheight = text.Length + 5;
			//for(bool done=false;!done;){
			colorstring[] box = new colorstring[boxheight]; //maybe i should make this a list to match the others
			box[0] = new colorstring("+",box_corner_color,"".PadRight(stringwidth,'-'),box_edge_color,"+",box_corner_color);
			box[text.Length + 1] = new colorstring("|",box_edge_color,"".PadRight(stringwidth),Color.Gray,"|",box_edge_color);
			box[text.Length + 2] = new colorstring("|",box_edge_color) + "[Press any key to continue]".PadOuter(stringwidth).GetColorString(text_color) + new colorstring("|",box_edge_color);
			box[text.Length + 3] = new colorstring("|",box_edge_color) + "[=] Stop showing tips".PadOuter(stringwidth).GetColorString(text_color) + new colorstring("|",box_edge_color);
			box[text.Length + 4] = new colorstring("+",box_corner_color,"".PadRight(stringwidth,'-'),box_edge_color,"+",box_corner_color);
			int pos = 1;
			foreach(string s in text){
				box[pos] = new colorstring("|",box_edge_color) + s.PadOuter(stringwidth).GetColorString(text_color) + new colorstring("|",box_edge_color);
				if(pos == 1){
					box[pos] = new colorstring();
					box[pos].strings.Add(new cstr("|",box_edge_color));
					box[pos].strings.Add(new cstr(s.PadOuter(stringwidth),first_line_color));
					box[pos].strings.Add(new cstr("|",box_edge_color));
				}
				++pos;
			}
			int y = (Global.SCREEN_H - boxheight) / 2;
			int x = (Global.SCREEN_W - boxwidth) / 2;
			colorchar[,] memory = Screen.GetCurrentRect(y,x,boxheight,boxwidth);
			List<List<colorstring>> frames = new List<List<colorstring>>();
			frames.Add(BoxAnimationFrame(boxheight-2,FrameWidth(boxheight,boxwidth)));
			for(int i=boxheight-4;i>0;i-=2){
				frames.Add(BoxAnimationFrame(i,FrameWidth(frames.Last().Count,frames.Last()[0].Length())));
			}
			for(int i=frames.Count-1;i>=0;--i){ //since the frames are in reverse order
				int y_offset = i + 1;
				int x_offset = (boxwidth - frames[i][0].Length()) / 2;
				Screen.WriteList(y+y_offset,x+x_offset,frames[i]);
				Thread.Sleep(20);
			}
			foreach(colorstring s in box){
				Screen.WriteString(y,x,s);
				++y;
			}
			Actor.player.DisplayStats(false);
			if(topic != TutorialTopic.Feats){ //hacky exception - don't get rid of the line that's already there.
				Actor.B.DisplayNow();
			}
			Console.CursorVisible = false;
			Global.FlushInput();
			/*	switch(Console.ReadKey(true).KeyChar){
				case 'q':
					box_edge_color = NextColor(box_edge_color);
					break;
				case 'w':
					box_corner_color = NextColor(box_corner_color);
					break;
				case 'e':
					first_line_color = NextColor(first_line_color);
					break;
				case 'r':
					text_color = NextColor(text_color);
					break;
				default:
					done=true;
					break;
				}
			}*/
			if(Console.ReadKey(true).KeyChar == '='){
				Global.Options[OptionType.NEVER_DISPLAY_TIPS] = true;
			}
			Screen.WriteArray((Global.SCREEN_H - boxheight) / 2,x,memory);
			displayed[topic] = true;
			Console.CursorVisible = true;
		}
	}
}