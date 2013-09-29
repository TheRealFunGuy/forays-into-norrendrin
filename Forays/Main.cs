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
using Utilities;
using Forays;
namespace Forays{
	public enum TileType{WALL,FLOOR,DOOR_O,DOOR_C,STAIRS,CHEST,FIREPIT,STALAGMITE,QUICKFIRE_TRAP,TELEPORT_TRAP,LIGHT_TRAP,UNDEAD_TRAP,GRENADE_TRAP,STUN_TRAP,ALARM_TRAP,DARKNESS_TRAP,POISON_GAS_TRAP,DIM_VISION_TRAP,ICE_TRAP,PHANTOM_TRAP,HIDDEN_DOOR,COMBAT_SHRINE,DEFENSE_SHRINE,MAGIC_SHRINE,SPIRIT_SHRINE,STEALTH_SHRINE,RUINED_SHRINE,SPELL_EXCHANGE_SHRINE,RUBBLE,FIRE_GEYSER,STATUE,HEALING_POOL,FOG_VENT,POISON_GAS_VENT,STONE_SLAB,CHASM,BREACHED_WALL,WATER,ICE,CRACKED_WALL,BRUSH,POPPY_FIELD,JUNGLE,GRAVEL,BLAST_FUNGUS,GLOWING_FUNGUS,TOMBSTONE,GRAVE_DIRT};
	public enum FeatureType{GRENADE,TROLL_CORPSE,TROLL_BLOODWITCH_CORPSE,QUICKFIRE,FOG,POISON_GAS,SLIME,TELEPORTAL,INACTIVE_TELEPORTAL,STABLE_TELEPORTAL,OIL,FIRE,BONES,WEB,PIXIE_DUST};
	public enum ActorType{PLAYER,RAT,FIRE_DRAKE,GOBLIN,GIANT_BAT,LONE_WOLF,BLOOD_MOTH,DARKNESS_DWELLER,CARNIVOROUS_BRAMBLE,FROSTLING,SWORDSMAN,DREAM_WARRIOR,SPITTING_COBRA,KOBOLD,SPORE_POD,FORASECT,POLTERGEIST,CULTIST,GOBLIN_ARCHER,GOBLIN_SHAMAN,GOLDEN_DART_FROG,SKELETON,SHADOW,MIMIC,PHASE_SPIDER,ZOMBIE,ROBED_ZEALOT,GIANT_SLUG,VULGAR_DEMON,BANSHEE,CAVERN_HAG,BERSERKER,DIRE_RAT,SKULKING_KILLER,WILD_BOAR,TROLL,DREAM_SPRITE,CLOUD_ELEMENTAL,DERANGED_ASCETIC,SHADOWVEIL_DUELIST,WARG,ALASI_SCOUT,CARRION_CRAWLER,MECHANICAL_KNIGHT,RUNIC_TRANSCENDENT,ALASI_BATTLEMAGE,ALASI_SOLDIER,INFESTED_MASS_TODO_NAME,STONE_GOLEM,MUD_ELEMENTAL,FLAMETONGUE_TOAD,ENTRANCER,OGRE,ORC_GRENADIER,SPELLMUDDLE_PIXIE,CRUSADING_KNIGHT,TROLL_BLOODWITCH,SAVAGE_HULK,MARBLE_HORROR,CRYOLICH,PYREN_ARCHER,PLACEHOLDER,ALASI_SENTINEL,NOXIOUS_WORM,LASHER_FUNGUS,VAMPIRE,ORC_WARMAGE,NECROMANCER,STALKING_WEBSTRIDER,ORC_ASSASSIN,LUMINOUS_AVENGER,CORPSETOWER_BEHEMOTH,MACHINE_OF_WAR,DREAM_WARRIOR_CLONE,DREAM_SPRITE_CLONE,MUD_TENTACLE,MARBLE_HORROR_STATUE,GHOST,PHANTOM,PHANTOM_ZOMBIE,PHANTOM_CRUSADER,PHANTOM_TIGER,PHANTOM_OGRE,PHANTOM_BEHEMOTH,PHANTOM_BLIGHTWING,PHANTOM_SWORDMASTER,PHANTOM_ARCHER,PHANTOM_CONSTRICTOR};
	public enum AttrType{STEALTHY,NONLIVING,PLANTLIKE,MEDIUM_HUMANOID,HUMANOID_INTELLIGENCE,KEEN_SENSES,BLINDSIGHT,SMALL,FLYING,WANDERING,IMMOBILE,SHADOW_CLOAK,BRUTISH_STRENGTH,VIGOR,SILENCED,NOTICED,PLAYER_NOTICED,DANGER_SENSED,SHINING,LOW_LIGHT_VISION,REGENERATING,REGENERATES_FROM_DEATH,REASSEMBLES,NO_ITEM,STUNNED,PARALYZED,BLIND,POISONED,FROZEN,CHILLED,SLIMED,GREASED,BURNING,CATCHING_FIRE,STARTED_CATCHING_FIRE_THIS_TURN,AFRAID,SLOWED,POPPY_COUNTER,MAGICAL_DROWSINESS,ASLEEP,AGGRAVATING,DETECTING_MONSTERS,DETECTING_MOVEMENT,TELEPORTING,VULNERABLE,SUSCEPTIBLE_TO_CRITS,LIGHT_SENSITIVE,DESTROYED_BY_SUNLIGHT,DIM_VISION,DIM_LIGHT,POISON_HIT,PARALYSIS_HIT,DIM_VISION_HIT,STALAGMITE_HIT,WORN_OUT_HIT,STUN_HIT,LIFE_DRAIN_HIT,GRAB_HIT,FIERY_ARROWS,DULLS_BLADES,IMMUNE_BURNING,IMMUNE_FIRE,IMMUNE_COLD,IMMUNE_ELECTRICITY,RESIST_WEAPONS,IMMUNE_ARROWS,RESIST_NECK_SNAP,COMBO_ATTACK,COOLDOWN_1,COOLDOWN_2,HOLY_SHIELDED,ARCANE_SHIELDED,SPORE_BURST,CAN_POISON_BLADES,SPELL_DISRUPTION,TERRIFYING,DAMAGE_REDUCTION,MECHANICAL_SHIELD,TURNS_HERE,TURNS_VISIBLE,RESTING,RUNNING,WAITING,AUTOEXPLORE,TUMBLING,BLOOD_BOILED,SHADOWSIGHT,IN_COMBAT,CONVICTION,KILLSTREAK,EMPOWERED_SPELLS,WARG_HOWL,PERMANENT_DAMAGE,JUST_GRABBED,JUST_TELEPORTED,JUST_FLUNG,JUST_BITTEN,LUNGING_AUTO_HIT,ROOTS,DODGED,CHAIN_CAST,IGNORES_QUIET_SOUNDS,ALERTED,SEES_ADJACENT_PLAYER,DIRECTION_OF_PREVIOUS_TILE,FOLLOW_DIRECTION_EXITED,AMNESIA_STUN,GRABBED,GRABBING,BONUS_COMBAT,BONUS_DEFENSE,BONUS_MAGIC,BONUS_SPIRIT,BONUS_STEALTH,INVULNERABLE,SMALL_GROUP,MEDIUM_GROUP,LARGE_GROUP,TURN_INTO_CORPSE,CORPSE,NO_CORPSE_KNOCKBACK,BOSS_MONSTER,NUM_ATTRS,NO_ATTR};
	public enum SpellType{SHINE,FORCE_PALM,DETECT_MOVEMENT,RADIANCE,MERCURIAL_SPHERE,GREASE,BLINK,FREEZE,SCORCH,LIGHTNING_BOLT,MAGIC_HAMMER,PORTAL,GLACIAL_BLAST,PASSAGE,AMNESIA,SHADOWSIGHT,COLLAPSE,BLIZZARD,FIRE_BLITZ,PLACEHOLDER,NUM_SPELLS,NO_SPELL};
	public enum SkillType{COMBAT,DEFENSE,MAGIC,SPIRIT,STEALTH,NUM_SKILLS,NO_SKILL};
	public enum FeatType{QUICK_DRAW,ATTACK_EVERYTHING_TODO,LUNGE,DRIVE_BACK,ARMOR_MASTERY,CUNNING_DODGE,DEFLECT_ATTACK,TUMBLE,MASTERS_EDGE,ARCANE_INTERFERENCE,CHAIN_CASTING,FORCE_OF_WILL,CONVICTION,ENDURING_SOUL,FEEL_NO_PAIN,BOILING_BLOOD,NECK_SNAP,DISARM_TRAP,CORNER_CLIMB,DANGER_SENSE,NUM_FEATS,NO_FEAT};
	public enum ConsumableType{HEALING,REGENERATION,STONEFORM,VAMPIRISM,BRUTISH_STRENGTH,ROOTS,VIGOR,SILENCE,CLOAKING,BLINKING,PASSAGE,TIME,DETECT_MONSTERS,MAGIC_MAP,SUNLIGHT,DARKNESS,REPAIR,CALLING,TRAP_CLEARING,ENCHANTMENT,FREEZING,FLAMES,FOG,DETONATION,BREACHING,SHIELDING,TELEPORTAL,PAIN,BANDAGE,BLAST_FUNGUS,TRAP};
	public enum WeaponType{SWORD,MACE,DAGGER,STAFF,BOW,NUM_WEAPONS,NO_WEAPON};
	public enum ArmorType{LEATHER,CHAINMAIL,FULL_PLATE,NUM_ARMORS,NO_ARMOR};
	public enum EnchantmentType{CHILLING,ECHOES,DISRUPTION,PRECISION,VICTORY,NUM_ENCHANTMENTS,NO_ENCHANTMENT};
	public enum EquipmentStatus{DULLED,POSSESSED,BURDENSOME,NEGATED,CURSED,INFESTED,RUSTED,WEAK_POINT,WORN_OUT,DAMAGED,NUM_STATUS};
	public enum MagicTrinketType{PENDANT_OF_LIFE,CLOAK_OF_SAFETY,BRACERS_OF_ARROW_DEFLECTION,CIRCLET_OF_THE_THIRD_EYE,RING_OF_KEEN_SIGHT,RING_OF_THE_LETHARGIC_FLAME,LENS_OF_SCRYING,BELT_OF_TOUGHNESS,NUM_MAGIC_TRINKETS,NO_MAGIC_TRINKET};
	public enum DamageType{NORMAL,FIRE,COLD,ELECTRIC,POISON,MAGIC,NONE};
	public enum DamageClass{PHYSICAL,MAGICAL,NO_TYPE};
	public enum CriticalEffect{STUN,ONE_TURN_STUN,MAX_DAMAGE,PERCENT_DAMAGE,WEAK_POINT,WORN_OUT,REDUCE_ACCURACY,DRAIN_LIFE,GRAB,CHILL,FREEZE,INFLICT_VULNERABILITY,TRIP,KNOCKBACK,STRONG_KNOCKBACK,IGNITE,DIM_VISION,SWAP_POSITIONS,SLIME,MAKE_NOISE,BLIND,SLOW,POISON,PARALYZE,ONE_TURN_PARALYZE,STALAGMITES,FLING,SILENCE,INFEST,DISRUPTION,VICTORY,NO_CRIT};
	public enum EventType{ANY_EVENT,MOVE,REMOVE_ATTR,CHECK_FOR_HIDDEN,RELATIVELY_SAFE,POLTERGEIST,MIMIC,REGENERATING_FROM_DEATH,REASSEMBLING,GRENADE,BLAST_FUNGUS,STALAGMITE,FIRE_GEYSER,FIRE_GEYSER_ERUPTION,FOG_VENT,FOG,POISON_GAS_VENT,POISON_GAS,STONE_SLAB,MARBLE_HORROR,QUICKFIRE,BOSS_SIGN,BOSS_ARRIVE,FLOOR_COLLAPSE,CEILING_COLLAPSE,NORMAL_LIGHTING,TELEPORTAL,BREACH,GRAVE_DIRT,POPPIES,TOMBSTONE_GHOST,SHIELDING,PIXIE_DUST};
	public enum OptionType{LAST_TARGET,AUTOPICKUP,NO_ROMAN_NUMERALS,HIDE_OLD_MESSAGES,HIDE_COMMANDS,NEVER_DISPLAY_TIPS,ALWAYS_RESET_TIPS};
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
				Console.Title = "Forays into Norrendrin";
				Console.BufferHeight = Global.SCREEN_H; //25
			}
			Console.TreatControlCAsInput = true;
			U.SetBoundsStartingAtZero(22,66);
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
			char recentwin = '-';
			string recentcause = "";
			while(true){
				Screen.Blank();
				Screen.WriteMapString(1,0,new cstr(Color.Yellow,"Forays into Norrendrin " + Global.VERSION));
				bool saved_game = File.Exists("forays.sav");
				if(!saved_game){
					Screen.WriteMapString(4,0,"[a] Start a new game");
				}
				else{
					Screen.WriteMapString(4,0,"[a] Resume saved game");
				}
				Screen.WriteMapString(5,0,"[b] How to play");
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
					Global.SAVING = false;
					Global.LoadOptions();
					Game game = new Game();
					if(!saved_game){
						game.player = new Actor(ActorType.PLAYER,"you",'@',Color.White,100,100,0,0,AttrType.HUMANOID_INTELLIGENCE);
						Actor.attack[ActorType.PLAYER] = new List<AttackInfo>{new AttackInfo(100,2,CriticalEffect.NO_CRIT,"& hit *","& miss *")};
						game.player.inv = new List<Item>();
						Actor.feats_in_order = new List<FeatType>();
						Actor.spells_in_order = new List<SpellType>();
						game.player.weapons.AddLast(new Weapon(WeaponType.SWORD));
						game.player.weapons.AddLast(new Weapon(WeaponType.MACE));
						game.player.weapons.AddLast(new Weapon(WeaponType.DAGGER));
						game.player.weapons.AddLast(new Weapon(WeaponType.STAFF));
						game.player.weapons.AddLast(new Weapon(WeaponType.BOW));
						game.player.armors.AddLast(new Armor(ArmorType.LEATHER));
						game.player.armors.AddLast(new Armor(ArmorType.CHAINMAIL));
						game.player.armors.AddLast(new Armor(ArmorType.FULL_PLATE));
					}
					game.M = new Map(game);
					game.B = new Buffer(game);
					game.Q = new Queue(game);
					Map.Q = game.Q;
					Map.B = game.B;
					PhysicalObject.M = game.M;
					PhysicalObject.B = game.B;
					PhysicalObject.Q = game.Q;
					PhysicalObject.player = game.player;
					Event.Q = game.Q;
					Event.B = game.B;
					Event.M = game.M;
					Event.player = game.player;
					if(!saved_game){
						Actor.player_name = "";
						if(File.Exists("name.txt")){
							StreamReader file = new StreamReader("name.txt");
							string base_name = file.ReadLine();
							if(base_name == "%random%"){
								Actor.player_name = Global.GenerateCharacterName();
							}
							else{
								Actor.player_name = base_name;
							}
							int num = 0;
							if(base_name != "%random%" && file.Peek() != -1){
								num = Convert.ToInt32(file.ReadLine());
								if(num > 1){
									Actor.player_name = Actor.player_name + " " + Global.RomanNumeral(num);
								}
							}
							file.Close();
							if(num > 0){
								StreamWriter fileout = new StreamWriter("name.txt",false);
								fileout.WriteLine(base_name);
								fileout.WriteLine(num+1);
								fileout.Close();
							}
						}
						if(Actor.player_name == ""){
							for(int i=4;i<=7;++i){
								Screen.WriteMapString(i,0,"".PadToMapSize());
							}
							string s = "";
							int name_option = 0;
							while(true){
								Screen.WriteMapString(4,0,"Enter name: ");
								if(s == ""){
									Screen.WriteMapString(6,0,"(Press [Enter] for a random name)".GetColorString());
								}
								else{
									Screen.WriteMapString(6,0,"(Press [Enter] when finished)    ".GetColorString());
								}
								List<string> name_options = new List<string>{"Default: Choose a new name for each character","Static:  Use this name for every character","Legacy:  Name all future characters after this one","Random:  Name all future characters randomly"};
								for(int i=0;i<4;++i){
									Color option_color = Color.DarkGray;
									if(i == name_option){
										option_color = Color.White;
									}
									Screen.WriteMapString(15+i,0,name_options[i],option_color);
								}
								Screen.WriteMapString(20,0,"(Press [Tab] to change naming preference)".GetColorString());
								if(name_option != 0){
									Screen.WriteMapString(21,0,"(To stop naming characters automatically, delete name.txt)");
								}
								else{
									Screen.WriteMapString(21,0,"".PadToMapSize());
								}
								Screen.WriteMapString(4,12,s.PadRight(26));
								Console.SetCursorPosition(Global.MAP_OFFSET_COLS + 12 + s.Length,Global.MAP_OFFSET_ROWS + 4);
								Console.CursorVisible = true;
								command = Console.ReadKey(true);
								if((command.KeyChar >= '!' && command.KeyChar <= '~') || command.KeyChar == ' '){
									if(s.Length < 26){
										s = s + command.KeyChar;
									}
								}
								else{
									if(command.Key == ConsoleKey.Backspace && s.Length > 0){
										s = s.Substring(0,s.Length-1);
									}
									else{
										if(command.Key == ConsoleKey.Escape){
											s = "";
										}
										else{
											if(command.Key == ConsoleKey.Tab){
												name_option = (name_option + 1) % 4;
											}
											else{
												if(command.Key == ConsoleKey.Enter){
													if(s.Length == 0){
														s = Global.GenerateCharacterName();
													}
													else{
														Actor.player_name = s;
														break;
													}
												}
											}
										}
									}
								}
							}
							switch(name_option){
							case 1: //static
							{
								StreamWriter fileout = new StreamWriter("name.txt",false);
								fileout.WriteLine(s);
								fileout.WriteLine(0);
								fileout.Close();
								break;
							}
							case 2: //legacy
							{
								StreamWriter fileout = new StreamWriter("name.txt",false);
								fileout.WriteLine(s);
								fileout.WriteLine(2);
								fileout.Close();
								break;
							}
							case 3: //random
							{
								StreamWriter fileout = new StreamWriter("name.txt",false);
								fileout.WriteLine("%random%");
								fileout.WriteLine(0);
								fileout.Close();
								break;
							}
							}
						}
						Item.GenerateUnIDedNames();
						game.M.GenerateLevelTypes();
						game.M.GenerateLevel();
						{
							Event e = new Event(game.player,0,EventType.MOVE);
							e.tiebreaker = 0;
							game.Q.Add(e);
						}
						game.player.UpdateRadius(0,6,true);
						Item.Create(ConsumableType.HEALING,game.player);
						Item.Create(ConsumableType.BLINKING,game.player);
						Item.Create(ConsumableType.BANDAGE,game.player);
						Item.Create(ConsumableType.BANDAGE,game.player);
					}
					else{ //loading
						FileStream file = new FileStream("forays.sav",FileMode.Open);
						BinaryReader b = new BinaryReader(file);
						Dictionary<int,PhysicalObject> id = new Dictionary<int, PhysicalObject>();
						id.Add(0,null);
						Dict<PhysicalObject,int> missing_target_id = new Dict<PhysicalObject, int>();
						List<Actor> need_targets = new List<Actor>();
						Dict<PhysicalObject,int> missing_location_id = new Dict<PhysicalObject, int>();
						List<Actor> need_location = new List<Actor>();
						Actor.player_name = b.ReadString();
						game.M.current_level = b.ReadInt32();
						game.M.level_types = new List<LevelType>();
						for(int i=0;i<20;++i){
							game.M.level_types.Add((LevelType)b.ReadInt32());
						}
						game.M.wiz_lite = b.ReadBoolean();
						game.M.wiz_dark = b.ReadBoolean();
						//skipping danger_sensed
						Actor.feats_in_order = new List<FeatType>();
						Actor.spells_in_order = new List<SpellType>();
						int num_featlist = b.ReadInt32();
						for(int i=0;i<num_featlist;++i){
							Actor.feats_in_order.Add((FeatType)b.ReadInt32());
						}
						int num_spelllist = b.ReadInt32();
						for(int i=0;i<num_spelllist;++i){
							Actor.spells_in_order.Add((SpellType)b.ReadInt32());
						}
						int num_actors = b.ReadInt32();
						for(int i=0;i<num_actors;++i){
							Actor a = new Actor();
							int ID = b.ReadInt32();
							id.Add(ID,a);
							a.row = b.ReadInt32();
							a.col = b.ReadInt32();
							game.M.actor[a.row,a.col] = a;
							a.name = b.ReadString();
							a.the_name = b.ReadString();
							a.a_name = b.ReadString();
							a.symbol = b.ReadChar();
							a.color = (Color)b.ReadInt32();
							a.type = (ActorType)b.ReadInt32();
							if(a.type == ActorType.PLAYER){
								game.player = a;
								Actor.player = a;
								Buffer.player = a;
								Item.player = a;
								Map.player = a;
								Event.player = a;
								Tile.player = a;
							}
							a.maxhp = b.ReadInt32();
							a.curhp = b.ReadInt32();
							a.speed = b.ReadInt32();
							a.light_radius = b.ReadInt32();
							int target_ID = b.ReadInt32();
							if(id.ContainsKey(target_ID)){
								a.target = (Actor)id[target_ID];
							}
							else{
								a.target = null;
								need_targets.Add(a);
								missing_target_id[a] = target_ID;
							}
							int num_items = b.ReadInt32();
							for(int j=0;j<num_items;++j){
								Item item = new Item();
								item.name = b.ReadString();
								item.the_name = b.ReadString();
								item.a_name = b.ReadString();
								item.symbol = b.ReadChar();
								item.color = (Color)b.ReadInt32();
								item.type = (ConsumableType)b.ReadInt32();
								item.quantity = b.ReadInt32();
								item.ignored = b.ReadBoolean();
								a.inv.Add(item);
							}
							for(int j=0;j<13;++j){
								a.F[j] = (SpellType)b.ReadInt32();
							}
							int num_attrs = b.ReadInt32();
							for(int j=0;j<num_attrs;++j){
								AttrType t = (AttrType)b.ReadInt32();
								a.attrs[t] = b.ReadInt32();
							}
							int num_skills = b.ReadInt32();
							for(int j=0;j<num_skills;++j){
								SkillType t = (SkillType)b.ReadInt32();
								a.skills[t] = b.ReadInt32();
							}
							int num_feats = b.ReadInt32();
							for(int j=0;j<num_feats;++j){
								FeatType t = (FeatType)b.ReadInt32();
								a.feats[t] = b.ReadBoolean();
							}
							int num_spells = b.ReadInt32();
							for(int j=0;j<num_spells;++j){
								SpellType t = (SpellType)b.ReadInt32();
								a.spells[t] = b.ReadBoolean();
							}
							a.exhaustion = b.ReadInt32();
							a.time_of_last_action = b.ReadInt32();
							a.recover_time = b.ReadInt32();
							int path_count = b.ReadInt32();
							for(int j=0;j<path_count;++j){
								a.path.Add(new pos(b.ReadInt32(),b.ReadInt32()));
							}
							int location_ID = b.ReadInt32();
							if(id.ContainsKey(location_ID)){
								a.target_location = (Tile)id[location_ID];
							}
							else{
								a.target_location = null;
								need_location.Add(a);
								missing_location_id[a] = location_ID;
							}
							a.player_visibility_duration = b.ReadInt32();
							int num_weapons = b.ReadInt32();
							for(int j=0;j<num_weapons;++j){
								//a.weapons.AddLast((WeaponType)b.ReadInt32());
							}
							int num_armors = b.ReadInt32();
							for(int j=0;j<num_armors;++j){
								//a.armors.AddLast((ArmorType)b.ReadInt32());
							}
							int num_magic_trinkets = b.ReadInt32();
							for(int j=0;j<num_magic_trinkets;++j){
								//a.magic_trinkets.AddLast((MagicTrinketType)b.ReadInt32());
							}
						}
						int num_groups = b.ReadInt32();
						for(int i=0;i<num_groups;++i){
							List<Actor> group = new List<Actor>();
							int group_size = b.ReadInt32();
							for(int j=0;j<group_size;++j){
								group.Add((Actor)id[b.ReadInt32()]);
							}
							foreach(Actor a in group){
								a.group = group;
							}
						}
						int num_tiles = b.ReadInt32();
						for(int i=0;i<num_tiles;++i){
							Tile t = new Tile();
							int ID = b.ReadInt32();
							id.Add(ID,t);
							t.row = b.ReadInt32();
							t.col = b.ReadInt32();
							game.M.tile[t.row,t.col] = t;
							t.name = b.ReadString();
							t.the_name = b.ReadString();
							t.a_name = b.ReadString();
							t.symbol = b.ReadChar();
							t.color = (Color)b.ReadInt32();
							t.type = (TileType)b.ReadInt32();
							t.passable = b.ReadBoolean();
							t.opaque = b.ReadBoolean();
							t.seen = b.ReadBoolean();
							t.solid_rock = b.ReadBoolean();
							t.light_value = b.ReadInt32();
							if(b.ReadBoolean()){ //indicates a toggles_into value
								t.toggles_into = (TileType)b.ReadInt32();
							}
							else{
								t.toggles_into = null;
							}
							if(b.ReadBoolean()){ //indicates an item
								t.inv = new Item();
								t.inv.name = b.ReadString();
								t.inv.the_name = b.ReadString();
								t.inv.a_name = b.ReadString();
								t.inv.symbol = b.ReadChar();
								t.inv.color = (Color)b.ReadInt32();
								t.inv.type = (ConsumableType)b.ReadInt32();
								t.inv.quantity = b.ReadInt32();
								t.inv.ignored = b.ReadBoolean();
							}
							else{
								t.inv = null;
							}
							int num_features = b.ReadInt32();
							for(int j=0;j<num_features;++j){
								t.features.Add((FeatureType)b.ReadInt32());
							}
						}
						foreach(Actor a in need_targets){
							if(id.ContainsKey(missing_target_id[a])){
								a.target = (Actor)id[missing_target_id[a]];
							}
							else{
								throw new Exception("Error: some actors weren't loaded(1). ");
							}
						}
						foreach(Actor a in need_location){
							if(id.ContainsKey(missing_location_id[a])){
								a.target_location = (Tile)id[missing_location_id[a]];
							}
							else{
								throw new Exception("Error: some tiles weren't loaded(2). ");
							}
						}
						int game_turn = b.ReadInt32();
						game.Q.turn = -1; //this keeps events from being added incorrectly to the front of the queue while loading. turn is set correctly after events are all loaded.
						int num_tiebreakers = b.ReadInt32();
						Actor.tiebreakers = new List<Actor>(num_tiebreakers);
						for(int i=0;i<num_tiebreakers;++i){
							int tiebreaker_ID = b.ReadInt32();
							if(id.ContainsKey(tiebreaker_ID)){
								Actor.tiebreakers.Add((Actor)id[tiebreaker_ID]);
							}
							else{
								throw new Exception("Error: some actors weren't loaded(3). ");
							}
						}
						int num_events = b.ReadInt32();
						for(int i=0;i<num_events;++i){
							Event e = new Event();
							int target_ID = b.ReadInt32();
							if(id.ContainsKey(target_ID)){
								e.target = id[target_ID];
							}
							else{
								throw new Exception("Error: some tiles/actors weren't loaded(4). ");
							}
							int area_count = b.ReadInt32();
							for(int j=0;j<area_count;++j){
								if(e.area == null){
									e.area = new List<Tile>();
								}
								int tile_ID = b.ReadInt32();
								if(id.ContainsKey(tile_ID)){
									e.area.Add((Tile)id[tile_ID]);
								}
								else{
									throw new Exception("Error: some tiles weren't loaded(5). ");
								}
							}
							e.delay = b.ReadInt32();
							e.type = (EventType)b.ReadInt32();
							e.attr = (AttrType)b.ReadInt32();
							e.value = b.ReadInt32();
							e.msg = b.ReadString();
							int objs_count = b.ReadInt32();
							for(int j=0;j<objs_count;++j){
								if(e.msg_objs == null){
									e.msg_objs = new List<PhysicalObject>();
								}
								int obj_ID = b.ReadInt32();
								if(id.ContainsKey(obj_ID)){
									e.msg_objs.Add(id[obj_ID]);
								}
								else{
									throw new Exception("Error: some actors/tiles weren't loaded(6). ");
								}
							}
							e.time_created = b.ReadInt32();
							e.dead = b.ReadBoolean();
							e.tiebreaker = b.ReadInt32();
							game.Q.Add(e);
						}
						game.Q.turn = game_turn;
						string[] messages = new string[20];
						for(int i=0;i<20;++i){
							messages[i] = b.ReadString();
						}
						game.B.SetPreviousMessages(messages);
						b.Close();
						file.Close();
						File.Delete("forays.sav");
					}
					while(!Global.GAME_OVER){ game.Q.Pop(); }
					Console.CursorVisible = false;
					Global.SaveOptions();
					recentdepth = game.M.current_level;
					recentname = Actor.player_name;
					recentwin = Global.BOSS_KILLED? 'W' : '-';
					recentcause = Global.KILLED_BY;
					if(!Global.SAVING){
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
								if(!added && num_scores < 22){
									char symbol = Global.BOSS_KILLED? 'W' : '-';
									newhighscores.Add(game.M.current_level.ToString() + " " + symbol + " " + Actor.player_name + " -- " + Global.KILLED_BY);
								}
								newhighscores.Add(s);
								break;
							}
							if(num_scores < 22){
								string[] tokens = s.Split(' ');
								int dlev = Convert.ToInt32(tokens[0]);
								if(dlev < game.M.current_level){
									if(!added){
										char symbol = Global.BOSS_KILLED? 'W' : '-';
										newhighscores.Add(game.M.current_level.ToString() + " " + symbol + " " + Actor.player_name + " -- " + Global.KILLED_BY);
										++num_scores;
										added = true;
									}
									if(num_scores < 22){
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
						file.Close();
						StreamWriter fileout = new StreamWriter("highscore.txt",false);
						foreach(string str in newhighscores){
							fileout.WriteLine(str);
						}
						fileout.Close();
					}
					if(!Global.QUITTING && !Global.SAVING){
						game.player.DisplayStats(false);
						if(Global.KILLED_BY != "giving up" && !Help.displayed[TutorialTopic.Consumables]){
							if(game.player.inv.Where(item=>item.type == ConsumableType.HEALING || item.type == ConsumableType.TIME).Count > 0){
								Help.TutorialTip(TutorialTopic.Consumables); //todo! this needs to check for identification, and more consumable types too.
								Global.SaveOptions();
							}
						}
						foreach(Item i in game.player.inv){
							Item.identified[i.type] = true;
						}
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
							int sel = game.player.GetSelection("Would you like to examine your character? ",7,true,false,false);
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
					Help.DisplayHelp();
					break;
				}
				/*case 'c':
				{
					StreamReader file = new StreamReader("highscore.txt");
					Screen.Blank();
					Color primary = Color.Green;
					Color recent = Color.Cyan;
					Screen.WriteString(0,34,new cstr("HIGH SCORES",Color.Yellow));
					Screen.WriteString(1,34,new cstr("-----------",Color.Cyan));
					Screen.WriteString(2,21,new cstr("Character",primary));
					Screen.WriteString(2,49,new cstr("Depth",primary));
					bool written_recent = false;
					string s = "";
					while(s.Length < 2 || s.Substring(0,2) != "--"){
						s = file.ReadLine();
					}
					int line = 3;
					s = "!!";
					while(s.Substring(0,2) != "--"){
						s = file.ReadLine();
						if(s.Substring(0,2) == "--"){
							break;
						}
						if(line > 24){
							continue;
						}
						string[] tokens = s.Split(' ');
						int dlev = Convert.ToInt32(tokens[0]);
						char winning = tokens[1][0];
						string name_and_cause_of_death = s.Substring(tokens[0].Length + 3);
						int idx = name_and_cause_of_death.LastIndexOf(" -- ");
						string name = name_and_cause_of_death.Substring(0,idx);
						string cause_of_death = name_and_cause_of_death.Substring(idx+4);
						if(!written_recent && name == recentname && dlev == recentdepth && winning == recentwin && cause_of_death == recentcause){
							Screen.WriteString(line,18,new cstr(name,recent));
							written_recent = true;
						}
						else{
							Screen.WriteString(line,18,new cstr(name,Color.White));
						}
						Screen.WriteString(line,50,new cstr(dlev.ToString().PadLeft(2),Color.White));
						if(winning == 'W'){
							Screen.WriteString(line,53,new cstr("W",Color.Yellow));
						}
						++line;
					}
					Console.ReadKey(true);
					file.Close();
					break;
				}*/
				case 'c':
				{
					Screen.Blank();
					List<string> scores = new List<string>();
					{
						StreamReader file = new StreamReader("highscore.txt");
						string s = "";
						while(s.Length < 2 || s.Substring(0,2) != "--"){
							s = file.ReadLine();
						}
						s = "!!";
						while(s.Substring(0,2) != "--"){
							s = file.ReadLine();
							if(s.Substring(0,2) == "--"){
								break;
							}
							else{
								scores.Add(s);
							}
						}
						file.Close();
					}
					int longest_name = 0;
					int longest_cause = 0;
					foreach(string s in scores){
						string[] tokens = s.Split(' ');
						string name_and_cause_of_death = s.Substring(tokens[0].Length + 3);
						int idx = name_and_cause_of_death.LastIndexOf(" -- ");
						string name = name_and_cause_of_death.Substring(0,idx);
						string cause_of_death = name_and_cause_of_death.Substring(idx+4);
						if(name.Length > longest_name){
							longest_name = name.Length;
						}
						if(cause_of_death.Length > longest_cause){
							longest_cause = cause_of_death.Length;
						}
					}
					int total_spaces = 76 - (longest_name+longest_cause); //max name length is 26 and max cause length is 42. The other 4 spaces are used for depth.
					int half_spaces = total_spaces / 2;
					int half_spaces_offset = (total_spaces+1) / 2;
					int spaces1 = half_spaces / 4;
					int spaces2 = half_spaces - (half_spaces / 4);
					int spaces3 = half_spaces_offset - (half_spaces_offset / 4);
					//int spaces4 = half_spaces_offset / 4;
					int name_middle = spaces1 + longest_name/2;
					int depth_middle = spaces1 + spaces2 + longest_name + 1;
					int cause_middle = spaces1 + spaces2 + spaces3 + longest_name + 4 + (longest_cause-1)/2;
					Color primary = Color.Green;
					Color recent = Color.Cyan;
					Screen.WriteString(0,34,new cstr("HIGH SCORES",Color.Yellow));
					Screen.WriteString(1,34,new cstr("-----------",Color.Cyan));
					Screen.WriteString(2,name_middle-4,new cstr("Character",primary));
					Screen.WriteString(2,depth_middle-2,new cstr("Depth",primary));
					Screen.WriteString(2,cause_middle-6,new cstr("Cause of death",primary));
					bool written_recent = false;
					int line = 3;
					foreach(string s in scores){
						if(line > 24){
							continue;
						}
						string[] tokens = s.Split(' ');
						int dlev = Convert.ToInt32(tokens[0]);
						char winning = tokens[1][0];
						string name_and_cause_of_death = s.Substring(tokens[0].Length + 3);
						int idx = name_and_cause_of_death.LastIndexOf(" -- ");
						string name = name_and_cause_of_death.Substring(0,idx);
						string cause_of_death = name_and_cause_of_death.Substring(idx+4);
						string cause_capitalized = cause_of_death.Substring(0,1).ToUpper() + cause_of_death.Substring(1);
						Color current_color = Color.White;
						if(!written_recent && name == recentname && dlev == recentdepth && winning == recentwin && cause_of_death == recentcause){
							current_color = recent;
							written_recent = true;
						}
						else{
							current_color = Color.White;
						}
						Screen.WriteString(line,spaces1,new cstr(name,current_color));
						Screen.WriteString(line,spaces1 + spaces2 + longest_name,new cstr(dlev.ToString().PadLeft(2),current_color));
						Screen.WriteString(line,spaces1 + spaces2 + spaces3 + longest_name + 4,new cstr(cause_capitalized,current_color));
						if(winning == 'W'){
							Screen.WriteString(line,spaces1 + spaces2 + longest_name + 3,new cstr("W",Color.Yellow));
						}
						++line;
					}
					Console.ReadKey(true);
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
