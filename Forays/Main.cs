/*Copyright (c) 2011  Derrick Creamer
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.*/
using System;
using System.Collections.Generic;
using Forays;
namespace Forays{
	public enum TileType{WALL,FLOOR,DOOR_O,DOOR_C,STAIRS,CHEST,FIREPIT,STALAGMITE,GRENADE,QUICKFIRE,QUICKFIRE_TRAP,TELEPORT_TRAP,LIGHT_TRAP,UNDEAD_TRAP,GRENADE_TRAP,STUN_TRAP,HIDDEN_DOOR};
	public enum ActorType{PLAYER,RAT,FIRE_DRAKE,GOBLIN,LARGE_BAT,SHAMBLING_SCARECROW,SKELETON,CULTIST,POLTERGEIST,ZOMBIE,WOLF,FROSTLING,GOBLIN_ARCHER,GOBLIN_SHAMAN,SWORDSMAN,DIRE_RAT,DREAM_WARRIOR,BANSHEE,WARG,ROBED_ZEALOT,SKULKING_KILLER,CARRION_CRAWLER,OGRE,SHADOW,BERSERKER,ORC_GRENADIER,PHASE_SPIDER,STONE_GOLEM,NECROMANCER,TROLL,ORC_WARMAGE,LASHER_FUNGUS,CORPSETOWER_BEHEMOTH,DREAM_CLONE};
	public enum AttrType{STEALTHY,UNDEAD,CONSTRUCT,PLANTLIKE,MEDIUM_HUMANOID,HUMANOID_INTELLIGENCE,ENHANCED_TORCH,MAGICAL_BLOOD,KEEN_EYES,TOUGH,LONG_STRIDE,RUNIC_BIRTHMARK,LOW_LIGHT_VISION,DARKVISION,REGENERATING,REGENERATES_FROM_DEATH,STUNNED,PARALYZED,POISONED,IMMOBILIZED,ON_FIRE,CATCHING_FIRE,STARTED_CATCHING_FIRE_THIS_TURN,AFRAID,SLOWED,DETECTING_MONSTERS,TELEPORTING,DIM_VISION,DIM_LIGHT,FIRE_HIT,COLD_HIT,POISON_HIT,PARALYSIS_HIT,FORCE_HIT,DIM_VISION_HIT,STALAGMITE_HIT,RESIST_SLASH,RESIST_PIERCE,RESIST_BASH,RESIST_FIRE,RESIST_COLD,RESIST_ELECTRICITY,IMMUNE_FIRE,IMMUNE_COLD,IMMUNE_ARROWS,GLOBAL_FAIL_RATE,COOLDOWN_1,COOLDOWN_2,BLESSED,HOLY_SHIELDED,SPORE_BURST,TURNS_VISIBLE,RESTING,RUNNING,STUDENTS_LUCK_USED,DEFENSIVE_STANCE,DRIVE_BACK_ON,TUMBLING,DANGER_SENSE_ON,WAR_SHOUTED,BONUS_COMBAT,BONUS_DEFENSE,BONUS_MAGIC,BONUS_SPIRIT,BONUS_STEALTH,INVULNERABLE,SMALL_GROUP,MEDIUM_GROUP,LARGE_GROUP,BOSS_MONSTER,NUM_ATTRS,NO_ATTR};
	public enum SpellType{SHINE,MAGIC_MISSILE,DETECT_MONSTERS,FORCE_PALM,BLINK,IMMOLATE,ICY_BLAST,BURNING_HANDS,FREEZE,SONIC_BOOM,ARC_LIGHTNING,PLACEHOLDER,SHOCK,SHADOWSIGHT,RETREAT,FIREBALL,PASSAGE,FORCE_BEAM,DISINTEGRATE,BLIZZARD,BLESS,MINOR_HEAL,HOLY_SHIELD,NUM_SPELLS,NO_SPELL};
	public enum SkillType{COMBAT,DEFENSE,MAGIC,SPIRIT,STEALTH,NUM_SKILLS,NO_SKILL};
	public enum FeatType{QUICK_DRAW,SPIN_ATTACK,LUNGE,DRIVE_BACK,SILENT_CHAINMAIL,ARMORED_MAGE,FULL_DEFENSE,TUMBLE,MASTERS_EDGE,STUDENTS_LUCK,ARCANE_HEALING,FORCE_OF_WILL,WAR_SHOUT,ENDURING_SOUL,FEEL_NO_PAIN,FOCUSED_RAGE,CORNER_LOOK,DISARM_TRAP,NECK_SNAP,DANGER_SENSE,NUM_FEATS,NO_FEAT};
	public enum ConsumableType{HEALING,REGENERATION,CURE_POISON,RESISTANCE,CLARITY,PHASING,TELEPORTATION,PASSAGE,DETECT_MONSTERS,MAGIC_MAP,WIZARDS_LIGHT,PRISMATIC_ORB,BANDAGE};
	public enum WeaponType{SWORD,MACE,DAGGER,STAFF,BOW,FLAMEBRAND,MACE_OF_FORCE,VENOMOUS_DAGGER,STAFF_OF_MAGIC,HOLY_LONGBOW,NUM_WEAPONS,NO_WEAPON};
	public enum ArmorType{LEATHER,CHAINMAIL,FULL_PLATE,ELVEN_LEATHER,CHAINMAIL_OF_ARCANA,FULL_PLATE_OF_RESISTANCE,NUM_ARMORS,NO_ARMOR};
	public enum MagicItemType{PENDANT_OF_LIFE,RING_OF_RESISTANCE,RING_OF_PROTECTION,CLOAK_OF_DISAPPEARANCE,NUM_MAGIC_ITEMS,NO_MAGIC_ITEM};
	public enum DamageType{NORMAL,FIRE,COLD,ELECTRIC,POISON,HEAL,SLASHING,BASHING,PIERCING,MAGIC,NONE};
	public enum DamageClass{PHYSICAL,MAGICAL,NO_TYPE};
	public enum EventType{ANY_EVENT,MOVE,REMOVE_ATTR,CHECK_FOR_HIDDEN,POLTERGEIST,REGENERATING_FROM_DEATH,GRENADE,STALAGMITE,QUICKFIRE,BOSS_ARRIVE};
	public enum OptionType{LAST_TARGET,VI_KEYS,OPEN_CHESTS,ITEMS_AND_TILES_ARE_INTERESTING,WIZLIGHT_CAST};
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
			if(Global.LINUX){
				Screen.Blank();
			}
			else{
				Console.BufferHeight = Global.SCREEN_H; //25
			}
			Console.TreatControlCAsInput = true;
			//Console.CursorSize = 100;
		//	while(true){
		//		MainMenu();
		//	}
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
			Actor.player_name = "Doomguy";
			game.player.attrs[AttrType.LONG_STRIDE]++;
			game.M.LoadLevel("map.txt");
			game.player.Q0();
			game.player.Move(10,20,false); //this is why the voodoo was needed before: the player must be moved onto the map *before*
			game.player.UpdateRadius(0,6,true); //gaining a light radius.
			game.player.GainXP(1);
			while(!Global.GAME_OVER){ game.Q.Pop(); }
		}
		static void MainMenu(){
			Screen.Blank();
			Screen.WriteMapString(3,0,"[a] Start a new game ");
			Screen.WriteMapString(4,0,"[b] Quit ");
			ConsoleKeyInfo command;
			bool done = false;
			while(!done){
				command = Console.ReadKey(true);
				switch(command.KeyChar){
				case 'a':
				{
					done = true;
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
					Tile.player = game.player;
					//game.M.InitLevel();
					game.M.LoadLevel("map.txt");
					Actor.player_name = "Doomguy"; //todo
					game.player.Q0();
					game.player.Move(10,20,false); //this is why the voodoo was needed before: the player must be moved onto the map *before*
					game.player.UpdateRadius(0,6,true); //gaining a light radius.
					game.player.GainXP(1);
					while(!Global.GAME_OVER){ game.Q.Pop(); }
					break;
				}
				case 'b':
					Environment.Exit(0);
					break;
				default:
					break;
				}
			}
		}
	}
}
