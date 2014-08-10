/*Copyright (c) 2011-2014  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using Utilities;
using Forays;
using GLDrawing;
namespace Forays{
	public enum TileType{WALL,FLOOR,DOOR_O,DOOR_C,STAIRS,CHEST,FIREPIT,STALAGMITE,FIRE_TRAP,TELEPORT_TRAP,LIGHT_TRAP,SLIDING_WALL_TRAP,GRENADE_TRAP,SHOCK_TRAP,ALARM_TRAP,DARKNESS_TRAP,POISON_GAS_TRAP,BLINDING_TRAP,ICE_TRAP,PHANTOM_TRAP,SCALDING_OIL_TRAP,FLING_TRAP,STONE_RAIN_TRAP,HIDDEN_DOOR,COMBAT_SHRINE,DEFENSE_SHRINE,MAGIC_SHRINE,SPIRIT_SHRINE,STEALTH_SHRINE,RUINED_SHRINE,SPELL_EXCHANGE_SHRINE,RUBBLE,FIRE_GEYSER,STATUE,POOL_OF_RESTORATION,FOG_VENT,POISON_GAS_VENT,STONE_SLAB,CHASM,BREACHED_WALL,WATER,ICE,CRACKED_WALL,BRUSH,POPPY_FIELD,JUNGLE,GRAVEL,BLAST_FUNGUS,GLOWING_FUNGUS,TOMBSTONE,GRAVE_DIRT,BARREL,STANDING_TORCH,VINE,POISON_BULB,WAX_WALL,DEMONIC_IDOL};
	public enum FeatureType{GRENADE,TROLL_CORPSE,TROLL_BLOODWITCH_CORPSE,FOG,POISON_GAS,SLIME,TELEPORTAL,INACTIVE_TELEPORTAL,STABLE_TELEPORTAL,OIL,FIRE,BONES,WEB,PIXIE_DUST,FORASECT_EGG,SPORES,THICK_DUST,CONFUSION_GAS};
	public enum ActorType{PLAYER,SPECIAL,FIRE_DRAKE,GOBLIN,GIANT_BAT,LONE_WOLF,BLOOD_MOTH,DARKNESS_DWELLER,CARNIVOROUS_BRAMBLE,FROSTLING,SWORDSMAN,DREAM_WARRIOR,SPITTING_COBRA,KOBOLD,SPORE_POD,FORASECT,POLTERGEIST,CULTIST,GOBLIN_ARCHER,GOBLIN_SHAMAN,GOLDEN_DART_FROG,SKELETON,SHADOW,MIMIC,PHASE_SPIDER,ZOMBIE,BERSERKER,GIANT_SLUG,VULGAR_DEMON,BANSHEE,CAVERN_HAG,ROBED_ZEALOT,DIRE_RAT,SKULKING_KILLER,WILD_BOAR,TROLL,DREAM_SPRITE,CLOUD_ELEMENTAL,DERANGED_ASCETIC,ORC_GRENADIER,WARG,ALASI_SCOUT,CARRION_CRAWLER,MECHANICAL_KNIGHT,RUNIC_TRANSCENDENT,ALASI_BATTLEMAGE,ALASI_SOLDIER,SKITTERMOSS,STONE_GOLEM,MUD_ELEMENTAL,FLAMETONGUE_TOAD,ENTRANCER,OGRE_BARBARIAN,SNEAK_THIEF,LASHER_FUNGUS,CRUSADING_KNIGHT,TROLL_BLOODWITCH,LUMINOUS_AVENGER,MARBLE_HORROR,CORROSIVE_OOZE,PYREN_ARCHER,SPELLMUDDLE_PIXIE,ALASI_SENTINEL,NOXIOUS_WORM,CYCLOPEAN_TITAN,VAMPIRE,ORC_WARMAGE,NECROMANCER,STALKING_WEBSTRIDER,ORC_ASSASSIN,CORPSETOWER_BEHEMOTH,MACHINE_OF_WAR,IMPOSSIBLE_NIGHTMARE,DREAM_WARRIOR_CLONE,DREAM_SPRITE_CLONE,MUD_TENTACLE,MARBLE_HORROR_STATUE,GHOST,HOMUNCULUS,BLADE,FINAL_LEVEL_CULTIST,MINOR_DEMON,FROST_DEMON,BEAST_DEMON,DEMON_LORD,PHANTOM,PHANTOM_ZOMBIE,PHANTOM_CRUSADER,PHANTOM_TIGER,PHANTOM_OGRE,PHANTOM_BEHEMOTH,PHANTOM_BLIGHTWING,PHANTOM_SWORDMASTER,PHANTOM_ARCHER,PHANTOM_CONSTRICTOR};
	public enum AttrType{STEALTHY,NONLIVING,PLANTLIKE,MEDIUM_HUMANOID,HUMANOID_INTELLIGENCE,MINDLESS,AGGRESSIVE,TERRITORIAL,AVOIDS_LIGHT,NONEUCLIDEAN_MOVEMENT,KEEN_SENSES,BLINDSIGHT,SMALL,FLYING,DESCENDING,WANDERING,IMMOBILE,CAN_DODGE,SHADOW_CLOAK,INVISIBLE,BRUTISH_STRENGTH,VIGOR,FLYING_LEAP,SILENCED,NOTICED,PLAYER_NOTICED,DANGER_SENSED,SHINING,LOW_LIGHT_VISION,REGENERATING,REGENERATES_FROM_DEATH,REASSEMBLES,NO_ITEM,STUNNED,PARALYZED,BLIND,POISONED,FROZEN,CHILLED,SLIMED,OIL_COVERED,BURNING,SLOWED,BLEEDING,POPPY_COUNTER,MAGICAL_DROWSINESS,ASLEEP,AGGRAVATING,DETECTING_MONSTERS,DETECTING_MOVEMENT,TELEPORTING,VULNERABLE,SUSCEPTIBLE_TO_CRITS,LIGHT_SENSITIVE,DESTROYED_BY_SUNLIGHT,DIM_VISION,DIM_LIGHT,CONFUSED,ENRAGED,POISON_HIT,PARALYSIS_HIT,STALAGMITE_HIT,WORN_OUT_HIT,STUN_HIT,LIFE_DRAIN_HIT,GRAB_HIT,ACID_HIT,BLEED_HIT,ONE_HP_HIT,SILENCE_HIT,FIERY_ARROWS,DULLS_BLADES,IMMUNE_BURNING,IMMUNE_FIRE,IMMUNE_COLD,IMMUNE_ELECTRICITY,MENTAL_IMMUNITY,RESIST_WEAPONS,IMMUNE_ARROWS,RESIST_NECK_SNAP,COMBO_ATTACK,COOLDOWN_1,COOLDOWN_2,COOLDOWN_3,LIFESPAN,RADIANT_HALO,SPORE_BURST,CAN_POISON_WEAPONS,ACIDIFIED,SILENCE_AURA,TERRIFYING,DAMAGE_RESISTANCE,BLOCKING,SHIELDED,MINOR_IMMUNITY,MECHANICAL_SHIELD,TURNS_HERE,TURNS_VISIBLE,RESTING,RUNNING,WAITING,AUTOEXPLORE,TUMBLING,BLOOD_BOILED,SHADOWSIGHT,IN_COMBAT,CONVICTION,KILLSTREAK,EMPOWERED_SPELLS,BANDAGED,PERMANENT_DAMAGE,SWITCHING_ARMOR,JUST_GRABBED,JUST_TELEPORTED,JUST_FLUNG,JUST_BITTEN,JUST_SEARED,AGGRESSION_MESSAGE_PRINTED,NO_PLATE_ARMOR_NOISE,GRAVEL_MESSAGE_COOLDOWN,SILENCE_AURA_MESSAGE_COOLDOWN,LUNGING_AUTO_HIT,TELEKINETICALLY_THROWN,SELF_TK_NO_DAMAGE,BURROWING,ROOTS,DODGED,CHAIN_CAST,IGNORES_QUIET_SOUNDS,ALERTED,SEES_ADJACENT_PLAYER,DIRECTION_OF_PREVIOUS_TILE,FOLLOW_DIRECTION_EXITED,AMNESIA_STUN,GRABBED,GRABBING,BONUS_COMBAT,BONUS_DEFENSE,BONUS_MAGIC,BONUS_SPIRIT,BONUS_STEALTH,INVULNERABLE,SMALL_GROUP,MEDIUM_GROUP,LARGE_GROUP,TURN_INTO_CORPSE,CORPSE,NO_CORPSE_KNOCKBACK,BOSS_MONSTER,NUM_ATTRS,NO_ATTR};
	public enum SpellType{RADIANCE,FORCE_PALM,DETECT_MOVEMENT,FLYING_LEAP,MERCURIAL_SPHERE,GREASE,BLINK,FREEZE,SCORCH,LIGHTNING_BOLT,MAGIC_HAMMER,PORTAL,PASSAGE,AMNESIA,STONE_SPIKES,SHADOWSIGHT,BLIZZARD,COLLAPSE,DOOM,TELEKINESIS,NUM_SPELLS,NO_SPELL};
	public enum SkillType{COMBAT,DEFENSE,MAGIC,SPIRIT,STEALTH,NUM_SKILLS,NO_SKILL};
	public enum FeatType{QUICK_DRAW,WHIRLWIND_STYLE,LUNGE,DRIVE_BACK,ARMOR_MASTERY,CUNNING_DODGE,DEFLECT_ATTACK,TUMBLE,MASTERS_EDGE,ARCANE_INTERFERENCE,CHAIN_CASTING,FORCE_OF_WILL,CONVICTION,ENDURING_SOUL,FEEL_NO_PAIN,BOILING_BLOOD,NECK_SNAP,DISARM_TRAP,CORNER_CLIMB,DANGER_SENSE,NUM_FEATS,NO_FEAT};
	public enum ConsumableType{HEALING,REGENERATION,STONEFORM,VAMPIRISM,BRUTISH_STRENGTH,ROOTS,HASTE,SILENCE,CLOAKING,MYSTIC_MIND,BLINKING,PASSAGE,TIME,KNOWLEDGE,SUNLIGHT,DARKNESS,RENEWAL,CALLING,TRAP_CLEARING,ENCHANTMENT,THUNDERCLAP,FIRE_RING,RAGE,FREEZING,FLAMES,FOG,DETONATION,BREACHING,SHIELDING,TELEPORTAL,PAIN,CONFUSION,BLADES,DUST_STORM,INVISIBILITY,FLESH_TO_FIRE,WEBS,SLUMBER,REACH,TELEKINESIS,BANDAGES,FLINT_AND_STEEL,BLAST_FUNGUS,MAGIC_TRINKET};
	public enum WeaponType{SWORD,MACE,DAGGER,STAFF,BOW,NUM_WEAPONS,NO_WEAPON};
	public enum ArmorType{LEATHER,CHAINMAIL,FULL_PLATE,NUM_ARMORS,NO_ARMOR};
	public enum EnchantmentType{CHILLING,ECHOES,DISRUPTION,PRECISION,VICTORY,NUM_ENCHANTMENTS,NO_ENCHANTMENT};
	public enum EquipmentStatus{DULLED,POSSESSED,HEAVY,MERCIFUL,NEGATED,STUCK,POISONED,INFESTED,RUSTED,DAMAGED,WORN_OUT,WEAK_POINT,LOW_ON_ARROWS,ONE_ARROW_LEFT,OUT_OF_ARROWS,NUM_STATUS};
	public enum MagicTrinketType{PENDANT_OF_LIFE,CLOAK_OF_SAFETY,BRACERS_OF_ARROW_DEFLECTION,CIRCLET_OF_THE_THIRD_EYE,RING_OF_KEEN_SIGHT,RING_OF_THE_LETHARGIC_FLAME,LENS_OF_SCRYING,BELT_OF_WARDING,BOOTS_OF_GRIPPING,NUM_MAGIC_TRINKETS,NO_MAGIC_TRINKET};
	public enum DamageType{NORMAL,FIRE,COLD,ELECTRIC,POISON,MAGIC,NONE};
	public enum DamageClass{PHYSICAL,MAGICAL,NO_TYPE};
	public enum CriticalEffect{STUN,ONE_TURN_STUN,MAX_DAMAGE,PERCENT_DAMAGE,WEAK_POINT,WORN_OUT,REDUCE_ACCURACY,DRAIN_LIFE,GRAB,CHILL,FREEZE,INFLICT_VULNERABILITY,TRIP,KNOCKBACK,STRONG_KNOCKBACK,IGNITE,DIM_VISION,SWAP_POSITIONS,SLIME,MAKE_NOISE,BLIND,SLOW,POISON,PARALYZE,ONE_TURN_PARALYZE,STALAGMITES,FLING,PULL,SILENCE,INFEST,DISRUPTION,VICTORY,ACID,CONFUSE,BLEED,ONE_HP,STEAL,NO_CRIT};
	public enum EventType{ANY_EVENT,MOVE,REMOVE_ATTR,REMOVE_GAS,CHECK_FOR_HIDDEN,RELATIVELY_SAFE,POLTERGEIST,MIMIC,REGENERATING_FROM_DEATH,REASSEMBLING,GRENADE,BLAST_FUNGUS,STALAGMITE,FIRE_GEYSER,FIRE_GEYSER_ERUPTION,FOG_VENT,POISON_GAS_VENT,STONE_SLAB,MARBLE_HORROR,FIRE,NORMAL_LIGHTING,TELEPORTAL,BREACH,GRAVE_DIRT,POPPIES,TOMBSTONE_GHOST,SHIELDING,BURROWING,FINAL_LEVEL_SPAWN_CULTISTS,SPAWN_WANDERING_MONSTER};
	public enum OptionType{NO_WALL_SLIDING,AUTOPICKUP,TOP_ROW_MOVEMENT,CONFIRM_BEFORE_RESTING,NEVER_DISPLAY_TIPS,ALWAYS_RESET_TIPS,DISABLE_GRAPHICS};
	public class Game{
		public Map M;
		public Queue Q;
		public Buffer B;
		public Actor player;
		public static GLGame gl;
		
		static void Main(string[] args){
			{
				int os = (int)Environment.OSVersion.Platform;
				if(os == 4 || os == 6 ||  os == 128){
					Global.LINUX = true;
				}
			}
			//Screen.GLMode = false;
			if(args != null && args.Length > 0){
				if(args[0] == "-c" || args[0] == "--console"){
					Screen.GLMode = false;
				}
				if(args[0] == "-g" || args[0] == "--gl"){
					Screen.GLMode = true;
				}
			}
			if(Screen.GLMode){
				ToolkitOptions.Default.EnableHighResolution = false;
				gl = new GLGame(400,640,25,80,400,640,"Forays into Norrendrin");
				GLGame.text_surface = new SpriteSurface(gl,25,80,16,8,0,0,"font8x16.bmp",1,128,0,0,1.0f,8.0f / 9.0f,
					GLWindow.GetBasicVertexShader(),GLWindow.GetBasicFontFragmentShader(),GLWindow.GetBasicFontVertexAttributeSizes(),
					GLWindow.GetBasicFontDefaultVertexAttributes(),GLWindow.GetBasicFontVertexAttributes());
				GLGame.graphics_surface = new SpriteSurface(gl,22,33,16,16,16*3,8*13,"sprites.png",64,64,17,0,1.0f,1.0f,GLWindow.GetBasicVertexShader(),
					GLWindow.GetBasicGraphicalFragmentShader(),GLWindow.GetBasicGraphicalVertexAttributeSizes(),GLWindow.GetBasicGraphicalDefaultVertexAttributes(),GLWindow.GetBasicGraphicalVertexAttributes());
				gl.SpriteSurfaces.Add(GLGame.graphics_surface);
				GLGame.actors_surface = new SpriteSurface(gl,22,33,16,16,16*3,8*13,"sprites.png",64,64,17,0,1.0f,1.0f,GLWindow.GetBasicVertexShader(),
					GLWindow.GetBasicGraphicalFragmentShader(),GLWindow.GetBasicGraphicalVertexAttributeSizes(),GLWindow.GetBasicGraphicalDefaultVertexAttributes(),GLWindow.GetBasicGraphicalVertexAttributes());
				gl.SpriteSurfaces.Add(GLGame.actors_surface);
				GLGame.visibility_surface = new SpriteSurface(gl,22,33,16,16,16*3,8*13,"visibility.png",1,3,0,0,1.0f,1.0f,GLWindow.GetBasicVertexShader(),
					GLWindow.GetBasicGraphicalFragmentShader(),GLWindow.GetBasicGraphicalVertexAttributeSizes(),GLWindow.GetBasicGraphicalDefaultVertexAttributes(),GLWindow.GetBasicGraphicalVertexAttributes());
				gl.SpriteSurfaces.Add(GLGame.visibility_surface);
				gl.SpriteSurfaces.Add(GLGame.text_surface);
				GLGame.cursor_surface = new SpriteSurface(gl,1,1,2,8,-99,-99,"font6x12.bmp",1,128,0,0,1.0f,8.0f / 9.0f,
					GLWindow.GetBasicVertexShader(),GLWindow.GetBasicFontFragmentShader(),GLWindow.GetBasicFontVertexAttributeSizes(),
					GLWindow.GetBasicFontDefaultVertexAttributes(),GLWindow.GetBasicFontVertexAttributes());
				gl.SpriteSurfaces.Add(GLGame.cursor_surface);
				GLGame.particle_surface = new SpriteSurface(gl,22,33,16,16,16*3,8*13,"animations.png",128,128,0,0,1.0f,1.0f,GLWindow.GetBasicVertexShader(),
					GLGame.GetParticleFragmentShader(),GLWindow.GetBasicFontVertexAttributeSizes(),GLWindow.GetBasicFontDefaultVertexAttributes(),GLWindow.GetBasicFontVertexAttributes());
				gl.SpriteSurfaces.Add(GLGame.particle_surface);
				GLGame.particle_surface.NumElements = 0;
				float r1 = (float)(R.r.NextDouble() * 22);
				float r2 = (float)(R.r.NextDouble() * 22);
				float r3 = (float)(R.r.NextDouble() * 22);
				float c1 = (float)(R.r.NextDouble() * 33);
				float c2 = (float)(R.r.NextDouble() * 33);
				float c3 = (float)(R.r.NextDouble() * 33);
				int s1 = R.Roll(10) + 4;
				int s2 = R.Roll(10) + 4;
				int s3 = R.Roll(10) + 4;
				Animations.Generators.Add(new ParticleGenerator(0,0,5,3,Color4.Magenta,Color4.White,r1,c1,FloatNumber.CreateRange(FloatNumber.CreateDelta(0.0f,0.001f * s1),FloatNumber.CreateDelta(0.1f,0.003f * s1)),FloatNumber.CreateValue(0.0f),FloatNumber.CreateValue(0.0f),Number.CreateValue(3),Number.CreateValue(5),500/s1));
				Animations.Generators.Add(new ParticleGenerator(0,0,5,3,Color4.Yellow,Color4.Firebrick,r2,c2,FloatNumber.CreateRange(FloatNumber.CreateDelta(0.0f,0.001f * s2),FloatNumber.CreateDelta(0.01f,0.003f * s2)),FloatNumber.CreateValue(0.0f),FloatNumber.CreateValue(0.0f),Number.CreateValue(3),Number.CreateValue(5),500/s2));
				Animations.Generators.Add(new ParticleGenerator(0,0,5,3,Color4.Cyan,Color4.Yellow,r3,c3,FloatNumber.CreateRange(FloatNumber.CreateDelta(0.0f,0.001f * s3),FloatNumber.CreateDelta(0.01f,0.003f * s3)),FloatNumber.CreateDelta(0.0f,0.01f),FloatNumber.CreateValue(0.0f),Number.CreateValue(3),Number.CreateValue(5),500/s3));
				Animations.Generators.Add(new ParticleGenerator(0,0,5,3,Color4.Chocolate,Color4.Cyan,r1,c1,FloatNumber.CreateRange(FloatNumber.CreateDelta(0.0f,0.001f * s1),FloatNumber.CreateDelta(0.1f,0.003f * s1)),FloatNumber.CreateValue(0.0f),FloatNumber.CreateValue(0.0f),Number.CreateValue(3),Number.CreateValue(5),500/s1));
				//GLGame.particle_surface.Disabled = true;
				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactorSrc.SrcAlpha,BlendingFactorDest.OneMinusSrcAlpha);
				gl.AllowScaling = false;
				//GLGame.graphics_surface.Disabled = true;
				//gl.ToggleFullScreen(true);
				gl.Visible = true;
				GLGame.Timer = new Stopwatch();
				GLGame.Timer.Start();
			}
			Screen.CursorVisible = false;
			if(!Screen.GLMode){
				if(Global.LINUX){
					Screen.SetCursorPosition(0,0); //todo: this should still work fine but it's worth a verification.
					if(Console.BufferWidth < 80 || Console.BufferHeight < 25){
						Console.Write("Please resize your terminal to 80x25, then press any key.");
						Screen.SetCursorPosition(0,1);
						Console.Write("         Current dimensions are {0}x{1}.".PadRight(57),Console.BufferWidth,Console.BufferHeight);
						Global.ReadKey();
						Screen.SetCursorPosition(0,0);
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
			}
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
			Global.ReadKey();
			MainMenu();
		}
		static void MainMenu(){
			ConsoleKeyInfo command;
			string recentname = "".PadRight(30);
			int recentdepth = -1;
			char recentwin = '-';
			string recentcause = "";
			MouseUI.PushButtonMap();
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
				MouseUI.CreateButton(ConsoleKey.A,false,4+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
				MouseUI.CreateButton(ConsoleKey.B,false,5+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
				MouseUI.CreateButton(ConsoleKey.C,false,6+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
				MouseUI.CreateButton(ConsoleKey.D,false,7+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
				Screen.ResetColors();
				Screen.SetCursorPosition(Global.MAP_OFFSET_COLS,Global.MAP_OFFSET_ROWS+8);
				command = Global.ReadKey();
				switch(command.KeyChar){
				case 'a':
				{
					Global.GAME_OVER = false;
					Global.BOSS_KILLED = false;
					Global.SAVING = false;
					Global.LoadOptions();
					Game game = new Game();
					Actor.attack[ActorType.PLAYER] = new List<AttackInfo>{new AttackInfo(100,2,CriticalEffect.NO_CRIT,"& hit *","& miss *")};
					if(!saved_game){
						game.player = new Actor(ActorType.PLAYER,"you",'@',Color.White,100,100,0,0,AttrType.HUMANOID_INTELLIGENCE);
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
					Fire.fire_event = null;
					Fire.burning_objects = new List<PhysicalObject>();
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
							MouseUI.PushButtonMap(MouseMode.NameEntry);
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
								Screen.SetCursorPosition(Global.MAP_OFFSET_COLS + 12 + s.Length,Global.MAP_OFFSET_ROWS + 4);
								MouseUI.CreateButton(ConsoleKey.Enter,false,6+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.Tab,false,20+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.F1,false,15+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.F2,false,16+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.F3,false,17+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								MouseUI.CreateButton(ConsoleKey.F4,false,18+Global.MAP_OFFSET_ROWS,0,1,Global.SCREEN_W);
								Screen.CursorVisible = true;
								command = Global.ReadKey();
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
												else{
													switch(command.Key){
													case ConsoleKey.F1:
														name_option = 0;
														break;
													case ConsoleKey.F2:
														name_option = 1;
														break;
													case ConsoleKey.F3:
														name_option = 2;
														break;
													case ConsoleKey.F4:
														name_option = 3;
														break;
													}
												}
											}
										}
									}
								}
							}
							MouseUI.PopButtonMap();
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
						{
							Event e = new Event(game.player,0,EventType.MOVE);
							e.tiebreaker = 0;
							game.Q.Add(e);
						}
						Item.GenerateUnIDedNames();
						game.M.GenerateLevelTypes();
						game.M.GenerateLevel();
						game.player.UpdateRadius(0,6,true);
						Item.Create(ConsumableType.BANDAGES,game.player).other_data = 5;
						Item.Create(ConsumableType.FLINT_AND_STEEL,game.player).other_data = 3;
						game.player.inv[0].revealed_by_light = true;
						game.player.inv[1].revealed_by_light = true;
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
						for(int i=0;i<Global.ROWS;++i){
							for(int j=0;j<Global.COLS;++j){
								game.M.last_seen[i,j].c = b.ReadChar();
								game.M.last_seen[i,j].color = (Color)b.ReadInt32();
								game.M.last_seen[i,j].bgcolor = (Color)b.ReadInt32();
							}
						}
						if(game.M.current_level == 21){
							game.M.final_level_cultist_count = new int[5];
							for(int i=0;i<5;++i){
								game.M.final_level_cultist_count[i] = b.ReadInt32();
							}
							game.M.final_level_demon_count = b.ReadInt32();
							game.M.final_level_clock = b.ReadInt32();
						}
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
							a.maxmp = b.ReadInt32();
							a.curmp = b.ReadInt32();
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
								item.light_radius = b.ReadInt32();
								item.type = (ConsumableType)b.ReadInt32();
								item.quantity = b.ReadInt32();
								item.other_data = b.ReadInt32();
								item.ignored = b.ReadBoolean();
								item.do_not_stack = b.ReadBoolean();
								item.revealed_by_light = b.ReadBoolean();
								a.inv.Add(item);
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
								int path_row = b.ReadInt32();
								int path_col = b.ReadInt32();
								a.path.Add(new pos(path_row,path_col));
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
								Weapon w = new Weapon(WeaponType.NO_WEAPON);
								w.type = (WeaponType)b.ReadInt32();
								w.enchantment = (EnchantmentType)b.ReadInt32();
								int num_statuses = b.ReadInt32();
								for(int k=0;k<num_statuses;++k){
									EquipmentStatus st = (EquipmentStatus)b.ReadInt32();
									bool has_st = b.ReadBoolean();
									w.status[st] = has_st;
								}
								a.weapons.AddLast(w);
							}
							int num_armors = b.ReadInt32();
							for(int j=0;j<num_armors;++j){
								Armor ar = new Armor(ArmorType.NO_ARMOR);
								ar.type = (ArmorType)b.ReadInt32();
								ar.enchantment = (EnchantmentType)b.ReadInt32();
								int num_statuses = b.ReadInt32();
								for(int k=0;k<num_statuses;++k){
									EquipmentStatus st = (EquipmentStatus)b.ReadInt32();
									bool has_st = b.ReadBoolean();
									ar.status[st] = has_st;
								}
								a.armors.AddLast(ar);
							}
							int num_magic_trinkets = b.ReadInt32();
							for(int j=0;j<num_magic_trinkets;++j){
								a.magic_trinkets.Add((MagicTrinketType)b.ReadInt32());
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
							t.light_radius = b.ReadInt32();
							t.type = (TileType)b.ReadInt32();
							t.passable = b.ReadBoolean();
							t.SetInternalOpacity(b.ReadBoolean());
							t.seen = b.ReadBoolean();
							t.revealed_by_light = b.ReadBoolean();
							t.solid_rock = b.ReadBoolean();
							t.light_value = b.ReadInt32();
							t.direction_exited = b.ReadInt32();
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
								t.inv.light_radius = b.ReadInt32();
								t.inv.type = (ConsumableType)b.ReadInt32();
								t.inv.quantity = b.ReadInt32();
								t.inv.other_data = b.ReadInt32();
								t.inv.ignored = b.ReadBoolean();
								t.inv.do_not_stack = b.ReadBoolean();
								t.inv.revealed_by_light = b.ReadBoolean();
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
							if(e.type == EventType.FIRE && !e.dead){
								Fire.fire_event = e;
							}
						}
						game.Q.turn = game_turn;
						int num_footsteps = b.ReadInt32();
						for(int i=0;i<num_footsteps;++i){
							int step_row = b.ReadInt32();
							int step_col = b.ReadInt32();
							Actor.footsteps.Add(new pos(step_row,step_col));
						}
						int num_prev_footsteps = b.ReadInt32();
						for(int i=0;i<num_prev_footsteps;++i){
							int step_row = b.ReadInt32();
							int step_col = b.ReadInt32();
							Actor.previous_footsteps.Add(new pos(step_row,step_col));
						}
						Actor.interrupted_path.row = b.ReadInt32();
						Actor.interrupted_path.col = b.ReadInt32();
						int num_unIDed = b.ReadInt32();
						for(int i=0;i<num_unIDed;++i){
							ConsumableType ct = (ConsumableType)b.ReadInt32();
							string s = b.ReadString();
							Item.unIDed_name[ct] = s;
						}
						int num_IDed = b.ReadInt32();
						for(int i=0;i<num_IDed;++i){
							ConsumableType ct = (ConsumableType)b.ReadInt32();
							bool IDed = b.ReadBoolean();
							Item.identified[ct] = IDed;
						}
						int num_item_colors = b.ReadInt32();
						for(int i=0;i<num_item_colors;++i){
							ConsumableType ct = (ConsumableType)b.ReadInt32();
							Item.proto[ct].color = (Color)b.ReadInt32();
						}
						int num_burning = b.ReadInt32();
						for(int i=0;i<num_burning;++i){
							int obj_ID = b.ReadInt32();
							if(id.ContainsKey(obj_ID)){
								Fire.burning_objects.Add(id[obj_ID]);
							}
							else{
								throw new Exception("Error: some actors/tiles weren't loaded(7). ");
							}
						}
						string[] messages = new string[20];
						for(int i=0;i<20;++i){
							messages[i] = b.ReadString();
						}
						game.B.SetPreviousMessages(messages);
						b.Close();
						file.Close();
						File.Delete("forays.sav");
						Tile.Feature(FeatureType.TELEPORTAL).color = Item.Prototype(ConsumableType.TELEPORTAL).color;
						game.M.UpdateSafetyMap(game.player);
						game.M.poppy_distance_map = game.M.tile.GetDijkstraMap(x=>!game.M.tile[x].Is(TileType.POPPY_FIELD),x=>game.M.tile[x].passable && !game.M.tile[x].Is(TileType.POPPY_FIELD));
					}
					Game.gl.NoClose = true;
					MouseUI.PushButtonMap(MouseMode.Map);
					MouseUI.CreateStatsButton(ConsoleKey.I,false,12,1);
					MouseUI.CreateStatsButton(ConsoleKey.E,false,13,1);
					MouseUI.CreateStatsButton(ConsoleKey.C,false,14,1);
					MouseUI.CreateStatsButton(ConsoleKey.T,false,15,1);
					MouseUI.CreateStatsButton(ConsoleKey.Tab,false,16,1);
					MouseUI.CreateStatsButton(ConsoleKey.R,false,17,1);
					MouseUI.CreateStatsButton(ConsoleKey.A,false,18,1);
					MouseUI.CreateStatsButton(ConsoleKey.G,false,19,1);
					MouseUI.CreateStatsButton(ConsoleKey.F,false,20,1);
					MouseUI.CreateStatsButton(ConsoleKey.S,false,21,1);
					MouseUI.CreateStatsButton(ConsoleKey.Z,false,22,1);
					MouseUI.CreateStatsButton(ConsoleKey.X,false,23,1);
					MouseUI.CreateStatsButton(ConsoleKey.V,false,24,1);
					MouseUI.CreateStatsButton(ConsoleKey.E,false,7,2);
					MouseUI.CreateMapButton(ConsoleKey.P,false,0,3);
					try{
						while(!Global.GAME_OVER){ game.Q.Pop(); }
					}
					catch(Exception e){
						StreamWriter fileout = new StreamWriter("error.txt",false);
						fileout.WriteLine(e.Message);
						fileout.WriteLine(e.StackTrace);
						fileout.Close();
						MouseUI.IgnoreMouseMovement = true;
						Screen.CursorVisible = false;
						Screen.Blank();
						Screen.WriteString(11,3,"An error has occured. See error.txt for more details. Press any key to quit. ");
						Global.ReadKey();
						Global.Quit();
					}
					MouseUI.PopButtonMap();
					MouseUI.IgnoreMouseMovement = false;
					Game.gl.NoClose = false;
					Screen.CursorVisible = false;
					Global.SaveOptions();
					recentdepth = game.M.current_level;
					recentname = Actor.player_name;
					recentwin = Global.BOSS_KILLED? 'W' : '-';
					recentcause = Global.KILLED_BY;
					if(!Global.SAVING){
						List<string> newhighscores = new List<string>();
						int num_scores = 0;
						bool added = false;
						if(File.Exists("highscore.txt")){
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
									if(dlev < game.M.current_level || (dlev == game.M.current_level && Global.BOSS_KILLED)){
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
						}
						else{
							newhighscores.Add("High scores:");
							newhighscores.Add("--");
							char symbol = Global.BOSS_KILLED? 'W' : '-';
							newhighscores.Add(game.M.current_level.ToString() + " " + symbol + " " + Actor.player_name + " -- " + Global.KILLED_BY);
							newhighscores.Add("--");
						}
						StreamWriter fileout = new StreamWriter("highscore.txt",false);
						foreach(string str in newhighscores){
							fileout.WriteLine(str);
						}
						fileout.Close();
					}
					if(!Global.QUITTING && !Global.SAVING){
						GameOverScreen(game);
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
					Global.ReadKey();
					file.Close();
					break;
				}*/
				case 'c':
				{
					MouseUI.PushButtonMap();
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
					Global.ReadKey();
					MouseUI.PopButtonMap();
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
		static void GameOverScreen(Game game){
			MouseUI.PushButtonMap();
			game.player.attrs[AttrType.BLIND] = 0; //make sure the player can actually view the map
			game.player.attrs[AttrType.BURNING] = 0;
			game.player.attrs[AttrType.FROZEN] = 0; //...without borders
			//game.M.Draw();
			colorchar[,] mem = null;
			game.player.DisplayStats(false);
			bool showed_IDed_tip = false;
			if(Global.KILLED_BY != "giving up" && !Help.displayed[TutorialTopic.IdentifiedConsumables]){
				if(game.player.inv.Where(item=>Item.identified[item.type] && item.Is(ConsumableType.HEALING,ConsumableType.TIME,ConsumableType.TELEPORTAL)).Count > 0){
					Help.TutorialTip(TutorialTopic.IdentifiedConsumables);
					Global.SaveOptions();
					showed_IDed_tip = true;
				}
			}
			if(!showed_IDed_tip && Global.KILLED_BY != "giving up" && !Help.displayed[TutorialTopic.UnidentifiedConsumables]){
				int known_count = 0;
				foreach(ConsumableType ct in Item.identified.d.Keys){
					if(Item.identified[ct] && Item.NameOfItemType(ct) != "other"){
						++known_count;
					}
				}
				if(known_count < 2 && game.player.inv.Where(item=>!Item.identified[item.type]).Count > 2){
					Help.TutorialTip(TutorialTopic.UnidentifiedConsumables);
					Global.SaveOptions();
				}
			}
			Dict<ConsumableType,bool> known_items = new Dict<ConsumableType,bool>();
			foreach(Item i in game.player.inv){
				if(i.NameOfItemType() != "other"){
					if(!Item.identified[i.type]){
						if(!Item.unIDed_name[i.type].Contains("{tried}")){
							Item.unIDed_name[i.type] = Item.unIDed_name[i.type] + " {untried}";
						}
						Item.identified[i.type] = true;
					}
					else{
						known_items[i.type] = true;
					}
					if(i.NameOfItemType() == "wand"){
						i.other_data = -1;
					}
					if(Item.unIDed_name[i.type].Contains("{tried}")){
						i.SetName(i.name + " {tried}");
					}
					else{
						if(Item.unIDed_name[i.type].Contains("{untried}")){
							i.SetName(i.name + " {untried}");
						}
					}
				}
			}
			List<string> ls = new List<string>();
			ls.Add("See the map");
			ls.Add("See last messages");
			ls.Add("Examine your equipment");
			ls.Add("Examine your inventory");
			ls.Add("View known item types");
			ls.Add("See character info");
			ls.Add("Write this information to a file");
			ls.Add("Done");
			for(bool done=false;!done;){
				if(mem != null){
					Screen.MapDrawWithStrings(mem,0,0,Global.ROWS,Global.COLS);
				}
				game.player.Select("Would you like to examine your character! ","".PadRight(Global.COLS),"".PadRight(Global.COLS),ls,true,false,false);
				int sel = game.player.GetSelection("Would you like to examine your character? ",ls.Count,true,false,false);
				mem = Screen.GetCurrentMap();
				switch(sel){
				case 0:
					MouseUI.PushButtonMap();
					Dictionary<Actor,colorchar> old_ch = new Dictionary<Actor,colorchar>();
					List<Actor> drawn = new List<Actor>();
					foreach(Actor a in game.M.AllActors()){
						if(game.player.CanSee(a)){
							old_ch.Add(a,game.M.last_seen[a.row,a.col]);
							game.M.last_seen[a.row,a.col] = new colorchar(a.symbol,a.color);
							drawn.Add(a);
						}
					}
					Screen.MapDrawWithStrings(game.M.last_seen,0,0,Global.ROWS,Global.COLS);
					game.player.GetTarget(true,-1,-1,true,false,false,"");
					//game.B.DisplayNow("Press any key to continue. ");
					//Screen.CursorVisible = true;
					//Global.ReadKey();
					MouseUI.PopButtonMap();
					foreach(Actor a in drawn){
						game.M.last_seen[a.row,a.col] = old_ch[a];
					}
					game.M.Redraw();
					/*foreach(Tile t in game.M.AllTiles()){
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
					Screen.CursorVisible = true;
					Screen.WriteMapChar(0,0,'-');
					game.M.Draw();
					Global.ReadKey();*/
					break;
				case 1:
				{
					MouseUI.PushButtonMap();
					Screen.WriteMapString(0,0,"".PadRight(Global.COLS,'-'));
					int i = 1;
					foreach(string s in game.B.GetMessages()){
						Screen.WriteMapString(i,0,s.PadRight(Global.COLS));
						++i;
					}
					Screen.WriteMapString(21,0,"".PadRight(Global.COLS,'-'));
					game.B.DisplayNow("Previous messages: ");
					Screen.CursorVisible = true;
					Global.ReadKey();
					MouseUI.PopButtonMap();
					break;
				}
				case 2:
					game.player.DisplayEquipment();
					break;
				case 3:
					MouseUI.PushButtonMap();
					MouseUI.AutomaticButtonsFromStrings = true;
					for(int i=1;i<9;++i){
						Screen.WriteMapString(i,0,"".PadRight(Global.COLS));
					}
					MouseUI.AutomaticButtonsFromStrings = false;
					game.player.Select("In your pack: ",game.player.InventoryList(),true,false,false);
					Global.ReadKey();
					MouseUI.PopButtonMap();
					break;
				case 4:
				{
					int ROWS = Global.ROWS;
					int COLS = Global.COLS;
					MouseUI.PushButtonMap();
					List<colorstring> potions = new List<colorstring>();
					List<colorstring> scrolls = new List<colorstring>();
					List<colorstring> orbs = new List<colorstring>();
					foreach(ConsumableType ct in Enum.GetValues(typeof(ConsumableType))){
						string type_name = "    " + ct.ToString()[0] + ct.ToString().Substring(1).ToLower();
						type_name = type_name.Replace('_',' ');
						Color ided_color = Color.Cyan;
						if(Item.NameOfItemType(ct) == "potion"){
							if(known_items[ct]){
								potions.Add(new colorstring(type_name,ided_color));
				            }
				            else{
								potions.Add(new colorstring(type_name,Color.DarkGray));
							}
						}
						else{
							if(Item.NameOfItemType(ct) == "scroll"){
								if(known_items[ct]){
									scrolls.Add(new colorstring(type_name,ided_color));
					            }
					            else{
									scrolls.Add(new colorstring(type_name,Color.DarkGray));
								}
							}
							else{
								if(Item.NameOfItemType(ct) == "orb"){
									if(known_items[ct]){
										orbs.Add(new colorstring(type_name,ided_color));
						            }
						            else{
										orbs.Add(new colorstring(type_name,Color.DarkGray));
									}
								}
							}
						}
					}
					Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
					for(int i=1;i<ROWS-1;++i){
						Screen.WriteMapString(i,0,"".PadToMapSize());
					}
					Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
					Color label_color = Color.Yellow;
					Screen.WriteMapString(1,0,"  - Potions -",label_color);
					Screen.WriteMapString(1,33,"  - Scrolls -",label_color);
					int line = 2;
					foreach(colorstring s in potions){
						Screen.WriteMapString(line,0,s);
						++line;
					}
					line = 2;
					foreach(colorstring s in scrolls){
						Screen.WriteMapString(line,33,s);
						++line;
					}
					Screen.WriteMapString(12,0,"  - Orbs -",label_color);
					line = 13;
					foreach(colorstring s in orbs){
						Screen.WriteMapString(line,0,s);
						++line;
					}
					game.B.DisplayNow("Discovered item types: ");
					Screen.CursorVisible = true;
					Global.ReadKey();
					MouseUI.PopButtonMap();
					break;
				}
				case 5:
					game.player.DisplayCharacterInfo();
					break;
				case 6:
				{
					game.B.DisplayNow("Enter file name: ");
					Screen.CursorVisible = true;
					MouseUI.PushButtonMap();
					string filename = Global.EnterString(40);
					MouseUI.PopButtonMap();
					if(filename == ""){
						break;
					}
					if(!filename.Contains(".")){
						filename = filename + ".txt";
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
					if(game.player.InventoryList().Count == 0){
						file.WriteLine("(nothing)");
					}
					file.WriteLine();
					file.WriteLine("Known items: ");
					bool known_items_found = false;
					foreach(ConsumableType ct in known_items.d.Keys){
						if(known_items[ct] && (Item.NameOfItemType(ct) == "potion" || Item.NameOfItemType(ct) == "scroll" || Item.NameOfItemType(ct) == "orb")){
							file.WriteLine(Item.Prototype(ct).Name(false));
							known_items_found = true;
						}
					}
					if(!known_items_found){
						file.WriteLine("(none)");
					}
					else{
						file.WriteLine();
					}
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
					Screen.WriteMapChar(0,0,'-'); //todo: this was a hack. can now be replaced with the proper Redraw method, I think.
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
				case 7:
					done = true;
					break;
				default:
					break;
				}
			}
			MouseUI.PopButtonMap();
		}
	}
}
