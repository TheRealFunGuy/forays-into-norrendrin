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
using System.IO;
using Forays;
namespace Forays{
	public enum TileType{WALL,FLOOR,DOOR_O,DOOR_C,STAIRS,CHEST,FIREPIT,STALAGMITE,GRENADE,QUICKFIRE,QUICKFIRE_TRAP,TELEPORT_TRAP,LIGHT_TRAP,UNDEAD_TRAP,GRENADE_TRAP,STUN_TRAP,HIDDEN_DOOR};
	public enum ActorType{PLAYER,RAT,FIRE_DRAKE,GOBLIN,LARGE_BAT,SHAMBLING_SCARECROW,SKELETON,CULTIST,POLTERGEIST,ZOMBIE,WOLF,FROSTLING,GOBLIN_ARCHER,GOBLIN_SHAMAN,SWORDSMAN,DIRE_RAT,DREAM_WARRIOR,BANSHEE,WARG,ROBED_ZEALOT,SKULKING_KILLER,CARRION_CRAWLER,OGRE,SHADOW,BERSERKER,ORC_GRENADIER,PHASE_SPIDER,STONE_GOLEM,NECROMANCER,TROLL,ORC_WARMAGE,LASHER_FUNGUS,CORPSETOWER_BEHEMOTH,DREAM_CLONE};
	public enum AttrType{STEALTHY,UNDEAD,CONSTRUCT,PLANTLIKE,DEMON,MEDIUM_HUMANOID,HUMANOID_INTELLIGENCE,FLYING,ENHANCED_TORCH,MAGICAL_BLOOD,KEEN_EYES,TOUGH,LONG_STRIDE,RUNIC_BIRTHMARK,LOW_LIGHT_VISION,DARKVISION,REGENERATING,REGENERATES_FROM_DEATH,STUNNED,PARALYZED,POISONED,IMMOBILIZED,ON_FIRE,CATCHING_FIRE,STARTED_CATCHING_FIRE_THIS_TURN,AFRAID,SLOWED,DETECTING_MONSTERS,TELEPORTING,DIM_VISION,DIM_LIGHT,FIRE_HIT,COLD_HIT,POISON_HIT,PARALYSIS_HIT,FORCE_HIT,DIM_VISION_HIT,STALAGMITE_HIT,RESIST_SLASH,RESIST_PIERCE,RESIST_BASH,RESIST_FIRE,RESIST_COLD,RESIST_ELECTRICITY,IMMUNE_FIRE,IMMUNE_COLD,IMMUNE_ARROWS,GLOBAL_FAIL_RATE,COOLDOWN_1,COOLDOWN_2,BLESSED,HOLY_SHIELDED,SPORE_BURST,TURNS_VISIBLE,RESTING,RUNNING,STUDENTS_LUCK_USED,DEFENSIVE_STANCE,DRIVE_BACK_ON,TUMBLING,DANGER_SENSE_ON,BLOOD_BOILED,WAR_SHOUTED,NIMBUS_ON,BONUS_COMBAT,BONUS_DEFENSE,BONUS_MAGIC,BONUS_SPIRIT,BONUS_STEALTH,INVULNERABLE,SMALL_GROUP,MEDIUM_GROUP,LARGE_GROUP,BOSS_MONSTER,NUM_ATTRS,NO_ATTR};
	public enum SpellType{SHINE,MAGIC_MISSILE,DETECT_MONSTERS,FORCE_PALM,BLINK,IMMOLATE,SHOCK,BURNING_HANDS,FREEZE,NIMBUS,SONIC_BOOM,ARC_LIGHTNING,ICY_BLAST,SHADOWSIGHT,RETREAT,FIREBALL,PASSAGE,FORCE_BEAM,DISINTEGRATE,BLIZZARD,BLESS,MINOR_HEAL,HOLY_SHIELD,NUM_SPELLS,NO_SPELL};
	public enum SkillType{COMBAT,DEFENSE,MAGIC,SPIRIT,STEALTH,NUM_SKILLS,NO_SKILL};
	public enum FeatType{QUICK_DRAW,SPIN_ATTACK,LUNGE,DRIVE_BACK,SILENT_CHAINMAIL,ARMORED_MAGE,FULL_DEFENSE,TUMBLE,MASTERS_EDGE,STUDENTS_LUCK,ARCANE_HEALING,FORCE_OF_WILL,WAR_SHOUT,ENDURING_SOUL,FEEL_NO_PAIN,BOILING_BLOOD,CORNER_LOOK,DISARM_TRAP,NECK_SNAP,DANGER_SENSE,NUM_FEATS,NO_FEAT};
	public enum ConsumableType{HEALING,REGENERATION,CURE_POISON,RESISTANCE,CLARITY,PHASING,TELEPORTATION,PASSAGE,DETECT_MONSTERS,MAGIC_MAP,WIZARDS_LIGHT,PRISMATIC_ORB,BANDAGE};
	public enum WeaponType{SWORD,MACE,DAGGER,STAFF,BOW,FLAMEBRAND,MACE_OF_FORCE,VENOMOUS_DAGGER,STAFF_OF_MAGIC,HOLY_LONGBOW,NUM_WEAPONS,NO_WEAPON};
	public enum ArmorType{LEATHER,CHAINMAIL,FULL_PLATE,ELVEN_LEATHER,CHAINMAIL_OF_ARCANA,FULL_PLATE_OF_RESISTANCE,NUM_ARMORS,NO_ARMOR};
	public enum MagicItemType{PENDANT_OF_LIFE,RING_OF_RESISTANCE,RING_OF_PROTECTION,CLOAK_OF_DISAPPEARANCE,NUM_MAGIC_ITEMS,NO_MAGIC_ITEM};
	public enum DamageType{NORMAL,FIRE,COLD,ELECTRIC,POISON,HEAL,SLASHING,BASHING,PIERCING,MAGIC,NONE};
	public enum DamageClass{PHYSICAL,MAGICAL,NO_TYPE};
	public enum EventType{ANY_EVENT,MOVE,REMOVE_ATTR,CHECK_FOR_HIDDEN,RELATIVELY_SAFE,POLTERGEIST,REGENERATING_FROM_DEATH,GRENADE,STALAGMITE,QUICKFIRE,BOSS_ARRIVE};
	public enum OptionType{LAST_TARGET,VI_KEYS,OPEN_CHESTS,ITEMS_AND_TILES_ARE_INTERESTING,NO_BLOOD_BOIL_MESSAGE,AUTOPICKUP,NO_ROMAN_NUMERALS};
	public class Game{
		public Map M;
		public Queue Q;
		public Buffer B;
		public Actor player;
		
