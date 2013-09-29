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
using System.Threading;
using System.IO;
using Utilities;
namespace Forays{
	public class AttackInfo{
		public int cost;
		public Damage damage;
		public CriticalEffect crit;
		public string hit;
		public string miss;
		public AttackInfo(int cost_,int dice_,CriticalEffect crit_,string hit_){
			cost=cost_;
			damage.dice=dice_;
			damage.type = DamageType.NORMAL;
			damage.damclass=DamageClass.PHYSICAL;
			crit = crit_;
			hit = hit_;
			miss = "";
		}
		public AttackInfo(int cost_,int dice_,CriticalEffect crit_,string hit_,string miss_){
			cost=cost_;
			damage.dice=dice_;
			damage.type = DamageType.NORMAL;
			damage.damclass=DamageClass.PHYSICAL;
			crit = crit_;
			hit = hit_;
			miss = miss_;
		}
		public AttackInfo(AttackInfo a){
			cost = a.cost;
			damage = a.damage;
			crit = a.crit;
			hit = a.hit;
			miss = a.miss;
		}
	}
	public struct Damage{
		public int amount{ //amount isn't determined until you ask for it
			get{
				if(!num.HasValue){
					num = R.Roll(dice,6);
				}
				return num.Value;
			}
			set{
				num = value;
			}
		}
		private int? num;
		public int dice;
		public DamageType type;
		public DamageClass damclass;
		public Actor source;
		public WeaponType weapon_used;
		public Damage(int dice_,DamageType type_,DamageClass damclass_,Actor source_){
			dice=dice_;
			num = null;
			type=type_;
			damclass=damclass_;
			source=source_;
			weapon_used = WeaponType.NO_WEAPON;
		}
		public Damage(DamageType type_,DamageClass damclass_,Actor source_,int totaldamage){
			dice=0;
			num=totaldamage;
			type=type_;
			damclass=damclass_;
			source=source_;
			weapon_used = WeaponType.NO_WEAPON;
		}
	}
	public class Actor : PhysicalObject{
		public ActorType type{get;set;}
		public int maxhp{get;set;}
		public int curhp{get;set;}
		public int maxmp{get;set;}
		public int curmp{get;set;}
		public int speed{get;set;}
		//public int light_radius{get;set;} //inherited
		public Actor target{get;set;}
		public List<Item> inv{get;set;}
		public SpellType[] F{get;set;} //F[0] is the 'autospell' you cast instead of attacking, if that option is set
		public Dict<AttrType,int> attrs = new Dict<AttrType,int>();
		public Dict<SkillType,int> skills = new Dict<SkillType,int>();
		public Dict<FeatType,bool> feats = new Dict<FeatType,bool>();
		public Dict<SpellType,bool> spells = new Dict<SpellType,bool>();
		public int exhaustion;
		public int time_of_last_action;
		public int recover_time;
		public List<pos> path = new List<pos>();
		public Tile target_location;
		public int player_visibility_duration;
		public List<Actor> group = null;
		public LinkedList<Weapon> weapons = new LinkedList<Weapon>();
		public LinkedList<Armor> armors = new LinkedList<Armor>();
		public List<MagicTrinketType> magic_trinkets = new List<MagicTrinketType>();

		public static string player_name;
		public static List<FeatType> feats_in_order = null;
		public static List<SpellType> spells_in_order = null; //used only for keeping track of the order in which feats/spells were learned by the player
		public static List<pos> footsteps = new List<pos>();
		public static List<pos> previous_footsteps = new List<pos>();
		public static List<Actor> tiebreakers = null; //a list of all actors on this level. used to determine sub-turn order of events
		public static Dict<ActorType,List<AttackInfo>> attack = new Dict<ActorType,List<AttackInfo>>();
		private static Dict<ActorType,Actor> proto = new Dict<ActorType, Actor>();
		public static Actor Prototype(ActorType type){ return proto[type]; }
		private const int ROWS = Global.ROWS;
		private const int COLS = Global.COLS;
		//public static Map M{get;set;} //inherited
		//public static Buffer B{get;set;}
		//public static Queue Q{get;set;}
		//public static Actor player{get;set;}
		static Actor(){
			Define(ActorType.RAT,"rat",'r',Color.DarkGray,15,90,0,AttrType.LOW_LIGHT_VISION,AttrType.SMALL,AttrType.KEEN_SENSES);
			DefineAttack(ActorType.RAT,100,1,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.GOBLIN,"goblin",'g',Color.Green,20,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.GOBLIN,100,2,CriticalEffect.STUN,"& hits *");

			Define(ActorType.GIANT_BAT,"giant bat",'b',Color.DarkGray,15,50,0,AttrType.FLYING,AttrType.SMALL,AttrType.KEEN_SENSES,AttrType.BLINDSIGHT);
			DefineAttack(ActorType.GIANT_BAT,100,1,CriticalEffect.NO_CRIT,"& bites *");
			DefineAttack(ActorType.GIANT_BAT,100,1,CriticalEffect.NO_CRIT,"& scratches *");

			Define(ActorType.LONE_WOLF,"lone wolf",'c',Color.DarkYellow,20,50,0,AttrType.LOW_LIGHT_VISION,AttrType.KEEN_SENSES);
			DefineAttack(ActorType.LONE_WOLF,100,2,CriticalEffect.MAX_DAMAGE,"& bites *");

			Define(ActorType.BLOOD_MOTH,"blood moth",'i',Color.Red,20,100,0,AttrType.FLYING,AttrType.SMALL);
			DefineAttack(ActorType.BLOOD_MOTH,100,3,CriticalEffect.DRAIN_LIFE,"& bites *");

			Define(ActorType.DARKNESS_DWELLER,"darkness dweller",'h',Color.DarkGreen,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.DARKNESS_DWELLER,100,2,CriticalEffect.STUN,"& hits *");

			Define(ActorType.CARNIVOROUS_BRAMBLE,"carnivorous bramble",'B',Color.DarkYellow,30,100,0,AttrType.PLANTLIKE,AttrType.IMMOBILE,AttrType.BLINDSIGHT);
			DefineAttack(ActorType.CARNIVOROUS_BRAMBLE,100,6,CriticalEffect.MAX_DAMAGE,"& rakes *");

			Define(ActorType.FROSTLING,"frostling",'E',Color.Gray,30,100,0,AttrType.IMMUNE_COLD);
			DefineAttack(ActorType.FROSTLING,100,2,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.SWORDSMAN,"swordsman",'p',Color.White,35,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.SWORDSMAN,100,2,CriticalEffect.NO_CRIT,"& hits *"); //todo: additional attacks to describe combo hits?
			Prototype(ActorType.SWORDSMAN).skills[SkillType.DEFENSE] = 2;

			Define(ActorType.DREAM_WARRIOR,"dream warrior",'p',Color.Cyan,35,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.DREAM_WARRIOR,100,2,CriticalEffect.STUN,"& hits *");

			Define(ActorType.DREAM_WARRIOR_CLONE,"dream warrior",'p',Color.Cyan,1,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.NONLIVING,AttrType.NO_CORPSE_KNOCKBACK);
			DefineAttack(ActorType.DREAM_WARRIOR_CLONE,100,0,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.SPITTING_COBRA,"spitting cobra",'S',Color.Red,25,100,0,AttrType.SMALL,AttrType.KEEN_SENSES,AttrType.POISON_HIT);
			DefineAttack(ActorType.SPITTING_COBRA,100,1,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.KOBOLD,"kobold",'k',Color.Blue,15,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE,AttrType.STEALTHY,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.KOBOLD,100,1,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.SPORE_POD,"spore pod",'e',Color.DarkMagenta,10,100,0,AttrType.FLYING,AttrType.SPORE_BURST,AttrType.PLANTLIKE,AttrType.BLINDSIGHT,AttrType.SMALL,AttrType.NO_CORPSE_KNOCKBACK);
			DefineAttack(ActorType.SPORE_POD,100,0,CriticalEffect.NO_CRIT,"& bumps *");

			Define(ActorType.FORASECT,"forasect",'i',Color.Gray,25,100,0,AttrType.REGENERATING);
			DefineAttack(ActorType.FORASECT,100,2,CriticalEffect.WEAK_POINT,"& bites *");

			Define(ActorType.POLTERGEIST,"poltergeist",'G',Color.DarkGreen,35,100,0,AttrType.NONLIVING,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION,AttrType.FLYING);
			DefineAttack(ActorType.POLTERGEIST,100,2,CriticalEffect.NO_CRIT,"& slimes *");
			DefineAttack(ActorType.POLTERGEIST,100,0,CriticalEffect.NO_CRIT,"& grabs at *");

			Define(ActorType.CULTIST,"cultist",'p',Color.DarkRed,30,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SMALL_GROUP);
			DefineAttack(ActorType.CULTIST,100,2,CriticalEffect.MAX_DAMAGE,"& hits *");

			Define(ActorType.GOBLIN_ARCHER,"goblin archer",'g',Color.DarkCyan,20,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.GOBLIN_ARCHER,100,2,CriticalEffect.STUN,"& hits *");

			Define(ActorType.GOBLIN_SHAMAN,"goblin shaman",'g',Color.Magenta,20,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.GOBLIN_SHAMAN,100,2,CriticalEffect.STUN,"& hits *");
			Prototype(ActorType.GOBLIN_SHAMAN).DefineMagicSkillForMonster(4);
			Prototype(ActorType.GOBLIN_SHAMAN).GainSpell(SpellType.FORCE_PALM,SpellType.SCORCH);

			Define(ActorType.GOLDEN_DART_FROG,"golden dart frog",'t',Color.Yellow,30,100,0,AttrType.LOW_LIGHT_VISION,AttrType.CAN_POISON_BLADES);
			DefineAttack(ActorType.GOLDEN_DART_FROG,100,2,CriticalEffect.POISON,"& slams *");

			Define(ActorType.SKELETON,"skeleton",'s',Color.White,15,100,0,AttrType.NONLIVING,AttrType.IMMUNE_BURNING,AttrType.IMMUNE_COLD,AttrType.REASSEMBLES,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.SKELETON,100,2,CriticalEffect.MAX_DAMAGE,"& hits *");

			Define(ActorType.SHADOW,"shadow",'G',Color.DarkGray,35,100,0,AttrType.NONLIVING,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION,AttrType.SHADOW_CLOAK);
			DefineAttack(ActorType.SHADOW,100,2,CriticalEffect.DIM_VISION,"& hits *");

			Define(ActorType.MIMIC,"mimic",'m',Color.White,30,200,0,AttrType.GRAB_HIT);
			DefineAttack(ActorType.MIMIC,100,2,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.PHASE_SPIDER,"phase spider",'A',Color.Cyan,40,100,0,AttrType.POISON_HIT,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.PHASE_SPIDER,100,1,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.ZOMBIE,"zombie",'z',Color.DarkGray,50,150,0,AttrType.NONLIVING,AttrType.MEDIUM_HUMANOID,AttrType.RESIST_NECK_SNAP,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.ZOMBIE,200,2,CriticalEffect.NO_CRIT,"& lunges forward and hits *","& lunges forward and misses *");
			DefineAttack(ActorType.ZOMBIE,100,3,CriticalEffect.MAX_DAMAGE,"& bites *");

			Define(ActorType.ROBED_ZEALOT,"robed zealot",'p',Color.Yellow,35,100,2,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.ROBED_ZEALOT,100,3,CriticalEffect.KNOCKBACK,"& hammers *");

			Define(ActorType.GIANT_SLUG,"giant slug",'w',Color.DarkGreen,40,150,0);
			DefineAttack(ActorType.GIANT_SLUG,100,2,CriticalEffect.SLIME,"& slams *");

			Define(ActorType.VULGAR_DEMON,"vulgar demon",'d',Color.Red,35,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.RESIST_NECK_SNAP,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.IMMUNE_FIRE);
			DefineAttack(ActorType.VULGAR_DEMON,100,3,CriticalEffect.WEAK_POINT,"& hits *");

			Define(ActorType.BANSHEE,"banshee",'G',Color.Magenta,35,50,0,AttrType.NONLIVING,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION,AttrType.FLYING);
			DefineAttack(ActorType.BANSHEE,100,2,CriticalEffect.MAX_DAMAGE,"& claws *");

			Define(ActorType.CAVERN_HAG,"cavern hag",'h',Color.Blue,35,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.CAVERN_HAG,100,2,CriticalEffect.GRAB,"& clutches at *");

			Define(ActorType.BERSERKER,"berserker",'p',Color.Red,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.BERSERKER,100,3,CriticalEffect.MAX_DAMAGE,"& hits *");

			Define(ActorType.DIRE_RAT,"dire rat",'r',Color.DarkRed,25,100,0,AttrType.LOW_LIGHT_VISION,AttrType.LARGE_GROUP,AttrType.SMALL,AttrType.KEEN_SENSES);
			DefineAttack(ActorType.DIRE_RAT,100,1,CriticalEffect.INFLICT_VULNERABILITY,"& bites *");

			Define(ActorType.SKULKING_KILLER,"skulking killer",'p',Color.DarkBlue,35,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.STEALTHY,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.SKULKING_KILLER,100,3,CriticalEffect.WEAK_POINT,"& hits *");
			Prototype(ActorType.SKULKING_KILLER).skills[SkillType.STEALTH] = 5;
			Prototype(ActorType.SKULKING_KILLER).skills[SkillType.DEFENSE] = 2;

			Define(ActorType.WILD_BOAR,"wild boar",'q',Color.DarkYellow,45,100,0,AttrType.LOW_LIGHT_VISION,AttrType.KEEN_SENSES);
			DefineAttack(ActorType.WILD_BOAR,100,2,CriticalEffect.NO_CRIT,"& gores *");

			Define(ActorType.TROLL,"troll",'T',Color.DarkGreen,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.REGENERATING,AttrType.REGENERATES_FROM_DEATH,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.TROLL,100,3,CriticalEffect.WORN_OUT,"& claws *");

			Define(ActorType.DREAM_SPRITE,"dream sprite",'y',Color.Cyan,30,100,0,AttrType.SMALL,AttrType.FLYING);
			DefineAttack(ActorType.DREAM_SPRITE,100,1,CriticalEffect.NO_CRIT,"& pokes *");

			Define(ActorType.DREAM_SPRITE_CLONE,"dream sprite",'y',Color.Cyan,1,0,0,AttrType.SMALL,AttrType.FLYING,AttrType.NONLIVING,AttrType.NO_CORPSE_KNOCKBACK); //speed is set to 100 *after* a clone is created for technical reasons
			DefineAttack(ActorType.DREAM_SPRITE_CLONE,100,0,CriticalEffect.NO_CRIT,"& pokes *");

			Define(ActorType.CLOUD_ELEMENTAL,"cloud elemental",'E',Color.RandomLightning,40,100,0,AttrType.NONLIVING,AttrType.FLYING,AttrType.IMMUNE_ELECTRICITY,AttrType.BLINDSIGHT,AttrType.NO_CORPSE_KNOCKBACK);
			DefineAttack(ActorType.CLOUD_ELEMENTAL,100,0,CriticalEffect.NO_CRIT,"& bumps *");

			Define(ActorType.DERANGED_ASCETIC,"deranged ascetic",'p',Color.RandomDark,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SPELL_DISRUPTION);
			DefineAttack(ActorType.DERANGED_ASCETIC,100,3,CriticalEffect.SWAP_POSITIONS,"& strikes *");
			DefineAttack(ActorType.DERANGED_ASCETIC,100,3,CriticalEffect.SWAP_POSITIONS,"& punches *");
			DefineAttack(ActorType.DERANGED_ASCETIC,100,3,CriticalEffect.SWAP_POSITIONS,"& kicks *");

			Define(ActorType.SHADOWVEIL_DUELIST,"shadowveil duelist",'p',Color.DarkCyan,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SHADOW_CLOAK,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.SHADOWVEIL_DUELIST,100,3,CriticalEffect.WEAK_POINT,"& hits *");
			Prototype(ActorType.SHADOWVEIL_DUELIST).skills[SkillType.DEFENSE] = 2;

			Define(ActorType.WARG,"warg",'c',Color.White,25,50,0,AttrType.LOW_LIGHT_VISION,AttrType.MEDIUM_GROUP,AttrType.KEEN_SENSES);
			DefineAttack(ActorType.WARG,100,3,CriticalEffect.STUN,"& bites *"); //todo: 25 or 30 health?

			Define(ActorType.ALASI_SCOUT,"alasi scout",'a',Color.Blue,35,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE);
			DefineAttack(ActorType.ALASI_SCOUT,100,3,CriticalEffect.WEAK_POINT,"& hits *");
			DefineAttack(ActorType.ALASI_SCOUT,100,3,CriticalEffect.WEAK_POINT,"& fires a phantom blade at *","& misses * with a phantom blade");
			Prototype(ActorType.ALASI_SCOUT).skills[SkillType.DEFENSE] = 4;

			Define(ActorType.CARRION_CRAWLER,"carrion crawler",'i',Color.DarkGreen,25,100,0,AttrType.PARALYSIS_HIT,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.CARRION_CRAWLER,100,0,CriticalEffect.NO_CRIT,"& lashes * with a tentacle");
			DefineAttack(ActorType.CARRION_CRAWLER,100,1,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.MECHANICAL_KNIGHT,"mechanical knight",'K',Color.DarkRed,10,100,0,AttrType.NONLIVING,AttrType.MECHANICAL_SHIELD,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.DULLS_BLADES);
			DefineAttack(ActorType.MECHANICAL_KNIGHT,100,3,CriticalEffect.WEAK_POINT,"& hits *");
			DefineAttack(ActorType.MECHANICAL_KNIGHT,100,3,CriticalEffect.WEAK_POINT,"& kicks *");

			Define(ActorType.RUNIC_TRANSCENDENT,"runic transcendent",'h',Color.Magenta,30,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE);
			DefineAttack(ActorType.RUNIC_TRANSCENDENT,100,2,CriticalEffect.NO_CRIT,"& hits *");
			Prototype(ActorType.RUNIC_TRANSCENDENT).DefineMagicSkillForMonster(6);
			Prototype(ActorType.RUNIC_TRANSCENDENT).GainSpell(SpellType.MERCURIAL_SPHERE);

			Define(ActorType.ALASI_BATTLEMAGE,"alasi battlemage",'a',Color.Yellow,35,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE);
			DefineAttack(ActorType.ALASI_BATTLEMAGE,100,2,CriticalEffect.NO_CRIT,"& hits *");
			Prototype(ActorType.ALASI_BATTLEMAGE).skills[SkillType.DEFENSE] = 4;
			Prototype(ActorType.ALASI_BATTLEMAGE).DefineMagicSkillForMonster(7);
			Prototype(ActorType.ALASI_BATTLEMAGE).GainSpell(SpellType.FIRE_BLITZ,SpellType.MAGIC_HAMMER);

			Define(ActorType.ALASI_SOLDIER,"alasi soldier",'a',Color.White,40,100,0,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE);
			DefineAttack(ActorType.ALASI_SOLDIER,100,3,CriticalEffect.NO_CRIT,"& hits * with its spear","& misses * with its spear"); //todo: additional attacks for combo hits? doesn't seem necessary...
			Prototype(ActorType.ALASI_SOLDIER).skills[SkillType.DEFENSE] = 4;

			Define(ActorType.INFESTED_MASS_TODO_NAME,"infested mass",'F',Color.Gray,40,50,0,AttrType.BLINDSIGHT);
			DefineAttack(ActorType.INFESTED_MASS_TODO_NAME,100,3,CriticalEffect.INFEST,"& hits *");

			Define(ActorType.STONE_GOLEM,"stone golem",'x',Color.Gray,55,120,0,AttrType.NONLIVING,AttrType.STALAGMITE_HIT,AttrType.DULLS_BLADES,AttrType.IMMUNE_BURNING,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.STONE_GOLEM,100,4,CriticalEffect.NO_CRIT,"& slams *"); //todo: speed change? it's the ONLY one that isn't a multiple of 50.

			Define(ActorType.MUD_ELEMENTAL,"mud elemental",'E',Color.DarkYellow,30,100,0,AttrType.NONLIVING,AttrType.BLINDSIGHT,AttrType.RESIST_WEAPONS,AttrType.IMMUNE_ARROWS); //todo: keep immunity to arrows or not?
			DefineAttack(ActorType.MUD_ELEMENTAL,100,2,CriticalEffect.BLIND,"& hits *");

			Define(ActorType.MUD_TENTACLE,"mud tentacle",'~',Color.DarkYellow,1,100,0,AttrType.NONLIVING,AttrType.BLINDSIGHT,AttrType.GRAB_HIT,AttrType.IMMOBILE);
			DefineAttack(ActorType.MUD_TENTACLE,100,1,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.FLAMETONGUE_TOAD,"flametongue toad",'t',Color.Red,50,100,0,AttrType.IMMUNE_FIRE,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.FLAMETONGUE_TOAD,100,2,CriticalEffect.KNOCKBACK,"& slams *");

			Define(ActorType.ENTRANCER,"entrancer",'p',Color.DarkMagenta,30,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.ENTRANCER,100,2,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.OGRE,"ogre",'O',Color.Green,50,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.LOW_LIGHT_VISION,AttrType.SMALL_GROUP);
			DefineAttack(ActorType.OGRE,100,4,CriticalEffect.WORN_OUT,"& hits *");
			Prototype(ActorType.OGRE).skills[SkillType.DEFENSE] = 4;

			Define(ActorType.ORC_GRENADIER,"orc grenadier",'o',Color.DarkYellow,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.ORC_GRENADIER,100,3,CriticalEffect.STUN,"& hits *");
			Prototype(ActorType.ORC_GRENADIER).skills[SkillType.DEFENSE] = 2;

			Define(ActorType.SPELLMUDDLE_PIXIE,"spellmuddle pixie",'y',Color.RandomBright,35,50,0,AttrType.SMALL,AttrType.FLYING,AttrType.SPELL_DISRUPTION);
			DefineAttack(ActorType.SPELLMUDDLE_PIXIE,100,2,CriticalEffect.INFLICT_VULNERABILITY,"& scratches *");

			Define(ActorType.CRUSADING_KNIGHT,"crusading knight",'K',Color.DarkGray,45,100,6,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.CRUSADING_KNIGHT,200,7,CriticalEffect.STRONG_KNOCKBACK,"& hits * with a huge mace","& misses * with a huge mace");
			Prototype(ActorType.CRUSADING_KNIGHT).skills[SkillType.DEFENSE] = 10;

			Define(ActorType.TROLL_BLOODWITCH,"troll bloodwitch",'T',Color.DarkRed,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.REGENERATING,AttrType.REGENERATING,AttrType.REGENERATING,AttrType.REGENERATES_FROM_DEATH,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.TROLL_BLOODWITCH,100,3,CriticalEffect.WORN_OUT,"& claws *");

			Define(ActorType.SAVAGE_HULK,"savage hulk",'H',Color.DarkGreen,55,50,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.GRAB_HIT);
			DefineAttack(ActorType.SAVAGE_HULK,100,4,CriticalEffect.WORN_OUT,"& smashes *");

			Define(ActorType.MARBLE_HORROR,"marble horror",'&',Color.Gray,45,100,0,AttrType.NONLIVING,AttrType.LOW_LIGHT_VISION,AttrType.DIM_VISION_HIT); //todo: keep dim vision?
			DefineAttack(ActorType.MARBLE_HORROR,100,4,CriticalEffect.NO_CRIT,"& hits *");

			Define(ActorType.CRYOLICH,"cryolich",'L',Color.White,35,100,0,AttrType.NONLIVING,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE,AttrType.LOW_LIGHT_VISION,AttrType.RESIST_NECK_SNAP);
			DefineAttack(ActorType.CRYOLICH,100,1,CriticalEffect.NO_CRIT,"& touches *");
			Prototype(ActorType.CRYOLICH).DefineMagicSkillForMonster(9);
			Prototype(ActorType.CRYOLICH).GainSpell(SpellType.GLACIAL_BLAST,SpellType.BLIZZARD);
			
			Define(ActorType.PYREN_ARCHER,"pyren archer",'P',Color.DarkRed,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.FIERY_ARROWS,AttrType.IMMUNE_BURNING);
			DefineAttack(ActorType.PYREN_ARCHER,100,3,CriticalEffect.IGNITE,"& hits *");
			Prototype(ActorType.PYREN_ARCHER).skills[SkillType.DEFENSE] = 2;

			Define(ActorType.PLACEHOLDER,"[REDACTED]",'?',Color.RandomAny,1,100,0,AttrType.SHADOW_CLOAK,AttrType.TERRIFYING,AttrType.BLINDSIGHT);
			DefineAttack(ActorType.PLACEHOLDER,100,5,CriticalEffect.NO_CRIT,"& [DATA EXPUNGED] *");

			Define(ActorType.ALASI_SENTINEL,"alasi sentinel",'a',Color.DarkGray,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.DAMAGE_REDUCTION);
			DefineAttack(ActorType.ALASI_SENTINEL,100,3,CriticalEffect.NO_CRIT,"& hits *");
			Prototype(ActorType.ALASI_SENTINEL).skills[SkillType.DEFENSE] = 6;

			Define(ActorType.NOXIOUS_WORM,"noxious worm",'W',Color.DarkMagenta,55,150,0);
			DefineAttack(ActorType.NOXIOUS_WORM,100,3,CriticalEffect.STUN,"& slams *");
			DefineAttack(ActorType.NOXIOUS_WORM,100,3,CriticalEffect.STUN,"& bites *");

			Define(ActorType.LASHER_FUNGUS,"lasher fungus",'F',Color.DarkGreen,45,100,0,AttrType.PLANTLIKE,AttrType.SPORE_BURST,AttrType.IMMUNE_BURNING,AttrType.BLINDSIGHT,AttrType.IMMOBILE);
			DefineAttack(ActorType.LASHER_FUNGUS,100,3,CriticalEffect.NO_CRIT,"& extends a tentacle and hits *","& misses * with a tentacle");
			DefineAttack(ActorType.LASHER_FUNGUS,100,1,CriticalEffect.NO_CRIT,"& extends a tentacle and drags * closer","& misses * with a tentacle");

			Define(ActorType.VAMPIRE,"vampire",'V',Color.Blue,40,100,0,AttrType.NONLIVING,AttrType.MEDIUM_HUMANOID,AttrType.HUMANOID_INTELLIGENCE,AttrType.RESIST_NECK_SNAP,AttrType.FLYING,AttrType.LIGHT_SENSITIVE,AttrType.DESTROYED_BY_SUNLIGHT,AttrType.LIFE_DRAIN_HIT,AttrType.IMMUNE_COLD,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.VAMPIRE,100,3,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.ORC_WARMAGE,"orc warmage",'o',Color.Red,45,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.ORC_WARMAGE,100,3,CriticalEffect.STUN,"& hits *");
			Prototype(ActorType.ORC_WARMAGE).GainSpell(SpellType.DETECT_MOVEMENT,SpellType.BLINK,SpellType.SCORCH,SpellType.MAGIC_HAMMER,SpellType.GLACIAL_BLAST,SpellType.COLLAPSE);
			Prototype(ActorType.ORC_WARMAGE).DefineMagicSkillForMonster(10);
			Prototype(ActorType.ORC_WARMAGE).skills[SkillType.DEFENSE] = 2;
			
			Define(ActorType.NECROMANCER,"necromancer",'p',Color.Blue,40,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			DefineAttack(ActorType.NECROMANCER,100,2,CriticalEffect.DRAIN_LIFE,"& hits *");

			Define(ActorType.STALKING_WEBSTRIDER,"stalking webstrider",'A',Color.Red,40,100,0,AttrType.KEEN_SENSES,AttrType.LOW_LIGHT_VISION,AttrType.POISON_HIT);
			DefineAttack(ActorType.STALKING_WEBSTRIDER,100,3,CriticalEffect.NO_CRIT,"& bites *");

			Define(ActorType.ORC_ASSASSIN,"orc assassin",'o',Color.DarkBlue,50,100,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.STEALTHY,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.ORC_ASSASSIN,100,3,CriticalEffect.NO_CRIT,"& hits *");
			Prototype(ActorType.ORC_ASSASSIN).skills[SkillType.STEALTH] = 10;
			Prototype(ActorType.ORC_ASSASSIN).skills[SkillType.DEFENSE] = 2;

			Define(ActorType.LUMINOUS_AVENGER,"luminous avenger",'E',Color.Yellow,40,50,12,AttrType.HOLY_SHIELDED);
			DefineAttack(ActorType.LUMINOUS_AVENGER,100,3,CriticalEffect.BLIND,"& strikes *");

			Define(ActorType.CORPSETOWER_BEHEMOTH,"corpsetower behemoth",'Z',Color.DarkMagenta,75,100,0,AttrType.NONLIVING,AttrType.REGENERATING,AttrType.IMMUNE_COLD,AttrType.STUN_HIT,AttrType.WORN_OUT_HIT,AttrType.LOW_LIGHT_VISION);
			DefineAttack(ActorType.CORPSETOWER_BEHEMOTH,100,7,CriticalEffect.NO_CRIT,"& clobbers *");

			Define(ActorType.MACHINE_OF_WAR,"machine of war",'M',Color.DarkGray,50,100,0,AttrType.NONLIVING,AttrType.BLINDSIGHT,AttrType.DULLS_BLADES,AttrType.IMMUNE_FIRE);
			DefineAttack(ActorType.MACHINE_OF_WAR,100,0,CriticalEffect.NO_CRIT,"& bumps *");

			Define(ActorType.FIRE_DRAKE,"fire drake",'D',Color.DarkRed,200,50,2,AttrType.BOSS_MONSTER,AttrType.LOW_LIGHT_VISION,AttrType.IMMUNE_FIRE,AttrType.HUMANOID_INTELLIGENCE);
			DefineAttack(ActorType.FIRE_DRAKE,100,3,CriticalEffect.MAX_DAMAGE,"& bites *");
			DefineAttack(ActorType.FIRE_DRAKE,100,3,CriticalEffect.MAX_DAMAGE,"& claws *");

			Define(ActorType.GHOST,"ghost",'G',Color.White,30,100,0,AttrType.NONLIVING,AttrType.FLYING);
			DefineAttack(ActorType.GHOST,100,2,CriticalEffect.INFLICT_VULNERABILITY,"& touches *");

			Define(ActorType.PHANTOM,"phantom",'?',Color.Cyan,1,100,0,AttrType.NONLIVING,AttrType.FLYING,AttrType.NO_CORPSE_KNOCKBACK); //the template on which the different types of phantoms are based
			DefineAttack(ActorType.PHANTOM_ARCHER,100,2,CriticalEffect.NO_CRIT,"& hits *");
			DefineAttack(ActorType.PHANTOM_BEHEMOTH,100,7,CriticalEffect.NO_CRIT,"& clobbers *"); //todo: update phantom definitions with worn_out_hit etc
			DefineAttack(ActorType.PHANTOM_BLIGHTWING,100,3,CriticalEffect.MAX_DAMAGE,"& bites *");
			DefineAttack(ActorType.PHANTOM_BLIGHTWING,100,3,CriticalEffect.MAX_DAMAGE,"& scratches *");
			DefineAttack(ActorType.PHANTOM_CONSTRICTOR,100,2,CriticalEffect.NO_CRIT,"& hits *");
			DefineAttack(ActorType.PHANTOM_CRUSADER,200,7,CriticalEffect.STRONG_KNOCKBACK,"& hits * with a huge mace","& misses * with a huge mace");
			DefineAttack(ActorType.PHANTOM_OGRE,100,4,CriticalEffect.WORN_OUT,"& hits *");
			DefineAttack(ActorType.PHANTOM_SWORDMASTER,100,3,CriticalEffect.NO_CRIT,"& hits *"); //todo: extra attacks?
			DefineAttack(ActorType.PHANTOM_TIGER,100,3,CriticalEffect.SLOW,"& bites *");
			DefineAttack(ActorType.PHANTOM_ZOMBIE,200,2,CriticalEffect.NO_CRIT,"& lunges forward and hits *","& lunges forward and misses *");
			DefineAttack(ActorType.PHANTOM_ZOMBIE,100,3,CriticalEffect.MAX_DAMAGE,"& bites *");
		}
		private static void Define(ActorType type_,string name_,char symbol_,Color color_,int maxhp_,int speed_,int light_radius_,params AttrType[] attrlist){
			proto[type_] = new Actor(type_,name_,symbol_,color_,maxhp_,speed_,light_radius_,attrlist);
		}
		private static void DefineAttack(ActorType type,int cost,int damage_dice,CriticalEffect crit,string message){ DefineAttack(type,cost,damage_dice,crit,message,""); }
		private static void DefineAttack(ActorType type,int cost,int damage_dice,CriticalEffect crit,string message,string miss_message){
			if(attack[type] == null){
				attack[type] = new List<AttackInfo>();
			}
			attack[type].Add(new AttackInfo(cost,damage_dice,crit,message,miss_message));
		}
		public Actor(){
			F = new SpellType[13];
			inv = new List<Item>();
			attrs = new Dict<AttrType, int>();
			skills = new Dict<SkillType,int>();
			feats = new Dict<FeatType,bool>();
			spells = new Dict<SpellType,bool>();
		}
		public Actor(Actor a,int r,int c){
			type = a.type;
			name = a.name;
			the_name = a.the_name;
			a_name = a.a_name;
			symbol = a.symbol;
			color = a.color;
			maxhp = a.maxhp;
			curhp = maxhp;
			maxmp = a.maxmp;
			curmp = maxmp;
			speed = a.speed;
			light_radius = a.light_radius;
			target = null;
			F = new SpellType[13];
			for(int i=0;i<13;++i){
				F[i] = SpellType.NO_SPELL;
			}
			inv = new List<Item>();
			row = r;
			col = c;
			target_location = null;
			time_of_last_action = 0;
			recover_time = 0;
			player_visibility_duration = 0;
			weapons.AddFirst(new Weapon(WeaponType.NO_WEAPON));
			armors.AddFirst(new Armor(ArmorType.NO_ARMOR));
			attrs = new Dict<AttrType, int>(a.attrs);
			skills = new Dict<SkillType,int>(a.skills);
			feats = new Dict<FeatType,bool>(a.feats);
			spells = new Dict<SpellType,bool>(a.spells);
			exhaustion = 0;
		}
		public Actor(ActorType type_,string name_,char symbol_,Color color_,int maxhp_,int speed_,int light_radius_,params AttrType[] attrlist){
			type = type_;
			SetName(name_);
			symbol = symbol_;
			color = color_;
			maxhp = maxhp_;
			curhp = maxhp;
			maxmp = 0;
			curmp = maxmp;
			speed = speed_;
			light_radius = light_radius_;
			target = null;
			inv = null;
			target_location = null;
			time_of_last_action = 0;
			recover_time = 0;
			player_visibility_duration = 0;
			F = new SpellType[13];
			for(int i=0;i<13;++i){
				F[i] = SpellType.NO_SPELL;
			}
			exhaustion = 0;
			foreach(AttrType at in attrlist){
				attrs[at]++;
			}//row and col are -1
		}
		public static Actor Create(ActorType type,int r,int c){ return Create(type,r,c,false,false); } //not sure that false,false should be the default here
		public static Actor Create(ActorType type,int r,int c,bool add_to_tiebreaker_list,bool insert_after_current){
			Actor a = null;
			if(M.actor[r,c] == null){
				a = new Actor(proto[type],r,c);
				M.actor[r,c] = a;
				if(add_to_tiebreaker_list){
					if(insert_after_current){
						tiebreakers.Insert(Q.Tiebreaker + 1,a);
						Q.UpdateTiebreaker(Q.Tiebreaker + 1);
						Event e = new Event(a,a.Speed(),EventType.MOVE);
						e.tiebreaker = Q.Tiebreaker + 1;
						Q.Add(e);
					}
					else{
						tiebreakers.Add(a);
						Event e = new Event(a,a.Speed(),EventType.MOVE);
						e.tiebreaker = tiebreakers.Count - 1; //since it's the last one
						Q.Add(e);
					}
				}
				else{
					a.QS();
				}
				if(R.OneIn(10) && (type == ActorType.SWORDSMAN || type == ActorType.ENTRANCER || type == ActorType.DERANGED_ASCETIC || type == ActorType.ALASI_BATTLEMAGE
				   || type == ActorType.ALASI_SCOUT || type == ActorType.ALASI_SENTINEL || type == ActorType.ALASI_SOLDIER || type == ActorType.PYREN_ARCHER)){
					a.light_radius = 4;
				}
				if(a.light_radius > 0){
					a.UpdateRadius(0,a.light_radius);
				}
			}
			return a;
		}
		public static Actor CreatePhantom(int r,int c){
			Actor a = Create(ActorType.PHANTOM,r,c,true,true);
			if(a == null){
				return null;
			}
			ActorType type = (ActorType)(R.Roll(9) + (int)ActorType.PHANTOM);
			a.type = type;
			switch(type){
			case ActorType.PHANTOM_ARCHER:
				a.SetName("phantom archer");
				a.symbol = 'g';
				break;
			case ActorType.PHANTOM_BEHEMOTH:
				a.SetName("phantom behemoth");
				a.symbol = 'H';
				a.attrs[AttrType.STUN_HIT]++;
				break;
			case ActorType.PHANTOM_BLIGHTWING:
				a.SetName("phantom blightwing");
				a.symbol = 'b';
				a.speed = 50;
				break;
			case ActorType.PHANTOM_CONSTRICTOR:
				a.SetName("phantom constrictor");
				a.symbol = 'S';
				a.attrs[AttrType.GRAB_HIT]++;
				break;
			case ActorType.PHANTOM_CRUSADER:
				a.SetName("phantom crusader");
				a.symbol = 'p';
				a.UpdateRadius(0,6,true);
				break;
			case ActorType.PHANTOM_OGRE:
				a.SetName("phantom ogre");
				a.symbol = 'O';
				break;
			case ActorType.PHANTOM_SWORDMASTER:
				a.SetName("phantom swordmaster");
				a.symbol = 'h';
				break;
			case ActorType.PHANTOM_TIGER:
				a.SetName("phantom tiger");
				a.symbol = 'f';
				a.speed = 50;
				break;
			case ActorType.PHANTOM_ZOMBIE:
				a.SetName("phantom zombie");
				a.symbol = 'z';
				a.speed = 150;
				break;
			}
			return a;
		}
		public bool Is(params ActorType[] types){
			foreach(ActorType at in types){
				if(type == at){
					return true;
				}
			}
			return false;
		}
		public string AName(bool consider_visibility){
			if(!consider_visibility || player.CanSee(this)){
				return a_name;
			}
			else{
				return "something";
			}
		}
		public string TheName(bool consider_visibility){
			if(!consider_visibility || player.CanSee(this)){
				return the_name;
			}
			else{
				return "something";
			}
		}
		override public string YouVisible(string s){ return YouVisible(s,false); }
		override public string YouVisible(string s,bool ends_in_es){ //if not visible, YouVisible("attack") returns "something attacks"
			if(name == "you"){
				return "you " + s;
			}
			else{
				if(ends_in_es){
					return TheName(true) + " " + s + "es";
				}
				else{
					return TheName(true) + " " + s + "s";
				}
			}
		}
		public string YouVisibleAre(){
			if(name == "you"){
				return "you are";
			}
			else{
				if(player.CanSee(this)){
					return the_name + " is";
				}
				else{
					return "something is";
				}
			}
		}
		public string YourVisible(){
			if(name == "you"){
				return "your";
			}
			else{
				if(player.CanSee(this)){
					return the_name + "'s";
				}
				else{
					return "something's";
				}
			}
		}
		public void Move(int r,int c){ Move(r,c,true); }
		public void Move(int r,int c,bool trigger_traps){
			if(r>=0 && r<ROWS && c>=0 && c<COLS){
				if(row >= 0 && row < ROWS && col >= 0 && col < COLS){
					if(this == player){
						if(DistanceFrom(r,c) == 1){
							tile().direction_exited = DirectionOf(new pos(r,c));
						}
						else{
							tile().direction_exited = 0;
						}
					}
					else{
						if(DistanceFrom(r,c) == 1){
							attrs[AttrType.DIRECTION_OF_PREVIOUS_TILE] = DirectionOf(new pos(r,c)).RotateDir(true,4);
						}
						else{
							attrs[AttrType.DIRECTION_OF_PREVIOUS_TILE] = -1;
						}
					}
				}
				if(M.actor[r,c] == null){
					if(HasAttr(AttrType.GRABBED)){
						foreach(Actor a in ActorsAtDistance(1)){
							if(a.attrs[AttrType.GRABBING] == a.DirectionOf(this)){
								if(a.DistanceFrom(r,c) > 1){
									attrs[AttrType.GRABBED]--;
									a.attrs[AttrType.GRABBING] = 0;
								}
								else{
									a.attrs[AttrType.GRABBING] = a.DirectionOf(new pos(r,c));
								}
							}
						}
					}
					bool torch=false;
					if(LightRadius() > 0){
						torch=true;
						UpdateRadius(LightRadius(),0);
					}
					M.actor[r,c] = this;
					if(row>=0 && row<ROWS && col>=0 && col<COLS){
						M.actor[row,col] = null;
						if(this == player && M.tile[row,col].inv != null){
							M.tile[row,col].inv.ignored = true; //todo: this will not work as intended once you can be knocked back over items.
						}
					}
					row = r;
					col = c;
					if(torch){
						UpdateRadius(0,LightRadius());
					}
					if(trigger_traps && tile().IsTrap() && !HasAttr(AttrType.FLYING) && !HasAttr(AttrType.SMALL)
					   && (type==ActorType.PLAYER || target == player)){ //prevents wandering monsters from triggering traps
						tile().TriggerTrap();
					}
				}
				else{ //default is now to swap places, rather than do nothing, since everything checks anyway.
					Actor a = M.actor[r,c];
					if(!a.HasAttr(AttrType.IMMOBILE)){
						bool torch = false;
						bool other_torch = false;
						if(LightRadius() > 0){
							torch = true;
							UpdateRadius(LightRadius(),0);
						}
						if(a.LightRadius() > 0){
							other_torch = true;
							a.UpdateRadius(a.LightRadius(),0);
						}
						if(row>=0 && row<ROWS && col>=0 && col<COLS){
							if(this == player && M.tile[row,col].inv != null){
								M.tile[row,col].inv.ignored = true;
							}
						}
						M.actor[r,c] = this;
						M.actor[row,col] = a;
						a.row = row;
						a.col = col;
						row = r;
						col = c;
						if(torch){
							UpdateRadius(0,LightRadius());
						}
						if(other_torch){
							a.UpdateRadius(0,a.LightRadius());
						}
					}
				}
				if(this == player){
					M.UpdateSafetyMap2(this);
				}
				else{
					if(player.HasAttr(AttrType.DETECTING_MOVEMENT) && DistanceFrom(player) <= 8 && !player.CanSee(this)){
						footsteps.AddUnique(p);
					}
				}
			}
		}
		public bool GrabPreventsMovement(PhysicalObject o){
			if(!HasAttr(AttrType.GRABBED) || DistanceFrom(o) > 1 || HasAttr(AttrType.BRUTISH_STRENGTH) || HasAttr(AttrType.SLIMED) || HasAttr(AttrType.GREASED)){
				return false;
			}
			List<Actor> grabbers = new List<Actor>();
			foreach(Actor a in ActorsAtDistance(1)){
				if(a.attrs[AttrType.GRABBING] == a.DirectionOf(this)){
					grabbers.Add(a);
				}
			}
			foreach(Actor a in grabbers){
				if(o.DistanceFrom(a) > 1){
					return true;
				}
			}
			return false;
		}
		public int InventoryCount(){
			int result = 0;
			foreach(Item i in inv){
				result += i.quantity;
			}
			return result;
		}
		public bool GetItem(Item i){
			if(InventoryCount() + i.quantity > Global.MAX_INVENTORY_SIZE){
				return false;
			}
			foreach(Item held in inv){
				if(held.type == i.type && !held.do_not_stack && !i.do_not_stack){
					held.quantity += i.quantity;
					return true;
				}
			}
			List<Item> new_inv = new List<Item>();
			bool added = false;
			foreach(Item held in inv){
				if(!added && i.SortOrderOfItemType() < held.SortOrderOfItemType()){
					new_inv.Add(i);
					added = true;
				}
				new_inv.Add(held);
			}
			if(!added){
				new_inv.Add(i);
			}
			inv = new_inv;
			return true;
		}
		public bool HasAttr(AttrType attr){ return attrs[attr] > 0; }
		public bool HasFeat(FeatType feat){ return feats[feat]; }
		public bool HasSpell(SpellType spell){ return spells[spell]; }
		public void GainAttr(AttrType attr,int duration){
			attrs[attr]++;
			Q.Add(new Event(this,duration,attr));
		}
		public void GainAttr(AttrType attr,int duration,int value){
			attrs[attr] += value;
			Q.Add(new Event(this,duration,attr,value));
		}
		public void GainAttr(AttrType attr,int duration,string msg,params PhysicalObject[] objs){
			attrs[attr]++;
			Q.Add(new Event(this,duration,attr,msg,objs));
		}
		public void GainAttr(AttrType attr,int duration,int value,string msg,params PhysicalObject[] objs){
			attrs[attr] += value;
			Q.Add(new Event(this,duration,attr,value,msg,objs));
		}
		public void GainAttrRefreshDuration(AttrType attr,int duration){
			attrs[attr]++;
			Event e = Q.FindAttrEvent(this,attr);
			if(e != null){
				if(e.TimeToExecute() < duration + Q.turn){ //if the new one would last longer than the old one, replace it.
					e.dead = true;
					Q.Add(new Event(this,duration,attr,attrs[attr]));
				}
				else{ //(if the old one still lasts longer, update it so it removes the new value)
					e.value = attrs[attr];
				}
			}
			else{
				Q.Add(new Event(this,duration,attr,attrs[attr]));
			}
		}
		public void GainAttrRefreshDuration(AttrType attr,int duration,string msg,params PhysicalObject[] objs){
			attrs[attr]++;
			Event e = Q.FindAttrEvent(this,attr);
			if(e != null){
				if(e.TimeToExecute() < duration + Q.turn){ //if the new one would last longer than the old one, replace it.
					e.dead = true;
					Q.Add(new Event(this,duration,attr,attrs[attr],msg,objs));
				}
				else{ //(if the old one still lasts longer, update it so it removes the new value)
					e.value = attrs[attr];
				}
			}
			else{
				Q.Add(new Event(this,duration,attr,attrs[attr],msg,objs));
			}
		}
		public void RefreshDuration(AttrType attr,int duration){
			if(attrs[attr] == 0){
				attrs[attr]++;
			}
			Event e = Q.FindAttrEvent(this,attr);
			if(e != null){
				if(e.TimeToExecute() < duration + Q.turn){ //if the new one would last longer than the old one, replace it.
					e.dead = true;
					Q.Add(new Event(this,duration,attr,attrs[attr]));
				} //(if the old one still lasts longer, do nothing)
			}
			else{
				Q.Add(new Event(this,duration,attr,attrs[attr]));
			}
		}
		public void RefreshDuration(AttrType attr,int duration,string msg,params PhysicalObject[] objs){
			if(attrs[attr] == 0){
				attrs[attr]++;
			}
			Event e = Q.FindAttrEvent(this,attr);
			if(e != null){
				if(e.TimeToExecute() < duration + Q.turn){ //if the new one would last longer than the old one, replace it.
					e.dead = true;
					Q.Add(new Event(this,duration,attr,attrs[attr],msg,objs));
				} //(if the old one still lasts longer, do nothing)
			}
			else{
				Q.Add(new Event(this,duration,attr,attrs[attr],msg,objs));
			}
		}
		public void DefineMagicSkillForMonster(int value){ //assumes this will happen only for prototypes
			skills[SkillType.MAGIC] = value;
			maxmp = skills[SkillType.MAGIC] * 5;
			curmp = maxmp;
		}
		public void GainSpell(params SpellType[] spell_list){
			foreach(SpellType spell in spell_list){
				spells[spell] = true;
			}
		}
		public Weapon EquippedWeapon{
			get{
				if(weapons != null && weapons.Count > 0){
					return weapons.First.Value;
				}
				return null;
			}
			set{
				if(weapons == null){
					weapons = new LinkedList<Weapon>();
				}
				if(weapons.Contains(value)){
					while(weapons.First.Value != value){
						Weapon temp = weapons.First.Value;
						weapons.Remove(temp);
						weapons.AddLast(temp);
					}
				}
				else{
					weapons.AddFirst(value);
				}
			}
		}
		public Armor EquippedArmor{
			get{
				if(armors != null && armors.Count > 0){
					return armors.First.Value;
				}
				return null;
			}
			set{
				if(armors == null){
					armors = new LinkedList<Armor>();
				}
				if(armors.Contains(value)){
					while(armors.First.Value != value){
						Armor temp = armors.First.Value;
						armors.Remove(temp);
						armors.AddLast(temp);
					}
				}
				else{
					armors.AddFirst(value);
				}
			}
		}
		private Weapon WeaponOfType(WeaponType w){
			if(weapons == null || weapons.Count == 0){
				return null;
			}
			LinkedListNode<Weapon> n = weapons.First;
			while(n.Value.type != w){
				if(n == weapons.Last){
					return null; //reached the end
				}
				else{
					n = n.Next;
				}
			}
			return n.Value;
		}
		private Armor ArmorOfType(ArmorType a){
			if(armors == null || armors.Count == 0){
				return null;
			}
			LinkedListNode<Armor> n = armors.First;
			while(n.Value.type != a){
				if(n == armors.Last){
					return null; //reached the end
				}
				else{
					n = n.Next;
				}
			}
			return n.Value;
		}
		public Weapon Sword{get{return WeaponOfType(WeaponType.SWORD);}}
		public Weapon Mace{get{return WeaponOfType(WeaponType.MACE);}}
		public Weapon Dagger{get{return WeaponOfType(WeaponType.DAGGER);}}
		public Weapon Staff{get{return WeaponOfType(WeaponType.STAFF);}}
		public Weapon Bow{get{return WeaponOfType(WeaponType.BOW);}}
		public Armor Leather{get{return ArmorOfType(ArmorType.LEATHER);}}
		public Armor Chainmail{get{return ArmorOfType(ArmorType.CHAINMAIL);}}
		public Armor Plate{get{return ArmorOfType(ArmorType.FULL_PLATE);}}
		public int Speed(){
			int bloodboil = attrs[AttrType.BLOOD_BOILED]*10;
			int vigor = HasAttr(AttrType.VIGOR)? 50 : 0;
			int haste = Math.Max(bloodboil,vigor); //only the biggest applies
			if(HasAttr(AttrType.SLOWED)){
				return (speed - haste) * 2;
			}
			else{
				return speed - haste;
			}
		}
		public int LightRadius(){ return Math.Max(light_radius,attrs[AttrType.BURNING]); } //BURNING should now have a maximum of 1
		public int TotalProtectionFromArmor(){
			int effective_exhaustion = exhaustion;
			if(HasFeat(FeatType.ARMOR_MASTERY)){
				effective_exhaustion -= 5;
			}
			switch(EquippedArmor.type){
			case ArmorType.LEATHER:
				if(effective_exhaustion >= 15){
					return 0;
				}
				break;
			case ArmorType.CHAINMAIL:
				if(effective_exhaustion >= 10){
					return 0;
				}
				break;
			case ArmorType.FULL_PLATE:
				if(effective_exhaustion >= 5){
					return 0;
				}
				break;
			}
			return EquippedArmor.Protection();
		}
		public int TotalSkill(SkillType skill){
			int result = skills[skill];
			switch(skill){
			case SkillType.COMBAT:
				result += attrs[AttrType.BONUS_COMBAT];
				break;
			case SkillType.DEFENSE:
				result += attrs[AttrType.BONUS_DEFENSE];
				result += TotalProtectionFromArmor();
				break;
			case SkillType.MAGIC:
				result += attrs[AttrType.BONUS_MAGIC];
				break;
			case SkillType.SPIRIT:
				result += attrs[AttrType.BONUS_SPIRIT];
				break;
			case SkillType.STEALTH:
				result += attrs[AttrType.BONUS_STEALTH];
				if(LightRadius() > 0){
					return 0;
				}
				if(!tile().IsLit()){
					if(type == ActorType.PLAYER || !player.HasAttr(AttrType.SHADOWSIGHT)){ //+2 stealth while in darkness unless shadowsight is in effect
						result += 2;
					}
				}
				if(!HasFeat(FeatType.CUNNING_DODGE) || EquippedArmor.type != ArmorType.CHAINMAIL){
					result -= EquippedArmor.StealthPenalty();
				}
				break;
			}
			return result;
		}
		public string WoundStatus(){
			if(type == ActorType.DREAM_WARRIOR_CLONE){
				if(group != null && group.Count > 0){
					foreach(Actor a in group){
						if(a.type == ActorType.DREAM_WARRIOR){
							return a.WoundStatus();
						}
					}
				}
			}
			if(type == ActorType.DREAM_SPRITE_CLONE){
				if(group != null && group.Count > 0){
					foreach(Actor a in group){
						if(a.type == ActorType.DREAM_SPRITE){
							return a.WoundStatus();
						}
					}
				}
			}
			int percentage = (curhp * 100) / maxhp;
			if(percentage == 100){
				return "(unhurt)";
			}
			else{
				if(percentage > 90){
					return "(scratched)";
				}
				else{
					if(percentage > 70){
						return "(slightly damaged)";
					}
					else{
						if(percentage > 50){
							return "(somewhat damaged)";
						}
						else{
							if(percentage > 30){
								return "(heavily damaged)";
							}
							else{
								if(percentage > 10){
									return "(extremely damaged)";
								}
								else{
									if(HasAttr(AttrType.NONLIVING)){
										return "(almost destroyed)";
									}
									else{
										return "(almost dead)";
									}
								}
							}
						}
					}
				}
			}
		}
		public int DurationOfMagicalEffect(int original){ //intended to be used with whole turns, not "ticks"
			int diff = (original * TotalSkill(SkillType.SPIRIT)) / 20; //each point of Spirit takes off 1/20th of the duration
			int result = original - diff; //therefore, maxed Spirit cuts durations in half
			if(result < 1){
				result = 1; //no negative turncounts please
			}
			return result;
		}
		public bool CanWanderAtLevelGen(){
			switch(type){
			case ActorType.GIANT_BAT:
			case ActorType.BLOOD_MOTH:
			case ActorType.SKELETON:
			case ActorType.CARNIVOROUS_BRAMBLE:
			case ActorType.MIMIC:
			case ActorType.PHASE_SPIDER:
			case ActorType.POLTERGEIST:
			case ActorType.VAMPIRE:
			case ActorType.MARBLE_HORROR:
			case ActorType.STONE_GOLEM:
			case ActorType.LASHER_FUNGUS:
			case ActorType.PLAYER:
			case ActorType.FIRE_DRAKE:
				return false; //todo update
			default:
				return true;
			}
		}
		public bool NeverWanders(){
			switch(type){
			case ActorType.GIANT_BAT:
			case ActorType.BLOOD_MOTH:
			case ActorType.CARNIVOROUS_BRAMBLE:
			case ActorType.LASHER_FUNGUS:
			case ActorType.PHASE_SPIDER:
			case ActorType.POLTERGEIST:
			case ActorType.MARBLE_HORROR:
			case ActorType.PLAYER:
			case ActorType.FIRE_DRAKE:
				return true; //todo update
			default:
				return false;
			}
		}
		public bool AlwaysWanders(){
			switch(type){
			case ActorType.SKULKING_KILLER:
			//case ActorType.COMPY:
			case ActorType.ENTRANCER:
			case ActorType.SHADOWVEIL_DUELIST:
			case ActorType.ORC_ASSASSIN:
				return true; //todo update
			default:
				return false;
			}
		}
		/*public static int Rarity(ActorType type){
			int result = 1;
			if(((int)type)%3 == 2){
				result = 2;
			}
			if(type == ActorType.PLAYER || type == ActorType.FIRE_DRAKE
			|| type == ActorType.RAT || type == ActorType.DREAM_WARRIOR_CLONE){
				return 0;
			}
			return result;
		}*/
		/*public void UpdateRadius(int from,int to){ UpdateRadius(from,to,false); }
		public void UpdateRadius(int from,int to,bool change){
			if(from > 0){
				for(int i=row-from;i<=row+from;++i){
					for(int j=col-from;j<=col+from;++j){
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(!M.tile[i,j].opaque && (HasBresenhamLine(i,j) || M.tile[i,j].HasBresenhamLine(row,col))){
								M.tile[i,j].light_value--;
							}
						}
					}
				}
			}
			if(to > 0){
				for(int i=row-to;i<=row+to;++i){
					for(int j=col-to;j<=col+to;++j){
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(!M.tile[i,j].opaque && (HasBresenhamLine(i,j) || M.tile[i,j].HasBresenhamLine(row,col))){
								M.tile[i,j].light_value++;
							}
						}
					}
				}
			}
			if(change){
				light_radius = to;
			}
		}*/
		public void RemoveTarget(Actor a){
			if(target == a){
				target = null;
			}
		}
		public void Q0(){ //add movement event to queue, zero turns
			Q.Add(new Event(this,0));
		}
		public void Q1(){ //one turn
			Q.Add(new Event(this,100));
		}
		public void QS(){ //equal to speed
			Q.Add(new Event(this,Speed()));
		}
		public override string ToString(){ return symbol.ToString(); }
		public void Input(){
			bool skip_input = false;
			pos old_position = p;
			if(HasAttr(AttrType.DESTROYED_BY_SUNLIGHT)){
				if(M.wiz_lite || (player.HasAttr(AttrType.SHINING) && DistanceFrom(player) <= player.light_radius
									&& player.HasBresenhamLineOfSight(row,col))){
					B.Add(You("turn") + " to dust! ",this); //todo! this must check all actors within MAX_LIGHT_DISTANCE
					Kill();
					return;
				}
			}
			if(type == ActorType.MUD_TENTACLE){
				attrs[AttrType.COOLDOWN_1]--;
				if(attrs[AttrType.COOLDOWN_1] < 0){
					Kill();
					return;
				}
			}
			if(this == player && tile().Is(TileType.CHASM)){
				bool drake_on_next_level = false;
				foreach(Actor a in M.AllActors()){
					if(a.type == ActorType.FIRE_DRAKE && a.tile().Is(TileType.CHASM)){
						drake_on_next_level = true;
						break;
					}
				}
				foreach(Event e in Q.list){
					if(e.type == EventType.BOSS_ARRIVE){
						if(e.attr == AttrType.COOLDOWN_1){ //if this attr is set, it means that the drake is supposed to be on the level above you.
							drake_on_next_level = false;
						}
						else{
							drake_on_next_level = true;
						}
						break;
					}
				}
				B.Add("You fall. ");
				B.PrintAll();
				int old_resting_status = attrs[AttrType.RESTING];
				M.GenerateBossLevel(drake_on_next_level); //falling to a new level doesn't let you rest again during the boss fight
				attrs[AttrType.RESTING] = old_resting_status;
				Q0();
				return;
			}
			if(type == ActorType.FIRE_DRAKE && tile().Is(TileType.CHASM)){
				if(player.tile().type == TileType.CHASM){
					B.Add("You fall. ");
					B.PrintAll();
					int old_resting_status = player.attrs[AttrType.RESTING];
					M.GenerateBossLevel(true); //falling to a new level doesn't let you rest again during the boss fight
					player.attrs[AttrType.RESTING] = old_resting_status;
					return;
				}
				else{
					if(player.CanSee(this)){
						B.Add(the_name + " drops to the next level. ");
					}
					else{
						B.Add("You hear a crash as " + the_name + " drops to the next level. ");
					}
					Q.Add(new Event(null,null,(R.Roll(20)+50)*100,EventType.BOSS_ARRIVE,AttrType.NO_ATTR,curhp,""));
					attrs[AttrType.BOSS_MONSTER] = 0;
					Kill();
					return;
				}
			}
			if(HasAttr(AttrType.AGGRAVATING)){ //this probably wouldn't work well for anyone but the player, yet.
				foreach(Actor a in ActorsWithinDistance(12)){
					a.player_visibility_duration = -1; //todo: is this conceptually different than just making lots of noise?
					a.attrs[AttrType.PLAYER_NOTICED] = 1; //todo: if not, maybe change this.
					if(a.HasLOS(this)){
						a.target_location = tile();
					}
					else{
						a.FindPath(this);
					}
				}
			}
			if(HasAttr(AttrType.IN_COMBAT)){
				attrs[AttrType.IN_COMBAT] = 0;
				if(HasFeat(FeatType.CONVICTION)){
					GainAttrRefreshDuration(AttrType.CONVICTION,Math.Max(Speed(),100));
					attrs[AttrType.BONUS_SPIRIT]++;
					if(attrs[AttrType.CONVICTION] % 2 == 0){
						attrs[AttrType.BONUS_COMBAT]++;
					}
				}
			}
			else{
				if(HasAttr(AttrType.MAGICAL_DROWSINESS) && !HasAttr(AttrType.ASLEEP) && R.OneIn(4) && time_of_last_action < Q.turn){
					B.Add(You("fall") + " asleep. ",this);
					int duration = 4 + R.Roll(2);
					attrs[AttrType.ASLEEP] = DurationOfMagicalEffect(duration);
				}
			}
			if(HasAttr(AttrType.TELEPORTING) && time_of_last_action < Q.turn){
				attrs[AttrType.TELEPORTING]--;
				if(!HasAttr(AttrType.TELEPORTING)){
					for(int i=0;i<9999;++i){
						int rr = R.Roll(1,Global.ROWS-2);
						int rc = R.Roll(1,Global.COLS-2);
						if(M.BoundsCheck(rr,rc) && M.tile[rr,rc].passable && M.actor[rr,rc] == null){
							if(type == ActorType.PLAYER){
								B.Add("You are suddenly somewhere else. ");
								Interrupt();
								Move(rr,rc);
							}
							else{
								bool seen = false;
								if(player.CanSee(this)){
									seen = true;
								}
								if(player.CanSee(tile())){
									B.Add(the_name + " suddenly disappears. ",this);
								}
								Move(rr,rc);
								if(player.CanSee(tile())){
									if(seen){
										B.Add(the_name + " reappears. ",this);
									}
									else{
										B.Add(a_name + " suddenly appears! ",this);
									}
								}
							}
							break;
						}
					}
					attrs[AttrType.TELEPORTING] = R.Roll(2,10) + 5;
				}
			}
			if(HasAttr(AttrType.ASLEEP)){
				attrs[AttrType.ASLEEP]--;
				Global.FlushInput();
				if(!HasAttr(AttrType.ASLEEP)){
					B.Add(You("wake") + " up. ",this);
				}
				if(type != ActorType.PLAYER){
					if(!skip_input){
						Q1();
						skip_input = true;
					}
				}
			}
			if(HasAttr(AttrType.PARALYZED)){
				attrs[AttrType.PARALYZED]--;
				if(type == ActorType.PLAYER){
					//B.AddDependingOnLastPartialMessage("You can't move! ");
					if(!HasAttr(AttrType.PARALYZED)){
						B.Add("You can move again. ");
					}
				}
				else{ //handled differently for the player: since the map still needs to be drawn,
					if(HasAttr(AttrType.PARALYZED)){
						if(attrs[AttrType.PARALYZED] == 1){
							B.Add(the_name + " can move again. ",this);
						}
						/*else{
							B.Add(the_name + " can't move! ",this);
						}*/
						if(!skip_input){
							Q1();						// this is handled in InputHuman().
							skip_input = true; //the message is still printed, of course.
						}
					}
				}
			}
			if(HasAttr(AttrType.AMNESIA_STUN)){
				attrs[AttrType.AMNESIA_STUN] = 0;
				if(!skip_input){
					Q1();
					skip_input = true;
				}
			}
			if(HasAttr(AttrType.FROZEN)){
				if(type != ActorType.PLAYER){
					int damage = R.Roll(attack[type].WhereGreatest(x=>x.damage.dice)[0].damage.dice,6) + TotalSkill(SkillType.COMBAT);
					//int damage = R.Roll(AttackList.Attack(type,0).damage.dice,6) + TotalSkill(SkillType.COMBAT);
					attrs[AttrType.FROZEN] -= damage;
					if(attrs[AttrType.FROZEN] < 0){
						attrs[AttrType.FROZEN] = 0;
					}
					if(HasAttr(AttrType.FROZEN)){
						B.Add(the_name + " attempts to break free. ",this);
					}
					else{
						B.Add(the_name + " breaks free! ",this);
					}
					if(!skip_input){
						Q1();
						skip_input = true;
					}
				}
			}
			if(curhp < maxhp - attrs[AttrType.PERMANENT_DAMAGE] && !HasAttr(AttrType.NONLIVING)){
				if(HasAttr(AttrType.REGENERATING) && time_of_last_action < Q.turn){
					int recovered = attrs[AttrType.REGENERATING];
					if(curhp + recovered > maxhp - attrs[AttrType.PERMANENT_DAMAGE]){
						recovered = (maxhp - attrs[AttrType.PERMANENT_DAMAGE]) - curhp;
					}
					curhp += recovered;
					if(curhp > maxhp){
						curhp = maxhp;
					}
					B.Add(You("regenerate") + ". ",this);
					if(type == ActorType.TROLL_BLOODWITCH){
						if(player.CanSee(this) && player.HasLOS(this)){
							List<pos> cells = new List<pos>();
							List<colorchar> cch = new List<colorchar>();
							foreach(pos p2 in PositionsWithinDistance(6,true)){
								if(M.actor[p2] != null && HasLOE(M.actor[p2]) && player.CanSee(M.actor[p2])){
									cells.Add(p2);
									colorchar ch = new colorchar(M.actor[p2].symbol,Color.DarkRed);
									if(M.actor[p2].color == Color.DarkRed){
										ch.color = Color.Red;
									}
									cch.Add(ch);
								}
							}
							M.Draw();
							Screen.AnimateMapCells(cells,cch,150);
						}
						foreach(Actor a in ActorsWithinDistance(6,true)){
							if(HasLOE(a)){
								a.TakeDamage(DamageType.NORMAL,DamageClass.MAGICAL,recovered,this,"trollish blood magic");
							}
						}
					}
				}
				else{
					if(HasFeat(FeatType.ENDURING_SOUL) && recover_time <= Q.turn && curhp % 10 != 0){
						//if(HasAttr(AttrType.MAGICAL_BLOOD)){ // todo
						if(false){
							recover_time = Q.turn + 100;
						}
						else{
							recover_time = Q.turn + 500;
						}
						curhp++;
					}
				}
					
			}
			if(tile().Is(FeatureType.POISON_GAS) && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.NONLIVING) && !HasAttr(AttrType.PLANTLIKE) && type != ActorType.NOXIOUS_WORM){
					if(!HasAttr(AttrType.POISONED) && this == player){
						B.Add("Poisonous fumes fill your lungs! ");
					}
					string msg = "";
					if(this == player){
						msg = "You are no longer poisoned. ";
					}
					RefreshDuration(AttrType.POISONED,300,msg,this);
				}
			}
			if(HasAttr(AttrType.POISONED) && time_of_last_action < Q.turn){
				if(!TakeDamage(DamageType.POISON,DamageClass.NO_TYPE,R.Roll(3)-1,null,"*succumbed to poison")){
					return;
				}
			}
			if(HasAttr(AttrType.BURNING) && time_of_last_action < Q.turn){
				if(player.HasLOS(this)){
					B.Add(YouAre() + " on fire! ",this);
				}
				int damage = R.Roll(6);
				if(magic_trinkets.Contains(MagicTrinketType.RING_OF_THE_LETHARGIC_FLAME)){
					damage = 1;
				}
				if(!TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,damage,null,"*burned to death")){
					return;
				}
			}
			if(EquippedArmor != null && EquippedArmor.status[EquipmentStatus.INFESTED] && !HasAttr(AttrType.RESTING) && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.JUST_BITTEN)){
					B.Add("From within your " + EquippedArmor.NameWithoutEnchantment() + " you feel dozens of insect bites! ");
				}
				else{
					B.Add("Dozens of insects bite you! ");
				}
				if(!TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,1,null,"an insect infestation")){
					return;
				}
				else{
					RefreshDuration(AttrType.JUST_BITTEN,300);
				}
			}
			if(tile().Is(FeatureType.PIXIE_DUST) && maxmp > 0 && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.EMPOWERED_SPELLS)){
					B.Add("The pixie dust empowers " + the_name + ". ",this);
				}
				if(curmp < maxmp){
					curmp++;
				}
				RefreshDuration(AttrType.EMPOWERED_SPELLS,R.Between(4,7)*100,Your() + " spells are no longer empowered. ",this);
			}
			if(HasAttr(AttrType.LIGHT_SENSITIVE) && tile().IsLit() && time_of_last_action < Q.turn){
				if(!HasAttr(AttrType.VULNERABLE)){
					B.Add("The light weakens " + the_name + ". ",this);
				}
				if(type == ActorType.PLAYER){
					RefreshDuration(AttrType.VULNERABLE,R.Between(5,9)*100,"You shake off the memory of the harsh light. ");
				}
				else{
					RefreshDuration(AttrType.VULNERABLE,R.Between(5,9)*100);
				}
			}
			if(!skip_input){
				if(type==ActorType.PLAYER){
					InputHuman();
				}
				else{
					InputAI();
				}
			}
			if(HasAttr(AttrType.STEALTHY)){ //monsters only
				if((player.IsWithinSightRangeOf(row,col) || M.tile[row,col].IsLit()) && player.HasLOS(row,col)){
					if(IsHiddenFrom(player)){  //if they're stealthed and near the player...
						if(TotalSkill(SkillType.STEALTH) * DistanceFrom(player) * 10 - attrs[AttrType.TURNS_VISIBLE]++*5 < R.Roll(1,100)){
							attrs[AttrType.TURNS_VISIBLE] = -1;
							if(DistanceFrom(player) > 3){
								B.Add("You notice " + a_name + ". ");
							}
							else{
								B.Add("You notice " + a_name + " nearby. ");
							}
						}
					}
					else{
						attrs[AttrType.TURNS_VISIBLE] = -1;
					}
				}
				else{
					if(attrs[AttrType.TURNS_VISIBLE] >= 0){ //if they hadn't been seen yet...
						attrs[AttrType.TURNS_VISIBLE] = 0;
					}
					else{
						if(attrs[AttrType.TURNS_VISIBLE]-- == -10){ //check this value for balance
							attrs[AttrType.TURNS_VISIBLE] = 0;
						}
					}
				}
			}
			if(HasAttr(AttrType.CATCHING_FIRE) && time_of_last_action < Q.turn){
				if(R.OneIn(3)){
					attrs[AttrType.CATCHING_FIRE] = 0;
					if(!HasAttr(AttrType.BURNING)){
						if(light_radius == 0){
							UpdateRadius(0,1);
						}
						//attrs[AttrType.BURNING] = 1;
						RefreshDuration(AttrType.BURNING,(R.Roll(3)+4) * 100,YouAre() + " no longer on fire. ",this);
						Help.TutorialTip(TutorialTopic.Fire);
					}
				}
			}
			if(HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){ //this hack is necessary because of
				if(!HasAttr(AttrType.CATCHING_FIRE)){ //  the timing involved - 
					attrs[AttrType.CATCHING_FIRE] = 1;	// anything that catches fire on its own turn would immediately be on fire.
				}
				attrs[AttrType.STARTED_CATCHING_FIRE_THIS_TURN] = 0;
			}
			if(HasAttr(AttrType.ARCANE_SHIELDED)){
				attrs[AttrType.ARCANE_SHIELDED]--;
				if(!HasAttr(AttrType.ARCANE_SHIELDED)){
					B.Add(Your() + " shield fades. ",this);
				}
			}
			if(old_position.row == row && old_position.col == col){
				attrs[AttrType.TURNS_HERE]++;
			}
			else{
				attrs[AttrType.TURNS_HERE] = 0;
			}
			time_of_last_action = Q.turn; //this might eventually need a slight rework for 0-time turns
		}
		public static char ConvertInput(ConsoleKeyInfo k){
			switch(k.Key){
			case ConsoleKey.UpArrow: //notes: the existing design necessitated that I choose characters to assign to the toprow numbers.
			case ConsoleKey.NumPad8: //Not being able to think of anything better, I went with '!' through ')' ...
				return '8';
			case ConsoleKey.D8: // (perhaps I'll redesign if needed)
				return '*';
			case ConsoleKey.DownArrow:
			case ConsoleKey.NumPad2:
				return '2';
			case ConsoleKey.D2:
				return '@';
			case ConsoleKey.LeftArrow:
			case ConsoleKey.NumPad4:
				return '4';
			case ConsoleKey.D4:
				return '$';
			case ConsoleKey.Clear:
			case ConsoleKey.NumPad5:
				return '5';
			case ConsoleKey.D5:
				return '%';
			case ConsoleKey.RightArrow:
			case ConsoleKey.NumPad6:
				return '6';
			case ConsoleKey.D6:
				return '^';
			case ConsoleKey.Home:
			case ConsoleKey.NumPad7:
				return '7';
			case ConsoleKey.D7:
				return '&';
			case ConsoleKey.PageUp:
			case ConsoleKey.NumPad9:
				return '9';
			case ConsoleKey.D9:
				return '(';
			case ConsoleKey.End:
			case ConsoleKey.NumPad1:
				return '1';
			case ConsoleKey.D1:
				return '!';
			case ConsoleKey.PageDown:
			case ConsoleKey.NumPad3:
				return '3';
			case ConsoleKey.D3:
				return '#';
			case ConsoleKey.D0:
				return ')';
			case ConsoleKey.Tab:
				return (char)9;
			case ConsoleKey.Escape:
				return (char)27;
			case ConsoleKey.Enter:
				return (char)13;
			default:
				if((k.Modifiers & ConsoleModifiers.Shift)==ConsoleModifiers.Shift){
					return Char.ToUpper(k.KeyChar);
				}
				else{
					return k.KeyChar;
				}
			}
		}
		public static char ConvertVIKeys(char ch){
			switch(ch){
			case 'h':
			case 'H':
				return '4';
			case 'j':
			case 'J':
				return '2';
			case 'k':
			case 'K':
				return '8';
			case 'l':
			case 'L':
				return '6';
			case 'y':
			case 'Y':
				return '7';
			case 'u':
			case 'U':
				return '9';
			case 'b':
			case 'B':
				return '1';
			case 'n':
			case 'N':
				return '3';
			default:
				return ch;
			}
		}
		public void InputHuman(){
			if(HasAttr(AttrType.DETECTING_MOVEMENT) && footsteps.Count > 0 && time_of_last_action < Q.turn){
				Console.CursorVisible = false;
				Screen.AnimateMapCells(footsteps,new colorchar('!',Color.Red));
				previous_footsteps = footsteps;
				footsteps = new List<pos>();
			}
			DisplayStats(true);
			if(HasFeat(FeatType.DANGER_SENSE)){
				M.UpdateDangerValues();
			}
			M.Draw();
			if(HasAttr(AttrType.AUTOEXPLORE)){
				if(path.Count == 0){
					if(!FindAutoexplorePath()){
						B.Add("You don't see a path for further exploration. ");
					}
				}
			}
			if(!HasAttr(AttrType.PARALYZED) && !HasAttr(AttrType.FROZEN) && !HasAttr(AttrType.ASLEEP)){
				B.Print(false);
			}
			else{
				B.DisplayNow();
			}
			Cursor();
			Console.CursorVisible = true;
			if(HasAttr(AttrType.PARALYZED) || HasAttr(AttrType.ASLEEP)){
				if(HasAttr(AttrType.ASLEEP)){
					Thread.Sleep(100); //todo: any changes here?
				}
				Q1();
				return;
			}
			if(HasAttr(AttrType.FROZEN)){
				if(HasAttr(AttrType.BRUTISH_STRENGTH)){
					attrs[AttrType.FROZEN] = 0;
					B.Add("You smash through the ice! ");
					B.Print(false);
				}
				else{
					int damage = R.Roll(EquippedWeapon.Attack().damage.dice,6) + TotalSkill(SkillType.COMBAT);
					attrs[AttrType.FROZEN] -= damage;
					if(attrs[AttrType.FROZEN] < 0){
						attrs[AttrType.FROZEN] = 0;
					}
					if(HasAttr(AttrType.FROZEN)){
						B.Add("You attempt to break free. ");
					}
					else{
						B.Add("You break free! ");
					}
					Q1();
					return;
				}
			}
			if(Global.Option(OptionType.AUTOPICKUP) && tile().inv != null && !tile().inv.ignored && !tile().Is(FeatureType.QUICKFIRE)){ //todo
				bool grenade = false;
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.Is(FeatureType.GRENADE)){
						grenade = true;
					}
				}
				if(!grenade && !HasAttr(AttrType.BURNING) && !HasAttr(AttrType.CATCHING_FIRE)){
					bool monster = false;
					foreach(Actor a in M.AllActors()){
						if(a != this && CanSee(a)){
							monster = true;
							break;
						}
					}
					if(!monster){
						if(StunnedThisTurn()){
							return;
						}
						Item i = tile().inv;
						i.row = -1;
						i.col = -1;
						tile().inv = null;
						B.Add("You pick up " + i.TheName() + ". ");
						GetItem(i);
						/*bool added = false;
						foreach(Item item in inv){
							if(item.type == i.type && !item.do_not_stack && !i.do_not_stack){
								item.quantity += i.quantity;
								added = true;
								break;
							}
						}
						if(!added){
							inv.Add(i);
						}*/
						Q1();
						return;
					}
				}
			}
			if(path.Count > 0){
				bool monsters_visible = false;
				foreach(Actor a in M.AllActors()){
					if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){ //check LOS, prevents detected mobs from stopping you
						monsters_visible = true;
					}
				}
				if(!monsters_visible){
					if(Console.KeyAvailable){
						Console.ReadKey(true);
						Interrupt();
					}
					else{
						//AI_Step(M.tile[path[0]]);
						PlayerWalk(DirectionOf(path[0]));
						if(path.Count > 0){
							if(DistanceFrom(path[0]) == 0){
								path.RemoveAt(0);
							}
						}
						//QS();
						return;
					}
				}
				else{
					Interrupt();
				}
			}
			if(HasAttr(AttrType.RUNNING)){
				bool monsters_visible = false;
				foreach(Actor a in M.AllActors()){
					if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){ //check LOS, prevents detected mobs from stopping you
						monsters_visible = true;
					}
				}
				Tile t = TileInDirection(attrs[AttrType.RUNNING]);
				bool stopped_by_terrain = false;
				if(t.IsKnownTrap() || (t.inv != null && t.inv.type == ConsumableType.BLAST_FUNGUS)/*t.Is(FeatureType.FUNGUS_ACTIVE) || t.Is(FeatureType.FUNGUS_PRIMED)*/
				|| t.Is(FeatureType.GRENADE,FeatureType.POISON_GAS,FeatureType.QUICKFIRE)){
					stopped_by_terrain = true; //todo! check this for new terrain.
				}
				if(!monsters_visible && !stopped_by_terrain && !Console.KeyAvailable){
					if(attrs[AttrType.RUNNING] == 5){
						int hplimit = HasFeat(FeatType.ENDURING_SOUL)? 20 : 10;
						if(curhp % hplimit == 0){
							if(HasAttr(AttrType.WAITING)){
								attrs[AttrType.WAITING]--;
								Q1();
								return;
							}
							else{
								attrs[AttrType.RUNNING] = 0;
							}
						}
						else{
							Q1();
							return;
						}
					}
					else{
						bool corridor = true;
						foreach(int dir in U.FourDirections){
							if(TileInDirection(dir).passable && TileInDirection(dir.RotateDir(true,1)).passable && TileInDirection(dir.RotateDir(true,2)).passable){
								corridor = false;
								break;
							}
						}
						List<Tile> tiles = new List<Tile>();
						if(corridor){
							List<int> blocked = new List<int>();
							for(int i=-1;i<=1;++i){
								blocked.Add(attrs[AttrType.RUNNING].RotateDir(true,4+i));
							}
							tiles = TilesAtDistance(1).Where(x=>(x.passable || x.Is(TileType.DOOR_C,TileType.RUBBLE)) && ApproximateEuclideanDistanceFromX10(x) == 10 && !blocked.Contains(DirectionOf(x)));
						}
						if(!corridor && t.passable){
							PlayerWalk(attrs[AttrType.RUNNING]);
							return;
						}
						else{
							if(corridor && tiles.Count == 1){
								attrs[AttrType.RUNNING] = DirectionOf(tiles[0]);
								PlayerWalk(attrs[AttrType.RUNNING]);
								foreach(int dir in U.FourDirections){ //now check again to see whether the player has entered a room
									if(TileInDirection(dir).passable && TileInDirection(dir.RotateDir(true,1)).passable && TileInDirection(dir.RotateDir(true,2)).passable){
										corridor = false;
										break;
									}
								}
								if(!corridor){
									attrs[AttrType.RUNNING] = 0;
									attrs[AttrType.WAITING] = 0;
								}
								return;
							}
							else{
								attrs[AttrType.RUNNING] = 0;
								attrs[AttrType.WAITING] = 0;
							}
							/*Tile opposite = TileInDirection(attrs[AttrType.RUNNING].RotateDir(true,4));
							int num_floors = 0;
							int floor_dir = 0;
							foreach(Tile t2 in TilesAtDistance(1)){
								//if(t2 != opposite && t2.name == "floor"){
								if(t2 != opposite && (t2.passable || t2.type == TileType.DOOR_C)){
									num_floors++;
									floor_dir = DirectionOf(t2);
								}
							}
							if(num_floors == 1){
								attrs[AttrType.RUNNING] = floor_dir;//the purpose of this code is to detect whether there's a valid turn to make
								PlayerWalk(floor_dir); //and take it if so. if there's a branch, it should stop.
								return;
							}
							else{
								attrs[AttrType.RUNNING] = 0;
								attrs[AttrType.WAITING] = 0;
							}*/
						}
					}
				}
				else{
					if(Console.KeyAvailable){
						Console.ReadKey(true);
					}
					attrs[AttrType.RUNNING] = 0;
					attrs[AttrType.WAITING] = 0;
				}
			}
			if(HasAttr(AttrType.RESTING)){
				if(attrs[AttrType.RESTING] == 10){
					attrs[AttrType.RESTING] = -1;
					curhp = maxhp;
					exhaustion = 0;
					curmp = maxmp;
					B.Add("You rest...you feel great! "); //todo: message if you can now wear your armor again. //is this necessary?
					bool repaired = false;
					foreach(EquipmentStatus eqstatus in Enum.GetValues(typeof(EquipmentStatus))){
						foreach(Weapon w in weapons){
							if(w.status[eqstatus]){
								repaired = true;
								w.status[eqstatus] = false;
							}
						}
						foreach(Armor a in armors){
							if(a.status[eqstatus]){
								repaired = true;
								a.status[eqstatus] = false;
							}
						}
					}
					if(repaired){
						B.Add("You finish repairing your equipment. ");
					}
					if(magic_trinkets.Contains(MagicTrinketType.CIRCLET_OF_THE_THIRD_EYE)){
						List<pos> floors_seen = M.tile.GetFloodFillPositions(this.p,false,x=>M.tile[x].seen && (M.tile[x].passable || M.tile[x].Is(TileType.DOOR_C,TileType.STONE_SLAB)));
						for(int i=0;i<600 && floors_seen.Count > 0;++i){
							pos p = floors_seen.RemoveRandom();
							List<pos> valid = new List<pos>();
							foreach(pos neighbor in p.PositionsAtDistance(1)){
								//if(!M.tile[neighbor].seen && (M.tile[neighbor].passable || M.tile[neighbor].Is(TileType.DOOR_C,TileType.STONE_SLAB))){
								if(!M.tile[neighbor].seen && !M.tile[neighbor].solid_rock){
									valid.Add(neighbor);
								}
							}
							if(valid.Count > 0){
								pos p2 = valid.Random();
								floors_seen.Add(p2);
								Tile t = M.tile[p2];
								t.seen = true;
								Event hiddencheck = null;
								foreach(Event e in Q.list){
									if(!e.dead && e.type == EventType.CHECK_FOR_HIDDEN){
										hiddencheck = e;
										break;
									}
								}
								if(t.type != TileType.WALL){
									t.revealed_by_light = true;
								}
								if(t.IsTrapOrVent() || t.Is(TileType.HIDDEN_DOOR)){
									if(hiddencheck != null){
										hiddencheck.area.Remove(t);
									}
								}
								if(t.IsTrapOrVent()){
									t.name = Tile.Prototype(t.type).name;
									t.a_name = Tile.Prototype(t.type).a_name;
									t.the_name = Tile.Prototype(t.type).the_name;
									t.symbol = Tile.Prototype(t.type).symbol;
									t.color = Tile.Prototype(t.type).color;
								}
								if(t.Is(TileType.HIDDEN_DOOR)){
									t.Toggle(null);
								}
							}
							else{
								--i;
							}
						}
						M.Draw(); //todo: make sure, if there's an item on a revealed tile, that autoexplore won't skip it!
						B.Add("Your " + MagicTrinket.Name(MagicTrinketType.CIRCLET_OF_THE_THIRD_EYE) + " grants you a vision. ");
					}
					B.Print(false);
					DisplayStats(true);
					Cursor();
				}
				else{
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){ //check LOS, prevents detected mobs from stopping you
							monsters_visible = true;
						}
					}
					if(monsters_visible || Console.KeyAvailable){
						if(Console.KeyAvailable){
							Console.ReadKey(true);
						}
						if(monsters_visible){
							attrs[AttrType.RESTING] = 0;
							B.Add("You rest...you are interrupted! ");
							B.Print(false);
							Cursor();
						}
						else{
							attrs[AttrType.RESTING] = 0;
							B.Add("You rest...you stop resting. ");
							B.Print(false);
							Cursor();
						}
					}
					else{
						attrs[AttrType.RESTING]++;
						B.Add("You rest... ");
						Q1();
						return;
					}
				}
			}
			if(Q.turn == 0){
				Help.TutorialTip(TutorialTopic.Movement);
				Cursor();
			}
			if(!Help.displayed[TutorialTopic.Attacking] && M.AllActors().Any(a=>(a != this && CanSee(a)))){
				Help.TutorialTip(TutorialTopic.Attacking);
				Cursor();
			}
			ConsoleKeyInfo command = Console.ReadKey(true);
			/*ConsoleKeyInfo command;
			bool command_entered = false;
			while(!command_entered){
				if(Console.KeyAvailable){
					command = Console.ReadKey(true);
					command_entered = true;
					break;
				}
				Screen.AnimateCellNonBlocking(row+Global.MAP_OFFSET_ROWS,col+Global.MAP_OFFSET_COLS,new colorchar('@',Color.DarkGray),200);
				if(Console.KeyAvailable){
					command = Console.ReadKey(true);
					command_entered = true;
					break;
				}
				Screen.AnimateCellNonBlocking(row+Global.MAP_OFFSET_ROWS,col+Global.MAP_OFFSET_COLS,new colorchar('@',Color.DarkRed),200);
			}*/
			char ch = ConvertInput(command);
			ch = ConvertVIKeys(ch);
			bool alt = false;
			bool ctrl = false;
			bool shift = false;
			if((command.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt){
				alt = true;
			}
			if((command.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control){
				ctrl = true;
			}
			if((command.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift){
				shift = true;
			}
			switch(ch){
			case '7':
			case '8':
			case '9':
			case '4':
			case '6':
			case '1':
			case '2':
			case '3':
				{
				int dir = ch - 48; //ascii 0-9 are 48-57
				if(shift || alt || ctrl){
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){
							monsters_visible = true;
						}
					}
					PlayerWalk(dir);
					if(!monsters_visible){
						attrs[AttrType.RUNNING] = dir;
					}
				}
				else{
					PlayerWalk(dir);
				}
				break;
				}
			case '5':
			case '.':
				if(HasAttr(AttrType.CATCHING_FIRE)){
					attrs[AttrType.CATCHING_FIRE] = 0;
					B.Add("You stop the flames from spreading. ");
				}
				/*else{
					if(HasAttr(AttrType.BURNING)){
						bool update = false;
						int oldradius = LightRadius();
						if(attrs[AttrType.BURNING] > light_radius){
							update = true;
						}
						int i = 2;
						if(R.Roll(1,3) == 3){ // 1 in 3 times, you don't make progress against the fire
							i = 1;
						}
						attrs[AttrType.BURNING] -= i;
						if(attrs[AttrType.BURNING] < 0){
							attrs[AttrType.BURNING] = 0;
						}
						if(update){
							UpdateRadius(oldradius,LightRadius());
						}
						if(HasAttr(AttrType.BURNING)){
							B.Add("You put out some of the fire. "); //better message?
						}
						else{
							B.Add("You put out the fire. ");
						}
					}
				}*/
				if(M.tile[row,col].inv != null){
					B.Add("You see " + M.tile[row,col].inv.AName() + ". ");
				}
				QS();
				break;
			case 'w':
				{
				int dir = GetDirection("Start walking in which direction? ",false,true);
				if(dir != 0){
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){
							monsters_visible = true;
						}
					}
					if(dir != 5){
						PlayerWalk(dir);
					}
					else{
						Q1();
					}
					if(!monsters_visible){
						attrs[AttrType.RUNNING] = dir;
						int hplimit = HasFeat(FeatType.ENDURING_SOUL)? 20 : 10;
						if(curhp % hplimit == 0 && dir == 5){
							attrs[AttrType.WAITING] = 20;
						}
					}
				}
				else{
					Q0();
				}
				break;
				}
			case 'o':
				{
				int dir = 0;
				int total = 0;
				foreach(Tile t in TilesAtDistance(1)){
					if(t.type == TileType.DOOR_C || t.type == TileType.DOOR_O || t.type == TileType.RUBBLE
					|| (HasFeat(FeatType.DISARM_TRAP) && t.IsKnownTrap())){
						if(t.actor() == null && (t.inv == null || t.IsTrap())){
							dir = DirectionOf(t);
							++total;
						}
					}
				}
				if(total == 1){
					Tile t = TileInDirection(dir);
					if(t.type == TileType.DOOR_C || t.type == TileType.DOOR_O || t.type == TileType.RUBBLE){
						if(StunnedThisTurn()){
							return;
						}
						t.Toggle(this);
						Q1();
					}
					else{
						if(t.IsTrap()){
							if(GrabPreventsMovement(t)){
								B.Add("You can't currently reach that trap. ");
								Q0();
								return;
							}
							else{
								if(StunnedThisTurn()){
									return;
								}
								if(R.Roll(5) <= 4){
									B.Add("You disarm " + Tile.Prototype(t.type).the_name + ". ");
									t.Toggle(this);
									Q1();
								}
								else{
									if(R.Roll(20) <= skills[Forays.SkillType.DEFENSE]){
										B.Add("You almost set off " + Tile.Prototype(t.type).the_name + "! ");
										Q1();
									}
									else{
										B.Add("You set off " + Tile.Prototype(t.type).the_name + "! ");
										Move(t.row,t.col);
										Q1();
									}
								}
							}
						}
						else{
							Q0(); //shouldn't happen
						}
					}
				}
				else{
					dir = GetDirection("Operate something in which direction? ");
					if(dir != -1){
						Tile t = TileInDirection(dir);
						if(t.IsKnownTrap()){
							if(HasFeat(FeatType.DISARM_TRAP)){
								if(GrabPreventsMovement(t)){
									B.Add("You can't currently reach that trap. ");
									Q0();
									return;
								}
								if(StunnedThisTurn()){
									return;
								}
								if(R.Roll(5) <= 4){
									B.Add("You disarm " + Tile.Prototype(t.type).the_name + ". ");
									t.Toggle(this);
									Q1();
								}
								else{
									if(R.Roll(20) <= skills[Forays.SkillType.DEFENSE]){
										B.Add("You almost set off " + Tile.Prototype(t.type).the_name + "! ");
										Q1();
									}
									else{
										B.Add("You set off " + Tile.Prototype(t.type).the_name + "! ");
										Move(t.row,t.col);
										Q1();
									}
								}
							}
							else{
								B.Add("You don't know how to disable that trap. ");
								Q0();
								return;
							}
						}
						else{
							switch(t.type){
							case TileType.DOOR_C:
							case TileType.DOOR_O:
							case TileType.RUBBLE:
								if(StunnedThisTurn()){
									break;
								}
								t.Toggle(this);
								Q1();
								break;
							case TileType.CHEST:
								B.Add("Stand on the chest and press 'g' to retrieve its contents. ");
								Q0();
								break;
							case TileType.STAIRS:
								B.Add("Stand on the stairs and press '>' to descend. ");
								Q0();
								break;
							default:
								Q0();
								break;
							}
						}
					}
					else{
						Q0();
					}
				}
				break;
				}
			case 's':
			{
				if(EquippedWeapon.type == WeaponType.BOW || HasFeat(FeatType.QUICK_DRAW)){
					if(ActorsAtDistance(1).Count > 0){
						if(ActorsAtDistance(1).Count == 1){
							B.Add("You can't fire with an enemy so close. ");
						}
						else{
							B.Add("You can't fire with enemies so close. ");
						}
						Q0();
					}
					else{
						List<Tile> line = GetTargetLine(12);
						if(line != null && line.Last() != tile()){
							FireArrow(line);
						}
						else{
							Q0();
						}
					}
				}
				else{
					B.Add("You can't fire arrows without your bow equipped. ");
					Q0();
				}
				break;
			}
				/*{
				List<FeatType> active_feats = new List<FeatType>();
				List<FeatType> passive_feats = new List<FeatType>();
				foreach(FeatType ft in feats_in_order){
					if(Feat.IsActivated(ft)){
						active_feats.Add(ft);
					}
					else{
						passive_feats.Add(ft);
					}
				}
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				int line = 1;
				if(active_feats.Count > 0){
					Screen.WriteMapString(line,0,"Active feats:".PadToMapSize());
					++line;
					char letter = 'a';
					foreach(FeatType ft in active_feats){
						string s = "[" + letter + "] " + Feat.Name(ft);
						Screen.WriteMapString(line,0,s.PadToMapSize());
						Screen.WriteMapChar(line,1,letter,Color.Cyan);
						++line;
						++letter;
					}
					Screen.WriteMapString(line,0,"".PadToMapSize());
					++line;
				}
				if(passive_feats.Count > 0){
					Screen.WriteMapString(line,0,"Passive feats:".PadToMapSize());
					++line;
					foreach(FeatType ft in passive_feats){
						string s = "    " + Feat.Name(ft);
						Screen.WriteMapString(line,0,s.PadToMapSize());
						++line;
					}
					Screen.WriteMapString(line,0,"".PadToMapSize());
					++line;
				}
				Screen.WriteMapString(line,0,"Feats currently being learned:".PadToMapSize());
				++line;
				if(partial_feats_in_order.Count == 0){
					Screen.WriteMapString(line,0,"    None".PadToMapSize());
					++line;
				}
				else{
					if(partial_feats_in_order.Count + line > 21){
						int extras = partial_feats_in_order.Count + line - 21;
						foreach(FeatType ft in partial_feats_in_order){
							if(line == 21){ //don't print the bottommost feats again
								break;
							}
							Screen.WriteMapString(line,0,"    " + Feat.Name(ft).PadRight(21));
							if(extras > 0){
								Screen.WriteMapString(line,25,"(" + (-feats[ft]) + "/" + Feat.MaxRank(ft) + ")".PadRight(7));
								FeatType ft2 = partial_feats_in_order[partial_feats_in_order.Count - extras];
								Screen.WriteMapString(line,36,Feat.Name(ft2).PadRight(21));
								Screen.WriteMapString(line,57,"(" + (-feats[ft2]) + "/" + Feat.MaxRank(ft2) + ")".PadRight(6));
								++line;
								--extras;
							}
							else{
								Screen.WriteMapString(line,25,"(" + (-feats[ft]) + "/" + Feat.MaxRank(ft) + ")".PadRight(37));
								++line;
							}
						}
					}
					else{
						foreach(FeatType ft in partial_feats_in_order){
							Screen.WriteMapString(line,0,"    " + Feat.Name(ft).PadRight(21));
							Screen.WriteMapString(line,25,"(" + (-feats[ft]) + "/" + Feat.MaxRank(ft) + ")".PadRight(37));
							++line;
						}
					}
				}
				Screen.WriteMapString(line,0,("".PadRight(25,'-') + "[?] for help").PadRight(COLS,'-'));
				Screen.WriteMapChar(line,26,new colorchar(Color.Cyan,'?'));
				++line;
				if(line <= 21){
					Screen.WriteMapString(line,0,"".PadToMapSize());
				}
				Screen.ResetColors();
				if(active_feats.Count > 0){
					B.DisplayNow("Use which feat? ");
				}
				else{
					B.DisplayNow("Feats: ");
				}
				Console.CursorVisible = true;
				FeatType selected_feat = FeatType.NO_FEAT;
				bool done = false;
				while(!done){
					command = Console.ReadKey(true);
					ch = ConvertInput(command);
					int ii = ch - 'a';
					if(active_feats.Count > ii && ii >= 0){
						selected_feat = active_feats[ii];
						done = true;
					}
					else{
						if(ch == '?'){
							Help.DisplayHelp(HelpTopic.Feats);
							done = true;
						}
						else{
							done = true;
						}
					}
				}
				M.RedrawWithStrings();
				if(selected_feat != FeatType.NO_FEAT){
					if(StunnedThisTurn()){
						break;
					}
					if(!UseFeat(selected_feat)){
						Q0();
					}
				}
				else{
					Q0();
				}
				break;
				}*/
			case 'z':
			{
				foreach(Actor a in ActorsWithinDistance(2)){
					if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
						if(this == player){
							if(CanSee(a)){
								B.Add(a.Your() + " presence prevents you from casting! ");
							}
							else{
								B.Add("Something prevents you from casting! ");
							}
						}
						Q0();
						return;
					}
				}
				if(HasAttr(AttrType.SILENCED)){
					B.Add("You can't cast while silenced. ");
					Q0();
					return;
				}
				List<colorstring> ls = new List<colorstring>();
				List<SpellType> sp = new List<SpellType>();
				//foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
				bool bonus_marked = false;
				foreach(SpellType spell in spells_in_order){
					if(HasSpell(spell)){
						//string s = Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3);
						//s = s + FailRate(spell).ToString().PadLeft(9) + "%";
						//s = s + Spell.Description(spell).PadLeft(34);
						colorstring cs = new colorstring(Spell.Name(spell).PadRight(18) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray);
						//cs.strings.Add(new cstr(FailRate(spell).ToString().PadLeft(9) + "%",FailColor(spell)));
						cs.strings.Add(new cstr("".PadLeft(7),Color.Gray));
						if(HasFeat(FeatType.MASTERS_EDGE) && Spell.IsDamaging(spell) && !bonus_marked){
							bonus_marked = true;
							cs = cs + Spell.DescriptionWithIncreasedDamage(spell);
						}
						else{
							cs = cs + Spell.Description(spell);
						}
						ls.Add(cs);
						sp.Add(spell);
					}
				}
				if(sp.Count > 0){
					colorstring topborder = new colorstring("----------------------Tier-----------------Description------------",Color.Gray);
					int basefail = exhaustion * 5;
					colorstring bottomborder = new colorstring("----------------" + "Exhaustion: ".PadLeft(12+(3-basefail.ToString().Length),'-'),Color.Gray,(basefail.ToString() + "%"),FailColor(basefail),"----------[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(22,'-'),Color.Gray);
					//int i = Select("Cast which spell? ",topborder,bottomborder,ls);
					int i = Select("Cast which spell? ",topborder,bottomborder,ls,false,false,true,true,HelpTopic.Spells);
					if(i != -1){
						if(!CastSpell(sp[i])){
							Q0();
						}
					}
					else{
						Q0();
					}
				}
				else{
					B.Add("You don't know any spells. ");
					Q0();
				}
				break;
			}
			case 'r':
				if(attrs[AttrType.RESTING] != -1){ //gets set to -1 if you've rested on this level
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){ //check LOS, prevents detected mobs from stopping you
							monsters_visible = true;
						}
					}
					if(!monsters_visible){
						if(curhp < maxhp || curmp < maxmp || exhaustion > 0){
							if(StunnedThisTurn()){
								break;
							}
							attrs[AttrType.RESTING] = 1;
							B.Add("You rest... ");
							Q1();
						}
						else{
							B.Add("You don't need to rest right now. ");
							Q0();
						}
					}
					else{
						B.Add("You can't rest while there are enemies around! ");
						Q0();
					}
				}
				else{
					B.Add("You find it impossible to rest again on this dungeon level. ");
					Q0();
				}
				break;
			case '>':
				if(M.tile[row,col].type == TileType.STAIRS){
					if(StunnedThisTurn()){
						break;
					}
					if(attrs[AttrType.RESTING] != -1 && (curhp < maxhp || curmp < maxmp || exhaustion > 0)){
						B.DisplayNow("Really take the stairs without resting first?(y/n): ");
						Console.CursorVisible = true;
						bool done = false;
						while(!done){
							command = Console.ReadKey(true);
							switch(command.KeyChar){
							case 'y':
							case 'Y':
								done = true;
								break;
							default:
								Q0();
								return;
							}
						}
					}
					B.Add("You walk down the stairs. ");
					B.PrintAll();
					if(M.current_level < 20){
						M.GenerateLevel();
					}
					else{
						M.GenerateBossLevel(false);
						B.Add("You enter a sweltering cavern. ");
						B.Add("Bones lie scattered across the sulfurous ground. ");
					}
					if(magic_trinkets.Contains(MagicTrinketType.LENS_OF_SCRYING)){
						Item i = inv.Where(x=>!Item.identified[x.type]).Random();
						if(i != null){
							string itemname = i.NameWithoutQuantity();
							Item.identified[i.type] = true;
							string IDedname = i.NameWithoutQuantity();
							string isare = " is ";
							if(i.quantity > 1){
								isare = " are ";
							}
							B.Add("Your " + MagicTrinket.Name(MagicTrinketType.LENS_OF_SCRYING) + " reveals that your " + itemname + isare + IDedname + ". ");
						}
					}
					Q0();
				}
				else{
					Tile stairs = null;
					foreach(Tile t in M.AllTiles()){
						if(t.type == TileType.STAIRS && t.seen){
							stairs = t;
							break;
						}
					}
					if(stairs != null){
						B.DisplayNow("Travel to the stairs?(y/n): ");
						Console.CursorVisible = true;
						bool done = false;
						while(!done){
							command = Console.ReadKey(true);
							switch(command.KeyChar){
							case 'y':
							case 'Y':
							case '>':
							case (char)13:
								done = true;
								break;
							default:
								Q0();
								return;
							}
						}
						FindPath(stairs,-1,true);
						if(path.Count > 0){
							PlayerWalk(DirectionOf(path[0]));
							if(DistanceFrom(path[0]) == 0){
								path.RemoveAt(0);
							}
						}
						else{
							B.Add("There's no path to the stairs. ");
							Q0();
						}
					}
					else{
						B.Add("You don't see any stairs here. ");
						Q0();
					}
				}
				break;
			case 'x':
			{
				if(!FindAutoexplorePath()){
					B.Add("You don't see a path for further exploration. ");
					Q0();
				}
				else{
					attrs[AttrType.AUTOEXPLORE]++;
					PlayerWalk(DirectionOf(path[0]));
					if(path.Count > 0){
						if(DistanceFrom(path[0]) == 0){
							path.RemoveAt(0);
						}
					}
				}
			}
				break;
			case 'g':
			case ';':
				if(tile().inv == null){
					if(tile().type == TileType.CHEST){
						if(StunnedThisTurn()){
							break;
						}
						tile().OpenChest();
						Q1();
					}
					else{
						if(tile().IsShrine()){
							if(StunnedThisTurn()){
								break;
							}
							switch(tile().type){
							case TileType.COMBAT_SHRINE:
								IncreaseSkill(SkillType.COMBAT);
								break;
							case TileType.DEFENSE_SHRINE:
								IncreaseSkill(SkillType.DEFENSE);
								break;
							case TileType.MAGIC_SHRINE:
								IncreaseSkill(SkillType.MAGIC);
								break;
							case TileType.SPIRIT_SHRINE:
								IncreaseSkill(SkillType.SPIRIT);
								break;
							case TileType.STEALTH_SHRINE:
								IncreaseSkill(SkillType.STEALTH);
								break;
							case TileType.SPELL_EXCHANGE_SHRINE:
							{
								List<colorstring> ls = new List<colorstring>();
								List<SpellType> sp = new List<SpellType>();
								bool bonus_marked = false;
								foreach(SpellType spell in spells_in_order){
									if(HasSpell(spell)){
										colorstring cs = new colorstring(Spell.Name(spell).PadRight(18) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray);
										//cs.strings.Add(new cstr(FailRate(spell).ToString().PadLeft(9) + "%",FailColor(spell)));
										cs.strings.Add(new cstr("".PadRight(7),Color.Gray));
										if(HasFeat(FeatType.MASTERS_EDGE) && Spell.IsDamaging(spell) && !bonus_marked){
											bonus_marked = true;
											cs = cs + Spell.DescriptionWithIncreasedDamage(spell);
										}
										else{
											cs = cs + Spell.Description(spell);
										}
										ls.Add(cs);
										sp.Add(spell);
									}
								}
								if(sp.Count > 0){
									colorstring topborder = new colorstring("----------------------Tier-----------------Description------------",Color.Gray);
									int basefail = exhaustion * 5;
									colorstring bottomborder = new colorstring("----------------" + "Exhaustion: ".PadLeft(12+(3-basefail.ToString().Length),'-'),Color.Gray,(basefail.ToString() + "%"),FailColor(basefail),"----------[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(22,'-'),Color.Gray);
									int i = Select("Trade one of your spells for another? ",topborder,bottomborder,ls,false,false,true,true,HelpTopic.Spells);
									if(i != -1){
										List<SpellType> unknown = new List<SpellType>();
										foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
											if(!HasSpell(spell) && spell != SpellType.NO_SPELL && spell != SpellType.NUM_SPELLS){
												unknown.Add(spell);
											}
										}
										SpellType forgotten = sp[i];
										spells_in_order.Remove(forgotten);
										spells[forgotten] = false;
										SpellType learned = unknown.Random();
										spells[learned] = true;
										spells_in_order.Add(learned);
										B.Add("You forget " + Spell.Name(forgotten) + ". You learn " + Spell.Name(learned) + ". ");
										tile().TransformTo(TileType.RUINED_SHRINE);
									}
									else{
										Q0();
									}
								}
								break;
							}
							default:
								break;
							}
							if(tile().type != TileType.SPELL_EXCHANGE_SHRINE){
								Q1();
							}
							if(tile().type == TileType.MAGIC_SHRINE && spells_in_order.Count > 1){
								tile().TransformTo(TileType.SPELL_EXCHANGE_SHRINE);
							}
							else{
								if(tile().type != TileType.SPELL_EXCHANGE_SHRINE){
									tile().TransformTo(TileType.RUINED_SHRINE);
								}
							}
							foreach(Tile t in TilesAtDistance(2)){
								if(t.IsShrine()){
									t.TransformTo(TileType.RUINED_SHRINE);
								}
							}
						}
						else{
							if(tile().type == TileType.BLAST_FUNGUS){
								B.Add("The blast fungus is still rooted to the ground by its fuse. ");
								Q0();
							}
							else{
								B.Add("There's nothing here to pick up. ");
								Q0();
							}
						}
					}
				}
				else{
					if(StunnedThisTurn()){
						break;
					}
					if(InventoryCount() < Global.MAX_INVENTORY_SIZE){
						if(InventoryCount() + tile().inv.quantity <= Global.MAX_INVENTORY_SIZE){
							Item i = tile().inv;
							tile().inv = null;
							if(i.light_radius > 0){
								i.UpdateRadius(i.light_radius,0);
							}
							i.row = -1;
							i.col = -1;
							B.Add("You pick up " + i.TheName() + ". ");
							GetItem(i);
							/*bool added = false;
							foreach(Item item in inv){
								if(item.type == i.type && !item.do_not_stack && !i.do_not_stack){
									item.quantity += i.quantity;
									added = true;
									break;
								}
							}
							if(!added){
								inv.Add(i);
							}*/
							Q1();
						}
						else{
							int space_left = Global.MAX_INVENTORY_SIZE - InventoryCount();
							Item i = tile().inv;
							Item newitem = new Item(i,row,col);
							newitem.quantity = space_left;
							i.quantity -= space_left;
							B.Add("You pick up " + newitem.TheName() + ", but have no room for the other " + i.quantity.ToString() + ". ");
							GetItem(newitem);
							/*bool added = false;
							foreach(Item item in inv){
								if(item.type == newitem.type && !item.do_not_stack && !newitem.do_not_stack){
									item.quantity += newitem.quantity;
									added = true;
									break;
								}
							}
							if(!added){
								inv.Add(newitem);
							}*/
							Q1();
						}
					}
					else{
						B.Add("Your pack is too full to pick up " + tile().inv.TheName() + ". ");
						Q0();
					}
				}
				break;
			case 'i':
			case 'a': //these are handled in the same case label so I can drop down from 'i' to the others
			case 'f':
			case 'd':
			{
				int num = -2;
				if(ch == 'i'){
					if(inv.Count == 0){
						B.Add("You have nothing in your pack. ");
						Q0();
					}
					else{
						num = -2;
						while(num != -1){
							num = SelectItem("In your pack: ",true);
							if(num != -1){
								Console.CursorVisible = false;
								colorchar[,] screen = Screen.GetCurrentScreen();
								for(int letter=0;letter<inv.Count;++letter){
									Screen.WriteMapChar(letter+1,1,(char)(letter+'a'),Color.DarkCyan);
								}
								List<colorstring> box = ItemDescriptionBox(inv[num],false,31);
								int i = (Global.SCREEN_H - box.Count) / 2;
								int j = (Global.SCREEN_W - box[0].Length()) / 2;
								foreach(colorstring cs in box){
									Screen.WriteString(i,j,cs);
									++i;
								}
								switch(ConvertInput(Console.ReadKey(true))){
								case 'a':
									ch = 'a';
									break;
								case 'f':
									ch = 'f';
									break;
								case 'd':
									ch = 'd';
									break;
								}
								if(ch == 'i'){
									Screen.WriteArray(0,0,screen);
								}
								else{
									M.RedrawWithStrings(); //this will break if the box goes off the map, todo
									break;
								}
								Console.CursorVisible = true;
							}
						}
						if(num == -1){
							Q0();
						}
					}
				}
				switch(ch){
				case 'a':
					if(inv.Count == 0){
						B.Add("You have nothing in your pack. ");
						Q0();
					}
					else{
						if(num == -2){
							num = SelectItem("Apply which item? ");
						}
						if(num != -1){
							if(StunnedThisTurn()){
								break;
							}
							if(HasAttr(AttrType.NONLIVING) && inv[num].NameOfItemType() == "potion"){
								B.Add("Potions have no effect on you in stone form. ");
								Q0();
							}
							else{
								if(HasAttr(AttrType.SILENCED) && inv[num].NameOfItemType() == "scroll"){
									B.Add("You can't read scrolls while silenced. ");
									Q0();
								}
								else{
									if(inv[num].Use(this)){
										Q1();
									}
									else{
										Q0();
									}
								}
							}
						}
						else{
							Q0();
						}
					}
					break;
				case 'f':
					if(inv.Count == 0){
						B.Add("You have nothing in your pack. ");
						Q0();
					}
					else{
						if(num == -2){
							num = SelectItem("Throw which item? ");
						}
						if(num != -1){
							if(StunnedThisTurn()){
								break;
							}
							bool target_nearest_enemy = false;
							if(inv[num].NameOfItemType() == "orb" || inv[num].type == ConsumableType.BLAST_FUNGUS){
								target_nearest_enemy = true;
							}
							List<Tile> line = GetTarget(false,12,0,false,target_nearest_enemy);
							if(line != null){
								if(inv[num].NameOfItemType() == "orb" || inv[num].type == ConsumableType.BLAST_FUNGUS){
									inv[num].Use(this,line);
									Q1();
								}
								else{
									Item i = null;
									if(inv[num].quantity == 1){
										i = inv[num];
									}
									else{
										i = new Item(inv[num],-1,-1);
										inv[num].quantity--;
									}
									i.revealed_by_light = true;
									i.ignored = true;
									Tile t = line.LastBeforeSolidTile();
									Actor first = FirstActorInLine(line);
									B.Add(You("fling") + " " + i.TheName() + ". ");
									if(first != null && first != this){
										t = first.tile();
										B.Add("It hits " + first.the_name + ". ",first);
									}
									line = line.ToFirstObstruction();
									if(line.Count > 0){
										line.RemoveAt(line.Count - 1);
									}
									if(line.Count > 0){
										line.RemoveAt(line.Count - 1); //i forget why I needed to do this twice, but it seems to work
									}
									int idx = 0;
									foreach(Tile tile2 in line){
										if(tile2.seen){
											++idx;
										}
										else{
											line = line.To(tile2);
											if(line.Count > 0){
												line.RemoveAt(line.Count - 1);
											}
											break;
										}
									}
									if(line.Count > 0){
										AnimateProjectile(line,i.symbol,i.color);
									}
									t.GetItem(i);
									inv.Remove(i);
									t.MakeNoise(2);
									if(first != null && first != this){
										first.player_visibility_duration = -1;
										first.attrs[AttrType.PLAYER_NOTICED]++;
									}
									else{
										if(t.IsTrap()){
											t.TriggerTrap();
										}
									}
									Q1();
								}
							}
							else{
								Q0();
							}
						}
						else{
							Q0();
						}
					}
					break;
				case 'd':
					if(inv.Count == 0){
						B.Add("You have nothing to drop. ");
						Q0();
					}
					else{
						if(num == -2){
							num = SelectItem("Drop which item? ");
						}
						if(num != -1){
							if(StunnedThisTurn()){
								break;
							}
							Item i = inv[num];
							i.revealed_by_light = true;
							if(i.quantity <= 1 || i.type == ConsumableType.BLAST_FUNGUS){ //todo fungus check is no longer needed
								if(tile().type == TileType.HEALING_POOL){
									B.Add("You drop " + i.TheName() + " into the healing pool. ");
									inv.Remove(i);
									if(curhp < maxhp){
										B.Add("The pool glows briefly. ");
										B.Add("You suddenly feel great again! ");
										B.Add("The healing pool dries up. ");
										curhp = maxhp;
									}
									else{
										B.Add("The pool glows briefly, then dries up. ");
									}
									tile().TurnToFloor();
									Q1();
								}
								else{
									if(tile().GetItem(i)){
										B.Add("You drop " + i.TheName() + ". ");
										inv.Remove(i);
										i.ignored = true;
										Q1();
									}
									else{
										B.Add("There is no room. ");
										Q0();
									}
								}
							}
							else{
								if(tile().type == TileType.HEALING_POOL){
									Item newitem = new Item(i,row,col);
									newitem.quantity = 1;
									i.quantity--;
									B.Add("You drop " + newitem.TheName() + " into the healing pool. ");
									if(curhp < maxhp){
										B.Add("The pool glows briefly. ");
										B.Add("You suddenly feel great again! ");
										B.Add("The healing pool dries up. ");
										curhp = maxhp;
									}
									else{
										B.Add("The pool glows briefly, then dries up. ");
									}
									tile().TurnToFloor();
									Q1();
								}
								else{
									B.DisplayNow("Drop how many? (1-" + i.quantity + "): ");
									int count = Global.EnterInt();
									if(count == 0){
										Q0();
									}
									else{
										if(count >= i.quantity || count == -1){
											if(tile().GetItem(i)){
												B.Add("You drop " + i.TheName() + ". ");
												inv.Remove(i);
												i.ignored = true;
												Q1();
											}
											else{
												B.Add("There is no room. ");
												Q0();
											}
										}
										else{
											Item newitem = new Item(i,row,col);
											newitem.quantity = count;
											newitem.revealed_by_light = true;
											if(tile().GetItem(newitem)){
												i.quantity -= count;
												B.Add("You drop " + newitem.TheName() + ". ");
												newitem.ignored = true;
												Q1();
											}
											else{
												B.Add("There is no room. ");
												Q0();
											}
										}
									}
								}
							}
						}
						else{
							Q0();
						}
					}
					break;
				}
				break;
			}
			case 'e':
			{
				int[] changes = DisplayEquipment();
				Weapon new_weapon = WeaponOfType((WeaponType)changes[0]);
				Armor new_armor = ArmorOfType((ArmorType)changes[1]);
				Weapon old_weapon = EquippedWeapon;
				Armor old_armor = EquippedArmor;
				bool weapon_changed = (new_weapon != old_weapon);
				bool armor_changed = (new_armor != old_armor);
				bool cursed_weapon = false;
				bool cursed_armor = false;
				if(weapon_changed && EquippedWeapon.status[EquipmentStatus.CURSED]){
					cursed_weapon = true;
					weapon_changed = false;
				}
				if(armor_changed && EquippedArmor.status[EquipmentStatus.CURSED]){
					cursed_armor = true;
					armor_changed = false;
				}
				if(!weapon_changed && !armor_changed){
					if(cursed_weapon){
						B.Add("Your " + EquippedWeapon + " is stuck to your hand and can't be put away. ");
					}
					if(cursed_armor){
						B.Add("Your " + EquippedArmor + " is stuck to your body and can't be removed. ");
					}
					Q0();
				}
				else{
					if(StunnedThisTurn()){
						break;
					}
					if(weapon_changed){
						EquippedWeapon = new_weapon;
						if(HasFeat(FeatType.QUICK_DRAW) && !armor_changed){
							B.Add("You quickly ready your " + EquippedWeapon + ". ");
						}
						else{
							B.Add("You ready your " + EquippedWeapon + ". ");
						}
						//UpdateOnEquip(old_weapon,EquippedWeapon);
					}
					if(armor_changed){
						EquippedArmor = new_armor;
						B.Add("You wear your " + EquippedArmor + ". ");
						//UpdateOnEquip(old_armor,EquippedArmor);
					}
					if(cursed_weapon){
						B.Add("Your " + EquippedWeapon + " is stuck to your hand and can't be put away. ");
					}
					if(cursed_armor){
						B.Add("Your " + EquippedArmor + " is stuck to your body and can't be removed. ");
					}
					if(HasFeat(FeatType.QUICK_DRAW) && !armor_changed){
						Q0();
					}
					else{
						Q1();
					}
				}
				break;
			}
			case '!': //note that these are the top-row numbers, NOT the actual shifted versions
			case '@': //<---this is the '2' above the 'w'    (not the '@', and not the numpad 2)
			case '#':
			case '$':
			case '%':
			{
				if(EquippedWeapon.status[EquipmentStatus.CURSED]){
					B.Add("Your " + EquippedWeapon + " is stuck to your hand and can't be put away. ");
					Q0();
				}
				else{
					Weapon new_weapon = null;
					switch(ch){
					case '!':
						new_weapon = Sword;
						break;
					case '@':
						new_weapon = Mace;
						break;
					case '#':
						new_weapon = Dagger;
						break;
					case '$':
						new_weapon = Staff;
						break;
					case '%':
						new_weapon = Bow;
						break;
					}
					Weapon old_weapon = EquippedWeapon;
					if(new_weapon == old_weapon){
						Q0();
					}
					else{
						if(StunnedThisTurn()){
							break;
						}
						EquippedWeapon = new_weapon;
						if(HasFeat(FeatType.QUICK_DRAW)){
							B.Add("You quickly ready your " + EquippedWeapon + ". ");
							Q0();
						}
						else{
							B.Add("You ready your " + EquippedWeapon + ". ");
							Q1();
						}
						//UpdateOnEquip(old_weapon,EquippedWeapon);
					}
				}
				break;
			}
			case '*': //these are toprow numbers, not shifted versions. see above.
			case '(':
			case ')':
			{
				if(EquippedArmor.status[EquipmentStatus.CURSED]){
					B.Add("Your " + EquippedArmor + " is stuck to your body and can't be removed. ");
					Q0();
				}
				else{
					Armor new_armor = null;
					switch(ch){
					case '*':
						new_armor = Leather;
						break;
					case '(':
						new_armor = Chainmail;
						break;
					case ')':
						new_armor = Plate;
						break;
					}
					Armor old_armor = EquippedArmor;
					if(new_armor == old_armor){
						Q0();
					}
					else{
						if(StunnedThisTurn()){
							break;
						}
						EquippedArmor = new_armor;
						B.Add("You wear your " + EquippedArmor + ". ");
						Q1();
						//UpdateOnEquip(old_weapon,EquippedWeapon);
					}
				}
				break;
			}
			case 't':
				if(StunnedThisTurn()){
					break;
				}
				if(light_radius==0){
					if(!M.wiz_dark){
						B.Add("You bring out your torch. ");
					}
					else{
						B.Add("You bring out your torch, but it gives off no light! ");
					}
					if(HasAttr(AttrType.SHINING)){
						UpdateRadius(LightRadius(),Global.MAX_LIGHT_RADIUS - attrs[AttrType.DIM_LIGHT]*2,true);
					}
					else{
						UpdateRadius(LightRadius(),6 - attrs[AttrType.DIM_LIGHT],true); //normal light radius is 6
					}
				}
				else{
					if(!M.wiz_lite){
						B.Add("You put away your torch. ");
					}
					else{
						B.Add("You put away your torch. The air still shines brightly. ");
					}
					UpdateRadius(LightRadius(),0,true);
					UpdateRadius(0,attrs[AttrType.BURNING]); //todo: should be limited to radius 1
				}
				Q1();
				break;
			case (char)9:
				GetTarget(true,-1,0,false,true);
				Q0();
				break;
			case 'p':
			{
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				int i = 1;
				foreach(string s in B.GetMessages()){
					Screen.WriteMapString(i,0,s.PadRight(COLS));
					++i;
				}
				Screen.WriteMapString(21,0,"".PadRight(COLS,'-'));
				B.DisplayNow("Previous messages: ");
				Console.CursorVisible = true;
				Console.ReadKey(true);
				if(HasAttr(AttrType.DETECTING_MOVEMENT) && previous_footsteps.Count > 0){
					M.Draw();
					Screen.AnimateMapCells(previous_footsteps,new colorchar('!',Color.Red),150);
				}
				Q0();
				break;
			}
			case 'c':
			{
				int feat = DisplayCharacterInfo();
				if(feat >= 0){
					foreach(FeatType f in feats_in_order){
						if(Feat.IsActivated(f)){
							if(feat == 0){
								M.RedrawWithStrings();
								if(StunnedThisTurn()){
									break;
								}
								if(!UseFeat(f)){
									Q0();
									break;
								}
							}
							else{
								--feat;
							}
						}
					}
				}
				else{
					Q0();
				}
				break;
			}
			case '\\':
			{
				List<colorstring> potions = new List<colorstring>();
				List<colorstring> scrolls = new List<colorstring>();
				List<colorstring> orbs = new List<colorstring>();
				foreach(ConsumableType ct in Enum.GetValues(typeof(ConsumableType))){
					string type_name = "    " + ct.ToString()[0] + ct.ToString().Substring(1).ToLower();
					type_name = type_name.Replace('_',' ');
					Color ided_color = Color.Cyan;
					if(Item.NameOfItemType(ct) == "potion"){
						if(Item.identified[ct]){
							potions.Add(new colorstring(type_name,ided_color));
			            }
			            else{
							potions.Add(new colorstring(type_name,Color.DarkGray));
						}
					}
					else{
						if(Item.NameOfItemType(ct) == "scroll"){
							if(Item.identified[ct]){
								scrolls.Add(new colorstring(type_name,ided_color));
				            }
				            else{
								scrolls.Add(new colorstring(type_name,Color.DarkGray));
							}
						}
						else{
							if(Item.NameOfItemType(ct) == "orb"){
								if(Item.identified[ct]){
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
				B.DisplayNow("Discovered item types: ");
				Console.CursorVisible = true;
				Console.ReadKey(true);
				Q0();
				break;
			}
			case 'O':
			case '=':
			{
				for(bool done=false;!done;){
					List<string> ls = new List<string>();
					ls.Add("Use last target when possible".PadRight(58) + (Global.Option(OptionType.LAST_TARGET)? "yes ":"no ").PadLeft(4));
					ls.Add("Automatically pick up items (if safe)".PadRight(58) + (Global.Option(OptionType.AUTOPICKUP)? "yes ":"no ").PadLeft(4));
					ls.Add("Hide old messages instead of darkening them".PadRight(58) + (Global.Option(OptionType.HIDE_OLD_MESSAGES)? "yes ":"no ").PadLeft(4));
					ls.Add("Hide the command hints on the side".PadRight(58) + (Global.Option(OptionType.HIDE_COMMANDS)? "yes ":"no ").PadLeft(4));
					ls.Add("Cast a spell instead of attacking".PadRight(46) + (F[0]==SpellType.NO_SPELL? "no ":Spell.Name(F[0])).PadLeft(16));
					ls.Add("Don't use roman numerals for automatic naming".PadRight(58) + (Global.Option(OptionType.NO_ROMAN_NUMERALS)? "yes ":"no ").PadLeft(4));
					ls.Add("Never show tutorial tips".PadRight(58) + (Global.Option(OptionType.NEVER_DISPLAY_TIPS)? "yes ":"no ").PadLeft(4));
					ls.Add("Reset tutorial tips before each game".PadRight(58) + (Global.Option(OptionType.ALWAYS_RESET_TIPS)? "yes ":"no ").PadLeft(4));
					Select("Options: ",ls,true,false,false);
					Console.CursorVisible = true;
					ch = ConvertInput(Console.ReadKey(true));
					switch(ch){
					case 'a':
						Global.Options[OptionType.LAST_TARGET] = !Global.Option(OptionType.LAST_TARGET);
						break;
					case 'b':
						Global.Options[OptionType.AUTOPICKUP] = !Global.Option(OptionType.AUTOPICKUP);
						break;
					case 'c':
						Global.Options[OptionType.HIDE_OLD_MESSAGES] = !Global.Option(OptionType.HIDE_OLD_MESSAGES);
						break;
					case 'd':
						Global.Options[OptionType.HIDE_COMMANDS] = !Global.Option(OptionType.HIDE_COMMANDS);
						break;
					case 'e': //todo remove this option
					{
						break;
					}
					case 'f':
						Global.Options[OptionType.NO_ROMAN_NUMERALS] = !Global.Option(OptionType.NO_ROMAN_NUMERALS);
						break;
					case 'g':
						Global.Options[OptionType.NEVER_DISPLAY_TIPS] = !Global.Option(OptionType.NEVER_DISPLAY_TIPS);
						break;
					case 'h':
						Global.Options[OptionType.ALWAYS_RESET_TIPS] = !Global.Option(OptionType.ALWAYS_RESET_TIPS);
						break;
					case (char)27:
					case ' ':
					case (char)13:
						done = true;
						break;
					default:
						break;
					}
				}
				Q0();
				break;
			}
			case '?':
			case '/':
			{
				Help.DisplayHelp();
				Q0();
				break;
			}
			case '-':
			{
				Console.CursorVisible = false;
				List<string> commandhelp = Help.HelpText(HelpTopic.Commands);
				commandhelp.RemoveRange(0,2);
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				for(int i=0;i<20;++i){
					Screen.WriteMapString(i+1,0,commandhelp[i].PadRight(COLS));
				}
				Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
				B.DisplayNow("Commands: ");
				Console.CursorVisible = true;
				Console.ReadKey(true);
				Q0();
				break;
			}
			case 'q':
			{
				List<string> ls = new List<string>();
				ls.Add("Save your progress and exit to main menu");
				ls.Add("Save your progress and quit game");
				ls.Add("Abandon character and exit to main menu");
				ls.Add("Abandon character and quit game");
				ls.Add("Quit game immediately - don't save anything");
				ls.Add("Continue playing");
				Console.CursorVisible = true;
				switch(Select("Quit? ",ls)){
				case 0:
					Global.SaveGame(B,M,Q);
					Global.GAME_OVER = true;
					Global.SAVING = true;
					break;
				case 1:
					Global.SaveGame(B,M,Q);
					Global.GAME_OVER = true;
					Global.QUITTING = true;
					Global.SAVING = true;
					break;
				case 2:
					Global.GAME_OVER = true;
					Global.KILLED_BY = "giving up";
					break;
				case 3:
					Global.GAME_OVER = true;
					Global.QUITTING = true;
					Global.KILLED_BY = "giving up";
					break;
				case 4:
					Global.Quit();
					break;
				case 5:
				default:
					break;
				}
				Q0();
				break;
			}
			case '~': //debug mode 
				if(true){
					List<string> l = new List<string>();
					l.Add("blink");
					l.Add("create chests");
					l.Add("test character names");
					l.Add("spawn machine");
					l.Add("Forget the map");
					l.Add("Heal to full");
					l.Add("Become invulnerable");
					l.Add("get items!");
					l.Add("other");
					l.Add("Use a rune of passage");
					l.Add("See the entire level");
					l.Add("Generate new level");
					l.Add("Create ice or slime");
					l.Add("Spawn shrines");
					l.Add("create trap");
					l.Add("create door");
					l.Add("spawn lots of goblins and lose neck snap");
					l.Add("distance test");
					l.Add("detect monsters forever");
					l.Add("get specific items");
					switch(Select("Activate which cheat? ",l)){
					case 0:
						{
						//new Item(ConsumableType.DETONATION,"orb of detonation",'*',Color.White).Use(this);
						new Item(ConsumableType.BLINKING,"orb of detonation",'*',Color.White).Use(this);
						Q1();
						break;
						}
					case 1:
					{
						foreach(Tile t in TilesWithinDistance(3)){
							t.TransformTo(TileType.CHEST);
						}
						Q0();
						//Screen.AnimateExplosion(this,5,new colorchar(Color.RandomIce,'*'),25);
						//Q1();
						break;
					}
					case 2:
					{
						/*int[,] row_displacement = GetDiamondSquarePlasmaFractal(ROWS,COLS);
						int[,] col_displacement = GetDiamondSquarePlasmaFractal(ROWS,COLS);
						colorchar[,] scr = Screen.GetCurrentMap();
						M.actor[p] = null;
						scr[p.row,p.col] = M.VisibleColorChar(p.row,p.col);
						M.actor[p] = this;
						int total_rd = 0;
						int total_cd = 0;
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								total_rd += row_displacement[i,j];
								total_cd += col_displacement[i,j];
							}
						}
						int avg_rd = total_rd / (ROWS*COLS);
						int avg_cd = total_cd / (ROWS*COLS);
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								row_displacement[i,j] -= avg_rd;
								col_displacement[i,j] -= avg_cd;
							}
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(i == p.row && j == p.col){
									Screen.WriteMapChar(i,j,'@',Color.White);
								}
								else{
									row_displacement[i,j] /= 8;
									col_displacement[i,j] /= 8;
									if(U.BoundsCheck(i+row_displacement[i,j],j+col_displacement[i,j])){
										Screen.WriteMapChar(i,j,scr[i+row_displacement[i,j],j+col_displacement[i,j]]);
									}
									else{
										Screen.WriteMapChar(i,j,Screen.BlankChar());
									}
								}
							}
						}
						Console.ReadKey(true);*/
						for(int i=0;i<ROWS;++i){
							//Screen.WriteMapString(i,0,Global.GenerateCharacterName().PadToMapSize());
							Screen.WriteMapString(i,0,Item.RandomItem().ToString().PadToMapSize());
						}
						Console.ReadKey(true);
						/*TileInDirection(8).KnockObjectBack(this);
						foreach(Tile t in TilesAtDistance(2)){
							if(HasLOE(t) && t.passable){
								t.Toggle(null,TileType.BLAST_FUNGUS);
							}
						}*/
						Q0();
						break;
					}
					case 3:
					{
						/*ConsoleKeyInfo command2 = Console.ReadKey(true);
						Console.SetCursorPosition(14,14);
						Console.Write(command2.Key);
						Console.ReadKey(true);*/
						/*List<Tile> line = GetTarget(-1,-1);
						if(line != null){
							Tile t = line.Last();
							if(t != null){
								t.AddOpaqueFeature(FeatureType.FOG);
							}
						}*/
						M.SpawnMob(ActorType.MUD_ELEMENTAL);
						Q0();
						break;
					}
					case 4:
						{
						Console.CursorVisible = false;
						colorchar cch;
						cch.c = ' ';
						cch.color = Color.Black;
						cch.bgcolor = Color.Black;
						foreach(Tile t in M.AllTiles()){
							t.seen = false;
							Screen.WriteMapChar(t.row,t.col,cch);
						}
						Console.CursorVisible = true;
						Q0();
						break;
						}
					case 5:
						curhp = maxhp;
						Q0();
						break;
					case 6:
						if(!HasAttr(AttrType.INVULNERABLE)){
							attrs[AttrType.INVULNERABLE]++;
							B.Add("On. ");
						}
						else{
							attrs[AttrType.INVULNERABLE] = 0;
							B.Add("Off. ");
						}
						Q0();
						break;
					case 7:
					{
						for(int i=0;i<50;++i){
							Item.Create(Item.RandomItem(),this);
						}
						Q0();
						break;
					}
					case 8:
					{
						//int[,] a = GetBinaryNoise(ROWS,COLS);
						/*int[,] a = GetDividedNoise(ROWS,COLS,40);
						int[,] chances = new int[ROWS,COLS];
						int[,] values = new int[ROWS,COLS];
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								bool passable = (a[i,j] == 1);
								if(passable){
									values[i,j] = -1;
								}
								else{
									values[i,j] = 0;
								}
							}
						}
						int minrow = 1;
						int maxrow = ROWS-2;
						int mincol = 1;
						int maxcol = COLS-2;
						int val = 0;
						bool done = false;
						while(!done){
							done = true;
							for(int i=minrow;i<=maxrow;++i){
								for(int j=mincol;j<=maxcol;++j){
									if(values[i,j] == val){
										for(int s=i-1;s<=i+1;++s){
											for(int t=j-1;t<=j+1;++t){
												if(values[s,t] == -1){
													values[s,t] = val + 1;
													done = false;
												}
											}
										}
									}
								}
							}
							++val;
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(a[i,j] == 1){
									//distances[i,j] = values[i,j];
									int k = 5 + values[i,j];
									if(k >= 10){
										chances[i,j] = 10;
									}
									else{
										chances[i,j] = k;
									}
								}
							}
						}
						values = new int[ROWS,COLS];
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								bool passable = (a[i,j] == -1);
								if(passable){
									values[i,j] = -1;
								}
								else{
									values[i,j] = 0;
								}
							}
						}
						val = 0;
						done = false;
						while(!done){
							done = true;
							for(int i=minrow;i<=maxrow;++i){
								for(int j=mincol;j<=maxcol;++j){
									if(values[i,j] == val){
										for(int s=i-1;s<=i+1;++s){
											for(int t=j-1;t<=j+1;++t){
												if(values[s,t] == -1){
													values[s,t] = val + 1;
													done = false;
												}
											}
										}
									}
								}
							}
							++val;
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(a[i,j] == -1){
									//distances[i,j] = -(values[i,j]);
									int k = 5 + values[i,j];
									if(k >= 10){
										chances[i,j] = 0;
									}
									else{
										chances[i,j] = 10 - k;
									}
								}
							}
						}
						DungeonGen.StandardDungeon dungeon1 = new DungeonGen.StandardDungeon();
						char[,] map1 = dungeon1.GenerateStandard();
						DungeonGen.StandardDungeon dungeon2 = new DungeonGen.StandardDungeon();
						char[,] map2 = dungeon2.GenerateCave();
						char[,] map3 = new char[ROWS,COLS];
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(a[i,j] == -1){
									map3[i,j] = map1[i,j];
								}
								else{
									if(a[i,j] == 1){
										map3[i,j] = map2[i,j];
									}
									else{
										if(map1[i,j] == '#'){
											map3[i,j] = map2[i,j];
										}
										else{
											if(map2[i,j] == '#'){
												map3[i,j] = map1[i,j];
											}
											else{
												if(R.CoinFlip()){
													map3[i,j] = map1[i,j];
												}
												else{
													map3[i,j] = map2[i,j];
												}
											}
										}
									}
								}
							}
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								Screen.WriteMapChar(i,j,map3[i,j]);
								/*if(distances[i,j] > -10){
									if(distances[i,j] < 10){
										if(distances[i,j] < 0){
											Screen.WriteMapChar(i,j,(-distances[i,j]).ToString()[0],Color.DarkMagenta);
										}
										else{
											if(distances[i,j] > 0){
												Screen.WriteMapChar(i,j,distances[i,j].ToString()[0],Color.DarkCyan);
											}
											else{
												Screen.WriteMapChar(i,j,distances[i,j].ToString()[0],Color.DarkGray);
											}
										}
									}
									else{
										Screen.WriteMapChar(i,j,'+',Color.DarkCyan);
									}
								}
								else{
									Screen.WriteMapChar(i,j,'-',Color.DarkMagenta);
								}
							}
						}*/
						//tile().Toggle(null,TileType.BLAST_FUNGUS);
						/*List<Tile> area = new List<Tile>();
						foreach(Tile t in TilesWithinDistance(3).Where(x=>x.passable && HasLOE(x))){
							t.Toggle(null,TileType.POPPY_FIELD);
							area.Add(t);
						}
						Q.Add(new Event(area,100,EventType.POPPIES));*/
						//tile().Toggle(null,Forays.TileType.TOMBSTONE);
						//Console.ReadKey(true);
						List<string> movement = new List<string>{"is immobile","moves quickly","moves slowly"};
						List<string> ability = new List<string>{"can step back after attacking","moves erratically","flies","can use an aggressive stance","uses a ranged attack","uses a burst attack","lunges","has a poisonous attack","drains life","has a powerful but slow attack","grabs its targets","has a knockback attack","has a slowing attack","has a silencing attack","can steal items","explodes when defeated","stays at range",
						"sidesteps when it attacks","has a paralyzing attack","has a stunning attack","is stealthy","appears with others of its type","carries a light source","is invisible in darkness","disrupts nearby spells","regenerates","comes back after death","wears armor","has heightened senses","has hard skin that can blunt edged weapons","casts spells","can reduce its target's attack power","can burrow"};
						List<string> rare_ability = new List<string>{"is attracted to light","is blind in the light","can create illusions of itself","sets itself on fire","throws a bola to slow its targets","dims nearby light sources","screams to terrify its prey","howls to embolden others attacking its prey","breathes poison","is surrounded by a poisonous cloud",
						"is surrounded by a cloud of fog","can summon a minion","can fill the area with sunlight","is resistant to weapons","can turn into a statue","can throw explosives","can create stalagmites","collapses into rubble when defeated","has a fiery attack","can teleport its foes away","can pull its targets closer from a distance","releases spores when attacked","can absorb light to heal","leaves a trail as it travels","breathes fire","can spit blinding poison","lays volatile eggs","can breach nearby walls","is knocked back by blunt weapons","causes attackers to become exhausted","can create a temporary wall","can throw its foes overhead"};
						char randomsymbol = (char)((R.Roll(26)-1) + (int)'a');
						if(R.CoinFlip()){
							randomsymbol = randomsymbol.ToString().ToUpper()[0];
						}
						string s1 = "This monster is a " + Screen.GetColor(Color.RandomAny).ToString().ToLower().Replace("dark","dark ") + " '" + randomsymbol + "'. ";
						string s2 = "It ";
						bool add_move = R.OneIn(5);
						int num_abilities = R.Roll(2) + 1;
						if(R.OneIn(10)){
							++num_abilities;
						}
						int total = num_abilities;
						if(add_move){ ++total; }
						if(add_move){
							--total;
							if(total == 0){
								s2 = s2 + "and " + movement.Random() + ". ";
							}
							else{
								s2 = s2 + movement.Random() + ", ";
							}
						}
						for(int i=num_abilities;i>0;--i){
							--total;
							string a = "";
							if(R.PercentChance(50)){
								a = ability.Random();
							}
							else{
								a = rare_ability.Random();
							}
							if(!s2.Contains(a)){
								if(total == 0){
									s2 = s2 + "and " + a + ". ";
								}
								else{
									s2 = s2 + a + ", ";
								}
							}
							else{
								++i;
								++total;
							}
						}
						/*if(add_rare){
							--total;
							if(total == 0){
								s2 = s2 + "and " + rare_ability.Random() + ". ";
							}
							else{
								s2 = s2 + rare_ability.Random() + ", ";
							}
						}*/
						B.Add(s1);
						B.Add(s2);
						if(s2.Contains("casts spells")){
							List<SpellType> all_spells = new List<SpellType>();
							foreach(SpellType spl in Enum.GetValues(typeof(SpellType))){
								all_spells.Add(spl);
							}
							all_spells.Remove(SpellType.NO_SPELL);
							all_spells.Remove(SpellType.NUM_SPELLS);
							string sp = "It can cast ";
							for(int num_spells = R.Roll(4);num_spells > 0;--num_spells){
								if(num_spells == 1){
									sp = sp + "and " + all_spells.RemoveRandom().ToString().ToLower().Replace('_',' ') + ". ";
								}
								else{
									sp = sp + all_spells.RemoveRandom().ToString().ToLower().Replace('_',' ') + ", ";
								}
							}
							B.Add(sp);
						}
						Q0();
						break;
					}
					case 9:
						new Item(ConsumableType.PASSAGE,"rune of passage",'&',Color.White).Use(this);
						Q1();
						break;
					case 10:
						foreach(Tile t in M.AllTiles()){
							t.seen = true;
						}
						M.Draw();
						foreach(Actor a in M.AllActors()){
							Screen.WriteMapChar(a.row,a.col,new colorchar(a.color,Color.Black,a.symbol));
						}
						Console.ReadKey(true);
						Q0();
						break;
					case 11:
						for(int i=0;i<1;++i){
							if(M.current_level < 20){
								//M.level_types[M.current_level] = LevelType.Standard;
							}
							M.GenerateLevel();
							foreach(Tile t in M.AllTiles()){
								if(t.TilesWithinDistance(1).Any(x=>x.type != TileType.WALL)){
									t.seen = true;
								}
							}
							B.Print(false);
							M.Draw();
						}
						Q0();
						break;
					case 12:
					{
						/*PosArray<int> map = new PosArray<int>(ROWS,COLS);
						pos center = new pos(ROWS/2,COLS/2);
						int n = 2;
						foreach(pos p in center.PositionsWithinDistance(n-1)){
							map[p] = 1;
						}
						bool changed = true;
						while(changed){
							changed = false;
							List<pos> list = center.PositionsAtDistance(n);
							while(list.Count > 0){
								pos p = list.RemoveRandom();
								int count = p.PositionsAtDistance(1).Where(x=>map[x] == 1).Count;
								if(R.PercentChance(count*25)){ //this number can be anywhere from ~19 to 25
									map[p] = 1;
									changed = true;
								}
							}
							++n;
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								//pos p = new pos(i,j);
								//if(p.PositionsWithinDistance(1).Where(x=>map[x] == 1).Count >= 5){
								if(map[i,j] == 1){
									Screen.WriteMapChar(i,j,'.',Color.Green);
								}
								else{
									Screen.WriteMapChar(i,j,'~',Color.Blue);
								}
							}
						}
						Console.ReadKey(true);
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								pos p = new pos(i,j);
								if(p.PositionsWithinDistance(1).Where(x=>map[x] == 1).Count >= 5){
								//if(map[i,j] == 1){
									Screen.WriteMapChar(i,j,'.',Color.Green);
								}
								else{
									Screen.WriteMapChar(i,j,'~',Color.Blue);
								}
							}
						}
						Console.ReadKey(true);*/
						//
						//
						/*level = 10;
						skills[SkillType.COMBAT] = 10;
						skills[SkillType.DEFENSE] = 10;
						skills[SkillType.MAGIC] = 10;
						skills[SkillType.SPIRIT] = 10;
						skills[SkillType.STEALTH] = 10;
						foreach(FeatType f in Enum.GetValues(typeof(FeatType))){
							if(f != FeatType.NO_FEAT && f != FeatType.NUM_FEATS){
								feats[f] = true;
							}
						}*/
						if(R.CoinFlip()){
							foreach(Tile t in TilesWithinDistance(2).Where(x=>x.type == TileType.FLOOR)){
								t.Toggle(null,TileType.ICE);
							}
						}
						else{
							foreach(Tile t in TilesWithinDistance(2).Where(x=>x.type == TileType.FLOOR)){
								t.AddFeature(FeatureType.SLIME);
							}
						}
						Q0();
						//B.Add("\"I HAVE THE POWERRRR!\" ");
						break;
					}
					case 13:
					{
						//LevelUp();
							foreach(Tile t in TilesWithinDistance(2)){
								t.TransformTo((TileType)(R.Roll(5)+20));
							}
						Q0();
						break;
					}
					case 14:
					{
						foreach(Tile t in TilesAtDistance(1)){
							t.TransformTo(Tile.RandomTrap());
						}
						Q0();
						break;
					}
					case 15:
					{
							List<Tile> line = GetTargetTile(-1,0,false);
							if(line != null){
								Tile t = line.Last();
								if(t != null){
									t.TransformTo(TileType.DOOR_O);
								}
							}
						Q0();
						break;
					}
					case 16:
					{
						for(int i=0;i<100;++i){
							M.SpawnMob(ActorType.GOBLIN);
						}
						if(HasFeat(FeatType.NECK_SNAP)){
							feats[FeatType.NECK_SNAP] = false;
						}
						Q0();
						break;
					}
					case 17:
					{
						/*List<Tile> list = new List<Tile>();
						while(list.Count < 15){
							int rr = R.Roll(ROWS-2);
							int rc = R.Roll(COLS-2);
							if(M.tile[rr,rc].passable){
								list.AddUnique(M.tile[rr,rc]);
							}
						}
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(M.tile[i,j].passable){
									List<Tile> closest_tiles = list.WhereLeast(x => x.ApproximateEuclideanDistanceFromX10(i,j));
									//List<Tile> closest_tiles = list.WhereLeast(x => new Actor(this,i,j).GetPath(x.row,x.col).Count);
									if(closest_tiles.Count == 2){
										Screen.WriteMapChar(i,j,'=',Color.White);
									}
									else{
										int idx = list.IndexOf(closest_tiles[0]);
										Screen.WriteMapChar(i,j,M.tile[i,j].symbol,(Color)(idx+3));
									}
								}
								else{
									if(!M.tile[i,j].solid_rock){
										Screen.WriteMapChar(i,j,M.tile[i,j].symbol,M.tile[i,j].color);
									}
									else{
										Screen.WriteMapChar(i,j,Screen.BlankChar());
									}
								}
							}
						}*/
						Screen.Blank();
						Tile[] prev = new Tile[20];
						int idx = 0;
						foreach(Tile t in M.ReachableTilesByDistance(row,col,false,TileType.DOOR_C)){
							Screen.WriteMapChar(t.row,t.col,t.symbol,t.color);
							prev[idx] = t;
							idx = (idx + 1) % 20;
							if(prev[idx] != null){
								Screen.WriteMapChar(prev[idx].row,prev[idx].col,Screen.BlankChar());
							}
							Thread.Sleep(10);
						}
						Q0();
						break;
					}
					case 18:
					{
						if(attrs[AttrType.DETECTING_MONSTERS] == 0){
							attrs[AttrType.DETECTING_MONSTERS] = 1;
						}
						else{
							attrs[AttrType.DETECTING_MONSTERS] = 0;
						}
						Q0();
						break;
					}
					case 19:
					{
						/*List<Tile> line = GetTargetTile(-1,0,false);
						if(line != null){
							Tile t = line.Last();
							if(t != null){
								t.Toggle(null,TileType.CHASM);
								Q.Add(new Event(t,100,EventType.FLOOR_COLLAPSE));
								B.Add("The floor begins to collapse! ");
							}
						}*/
						if(tile().inv == null){
							tile().inv = Item.Create(ConsumableType.ENCHANTMENT,row,col);
							B.Add("You feel something roll beneath your feet. ");
						}
						else{
							magic_trinkets.Add(MagicTrinketType.CIRCLET_OF_THE_THIRD_EYE);
							magic_trinkets.Add(MagicTrinketType.BELT_OF_TOUGHNESS);
							magic_trinkets.Add(MagicTrinketType.LENS_OF_SCRYING);
						}
						Q0();
						break;
					}
					default:
						Q0();
						break;
					}
				}
				else{
					Q0();
				}
				break;
			case ' ':
				Q0();
				break;
			default:
				B.Add("Press '?' for help. ");
				Q0();
				break;
			}
			if(ch != 'x'){
				attrs[AttrType.AUTOEXPLORE] = 0;
			}
		}
		public static int[,] GetBinaryNoise(int height,int width){ return GetBinaryNoise(height,width,50); }
		public static int[,] GetBinaryNoise(int height,int width,int target_percentage){ //todo: is this used?
			int[,] a = GetDiamondSquarePlasmaFractal(height,width,false);
			int[,] result = new int[height,width];
			int n = 128;
			int current_num = n;
			int closest_total = -1;
			int num_of_closest_total = -1;
			while(n > 0){
				n /= 2;
				int total = 0;
				for(int i=0;i<height;++i){
					for(int j=0;j<width;++j){
						if(a[i,j] >= current_num){
							++total;
						}
					}
				}
				int target_total = height * width * target_percentage / 100;
				if(Math.Abs(target_total - total) < Math.Abs(target_total - closest_total)){
					closest_total = total;
					num_of_closest_total = current_num;
				}
				if(total < target_total){
					current_num -= n;
				}
				else{
					if(total > target_total){
						current_num += n;
					}
					else{
						break; //done
					}
				}
			}
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(a[i,j] >= num_of_closest_total){
						result[i,j] = 1;
					}
					else{
						result[i,j] = -1;
					}
				}
			}
			return result;
		}
		public static int[,] GetDividedNoise(int height,int width,int target_percentage_of_outer){
			int[,] a = GetDiamondSquarePlasmaFractal(height,width,false);
			int[,] result = new int[height,width];
			int num_of_closest_total_one = -1;
			int num_of_closest_total_two = -1;
			{
				int n = 128;
				int current_num = n;
				int closest_total = -1;
				int num_of_closest_total = -1;
				while(n > 0){
					n /= 2;
					int total = 0;
					for(int i=0;i<height;++i){
						for(int j=0;j<width;++j){
							if(a[i,j] >= current_num){
								++total;
							}
						}
					}
					int target_total = height * width * target_percentage_of_outer / 100;
					if(Math.Abs(target_total - total) < Math.Abs(target_total - closest_total)){
						closest_total = total;
						num_of_closest_total = current_num;
					}
					if(total < target_total){
						current_num -= n;
					}
					else{
						if(total > target_total){
							current_num += n;
						}
						else{
							break; //done
						}
					}
				}
				num_of_closest_total_one = num_of_closest_total;
			}
			{
				int n = 128;
				int current_num = n;
				int closest_total = -1;
				int num_of_closest_total = -1;
				while(n > 0){
					n /= 2;
					int total = 0;
					for(int i=0;i<height;++i){
						for(int j=0;j<width;++j){
							if(a[i,j] >= current_num){
								++total;
							}
						}
					}
					int target_total = height * width * (100 - target_percentage_of_outer) / 100;
					if(Math.Abs(target_total - total) < Math.Abs(target_total - closest_total)){
						closest_total = total;
						num_of_closest_total = current_num;
					}
					if(total < target_total){
						current_num -= n;
					}
					else{
						if(total > target_total){
							current_num += n;
						}
						else{
							break; //done
						}
					}
				}
				num_of_closest_total_two = num_of_closest_total;
			}
			int upper = Math.Max(num_of_closest_total_one,num_of_closest_total_two);
			int lower = Math.Min(num_of_closest_total_one,num_of_closest_total_two);
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					if(a[i,j] >= lower){
						if(a[i,j] <= upper){
							result[i,j] = 0;
						}
						else{
							result[i,j] = 1;
						}
					}
					else{
						result[i,j] = -1;
					}
					/*if(a[i,j] >= num_of_closest_total){
						result[i,j] = 1;
					}
					else{
						result[i,j] = -1;
					}*/
				}
			}
			return result;
		}
		public static int[,] GetDiamondSquarePlasmaFractal(int height,int width,int roughness){
			int[,] a = GetDiamondSquarePlasmaFractal(height*roughness,width*roughness);
			int[,] result = new int[height,width];
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					result[i,j] = a[i*roughness,j*roughness];
				}
			}
			return result;
		}
		public static int[,] GetDiamondSquarePlasmaFractal(int height,int width){ return GetDiamondSquarePlasmaFractal(height,width,true); }
		public static int[,] GetDiamondSquarePlasmaFractal(int height,int width,bool normalize){
			int size = 1;
			int max = Math.Max(height,width);
			while(size < max){
				size *= 2; //find the smallest square that the dungeon fits into
			}
			size++; //diamond-square needs 2^x + 1
			int[,] a = GetDiamondSquarePlasmaFractal(size,normalize);
			int[,] result = new int[height,width];
			for(int i=0;i<height;++i){
				for(int j=0;j<width;++j){
					result[i,j] = a[i,j];
				}
			}
			return result;
		}
		public static int[,] GetDiamondSquarePlasmaFractal(int size){ return GetDiamondSquarePlasmaFractal(size,true); }
		public static int[,] GetDiamondSquarePlasmaFractal(int size,bool normalize){
			int[,] a = new int[size,size];
			a[0,0] = 128;
			a[0,size-1] = 128;
			a[size-1,0] = 128;
			a[size-1,size-1] = 128;
			int step = 1;
			while(DiamondStep(a,step)){
				SquareStep(a,step);
				++step;
			}
			if(normalize){
				int total = 0;
				for(int i=0;i<size;++i){
					for(int j=0;j<size;++j){
						total += a[i,j];
					}
				}
				int mean = total / (size*size);
				for(int i=0;i<size;++i){
					for(int j=0;j<size;++j){
						a[i,j] -= mean;
					}
				}
			}
			return a;
		}
		private static bool ArrayBoundsCheck(int[,] a,int r,int c){
			if(r < 0 || r > a.GetUpperBound(0) || c < 0 || c > a.GetUpperBound(1)){
				return false;
			}
			return true;
		}
		private static bool DiamondStep(int[,] a,int step){ //step starts at 1
			int divisions = 1; //divisions^2 is the number of squares
			while(step > 1){
				divisions *= 2;
				--step;
			}
			int increment = a.GetUpperBound(0) / divisions;
			if(increment == 1){
				return false; //done!
			}
			for(int i=0;i<divisions;++i){
				for(int j=0;j<divisions;++j){
					int total = 0;
					total += a[i*increment,j*increment];
					total += a[i*increment,(j+1)*increment];
					total += a[(i+1)*increment,j*increment];
					total += a[(i+1)*increment,(j+1)*increment];
					total = total / 4;
					total += R.Roll((128 / divisions) + 1) - ((64 / divisions) + 1);
					a[i*increment + increment/2,j*increment + increment/2] = total;
				}
			}
			return true;
		}
		private static void SquareStep(int[,] a,int step){
			int divisions = 1;
			while(step > 0){
				divisions *= 2;
				--step;
			}
			int increment = a.GetUpperBound(0) / divisions;
			for(int i=0;i<=divisions;++i){
				for(int j=0;j<=divisions;++j){
					if((i+j)%2 == 1){
						int total = 0;
						int num = 0;
						if(ArrayBoundsCheck(a,(i-1)*increment,j*increment)){
							++num;
							total += a[(i-1)*increment,j*increment];
						}
						if(ArrayBoundsCheck(a,i*increment,(j-1)*increment)){
							++num;
							total += a[i*increment,(j-1)*increment];
						}
						if(ArrayBoundsCheck(a,i*increment,(j+1)*increment)){
							++num;
							total += a[i*increment,(j+1)*increment];
						}
						if(ArrayBoundsCheck(a,(i+1)*increment,j*increment)){
							++num;
							total += a[(i+1)*increment,j*increment];
						}
						total = total / num;
						total += R.Roll((256 / divisions) + 1) - ((128 / divisions) + 1); //doubled because divisions are doubled
						a[i*increment,j*increment] = total;
					}
				}
			}
		}
		public void PlayerWalk(int dir){
			if(dir > 0){
				if(ActorInDirection(dir)!=null){
					if(!ActorInDirection(dir).IsHiddenFrom(this)){
						if(F[0] == SpellType.NO_SPELL){ //todo: remove f-spells probably
							if(ActorInDirection(dir).HasAttr(AttrType.TERRIFYING) && CanSee(ActorInDirection(dir))){
								B.Add("You're too afraid! ");
								Q0();
								return;
							}
							Attack(0,ActorInDirection(dir));
						}
						else{
							if(!CastSpell(F[0],TileInDirection(dir))){
								Q0();
							}
						}
					}
					else{
						if(HasAttr(AttrType.IMMOBILE)){
							if(HasAttr(AttrType.ROOTS)){
								B.Add("You're rooted to the ground! ");
							}
							else{
								B.Add("You can't move! ");
							}
							Q0();
							return;
						}
						ActorInDirection(dir).attrs[AttrType.TURNS_VISIBLE] = -1;
						ActorInDirection(dir).attrs[AttrType.NOTICED]++;
						if(!IsHiddenFrom(ActorInDirection(dir))){
							B.Add("You walk straight into " + ActorInDirection(dir).AName(true) + "! ");
						}
						else{
							B.Add("You walk straight into " + ActorInDirection(dir).AName(true) + "! ");
							if(CanSee(ActorInDirection(dir))){
								B.Add(ActorInDirection(dir).the_name + " looks just as surprised as you. ");
							}
							ActorInDirection(dir).player_visibility_duration = -1;
							ActorInDirection(dir).attrs[AttrType.PLAYER_NOTICED]++;
						}
						Q1();
					}
				}
				else{
					Tile t = TileInDirection(dir);
					if(t.passable){
						if(GrabPreventsMovement(t)){
							List<Actor> grabbers = new List<Actor>();
							foreach(Actor a in ActorsAtDistance(1)){
								if(a.attrs[AttrType.GRABBING] == a.DirectionOf(this)){
									grabbers.Add(a);
								}
							}
							B.Add(grabbers.Random().the_name + " prevents you from moving away! ");
							Q0();
							return;
						}
						if(HasAttr(AttrType.IMMOBILE)){
							if(HasAttr(AttrType.ROOTS)){
								B.Add("You're rooted to the ground! ");
							}
							else{
								B.Add("You can't move! ");
							}
							Q0();
							return;
						}
						for(int i=t.row-1;i<=t.row+1;++i){
							for(int j=t.col-1;j<=t.col+1;++j){
								if(M.actor[i,j] != null && M.actor[i,j].HasAttr(AttrType.TERRIFYING) && CanSee(M.actor[i,j])){
									B.Add("You're too afraid! "); 
									Q0();
									return;
								}
							}
						}
						if(HasAttr(AttrType.BRUTISH_STRENGTH) && t.IsTrap()){
							t.SetName(Tile.Prototype(t.type).name);
							B.Add("You smash " + t.TheName(true) + ". ");
							t.TurnToFloor();
						}
						if(t.type == TileType.STAIRS){
							if(!Global.Option(OptionType.HIDE_COMMANDS)){
								B.Add("There are stairs here - press > to descend. ");
							}
							else{
								B.Add("There are stairs here. ");
							}
						}
						if(t.IsShrine()){
							B.Add(t.the_name + " glows faintly - press g to touch it. ");
							t.revealed_by_light = true;
						}
						if(t.Is(TileType.CHEST)){
							B.Add("There is a chest here - press g to open it. ");
						}
						if(t.Is(TileType.HEALING_POOL)){
							B.Add("There is a healing pool here. ");
							Help.TutorialTip(TutorialTopic.HealingPool);
						}
						if(t.Is(TileType.FIRE_GEYSER,TileType.FOG_VENT,TileType.POISON_GAS_VENT)){
							if(t.name != "floor"){
								t.revealed_by_light = true;
								B.Add("There is " + t.AName(true) + " here. ");
							}
						}
						if(t.Is(TileType.CHASM) && !HasAttr(AttrType.FLYING)){
							Interrupt();
							B.DisplayNow("Jump into the chasm?(y/n): ");
							Console.CursorVisible = true;
							bool done = false;
							while(!done){
								switch(Console.ReadKey(true).KeyChar){
								case 'y':
								case 'Y':
									done = true;
									break;
								default:
									Q0();
									return;
								}
							}
						}
						if(t.inv != null){
							B.Add("You see " + t.inv.AName() + ". ");
							t.inv.revealed_by_light = true;
						}
						List<Actor> previously_adjacent = null;
						if(HasFeat(FeatType.ATTACK_EVERYTHING_TODO)){
							previously_adjacent = new List<Actor>();
							foreach(Actor a in ActorsAtDistance(1)){
								if(!a.IsHiddenFrom(this)){
									previously_adjacent.Add(a);
								}
							}
						}
						Move(t.row,t.col);
						if(t.Is(TileType.GRAVEL) && !HasAttr(AttrType.FLYING)){
							B.Add("The gravel crunches. ",t);
							MakeNoise(3);
						}
						if(HasFeat(FeatType.ATTACK_EVERYTHING_TODO) && previously_adjacent.Count > 0){
							List<Actor> still_adjacent = new List<Actor>();
							foreach(Actor a in ActorsAtDistance(1)){
								if(previously_adjacent.Contains(a)){
									still_adjacent.Add(a);
								}
							}
							if(still_adjacent.Count > 0){
								if(HasAttr(AttrType.STUNNED) && R.OneIn(3)){
									B.Add("You stagger. ",this);
									QS();
									return;
								}
								if(exhaustion == 20 && R.CoinFlip()){ //100% exhaustion
									B.Add(You("fumble") + " from exhaustion. ",this);
									QS();
									return;
								}
								if(EquippedWeapon.status[EquipmentStatus.POSSESSED]){ //todo: make sure this works correctly with 'attack everything' feat
									List<Actor> actors = ActorsWithinDistance(1);
									Actor chosen = actors.Random();
									if(chosen == this){
										B.Add("Your possessed " + EquippedWeapon.NameWithEnchantment() + " tries to attack you! ");
										B.Add("You fight it off! ");
										QS();
										return;
									}
								}
								while(still_adjacent.Count > 0){
									Actor a = still_adjacent.RemoveRandom();
									Attack(0,a,true);
								}
								QS();
								return;
							}
						}
						QS();
						if(!Help.displayed[TutorialTopic.Recovery] && !HasAttr(AttrType.POISONED) && !HasAttr(AttrType.BURNING) && !HasAttr(AttrType.CATCHING_FIRE)
						&& curhp % 10 > 0 && curhp % 10 <= 5 && !M.AllActors().Any(a=>(a != this && CanSee(a))) && !TilesWithinDistance(1).Any(x=>(x.inv != null && x.inv.type == ConsumableType.BLAST_FUNGUS)
						|| x.Is(FeatureType.GRENADE,FeatureType.POISON_GAS,FeatureType.QUICKFIRE) || x.Is(TileType.FIRE_GEYSER))){
							Help.TutorialTip(TutorialTopic.Recovery); //not poisoned or on fire, can recover at least 5hp, can't see any enemies, and isn't adjacent to hazardous terrain
							Interrupt(); //todo remove
						}
					}
					else{
						if(HasAttr(AttrType.BRUTISH_STRENGTH) && t.Is(TileType.CRACKED_WALL,TileType.DOOR_C,TileType.STALAGMITE,TileType.STATUE,TileType.RUBBLE)){
							B.Add("You smash " + t.TheName(true) + ". ");
							if(t.Is(TileType.STALAGMITE)){
								t.Toggle(this);
							}
							else{
								t.TurnToFloor();
							}
							Move(t.row,t.col);
							QS();
						}
						else{
							if(t.Is(TileType.DOOR_C,TileType.RUBBLE)){
								if(StunnedThisTurn()){
									return;
								}
								t.Toggle(this);
								Q1();
							}
							else{
								B.Add("There is " + t.a_name + " in the way. ");
								if(HasAttr(AttrType.BLIND)){
									t.seen = true;
								}
								Q0();
							}
						}
					}
				}
			}
			else{
				Q0();
			}
		}
		public void InputAI(){
			if(type == ActorType.DREAM_SPRITE && HasAttr(AttrType.COOLDOWN_2) && target != null){
				bool no_los_needed = !target.CanSee(this);
				Tile t = target.TilesAtDistance(DistanceFrom(target)).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(this) > 1 && (no_los_needed || target.CanSee(x))).Random();
				if(t == null){ //gradually loosening the restrictions on placement...
					t = target.TilesAtDistance(DistanceFrom(target)).Where(x=>x.passable && x.actor() == null && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = target.TilesWithinDistance(12).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(target) >= this.DistanceFrom(target) && x.DistanceFrom(this) > 1 && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = target.TilesWithinDistance(12).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(target) >= this.DistanceFrom(target) && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = TilesAtDistance(2).Where(x=>x.passable && x.actor() == null && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = TilesWithinDistance(6).Where(x=>x.passable && x.actor() == null && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t == null){
					t = M.AllTiles().Where(x=>x.passable && x.actor() == null && (no_los_needed || target.CanSee(x))).Random();
				}
				if(t != null){
					attrs[AttrType.COOLDOWN_2] = 0;
					if(group == null){
						group = new List<Actor>{this};
					}
					Actor clone = Create(ActorType.DREAM_SPRITE_CLONE,t.row,t.col,true,true);
					clone.speed = 100;
					bool seen = target.CanSee(clone);
					clone.player_visibility_duration = -1;
					group.Add(clone);
					clone.group = group;
					group.Randomize();
					List<Tile> valid_tiles = new List<Tile>();
					foreach(Actor a in group){
						valid_tiles.Add(a.tile());
					}
					Tile newtile = valid_tiles.Random();
					if(newtile != tile()){
						Move(newtile.row,newtile.col,false);
					}
					if(seen){
						B.Add("Another " + name + " appears! ");
					}
				}
			}
			bool no_act = false;
			if(HasAttr(AttrType.BLIND)){
				string verb = "stagger";
				bool es = false;
				if(HasAttr(AttrType.FLYING)){
					verb = "careen";
				}
				else{
					if(Is(ActorType.SPITTING_COBRA,ActorType.MIMIC,ActorType.GIANT_SLUG,ActorType.INFESTED_MASS_TODO_NAME,ActorType.NOXIOUS_WORM)){
						verb = "lurch";
						es = true;
					}
				}
				Tile t = null;
				if(type == ActorType.PHASE_SPIDER){
					if(target != null){
						t = target.TilesWithinDistance(DistanceFrom(target)+1).Where(x=>x.DistanceFrom(target) >= DistanceFrom(target)-1).Random();
					}
				}
				else{
					t = TileInDirection(Global.RandomDirection());
				}
				if(t != null){
					Actor a = t.actor();
					if(!t.passable){
						B.Add(You(verb,es) + " into " + t.the_name + ". ",this);
					}
					else{
						if(a != null){
							B.Add(YouVisible(verb,es) + " into " + a.TheName(true) + ". ",this,a);
						}
						else{
							if(GrabPreventsMovement(t)){
								B.Add(You(verb,es) + " and almost falls over. ",this);
							}
							else{
								B.Add(You(verb,es) + ". ",this);
								Move(t.row,t.col);
							}
						}
					}
				}
				QS();
				no_act = true;
			}
			bool aware_of_player = CanSee(player);
			if(HasAttr(AttrType.SEES_ADJACENT_PLAYER)){
				if(DistanceFrom(player) == 1){ //this allows them to attack when the player is shadow cloaked
					aware_of_player = true;
				}
				else{
					attrs[AttrType.SEES_ADJACENT_PLAYER] = 0;
				}
			}
			if(target == player && target_location != null && target_location.actor() == player){
				aware_of_player = true;
			}
			if(target == player && player_visibility_duration == -1 && DistanceFrom(player) == 1){
				aware_of_player = true;
			}
			if(aware_of_player){
				/*if(target_location == null && HasAttr(AttrType.BLOODSCENT)){ //orc warmages etc. when they first notice
					player_visibility_duration = -1;
					target = player;
					target_location = M.tile[player.row,player.col];
					if((player.IsWithinSightRangeOf(this) || tile().IsLit()) && player.HasLOS(this)){
						B.Add(the_name + "'s gaze meets your eyes! ",this);
					}
					B.Add(the_name + " snarls loudly. ",this);
					MakeNoise(4);
					Q1();
					no_act = true; //todo remove this but make sure detect movement works for monsters.
				}
				else{*/
					target = player;
					target_location = M.tile[player.row,player.col];
					player_visibility_duration = -1;
				//}
			}
			else{
				bool might_notice = false;
				if((IsWithinSightRangeOf(player.row,player.col) || (player.tile().IsLit() && !HasAttr(AttrType.BLINDSIGHT))) //if they're stealthed and nearby...
					&& HasLOS(player.row,player.col)
					&& (!player.HasAttr(AttrType.SHADOW_CLOAK) || player.tile().IsLit() || HasAttr(AttrType.BLINDSIGHT))){ //((removed player_noticed check from this line))
					might_notice = true;
				}
				if(type == ActorType.CLOUD_ELEMENTAL){
					List<pos> cloud = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].features.Contains(FeatureType.FOG));
					foreach(pos p2 in cloud){
						if(player.DistanceFrom(p2) <= 12){
							if(M.tile[p2].HasLOS(player)){
								might_notice = true;
								break;
							}
						}
					}
				}
				if(player.HasFeat(FeatType.CORNER_CLIMB) && DistanceFrom(player) > 1 && !player.tile().IsLit()){
					if(SchismExtensionMethods.Extensions.ConsecutiveAdjacent(player.p,x=>M.tile[x].Is(TileType.WALL,TileType.CRACKED_WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR,TileType.STATUE,TileType.STONE_SLAB)) >= 5){
						might_notice = false;
					}
				}
				if(!no_act && might_notice){
					int multiplier = HasAttr(AttrType.KEEN_SENSES)? 5 : 10; //animals etc. are approximately twice as hard to sneak past
					int stealth = player.TotalSkill(SkillType.STEALTH);
					if(HasAttr(AttrType.BLINDSIGHT) && !player.tile().IsLit()){ //if this monster has blindsight, take away the stealth bonus for being in darkness
						stealth -= 2;
					}
					if(player.TotalSkill(SkillType.STEALTH) * DistanceFrom(player) * multiplier - player_visibility_duration++*5 < R.Roll(1,100)){
						player_visibility_duration = -1;
						attrs[AttrType.PLAYER_NOTICED]++;
						target = player;
						target_location = M.tile[player.row,player.col];
						if(group != null){
							foreach(Actor a in group){
								if(a != this && DistanceFrom(a) < 3){
									a.player_visibility_duration = -1;
									a.attrs[AttrType.PLAYER_NOTICED]++;
									a.target = player;
									a.target_location = M.tile[player.row,player.col];
								}
							}
						}
						switch(type){
						case ActorType.RAT:
						case ActorType.DIRE_RAT:
							B.Add(TheName(true) + " squeaks at you. ");
							MakeNoise(4);
							break;
						case ActorType.GOBLIN:
						case ActorType.GOBLIN_ARCHER:
						case ActorType.GOBLIN_SHAMAN:
							B.Add(TheName(true) + " growls. ");
							MakeNoise(4);
							break;
						case ActorType.BLOOD_MOTH:
							if(!M.wiz_lite && !M.wiz_dark && player.LightRadius() > 0){
								B.Add(the_name + " notices your light. ",this);
							}
							break;
						case ActorType.CULTIST:
						case ActorType.ROBED_ZEALOT:
							B.Add(TheName(true) + " yells. ");
							MakeNoise(4);
							break;
						case ActorType.ZOMBIE:
							B.Add(TheName(true) + " moans. Uhhhhhhghhh. ");
							MakeNoise(4);
							break;
						case ActorType.LONE_WOLF:
							B.Add(TheName(true) + " snarls at you. ");
							MakeNoise(4);
							break;
						case ActorType.FROSTLING:
							B.Add(TheName(true) + " makes a chittering sound. ");
							MakeNoise(4);
							break;
						case ActorType.SWORDSMAN:
						case ActorType.BERSERKER:
						case ActorType.CRUSADING_KNIGHT:
							B.Add(TheName(true) + " shouts. ");
							MakeNoise(4);
							break;
						case ActorType.BANSHEE:
							B.Add(TheName(true) + " shrieks. ");
							MakeNoise(4);
							break;
						case ActorType.WARG:
							B.Add(TheName(true) + " howls. ");
							MakeNoise(4);
							break;
						case ActorType.DERANGED_ASCETIC:
							B.Add(TheName(true) + " starts babbling incoherently. ");
							break;
						case ActorType.CAVERN_HAG:
							B.Add(TheName(true) + " cackles. ");
							MakeNoise(4);
							break;
						case ActorType.OGRE:
							B.Add(TheName(true) + " bellows at you. ");
							MakeNoise(4);
							break;
						case ActorType.SHADOW:
							B.Add(TheName(true) + " hisses faintly. ");
							break;
						case ActorType.ORC_GRENADIER:
						case ActorType.ORC_WARMAGE:
							B.Add(TheName(true) + " snarls loudly. ");
							MakeNoise(4);
							break;
						case ActorType.ENTRANCER:
							B.Add(the_name + " stares at you for a moment. ",this);
							break;
						case ActorType.STONE_GOLEM:
							B.Add(the_name + " starts moving. ",this);
							break;
						case ActorType.NECROMANCER:
							B.Add(TheName(true) + " starts chanting in low tones. ");
							break;
						case ActorType.TROLL:
						case ActorType.TROLL_BLOODWITCH:
							B.Add(TheName(true) + " growls viciously. ");
							MakeNoise(4);
							break;
						case ActorType.CARNIVOROUS_BRAMBLE:
						case ActorType.MIMIC:
						case ActorType.MUD_TENTACLE:
						case ActorType.MARBLE_HORROR:
						case ActorType.MARBLE_HORROR_STATUE:
						case ActorType.LASHER_FUNGUS:
							break;
						default:
							B.Add(the_name + " notices you. ",this);
							break;
						}
						Q1();
						no_act = true;
					}
				}
				else{
					if(player_visibility_duration >= 0){ //if they hadn't seen the player yet...
						player_visibility_duration = 0;
					}
					else{
						if(target_location == null && player_visibility_duration-- == -(10+attrs[AttrType.ALERTED]*40)){
							if(attrs[AttrType.ALERTED] < 2){ //they'll forget the player after 10 turns the first time and
								attrs[AttrType.ALERTED]++; //50 turns the second time, but that's the limit
								player_visibility_duration = 0;
								target = null;
							}
						}
					}
				}
			}
			if(type == ActorType.DARKNESS_DWELLER){ //this is checked before & after the dweller moves, but the duration is only updated after.
				if(!HasAttr(AttrType.COOLDOWN_2)){
					if(tile().IsLit()){
						if(player.HasLOS(this)){
							B.Add(the_name + " is blinded by the light! ",this);
						}
						attrs[AttrType.BLIND]++;
						attrs[AttrType.COOLDOWN_2] = 5;
						Q.Add(new Event(this,(R.Roll(2)+4)*100,AttrType.BLIND,the_name + " is no longer blinded. ",this));
					}
				}
			}
			if(type == ActorType.INFESTED_MASS_TODO_NAME && tile().IsLit()){
				RefreshDuration(AttrType.SLOWED,100);
			}
			if(type == ActorType.MARBLE_HORROR && tile().IsLit()){
				B.Add("The marble horror reverts to its statue form. ",this);
				type = ActorType.MARBLE_HORROR_STATUE;
				SetName("marble horror statue");
				attrs[AttrType.IMMOBILE] = 1;
				attrs[AttrType.INVULNERABLE] = 1;
				attrs[AttrType.IMMUNE_FIRE] = 1;
			}
			if(type == ActorType.MARBLE_HORROR_STATUE && !tile().IsLit()){
				B.Add("The marble horror animates once more. ",this);
				type = ActorType.MARBLE_HORROR;
				SetName("marble horror");
				attrs[AttrType.IMMOBILE] = 0;
				attrs[AttrType.INVULNERABLE] = 0;
				attrs[AttrType.IMMUNE_FIRE] = 0;
			}
			if(type == ActorType.CORPSETOWER_BEHEMOTH && tile().Is(TileType.FLOOR)){
				tile().Toggle(null,TileType.GRAVE_DIRT);
				bool found = false;
				foreach(Event e in Q.list){
					if(!e.dead && e.type == EventType.GRAVE_DIRT){
						e.area.Add(tile());
						found = true;
						break;
					}
				}
				if(!found){
					Q.Add(new Event(new List<Tile>{tile()},100,EventType.GRAVE_DIRT));
				}
			}
			if(!no_act && type != ActorType.CULTIST && type != ActorType.CORPSETOWER_BEHEMOTH && type != ActorType.BLOOD_MOTH
			&& type != ActorType.MUD_TENTACLE && type != ActorType.DREAM_WARRIOR_CLONE && type != ActorType.ZOMBIE
			&& type != ActorType.CARNIVOROUS_BRAMBLE){ //todo: double-check this list!
				if(HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
					if(HasAttr(AttrType.CATCHING_FIRE) && R.OneIn(10)){ //todo: redo this: probably only intelligent monsters will
						attrs[AttrType.CATCHING_FIRE] = 0; //put out fires, and only if the player is out of LOS, helpless, or occupied with other monsters adjacent.
						B.Add(the_name + " stops the flames from spreading. ",this);
						Q1();
						no_act = true;
					}
					else{
						/*if(HasAttr(AttrType.BURNING)){
							if(attrs[AttrType.BURNING] == 1 && R.OneIn(4)){
								bool update = false;
								int oldradius = LightRadius();
								if(attrs[AttrType.BURNING] > light_radius){
									update = true;
								}
								attrs[AttrType.BURNING] = 0;
								if(update){
									UpdateRadius(oldradius,LightRadius());
								}
								B.Add(the_name + " puts out the fire. ",this);
								Q1();
								no_act = true;
							}
							else{
								if(attrs[AttrType.BURNING] > 1 && R.Roll(10) <= 8){
									bool update = false;
									int oldradius = LightRadius();
									if(attrs[AttrType.BURNING] > light_radius){
										update = true;
									}
									int i = 2;
									if(R.Roll(1,3) == 3){ // 1 in 3 times, no progress against the fire
										i = 1;
									}
									attrs[AttrType.BURNING] -= i;
									if(attrs[AttrType.BURNING] < 0){
										attrs[AttrType.BURNING] = 0;
									}
									if(update){
										UpdateRadius(oldradius,LightRadius ());
									}
									if(HasAttr(AttrType.BURNING)){
										B.Add(the_name + " puts out some of the fire. ",this);
									}
									else{
										B.Add(the_name + " puts out the fire. ",this);
									}
									Q1();
									no_act = true;
								}
								else{
									if(attrs[AttrType.BURNING] > 2 && R.Roll(2) + attrs[AttrType.BURNING] >= 5){
										if(HasAttr(AttrType.MEDIUM_HUMANOID)){
											B.Add(the_name + " runs around with arms flailing. ",this);
										}
										else{
											B.Add(the_name + " flails about. ",this);
										}
										AI_Step(TileInDirection(Global.RandomDirection()));
										Q1();
										no_act = true;
									}
									else{
										bool update = false;
										int oldradius = LightRadius();
										if(attrs[AttrType.BURNING] > light_radius){
											update = true;
										}
										int i = 2;
										if(R.Roll(1,3) == 3){ // 1 in 3 times, no progress against the fire
											i = 1;
										}
										attrs[AttrType.BURNING] -= i;
										if(attrs[AttrType.BURNING] < 0){
											attrs[AttrType.BURNING] = 0;
										}
										if(update){
											UpdateRadius(oldradius,LightRadius());
										}
										if(HasAttr(AttrType.BURNING)){
											B.Add(the_name + " puts out some of the fire. ",this);
										}
										else{
											B.Add(the_name + " puts out the fire. ",this);
										}
										Q1();
										no_act = true;
									}
								}
							}
						}*/
					}
				}
				else{
					if(HasAttr(AttrType.CATCHING_FIRE) && R.CoinFlip()){
						attrs[AttrType.CATCHING_FIRE] = 0;
						if(type == ActorType.SHADOW){
							B.Add(the_name + " reforms itself to stop the flames. ",this);
						}
						else{
							if(type == ActorType.BANSHEE || type == ActorType.VAMPIRE){
								B.Add(the_name + " stops the flames from spreading. ",this);
							}
							else{
								B.Add(the_name + " rolls on the ground to stop the flames. ",this);
							}
						}
						Q1();
						no_act = true;
					}
					else{
						/*if(HasAttr(AttrType.BURNING) && R.Roll(3) >= 2){
							bool update = false;
							int oldradius = LightRadius();
							if(attrs[AttrType.BURNING] > light_radius){
								update = true;
							}
							int i = 2;
							if(R.Roll(1,3) == 3){ // 1 in 3 times, no progress against the fire
								i = 1;
							}
							attrs[AttrType.BURNING] -= i;
							if(attrs[AttrType.BURNING] < 0){
								attrs[AttrType.BURNING] = 0;
							}
							if(update){
								UpdateRadius(oldradius,LightRadius());
							}
							if(HasAttr(AttrType.BURNING)){
								if(type == ActorType.SHADOW){
									B.Add(the_name + " reforms itself to put out some of the fire. ",this);
								}
								else{
									if(type == ActorType.BANSHEE){
										B.Add(the_name + " puts out some of the fire. ",this);
									}
									else{
										B.Add(the_name + " rolls on the ground to put out some of the fire. ",this);
									}
								}
							}
							else{
								if(type == ActorType.SHADOW){
									B.Add(the_name + " reforms itself to put out the fire. ",this);
								}
								else{
									if(type == ActorType.BANSHEE){
										B.Add(the_name + " puts out the fire. ",this);
									}
									else{
										B.Add(the_name + " rolls on the ground to put out the fire. ",this);
									}
								}
							}
							Q1();
							no_act = true;
						}*/
					}
				}
			}
			if(tile().Is(FeatureType.QUICKFIRE) || tile().Is(FeatureType.POISON_GAS) || (HasAttr(AttrType.LIGHT_SENSITIVE) && tile().IsLit())){
				List<Tile> dangerous_terrain = new List<Tile>();
				bool dangerous_terrain_here = false;
				if(HasAttr(AttrType.LIGHT_SENSITIVE) && target == null){ //ignore this if the vampire sees the player already
					foreach(Tile t in TilesWithinDistance(1)){
						if(t.IsLit() && t.passable){
							dangerous_terrain.Add(t);
							if(t == tile()){
								dangerous_terrain_here = true;
							}
						}
					}
				}
				if(!HasAttr(AttrType.IMMUNE_FIRE) && !HasAttr(AttrType.INVULNERABLE) && !HasAttr(AttrType.IMMUNE_BURNING)){
					if(type != ActorType.ZOMBIE && type != ActorType.CORPSETOWER_BEHEMOTH && type != ActorType.SKELETON
					&& type != ActorType.CULTIST && type != ActorType.PHASE_SPIDER && type != ActorType.MARBLE_HORROR && type != ActorType.MECHANICAL_KNIGHT){
						foreach(Tile t in TilesWithinDistance(1)){
							if(t.Is(FeatureType.QUICKFIRE)){ //todo
								dangerous_terrain.AddUnique(t);
								if(t == tile()){
									dangerous_terrain_here = true;
								}
							}
						}
					}
				}
				if(!HasAttr(AttrType.NONLIVING)){
					if(type != ActorType.CULTIST && type != ActorType.PHASE_SPIDER && type != ActorType.NOXIOUS_WORM){
						foreach(Tile t in TilesWithinDistance(1)){
							if(t.Is(FeatureType.POISON_GAS)){
								dangerous_terrain.AddUnique(t);
								if(t == tile()){
									dangerous_terrain_here = true;
								}
							}
						}
					}
				}
				if(dangerous_terrain_here){
					/*if(target == null || DistanceFrom(target) > 1 || R.CoinFlip()){
					}*/
					List<Tile> safe = new List<Tile>();
					foreach(Tile t in TilesAtDistance(1)){
						if(t.passable && t.actor() == null && !dangerous_terrain.Contains(t)){
							safe.Add(t);
						}
					}
					if(safe.Count > 0){
						if(AI_Step(safe.Random())){
							QS();
							no_act = true;
						}
					}
				}
			}
			if(type == ActorType.MECHANICAL_KNIGHT && attrs[AttrType.COOLDOWN_1] != 1){
				attrs[AttrType.MECHANICAL_SHIELD] = 1; //if the knight dropped its guard, it regains its shield here (unless it has no arms)
			}
			if(group != null && group.Count == 0){ //this shouldn't happen, but does. this stops it from crashing.
				group = null;
			}
			if(!no_act){
				if(target != null){
					if(CanSee(target) || (target == player && aware_of_player)){
						ActiveAI();
					}
					else{
						SeekAI();
					}
				}
				else{
					IdleAI();
				}
			}
			if(type == ActorType.DARKNESS_DWELLER){
				if(HasAttr(AttrType.COOLDOWN_2)){
					if(tile().IsLit()){
						attrs[AttrType.COOLDOWN_2] = 5;
					}
					else{
						attrs[AttrType.COOLDOWN_2]--;
					}
				}
				else{
					if(tile().IsLit()){
						if(player.HasLOS(this)){
							B.Add(the_name + " is blinded by the light! ",this);
						}
						attrs[AttrType.BLIND]++;
						attrs[AttrType.COOLDOWN_2] = 5;
						Q.Add(new Event(this,(R.Roll(2)+4)*100,AttrType.BLIND,the_name + " is no longer blinded. ",this));
					}
				}
			}
			if(type == ActorType.SHADOW){
				CalculateDimming();
			}
			if(type == ActorType.STALKING_WEBSTRIDER && !tile().Is(FeatureType.WEB,FeatureType.FIRE)){
				if(target != null && (CanSee(target) || target == player && aware_of_player)){ //not while wandering, just while chasing the player.
					tile().AddFeature(FeatureType.WEB);
				}
			}
			if(type == ActorType.CLOUD_ELEMENTAL){
				List<Tile> area = new List<Tile>();
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.passable){
						t.AddFeature(FeatureType.FOG);
						area.Add(t);
					}
				}
				List<Tile> area2 = tile().AddGaseousFeature(FeatureType.FOG,2);
				area.AddRange(area2);
				if(area.Count > 0){
					Q.RemoveTilesFromEventAreas(area,EventType.FOG);
					Q.Add(new Event(area,101,EventType.FOG,75));
				}
			}
			if(type == ActorType.NOXIOUS_WORM){
				List<Tile> area = tile().AddGaseousFeature(FeatureType.POISON_GAS,2);
				if(area.Count > 0){
					Q.RemoveTilesFromEventAreas(area,EventType.POISON_GAS);
					Q.Add(new Event(area,200,EventType.POISON_GAS));
				}
			}
		}
		public void ActiveAI(){
			if(path.Count > 0){
				path.Clear();
			}
			switch(type){
			case ActorType.GIANT_BAT:
			case ActorType.PHANTOM_BLIGHTWING:
				if(DistanceFrom(target) == 1){
					int idx = R.Roll(1,2) - 1;
					Attack(idx,target);
					if(R.CoinFlip()){ //chance of retreating
						AI_Step(target,true);
					}
				}
				else{
					if(R.CoinFlip()){
						AI_Step(target);
						QS();
					}
					else{
						AI_Step(TileInDirection(Global.RandomDirection()));
						QS();
					}
				}
				break;
			case ActorType.BLOOD_MOTH:
			{
				PhysicalObject brightest = null;
				if(!M.wiz_lite && !M.wiz_dark){
					List<PhysicalObject> current_brightest = new List<PhysicalObject>();
					foreach(Tile t in M.AllTiles()){
						int pos_radius = t.light_radius;
						PhysicalObject pos_obj = t;
						if(t.inv != null && t.inv.light_radius > pos_radius){
							pos_radius = t.inv.light_radius;
							pos_obj = t.inv;
						}
						if(t.actor() != null && t.actor().LightRadius() > pos_radius){
							pos_radius = t.actor().LightRadius();
							pos_obj = t.actor();
						}
						if(pos_radius > 0){
							if(current_brightest.Count == 0 && CanSee(t)){
								current_brightest.Add(pos_obj);
							}
							else{
								foreach(PhysicalObject o in current_brightest){
									if(pos_radius > o.light_radius){
										if(CanSee(t)){
											current_brightest.Clear();
											current_brightest.Add(pos_obj);
											break;
										}
									}
									else{
										if(pos_radius == o.light_radius && DistanceFrom(t) < DistanceFrom(o)){
											if(CanSee(t)){
												current_brightest.Clear();
												current_brightest.Add(pos_obj);
												break;
											}
										}
										else{
											if(pos_radius == o.light_radius && DistanceFrom(t) == DistanceFrom(o) && pos_obj == player){
												if(CanSee(t)){
													current_brightest.Clear();
													current_brightest.Add(pos_obj);
													break;
												}
											}
										}
									}
								}
							}
						}
					}
					if(current_brightest.Count > 0){
						brightest = current_brightest.Random();
					}
				}
				if(brightest != null){
					if(DistanceFrom(brightest) <= 1){
						if(brightest == target){
							Attack(0,target);
							if(target == player && player.curhp > 0){
								Help.TutorialTip(TutorialTopic.Torch);
							}
						}
						else{
							List<Tile> open = new List<Tile>();
							foreach(Tile t in TilesAtDistance(1)){
								if(t.DistanceFrom(brightest) <= 1 && t.passable && t.actor() == null){
									open.Add(t);
								}
							}
							if(open.Count > 0){
								AI_Step(open.Random());
							}
							QS();
						}
					}
					else{
						AI_Step(brightest);
						QS();
					}
				}
				else{
					int dir = Global.RandomDirection();
					if(TilesAtDistance(1).Where(t => !t.passable).Count > 4 && !TileInDirection(dir).passable){
						dir = Global.RandomDirection();
					}
					if(TileInDirection(dir).passable && ActorInDirection(dir) == null){
						AI_Step(TileInDirection(dir));
						QS();
					}
					else{
						if(curhp < maxhp && ActorInDirection(dir) == target){
							Attack(0,target);
						}
						else{
							if(player.HasLOS(TileInDirection(dir))){
								if(!TileInDirection(dir).passable){
									B.Add(the_name + " brushes up against " + TileInDirection(dir).the_name + ". ",this);
								}
								else{
									if(ActorInDirection(dir) != null){
										B.Add(the_name + " brushes up against " + ActorInDirection(dir).TheName(true) + ". ",this);
									}
								}
							}
							QS();
						}
					}
				}
				break;
			}
			/*case ActorType.DARKNESS_DWELLER:
				if(HasAttr(AttrType.BLIND)){
					int dir = Global.RandomDirection();
					if(!TileInDirection(dir).passable){
						B.Add(You("stagger") + " into " + TileInDirection(dir).the_name + ". ",this);
					}
					else{
						if(ActorInDirection(dir) != null){
							B.Add(YouVisible("stagger") + " into " + ActorInDirection(dir).TheName(true) + ". ",this,ActorInDirection(dir));
						}
						else{
							if(GrabPreventsMovement(TileInDirection(dir))){
								B.Add(the_name + " staggers and almost falls over. ",this);
							}
							else{
								B.Add(You("stagger") + ". ",this);
								Move(TileInDirection(dir).row,TileInDirection(dir).col);
							}
						}
					}
					QS();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;*/
			case ActorType.CARNIVOROUS_BRAMBLE:
			case ActorType.MUD_TENTACLE:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(target == player && player.curhp > 0){
						Help.TutorialTip(TutorialTopic.RangedAttacks);
					}
				}
				else{
					QS();
				}
				break;
			case ActorType.FROSTLING:
				if(target.HasAttr(AttrType.FROZEN)){
					if(DistanceFrom(target) < 6){
						AI_Step(target,true);
					}
					else{
						if(DistanceFrom(target) > 6){
							AI_Step(target);
						}
						else{
							AI_Sidestep(target);
						}
					}
					QS();
				}
				else{
					if(DistanceFrom(target) == 1){
						if(!HasAttr(AttrType.COOLDOWN_2)){ //burst attack cooldown
							RefreshDuration(AttrType.COOLDOWN_2,R.Between(13,16)*100);
							AnimateExplosion(this,1,Color.RandomIce,'*');
							B.Add(the_name + " releases a burst of cold. ",this);
							foreach(Actor a in ActorsAtDistance(1)){
								if(a.TakeDamage(DamageType.COLD,DamageClass.PHYSICAL,R.Roll(2,6),this,"a frostling")){
									a.attrs[AttrType.FROZEN] = 35;
									B.Add(a.YouAre() + " encased in ice. ",a);
								}
							}
							Q1();
						}
						else{
							if(R.CoinFlip()){
								Attack(0,target);
							}
							else{
								if(AI_Step(target,true)){
									QS();
								}
								else{
									Attack(0,target);
								}
							}
						}
					}
					else{
						if(FirstActorInLine(target) == target && !HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 6){
							int cooldown = R.Roll(1,4);
							if(cooldown != 1){
								RefreshDuration(AttrType.COOLDOWN_1,cooldown*100);
							}
							AnimateBoltProjectile(target,Color.RandomIce);
							if(R.CoinFlip()){
								B.Add(TheName(true) + " hits " + target.the_name + " with a blast of cold. ",target);
								target.TakeDamage(DamageType.COLD,DamageClass.PHYSICAL,R.Roll(2,6),this,"a frostling");
							}
							else{
								B.Add(TheName(true) + " misses " + target.the_name + " with a blast of cold. ",target);
							}
							Q1();
						}
						else{
							if(!HasAttr(AttrType.COOLDOWN_2)){
								AI_Step(target);
							}
							else{
								AI_Sidestep(target); //message for this? hmm.
							}
							QS();
						}
					}
				}
				break;
			case ActorType.SWORDSMAN:
			case ActorType.PHANTOM_SWORDMASTER:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					attrs[AttrType.COMBO_ATTACK] = 0;
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.DREAM_WARRIOR:
				if(DistanceFrom(target) == 1){
					if(curhp <= 18 && !HasAttr(AttrType.COOLDOWN_1)){
						attrs[AttrType.COOLDOWN_1]++;
						List<Tile> openspaces = new List<Tile>();
						foreach(Tile t in target.TilesAtDistance(1)){
							if(t.passable && t.actor() == null){
								openspaces.Add(t);
							}
						}
						foreach(Tile t in openspaces){
							if(group == null){
								group = new List<Actor>{this};
							}
							Create(ActorType.DREAM_WARRIOR_CLONE,t.row,t.col,true,true);
							t.actor().player_visibility_duration = -1;
							t.actor().attrs[AttrType.NO_ITEM]++;
							group.Add(M.actor[t.row,t.col]);
							M.actor[t.row,t.col].group = group;
							group.Randomize();
						}
						openspaces.Add(tile());
						Tile newtile = openspaces[R.Roll(openspaces.Count)-1];
						if(newtile != tile()){
							Move(newtile.row,newtile.col,false);
						}
						if(openspaces.Count > 1){
							B.Add(the_name + " is suddenly standing all around " + target.the_name + ". ");
							Q1();
						}
						else{
							Attack(0,target);
						}
					}
					else{
						Attack(0,target);
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.SPITTING_COBRA: //todo: ignores you until you're 6 tiles away
				if(DistanceFrom(target) <= 3 && !HasAttr(AttrType.COOLDOWN_1) && FirstActorInLine(target) == target){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(50,75)*100);
					B.Add(TheName(true) + " spits poison in " + target.YourVisible() + " eyes! ",this,target);
					AnimateBoltProjectile(target,Color.DarkGreen);
					B.Add(target.YouAre() + " blind! ",target);
					target.RefreshDuration(AttrType.BLIND,R.Between(5,8)*100,target.YouAre() + " no longer blind. ",target);
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						List<Tile> tiles = new List<Tile>();
						if(target.row == row || target.col == col){
							int targetdir = DirectionOf(target);
							for(int i=-1;i<=1;++i){
								pos adj = p.PosInDir(targetdir.RotateDir(true,i));
								if(M.tile[adj].passable && M.actor[adj] == null){
									tiles.Add(M.tile[adj]);
								}
							}
						}
						if(tiles.Count > 0){
							AI_Step(tiles.Random());
						}
						else{
							AI_Step(target);
						}
						QS();
					}
				}
				break;
			case ActorType.KOBOLD:
				if(!HasAttr(AttrType.COOLDOWN_1)){
					if(DistanceFrom(target) > 12){
						AI_Step(target);
						QS();
					}
					else{
						if(FirstActorInLine(target) != target){
							AI_Sidestep(target);
							QS();
						}
						else{
							attrs[AttrType.COOLDOWN_1]++;
							AnimateBoltProjectile(target,Color.DarkCyan);
							if(player.CanSee(this)){
								B.Add(the_name + " fires a dart at " + target.the_name + ". ",this,target);
							}
							else{
								B.Add("A dart hits " + target.the_name + "! ",target);
								if(player.CanSee(tile())){
									B.Add("You notice " + a_name + ". ",tile());
									attrs[AttrType.TURNS_VISIBLE] = -1;
								}
							}
							if(target.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),this,"a kobold's dart")){
								if(!target.HasAttr(AttrType.VULNERABLE)){
									B.Add(target.YouFeel() + " vulnerable. ",target);
								}
								target.RefreshDuration(AttrType.VULNERABLE,R.Between(2,4)*100,target.YouFeel() + " less vulnerable. ",target);
							}
							Q1();
						}
					}
				}
				else{
					if(DistanceFrom(target) <= 2){
						AI_Flee(); //todo? all good here? test this I guess.
						QS();
					}
					else{
						B.Add(the_name + " starts reloading. ",this);
						attrs[AttrType.COOLDOWN_1] = 0;
						Q.Add(new Event(this,R.Between(5,6)*100,EventType.MOVE));
					}
				}
				break;
			case ActorType.SPORE_POD:
				if(DistanceFrom(target) == 1){
					TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,100,null);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.FORASECT:
			{
				bool burrow = false;
				if((curhp * 2 <= maxhp || DistanceFrom(target) > 6) && R.CoinFlip()){
					burrow = true;
				}
				if(DistanceFrom(target) <= 6 && DistanceFrom(target) > 1){
					if(R.OneIn(10)){
						burrow = true;
					}
				}
				if(burrow && !HasAttr(AttrType.COOLDOWN_1)){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(8,11)*100);
					if(curhp * 2 <= maxhp){
						Burrow(TilesWithinDistance(6)); //todo
					}
					else{
						Burrow(GetCone(DirectionOf(target),6,true));
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.POLTERGEIST:
				if(inv.Count == 0){
					if(DistanceFrom(target) == 1){
						int target_r = target.row;
						int target_c = target.col;
						if(Attack(0,target) && M.actor[target_r,target_c] != null && target.inv.Any(i=>!i.do_not_stack)){
							Item item = target.inv.Where(i=>!i.do_not_stack).Random();
							if(item.quantity > 1){
								inv.Add(new Item(item,-1,-1));
								item.quantity--;
							}
							else{
								inv.Add(item);
								target.inv.Remove(item);
							}
							B.Add(YouVisible("steal") + " " + target.YourVisible() + " " + item.Name() + "! ",this,target);
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				else{
					List<Tile> line = target.GetBestExtendedLineOfEffect(this);
					Tile next = null;
					bool found = false;
					foreach(Tile t in line){
						if(found){
							next = t;
							break;
						}
						else{
							if(t.actor() == this){
								found = true;
							}
						}
					}
					if(next != null){
						if(next.passable && next.actor() == null && AI_Step(next)){
							QS();
						}
						else{
							if(!next.passable){
								B.Add(the_name + " disappears into " + next.the_name + ". ",this);
								foreach(Tile t in TilesWithinDistance(1)){
									if(t.DistanceFrom(next) == 1 && t.name == "floor"){
										t.features.Add(FeatureType.SLIME);
									}
								}
								Event e = null;
								foreach(Event e2 in Q.list){
									if(e2.target == this && e2.type == EventType.POLTERGEIST){
										e = e2;
										break;
									}
								}
								e.target = inv[0];
								Actor.tiebreakers[e.tiebreaker] = null;
								inv.Clear();
								Kill();
							}
							else{
								if(next.actor() != null){
									if(!next.actor().HasAttr(AttrType.IMMOBILE)){
										Move(next.row,next.col);
										QS();
									}
									else{
										if(next.actor().HasAttr(AttrType.IMMOBILE)){
											if(AI_Step(next)){
												QS();
											}
											else{
												if(DistanceFrom(target) == 1){
													Attack(1,target);
												}
												else{
													QS();
												}
											}
										}
									}
								}
								else{
									QS();
								}
							}
						}
					}
				}
				break;
			case ActorType.CULTIST:
				if(curhp <= 10 && !HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					string invocation;
					switch(R.Roll(4)){
					case 1:
						invocation = "ae vatra kersai";
						break;
					case 2:
						invocation = "kersai dzaggath";
						break;
					case 3:
						invocation = "od fir od bahgal";
						break;
					case 4:
						invocation = "denei kersai nammat";
						break;
					default:
						invocation = "denommus pilgni";
						break;
					}
					if(R.CoinFlip()){
						B.Add(You("whisper") + " '" + invocation + "'. ",this);
					}
					else{
						B.Add(You("scream") + " '" + invocation.ToUpper() + "'. ",this);
					}
					B.Add("Flames erupt from " + the_name + ". ",this);
					AnimateExplosion(this,1,Color.RandomFire,'*');
					/*if(LightRadius() < 2){
						UpdateRadius(LightRadius(),2);
					}
					attrs[AttrType.BURNING] = Math.Max(attrs[AttrType.BURNING],2);*/
					if(LightRadius() < 1){
						UpdateRadius(LightRadius(),1);
					}
					RefreshDuration(AttrType.BURNING,(R.Roll(3)+4) * 100,YouAre() + " no longer on fire. ",this);
					//attrs[AttrType.BURNING]++;
					foreach(Actor a in ActorsAtDistance(1)){
						if(!a.HasAttr(AttrType.IMMUNE_BURNING) && !a.HasAttr(AttrType.IMMUNE_FIRE)
						&& !a.HasAttr(AttrType.BURNING) && !a.HasAttr(AttrType.CATCHING_FIRE)
						&& !a.HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){
							if(a.name == "you"){
								B.Add("You start to catch fire! ");
							}
							else{
								B.Add(a.the_name + " starts to catch fire. ",a);
							}
							a.attrs[AttrType.CATCHING_FIRE] = 1;
						}
					}
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.GOBLIN_ARCHER:
			case ActorType.PHANTOM_ARCHER:
				switch(DistanceFrom(target)){
				case 1:
					if(target.EnemiesAdjacent() > 1){
						Attack(0,target);
					}
					else{
						if(AI_Flee()){ //todo test
							QS();
						}
						else{
							Attack(0,target);
						}
					}
					break;
				case 2:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						if(AI_Flee()){ //todo test
							QS();
						}
						else{ 
							if(AI_Sidestep(target)){
								B.Add(the_name + " tries to line up a shot. ",this);
							}
							QS();
						}
					}
					break;
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						if(AI_Sidestep(target)){
							B.Add(the_name + " tries to line up a shot. ",this);
						}
						QS();
					}
					break;
				default:
					AI_Step(target);
					QS();
					break;
				}
				break;
			case ActorType.GOBLIN_SHAMAN:
			{
				foreach(Actor a in ActorsWithinDistance(2)){
					if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
						return;
					}
				}
				if(DistanceFrom(target) == 1){
					if(exhaustion > 10){
						Attack(0,target);
					}
					else{
						CastCloseRangeSpellOrAttack(target);
						/*if(R.CoinFlip()){
							CastSpell(SpellType.SCORCH,target);
						}
						else{
							CastSpell(SpellType.FORCE_PALM,target);
						}*/
					}
				}
				else{
					if(FirstActorInLine(target) != target || R.CoinFlip()){
						AI_Step(target);
						QS();
					}
					else{
						CastRangedSpellOrMove(target);
						//CastSpell(SpellType.SCORCH,target);
					}
				}
				break;
			}
			case ActorType.PHASE_SPIDER:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					Tile t = target.TilesAtDistance(DistanceFrom(target)-1).Where(x=>x.passable && x.actor() == null).Random();
					if(t != null){
						Move(t.row,t.col);
					}
					QS();
				}
				break;
			case ActorType.ZOMBIE:
			case ActorType.PHANTOM_ZOMBIE:
				if(DistanceFrom(target) == 1){
					Attack(1,target);
				}
				else{
					AI_Step(target);
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						QS();
					}
				}
				break;
			case ActorType.ROBED_ZEALOT:
				if(HasAttr(AttrType.COOLDOWN_2)){
					attrs[AttrType.COOLDOWN_2] = 0;
					B.Add(the_name + " finishes the prayer. ",this);
					B.Add(the_name + " looks healthier. ",this);
					curhp += maxhp/2;
					if(curhp > maxhp){
						curhp = maxhp;
					}
					Q1();
				}
				else{
					if((maxhp / 5) * 4 > curhp && !HasAttr(AttrType.COOLDOWN_1)){
						RefreshDuration(AttrType.COOLDOWN_1,R.Between(11,13)*100);
						attrs[AttrType.COOLDOWN_2]++;
						B.Add(the_name + " starts praying. ",this);
						B.Add("A fiery halo appears above " + the_name + ". ",this);
						RefreshDuration(AttrType.HOLY_SHIELDED,R.Between(5,7)*100,Your() + " halo fades. ",this); //todo: consider shorter messages
						Q1();
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				break;
			case ActorType.GIANT_SLUG:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12 && FirstActorInLine(target) == target){
						RefreshDuration(AttrType.COOLDOWN_1,R.Between(11,14)*100);
						B.Add(TheName(true) + " spits slime at " + target.the_name + ". ",target);
						List<Tile> slimed = GetBestLineOfEffect(target);
						List<Tile> added = new List<Tile>();
						foreach(Tile t in slimed){
							foreach(int dir in U.FourDirections){
								Tile neighbor = t.TileInDirection(dir);
								if(R.OneIn(3) && neighbor.passable && !slimed.Contains(neighbor)){
									added.AddUnique(neighbor);
								}
							}
						}
						slimed.AddRange(added);
						for(int i=0;slimed.Count > 0;++i){
							List<Tile> removed = new List<Tile>();
							foreach(Tile t in slimed){
								if(DistanceFrom(t) == i){
									t.AddFeature(FeatureType.SLIME);
									removed.Add(t);
								}
							}
							foreach(Tile t in removed){
								slimed.Remove(t);
							}
							M.Draw();
						}
						/*foreach(Tile t in slimed){
							t.AddFeature(FeatureType.SLIME);
							M.Draw();
							//Screen.AnimateMapCell(t.row,t.col,new colorchar('!',Color.Green));
						}*/
						if(target.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(2,6),this,"a giant slug")){
							target.attrs[AttrType.SLIMED]++;
							B.Add(target.YouAre() + " covered in slime. ",target);
						}
						Q1();
					}
					else{
						AI_Step(target);
						if(tile().Is(FeatureType.SLIME)){
							Q1(); //normal speed is 150
						}
						else{
							QS();
						}
					}
				}
				break;
			case ActorType.BANSHEE:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(13,15)*100);
					if(player.CanSee(this)){
						B.Add(You("scream") + ". ",this);
					}
					else{
						B.Add("You hear a scream! ");
					}
					if(!target.HasAttr(AttrType.NONLIVING)){
						B.Add(target.YouAre() + " terrified! ",target);
						RefreshDuration(AttrType.TERRIFYING,target.DurationOfMagicalEffect(R.Between(5,8)*100),target.YouAre() + " no longer afraid. ",target);
					}
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.CAVERN_HAG:
				if(curhp < maxhp && !HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12){
					B.Add(the_name + " curses you! ");
					switch(R.Roll(4)){
					case 1: //light allergy
						B.Add("You become allergic to light! ");
						target.RefreshDuration(AttrType.LIGHT_SENSITIVE,10000,"You are no longer allergic to light. ");
						break;
					case 2: //heavy weapon
						B.Add("Your " + target.EquippedWeapon + " suddenly feels much heavier. ");
						target.EquippedWeapon.status[EquipmentStatus.BURDENSOME] = true; //todo: implement
						break;
					case 3: //aggravate monsters
						B.Add("Every sound you make becomes amplified and echoes across the dungeon. ");
						target.RefreshDuration(AttrType.AGGRAVATING,10000,"Your sounds are no longer amplified. ");
						break;
					case 4: //cursed weapon
						B.Add("Your " + target.EquippedWeapon + " becomes stuck to your hand! ");
						target.EquippedWeapon.status[EquipmentStatus.CURSED] = true;
						break;
					}
					attrs[AttrType.COOLDOWN_1]++;
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.BERSERKER:
				if(HasAttr(AttrType.COOLDOWN_2)){
					int dir = attrs[AttrType.COOLDOWN_2];
					bool cw = R.CoinFlip();
					if(TileInDirection(dir).passable && ActorInDirection(dir) == null && !GrabPreventsMovement(TileInDirection(dir))){
						B.Add(the_name + " leaps forward swinging his axe! ",this);
						Move(TileInDirection(dir).row,TileInDirection(dir).col);
						Actor a = ActorInDirection(dir.RotateDir(cw));
						if(a != null){
							B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),this,"a berserker's axe");
						}
						a = ActorInDirection(dir);
						if(a != null){
							B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),this,"a berserker's axe");
						}
						a = ActorInDirection(dir.RotateDir(!cw));
						if(a != null){
							B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),this,"a berserker's axe");
						}
						Q1();
					}
					else{
						if(ActorInDirection(dir) != null || GrabPreventsMovement(TileInDirection(dir))){
							B.Add(the_name + " swings his axe furiously! ",this);
							Actor a = ActorInDirection(dir.RotateDir(cw));
							if(a != null){
								B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),this,"a berserker's axe");
							}
							a = ActorInDirection(dir);
							if(a != null){
								B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),this,"a berserker's axe");
							}
							a = ActorInDirection(dir.RotateDir(!cw));
							if(a != null){
								B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6),this,"a berserker's axe");
							}
							Q1();
						}
						else{
							B.Add(the_name + " turns to face " + target.the_name + ". ",this);
							attrs[AttrType.COOLDOWN_2] = DirectionOf(target);
							Q1();
						}
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
						if(target != null && R.Roll(3) == 3){
							B.Add(the_name + " screams with fury! ",this);
							attrs[AttrType.COOLDOWN_2] = DirectionOf(target);
							Q.Add(new Event(this,350,AttrType.COOLDOWN_2,Your() + " rage diminishes. ",this));
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.DIRE_RAT:
			{
				bool slip_past = false;
				if(DistanceFrom(target) == 1){
					foreach(Actor a in ActorsAtDistance(1)){
						if(a.type == ActorType.DIRE_RAT && a.DistanceFrom(target) > this.DistanceFrom(target)){
							bool can_walk = false;
							foreach(Tile t in a.TilesAtDistance(1)){
								if(t.DistanceFrom(target) < a.DistanceFrom(target) && t.passable && t.actor() == null){
									can_walk = true;
									break;
								}
							}
							if(!can_walk){ //there's a rat that would benefit from a space opening up - now check to see whether a move is possible
								foreach(Tile t in target.TilesAtDistance(1)){
									if(t.passable && t.actor() == null){
										slip_past = true;
										break;
									}
								}
								break;
							}
						}
					}
				}
				if(slip_past){
					bool moved = false;
					foreach(Tile t in TilesAtDistance(1)){
						if(t.DistanceFrom(target) == 1 && t.passable && t.actor() == null){
							AI_Step(t);
							QS();
							moved = true;
							break;
						}
					}
					if(!moved){
						Tile t = target.TilesAtDistance(1).Where(x=>x.passable && x.actor() == null).Random();
						if(t != null){
							B.Add(the_name + " slips past you. ",this);
							Move(t.row,t.col);
							Q.Add(new Event(this,Speed() + 100,EventType.MOVE));
						}
						else{
							QS();
						}
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.SKULKING_KILLER:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 3){
					attrs[AttrType.COOLDOWN_1]++;
					AnimateProjectile(target,Color.DarkYellow,'%');
					if(target.CanSee(this)){
						B.Add(the_name + " throws a bola at " + target.the_name + ". ",this,target);
					}
					else{
						B.Add("A bola whirls toward " + target.the_name + ". ",this,target);
					}
					attrs[AttrType.TURNS_VISIBLE] = -1;
					target.RefreshDuration(AttrType.SLOWED,(R.Roll(3)+6)*100,target.YouAre() + " no longer slowed. ",target);
					B.Add(target.YouAre() + " slowed by the bola. ",target);
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.WILD_BOAR:
				if(DistanceFrom(target) == 1){ //todo: nonaggressive
					Attack(0,target); //todo: make sure that the fling part is handled in Attack()
					if(HasAttr(AttrType.JUST_FLUNG)){ //if it just flung its target...
						attrs[AttrType.JUST_FLUNG] = 0;
						attrs[AttrType.COOLDOWN_1] = 0;
					}
					else{ //...otherwise it might prepare to fling again
						if(!HasAttr(AttrType.COOLDOWN_1)){
							if(!HasAttr(AttrType.COOLDOWN_2) || R.OneIn(5)){
								attrs[AttrType.COOLDOWN_2]++;
								B.Add(the_name + " lowers its head. ",this);
								attrs[AttrType.COOLDOWN_1]++;
							}
						}
					}
				}
				else{
					AI_Step(target);
					if(!HasAttr(AttrType.COOLDOWN_2)){
						attrs[AttrType.COOLDOWN_2]++;
						B.Add(the_name + " lowers its head. ",this);
						attrs[AttrType.COOLDOWN_1]++;
					}
					QS();
				}
				break;
			case ActorType.DREAM_SPRITE:
				if(!HasAttr(AttrType.COOLDOWN_1)){
					if(DistanceFrom(target) <= 12 && FirstActorInLine(target) == target){
						RefreshDuration(AttrType.COOLDOWN_1,R.Between(3,4)*100);
						bool visible = false;
						List<List<Tile>> lines = new List<List<Tile>>{GetBestLineOfEffect(target)};
						if(group != null && group.Count > 0){
							foreach(Actor a in group){
								if(target == player && player.CanSee(a)){
									visible = true;
								}
								if(a.type == ActorType.DREAM_SPRITE_CLONE){
									a.attrs[AttrType.COOLDOWN_1]++; //for them, it means 'skip next turn'
									if(a.FirstActorInLine(target) == target){
										lines.Add(a.GetBestLineOfEffect(target));
									}
								}
							}
						}
						foreach(List<Tile> line in lines){
							if(line.Count > 0){
								line.RemoveAt(0);
							}
						}
						if(visible){
							B.Add(the_name + " hits " + target.the_name + " with stinging magic. ",target);
						}
						else{
							B.Add(TheName(true) + " hits " + target.the_name + " with stinging magic. ",target);
						}
						int max = lines.WhereGreatest(x=>x.Count)[0].Count;
						for(int i=0;i<max;++i){
							List<pos> cells = new List<pos>();
							foreach(List<Tile> line in lines){
								if(line.Count > i){
									cells.Add(line[i].p);
								}
							}
							Screen.AnimateMapCells(cells,new colorchar('*',Color.RandomRainbow));
						}
						target.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(2,6),this,"a blast of fairy magic");
						Q1();
					}
					else{
						if(DistanceFrom(target) > 12){
							AI_Step(target);
						}
						else{
							AI_Sidestep(target);
						}
						QS();
					}
				}
				else{
					if(DistanceFrom(target) > 5){
						AI_Step(target);
					}
					else{
						if(DistanceFrom(target) < 3){
							AI_Flee(); //todo test
						}
						else{
							Tile t = TilesAtDistance(1).Where(x=>x.passable && x.actor() == null).Random();
							if(t != null){
								AI_Step(t);
							}
						}
					}
					QS();
				}
				break;
			case ActorType.DREAM_SPRITE_CLONE:
				if(HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1] = 0;
					Q1();
				}
				else{
					if(DistanceFrom(target) > 5){
						AI_Step(target);
					}
					else{
						if(DistanceFrom(target) < 3){
							AI_Flee(); //todo test
						}
						else{
							Tile t = TilesAtDistance(1).Where(x=>x.passable && x.actor() == null).Random();
							if(t != null){
								AI_Step(t);
							}
						}
					}
					QS();
				}
				break;
			case ActorType.CLOUD_ELEMENTAL:
			{
				List<pos> cloud = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].features.Contains(FeatureType.FOG));
				if(cloud.Contains(target.p)){
					B.Add(TheName(true) + " electrifies the cloud! ");
					foreach(pos p2 in cloud){
						if(M.actor[p2] != null && M.actor[p2] != this){
							M.actor[p2].TakeDamage(DamageType.ELECTRIC,DamageClass.PHYSICAL,R.Roll(3,6),this,"*electrocuted by a cloud elemental"); //todo verify this
						}
					}
					Screen.AnimateMapCells(cloud,new colorchar('*',Color.RandomLightning),50);
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Tile t = TilesAtDistance(1).Where(x=>x.actor() == null && x.passable).Random();
						if(t != null){
							AI_Step(t);
						}
						QS();
					}
					else{
						if(R.OneIn(4)){
							Tile t = TilesAtDistance(1).Where(x=>x.actor() == null && x.passable).Random();
							if(t != null){
								AI_Step(t);
							}
							QS();
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				break;
			}
			case ActorType.DERANGED_ASCETIC:
				if(DistanceFrom(target) == 1){
					Attack(R.Roll(3)-1,target);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.SHADOWVEIL_DUELIST:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(target != null){
						List<Tile> valid_dirs = new List<Tile>();
						foreach(Tile t in target.TilesAtDistance(1)){
							if(t.passable && t.actor() == null && DistanceFrom(t) == 1){
								valid_dirs.Add(t);
							}
						}
						if(valid_dirs.Count > 0){
							AI_Step(valid_dirs.Random());
						}
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.WARG:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					if(AI_Step(target)){
						QS();
					}
					else{
						if(player.DistanceFrom(this) <= 12){
							B.Add(TheName(true) + " howls. ");
						}
						foreach(Actor a in ActorsWithinDistance(12)){
							if(a.type == ActorType.WARG && a != this){
								if(a.attrs[AttrType.WARG_HOWL] == 0 && a.DistanceFrom(target) <= 2){
									B.Add(a.the_name + " is emboldened. ",a);
								}
								a.RefreshDuration(AttrType.WARG_HOWL,100); //todo: make warg_howl increase crit chance
							}
						}
					}
				}
				break;
			case ActorType.RUNIC_TRANSCENDENT:
			{
				foreach(Actor a in ActorsWithinDistance(2)){
					if((a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)) || !HasSpell(SpellType.MERCURIAL_SPHERE)){
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
						return;
					}
				}
				if(curmp < 2){
					B.Add(the_name + " absorbs mana from the universe. ",this);
					curmp = maxmp;
					Q1();
				}
				else{
					Tile t = TilesAtDistance(1).Where(x=>x.DistanceFrom(target) == 3 && x.passable && x.actor() == null).WhereLeast(x=>M.safetymap[x.p]).Random();
					if(t != null){ //check safety map. if there's a safer spot at distance 3, step there.
						AI_Step(t);
					}
					else{
						if(DistanceFrom(target) > 3){
							AI_Step(target);
						}
						else{
							if(DistanceFrom(target) < 3){
								AI_Flee(); //todo test
							}
						}
					}
					if(FirstActorInLine(target) != null && FirstActorInLine(target).DistanceFrom(target) <= 3){
						CastSpell(SpellType.MERCURIAL_SPHERE,target);
					}
					else{
						QS();
					}
				}
				break;
			}
			case ActorType.CARRION_CRAWLER:
				if(DistanceFrom(target) == 1){
					if(target.HasAttr(AttrType.PARALYZED)){
						Attack(0,target);
					}
					else{
						Attack(1,target);
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.MECHANICAL_KNIGHT:
				if(attrs[AttrType.COOLDOWN_1] == 3){ //no head
					int dir = Global.RandomDirection();
					if(R.CoinFlip()){
						Actor a = ActorInDirection(dir);
						if(a != null){
							if(!Attack(0,a)){
								B.Add(the_name + " drops its guard! ",this);
								attrs[AttrType.MECHANICAL_SHIELD] = 0;
							}
						}
						else{
							B.Add(the_name + " attacks empty space. ",this);
							B.Add(the_name + " drops its guard! ",this);
							attrs[AttrType.MECHANICAL_SHIELD] = 0;
							Q1();
						}
					}
					else{
						Tile t = TileInDirection(dir);
						if(t.passable){
							if(t.actor() == null){
								AI_Step(t);
								QS();
							}
							else{
								B.Add(the_name + " bumps into " + t.actor().TheName(true) + ". ",this);
								QS();
							}
						}
						else{
							B.Add(the_name + " bumps into " + t.TheName(true) + ". ",this);
							QS();
						}
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						if(attrs[AttrType.COOLDOWN_1] == 1){ //no arms
							Attack(1,target);
						}
						else{
							if(!Attack(0,target)){
								B.Add(the_name + " drops its guard! ",this);
								attrs[AttrType.MECHANICAL_SHIELD] = 0;
							}
						}
					}
					else{
						if(attrs[AttrType.COOLDOWN_1] != 2){ //no legs
							AI_Step(target);
						}
						QS();
					}
				}
				break;
			case ActorType.ALASI_BATTLEMAGE:
				foreach(Actor a in ActorsWithinDistance(2)){
					if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
						return;
					}
				}
				if(DistanceFrom(target) > 12){
					AI_Step(target);
					QS();
				}
				else{
					if(DistanceFrom(target) == 1){
						if(exhaustion < 10){
							CastCloseRangeSpellOrAttack(null,target,true);
							/*List<SpellType> shortrange = new List<SpellType>();
							foreach(SpellType sp in new SpellType[]{SpellType.FORCE_PALM,SpellType.MAGIC_HAMMER}){
								if(HasSpell(sp)){
									shortrange.Add(sp);
								}
							}
							if(shortrange.Count > 0){
								CastRandomSpell(target,shortrange.ToArray());
							}
							else{
								Attack(0,target);
							}*/
						}
						else{
							Attack(0,target);
						}
					}
					else{
						CastRangedSpellOrMove(target);
						/*List<SpellType> midrange = new List<SpellType>();
						foreach(SpellType sp in new SpellType[]{SpellType.MERCURIAL_SPHERE,SpellType.FREEZE,SpellType.SCORCH,SpellType.LIGHTNING_BOLT,SpellType.GLACIAL_BLAST}){
							if(HasSpell(sp)){
								midrange.Add(sp);
							}
						}
						if(DistanceFrom(target) <= 5 && HasSpell(SpellType.BLIZZARD)){
							midrange.Add(SpellType.BLIZZARD);
						}
						if(DistanceFrom(target) <= 11 && HasSpell(SpellType.FIRE_BLITZ)){
							midrange.Add(SpellType.FIRE_BLITZ);
						}
						if(midrange.Count > 0){
							CastRandomSpell(target,midrange.ToArray());
						}
						else{
							AI_Step(target);
							QS();
						}*/
					}
				}
				break;
			case ActorType.ALASI_SOLDIER:
				if(DistanceFrom(target) > 2){
					AI_Step(target);
					QS();
					attrs[AttrType.COMBO_ATTACK] = 0;
				}
				else{
					if(FirstActorInLine(target) != null && !FirstActorInLine(target).name.Contains("alasi")){ //I had planned to make this attack possibly hit multiple targets, but not yet.
						Attack(0,target);
					}
					else{
						if(AI_Step(target)){
							QS();
						}
						else{
							AI_Sidestep(target);
							QS();
						}
						attrs[AttrType.COMBO_ATTACK] = 0;
					}
				}
				break;
			case ActorType.ALASI_SCOUT:
				if(curhp == maxhp){
					if(FirstActorInLine(target) == target){
						Attack(1,target);
					}
					else{
						AI_Sidestep(target);
						QS();
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.MUD_ELEMENTAL:
			{
				int count = 0;
				int walls = 0;
				foreach(Tile t in target.TilesAtDistance(1)){
					if(t.type == TileType.WALL){
						++walls;
						if(t.actor() == null){
							++count;
						}
					}
				}
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12 && count >= 2 || (count == 1 && walls == 1)){
					RefreshDuration(AttrType.COOLDOWN_1,150);
					foreach(Tile t in target.TilesAtDistance(1)){
						if(t.type == TileType.WALL && t.actor() == null){
							Create(ActorType.MUD_TENTACLE,t.row,t.col,true,true);
							M.actor[t.p].player_visibility_duration = -1;
							M.actor[t.p].attrs[AttrType.COOLDOWN_1] = 20;
						}
					}
					if(count >= 2){
						B.Add("Mud tentacles emerge from the walls! ");
					}
					else{
						B.Add("A mud tentacle emerges from the wall! ");
					}
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.FLAMETONGUE_TOAD:
			{
				bool burrow = false;
				if((curhp * 3 <= maxhp || DistanceFrom(target) > 6) && R.CoinFlip()){
					burrow = true;
				}
				if(DistanceFrom(target) <= 6 && DistanceFrom(target) > 1){
					if(R.OneIn(20)){
						burrow = true;
					}
				}
				if(burrow && !HasAttr(AttrType.COOLDOWN_1)){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(12,16)*100);
					if(curhp * 3 <= maxhp){
						Burrow(TilesWithinDistance(6));
					}
					else{
						Burrow(GetCone(DirectionOf(target),6,true));
					}
				}
				else{
					if(!HasAttr(AttrType.COOLDOWN_2) && FirstActorInLine(target) != null && FirstActorInLine(target).DistanceFrom(target) <= 1){
						RefreshDuration(AttrType.COOLDOWN_2,R.Between(10,14)*100);
						B.Add(TheName(true) + " breathes fire! ",target);
						AnimateProjectile(target,'*',Color.RandomFire);
						AnimateExplosion(target,1,'*',Color.RandomFire);
						foreach(Actor a in target.ActorsWithinDistance(1)){
							if(a.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,R.Roll(6),this,"a flametongue toad")){
								a.RefreshDuration(AttrType.BURNING,R.Between(4,7)*100); //todo! check resistances. check duration. add message. decide how much damage firebolt effects do, and how long they burn. todo
							} //todo: bug here. needs to consider light and such. maybe put the check into RefreshDuration.
						}
						Q1();
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				break;
			}
			case ActorType.ENTRANCER:
				if(group == null){
					if(AI_Flee()){ //todo test
						QS();
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							QS();
						}
					}
				}
				else{
					Actor thrall = group[1];
					if(CanSee(thrall)){ //cooldown 1 is teleport. cooldown 2 is shield.
						if(DistanceFrom(target) < thrall.DistanceFrom(target) && DistanceFrom(thrall) == 1){
							Move(thrall.row,thrall.col);
							QS();
						}
						else{
							if(DistanceFrom(target) == 1 && curhp < maxhp){
								List<Tile> safe = TilesAtDistance(1).Where(t=>t.passable && t.actor() == null && target.GetBestExtendedLineOfEffect(thrall).Contains(t));
								if(DistanceFrom(thrall) == 1 && safe.Count > 0){
									AI_Step(safe.Random());
									QS();
								}
								else{
									if(AI_Flee()){ //todo test
										QS();
									}
									else{
										Attack(0,target);
									}
								}
							}
							else{
								if(!HasAttr(AttrType.COOLDOWN_1) && (thrall.DistanceFrom(target) > 1 || !target.GetBestExtendedLineOfEffect(thrall).Any(t=>t.actor()==this))){ //the entrancer tries to be smart about placing the thrall in a position that blocks ranged attacks
									List<Tile> closest = new List<Tile>();
									int dist = 99;
									foreach(Tile t in thrall.TilesWithinDistance(2).Where(x=>x.passable && (x.actor()==null || x.actor()==thrall))){
										if(t.DistanceFrom(target) < dist){
											closest.Clear();
											closest.Add(t);
											dist = t.DistanceFrom(target);
										}
										else{
											if(t.DistanceFrom(target) == dist){
												closest.Add(t);
											}
										}
									}
									List<Tile> in_line = new List<Tile>();
									foreach(Tile t in closest){
										if(target.GetBestExtendedLineOfEffect(t).Any(x=>x.actor()==this)){
											in_line.Add(t);
										}
									}
									Tile tile = null;
									if(in_line.Count > 0){
										tile = in_line.Random();
									}
									else{
										if(closest.Count > 0){
											tile = closest.Random();
										}
									}
									if(tile != null && tile.actor() != thrall){
										GainAttr(AttrType.COOLDOWN_1,400);
										B.Add(TheName(true) + " teleports " + thrall.TheName(true) + ". ",this,thrall);
										M.Draw();
										thrall.Move(tile.row,tile.col);
										B.DisplayNow();
										Screen.AnimateStorm(tile.p,1,1,4,thrall.symbol,thrall.color);
										foreach(Tile t2 in thrall.GetBestLineOfEffect(tile)){
											Screen.AnimateStorm(t2.p,1,1,4,thrall.symbol,thrall.color);
										}
										Q1();
									}
									else{
										List<Tile> safe = target.GetBestExtendedLineOfEffect(thrall).Where(t=>t.passable
										&& t.actor() == null && t.DistanceFrom(target) > thrall.DistanceFrom(target)).WhereLeast(t=>DistanceFrom(t));
										if(safe.Any(t=>t.DistanceFrom(target) > 2)){
											AI_Step(safe.Where(t=>t.DistanceFrom(target) > 2).Random());
										}
										else{
											AI_Step(safe.Random());
										}
										QS();
									}
								}
								else{
									if(!HasAttr(AttrType.COOLDOWN_2) && thrall.attrs[AttrType.ARCANE_SHIELDED] < 25){
										GainAttr(AttrType.COOLDOWN_2,1500);
										B.Add(TheName(true) + " shields " + thrall.TheName(true) + ". ",this,thrall);
										B.DisplayNow();
										Screen.AnimateStorm(thrall.p,1,2,5,'*',Color.White);
										thrall.attrs[AttrType.ARCANE_SHIELDED] = 25;
										Q1();
									}
									else{
										List<Tile> safe = target.GetBestExtendedLineOfEffect(thrall).Where(t=>t.passable && t.actor() == null).WhereLeast(t=>DistanceFrom(t));
										if(safe.Any(t=>t.DistanceFrom(target) > 2)){
											AI_Step(safe.Where(t=>t.DistanceFrom(target) > 2).Random());
										}
										else{
											AI_Step(safe.Random());
										}
										QS();
									}
								}
							}
						}
					}
					else{
						group[1].FindPath(this); //call for help
						if(AI_Flee()){ //todo test
							QS();
						}
						else{
							if(DistanceFrom(target) == 1){
								Attack(0,target);
							}
							else{
								QS();
							}
						}
					}
				}
				break;
			case ActorType.ORC_GRENADIER:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 8){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(R.Roll(2)*100)+150,AttrType.COOLDOWN_1));
					B.Add(the_name + " tosses a grenade toward " + target.the_name + ". ",this,target);
					List<Tile> tiles = new List<Tile>();
					foreach(Tile tile in target.TilesWithinDistance(1)){
						if(tile.passable){
							tiles.Add(tile);
						}
					}
					Tile t = tiles[R.Roll(tiles.Count)-1];
					if(t.actor() != null){
						if(t.actor() == player){
							B.Add("It lands under you! ");
						}
						else{
							B.Add("It lands under " + t.actor().the_name + ". ",t.actor());
						}
					}
					else{
						if(t.inv != null){
							B.Add("It lands under " + t.inv.TheName() + ". ",t);
						}
					}
					t.features.Add(FeatureType.GRENADE);
					Q.Add(new Event(t,100,EventType.GRENADE));
					Q1();
				}
				else{
					if(curhp <= 18){
						if(AI_Step(target,true)){
							B.Add(the_name + " backs away. ",this);
							QS();
						}
						else{
							if(DistanceFrom(target) == 1){
								Attack(0,target);
							}
							else{
								QS();
							}
						}
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				break;
			case ActorType.SPELLMUDDLE_PIXIE:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(R.CoinFlip()){
						AI_Step(target,true);
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.SAVAGE_HULK:
				//if has grabbed target, check for open spaces near the opposite side.
				//if one is found, slam target into that tile, then pummel.
				//otherwise, slam target into a solid tile (target doesn't move), then pummel.
				//if nothing is grabbed yet, just keep attacking.
				if(DistanceFrom(target) == 1){
					if(target.HasAttr(AttrType.GRABBED) && attrs[AttrType.GRABBING] == DirectionOf(target) && !target.GrabPreventsMovement(tile())){ //todo: pretty sure this should check for FROZEN too
						Tile t = null;
						Tile opposite = TileInDirection(DirectionOf(target).RotateDir(true,4));
						if(opposite.passable && opposite.actor() == null){
							t = opposite;
						}
						if(t == null){
							List<Tile> near_opposite = new List<Tile>();
							foreach(int i in new int[]{-1,1}){
								Tile near = TileInDirection(DirectionOf(target).RotateDir(true,4+i));
								if(near.passable && near.actor() == null){
									near_opposite.Add(near);
								}
							}
							if(near_opposite.Count > 0){
								t = near_opposite.Random();
							}
						}
						if(t != null){
							B.Add(the_name + " lifts " + target.the_name + " and slams " + target.the_name + " down! ",this,target);
							target.Move(t.row,t.col);
							target.CollideWith(target.tile());
							Attack(0,target);
						}
						else{
							B.Add(the_name + " lifts " + target.the_name + " and slams " + target.the_name + " down! ",this,target);
							target.CollideWith(target.tile());
							Attack(0,target);
						}
					}
					else{
						Attack(0,target);
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.MARBLE_HORROR_STATUE:
				QS();
				break;
			case ActorType.CRYOLICH:
				foreach(Actor a in ActorsWithinDistance(2)){
					if((a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)) || !HasSpell(SpellType.GLACIAL_BLAST)){
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
						return;
					}
				}
				switch(DistanceFrom(target)){
				case 1:
					if(AI_Flee()){
						QS();
					}
					else{
						Attack(0,target);
					}
					break;
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
					if(FirstActorInLine(target) == target){
						CastSpell(SpellType.GLACIAL_BLAST,target);
					}
					else{
						if(AI_Flee()){
							QS();
						}
						else{ 
							AI_Sidestep(target);
							QS();
						}
					}
					break;
				default:
					AI_Step(target);
					QS();
					break;
				}
					/*if(target.HasAttr(AttrType.FROZEN)){
						if(DistanceFrom(target) == 1){
							AI_Step(target,true);
							QS();
						}
						else{
							AI_Sidestep(target);
							QS();
						}
					}
					else{
						if(DistanceFrom(target) == 1 && target.HasAttr(AttrType.SLOWED) && (!target.HasAttr(AttrType.CHILLED) || curmp < 10)){
							if(HasAttr(AttrType.COOLDOWN_1)){
								AI_Step(target,true);
								QS();
							}
							else{
								B.Add(the_name + " syphons some of " + target.Your() + " life. ",target);
								if(target.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,10,this,"a cryolich's touch")){
									int remaining = 10;
									int missing_hp_percentage = 100 - (100 * curhp / maxhp);
									int missing_mp_percentage = 100 - (100 * curmp / maxmp);
									int total_missing_percentage = missing_hp_percentage + missing_mp_percentage;
									if(total_missing_percentage > 0){
										int recovered_hp = remaining * missing_hp_percentage / total_missing_percentage;
										curhp += recovered_hp;
										if(curhp > maxhp){
											curhp = maxhp;
										}
										remaining -= recovered_hp;
										curmp += remaining;
										if(curmp > maxmp){
											curmp = maxmp;
										}
									}
								}
								RefreshDuration(AttrType.COOLDOWN_1,100);
								Q1();
							}
						}
						else{
							if(DistanceFrom(target) > 5 || target.HasAttr(AttrType.SLOWED)){
								if(FirstActorInLine(target) == target){
									CastSpell(SpellType.GLACIAL_BLAST,target);
								}
								else{
									AI_Sidestep(target);
									QS();
								}
							}
							else{
								if(R.PercentChance(120 - DistanceFrom(target)*20)){
									CastSpell(SpellType.BLIZZARD,target);
								}
								else{
									if(FirstActorInLine(target) == target){
										CastSpell(SpellType.GLACIAL_BLAST,target);
									}
									else{
										AI_Sidestep(target);
										QS();
									}
								}
							}
						}
					}*/
				break;
			case ActorType.PYREN_ARCHER: //still considering some sort of fire trail movement ability for this guy
				switch(DistanceFrom(target)){
				case 1:
					if(target.EnemiesAdjacent() > 1){
						Attack(0,target);
					}
					else{
						if(AI_Flee()){ //todo test
							QS();
						}
						else{
							Attack(0,target);
						}
					}
					break;
				case 2:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						if(AI_Flee()){ //todo test
							QS();
						}
						else{ 
							if(AI_Sidestep(target)){
								B.Add(the_name + " tries to line up a shot. ",this);
							}
							QS();
						}
					}
					break;
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						if(AI_Sidestep(target)){
							B.Add(the_name + " tries to line up a shot. ",this);
						}
						QS();
					}
					break;
				default:
					AI_Step(target);
					QS();
					break;
				}
				break;
			case ActorType.PLACEHOLDER:
			{
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					List<Tile> tiles = new List<Tile>(); //the "slither" movement style, just like the cobra
					if(target.row == row || target.col == col){
						int targetdir = DirectionOf(target);
						for(int i=-1;i<=1;++i){
							pos adj = p.PosInDir(targetdir.RotateDir(true,i));
							if(M.tile[adj].passable && M.actor[adj] == null){
								tiles.Add(M.tile[adj]);
							}
						}
					}
					if(tiles.Count > 0){
						AI_Step(tiles.Random());
					}
					else{
						AI_Step(target);
					}
					QS();
				}
				break;
			}
			case ActorType.ALASI_SENTINEL:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(HasAttr(AttrType.JUST_FLUNG)){
						attrs[AttrType.JUST_FLUNG] = 0;
					}
					else{
						if(target != null){
							List<Tile> valid_dirs = new List<Tile>();
							foreach(Tile t in target.TilesAtDistance(1)){
								if(t.passable && t.actor() == null && DistanceFrom(t) == 1){
									valid_dirs.Add(t);
								}
							}
							if(valid_dirs.Count > 0){
								AI_Step(valid_dirs.Random());
							}
						}
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.NOXIOUS_WORM:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12){
					B.Add(TheName(true) + " breathes poisonous gas. ");
					List<Tile> area = new List<Tile>();
					foreach(Tile t in target.TilesWithinDistance(1)){
						if(t.passable && target.HasLOE(t)){
							t.AddFeature(FeatureType.POISON_GAS);
							area.Add(t);
						}
					}
					List<Tile> area2 = target.tile().AddGaseousFeature(FeatureType.POISON_GAS,8);
					area.AddRange(area2);
					Q.Add(new Event(area,600,EventType.POISON_GAS));
					RefreshDuration(AttrType.COOLDOWN_1,(R.Roll(6) + 18) * 100);
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.LASHER_FUNGUS:
				if(DistanceFrom(target) <= 12){
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						if(FirstActorInLine(target) == target){
							List<Tile> line = GetBestLineOfEffect(target.row,target.col);
							line.Remove(line[line.Count-1]);
							AnimateBoltBeam(line,Color.DarkGreen);
							if(R.Roll(1,4) == 4){
								Attack(0,target);
							}
							else{
								int target_r = target.row;
								int target_c = target.col;
								if(Attack(1,target) && M.actor[target_r,target_c] != null){
									if(target.HasAttr(AttrType.FROZEN)){
										if(target.name == "you"){
											B.Add("You don't move far. ");
										}
										else{
											B.Add(target.the_name + " doesn't move far. ",target);
										}
									}
									else{
										int rowchange = 0;
										int colchange = 0;
										if(target.row < row){
											rowchange = 1;
										}
										if(target.row > row){
											rowchange = -1;
										}
										if(target.col < col){
											colchange = 1;
										}
										if(target.col > col){
											colchange = -1;
										}
										if(!target.AI_MoveOrOpen(target.row+rowchange,target.col+colchange)){
											if(Math.Abs(target.row - row) > Math.Abs(target.col - col)){
												target.AI_Step(M.tile[row,target.col]);
											}
											else{
												if(Math.Abs(target.row - row) < Math.Abs(target.col - col)){
													target.AI_Step(M.tile[target.row,col]); //todo: playerWalk for player? probably not, if only playerwalk is restricted by banshees
												}
												else{
													target.AI_Step(this);
												}
											}
										}
									}
								}
							}
						}
						else{
							Q1();
						}
					}
				}
				else{
					Q1();
				}
				break;
			case ActorType.VAMPIRE:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					if(DistanceFrom(target) <= 12){
						if(tile().IsLit() && !HasAttr(AttrType.COOLDOWN_1)){
							attrs[AttrType.COOLDOWN_1]++;
							B.Add(the_name + " gestures. ",this);
							List<Tile> tiles = new List<Tile>();
							foreach(Tile t in target.TilesWithinDistance(6)){
								if(t.passable && t.actor() == null && DistanceFrom(t) >= DistanceFrom(target)
								&& target.HasLOS(t) && target.HasLOE(t)){
									tiles.Add(t);
								}
							}
							if(tiles.Count == 0){
								foreach(Tile t in target.TilesWithinDistance(6)){ //same, but with no distance requirement
									if(t.passable && t.actor() == null && target.HasLOS(t) && target.HasLOE(t)){
										tiles.Add(t);
									}
								}
							}
							if(tiles.Count == 0){
								B.Add("Nothing happens. ",this);
							}
							else{
								if(tiles.Count == 1){
									B.Add("A blood moth appears! ");
								}
								else{
									B.Add("Blood moths appear! ");
								}
								for(int i=0;i<2;++i){
									if(tiles.Count > 0){
										Tile t = tiles.RemoveRandom();
										Create(Forays.ActorType.BLOOD_MOTH,t.row,t.col,true,true);
										M.actor[t.row,t.col].player_visibility_duration = -1;
									}
								}
							}
							Q1();
						}
						else{
							AI_Step(target);
							QS();
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.ORC_WARMAGE:
			{
				foreach(Actor a in ActorsWithinDistance(2)){
					if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
						if(DistanceFrom(target) == 1){
							Attack(0,target);
						}
						else{
							AI_Step(target);
							QS();
						}
						return;
					}
				}
				/*if(curhp <= 15 && HasLOS(target)){
					Tile wall = null;
					int wall_distance_to_center = 9999;
					pos center = new pos(ROWS/2,COLS/2);
					for(int i = 2;i<=8;i += 2){
						if(TileInDirection(i).type == TileType.WALL){
							if(TileInDirection(i).ApproximateEuclideanDistanceFromX10(center) < wall_distance_to_center){
								wall = TileInDirection(i);
								wall_distance_to_center = TileInDirection(i).ApproximateEuclideanDistanceFromX10(center);
							}
						}
					}
					if(wall != null){
						CastSpell(SpellType.PASSAGE,wall);
						break;
					}
				}
				List<SpellType> valid_spells = new List<SpellType>();
				valid_spells.Add(SpellType.SCORCH);
				valid_spells.Add(SpellType.GLACIAL_BLAST);
				valid_spells.Add(SpellType.GLACIAL_BLAST);*/
				/*valid_spells.Add(SpellType.IMMOLATE);
				if(target.HasAttr(AttrType.BURNING) || target.HasAttr(AttrType.CATCHING_FIRE)){
					valid_spells.Remove(Forays.SpellType.IMMOLATE);
				}*/
				//SpellType[] ranged_spells = valid_spells.ToArray();
				switch(DistanceFrom(target)){
				case 1:
				{
					List<SpellType> close_range = new List<SpellType>();
					close_range.Add(SpellType.MAGIC_HAMMER);
					close_range.Add(SpellType.MAGIC_HAMMER);
					close_range.Add(SpellType.BLINK);
					if(target.EnemiesAdjacent() > 1 || R.CoinFlip()){
						CastCloseRangeSpellOrAttack(close_range,target,false);
						//CastRandomSpell(target,SpellType.MAGIC_HAMMER,SpellType.MAGIC_HAMMER,SpellType.SCORCH);
					}
					else{
						if(AI_Step(target,true)){
							QS();
						}
						else{
							CastCloseRangeSpellOrAttack(close_range,target,false);
							//CastRandomSpell(target,SpellType.MAGIC_HAMMER,SpellType.MAGIC_HAMMER,SpellType.SCORCH);
						}
					}
					break;
				}
				case 2:
					if(R.CoinFlip()){
						if(AI_Step(target,true)){
							QS();
						}
						else{
							if(FirstActorInLine(target) == target){
								CastRangedSpellOrMove(target);
								//CastRandomSpell(target,SpellType.SCORCH,SpellType.GLACIAL_BLAST);
							}
							else{
								AI_Sidestep(target);
								QS();
							}
						}
					}
					else{
						if(FirstActorInLine(target) == target){
							CastRangedSpellOrMove(target);
							//CastRandomSpell(target,SpellType.SCORCH,SpellType.GLACIAL_BLAST);
						}
						else{
							if(AI_Step(target,true)){
								QS();
							}
							else{
								AI_Sidestep(target);
								QS();
							}
						}
					}
					break;
				case 3:
				case 4:
				case 5:
				case 6:
				case 7:
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
					if(FirstActorInLine(target) == target){
						CastRangedSpellOrMove(target);
						//CastRandomSpell(target,ranged_spells);
					}
					else{
						AI_Sidestep(target);
						QS();
					}
					break;
				default:
					AI_Step(target);
					QS();
					break;
				}
				break;
			}
			case ActorType.NECROMANCER:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(R.Roll(4)+8)*100,AttrType.COOLDOWN_1));
					B.Add(the_name + " calls out to the dead. ",this);
					ActorType summon = R.CoinFlip()? ActorType.SKELETON : ActorType.ZOMBIE;
					List<Tile> tiles = new List<Tile>();
					foreach(Tile tile in TilesWithinDistance(2)){
						if(tile.passable && tile.actor() == null && DirectionOf(tile) == DirectionOf(target)){
							tiles.Add(tile);
						}
					}
					if(tiles.Count == 0){
						foreach(Tile tile in TilesWithinDistance(2)){
							if(tile.passable && tile.actor() == null){
								tiles.Add(tile);
							}
						}
					}
					if(tiles.Count == 0 || (group != null && group.Count > 3)){
						B.Add("Nothing happens. ",this);
					}
					else{
						Tile t = tiles.Random();
						B.Add(Prototype(summon).a_name + " digs through the floor! ");
						Create(summon,t.row,t.col,true,true);
						M.actor[t.row,t.col].player_visibility_duration = -1;
						if(group == null){
							group = new List<Actor>{this};
						}
						group.Add(M.actor[t.row,t.col]);
						M.actor[t.row,t.col].group = group;
					}
					Q1();
				}
				else{
					bool blast = false;
					switch(DistanceFrom(target)){
					case 1:
						if(AI_Step(target,true)){
							QS();
						}
						else{
							Attack(0,target);
						}
						break;
					case 2:
						if(R.CoinFlip() && FirstActorInLine(target) == target){
							blast = true;
						}
						else{
							if(AI_Step(target,true)){
								QS();
							}
							else{
								blast = true;
							}
						}
						break;
					case 3:
					case 4:
					case 5:
					case 6:
						if(FirstActorInLine(target) == target){
							blast = true;
						}
						else{
							AI_Sidestep(target);
							QS();
						}
						break;
					default:
						AI_Step(target);
						QS();
						break;
					}
					if(blast){
						B.Add(the_name + " fires dark energy at " + target.the_name + ". ",this,target);
						AnimateBoltProjectile(target,Color.DarkBlue);
						target.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(6),this,"*blasted by a necromancer");
						Q1();
					}
				}
				break;
			case ActorType.STALKING_WEBSTRIDER:
			{
				bool burrow = false;
				if(DistanceFrom(target) >= 2 && DistanceFrom(target) <= 6){
					if(R.CoinFlip() && !target.tile().Is(FeatureType.WEB)){
						burrow = true;
					}
				}
				if((DistanceFrom(target) > 6 || target.HasAttr(AttrType.POISONED))){
					burrow = true;
				}
				if(burrow && !HasAttr(AttrType.COOLDOWN_1)){
					RefreshDuration(AttrType.COOLDOWN_1,R.Between(5,8)*100);
					if(DistanceFrom(target) <= 2){
						Burrow(TilesWithinDistance(6)); //todo
					}
					else{
						Burrow(GetCone(DirectionOf(target),6,true));
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			}
			case ActorType.ORC_ASSASSIN:
				if(DistanceFrom(target) > 1 && attrs[AttrType.TURNS_VISIBLE] < 0){ //todo: make sure this means "if the player knows about this monster"
					Tile t = TilesAtDistance(1).Where(x=>x.passable && x.actor() == null && target.DistanceFrom(x) == target.DistanceFrom(this)-1 && !target.CanSee(x)).Random();
					if(t != null){
						AI_Step(t);
						FindPath(target); //so it won't forget where the target is...
						QS();
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target); //todo: make sure attack does the combo hit. or will it even be a combo hit?
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.LUMINOUS_AVENGER:
				if(curhp <= 10 && !M.wiz_dark){
					if(player.CanSee(this)){
						B.Add(the_name + " absorbs the light from the air. ");
					}
					else{
						B.Add("Something drains the light from the air. ");
					}
					B.Add(the_name + " is restored. ",this);
					curhp = maxhp;
					M.wiz_dark = true;
					M.wiz_lite = false;
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
					Q1();
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.MACHINE_OF_WAR: //todo: pathing should also happen every other turn
				if(attrs[AttrType.COOLDOWN_1] % 2 == 0){ //the machine of war moves on even turns and fires on odd turns.
					AI_Step(target);
					QS(); //todo make sure the c1 variable is always updated
					if(attrs[AttrType.COOLDOWN_1] == 16){
						foreach(Tile t in TilesWithinDistance(1)){
							if(t.actor() != null && t.actor() != this){
								t.actor().TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,R.Roll(5,6),this,a_name); //todo: also ignite actors, right?
							}
							t.ApplyEffect(DamageType.FIRE);
						}
						attrs[AttrType.COOLDOWN_1] = 0;
					}
				}
				else{
					if(DistanceFrom(target) <= 12 && FirstActorInLine(target) == target){
						B.Add(TheName(true) + " fires a stream of scalding oil at " + target.the_name + ". ",target);
						List<Tile> greased = GetBestLineOfEffect(target);
						List<Tile> added = new List<Tile>();
						foreach(Tile t in greased){
							foreach(int dir in U.FourDirections){
								Tile neighbor = t.TileInDirection(dir);
								if(R.OneIn(3) && neighbor.passable && !greased.Contains(neighbor)){
									added.AddUnique(neighbor);
								}
							}
						}
						greased.AddRange(added);
						/*foreach(Tile t in greased){
							t.AddFeature(FeatureType.OIL);
							Screen.AnimateMapCell(t.row,t.col,new colorchar('!',Color.DarkYellow));
						}*/
						for(int i=0;greased.Count > 0;++i){
							List<Tile> removed = new List<Tile>();
							foreach(Tile t in greased){
								if(DistanceFrom(t) == i){
									t.AddFeature(FeatureType.OIL);
									removed.Add(t);
								}
							}
							foreach(Tile t in removed){
								greased.Remove(t);
							}
							M.Draw();
						}
						if(target.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(4,6),this,"a stream of scalding oil")){
							target.attrs[AttrType.GREASED]++;
							B.Add(target.YouAre() + " covered in oil. ",target);
						}
						Q1();
					}
					else{
						Q1();
					}
				}
				attrs[AttrType.COOLDOWN_1]++;
				break;
			case ActorType.FIRE_DRAKE:
				/*if(player.magic_trinkets.Contains(MagicTrinketType.RING_OF_RESISTANCE) && DistanceFrom(player) <= 12 && CanSee(player)){
					B.Add(the_name + " exhales an orange mist toward you. ");
					foreach(Tile t in GetBestLineOfEffect(player)){
						Screen.AnimateStorm(t.p,1,2,3,'*',Color.Red);
					}
					B.Add("Your ring of resistance melts and drips onto the floor! ");
					player.magic_trinkets.Remove(MagicTrinketType.RING_OF_RESISTANCE);
					Q.Add(new Event(this,100,EventType.MOVE));
				}
				else{*/
					/*if(player.EquippedArmor == ArmorType.FULL_PLATE_OF_RESISTANCE && DistanceFrom(player) <= 12 && CanSee(player)){
						B.Add(the_name + " exhales an orange mist toward you. ");
						foreach(Tile t in GetBestLine(player)){
							Screen.AnimateStorm(t.p,1,2,3,'*',Color.Red);
						}
						B.Add("The runes drip from your full plate of resistance! ");
						player.EquippedArmor = ArmorType.FULL_PLATE;
						player.UpdateOnEquip(ArmorType.FULL_PLATE_OF_RESISTANCE,ArmorType.FULL_PLATE);
						Q.Add(new Event(this,100,EventType.MOVE));
					}
					else{*/
					if(!HasAttr(AttrType.COOLDOWN_1)){
						if(DistanceFrom(target) <= 12){
							attrs[AttrType.COOLDOWN_1]++;
							int cooldown = (R.Roll(1,4)+1) * 100;
							Q.Add(new Event(this,cooldown,AttrType.COOLDOWN_1));
							AnimateBeam(target,Color.RandomFire,'*');
							B.Add(TheName(true) + " breathes fire. ",target);
							target.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,R.Roll(6,6),this,"*roasted by fire breath");
							if(target != null && !target.HasAttr(AttrType.BURNING) && !target.HasAttr(AttrType.CATCHING_FIRE)
							   && !target.HasAttr(AttrType.IMMUNE_FIRE) && !target.HasAttr(AttrType.IMMUNE_BURNING)){
								target.attrs[AttrType.CATCHING_FIRE] = 1;
								B.Add(target.You("start") + " catching fire! ",target);
							}
							Q.Add(new Event(this,200,EventType.MOVE));
						}
						else{
							AI_Step(target);
							QS();
						}
					}
					else{
						if(DistanceFrom(target) == 1){
							Attack(R.Roll(1,2)-1,target);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
					//}
				//}
				break;
			case ActorType.GHOST:
			{
				if(DistanceFrom(target) == 1){ //todo: make the ghost ignore the player at distances of more than 2.
					Attack(0,target);
				}
				else{
					List<Tile> valid = TilesAtDistance(1).Where(x=>x.TilesAtDistance(1).Any(y=>y.type == TileType.TOMBSTONE));
					List<Tile> best = valid.Where(x=>x.DistanceFrom(target) < this.DistanceFrom(target));
					if(best.Count > 0){
						AI_Step(best.Random());
					}
					else{
						if(valid.Count > 0){
							AI_Step(valid.Random());
						}
					}
					QS();
				}
				break;
			}
			default:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			}
		}
		public void SeekAI(){
			if(PathStep()){
				return;
			}
			if(type == ActorType.STALKING_WEBSTRIDER && tile().Is(FeatureType.WEB)){
				List<pos> webs = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].Is(FeatureType.WEB));
				if(webs.Contains(target.p)){
					FindPath(target);
					if(PathStep()){
						return;
					}
					else{
						path.Clear();
					}
				}
			}
			if(type == ActorType.SWORDSMAN || type == ActorType.PHANTOM_SWORDMASTER || type == ActorType.ALASI_SOLDIER){
				attrs[AttrType.COMBO_ATTACK] = 0;
			}
			switch(type){
			/*case ActorType.SHAMBLING_SCARECROW:
				if(R.CoinFlip()){
					AI_Step(TileInDirection(Global.RandomDirection()));
				}
				else{
					if(R.Roll(1,3) == 3 && DistanceFrom(player) <= 10){
						if(player.CanSee(this)){
							B.Add(the_name + " emits an eerie whistling sound. ");
						}
						else{
							B.Add("You hear an eerie whistling sound. ");
						}
					}
				}
				Q1();
				break;*/
			case ActorType.BLOOD_MOTH:
			{
				PhysicalObject brightest = null;
				if(!M.wiz_lite){
					List<PhysicalObject> current_brightest = new List<PhysicalObject>();
					foreach(Tile t in M.AllTiles()){
						int pos_radius = t.light_radius;
						PhysicalObject pos_obj = t;
						if(t.inv != null && t.inv.light_radius > pos_radius){
							pos_radius = t.inv.light_radius;
							pos_obj = t.inv;
						}
						if(t.actor() != null && t.actor().LightRadius() > pos_radius){
							pos_radius = t.actor().LightRadius();
							pos_obj = t.actor();
						}
						if(pos_radius > 0){
							if(current_brightest.Count == 0 && CanSee(t)){
								current_brightest.Add(pos_obj);
							}
							else{
								foreach(PhysicalObject o in current_brightest){
									if(pos_radius > o.light_radius){
										if(CanSee(t)){
											current_brightest.Clear();
											current_brightest.Add(pos_obj);
											break;
										}
									}
									else{
										if(pos_radius == o.light_radius && DistanceFrom(t) < DistanceFrom(o)){
											if(CanSee(t)){
												current_brightest.Clear();
												current_brightest.Add(pos_obj);
												break;
											}
										}
										else{
											if(pos_radius == o.light_radius && DistanceFrom(t) == DistanceFrom(o) && pos_obj == player){
												if(CanSee(t)){
													current_brightest.Clear();
													current_brightest.Add(pos_obj);
													break;
												}
											}
										}
									}
								}
							}
						}
					}
					if(current_brightest.Count > 0){
						brightest = current_brightest.Random();
					}
				}
				if(brightest != null){
					if(DistanceFrom(brightest) <= 1){
						List<Tile> open = new List<Tile>();
						foreach(Tile t in TilesAtDistance(1)){
							if(t.DistanceFrom(brightest) <= 1 && t.passable && t.actor() == null){
								open.Add(t);
							}
						}
						if(open.Count > 0){
							AI_Step(open.Random());
						}
						QS();
					}
					else{
						AI_Step(brightest);
						QS();
					}
				}
				else{
					int dir = Global.RandomDirection();
					if(TilesAtDistance(1).Where(t => !t.passable).Count > 4 && !TileInDirection(dir).passable){
						dir = Global.RandomDirection();
					}
					if(TileInDirection(dir).passable && ActorInDirection(dir) == null){
						AI_Step(TileInDirection(dir));
						QS();
					}
					else{
						if(player.HasLOS(TileInDirection(dir))){
							if(!TileInDirection(dir).passable){
								B.Add(the_name + " brushes up against " + TileInDirection(dir).the_name + ". ",this);
							}
							else{
								if(ActorInDirection(dir) != null){
									B.Add(the_name + " brushes up against " + ActorInDirection(dir).TheName(true) + ". ",this);
								}
							}
						}
						QS();
					}
				}
				break;
			}
			case ActorType.PHASE_SPIDER:
				if(DistanceFrom(target_location) <= 12){
					Tile t = target_location.TilesAtDistance(DistanceFrom(target_location)-1).Where(x=>x.passable && x.actor() == null).Random();
					if(t != null){
						Move(t.row,t.col);
					}
				}
				QS();
				break;
			case ActorType.ORC_WARMAGE:
				foreach(Actor a in ActorsWithinDistance(2)){
					if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
						QS();
						return;
					}
				}
				/*if(!HasAttr(AttrType.BLOODSCENT)){
					CastSpell(SpellType.BLOODSCENT);
				//todo detect movement
				else{
			}
				}*/
				QS();
				break;
			case ActorType.CARNIVOROUS_BRAMBLE:
			case ActorType.MUD_TENTACLE:
			case ActorType.LASHER_FUNGUS:
			case ActorType.MARBLE_HORROR_STATUE:
				QS();
				break;
			case ActorType.FIRE_DRAKE:
				FindPath(player);
				QS();
				break;
			default:
				if(target_location != null){
					if(DistanceFrom(target_location) == 1 && M.actor[target_location.p] != null){
						if(GrabPreventsMovement(target_location) || M.actor[target_location.p].GrabPreventsMovement(tile())
						|| HasAttr(AttrType.IMMOBILE) || M.actor[target_location.p].HasAttr(AttrType.IMMOBILE)){
							QS();
						}
						else{
							Move(target_location.row,target_location.col); //swap places
							target_location = null;
							attrs[AttrType.FOLLOW_DIRECTION_EXITED]++;
							QS();
						}
					}
					else{
						int dist = DistanceFrom(target_location);
						if(AI_Step(target_location)){
							QS();
							if(DistanceFrom(target_location) == 0){
								target_location = null;
								attrs[AttrType.FOLLOW_DIRECTION_EXITED]++;
							}
							else{
								if(DistanceFrom(target_location) == dist && !HasLOE(target_location)){ //if you didn't get any closer and you can't see it...
									target_location = null;
								}
							}
						}
						else{ //could not move, end turn.
							if(DistanceFrom(target_location) == 1 && !target_location.passable){
								target_location = null;
							}
							QS();
						}
					}
					if(target_location == null){
						if(!NeverWanders()){
							attrs[AttrType.WANDERING] = 1;
						}
					}
				}
				else{
					if(DistanceFrom(target) <= 2){
						List<pos> path2 = GetPath(target,2);
						if(path2.Count > 0){
							path = path2;
							player_visibility_duration = -1; //stay at -1 while in close pursuit
						}
						if(PathStep()){
							path.Clear(); //testing this; seems to be working.
							return;
						}
						QS();
					}
					else{
						if(HasAttr(AttrType.FOLLOW_DIRECTION_EXITED) && tile().direction_exited > 0){
							AI_Step(TileInDirection(tile().direction_exited));
							attrs[AttrType.FOLLOW_DIRECTION_EXITED] = 0;
						}
						else{
							bool corridor = HasAttr(AttrType.DIRECTION_OF_PREVIOUS_TILE); //if it's 0 or -1, ignore it
							foreach(int dir in U.FourDirections){
								if(TileInDirection(dir).passable && TileInDirection(dir.RotateDir(true,1)).passable && TileInDirection(dir.RotateDir(true,2)).passable){
									corridor = false;
									break;
								}
							}
							if(corridor){
								List<int> blocked = new List<int>();
								for(int i=-1;i<=1;++i){
									blocked.Add(attrs[AttrType.DIRECTION_OF_PREVIOUS_TILE].RotateDir(true,i));
								}
								List<Tile> tiles = TilesAtDistance(1).Where(x=>x.passable && x.actor() == null && !blocked.Contains(DirectionOf(x)));
								if(tiles.Count > 0){
									bool multiple_paths = false;
									foreach(Tile t1 in tiles){
										foreach(Tile t2 in tiles){
											if(t1 != t2 && t1.ApproximateEuclideanDistanceFromX10(t2) > 10){ //cardinally adjacent only
												multiple_paths = true;
												break;
											}
										}
										if(multiple_paths){
											break;
										}
									}
									if(!multiple_paths && player_visibility_duration < -1){
										++player_visibility_duration;
									}
									AI_Step(tiles.Random());
								}
							}
							else{
								if(group != null && group[0] != this){ //groups try to get back together
									if(DistanceFrom(group[0]) > 1){
										int dir = DirectionOf(group[0]);
										bool found = false;
										for(int i=-1;i<=1;++i){
											Actor a = ActorInDirection(dir.RotateDir(true,i));
											if(a != null && group.Contains(a)){
												found = true;
												break;
											}
										}
										if(!found){
											if(HasLOS(group[0])){
												AI_Step(group[0]);
											}
											else{
												FindPath(group[0],8);
												if(PathStep()){
													return;
												}
											}
										}
									}
								}
							}
						}
						QS();
					}
					/*if(DistanceFrom(target) <= 5){
						if(DistanceFrom(target) <= 3){
							List<pos> path2 = GetPath(target,4);
							if(path2.Count > 0){
								path = path2;
								player_visibility_duration = -1; //stay at -1 while in close pursuit
							}
						}
						else{
							List<pos> path2 = GetPath(target,8);
							if(path2.Count <= 10){
								path = path2;
							}
						}
						//FindPath(target,8);
						if(PathStep()){
							return;
						}
						QS();
					}
					else{ //if they're too far away, forget them and end turn.
						target = null;
						if(group != null && group[0] != this){ //groups try to get back together
							if(DistanceFrom(group[0]) > 1){
								int dir = DirectionOf(group[0]);
								bool found = false;
								for(int i=-1;i<=1;++i){
									Actor a = ActorInDirection(dir.RotateDir(true,i));
									if(a != null && group.Contains(a)){
										found = true;
										break;
									}
								}
								if(!found){
									if(HasLOS(group[0])){
										AI_Step(group[0]);
									}
									else{
										FindPath(group[0],8);
										if(PathStep()){
											return;
										}
									}
								}
							}
						}
						QS();
					}*/
				}
				break;
			}
		}
		public void IdleAI(){
			if(PathStep()){
				return;
			}
			switch(type){
			case ActorType.GIANT_BAT: //flies around
			case ActorType.PHANTOM_BLIGHTWING:
				AI_Step(TileInDirection(Global.RandomDirection()));
				QS();
				return; //<--!
			case ActorType.BLOOD_MOTH:
			{
				PhysicalObject brightest = null;
				if(!M.wiz_lite){
					List<PhysicalObject> current_brightest = new List<PhysicalObject>();
					foreach(Tile t in M.AllTiles()){
						int pos_radius = t.light_radius;
						PhysicalObject pos_obj = t;
						if(t.inv != null && t.inv.light_radius > pos_radius){
							pos_radius = t.inv.light_radius;
							pos_obj = t.inv;
						}
						if(t.actor() != null && t.actor().LightRadius() > pos_radius){
							pos_radius = t.actor().LightRadius();
							pos_obj = t.actor();
						}
						if(pos_radius > 0){
							if(current_brightest.Count == 0 && CanSee(t)){
								current_brightest.Add(pos_obj);
							}
							else{
								foreach(PhysicalObject o in current_brightest){
									if(pos_radius > o.light_radius){
										if(CanSee(t)){
											current_brightest.Clear();
											current_brightest.Add(pos_obj);
											break;
										}
									}
									else{
										if(pos_radius == o.light_radius && DistanceFrom(t) < DistanceFrom(o)){
											if(CanSee(t)){
												current_brightest.Clear();
												current_brightest.Add(pos_obj);
												break;
											}
										}
										else{
											if(pos_radius == o.light_radius && DistanceFrom(t) == DistanceFrom(o) && pos_obj == player){
												if(CanSee(t)){
													current_brightest.Clear();
													current_brightest.Add(pos_obj);
													break;
												}
											}
										}
									}
								}
							}
						}
					}
					if(current_brightest.Count > 0){
						brightest = current_brightest.Random();
					}
				}
				if(brightest != null){
					if(DistanceFrom(brightest) <= 1){
						List<Tile> open = new List<Tile>();
						foreach(Tile t in TilesAtDistance(1)){
							if(t.DistanceFrom(brightest) <= 1 && t.passable && t.actor() == null){
								open.Add(t);
							}
						}
						if(open.Count > 0){
							AI_Step(open.Random());
						}
						QS();
					}
					else{
						AI_Step(brightest);
						QS();
					}
				}
				else{
					int dir = Global.RandomDirection();
					if(TilesAtDistance(1).Where(t => !t.passable).Count > 4 && !TileInDirection(dir).passable){
						dir = Global.RandomDirection();
					}
					if(TileInDirection(dir).passable && ActorInDirection(dir) == null){
						AI_Step(TileInDirection(dir));
						QS();
					}
					else{
						if(player.HasLOS(TileInDirection(dir))){
							if(!TileInDirection(dir).passable){
								B.Add(the_name + " brushes up against " + TileInDirection(dir).the_name + ". ",this);
							}
							else{
								if(ActorInDirection(dir) != null){
									B.Add(the_name + " brushes up against " + ActorInDirection(dir).TheName(true) + ". ",this);
								}
							}
						}
						QS();
					}
				}
				return;
			}
			case ActorType.NOXIOUS_WORM:
			{
				if(TilesWithinDistance(1).All(x=>x.Is(TileType.WALL,TileType.CRACKED_WALL))){
					if(DistanceFrom(player) == 2){
						player_visibility_duration = -1;
						Tile t = tile().NeighborsBetween(player.row,player.col).Random();
						Move(t.row,t.col);
						t.TurnToFloor();
						B.Add(AName(true) + " bursts through the wall! ",t);
						B.Print(true);
						List<Tile> area = t.AddGaseousFeature(FeatureType.POISON_GAS,5);
						if(area.Count > 0){
							Q.RemoveTilesFromEventAreas(area,EventType.POISON_GAS);
							Q.Add(new Event(area,300,EventType.POISON_GAS));
						}
						RefreshDuration(AttrType.COOLDOWN_1,R.Between(2,5)*100);
						Q1();
						return;
					}
					else{
						List<Tile> valid = TilesAtDistance(1).Where(x=>x.Is(TileType.CRACKED_WALL) && !x.TilesAtDistance(1).Any(y=>y.passable));
						if(valid.Count > 0){
							Tile t = valid.Random();
							Move(t.row,t.col);
							QS();
							return;
						}
					}
				}
				break;
			}
			case ActorType.STALKING_WEBSTRIDER:
				if(tile().Is(FeatureType.WEB)){
					List<pos> webs = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].Is(FeatureType.WEB));
					if(webs.Contains(player.p)){
						player_visibility_duration = -1; //todo: check this
						FindPath(player);
						if(PathStep()){
							return;
						}
						else{
							path.Clear();
						}
					}
				}
				break;
			case ActorType.ORC_WARMAGE:
				foreach(Actor a in ActorsWithinDistance(2)){
					if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
						QS();
						return;
					}
				}
				/*if(!HasAttr(AttrType.BLOODSCENT)){
					CastSpell(SpellType.BLOODSCENT);
					return; //<--!
				}*/
				//todo detect movement
				break;
			case ActorType.SWORDSMAN:
			case ActorType.PHANTOM_SWORDMASTER:
			case ActorType.ALASI_SOLDIER:
				attrs[AttrType.COMBO_ATTACK] = 0;
				break;
			case ActorType.FIRE_DRAKE:
				FindPath(player);
				QS();
				return; //<--!
			default:
				break;
			}
			if(HasAttr(AttrType.WANDERING)){
				if(R.Roll(10) <= 6){
					List<Tile> in_los = new List<Tile>();
					foreach(Tile t in M.AllTiles()){
						if(t.passable && CanSee(t)){
							in_los.Add(t);
						}
					}
					if(in_los.Count > 0){
						FindPath(in_los.Random());
					}
					else{ //trapped?
						attrs[AttrType.WANDERING] = 0;
					}
				}
				else{
					if(R.OneIn(4)){
						List<Tile> passable = new List<Tile>();
						foreach(Tile t in M.AllTiles()){
							if(t.passable){
								passable.Add(t);
							}
						}
						if(passable.Count > 0){
							FindPath(passable.Random());
						}
						else{ //trapped?
							attrs[AttrType.WANDERING] = 0;
						}
					}
					else{
						List<Tile> nearby = new List<Tile>();
						foreach(Tile t in M.AllTiles()){
							if(t.passable && DistanceFrom(t) <= 12){
								nearby.Add(t);
							}
						}
						if(nearby.Count > 0){
							FindPath(nearby.Random());
						}
						else{ //trapped?
							attrs[AttrType.WANDERING] = 0;
						}
					}
				}
				if(PathStep()){
					return;
				}
				QS();
			}
			else{
				if(group != null && group[0] != this){
					if(DistanceFrom(group[0]) > 1){
						int dir = DirectionOf(group[0]);
						bool found = false;
						for(int i=-1;i<=1;++i){
							Actor a = ActorInDirection(dir.RotateDir(true,i));
							if(a != null && group.Contains(a)){
								found = true;
								break;
							}
						}
						if(!found){
							if(HasLOS(group[0])){
								AI_Step(group[0]);
							}
							else{
								FindPath(group[0],8);
								if(PathStep()){
									return;
								}
							}
						}
					}
				}
				QS();
			}
		}
		public void CalculateDimming(){
			if(M.wiz_lite || M.wiz_dark){
				return;
			}
			List<Actor> actors = new List<Actor>();
			foreach(Actor a in M.AllActors()){
				if(a.light_radius > 0){
					actors.Add(a);
				}
			}
			foreach(Actor actor in actors){
				int dist = 100;
				Actor closest_shadow = null;
				foreach(Actor a in actor.ActorsWithinDistance(10,true)){
					if(a.type == ActorType.SHADOW){
						if(a.DistanceFrom(actor) < dist){
							dist = a.DistanceFrom(actor);
							closest_shadow = a;
						}
					}
				}
				if(closest_shadow == null){
					if(actor.HasAttr(AttrType.DIM_LIGHT)){
						actor.attrs[AttrType.DIM_LIGHT] = 0;
						if(actor.light_radius > 0){
							B.Add(actor.Your() + " light grows brighter. ",actor);
							if(actor.HasAttr(AttrType.SHINING)){
								actor.UpdateRadius(actor.LightRadius(),12,true);
							}
							else{
								actor.UpdateRadius(actor.LightRadius(),6,true);
							}
						}
					}
				}
				else{
					Actor sh = closest_shadow; //laziness
					int dimness = 0;
					if(sh.DistanceFrom(actor) <= 2){
						dimness = 5;
					}
					else{
						if(sh.DistanceFrom(actor) <= 3){
							dimness = 4;
						}
						else{
							if(sh.DistanceFrom(actor) <= 5){
								dimness = 3;
							}
							else{
								if(sh.DistanceFrom(actor) <= 7){
									dimness = 2;
								}
								else{
									if(sh.DistanceFrom(actor) <= 10){
										dimness = 1;
									}
								}
							}
						}
					}
					if(dimness > actor.attrs[AttrType.DIM_LIGHT]){
						int difference = dimness - actor.attrs[AttrType.DIM_LIGHT];
						actor.attrs[AttrType.DIM_LIGHT] = dimness;
						if(actor.light_radius > 0){
							//if(actor.attrs[AttrType.BURNING] < actor.light_radius){ //if the player should notice...
								B.Add(actor.Your() + " light grows dimmer. ",actor);
								actor.UpdateRadius(actor.light_radius,actor.light_radius - difference,true);
								//if(actor.attrs[AttrType.BURNING] > actor.light_radius){
									//actor.UpdateRadius(actor.light_radius,actor.attrs[AttrType.BURNING]);
								//}
							//}
						}
					}
					else{
						if(dimness < actor.attrs[AttrType.DIM_LIGHT]){
							int difference = dimness - actor.attrs[AttrType.DIM_LIGHT];
							actor.attrs[AttrType.DIM_LIGHT] = dimness;
							if(actor.light_radius > 0){
								//if(actor.attrs[AttrType.BURNING] < actor.light_radius - difference){ //if the player should notice...
									B.Add(actor.Your() + " light grows brighter. ",actor);
									actor.UpdateRadius(actor.LightRadius(),actor.light_radius - difference,true);
								//}
							}
						}
					}
				}
			}
		}
		public void Burrow(List<Tile> area){ //todo when you change this, make sure the forasect regains HP for each turn it's gone.
			//List<Tile> open = TilesWithinDistance(6,true).Where(x=>x.passable && x.actor() == null);
			List<Tile> open = area.Where(x=>x.passable && x.actor() == null);
			if(open.Count > 0){
				Tile t = open.Random();
				B.Add(the_name + " burrows into the ground. ",this);
				Move(t.row,t.col);
			}
			else{
				B.Add(the_name + " fails to burrow. ",this);
			}
			Q.Add(new Event(this,R.Between(3,5)*100,EventType.MOVE));
		}
		public bool AI_Step(PhysicalObject obj){ return AI_Step(obj,false); }
		public bool AI_Step(PhysicalObject obj,bool flee){
			if(HasAttr(AttrType.IMMOBILE) || (type == ActorType.MECHANICAL_KNIGHT && attrs[AttrType.COOLDOWN_1] == 2)){
				return false;
			}
			int rowchange = 0;
			int colchange = 0;
			if(obj.row < row){
				rowchange = -1;
			}
			if(obj.row > row){
				rowchange = 1;
			}
			if(obj.col < col){
				colchange = -1;
			}
			if(obj.col > col){
				colchange = 1;
			}
			if(flee){
				rowchange = -rowchange;
				colchange = -colchange;
			}
			List<int> dirs = new List<int>();
			if(rowchange == -1){
				if(colchange == -1){
					dirs.Add(7);
				}
				if(colchange == 0){
					dirs.Add(8);
				}
				if(colchange == 1){
					dirs.Add(9);
				}
			}
			if(rowchange == 0){
				if(colchange == -1){
					dirs.Add(4);
				}
				if(colchange == 1){
					dirs.Add(6);
				}
			}
			if(rowchange == 1){
				if(colchange == -1){
					dirs.Add(1);
				}
				if(colchange == 0){
					dirs.Add(2);
				}
				if(colchange == 1){
					dirs.Add(3);
				}
			}
			if(dirs.Count == 0){ return true; }
			bool clockwise = R.CoinFlip();
			if(obj.DistanceFrom(TileInDirection(dirs[0].RotateDir(true))) < obj.DistanceFrom(TileInDirection(dirs[0].RotateDir(false)))){
				clockwise = true;
			}
			if(obj.DistanceFrom(TileInDirection(dirs[0].RotateDir(false))) < obj.DistanceFrom(TileInDirection(dirs[0].RotateDir(true)))){
				clockwise = false;
			}
			if(clockwise){
				dirs.Add(dirs[0].RotateDir(true));
				dirs.Add(dirs[0].RotateDir(false)); //building a list of directions to try: first the primary direction,
			}
			else{
				dirs.Add(dirs[0].RotateDir(false));
				dirs.Add(dirs[0].RotateDir(true));
			}
			clockwise = R.CoinFlip(); //then the ones next to it, then the ones next to THOSE(in random order, unless one is closer)
			if(obj.DistanceFrom(TileInDirection(dirs[0].RotateDir(true,2))) < obj.DistanceFrom(TileInDirection(dirs[0].RotateDir(false,2)))){
				clockwise = true;
			}
			if(obj.DistanceFrom(TileInDirection(dirs[0].RotateDir(false,2))) < obj.DistanceFrom(TileInDirection(dirs[0].RotateDir(true,2)))){
				clockwise = false;
			}
			if(clockwise){
				dirs.Add(dirs[0].RotateDir(true,2));
				dirs.Add(dirs[0].RotateDir(false,2));
			}
			else{
				dirs.Add(dirs[0].RotateDir(false,2));
				dirs.Add(dirs[0].RotateDir(true,2));//this completes the list of 5 directions.
			}
			/*bool cw = R.CoinFlip();
			dirs.Add(RotateDir(dirs[0],cw));
			dirs.Add(RotateDir(dirs[0],!cw)); //building a list of directions to try: first the primary direction,
			cw = R.CoinFlip(); 				//then the ones next to it, then the ones next to THOSE(in random order)
			dirs.Add(RotateDir(RotateDir(dirs[0],cw),cw));
			dirs.Add(RotateDir(RotateDir(dirs[0],!cw),!cw)); //this completes the list of 5 directions.*/
			foreach(int i in dirs){
				if(ActorInDirection(i) != null && ActorInDirection(i).IsHiddenFrom(this)){
					player_visibility_duration = -1;
					if(ActorInDirection(i) == player){
						attrs[AttrType.PLAYER_NOTICED]++;
					}
					target = player; //not extensible yet
					target_location = M.tile[player.row,player.col];
					string walks = " walks straight into you! ";
					if(HasAttr(AttrType.FLYING)){
						walks = " flies straight into you! ";
					}
					if(!IsHiddenFrom(player)){
						B.Add(TheName(true) + walks);
						if(player.CanSee(this)){
							B.Add(the_name + " looks startled. ");
						}
					}
					else{
						attrs[AttrType.TURNS_VISIBLE] = -1;
						attrs[AttrType.NOTICED]++;
						B.Add(AName(true) + walks);
						if(player.CanSee(this)){
							B.Add(the_name + " looks just as surprised as you. ");
						}
					}
					return true;
				}
				if(AI_MoveOrOpen(i)){
					return true;
				}
			}
			return false;
		}
		public bool AI_Flee(){
			List<pos> best = PositionsWithinDistance(1).Where(x=>M.actor[x] == null && M.safetymap[x] != U.DijkstraMax && M.safetymap[x] != U.DijkstraMin).WhereLeast(y=>M.safetymap[y]);
			if(best.Count > 0){
				pos p = best.Random();
				return AI_MoveOrOpen(p.row,p.col);
			}
			else{
				return false;
			}
		}
		public bool AI_MoveOrOpen(int dir){
			return AI_MoveOrOpen(TileInDirection(dir).row,TileInDirection(dir).col);
		}
		public bool AI_MoveOrOpen(int r,int c){
			if(M.tile[r,c].passable && M.actor[r,c] == null && !GrabPreventsMovement(M.tile[r,c]) && M.tile[r,c].type != TileType.CHASM){
				Move(r,c);
				if(tile().Is(TileType.GRAVEL) && !HasAttr(AttrType.FLYING)){
					if(player.DistanceFrom(tile()) <= 3){
						if(player.CanSee(tile())){
							B.Add("The gravel crunches. ",tile());
						}
						else{
							B.Add("You hear gravel crunching. ");
						}
					}
					MakeNoise(3);
				}
				return true;
			}
			else{
				if(M.tile[r,c].type == TileType.DOOR_C && HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
					M.tile[r,c].Toggle(this);
					return true;
				}
				else{
					if(M.tile[r,c].type == TileType.RUBBLE){
						if(HasAttr(AttrType.SMALL)){
							if(M.actor[r,c] == null && !GrabPreventsMovement(M.tile[r,c])){
								Move(r,c);
							}
							else{
								return false;
							}
						}
						else{
							M.tile[r,c].Toggle(this);
						}
						return true;
					}
					else{
						if(M.tile[r,c].type == TileType.HIDDEN_DOOR && HasAttr(AttrType.BOSS_MONSTER)){
							M.tile[r,c].Toggle(this);
							M.tile[r,c].Toggle(this);
							return true;
						}
					}
				}
			}
			return false;
		}
		public bool AI_Sidestep(PhysicalObject obj){
			int dist = DistanceFrom(obj);
			List<Tile> tiles = new List<Tile>();
			for(int i=row-1;i<=row+1;++i){
				for(int j=col-1;j<=col+1;++j){
					if(M.tile[i,j].DistanceFrom(obj) == dist && M.tile[i,j].passable && M.actor[i,j] == null){
						tiles.Add(M.tile[i,j]);
					}
				}
			}
			while(tiles.Count > 0){
				int idx = R.Roll(1,tiles.Count)-1;
				if(AI_Step(tiles[idx])){
					return true;
				}
				else{
					tiles.RemoveAt(idx);
				}
			}
			return false;
		}
		public bool PathStep(){ return PathStep(false); }
		public bool PathStep(bool never_clear_path){
			if(path.Count > 0 && !HasAttr(AttrType.IMMOBILE)){
				if(DistanceFrom(path[0]) == 1 && M.actor[path[0]] != null){
					if(group != null && group[0] == this && group.Contains(M.actor[path[0]])){
						if(GrabPreventsMovement(M.tile[path[0]]) || M.actor[path[0]].GrabPreventsMovement(tile())){
							path.Clear();
						}
						else{
							Move(path[0].row,path[0].col); //leaders can push through their followers
							if(DistanceFrom(path[0]) == 0){
								path.RemoveAt(0);
							}
						}
					}
					else{
						if(path.Count == 1 && M.actor[path[0]] != player){
							if(!never_clear_path){
								path.Clear();
							}
						}
						else{
							AI_Step(M.tile[path[0]]);
							if(path.Count > 1){
								if(DistanceFrom(path[1]) > 1){
									if(!never_clear_path){
										path.Clear();
									}
								}
								else{
									if(DistanceFrom(path[1]) == 0){
										path.RemoveAt(0);
										path.RemoveAt(0);
									}
								}
							}
						}
					}
				}
				else{
					AI_Step(M.tile[path[0]]);
					if(DistanceFrom(path[0]) == 0){
						path.RemoveAt(0);
					}
					else{
						if(path.Count > 0 && M.tile[path[0]].type == TileType.CHASM){
							path.Clear();
						}//todo: should there be an 'else' here?
						if(path.Count > 1 && DistanceFrom(path[1]) == 1){
							path.RemoveAt(0);
						}
					}
				}
				QS();
				return true;
			}
			return false;
		}
		public bool Attack(int attack_idx,Actor a){ return Attack(attack_idx,a,false); }
		public bool Attack(int attack_idx,Actor a,bool attack_is_part_of_another_action){ //returns true if attack hit
			AttackInfo info = attack[type][attack_idx];
			pos original_pos = p;
			pos target_original_pos = a.p;
			if(EquippedWeapon.type != WeaponType.NO_WEAPON){
				info = EquippedWeapon.Attack();
			}
			info.damage.source = this;
			if(a.HasFeat(FeatType.DEFLECT_ATTACK) && DistanceFrom(a) == 1){
				Actor other = a.ActorsWithinDistance(1).Where(x=>x.DistanceFrom(this) == 1).Random();
				if(other != a){
					B.Add(a.You("deflect") + "! ",this,a);
					return Attack(attack_idx,other,attack_is_part_of_another_action);
				}
			}
			if(!attack_is_part_of_another_action && StunnedThisTurn()){
				return false;
			}
			if(!attack_is_part_of_another_action && exhaustion == 20 && R.CoinFlip()){ //100% exhaustion
				B.Add(You("fumble") + " from exhaustion. ",this);
				Q1(); //this is checked in PlayerWalk if attack_is_part_of_another_action is true
				return false;
			}
			if(!attack_is_part_of_another_action && this == player && EquippedWeapon.status[EquipmentStatus.POSSESSED]){ //todo: make sure this works correctly with 'attack everything' feat
				List<Actor> actors = ActorsWithinDistance(1);
				Actor chosen = actors.Random();
				if(chosen != a){
					if(chosen == this){
						B.Add("Your possessed " + EquippedWeapon.NameWithEnchantment() + " tries to attack you! ");
						B.Add("You fight it off! "); //this is also checked in PlayerWalk if attack_is_part_of_another_action is true
						Q1();
						return true; //todo not sure about return type here
					}
					else{
						return Attack(attack_idx,chosen);
					}
				}
			}
			bool player_in_combat = false;
			if(this == player || a == player){
				player_in_combat = true;
			}
			if(a == player && (type == ActorType.DREAM_WARRIOR_CLONE || type == ActorType.DREAM_SPRITE_CLONE)){
				player_in_combat = false;
			}
			if(player_in_combat){
				player.attrs[AttrType.IN_COMBAT]++;
			}
			if(a.HasFeat(FeatType.CUNNING_DODGE) && !this.HasAttr(AttrType.DODGED)){
				attrs[AttrType.DODGED]++;
				B.Add(a.You("dodge") + " " + YourVisible() + " attack. ",this,a);
				Q.Add(new Event(this,info.cost));
				return false;
			}
			//pos pos_of_target = new pos(a.row,a.col);
			bool drive_back_nowhere_to_run = false;
			if(!attack_is_part_of_another_action && HasFeat(FeatType.DRIVE_BACK)){ //doesn't work while moving
				drive_back_nowhere_to_run = true;
				int dir = DirectionOf(a);
				if(a.TileInDirection(dir).passable && a.ActorInDirection(dir) == null && !a.GrabPreventsMovement(TileInDirection(dir))){
					drive_back_nowhere_to_run = false;
				}
				if(a.TileInDirection(dir.RotateDir(true)).passable && a.ActorInDirection(dir.RotateDir(true)) == null && !a.GrabPreventsMovement(TileInDirection(dir.RotateDir(true)))){
					drive_back_nowhere_to_run = false;
				}
				if(a.TileInDirection(dir.RotateDir(false)).passable && a.ActorInDirection(dir.RotateDir(false)) == null && !a.GrabPreventsMovement(TileInDirection(dir.RotateDir(false)))){
					drive_back_nowhere_to_run = false;
				}
				if(a.HasAttr(AttrType.FROZEN) || a.HasAttr(AttrType.IMMOBILE)){
					drive_back_nowhere_to_run = true;
				}
			}
			bool obscured_vision_miss = false;
			{
				bool fog = false;
				bool hidden = false;
				if((this.tile().Is(FeatureType.FOG) || a.tile().Is(FeatureType.FOG))){
					fog = true;
				}
				if(a.IsHiddenFrom(this) || !CanSee(a) || (a.HasAttr(AttrType.SHADOW_CLOAK) && !a.tile().IsLit() && !HasAttr(AttrType.BLINDSIGHT))){ //made shadow cloak give the player a 50% miss chance too
					hidden = true;
				}
				if((fog || hidden) && R.CoinFlip()){
					obscured_vision_miss = true;
				}
			}
			int plus_to_hit = TotalSkill(SkillType.COMBAT);
			bool sneak_attack = false;
			if(this.IsHiddenFrom(a) || !a.CanSee(this) || (this == player && HasAttr(AttrType.SHADOW_CLOAK) && !tile().IsLit() && !a.HasAttr(AttrType.BLINDSIGHT))){ //todo: i think the shadow cloak check is redundant now
				sneak_attack = true;
				a.attrs[AttrType.SEES_ADJACENT_PLAYER] = 1;
			} //...insert any other changes to sneak attack calculation here...
			if(sneak_attack || HasAttr(AttrType.LUNGING_AUTO_HIT) || (EquippedWeapon == Dagger && !tile().IsLit())){ //some attacks get +25% accuracy. this usually totals 100% vs. unarmored targets.
				plus_to_hit += 25;
			}
			plus_to_hit -= a.TotalSkill(SkillType.DEFENSE) * 2;
			bool attack_roll_hit = a.IsHit(plus_to_hit);
			bool blocked_by_armor_miss = false;
			bool blocked_by_root_shell_miss = false;
			bool mace_through_armor = false;
			if(!attack_roll_hit){
				int total_miss_chance = 25; //todo: if the attacker's accuracy can be lowered, consider that here.
				if(sneak_attack){
					total_miss_chance = 0;
				}
				total_miss_chance += a.TotalSkill(SkillType.DEFENSE) * 2;
				int roll = R.Roll(total_miss_chance);
				if(roll <= a.TotalProtectionFromArmor()){
					bool mace = (EquippedWeapon == Mace || type == ActorType.CRUSADING_KNIGHT || type == ActorType.PHANTOM_CRUSADER);
					if(mace){
						attack_roll_hit = true;
						mace_through_armor = true;
					}
					else{
						blocked_by_armor_miss = true;
					}
				}
				else{
					if(a.HasAttr(AttrType.ROOTS) && roll <= a.TotalProtectionFromArmor() + 20){ //+20 because each +1 to defense is -2% to hit, and potion of roots gives 10 defense
						blocked_by_root_shell_miss = true;
					}
				}
			}
			bool hit = true;
			if(obscured_vision_miss){ //this calculation turned out to be pretty complicated
				hit = false;
			}
			else{
				if(blocked_by_armor_miss || blocked_by_root_shell_miss){
					hit = false;
				}
				else{
					if(drive_back_nowhere_to_run || attack_roll_hit){
						hit = true;
					}
					else{
						hit = false;
					}
				}
			}
			if(a.HasAttr(AttrType.GRABBED) && attrs[AttrType.GRABBING] == DirectionOf(a)){
				hit = true; //one more modifier: automatically hit things you're grabbing.
			}
			if(!hit){
				if(blocked_by_armor_miss){
					if(a.HasFeat(FeatType.ARMOR_MASTERY)){
						B.Add(a.YourVisible() + " armor blocks the attack, leaving " + TheName(true) + " off-balance. ",a,this);
						RefreshDuration(AttrType.SUSCEPTIBLE_TO_CRITS,100); //todo: test this
					}
					else{
						B.Add(a.YourVisible() + " armor blocks " + YourVisible() + " attack. ",this,a);
					}
				}
				else{
					if(blocked_by_root_shell_miss){
						B.Add(a.YourVisible() + " root shell blocks " + YourVisible() + " attack. ",this,a);
					}
					else{
						if(obscured_vision_miss){
							B.Add(Your() + " attack goes wide. ",this);
						}
						else{
							if(!attack_is_part_of_another_action && HasFeat(FeatType.DRIVE_BACK)){
								B.Add(You("drive") + " " + a.TheName(true) + " back. ",this,a);
								if(!a.HasAttr(AttrType.FROZEN) && !HasAttr(AttrType.FROZEN)){ //todo, check FROZEN interactions.
									a.AI_Step(this,true);
									AI_Step(a); //todo: playerwalk?
								}
							}
							else{
								if(info.miss != ""){
									string s = info.miss + ". ";
									int pos = s.IndexOf('&');
									if(pos != -1){
										s = s.Substring(0,pos) + TheName(true) + s.Substring(pos+1);
									}
									pos = s.IndexOf('*');
									if(pos != -1){
										s = s.Substring(0,pos) + a.TheName(true) + s.Substring(pos+1);
									}
									B.Add(s,this,a);
								}
								else{
									B.Add(YouVisible("miss",true) + " " + a.TheName(true) + ". ",this,a);
								}
							}
						}
					}
				}
				if(type == ActorType.SWORDSMAN || type == ActorType.PHANTOM_SWORDMASTER || type == ActorType.ALASI_SOLDIER){
					attrs[AttrType.COMBO_ATTACK] = 0;
				}
			}
			else{
				string s = info.hit + ". ";
				if(!attack_is_part_of_another_action && HasFeat(FeatType.NECK_SNAP) && a.HasAttr(AttrType.MEDIUM_HUMANOID) && IsHiddenFrom(a)){
					if(!HasAttr(AttrType.RESIST_NECK_SNAP)){
						B.Add(You("silently snap") + " " + a.Your() + " neck. ");
						a.Kill();
						Q1();
						return true;
					}
					else{
						B.Add(You("silently snap") + " " + a.Your() + " neck. ");
						B.Add("It doesn't seem to affect " + a.the_name + ". ");
					}
				}
				bool crit = false;
				int crit_multiplier = 1; //base crit rate is 10%
				if(EquippedWeapon.type == WeaponType.DAGGER && !tile().IsLit()){
					crit_multiplier *= 2;
				}
				if(a.EquippedArmor != null && (a.EquippedArmor.status[EquipmentStatus.WEAK_POINT] || a.EquippedArmor.status[EquipmentStatus.DAMAGED])){
					crit_multiplier *= 2;
				}
				if(a.HasAttr(AttrType.SUSCEPTIBLE_TO_CRITS)){
					crit_multiplier *= 2;
				}
				if(EquippedWeapon.enchantment == EnchantmentType.PRECISION && !EquippedWeapon.status[EquipmentStatus.NEGATED]){
					crit_multiplier *= 2;
				}
				if(R.Roll(10) + crit_multiplier > 10){
					crit = true;
				}
				int pos = s.IndexOf('&');
				if(pos != -1){
					s = s.Substring(0,pos) + TheName(true) + s.Substring(pos+1);
				}
				pos = s.IndexOf('*');
				if(pos != -1){
					s = s.Substring(0,pos) + a.TheName(true) + s.Substring(pos+1);
				}
				int dice = info.damage.dice;
				if(sneak_attack && crit){
					if(!a.HasAttr(AttrType.NONLIVING) && !a.HasAttr(AttrType.PLANTLIKE) && !a.HasAttr(AttrType.BOSS_MONSTER)){
						if(a.type != ActorType.PLAYER){ //being nice to the player here...
							switch(EquippedWeapon.type){
							case WeaponType.SWORD:
								B.Add("You run " + a.TheName(true) + " through! ");
								break;
							case WeaponType.MACE:
								B.Add("You bash " + a.YourVisible() + " head in! ");
								break;
							case WeaponType.DAGGER:
								B.Add("You pierce one of " + a.YourVisible() + " vital organs! ");
								break;
							case WeaponType.STAFF:
								B.Add("You bring your staff down on " + a.YourVisible() + " head with a loud crack! ");
								break;
							case WeaponType.BOW:
								B.Add("You choke " + a.TheName(true) + " with your bowstring! ");
								break;
							default:
								break;
							}
							MakeNoise(6);
							a.Kill();
							if(!attack_is_part_of_another_action){
								Q1();
							}
							return true;
						}
						else{ //...but not too nice
							B.Add(AName(true) + " strikes from hiding! ");
							B.Add("The deadly attack damages your armor! ");
							a.EquippedArmor.status[EquipmentStatus.DAMAGED] = true;
							int lotsofdamage = Math.Max(dice*6,a.curhp/2);
							MakeNoise(6);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,lotsofdamage,this,a_name);
							Q1();
							return true;
						}
					}
				}
				if(sneak_attack){
					B.Add(YouVisible("strike") + " from hiding! ");
					if(type != ActorType.PLAYER){
						attrs[AttrType.TURNS_VISIBLE] = -1;
						attrs[AttrType.NOTICED]++;
					}
					else{
						a.player_visibility_duration = -1;
						a.attrs[AttrType.PLAYER_NOTICED]++;
					}
				}
				if(mace_through_armor){
					if(type == ActorType.CRUSADING_KNIGHT || type == ActorType.PHANTOM_CRUSADER){
						B.Add(Your() + " huge mace punches through " + a.Your() + " armor. ",this,a);
					}
					else{
						B.Add(Your() + " mace punches through " + a.Your() + " armor. ",this,a);
					}
				}
				else{
					B.Add(s,this,a);
				}
				if(a.type == ActorType.GHOST && EquippedWeapon.enchantment != EnchantmentType.NO_ENCHANTMENT && !EquippedWeapon.status[EquipmentStatus.NEGATED]){
					EquippedWeapon.status[EquipmentStatus.NEGATED] = true;
					B.Add(Your() + " " + EquippedWeapon.NameWithEnchantment() + "'s magic is suppressed! ",this);
				}
				int dmg = R.Roll(dice,6);
				bool no_max_damage_message = false;
				List<CriticalEffect> effects = new List<CriticalEffect>(); //critical effects actually include all on-hit things
				if(crit && info.crit != CriticalEffect.NO_CRIT){
					effects.AddUnique(info.crit);
				}
				if(HasAttr(AttrType.DIM_VISION_HIT)){
					effects.AddUnique(CriticalEffect.DIM_VISION);
				}
				if(HasAttr(AttrType.GRAB_HIT)){
					effects.AddUnique(CriticalEffect.GRAB);
				}
				if(HasAttr(AttrType.LIFE_DRAIN_HIT)){
					effects.AddUnique(CriticalEffect.DRAIN_LIFE);
				}
				if(HasAttr(AttrType.PARALYSIS_HIT)){
					effects.AddUnique(CriticalEffect.PARALYZE);
				}
				if(HasAttr(AttrType.POISON_HIT)){
					effects.AddUnique(CriticalEffect.POISON);
				}
				if(HasAttr(AttrType.STALAGMITE_HIT)){
					effects.AddUnique(CriticalEffect.STALAGMITES);
				}
				if(HasAttr(AttrType.STUN_HIT)){
					effects.AddUnique(CriticalEffect.STUN);
				}
				if(HasAttr(AttrType.WORN_OUT_HIT)){
					effects.AddUnique(CriticalEffect.WORN_OUT);
				}
				if(HasAttr(AttrType.BRUTISH_STRENGTH)){
					effects.AddUnique(CriticalEffect.MAX_DAMAGE);
					effects.AddUnique(CriticalEffect.STRONG_KNOCKBACK);
					effects.Remove(CriticalEffect.KNOCKBACK); //strong knockback replaces these
					effects.Remove(CriticalEffect.TRIP);
					effects.Remove(CriticalEffect.FLING);
				}
				if(type == ActorType.SWORDSMAN && attrs[AttrType.COMBO_ATTACK] == 2){
					effects.AddUnique(CriticalEffect.MAX_DAMAGE);
					effects.AddUnique(CriticalEffect.STRONG_KNOCKBACK);
				}
				if(type == ActorType.PHANTOM_SWORDMASTER && attrs[AttrType.COMBO_ATTACK] == 2){
					effects.AddUnique(CriticalEffect.PERCENT_DAMAGE);
					effects.AddUnique(CriticalEffect.STRONG_KNOCKBACK);
				}
				if(type == ActorType.ALASI_SOLDIER){
					if(attrs[AttrType.COMBO_ATTACK] == 1){
						effects.AddUnique(CriticalEffect.ONE_TURN_STUN);
					}
					else{
						if(attrs[AttrType.COMBO_ATTACK] == 2){
							effects.AddUnique(CriticalEffect.ONE_TURN_PARALYZE);
						}
					}
				}
				if(type == ActorType.WILD_BOAR && HasAttr(AttrType.COOLDOWN_1)){
					effects.AddUnique(CriticalEffect.FLING);
				}
				if(type == ActorType.ALASI_SENTINEL && R.OneIn(3)){
					effects.AddUnique(CriticalEffect.FLING);
				}
				if(type == ActorType.ORC_ASSASSIN && sneak_attack){
					effects.AddUnique(CriticalEffect.SILENCE);
				}
				if(EquippedWeapon != null && !EquippedWeapon.status[EquipmentStatus.NEGATED]){
					switch(EquippedWeapon.enchantment){
					case EnchantmentType.CHILLING:
						effects.AddUnique(CriticalEffect.CHILL);
						break;
					case EnchantmentType.DISRUPTION:
						effects.AddUnique(CriticalEffect.DISRUPTION); //not entirely sure that these should be crit effects
						break;
					case EnchantmentType.VICTORY:
						if(a.maxhp > 1){ // no illusions, phantoms, or minions
							effects.AddUnique(CriticalEffect.VICTORY);
						}
						break;
					}
				}
				if(type == ActorType.INFESTED_MASS_TODO_NAME && HasAttr(AttrType.COOLDOWN_1)){
					effects.Remove(CriticalEffect.INFEST);
				}
				if(a.HasAttr(AttrType.NONLIVING)){
					effects.Remove(CriticalEffect.DRAIN_LIFE);
				}
				foreach(CriticalEffect effect in effects){ //pre-damage effects - these can alter the amount of damage.
					switch(effect){
					case CriticalEffect.MAX_DAMAGE:
						dmg = Math.Max(dmg,dice * 6);
						break;
					case CriticalEffect.PERCENT_DAMAGE:
						dmg = Math.Max(dmg,(a.maxhp+1)/2);
						no_max_damage_message = true;
						B.Add("TODO: CRIT: 50% damage! "); //todo
						break;
					}
				}
				if(dice < 2){
					no_max_damage_message = true;
				}
				if(dmg >= dice * 6 && !no_max_damage_message){
					if(a != player){
						B.Add("It was a good hit! ");
					}
					else{
						B.Add("Ow! ");
					}
				}
				dmg += TotalSkill(SkillType.COMBAT);
				int r = a.row;
				int c = a.col;
				bool still_alive = true;
				bool knockback_effect = effects.Contains(CriticalEffect.KNOCKBACK) || effects.Contains(CriticalEffect.STRONG_KNOCKBACK) || effects.Contains(CriticalEffect.TRIP) || effects.Contains(CriticalEffect.FLING);
				if(knockback_effect){
					a.attrs[AttrType.TURN_INTO_CORPSE] = 1;
				}
				if(dice > 0){
					Damage damage = new Damage(info.damage.type,info.damage.damclass,this,dmg);
					damage.weapon_used = EquippedWeapon.type;
					still_alive = a.TakeDamage(damage,a_name);
				}
				if(still_alive){ //post-damage crit effects that require the target to still be alive
					foreach(CriticalEffect effect in effects){
						if(still_alive){
							switch(effect){
							case CriticalEffect.BLIND: //if it exists, 5-7 turns.
								break;
							case CriticalEffect.DIM_VISION: //2d20+20
								break;
							case CriticalEffect.CHILL:
								if(!a.HasAttr(AttrType.IMMUNE_COLD)){
									B.Add(a.the_name + " is chilled. ",a);
									if(!a.HasAttr(AttrType.CHILLED)){
										a.attrs[AttrType.CHILLED] = 1;
									}
									else{
										a.attrs[AttrType.CHILLED] *= 2;
									}
									if(!a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,a.attrs[AttrType.CHILLED],this)){
										still_alive = false;
									}
								}
								break;
							case CriticalEffect.DISRUPTION:
								if(a.HasAttr(AttrType.NONLIVING)){
									B.Add(a.the_name + " is disrupted. ",a);
									if(!a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,a.maxhp / 5,this)){
										still_alive = false;
									}
								}
								break;
							case CriticalEffect.FREEZE:
								break;
							case CriticalEffect.GRAB:
								if(!HasAttr(AttrType.GRABBING) && DistanceFrom(a) == 1){
									a.attrs[AttrType.GRABBED]++;
									attrs[AttrType.GRABBING] = DirectionOf(a);
									B.Add(YouVisible("grab") + " " + a.TheName(true) + ". ",this,a);
								}
								break;
							case CriticalEffect.POISON:
								//check for immunities here. (what did I decide was actually immune to this? nothing? well, dart frogs kinda need to be immune, don't they?)
								//and don't print the message if they're already poisoned, just refresh.
								break;
							case CriticalEffect.PARALYZE:
								B.Add(a.YouAre() + " paralyzed! ",a);
								a.attrs[AttrType.PARALYZED] = a.DurationOfMagicalEffect(R.Between(3,5));
								break;
							case CriticalEffect.ONE_TURN_PARALYZE:
								Event e = Q.FindAttrEvent(a,AttrType.STUNNED);
								if(e != null && e.delay == 100 && e.TimeToExecute() == Q.turn){ //if the target was hit with a 1-turn stun that's about to expire, don't print a message for it.
									e.msg = "";
								}
								B.Add(a.YouAre() + " paralyzed! ",a);
								a.attrs[AttrType.PARALYZED] = 2; //setting it to 1 means it would end immediately
								break;
							case CriticalEffect.INFLICT_VULNERABILITY: //like 2d4 turns?
								break;
							case CriticalEffect.IGNITE:
								break;
							case CriticalEffect.INFEST:
								if(a == player && !a.EquippedArmor.status[EquipmentStatus.INFESTED]){
									B.Add("Thousands of insects crawl into your " + a.EquippedArmor.NameWithoutEnchantment() + "! ");
									a.EquippedArmor.status[EquipmentStatus.INFESTED] = true;
								}
								break;
							case CriticalEffect.SLOW:
								break;
							case CriticalEffect.REDUCE_ACCURACY: //also about 2d4 turns?
								break;
							case CriticalEffect.SLIME: //like 10-20 turns
								break;
							case CriticalEffect.STUN: //2d3 turns, at most
								B.Add(a.YouAre() + " stunned! ",a);
								a.RefreshDuration(AttrType.STUNNED,R.Roll(2,3)*100,a.YouAre() + " no longer stunned. ",a);
								break;
							case CriticalEffect.ONE_TURN_STUN:
								B.Add(a.YouAre() + " stunned! ",a);
								a.RefreshDuration(AttrType.STUNNED,100,a.YouAre() + " no longer stunned. ",a);
								break;
							case CriticalEffect.SILENCE:
								break;
							case CriticalEffect.WEAK_POINT:
								break;
							case CriticalEffect.WORN_OUT:
								break;
							}
						}
					}
					if(HasAttr(AttrType.POISON_HIT) && !a.HasAttr(AttrType.NONLIVING)){
						B.Add(a.YouAre() + " poisoned. ",a);
						string msg = "";
						if(a == player){
							msg = "You are no longer poisoned. ";
						}
						a.RefreshDuration(AttrType.POISONED,(R.Roll(2,6)+6)*100,msg,a); //todo: reduce this duration by just a little?
					}
					if(HasAttr(AttrType.PARALYSIS_HIT) && attack_idx==1 && type == ActorType.CARRION_CRAWLER){ //todo: fix all this
						if(!a.HasAttr(AttrType.NONLIVING)){ //hack: carrion crawler only
							if(a == player){
								B.Add("You suddenly can't move! ");
							}
							else{
								B.Add(a.YouAre() + " paralyzed. ",a);
							}
							a.attrs[AttrType.PARALYZED] = R.Roll(2)+1;
						}
					}
					if(HasAttr(AttrType.DIM_VISION_HIT)){
						string str = "";
						if(a.type == ActorType.PLAYER){
							B.Add("Your vision grows weak. ");
							str = "Your vision returns to normal. ";
						}
						a.RefreshDuration(AttrType.DIM_VISION,a.DurationOfMagicalEffect(R.Roll(2,20)+20)*100,str);
					}
					if(HasAttr(AttrType.STUN_HIT)){
						B.Add(a.YouAre() + " stunned. ",a);
						int duration = 500;
						a.RefreshDuration(AttrType.STUNNED,duration,a.YouAre() + " no longer stunned. ",a);
					}
				}
				foreach(CriticalEffect effect in effects){ //effects that don't care whether the target is still alive
					switch(effect){
					case CriticalEffect.DRAIN_LIFE:
						if(curhp < maxhp){
							curhp += 10;
							if(curhp > maxhp){
								curhp = maxhp;
							}
							B.Add(You("drain") + " some life from " + a.TheName(true) + ". ",this); //todo this doesn't check anywhere for NONLIVING yet, does it?
						}
						break;
					case CriticalEffect.VICTORY:
						if(!still_alive){
							curhp += 5;
							if(curhp > maxhp){
								curhp = maxhp;
							}
						}
						break;
					case CriticalEffect.STALAGMITES:
					{
						List<Tile> tiles = new List<Tile>();
						foreach(Tile t in M.tile[r,c].TilesWithinDistance(1)){
							if(t.actor() == null && (t.type == TileType.FLOOR || t.type == TileType.STALAGMITE)){
								if(R.CoinFlip()){ //50% for each...
									tiles.Add(t);
								}
							}
						}
						foreach(Tile t in tiles){
							if(t.type == TileType.STALAGMITE){
								Q.KillEvents(t,EventType.STALAGMITE);
							}
							else{
								t.Toggle(this,TileType.STALAGMITE);
							}
						}
						Q.Add(new Event(tiles,150,EventType.STALAGMITE));
						break;
					}
					case CriticalEffect.MAKE_NOISE:
						break;
					case CriticalEffect.SWAP_POSITIONS:
						break;
					case CriticalEffect.TRIP:
						if(!a.HasAttr(AttrType.FLYING) && (a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK))){
							B.Add(YouVisible("trip") + " " + a.TheName(true) + ". ",this,a);
							KnockObjectBack(a,0); //todo: if it's a corpse, ONLY trip it if something is going to happen when it collides with the floor.
						} //...does that mean that dying enemies should break orbs too? let's just say that you trip them more forcefully. yeah, that's the ticket.
						break;
					case CriticalEffect.KNOCKBACK:
						if(a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
							KnockObjectBack(a,2);
						}
						break;
					case CriticalEffect.STRONG_KNOCKBACK:
						if(a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
							KnockObjectBack(a,5);
						}
						break;
					case CriticalEffect.FLING:
						if(a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
							attrs[AttrType.JUST_FLUNG] = 1;
							B.Add(a.YouAre() + " flung! ",a); //todo implement
						}
						break;
					}
				}
				if(knockback_effect){
					a.CorpseCleanup();
				}
				if(type == ActorType.SWORDSMAN || type == ActorType.PHANTOM_SWORDMASTER || type == ActorType.ALASI_SOLDIER){
					attrs[AttrType.COMBO_ATTACK]++;
					if(attrs[AttrType.COMBO_ATTACK] == 3){ //all these have 3-part combos
						attrs[AttrType.COMBO_ATTACK] = 0;
					}
				}
			}
			if(!hit && HasAttr(AttrType.BRUTISH_STRENGTH) && p.Equals(original_pos) && M.actor[target_original_pos] != null){
				Actor a2 = M.actor[target_original_pos];
				if(a2.HasAttr(AttrType.NO_CORPSE_KNOCKBACK) && a2.maxhp == 1){
					B.Add("You push " + a2.TheName(true) + ". ");
					a2.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,1,this);
				}
				else{
					a2.attrs[AttrType.TURN_INTO_CORPSE] = 1;
					KnockObjectBack(a2,5);
					a2.CorpseCleanup();
				}
			}
			if(HasAttr(AttrType.BRUTISH_STRENGTH) && p.Equals(original_pos)){
				if(M.actor[target_original_pos] != null && !M.actor[target_original_pos].HasAttr(AttrType.IMMOBILE)){
					Actor a2 = M.actor[target_original_pos];
					B.Add("You push " + a2.TheName(true) + ". ");
				}
				Move(target_original_pos.row,target_original_pos.col);
			}
			if(hit && EquippedWeapon.enchantment == EnchantmentType.ECHOES && !EquippedWeapon.status[EquipmentStatus.NEGATED]){
				List<Tile> line = GetBestExtendedLineOfEffect(target_original_pos.row,target_original_pos.col);
				int idx = line.IndexOf(M.tile[target_original_pos]);
				if(idx != -1 && line.Count > idx + 1){
					Actor next = line[idx+1].actor();
					if(next != null && next != this){
						Attack(attack_idx,next,true);
					}
				}
			}
			if(!attack_is_part_of_another_action && EquippedWeapon == Staff && p.Equals(original_pos) && true){ //if the monster moved last turn todo
				Move(target_original_pos.row,target_original_pos.col);
			}
			MakeNoise(6);
			if(!attack_is_part_of_another_action){
				Q.Add(new Event(this,info.cost));
			}
			return hit;
		}
		public void FireArrow(PhysicalObject obj){ FireArrow(GetBestExtendedLineOfEffect(obj),false); }
		public void FireArrow(List<Tile> line){ FireArrow(line,false); }
		public void FireArrow(List<Tile> line,bool free_attack){
			if(!free_attack && StunnedThisTurn()){
				return;
			}
			int mod = -30; //bows have base accuracy 45%
			if(magic_trinkets.Contains(MagicTrinketType.RING_OF_KEEN_SIGHT)){
				mod = -15; //keen eyes makes it 60%
			}
			mod += TotalSkill(SkillType.COMBAT);
			//Tile t = M.tile[obj.row,obj.col];
			Tile t = null;
			Actor a = null;
			bool actor_present = false;
			List<string> misses = new List<string>();
			List<Actor> missed = new List<Actor>();
			List<Tile> animation_line = new List<Tile>(line);
			line.RemoveAt(0); //remove the source of the arrow first
			if(line.Count > 12){
				line = line.GetRange(0,Math.Min(12,line.Count)); //todo: this part prevents attacks from echoing farther than 12 tiles away. that might be okay.
			}
			for(int i=0;i<line.Count;++i){
				a = line[i].actor();
				t = line[i];
				if(a != null){
					actor_present = true;
					if(a.IsHit(mod)){
						if(a.HasAttr(AttrType.TUMBLING)){
							a.attrs[AttrType.TUMBLING] = 0;
						}
						else{
							break;
						}
					}
					else{
						misses.Add("The arrow misses " + a.the_name + ". ");
						missed.Add(a);
					}
					a = null;
				}
				if(!t.passable){
					a = null;
					break;
				}
			}
			if(!free_attack){
				if(HasAttr(AttrType.FIERY_ARROWS)){
					B.Add(You("fire") + " a flaming arrow. ",this);
				}
				else{
					B.Add(You("fire") + " an arrow. ",this);
				}
				B.DisplayNow();
			}
			int idx = 0;
			foreach(Tile tile2 in animation_line){
				if(tile2.seen){
					++idx;
				}
				else{
					animation_line = animation_line.To(tile2);
					if(animation_line.Count > 0){
						animation_line.RemoveAt(animation_line.Count - 1);
					}
					break;
				}
			}
			if(animation_line.Count > 0){
				if(a != null){
					Screen.AnimateBoltProjectile(animation_line.To(a),Color.DarkYellow,20);
				}
				else{
					Screen.AnimateBoltProjectile(animation_line.To(t),Color.DarkYellow,20);
				}
			}
			idx = 0;
			foreach(string s in misses){
				B.Add(s,missed[idx]);
				++idx;
			}
			if(a != null){
				pos target_original_position = a.p;
				if(a.HasAttr(AttrType.IMMUNE_ARROWS)){
					B.Add("The arrow sticks out ineffectively from " + a.the_name + ". ",a);
				}
				else{
					if(a.magic_trinkets.Contains(MagicTrinketType.BRACERS_OF_ARROW_DEFLECTION)){
						B.Add(a.You("deflect") + " the arrow! ",a);
					}
					else{
						bool alive = true;
						bool crit = false;
						int crit_multiplier = 1; //base crit rate is 10%
						if(a.EquippedArmor != null && (a.EquippedArmor.status[EquipmentStatus.WEAK_POINT] || a.EquippedArmor.status[EquipmentStatus.DAMAGED])){
							crit_multiplier *= 2;
						}
						if(a.HasAttr(AttrType.SUSCEPTIBLE_TO_CRITS)){
							crit_multiplier *= 2;
						}
						if(Bow != null && Bow.enchantment == EnchantmentType.PRECISION && !Bow.status[EquipmentStatus.NEGATED]){
							crit_multiplier *= 2;
						}
						if(R.Roll(10) + crit_multiplier > 10){
							crit = true;
						}
						B.Add("The arrow hits " + a.the_name + ". ",a);
						if(!a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(3,6)+TotalSkill(SkillType.COMBAT),this,a_name + "'s arrow")){
							alive = false;
						}
						if(this == player && crit && alive){
							Event e = Q.FindAttrEvent(a,AttrType.IMMOBILE);
							if(!a.HasAttr(AttrType.IMMOBILE) || (e != null && e.msg.Contains("no longer pinned"))){
								B.Add(a.YouAre() + " pinned! ");
								a.RefreshDuration(AttrType.IMMOBILE,100,a.the_name + " is no longer pinned. ",a);
							}
						}
						if(alive && a.HasAttr(AttrType.NONLIVING)){
							if(Bow != null && Bow.enchantment == EnchantmentType.DISRUPTION && !Bow.status[EquipmentStatus.NEGATED]){
								B.Add(a.the_name + " is disrupted! ",a);
								if(!a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,a.maxhp / 5,this)){
									alive = false;
								}
							}
						}
						if(alive && !a.HasAttr(AttrType.IMMUNE_COLD)){
							if(Bow != null && Bow.enchantment == EnchantmentType.CHILLING && !Bow.status[EquipmentStatus.NEGATED]){
								B.Add(a.the_name + " is chilled. ",a);
								if(!a.HasAttr(AttrType.CHILLED)){
									a.attrs[AttrType.CHILLED] = 1;
								}
								else{
									a.attrs[AttrType.CHILLED] *= 2;
								}
								if(!a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,a.attrs[AttrType.CHILLED],this)){
									alive = false;
								}
							}
						}
						if(alive && HasAttr(AttrType.FIERY_ARROWS) && !a.HasAttr(AttrType.IMMUNE_FIRE) && !a.HasAttr(AttrType.INVULNERABLE)){
							if(!a.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,R.Roll(6),this,Your() + " arrow")){ //todo: extra damage...or just catching you on fire?
								alive = false;
							}
						}
						if(!alive && Bow != null && Bow.enchantment == EnchantmentType.VICTORY && !Bow.status[EquipmentStatus.NEGATED]){
							curhp += 5;
							if(curhp > maxhp){
								curhp = maxhp;
							}
						}
					}
				}
				if(Bow != null && Bow.enchantment == EnchantmentType.ECHOES && !Bow.status[EquipmentStatus.NEGATED]){
					List<Tile> line2 = line.From(M.tile[target_original_position]);
					if(line2.Count > 1){
						FireArrow(line2,true);
					}
				}
			}
			else{
				if(!actor_present){
					B.Add("The arrow hits " + t.the_name + ". ",t);
				}
			}
			if(!free_attack){
				Q1();
			}
		}
		public bool IsHit(int plus_to_hit){
			if(R.Roll(1,100) + plus_to_hit <= 25){ //base hit chance is 75%
				return false;
			}
			return true;
		}
		public void CorpseCleanup(){
			attrs[AttrType.TURN_INTO_CORPSE] = 0;
			if(HasAttr(AttrType.CORPSE)){
				Kill();
			}
		}
		public bool Kill(){ return TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,9999,null); }
		public bool TakeDamage(DamageType dmgtype,DamageClass damclass,int dmg,Actor source){
			return TakeDamage(new Damage(dmgtype,damclass,source,dmg),"");
		}
		public bool TakeDamage(DamageType dmgtype,DamageClass damclass,int dmg,Actor source,string cause_of_death){
			return TakeDamage(new Damage(dmgtype,damclass,source,dmg),cause_of_death);
		}
		public bool TakeDamage(Damage dmg,string cause_of_death){ //returns true if still alive
			bool damage_dealt = false;
			int old_hp = curhp;
			if(curhp <= 0 && dmg.amount < 1000){ //then we're dealing with a corpse, and they don't take normal amounts of damage
				return true;
			}
			if(dmg.amount < 1000){
				if(HasAttr(AttrType.FROZEN)){
					//attrs[AttrType.FROZEN] -= (dmg.amount+1) / 2;
					attrs[AttrType.FROZEN] -= (dmg.amount * 9) / 10; //todo: make frozen block 100% of damage unless the amount is over 1000
					if(attrs[AttrType.FROZEN] <= 0){
						attrs[AttrType.FROZEN] = 0;
						B.Add("The ice breaks! ",this);
					}
					//dmg.amount = dmg.amount / 2;
					dmg.amount = dmg.amount / 10;
				}
				if(HasAttr(AttrType.MECHANICAL_SHIELD)){
					B.Add(Your() + " shield moves to protect it from harm. ",this);
					return true;
				}
				if(HasAttr(AttrType.INVULNERABLE)){
					dmg.amount = 0;
				}
				/*if(HasAttr(AttrType.TOUGH) && dmg.damclass == DamageClass.PHYSICAL){
					dmg.amount -= 2;
				}*/
				if(dmg.damclass == DamageClass.MAGICAL){
					dmg.amount -= TotalSkill(SkillType.SPIRIT) / 2;
				}
				if(HasAttr(AttrType.ARCANE_SHIELDED)){
					if(attrs[AttrType.ARCANE_SHIELDED] >= dmg.amount){
						attrs[AttrType.ARCANE_SHIELDED] -= dmg.amount;
						dmg.amount = 0;
					}
					else{
						dmg.amount -= attrs[AttrType.ARCANE_SHIELDED];
						attrs[AttrType.ARCANE_SHIELDED] = 0;
					}
					if(!HasAttr(AttrType.ARCANE_SHIELDED)){
						B.Add(Your() + " shield fades. ",this);
					}
				}
				if(dmg.amount > 15 && magic_trinkets.Contains(MagicTrinketType.BELT_OF_TOUGHNESS)){
					dmg.amount = 15;
					B.Add(Your() + " " + MagicTrinket.Name(MagicTrinketType.BELT_OF_TOUGHNESS) + " softens the blow. ",this);
				}
			}
			//todo: check for mace, sword, weapon, whatever, for dulling, and mud elemental, and spore pod
			switch(dmg.type){
			case DamageType.NORMAL:
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
				}
				else{
					B.Add(YouAre() + " undamaged. ",this);
				}
				break;
			case DamageType.MAGIC:
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
				}
				else{
					B.Add(YouAre() + " unharmed. ",this);
				}
				break;
			case DamageType.FIRE:
				{
				int div = 1;
				if(HasAttr(AttrType.IMMUNE_FIRE)){
					dmg.amount = 0;
					//B.Add(the_name + " is immune! ",this);
				}
				dmg.amount = dmg.amount / div;
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
				}
				else{
					B.Add(YouAre() + " unburnt. ",this);
				}
				break;
				}
			case DamageType.COLD:
				{
				int div = 1;
				if(HasAttr(AttrType.IMMUNE_COLD)){
					dmg.amount = 0;
					//B.Add(YouAre() + " unharmed. ",this);
				}
				dmg.amount = dmg.amount / div;
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
				}
				else{
					B.Add(YouAre() + " unharmed. ",this);
				}
				break;
				}
			case DamageType.ELECTRIC:
				{
				int div = 1;
				if(HasAttr(AttrType.IMMUNE_ELECTRICITY)){
					dmg.amount = 0;
				}
				dmg.amount = dmg.amount / div;
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
				}
				else{
					B.Add(YouAre() + " unharmed. ",this);
				}
				break;
				}
			case DamageType.POISON:
				if(HasAttr(AttrType.NONLIVING)){
					dmg.amount = 0;
				}
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
					if(type == ActorType.PLAYER){
						B.Add("The poison burns! ");
					}
					else{
						if(R.Roll(1,5) == 5 && !HasAttr(AttrType.PLANTLIKE)){ //hmm
							B.Add(the_name + " shudders. ",this);
						}
					}
				}
				break;
			case DamageType.NONE:
				break;
			}
			/*if(dmg.source != null && dmg.source == player && dmg.damclass == DamageClass.PHYSICAL && resisted && !cause_of_death.Contains("arrow")){
				Help.TutorialTip(TutorialTopic.Resistance);
			}*/
			if(damage_dealt){
				recover_time = Q.turn + 500;
				Interrupt();
				if(HasAttr(AttrType.ASLEEP)){
					attrs[AttrType.ASLEEP] = 0;
					Global.FlushInput();
				}
				if(dmg.source != null){
					if(type != ActorType.PLAYER && dmg.source != this){
						target = dmg.source;
						target_location = M.tile[dmg.source.row,dmg.source.col];
						if(dmg.source.IsHiddenFrom(this)){
							player_visibility_duration = -1;
						}
						if(type == ActorType.DREAM_SPRITE){
							attrs[AttrType.COOLDOWN_2] = 1;
						}
						if(type == ActorType.CRUSADING_KNIGHT && dmg.source == player && !HasAttr(AttrType.COOLDOWN_1) && !M.wiz_lite && !CanSee(player) && curhp > 0){
							List<string> verb = new List<string>{"Show yourself","Reveal yourself","Unfold thyself","Present yourself","Unveil yourself","Make yourself known"};
							List<string> adjective = new List<string>{"despicable","filthy","foul","nefarious","vulgar","sorry","unworthy"};
							List<string> noun = new List<string>{"villain","blackguard","devil","scoundrel","wretch","cur","rogue"};
							B.Add(TheName(true) + " shouts \"" + verb.Random() + ", " + adjective.Random() + " " + noun.Random() + "!\" ");
							B.Add(the_name + " raises a gauntlet. ",this);
							B.Add("Sunlight fills the dungeon. ");
							M.wiz_lite = true;
							M.wiz_dark = false;
							Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
							attrs[AttrType.COOLDOWN_1]++;
							foreach(Actor a in M.AllActors()){
								if(a != this && a != player && !a.HasAttr(AttrType.BLINDSIGHT) && HasLOS(a)){
									B.Add(a.YouAre() + " blinded! ",a);
									a.RefreshDuration(AttrType.BLIND,R.Between(5,9)*100,a.YouAre() + " no longer blinded. ",a);
								}
							}
							if(!player.HasAttr(AttrType.BLINDSIGHT) && HasLOS(player)){ //do the player last, so all the previous messages can be seen.
								B.Add(player.YouAre() + " blinded! ");
								player.RefreshDuration(AttrType.BLIND,R.Between(5,9)*100,player.YouAre() + " no longer blinded. ");
							}
						}
					}
				}
				if(HasAttr(AttrType.SPORE_BURST) && !HasAttr(AttrType.COOLDOWN_1)){
					if(type == ActorType.SPORE_POD){
						curhp = 0;
						B.Add("The spore pod bursts! ",this);
						//AnimateExplosion(this,3,'*',Color.DarkYellow);
						for(int i=1;i<=3;++i){
							AnimateStorm(i,1,8,'*',Color.DarkYellow);
						}
						//todo this doesn't do anything yet
					}
					else{
						attrs[AttrType.COOLDOWN_1]++;
						Q.Add(new Event(this,(R.Roll(1,5)+1)*100,AttrType.COOLDOWN_1)); //todo update?
						B.Add(You("retaliate") + " with a burst of spores! ",this);
						for(int i=2;i<=8;i+=2){
							AnimateStorm(i,1,(((i*2)+1)*((i*2)+1)) / 4,'*',Color.DarkYellow); //todo this awful animation
						}
						foreach(Actor a in ActorsWithinDistance(8)){
							if(HasLOE(a.row,a.col) && a != this){
								B.Add("The spores hit " + a.the_name + ". ",a);
								if(!a.HasAttr(AttrType.NONLIVING) && !a.HasAttr(AttrType.SPORE_BURST)){
									int duration = R.Roll(2,4);
									B.Add(a.YouAre() + " poisoned. ",a);
									string msg = "";
									if(a == player){
										msg = "You are no longer poisoned. ";
									}
									a.RefreshDuration(AttrType.POISONED,duration*100,msg,a);
									if(!a.HasAttr(AttrType.STUNNED)){
										a.attrs[AttrType.STUNNED]++;
										Q.Add(new Event(a,duration*100,AttrType.STUNNED,a.YouAre() + " no longer stunned. ",a));
										B.Add(a.YouAre() + " stunned. ",a);
									}
								}
								else{
									B.Add(a.YouAre() + " unaffected. ",a);
								}
							}
						}
					}
				}
				if(HasAttr(AttrType.HOLY_SHIELDED) && dmg.source != null){
					B.Add(YourVisible() + " holy shield burns " + dmg.source.TheName(true) + ". ",this,dmg.source);
					int amount = R.Roll(2,6);
					if(amount >= dmg.source.curhp){
						amount = dmg.source.curhp - 1;
					}
					dmg.source.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,amount,this); //doesn't yet prevent loops involving 2 holy shields.
				}
				if(HasFeat(FeatType.BOILING_BLOOD) && dmg.type != DamageType.POISON && attrs[AttrType.BLOOD_BOILED] < 5){
					B.Add("Your blood boils! ");
					GainAttrRefreshDuration(AttrType.BLOOD_BOILED,1000,"Your blood cools. ");
				}
				if(type == ActorType.MECHANICAL_KNIGHT){
					if(old_hp == 5){
						curhp = 0;
					}
					else{
						if(old_hp == 10){
							curhp = 5;
							switch(R.Roll(3)){
							case 1:
								B.Add(Your() + " arms are destroyed! ",this);
								attrs[AttrType.COOLDOWN_1] = 1;
								attrs[AttrType.MECHANICAL_SHIELD] = 0;
								break;
							case 2:
								B.Add(Your() + " legs are destroyed! ",this);
								attrs[AttrType.COOLDOWN_1] = 2;
								path.Clear();
								target_location = null;
								break;
							case 3:
								B.Add(Your() + " head is destroyed! ",this);
								attrs[AttrType.COOLDOWN_1] = 3;
								break;
							}
						}
					}
				}
				if(dmg.type == DamageType.FIRE && (type == ActorType.TROLL || type == ActorType.TROLL_BLOODWITCH)){
					attrs[AttrType.PERMANENT_DAMAGE] += dmg.amount; //permanent damage doesn't regenerate
				}
				if(dmg.type == DamageType.FIRE && type == ActorType.INFESTED_MASS_TODO_NAME && !HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					B.Add("The fire kills " + Your() + " insects. ",this);
					color = Color.White;
				}
			}
			if(curhp <= 0){
				if(type == ActorType.PLAYER){
					if(magic_trinkets.Contains(MagicTrinketType.PENDANT_OF_LIFE)){
						curhp = 1;
						if(R.CoinFlip()){
							B.Add("Your pendant glows brightly, then crumbles to dust! ");
							magic_trinkets.Remove(MagicTrinketType.PENDANT_OF_LIFE);
						}
						else{
							B.Add("Your pendant glows brightly! ");
						}
					}
					else{
						if(cause_of_death.Length > 0 && cause_of_death[0] == '*'){
							Global.KILLED_BY = cause_of_death.Substring(1);
						}
						else{
							Global.KILLED_BY = "killed by " + cause_of_death;
						}
						M.Draw();
						if(Global.GAME_OVER == false){
							B.Add("You die. ");
						}
						B.PrintAll();
						Global.GAME_OVER = true;
						return false;
					}
				}
				else{
					if(HasAttr(AttrType.BOSS_MONSTER)){
						M.Draw();
						B.Add("The fire drake dies. ");
						B.PrintAll();
						if(player.curhp > 0){
							B.Add("The threat to your nation has been slain! You begin the long trek home to deliver the good news... ");
							Global.KILLED_BY = "Died of ripe old age";
						}
						else{
							B.Add("The threat to your nation has been slain! Unfortunately, you won't be able to deliver the news... ");
						}
						B.PrintAll();
						Global.GAME_OVER = true;
						Global.BOSS_KILLED = true;
					}
					if(type == ActorType.BERSERKER && dmg.amount < 1000){ //hack
						if(!HasAttr(AttrType.COOLDOWN_1)){
							attrs[AttrType.COOLDOWN_1]++; //todo, something broke here and the berserker never stops berserking
							Q.Add(new Event(this,300,AttrType.COOLDOWN_1)); //changed from 350
							Q.KillEvents(this,AttrType.COOLDOWN_2);
							if(!HasAttr(AttrType.COOLDOWN_2)){
								attrs[AttrType.COOLDOWN_2] = DirectionOf(player);
							}
							B.Add(the_name + " somehow remains standing! He screams with fury! ",this);
						}
						return true;
					}
					if(type == ActorType.CULTIST && dmg.type == DamageType.FIRE){
						B.Add(the_name + " is consumed by flames. ",this); //todo: keep this or not?
					}
					if(dmg.amount < 1000){ //everything that deals this much damage prints its own message
						if(HasAttr(AttrType.REGENERATES_FROM_DEATH) && dmg.type != DamageType.FIRE){
							B.Add(the_name + " falls to the ground, still twitching. ",this);
							Tile troll = null;
							foreach(Tile t in M.ReachableTilesByDistance(row,col,false)){
								if(!t.Is(TileType.DOOR_O) && !t.Is(FeatureType.TROLL_CORPSE,FeatureType.TROLL_BLOODWITCH_CORPSE,FeatureType.BONES)){
									if(type == ActorType.TROLL){
										t.AddFeature(FeatureType.TROLL_CORPSE);
									}
									else{
										t.AddFeature(FeatureType.TROLL_BLOODWITCH_CORPSE);
									}
									troll = t;
									break;
								}
							}
							curhp -= R.Roll(10)+5;
							if(curhp < -50){
								curhp = -50;
							}
							Q.Add(new Event(troll,100,EventType.REGENERATING_FROM_DEATH,curhp + attrs[AttrType.PERMANENT_DAMAGE]*1000));
						}
						else{
							if(HasAttr(AttrType.REASSEMBLES)){
								if(dmg.weapon_used == WeaponType.MACE && R.CoinFlip()){
									B.Add(the_name + " is smashed to pieces. ",this);
								}
								else{
									B.Add(the_name + " collapses into a pile of bones. ",this);
									Tile sk = null;
									foreach(Tile t in M.ReachableTilesByDistance(row,col,false)){
										if(!t.Is(TileType.DOOR_O) && !t.Is(FeatureType.TROLL_CORPSE,FeatureType.TROLL_BLOODWITCH_CORPSE,FeatureType.BONES)){
											if(type == ActorType.SKELETON){
												t.AddFeature(FeatureType.BONES);
											}
											sk = t;
											break;
										}
									}
									Q.Add(new Event(sk,R.Between(10,20)*100,EventType.REASSEMBLING));
								}
							}
							else{
								if(!HasAttr(AttrType.BOSS_MONSTER)){
									if(HasAttr(AttrType.NONLIVING)){
										B.Add(the_name + " is destroyed. ",this);
									}
									else{
										B.Add(the_name + " dies. ",this);
									}
								}
							}
						}
					}
					if(HasAttr(AttrType.TURN_INTO_CORPSE)){
						attrs[AttrType.TURN_INTO_CORPSE] = 0;
						attrs[AttrType.CORPSE] = 1;
						if(!HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
							if(HasAttr(AttrType.NONLIVING)){
								SetName("destroyed " + name);
							}
							else{
								SetName(name + "'s corpse");
							}
						}
						return false;
					}
					if(LightRadius() > 0){
						UpdateRadius(LightRadius(),0);
					}
					if(type == ActorType.SHADOW){
						if(player.HasAttr(AttrType.DIM_LIGHT)){
							type = ActorType.ZOMBIE; //awful awful hack. (CalculateDimming checks for Shadows)
							CalculateDimming();
						}
					}
					if(type == ActorType.STONE_GOLEM){
						foreach(Tile t in TilesWithinDistance(4)){
							if(t.name == "floor" && (t.actor() == null || t.actor() == this) && HasLOE(t)){
								if(DistanceFrom(t) <= 2 || R.CoinFlip()){
									t.TransformTo(TileType.RUBBLE);
								}
							}
						}
					}
					if(type == ActorType.VULGAR_DEMON && dmg.source != null && dmg.source == player && DistanceFrom(player) == 1){
						B.Add(the_name + " possesses your " + player.EquippedWeapon + ". ");
						player.EquippedWeapon.status[EquipmentStatus.POSSESSED] = true;
					}
					if(type == ActorType.DREAM_SPRITE){
						Tile current = tile();
						int num = R.Roll(5) + 4;
						List<Tile> new_area = new List<Tile>();
						for(int i=0;i<num;++i){
							if(!current.Is(FeatureType.PIXIE_DUST)){
								current.AddFeature(FeatureType.PIXIE_DUST);
								new_area.Add(current);
							}
							else{
								for(int tries=0;tries<50;++tries){
									List<Tile> open = new List<Tile>();
									foreach(Tile t in current.TilesAtDistance(1)){
										if(t.passable){
											open.Add(t);
										}
									}
									if(open.Count > 0){
										Tile possible = open.Random();
										if(!possible.Is(FeatureType.PIXIE_DUST)){
											possible.AddFeature(FeatureType.PIXIE_DUST);
											new_area.Add(possible);
											break;
										}
										else{
											current = possible;
										}
									}
									else{
										break;
									}
								}
							}
						}
						if(new_area.Count > 0){
							Q.Add(new Event(new_area,400,EventType.PIXIE_DUST));
						}
					}
					if(type == ActorType.CRYOLICH){
						bool disruption = false;
						foreach(Actor a in ActorsWithinDistance(2)){
							if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
								disruption = true;
								break;
							}
						}
						if(!disruption){
							exhaustion = 0;
							curmp = 5; //no failing to cast this one either
							CastSpell(SpellType.BLIZZARD);
						}
					}
					if(player.HasAttr(AttrType.CONVICTION)){
						player.attrs[AttrType.KILLSTREAK]++;
					}
					if((HasAttr(AttrType.HUMANOID_INTELLIGENCE) && type != ActorType.FIRE_DRAKE)
					   || type == ActorType.ZOMBIE){
						if(R.CoinFlip() && !HasAttr(AttrType.NO_ITEM)){
							tile().GetItem(Item.Create(Item.RandomItem(),-1,-1));
						}
					}
					foreach(Item item in inv){
						tile().GetItem(item);
					}
					Q.KillEvents(this,EventType.ANY_EVENT);
					M.RemoveTargets(this);
					int idx = Actor.tiebreakers.IndexOf(this);
					if(idx != -1){
						Actor.tiebreakers[Actor.tiebreakers.IndexOf(this)] = null;
					}
					if(group != null){
						if(type == ActorType.DREAM_WARRIOR || type == ActorType.DREAM_SPRITE){
							List<Actor> temp = new List<Actor>();
							foreach(Actor a in group){
								if(a != this){
									temp.Add(a);
									a.group = null;
								}
							}
							foreach(Actor a in temp){
								a.Kill();
							}
						}
						else{
							if(group.Count >= 2 && this == group[0] && HasAttr(AttrType.WANDERING)){
								if(type != ActorType.NECROMANCER){
									group[1].attrs[AttrType.WANDERING]++;
								}
							}
							if(group.Count <= 2 || type == ActorType.NECROMANCER){
								foreach(Actor a in group){
									if(a != this){
										a.group = null;
									}
								}
								group.Clear();
								group = null;
							}
							else{
								group.Remove(this);
								group = null;
							}
						}
					}
					M.actor[row,col] = null;
					return false;
				}
			}
			else{
				if(HasFeat(FeatType.FEEL_NO_PAIN) && damage_dealt && curhp < 20 && old_hp >= 20){
					B.Add("You can feel no pain! ");
					attrs[AttrType.INVULNERABLE]++;
					Q.Add(new Event(this,500,AttrType.INVULNERABLE,"You can feel pain again. "));
				}
				if(magic_trinkets.Contains(MagicTrinketType.CLOAK_OF_SAFETY) && damage_dealt && dmg.amount >= curhp){
					B.PrintAll();
					M.Draw();
					B.DisplayNow("Your cloak starts to vanish. Use your cloak to escape?(y/n): ");
					Console.CursorVisible = true;
					ConsoleKeyInfo command;
					bool done = false;
					while(!done){
						command = Console.ReadKey(true);
						switch(command.KeyChar){
						case 'n':
						case 'N':
							done = true;
							break;
						case 'y':
						case 'Y':
							done = true;
							bool[,] good = new bool[ROWS,COLS];
							foreach(Tile t in M.AllTiles()){
								if(t.passable){
									good[t.row,t.col] = true;
								}
								else{
									good[t.row,t.col] = false;
								}
							}
							foreach(Actor a in M.AllActors()){
								foreach(Tile t in M.AllTiles()){
									if(good[t.row,t.col]){
										if(a.DistanceFrom(t) < 6 || a.HasLOS(t.row,t.col)){ //was CanSee, but this is safer
											good[t.row,t.col] = false;
										}
									}
								}
							}
							List<Tile> tilelist = new List<Tile>();
							Tile destination = null;
							for(int i=4;i<COLS;++i){
								foreach(pos p in PositionsAtDistance(i)){
									if(good[p.row,p.col]){
										tilelist.Add(M.tile[p.row,p.col]);
									}
								}
								if(tilelist.Count > 0){
									destination = tilelist[R.Roll(1,tilelist.Count)-1];
									break;
								}
							}
							if(destination != null){
								Move(destination.row,destination.col);
							}
							else{
								for(int i=0;i<9999;++i){
									int rr = R.Roll(1,ROWS-2);
									int rc = R.Roll(1,COLS-2);
									if(M.tile[rr,rc].passable && M.actor[rr,rc] == null && DistanceFrom(rr,rc) >= 6 && !M.tile[rr,rc].IsTrap()){
										Move(rr,rc);
										break;
									}
								}
							}
							B.Add("You escape. ");
							break;
						default:
							break;
						}
					}
					B.Add("Your cloak vanishes completely! ");
					magic_trinkets.Remove(MagicTrinketType.CLOAK_OF_SAFETY);
				}
			}
			return true;
		}
		/*public bool GetKnockedBack(PhysicalObject obj){ return GetKnockedBack(obj.GetBestExtendedLine(row,col)); }
		public bool GetKnockedBack(List<Tile> line){
			int idx = line.IndexOf(M.tile[row,col]);
			if(idx == -1){
				B.Add("DEBUG: Error - " + the_name + "'s position doesn't seem to be in the line. ");
				return false;
			}
			Tile next = line[idx+1];
			Actor source = M.actor[line[0].row,line[0].col];
			bool no_movement = (GrabPreventsMovement(next) || HasAttr(AttrType.IMMOBILE));
			if(next.passable && M.actor[next.row,next.col] == null && !no_movement){
				if(player.CanSee(tile())){
					B.Add(YouAre() + " knocked back. ",this);
				}
				if(HasAttr(AttrType.FROZEN)){
					attrs[AttrType.FROZEN] = 0;
					if(player.CanSee(tile())){
						B.Add("The ice breaks! ",this);
					}
				}
				Move(next.row,next.col);
			}
			else{
				int r = row;
				int c = col;
				bool immobilized = HasAttr(AttrType.FROZEN);
				if(!next.passable){
					if(player.CanSee(tile())){
						B.Add(YouVisibleAre() + " knocked into " + next.TheName(true) + ". ",this,next);
					}
					TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(1,6),source,"*smashed against " + next.a_name);
				}
				else{
					if(M.actor[next.p] != null){
						if(player.CanSee(tile())){
							B.Add(YouVisibleAre() + " knocked into " + M.actor[next.row,next.col].TheName(true) + ". ",this,M.actor[next.row,next.col]); //vis
						}
						string this_name = a_name;
						TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(1,6),source,"*smashed against " + M.actor[next.p].a_name);
						M.actor[next.row,next.col].TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(1,6),source,"*smashed against " + this_name);
					}
					else{ //grabbed
						if(player.CanSee(tile())){
							B.Add(YouVisibleAre() + " knocked about. ",this);
						}
						Actor grabber = null;
						foreach(Actor a in ActorsAtDistance(1)){
							if(a.attrs[AttrType.GRABBING] == a.DirectionOf(this)){
								grabber = a;
							}
						}
						string grabber_name = "";
						if(grabber != null){
							grabber_name = grabber.a_name;
						}
						TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),source,"*smashed against " + grabber_name);
					}
				}
				if(immobilized && M.actor[r,c] != null){
					if(player.CanSee(tile())){
						B.Add("The ice breaks! ",this);
					}
				}
			}
			return true;
		}*/
		public void IncreaseExhaustion(int amount){
			int previous = exhaustion;
			int effective_previous = exhaustion;
			exhaustion += amount;
			if(exhaustion > 20){
				exhaustion = 20;
			}
			if(exhaustion < 0){
				exhaustion = 0;
			}
			if(this == player){
				int effective_exhaustion = exhaustion;
				if(HasFeat(FeatType.ARMOR_MASTERY)){
					effective_exhaustion -= 5;
					effective_previous -= 5;
				}
				bool msg = false;
				switch(EquippedArmor.type){
				case ArmorType.LEATHER:
					if(effective_exhaustion >= 15 && effective_previous < 15){
						msg = true;
					}
					break;
				case ArmorType.CHAINMAIL:
					if(effective_exhaustion >= 10 && effective_previous < 10){
						msg = true;
					}
					break;
				case ArmorType.FULL_PLATE:
					if(effective_exhaustion >= 5 && effective_previous < 5){
						msg = true;
					}
					break;
				}
				if(msg){
					B.Add("You can no longer wear your " + EquippedArmor + " effectively in your exhausted state. ");
				}
				if(exhaustion == 20 && previous < 20){
					B.Add("Your exhaustion makes it hard to even lift your " + EquippedWeapon + ". ");
				}
			}
		}
		public void CastCloseRangeSpellOrAttack(Actor a){ CastCloseRangeSpellOrAttack(null,a,false); }
		public void CastCloseRangeSpellOrAttack(List<SpellType> sp,Actor a,bool range_one_only){
			if(sp == null){
				sp = new List<SpellType>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(HasSpell(spell)){
						switch(spell){
						case SpellType.FORCE_PALM:
						case SpellType.MAGIC_HAMMER:
							sp.Add(spell);
							break;
						case SpellType.MERCURIAL_SPHERE:
						case SpellType.LIGHTNING_BOLT:
						case SpellType.GLACIAL_BLAST:
						case SpellType.COLLAPSE:
						case SpellType.BLIZZARD:
						case SpellType.FIRE_BLITZ: //todo placeholder spell?
							if(!range_one_only){
								sp.Add(spell);
							}
							break;
						case SpellType.FREEZE:
							if(!range_one_only && !a.HasAttr(AttrType.FROZEN)){
								sp.Add(spell);
							}
							break;
						case SpellType.SCORCH:
							if(!range_one_only && !a.HasAttr(AttrType.BURNING)){
								sp.Add(spell);
							}
							break;
						default:
							break;
						}
					}
				}
			}
			if(sp.Count > 0){
				CastRandomSpell(a,sp.ToArray());
			}
			else{
				Attack(0,a);
			}
		}
		public void CastRangedSpellOrMove(Actor a){ CastRangedSpellOrMove(null,a); }
		public void CastRangedSpellOrMove(List<SpellType> sp,Actor a){
			if(sp == null){
				sp = new List<SpellType>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(HasSpell(spell)){
						switch(spell){
						case SpellType.MERCURIAL_SPHERE:
						case SpellType.LIGHTNING_BOLT:
						case SpellType.GLACIAL_BLAST:
						case SpellType.COLLAPSE:
							sp.Add(spell);
							break;
						case SpellType.FREEZE:
							if(!a.HasAttr(AttrType.FROZEN)){
								sp.Add(spell);
							}
							break;
						case SpellType.SCORCH:
							if(!a.HasAttr(AttrType.BURNING)){
								sp.Add(spell);
							}
							break;
						case SpellType.BLIZZARD:
							if(DistanceFrom(a) <= 5){
								sp.Add(SpellType.BLIZZARD);
							}
							break;
						case SpellType.FIRE_BLITZ: //todo placeholder spell?
							if(DistanceFrom(a) <= 8){ //todo should this be 8 or 11 or what?
								sp.Add(SpellType.FIRE_BLITZ);
							}
							break;
						default:
							break;
						}
					}
				}
			}
			if(sp.Count > 0){
				CastRandomSpell(a,sp.ToArray());
			}
			else{
				AI_Step(a);
				QS();
			}
		}
		public bool CastSpell(SpellType spell){ return CastSpell(spell,null); }
		public bool CastSpell(SpellType spell,PhysicalObject obj){ //returns false if targeting is canceled.
			if(StunnedThisTurn()){ //eventually this will be moved to the last possible second
				return true; //returns true because turn was used up. 
			}
			if(!HasSpell(spell)){
				return false;
			}
			foreach(Actor a in ActorsWithinDistance(2)){
				if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
					if(this == player){
						if(CanSee(a)){
							B.Add(a.Your() + " presence disrupts your spell! ");
						}
						else{
							B.Add("Something disrupts your spell! ");
						}
					}
					return false;
				}
			}
			int required_mana = Spell.Level(spell);
			if(HasAttr(AttrType.CHAIN_CAST) && required_mana > 1){
				required_mana--;
			}
			if(curmp < required_mana && this == player){
				int missing_mana = required_mana - curmp;
				if(exhaustion + missing_mana > 20){
					B.Add("You're too exhausted! ");
					return false;
				}
				B.DisplayNow("Really exhaust yourself to cast this spell?(y/n): ");
				Console.CursorVisible = true;
				bool done = false;
				ConsoleKeyInfo command;
				while(!done){
					command = Console.ReadKey(true); //this could become a method
					switch(command.KeyChar){
					case 'y':
					case 'Y':
						done = true;
						break;
					default:
						return false;
					}
				}
				Console.CursorVisible = false;
			}
			Tile t = null;
			List<Tile> line = null;
			if(obj != null){
				t = M.tile[obj.row,obj.col];
				line = GetBestLineOfEffect(t);
			}
			if(exhaustion > 0){
				int fail = exhaustion * 5;
				if(R.PercentChance(fail)){
					if(HasFeat(FeatType.FORCE_OF_WILL)){
						B.Add("You focus your will. ");
					}
					else{
						if(player.CanSee(this)){
							B.Add("Sparks fly from " + Your() + " fingers. ",this);
						}
						else{
							if(player.DistanceFrom(this) <= 4 || (player.DistanceFrom(this) <= 12 && player.HasLOS(row,col))){
								B.Add("You hear words of magic, but nothing happens. ");
							}
						}
						Q1();
						return true;
					}
				}
			}
			int bonus = 0; //used for bonus damage on spells
			if(HasFeat(FeatType.MASTERS_EDGE)){
				foreach(SpellType s in spells_in_order){
					if(Spell.IsDamaging(s)){
						if(s == spell){
							bonus = 1;
						}
						break;
					}
				}
			}
			if(HasAttr(AttrType.EMPOWERED_SPELLS)){
				bonus++;
			}
			switch(spell){
			case SpellType.SHINE:
			{
				if(t == null){
					line = GetTargetTile(12,0,false);
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " shine. ",this);
					Actor a = t.actor();
					if(a != null){
						if(a == player){
							if(a.light_radius == 0 || M.wiz_dark){
								B.Add("Your torch is ready to shine brightly. ");
							}
							else{
								//Screen.AnimateMapCells(a.PositionsWithinDistance(a.light_radius+6).Where(x=>!M.tile[x].IsLit() && player.HasLOS(M.tile[x])),new colorchar('*',Color.Yellow),100);
								B.Add("Your torch begins to shine brightly. ");
							}
						}
						else{
							if(!M.wiz_dark){
								//Screen.AnimateMapCells(a.PositionsWithinDistance(a.light_radius+6).Where(x=>!M.tile[x].IsLit() && player.HasLOS(M.tile[x])),new colorchar('*',Color.Yellow),100);
								if(a.light_radius == 0){
									B.Add(a.TheName(false) + " starts glowing brightly. ");
								}
								else{
									B.Add(a.TheName(false) + "'s light glows brighter. ");
								}
								a.attrs[AttrType.TURNS_VISIBLE] = -1;
							}
							else{
								B.Add("Nothing seems to happen. ");
							}
						}
						if(a.type != ActorType.LUMINOUS_AVENGER){
							if(a == player){
								a.RefreshDuration(AttrType.SHINING,(R.Roll(2,20)+40)*100,"Your torch no longer shines as brightly. ");
							}
							else{
								a.RefreshDuration(AttrType.SHINING,(R.Roll(2,20)+40)*100,a.TheName(false) + " no longer glows brightly. ",a);
							}
							if(a.light_radius > 0 || a != player){
								a.UpdateRadius(a.LightRadius(),a.light_radius + 6 - attrs[AttrType.DIM_LIGHT]*2,true);
							}
						}
					}
					else{
						B.Add("Nothing happens. ");
					}
				}
				else{
					return false;
				}
				break;
			}
			/*case SpellType.IMMOLATE:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " immolate. ",this);
					Actor a = FirstActorInLine(line);
					line = line.ToFirstObstruction();
					AnimateBeam(line,'*',Color.RandomFire);
					foreach(Tile t2 in line){
						if(t2.Is(FeatureType.TROLL_CORPSE)){
							t2.features.Remove(FeatureType.TROLL_CORPSE);
							B.Add("The troll corpse burns to ashes! ",t2);
						}
						if(t2.Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){
							t2.features.Remove(FeatureType.TROLL_BLOODWITCH_CORPSE);
							B.Add("The troll seer corpse burns to ashes! ",t2);
						}
					}
					if(a != null){
						if(!a.HasAttr(AttrType.RESIST_FIRE) && !a.HasAttr(AttrType.CATCHING_FIRE) && !a.HasAttr(AttrType.BURNING)){
							if(a.name == "you"){
								B.Add("You start to catch fire! ");
							}
							else{
								B.Add(a.the_name + " starts to catch fire. ",a);
							}
							a.attrs[AttrType.CATCHING_FIRE]++;
						}
						else{
							B.Add(a.You("shrug") + " off the flames. ",a);
						}
					}
				}
				else{
					return false;
				}
				break;*/
			case SpellType.FORCE_PALM:
				if(t == null){
					t = TileInDirection(GetDirection());
				}
				if(t != null){
					Actor a = M.actor[t.row,t.col];
					B.Add(You("cast") + " force palm. ",this);
					//AnimateMapCell(t,Color.DarkCyan,'*');
					B.DisplayNow();
					Screen.AnimateMapCell(t.row,t.col,new colorchar('*',Color.Blue),100);
					if(a != null){
						B.Add(You("strike") + " " + a.TheName(true) + ". ",this,a);
						if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
							a.curmp += Spell.Level(spell);
							if(a.curmp > a.maxmp){
								a.curmp = a.maxmp;
							}
							a.GainSpell(spell);
							B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
						}
						a.attrs[AttrType.TURN_INTO_CORPSE] = 1;
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(1+bonus,6),this,a_name);
						if(a.curhp > 0 || !a.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
							KnockObjectBack(a);
						}
						a.CorpseCleanup();
					}
					else{
						if(t.passable){
							B.Add("You strike at empty space. ");
						}
						else{
							B.Add("You strike " + t.the_name + " with your palm. ");
							if(t.type == TileType.DOOR_C){ //heh, why not?
								B.Add("It flies open! ");
								t.Toggle(this);
							}
							if(t.type == TileType.HIDDEN_DOOR){ //and this one gives it an actual use
								B.Add("A hidden door flies open! ");
								t.Toggle(this);
								t.Toggle(this);
							}
						}
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.DETECT_MOVEMENT:
				B.Add(You("cast") + " detect movement. ",this);
				if(this == player){
					B.Add("Your senses sharpen. ");
					if(!HasAttr(AttrType.DETECTING_MOVEMENT)){
						previous_footsteps = new List<pos>(); //prevents old footsteps from appearing
					}
					RefreshDuration(AttrType.DETECTING_MOVEMENT,(R.Roll(2,20)+30)*100,"You no longer detect movement. ");
				}
				else{
					RefreshDuration(AttrType.DETECTING_MOVEMENT,(R.Roll(2,20)+30)*100);
				}
				break;
			case SpellType.RADIANCE:
			{
				if(t == null){
					line = GetTargetTile(12,0,(light_radius == 0)); //testing this - it'll start at the player if the torch is out.
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " radiance. ",this);
					PhysicalObject o = null;
					if(t.light_radius > 0){
						o = t;
					}
					else{
						if(t.inv != null && t.inv.light_radius > 0){
							o = t.inv;
						}
						else{
							if(t.actor() != null && t.actor().LightRadius() > 0){
								o = t.actor();
							}
						}
					}
					if(o != null && !M.wiz_dark){
						B.Add(o.the_name + " shines so bright it hurts. ",o); //todo!
						int rad = o.light_radius;
						if(o is Actor){
							rad = (o as Actor).LightRadius();
						}
						foreach(Actor a in o.ActorsWithinDistance(rad,true).Where(x=>x != this && o.HasLOS(x))){
							a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(2+bonus,6),this,"a shining " + o.name);
						}
					}
					else{
						B.Add("Nothing happens. ");
					}
				}
				else{
					return false;
				}
				break;
			}
			case SpellType.MERCURIAL_SPHERE:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " mercurial sphere. ",this);
					Actor a = FirstActorInLine(line);
					line = line.ToFirstObstruction();
					M.Draw();
					AnimateProjectile(line,'*',Color.Blue);
					List<string> targets = new List<string>();
					List<Tile> locations = new List<Tile>();
					if(a != null){
						for(int i=0;i<6;++i){
							if(player.CanSee(a)){
								targets.AddUnique(a.the_name);
							}
							Tile atile = a.tile();
							if(a != this){
								if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
									a.curmp += Spell.Level(spell);
									if(a.curmp > a.maxmp){
										a.curmp = a.maxmp;
									}
									a.GainSpell(spell);
									B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
								}
								a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(1+bonus,6),this,a_name);
							}
							a = atile.ActorsWithinDistance(3,true).Where(x=>atile.HasLOE(x)).Random();
							locations.AddUnique(atile);
							if(a == null){
								break;
							}
							if(i < 5){
								Screen.AnimateProjectile(atile.GetBestLineOfEffect(a),new colorchar('*',Color.Blue));
							}
						}
						int unknown = locations.Count - targets.Count; //every location for which we didn't see an actor
						if(unknown > 0){
							if(unknown == 1){
								targets.Add("one unknown creature"); //todo: this message tends to be pretty long. shorten it somehow?
							}
							else{
								targets.Add(unknown.ToString() + " unknown creatures");
							}
						}
						if(targets.Contains("you")){
							targets.Remove("you");
							targets.Add("you"); //move it to the end of the list
						}
						if(targets.Count == 1){
							B.Add("The sphere hits " + targets[0] + ". ",locations.ToArray());
						}
						else{
							B.Add("The sphere bounces between " + targets.ConcatenateListWithCommas() + ". ",locations.ToArray());
						}
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.GREASE:
			{
				if(t == null){
					line = GetTargetTile(12,0,false);
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null && t.passable){
					B.Add(You("cast") + " grease. ",this);
					B.Add("Oil covers the floor. ",t);
					foreach(Tile neighbor in t.TilesWithinDistance(1)){
						if(neighbor.passable){
							neighbor.AddFeature(FeatureType.OIL);
						}
					}
				}
				else{
					return false;
				}
				break;
			}
			case SpellType.BLINK:
				for(int i=0;i<9999;++i){
					int a = R.Roll(1,17) - 9; //-8 to 8
					int b = R.Roll(1,17) - 9;
					if(Math.Abs(a) + Math.Abs(b) >= 6){
						a += row;
						b += col;
						if(M.BoundsCheck(a,b) && M.tile[a,b].passable && M.actor[a,b] == null){
							B.Add(You("cast") + " blink. ",this);
							B.Add(You("step") + " through a rip in reality. ",this);
							AnimateStorm(2,3,4,'*',Color.DarkMagenta);
							Move(a,b);
							M.Draw();
							AnimateStorm(2,3,4,'*',Color.DarkMagenta);
							break;
						}
					}
				}
				break;
			case SpellType.FREEZE:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " freeze. ",this);
					Actor a = FirstActorInLine(line);
					if(a != null){
						AnimateBoltBeam(line.ToFirstObstruction(),Color.Cyan);
						if(!a.HasAttr(AttrType.FROZEN)){
							B.Add(a.YouAre() + " encased in ice. ",a);
							a.attrs[AttrType.FROZEN] = 35;
						}
						else{
							B.Add("The beam dissipates on the remaining ice. ",a);
						}
					}
					else{
						AnimateBoltBeam(line,Color.Cyan);
						B.Add("A bit of ice forms on " + t.the_name + ". ",t);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.SCORCH:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " scorch. ",this);
					Actor a = FirstActorInLine(line);
					line = line.ToFirstObstruction();
					AnimateProjectile(line,'*',Color.RandomFire);
					foreach(Tile t2 in line){
						t2.ApplyEffect(DamageType.FIRE);
					}
					if(a != null){
						B.Add("The scorching bolt hits " + a.the_name + ". ",a);
						if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
							a.curmp += Spell.Level(spell);
							if(a.curmp > a.maxmp){
								a.curmp = a.maxmp;
							}
							a.GainSpell(spell);
							B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
						}
						if(a.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,R.Roll(2+bonus,6),this,a_name)){
							a.RefreshDuration(AttrType.BURNING,(R.Roll(3)+4)*100,a.YouAre() + " no longer on fire. ",a); //check for resistances here, i guess. todo.
						}
					}
				}
				else{
					return false;
				}
				break;
			/*case SpellType.BLOODSCENT:
				if(!HasAttr(AttrType.BLOODSCENT)){
					B.Add(You("cast") + " bloodscent. ",this);
					attrs[AttrType.BLOODSCENT]++;
					if(type == ActorType.PLAYER){
						B.Add("You smell fear. ");
						Q.Add(new Event(this,10000,AttrType.BLOODSCENT,"You lose the scent. "));
					}
					else{
						Q.Add(new Event(this,10000,AttrType.BLOODSCENT));
					}
				}
				else{
					B.Add("You can already smell the blood of your enemies. ");
					return false;
				}
				break;*/
			case SpellType.LIGHTNING_BOLT:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " lightning bolt. ",this);
					PhysicalObject bolt_target = null;
					List<Actor> damage_targets = new List<Actor>();
					foreach(Tile t2 in line){
						if(t2.actor() != null && t2.actor() != this){
							bolt_target = t2.actor();
							damage_targets.Add(t2.actor());
							break;
						}
						else{
							if(t2.ConductsElectricity()){
								bolt_target = t2;
								break;
							}
						}
					}
					if(bolt_target != null){ //this code, man
						Dict<PhysicalObject,List<PhysicalObject>> chain = new Dict<PhysicalObject,List<PhysicalObject>>();
						chain[this] = new List<PhysicalObject>{bolt_target};
						List<PhysicalObject> last_added = new List<PhysicalObject>{bolt_target};
						for(bool done=false;!done;){
							done = true;
							List<PhysicalObject> new_last_added = new List<PhysicalObject>();
							foreach(PhysicalObject added in last_added){
								List<PhysicalObject> sort_list = new List<PhysicalObject>();
								foreach(Tile nearby in added.TilesWithinDistance(3,true)){
									if(nearby.actor() != null || nearby.ConductsElectricity()){
										if(added.HasLOE(nearby)){
											if(nearby.actor() != null){
												bolt_target = nearby.actor();
											}
											else{
												bolt_target = nearby;
											}
											bool contains_value = false;
											foreach(List<PhysicalObject> list in chain.d.Values){
												foreach(PhysicalObject o in list){
													if(o == bolt_target){
														contains_value = true;
														break;
													}
												}
												if(contains_value){
													break;
												}
											}
											if(!chain.d.ContainsKey(bolt_target) && !contains_value){
												if(bolt_target as Actor != null){
													damage_targets.AddUnique(bolt_target as Actor);
												}
												done = false;
												if(sort_list.Count == 0){
													sort_list.Add(bolt_target);
												}
												else{
													int idx = 0;
													foreach(PhysicalObject o in sort_list){
														if(bolt_target.DistanceFrom(added) < o.DistanceFrom(added)){
															sort_list.Insert(idx,bolt_target);
															break;
														}
														++idx;
													}
													if(idx == sort_list.Count){
														sort_list.Add(bolt_target);
													}
												}
												if(chain[added] == null){
													chain[added] = new List<PhysicalObject>{bolt_target};
												}
												else{
													chain[added].Add(bolt_target);
												}
											}
										}
									}
								}
								foreach(PhysicalObject o in sort_list){
									new_last_added.Add(o);
								}
							}
							if(!done){
								last_added = new_last_added;
							}
						} //whew. the tree structure is complete. start at chain[this] and go from there...
						Dict<int,List<pos>> frames = new Dict<int,List<pos>>();
						Dict<PhysicalObject,int> line_length = new Dict<PhysicalObject,int>();
						line_length[this] = 0;
						List<PhysicalObject> current = new List<PhysicalObject>{this};
						List<PhysicalObject> next = new List<PhysicalObject>();
						while(current.Count > 0){
							foreach(PhysicalObject o in current){
								if(chain[o] != null){
									foreach(PhysicalObject o2 in chain[o]){
										List<Tile> bres = o.GetBestLineOfEffect(o2);
										bres.RemoveAt(0);
										line_length[o2] = bres.Count + line_length[o];
										int idx = 0;
										foreach(Tile t2 in bres){
											if(frames[idx + line_length[o]] != null){
												frames[idx + line_length[o]].Add(new pos(t2.row,t2.col));
											}
											else{
												frames[idx + line_length[o]] = new List<pos>{new pos(t2.row,t2.col)};
											}
											++idx;
										}
										next.Add(o2);
									}
								}
							}
							current = next;
							next = new List<PhysicalObject>();
						}
						List<pos> frame = frames[0];
						for(int i=0;frame != null;++i){
							foreach(pos p in frame){
								Screen.WriteMapChar(p.row,p.col,'*',Color.RandomLightning);
							}
							Thread.Sleep(50);
							frame = frames[i];
						}
						foreach(Actor a in damage_targets){
							B.Add("The bolt hits " + a.the_name + ". ",a);
							if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
								a.curmp += Spell.Level(spell);
								if(a.curmp > a.maxmp){
									a.curmp = a.maxmp;
								}
								a.GainSpell(spell);
								B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
							}
							a.TakeDamage(DamageType.ELECTRIC,DamageClass.MAGICAL,R.Roll(3+bonus,6),this,a_name);
						}
					}
					else{
						AnimateBeam(line,'*',Color.RandomLightning);
						B.Add("The bolt hits " + t.the_name + ". ",t);
					}
				}
				else{
					return false;
				}
				break;
			/*case SpellType.BURNING_HANDS:
				if(t == null){
					t = TileInDirection(GetDirection());
				}
				if(t != null){
					B.Add(You("cast") + " burning hands. ",this);
					AnimateMapCell(t,Color.DarkRed,'*');
					Actor a = M.actor[t.row,t.col];
					if(a != null){
						B.Add(You("project") + " flames onto " + a.the_name + ". ",this,a);
						a.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,R.Roll(3+bonus,6),this);
						if(M.actor[t.row,t.col] != null && R.Roll(1,10) <= 2){
							B.Add(a.You("start") + " to catch fire! ",a);
							a.attrs[AttrType.CATCHING_FIRE]++;
						}
					}
					else{
						B.Add("You project flames from your hands. ");
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.NIMBUS:
			{
				if(HasAttr(AttrType.NIMBUS_ON)){
					B.Add("You're already surrounded by a nimbus. ");
					return false;
				}
				else{
					B.Add(You("cast") + " nimbus. ",this);
					B.Add("An electric glow surrounds " + the_name + ". ",this);
					attrs[AttrType.NIMBUS_ON]++;
					int duration = (R.Roll(5)+5)*100;
					Q.Add(new Event(this,duration,AttrType.NIMBUS_ON,"The electric glow fades from " + the_name + ". ",this));
				}
				break;
			}
			case SpellType.VOLTAIC_SURGE:
				{
				List<Actor> targets = new List<Actor>();
				foreach(Actor a in ActorsWithinDistance(2,true)){
					if(HasLOE(a)){
						targets.Add(a);
					}
				}
				B.Add(You("cast") + " voltaic surge. ",this);
				AnimateExplosion(this,2,Color.RandomLightning,'*');
				if(targets.Count == 0){
					B.Add("The air around " + the_name + " crackles. ",this);
				}
				else{
					while(targets.Count > 0){
						Actor a = targets.Random();
						targets.Remove(a);
						B.Add("Electricity blasts " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.ELECTRIC,DamageClass.MAGICAL,R.Roll(3+bonus,6),this,a_name);
					}
				}
				break;
				}*/
			case SpellType.MAGIC_HAMMER:
				if(t == null){
					t = TileInDirection(GetDirection());
				}
				if(t != null){
					Actor a = t.actor();
					B.Add(You("cast") + " magic hammer. ",this);
					B.DisplayNow();
					Screen.AnimateMapCell(t.row,t.col,new colorchar('*',Color.Magenta),100);
					if(a != null){
						B.Add(You("smash",true) + " " + a.TheName(true) + ". ",this,a); //todo: compare message to brutish strength's message
						if(a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(4+bonus,6),this,a_name)){
							a.RefreshDuration(AttrType.STUNNED,201,a.YouAre() + " no longer stunned. ",a);
							B.Add(a.YouAre() + " stunned. ",a);
						}
					}
					else{
						B.Add("You smash " + t.the_name + ". ");
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.PORTAL: //player-only for now
			{
				t = tile();
				if(t.Is(FeatureType.INACTIVE_TELEPORTAL,FeatureType.STABLE_TELEPORTAL,FeatureType.TELEPORTAL) || t.Is(TileType.DOOR_O,TileType.STAIRS)){
					B.Add("You can't create a portal here. ");
					return false;
				}
				B.Add("You cast portal. ");
				List<Tile> other_portals = M.AllTiles().Where(x=>x.Is(FeatureType.INACTIVE_TELEPORTAL,FeatureType.STABLE_TELEPORTAL));
				if(other_portals.Count == 0){
					B.Add("You create a dormant portal. "); //todo message? might be okay now.
					t.AddFeature(FeatureType.INACTIVE_TELEPORTAL);
				}
				else{
					if(other_portals.Count == 1){ //it should be inactive in this case
						B.Add("You open a portal. "); //todo message? might be okay now.
						t.AddFeature(FeatureType.STABLE_TELEPORTAL);
						Tile t2 = other_portals[0];
						t2.RemoveFeature(FeatureType.INACTIVE_TELEPORTAL);
						t2.AddFeature(FeatureType.STABLE_TELEPORTAL);
						Q.Add(new Event(t,new List<Tile>{t2},100,EventType.TELEPORTAL,AttrType.NO_ATTR,100,""));
						Q.Add(new Event(t2,new List<Tile>{t},100,EventType.TELEPORTAL,AttrType.NO_ATTR,100,""));
					}
					else{
						B.Add("You open a portal. "); //todo message? might be okay now.
						t.AddFeature(FeatureType.STABLE_TELEPORTAL);
						Q.Add(new Event(t,other_portals,100,EventType.TELEPORTAL,AttrType.NO_ATTR,100,""));
						foreach(Tile t2 in other_portals){
							Event e = Q.FindTargetedEvent(t2,EventType.TELEPORTAL);
							if(e != null){
								e.area.Add(t);
							}
						}
					}
				}
				break;
			}
			case SpellType.PASSAGE:
				{
				int i = DirectionOfOnlyUnblocked(TileType.WALL,true);
				if(i == 0){
					B.Add("There's no wall here. ",this);
					return false;
				}
				else{
					if(t == null){
						i = GetDirection(true,false);
						t = TileInDirection(i);
					}
					else{
						i = DirectionOf(t);
					}
					if(t != null){
						if(t.type == TileType.WALL){
							B.Add(You("cast") + " passage. ",this);
							colorchar ch = new colorchar(Color.Cyan,'!');
							if(this == player){
								Console.CursorVisible = false;
								switch(DirectionOf(t)){
								case 8:
								case 2:
									ch.c = '|';
									break;
								case 4:
								case 6:
									ch.c = '-';
									break;
								}
							}
							List<Tile> tiles = new List<Tile>();
							List<colorchar> memlist = new List<colorchar>();
							while(!t.passable){
								if(t.row == 0 || t.row == ROWS-1 || t.col == 0 || t.col == COLS-1){
									break;
								}
								if(this == player){
									tiles.Add(t);
									memlist.Add(Screen.MapChar(t.row,t.col));
									Screen.WriteMapChar(t.row,t.col,ch);
									Thread.Sleep(35);
								}
								t = t.TileInDirection(i);
							}
							if(t.passable && M.actor[t.row,t.col] == null){
								if(this == player){
									if(M.tile[row,col].inv != null){
										Screen.WriteMapChar(row,col,new colorchar(tile().inv.color,tile().inv.symbol));
									}
									else{
										Screen.WriteMapChar(row,col,new colorchar(tile().color,tile().symbol));
									}
									Screen.WriteMapChar(t.row,t.col,new colorchar(color,symbol));
									int j = 0;
									foreach(Tile tile in tiles){
										Screen.WriteMapChar(tile.row,tile.col,memlist[j++]);
										Thread.Sleep(35);
									}
								}
								Move(t.row,t.col);
								M.Draw();
								B.Add(You("travel") + " through the passage. ",this);
							}
							else{
								if(this == player){
									int j = 0;
									foreach(Tile tile in tiles){
										Screen.WriteMapChar(tile.row,tile.col,memlist[j++]);
										Thread.Sleep(35);
									}
									B.Add("The passage is blocked. ");
								}
							}
						}
						else{
							if(this == player){
								B.Add("There's no wall here. ",this);
							}
							return false;
						}
					}
					else{
						return false;
					}
				}
				break;
			}
			case SpellType.GLACIAL_BLAST:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " glacial blast. ",this);
					Actor a = FirstActorInLine(line);
					if(a != null){
						AnimateProjectile(line.ToFirstObstruction(),'*',Color.RandomIce);
						B.Add("The glacial blast hits " + a.the_name + ". ",a);
						if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
							a.curmp += Spell.Level(spell);
							if(a.curmp > a.maxmp){
								a.curmp = a.maxmp;
							}
							a.GainSpell(spell);
							B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
						}
						if(a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,R.Roll(3+bonus,6),this,a_name)){
							if(type == ActorType.CRYOLICH && !a.HasAttr(AttrType.NONLIVING) && a.exhaustion < 20){
								if(a == player){
									B.Add("The chill of the grave drains you. ");
									if(a.exhaustion == 0){
										B.Add("You feel tired. ");
									}
								}
								a.exhaustion++;
							}
						}
					}
					else{
						AnimateProjectile(line,'*',Color.RandomIce);
						B.Add("The glacial blast hits " + t.the_name + ". ",t);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.AMNESIA:
				if(t == null){
					t = TileInDirection(GetDirection());
				}
				if(t != null){
					Actor a = t.actor();
					if(a != null){
						B.Add(You("cast") + " amnesia. ",this);
						/*for(int i=0;i<4;++i){
							List<pos> cells = new List<pos>();
							List<colorchar> chars = new List<colorchar>();
							List<pos> nearby = a.p.PositionsWithinDistance(2);
							for(int j=0;j<4;++j){
								cells.Add(nearby.RemoveRandom());
								chars.Add(new colorchar('*',Color.RandomPrismatic));
							}
							Screen.AnimateMapCells(cells,chars);
						}*/
						a.AnimateStorm(2,4,4,'*',Color.RandomRainbow);
						B.Add("You fade from " + a.TheName(true) + "'s awareness. ");
						a.player_visibility_duration = 0;
						a.target = null;
						a.target_location = null;
						a.attrs[AttrType.AMNESIA_STUN]++;
					}
					else{
						B.Add("There's nothing to target there. ");
						return false;
					}
				}
				else{
					return false;
				}
				break;
			/*case SpellType.FLASHFIRE:
				if(t == null){
					line = GetTargetTile(12,2,true);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					Actor a = FirstActorInLine(line);
					if(a != null){
						t = a.tile();
					}
					line = line.ToFirstObstruction();
					B.Add(You("cast") + " flashfire. ",this);
					AnimateBoltProjectile(line,Color.Red);
					AnimateExplosion(t,2,'*',Color.RandomFire);
					B.Add("Fwoosh! ",this,t);
					foreach(Tile t2 in line){
						if(t2.Is(FeatureType.TROLL_CORPSE)){
							if(t2.Is(FeatureType.TROLL_CORPSE)){
								t2.features.Remove(FeatureType.TROLL_CORPSE);
								B.Add("The troll corpse burns to ashes! ",t2);
							}
						}
						if(t2.Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){
							if(t2.Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){
								t2.features.Remove(FeatureType.TROLL_BLOODWITCH_CORPSE);
								B.Add("The troll seer corpse burns to ashes! ",t2);
							}
						}
					}
					List<Actor> targets = new List<Actor>();
					Tile prev = line.ToFirstObstruction()[line.ToFirstObstruction().Count-2];
					foreach(Actor ac in t.ActorsWithinDistance(2)){
						if(t.passable){
							if(t.HasBresenhamLine(ac.row,ac.col)){
								targets.Add(ac);
							}
						}
						else{
							if(prev.HasBresenhamLine(ac.row,ac.col)){
								targets.Add(ac);
							}
						}
					}
					foreach(Tile t2 in t.TilesWithinDistance(2)){
						if(t.passable){
							if(t.HasBresenhamLine(t2.row,t2.col)){
								if(t2.actor() != null){
									targets.Add(t2.actor());
								}
								if(t2.Is(FeatureType.TROLL_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_CORPSE);
									B.Add("The troll corpse burns to ashes! ",t2);
								}
								if(t2.Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_BLOODWITCH_CORPSE);
									B.Add("The troll seer corpse burns to ashes! ",t2);
								}
							}
						}
						else{
							if(prev.HasBresenhamLine(t2.row,t2.col)){
								if(t2.actor() != null){
									targets.Add(t2.actor());
								}
								if(t2.Is(FeatureType.TROLL_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_CORPSE);
									B.Add("The troll corpse burns to ashes! ",t2);
								}
								if(t2.Is(FeatureType.TROLL_BLOODWITCH_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_BLOODWITCH_CORPSE);
									B.Add("The troll seer corpse burns to ashes! ",t2);
								}
							}
						}
					}
					while(targets.Count > 0){
						Actor ac = targets.RemoveRandom();
						B.Add("The explosion hits " + ac.the_name + ". ",ac);
						ac.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,R.Roll(3+bonus,6),this,a_name);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.SONIC_BOOM:
				if(t == null){
					line = GetTargetLine(12);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " sonic boom. ",this);
					Actor a = FirstActorInLine(line);
					if(a != null){
						AnimateProjectile(line.ToFirstObstruction(),'~',Color.Yellow);
						B.Add("A wave of sound hits " + a.the_name + ". ",a);
						int r = a.row;
						int c = a.col;
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(3+bonus,6),this,a_name);
						if(R.Roll(1,10) <= 5 && M.actor[r,c] != null && !M.actor[r,c].HasAttr(AttrType.STUNNED)){
							B.Add(a.YouAre() + " stunned. ",a);
							int duration = DurationOfMagicalEffect((R.Roll(4)+2)) * 100;
							a.RefreshDuration(AttrType.STUNNED,duration,a.YouAre() + " no longer stunned. ",a);
						}
					}
					else{
						AnimateProjectile(line,'~',Color.Yellow);
						B.Add("Sonic boom! ");
					}
				}
				else{
					return false;
				}
				break;*/
			case SpellType.SHADOWSIGHT:
				if(!HasAttr(AttrType.SHADOWSIGHT)){ //todo: fix this to use refresh, f'real
					B.Add("You cast shadowsight. ");
					B.Add("Your eyes pierce the darkness. ");
					int duration = 10001;
					GainAttr(AttrType.SHADOWSIGHT,duration,"You no longer see as well in darkness. ");
					GainAttr(AttrType.LOW_LIGHT_VISION,duration);
				}
				else{
					B.Add("Your eyes are already attuned to darkness. ");
					return false;
				}
				break;
			case SpellType.BLIZZARD:
			{
				List<Actor> targets = ActorsWithinDistance(5,true);
				if(type == ActorType.CRYOLICH && curhp <= 0){
					B.Add("The cryolich casts blizzard with its remaining life force. ",this);
				}
				else{
					B.Add(You("cast") + " blizzard. ",this);
				}
				AnimateStorm(5,8,24,'*',Color.RandomIce);
				B.Add("A massive ice storm surrounds " + the_name + ". ",this);
				while(targets.Count > 0){
					int idx = R.Roll(1,targets.Count) - 1;
					Actor a = targets[idx];
					targets.Remove(a);
					B.Add("The blizzard hits " + a.the_name + ". ",a); //todo: remove this message?
					if(a.type == ActorType.ALASI_BATTLEMAGE && !a.HasSpell(spell)){
						a.curmp += Spell.Level(spell);
						if(a.curmp > a.maxmp){
							a.curmp = a.maxmp;
						}
						a.GainSpell(spell);
						B.Add("Runes on " + a.Your() + " armor align themselves with the spell. ",a);
					}
					if(a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,R.Roll(5+bonus,6),this,a_name)){
						//if(DistanceFrom(a) == 1){ //changed to always freeze
							B.Add(a.YouAre() + " encased in ice. ",a);
							a.attrs[AttrType.FROZEN] = 35;
						/*}
						else{
							B.Add(a.YouAre() + " slowed. ",a);
							a.RefreshDuration(AttrType.SLOWED,R.Between(6,10)*100,a.YouAre() + " no longer slowed. ",a);
						}*/
						if(type == ActorType.CRYOLICH && !a.HasAttr(AttrType.NONLIVING) && a.exhaustion < 20){
							if(a == player){
								B.Add("The chill of the grave drains you. ");
								if(a.exhaustion == 0){
									B.Add("You feel tired. ");
								}
							}
							a.exhaustion++;
						}
					}
				}
				break;
			}
			/*case SpellType.FORCE_BEAM:
				if(t == null){
					line = GetTargetLine(-1);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " force beam. ",this);
					B.DisplayNow();
					//List<Tile> line2 = GetBestExtendedLine(t.row,t.col);
					List<Tile> full_line = new List<Tile>(line);
					line = line.GetRange(0,Math.Min(13,line.Count));
					for(int i=0;i<3;++i){ //hits thrice
						Actor firstactor = null;
						Actor nextactor = null;
						Tile firsttile = null;
						Tile nexttile = null;
						foreach(Tile tile in line){
							if(!tile.passable){
								firsttile = tile;
								break;
							}
							if(M.actor[tile.row,tile.col] != null && M.actor[tile.row,tile.col] != this){
								int idx = full_line.IndexOf(tile);
								firsttile = tile;
								firstactor = M.actor[tile.row,tile.col];
								nexttile = full_line[idx+1];
								nextactor = M.actor[nexttile.row,nexttile.col];
								break;
							}
						}
						AnimateBoltBeam(line.ToFirstObstruction(),Color.Cyan);
						if(firstactor != null){
							string s = firstactor.TheName(true);
							string s2 = firstactor.a_name;
							//Actor corpse = firstactor.GetCorpse();
							firstactor.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(1+bonus,6),this,a_name);
							if(M.actor[firsttile.row,firsttile.col] != null){
								KnockObjectBack(firstactor,3);
								//firstactor.GetKnockedBack(full_line);
							}
							else{
								if(!nexttile.passable){
									B.Add(s + "'s corpse is knocked into " + nexttile.the_name + ". ",firsttile,nexttile);
								}
								else{
									if(nextactor != null){
										B.Add(s + "'s corpse is knocked into " + nextactor.TheName(true) + ". ",firsttile,nextactor);
										nextactor.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(1,6),this,s2 + "'s falling corpse");
									}
								}
							}
						}
					}
				}
				else{
					return false;
				}
				break;*/
			case SpellType.FIRE_BLITZ:
				//todo
				break;
			case SpellType.COLLAPSE:
				if(t == null){
					line = GetTargetTile(12,0,false);
					if(line != null && line.Last() != tile()){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " collapse. ",this);
					B.DisplayNow();
					for(int dist=2;dist>0;--dist){
						List<pos> cells = new List<pos>();
						List<colorchar> chars = new List<colorchar>();
						pos p2 = new pos(t.row-dist,t.col-dist);
						if(p2.BoundsCheck()){
							cells.Add(p2);
							chars.Add(new colorchar('\\',Color.DarkGreen));
						}
						p2 = new pos(t.row-dist,t.col+dist);
						if(p2.BoundsCheck()){
							cells.Add(p2);
							chars.Add(new colorchar('/',Color.DarkGreen));
						}
						p2 = new pos(t.row+dist,t.col-dist);
						if(p2.BoundsCheck()){
							cells.Add(p2);
							chars.Add(new colorchar('/',Color.DarkGreen));
						}
						p2 = new pos(t.row+dist,t.col+dist);
						if(p2.BoundsCheck()){
							cells.Add(p2);
							chars.Add(new colorchar('\\',Color.DarkGreen));
						}
						Screen.AnimateMapCells(cells,chars);
					}
					Screen.AnimateMapCell(t.row,t.col,new colorchar('X',Color.DarkGreen));
					Actor a = t.actor();
					if(a != null){
						B.Add("Part of the ceiling falls onto " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(4+bonus,6),this,a_name);
					}
					else{
						if(t.row == 0 || t.col == 0 || t.row == ROWS-1 || t.col == COLS-1){
							B.Add("The wall resists. ");
						}
						else{
							if(t.type == TileType.WALL || t.type == TileType.HIDDEN_DOOR){
								B.Add("The wall crashes down! ");
								t.TurnToFloor();
								foreach(Tile neighbor in t.TilesAtDistance(1)){
									if(neighbor.solid_rock){
										neighbor.solid_rock = false;
									}
								}
							}
						}
					}
					List<Tile> open_spaces = new List<Tile>();
					foreach(Tile neighbor in t.TilesWithinDistance(1)){
						if(neighbor.passable){
							if(a == null || neighbor != t){ //don't hit the same guy again
								open_spaces.Add(neighbor);
							}
						}
					}
					int count = 4;
					if(open_spaces.Count < 4){
						count = open_spaces.Count;
					}
					for(;count>0;--count){
						Tile chosen = open_spaces.Random();
						open_spaces.Remove(chosen);
						if(chosen.actor() != null){
							B.Add("A rock falls onto " + chosen.actor().the_name + ". ",chosen.actor());
							chosen.actor().TakeDamage(DamageType.NORMAL,Forays.DamageClass.PHYSICAL,R.Roll(2,6),this,a_name);
						}
						else{
							TileType prev = chosen.type;
							chosen.TransformTo(TileType.RUBBLE);
							chosen.toggles_into = prev;
						}
					}
				}
				else{
					return false;
				}
				break;
			/*case SpellType.DISINTEGRATE:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " disintegrate. ",this);
					Actor a = FirstActorInLine(t);
					if(a != null){
						AnimateBoltBeam(a,Color.DarkGreen);
						B.Add(You("direct") + " destructive energies toward " + a.the_name + ". ",this,a);
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(8+bonus,6),this);
					}
					else{
						AnimateBoltBeam(t,Color.DarkGreen);
						if(t.type == TileType.WALL || t.type == TileType.DOOR_C || t.type == TileType.DOOR_O || t.type == TileType.CHEST){
							B.Add(You("direct") + " destructive energies toward " + t.the_name + ". ",this,t);
							B.Add(t.the_name + " turns to dust. ",t);
							t.TurnToFloor();
						}
					}
				}
				else{
					return false;
				}
				break;*/
			case SpellType.PLACEHOLDER:
				//todo
				break; //and make sure battlemage check is here, if this is a damaging spell.
			}
			if(curmp >= required_mana){
				curmp -= required_mana;
			}
			else{
				IncreaseExhaustion(required_mana - curmp);
				curmp = 0;
			}
			if(HasFeat(FeatType.ARCANE_INTERFERENCE)){
				foreach(Actor a in ActorsWithinDistance(12,true)){
					if(a.HasSpell(spell) && HasLOE(a)){
						B.Add("todo: " + a.the_name + " can't cast this spell any more. ",a); //todo! needs a message.
						a.spells[spell] = false;
					}
				}
			}
			if(HasFeat(FeatType.CHAIN_CASTING)){
				RefreshDuration(AttrType.CHAIN_CAST,100);
			}
			MakeNoise(4);
			Q1();
			return true;
		}
		public bool CastRandomSpell(PhysicalObject obj,params SpellType[] spells){
			if(spells.Length == 0){
				return false;
			}
			return CastSpell(spells[R.Roll(1,spells.Length)-1],obj);
		}
		public Color FailColor(int failrate){
			Color failcolor = Color.White;
			if(failrate > 50){
				failcolor = Color.DarkRed;
			}
			else{
				if(failrate > 20){
					failcolor = Color.Red;
				}
				else{
					if(failrate > 0){
						failcolor = Color.Yellow;
					}
				}
			}
			return failcolor;
		}
		public void ResetForNewLevel(){
			target = null;
			target_location = null;
			if(HasAttr(AttrType.DIM_LIGHT)){
				attrs[AttrType.DIM_LIGHT] = 0;
				if(light_radius > 0){
					if(HasAttr(AttrType.SHINING)){
						light_radius = 12;
					}
					else{
						light_radius = 6;
					}
				}
			}
			if(attrs[AttrType.RESTING] == -1){
				attrs[AttrType.RESTING] = 0;
			}
			if(HasAttr(AttrType.GRABBED)){
				attrs[AttrType.GRABBED] = 0;
			}
			Q.KillEvents(null,EventType.CHECK_FOR_HIDDEN);
		}
		public bool UseFeat(FeatType feat){
			switch(feat){
			case FeatType.LUNGE:
			{
				List<Tile> line = GetTargetTile(2,0,true);
				Tile t = null;
				if(line != null && line.Last() != tile()){
					t = line.Last();
				}
				if(t != null && t.actor() != null){
					bool moved = false;
					if(DistanceFrom(t) == 2 && line[1].passable && line[1].actor() == null && !GrabPreventsMovement(line[1])){
						moved = true;
						B.Add("You lunge! ");
						Move(line[1].row,line[1].col);
						attrs[AttrType.LUNGING_AUTO_HIT] = 1;
						Attack(0,t.actor());
						attrs[AttrType.LUNGING_AUTO_HIT] = 0;
					}
					if(!moved){
						if(GrabPreventsMovement(line[1])){
							B.Add("You can't currently reach that spot. ");
							return false;
						}
						else{
							B.Add("The way is blocked! ");
							return false;
						}
					}
					else{
						return true;
					}
				}
				else{
					return false;
				}
				//break;
			}
			case FeatType.TUMBLE:
			{
				target = null; //don't try to automatically pick previous targets while tumbling. this solution isn't ideal.
				List<Tile> line = GetTargetTile(2,0,false);
				target = null; //then, don't remember an actor picked as the target of tumble
				Tile t = null;
				if(line != null && line.Last() != tile()){
					t = line.Last();
				}
				if(t != null && t.passable && t.actor() == null && !GrabPreventsMovement(t)){
					bool moved = false;
					foreach(Tile neighbor in t.NonOpaqueNeighborsBetween(row,col)){
						if(neighbor.passable && !moved){
							B.Add("You tumble. ");
							Move(t.row,t.col);
							moved = true;
							attrs[AttrType.TUMBLING]++;
						}
					}
					if(moved){
						Q.Add(new Event(this,Speed() + 100,EventType.MOVE));
						return true;
					}
					else{
						B.Add("The way is blocked! ");
						return false;
					}
				}
				else{
					if(GrabPreventsMovement(t)){
						B.Add("You can't currently reach that spot. ");
					}
					return false;
				}
			}
			case FeatType.DISARM_TRAP:
			{
				int dir = GetDirection("Disarm which trap? ");
				Tile t = TileInDirection(dir);
				if(dir != -1 && t.IsKnownTrap()){
					if(ActorInDirection(dir) != null){
						B.Add("There is " + ActorInDirection(dir).AName(true) + " in the way. ");
					}
					else{
						if(GrabPreventsMovement(t)){
							B.Add("You can't currently reach that trap. ");
							Q0();
							return true;
						}
						B.Add("You disarm " + Tile.Prototype(t.type).the_name + ". ");
						Item i = null;
						if(inv.Count >= Global.MAX_INVENTORY_SIZE){
							i = Item.Create(ConsumableType.TRAP,t.row,t.col);
						}
						else{
							i = Item.Create(ConsumableType.TRAP,this);
						}
						if(i == null){
							foreach(Tile next in M.ReachableTilesByDistance(t.row,t.col,false)){
								if(next.Is(TileType.FLOOR)){
									i = Item.Create(ConsumableType.TRAP,next.row,next.col);
									if(i != null){
										break;
									}
								}
							}
						}
						if(i != null){
							i.other_data = (int)t.type;
							i.SetName("disarmed " + t.name);
							i.color = t.color;
						}
						t.Toggle(this);
						Q1();
					}
				}
				else{
					Q0();
				}
				return true;
			}
			default:
				return false;
			}
			Q1();
			return true;
		}
		public void Interrupt(){
			if(HasAttr(AttrType.RESTING)){
				attrs[AttrType.RESTING] = 0;
			}
			attrs[AttrType.RUNNING] = 0;
			attrs[AttrType.WAITING] = 0;
			attrs[AttrType.AUTOEXPLORE] = 0;
			if(path != null && path.Count > 0){
				path.Clear();
			}
		}
		public bool CollideWith(Tile t){
			B.Add(You("collide") + " with " + t.the_name + ". "); //todo
			return true; //todo: return value should indicate whether it's still alive
		}
		public bool StunnedThisTurn(){
			if(HasAttr(AttrType.STUNNED) && R.OneIn(3)){
				if(HasAttr(AttrType.IMMOBILE)){
					QS();
					return true;
				}
				string verb = "stagger";
				bool es = false;
				if(HasAttr(AttrType.FLYING)){
					verb = "careen";
				}
				else{
					if(Is(ActorType.SPITTING_COBRA,ActorType.MIMIC,ActorType.GIANT_SLUG,ActorType.INFESTED_MASS_TODO_NAME,ActorType.NOXIOUS_WORM)){
						verb = "lurch";
						es = true;
					}
				}
				Tile t = null;
				if(type == ActorType.PHASE_SPIDER){
					if(target != null){
						t = target.TilesWithinDistance(DistanceFrom(target)+1).Where(x=>x.DistanceFrom(target) >= DistanceFrom(target)-1).Random();
					}
				}
				else{
					t = TileInDirection(Global.RandomDirection());
				}
				if(t != null){
					Actor a = t.actor();
					if(!t.passable){
						B.Add(You(verb,es) + " into " + t.the_name + ". ",this);
					}
					else{
						if(a != null){
							B.Add(YouVisible(verb,es) + " into " + a.TheName(true) + ". ",this,a);
						}
						else{
							if(GrabPreventsMovement(t)){
								if(type == ActorType.PLAYER){
									B.Add(You(verb,es) + " and almost fall over. ",this);
								}
								else{
									B.Add(You(verb,es) + " and almost falls over. ",this);
								}
							}
							else{
								B.Add(You(verb,es) + ". ",this);
								Move(t.row,t.col);
							}
						}
					}
				}
				QS();
				return true;
			}
			return false;
		}
		public List<string> InventoryList(){
			List<string> result = new List<string>();
			foreach(Item i in inv){
				result.Add(i.AName());
			}
			return result;
		}
		public static List<colorstring> ItemDescriptionBox(Item item,bool lookmode,int max_string_length){
			List<string> text = item.Description().GetWordWrappedList(max_string_length);
			Color box_edge_color = Color.DarkGreen;
			Color box_corner_color = Color.Green;
			Color text_color = Color.Gray;
			int widest = 31; // length of "[Press any other key to cancel]"
			if(lookmode){
				widest = 20; // length of "[=] Hide description"
			}
			foreach(string s in text){
				if(s.Length > widest){
					widest = s.Length;
				}
			}
			if(!lookmode && item.Name(true).Length > widest){
				widest = item.Name(true).Length;
			}
			widest += 2; //one space on each side
			List<colorstring> box = new List<colorstring>();
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			if(!lookmode){
				box.Add(new colorstring("|",box_edge_color) + item.Name(true).PadOuter(widest).GetColorString(Color.White) + new colorstring("|",box_edge_color));
				box.Add(new colorstring("|",box_edge_color,"".PadRight(widest),Color.Gray,"|",box_edge_color));
			}
			foreach(string s in text){
				box.Add(new colorstring("|",box_edge_color) + s.PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
			}
			box.Add(new colorstring("|",box_edge_color,"".PadRight(widest),Color.Gray,"|",box_edge_color));
			if(lookmode){
				box.Add(new colorstring("|",box_edge_color) + "[=] Hide description".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
			}
			else{
				box.Add(new colorstring("|",box_edge_color) + "[a]pply  [f]ling  [d]rop".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
				//box.Add(new colorstring("|",box_edge_color) + "[Press any other key to cancel]".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
			}
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			return box;
		}
		public void DisplayStats(){ DisplayStats(false); }
		public void DisplayStats(bool cyan_letters){
			Console.CursorVisible = false;
			Screen.WriteStatsString(2,0,"HP: ");
			if(curhp < 50){
				if(curhp < 20){
					Screen.WriteStatsString(2,4,new cstr(Color.DarkRed,curhp.ToString() + "  "));
				}
				else{
					Screen.WriteStatsString(2,4,new cstr(Color.Red,curhp.ToString() + "  "));
				}
			}
			else{
				Screen.WriteStatsString(2,4,curhp.ToString() + "  ");
			}
			Screen.WriteStatsString(3,0,"Mana: ");
			Screen.WriteStatsString(3,6,curmp + "  ");
			Screen.WriteStatsString(4,0,"Exhaust:");
			if(exhaustion == 20){
				Screen.WriteStatsString(4,8,"100%");
			}
			else{
				Screen.WriteStatsString(4,8," " + ((exhaustion*5).ToString() + "%").PadRight(4));
			}
			Screen.WriteStatsString(5,0,"Depth: " + M.current_level + "  ");
			int magic_item_lines = magic_trinkets.Count;
			cstr cs = EquippedWeapon.StatsName();
			cs.s = cs.s.PadRight(12);
			Screen.WriteStatsString(7,0,cs);
			cs = EquippedArmor.StatsName();
			cs.s = cs.s.PadRight(12);
			Screen.WriteStatsString(8,0,cs);
			int line = 9;
			foreach(MagicTrinketType m in magic_trinkets){
				cs = MagicTrinket.StatsName(m);
				cs.s = cs.s.PadRight(12);
				Screen.WriteStatsString(line,0,cs);
				++line;
			}
			if(!Global.Option(OptionType.HIDE_COMMANDS)){ //todo: remove this option if there's nothing to replace it with
/*[i]nventory
[e]quipment
[c]haracter
[t]orch off
Cast [z]
[s]hoot			here is the full list, to be completed when there's enough room.
[a]pply item
[g]et item
[d]rop item     missing only drop, now. I don't think it really needs a spot.
[r]est
[w]alk
E[x]plore
[o]perate
[tab] Look
[f]ling
*/
				for(int i=9+magic_item_lines;i<11;++i){
					Screen.WriteStatsString(i,0,"".PadRight(12));
				}
				/*string[] commandhints = new string[]{"[i]nventory ","[e]quipment ","[c]haracter ","SPECIAL",
					"Use [f]eat  ","Cast [z]    ","[s]hoot(bow)","[Tab] Look  ","[a]pply item","[f]ling item",
					"[g]et item  ","[r]est      ","[w]alk      ","E[x]plore   ","[o]perate   "};*/
				string[] commandhints = new string[]{"[i]nventory ","[e]quipment ","[c]haracter ","SPECIAL",
					"[Tab] Look  ","[r]est      ","[s]hoot bow ","Cast [z]    ","[a]pply item","[g]et item  ",
					"[f]ling item","[o]perate   ","[w]alk      ","E[x]plore   "};
				if(light_radius > 0){
					commandhints[3] = "[t]orch off "; //todo: should this part stay or go?
				}
				else{
					commandhints[3] = "[t]orch on  ";
				}
				Color lettercolor = cyan_letters? Color.Cyan : Color.DarkCyan;
				Color wordcolor = cyan_letters? Color.Gray : Color.DarkGray;
				for(int i=0;i<commandhints.Length;++i){
					int open = commandhints[i].LastIndexOf('[');
					cstr front = new cstr(commandhints[i].Substring(0,open+1),wordcolor); //todo: replace this part with "".GetColorString()
					int close = commandhints[i].LastIndexOf(']');
					cstr middle = new cstr(commandhints[i].Substring(open+1,(close - open)-1),lettercolor);
					cstr end = new cstr(commandhints[i].Substring(close),wordcolor);
					Screen.WriteString(11+i,0,new colorstring(front,middle,end));
				}
			}
			else{
				for(int i=7+magic_item_lines;i<Global.SCREEN_H;++i){
					Screen.WriteStatsString(i,0,"".PadRight(12));
				}
			}
			Screen.ResetColors();
		}
		public int DisplayCharacterInfo(){ return DisplayCharacterInfo(true); }
		public int DisplayCharacterInfo(bool readkey){
			DisplayStats();
			for(int i=1;i<ROWS-1;++i){
				Screen.WriteMapString(i,0,"".PadRight(COLS));
			}
			Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
			Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
			Color catcolor = Color.Green;
			string s = ("Name: " + player_name).PadRight(COLS/2) + "Turns played: " + (Q.turn / 100);
			Screen.WriteMapString(2,0,s);
			Screen.WriteMapString(2,0,new cstr(catcolor,"Name"));
			Screen.WriteMapString(2,COLS/2,new cstr(catcolor,"Turns played"));
			//todo trait removed
			Screen.WriteMapString(8,0,"Skills:");
			Screen.WriteMapString(8,0,new cstr(catcolor,"Skills"));
			int pos = 7;
			for(SkillType sk = SkillType.COMBAT;sk < SkillType.NUM_SKILLS;++sk){
				if(sk == SkillType.STEALTH && pos > 50){
					Screen.WriteMapString(9,8,"Stealth(" + skills[SkillType.STEALTH].ToString());
					pos = 16 + skills[SkillType.STEALTH].ToString().Length;
					if(HasAttr(AttrType.BONUS_STEALTH)){
						Screen.WriteMapString(9,pos,new cstr(Color.Yellow,"+" + attrs[AttrType.BONUS_STEALTH].ToString()));
						pos += attrs[AttrType.BONUS_STEALTH].ToString().Length + 1;
					}
					Screen.WriteMapChar(9,pos,')');
				}
				else{
					Screen.WriteMapString(8,pos," " + Skill.Name(sk));
					pos += Skill.Name(sk).Length + 1;
					string count1 = skills[sk].ToString();
					string count2;
					switch(sk){
					case SkillType.COMBAT:
						count2 = attrs[AttrType.BONUS_COMBAT].ToString();
						break;
					case SkillType.DEFENSE:
						count2 = attrs[AttrType.BONUS_DEFENSE].ToString();
						break;
					case SkillType.MAGIC:
						count2 = attrs[AttrType.BONUS_MAGIC].ToString();
						break;
					case SkillType.SPIRIT:
						count2 = attrs[AttrType.BONUS_SPIRIT].ToString();
						break;
					case SkillType.STEALTH:
						count2 = attrs[AttrType.BONUS_STEALTH].ToString();
						break;
					default:
						count2 = "error";
						break;
					}
					Screen.WriteMapString(8,pos,"(" + count1);
					pos += count1.Length + 1;
					if(count2 != "0"){
						Screen.WriteMapString(8,pos,new cstr(Color.Yellow,"+" + count2));
						pos += count2.Length + 1;
					}
					Screen.WriteMapChar(8,pos,')');
					pos++;
				}
			}
			Screen.WriteMapString(11,0,"Feats: ");
			Screen.WriteMapString(11,0,new cstr(catcolor,"Feats"));
			string featlist = "";
			int active_feat_count = 0;
			foreach(FeatType f in feats_in_order){
				if(featlist.Length > 0){
					featlist = featlist + ", ";
				}
				if(Feat.IsActivated(f)){
					featlist = featlist + "[" + (char)(active_feat_count + 'a') + "] " + Feat.Name(f);
					++active_feat_count;
				}
				else{
					featlist = featlist + Feat.Name(f);
				}
			}
			int currentrow = 11;
			while(featlist.Length > COLS-7){
				int currentcol = COLS-8;
				while(featlist[currentcol] != ','){
					--currentcol;
				}
				Screen.WriteMapString(currentrow,7,featlist.Substring(0,currentcol+1).GetColorString());
				featlist = featlist.Substring(currentcol+2);
				++currentrow;
			}
			Screen.WriteMapString(currentrow,7,featlist.GetColorString());
			Screen.WriteMapString(14,0,"Spells: ");
			Screen.WriteMapString(14,0,new cstr(catcolor,"Spells"));
			string spelllist = "";
			for(SpellType sp = SpellType.SHINE;sp < SpellType.NUM_SPELLS;++sp){
				if(HasSpell(sp)){
					if(spelllist.Length == 0){ //if this is the first one...
						spelllist = spelllist + Spell.Name(sp);
					}
					else{
						spelllist = spelllist + ", " + Spell.Name(sp);
					}
				}
			}
			currentrow = 14;
			while(spelllist.Length > COLS-8){
				int currentcol = COLS-9;
				while(spelllist[currentcol] != ','){
					--currentcol;
				}
				Screen.WriteMapString(currentrow,8,spelllist.Substring(0,currentcol+1));
				spelllist = spelllist.Substring(currentcol+2);
				++currentrow;
			}
			Screen.WriteMapString(currentrow,8,spelllist);
			/*Screen.WriteMapString(11,7,"Armor mastery, [a] Tumble, Student's luck, Neck snap,".GetColorString()); example of active feats in the character screen
			Screen.WriteMapString(12,8,"[b] Disarm trap".GetColorString());*/
			Screen.ResetColors();
			B.DisplayNow("Character information: ");
			Console.CursorVisible = true;
			int num_active_feats = 0;
			foreach(FeatType feat in Enum.GetValues(typeof(FeatType))){
				if(HasFeat(feat) && Feat.IsActivated(feat)){
					++num_active_feats;
				}
			}
			if(readkey){
				return GetSelection("Character information: ",num_active_feats,false,true,false);
				//Console.ReadKey(true);
			}
			else{
				return -1;
			}
		}
		public int[] DisplayEquipment(){
			WeaponType new_weapon_type = EquippedWeapon.type;
			ArmorType new_armor_type = EquippedArmor.type;
			int selected_magic_trinket_idx = -1;
			if(magic_trinkets.Count > 0){
				selected_magic_trinket_idx = R.Roll(magic_trinkets.Count)-1;
			}
			Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
			for(int i=1;i<ROWS-1;++i){
				Screen.WriteMapString(i,0,"".PadRight(COLS));
			}
			int line = 1;
			for(WeaponType w = WeaponType.SWORD;w <= WeaponType.BOW;++w){
				Screen.WriteMapString(line,6,WeaponOfType(w).EquipmentScreenName());
				++line;
			}
			line = 8;
			for(ArmorType a = ArmorType.LEATHER;a <= ArmorType.FULL_PLATE;++a){
				Screen.WriteMapString(line,6,ArmorOfType(a).EquipmentScreenName());
				++line;
			}
			line = 1;
			foreach(MagicTrinketType m in magic_trinkets){
				string s = MagicTrinket.Name(m);
				Screen.WriteMapString(line,38,s[0].ToString().ToUpper() + s.Substring(1));
				++line;
			}
			Screen.WriteMapString(12,1,new cstr(Color.DarkRed,"Weapon: "));
			Screen.WriteMapChar(12,7,':');
			Screen.WriteMapString(15,1,new cstr(Color.DarkCyan,"Armor: "));
			Screen.WriteMapChar(15,6,':');
			Screen.WriteMapString(18,1,new cstr(Color.DarkGreen,"Magic trinket: "));
			Screen.WriteMapChar(18,14,':');
			ConsoleKeyInfo command;
			bool done = false;
			while(!done){
				Weapon new_weapon = WeaponOfType(new_weapon_type);
				Armor new_armor = ArmorOfType(new_armor_type);
				line = 1;
				for(WeaponType w = WeaponType.SWORD;w <= WeaponType.BOW;++w){
					if(new_weapon_type == w){
						Screen.WriteMapChar(line,0,'>');
						Screen.WriteMapString(line,2,new cstr(Color.Red,"[" + (char)(w+(int)'a') + "]"));
					}
					else{
						Screen.WriteMapChar(line,0,' ');
						Screen.WriteMapString(line,2,new cstr(Color.Cyan,"[" + (char)(w+(int)'a') + "]"));
					}
					++line;
				}
				line = 8;
				for(ArmorType a = ArmorType.LEATHER;a <= ArmorType.FULL_PLATE;++a){
					if(new_armor_type == a){
						Screen.WriteMapChar(line,0,'>');
						Screen.WriteMapString(line,2,new cstr(Color.Red,"[" + (char)(a+(int)'f') + "]"));
					}
					else{
						Screen.WriteMapChar(line,0,' ');
						Screen.WriteMapString(line,2,new cstr(Color.Cyan,"[" + (char)(a+(int)'f') + "]"));
					}
					++line;
				}
				line = 1;
				int letter = 0;
				foreach(MagicTrinketType m in magic_trinkets){
					if(selected_magic_trinket_idx == magic_trinkets.IndexOf(m)){
						Screen.WriteMapChar(line,32,'>');
					}
					else{
						Screen.WriteMapChar(line,32,' ');
					}
					Screen.WriteMapString(line,34,new cstr(Color.Red,"[" + (char)(letter+(int)'i') + "]"));
					++line;
					++letter;
				}
				Screen.WriteMapString(12,9,new_weapon.Description().PadRight(COLS));
				colorstring weaponstatus = new colorstring();
				colorstring armorstatus = new colorstring();
				for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
					EquipmentStatus st = (EquipmentStatus)i;
					if(new_weapon.status[st]){
						weaponstatus.strings.Add(new cstr(Weapon.StatusName(st) + "  ",Weapon.StatusColor(st)));
					}
					if(new_armor.status[st]){
						armorstatus.strings.Add(new cstr(Weapon.StatusName(st) + "  ",Weapon.StatusColor(st)));
					}
				}
				if(weaponstatus.Length() < COLS){
					weaponstatus.strings.Add(new cstr("".PadRight(COLS-weaponstatus.Length()),Color.Gray));
				}
				if(armorstatus.Length() < COLS){
					armorstatus.strings.Add(new cstr("".PadRight(COLS-armorstatus.Length()),Color.Gray));
				}
				if(new_weapon.enchantment != EnchantmentType.NO_ENCHANTMENT){
					Screen.WriteMapString(13,9,new_weapon.DescriptionOfEnchantment().PadRight(COLS));
					Screen.WriteMapString(14,4,weaponstatus);
				}
				else{
					Screen.WriteMapString(13,4,weaponstatus);
				}
				Screen.WriteMapString(15,8,new_armor.Description().PadRight(COLS));
				if(new_armor.enchantment != EnchantmentType.NO_ENCHANTMENT){
					Screen.WriteMapString(16,8,new_armor.DescriptionOfEnchantment().PadRight(COLS));
					Screen.WriteMapString(17,4,armorstatus);
				}
				else{
					Screen.WriteMapString(16,4,armorstatus);
				}
				if(selected_magic_trinket_idx >= 0){
					string[] magic_item_desc = MagicTrinket.Description(magic_trinkets[selected_magic_trinket_idx]);
					Screen.WriteMapString(18,16,magic_item_desc[0].PadRight(50));
					Screen.WriteMapString(19,16,magic_item_desc[1].PadRight(50)); //todo: re-align this part?
					Screen.WriteMapString(20,16,magic_item_desc[2].PadRight(50));
				}
				if(new_weapon == EquippedWeapon && new_armor == EquippedArmor){
					Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
				}
				else{
					Screen.WriteMapString(ROWS-1,0,"[Enter] to confirm-----".PadLeft(43,'-'));
					Screen.WriteMapString(ROWS-1,21,new cstr(Color.Magenta,"Enter"));
				}
				Screen.ResetColors();
				B.DisplayNow("Your equipment: ");
				Console.CursorVisible = true;
				command = Console.ReadKey(true);
				char ch = ConvertInput(command);
				switch(ch){
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
				case '!':
				case '@':
				case '#':
				case '$':
				case '%':
				{
					switch(ch){
					case '!':
						ch = 'a';
						break;
					case '@':
						ch = 'b';
						break;
					case '#':
						ch = 'c';
						break;
					case '$':
						ch = 'd';
						break;
					case '%':
						ch = 'e';
						break;
					}
					if((int)ch - (int)'a' != (int)(new_weapon_type)){
						new_weapon_type = (WeaponType)((int)ch - (int)'a');
					}
					break;
				}
				case 'f':
				case 'g':
				case 'h':
				case '*':
				case '(':
				case ')':
					switch(ch){
					case '*':
						ch = 'f';
						break;
					case '(':
						ch = 'g';
						break;
					case ')':
						ch = 'h';
						break;
					}
					if((int)ch - (int)'f' != (int)(new_armor_type)){
						new_armor_type = (ArmorType)((int)ch - (int)'f');
					}
					break;
				case 'i':
				case 'j':
				case 'k':
				case 'l':
				case 'm':
				case 'n':
				case 'o':
				case 'p':
				case 'q':
				case 'r':
				{
					int num = (int)ch - (int)'i';
					if(num < magic_trinkets.Count && num != selected_magic_trinket_idx){
						selected_magic_trinket_idx = num;
					}
					break;
				}
				case (char)27:
				case ' ':
					new_weapon_type = EquippedWeapon.type; //reset
					new_armor_type = EquippedArmor.type;
					done = true;
					break;
				case (char)13:
					done = true;
					break;
				default:
					break;
				}
			}
			return new int[]{(int)new_weapon_type,(int)new_armor_type};
		}
		public void IncreaseSkill(SkillType skill){
			List<string> learned = new List<string>();
			skills[skill]++;
			B.Add("You feel a rush of power. ");
			//DisplayStats();
			B.PrintAll();
			ConsoleKeyInfo command;
			if(skills[skill] == 1 || skills[skill] == 6){
				FeatType feat_chosen = FeatType.NO_FEAT;
				bool done = false;
				while(!done){
					Screen.ResetColors();
					Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
					for(int i=0;i<4;++i){
						FeatType ft = Feat.OfSkill(skill,i);
						Color featcolor = (feat_chosen == ft)? Color.Green : Color.Gray;
						Color lettercolor = Color.Cyan;
						if(HasFeat(ft)){
							featcolor = Color.Magenta;
							lettercolor = Color.DarkRed;
						}
						Screen.WriteMapString(1+i*5,0,("["+(char)(i+97)+"] "));
						Screen.WriteMapChar(1+i*5,1,(char)(i+97),lettercolor);
						Screen.WriteMapString(1+i*5,4,Feat.Name(ft).PadRight(30),featcolor);
						if(Feat.IsActivated(ft)){
							Screen.WriteMapString(1+i*5,30,"        Active".PadToMapSize());
						}
						else{
							Screen.WriteMapString(1+i*5,30,"        Passive".PadToMapSize());
						}
						List<string> desc = Feat.Description(ft);
						for(int j=0;j<4;++j){
							if(desc.Count > j){
								Screen.WriteMapString(2+j+i*5,0,"    " + desc[j].PadRight(64));
							}
							else{
								Screen.WriteMapString(2+j+i*5,0,"".PadRight(66));
							}
						}
					}
					if(feat_chosen != FeatType.NO_FEAT){
						Screen.WriteMapString(21,0,"--Type [a-d] to choose a feat---[?] for help---[Enter] to accept--");
						Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
						Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'d'));
						Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
						Screen.WriteMapString(21,48,new cstr(Color.Magenta,"Enter"));
					}
					else{
						Screen.WriteMapString(21,0,"--Type [a-d] to choose a feat---[?] for help----------------------");
						Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
						Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'d'));
						Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
					}
					B.DisplayNow("Your " + Skill.Name(skill) + " skill increases to " + skills[skill] + ". Choose a feat: ");
					if(!Help.displayed[TutorialTopic.Feats]){
						Help.TutorialTip(TutorialTopic.Feats);
						B.DisplayNow("Your " + Skill.Name(skill) + " skill increases to " + skills[skill] + ". Choose a feat: ");
					}
					Console.CursorVisible = true;
					command = Console.ReadKey(true);
					Console.CursorVisible = false;
					char ch = ConvertInput(command);
					switch(ch){
					case 'a':
					case 'b':
					case 'c':
					case 'd':
					{
						FeatType ft = Feat.OfSkill(skill,(int)(ch-97));
						if(feat_chosen == ft){
							feat_chosen = FeatType.NO_FEAT;
						}
						else{
							if(!HasFeat(ft)){
								feat_chosen = ft;
							}
						}
						break;
					}
					case '?':
						Help.DisplayHelp(HelpTopic.Feats);
						DisplayStats();
						break;
					case (char)13:
						if(feat_chosen != FeatType.NO_FEAT){
							done = true;
						}
						break;
					default:
						break;
					}
				}
				feats[feat_chosen] = true;
				feats_in_order.Add(feat_chosen);
				learned.Add("You master the " + Feat.Name(feat_chosen) + " feat. ");
			}
			else{
				learned.Add("Your " + Skill.Name(skill) + " skill increases to " + skills[skill] + ". ");
			}
			if(skill == SkillType.MAGIC){
				maxmp += 5;
				curmp += 5;
				List<SpellType> unknown = new List<SpellType>();
				List<colorstring> unknownstr = new List<colorstring>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(!HasSpell(spell) && spell != SpellType.NO_SPELL && spell != SpellType.NUM_SPELLS){
						unknown.Add(spell);
						colorstring cs = new colorstring();
						cs.strings.Add(new cstr(Spell.Name(spell).PadRight(18) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray));
						/*int failrate = (Spell.Level(spell) - TotalSkill(SkillType.MAGIC)) * 5;
						if(failrate < 0){
							failrate = 0;
						}
						cs.strings.Add(new cstr(failrate.ToString().PadLeft(9) + "%",FailColor(failrate)));*/
						cs.strings.Add(new cstr("".PadRight(7),Color.Gray));
						unknownstr.Add(cs + Spell.Description(spell));
					}
				}
				for(int i=unknown.Count+2;i<ROWS;++i){
					Screen.WriteMapString(i,0,"".PadRight(COLS));
				}
				colorstring topborder = new colorstring("----------------------Tier-----------------Description------------",Color.Gray);
				int selection = Select("Learn which spell? ",topborder,new colorstring("".PadRight(25,'-') + "[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(COLS,'-'),Color.Gray),unknownstr,false,true,false,true,HelpTopic.Spells);
				spells[unknown[selection]] = true;
				learned.Add("You learn " + Spell.Name(unknown[selection]) + ". ");
				spells_in_order.Add(unknown[selection]);
			}
			if(learned.Count > 0){
				foreach(string s in learned){
					B.Add(s);
				}
			}
		}
		/*public void GainXP(int num){
			if(num <= 0){
				num = 1;
			}
			xp += num;
			//here's the formula for gaining the next level:
			// (standard experience is mlevel * (10 + mlevel - playerlevel) )
			// the number of monsters of the CURRENT level you would need to slay in order to reach the next level is equal to
			//  10 + (currentlevel-1)*2 / 3
			// therefore you reach level 2 after defeating 10 level 1 foes, which give 10xp each,
			// and you reach level 3 after defeating 11 level 2 foes, which give 20xp each.
			// (and so on)
			List<string> learned = null;
			switch(level){
			case 0:
				if(xp >= 0){
					learned = LevelUp();
				}
				break;
			case 1:
				if(xp >= 100){
					learned = LevelUp();
				}
				break;
			case 2:
				if(xp >= 320){
					learned = LevelUp();
				}
				break;
			case 3:
				if(xp >= 680){
					learned = LevelUp();
				}
				break;
			case 4:
				if(xp >= 1160){
					learned = LevelUp();
				}
				break;
			case 5:
				if(xp >= 1810){
					learned = LevelUp();
				}
				break;
			case 6:
				if(xp >= 2650){
					learned = LevelUp();
				}
				break;
			case 7:
				if(xp >= 3630){
					learned = LevelUp();
				}
				break;
			case 8:
				if(xp >= 4830){
					learned = LevelUp();
				}
				break;
			case 9:
				if(xp >= 6270){
					learned = LevelUp();
				}
				break;
			}
			if(learned != null){
				foreach(string s in learned){
					B.Add(s);
				}
			}
		}
		public List<string> LevelUp(){
			List<string> learned = new List<string>();
			++level;
			if(level == 1){
				//B.Add("Welcome, adventurer! ");
				B.Add("Welcome, " + player_name + "! ");
			}
			else{
				B.Add("Welcome to level " + level + ". ");
			}
			DisplayStats();
			B.PrintAll();
			ConsoleKeyInfo command;
			List<SkillType> skills_increased = new List<SkillType>();
			List<FeatType> feats_increased = new List<FeatType>();
			bool done = false;
			while(!done){
				Screen.ResetColors();
				B.DisplayNow("Choose which skills you'll increase: ");
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				for(int i=0;i<5;++i){
					SkillType sk = (SkillType)i;
					Screen.WriteMapString(1+i*4,0,("["+(char)(i+97)+"] " + Skill.Name(sk)).PadRight(22));
					Screen.WriteMapChar(1+i*4,1,new colorchar(Color.Cyan,(char)(i+97)));
					Color levelcolor = skills_increased.Contains(sk)? Color.Green : Color.Gray;
					int skill_level = skills_increased.Contains(sk)? skills[sk] + 1 : skills[sk];
					Screen.WriteMapString(1+i*4,22,new cstr(levelcolor,("Level " + skill_level).PadRight(70)));
					FeatType ft = Feat.OfSkill(sk,0);
					Color featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					int feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(2+i*4,0,new cstr(featcolor,("    " + Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(35)));
					ft = Feat.OfSkill(sk,1);
					featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(2+i*4,35,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
					ft = Feat.OfSkill(sk,2);
					featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(3+i*4,0,new cstr(featcolor,("    " + Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(35)));
					ft = Feat.OfSkill(sk,3);
					featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(3+i*4,35,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
					Screen.WriteMapString(4+i*4,0,"".PadRight(COLS));
				}
				if(skills_increased.Count == 3){
					Screen.WriteMapString(21,0,"--Type [a-e] to choose a skill--[?] for help--[Enter] to accept---");
					Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
					Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'e'));
					Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
					Screen.WriteMapString(21,47,new cstr(Color.Magenta,"Enter"));
				}
				else{
					Screen.WriteMapString(21,0,"--Type [a-e] to choose a skill--[?] for help-------(" + (3-skills_increased.Count) + " left)-------");
					Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
					Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'e'));
					Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
				}
				Console.SetCursorPosition(37+Global.MAP_OFFSET_COLS,2);
				Console.CursorVisible = true;
				command = Console.ReadKey(true);
				Console.CursorVisible = false;
				char ch = ConvertInput(command);
				switch(ch){
				case 'a':
				case 'b':
				case 'c':
				case 'd':
				case 'e':
					SkillType chosen_skill = (SkillType)(((int)ch)-97);
					if(skills_increased.Count == 3 && !skills_increased.Contains(chosen_skill)){
						break;
					}
					if(skills_increased.Contains(chosen_skill)){
						skills_increased.Remove(chosen_skill);
						for(int i=0;i<4;++i){
							if(feats_increased.Contains(Feat.OfSkill(chosen_skill,i))){
								feats_increased.Remove(Feat.OfSkill(chosen_skill,i));
							}
						}
					}
					else{
						skills_increased.Add(chosen_skill);
						bool done2 = false;
						while(!done2){
							Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
							for(int i=0;i<5;++i){
								SkillType sk = (SkillType)i;
								Color graycolor = Color.DarkGray;
								Color greencolor = Color.DarkGreen;
								Color magentacolor = Color.DarkMagenta;
								if(sk == chosen_skill){
									graycolor = Color.Gray;
									greencolor = Color.Green;
									magentacolor = Color.Magenta;
								}
								Screen.WriteMapString(1+i*4,0,new cstr(graycolor,("    " + Skill.Name(sk)).PadRight(22)));
								Color levelcolor = skills_increased.Contains(sk)? greencolor : graycolor;
								int skill_level = skills_increased.Contains(sk)? skills[sk] + 1 : skills[sk];
								Screen.WriteMapString(1+i*4,22,new cstr(levelcolor,("Level " + skill_level).PadRight(70)));
								FeatType ft = Feat.OfSkill(sk,0);
								Color featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								int feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(2+i*4,4,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(31)));
								ft = Feat.OfSkill(sk,1);
								featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(2+i*4,35,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
								ft = Feat.OfSkill(sk,2);
								featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(3+i*4,4,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(31)));
								ft = Feat.OfSkill(sk,3);
								featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(3+i*4,35,new cstr(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
								Screen.WriteMapString(4+i*4,0,"".PadRight(COLS));
							}
							Screen.WriteMapString(2+4*(int)chosen_skill,0,"[a]");
							Screen.WriteMapString(2+4*(int)chosen_skill,31,"[b]");
							Screen.WriteMapString(3+4*(int)chosen_skill,0,"[c]");
							Screen.WriteMapString(3+4*(int)chosen_skill,31,"[d]");
							if(feats[Feat.OfSkill(chosen_skill,0)] == 1){
								Screen.WriteMapChar(2+4*(int)chosen_skill,1,new colorchar(Color.DarkRed,'a'));
							}
							else{
								Screen.WriteMapChar(2+4*(int)chosen_skill,1,new colorchar(Color.Cyan,'a'));
							}
							if(feats[Feat.OfSkill(chosen_skill,1)] == 1){
								Screen.WriteMapChar(2+4*(int)chosen_skill,32,new colorchar(Color.DarkRed,'b'));
							}
							else{
								Screen.WriteMapChar(2+4*(int)chosen_skill,32,new colorchar(Color.Cyan,'b'));
							}
							if(feats[Feat.OfSkill(chosen_skill,2)] == 1){
								Screen.WriteMapChar(3+4*(int)chosen_skill,1,new colorchar(Color.DarkRed,'c'));
							}
							else{
								Screen.WriteMapChar(3+4*(int)chosen_skill,1,new colorchar(Color.Cyan,'c'));
							}
							if(feats[Feat.OfSkill(chosen_skill,3)] == 1){
								Screen.WriteMapChar(3+4*(int)chosen_skill,32,new colorchar(Color.DarkRed,'d'));
							}
							else{
								Screen.WriteMapChar(3+4*(int)chosen_skill,32,new colorchar(Color.Cyan,'d'));
							}
							Screen.WriteMapString(21,0,"--Type [a-d] to choose a feat---[?] for help----------------------");
							Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
							Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'d'));
							Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
							Screen.ResetColors();
							B.DisplayNow("Choose a " + Skill.Name(chosen_skill) + " feat: ");
							Console.CursorVisible = true;
							command = Console.ReadKey(true);
							Console.CursorVisible = false;
							ch = ConvertInput(command);
							switch(ch){
							case 'a':
							case 'b':
							case 'c':
							case 'd':
								{
								FeatType feat = Feat.OfSkill(chosen_skill,((int)ch)-97);
								if(!HasFeat(feat)){
									feats_increased.Add(feat);
									done2 = true;
								}
								break;
								}
							case '?':
								Help.DisplayHelp(HelpTopic.Feats);
								DisplayStats();
								break;
							case ' ':
							case (char)27:
								skills_increased.Remove(chosen_skill);
								done2 = true;
								break;
							default:
								break;
							}
						}
					}
					break;
				case '?':
					Help.DisplayHelp(HelpTopic.Feats);
					DisplayStats();
					break;
				case (char)13:
					if(skills_increased.Count == 3){
						done = true;
					}
					break;
				default:
					break;
				}
			}
			foreach(SkillType skill in skills_increased){
				skills[skill]++;
				if(Global.quickstartinfo != null){
					Global.quickstartinfo.Add(skill.ToString());
				}
			}
			foreach(FeatType feat in feats_increased){
				feats[feat]--; //negative values are used until you've completely learned a feat
				if(feats[feat] == -(Feat.MaxRank(feat))){
					feats[feat] = 1;
					learned.Add("You learn the " + Feat.Name(feat) + " feat. ");
					if(feat == FeatType.DANGER_SENSE){
						attrs[AttrType.DANGER_SENSE_ON]++;
					}
					if(feat == FeatType.DRIVE_BACK){
						attrs[AttrType.DRIVE_BACK_ON]++;
					}
				}
				if(Global.quickstartinfo != null){
					Global.quickstartinfo.Add(feat.ToString());
				}
			}
			if(skills_increased.Contains(SkillType.MAGIC)){
				List<SpellType> unknown = new List<SpellType>();
				List<colorstring> unknownstr = new List<colorstring>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(!HasSpell(spell) && spell != SpellType.BLESS && spell != SpellType.MINOR_HEAL
					&& spell != SpellType.HOLY_SHIELD && spell != SpellType.NO_SPELL && spell != SpellType.NUM_SPELLS){
						unknown.Add(spell);
						cstr cs1 = new cstr(Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray);
						int failrate = (Spell.Level(spell) - TotalSkill(SkillType.MAGIC)) * 5;
						if(failrate < 0){
							failrate = 0;
						}
						Color failcolor = Color.White;
						if(failrate > 50){
							failcolor = Color.DarkRed;
						}
						else{
							if(failrate > 20){
								failcolor = Color.Red;
							}
							else{
								if(failrate > 0){
									failcolor = Color.Yellow;
								}
							}
						}
						cstr cs2 = new cstr(failrate.ToString().PadLeft(9) + "%",failcolor);
						cstr cs3 = new cstr(Spell.Description(spell).PadLeft(34),Color.Gray);
						unknownstr.Add(new colorstring(cs1,cs2,cs3));
					}
				}
				for(int i=unknown.Count+2;i<ROWS;++i){
					Screen.WriteMapString(i,0,"".PadRight(COLS));
				}
				colorstring topborder = new colorstring("------------------Level---Fail rate--------Description------------",Color.Gray);
				int selection = Select("Learn which spell? ",topborder,new colorstring("".PadRight(COLS,'-'),Color.Gray),unknownstr,false,true,false,true,HelpTopic.Spells);
				spells[unknown[selection]] = 1;
				learned.Add("You learn " + Spell.Name(unknown[selection]) + ". ");
				if(Global.quickstartinfo != null){
					Global.quickstartinfo.Add(unknown[selection].ToString());
				}
			}
			return learned;
		}*/
		public bool CanSee(int r,int c){ return CanSee(M.tile[r,c]); }
		public bool CanSee(PhysicalObject o){
			if(o == this || p.Equals(o.p)){ //same object or same location
				return true;
			}
			if(HasAttr(AttrType.ASLEEP)){
				return false;
			}
			Actor a = o as Actor;
			if(a != null){
				/*if(HasAttr(AttrType.BLOODSCENT) && !a.HasAttr(AttrType.NONLIVING)){
					int distance_of_closest = 99;
					foreach(Actor a2 in ActorsWithinDistance(12,true)){
						if(!a2.HasAttr(AttrType.NONLIVING)){
							if(DistanceFrom(a2) < distance_of_closest){
								distance_of_closest = DistanceFrom(a2);
							}
						}
					}
					if(distance_of_closest == DistanceFrom(a)){
						return true;
					}
				}*/
				if(HasAttr(AttrType.DETECTING_MONSTERS)){
					return true;
				}
				if(a.HasAttr(AttrType.SHADOW_CLOAK) && !a.tile().IsLit() && !HasAttr(AttrType.BLINDSIGHT)){
					//if(a != player || !HasAttr(AttrType.PLAYER_NOTICED)){ //player is visible once noticed
						return false;
					//}
				}
			}
			Tile t = o as Tile;
			if(t != null){
				if(t.solid_rock){
					return false;
				}
			}
			if(HasAttr(AttrType.BLIND) && !HasAttr(AttrType.BLINDSIGHT)){
				return false;
			}
			if(type == ActorType.CLOUD_ELEMENTAL){
				List<pos> cloud = M.tile.GetFloodFillPositions(p,false,x=>M.tile[x].features.Contains(FeatureType.FOG));
				foreach(pos p2 in cloud){
					if(o.DistanceFrom(p2) <= 12){
						if(M.tile[p2].HasLOS(o.row,o.col)){
							if(o is Actor){
								if((o as Actor).IsHiddenFrom(this)){
									return false;
								}
								return true;
							}
							else{
								return true;
							}
						}
					}
				}
				return false;
			}
			else{
				if(IsWithinSightRangeOf(o.row,o.col) || (M.tile[o.row,o.col].IsLit() && !HasAttr(AttrType.BLINDSIGHT))){
					if(HasLOS(o.row,o.col)){
						if(o is Actor){
							if((o as Actor).IsHiddenFrom(this)){
								return false;
							}
							return true;
						}
						else{
							return true;
						}
					}
				}
			}
			return false;
		}
		public int SightRange(){
			int divisor = HasAttr(AttrType.DIM_VISION)? 3 : 1;
			if(HasAttr(AttrType.BLINDSIGHT)){
				return 12 / divisor;
			}
			if(HasAttr(AttrType.LOW_LIGHT_VISION)){
				return 5 / divisor;
			}
			return 3 / divisor;
		}
		public bool IsWithinSightRangeOf(PhysicalObject o){ return IsWithinSightRangeOf(o.row,o.col); }
		public bool IsWithinSightRangeOf(int r,int c){
			int dist = DistanceFrom(r,c);
			int divisor = HasAttr(AttrType.DIM_VISION)? 3 : 1;
			if(dist <= 3/divisor){
				return true;
			}
			if(dist <= 5/divisor && HasAttr(AttrType.LOW_LIGHT_VISION)){
				return true;
			}
			if(dist <= 12/divisor && HasAttr(AttrType.BLINDSIGHT)){
				return true;
			}
			if(M.tile[r,c].opaque){
				foreach(Tile t in M.tile[r,c].NonOpaqueNeighborsBetween(row,col)){
					if(IsWithinSightRangeOf(t.row,t.col)){
						return true;
					}
				}
			}
			return false;
		}
		public bool IsHiddenFrom(Actor a){
			if(this == a){ //you can always see yourself
				return false;
			}
			//if(a.HasAttr(AttrType.ASLEEP)){ //todo: testing this
			//	return true;
			//}
			if(HasAttr(AttrType.SHADOW_CLOAK) && !tile().IsLit() && !a.HasAttr(AttrType.BLINDSIGHT)){
				if(this == player && !a.HasAttr(AttrType.PLAYER_NOTICED)){ //monsters aren't hidden from each other
					return true;
				}
				if(a == player && !HasAttr(AttrType.NOTICED)){
					return true;
				}
			}
			if(type == ActorType.PLAYER){
				if(a.player_visibility_duration < 0){
					return false;
				}
				return true;
			}
			else{
				if(a.type != ActorType.PLAYER){ //monsters are never hidden from each other
					return false;
				}
				if(HasAttr(AttrType.STEALTHY) && attrs[AttrType.TURNS_VISIBLE] >= 0){
					return true;
				}
				return false;
			}
		}
		public static string MonsterDescriptionText(ActorType type){
			switch(type){
			case ActorType.GOBLIN:
				return "The goblin is a small ugly humanoid, often found inhabiting the upper reaches of any cave, chamber, or tunnel it can find.";
			case ActorType.GIANT_BAT:
				return "The bats here are substantially bigger than most, perhaps because their insect prey is also unusually large.";
			case ActorType.LONE_WOLF:
				return "Lithe and quick, this canine predator has formidable teeth and powerful jaws.";
			case ActorType.SKELETON:
				return "A humanoid skeleton, animated by magic, seeing without eyes, and moving without muscles.";
			case ActorType.BLOOD_MOTH:
				return "Found fluttering around any source of light, this huge moth is named for the rivulets of crimson on its wings that mimic dripping blood. Unlike most moths, it has a wide razor-filled mouth.";
			case ActorType.SWORDSMAN:
				return "Always ready for a fight, the swordsman twirls his sword in his hand as he walks. His eyes never leave his foe, watching and waiting for the next advance.";
			case ActorType.DARKNESS_DWELLER:
				return "This pale dirty humanoid wears tattered rags. Its huge eyes are sensitive to light.";
			case ActorType.CARNIVOROUS_BRAMBLE:
				return "Sharp tangles of thorny branches spread out from its center. The closest branches seem to follow your movements.";
			case ActorType.FROSTLING:
				return "An alien-looking creature of cold, the frostling possesses insectlike mandibles, claws, and smooth whitish skin. A fog of chill condensation surrounds it.";
			case ActorType.DREAM_WARRIOR:
			case ActorType.DREAM_WARRIOR_CLONE:
				return "The features of this warrior are hard to make out, but the curved blade held at the ready is clear enough.";
			case ActorType.CULTIST:
				return "This cultist wears a crimson robe that reaches the ground. His head has been shaved and tattooed in devotion to his demon lord.";
			case ActorType.GOBLIN_ARCHER:
				return "This goblin carries a crude bow and wears a quiver of arrows. It glances around, looking for inviting targets.";
			case ActorType.GOBLIN_SHAMAN:
				return "This goblin's markings identify it as a tribe leader and shaman. It carries a small staff and wears a necklace of ears and fingers.";
			case ActorType.MIMIC:
				return "The mimic changes its shape to that of an ordinary object, then waits for an unwary goblin or adventurer. It can secrete a powerful adhesive to hold its prey.";
			case ActorType.SKULKING_KILLER:
				return "This rogue dashes from shadow to shadow, dagger in hand. A smirk appears as the killer overtakes another victim.";
			case ActorType.ZOMBIE:
				return "The zombie is a rotting, shambling corpse animated by the dark art of necromancy. It mindlessly seeks the flesh of the living.";
			case ActorType.DIRE_RAT:
				return "With red eyes and long yellow teeth, most dire rats outweigh forty of their smaller brethren.";
			case ActorType.ROBED_ZEALOT:
				return "A holy symbol hangs, silver and forked, from the neck of the zealot. The holy magic of the church's spells promises the zealot a swift victory over heretics.";
			case ActorType.SHADOW:
				return "Shadows are manifest darkness, barely maintaining a physical presence. A dark environment hides them utterly, but the light reveals their warped human shape.";
			case ActorType.BANSHEE:
				return "The banshee floats shrieking, trailing wisps of a faded dress behind her. Her nails are blood-caked claws. The banshee's hateful scream is painful for the living to hear.";
			case ActorType.WARG:
				return "This wolf has white fur with black markings. Its eyes are too human for your liking.";
			case ActorType.PHASE_SPIDER:
				return "Heedless of the laws of nature, this brilliantly iridescent spider steps to the side and appears twenty feet away. Even when you're looking right at it, you think you can hear it behind you.";
			case ActorType.DERANGED_ASCETIC:
				return "This solitary monk constantly kicks and punches at empty space, madly repeating words of nonsense. Those nearby will find themselves uttering the same gibberish.";
			case ActorType.POLTERGEIST:
				return "This troublesome spirit has a penchant for throwing things and upending furniture. It affords no rest to intruders in the area that it haunts.";
			case ActorType.CAVERN_HAG:
				return "The hag's foul brand of magic can impart a nasty curse on those who cross her. Cracked, warty skin hides surprising strength, used to wrestle her victims into the stewpot.";
			case ActorType.NOXIOUS_WORM:
				return "The noxious worm, almost as tall as a man, slams foes with its bulk. It vomits a thick stench from its maw.";
			case ActorType.BERSERKER:
				return "In battle, the berserker enters a state of unfeeling rage, axe swinging at anything within reach. Trophies of war adorn the berserker's minimally armored form.";
			case ActorType.TROLL:
				return "The troll towers above you, all muscles, claws, and warty greenish skin. The regenerative powers of the troll are well-known, as is the suggestion to fight them with fire.";
			case ActorType.VAMPIRE:
				return "The vampire floats above the ground with hunger in its eyes. A dark cape flows around its pale form.";
			case ActorType.CRUSADING_KNIGHT:
				return "This knight's armor bears the holy symbols of his church. He holds his torch aloft, awaiting the appearance of evildoers.";
			case ActorType.INFESTED_MASS_TODO_NAME:
				return "TODO"; //todo
			case ActorType.MUD_ELEMENTAL:
				return "As the mud elemental oozes across the floor, bits of dirt seem to animate and are absorbed into its body.";
			case ActorType.MUD_TENTACLE:
				return "A writhing, grasping tendril of mud emerges from the wall.";
			case ActorType.ENTRANCER:
				return "The entrancer bends a weak-minded being to her will and has it fight on her behalf, at least until a more desirable thrall appears. In battle, the entrancer can protect and teleport the enthralled creature.";
			case ActorType.MARBLE_HORROR:
				return "Its shape is still that of a statue, but the darkness reveals the diseased appearance of its pale skin. No light is reflected from its empty eyes.";
			case ActorType.MARBLE_HORROR_STATUE:
				return "As a statue, the marble horror is invulnerable and inactive. It will remain in this form as long as light falls upon it.";
			case ActorType.OGRE:
				return "Built like an orc, but as big as a troll, this tusked brute wields a giant club.";
			case ActorType.ORC_GRENADIER:
				return "Orcs are a burly and warlike race, quick to make enemies. This one carries a satchel filled with deadly orcish explosives.";
			case ActorType.SHADOWVEIL_DUELIST:
				return "The shadowveil duelist hides under a cloak of shadows to strike unseen. A spinning, feinting fighting style keeps the duelist in motion.";
			case ActorType.CARRION_CRAWLER:
				return "This many-legged segmented insect crawls over the ground and walls in search of carrion. When threatened or lacking another source of food, tentacles on its head are used to apply a paralyzing substance to living prey.";
			case ActorType.SPELLMUDDLE_PIXIE:
				return "Using fairy enchantments to influence the flow of magic, this pixie causes its every wingbeat to reverberate in the skulls of those nearby, stifling words of magic.";
			case ActorType.STONE_GOLEM:
				return "Constructs of stone are often created to guard or serve. Their rocky nature grants them a degree of resistance to many forms of attack.";
			case ActorType.PYREN_ARCHER:
				return "Tall and wide-shouldered descendants of flame, the pyren are a strange race of men. Though they are flesh and blood, they still possess the power to ignite nearby objects.";
			case ActorType.ORC_ASSASSIN:
				return "This orcish stalker is well camouflaged. A wicked grin shows off sharp teeth as the assassin brandishes a long blade.";
			case ActorType.TROLL_BLOODWITCH:
				return "The seer is a leader among the solitary troll population, sought for augury and council. Spells and arcane tricks are passed down from seer to seer. ";
			case ActorType.MECHANICAL_KNIGHT:
				return "The mechanical knight's shield moves with unnatural speed, ready to foil any onslaught. Its exposed gears appear vulnerable to any attack that could bypass its shield.";
			case ActorType.ORC_WARMAGE:
				return "The destruction wreaked by warmages evokes respect and fear even among their own kind. They often lead raids and war parties, using tracking spells to complement their lethal magic.";
			case ActorType.LASHER_FUNGUS:
				return "The lasher is a tall mass of fungal growth with several ropelike tentacles extending from it.";
			case ActorType.NECROMANCER:
				return "Necromancers practice the dark arts, raising the dead to serve them. They gain power through unholy rituals that make them unwelcome in any civilized place.";
			case ActorType.LUMINOUS_AVENGER:
				return "The radiance of this empyreal being makes your eyes hurt after a few seconds. When you look again it still has the shape of a human, but occasionally its silhouette seems to have wings, horns, or four legs.";
			case ActorType.CORPSETOWER_BEHEMOTH:
				return "This monstrosity looks like it was stitched together from corpses of several different species. You see pieces of humans, orcs, and trolls, in addition to some you can't begin to identify.";
			case ActorType.FIRE_DRAKE:
				return "Huge, deadly, and hungry for your charred flesh, the fire drake prepares to drag your valuables back to its lair. You have no doubts that you now face the snarling fiery master of this dungeon.";
			default:
				return "Phantoms are beings of illusion, but real enough to do lasting harm. Because they vanish at the slightest touch, they are easily dispatched with magic spells.";
			}
		}
		public static List<colorstring> MonsterDescriptionBox(ActorType type,int max_string_length){
			List<string> text = MonsterDescriptionText(type).GetWordWrappedList(max_string_length);
			Color box_edge_color = Color.Green;
			Color box_corner_color = Color.Yellow;
			Color text_color = Color.Gray;
			int widest = 20; // length of "[=] Hide description"
			foreach(string s in text){
				if(s.Length > widest){
					widest = s.Length;
				}
			}
			widest += 2; //one space on each side
			List<colorstring> box = new List<colorstring>();
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			foreach(string s in text){
				box.Add(new colorstring("|",box_edge_color) + s.PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
			}
			box.Add(new colorstring("|",box_edge_color,"".PadRight(widest),Color.Gray,"|",box_edge_color));
			box.Add(new colorstring("|",box_edge_color) + "[=] Hide description".PadOuter(widest).GetColorString(text_color) + new colorstring("|",box_edge_color));
			box.Add(new colorstring("+",box_corner_color,"".PadRight(widest,'-'),box_edge_color,"+",box_corner_color));
			return box;
		}
		public void FindPath(PhysicalObject o){ path = GetPath(o); }
		public void FindPath(PhysicalObject o,int max_distance){ path = GetPath(o,max_distance); }
		public void FindPath(PhysicalObject o,int max_distance,bool path_around_seen_traps){ path = GetPath(o,max_distance,path_around_seen_traps); }
		public void FindPath(int r,int c){ path = GetPath(r,c); }
		public void FindPath(int r,int c,int max_distance){ path = GetPath(r,c,max_distance); }
		public void FindPath(int r,int c,int max_distance,bool path_around_seen_traps){ path = GetPath(r,c,max_distance,path_around_seen_traps); }
		public List<pos> GetPath(PhysicalObject o){ return GetPath(o.row,o.col,-1,false); }
		public List<pos> GetPath(PhysicalObject o,int max_distance){ return GetPath(o.row,o.col,max_distance,false); }
		public List<pos> GetPath(PhysicalObject o,int max_distance,bool path_around_seen_traps){ return GetPath(o.row,o.col,max_distance,path_around_seen_traps); }
		public List<pos> GetPath(int r,int c){ return GetPath(r,c,-1,false); }
		public List<pos> GetPath(int r,int c,int max_distance){ return GetPath(r,c,max_distance,false); }
		public List<pos> GetPath(int r,int c,int max_distance,bool path_around_seen_traps){ //tiles past this distance are ignored entirely
			List<pos> path = new List<pos>();
			int[,] values = new int[ROWS,COLS];
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(M.tile[i,j].passable || (HasAttr(AttrType.HUMANOID_INTELLIGENCE) && M.tile[i,j].Is(TileType.DOOR_C))
					|| (HasAttr(AttrType.BOSS_MONSTER) && M.tile[i,j].Is(TileType.HIDDEN_DOOR))){
						if(path_around_seen_traps && M.tile[i,j].IsKnownTrap()){
							values[i,j] = -1;
						}
						else{
							values[i,j] = 0;
						}
						if(M.tile[i,j].type == TileType.CHASM){ //don't path over chasms
							values[i,j] = -1;
						}
					}
					else{
						values[i,j] = -1;
					}
				}
			}
			int minrow = Math.Max(1,row-max_distance);
			int maxrow = Math.Min(ROWS-2,row+max_distance);
			int mincol = Math.Max(1,col-max_distance);
			int maxcol = Math.Min(COLS-2,col+max_distance);
			if(max_distance == -1){
				minrow = 1;
				maxrow = ROWS-2;
				mincol = 1;
				maxcol = COLS-2;
			}
			values[row,col] = 1;
			int val = 1;
			bool done = false;
			while(!done){
				for(int i=minrow;!done && i<=maxrow;++i){
					for(int j=mincol;!done && j<=maxcol;++j){
						if(values[i,j] == val){
							for(int s=i-1;!done && s<=i+1;++s){
								for(int t=j-1;!done && t<=j+1;++t){
									if(s != i || t != j){
										if(values[s,t] == 0){
											values[s,t] = val + 1;
											if(s == r && t == c){ //if we've found the target..
												done = true;
												path.Add(new pos(s,t));
											}
										}
									}
								}
							}
						}
					}
				}
				++val;
				if(val > 1000){//not sure what this value should be
					path.Clear();
					return path;
				}
			}
			//val is now equal to the value of the target's position
			pos p = path[0];
			for(int i=val-1;i>1;--i){
				pos? best = null;
				foreach(pos neighbor in p.PositionsAtDistance(1)){
					if(values[neighbor.row,neighbor.col] == i){
						if(best == null){
							best = neighbor;
						}
						else{
							if(neighbor.ApproximateEuclideanDistanceFromX10(p) < best.Value.ApproximateEuclideanDistanceFromX10(p)){
								best = neighbor;
							}
						}
					}
				}
				if(best == null){//<--hope this doesn't happen
					path.Clear();
					return path;
				}
				p = best.Value;
				path.Add(p);
			}
			path.Reverse();
			if(DistanceFrom(path[0]) > 1){
				throw new Exception("too far away");
			}
			return path;
		}
		public bool FindAutoexplorePath(){ //returns true if successful
			List<pos> path = new List<pos>();
			int[,] values = new int[ROWS,COLS];
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(!M.tile[i,j].passable && !(M.tile[i,j].type == TileType.DOOR_C)){ //default is 0 of course
						values[i,j] = -1;
					}
					if(M.tile[i,j].IsKnownTrap()){
						values[i,j] = -1;
					}
					if(M.tile[i,j].type == TileType.CHASM){
						values[i,j] = -1;
					}
				}
			}
			int minrow = 1;
			int maxrow = ROWS-2;
			int mincol = 1;
			int maxcol = COLS-2;
			values[row,col] = 1;
			int val = 1;
			bool val_plus_one = false; //a bit hacky; changes based on whether you're going to an item or not.
			List<pos> frontiers = new List<pos>();
			while(frontiers.Count == 0){
				for(int i=minrow;i<=maxrow;++i){
					for(int j=mincol;j<=maxcol;++j){
						if(values[i,j] == val){
							for(int s=i-1;s<=i+1;++s){
								for(int t=j-1;t<=j+1;++t){
									if(s != i || t != j){
										if(values[s,t] == 0){
											values[s,t] = val + 1;
											if(!M.tile[s,t].seen && (M.tile[s,t].passable || M.tile[s,t].type == TileType.DOOR_C)){
												//frontiers.AddUnique(new pos(i,j));
												frontiers.AddUnique(new pos(s,t));
												val_plus_one = true;
											}
											if(M.tile[s,t].inv != null && !M.tile[s,t].inv.ignored){
												frontiers.AddUnique(new pos(s,t));
												val_plus_one = true;
											}
										}
									}
								}
							}
						}
					}
				}
				++val;
				if(val > 1000){//not sure what this value should be
					this.path.Clear();
					return false;
				}
			}
			if(val_plus_one){
				++val;
			}
			//val is now equal to the value of the unseen tile's position
			pos frontier = new pos(-1,-1);
			int unseen_tiles = 9;
			foreach(pos p in frontiers){
				int total = 0;
				foreach(pos neighbor in p.PositionsAtDistance(1)){
					if(!M.tile[neighbor].seen && (M.tile[neighbor].passable || M.tile[neighbor].type == TileType.DOOR_C)){
						++total;
					}
				}
				if(total < unseen_tiles){
					unseen_tiles = total;
					frontier = p;
				}
			}
			path.Add(frontier);
			pos current = frontier;
			for(int i=val-2;i>1;--i){
				pos? best = null;
				foreach(pos neighbor in current.PositionsAtDistance(1)){
					if(values[neighbor.row,neighbor.col] == i){ //forgot to use the PosArray type for values, whoops
						if(best == null){
							best = neighbor;
						}
						else{
							if(neighbor.ApproximateEuclideanDistanceFromX10(current) < best.Value.ApproximateEuclideanDistanceFromX10(current)){
								best = neighbor;
							}
						}
					}
				}
				if(best == null){//<--hope this doesn't happen
					this.path.Clear();
					return false;
				}
				current = best.Value;
				path.Add(current);
			}
			path.Reverse();
			this.path = path;
			return true;
		}
		public int EnemiesAdjacent(){ //currently counts ALL actors adjacent, and as such really only applies to the player.
			int count = -1; //don't count self
			for(int i=row-1;i<=row+1;++i){
				for(int j=col-1;j<=col+1;++j){
					if(M.actor[i,j] != null){ //no bounds check, actors shouldn't be on edge tiles.
						++count;
					}
				}
			}
			return count;
		}
		public int GetDirection(){ return GetDirection("Which direction? ",false,false); }
		public int GetDirection(bool orth,bool allow_self_targeting){ return GetDirection("Which direction? ",orth,allow_self_targeting); }
		public int GetDirection(string s){ return GetDirection(s,false,false); }
		public int GetDirection(string s,bool orth,bool allow_self_targeting){
			B.DisplayNow(s);
			ConsoleKeyInfo command;
			char ch;
			Console.CursorVisible = true;
			while(true){
				command = Console.ReadKey(true);
				ch = ConvertInput(command);
				if(command.KeyChar == '.'){
					ch = '5';
				}
				ch = ConvertVIKeys(ch);
				int i = (int)Char.GetNumericValue(ch);
				if(i>=1 && i<=9){
					if(i != 5){
						if(!orth || i%2==0){ //in orthogonal mode, return only even dirs
							Console.CursorVisible = false;
							return i;
						}
					}
					else{
						if(allow_self_targeting){
							Console.CursorVisible = false;
							return i;
						}
					}
				}
				if(ch == (char)27){ //escape
					Console.CursorVisible = false;
					return -1;
				}
				if(ch == ' '){
					Console.CursorVisible = false;
					return -1;
				}
			}
		}
		public List<Tile> GetTargetTile(int max_distance,int radius,bool start_at_interesting_target){ return GetTarget(false,max_distance,radius,false,start_at_interesting_target); }
		public List<Tile> GetTargetLine(int max_distance){ return GetTarget(false,max_distance,0,true,true); }
		public List<Tile> GetTarget(bool lookmode,int max_distance,int radius,bool extend_line,bool start_at_interesting_target){
			List<Tile> result = null;
			ConsoleKeyInfo command;
			int r,c;
			int minrow = 0;
			int maxrow = ROWS-1;
			int mincol = 0;
			int maxcol = COLS-1;
			if(max_distance > 0){
				minrow = Math.Max(minrow,row - max_distance);
				maxrow = Math.Min(maxrow,row + max_distance);
				mincol = Math.Max(mincol,col - max_distance);
				maxcol = Math.Min(maxcol,col + max_distance);
			}
			bool hide_descriptions = false;
			List<PhysicalObject> interesting_targets = new List<PhysicalObject>();
			for(int i=1;(i<=max_distance || max_distance==-1) && i<=Math.Max(ROWS,COLS);++i){
				foreach(Actor a in ActorsAtDistance(i)){
					if(CanSee(a)){
						if(lookmode || ((IsWithinSightRangeOf(a) || a.tile().IsLit()) && HasLOS(a))){
							interesting_targets.Add(a);
						}
					}
				}
			}
			if(lookmode){
				for(int i=1;(i<=max_distance || max_distance==-1) && i<=Math.Max(ROWS,COLS);++i){
					foreach(Tile t in TilesAtDistance(i)){
						if(t.Is(TileType.STAIRS,TileType.CHEST,TileType.FIREPIT,TileType.STALAGMITE,TileType.FIRE_GEYSER,TileType.FOG_VENT,
						        TileType.POISON_GAS_VENT,TileType.HEALING_POOL,TileType.BLAST_FUNGUS)
						|| t.Is(FeatureType.GRENADE,FeatureType.QUICKFIRE,FeatureType.FIRE,FeatureType.TROLL_CORPSE,FeatureType.TROLL_BLOODWITCH_CORPSE,
						        FeatureType.INACTIVE_TELEPORTAL,FeatureType.POISON_GAS,FeatureType.FOG)
						|| t.IsShrine() || t.inv != null){ //todo: update this with new terrain & features
							if(CanSee(t)){
								interesting_targets.Add(t);
							}
						}
						if(t.IsKnownTrap() && CanSee(t)){
							interesting_targets.AddUnique(t);
						}
					}
				}
			}
			colorchar[,] mem = new colorchar[ROWS,COLS];
			List<Tile> line = new List<Tile>();
			List<Tile> oldline = new List<Tile>();
			bool description_shown_last_time = false;
			int desc_row = -1;
			int desc_col = -1;
			int desc_height = -1;
			int desc_width = -1;
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					mem[i,j] = Screen.MapChar(i,j);
				}
			}
			if(!start_at_interesting_target || interesting_targets.Count == 0){
				if(lookmode){
					B.DisplayNow("Move the cursor to look around. ");
				}
				else{
					B.DisplayNow("Move cursor to choose target, then press Enter. ");
				}
			}
			if(lookmode){
				if(!start_at_interesting_target || interesting_targets.Count == 0){
					r = row;
					c = col;
				}
				else{
					r = interesting_targets[0].row;
					c = interesting_targets[0].col;
				}
			}
			else{
				if(target == null || !CanSee(target)
				|| (max_distance > 0 && DistanceFrom(target) > max_distance)){
					if(!start_at_interesting_target || interesting_targets.Count == 0){
						r = row;
						c = col;
					}
					else{
						r = interesting_targets[0].row;
						c = interesting_targets[0].col;
					}
				}
				else{
					r = target.row;
					c = target.col;
					if(Global.Option(OptionType.LAST_TARGET)){
						List<Tile> bestline = null;
						if(extend_line){
							bestline = GetBestExtendedLineOfEffect(target).ToFirstSolidTile();
							if(bestline.Count > max_distance+1){
								bestline.RemoveRange(max_distance+1,bestline.Count - max_distance - 1);
							}
						}
						else{
							bestline = GetBestLineOfEffect(target).ToFirstSolidTile();
						}
						return bestline;
					}
				}
			}
			bool first_iteration = true;
			bool done=false; //when done==true, we're ready to return 'result'
			while(!done){
				Screen.ResetColors();
				string contents = "You see ";
				List<string> items = new List<string>();
				if(M.actor[r,c] != null && M.actor[r,c] != this && CanSee(M.actor[r,c])){
					items.Add(M.actor[r,c].a_name + " " + M.actor[r,c].WoundStatus());
				}
				if(M.tile[r,c].inv != null){
					items.Add(M.tile[r,c].inv.AName(true));
				}
				foreach(FeatureType f in M.tile[r,c].features){
					items.Add(Tile.Feature(f).a_name);
				}
				if(items.Count == 0){
					contents += M.tile[r,c].AName(true);
				}
				else{
					if(items.Count == 1){
						contents += items[0] + M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
					}
					else{
						if(items.Count == 2){
							if(M.tile[r,c].type != TileType.FLOOR){
								if(M.tile[r,c].Preposition() == " and "){
									contents += items[0] + ", " + items[1] + ",";
									contents += M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
								}
								else{
									contents += items[0] + " and " + items[1];
									contents += M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
								}
							}
							else{
								contents += items[0] + " and " + items[1]; //todo: this might be able to use the ConcatenateWithCommas extension method
							}
						}
						else{
							foreach(string s in items){
								if(s != items.Last()){
									contents += s + ", ";
								}
								else{
									if(M.tile[r,c].type != TileType.FLOOR){
										contents += s + ","; //because preposition contains a space already
									}
									else{
										contents += "and " + s;
									}
								}
							}
							if(M.tile[r,c].type != TileType.FLOOR){
								contents += M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
							}
						}
					}
				}
				if(r == row && c == col){
					if(!first_iteration){
						string s = "You're standing here. ";
						if(items.Count == 0 && M.tile[r,c].type == TileType.FLOOR){
							B.DisplayNow(s);
						}
						else{
							B.DisplayNow(s + contents + " here. ");
						}
					}
				}
				else{
					if(CanSee(M.tile[r,c])){
						B.DisplayNow(contents + ". ");
					}
					else{
						if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
							B.DisplayNow("You sense " + M.actor[r,c].a_name + " " + M.actor[r,c].WoundStatus() + ". ");
						}
						else{
							if(M.tile[r,c].seen){
								colorchar tilech = new colorchar(M.tile[r,c].symbol,M.tile[r,c].color);
								colorchar screench = Screen.MapChar(r,c);
								if(M.tile[r,c].inv != null && (tilech.c != screench.c || tilech.color != screench.color)){ //hacky, but it seems to work (when a monster drops an item you haven't seen yet)
									if(M.tile[r,c].inv.quantity > 1){
										B.DisplayNow("You can no longer see these " + M.tile[r,c].inv.Name(true) + ". "); //todo: double check this hack. it might not work.
									}
									else{
										B.DisplayNow("You can no longer see this " + M.tile[r,c].inv.Name(true) + ". ");
									}
								}
								else{
									B.DisplayNow("You can no longer see this " + M.tile[r,c].Name(true) + ". ");
								}
							}
							else{
								if(lookmode){
									B.DisplayNow("");
								}
								else{
									B.DisplayNow("Move cursor to choose target, then press Enter. ");
								}
							}
						}
					}
				}
				if(!lookmode){
					bool blocked=false;
					Console.CursorVisible = false;
					if(extend_line){
						line = GetBestExtendedLineOfEffect(r,c);
						if(line.Count > max_distance+1){
							line.RemoveRange(max_distance+1,line.Count - max_distance - 1);
						}
					}
					else{
						line = GetBestLineOfEffect(r,c);
					}
					foreach(Tile t in line){
						if(t.row != row || t.col != col){
							colorchar cch = mem[t.row,t.col];
							if(t.row == r && t.col == c){
								if(!blocked){
									cch.bgcolor = Color.Green;
									if(Global.LINUX){ //no bright bg in terminals
										cch.bgcolor = Color.DarkGreen;
									}
									if(cch.color == cch.bgcolor){
										cch.color = Color.Black;
									}
									Screen.WriteMapChar(t.row,t.col,cch);
								}
								else{
									cch.bgcolor = Color.Red;
									if(Global.LINUX){
										cch.bgcolor = Color.DarkRed;
									}
									if(cch.color == cch.bgcolor){
										cch.color = Color.Black;
									}
									Screen.WriteMapChar(t.row,t.col,cch);
								}
							}
							else{
								if(!blocked){
									cch.bgcolor = Color.DarkGreen;
									if(cch.color == cch.bgcolor){
										cch.color = Color.Black;
									}
									Screen.WriteMapChar(t.row,t.col,cch);
								}
								else{
									cch.bgcolor = Color.DarkRed;
									if(cch.color == cch.bgcolor){
										cch.color = Color.Black;
									}
									Screen.WriteMapChar(t.row,t.col,cch);
								}
							}
							if(t.seen && !t.passable && t != line.Last()){
								blocked=true;
							}
						}
						oldline.Remove(t);
					}
					if(radius > 0){
						foreach(Tile t in M.tile[r,c].TilesWithinDistance(radius,true)){
							if(!line.Contains(t)){
								colorchar cch = mem[t.row,t.col];
								if(blocked){
									cch.bgcolor = Color.DarkRed;
								}
								else{
									cch.bgcolor = Color.DarkGreen;
								}
								if(cch.color == cch.bgcolor){
									cch.color = Color.Black;
								}
								Screen.WriteMapChar(t.row,t.col,cch);
								oldline.Remove(t);
							}
						}
					}
					foreach(Tile t in oldline){
						Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
					}
				}
				else{
					colorchar cch = mem[r,c];
					cch.bgcolor = Color.Green;
					if(Global.LINUX){ //no bright bg in terminals
						cch.bgcolor = Color.DarkGreen;
					}
					if(cch.color == cch.bgcolor){
						cch.color = Color.Black;
					}
					Screen.WriteMapChar(r,c,cch);
					line = new List<Tile>{M.tile[r,c]};
					oldline.Remove(M.tile[r,c]);
					foreach(Tile t in oldline){ //to prevent the previous target appearing on top of the description box
						Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
					}
					if(!hide_descriptions && M.actor[r,c] != null && M.actor[r,c] != this && CanSee(M.actor[r,c])){
						bool description_on_right = false;
						int max_length = 29;
						if(c - 6 < max_length){
							max_length = c - 6;
						}
						if(max_length < 20){
							description_on_right = true;
							max_length = 29;
						}
						List<colorstring> desc = MonsterDescriptionBox(M.actor[r,c].type,max_length);
						if(description_on_right){
							int start_c = COLS - desc[0].Length();
							description_shown_last_time = true;
							desc_row = 0;
							desc_col = start_c;
							desc_height = desc.Count;
							desc_width = desc[0].Length();
							for(int i=0;i<desc.Count;++i){
								Screen.WriteMapString(i,start_c,desc[i]);
							}
						}
						else{
							description_shown_last_time = true;
							desc_row = 0;
							desc_col = 0;
							desc_height = desc.Count;
							desc_width = desc[0].Length();
							for(int i=0;i<desc.Count;++i){
								Screen.WriteMapString(i,0,desc[i]);
							}
						}
					}
					else{
						//description_shown_last_time = false;
					}
				}
				/*foreach(Tile t in oldline){ //moved. see above.
					Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
				}*/
				oldline = new List<Tile>(line);
				if(radius > 0){
					foreach(Tile t in M.tile[r,c].TilesWithinDistance(radius,true)){
						oldline.AddUnique(t);
					}
				}
				first_iteration = false;
				M.tile[r,c].Cursor();
				Console.CursorVisible = true;
				command = Console.ReadKey(true);
				char ch = ConvertInput(command);
				ch = ConvertVIKeys(ch);
				int move_value = 1;
				if((command.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt
				|| (command.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control
				|| (command.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift){
					move_value = 6;
				}
				switch(ch){
				case '7':
					r -= move_value;
					c -= move_value;
					break;
				case '8':
					r -= move_value;
					break;
				case '9':
					r -= move_value;
					c += move_value;
					break;
				case '4':
					c -= move_value;
					break;
				case '6':
					c += move_value;
					break;
				case '1':
					r += move_value;
					c -= move_value;
					break;
				case '2':
					r += move_value;
					break;
				case '3':
					r += move_value;
					c += move_value;
					break;
				case (char)9:
					if((command.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift){
						if(interesting_targets.Count > 0){
							List<PhysicalObject> reversed_targets = new List<PhysicalObject>(interesting_targets);
							reversed_targets.Reverse();
							int idx = 0;
							int dist = DistanceFrom(r,c);
							int idx_of_next_closest = -1;
							bool found = false;
							foreach(PhysicalObject o in reversed_targets){
								if(o.row == r && o.col == c){
									int prev_idx = idx + 1; //this goes backwards because the list goes backwards
									if(prev_idx == reversed_targets.Count){
										prev_idx = 0;
									}
									r = reversed_targets[prev_idx].row;
									c = reversed_targets[prev_idx].col;
									found = true;
									break;
								}
								else{
									if(idx_of_next_closest == -1 && DistanceFrom(o) < dist){
										idx_of_next_closest = idx;
									}
								}
								++idx;
							}
							if(!found){
								if(idx_of_next_closest == -1){
									r = reversed_targets[0].row;
									c = reversed_targets[0].col;
								}
								else{
									r = reversed_targets[idx_of_next_closest].row;
									c = reversed_targets[idx_of_next_closest].col;
								}
							}
						}
					}
					else{
						if(interesting_targets.Count > 0){
							int idx = 0;
							int dist = DistanceFrom(r,c);
							int idx_of_next_farthest = -1;
							bool found = false;
							foreach(PhysicalObject o in interesting_targets){
								if(o.row == r && o.col == c){
									int next_idx = idx + 1;
									if(next_idx == interesting_targets.Count){
										next_idx = 0;
									}
									r = interesting_targets[next_idx].row;
									c = interesting_targets[next_idx].col;
									found = true;
									break;
								}
								else{
									if(idx_of_next_farthest == -1 && DistanceFrom(o) > dist){
										idx_of_next_farthest = idx;
									}
								}
								++idx;
							}
							if(!found){
								if(idx_of_next_farthest == -1){
									r = interesting_targets[0].row;
									c = interesting_targets[0].col;
								}
								else{
									r = interesting_targets[idx_of_next_farthest].row;
									c = interesting_targets[idx_of_next_farthest].col;
								}
							}
						}
					}
					break;
				case '=':
					if(lookmode){
						hide_descriptions = !hide_descriptions;
					}
					break;
				case (char)27:
				case ' ':
					done = true;
					break;
				case (char)13:
				case 's':
					if(M.actor[r,c] != null && M.actor[r,c] != this && CanSee(M.actor[r,c]) && HasLOE(M.actor[r,c])){
						target = M.actor[r,c];
					}
					result = line.ToFirstSolidTile();
					done = true;
					break;
				default:
					break;
				}
				if(r < minrow){
					r = minrow;
				}
				if(r > maxrow){
					r = maxrow;
				}
				if(c < mincol){
					c = mincol;
				}
				if(c > maxcol){
					c = maxcol;
				}
				if(description_shown_last_time){
					Screen.MapDrawWithStrings(mem,desc_row,desc_col,desc_height,desc_width);
					description_shown_last_time = false;
				}
				if(done){
					Console.CursorVisible = false;
					foreach(Tile t in line){
						Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
					}
					if(radius > 0){
						foreach(Tile t in M.tile[r,c].TilesWithinDistance(radius,true)){
							if(!line.Contains(t)){
								Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
							}
						}
					}
					Console.CursorVisible = true;
				}
			}
			return result;
		}
		/*public List<Tile> GetTarget(){ return GetTarget(false,-1,true); }
		public List<Tile> GetTarget(bool lookmode){ return GetTarget(lookmode,-1,!lookmode); } //note default
		public List<Tile> GetTarget(int max_distance){ return GetTarget(false,max_distance,true); }
		public List<Tile> GetTarget(int max_distance,int radius){ return GetTarget(false,max_distance,true,radius); }
		public List<Tile> GetTarget(bool lookmode,int max_distance){ return GetTarget(lookmode,max_distance,!lookmode); }
		public List<Tile> GetTarget(bool lookmode,int max_distance,bool start_at_interesting_target){ return GetTarget(lookmode,max_distance,start_at_interesting_target,0); }
		public List<Tile> GetTarget(bool lookmode,int max_distance,bool start_at_interesting_target,int radius){
			List<Tile> result = null;
			ConsoleKeyInfo command;
			int r,c;
			int minrow = 0;
			int maxrow = ROWS-1;
			int mincol = 0;
			int maxcol = COLS-1;
			if(max_distance > 0){
				minrow = Math.Max(minrow,row - max_distance);
				maxrow = Math.Min(maxrow,row + max_distance);
				mincol = Math.Max(mincol,col - max_distance);
				maxcol = Math.Min(maxcol,col + max_distance);
			}
			bool allow_targeting_ground = false;
			bool hide_descriptions = false;
			if(radius < 0){
				if(radius != -1){ //negative radius is a hacky signal value
					radius = -(radius);
				}
				allow_targeting_ground = true;
			}
			List<PhysicalObject> interesting_targets = new List<PhysicalObject>();
			int target_idx = 0;
			for(int i=1;(i<=max_distance || max_distance==-1) && i<=COLS;++i){
				foreach(Actor a in ActorsAtDistance(i)){
					if(CanSee(a)){
						if(lookmode || ((IsWithinSightRangeOf(a) || a.tile().IsLit()) && HasLOS(a))){
							interesting_targets.Add(a);
						}
					}
				}
			}
			if(lookmode){
				for(int i=1;(i<=max_distance || max_distance==-1) && i<=COLS;++i){
					foreach(Tile t in TilesAtDistance(i)){
						if(t.type == TileType.STAIRS || t.type == TileType.CHEST
						|| t.Is(FeatureType.GRENADE) || t.type == TileType.FIREPIT
						|| t.Is(FeatureType.QUICKFIRE) || t.type == TileType.STALAGMITE
						|| t.Is(FeatureType.TROLL_CORPSE) || t.Is(FeatureType.TROLL_BLOODWITCH_CORPSE)
						|| t.Is(FeatureType.INACTIVE_TELEPORTAL)
						|| t.Is(TileType.FIRE_GEYSER) || t.Is(FeatureType.POISON_GAS)
						|| t.Is(FeatureType.FOG) || t.Is(FeatureType.FUNGUS)
						|| t.Is(FeatureType.FUNGUS_ACTIVE) || t.Is(FeatureType.FUNGUS_PRIMED)
						|| t.Is(TileType.FOG_VENT) || t.Is(TileType.HEALING_POOL)
						|| t.Is(TileType.POISON_GAS_VENT)
						|| t.IsShrine() || t.inv != null){
							if(CanSee(t)){
								interesting_targets.Add(t);
							}
						}
						if(lookmode && t.IsKnownTrap() && CanSee(t)){
							interesting_targets.AddUnique(t);
						}
					}
				}
			}
			colorchar[,] mem = new colorchar[ROWS,COLS];
			List<Tile> line = new List<Tile>();
			List<Tile> oldline = new List<Tile>();
			bool description_shown_last_time = false;
			int desc_row = -1;
			int desc_col = -1;
			int desc_height = -1;
			int desc_width = -1;
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					mem[i,j] = Screen.MapChar(i,j);
				}
			}
			if(!start_at_interesting_target || interesting_targets.Count == 0){
				if(lookmode){
					B.DisplayNow("Move the cursor to look around. ");
				}
				else{
					B.DisplayNow("Move cursor to choose target, then press Enter. ");
				}
			}
			if(lookmode){
				if(!start_at_interesting_target || interesting_targets.Count == 0){
					r = row;
					c = col;
					target_idx = -1;
				}
				else{
					r = interesting_targets[0].row;
					c = interesting_targets[0].col;
				}
			}
			else{
				if(target == null || !CanSee(target)
				|| (max_distance > 0 && DistanceFrom(target) > max_distance)){
					if(!start_at_interesting_target || interesting_targets.Count == 0){
						r = row;
						c = col;
						target_idx = -1;
					}
					else{
						r = interesting_targets[0].row;
						c = interesting_targets[0].col;
					}
				}
				else{
					r = target.row;
					c = target.col;
					if(Global.Option(OptionType.LAST_TARGET)){
						//return M.tile[r,c];
						List<Tile> bestline = GetBestExtendedLine(target);
						bestline = bestline.ToFirstSolidTile();
						if(bestline.Count > max_distance+1){
							bestline.RemoveRange(max_distance+1,bestline.Count - max_distance - 1);
						}
						return bestline;
					}
					target_idx = interesting_targets.IndexOf(target);
				}
			}
			bool first_iteration = true;
			bool done=false; //when done==true, we're ready to return 'result'
			while(!done){
				if(!done){ //i moved this around, thus this relic.
					Screen.ResetColors();
					string contents = "You see ";
					List<string> items = new List<string>();
					if(M.actor[r,c] != null && M.actor[r,c] != this && CanSee(M.actor[r,c])){
						items.Add(M.actor[r,c].a_name + " " + M.actor[r,c].WoundStatus());
					}
					if(M.tile[r,c].inv != null){
						items.Add(M.tile[r,c].inv.AName(true));
					}
					foreach(FeatureType f in M.tile[r,c].features){
						items.Add(Tile.Feature(f).a_name);
					}
					if(items.Count == 0){
						contents += M.tile[r,c].AName(true);
					}
					else{
						if(items.Count == 1){
							contents += items[0] + M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
						}
						else{
							if(items.Count == 2){
								if(M.tile[r,c].type != TileType.FLOOR){
									if(M.tile[r,c].Preposition() == " and "){
										contents += items[0] + ", " + items[1] + ",";
										contents += M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
									}
									else{
										contents += items[0] + " and " + items[1];
										contents += M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
									}
								}
								else{
									contents += items[0] + " and " + items[1];
								}
							}
							else{
								foreach(string s in items){
									if(s != items.Last()){
										contents += s + ", ";
									}
									else{
										if(M.tile[r,c].type != TileType.FLOOR){
											contents += s + ","; //because preposition contains a space already
										}
										else{
											contents += "and " + s;
										}
									}
								}
								if(M.tile[r,c].type != TileType.FLOOR){
									contents += M.tile[r,c].Preposition() + M.tile[r,c].AName(true);
								}
							}
						}
					}
					if(r == row && c == col){
						if(!first_iteration){
							string s = "You're standing here. ";
							if(items.Count == 0 && M.tile[r,c].type == TileType.FLOOR){
								B.DisplayNow(s);
							}
							else{
								B.DisplayNow(s + contents + " here. ");
							}
						}
					}
					else{
						if(CanSee(M.tile[r,c])){
							B.DisplayNow(contents + ". ");
						}
						else{
							if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
								B.DisplayNow("You sense " + M.actor[r,c].a_name + " " + M.actor[r,c].WoundStatus() + ". ");
							}
							else{
								if(M.tile[r,c].seen){
									colorchar tilech = new colorchar(M.tile[r,c].symbol,M.tile[r,c].color);
									colorchar screench = Screen.MapChar(r,c);
									if(M.tile[r,c].inv != null && (tilech.c != screench.c || tilech.color != screench.color)){ //hacky, but it seems to work (when a monster drops an item you haven't seen yet)
										B.DisplayNow("You can no longer see this " + M.tile[r,c].inv.Name(true) + ". ");
									}
									else{
										B.DisplayNow("You can no longer see this " + M.tile[r,c].Name(true) + ". ");
									}
								}
								else{
									if(lookmode){
										B.DisplayNow("");
									}
									else{
										B.DisplayNow("Move cursor to choose target, then press Enter. ");
									}
								}
							}
						}
					}
					if(!lookmode){
						bool blocked=false;
						Console.CursorVisible = false;
						line = GetBestLineOfEffect(r,c);
						//Tile last_good = tile();
						foreach(Tile t in line){
							if(t.row != row || t.col != col){
								colorchar cch = mem[t.row,t.col];
								if(t.row == r && t.col == c){
									if(!blocked){
										cch.bgcolor = Color.Green;
										if(Global.LINUX){ //no bright bg in terminals
											cch.bgcolor = Color.DarkGreen;
										}
										if(cch.color == cch.bgcolor){
											cch.color = Color.Black;
										}
										Screen.WriteMapChar(t.row,t.col,cch);
									}
									else{
										cch.bgcolor = Color.Red;
										if(Global.LINUX){
											cch.bgcolor = Color.DarkRed;
										}
										if(cch.color == cch.bgcolor){
											cch.color = Color.Black;
										}
										Screen.WriteMapChar(t.row,t.col,cch);
									}
								}
								else{
									if(!blocked){
										cch.bgcolor = Color.DarkGreen;
										if(cch.color == cch.bgcolor){
											cch.color = Color.Black;
										}
										Screen.WriteMapChar(t.row,t.col,cch);
									}
									else{
										cch.bgcolor = Color.DarkRed;
										if(cch.color == cch.bgcolor){
											cch.color = Color.Black;
										}
										Screen.WriteMapChar(t.row,t.col,cch);
									}
								}
								if(t.seen && !t.passable && (t.row != r || t.col != c)){
									blocked=true;
								}
							}
							oldline.Remove(t);
						}
						if(radius > 0){
							foreach(Tile t in M.tile[r,c].TilesWithinDistance(radius,true)){
								if(!line.Contains(t)){
									colorchar cch = mem[t.row,t.col];
									if(blocked){
										cch.bgcolor = Color.DarkRed;
									}
									else{
										cch.bgcolor = Color.DarkGreen;
									}
									if(cch.color == cch.bgcolor){
										cch.color = Color.Black;
									}
									Screen.WriteMapChar(t.row,t.col,cch);
									oldline.Remove(t);
								}
							}
						}
					}
					else{
						colorchar cch = mem[r,c];
						cch.bgcolor = Color.Green;
						if(Global.LINUX){ //no bright bg in terminals
							cch.bgcolor = Color.DarkGreen;
						}
						if(cch.color == cch.bgcolor){
							cch.color = Color.Black;
						}
						Screen.WriteMapChar(r,c,cch);
						line = new List<Tile>{M.tile[r,c]};
						oldline.Remove(M.tile[r,c]);
						if(!hide_descriptions && M.actor[r,c] != null && M.actor[r,c] != this && CanSee(M.actor[r,c])){
							bool description_on_right = false;
							int max_length = 29;
							if(c - 6 < max_length){
								max_length = c - 6;
							}
							if(max_length < 20){
								description_on_right = true;
								max_length = 29;
							}
							List<colorstring> desc = MonsterDescriptionBox(M.actor[r,c].type,max_length);
							if(description_on_right){
								int start_c = COLS - desc[0].Length();
								description_shown_last_time = true;
								desc_row = 0;
								desc_col = start_c;
								desc_height = desc.Count;
								desc_width = desc[0].Length();
								for(int i=0;i<desc.Count;++i){
									Screen.WriteMapString(i,start_c,desc[i]);
								}
							}
							else{
								description_shown_last_time = true;
								desc_row = 0;
								desc_col = 0;
								desc_height = desc.Count;
								desc_width = desc[0].Length();
								for(int i=0;i<desc.Count;++i){
									Screen.WriteMapString(i,0,desc[i]);
								}
							}
						}
						else{
							//description_shown_last_time = false;
						}
					}
					foreach(Tile t in oldline){
						Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
					}
					oldline = new List<Tile>(line);
					if(radius > 0){
						foreach(Tile t in M.tile[r,c].TilesWithinDistance(radius,true)){
							oldline.AddUnique(t);
						}
					}
					first_iteration = false;
					M.tile[r,c].Cursor();
				}
				Console.CursorVisible = true;
				command = Console.ReadKey(true);
				char ch = ConvertInput(command);
				ch = ConvertVIKeys(ch);
				int move_value = 1;
				if((command.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt
				|| (command.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control
				|| (command.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift){
					move_value = 10;
				}
				switch(ch){
				case '7':
					r -= move_value;
					c -= move_value;
					break;
				case '8':
					r -= move_value;
					break;
				case '9':
					r -= move_value;
					c += move_value;
					break;
				case '4':
					c -= move_value;
					break;
				case '6':
					c += move_value;
					break;
				case '1':
					r += move_value;
					c -= move_value;
					break;
				case '2':
					r += move_value;
					break;
				case '3':
					r += move_value;
					c += move_value;
					break;
				case (char)9:
					if(interesting_targets.Count > 0){
						target_idx++;
						if(target_idx == interesting_targets.Count){
							target_idx = 0;
						}
						r = interesting_targets[target_idx].row;
						c = interesting_targets[target_idx].col;
				//		interesting_targets[target_idx].Cursor();
					}
					break;
				case '=':
					if(lookmode){
						hide_descriptions = !hide_descriptions;
					}
					break;
				case (char)27:
				case ' ':
					done = true;
					break;
				case (char)13:
				case 's':
					if(r != row || c != col){
						if(HasBresenhamLineOfEffect(r,c)){
							if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
								target = M.actor[r,c];
							}
							//result = M.tile[r,c];
							if(radius == 0){
								result = GetBestExtendedLineOfEffect(r,c).ToFirstSolidTile();
								if(max_distance > 0 && result.Count > max_distance+1){
									result = result.GetRange(0,max_distance+1);
								}
							}
							else{
								bool nearby_actors = false;
								foreach(Actor a in M.tile[r,c].ActorsWithinDistance(radius)){
									if(a != this){
										nearby_actors = true;
										break;
									}
								}
								if(nearby_actors || allow_targeting_ground){
									result = GetBestLineOfEffect(r,c);
									if(max_distance > 0 && result.Count > max_distance+1){
										result = result.GetRange(0,max_distance+1);
									}
								}
								else{ //same as for radius 0
									result = GetBestExtendedLineOfEffect(r,c).ToFirstSolidTile();
									if(max_distance > 0 && result.Count > max_distance+1){
										result = result.GetRange(0,max_distance+1);
									}
								}
							}
						}
						else{
							//result = FirstSolidTileInLine(M.tile[r,c]);
							//result = M.tile[r,c];
							result = GetBestExtendedLineOfEffect(r,c).ToFirstSolidTile();
							if(max_distance > 0 && result.Count > max_distance+1){
								result = result.GetRange(0,max_distance+1);
							}
						}
						done = true;
					}
					else{
						bool nearby_actors = false;
						foreach(Actor a in ActorsWithinDistance(radius)){
							if(a != this){
								nearby_actors = true;
								break;
							}
						}
						if(nearby_actors){
							result = GetBestLineOfEffect(this);
							done = true;
						}
					}
					break;
				default:
					break;
				}
				if(r < minrow){
					r = minrow;
				}
				if(r > maxrow){
					r = maxrow;
				}
				if(c < mincol){
					c = mincol;
				}
				if(c > maxcol){
					c = maxcol;
				}
				if(description_shown_last_time){
					Screen.MapDrawWithStrings(mem,desc_row,desc_col,desc_height,desc_width);
					description_shown_last_time = false;
				}
				if(done){
					Console.CursorVisible = false;
					foreach(Tile t in line){
						Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
					}
					if(radius > 0){
						foreach(Tile t in M.tile[r,c].TilesWithinDistance(radius,true)){
							if(!line.Contains(t)){
								Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
							}
						}
					}
					Console.CursorVisible = true;
				}
			}
			return result;
		}*/
		public int SelectItem(string message){ return SelectItem(message,false); }
		public int SelectItem(string message,bool never_redraw_map){
			return Select(message,"".PadRight(COLS,'-').GetColorString(),
					("------Space left: " + (Global.MAX_INVENTORY_SIZE - InventoryCount()).ToString().PadRight(7,'-') + "[?] for help").PadRight(COLS,'-').GetColorString(),
					InventoryList().GetColorStrings(),false,false,true,never_redraw_map,true,HelpTopic.Items);
		}
		public int Select(string message,List<string> strings){ return Select(message,"".PadLeft(COLS,'-'),"".PadLeft(COLS,'-'),strings,false,false,true); }
		public int Select(string message,List<string> strings,bool no_ask,bool no_cancel,bool easy_cancel){ return Select(message,"".PadLeft(COLS,'-'),"".PadLeft(COLS,'-'),strings,no_ask,no_cancel,easy_cancel); }
		public int Select(string message,string top_border,List<string> strings){ return Select(message,top_border,"".PadLeft(COLS,'-'),strings,false,false,true); }
		public int Select(string message,string top_border,List<string> strings,bool no_ask,bool no_cancel,bool easy_cancel){ return Select(message,top_border,"".PadLeft(COLS,'-'),strings,no_ask,no_cancel,easy_cancel); }
		public int Select(string message,string top_border,string bottom_border,List<string> strings){ return Select(message,top_border,bottom_border,strings,false,false,true); }
		public int Select(string message,string top_border,string bottom_border,List<string> strings,bool no_ask,bool no_cancel,bool easy_cancel){
			Screen.WriteMapString(0,0,top_border);
			char letter = 'a';
			int i=1;
			foreach(string s in strings){
				string s2 = "[" + letter + "] " + s;
				Screen.WriteMapString(i,0,s2.PadRight(COLS));
				Screen.WriteMapChar(i,1,new colorchar(Color.Cyan,letter));
				letter++;
				i++;
			}
			Screen.WriteMapString(i,0,bottom_border);
			if(i < ROWS-1){
				Screen.WriteMapString(i+1,0,"".PadRight(COLS));
			}
			if(no_ask){
				B.DisplayNow(message);
				return -1;
			}
			else{
				int result = GetSelection(message,strings.Count,no_cancel,easy_cancel,false);
				if(result != -1){
					M.RedrawWithStrings(); //again, todo: why is this here? - i think it's as close as it's gonna get now.
				}
				return result;
			}
		} //todo: check how many things actually use the non-colorstring version of Select and consider removing it
		public int Select(string message,colorstring top_border,colorstring bottom_border,List<colorstring> strings,bool no_ask,bool no_cancel,bool easy_cancel,bool help_key,HelpTopic help_topic){ return Select(message,top_border,bottom_border,strings,no_ask,no_cancel,easy_cancel,false,help_key,help_topic); }
		public int Select(string message,colorstring top_border,colorstring bottom_border,List<colorstring> strings,bool no_ask,bool no_cancel,bool easy_cancel,bool never_redraw_map,bool help_key,HelpTopic help_topic){
			int result = -2;
			while(result == -2){
				Screen.WriteMapString(0,0,top_border);
				char letter = 'a';
				int i=1;
				foreach(colorstring s in strings){
					Screen.WriteMapString(i,0,new colorstring("[",Color.Gray,letter.ToString(),Color.Cyan,"] ",Color.Gray));
					Screen.WriteMapString(i,4,s);
					if(s.Length() < COLS-4){
						Screen.WriteMapString(i,s.Length()+4,"".PadRight(COLS - (s.Length()+4)));
					}
					letter++;
					i++;
				}
				Screen.WriteMapString(i,0,bottom_border);
				if(i < ROWS-1){
					Screen.WriteMapString(i+1,0,"".PadRight(COLS));
				}
				if(no_ask){
					B.DisplayNow(message);
					return -1;
				}
				else{
					result = GetSelection(message,strings.Count,no_cancel,easy_cancel,help_key);
					if(result == -2){
						Help.DisplayHelp(help_topic);
					}
					else{
						if(!never_redraw_map && result != -1){
							M.RedrawWithStrings();
						}
						return result;
					}
				}
			}
			return -1;
		}
		public int GetSelection(string s,int count,bool no_cancel,bool easy_cancel,bool help_key){
			//if(count == 0){ return -1; }
			B.DisplayNow(s);
			Console.CursorVisible = true;
			ConsoleKeyInfo command;
			char ch;
			while(true){
				command = Console.ReadKey(true);
				ch = ConvertInput(command);
				int i = ch - 'a';
				if(i >= 0 && i < count){
					return i;
				}
				if(help_key && ch == '?'){
					return -2;
				}
				if(no_cancel == false){
					if(easy_cancel){
						return -1;
					}
					if(ch == (char)27 || ch == ' '){
						return -1;
					}
				}
				if(count == 0){
					return -1;
				}
			}
		}
		public void AnimateProjectile(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateProjectile(GetBestLineOfEffect(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateMapCell(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateMapCell(o.row,o.col,new colorchar(color,c));
		}
		public void AnimateBoltProjectile(PhysicalObject o,Color color){
			B.DisplayNow();
			Screen.AnimateBoltProjectile(GetBestLineOfEffect(o.row,o.col),color);
		}
		public void AnimateExplosion(PhysicalObject o,int radius,Color color,char c){
			B.DisplayNow();
			Screen.AnimateExplosion(o,radius,new colorchar(color,c));
		}
		public void AnimateBeam(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateBeam(GetBestLineOfEffect(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateBoltBeam(PhysicalObject o,Color color){
			B.DisplayNow();
			Screen.AnimateBoltBeam(GetBestLineOfEffect(o.row,o.col),color);
		}
		//
		// i should have made them (char,color) from the start..
		//
		public void AnimateProjectile(PhysicalObject o,char c,Color color){
			B.DisplayNow();
			Screen.AnimateProjectile(GetBestLineOfEffect(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateMapCell(PhysicalObject o,char c,Color color){
			B.DisplayNow();
			Screen.AnimateMapCell(o.row,o.col,new colorchar(color,c));
		}
		public void AnimateExplosion(PhysicalObject o,int radius,char c,Color color){
			B.DisplayNow();
			Screen.AnimateExplosion(o,radius,new colorchar(color,c));
		}
		public void AnimateBeam(PhysicalObject o,char c,Color color){
			B.DisplayNow();
			Screen.AnimateBeam(GetBestLineOfEffect(o.row,o.col),new colorchar(color,c));
		}
		//from here forward, i'll just do (char,color)..
		public void AnimateStorm(int radius,int num_frames,int num_per_frame,char c,Color color){
			B.DisplayNow();
			Screen.AnimateStorm(p,radius,num_frames,num_per_frame,new colorchar(c,color));
		}
		public void AnimateProjectile(List<Tile> line,char c,Color color){
			B.DisplayNow();
			Screen.AnimateProjectile(line,new colorchar(color,c));
		}
		public void AnimateBeam(List<Tile> line,char c,Color color){
			B.DisplayNow();
			Screen.AnimateBeam(line,new colorchar(color,c));
		}
		public void AnimateBoltProjectile(List<Tile> line,Color color){
			B.DisplayNow();
			Screen.AnimateBoltProjectile(line,color);
		}
		public void AnimateBoltBeam(List<Tile> line,Color color){
			B.DisplayNow();
			Screen.AnimateBoltBeam(line,color);
		}
	}
	public static class Skill{
		public static string Name(SkillType type){
			switch(type){
			case SkillType.COMBAT:
				return "Combat";
			case SkillType.DEFENSE:
				return "Defense";
			case SkillType.MAGIC:
				return "Magic";
			case SkillType.SPIRIT:
				return "Spirit";
			case SkillType.STEALTH:
				return "Stealth";
			default:
				return "no skill";
			}
		}
	}
	public static class Feat{
		public static bool IsActivated(FeatType type){
			switch(type){
			case FeatType.LUNGE:
			case FeatType.TUMBLE:
			case FeatType.DISARM_TRAP:
				return true;
			case FeatType.QUICK_DRAW:
			case FeatType.ATTACK_EVERYTHING_TODO:
			case FeatType.DRIVE_BACK:
			case FeatType.CUNNING_DODGE:
			case FeatType.ARMOR_MASTERY:
			case FeatType.DEFLECT_ATTACK:
			case FeatType.MASTERS_EDGE:
			case FeatType.ARCANE_INTERFERENCE:
			case FeatType.CHAIN_CASTING:
			case FeatType.FORCE_OF_WILL:
			case FeatType.CONVICTION:
			case FeatType.ENDURING_SOUL:
			case FeatType.FEEL_NO_PAIN:
			case FeatType.BOILING_BLOOD:
			case FeatType.NECK_SNAP:
			case FeatType.CORNER_CLIMB:
			case FeatType.DANGER_SENSE:
			default:
				return false;
			}
		}
		public static FeatType OfSkill(SkillType skill,int num){ // 0 through 3
			switch(skill){
			case SkillType.COMBAT:
				return (FeatType)num;
			case SkillType.DEFENSE:
				return (FeatType)num+4;
			case SkillType.MAGIC:
				return (FeatType)num+8;
			case SkillType.SPIRIT:
				return (FeatType)num+12;
			case SkillType.STEALTH:
				return (FeatType)num+16;
			default:
				return FeatType.NO_FEAT;
			}
		}
		public static string Name(FeatType type){
			switch(type){
			case FeatType.CORNER_CLIMB:
				return "Corner climb";
			case FeatType.QUICK_DRAW:
				return "Quick draw";
			case FeatType.CUNNING_DODGE:
				return "Cunning dodge";
			case FeatType.DANGER_SENSE:
				return "Danger sense";
			case FeatType.DEFLECT_ATTACK:
				return "Deflect attack";
			case FeatType.ENDURING_SOUL:
				return "Enduring soul";
			case FeatType.NECK_SNAP:
				return "Neck snap";
			case FeatType.BOILING_BLOOD:
				return "Boiling blood";
			case FeatType.ATTACK_EVERYTHING_TODO:
				return "Attack everything TODO";
			case FeatType.LUNGE:
				return "Lunge";
			case FeatType.DRIVE_BACK:
				return "Drive back";
			case FeatType.ARMOR_MASTERY:
				return "Armor mastery";
			case FeatType.TUMBLE:
				return "Tumble";
			case FeatType.MASTERS_EDGE:
				return "Master's edge";
			case FeatType.ARCANE_INTERFERENCE:
				return "Arcane interference";
			case FeatType.CHAIN_CASTING:
				return "Chain absorb TODO";
			case FeatType.FORCE_OF_WILL:
				return "Force of will";
			case FeatType.CONVICTION:
				return "Conviction";
			case FeatType.FEEL_NO_PAIN:
				return "Feel no pain";
			case FeatType.DISARM_TRAP:
				return "Disarm trap";
			default:
				return "no feat";
			}
		}
		public static List<string> Description(FeatType type){
			switch(type){
			case FeatType.QUICK_DRAW:
				return new List<string>{
					"Wielding a different weapon takes no time.",
					"(This also enables you to fire arrows without first switching", //todo! redo these
					"to your bow.)"};
			case FeatType.ATTACK_EVERYTHING_TODO:
				return new List<string>{
					"DESCRIPTION GOES HERE."};
			case FeatType.LUNGE:
				return new List<string>{
					"Leap from one space away and attack your target (with a +4",
					"bonus to Combat). The intervening space must be unoccupied."};
			case FeatType.DRIVE_BACK:
				return new List<string>{
					"Enemies must yield ground in order to avoid your attacks.",
					"(If your target has nowhere to run, your attacks will",
					"automatically hit.)"};
			case FeatType.CUNNING_DODGE:
				return new List<string>{
					"DESCRIPTION GOES HERE."};
			case FeatType.ARMOR_MASTERY:
				return new List<string>{
					"DESCRIPTION GOES HERE."};
			case FeatType.DEFLECT_ATTACK:
				return new List<string>{
					"DESCRIPTION GOES HERE."};
				//"when an enemy attacks you, you might deflect the attack into an enemy adjacent to both of you."
			case FeatType.TUMBLE:
				return new List<string>{
					"Move up to 2 spaces while avoiding arrows. (Also useful for",
					"slipping behind enemies and putting out fires.)"};
			case FeatType.MASTERS_EDGE:
				return new List<string>{
					"The first offensive spell you've learned will deal 1d6 extra",
					"damage. (Affects the first spell in the list that deals damage",
					"directly.)"};
			case FeatType.ARCANE_INTERFERENCE:
				return new List<string>{
					"DESCRIPTION GOES HERE."};
			case FeatType.CHAIN_CASTING:
				return new List<string>{
					"DESCRIPTION GOES HERE."};
			case FeatType.FORCE_OF_WILL:
				return new List<string>{
					"Drain your magic reserves to flawlessly cast a spell. (The",
					"spell's level and any penalty from your armor are ignored. Any",
					"drain on your magic reserves still decreases your chances.)",
					"If you have skill in Spirit, your chances are increased."};
			case FeatType.CONVICTION:
				return new List<string>{
					"Each turn you're engaged in combat (attacking/being attacked),",
					"you gain 1 bonus Spirit, and bonus Combat skill equal to half",
					"that."};
			case FeatType.ENDURING_SOUL:
				return new List<string>{
					"Improves the amount by which your natural recovery can heal",
					"you. (You can recover to a multiple of 20HP instead of 10.)"};
			case FeatType.FEEL_NO_PAIN:
				return new List<string>{
					"When your health becomes very low (less than 20%), you",
					"briefly enter a state of invulnerability. (For about 5 turns,",
					"you'll be immune to damage, but not other effects.)"};
			case FeatType.BOILING_BLOOD:
				return new List<string>{
					"Taking damage briefly increases your movement speed. (This",
					"effect can stack up to 5 times. At 5 stacks, your speed is",
					"doubled.)"};
			case FeatType.CORNER_CLIMB:
				return new List<string>{
					"DESCRIPTION GOES HERE."};
			case FeatType.DISARM_TRAP:
				return new List<string>{
					"Attempt to disable a trap without setting it off. If you have",
					"skill in Defense, you might avoid damage if you do trigger it."};
			case FeatType.NECK_SNAP:
				return new List<string>{
					"Automatically perform a stealth kill when attacking an unaware",
					"medium humanoid. (Living enemies of approximately human size.)"};
			case FeatType.DANGER_SENSE:
				return new List<string>{
					"You can sense where it's safe and where enemies might detect",
					"you. Your torch must be extinguished while you're sneaking."};
			default:
				return null;
			}
		}
	}
}

