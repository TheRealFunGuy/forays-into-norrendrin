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
	public enum HelpTopic{Overview,Skills,Feats,Spells,Items,Commands,Advanced};
	public enum TutorialTopic{Movement,Attacking,Resistance,Fire,Recovery,RangedAttacks,Armor,HealingPool};
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
					"...",
					"..."};
			case TutorialTopic.Attacking:
				return new string[]{
					"Attacking",
					"",
					"...",
					"",
					"..."};
			case TutorialTopic.Resistance:
				return new string[]{
					"Resisted!",
					"",
					"",
					"",
					"..."};
			case TutorialTopic.RangedAttacks:
				return new string[]{
					"Ranged attacks",
					"",
					"",
					"",
					"..."};
			case TutorialTopic.Armor:
				return new string[]{
					"Armor",
					"",
					"",
					"..."};
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
					"",
					"",
					"..."};
			case TutorialTopic.HealingPool:
				return new string[]{
					"Healing pools",
					"",
					"yeahhh...",
					"",
					"..."};
			default:
				return new string[0]{};
			}
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
			colorstring[] box = new colorstring[boxheight];
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
			int x = (Global.SCREEN_W - boxwidth) / 2;
			int y = (Global.SCREEN_H - boxheight) / 2;
			colorchar[,] memory = Screen.GetCurrentRect(y,x,boxheight,boxwidth);
			foreach(colorstring s in box){
				Screen.WriteString(y,x,s);
				++y;
			}
			Actor.player.DisplayStats(false);
			Actor.B.DisplayNow();
			Console.CursorVisible = false;
			while(Console.KeyAvailable){
				Console.ReadKey(true);
			}
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