		static void Main(string[] args){
			{
				int os = (int)Environment.OSVersion.Platform;
				if(os == 4 || os == 6 ||  os == 128){
					Global.LINUX = true;
				}
			}
			Console.CursorVisible = false;
			if(Global.LINUX){
				Console.SetCursorPosition(0,0);
				if(Console.BufferWidth < 80 || Console.BufferHeight < 25){
					Console.Write("Please resize your terminal to 80x25, then press any key.");
					Console.SetCursorPosition(0,1);
					Console.Write("         Current dimensions are {0}x{1}.".PadRight(57),Console.BufferWidth,Console.BufferHeight);
					Console.ReadKey(true);
					Console.SetCursorPosition(0,0);
					if(Console.BufferWidth < 80 || Console.BufferHeight < 25){
						Environment.Exit(0);
					}
				}
				Screen.Blank();
			}
			else{
				Console.BufferHeight = Global.SCREEN_H; //25
			}
			Console.TreatControlCAsInput = true;
			//Console.CursorSize = 100;
			for(int i=0;i<24;++i){
				Color color = Color.Yellow;
				if(i==18){
					color = Color.Green;
				}
				if(i>18){
					color = Color.DarkGray;
				}
				for(int j=0;j<80;++j){
					if(Global.titlescreen[i][j] != ' '){
						if(Global.titlescreen[i][j] == '#' && !Global.LINUX){
							Screen.WriteChar(i,j,new colorchar(Color.Black,Color.Yellow,' '));
						}
						else{
							Screen.WriteChar(i,j,new colorchar(color,Color.Black,Global.titlescreen[i][j]));
						}
					}
				}
			}
			Console.ReadKey(true);
			MainMenu();
		}
		static void MainMenu(){
			ConsoleKeyInfo command;
			string recentname = "".PadRight(30);
			int recentdepth = -1;
			int recentxp = -1;
			char recentwin = '-';
			while(true){
				Screen.Blank();
				Screen.WriteMapString(1,0,new cstr(Color.Yellow,"Forays into Norrendrin " + Global.VERSION));
				Screen.WriteMapString(4,0,"[a] Start a new game");
				Screen.WriteMapString(5,0,"[b] Overview");
				Screen.WriteMapString(6,0,"[c] High scores");
				Screen.WriteMapString(7,0,"[d] Quit");
				for(int i=0;i<4;++i){
					Screen.WriteMapChar(i+4,1,new colorchar(Color.Cyan,(char)(i+'a')));
				}
				Screen.ResetColors();
				Console.SetCursorPosition(Global.MAP_OFFSET_COLS,Global.MAP_OFFSET_ROWS+8);
				command = Console.ReadKey(true);
				switch(command.KeyChar){
				case 'a':
				{
					Global.GAME_OVER = false;
					Global.BOSS_KILLED = false;
					Global.LoadOptions();
					Game game = new Game();
					game.player = new Actor(ActorType.PLAYER,"you",'@',Color.White,100,100,-1,0,0);
					game.player.inv = new List<Item>();
					game.player.weapons.Remove(WeaponType.NO_WEAPON);
					game.player.weapons.AddLast(WeaponType.SWORD);
					game.player.weapons.AddLast(WeaponType.MACE);
					game.player.weapons.AddLast(WeaponType.DAGGER);
					game.player.weapons.AddLast(WeaponType.STAFF);
					game.player.weapons.AddLast(WeaponType.BOW);
					game.player.armors.Remove(ArmorType.NO_ARMOR);
					game.player.armors.AddLast(ArmorType.LEATHER);
					game.player.armors.AddLast(ArmorType.CHAINMAIL);
					game.player.armors.AddLast(ArmorType.FULL_PLATE);
					game.M = new Map(game);
					game.B = new Buffer(game);
					game.Q = new Queue(game);
					Map.Q = game.Q;
					PhysicalObject.M = game.M;
					Actor.M = game.M;
					Actor.Q = game.Q;
					Actor.B = game.B;
					Actor.player = game.player;
					Item.M = game.M;
					Item.Q = game.Q; //this part is so ugly
					Item.B = game.B;
					Item.player = game.player;
					Event.Q = game.Q;
					Event.B = game.B; //i want to change it somehow
					Event.M = game.M;
					Event.player = game.player;
					Tile.M = game.M;
					Tile.B = game.B;
					Tile.Q = game.Q;
					Tile.player = game.player;
					//game.M.InitLevel();
					//game.M.LoadLevel("map.txt");
					game.M.GenerateLevel();
					Actor.player_name = "";
					if(File.Exists("name.txt")){
						StreamReader file = new StreamReader("name.txt");
						string base_name = file.ReadLine();
						Actor.player_name = base_name;
						int num = 1;
						if(!Global.Option(OptionType.NO_ROMAN_NUMERALS) && file.Peek() != -1){
							num = Convert.ToInt32(file.ReadLine());
							if(num > 1){
								Actor.player_name = Actor.player_name + " " + Global.RomanNumeral(num);
							}
						}
						file.Close();
						StreamWriter fileout = new StreamWriter("name.txt",false);
						fileout.WriteLine(base_name);
						if(!Global.Option(OptionType.NO_ROMAN_NUMERALS)){
							fileout.WriteLine(num+1);
						}
						fileout.Close();
					}
					while(Actor.player_name == ""){
						Console.CursorVisible = false;
						game.B.DisplayNow("".PadRight(Global.COLS));
						game.B.DisplayNow("Enter name: ");
						Actor.player_name = Global.EnterString(26);
					}
					Screen.Blank();
					Screen.WriteMapString(0,0,"".PadRight(Global.COLS,'-'));
					Screen.WriteMapString(1,0,"[a] Toughness - You have a slight resistance to physical damage.");
					Screen.WriteMapString(2,0,"[b] Magical blood - Your natural recovery is faster than normal.");
					Screen.WriteMapString(3,0,"[c] Low-light vision - You can see farther in darkness.");
					Screen.WriteMapString(4,0,"[d] Keen eyes - You're better at spotting traps and aiming arrows.");
					Screen.WriteMapString(5,0,"[e] Long stride - You are slightly faster than normal.");//todo previous char/use name
					Screen.WriteMapString(6,0,"".PadRight(Global.COLS,'-'));
					if(File.Exists("quickstart.txt")){
						Screen.WriteMapString(16,5,"[ ] Repeat previous choices and start immediately.");
						Screen.WriteMapChar(16,6,new colorchar('p',Color.Cyan));
					}
					if(!File.Exists("name.txt")){
						Screen.WriteMapString(18,5,"[ ] Automatically name future characters after this one.");
						Screen.WriteMapChar(18,6,new colorchar('n',Color.Cyan));
					}
					for(int i=0;i<5;++i){
						Screen.WriteMapChar(i+1,1,new colorchar(Color.Cyan,(char)(i+'a')));
					}
					Screen.WriteMapString(-1,0,"Select a trait: "); //haha, it works
					Console.CursorVisible = true;
					bool quickstarted = false;
					Global.quickstartinfo = new List<string>();
					for(bool good=false;!good;){
						command = Console.ReadKey(true);
						switch(command.KeyChar){
						case 'a':
							good = true;
							game.player.attrs[AttrType.TOUGH]++;
							Global.quickstartinfo.Add("tough");
							break;
						case 'b':
							good = true;
							game.player.attrs[AttrType.MAGICAL_BLOOD]++;
							Global.quickstartinfo.Add("magical_blood");
							break;
						case 'c':
							good = true;
							game.player.attrs[AttrType.LOW_LIGHT_VISION]++;
							Global.quickstartinfo.Add("low_light_vision");
							break;
						case 'd':
							good = true;
							game.player.attrs[AttrType.KEEN_EYES]++;
							Global.quickstartinfo.Add("keen_eyes");
							break;
						case 'e':
							good = true;
							game.player.attrs[AttrType.LONG_STRIDE]++;
							game.player.speed = 90;
							Global.quickstartinfo.Add("long_stride");
							break;
						case 'p':
						{
							if(File.Exists("quickstart.txt")){
								quickstarted = true;
								good = true;
								game.B.Add("Welcome, " + Actor.player_name + "! ");
								StreamReader file = new StreamReader("quickstart.txt");
								AttrType attr = (AttrType)Enum.Parse(typeof(AttrType),file.ReadLine(),true);
								game.player.attrs[attr]++;
								bool magic = false;
								for(int i=0;i<3;++i){
									SkillType skill = (SkillType)Enum.Parse(typeof(SkillType),file.ReadLine(),true);
									if(skill == SkillType.MAGIC){
										magic = true;
									}
									game.player.skills[skill]++;
								}
								for(int i=0;i<3;++i){
									FeatType feat = (FeatType)Enum.Parse(typeof(FeatType),file.ReadLine(),true);
									game.player.feats[feat]--;
									if(game.player.feats[feat] == -(Feat.MaxRank(feat))){
										game.player.feats[feat] = 1;
										game.B.Add("You learn the " + Feat.Name(feat) + " feat. ");
									}
								}
								if(magic){
									SpellType spell = (SpellType)Enum.Parse(typeof(SpellType),file.ReadLine(),true);
									game.player.spells[spell]++;
									game.B.Add("You learn " + Spell.Name(spell) + ". ");
								}
								file.Close();
							}
							break;
						}
						case 'n':
							if(!File.Exists("name.txt")){
								StreamWriter fileout = new StreamWriter("name.txt",false);
								fileout.WriteLine(Actor.player_name);
								if(!Global.Option(OptionType.NO_ROMAN_NUMERALS)){
									fileout.WriteLine("2");
								}
								fileout.Close();
								//Screen.WriteMapString(18,5,"                                                        ");
								Screen.WriteMapString(18,5,"(to stop automatically naming characters, delete name.txt)");
								Console.SetCursorPosition(16+Global.MAP_OFFSET_COLS,1);
							}
							break;
						default:
							break;
						}
					}
					game.player.Q0();
					//game.player.Move(10,20,false); //this is why the voodoo was needed before: the player must be moved onto the map *before*
					game.player.UpdateRadius(0,6,true); //gaining a light radius.
					Item.Create(ConsumableType.HEALING,game.player);
					Item.Create(ConsumableType.PHASING,game.player);
					Item.Create(ConsumableType.BANDAGE,game.player);
					Item.Create(ConsumableType.BANDAGE,game.player);
					if(quickstarted){
						game.player.xp = 0;
						game.player.level = 1;
					}
					else{
						game.player.GainXP(1);
						var fileout = new StreamWriter("quickstart.txt",false);
						foreach(string s in Global.quickstartinfo){
							fileout.WriteLine(s.ToLower());
						}
						fileout.Close();
						Global.quickstartinfo = null;
					}
					while(!Global.GAME_OVER){ game.Q.Pop(); }
					Console.CursorVisible = false;
					Global.SaveOptions();
					recentdepth = game.M.current_level;
					recentxp = game.player.xp;
					recentname = Actor.player_name;
					recentwin = Global.BOSS_KILLED? 'W' : '-';
					{
						List<string> newhighscores = new List<string>();
						int num_scores = 0;
						bool added = false;
						StreamReader file = new StreamReader("highscore.txt");
						string s = "";
						while(s.Length < 2 || s.Substring(0,2) != "--"){
							s = file.ReadLine();
							newhighscores.Add(s);
						}
						s = "!!";
						while(s.Substring(0,2) != "--"){
							s = file.ReadLine();
							if(s.Substring(0,2) == "--"){
								if(!added && num_scores < 20){
									char symbol = Global.BOSS_KILLED? 'W' : '-';
									newhighscores.Add(game.player.level.ToString() + " " + game.M.current_level.ToString() + " "
										+ game.player.xp + " " + symbol + " " + Actor.player_name);
								}
								newhighscores.Add(s);
								break;
							}
							if(num_scores < 20){
								string[] tokens = s.Split(' ');
								int dlev = Convert.ToInt32(tokens[1]);
								int xp = Convert.ToInt32(tokens[2]);
								if(xp < game.player.xp){
									if(!added){
										char symbol = Global.BOSS_KILLED? 'W' : '-';
										newhighscores.Add(game.player.level.ToString() + " " + game.M.current_level.ToString() + " "
											+ game.player.xp + " " + symbol + " " + Actor.player_name);
										++num_scores;
										added = true;
									}
									if(num_scores < 20){
										newhighscores.Add(s);
										++num_scores;
									}
								}
								else{
									if(xp == game.player.xp && dlev < game.M.current_level){
										if(!added){
											char symbol = Global.BOSS_KILLED? 'W' : '-';
											newhighscores.Add(game.player.level.ToString() + " " + game.M.current_level.ToString() + " "
												+ game.player.xp + " " + symbol + " " + Actor.player_name);
											++num_scores;
											added = true;
										}
										if(num_scores < 20){
											newhighscores.Add(s);
											++num_scores;
										}
									}
									else{
										newhighscores.Add(s);
										++num_scores;
									}
								}
							}
						}
						s = "!!";
						num_scores = 0;
						added = false;
						while(s.Substring(0,2) != "--"){
							s = file.ReadLine();
							if(s.Substring(0,2) == "--"){
								if(!added && num_scores < 20){
									char symbol = Global.BOSS_KILLED? 'W' : '-';
									newhighscores.Add(game.M.current_level.ToString() + " " + game.player.level.ToString() + " "
										+ game.player.xp + " " + symbol + " " + Actor.player_name);
								}
								newhighscores.Add(s);
								break;
							}
							if(num_scores < 20){
								string[] tokens = s.Split(' ');
								int dlev = Convert.ToInt32(tokens[0]);
								int xp = Convert.ToInt32(tokens[2]);
								if(dlev < game.M.current_level){
									if(!added){
										char symbol = Global.BOSS_KILLED? 'W' : '-';
										newhighscores.Add(game.M.current_level.ToString() + " " + game.player.level.ToString() + " "
											+ game.player.xp + " " + symbol + " " + Actor.player_name);
										++num_scores;
										added = true;
									}
									if(num_scores < 20){
										newhighscores.Add(s);
										++num_scores;
									}
								}
								else{
									if(dlev == game.M.current_level && xp < game.player.xp){
										if(!added){
											char symbol = Global.BOSS_KILLED? 'W' : '-';
											newhighscores.Add(game.M.current_level.ToString() + " " + game.player.level.ToString() + " "
												+ game.player.xp + " " + symbol + " " + Actor.player_name);
											++num_scores;
											added = true;
										}
										if(num_scores < 20){
											newhighscores.Add(s);
											++num_scores;
										}
									}
									else{
										newhighscores.Add(s);
										++num_scores;
									}
								}
							}
						}
						file.Close();
						StreamWriter fileout = new StreamWriter("highscore.txt",false);
						foreach(string str in newhighscores){
							fileout.WriteLine(str);
						}
						fileout.Close();
					}
					if(!Global.QUITTING){
						List<string> ls = new List<string>();
						ls.Add("See the map");
						ls.Add("See last messages");
						ls.Add("Examine your equipment");
						ls.Add("Examine your inventory");
						ls.Add("See character info");
						ls.Add("Write this information to a file");
						ls.Add("Done");
						for(bool done=false;!done;){
							game.player.Select("Would you like to examine your character! ","".PadRight(Global.COLS),"".PadRight(Global.COLS),ls,true,false,false);
							int sel = game.player.GetSelection("Would you like to examine your character? ",7,true,false);
							switch(sel){
							case 0:
								foreach(Tile t in game.M.AllTiles()){
									if(t.type != TileType.FLOOR && !t.IsTrap()){
										bool good = false;
										foreach(Tile neighbor in t.TilesAtDistance(1)){
											if(neighbor.type != TileType.WALL){
												good = true;
											}
										}
										if(good){
											t.seen = true;
										}
									}
								}
								game.B.DisplayNow("Press any key to continue. ");
								Console.CursorVisible = true;
								Screen.WriteMapChar(0,0,'-');
								game.M.Draw();
								Console.ReadKey(true);
								break;
							case 1:
							{
								Screen.WriteMapString(0,0,"".PadRight(Global.COLS,'-'));
								int i = 1;
								foreach(string s in game.B.GetMessages()){
									Screen.WriteMapString(i,0,s.PadRight(Global.COLS));
									++i;
								}
								Screen.WriteMapString(21,0,"".PadRight(Global.COLS,'-'));
								game.B.DisplayNow("Previous messages: ");
								Console.CursorVisible = true;
								Console.ReadKey(true);
								break;
							}
							case 2:
								game.player.DisplayEquipment();
								break;
							case 3:
								for(int i=1;i<8;++i){
									Screen.WriteMapString(i,0,"".PadRight(Global.COLS));
								}
								game.player.Select("In your pack: ",game.player.InventoryList(),true,false,false);
								Console.ReadKey(true);
								break;
							case 4:
								game.player.DisplayCharacterInfo();
								break;
							case 5:
							{
								game.B.DisplayNow("Enter file name: ");
								Console.CursorVisible = true;
								string filename = Global.EnterString(40);
								if(filename == ""){
									break;
								}
								StreamWriter file = new StreamWriter(filename,true);
								game.player.DisplayCharacterInfo(false);
								file.WriteLine("             Character information: ");
								colorchar[,] screen = Screen.GetCurrentScreen();
								for(int i=2;i<Global.SCREEN_H;++i){
									for(int j=0;j<Global.SCREEN_W;++j){
										file.Write(screen[i,j].c);
									}
									file.WriteLine();
								}
								file.WriteLine();
								file.WriteLine("Inventory: ");
								foreach(string s in game.player.InventoryList()){
									file.WriteLine(s);
								}
								file.WriteLine();
								file.WriteLine();
								foreach(Tile t in game.M.AllTiles()){
									if(t.type != TileType.FLOOR && !t.IsTrap()){
										bool good = false;
										foreach(Tile neighbor in t.TilesAtDistance(1)){
											if(neighbor.type != TileType.WALL){
												good = true;
											}
										}
										if(good){
											t.seen = true;
										}
									}
								}
								Screen.WriteMapChar(0,0,'-');
								game.M.Draw();
								int col = 0;
								foreach(colorchar cch in Screen.GetCurrentMap()){
									file.Write(cch.c);
									++col;
									if(col == Global.COLS){
										file.WriteLine();
										col = 0;
									}
								}
								file.WriteLine();
								Screen.WriteMapString(0,0,"".PadRight(Global.COLS,'-'));
								int line = 1;
								foreach(string s in game.B.GetMessages()){
									Screen.WriteMapString(line,0,s.PadRight(Global.COLS));
									++line;
								}
								Screen.WriteMapString(21,0,"".PadRight(Global.COLS,'-'));
								file.WriteLine("Last messages: ");
								col = 0;
								foreach(colorchar cch in Screen.GetCurrentMap()){
									file.Write(cch.c);
									++col;
									if(col == Global.COLS){
										file.WriteLine();
										col = 0;
									}
								}
								file.WriteLine();
								file.Close();
								break;
							}
							case 6:
								done = true;
								break;
							default:
								break;
							}
						}
					}
					break;
				}
				case 'b':
				{
					Global.DisplayHelp();
					break;
				}
				case 'c':
				{
					StreamReader file = new StreamReader("highscore.txt");
					Screen.Blank();
					Color title = Color.Red;
					Color primary = Color.Green;
					Color secondary = Color.DarkGreen;
					Color recent = Color.Cyan;
					Screen.WriteString(0,34,new cstr("HIGH SCORES",Color.Magenta));
					Screen.WriteString(1,34,new cstr("-----------",Color.Blue));
					Screen.WriteString(2,11,new cstr("Most experienced:",title));
					Screen.WriteString(2,56,new cstr("Deepest:",title));
					Screen.WriteString(3,26,new cstr("Level",primary));
					Screen.WriteString(3,33,new cstr("Depth",secondary));
					Screen.WriteString(3,68,new cstr("Depth",primary));
					Screen.WriteString(3,75,new cstr("Level",secondary));
					bool written_recent = false;
					string s = "";
					while(s.Length < 2 || s.Substring(0,2) != "--"){
						s = file.ReadLine();
					}
					int line = 4;
					s = "!!";
					while(s.Substring(0,2) != "--"){
						s = file.ReadLine();
						if(s.Substring(0,2) == "--"){
							break;
						}
						if(line > 23){
							continue;
						}
						string[] tokens = s.Split(' ');
						int clev = Convert.ToInt32(tokens[0]);
						int dlev = Convert.ToInt32(tokens[1]);
						int xp = Convert.ToInt32(tokens[2]);
						char winning = tokens[3][0];
						string name = s.Substring(tokens[0].Length + tokens[1].Length + tokens[2].Length + 5);
						if(!written_recent && xp == recentxp && name == recentname && dlev == recentdepth && winning == recentwin){
							Screen.WriteString(line,0,new cstr(name,recent));
							written_recent = true;
						}
						else{
							Screen.WriteString(line,0,new cstr(name,Color.White));
						}
						Screen.WriteString(line,27,new cstr(clev.ToString().PadLeft(2),Color.White));
						Screen.WriteString(line,34,new cstr(dlev.ToString().PadLeft(2),Color.DarkGray));
						if(winning == 'W'){
							Screen.WriteString(line,37,new cstr("W",Color.Yellow));
						}
						++line;
					}
					written_recent = false;
					line = 4;
					s = "!!";
					while(s.Substring(0,2) != "--"){
						s = file.ReadLine();
						if(s.Substring(0,2) == "--"){
							break;
						}
						if(line > 23){
							continue;
						}
						string[] tokens = s.Split(' ');
						int dlev = Convert.ToInt32(tokens[0]);
						int clev = Convert.ToInt32(tokens[1]);
						int xp = Convert.ToInt32(tokens[2]);
						char winning = tokens[3][0];
						string name = s.Substring(tokens[0].Length + tokens[1].Length + tokens[2].Length + 5);
						if(!written_recent && xp == recentxp && name == recentname && dlev == recentdepth && winning == recentwin){
							Screen.WriteString(line,42,new cstr(name,recent));
							written_recent = true;
						}
						else{
							Screen.WriteString(line,42,new cstr(name,Color.White));
						}
						Screen.WriteString(line,69,new cstr(dlev.ToString().PadLeft(2),Color.White));
						Screen.WriteString(line,76,new cstr(clev.ToString().PadLeft(2),Color.DarkGray));
						if(winning == 'W'){
							Screen.WriteString(line,79,new cstr("W",Color.Yellow));
						}
						++line;
					}
					Console.ReadKey(true);
					file.Close();
					break;
				}
				case 'd':
					Global.Quit();
					break;
				default:
					break;
				}
				if(Global.QUITTING){
					Global.Quit();
				}
			}
		}
	}
}
