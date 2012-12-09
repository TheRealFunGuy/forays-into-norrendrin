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
namespace Forays{
	public class AttackInfo{
		public int cost;
		public Damage damage;
		public string desc;
		public AttackInfo(int cost_,int dice_,DamageType type_,string desc_){
			cost=cost_;
			damage.dice=dice_;
			damage.type=type_;
			damage.damclass=DamageClass.PHYSICAL;
			desc=desc_;
		}
		public AttackInfo(int cost_,int dice_,DamageType type_,DamageClass damclass_,string desc_){
			cost=cost_;
			damage.dice=dice_;
			damage.type=type_;
			damage.damclass=damclass_;
			desc=desc_;
		}
		public AttackInfo(AttackInfo a){
			cost=a.cost;
			damage = a.damage;
			desc=a.desc;
		}
	}
	public struct Damage{
		public int amount{ //amount isn't determined until you ask for it
			get{
				if(!num.HasValue){
					num = Global.Roll(dice,6);
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
		public Damage(int dice_,DamageType type_,DamageClass damclass_,Actor source_){
			dice=dice_;
			num = null;
			type=type_;
			damclass=damclass_;
			source=source_;
		}
		public Damage(DamageType type_,DamageClass damclass_,Actor source_,int totaldamage){
			dice=0;
			num=totaldamage;
			type=type_;
			damclass=damclass_;
			source=source_;
		}
	}
	public class Actor : PhysicalObject{
		public ActorType type{get;set;}
		public int maxhp{get;set;}
		public int curhp{get;set;}
		public int speed{get;set;}
		public int level{get;set;}
		//public int light_radius{get;set;} //inherited
		public Actor target{get;set;}
		public List<Item> inv{get;set;}
		public SpellType[] F{get;set;} //F[0] is the 'autospell' you cast instead of attacking, if that option is set
		public Dict<AttrType,int> attrs = new Dict<AttrType,int>();
		public Dict<SkillType,int> skills = new Dict<SkillType,int>();
		public Dict<FeatType,int> feats = new Dict<FeatType,int>();
		public Dict<SpellType,int> spells = new Dict<SpellType,int>(); //change to bool? todo
		public int magic_penalty;
		public int time_of_last_action;
		public int recover_time;
		public List<pos> path = new List<pos>();
		public Tile target_location;
		public int player_visibility_duration;
		public List<Actor> group = null;
		public LinkedList<WeaponType> weapons = new LinkedList<WeaponType>();
		public LinkedList<ArmorType> armors = new LinkedList<ArmorType>();
		public LinkedList<MagicItemType> magic_items = new LinkedList<MagicItemType>();
		
		public static string player_name;
		public static List<FeatType> feats_in_order = null;
		public static List<FeatType> partial_feats_in_order = null;
		public static List<SpellType> spells_in_order = null; //used only for keeping track of the order in which feats/spells were learned by the player
		public static List<Actor> tiebreakers = null; //a list of all actors on this level. used to determine sub-turn order of events
		public static AttackInfo[] attack = new AttackInfo[20];
		private static Dict<ActorType,Actor> proto = new Dict<ActorType, Actor>();
		public static Actor Prototype(ActorType type){ return proto[type]; }
		private const int ROWS = Global.ROWS;
		private const int COLS = Global.COLS;
		//public static Map M{get;set;} //inherited
		public static Queue Q{get;set;}
		public static Buffer B{get;set;}
		public static Actor player{get;set;}
		static Actor(){
			Define(ActorType.RAT,"rat",'r',Color.DarkGray,15,90,1,0,AttrType.LOW_LIGHT_VISION,AttrType.SMALL,AttrType.KEEN_SENSES);
			Define(ActorType.GOBLIN,"goblin",'g',Color.Green,25,100,1,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.LARGE_BAT,"large bat",'b',Color.DarkGray,20,60,1,0,AttrType.DARKVISION,AttrType.FLYING,AttrType.SMALL,AttrType.KEEN_SENSES,AttrType.BLINDSIGHT);
			Define(ActorType.WOLF,"wolf",'c',Color.DarkYellow,25,50,1,0,AttrType.LOW_LIGHT_VISION,AttrType.KEEN_SENSES);
			Define(ActorType.SKELETON,"skeleton",'s',Color.White,30,100,1,0,AttrType.UNDEAD,AttrType.RESIST_SLASH,AttrType.RESIST_PIERCE,AttrType.RESIST_FIRE,AttrType.RESIST_COLD,AttrType.RESIST_ELECTRICITY,AttrType.DARKVISION);
			Define(ActorType.BLOOD_MOTH,"blood moth",'i',Color.Red,25,100,1,0,AttrType.FLYING);
			//Define(ActorType.SHAMBLING_SCARECROW,"shambling scarecrow",'x',Color.DarkYellow,30,90,0,1,0,AttrType.CONSTRUCT,AttrType.RESIST_BASH,AttrType.RESIST_PIERCE,AttrType.IMMUNE_ARROWS,AttrType.DARKVISION);
			Define(ActorType.SWORDSMAN,"swordsman",'p',Color.White,35,100,2,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.DARKNESS_DWELLER,"darkness dweller",'h',Color.DarkGreen,45,100,2,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.DARKVISION);
			Define(ActorType.CARNIVOROUS_BRAMBLE,"carnivorous bramble",'B',Color.DarkYellow,35,100,2,0,AttrType.PLANTLIKE,AttrType.NEVER_MOVES,AttrType.BLINDSIGHT);
			Define(ActorType.FROSTLING,"frostling",'E',Color.Gray,35,100,2,0,AttrType.IMMUNE_COLD,AttrType.COLD_HIT);
			Define(ActorType.DREAM_WARRIOR,"dream warrior",'p',Color.Cyan,40,100,2,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.DREAM_CLONE,"dream warrior",'p',Color.Cyan,1,100,0,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.CONSTRUCT,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.CULTIST,"cultist",'p',Color.DarkRed,35,100,3,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SMALL_GROUP);
			Define(ActorType.GOBLIN_ARCHER,"goblin archer",'g',Color.DarkCyan,25,100,3,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.GOBLIN_SHAMAN,"goblin shaman",'g',Color.Magenta,25,100,3,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Prototype(ActorType.GOBLIN_SHAMAN).GainSpell(SpellType.FORCE_PALM,SpellType.IMMOLATE,SpellType.SCORCH);
			Prototype(ActorType.GOBLIN_SHAMAN).skills[SkillType.MAGIC] = 4;
			Define(ActorType.MIMIC,"mimic",'m',Color.White,30,200,3,0,AttrType.GRAB_HIT);
			Define(ActorType.SKULKING_KILLER,"skulking killer",'p',Color.DarkBlue,35,100,3,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.STEALTHY,AttrType.LOW_LIGHT_VISION);
			Prototype(ActorType.SKULKING_KILLER).skills[SkillType.STEALTH] = 4;
			Define(ActorType.ZOMBIE,"zombie",'z',Color.DarkGray,50,150,4,0,AttrType.UNDEAD,AttrType.MEDIUM_HUMANOID,AttrType.RESIST_NECK_SNAP,AttrType.RESIST_PIERCE,AttrType.RESIST_COLD);
			Define(ActorType.DIRE_RAT,"dire rat",'r',Color.DarkRed,25,90,4,0,AttrType.LOW_LIGHT_VISION,AttrType.LARGE_GROUP,AttrType.SMALL,AttrType.KEEN_SENSES);
			Define(ActorType.ROBED_ZEALOT,"robed zealot",'p',Color.Yellow,40,100,4,6,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Prototype(ActorType.ROBED_ZEALOT).GainSpell(SpellType.MINOR_HEAL,SpellType.BLESS,SpellType.HOLY_SHIELD);
			Prototype(ActorType.ROBED_ZEALOT).skills[SkillType.MAGIC] = 6;
			Define(ActorType.SHADOW,"shadow",'G',Color.DarkGray,40,100,4,0,AttrType.UNDEAD,AttrType.RESIST_COLD,AttrType.DARKVISION,AttrType.SHADOW_CLOAK);
			Define(ActorType.BANSHEE,"banshee",'G',Color.Magenta,40,80,4,0,AttrType.UNDEAD,AttrType.RESIST_COLD,AttrType.LOW_LIGHT_VISION,AttrType.FLYING);
			Define(ActorType.WARG,"warg",'c',Color.White,30,50,5,0,AttrType.LOW_LIGHT_VISION,AttrType.MEDIUM_GROUP,AttrType.KEEN_SENSES);
			Define(ActorType.PHASE_SPIDER,"phase spider",'A',Color.Cyan,45,100,5,0,AttrType.POISON_HIT,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.DERANGED_ASCETIC,"deranged ascetic",'p',Color.RandomDark,40,100,5,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SPELL_DISRUPTION);
			Define(ActorType.POLTERGEIST,"poltergeist",'G',Color.DarkGreen,35,100,5,0,AttrType.UNDEAD,AttrType.RESIST_COLD,AttrType.LOW_LIGHT_VISION,AttrType.SMALL,AttrType.FLYING);
			Define(ActorType.CAVERN_HAG,"cavern hag",'h',Color.Blue,40,100,5,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.COMPY,"compy",'l',Color.Green,25,100,6,0,AttrType.SMALL,AttrType.LARGE_GROUP,AttrType.KEEN_SENSES);
			Define(ActorType.NOXIOUS_WORM,"noxious worm",'w',Color.DarkMagenta,55,140,6,0,AttrType.RESIST_BASH,AttrType.IMMUNE_TOXINS);
			Define(ActorType.BERSERKER,"berserker",'p',Color.Red,40,100,6,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.TROLL,"troll",'T',Color.DarkGreen,50,100,6,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.REGENERATING,AttrType.REGENERATES_FROM_DEATH,AttrType.DARKVISION);
			Define(ActorType.VAMPIRE,"vampire",'V',Color.Blue,40,100,6,0,AttrType.UNDEAD,AttrType.MEDIUM_HUMANOID,AttrType.RESIST_NECK_SNAP,AttrType.FLYING,AttrType.LIGHT_ALLERGY,AttrType.DESTROYED_BY_SUNLIGHT,AttrType.LIFE_DRAIN_HIT,AttrType.RESIST_COLD);
			Define(ActorType.CRUSADING_KNIGHT,"crusading knight",'p',Color.DarkGray,45,100,7,6,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Prototype(ActorType.CRUSADING_KNIGHT).skills[Forays.SkillType.DEFENSE] = 7;
			Define(ActorType.SKELETAL_SABERTOOTH,"skeletal sabertooth",'f',Color.White,40,50,7,0,AttrType.UNDEAD,AttrType.RESIST_SLASH,AttrType.RESIST_PIERCE,AttrType.RESIST_FIRE,AttrType.RESIST_COLD,AttrType.RESIST_ELECTRICITY,AttrType.DARKVISION,AttrType.KEEN_SENSES);
			Define(ActorType.MUD_ELEMENTAL,"mud elemental",'E',Color.DarkYellow,35,100,7,0,AttrType.RESIST_BASH,AttrType.RESIST_SLASH,AttrType.RESIST_PIERCE,AttrType.IMMUNE_TOXINS,AttrType.IMMUNE_ARROWS);
			Define(ActorType.MUD_TENTACLE,"mud tentacle",'~',Color.DarkYellow,1,100,0,0,AttrType.CONSTRUCT,AttrType.BLINDSIGHT,AttrType.GRAB_HIT,AttrType.NEVER_MOVES,AttrType.IMMUNE_TOXINS);
			Define(ActorType.ENTRANCER,"entrancer",'p',Color.DarkMagenta,35,100,7,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.MARBLE_HORROR,"marble horror",'&',Color.Gray,45,100,7,0,AttrType.CONSTRUCT,AttrType.DARKVISION,AttrType.DIM_VISION_HIT,AttrType.IMMUNE_TOXINS,AttrType.DESTROYED_BY_SUNLIGHT);
			Define(ActorType.OGRE,"ogre",'O',Color.Green,55,100,8,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.DARKVISION,AttrType.SMALL_GROUP);
			Define(ActorType.ORC_GRENADIER,"orc grenadier",'o',Color.DarkYellow,50,100,8,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.SHADOWVEIL_DUELIST,"shadowveil duelist",'p',Color.DarkCyan,40,100,8,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SHADOW_CLOAK);
			Define(ActorType.CARRION_CRAWLER,"carrion crawler",'i',Color.DarkGreen,35,100,8,0,AttrType.PARALYSIS_HIT,AttrType.DARKVISION);
			Define(ActorType.SPELLMUDDLE_PIXIE,"spellmuddle pixie",'y',Color.RandomBright,35,50,8,0,AttrType.SMALL,AttrType.FLYING,AttrType.SPELL_DISRUPTION);
			Define(ActorType.STONE_GOLEM,"stone golem",'x',Color.Gray,65,120,9,0,AttrType.CONSTRUCT,AttrType.STALAGMITE_HIT,AttrType.RESIST_SLASH,AttrType.RESIST_PIERCE,AttrType.RESIST_FIRE,AttrType.RESIST_COLD,AttrType.RESIST_ELECTRICITY,AttrType.DARKVISION);
			Define(ActorType.PYREN_ARCHER,"pyren archer",'P',Color.DarkRed,55,100,9,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.FIRE_HIT,AttrType.FIERY_ARROWS,AttrType.RESIST_FIRE);
			Define(ActorType.ORC_ASSASSIN,"orc assassin",'o',Color.DarkBlue,50,100,9,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.STEALTHY);
			Prototype(ActorType.ORC_ASSASSIN).skills[Forays.SkillType.STEALTH] = 9;
			Define(ActorType.TROLL_SEER,"troll seer",'T',Color.Cyan,50,100,9,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.REGENERATING,AttrType.REGENERATES_FROM_DEATH,AttrType.DARKVISION);
			Prototype(ActorType.TROLL_SEER).GainSpell(SpellType.GLACIAL_BLAST,SpellType.SONIC_BOOM);
			Prototype(ActorType.TROLL_SEER).skills[SkillType.MAGIC] = 9;
			Define(ActorType.MECHANICAL_KNIGHT,"mechanical knight",'x',Color.DarkRed,20,100,9,0,AttrType.CONSTRUCT,AttrType.MECHANICAL_SHIELD,AttrType.KEEN_SENSES,AttrType.BLINDSIGHT);
			Define(ActorType.ORC_WARMAGE,"orc warmage",'o',Color.Red,50,100,10,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Prototype(ActorType.ORC_WARMAGE).GainSpell(SpellType.FORCE_BEAM,SpellType.IMMOLATE,SpellType.VOLTAIC_SURGE,SpellType.MAGIC_HAMMER,SpellType.GLACIAL_BLAST,SpellType.BLOODSCENT,SpellType.PASSAGE);
			Prototype(ActorType.ORC_WARMAGE).skills[SkillType.MAGIC] = 10;
			Define(ActorType.LASHER_FUNGUS,"lasher fungus",'F',Color.DarkGreen,50,100,10,0,AttrType.PLANTLIKE,AttrType.SPORE_BURST,AttrType.RESIST_BASH,AttrType.RESIST_FIRE,AttrType.DARKVISION,AttrType.BLINDSIGHT,AttrType.NEVER_MOVES);
			Define(ActorType.NECROMANCER,"necromancer",'p',Color.Blue,40,100,10,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.LUMINOUS_AVENGER,"luminous avenger",'E',Color.Yellow,40,50,10,12,AttrType.HOLY_SHIELDED);
			Define(ActorType.CORPSETOWER_BEHEMOTH,"corpsetower behemoth",'z',Color.DarkMagenta,75,120,10,0,AttrType.UNDEAD,AttrType.TOUGH,AttrType.REGENERATING,AttrType.RESIST_COLD,AttrType.STUN_HIT);
			Define(ActorType.FIRE_DRAKE,"fire drake",'D',Color.DarkRed,200,50,10,2,AttrType.BOSS_MONSTER,AttrType.DARKVISION,AttrType.FIRE_HIT,AttrType.IMMUNE_FIRE,AttrType.HUMANOID_INTELLIGENCE);
			Define(ActorType.PHANTOM,"phantom",'?',Color.Cyan,1,100,0,0,AttrType.CONSTRUCT,AttrType.FLYING,AttrType.IMMUNE_TOXINS); //the template on which the different types of phantoms are based
		}
		private static void Define(ActorType type_,string name_,char symbol_,Color color_,int maxhp_,int speed_,int level_,int light_radius_,params AttrType[] attrlist){
			proto[type_] = new Actor(type_,name_,symbol_,color_,maxhp_,speed_,level_,light_radius_,attrlist);
		}
		public Actor(){
			F = new SpellType[13];
			inv = new List<Item>();
			weapons = new LinkedList<WeaponType>();
			armors = new LinkedList<ArmorType>();
			magic_items = new LinkedList<MagicItemType>();
			attrs = new Dict<AttrType, int>();
			skills = new Dict<SkillType,int>();
			feats = new Dict<FeatType,int>();
			spells = new Dict<SpellType,int>();
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
			speed = a.speed;
			level = a.level;
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
			weapons = new LinkedList<WeaponType>(a.weapons);
			armors = new LinkedList<ArmorType>(a.armors);
			magic_items = new LinkedList<MagicItemType>(a.magic_items);
			attrs = new Dict<AttrType, int>(a.attrs);
			skills = new Dict<SkillType,int>(a.skills);
			feats = new Dict<FeatType,int>(a.feats);
			spells = new Dict<SpellType,int>(a.spells);
			magic_penalty = 0;
		}
		public Actor(ActorType type_,string name_,char symbol_,Color color_,int maxhp_,int speed_,int level_,int light_radius_,params AttrType[] attrlist){
			type = type_;
			SetName(name_);
			symbol = symbol_;
			color = color_;
			maxhp = maxhp_;
			curhp = maxhp;
			speed = speed_;
			level = level_;
			light_radius = light_radius_;
			target = null;
			inv = null;
			target_location = null;
			time_of_last_action = 0;
			recover_time = 0;
			player_visibility_duration = 0;
			weapons.AddFirst(WeaponType.NO_WEAPON);
			armors.AddFirst(ArmorType.NO_ARMOR);
			F = new SpellType[13];
			for(int i=0;i<13;++i){
				F[i] = SpellType.NO_SPELL;
			}
			magic_penalty = 0;
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
						Event e = new Event(a,a.speed,EventType.MOVE);
						e.tiebreaker = Q.Tiebreaker + 1;
						Q.Add(e);
					}
					else{
						tiebreakers.Add(a);
						Event e = new Event(a,a.speed,EventType.MOVE);
						e.tiebreaker = tiebreakers.Count - 1; //since it's the last one
						Q.Add(e);
					}
				}
				else{
					a.QS();
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
			ActorType type = (ActorType)(Global.Roll(9) + (int)ActorType.PHANTOM);
			a.type = type;
			switch(type){
			case ActorType.PHANTOM_ARCHER:
				a.SetName("phantom archer");
				a.symbol = 'g';
				break;
			case ActorType.PHANTOM_BEHEMOTH:
				a.SetName("phantom behemoth");
				a.symbol = 'H';
				a.speed = 120;
				a.attrs[AttrType.STUN_HIT]++;
				break;
			case ActorType.PHANTOM_BLIGHTWING:
				a.SetName("phantom blightwing");
				a.symbol = 'b';
				a.speed = 60;
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
		override public string TheVisible(){ //returns the_name or "something"
			if(player.CanSee(this)){
				return the_name;
			}
			else{
				return "something";
			}
		}
		override public string AVisible(){ //returns a_name or "something"
			if(player.CanSee(this)){
				return a_name;
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
					return TheVisible() + " " + s + "es";
				}
				else{
					return TheVisible() + " " + s + "s";
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
				if(M.actor[r,c] == null){
					if(HasAttr(AttrType.GRABBED)){
						foreach(Actor a in ActorsAtDistance(1)){
							if(a.attrs[Forays.AttrType.GRABBING] == a.DirectionOf(this)){
								if(a.DistanceFrom(r,c) > 1){
									attrs[Forays.AttrType.GRABBED]--;
									a.attrs[Forays.AttrType.GRABBING] = 0;
								}
								else{
									a.attrs[Forays.AttrType.GRABBING] = a.DirectionOf(new pos(r,c));
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
							M.tile[row,col].inv.ignored = true;
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
		}
		public bool GrabPreventsMovement(PhysicalObject o){
			if(!HasAttr(AttrType.GRABBED) || DistanceFrom(o) > 1){
				return false;
			}
			List<Actor> grabbers = new List<Actor>();
			foreach(Actor a in ActorsAtDistance(1)){
				if(a.attrs[Forays.AttrType.GRABBING] == a.DirectionOf(this)){
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
		public bool HasAttr(AttrType attr){ return attrs[attr] > 0; }
		public bool HasFeat(FeatType feat){ return feats[feat] > 0; }
		public bool HasSpell(SpellType spell){ return spells[spell] > 0; }
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
			Q.KillEvents(this,attr);
			attrs[attr]++;
			Q.Add(new Event(this,duration,attr,attrs[attr]));
		}
		public void GainAttrRefreshDuration(AttrType attr,int duration,string msg,params PhysicalObject[] objs){
			Q.KillEvents(this,attr);
			attrs[attr]++;
			Q.Add(new Event(this,duration,attr,attrs[attr],msg,objs));
		}
		public void GainSpell(params SpellType[] spell_list){
			foreach(SpellType spell in spell_list){
				spells[spell]++;
			}
		}
		public int LightRadius(){ return Math.Max(light_radius,attrs[AttrType.ON_FIRE]); }
		public int ArmorClass(){
			int total = TotalSkill(SkillType.DEFENSE);
			if(weapons.First.Value == WeaponType.STAFF || weapons.First.Value == WeaponType.STAFF_OF_MAGIC){
				total++;
			}
			if(magic_items.Contains(MagicItemType.RING_OF_PROTECTION)){
				total++;
			}
			total += Armor.Protection(armors.First.Value);
			return total;
		}
		public int Stealth(){ return Stealth(row,col); }
		public int Stealth(int r,int c){ //this method should probably become part of TotalSkill
			if(LightRadius() > 0){
				return 0; //negative stealth is the same as zero stealth
			}
			int total = TotalSkill(SkillType.STEALTH);
			if(!M.tile[r,c].IsLit()){
				if(type == ActorType.PLAYER || !player.HasAttr(AttrType.SHADOWSIGHT)){ //+2 stealth while in darkness unless shadowsight is in effect
					total += 2;
				}
			}
			if(!HasFeat(FeatType.SILENT_CHAINMAIL) || Armor.BaseArmor(armors.First.Value) != ArmorType.CHAINMAIL){
				total -= Armor.StealthPenalty(armors.First.Value);
			}
			return total;
		}
		public int TotalSkill(SkillType skill){
			int result = skills[skill];
			switch(skill){
			case SkillType.COMBAT:
				result += attrs[AttrType.BONUS_COMBAT];
				break;
			case SkillType.DEFENSE:
				result += attrs[AttrType.BONUS_DEFENSE];
				break;
			case SkillType.MAGIC:
				result += attrs[AttrType.BONUS_MAGIC];
				break;
			case SkillType.SPIRIT:
				result += attrs[AttrType.BONUS_SPIRIT];
				break;
			case SkillType.STEALTH:
				result += attrs[AttrType.BONUS_STEALTH];
				break;
			}
			return result;
		}
		public string WoundStatus(){
			if(type == ActorType.DREAM_CLONE){
				if(group != null && group.Count > 0){
					foreach(Actor a in group){
						if(a.type == ActorType.DREAM_WARRIOR){
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
									if(HasAttr(AttrType.UNDEAD) || HasAttr(AttrType.CONSTRUCT)){
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
		public int DurationOfMagicalEffect(int original){ //intended to be used with whole turns, i.e. numbers below 50.
			int diff = (original * TotalSkill(SkillType.SPIRIT)) / 20; //each point of Spirit takes off 1/20th of the duration
			int result = original - diff; //therefore, maxed Spirit cuts durations in half
			if(result < 1){
				result = 1; //no negative turncounts please
			}
			return result;
		}
		public bool CanWander(){
			switch(type){
			case ActorType.LARGE_BAT:
			case ActorType.BLOOD_MOTH:
			case ActorType.SKELETON:
			case ActorType.CARNIVOROUS_BRAMBLE:
			case ActorType.MIMIC:
			case ActorType.PHASE_SPIDER:
			case ActorType.POLTERGEIST:
			case ActorType.VAMPIRE:
			case ActorType.SKELETAL_SABERTOOTH:
			case ActorType.MARBLE_HORROR:
			case ActorType.STONE_GOLEM:
			case ActorType.LASHER_FUNGUS:
			case ActorType.PLAYER:
			case ActorType.FIRE_DRAKE:
				return false;
			default:
				return true;
			}
		}
		public bool AlwaysWanders(){
			switch(type){
			case ActorType.SKULKING_KILLER:
			case ActorType.COMPY:
			case ActorType.ENTRANCER:
			case ActorType.SHADOWVEIL_DUELIST:
			case ActorType.ORC_ASSASSIN:
				return true;
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
			|| type == ActorType.RAT || type == ActorType.DREAM_CLONE){
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
			Q.Add(new Event(this,speed));
		}
		public override string ToString(){ return symbol.ToString(); }
		public void Input(){
			bool skip_input = false;
			if(HasAttr(AttrType.DESTROYED_BY_SUNLIGHT)){
				if(M.wiz_lite || (player.HasAttr(AttrType.ENHANCED_TORCH) && DistanceFrom(player) <= player.light_radius
									&& player.HasBresenhamLine(row,col))){
					B.Add(You("turn") + " to dust! ",this);
					TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,9999,null);
					return;
				}
			}
			if(type == ActorType.MUD_TENTACLE){
				attrs[Forays.AttrType.COOLDOWN_1]--;
				if(attrs[Forays.AttrType.COOLDOWN_1] < 0){
					TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,9999,null);
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
				int old_magic_penalty = magic_penalty;
				int old_resting_status = attrs[Forays.AttrType.RESTING];
				M.GenerateBossLevel(drake_on_next_level);
				magic_penalty = old_magic_penalty; //falling to a new level doesn't let you rest again during the boss fight
				attrs[Forays.AttrType.RESTING] = old_resting_status;
				Q0();
				return;
			}
			if(type == ActorType.FIRE_DRAKE && tile().Is(TileType.CHASM)){
				if(player.tile().type == TileType.CHASM){
					B.Add("You fall. ");
					B.PrintAll();
					int old_magic_penalty = player.magic_penalty;
					int old_resting_status = player.attrs[Forays.AttrType.RESTING];
					M.GenerateBossLevel(true);
					player.magic_penalty = old_magic_penalty; //falling to a new level doesn't let you rest again during the boss fight
					player.attrs[Forays.AttrType.RESTING] = old_resting_status;
					return;
				}
				else{
					if(player.CanSee(this)){
						B.Add(the_name + " drops to the next level. ");
					}
					else{
						B.Add("You hear a crash as " + the_name + " drops to the next level. ");
					}
					Q.Add(new Event(null,null,(Global.Roll(20)+50)*100,EventType.BOSS_ARRIVE,AttrType.NO_ATTR,curhp,""));
					attrs[Forays.AttrType.BOSS_MONSTER] = 0;
					TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,9999,null);
					return;
				}
			}
			if(HasAttr(AttrType.AGGRAVATING)){ //this probably wouldn't work well for anyone but the player, yet.
				foreach(Actor a in ActorsWithinDistance(12)){
					a.player_visibility_duration = -1;
					a.attrs[AttrType.PLAYER_NOTICED] = 1;
					if(a.HasLOS(this)){
						a.target_location = tile();
					}
					else{
						a.FindPath(this);
					}
				}
			}
			if(HasAttr(AttrType.DEFENSIVE_STANCE)){
				attrs[AttrType.DEFENSIVE_STANCE] = 0;
			}
			if(HasAttr(AttrType.IN_COMBAT)){
				attrs[Forays.AttrType.IN_COMBAT] = 0;
				if(HasFeat(FeatType.CONVICTION)){
					GainAttrRefreshDuration(AttrType.CONVICTION,Math.Max(speed,100));
					attrs[Forays.AttrType.BONUS_SPIRIT]++;
					if(attrs[Forays.AttrType.CONVICTION] % 2 == 0){
						attrs[Forays.AttrType.BONUS_COMBAT]++;
					}
				}
			}
			else{
				if(HasAttr(AttrType.MAGICAL_DROWSINESS) && !HasAttr(AttrType.ASLEEP) && Global.OneIn(4) && time_of_last_action < Q.turn){
					B.Add(You("fall") + " asleep. ",this);
					int duration = 4 + Global.Roll(2);
					attrs[Forays.AttrType.ASLEEP] = DurationOfMagicalEffect(duration);
				}
			}
			if(HasAttr(AttrType.TELEPORTING) && time_of_last_action < Q.turn){
				attrs[AttrType.TELEPORTING]--;
				if(!HasAttr(AttrType.TELEPORTING)){
					for(int i=0;i<9999;++i){
						int rr = Global.Roll(1,Global.ROWS-2);
						int rc = Global.Roll(1,Global.COLS-2);
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
					attrs[AttrType.TELEPORTING] = Global.Roll(2,10) + 5;
				}
			}
			if(HasAttr(AttrType.ASLEEP)){
				attrs[AttrType.ASLEEP]--;
				Global.FlushInput();
				if(!HasAttr(AttrType.ASLEEP)){
					B.Add(You("wake") + " up. ",this);
				}
				if(type != ActorType.PLAYER){
					Q1();
					skip_input = true;
				}
			}
			if(HasAttr(AttrType.PARALYZED)){
				attrs[AttrType.PARALYZED]--;
				if(type == ActorType.PLAYER){
					B.AddDependingOnLastPartialMessage("You can't move! ");
				}
				else{ //handled differently for the player: since the map still needs to be drawn,
					B.Add(the_name + " can't move! ",this);
					Q1();						// this is handled in InputHuman().
					skip_input = true; //the message is still printed, of course.
				}
			}
			if(HasAttr(AttrType.AMNESIA_STUN)){
				attrs[Forays.AttrType.AMNESIA_STUN] = 0;
				Q1();
				skip_input = true;
			}
			if(HasAttr(AttrType.FROZEN)){
				if(type != ActorType.PLAYER){
					int damage = Global.Roll(AttackList.Attack(type,0).damage.dice,6) + TotalSkill(SkillType.COMBAT);
					attrs[Forays.AttrType.FROZEN] -= damage;
					if(attrs[Forays.AttrType.FROZEN] < 0){
						attrs[Forays.AttrType.FROZEN] = 0;
					}
					if(HasAttr(AttrType.FROZEN)){
						B.Add(the_name + " attempts to break free. ",this);
					}
					else{
						B.Add(the_name + " breaks free! ",this);
					}
					Q1();
					skip_input = true;
				}
			}
			if(HasAttr(AttrType.AFRAID) && !HasAttr(AttrType.FROZEN) && !HasAttr(AttrType.PARALYZED)){
				Actor banshee = null;
				int dist = 100;
				foreach(Actor a in M.AllActors()){
					if(a.type == ActorType.BANSHEE && DistanceFrom(a) < dist && HasLOS(a.row,a.col)){
						banshee = a;
						dist = DistanceFrom(a);
					}
				}
				if(type == ActorType.PLAYER){
					if(banshee != null){
						B.AddDependingOnLastPartialMessage("You flee. ");
						AI_Step(banshee,true);
					}
					else{
						B.AddDependingOnLastPartialMessage("You feel unsettled. ");
					}
				}
				else{ //same story
					if(banshee != null){
						B.Add(You("flee") + ". ",this);
						AI_Step(banshee,true);
					}
					else{
						B.Add(YouFeel() + " unsettled. ",this);
					}
					Q1();
					skip_input = true;
				}
			}
			if(curhp < maxhp){
				if(HasAttr(AttrType.REGENERATING) && time_of_last_action < Q.turn){
					curhp += attrs[AttrType.REGENERATING];
					if(curhp > maxhp){
						curhp = maxhp;
					}
					B.Add(You("regenerate") + ". ",this);
				}
				else{
					int hplimit = 10;
					if(HasFeat(FeatType.ENDURING_SOUL)){ //the feat lets you heal to an even 20
						hplimit = 20;
					}
					if(recover_time <= Q.turn && curhp % hplimit != 0){
						if(HasAttr(AttrType.MAGICAL_BLOOD)){
							recover_time = Q.turn + 100;
						}
						else{
							recover_time = Q.turn + 500;
						}
						curhp++;
					}
				}
					
			}
			if((HasAttr(AttrType.POISONED) || tile().Is(FeatureType.POISON_GAS)) && time_of_last_action < Q.turn){
				int strength = attrs[Forays.AttrType.POISONED];
				if(tile().Is(FeatureType.POISON_GAS) && strength < 3){
					strength = 3;
				}
				if(!TakeDamage(DamageType.POISON,DamageClass.NO_TYPE,Global.Roll(strength+2)-1,null,"*succumbed to poison")){
					return;
				}
			}
			if(HasAttr(AttrType.ON_FIRE) && time_of_last_action < Q.turn){
				if(type == ActorType.CORPSETOWER_BEHEMOTH){
					B.Add(the_name + " burns slowly. ",this);
				}
				else{
					B.Add(YouAre() + " on fire! ",this);
				}
				if(!TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,Global.Roll(attrs[AttrType.ON_FIRE],6),null,"*burned to death")){
					return;
				}
			}
			if(HasAttr(AttrType.LIGHT_ALLERGY) && tile().IsLit() && time_of_last_action < Q.turn){
				if(type == ActorType.PLAYER){
					B.Add("The light burns you! ");
				}
				else{
					B.Add("The light burns " + the_name + ". ",this);
				}
				if(!TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,Global.Roll(6),null,"*shriveled in the light")){
					return;
				}
			}
			if(HasAttr(AttrType.COMPY_POISON_LETHAL) && this == player && time_of_last_action < Q.turn){
				if(attrs[Forays.AttrType.COMPY_POISON_LETHAL] == 2){
					if(attrs[Forays.AttrType.COMPY_POISON_COUNTER] >= curhp && !HasAttr(AttrType.IMMUNE_TOXINS)){
						B.Add("You can't resist the poison any longer. ");
						B.Add("You lose consciousness. ");
						if(!TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,curhp,null,"*eaten alive by a pack of compys")){
							return;
						}
					}
					else{
						B.Add("You manage to stay awake! ");
						attrs[Forays.AttrType.COMPY_POISON_LETHAL] = 0;
					}
				}
				if(HasAttr(AttrType.COMPY_POISON_LETHAL)){ //it needs to go to 2 to ensure the proper timing
					attrs[Forays.AttrType.COMPY_POISON_LETHAL]++;
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
						if(Stealth() * DistanceFrom(player) * 10 - attrs[AttrType.TURNS_VISIBLE]++*5 < Global.Roll(1,100)){
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
			if(HasAttr(AttrType.ON_FIRE) && attrs[AttrType.ON_FIRE] < 5 && time_of_last_action < Q.turn
			&& type != ActorType.CORPSETOWER_BEHEMOTH){
				if(Global.CoinFlip()){
					if(attrs[AttrType.ON_FIRE] >= light_radius){
						UpdateRadius(attrs[AttrType.ON_FIRE],attrs[AttrType.ON_FIRE]+1);
					}
					attrs[AttrType.ON_FIRE]++;
				}
			}
			if(HasAttr(AttrType.CATCHING_FIRE) && time_of_last_action < Q.turn){
				if(Global.OneIn(3)){
					attrs[AttrType.CATCHING_FIRE] = 0;
					if(!HasAttr(AttrType.ON_FIRE)){
						if(light_radius == 0){
							UpdateRadius(0,1);
						}
						attrs[AttrType.ON_FIRE] = 1;
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
			if(!HasAttr(AttrType.AFRAID) && !HasAttr(AttrType.PARALYZED) && !HasAttr(AttrType.FROZEN) && !HasAttr(AttrType.ASLEEP)){
				B.Print(false);
			}
			else{
				B.DisplayNow();
			}
			Cursor();
			Console.CursorVisible = true;
			if(HasAttr(AttrType.PARALYZED) || HasAttr(AttrType.AFRAID) || HasAttr(AttrType.ASLEEP)){
				if(HasAttr(AttrType.AFRAID)){
					Thread.Sleep(250);
				}
				if(HasAttr(AttrType.ASLEEP)){
					Thread.Sleep(100);
				}
				Q1();
				return;
			}
			if(HasAttr(AttrType.FROZEN)){
				int damage = Global.Roll(Weapon.Damage(weapons.First.Value).dice,6) + TotalSkill(SkillType.COMBAT);
				attrs[Forays.AttrType.FROZEN] -= damage;
				if(attrs[Forays.AttrType.FROZEN] < 0){
					attrs[Forays.AttrType.FROZEN] = 0;
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
			if(Global.Option(OptionType.AUTOPICKUP) && tile().inv != null && !tile().inv.ignored && !tile().Is(FeatureType.QUICKFIRE)){
				bool grenade = false;
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.Is(FeatureType.GRENADE)){
						grenade = true;
					}
				}
				if(!grenade && !HasAttr(AttrType.ON_FIRE) && !HasAttr(AttrType.CATCHING_FIRE)){
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
						bool added = false;
						foreach(Item item in inv){
							if(item.type == i.type && !item.do_not_stack && !i.do_not_stack){
								item.quantity += i.quantity;
								added = true;
								break;
							}
						}
						if(!added){
							inv.Add(i);
						}
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
				if(t.IsKnownTrap() || t.Is(FeatureType.FUNGUS_ACTIVE) || t.Is(FeatureType.FUNGUS_PRIMED)
				|| t.Is(FeatureType.GRENADE) || t.Is(FeatureType.POISON_GAS) || t.Is(FeatureType.QUICKFIRE)){
					stopped_by_terrain = true;
				}
				if(!monsters_visible && !stopped_by_terrain && !Console.KeyAvailable){
					if(attrs[AttrType.RUNNING] == 5){
						int hplimit = HasFeat(FeatType.ENDURING_SOUL)? 20 : 10;
						if(curhp % hplimit == 0){
							if(HasAttr(AttrType.WAITING)){
								attrs[Forays.AttrType.WAITING]--;
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
						if(t.passable){
							PlayerWalk(attrs[AttrType.RUNNING]);
							return;
						}
						else{
							Tile opposite = TileInDirection(RotateDirection(attrs[AttrType.RUNNING],true,4));
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
								attrs[Forays.AttrType.RUNNING] = floor_dir;
								PlayerWalk(floor_dir);
								return;
							}
							else{
								attrs[Forays.AttrType.RUNNING] = 0;
								attrs[Forays.AttrType.WAITING] = 0;
							}
						}
					}
				}
				else{
					if(Console.KeyAvailable){
						Console.ReadKey(true);
					}
					attrs[AttrType.RUNNING] = 0;
					attrs[Forays.AttrType.WAITING] = 0;
				}
			}
			if(HasAttr(AttrType.RESTING)){
				if(attrs[AttrType.RESTING] == 10){
					attrs[AttrType.RESTING] = -1;
					curhp += ((maxhp - curhp) / 2); //recover half of your missing health
					ResetSpells();
					B.Add("You rest...you feel great! ");
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
				if(HasFeat(FeatType.FULL_DEFENSE) && EnemiesAdjacent() > 0){
					if(!HasAttr(AttrType.CATCHING_FIRE) && !HasAttr(AttrType.ON_FIRE)){
						attrs[AttrType.DEFENSIVE_STANCE]++;
						B.Add("You ready yourself. ");
					}
				}
				if(HasAttr(AttrType.CATCHING_FIRE)){
					attrs[AttrType.CATCHING_FIRE] = 0;
					B.Add("You stop the flames from spreading. ");
				}
				else{
					if(HasAttr(AttrType.ON_FIRE)){
						bool update = false;
						int oldradius = LightRadius();
						if(attrs[AttrType.ON_FIRE] > light_radius){
							update = true;
						}
						int i = 2;
						if(Global.Roll(1,3) == 3){ // 1 in 3 times, you don't make progress against the fire
							i = 1;
						}
						attrs[AttrType.ON_FIRE] -= i;
						if(attrs[AttrType.ON_FIRE] < 0){
							attrs[AttrType.ON_FIRE] = 0;
						}
						if(update){
							UpdateRadius(oldradius,LightRadius());
						}
						if(HasAttr(AttrType.ON_FIRE)){
							B.Add("You put out some of the fire. "); //better message?
						}
						else{
							B.Add("You put out the fire. ");
						}
					}
				}
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
							attrs[Forays.AttrType.WAITING] = 20;
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
								if(Global.Roll(5) <= 4){
									B.Add("You disarm " + Tile.Prototype(t.type).the_name + ". ");
									t.Toggle(this);
									Q1();
								}
								else{
									if(Global.Roll(20) <= skills[Forays.SkillType.DEFENSE]){
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
								if(Global.Roll(5) <= 4){
									B.Add("You disarm " + Tile.Prototype(t.type).the_name + ". ");
									t.Toggle(this);
									Q1();
								}
								else{
									if(Global.Roll(20) <= skills[Forays.SkillType.DEFENSE]){
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
			/*case 'c':
				{
				int door = DirectionOfOnlyUnblocked(TileType.DOOR_O);
				if(door == -1){
					int dir = GetDirection("Close in which direction? ");
					if(dir != -1){
						if(TileInDirection(dir).type == TileType.DOOR_O){
							if(StunnedThisTurn()){
								break;
							}
							TileInDirection(dir).Toggle(this);
							Q1();
						}
						else{
							Q0();
						}
					}
					else{
						Q0();
					}
				}
				else{
					if(door == 0){
						B.Add("There's nothing to close here. ");
						Q0();
					}
					else{
						if(StunnedThisTurn()){
							break;
						}
						TileInDirection(door).Toggle(this);
						Q1();
					}
				}
				break;
				}*/
			case 's':
				{
				if(Weapon.BaseWeapon(weapons.First.Value) == WeaponType.BOW || HasFeat(FeatType.QUICK_DRAW)){
					if(ActorsAtDistance(1).Count > 0){
						if(ActorsAtDistance(1).Count == 1){
							B.Add("You can't fire with an enemy so close. ");
						}
						else{
							B.Add("You can't fire with enemies so close. ");
						}
						Q0();
					}
					/*if(Global.Option(OptionType.LAST_TARGET) && target!=null && DistanceFrom(target)==1){ //since you can't fire
						target = null;										//at adjacent targets anyway.
					}*/
					else{
						List<Tile> line = GetTarget(12);
						if(line != null){
							//if(DistanceFrom(t) > 1 || t.actor() == null){
							FireArrow(line);
							/*}
							else{
								B.Add("You can't fire at adjacent targets. ");
								Q0();
							}*/
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
			case 'f':
				{
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
				}
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
				List<colorstring> ls = new List<colorstring>();
				List<SpellType> sp = new List<SpellType>();
				//foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
				bool bonus_marked = false;
				foreach(SpellType spell in spells_in_order){
					if(HasSpell(spell)){
						//string s = Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3);
						//s = s + FailRate(spell).ToString().PadLeft(9) + "%";
						//s = s + Spell.Description(spell).PadLeft(34);
						colorstring cs = new colorstring(Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray);
						cs.strings.Add(new cstr(FailRate(spell).ToString().PadLeft(9) + "%",FailColor(spell)));
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
					colorstring topborder = new colorstring("------------------Level---Fail rate--------Description------------",Color.Gray);
					int basefail = magic_penalty * 5;
					if(!HasFeat(FeatType.ARMORED_MAGE)){
						basefail += Armor.AddedFailRate(armors.First.Value);
					}
					colorstring bottomborder = new colorstring("------------Base fail rate: ",Color.Gray,(basefail.ToString().PadLeft(3) + "%"),FailColor(basefail),"----------[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(22,'-'),Color.Gray);
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
						bool can_recover_spells = false;
						if(magic_penalty > 0){
							can_recover_spells = true;
						}
						if(curhp < maxhp || can_recover_spells){
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
					bool can_recover_spells = false;
					if(magic_penalty > 0){
						can_recover_spells = true;
					}
					if(attrs[AttrType.RESTING] != -1 && (curhp < maxhp || can_recover_spells)){
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
						Q0();
					}
					else{
						B.Add("You don't see any stairs here. ");
						Q0();
					}
				}
				break;
			case 'x':
			{
				attrs[AttrType.AUTOEXPLORE]++;
				Q0();
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
										colorstring cs = new colorstring(Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray);
										cs.strings.Add(new cstr(FailRate(spell).ToString().PadLeft(9) + "%",FailColor(spell)));
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
									colorstring topborder = new colorstring("------------------Level---Fail rate--------Description------------",Color.Gray);
									int basefail = magic_penalty * 5;
									if(!HasFeat(FeatType.ARMORED_MAGE)){
										basefail += Armor.AddedFailRate(armors.First.Value);
									}
									colorstring bottomborder = new colorstring("------------Base fail rate: ",Color.Gray,(basefail.ToString().PadLeft(3) + "%"),FailColor(basefail),"----------[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(22,'-'),Color.Gray);
									int i = Select("Trade one of your spells for another? ",topborder,bottomborder,ls,false,false,true,true,HelpTopic.Spells);
									if(i != -1){
										List<SpellType> unknown = new List<SpellType>();
										foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
											if(!HasSpell(spell) && spell != SpellType.BLESS && spell != SpellType.MINOR_HEAL
											&& spell != SpellType.HOLY_SHIELD && spell != SpellType.NO_SPELL && spell != SpellType.NUM_SPELLS){
												unknown.Add(spell);
											}
										}
										SpellType forgotten = sp[i];
										spells_in_order.Remove(forgotten);
										spells[forgotten] = 0;
										SpellType learned = unknown.Random();
										spells[learned] = 1;
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
							B.Add("There's nothing here to pick up. ");
							Q0();
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
							bool added = false;
							foreach(Item item in inv){
								if(item.type == i.type && !item.do_not_stack && !i.do_not_stack){
									item.quantity += i.quantity;
									added = true;
									break;
								}
							}
							if(!added){
								inv.Add(i);
							}
							Q1();
						}
						else{
							int space_left = Global.MAX_INVENTORY_SIZE - InventoryCount();
							Item i = tile().inv;
							Item newitem = new Item(i,row,col);
							newitem.quantity = space_left;
							i.quantity -= space_left;
							B.Add("You pick up " + newitem.TheName() + ", but have no room for the other " + i.quantity.ToString() + ". ");
							bool added = false;
							foreach(Item item in inv){
								if(item.type == newitem.type && !item.do_not_stack && !newitem.do_not_stack){
									item.quantity += newitem.quantity;
									added = true;
									break;
								}
							}
							if(!added){
								inv.Add(newitem);
							}
							Q1();
						}
					}
					else{
						B.Add("Your pack is too full to pick up " + tile().inv.TheName() + ". ");
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
					int num = -1;
					Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
					char letter = 'a';
					int line=1;
					foreach(string s in InventoryList()){
						string s2 = "[" + letter + "] " + s;
						Screen.WriteMapString(line,0,s2.PadRight(COLS));
						Screen.WriteMapChar(line,1,new colorchar(Color.Cyan,letter));
						letter++;
						line++;
					}
					//Screen.WriteMapString(line,0,("".PadRight(25,'-') + "[?] for help").PadRight(COLS,'-'));
					Screen.WriteMapString(line,0,("------Space left: " + (Global.MAX_INVENTORY_SIZE - InventoryCount()).ToString().PadRight(7,'-') + "[?] for help").PadRight(COLS,'-'));
					Screen.WriteMapChar(line,26,new colorchar(Color.Cyan,'?'));
					if(line < ROWS){
						Screen.WriteMapString(line+1,0,"".PadRight(COLS));
					}
					B.DisplayNow("Drop which item? ");
					Console.CursorVisible = true;
					while(true){
						command = Console.ReadKey(true);
						ch = ConvertInput(command);
						int ii = ch - 'a';
						if(ii >= 0 && ii < InventoryList().Count){
							num = ii;
							break;
						}
						else{
							if(ch == '?'){
								Help.DisplayHelp(HelpTopic.Items);
								num = -1;
								break;
							}
						}
						break;
					}
					M.RedrawWithStrings();
					if(num != -1){
						if(StunnedThisTurn()){
							break;
						}
						Item i = inv[num];
						if(i.quantity <= 1){
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
			case 'i':
/*				if(inv.Count == 0){
					B.Add("You have nothing in your pack. ");
				}
				else{
					Select("In your pack: ",InventoryList(),true,false,true);
					Console.CursorVisible = true;
					Console.ReadKey(true);
				}
				Q0();
				break;*/
				if(inv.Count == 0){
					B.Add("You have nothing in your pack. ");
					Q0();
				}
				else{
//					int i = Select("Use which item? ",InventoryList());
					Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
					char letter = 'a';
					int line=1;
					foreach(string s in InventoryList()){
						string s2 = "[" + letter + "] " + s;
						Screen.WriteMapString(line,0,s2.PadRight(COLS));
						Screen.WriteMapChar(line,1,new colorchar(Color.Cyan,letter));
						letter++;
						line++;
					}
					Screen.WriteMapString(line,0,("------Space left: " + (Global.MAX_INVENTORY_SIZE - InventoryCount()).ToString().PadRight(7,'-') + "[?] for help").PadRight(COLS,'-'));
					Screen.WriteMapChar(line,26,new colorchar(Color.Cyan,'?'));
					if(line < ROWS){
						Screen.WriteMapString(line+1,0,"".PadRight(COLS));
					}
					B.DisplayNow("In your pack: ");
					Console.CursorVisible = true;
					command = Console.ReadKey(true);
					ch = ConvertInput(command);
					if(ch == '?'){
						Help.DisplayHelp(HelpTopic.Items);
					}
					M.RedrawWithStrings();
					Q0();
				}
				break;
			case 'a':
				if(inv.Count == 0){
					B.Add("You have nothing in your pack. ");
					Q0();
				}
				else{
//					int i = Select("Use which item? ",InventoryList());
					int num = -1;
					Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
					char letter = 'a';
					int line=1;
					foreach(string s in InventoryList()){
						string s2 = "[" + letter + "] " + s;
						Screen.WriteMapString(line,0,s2.PadRight(COLS));
						Screen.WriteMapChar(line,1,new colorchar(Color.Cyan,letter));
						letter++;
						line++;
					}
					Screen.WriteMapString(line,0,("------Space left: " + (Global.MAX_INVENTORY_SIZE - InventoryCount()).ToString().PadRight(7,'-') + "[?] for help").PadRight(COLS,'-'));
					//Screen.WriteMapString(line,0,("".PadRight(25,'-') + "[?] for help").PadRight(COLS,'-'));
					Screen.WriteMapChar(line,26,new colorchar(Color.Cyan,'?'));
					if(line < ROWS){
						Screen.WriteMapString(line+1,0,"".PadRight(COLS));
					}
					B.DisplayNow("Apply which item? ");
					Console.CursorVisible = true;
					while(true){
						command = Console.ReadKey(true);
						ch = ConvertInput(command);
						int ii = ch - 'a';
						if(ii >= 0 && ii < InventoryList().Count){
							num = ii;
							break;
						}
						else{
							if(ch == '?'){
								Help.DisplayHelp(HelpTopic.Items);
								num = -1;
								break;
							}
						}
						break;
					}
					M.RedrawWithStrings();
					//if(i != -1){
					if(num != -1){
						if(StunnedThisTurn()){
							break;
						}
						//if(inv[i].Use(this)){
						if(inv[num].Use(this)){
							Q1();
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
			case 'e':
			{
				int[] changes = DisplayEquipment();
				WeaponType new_weapon = Weapon.BaseWeapon((WeaponType)changes[0]);
				ArmorType new_armor = Armor.BaseArmor((ArmorType)changes[1]);
				WeaponType old_weapon = weapons.First.Value;
				ArmorType old_armor = armors.First.Value;
				bool weapon_changed = (new_weapon != Weapon.BaseWeapon(old_weapon));
				bool armor_changed = (new_armor != Armor.BaseArmor(old_armor));
				bool cursed_weapon = false;
				if(weapon_changed && HasAttr(AttrType.CURSED_WEAPON)){
					cursed_weapon = true;
					weapon_changed = false;
				}
				if(!weapon_changed && !armor_changed){
					if(cursed_weapon){
						B.Add("Your " + Weapon.Name(weapons.First.Value) + " is stuck to your hand and can't be dropped. ");
					}
					Q0();
				}
				else{
					if(StunnedThisTurn()){
						break;
					}
					if(weapon_changed){
						bool done=false;
						while(!done){
							WeaponType w = weapons.First.Value;
							weapons.Remove(w);
							weapons.AddLast(w);
							if(new_weapon == Weapon.BaseWeapon(weapons.First.Value)){
								done = true;
							}
						}
						if(HasFeat(FeatType.QUICK_DRAW) && !armor_changed){
							B.Add("You quickly ready your " + Weapon.Name(weapons.First.Value) + ". ");
						}
						else{
							B.Add("You ready your " + Weapon.Name(weapons.First.Value) + ". ");
						}
						UpdateOnEquip(old_weapon,weapons.First.Value);
					}
					if(armor_changed){
						bool done=false;
						while(!done){
							ArmorType a = armors.First.Value;
							armors.Remove(a);
							armors.AddLast(a);
							if(new_armor == Armor.BaseArmor(armors.First.Value)){
								done = true;
							}
						}
						B.Add("You wear your " + Armor.Name(armors.First.Value) + ". ");
						UpdateOnEquip(old_armor,armors.First.Value);
					}
					if(cursed_weapon){
						B.Add("Your " + Weapon.Name(weapons.First.Value) + " is stuck to your hand and can't be dropped. ");
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
				if(HasAttr(AttrType.CURSED_WEAPON)){
					B.Add("Your " + Weapon.Name(weapons.First.Value) + " is stuck to your hand and can't be dropped. ");
					Q0();
				}
				else{
					WeaponType new_weapon = WeaponType.NO_WEAPON;
					switch(ch){
					case '!':
						new_weapon = WeaponType.SWORD;
						break;
					case '@':
						new_weapon = WeaponType.MACE;
						break;
					case '#':
						new_weapon = WeaponType.DAGGER;
						break;
					case '$':
						new_weapon = WeaponType.STAFF;
						break;
					case '%':
						new_weapon = WeaponType.BOW;
						break;
					}
					WeaponType old_weapon = weapons.First.Value;
					if(new_weapon == Weapon.BaseWeapon(old_weapon)){
						Q0();
					}
					else{
						if(StunnedThisTurn()){
							break;
						}
						bool done=false;
						while(!done){
							WeaponType w = weapons.First.Value;
							weapons.Remove(w);
							weapons.AddLast(w);
							if(new_weapon == Weapon.BaseWeapon(weapons.First.Value)){
								done = true;
							}
						}
						if(HasFeat(FeatType.QUICK_DRAW)){
							B.Add("You quickly ready your " + Weapon.Name(weapons.First.Value) + ". ");
							Q0();
						}
						else{
							B.Add("You ready your " + Weapon.Name(weapons.First.Value) + ". ");
							Q1();
						}
						UpdateOnEquip(old_weapon,weapons.First.Value);
					}
				}
				break;
			}
			case '*': //these are toprow numbers, not shifted versions. see above.
			case '(':
			case ')':
			{
				ArmorType new_armor = ArmorType.NO_ARMOR;
				switch(ch){
				case '*':
					new_armor = ArmorType.LEATHER;
					break;
				case '(':
					new_armor = ArmorType.CHAINMAIL;
					break;
				case ')':
					new_armor = ArmorType.FULL_PLATE;
					break;
				}
				ArmorType old_armor = armors.First.Value;
				if(new_armor == Armor.BaseArmor(old_armor)){
					Q0();
				}
				else{
					if(StunnedThisTurn()){
						break;
					}
					bool done=false;
					while(!done){
						ArmorType a = armors.First.Value;
						armors.Remove(a);
						armors.AddLast(a);
						if(new_armor == Armor.BaseArmor(armors.First.Value)){
							done = true;
						}
					}
					B.Add("You wear your " + Armor.Name(armors.First.Value) + ". ");
					Q1();
					UpdateOnEquip(old_armor,armors.First.Value);
				}
				break;
			}
			case 't':
				if(StunnedThisTurn()){
					break;
				}
				if(light_radius==0){
					if(HasAttr(AttrType.ENHANCED_TORCH)){
						UpdateRadius(LightRadius(),Global.MAX_LIGHT_RADIUS - attrs[AttrType.DIM_LIGHT]*2,true);
					}
					else{
						UpdateRadius(LightRadius(),6 - attrs[AttrType.DIM_LIGHT],true); //normal light radius is 6
					}
					if(!M.wiz_dark){
						B.Add("You bring out your torch. ");
					}
					else{
						B.Add("You bring out your torch, but it gives off no light! ");
					}
				}
				else{
					UpdateRadius(LightRadius(),0,true);
					UpdateRadius(0,attrs[AttrType.ON_FIRE]);
					if(!M.wiz_lite){
						B.Add("You put away your torch. ");
					}
					else{
						B.Add("You put away your torch. The air still shines brightly. ");
					}
				}
				Q1();
				break;
			case (char)9:
				GetTarget(true,-1,true);
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
				Q0();
				break;
				}
			case 'c':
				DisplayCharacterInfo();
				Q0();
				break;
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
					case 'e':
					{
						if(skills[Forays.SkillType.MAGIC] > 0){
							M.RedrawWithStrings();
							List<colorstring> list = new List<colorstring>();
							List<SpellType> sp = new List<SpellType>();
							bool bonus_marked = false;
							foreach(SpellType spell in spells_in_order){
								if(HasSpell(spell)){
									colorstring cs = new colorstring(Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray);
									cs.strings.Add(new cstr(FailRate(spell).ToString().PadLeft(9) + "%",FailColor(spell)));
									if(HasFeat(FeatType.MASTERS_EDGE) && Spell.IsDamaging(spell) && !bonus_marked){
										bonus_marked = true;
										cs = cs + Spell.DescriptionWithIncreasedDamage(spell);
									}
									else{
										cs = cs + Spell.Description(spell);
									}
									list.Add(cs);
									sp.Add(spell);
								}
							}
							if(sp.Count > 0){
								colorstring topborder = new colorstring("------------------Level---Fail rate--------Description------------",Color.Gray);
								int basefail = magic_penalty * 5;
								if(!HasFeat(FeatType.ARMORED_MAGE)){
									basefail += Armor.AddedFailRate(armors.First.Value);
								}
								colorstring bottomborder = new colorstring("------------Base fail rate: ",Color.Gray,(basefail.ToString().PadLeft(3) + "%"),FailColor(basefail),"".PadRight(37,'-'),Color.Gray);
								int i = Select("Automatically cast which spell? ",topborder,bottomborder,list,false,false,false,false,HelpTopic.Overview);
								if(i != -1){
									F[0] = sp[i];
								}
								else{
									F[0] = SpellType.NO_SPELL;
								}
							}
						}
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
				if(false){
					List<string> l = new List<string>();
					l.Add("Throw a prismatic orb");
					l.Add("create chests");
					l.Add("Create a poison gas vent");
					l.Add("create fog");
					l.Add("Forget the map");
					l.Add("Heal to full");
					l.Add("Become invulnerable");
					l.Add("get items!");
					l.Add("Spawn a monster");
					l.Add("Use a rune of passage");
					l.Add("See the entire level");
					l.Add("Generate new level");
					l.Add("Gain all skills and feats");
					l.Add("Spawn shrines");
					l.Add("create trap");
					l.Add("create door");
					l.Add("spawn lots of goblins and lose neck snap");
					l.Add("remove all enemies, spawn boss");
					l.Add("detect monsters forever");
					l.Add("trigger floor collapse");
					switch(Select("Activate which cheat? ",l)){
					case 0:
						{
						new Item(ConsumableType.PRISMATIC,"prismatic orb",'*',Color.White).Use(this);
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
						List<Tile> line = GetTarget(-1,-1);
						if(line != null){
							Tile t = line.Last();
							/*if(t != null && t.inv == null){
								Item.Create(Item.RandomItem(),t.row,t.col);
								t.inv.do_not_stack = true;
								Q.Add(new Event(t.inv,new List<Tile>{t},100,Forays.EventType.MIMIC,AttrType.NO_ATTR,0,""));
							}*/
							if(t != null){
								/*t.TransformTo(TileType.FIRE_GEYSER);
								int frequency = Global.Roll(21) + 4; //5-25
								int variance = Global.Roll(10) - 1; //0-9
								int variance_amount = (frequency * variance) / 10;
								int number_of_values = variance_amount*2 + 1;
								int minimum_value = frequency - variance_amount;
								if(minimum_value < 5){
									int diff = 5 - minimum_value;
									number_of_values -= diff;
									minimum_value = 5;
								}
								int delay = ((minimum_value - 1) + Global.Roll(number_of_values)) * 100;
								Q.Add(new Event(t,delay + 200,EventType.FIRE_GEYSER,(frequency*10)+variance)); //notice the hacky way the value is stored
								Q.Add(new Event(t,delay,EventType.FIRE_GEYSER_ERUPTION,2));*/
								t.TransformTo(TileType.POISON_GAS_VENT);
								Q.Add(new Event(t,100,EventType.POISON_GAS_VENT));
							}
						}
						Q0();
						break;
					}
					case 3:
					{
						//ConsoleKeyInfo command2 = Console.ReadKey(true);
						//Console.Write(command2.Key);
						List<Tile> line = GetTarget(-1,-1);
						if(line != null){
							Tile t = line.Last();
							if(t != null){
								t.AddOpaqueFeature(FeatureType.FOG);
							}
						}
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
						//Create(ActorType.CULTIST,18,50);
						M.SpawnMob(ActorType.DIRE_RAT);
						Q1();
						break;
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
						M.GenerateLevel();
						Q0();
						break;
					case 12:
					{
						/*Tile t = GetTarget();
						if(t != null){
							TileType oldtype = t.type;
							t.TransformTo(TileType.GRENADE);
							t.toggles_into = oldtype;
							t.passable = Tile.Prototype(oldtype).passable;
							t.opaque = Tile.Prototype(oldtype).opaque;
							switch(oldtype){
							case TileType.FLOOR:
								t.the_name = "the grenade on the floor";
								t.a_name = "a grenade on a floor";
								break;
							case TileType.STAIRS:
								t.the_name = "the grenade on the stairway";
								t.a_name = "a grenade on a stairway";
								break;
							case TileType.DOOR_O:
								t.the_name = "the grenade in the open door";
								t.a_name = "a grenade in an open door";
								break;
							default:
								t.the_name = "the grenade and " + Tile.Prototype(oldtype).the_name;
								t.a_name = "a grenade and " + Tile.Prototype(oldtype).a_name;
								break;
							}
							Q.Add(new Event(t,100,EventType.GRENADE));
						}*/
						level = 10;
						skills[SkillType.COMBAT] = 10;
						skills[SkillType.DEFENSE] = 10;
						skills[SkillType.MAGIC] = 10;
						skills[SkillType.SPIRIT] = 10;
						skills[SkillType.STEALTH] = 10;
						foreach(FeatType f in Enum.GetValues(typeof(FeatType))){
							if(f != FeatType.NO_FEAT && f != FeatType.NUM_FEATS){
								feats[f] = 1;
							}
						}
						Q0();
						B.Add("\"I HAVE THE POWERRRR!\" ");
						break;
					}
					case 13:
					{
						//LevelUp();
							foreach(Tile t in TilesWithinDistance(2)){
								t.TransformTo((TileType)(Global.Roll(5)+20));
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
							List<Tile> line = GetTarget(-1,-1);
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
							feats[FeatType.NECK_SNAP] = 0;
						}
						Q0();
						break;
					}
					case 17:
					{
						foreach(Actor a in M.AllActors()){
							if(a != this){
								Q.KillEvents(a,Forays.EventType.ANY_EVENT);
								M.RemoveTargets(a);
								M.actor[a.p] = null;
							}
						}
						foreach(Tile t in M.AllTiles()){
							if(t.passable && t.actor() == null){
								Create(ActorType.FIRE_DRAKE,t.row,t.col,true,false);
								break;
							}
						}
						Q0();
						break;
					}
					case 18:
					{
						if(attrs[Forays.AttrType.DETECTING_MONSTERS] == 0){
							attrs[Forays.AttrType.DETECTING_MONSTERS] = 1;
						}
						else{
							attrs[Forays.AttrType.DETECTING_MONSTERS] = 0;
						}
						Q0();
						break;
					}
					case 19:
					{
						List<Tile> line = GetTarget(-1,-1);
						if(line != null){
							Tile t = line.Last();
							if(t != null){
								t.Toggle(null,TileType.CHASM);
								Q.Add(new Event(t,100,EventType.FLOOR_COLLAPSE));
								B.Add("The floor begins to collapse! ");
							}
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
				attrs[Forays.AttrType.AUTOEXPLORE] = 0;
			}
		}
		public void PlayerWalk(int dir){
			if(dir > 0){
				if(ActorInDirection(dir)!=null){
					if(!ActorInDirection(dir).IsHiddenFrom(this)){
						if(F[0] == SpellType.NO_SPELL){
							Attack(0,ActorInDirection(dir));
						}
						else{
							if(!CastSpell(F[0],TileInDirection(dir))){
								Q0();
							}
						}
					}
					else{
						ActorInDirection(dir).attrs[AttrType.TURNS_VISIBLE] = -1;
						ActorInDirection(dir).attrs[Forays.AttrType.NOTICED]++;
						if(!IsHiddenFrom(ActorInDirection(dir))){
							B.Add("You walk straight into " + ActorInDirection(dir).AVisible() + "! ");
						}
						else{
							B.Add("You walk straight into " + ActorInDirection(dir).AVisible() + "! ");
							if(CanSee(ActorInDirection(dir))){
								B.Add(ActorInDirection(dir).the_name + " looks just as surprised as you. ");
							}
							ActorInDirection(dir).player_visibility_duration = -1;
							ActorInDirection(dir).attrs[Forays.AttrType.PLAYER_NOTICED]++;
						}
						Q1();
					}
				}
				else{
					if(TileInDirection(dir).passable){
						if(GrabPreventsMovement(TileInDirection(dir))){
							List<Actor> grabbers = new List<Actor>();
							foreach(Actor a in ActorsAtDistance(1)){
								if(a.attrs[Forays.AttrType.GRABBING] == a.DirectionOf(this)){
									grabbers.Add(a);
								}
							}
							B.Add(grabbers.Random().the_name + " prevents you from moving away! ");
							Q0();
							return;
						}
						if(TileInDirection(dir).type == TileType.STAIRS){
						   if(!Global.Option(OptionType.HIDE_COMMANDS)){
								B.Add("There are stairs here - press > to descend. ");
							}
							else{
								B.Add("There are stairs here. ");
							}
						}
						if(TileInDirection(dir).IsShrine()){
							B.Add(TileInDirection(dir).the_name + " glows faintly - press g to touch it. ");
						}
						if(TileInDirection(dir).Is(TileType.CHEST)){
							B.Add("There is a chest here - press g to open it. ");
						}
						if(TileInDirection(dir).Is(TileType.HEALING_POOL)){
							B.Add("There is a healing pool here. ");
							Help.TutorialTip(TutorialTopic.HealingPool);
						}
						if(TileInDirection(dir).Is(TileType.CHASM) && !HasAttr(AttrType.FLYING)){
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
						if(TileInDirection(dir).inv != null){
							B.Add("You see " + TileInDirection(dir).inv.AName() + ". ");
						}
						Move(TileInDirection(dir).row,TileInDirection(dir).col);
						QS();
						if(!Help.displayed[TutorialTopic.Recovery] && !HasAttr(AttrType.POISONED) && !HasAttr(AttrType.ON_FIRE) && !HasAttr(AttrType.CATCHING_FIRE)
						&& curhp % 10 > 0 && curhp % 10 <= 5 && !M.AllActors().Any(a=>(a != this && CanSee(a))) && !TilesWithinDistance(1).Any(t=>t.Is(FeatureType.FUNGUS_ACTIVE)||t.Is(FeatureType.FUNGUS_PRIMED)
						||t.Is(FeatureType.GRENADE)||t.Is(FeatureType.POISON_GAS)||t.Is(FeatureType.QUICKFIRE)||t.Is(TileType.FIRE_GEYSER))){
							Help.TutorialTip(TutorialTopic.Recovery); //not poisoned or on fire, can recover at least 5hp, can't see any enemies, and isn't adjacent to hazardous terrain
							Interrupt();
						}
					}
					else{
						if(TileInDirection(dir).type == TileType.DOOR_C || TileInDirection(dir).type == TileType.RUBBLE){
							if(StunnedThisTurn()){
								return;
							}
							TileInDirection(dir).Toggle(this);
							Q1();
						}
						else{
							B.Add("There is " + TileInDirection(dir).a_name + " in the way. ");
							Q0();
						}
					}
				}
			}
			else{
				Q0();
			}
		}
		public void InputAI(){
			bool no_act = false;
			if(CanSee(player)){
				if(target_location == null && HasAttr(AttrType.BLOODSCENT)){ //orc warmages etc. when they first notice
					player_visibility_duration = -1;
					target = player;
					target_location = M.tile[player.row,player.col];
					if((player.IsWithinSightRangeOf(this) || tile().IsLit()) && player.HasLOS(this)){
						B.Add(the_name + "'s gaze meets your eyes! ",this);
						if(DistanceFrom(player) <= 6){
							player.MakeNoise();
						}
					}
					B.Add(the_name + " snarls loudly. ",this);
					Q1();
					no_act = true;
				}
				else{
					target = player;
					target_location = M.tile[player.row,player.col];
					player_visibility_duration = -1;
				}
			}
			else{
				if((IsWithinSightRangeOf(player.row,player.col) || M.tile[player.row,player.col].IsLit()) //if they're stealthed and nearby...
					&& HasLOS(player.row,player.col)
					&& (!player.HasAttr(AttrType.SHADOW_CLOAK) || HasAttr(AttrType.PLAYER_NOTICED) || player.tile().IsLit() || HasAttr(AttrType.BLINDSIGHT))){
					int multiplier = HasAttr(AttrType.KEEN_SENSES)? 5 : 10; //animals etc. are approximately twice as hard to sneak past
					if(player.Stealth() * DistanceFrom(player) * multiplier - player_visibility_duration++*5 < Global.Roll(1,100)){
						player_visibility_duration = -1;
						attrs[Forays.AttrType.PLAYER_NOTICED]++;
						target = player;
						target_location = M.tile[player.row,player.col];
						if(group != null){
							foreach(Actor a in group){
								if(a != this && DistanceFrom(a) < 3){
									a.player_visibility_duration = -1;
									a.attrs[Forays.AttrType.PLAYER_NOTICED]++;
									a.target = player;
									a.target_location = M.tile[player.row,player.col];
								}
							}
						}
						switch(type){
						case ActorType.RAT:
						case ActorType.DIRE_RAT:
							B.Add(TheVisible() + " squeaks at you. ");
							player.MakeNoise();
							break;
						case ActorType.GOBLIN:
						case ActorType.GOBLIN_ARCHER:
						case ActorType.GOBLIN_SHAMAN:
							B.Add(TheVisible() + " growls. ");
							player.MakeNoise();
							break;
						case ActorType.BLOOD_MOTH:
							if(!M.wiz_lite && player.LightRadius() > 0){
								B.Add(the_name + " notices your light. ",this);
							}
							break;
						case ActorType.CULTIST:
						case ActorType.ROBED_ZEALOT:
							B.Add(TheVisible() + " yells. ");
							player.MakeNoise();
							break;
						case ActorType.ZOMBIE:
							B.Add(TheVisible() + " moans. Uhhhhhhghhh. ");
							player.MakeNoise();
							break;
						case ActorType.WOLF:
							B.Add(TheVisible() + " snarls at you. ");
							player.MakeNoise();
							break;
						case ActorType.FROSTLING:
							B.Add(TheVisible() + " makes a chittering sound. ");
							player.MakeNoise();
							break;
						case ActorType.SWORDSMAN:
						case ActorType.BERSERKER:
						case ActorType.CRUSADING_KNIGHT:
							B.Add(TheVisible() + " shouts. ");
							player.MakeNoise();
							break;
						case ActorType.BANSHEE:
							B.Add(TheVisible() + " shrieks. ");
							player.MakeNoise();
							break;
						case ActorType.WARG:
							B.Add(TheVisible() + " howls. ");
							player.MakeNoise();
							break;
						case ActorType.DERANGED_ASCETIC:
							B.Add(TheVisible() + " starts babbling incoherently. ");
							break;
						case ActorType.CAVERN_HAG:
							B.Add(TheVisible() + " cackles. ");
							player.MakeNoise();
							break;
						case ActorType.COMPY:
							B.Add(TheVisible() + " squeaks. ");
							player.MakeNoise();
							break;
						case ActorType.OGRE:
							B.Add(TheVisible() + " bellows at you. ");
							player.MakeNoise();
							break;
						case ActorType.SHADOW:
							B.Add(TheVisible() + " hisses faintly. ");
							break;
						case ActorType.ORC_GRENADIER:
						case ActorType.ORC_WARMAGE:
							B.Add(TheVisible() + " snarls loudly. ");
							player.MakeNoise();
							break;
						case ActorType.ENTRANCER:
							B.Add(the_name + " stares at you for a moment. ",this);
							break;
						case ActorType.STONE_GOLEM:
							B.Add(the_name + " starts moving. ",this);
							break;
						case ActorType.NECROMANCER:
							B.Add(TheVisible() + " starts chanting in low tones. ");
							break;
						case ActorType.TROLL:
						case ActorType.TROLL_SEER:
							B.Add(TheVisible() + " growls viciously. ");
							player.MakeNoise();
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
						if(target_location == null && player_visibility_duration-- == -(10+attrs[Forays.AttrType.ALERTED]*40)){
							if(attrs[Forays.AttrType.ALERTED] < 2){ //they'll forget the player after 10 turns the first time and
								attrs[Forays.AttrType.ALERTED]++; //50 turns the second time, but that's the limit
								player_visibility_duration = 0;
								target = null;
							}
						}
					}
				}
			}
			if(type == ActorType.MARBLE_HORROR && tile().IsLit()){
				B.Add("The marble horror reverts to its statue form. ",this);
				type = ActorType.MARBLE_HORROR_STATUE;
				SetName("marble horror statue");
				attrs[Forays.AttrType.NEVER_MOVES] = 1;
				attrs[Forays.AttrType.INVULNERABLE] = 1;
				attrs[Forays.AttrType.IMMUNE_FIRE] = 1;
			}
			if(type == ActorType.MARBLE_HORROR_STATUE && !tile().IsLit()){
				B.Add("The marble horror animates once more. ",this);
				type = ActorType.MARBLE_HORROR;
				SetName("marble horror");
				attrs[Forays.AttrType.NEVER_MOVES] = 0;
				attrs[Forays.AttrType.INVULNERABLE] = 0;
				attrs[Forays.AttrType.IMMUNE_FIRE] = 0;
			}
			if(type == ActorType.COMPY && group != null && target != null){
				if(!group.Any(a=>a.curhp < a.maxhp) && target.curhp >= 20 && !target.HasAttr(AttrType.ASLEEP) && !target.HasAttr(AttrType.PARALYZED)
				&& !target.HasAttr(AttrType.IN_COMBAT)){
					target = null;
					target_location = null;
				}
			}
			if(!no_act && type != ActorType.CULTIST && type != ActorType.CORPSETOWER_BEHEMOTH && type != ActorType.BLOOD_MOTH
			&& type != ActorType.MUD_TENTACLE && type != ActorType.DREAM_CLONE && type != ActorType.ZOMBIE
			&& type != ActorType.CARNIVOROUS_BRAMBLE){
				if(HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
					if(HasAttr(AttrType.CATCHING_FIRE) && Global.OneIn(10)){
						attrs[AttrType.CATCHING_FIRE] = 0;
						B.Add(the_name + " stops the flames from spreading. ",this);
						Q1();
						no_act = true;
					}
					else{
						if(HasAttr(AttrType.ON_FIRE)){
							if(attrs[AttrType.ON_FIRE] == 1 && Global.OneIn(4)){
								bool update = false;
								int oldradius = LightRadius();
								if(attrs[AttrType.ON_FIRE] > light_radius){
									update = true;
								}
								attrs[AttrType.ON_FIRE] = 0;
								if(update){
									UpdateRadius(oldradius,LightRadius());
								}
								B.Add(the_name + " puts out the fire. ",this);
								Q1();
								no_act = true;
							}
							else{
								if(attrs[AttrType.ON_FIRE] > 1 && Global.Roll(10) <= 8){
									bool update = false;
									int oldradius = LightRadius();
									if(attrs[AttrType.ON_FIRE] > light_radius){
										update = true;
									}
									int i = 2;
									if(Global.Roll(1,3) == 3){ // 1 in 3 times, no progress against the fire
										i = 1;
									}
									attrs[AttrType.ON_FIRE] -= i;
									if(attrs[AttrType.ON_FIRE] < 0){
										attrs[AttrType.ON_FIRE] = 0;
									}
									if(update){
										UpdateRadius(oldradius,LightRadius ());
									}
									if(HasAttr(AttrType.ON_FIRE)){
										B.Add(the_name + " puts out some of the fire. ",this);
									}
									else{
										B.Add(the_name + " puts out the fire. ",this);
									}
									Q1();
									no_act = true;
								}
								else{
									if(attrs[AttrType.ON_FIRE] > 2 && Global.Roll(2) + attrs[AttrType.ON_FIRE] >= 5){
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
										if(attrs[AttrType.ON_FIRE] > light_radius){
											update = true;
										}
										int i = 2;
										if(Global.Roll(1,3) == 3){ // 1 in 3 times, no progress against the fire
											i = 1;
										}
										attrs[AttrType.ON_FIRE] -= i;
										if(attrs[AttrType.ON_FIRE] < 0){
											attrs[AttrType.ON_FIRE] = 0;
										}
										if(update){
											UpdateRadius(oldradius,LightRadius());
										}
										if(HasAttr(AttrType.ON_FIRE)){
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
						}
					}
				}
				else{
					if(HasAttr(AttrType.CATCHING_FIRE) && Global.CoinFlip()){
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
						if(HasAttr(AttrType.ON_FIRE) && Global.Roll(3) >= 2){
							bool update = false;
							int oldradius = LightRadius();
							if(attrs[AttrType.ON_FIRE] > light_radius){
								update = true;
							}
							int i = 2;
							if(Global.Roll(1,3) == 3){ // 1 in 3 times, no progress against the fire
								i = 1;
							}
							attrs[AttrType.ON_FIRE] -= i;
							if(attrs[AttrType.ON_FIRE] < 0){
								attrs[AttrType.ON_FIRE] = 0;
							}
							if(update){
								UpdateRadius(oldradius,LightRadius());
							}
							if(HasAttr(AttrType.ON_FIRE)){
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
						}
					}
				}
			}
			if(tile().Is(FeatureType.QUICKFIRE) || tile().Is(FeatureType.POISON_GAS) || (HasAttr(AttrType.LIGHT_ALLERGY) && tile().IsLit())){
				List<Tile> dangerous_terrain = new List<Tile>();
				bool dangerous_terrain_here = false;
				if(HasAttr(AttrType.LIGHT_ALLERGY) && target == null){ //ignore this if the vampire sees the player already
					foreach(Tile t in TilesWithinDistance(1)){
						if(t.IsLit() && t.passable){
							dangerous_terrain.Add(t);
							if(t == tile()){
								dangerous_terrain_here = true;
							}
						}
					}
				}
				if(!HasAttr(AttrType.IMMUNE_FIRE) && !HasAttr(AttrType.INVULNERABLE) && !HasAttr(AttrType.RESIST_FIRE)){
					if(type != ActorType.ZOMBIE && type != ActorType.CORPSETOWER_BEHEMOTH && type != ActorType.SKELETON && type != ActorType.SKELETAL_SABERTOOTH
					&& type != ActorType.CULTIST && type != ActorType.PHASE_SPIDER && type != ActorType.MARBLE_HORROR && type != ActorType.MECHANICAL_KNIGHT){
						foreach(Tile t in TilesWithinDistance(1)){
							if(t.Is(FeatureType.QUICKFIRE)){
								dangerous_terrain.AddUnique(t);
								if(t == tile()){
									dangerous_terrain_here = true;
								}
							}
						}
					}
				}
				if(!HasAttr(AttrType.IMMUNE_TOXINS) && !HasAttr(AttrType.UNDEAD) && !HasAttr(AttrType.CONSTRUCT)){
					if(type != ActorType.CULTIST && type != ActorType.PHASE_SPIDER && type != ActorType.MECHANICAL_KNIGHT){
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
					/*if(target == null || DistanceFrom(target) > 1 || Global.CoinFlip()){
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
			if(type == ActorType.MECHANICAL_KNIGHT && !HasAttr(AttrType.COOLDOWN_1)){
				attrs[Forays.AttrType.MECHANICAL_SHIELD] = 1; //if the knight was off balance, it regains its shield here.
			}
			if(group != null && group.Count == 0){ //this shouldn't happen, but does. this stops it from crashing.
				group = null;
			}
			if(!no_act){
				if(target != null){
					if(CanSee(target)){
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
						attrs[Forays.AttrType.COOLDOWN_2] = 5;
					}
					else{
						attrs[Forays.AttrType.COOLDOWN_2]--;
					}
				}
				else{
					if(tile().IsLit()){
						B.Add(the_name + " is blinded by the light! ",this);
						attrs[Forays.AttrType.COOLDOWN_1]++;
						attrs[Forays.AttrType.COOLDOWN_2] = 5;
						Q.Add(new Event(this,(Global.Roll(2)+4)*100,AttrType.COOLDOWN_1,the_name + " is no longer blinded. ",this));
					}
				}
			}
			if(type == ActorType.SHADOW){
				CalculateDimming();
			}
		}
		public void ActiveAI(){
			if(path.Count > 0){
				path.Clear();
			}
			switch(type){
			case ActorType.LARGE_BAT:
			case ActorType.PHANTOM_BLIGHTWING:
				if(DistanceFrom(target) == 1){
					int idx = Global.Roll(1,2) - 1;
					Attack(idx,target);
					if(Global.CoinFlip()){ //chance of retreating
						AI_Step(target,true);
					}
				}
				else{
					if(Global.CoinFlip()){
						AI_Step(target);
						QS();
					}
					else{
						AI_Step(TileInDirection(Global.RandomDirection())); //could also have RandomGoodDirection, but it
						QS();												//would be part of Actor or Map
					}
				}
				break;
			/*case ActorType.SHAMBLING_SCARECROW:
				if(DistanceFrom(target) == 1){
					if(curhp < maxhp || Global.CoinFlip()){
						if(HasAttr(AttrType.ON_FIRE)){
							attrs[AttrType.FIRE_HIT]++;
						}
						Attack(0,target);
						if(HasAttr(AttrType.ON_FIRE)){
							attrs[AttrType.FIRE_HIT]--;
						}
					}
					else{
						B.Add(the_name + " stares at you silently. ",this);
						Q1();
					}
				}
				else{
					if(speed == 90){
						if(curhp < maxhp){
							AI_Step(target);
							QS();
						}
						else{
							if(Global.CoinFlip()){
								AI_Step(TileInDirection(Global.RandomDirection()));
							}
							else{
								if(Global.Roll(1,3) == 3 && DistanceFrom(player) <= 6){
									if(player.CanSee(this)){
										B.Add(the_name + " emits an eerie whistling sound. ");
									}
									else{
										B.Add("You hear an eerie whistling sound. ");
									}
								}
							}
							Q1(); //note that the scarecrow doesn't move quickly until it is disturbed.
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;*/
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
										B.Add(the_name + " brushes up against " + ActorInDirection(dir).TheVisible() + ". ",this);
									}
								}
							}
							QS();
						}
					}
				}
				break;
			}
			case ActorType.SWORDSMAN:
			case ActorType.PHANTOM_SWORDMASTER:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(!HasAttr(AttrType.COOLDOWN_1)){
						B.Add(You("adopt") + " a more aggressive stance. ",this);
						attrs[AttrType.BONUS_COMBAT] += 5;
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.DARKNESS_DWELLER:
				if(HasAttr(AttrType.COOLDOWN_1)){
					int dir = Global.RandomDirection();
					if(!TileInDirection(dir).passable){
						B.Add(You("stagger") + " into " + TileInDirection(dir).the_name + ". ",this);
					}
					else{
						if(ActorInDirection(dir) != null){
							B.Add(YouVisible("stagger") + " into " + ActorInDirection(dir).TheVisible() + ". ",this,ActorInDirection(dir));
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
				break;
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
				if(DistanceFrom(target) == 1){
					if(!HasAttr(AttrType.COOLDOWN_2)){ //burst attack cooldown
						attrs[AttrType.COOLDOWN_2]++;
						int cooldown = 100 * (Global.Roll(1,3) + 8);
						Q.Add(new Event(this,cooldown,AttrType.COOLDOWN_2));
						AnimateExplosion(this,1,Color.RandomIce,'*');
						Attack(2,target);
					}
					else{
						if(Global.CoinFlip()){
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
						int cooldown = Global.Roll(1,4);
						if(cooldown != 1){
							attrs[AttrType.COOLDOWN_1]++;
							cooldown *= 100;
							Q.Add(new Event(this,cooldown,AttrType.COOLDOWN_1));
						}
						AnimateBoltProjectile(target,Color.RandomIce);
						Attack(1,target);
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
							Create(ActorType.DREAM_CLONE,t.row,t.col,true,true);
							t.actor().player_visibility_duration = -1;
							group.Add(M.actor[t.row,t.col]);
							M.actor[t.row,t.col].group = group;
							group.Randomize();
						}
						openspaces.Add(tile());
						Tile newtile = openspaces[Global.Roll(openspaces.Count)-1];
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
			case ActorType.CULTIST:
				if(curhp <= 10 && !HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					string invocation;
					switch(Global.Roll(4)){
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
						invocation = "gubed gubed gubed";
						break;
					}
					if(Global.CoinFlip()){
						B.Add(You("whisper") + " '" + invocation + "'. ",this);
					}
					else{
						B.Add(You("scream") + " '" + invocation.ToUpper() + "'. ",this);
					}
					B.Add("Flames erupt from " + the_name + ". ",this);
					if(LightRadius() < 2){
						UpdateRadius(LightRadius(),2);
					}
					attrs[AttrType.ON_FIRE] = Math.Max(attrs[AttrType.ON_FIRE],2);
					foreach(Actor a in ActorsAtDistance(1)){
						if(!a.HasAttr(AttrType.RESIST_FIRE) && !a.HasAttr(AttrType.IMMUNE_FIRE)
						&& !a.HasAttr(AttrType.ON_FIRE) && !a.HasAttr(AttrType.CATCHING_FIRE)
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
						if(AI_Step(target,true)){
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
						if(AI_Step(target,true)){
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
				List<SpellType> valid_spells = new List<SpellType>();
				valid_spells.Add(SpellType.FORCE_PALM);
				valid_spells.Add(SpellType.IMMOLATE);
				if(target.HasAttr(AttrType.ON_FIRE) || target.HasAttr(AttrType.CATCHING_FIRE)){
					valid_spells.Remove(SpellType.IMMOLATE);
				}
				SpellType[] close_spells = valid_spells.ToArray();
				valid_spells.Add(SpellType.SCORCH);
				//SpellType[] all_spells = valid_spells.ToArray();
				valid_spells.Remove(SpellType.FORCE_PALM);
				SpellType[] ranged_spells = valid_spells.ToArray();
				switch(DistanceFrom(target)){
				case 1:
					if(target.EnemiesAdjacent() > 1 || Global.CoinFlip()){
						CastRandomSpell(target,close_spells);
					}
					else{
						if(AI_Step(target,true)){
							QS();
						}
						else{
							CastRandomSpell(target,close_spells);
						}
					}
					break;
				case 2:
					if(Global.CoinFlip()){
						if(AI_Step(target,true)){
							QS();
						}
						else{
							if(FirstActorInLine(target) == target){
								CastRandomSpell(target,ranged_spells);
							}
							else{
								AI_Sidestep(target);
								QS();
							}
						}
					}
					else{
						if(FirstActorInLine(target) == target){
							CastRandomSpell(target,ranged_spells);
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
						CastRandomSpell(target,ranged_spells);
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
					target.attrs[AttrType.SLOWED]++;
					target.speed += 100;
					Q.Add(new Event(target,(Global.Roll(3)+5)*100,AttrType.SLOWED,target.YouAre() + " no longer slowed. ",target));
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
				switch(DistanceFrom(target)){
				case 1:
					if(HasAttr(AttrType.BLESSED)){
						Attack(0,target);
					}
					else{
						if(curhp <= 13){
							CastSpell(SpellType.MINOR_HEAL);
						}
						else{
							if(curhp < maxhp){
								if(HasAttr(AttrType.HOLY_SHIELDED)){
									CastSpell(SpellType.BLESS);
								}
								else{
									CastRandomSpell(null,SpellType.HOLY_SHIELD,SpellType.BLESS);
								}
							}
							else{
								CastSpell(SpellType.BLESS);
							}
						}
					}
					break;
				case 2:
					if(curhp <= 20){
						CastSpell(SpellType.MINOR_HEAL);
					}
					else{
						if(HasAttr(AttrType.BLESSED)){
							if(AI_Step(target)){
								QS();
							}
							else{
								AI_Sidestep(target);
								QS();
							}
						}
						else{
							if(Global.Roll(1,3) == 3){
								CastSpell(SpellType.BLESS);
							}
							else{
								if(AI_Step(target)){
									QS();
								}
								else{
									if(AI_Sidestep(target)){
										QS();
									}
									else{
										CastSpell(SpellType.BLESS);
									}
								}
							}
						}
					}
					break;
				default:
					if(curhp <= 26){
						CastSpell(SpellType.MINOR_HEAL);
					}
					else{
						if(curhp < maxhp){
							if(HasAttr(AttrType.HOLY_SHIELDED)){
								if(AI_Step(target)){
									QS();
								}
								else{
									if(AI_Sidestep(target)){
										QS();
									}
									else{
										CastSpell(SpellType.BLESS);
									}
								}
							}
							else{
								if(Global.CoinFlip()){
									CastSpell(SpellType.HOLY_SHIELD);
								}
								else{
									if(AI_Step(target)){
										QS();
									}
									else{
										if(AI_Sidestep(target)){
											QS();
										}
										else{
											CastSpell(SpellType.BLESS);
										}
									}
								}
							}
						}
						else{
							if(AI_Step(target)){
								QS();
							}
							else{
								if(AI_Sidestep(target)){
									QS();
								}
								else{
									CastSpell(SpellType.BLESS);
								}
							}
						}
					}
					break;
				}
				break;
			case ActorType.BANSHEE:
				if(!HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(Global.Roll(5)+5)*100,AttrType.COOLDOWN_1));
					if(player.CanSee(this)){
						B.Add(You("scream") + ". ",this);
					}
					else{
						if(DistanceFrom(player) <= 12){
							B.Add("You hear a scream! ");
						}
						else{
							B.Add("You hear a distant scream! ");
						}
					}
					int i = 1;
					Actor a;
					List<Actor> targets = new List<Actor>();
					for(bool done=false;!done;++i){
						a = FirstActorInLine(target,i);
						if(a != null && !a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT) && !a.HasAttr(AttrType.PLANTLIKE)){
							targets.Add(a);
						}
						if(a == target){
							done = true;
						}
						if(i > 100){
							B.Add(target.You("resist") + " the scream. ",target);
							Q1();
							return;
						}
					}
					foreach(Actor actor in targets){
						if(actor.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(6),this,"a banshee's scream")){
							actor.attrs[AttrType.AFRAID]++;
							Q.Add(new Event(actor,actor.DurationOfMagicalEffect((Global.Roll(3)+2))*100,AttrType.AFRAID));
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
			case ActorType.PHASE_SPIDER:
			{
				int action = 0;
				if(DistanceFrom(target) == 1){
					if(Global.CoinFlip()){
						action = 2; //disappear
					}
					else{
						if(Global.CoinFlip()){
							Attack(0,target);
						}
						else{
							action = 1; //blink
						}
					}
				}
				else{
					if(Global.CoinFlip()){ //teleport next to target and attack
						List<Tile> tilelist = new List<Tile>();
						for(int dir=1;dir<=9;++dir){
							if(dir != 5){
								if(target.TileInDirection(dir).passable && target.ActorInDirection(dir) == null){
									tilelist.Add(target.TileInDirection(dir));
								}
							}
						}
						if(tilelist.Count > 0){
							Tile t = tilelist[Global.Roll(1,tilelist.Count)-1];
							Move(t.row,t.col);
							Attack(0,target);
						}
						else{
							action = 2; //disappear
						}
					}
					else{
						if(Global.CoinFlip()){
							action = 1; //blink
						}
						else{
							action = 2; //disappear
						}
					}
				}
				switch(action){
				case 1: //blink
					for(int i=0;i<9999;++i){
						int a = Global.Roll(1,17) - 9; //-8 to 8
						int b = Global.Roll(1,17) - 9;
						if(Math.Abs(a) + Math.Abs(b) >= 6){
							a += row;
							b += col;
							if(M.BoundsCheck(a,b)){
								if(M.tile[a,b].passable && M.actor[a,b] == null){
									Move(a,b);
									break;
								}
							}
						}
					}
					QS();
					break;
				case 2: //disappear from target's sight
					bool[,] valid_tiles = new bool[ROWS,COLS];
					for(int i=0;i<ROWS;++i){
						for(int j=0;j<COLS;++j){
							if(M.tile[i,j].passable && M.actor[i,j] == null && !target.CanSee(i,j)){
								valid_tiles[i,j] = true;
							}
							else{
								valid_tiles[i,j] = false;
							}
						}
					}
					List<Tile> tilelist = new List<Tile>();
					bool found = false;
					for(int distance=1;distance<COLS && !found;++distance){
						for(int i=row-distance;i<=row+distance;++i){
							for(int j=col-distance;j<=col+distance;++j){
								if(M.BoundsCheck(i,j) && valid_tiles[i,j] && DistanceFrom(i,j) == distance){
									found = true;
									tilelist.Add(M.tile[i,j]);
								}
							}
						}
					}
					if(found){
						Tile t = tilelist[Global.Roll(1,tilelist.Count)-1];
						Move(t.row,t.col);
					}
					QS();
					break;
				default:
					break;
				}
				break;
			}
			case ActorType.DERANGED_ASCETIC:
				if(DistanceFrom(target) == 1){
					Attack(Global.Roll(3)-1,target);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
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
								TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,9999,null);
							}
							else{
								if(next.actor() != null){
									if(!next.actor().HasAttr(AttrType.NEVER_MOVES)){
										Move(next.row,next.col);
										QS();
									}
									else{
										if(next.actor().HasAttr(AttrType.NEVER_MOVES)){
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
			case ActorType.CAVERN_HAG:
				if(curhp < maxhp && !HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12){
					B.Add(the_name + " curses you! ");
					switch(Global.Roll(4)){
					case 1: //light allergy
						B.Add("You become allergic to light! ");
						target.GainAttrRefreshDuration(AttrType.LIGHT_ALLERGY,10000,"You are no longer allergic to light. ");
						break;
					case 2: //magical drowsiness
						B.Add("The floor suddenly looks like a wonderful spot for a nap. ");
						target.GainAttrRefreshDuration(AttrType.MAGICAL_DROWSINESS,10000,"You are no longer quite so drowsy. ");
						break;
					case 3: //aggravate monsters
						B.Add("Every sound you make becomes amplified and echoes across the dungeon. ");
						target.GainAttrRefreshDuration(AttrType.AGGRAVATING,10000,"Your sounds are no longer amplified. ");
						break;
					case 4: //cursed weapon
						B.Add("Your " + Weapon.Name(target.weapons.First.Value) + " becomes stuck to your hand! ");
						target.GainAttrRefreshDuration(AttrType.CURSED_WEAPON,10000,"Your " + Weapon.Name(target.weapons.First.Value) + " is no longer stuck to your hand. ");
						break;
					}
					attrs[Forays.AttrType.COOLDOWN_1]++;
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
			case ActorType.COMPY:
				if(DistanceFrom(target) == 1){
					pos target_pos = target.p;
					if(Attack(0,target) && M.actor[target_pos] != null && target == player && !target.HasAttr(AttrType.INVULNERABLE)
					&& !target.HasAttr(AttrType.ARCANE_SHIELDED) && !target.HasAttr(AttrType.IMMUNE_TOXINS)){
						bool first_bite = !target.HasAttr(AttrType.COMPY_POISON_COUNTER);
						target.GainAttrRefreshDuration(AttrType.COMPY_POISON_COUNTER,5000,"You no longer feel the effects of the poison. ");
						if(target.attrs[Forays.AttrType.COMPY_POISON_COUNTER] >= target.curhp){
							if(!target.HasAttr(AttrType.COMPY_POISON_LETHAL)){
								B.Add("The poison is overwhelming you! ");
								B.Add("You're falling asleep. ");
								B.Add("You'll surely be eaten... ");
								B.PrintAll();
								target.attrs[Forays.AttrType.COMPY_POISON_LETHAL]++;
							}
						}
						else{
							if(target.attrs[Forays.AttrType.COMPY_POISON_COUNTER] >= target.curhp / 2 && !target.HasAttr(AttrType.COMPY_POISON_WARNING)){
								target.GainAttrRefreshDuration(AttrType.COMPY_POISON_WARNING,5000);
								B.Add("You feel the subtle poison starting to take effect. ");
								B.Add("Your injuries make it hard to stay awake. ");
								B.PrintAll();
							}
							else{
								if(first_bite){
									B.Add("The compy's bite makes you momentarily fatigued. ");
									B.Add("You shake off the effects. ");
								}
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
					B.Add(TheVisible() + " breathes poisonous gas. ");
					List<Tile> area = new List<Tile>();
					foreach(Tile t in target.TilesWithinDistance(1)){
						if(t.passable && target.HasLOE(t) && !t.Is(FeatureType.POISON_GAS)){
							t.features.Add(FeatureType.POISON_GAS);
							area.Add(t);
						}
					}
					Tile current = target.tile();
					int num = 8;
					for(int i=0;i<num;++i){ //i should make this gas placement bit into a method
						if(!current.Is(FeatureType.POISON_GAS)){
							current.features.Add(FeatureType.POISON_GAS);
							area.Add(current);
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
									if(!possible.Is(FeatureType.POISON_GAS)){
										possible.features.Add(FeatureType.POISON_GAS);
										area.Add(possible);
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
					Q.Add(new Event(area,600,EventType.POISON_GAS));
					GainAttr(AttrType.COOLDOWN_1,(Global.Roll(6) + 18) * 100);
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
					bool cw = Global.CoinFlip();
					if(TileInDirection(dir).passable && ActorInDirection(dir) == null && !GrabPreventsMovement(TileInDirection(dir))){
						B.Add(the_name + " leaps forward swinging his axe! ",this);
						Move(TileInDirection(dir).row,TileInDirection(dir).col);
						Actor a = ActorInDirection(RotateDirection(dir,cw));
						if(a != null){
							B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this,"a berserker's axe");
						}
						a = ActorInDirection(dir);
						if(a != null){
							B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this,"a berserker's axe");
						}
						a = ActorInDirection(RotateDirection(dir,!cw));
						if(a != null){
							B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this,"a berserker's axe");
						}
						Q1();
					}
					else{
						if(ActorInDirection(dir) != null || GrabPreventsMovement(TileInDirection(dir))){
							B.Add(the_name + " swings his axe furiously! ",this);
							Actor a = ActorInDirection(RotateDirection(dir,cw));
							if(a != null){
								B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this,"a berserker's axe");
							}
							a = ActorInDirection(dir);
							if(a != null){
								B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this,"a berserker's axe");
							}
							a = ActorInDirection(RotateDirection(dir,!cw));
							if(a != null){
								B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this,"a berserker's axe");
							}
							Q1();
						}
						else{
							B.Add(the_name + " turns to face " + target.the_name + ". ",this,target);
							attrs[AttrType.COOLDOWN_2] = DirectionOf(target);
							Q1();
						}
					}
				}
				else{
					if(DistanceFrom(target) == 1){
						Attack(0,target);
						if(target != null && Global.Roll(3) == 3){
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
			case ActorType.VAMPIRE:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					if(DistanceFrom(target) <= 12){
						if(tile().IsLit() && !HasAttr(AttrType.COOLDOWN_1)){
							attrs[Forays.AttrType.COOLDOWN_1]++;
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
				if(DistanceFrom(target) <= 12 && count >= 2 || (count == 1 && walls == 1)){
					foreach(Tile t in target.TilesAtDistance(1)){
						if(t.type == TileType.WALL && t.actor() == null){
							Create(ActorType.MUD_TENTACLE,t.row,t.col,true,true);
							M.actor[t.p].player_visibility_duration = -1;
							M.actor[t.p].attrs[Forays.AttrType.COOLDOWN_1] = 20;
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
			case ActorType.ENTRANCER:
				if(group == null){
					if(AI_Step(target,true)){
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
									if(AI_Step(target,true)){
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
										B.Add(TheVisible() + " teleports " + thrall.TheVisible() + ". ",this,thrall);
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
									if(!HasAttr(AttrType.COOLDOWN_2) && !thrall.HasAttr(AttrType.ARCANE_SHIELDED)){
										GainAttr(AttrType.COOLDOWN_2,1500);
										B.Add(TheVisible() + " shields " + thrall.TheVisible() + ". ",this,thrall);
										B.DisplayNow();
										Screen.AnimateStorm(thrall.p,1,2,5,'*',Color.White);
										thrall.attrs[Forays.AttrType.ARCANE_SHIELDED] = 25;
										Q.Add(new Event(thrall,2000,AttrType.ARCANE_SHIELDED,thrall.Your() + " arcane shield dissolves. ",thrall));
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
						if(AI_Step(target,true)){
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
			case ActorType.MARBLE_HORROR_STATUE:
				QS();
				break;
			/*case ActorType.MARBLE_HORROR:
				break;//todo : anything here?*/
			case ActorType.ORC_GRENADIER:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 8){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(Global.Roll(2)*100)+150,AttrType.COOLDOWN_1));
					B.Add(the_name + " tosses a grenade toward " + target.the_name + ". ",this,target);
					List<Tile> tiles = new List<Tile>();
					foreach(Tile tile in target.TilesWithinDistance(1)){
						if(tile.passable){
							tiles.Add(tile);
						}
					}
					Tile t = tiles[Global.Roll(tiles.Count)-1];
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
			case ActorType.CARRION_CRAWLER:
				if(DistanceFrom(target) == 1){
					if(target.HasAttr(AttrType.PARALYZED)){
						Attack(0,target);
					}
					else{
						Attack(Global.Roll(1,2)-1,target);
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.SPELLMUDDLE_PIXIE:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(Global.CoinFlip()){
						AI_Step(target,true);
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.PYREN_ARCHER:
				switch(DistanceFrom(target)){
				case 1:
					if(target.EnemiesAdjacent() > 1){
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
					break;
				case 2:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						if(AI_Step(target,true)){
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
			case ActorType.TROLL_SEER:
				if(curhp <= 10 && !HasAttr(AttrType.COOLDOWN_1)){
					for(int i=0;i<9999;++i){
						int rr = Global.Roll(1,Global.ROWS-2);
						int rc = Global.Roll(1,Global.COLS-2);
						if(Math.Abs(rr-row) >= 10 || Math.Abs(rc-col) >= 10 || (Math.Abs(rr-row) >= 7 && Math.Abs(rc-col) >= 7)){
							if(M.BoundsCheck(rr,rc) && M.tile[rr,rc].passable && M.actor[rr,rc] == null && !HasLOS(rr,rc)){
								B.Add(TheVisible() + " slashes at the air, sending a swirling vortex toward " + target.the_name + ". ",target);
								AnimateBeam(target,'*',Color.Green);
								target.AnimateStorm(3,3,10,'*',Color.Green);
								target.Move(rr,rc);
								M.Draw();
								target.AnimateStorm(3,3,10,'*',Color.Green);
								B.Add(target.YouAre() + " transported elsewhere. ");
								attrs[Forays.AttrType.COOLDOWN_1]++;
								break;
							}
						}
					}
					QS();
				}
				else{
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
						Attack(0,target);
					}
					else{
						if(DistanceFrom(target) <= 12 && FirstActorInLine(target) == target){
							CastRandomSpell(target,SpellType.GLACIAL_BLAST,SpellType.SONIC_BOOM);
						}
						else{
							AI_Step(target);
							QS();
						}
					}
				}
				break;
			case ActorType.MECHANICAL_KNIGHT:
				if(DistanceFrom(target) == 1){
					if(HasAttr(AttrType.COOLDOWN_1)){ //no arms
						Attack(1,target);
					}
					else{
						if(!Attack(0,target)){
							B.Add(the_name + " is off balance! ",this);
							attrs[Forays.AttrType.MECHANICAL_SHIELD] = 0;
						}
					}
				}
				else{
					if(!HasAttr(AttrType.COOLDOWN_2)){ //no legs
						AI_Step(target);
					}
					QS();
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
				if(curhp <= 15 && HasLOS(target)){
					Tile wall = null;
					int wall_distance_to_center = 9999;
					pos center = new pos(ROWS/2,COLS/2);
					for(int i = 2;i<=8;i += 2){
						if(TileInDirection(i).type == TileType.WALL){
							if(TileInDirection(i).EstimatedEuclideanDistanceFromX10(center) < wall_distance_to_center){
								wall = TileInDirection(i);
								wall_distance_to_center = TileInDirection(i).EstimatedEuclideanDistanceFromX10(center);
							}
						}
					}
					if(wall != null){
						CastSpell(Forays.SpellType.PASSAGE,wall);
						break;
					}
				}
				List<SpellType> valid_spells = new List<SpellType>();
				valid_spells.Add(SpellType.FORCE_BEAM);
				valid_spells.Add(SpellType.IMMOLATE);
				valid_spells.Add(SpellType.GLACIAL_BLAST);
				valid_spells.Add(SpellType.GLACIAL_BLAST);
				if(target.HasAttr(AttrType.ON_FIRE) || target.HasAttr(AttrType.CATCHING_FIRE)){
					valid_spells.Remove(Forays.SpellType.IMMOLATE);
				}
				SpellType[] ranged_spells = valid_spells.ToArray();
				switch(DistanceFrom(target)){
				case 1:
					if(target.EnemiesAdjacent() > 1 || Global.CoinFlip()){
						CastRandomSpell(target,SpellType.MAGIC_HAMMER,SpellType.MAGIC_HAMMER,SpellType.FORCE_BEAM);
					}
					else{
						if(AI_Step(target,true)){
							QS();
						}
						else{
							CastRandomSpell(target,SpellType.MAGIC_HAMMER,SpellType.MAGIC_HAMMER,SpellType.FORCE_BEAM);
						}
					}
					break;
				case 2:
					if(HasLOE(target) && FirstActorInLine(target) != target){
						CastSpell(SpellType.VOLTAIC_SURGE);
						break;
					}
					if(Global.CoinFlip()){
						if(AI_Step(target,true)){
							QS();
						}
						else{
							if(FirstActorInLine(target) == target){
								CastRandomSpell(target,SpellType.IMMOLATE,SpellType.FORCE_BEAM,SpellType.GLACIAL_BLAST);
							}
							else{
								AI_Sidestep(target);
								QS();
							}
						}
					}
					else{
						if(FirstActorInLine(target) == target){
							CastRandomSpell(target,SpellType.IMMOLATE,SpellType.FORCE_BEAM,SpellType.GLACIAL_BLAST);
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
						CastRandomSpell(target,ranged_spells);
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
			case ActorType.LASHER_FUNGUS:
				if(DistanceFrom(target) <= 12){
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						if(FirstActorInLine(target) == target){
							List<Tile> line = GetBestLine(target.row,target.col);
							line.Remove(line[line.Count-1]);
							AnimateBoltBeam(line,Color.DarkGreen);
							if(Global.Roll(1,4) == 4){
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
													target.AI_Step(M.tile[target.row,col]);
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
			case ActorType.NECROMANCER:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 12){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(Global.Roll(4)+8)*100,AttrType.COOLDOWN_1));
					B.Add(the_name + " calls out to the dead. ",this);
					ActorType summon = Global.CoinFlip()? ActorType.SKELETON : ActorType.ZOMBIE;
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
						if(Global.CoinFlip() && FirstActorInLine(target) == target){
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
						target.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(6),this,"*blasted by a necromancer");
						Q1();
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
			case ActorType.FIRE_DRAKE:
				if(player.magic_items.Contains(MagicItemType.RING_OF_RESISTANCE) && DistanceFrom(player) <= 12 && CanSee(player)){
					B.Add(the_name + " exhales an orange mist toward you. ");
					foreach(Tile t in GetBestLine(player)){
						Screen.AnimateStorm(t.p,1,2,3,'*',Color.Red);
					}
					B.Add("Your ring of resistance melts and drips onto the floor! ");
					player.magic_items.Remove(MagicItemType.RING_OF_RESISTANCE);
					Q.Add(new Event(this,100,EventType.MOVE));
				}
				else{
					if(player.armors.First.Value == ArmorType.FULL_PLATE_OF_RESISTANCE && DistanceFrom(player) <= 12 && CanSee(player)){
						B.Add(the_name + " exhales an orange mist toward you. ");
						foreach(Tile t in GetBestLine(player)){
							Screen.AnimateStorm(t.p,1,2,3,'*',Color.Red);
						}
						B.Add("The runes drip from your full plate of resistance! ");
						player.armors.First.Value = ArmorType.FULL_PLATE;
						player.UpdateOnEquip(ArmorType.FULL_PLATE_OF_RESISTANCE,ArmorType.FULL_PLATE);
						Q.Add(new Event(this,100,EventType.MOVE));
					}
					else{
						if(!HasAttr(AttrType.COOLDOWN_1)){
							if(DistanceFrom(target) <= 12){
								attrs[AttrType.COOLDOWN_1]++;
								int cooldown = (Global.Roll(1,4)+1) * 100;
								Q.Add(new Event(this,cooldown,AttrType.COOLDOWN_1));
								AnimateBeam(target,Color.RandomFire,'*');
								Attack(2,target);
								if(target != null && !target.HasAttr(AttrType.ON_FIRE) && !target.HasAttr(AttrType.CATCHING_FIRE)){
									target.attrs[Forays.AttrType.CATCHING_FIRE] = 1;
									B.Add(target.You("start") + " catching fire! ",target);
								}
							}
							else{
								AI_Step(target);
								QS();
							}
						}
						else{
							if(DistanceFrom(target) == 1){
								Attack(Global.Roll(1,2)-1,target);
							}
							else{
								AI_Step(target);
								QS();
							}
						}
					}
				}
				break;
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
			switch(type){
			/*case ActorType.SHAMBLING_SCARECROW:
				if(Global.CoinFlip()){
					AI_Step(TileInDirection(Global.RandomDirection()));
				}
				else{
					if(Global.Roll(1,3) == 3 && DistanceFrom(player) <= 10){
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
									B.Add(the_name + " brushes up against " + ActorInDirection(dir).TheVisible() + ". ",this);
								}
							}
						}
						QS();
					}
				}
				break;
			}
			case ActorType.PHASE_SPIDER:
				if(DistanceFrom(target) <= 10){
					if(Global.Roll(1,4) == 4){ //teleport into target's LOS somewhere nearby
						List<Tile> tilelist = new List<Tile>();
						for(int i=0;i<ROWS;++i){
							for(int j=0;j<COLS;++j){
								if(M.tile[i,j].passable && M.actor[i,j] == null){
									if(DistanceFrom(i,j)<=10 && target.DistanceFrom(i,j)<=10 && target.CanSee(i,j)){
										tilelist.Add(M.tile[i,j]);
									}
								}
							}
						}
						if(tilelist.Count > 0){
							Tile t = tilelist[Global.Roll(1,tilelist.Count)-1];
							Move(t.row,t.col);
						}
						QS();
					}
					else{ //do nothing
						QS();
					}
				}
				else{ //forget about target, do nothing
					target = null;
					QS();
				}
				break;
			case ActorType.ORC_WARMAGE:
				foreach(Actor a in ActorsWithinDistance(2)){
					if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
						QS();
						return;
					}
				}
				if(!HasAttr(AttrType.BLOODSCENT)){
					CastSpell(SpellType.BLOODSCENT);
				}
				else{
					QS();
				}
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
						|| HasAttr(AttrType.NEVER_MOVES) || M.actor[target_location.p].HasAttr(AttrType.NEVER_MOVES)){
							QS(); //todo: should target_location be cleared here?
						}
						else{
							Move(target_location.row,target_location.col); //swap places
							target_location = null;
							QS();
						}
					}
					else{
						if(AI_Step(target_location)){
							QS();
							if(DistanceFrom(target_location) == 0){
								target_location = null;
							}
						}
						else{ //could not move, end turn.
							if(DistanceFrom(target_location) == 1 && !target_location.passable){
								target_location = null;
							}
							QS();
						}
					}
				}
				else{
					if(DistanceFrom(target) <= 5){
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
						if(group != null && group[0] != this){
							if(DistanceFrom(group[0]) > 1){
								int dir = DirectionOf(group[0]);
								bool found = false;
								for(int i=-1;i<=1;++i){
									Actor a = ActorInDirection(RotateDirection(dir,true,i));
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
				break;
			}
		}
		public void IdleAI(){
			if(PathStep()){
				return;
			}
			/*if(HasAttr(AttrType.LIGHT_ALLERGY) && tile().IsLit()){
				List<Tile> dark = TilesAtDistance(1).Where(t=>t.passable && !t.IsLit() && t.actor() == null);
				if(dark.Count > 0 && AI_Step(dark.Random())){
					QS();
				}
				else{
					AI_Step(TileInDirection(Global.RandomDirection()));
					QS();
				}
				return;
			}*/
			switch(type){
			case ActorType.LARGE_BAT: //flies around
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
									B.Add(the_name + " brushes up against " + ActorInDirection(dir).TheVisible() + ". ",this);
								}
							}
						}
						QS();
					}
				}
				return;
			}
			case ActorType.ORC_WARMAGE:
				foreach(Actor a in ActorsWithinDistance(2)){
					if(a.HasAttr(AttrType.SPELL_DISRUPTION) && a.HasLOE(this)){
						QS();
						return;
					}
				}
				if(!HasAttr(AttrType.BLOODSCENT)){
					CastSpell(SpellType.BLOODSCENT);
					return; //<--!
				}
				break;
			/*case ActorType.SHAMBLING_SCARECROW:
				if(Global.CoinFlip()){
					AI_Step(TileInDirection(Global.RandomDirection()));
				}
				else{
					if(Global.Roll(1,3) == 3 && DistanceFrom(player) <= 10){
						if(player.CanSee(this)){
							B.Add(the_name + " emits an eerie whistling sound. ");
						}
						else{
							B.Add("You hear an eerie whistling sound. ");
						}
					}
				}
				Q1();
				return; //<--!*/
			case ActorType.SWORDSMAN:
			case ActorType.PHANTOM_SWORDMASTER:
				if(attrs[AttrType.BONUS_COMBAT] > 0){
					attrs[AttrType.BONUS_COMBAT] = 0;
				}
				break;
			case ActorType.FIRE_DRAKE:
				FindPath(player);
				QS();
				return; //<--!
			default:
				break;
			}
			if(HasAttr(AttrType.WANDERING)){
				if(Global.Roll(10) <= 6){
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
						attrs[Forays.AttrType.WANDERING] = 0;
					}
				}
				else{
					if(Global.OneIn(4)){
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
							attrs[Forays.AttrType.WANDERING] = 0;
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
							attrs[Forays.AttrType.WANDERING] = 0;
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
							Actor a = ActorInDirection(RotateDirection(dir,true,i));
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
							if(actor.HasAttr(AttrType.ENHANCED_TORCH)){
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
							if(actor.attrs[AttrType.ON_FIRE] < actor.light_radius){ //if the player should notice...
								B.Add(actor.Your() + " light grows dimmer. ",actor);
								actor.UpdateRadius(actor.light_radius,actor.light_radius - difference,true);
								if(actor.attrs[AttrType.ON_FIRE] > actor.light_radius){
									actor.UpdateRadius(actor.light_radius,actor.attrs[AttrType.ON_FIRE]);
								}
							}
						}
					}
					else{
						if(dimness < actor.attrs[AttrType.DIM_LIGHT]){
							int difference = dimness - actor.attrs[AttrType.DIM_LIGHT];
							actor.attrs[AttrType.DIM_LIGHT] = dimness;
							if(actor.light_radius > 0){
								if(actor.attrs[AttrType.ON_FIRE] < actor.light_radius - difference){ //if the player should notice...
									B.Add(actor.Your() + " light grows brighter. ",actor);
									actor.UpdateRadius(actor.LightRadius(),actor.light_radius - difference,true);
								}
							}
						}
					}
				}
			}
		}
		public bool AI_Step(PhysicalObject obj){ return AI_Step(obj,false); }
		public bool AI_Step(PhysicalObject obj,bool flee){
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
			bool cw = Global.CoinFlip();
			dirs.Add(RotateDirection(dirs[0],cw));
			dirs.Add(RotateDirection(dirs[0],!cw)); //building a list of directions to try: first the primary direction,
			cw = Global.CoinFlip(); 				//then the ones next to it, then the ones next to THOSE(in random order)
			dirs.Add(RotateDirection(RotateDirection(dirs[0],cw),cw));
			dirs.Add(RotateDirection(RotateDirection(dirs[0],!cw),!cw)); //this completes the list of 5 directions.
			foreach(int i in dirs){
				if(ActorInDirection(i) != null && ActorInDirection(i).IsHiddenFrom(this)){
					player_visibility_duration = -1;
					if(ActorInDirection(i) == player){
						attrs[Forays.AttrType.PLAYER_NOTICED]++;
					}
					target = player; //not extensible yet
					target_location = M.tile[player.row,player.col];
					string walks = " walks straight into you! ";
					if(HasAttr(AttrType.FLYING)){
						walks = " flies straight into you! ";
					}
					if(!IsHiddenFrom(player)){
						B.Add(TheVisible() + walks);
						if(player.CanSee(this)){
							B.Add(the_name + " looks startled. ");
						}
					}
					else{
						attrs[AttrType.TURNS_VISIBLE] = -1;
						attrs[Forays.AttrType.NOTICED]++;
						B.Add(AVisible() + walks);
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
		public bool AI_MoveOrOpen(int dir){
			return AI_MoveOrOpen(TileInDirection(dir).row,TileInDirection(dir).col);
		}
		public bool AI_MoveOrOpen(int r,int c){
			if(M.tile[r,c].passable && M.actor[r,c] == null && !GrabPreventsMovement(M.tile[r,c]) && M.tile[r,c].type != TileType.CHASM){
				Move(r,c);
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
				int idx = Global.Roll(1,tiles.Count)-1;
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
			if(path.Count > 0 && !HasAttr(AttrType.NEVER_MOVES)){
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
						if(path.Count == 1){
							if(!never_clear_path){
								path.Clear();
							}
						}
						else{
							AI_Step(M.tile[path[0]]);
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
				else{
					AI_Step(M.tile[path[0]]);
					if(DistanceFrom(path[0]) == 0){
						path.RemoveAt(0);
					}
					else{
						if(path.Count > 0 && M.tile[path[0]].type == TileType.CHASM){
							path.Clear();
						}
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
		public bool Attack(int attack_idx,Actor a){ //returns true if attack hit
			if(StunnedThisTurn()){
				return false;
			}
			//pos pos_of_target = new pos(a.row,a.col);
			AttackInfo info = AttackList.Attack(type,attack_idx);
			if(weapons.First.Value != WeaponType.NO_WEAPON){
				info.damage = Weapon.Damage(weapons.First.Value);
			}
			info.damage.source = this;
			int plus_to_hit = TotalSkill(SkillType.COMBAT);
			bool sneak_attack = false;
			if(this.IsHiddenFrom(a) || !a.CanSee(this) || (this == player && HasAttr(AttrType.SHADOW_CLOAK) && !tile().IsLit() && !a.HasAttr(AttrType.BLINDSIGHT))){
				sneak_attack = true;
			}
			if(sneak_attack){ //sneak attacks get +25% accuracy. this usually totals 100% vs. unarmored targets.
				plus_to_hit += 25;
			}
			if(HasAttr(AttrType.BLESSED)){
				plus_to_hit += 10;
			}
			plus_to_hit -= a.ArmorClass() * 2;
			bool hit = a.IsHit(plus_to_hit);
			if(HasFeat(FeatType.DRIVE_BACK)){
				bool nowhere_to_run = true;
				int dir = DirectionOf(a);
				if(a.TileInDirection(dir).passable && a.ActorInDirection(dir) == null){
					nowhere_to_run = false;
				}
				if(a.TileInDirection(RotateDirection(dir,true)).passable && a.ActorInDirection(RotateDirection(dir,true)) == null){
					nowhere_to_run = false;
				}
				if(a.TileInDirection(RotateDirection(dir,false)).passable && a.ActorInDirection(RotateDirection(dir,false)) == null){
					nowhere_to_run = false;
				}
				if(a.HasAttr(AttrType.FROZEN) || a.HasAttr(AttrType.NEVER_MOVES)){
					nowhere_to_run = true;
				}
				if(nowhere_to_run){
					hit = true;
				}
			}
			bool no_armor_message = false; //no_armor_message means "don't print 'your armor blocks the attack' for misses"
			if(a.HasAttr(AttrType.DEFENSIVE_STANCE) && Global.CoinFlip()){
				hit = false;
				no_armor_message = true;
			}
			if((this.tile().Is(FeatureType.FOG) || a.tile().Is(FeatureType.FOG)) && Global.CoinFlip()){
				hit = false;
				no_armor_message = true;
			}
			if(a.IsHiddenFrom(this) || !CanSee(a) || (a == player && a.HasAttr(AttrType.SHADOW_CLOAK) && !a.tile().IsLit() && !HasAttr(AttrType.BLINDSIGHT))){
				if(Global.CoinFlip()){
					hit = false;
					no_armor_message = true;
				}
			}
			bool player_in_combat = false;
			if(this == player || a == player){
				player_in_combat = true;
			}
			if(attack_idx==2 && (type==ActorType.FROSTLING || type==ActorType.FIRE_DRAKE)){
				hit = true; //hack! these are the 2 'area' attacks that always hit
				player_in_combat = false;
			}
			if(a == player && type == ActorType.DREAM_CLONE){
				player_in_combat = false;
			}
			if(player_in_combat){
				player.attrs[Forays.AttrType.IN_COMBAT]++;
			}
			string s = info.desc + ". ";
			if(hit){
				if(HasFeat(FeatType.NECK_SNAP) && a.HasAttr(AttrType.MEDIUM_HUMANOID) && IsHiddenFrom(a)){
					if(!HasAttr(AttrType.RESIST_NECK_SNAP)){
						B.Add(You("silently snap") + " " + a.Your() + " neck. ");
						a.TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,9001,this);
						Q1();
						return true;
					}
					else{
						B.Add(You("silently snap") + " " + a.Your() + " neck. ");
						B.Add("It doesn't seem to affect " + a.the_name + ". ");
					}
				}
				int dice = info.damage.dice;
				bool crit = false;
				int pos = s.IndexOf('&');
				if(pos != -1){
					s = s.Substring(0,pos) + TheVisible() + s.Substring(pos+1);
				}
				pos = s.IndexOf('^');
				if(pos != -1){
					string sc = "";
					int critical_target = 20;
					if(weapons.First.Value == WeaponType.DAGGER){
						critical_target -= 2;
					}
					if(HasFeat(FeatType.LETHALITY)){ //10% crit plus 5% for each 20% health the target is missing
						critical_target -= 2;
						int fifth = a.maxhp / 5; //uses int because it assumes everything has a multiple of 5hp
						int totaldamage = a.maxhp - a.curhp;
						if(fifth > 0){
							int missing_fifths = totaldamage / fifth;
							critical_target -= missing_fifths;
						}
					}
					if((info.damage.type == DamageType.NORMAL || info.damage.type == DamageType.PIERCING
					|| info.damage.type == DamageType.BASHING || info.damage.type == DamageType.SLASHING)
					&& Global.Roll(1,20) >= critical_target){ //maybe this should become a check for physical damage - todo?
						crit = true;
						sc = "critically ";
					}
					s = s.Substring(0,pos) + sc + s.Substring(pos+1);
				}
				pos = s.IndexOf('*');
				if(pos != -1){
					s = s.Substring(0,pos) + a.TheVisible() + s.Substring(pos+1);
				}
				if(sneak_attack && crit){
					if(!a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT) 
						&& !a.HasAttr(AttrType.PLANTLIKE) && !a.HasAttr(AttrType.BOSS_MONSTER)){
						if(a.type != ActorType.PLAYER){ //being nice to the player here...
							switch(weapons.First.Value){
							case WeaponType.SWORD:
							case WeaponType.FLAMEBRAND:
								B.Add("You run " + a.TheVisible() + " through! ");
								break;
							case WeaponType.MACE:
							case WeaponType.MACE_OF_FORCE:
								B.Add("You bash " + a.YourVisible() + " head in! ");
								break;
							case WeaponType.DAGGER:
							case WeaponType.VENOMOUS_DAGGER:
								B.Add("You pierce one of " + a.YourVisible() + " vital organs! ");
								break;
							case WeaponType.STAFF:
							case WeaponType.STAFF_OF_MAGIC:
								B.Add("You bring your staff down on " + a.YourVisible() + " head with a loud crack! ");
								break;
							case WeaponType.BOW:
							case WeaponType.HOLY_LONGBOW:
								B.Add("You choke " + a.TheVisible() + " with your bowstring! ");
								break;
							default:
								break;
							}
							MakeNoise();
							a.TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,1337,this);
							Q1();
							return true;
						}
						else{ //...but not too nice
							B.Add(AVisible() + " strikes from hiding! ");
							B.Add("The deadly attack leaves you stunned! ");
							int lotsofdamage = Math.Max(dice*6,a.curhp/2);
							a.attrs[AttrType.STUNNED]++;
							Q.Add(new Event(a,Global.Roll(2,5)*100,AttrType.STUNNED,"You are no longer stunned. "));
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,lotsofdamage,this,a_name);
						}
					}
				}
				if(sneak_attack){
					B.Add(YouVisible("strike") + " from hiding! ");
					if(type != ActorType.PLAYER){
						attrs[AttrType.TURNS_VISIBLE] = -1;
						attrs[Forays.AttrType.NOTICED]++;
					}
					else{
						a.player_visibility_duration = -1;
						a.attrs[Forays.AttrType.PLAYER_NOTICED]++;
					}
				}
				B.Add(s,this,a);
				int dmg;
				if(crit){
					dmg = dice * 6;
				}
				else{
					dmg = Global.Roll(dice,6);
				}
				dmg += TotalSkill(SkillType.COMBAT);
				int r = a.row;
				int c = a.col;
				bool troll = (a.type == ActorType.TROLL || a.type == ActorType.TROLL_SEER);
				bool mech_shield = a.HasAttr(AttrType.MECHANICAL_SHIELD);
				if(crit && mech_shield){
					a.attrs[Forays.AttrType.MECHANICAL_SHIELD] = 0;
				}
				a.TakeDamage(info.damage.type,info.damage.damclass,dmg,this,a_name);
				if(crit && mech_shield){
					a.attrs[Forays.AttrType.MECHANICAL_SHIELD]++;
				}
				if(M.actor[r,c] != null){
					if(HasAttr(AttrType.FIRE_HIT) || attrs[AttrType.ON_FIRE] >= 3){ //todo: a frostling's ranged attack shouldn't apply this
						if(!a.HasAttr(AttrType.INVULNERABLE)){ //to prevent the message
							int amount = Global.Roll(6);
							if(!a.HasAttr(AttrType.RESIST_FIRE) || amount / a.attrs[AttrType.RESIST_FIRE] > 0){ //todo i think resistance is wrong here
								B.Add(a.YouAre() + " burned. ",a);
							}
							a.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,amount,this,a_name);
						}
					}
				}
				if(troll && HasAttr(AttrType.FIRE_HIT) && M.tile[r,c].Is(FeatureType.TROLL_CORPSE)){
					M.tile[r,c].features.Remove(FeatureType.TROLL_CORPSE);
					B.Add("The troll corpse burns to ashes! ",M.tile[r,c]);
				}
				if(troll && HasAttr(AttrType.FIRE_HIT) && M.tile[r,c].Is(FeatureType.TROLL_SEER_CORPSE)){
					M.tile[r,c].features.Remove(FeatureType.TROLL_SEER_CORPSE);
					B.Add("The troll seer corpse burns to ashes! ",M.tile[r,c]);
				}
				if(HasAttr(AttrType.COLD_HIT) && attack_idx==0 && M.actor[r,c] != null){
					//hack: only applies to attack 0
					if(!a.HasAttr(AttrType.INVULNERABLE)){ //to prevent the message
						B.Add(a.YouAre() + " chilled. ",a);
						a.TakeDamage(DamageType.COLD,DamageClass.PHYSICAL,Global.Roll(1,6),this,a_name);
					}
				}
				if(HasAttr(AttrType.POISON_HIT) && M.actor[r,c] != null){
					if(!a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT)
					&& !a.HasAttr(AttrType.POISON_HIT) && !a.HasAttr(AttrType.IMMUNE_TOXINS)){
						if(a.HasAttr(AttrType.POISONED)){
							B.Add(a.YouAre() + " more poisoned. ",a);
						}
						else{
							B.Add(a.YouAre() + " poisoned. ",a);
						}
						a.attrs[AttrType.POISONED]++;
						Q.Add(new Event(a,(Global.Roll(6)+6)*100,AttrType.POISONED));
					}
				}
				if(HasAttr(AttrType.PARALYSIS_HIT) && attack_idx==1 && type == ActorType.CARRION_CRAWLER && M.actor[r,c] != null){
					if(!a.HasAttr(AttrType.IMMUNE_TOXINS)){
						//hack: carrion crawler only
						B.Add(a.YouAre() + " paralyzed. ",a);
						a.attrs[AttrType.PARALYZED] = Global.Roll(1,3)+3;
					}
				}
				if(HasAttr(AttrType.FORCE_HIT) && M.actor[r,c] != null){
					if(Global.OneIn(3)){
						if(Global.CoinFlip()){
							a.GetKnockedBack(this);
						}
						else{
							if(!a.HasAttr(AttrType.STUNNED)){
								B.Add(a.YouAre() + " stunned. ",a);
								a.attrs[AttrType.STUNNED]++;
								int duration = (Global.Roll(4)+3)*100;
								if(crit){
									duration += 250;
									crit = false; //note this - don't try to use crit again after this on-hit stuff.
								}
								Q.Add(new Event(a,duration,AttrType.STUNNED,a.YouAre() + " no longer stunned. ",a));
							}
						}
					}
				}
				if(HasAttr(AttrType.DIM_VISION_HIT) && M.actor[r,c] != null){
					string str = "";
					if(a.type == ActorType.PLAYER){
						B.Add("Your vision grows weak. ");
						str = "Your vision returns to normal. ";
					}
					//a.attrs[AttrType.DIM_VISION]++;
					//Q.Add(new Event(a,a.DurationOfMagicalEffect(Global.Roll(2,20)+20)*100,AttrType.DIM_VISION,str));
					a.GainAttrRefreshDuration(AttrType.DIM_VISION,a.DurationOfMagicalEffect(Global.Roll(2,20)+20)*100,str);
				}
				if(HasAttr(AttrType.STALAGMITE_HIT)){
					List<Tile> tiles = new List<Tile>();
					foreach(Tile t in M.tile[r,c].TilesWithinDistance(1)){
						if(t.actor() == null && (t.type == TileType.FLOOR || t.type == TileType.STALAGMITE)){
							if(Global.CoinFlip()){ //50% for each...
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
				}
				if(HasAttr(AttrType.GRAB_HIT) && M.actor[r,c] != null && !HasAttr(AttrType.GRABBING) && DistanceFrom(a) == 1){
					a.attrs[Forays.AttrType.GRABBED]++;
					attrs[Forays.AttrType.GRABBING] = DirectionOf(a);
					B.Add(the_name + " grabs " + a.the_name + ". ",this,a);
				}
				if(HasAttr(AttrType.LIFE_DRAIN_HIT) && curhp < maxhp){
					curhp += 10;
					if(curhp > maxhp){
						curhp = maxhp;
					}
					B.Add(YouFeel() + " restored. ",this);
				}
				if(HasAttr(AttrType.STUN_HIT) && M.actor[r,c] != null){
					B.Add(a.YouAre() + " stunned. ",a);
					int duration = 550;
					if(crit){
						duration += 250;
						crit = false;
					}
					a.GainAttrRefreshDuration(AttrType.STUNNED,duration,a.YouAre() + " no longer stunned. ",a);
				}
				if(crit && M.actor[r,c] != null){
					B.Add(a.YouAre() + " stunned. ",a);
					a.GainAttrRefreshDuration(AttrType.STUNNED,250,a.YouAre() + " no longer stunned. ",a);
				}
				if(M.actor[r,c] != null && a.type == ActorType.SWORDSMAN){
					if(a.attrs[AttrType.BONUS_COMBAT] > 0){
						B.Add(a.the_name + " returns to a defensive stance. ",a);
						a.attrs[AttrType.BONUS_COMBAT] = 0;
					}
					a.attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(a,100,AttrType.COOLDOWN_1));
				}
			}
			else{
				if(a.HasAttr(AttrType.DEFENSIVE_STANCE) || (a.HasFeat(FeatType.FULL_DEFENSE) && Global.CoinFlip())){
					//make an attack against a random enemy next to a
					List<Actor> list = a.ActorsWithinDistance(1,true);
					list.Remove(this); //don't consider yourself or the original target
					if(list.Count > 0){
						B.Add(a.You("deflect") + " the attack. ",this,a);
						return Attack(attack_idx,list[Global.Roll(1,list.Count)-1]);
					}
					//this would currently enter an infinite loop if two adjacent things used it at the same time
				}
				if(this==player || a==player || player.CanSee(this) || player.CanSee(a)){ //didn't change this yet
					if(s == "& lunges forward and ^hits *. "){
						B.Add(the_name + " lunges forward and misses " + a.the_name + ". ");
					}
					else{
						if(s == "& hits * with a blast of cold. "){
							B.Add(the_name + " nearly hits " + a.the_name + " with a blast of cold. ");
						}
						else{
							if(s.Length >= 20 && s.Substring(0,20) == "& extends a tentacle"){
								B.Add(the_name + " misses " + a.the_name + " with a tentacle. ");
							}
							else{
								if(HasFeat(FeatType.DRIVE_BACK)){
									B.Add(You("drive") + " " + a.TheVisible() + " back. ");
								}
								else{
									if(a.ArmorClass() > 0 && !no_armor_message){
										if(a.type != ActorType.PLAYER){
											B.Add(a.YourVisible() + " armor blocks " + YourVisible() + " attack. ");
										}
										else{
											int miss_chance = 25 - plus_to_hit;
											if(Global.Roll(miss_chance) <= Armor.Protection(a.armors.First.Value) * 2){
												B.Add(a.YourVisible() + " armor blocks " + YourVisible() + " attack. ");
											}
											else{
												B.Add(YouVisible("miss",true) + " " + a.TheVisible() + ". ");
											}
										}
									}
									else{
										B.Add(YouVisible("miss",true) + " " + a.TheVisible() + ". ");
									}
								}
							}
						}
					}
				}
				if(HasFeat(FeatType.DRIVE_BACK)){
					if(!a.HasAttr(AttrType.FROZEN) && !HasAttr(AttrType.FROZEN)){
						a.AI_Step(this,true);
						AI_Step(a);
					}
				}
				if(a.type == ActorType.SWORDSMAN){
					if(a.attrs[AttrType.BONUS_COMBAT] > 0){
						B.Add(a.the_name + " returns to a defensive stance. ",a);
						a.attrs[AttrType.BONUS_COMBAT] = 0;
					}
					a.attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(a,100,AttrType.COOLDOWN_1));
				}
			}
			MakeNoise();
			Q.Add(new Event(this,info.cost));
			return hit;
		}
		public void FireArrow(PhysicalObject obj){ FireArrow(GetBestExtendedLine(obj)); }
		public void FireArrow(List<Tile> line){
			if(StunnedThisTurn()){
				return;
			}
			int mod = -30; //bows have base accuracy 45%
			if(HasAttr(AttrType.KEEN_EYES)){
				mod = -20; //keen eyes makes it 55%
			}
			mod += TotalSkill(SkillType.COMBAT);
			//Tile t = M.tile[obj.row,obj.col];
			Tile t = null;
			Actor a = null;
			bool actor_present = false;
			List<string> misses = new List<string>();
			List<Actor> missed = new List<Actor>();
			line.RemoveAt(0); //remove the source of the arrow first
			if(line.Count > 12){
				line = line.GetRange(0,Math.Min(12,line.Count));
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
			if(HasAttr(AttrType.FIERY_ARROWS)){
				B.Add(You("fire") + " a flaming arrow. ",this);
			}
			else{
				B.Add(You("fire") + " an arrow. ",this);
			}
			B.DisplayNow();
			if(a != null){
				Screen.AnimateBoltProjectile(line.To(a),Color.DarkYellow,20);
			}
			else{
				Screen.AnimateBoltProjectile(line.To(t),Color.DarkYellow,20);
			}
			int idx = 0;
			foreach(string s in misses){
				B.Add(s,missed[idx]);
				++idx;
			}
			if(a != null){
				if(a.HasAttr(AttrType.IMMUNE_ARROWS)){
					B.Add("The arrow sticks out ineffectively from " + a.the_name + ". ",a);
				}
				else{
					bool alive = true;
					int critical_target = 20;
					if(HasFeat(FeatType.LETHALITY)){ //10% crit plus 5% for each 20% health the target is missing
						critical_target -= 2;
						int fifth = a.maxhp / 5; //uses int because it assumes everything has a multiple of 5hp
						int totaldamage = a.maxhp - a.curhp;
						int missing_fifths = totaldamage / fifth;
						critical_target -= missing_fifths;
					}
					if(Global.Roll(1,20) >= critical_target){
						B.Add("The arrow critically hits " + a.the_name + ". ",a);
						if(!a.TakeDamage(DamageType.PIERCING,DamageClass.PHYSICAL,18+TotalSkill(SkillType.COMBAT),this,Your() + " arrow")){
							alive = false;
						}
					}
					else{
						B.Add("The arrow hits " + a.the_name + ". ",a);
						if(!a.TakeDamage(DamageType.PIERCING,DamageClass.PHYSICAL,Global.Roll(3,6)+TotalSkill(SkillType.COMBAT),this,Your() + " arrow")){
							alive = false;
						}
					}
					if(alive && (a.HasAttr(AttrType.DEMON) || a.HasAttr(AttrType.UNDEAD))){
						foreach(WeaponType w in weapons){
							if(w == WeaponType.HOLY_LONGBOW){
								B.Add(a.the_name + " is blasted with holy energy! ",a);
								if(!a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(3,6),this)){
									alive = false;
								}
								break;
							}
						}
					}
					if(alive && HasAttr(AttrType.FIERY_ARROWS) && !a.HasAttr(AttrType.IMMUNE_FIRE) && !a.HasAttr(AttrType.INVULNERABLE)){
						if(!a.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,Global.Roll(6),this,Your() + " arrow")){
							alive = false;
						}
					}
				}
			}
			else{
				if(!actor_present){
					B.Add("The arrow hits " + t.the_name + ". ",t);
				}
			}
			Q1();
		}
		public bool IsHit(int plus_to_hit){
			if(Global.Roll(1,100) + plus_to_hit <= 25){ //base hit chance is 75%
				return false;
			}
			return true;
		}
		public bool TakeDamage(DamageType dmgtype,DamageClass damclass,int dmg,Actor source){
			return TakeDamage(new Damage(dmgtype,damclass,source,dmg),"");
		}
		public bool TakeDamage(DamageType dmgtype,DamageClass damclass,int dmg,Actor source,string cause_of_death){
			return TakeDamage(new Damage(dmgtype,damclass,source,dmg),cause_of_death);
		}
		public bool TakeDamage(Damage dmg,string cause_of_death){ //returns true if still alive
			bool damage_dealt = false;
			int old_hp = curhp;
			if(HasAttr(AttrType.FROZEN)){
				//attrs[Forays.AttrType.FROZEN] -= (dmg.amount+1) / 2;
				attrs[Forays.AttrType.FROZEN] -= (dmg.amount * 9) / 10;
				if(attrs[Forays.AttrType.FROZEN] <= 0){
					attrs[Forays.AttrType.FROZEN] = 0;
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
			if(HasAttr(AttrType.TOUGH) && dmg.damclass == DamageClass.PHYSICAL){
				dmg.amount -= 2;
			}
			if(dmg.damclass == DamageClass.MAGICAL){
				dmg.amount -= TotalSkill(SkillType.SPIRIT) / 2;
			}
			if(HasAttr(AttrType.ARCANE_SHIELDED)){
				if(attrs[Forays.AttrType.ARCANE_SHIELDED] >= dmg.amount){
					attrs[Forays.AttrType.ARCANE_SHIELDED] -= dmg.amount;
					if(attrs[Forays.AttrType.ARCANE_SHIELDED] < 0){
						attrs[Forays.AttrType.ARCANE_SHIELDED] = 0;
					}
					dmg.amount = 0;
				}
				else{
					dmg.amount -= attrs[Forays.AttrType.ARCANE_SHIELDED];
					attrs[Forays.AttrType.ARCANE_SHIELDED] = 0;
				}
				if(!HasAttr(AttrType.ARCANE_SHIELDED)){
					B.Add(Your() + " arcane shield crumbles. ",this);
				}
			}
			bool resisted = false;
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
			case DamageType.SLASHING:
				{
				int div = 1;
				if(HasAttr(AttrType.RESIST_SLASH)){
					for(int i=attrs[AttrType.RESIST_SLASH];i>0;--i){
						div = div * 2;
					}
					B.Add(You("resist") + ". ",this);
					resisted = true;
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
			case DamageType.BASHING:
				{
				int div = 1;
				if(HasAttr(AttrType.RESIST_BASH)){
					for(int i=attrs[AttrType.RESIST_BASH];i>0;--i){
						div = div * 2;
					}
					B.Add(You("resist") + ". ",this);
					resisted = true;
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
			case DamageType.PIERCING:
				{
				int div = 1;
				if(HasAttr(AttrType.RESIST_PIERCE)){
					for(int i=attrs[AttrType.RESIST_PIERCE];i>0;--i){
						div = div * 2;
					}
					B.Add(You("resist") + ". ",this);
					resisted = true;
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
				else{
					if(HasAttr(AttrType.RESIST_FIRE)){
						for(int i=attrs[AttrType.RESIST_FIRE];i>0;--i){
							div = div * 2;
						}
						B.Add(You("resist") + ". ",this);
					resisted = true;
					}
				}
				dmg.amount = dmg.amount / div;
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
					/*if(type == ActorType.SHAMBLING_SCARECROW && speed != 50){
						speed = 50;
						if(attrs[AttrType.ON_FIRE] >= LightRadius()){
							UpdateRadius(LightRadius(),LightRadius()+1);
						}
						attrs[AttrType.ON_FIRE]++;
						B.Add(the_name + " leaps about as it catches fire! ",this);
					}*/
				}
				else{
					if(type != ActorType.CORPSETOWER_BEHEMOTH){
						B.Add(YouAre() + " unburnt. ",this);
					}
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
				else{
					if(HasAttr(AttrType.RESIST_COLD)){
						for(int i=attrs[AttrType.RESIST_COLD];i>0;--i){
							div = div * 2;
						}
						B.Add(You("resist") + ". ",this);
					resisted = true;
					}
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
				if(HasAttr(AttrType.RESIST_ELECTRICITY)){
					for(int i=attrs[AttrType.RESIST_ELECTRICITY];i>0;--i){
						div = div * 2;
					}
					B.Add(You("resist") + ". ",this);
					resisted = true;
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
				if(HasAttr(AttrType.UNDEAD) || HasAttr(AttrType.CONSTRUCT) || HasAttr(AttrType.IMMUNE_TOXINS)){
					dmg.amount = 0;
				}
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
					if(type == ActorType.PLAYER){
						if(tile().Is(FeatureType.POISON_GAS)){
							B.Add("The poisonous gas burns your skin! ");
						}
						else{
							B.Add("You feel the poison coursing through your veins! ");
						}
					}
					else{
						if(Global.Roll(1,5) == 5){
							B.Add(the_name + " shudders. ",this);
						}
					}
				}
				break;
			case DamageType.HEAL:
				curhp += dmg.amount;
				if(curhp > maxhp){
					curhp = maxhp;
				}
				break;
			case DamageType.NONE:
				break;
			}
			if(dmg.source != null && dmg.source == player && dmg.damclass == DamageClass.PHYSICAL && resisted){
				Help.TutorialTip(TutorialTopic.Resistance);
			}
			if(damage_dealt){
				if(HasAttr(AttrType.MAGICAL_BLOOD)){
					recover_time = Q.turn + 200;
				}
				else{
					recover_time = Q.turn + 500;
				}
				Interrupt();
				if(HasAttr(AttrType.ASLEEP)){
					attrs[Forays.AttrType.ASLEEP] = 0;
					Global.FlushInput();
				}
				if(dmg.source != null){
					if(type != ActorType.PLAYER && dmg.source != this){
						target = dmg.source;
						target_location = M.tile[dmg.source.row,dmg.source.col];
						if(dmg.source.IsHiddenFrom(this)){
							player_visibility_duration = -1;
						}
						if(type == ActorType.CRUSADING_KNIGHT && dmg.source == player && !HasAttr(AttrType.COOLDOWN_1) && !M.wiz_lite && !CanSee(player) && curhp > 0){
							List<string> verb = new List<string>{"Show yourself","Reveal yourself","Unfold thyself","Present yourself","Unveil yourself","Make yourself known"};
							List<string> adjective = new List<string>{"despicable","filthy","foul","nefarious","vulgar","sorry","unworthy"};
							List<string> noun = new List<string>{"villain","blackguard","devil","scoundrel","wretch","cur","rogue"};
							B.Add(TheVisible() + " shouts \"" + verb.Random() + ", " + adjective.Random() + " " + noun.Random() + "!\" ");
							B.Add(the_name + " raises a gauntlet. ",this);
							B.Add("Sunlight fills the dungeon. ");
							M.wiz_lite = true;
							M.wiz_dark = false;
							attrs[Forays.AttrType.COOLDOWN_1]++;
						}
					}
				}
				if(HasAttr(AttrType.SPORE_BURST) && !HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(Global.Roll(1,5)+1)*100,AttrType.COOLDOWN_1));
					B.Add(You("retaliate") + " with a burst of spores! ",this);
					for(int i=2;i<=8;i+=2){
						AnimateStorm(i,1,(((i*2)+1)*((i*2)+1)) / 4,'*',Color.DarkYellow);
					}
					foreach(Actor a in ActorsWithinDistance(8)){
						if(HasLOE(a.row,a.col) && a != this){
							B.Add("The spores hit " + a.the_name + ". ",a);
							if(!a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT)
							&& !a.HasAttr(AttrType.SPORE_BURST) && !a.HasAttr(AttrType.IMMUNE_TOXINS)){
								int duration = Global.Roll(2,4);
								a.attrs[AttrType.POISONED]++;
								Q.Add(new Event(a,duration*100,AttrType.POISONED));
								if(a.name == "you"){
									B.Add("You are poisoned. ");
								}
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
				if(HasAttr(AttrType.HOLY_SHIELDED) && dmg.source != null){
					B.Add(YourVisible() + " holy shield burns " + dmg.source.TheVisible() + ". ",this,dmg.source);
					int amount = Global.Roll(2,6);
					if(amount >= dmg.source.curhp){
						amount = dmg.source.curhp - 1;
					}
					dmg.source.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,amount,this); //doesn't yet prevent loops involving 2 holy shields.
				}
				if(HasFeat(FeatType.BOILING_BLOOD) && dmg.type != DamageType.POISON && attrs[AttrType.BLOOD_BOILED] < 5){
					//if(!Global.Option(OptionType.NO_BLOOD_BOIL_MESSAGE)){
						B.Add("Your blood boils! ");
					//}
					speed -= 10;
					attrs[AttrType.BLOOD_BOILED]++;
					Q.KillEvents(this,AttrType.BLOOD_BOILED); //eventually replace this with refreshduration
					//GainAttr(AttrType.BLOOD_BOILED,1001,attrs[Forays.AttrType.BLOOD_BOILED],"Your blood cools. ");
					Q.Add(new Event(this,1000,Forays.AttrType.BLOOD_BOILED,attrs[Forays.AttrType.BLOOD_BOILED],"Your blood cools. "));
				}
				if(type == ActorType.MECHANICAL_KNIGHT){
					if(curhp <= 10 && !HasAttr(AttrType.COOLDOWN_1) && !HasAttr(AttrType.COOLDOWN_2)){
						if(Global.CoinFlip()){
							B.Add(Your() + " arms are destroyed! ",this);
							attrs[Forays.AttrType.COOLDOWN_1]++;
							attrs[Forays.AttrType.MECHANICAL_SHIELD] = 0;
						}
						else{
							B.Add(Your() + " legs are destroyed! ",this);
							attrs[Forays.AttrType.COOLDOWN_2]++;
							attrs[Forays.AttrType.NEVER_MOVES]++;
							path.Clear();
							target_location = null;
						}
					}
				}
			}
			if(curhp <= 0){
				if(type == ActorType.PLAYER){
					if(magic_items.Contains(MagicItemType.PENDANT_OF_LIFE)){
						magic_items.Remove(MagicItemType.PENDANT_OF_LIFE);
						curhp = 1;
						B.Add("Your pendant glows brightly, then crumbles to dust. ");
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
							attrs[AttrType.COOLDOWN_1]++;
							Q.Add(new Event(this,350,AttrType.COOLDOWN_1));
							Q.KillEvents(this,AttrType.COOLDOWN_2);
							if(!HasAttr(AttrType.COOLDOWN_2)){
								attrs[AttrType.COOLDOWN_2] = DirectionOf(player);
							}
							B.Add(the_name + " somehow remains standing! He screams with fury! ",this);
						}
						return true;
					}
					if(HasAttr(AttrType.REGENERATES_FROM_DEATH) && dmg.type != DamageType.FIRE){
						B.Add(the_name + " falls to the ground, still twitching. ",this);
						Tile troll = null;
						for(int i=0;i<COLS && troll == null;++i){
							foreach(Tile t in TilesAtDistance(i)){
								if(t.passable && !t.Is(FeatureType.TROLL_CORPSE)
								&& !t.Is(FeatureType.TROLL_SEER_CORPSE) && !t.Is(FeatureType.QUICKFIRE)){
									if(type == ActorType.TROLL){
										t.features.Add(FeatureType.TROLL_CORPSE);
									}
									else{
										t.features.Add(FeatureType.TROLL_SEER_CORPSE);
									}
									troll = t;
									break;
								}
							}
						}
						curhp -= Global.Roll(10)+5;
						if(curhp < -50){
							curhp = -50;
						}
						AttrType attr = HasAttr(AttrType.COOLDOWN_1)? AttrType.COOLDOWN_1 : AttrType.NO_ATTR;
						Q.Add(new Event(troll,null,200,EventType.REGENERATING_FROM_DEATH,attr,curhp,""));
					}
					else{
						if(dmg.amount < 1000 && !HasAttr(AttrType.BOSS_MONSTER)){ //everything that deals this much damage
							if(HasAttr(AttrType.UNDEAD) || HasAttr(AttrType.CONSTRUCT)){ //prints its own message
								B.Add(the_name + " is destroyed. ",this);
							}
							else{
								B.Add(the_name + " dies. ",this);
							}
						}
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
								if(DistanceFrom(t) <= 2 || Global.CoinFlip()){
									t.TransformTo(TileType.RUBBLE);
								}
							}
						}
					}
					if(player.HasAttr(AttrType.CONVICTION)){
						player.attrs[Forays.AttrType.KILLSTREAK]++;
					}
					if((HasAttr(AttrType.HUMANOID_INTELLIGENCE) && type != ActorType.DREAM_CLONE && type != ActorType.FIRE_DRAKE)
					   || type == ActorType.ZOMBIE){
						if(Global.CoinFlip() && !HasAttr(AttrType.NO_ITEM)){
							tile().GetItem(Item.Create(Item.RandomItem(),-1,-1));
						}
					}
					foreach(Item item in inv){
						tile().GetItem(item);
					}
					/*int divisor = 1;
					if(HasAttr(AttrType.SMALL_GROUP)){ divisor = 2; }
					if(HasAttr(AttrType.MEDIUM_GROUP)){ divisor = 3; }
					if(HasAttr(AttrType.LARGE_GROUP)){ divisor = 5; }
					if(!Global.GAME_OVER){
						player.GainXP(xp + (level*(10 + level - player.level))/divisor); //experimentally giving the player any
					}*/
					Q.KillEvents(this,EventType.ANY_EVENT);					// XP that the monster had collected. currently always 0.
					M.RemoveTargets(this);
					int idx = Actor.tiebreakers.IndexOf(this);
					if(idx != -1){
						Actor.tiebreakers[Actor.tiebreakers.IndexOf(this)] = null;
					}
					if(group != null){
						if(group.Count >= 2 && this == group[0] && HasAttr(AttrType.WANDERING)){
							if(type != ActorType.NECROMANCER && type != ActorType.DREAM_WARRIOR){
								group[1].attrs[Forays.AttrType.WANDERING]++;
							}
						}
						if(group.Count <= 2 || type == ActorType.NECROMANCER || type == ActorType.DREAM_WARRIOR){
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
				if(magic_items.Contains(MagicItemType.CLOAK_OF_DISAPPEARANCE) && damage_dealt && dmg.amount >= curhp){
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
									destination = tilelist[Global.Roll(1,tilelist.Count)-1];
									break;
								}
							}
							if(destination != null){
								Move(destination.row,destination.col);
							}
							else{
								for(int i=0;i<9999;++i){
									int rr = Global.Roll(1,ROWS-2);
									int rc = Global.Roll(1,COLS-2);
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
					magic_items.Remove(MagicItemType.CLOAK_OF_DISAPPEARANCE);
				}
			}
			return true;
		}
		public bool GetKnockedBack(PhysicalObject obj){ return GetKnockedBack(obj.GetBestExtendedLine(row,col)); }
		public bool GetKnockedBack(List<Tile> line){
			int idx = line.IndexOf(M.tile[row,col]);
			if(idx == -1){
				B.Add("DEBUG: Error - " + the_name + "'s position doesn't seem to be in the line. ");
				return false;
			}
			Tile next = line[idx+1];
			Actor source = M.actor[line[0].row,line[0].col];
			bool no_movement = (GrabPreventsMovement(next) || HasAttr(AttrType.NEVER_MOVES));
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
						B.Add(YouVisibleAre() + " knocked into " + next.TheVisible() + ". ",this,next);
					}
					TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),source,"*smashed against " + next.a_name);
				}
				else{
					if(M.actor[next.p] != null){
						if(player.CanSee(tile())){
							B.Add(YouVisibleAre() + " knocked into " + M.actor[next.row,next.col].TheVisible() + ". ",this,M.actor[next.row,next.col]); //vis
						}
						string this_name = a_name;
						TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),source,"*smashed against " + M.actor[next.p].a_name);
						M.actor[next.row,next.col].TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),source,"*smashed against " + this_name);
					}
					else{ //grabbed
						if(player.CanSee(tile())){
							B.Add(YouVisibleAre() + " knocked about. ",this);
						}
						Actor grabber = null;
						foreach(Actor a in ActorsAtDistance(1)){
							if(a.attrs[Forays.AttrType.GRABBING] == a.DirectionOf(this)){
								grabber = a;
							}
						}
						string grabber_name = "";
						if(grabber != null){
							grabber_name = grabber.a_name;
						}
						TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(6),source,"*smashed against " + grabber_name);
					}
				}
				if(immobilized && M.actor[r,c] != null){
					if(player.CanSee(tile())){
						B.Add("The ice breaks! ",this);
					}
				}
			}
			return true;
		}
		public bool CastSpell(SpellType spell){ return CastSpell(spell,null,false); }
		public bool CastSpell(SpellType spell,bool force_of_will){ return CastSpell(spell,null,force_of_will); }
		public bool CastSpell(SpellType spell,PhysicalObject obj){ return CastSpell(spell,obj,false); }
		public bool CastSpell(SpellType spell,PhysicalObject obj,bool force_of_will){ //returns false if targeting is canceled.
			if(StunnedThisTurn() && !force_of_will){ //eventually this will be moved to the last possible second
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
			Tile t = null;
			List<Tile> line = null;
			if(obj != null){
				t = M.tile[obj.row,obj.col];
				if(spell == SpellType.FORCE_BEAM){ //force beam requires a line for proper knockback
					line = GetBestExtendedLine(t);
				}
				else{
					line = GetBestLine(t);
				}
			}
			int bonus = 0; //used for bonus damage on spells - currently, only Master's Edge adds bonus damage.
			if(FailRate(spell) > 0){
				int fail = FailRate(spell);
				if(force_of_will){
					fail = magic_penalty * 5;
					fail -= skills[SkillType.SPIRIT]*2;
					if(fail < 0){
						fail = 0;
					}
				}
				if(Global.Roll(1,100) - fail <= 0){
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
			switch(spell){
			case SpellType.SHINE:
				if(!HasAttr(AttrType.ENHANCED_TORCH)){
					B.Add("You cast shine. ");
					if(!M.wiz_dark){
						B.Add("Your torch begins to shine brightly. ");
					}
					attrs[AttrType.ENHANCED_TORCH]++;
					if(light_radius > 0){
						UpdateRadius(LightRadius(),Global.MAX_LIGHT_RADIUS - attrs[AttrType.DIM_LIGHT]*2,true);
					}
					Q.Add(new Event(9500,"Your torch begins to flicker a bit. "));
					Q.Add(new Event(this,10000,AttrType.ENHANCED_TORCH,"Your torch no longer shines as brightly. "));
				}
				else{
					B.Add("Your torch is already shining brightly! ");
					return false;
				}
				break;
/*			case SpellType.MAGIC_MISSILE:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " magic missile. ",this);
					Actor a = FirstActorInLine(t);
					if(a != null){
						AnimateBoltProjectile(a,Color.Magenta);
						B.Add("The missile hits " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(1+bonus,6),this);
					}
					else{
						AnimateBoltProjectile(t,Color.Magenta);
						if(t.IsLit()){
							B.Add("The missile hits " + t.the_name + ". ");
						}
						else{
							B.Add("You attack the darkness. ");
						}
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.DETECT_MONSTERS:
				if(!HasAttr(AttrType.DETECTING_MONSTERS)){
					B.Add(You("cast") + " detect monsters. ",this);
					if(type == ActorType.PLAYER){
						B.Add("You can sense beings around you. ");
						Q.Add(new Event(this,2100,AttrType.DETECTING_MONSTERS,"You can no longer sense beings around you. "));
					}
					else{
						Q.Add(new Event(this,2100,AttrType.DETECTING_MONSTERS));
					}
					attrs[AttrType.DETECTING_MONSTERS]++;
				}
				else{
					B.Add("You are already detecting monsters! ");
					return false;
				}
				break;*/
			case SpellType.IMMOLATE:
				if(t == null){
					line = GetTarget(12);
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " immolate. ",this);
					Actor a = FirstActorInLine(line);
					if(a != null){
						AnimateBeam(line.ToFirstObstruction(),'*',Color.RandomFire);
						if(!a.HasAttr(AttrType.RESIST_FIRE) && !a.HasAttr(AttrType.CATCHING_FIRE) && !a.HasAttr(AttrType.ON_FIRE)){
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
					else{
						foreach(Tile t2 in line){
							if(t2.Is(FeatureType.TROLL_CORPSE) || t2.Is(FeatureType.TROLL_SEER_CORPSE)){
								line = line.To(t2);
							}
						}
						AnimateBeam(line,'*',Color.RandomFire);
						B.Add(You("throw") + " flames. ",this);
						if(line.Last().Is(FeatureType.TROLL_CORPSE)){
							line.Last().features.Remove(FeatureType.TROLL_CORPSE);
							B.Add("The troll corpse burns to ashes! ",line.Last());
						}
						if(line.Last().Is(FeatureType.TROLL_SEER_CORPSE)){
							line.Last().features.Remove(FeatureType.TROLL_SEER_CORPSE);
							B.Add("The troll seer corpse burns to ashes! ",line.Last());
						}
					}
				}
				else{
					return false;
				}
				break;
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
						B.Add(You("strike") + " " + a.TheVisible() + ". ",this,a);
						string s = a.the_name;
						string s2 = a.a_name;
						List<Tile> line2 = GetBestExtendedLine(a.row,a.col);
						int idx = line2.IndexOf(M.tile[a.row,a.col]);
						Tile next = line2[idx+1];
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(1+bonus,6),this,a_name);
						if(Global.Roll(1,10) <= 7){
							if(M.actor[t.row,t.col] != null){
								a.GetKnockedBack(this);
							}
							else{
								if(!next.passable){
									B.Add(s + "'s corpse is knocked into " + next.the_name + ". ",t,next);
								}
								else{
									if(M.actor[next.row,next.col] != null){
										B.Add(s + "'s corpse is knocked into " + M.actor[next.row,next.col].the_name + ". ",t,M.actor[next.row,next.col]);
										M.actor[next.row,next.col].TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),this,s2 + "'s falling corpse");
									}
								}
							}
						}
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
			case SpellType.FREEZE:
				if(t == null){
					line = GetTarget(12);
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " freeze. ",this);
					Actor a = FirstActorInLine(line);
					if(a != null){
						AnimateBoltBeam(line.ToFirstObstruction(),Color.Cyan);
						if(!a.HasAttr(AttrType.FROZEN) && !a.HasAttr(AttrType.UNFROZEN)){
							B.Add(a.YouAre() + " encased in ice. ",a);
							a.attrs[AttrType.FROZEN] = 25;
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
			case SpellType.BLINK:
				for(int i=0;i<9999;++i){
					int a = Global.Roll(1,17) - 9; //-8 to 8
					int b = Global.Roll(1,17) - 9;
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
			case SpellType.SCORCH:
				if(t == null){
					line = GetTarget(12);
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " scorch. ",this);
					Actor a = FirstActorInLine(line);
					if(a != null){
						AnimateProjectile(line.ToFirstObstruction(),'*',Color.RandomFire);
						B.Add("The scorching bolt hits " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,Global.Roll(2+bonus,6),this,a_name);
					}
					else{
						foreach(Tile t2 in line){
							if(t2.Is(FeatureType.TROLL_CORPSE) || t2.Is(FeatureType.TROLL_SEER_CORPSE)){
								line = line.To(t2);
							}
						}
						AnimateProjectile(line,'*',Color.RandomFire);
						B.Add("The scorching bolt hits " + t.the_name + ". ",t);
						if(line.Last().Is(FeatureType.TROLL_CORPSE)){
							line.Last().features.Remove(FeatureType.TROLL_CORPSE);
							B.Add("The troll corpse burns to ashes! ",line.Last());
						}
						if(line.Last().Is(FeatureType.TROLL_SEER_CORPSE)){
							line.Last().features.Remove(FeatureType.TROLL_SEER_CORPSE);
							B.Add("The troll seer corpse burns to ashes! ",line.Last());
						}
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.BLOODSCENT:
				if(!HasAttr(AttrType.BLOODSCENT)){
					B.Add(You("cast") + " bloodscent. ",this);
					attrs[Forays.AttrType.BLOODSCENT]++;
					if(type == ActorType.PLAYER){
						B.Add("You smell fear. ");
						Q.Add(new Event(this,10000,Forays.AttrType.BLOODSCENT,"You lose the scent. "));
					}
					else{
						Q.Add(new Event(this,10000,Forays.AttrType.BLOODSCENT));
					}
				}
				else{
					B.Add("You can already smell the blood of your enemies. ");
					return false;
				}
				break;
			case SpellType.LIGHTNING_BOLT:
				if(t == null){
					line = GetTarget(12);
					if(line != null){
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
					if(bolt_target != null){
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
										List<Tile> bres = o.GetBestLine(o2);
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
						foreach(Actor ac in damage_targets){
							B.Add("The bolt hits " + ac.the_name + ". ",ac);
							ac.TakeDamage(DamageType.ELECTRIC,DamageClass.MAGICAL,Global.Roll(2+bonus,6),this,a_name);
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
			case SpellType.SHADOWSIGHT:
				if(!HasAttr(AttrType.SHADOWSIGHT)){
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
						a.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this);
						if(M.actor[t.row,t.col] != null && Global.Roll(1,10) <= 2){
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
					int duration = (Global.Roll(5)+5)*100;
					Q.Add(new Event(this,duration,AttrType.NIMBUS_ON,"The electric glow fades from " + the_name + ". ",this));
				}
				break;
			}*/
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
						a.TakeDamage(DamageType.ELECTRIC,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this,a_name);
					}
				}
				break;
				}
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
						B.Add(You("smash",true) + " " + a.TheVisible() + ". ",this,a);
						if(a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(4+bonus,6),this,a_name)){
							a.GainAttrRefreshDuration(AttrType.STUNNED,201,a.YouAre() + " no longer stunned. ",a);
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
			case SpellType.RETREAT: //this is a player-only spell for now because it uses target_location to track position
				B.Add("You cast retreat. ");
				if(target_location == null){
					target_location = M.tile[row,col];
					B.Add("You create a rune of transport on " + M.tile[row,col].the_name + ". ");
					target_location.features.Add(FeatureType.RUNE_OF_RETREAT);
				}
				else{
					if(M.actor[target_location.row,target_location.col] == null && target_location.passable){
						B.Add("You activate your rune of transport. ");
						Move(target_location.row,target_location.col);
						target_location.features.Remove(FeatureType.RUNE_OF_RETREAT);
						target_location = null;
					}
					else{
						B.Add("Something blocks your transport. ");
					}
				}
				break;
			case SpellType.GLACIAL_BLAST:
				if(t == null){
					line = GetTarget(12);
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null){
					B.Add(You("cast") + " glacial blast. ",this);
					Actor a = FirstActorInLine(line);
					if(a != null){
						AnimateProjectile(line.ToFirstObstruction(),'*',Color.RandomIce);
						B.Add("The glacial blast hits " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this,a_name);
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
			case SpellType.FLASHFIRE:
				if(t == null){
					line = GetTarget(12,2);
					if(line != null){
						t = line.Last();
					}
				}
				if(t != null){
					Actor a = FirstActorInLine(line);
					if(a != null){
						t = a.tile();
					}
					B.Add(You("cast") + " flashfire. ",this);
					AnimateBoltProjectile(line.ToFirstObstruction(),Color.Red);
					AnimateExplosion(t,2,'*',Color.RandomFire);
					B.Add("Fwoosh! ",this,t);
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
								if(t2.Is(FeatureType.TROLL_SEER_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_SEER_CORPSE);
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
								if(t2.Is(FeatureType.TROLL_SEER_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_SEER_CORPSE);
									B.Add("The troll seer corpse burns to ashes! ",t2);
								}
							}
						}
					}
					while(targets.Count > 0){
						Actor ac = targets.RemoveRandom();
						B.Add("The explosion hits " + ac.the_name + ". ",ac);
						ac.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this,a_name);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.SONIC_BOOM:
				if(t == null){
					line = GetTarget(12);
					if(line != null){
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
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this,a_name);
						if(Global.Roll(1,10) <= 5 && M.actor[r,c] != null && !M.actor[r,c].HasAttr(AttrType.STUNNED)){
							B.Add(a.YouAre() + " stunned. ",a);
							a.attrs[AttrType.STUNNED]++;
							int duration = DurationOfMagicalEffect((Global.Roll(1,4)+2)) * 100;
							Q.Add(new Event(a,duration,AttrType.STUNNED,a.YouAre() + " no longer stunned. ",a));
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
				break;
			case SpellType.COLLAPSE:
				if(t == null){
					line = GetTarget(12,-1);
					if(line != null){
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
						a.TakeDamage(DamageType.BASHING,DamageClass.PHYSICAL,Global.Roll(4+bonus,6),this,a_name);
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
							chosen.actor().TakeDamage(DamageType.BASHING,Forays.DamageClass.PHYSICAL,Global.Roll(2,6),this,a_name);
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
			case SpellType.FORCE_BEAM:
				if(t == null){
					line = GetTarget();
					if(line != null){
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
							string s = firstactor.TheVisible();
							string s2 = firstactor.a_name;
							firstactor.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(1+bonus,6),this,a_name);
							if(M.actor[firsttile.row,firsttile.col] != null){
								firstactor.GetKnockedBack(full_line);
							}
							else{
								if(!nexttile.passable){
									B.Add(s + "'s corpse is knocked into " + nexttile.the_name + ". ",firsttile,nexttile);
								}
								else{
									if(nextactor != null){
										B.Add(s + "'s corpse is knocked into " + nextactor.TheVisible() + ". ",firsttile,nextactor);
										nextactor.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),this,s2 + "'s falling corpse");
									}
								}
							}
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
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(8+bonus,6),this);
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
						a.AnimateStorm(2,4,4,'*',Color.RandomPrismatic);
						B.Add("You fade from " + a.TheVisible() + "'s awareness. ");
						a.player_visibility_duration = 0;
						a.target = null;
						a.target_location = null;
						a.attrs[Forays.AttrType.AMNESIA_STUN]++;
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
			case SpellType.BLIZZARD:
				{
				List<Actor> targets = ActorsWithinDistance(5,true);
				B.Add(You("cast") + " blizzard. ",this);
				AnimateStorm(5,8,24,'*',Color.RandomIce);
				B.Add("A massive ice storm surrounds " + the_name + ". ",this);
				while(targets.Count > 0){
					int idx = Global.Roll(1,targets.Count) - 1;
					Actor a = targets[idx];
					targets.Remove(a);
					B.Add("The blizzard hits " + a.the_name + ". ",a);
					int r = a.row;
					int c = a.col;
					a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,Global.Roll(5+bonus,6),this,a_name);
					if(M.actor[r,c] != null && Global.Roll(1,10) <= 8){
						B.Add(a.the_name + " is encased in ice. ",a);
						a.attrs[AttrType.FROZEN] = 25;
					}
				}
				break;
				}
			case SpellType.BLESS:
				if(!HasAttr(AttrType.BLESSED)){
					B.Add(You("cast") + " bless. ",this);
					B.Add(You("shine") + " briefly with inner light. ",this);
					attrs[AttrType.BLESSED]++;
					Q.Add(new Event(this,400,AttrType.BLESSED));
				}
				else{
					B.Add(YouAre() + " already blessed! ",this);
					return false;
				}
				break;
			case SpellType.MINOR_HEAL:
				B.Add(You("cast") + " minor heal. ",this);
				B.Add("A bluish glow surrounds " + the_name + ". ",this);
				TakeDamage(DamageType.HEAL,DamageClass.NO_TYPE,Global.Roll(4,6),null);
				break;
			case SpellType.HOLY_SHIELD:
				if(!HasAttr(AttrType.HOLY_SHIELDED)){
					B.Add(You("cast") + " holy shield. ",this);
					B.Add("A fiery halo appears above " + the_name + ". ",this);
					attrs[AttrType.HOLY_SHIELDED]++;
					int duration = (Global.Roll(3,2)+1) * 100;
					Q.Add(new Event(this,duration,AttrType.HOLY_SHIELDED,the_name + "'s halo fades. ",this));
				}
				else{
					B.Add(Your() + " holy shield is already active. ",this);
					return false;
				}
				break;
			}
			if(type == ActorType.PLAYER && spell != SpellType.AMNESIA){
				MakeNoise();
			}
			if(!force_of_will){
				if(Spell.Level(spell) - TotalSkill(SkillType.MAGIC) > 0){
					if(HasFeat(FeatType.STUDENTS_LUCK)){
						if(Global.CoinFlip()){
							magic_penalty++;
							B.Add(YouFeel() + " drained. ",this);
						}
						else{
							if(type == ActorType.PLAYER){
								B.Add("You feel lucky. "); //punk
							}
						}
					}
					else{
						magic_penalty++;
						B.Add(YouFeel() + " drained. ",this);
					}
				}
			}
			else{
				magic_penalty += 5;
				if(magic_penalty > 20){
					magic_penalty = 20;
				}
				B.Add("You drain your magic reserves. ");
			}
			Q1();
			return true;
		}
		public bool CastRandomSpell(PhysicalObject obj,params SpellType[] spells){
			if(spells.Length == 0){
				return false;
			}
			return CastSpell(spells[Global.Roll(1,spells.Length)-1],obj);
		}
		public int FailRate(SpellType spell){
			int failrate = (Spell.Level(spell) - TotalSkill(SkillType.MAGIC)) * 5;
			if(failrate < 0){
				failrate = 0;
			}
			failrate += (magic_penalty * 5);
			if(!HasFeat(FeatType.ARMORED_MAGE)){
				failrate += Armor.AddedFailRate(armors.First.Value);
			}
			if(failrate > 100){
				failrate = 100;
			}
			return failrate;
		}
		public Color FailColor(SpellType spell){
			Color failcolor = Color.White;
			if(FailRate(spell) > 50){
				failcolor = Color.DarkRed;
			}
			else{
				if(FailRate(spell) > 20){
					failcolor = Color.Red;
				}
				else{
					if(FailRate(spell) > 0){
						failcolor = Color.Yellow;
					}
				}
			}
			return failcolor;
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
		public void ResetSpells(){
			magic_penalty = 0;
		}
		public void ResetForNewLevel(){
			target = null;
			target_location = null;
			if(HasAttr(AttrType.DIM_LIGHT)){
				attrs[AttrType.DIM_LIGHT] = 0;
				if(light_radius > 0){
					if(HasAttr(AttrType.ENHANCED_TORCH)){
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
			ResetSpells();
			Q.KillEvents(null,EventType.CHECK_FOR_HIDDEN);
		}
		public bool UseFeat(FeatType feat){
			switch(feat){
			case FeatType.LUNGE:
			{
				List<Tile> line = GetTarget(2);
				Tile t = null;
				if(line != null){
					t = line.Last();
				}
				if(t != null && t.actor() != null){
					bool moved = false;
					/*foreach(Tile neighbor in t.NeighborsBetween(row,col)){
						if(neighbor.passable && neighbor.actor() == null){
							moved = true;
							B.Add("You lunge! ");
							Move(neighbor.row,neighbor.col);
							attrs[AttrType.BONUS_COMBAT] += 4;
							Attack(0,t.actor());
							attrs[AttrType.BONUS_COMBAT] -= 4;
							break;
						}
					}*/
					if(DistanceFrom(t) == 2 && line[1].passable && line[1].actor() == null && !GrabPreventsMovement(line[1])){
						moved = true;
						B.Add("You lunge! ");
						Move(line[1].row,line[1].col);
						attrs[AttrType.BONUS_COMBAT] += 4;
						Attack(0,t.actor());
						attrs[AttrType.BONUS_COMBAT] -= 4;
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
						MakeNoise();
						return true;
					}
				}
				else{
					return false;
				}
				//break;
			}
			case FeatType.TUMBLE:
/*Tumble - (A, 200 energy) - You pick a tile within distance 2. If there is at least one passable tile between 
you and it(you CAN tumble past actors), you move to that tile. Additional effects: If you move past an actor, 
they lose sight of you and their turns_target_location is set to X - rand_function_of(stealth skill). (there's a good chance
they'll find you, then attack, but you will have still moved past them) ; You will automatically dodge the first arrow
that would hit you before your next turn.(it's still possible they'll roll 2 successes and hit you) ; Has the same
effect as standing still, if you're on fire or catching fire. */
				{
				target = null; //don't try to automatically pick previous targets while tumbling. this solution isn't ideal.
				List<Tile> line = GetTarget(false,2,false);
				target = null; //then, don't remember an actor picked as the target of tumble
				Tile t = null;
				if(line != null){
					t = line.Last();
				}
				if(t != null && t.passable && t.actor() == null && !GrabPreventsMovement(t)){
					List<Actor> actors_moved_past = new List<Actor>();
					bool moved = false;
					foreach(Tile neighbor in t.NeighborsBetween(row,col)){
						if(neighbor.actor() != null){
							actors_moved_past.Add(neighbor.actor());
						}
						if(neighbor.passable && !moved){
							B.Add("You tumble. ");
							Move(t.row,t.col);
							moved = true;
							attrs[AttrType.TUMBLING]++;
							if(HasAttr(AttrType.CATCHING_FIRE)){ //copy&paste happened here: todo, make a single fire-handling method
								attrs[AttrType.CATCHING_FIRE] = 0;
								B.Add("You stop the flames from spreading. ");
								if(HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){
									attrs[AttrType.STARTED_CATCHING_FIRE_THIS_TURN] = 0;
									B.Add("You stop the flames from spreading. ");
								}
							}
							else{
								if(HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){
									attrs[AttrType.STARTED_CATCHING_FIRE_THIS_TURN] = 0;
									B.Add("You stop the flames from spreading. ");
								}
								else{
									if(HasAttr(AttrType.ON_FIRE)){
										bool update = false;
										int oldradius = LightRadius();
										if(attrs[AttrType.ON_FIRE] > light_radius){
											update = true;
										}
										int i = 2;
										if(Global.Roll(1,3) == 3){ // 1 in 3 times, you don't make progress against the fire
											i = 1;
										}
										attrs[AttrType.ON_FIRE] -= i;
										if(attrs[AttrType.ON_FIRE] < 0){
											attrs[AttrType.ON_FIRE] = 0;
										}
										if(update){
											UpdateRadius(oldradius,LightRadius());
										}
										if(HasAttr(AttrType.ON_FIRE)){
											B.Add("You put out some of the fire. ");
										}
										else{
											B.Add("You put out the fire. ");
										}
									}
								}
							}
						}
					}
					if(moved){
						foreach(Actor a in actors_moved_past){
							int i = 10 - Global.Roll(Stealth());
							if(i < 0){
								i = 0;
							}
							a.player_visibility_duration = i;
						}
						Q.Add(new Event(this,200,EventType.MOVE));
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
				//break;
				}
			case FeatType.ARCANE_SHIELD: //25% fail rate for the 'failrate' feats
				if(magic_penalty < 20){
					/*if(curhp < maxhp){ here's the old arcane healing feat
						magic_penalty += 5;
						if(magic_penalty > 20){
							magic_penalty = 20;
						}
						B.Add("You drain your magic reserves. ");
						int amount = Global.Roll(TotalSkill(SkillType.MAGIC)/2,6) + 25;
						TakeDamage(DamageType.HEAL,DamageClass.NO_TYPE,amount,null);
						if(curhp == maxhp){
							B.Add("Your wounds close. ");
						}
						else{
							B.Add("Some of your wounds close. ");
						}
					}
					else{
						B.Add("You're not injured. ");
						return false;
					}*/
					magic_penalty += 5;
					if(magic_penalty > 20){
						magic_penalty = 20;
					}
					B.Add("You drain your magic reserves. ");
					int amount = Global.Roll(TotalSkill(SkillType.MAGIC)/2,6) + 25;
					if(HasAttr(AttrType.ARCANE_SHIELDED)){
						B.Add("You strengthen your arcane barrier. ");
					}
					else{
						B.Add("An arcane barrier surrounds you. ");
					}
					attrs[Forays.AttrType.ARCANE_SHIELDED] += amount;
					Q.KillEvents(this,AttrType.ARCANE_SHIELDED);
					Q.Add(new Event(this,2000,Forays.AttrType.ARCANE_SHIELDED,"Your arcane shield dissolves. "));
				}
				else{
					B.Add("Your magic reserves are empty! ");
					return false;
				}
				break;
			case FeatType.FORCE_OF_WILL:
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
						return false;
					}
				}
				if(magic_penalty < 20){
					int basefail = magic_penalty * 5;
					basefail -= skills[SkillType.SPIRIT]*2;
					if(basefail > 100){
						basefail = 100;
					}
					if(basefail < 0){
						basefail = 0;
					}
					List<colorstring> ls = new List<colorstring>();
					List<SpellType> sp = new List<SpellType>();
					bool bonus_marked = false;
					foreach(SpellType spell in spells_in_order){
						if(HasSpell(spell)){
							colorstring cs = new colorstring(Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray);
							cs.strings.Add(new cstr(basefail.ToString().PadLeft(9) + "%",FailColor(basefail)));
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
						colorstring topborder = new colorstring("------------------Level---Fail rate--------Description------------",Color.Gray);
						colorstring bottomborder = new colorstring("---Force of will fail rate: ",Color.Gray,(basefail.ToString().PadLeft(3) + "%"),FailColor(basefail),"".PadRight(37,'-'),Color.Gray);
						int i = Select("Use force of will to cast which spell? ",topborder,bottomborder,ls,false,false,true,true,HelpTopic.Spells);
						if(i != -1){
							if(!CastSpell(sp[i],true)){
								Q0();
								return true;
							}
							else{ //drained magic is now handled in CastSpell
								return true;
							}
						}
						else{
							Q0();
							return true;
						}
					}
					else{
						Q0();
						return true;
					}
				}
				else{
					B.Add("Your magic reserves are empty! ");
					return false;
				}
				//break;
			case FeatType.DISARM_TRAP:
			{
				int dir = GetDirection("Disarm which trap? ");
				if(dir != -1 && TileInDirection(dir).IsKnownTrap()){
					if(ActorInDirection(dir) != null){
						B.Add("There is " + ActorInDirection(dir).AVisible() + " in the way. ");
					}
					else{
						if(GrabPreventsMovement(TileInDirection(dir))){
							B.Add("You can't currently reach that trap. ");
							Q0();
							return true;
						}
						if(Global.Roll(5) <= 4){
							B.Add("You disarm " + Tile.Prototype(TileInDirection(dir).type).the_name + ". ");
							TileInDirection(dir).Toggle(this);
							Q1();
						}
						else{
							if(Global.Roll(20) <= skills[SkillType.DEFENSE]){
								B.Add("You almost set off " + Tile.Prototype(TileInDirection(dir).type).the_name + "! ");
								Q1();
							}
							else{
								B.Add("You set off " + Tile.Prototype(TileInDirection(dir).type).the_name + "! ");
								Move(TileInDirection(dir).row,TileInDirection(dir).col);
								Q1();
							}
						}
					}
				}
				else{
					Q0();
				}
				return true;
			}
			case FeatType.DISTRACT:
			{
				List<Tile> line = GetTarget(12,3);
				Tile t = null;
				if(line != null){
					t = line.Last();
				}
				if(t != null){
					if(!t.passable){
						t = line.LastBeforeSolidTile();
					}
					B.Add("You throw a small stone. ");
					foreach(Actor a in t.ActorsWithinDistance(3)){
						if(a != this && a.player_visibility_duration >= 0){
							if(a.HasAttr(AttrType.DISTRACTED)){
								B.Add(a.the_name + " isn't fooled. ",a);
								a.player_visibility_duration = 999; //automatic detection next turn
							}
							else{
								List<pos> p = a.GetPath(t);
								if(p.Count <= 6){
									a.path = p;
									if(Global.CoinFlip()){
										a.attrs[Forays.AttrType.DISTRACTED]++;
									}
								}
							}
						}
					}
				}
				else{
					return false;
				}
				break;
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
			attrs[Forays.AttrType.WAITING] = 0;
			attrs[AttrType.AUTOEXPLORE] = 0;
			if(path != null && path.Count > 0){
				path.Clear();
			}
		}
		public bool StunnedThisTurn(){
			if(HasAttr(AttrType.STUNNED) && Global.OneIn(3)){
				if(HasAttr(AttrType.NEVER_MOVES)){
					QS();
					return true;
				}
				int dir = Global.RandomDirection();
				if(!TileInDirection(dir).passable){
					B.Add(You("stagger") + " into " + TileInDirection(dir).the_name + ". ",this);
				}
				else{
					if(ActorInDirection(dir) != null){
						B.Add(YouVisible("stagger") + " into " + ActorInDirection(dir).TheVisible() + ". ",this,ActorInDirection(dir));
					}
					else{
						if(GrabPreventsMovement(TileInDirection(dir))){
							if(type == ActorType.PLAYER){
								B.Add("You stagger and almost fall over. ");
							}
							else{
								B.Add(the_name + " staggers and almost falls over. ",this);
							}
						}
						else{
							B.Add(You("stagger") + ". ",this);
							Move(TileInDirection(dir).row,TileInDirection(dir).col);
						}
					}
				}
				QS();
				return true;
			}
			return false;
		}
		public void MakeNoise(){
			foreach(Actor a in ActorsWithinDistance(12,true)){
				if(a != player){
					bool heard = false;
					bool los = a.HasLOS(row,col);
					if(a.DistanceFrom(this) <= 4){
						heard = true;
					}
					else{
						if((a.IsWithinSightRangeOf(row,col) || tile().IsLit()) && los){
							heard = true;
						}
					}
					if(heard){
						a.player_visibility_duration = -1;
						a.attrs[Forays.AttrType.PLAYER_NOTICED] = 1;
						if(los){
							a.target_location = tile();
						}
						else{
							a.FindPath(this,8);
						}
					}
				}
			}
		}
		public void UpdateOnEquip(WeaponType from,WeaponType to){
			switch(from){
			case WeaponType.FLAMEBRAND:
				attrs[AttrType.FIRE_HIT]--;
				break;
			case WeaponType.MACE_OF_FORCE:
				attrs[AttrType.FORCE_HIT]--;
				break;
			case WeaponType.VENOMOUS_DAGGER:
				attrs[AttrType.POISON_HIT]--;
				break;
			case WeaponType.STAFF_OF_MAGIC:
				attrs[AttrType.BONUS_MAGIC]--;
				break;
			}
			switch(to){
			case WeaponType.FLAMEBRAND:
				attrs[AttrType.FIRE_HIT]++;
				break;
			case WeaponType.MACE_OF_FORCE:
				attrs[AttrType.FORCE_HIT]++;
				break;
			case WeaponType.VENOMOUS_DAGGER:
				attrs[AttrType.POISON_HIT]++;
				break;
			case WeaponType.STAFF_OF_MAGIC:
				attrs[AttrType.BONUS_MAGIC]++;
				break;
			}
		}
		public void UpdateOnEquip(ArmorType from,ArmorType to){
			switch(from){
			case ArmorType.ELVEN_LEATHER:
				attrs[AttrType.BONUS_STEALTH] -= 2;
				break;
			case ArmorType.CHAINMAIL_OF_ARCANA:
				attrs[AttrType.BONUS_MAGIC]--;
				break;
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				attrs[AttrType.RESIST_FIRE]--;
				attrs[AttrType.RESIST_COLD]--;
				attrs[AttrType.RESIST_ELECTRICITY]--;
				break;
			}
			switch(to){
			case ArmorType.ELVEN_LEATHER:
				attrs[AttrType.BONUS_STEALTH] += 2; // balance check?
				break;
			case ArmorType.CHAINMAIL_OF_ARCANA:
				attrs[AttrType.BONUS_MAGIC]++;
				break;
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				attrs[AttrType.RESIST_FIRE]++;
				attrs[AttrType.RESIST_COLD]++;
				attrs[AttrType.RESIST_ELECTRICITY]++;
				if(HasAttr(AttrType.ON_FIRE) || HasAttr(AttrType.CATCHING_FIRE) || HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){
					B.Add("You are no longer on fire. ");
					int oldradius = LightRadius();
					attrs[AttrType.ON_FIRE] = 0;
					attrs[AttrType.CATCHING_FIRE] = 0;
					attrs[AttrType.STARTED_CATCHING_FIRE_THIS_TURN] = 0;
					if(oldradius != LightRadius()){
						UpdateRadius(oldradius,LightRadius());
					}
				}
				break;
			}
		}
		public void UpdateOnEquip(MagicItemType from,MagicItemType to){
			switch(from){
			case MagicItemType.RING_OF_RESISTANCE:
				attrs[AttrType.RESIST_FIRE]--;
				attrs[AttrType.RESIST_COLD]--;
				attrs[AttrType.RESIST_ELECTRICITY]--;
				break;
			}
			switch(to){
			case MagicItemType.RING_OF_RESISTANCE:
				attrs[AttrType.RESIST_FIRE]++;
				attrs[AttrType.RESIST_COLD]++;
				attrs[AttrType.RESIST_ELECTRICITY]++;
				if(HasAttr(AttrType.ON_FIRE) || HasAttr(AttrType.CATCHING_FIRE) || HasAttr(AttrType.STARTED_CATCHING_FIRE_THIS_TURN)){
					B.Add("You are no longer on fire. ");
					int oldradius = LightRadius();
					attrs[AttrType.ON_FIRE] = 0;
					attrs[AttrType.CATCHING_FIRE] = 0;
					attrs[AttrType.STARTED_CATCHING_FIRE_THIS_TURN] = 0;
					if(oldradius != LightRadius()){
						UpdateRadius(oldradius,LightRadius());
					}
				}
				break;
			}
		}
		public List<string> InventoryList(){
			List<string> result = new List<string>();
			foreach(Item i in inv){
				result.Add(i.AName());
			}
			return result;
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
			Screen.WriteStatsString(3,0,"Depth: " + M.current_level + "  ");
			Screen.WriteStatsString(4,0,"AC: " + ArmorClass() + "  ");
			int magic_item_lines = magic_items.Count;
			cstr cs = Weapon.StatsName(weapons.First.Value);
			cs.s = cs.s.PadRight(12);
			Screen.WriteStatsString(5,0,cs);
			cs = Armor.StatsName(armors.First.Value);
			cs.s = cs.s.PadRight(12);
			Screen.WriteStatsString(6,0,cs);
			int line = 7;
			foreach(MagicItemType m in magic_items){
				cs = MagicItem.StatsName(m);
				cs.s = cs.s.PadRight(12);
				Screen.WriteStatsString(line,0,cs);
				++line;
			}
			if(!Global.Option(OptionType.HIDE_COMMANDS)){
/*[i]nventory
[e]quipment
[c]haracter
[t]orch off
Use [f]eat
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
*/
				for(int i=7+magic_item_lines;i<11;++i){
					Screen.WriteStatsString(i,0,"".PadRight(12));
				}
				string[] commandhints = new string[]{"[i]nventory ","[e]quipment ","[c]haracter ","SPECIAL",
					"Use [f]eat  ","Cast [z]    ","[s]hoot     ","[Tab] Look  ","[a]pply item","[g]et item  ",
					"[r]est      ","[w]alk      ","E[x]plore   ","[o]perate   "};
				if(light_radius > 0){
					commandhints[3] = "[t]orch off ";
				}
				else{
					commandhints[3] = "[t]orch on  ";
				}
				Color lettercolor = cyan_letters? Color.Cyan : Color.DarkCyan;
				Color wordcolor = cyan_letters? Color.Gray : Color.DarkGray;
				for(int i=0;i<commandhints.Length;++i){
					int open = commandhints[i].LastIndexOf('[');
					cstr front = new cstr(commandhints[i].Substring(0,open+1),wordcolor);
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
		/*public void DisplayStats(bool expand_weapons,bool expand_armors){
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
			Screen.WriteStatsString(3,0,"Level: " + level + "  ");
			//Screen.WriteStatsString(4,0,"XP: " + xp + "  ");
			Screen.WriteStatsString(4,0,"Depth: " + M.current_level + "  ");
			Screen.WriteStatsString(5,0,"AC: " + ArmorClass() + "  ");
			int weapon_lines = 1;
			int armor_lines = 1;
			int magic_item_lines = magic_items.Count;
			string divider = "---".PadRight(12);
			//Screen.WriteStatsString(6,0,divider);
			cstr cs = Weapon.StatsName(weapons.First.Value);
			cs.s = cs.s.PadRight(12);
			Screen.WriteStatsString(6,0,cs);
			if(expand_weapons){ //this can easily be extended to handle a variable number of weapons
				weapon_lines = 5;
				int i = 7;
				foreach(WeaponType w in weapons){
					if(w != weapons.First.Value){
						cs = Weapon.StatsName(w);
						cs.s = cs.s.PadRight(12);
						Screen.WriteStatsString(i,0,cs);
						++i;
					}
				}
				
			}
			//Screen.WriteStatsString(7+weapon_lines,0,divider);
			cs = Armor.StatsName(armors.First.Value);
			cs.s = cs.s.PadRight(12);
			Screen.WriteStatsString(6+weapon_lines,0,cs);
			if(expand_armors){
				armor_lines = 3;
				int i = 7 + weapon_lines;
				foreach(ArmorType a in armors){
					if(a != armors.First.Value){
						cs = Armor.StatsName(a);
						cs.s = cs.s.PadRight(12);
						Screen.WriteStatsString(i,0,cs);
						++i;
					}
				}
			}
			//Screen.WriteStatsString(8+weapon_lines+armor_lines,0,divider);
			int line = 6 + weapon_lines + armor_lines;
			foreach(MagicItemType m in magic_items){
				cs = MagicItem.StatsName(m);
				cs.s = cs.s.PadRight(12);
				Screen.WriteStatsString(line,0,cs);
				++line;
			}
			for(int i=6+weapon_lines+armor_lines+magic_item_lines;i<ROWS-1;++i){
				Screen.WriteStatsString(i,0,"".PadRight(12));
			}
			Screen.ResetColors();
		}*/
		public void DisplayCharacterInfo(){ DisplayCharacterInfo(true); }
		public void DisplayCharacterInfo(bool readkey){
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
			s = "Trait: ";
			if(HasAttr(AttrType.MAGICAL_BLOOD)){
				s = s + "Magical blood";
			}
			if(HasAttr(AttrType.TOUGH)){
				s = s + "Tough";
			}
			if(HasAttr(AttrType.KEEN_EYES)){
				s = s + "Keen eyes";
			}
			if(HasAttr(AttrType.LOW_LIGHT_VISION)){
				s = s + "Low light vision";
			}
			if(HasAttr(AttrType.LONG_STRIDE)){
				s = s + "Long stride";
			}
			if(HasAttr(AttrType.RUNIC_BIRTHMARK)){
				s = s + "Runic birthmark";
			}
			Screen.WriteMapString(5,0,s);
			Screen.WriteMapString(5,0,new cstr(catcolor,"Trait"));
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
			for(FeatType f = FeatType.QUICK_DRAW;f < FeatType.NUM_FEATS;++f){
				if(HasFeat(f)){
					if(featlist.Length == 0){ //if this is the first one...
						featlist = featlist + Feat.Name(f);
					}
					else{
						featlist = featlist + ", " + Feat.Name(f);
					}
				}
			}
			int currentrow = 11;
			while(featlist.Length > COLS-7){
				int currentcol = COLS-8;
				while(featlist[currentcol] != ','){
					--currentcol;
				}
				Screen.WriteMapString(currentrow,7,featlist.Substring(0,currentcol+1));
				featlist = featlist.Substring(currentcol+2);
				++currentrow;
			}
			Screen.WriteMapString(currentrow,7,featlist);
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
			Screen.ResetColors();
			B.DisplayNow("Character information: ");
			Console.CursorVisible = true;
			if(readkey){
				Console.ReadKey(true);
			}
		}
		public int[] DisplayEquipment(){
			WeaponType new_weapon = weapons.First.Value;
			ArmorType new_armor = armors.First.Value;
			Dict<WeaponType,WeaponType> heldweapon = new Dict<WeaponType, WeaponType>();
			Dict<ArmorType,ArmorType> heldarmor = new Dict<ArmorType, ArmorType>();
			for(WeaponType w = WeaponType.SWORD;w <= WeaponType.BOW;++w){
				foreach(WeaponType wt in weapons){
					if(Weapon.BaseWeapon(wt) == w){
						heldweapon[w] = wt;
					}
				}
			}
			for(ArmorType a = ArmorType.LEATHER;a <= ArmorType.FULL_PLATE;++a){
				foreach(ArmorType at in armors){
					if(Armor.BaseArmor(at) == a){
						heldarmor[a] = at;
					}
				}
			}
			Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
			for(int i=1;i<ROWS-1;++i){
				Screen.WriteMapString(i,0,"".PadRight(COLS));
			}
			int line = 2;
			for(WeaponType w = WeaponType.SWORD;w <= WeaponType.BOW;++w){
				Screen.WriteMapString(line,11,Weapon.EquipmentScreenName(heldweapon[w]));
				++line;
			}
			line = 2;
			for(ArmorType a = ArmorType.LEATHER;a <= ArmorType.FULL_PLATE;++a){
				Screen.WriteMapString(line,COLS-24,Armor.EquipmentScreenName(heldarmor[a]));
				++line;
			}
			Screen.WriteMapString(9,1,new cstr(Color.DarkRed,"Weapon: "));
			Screen.WriteMapChar(9,7,':');
			Screen.WriteMapString(11,1,new cstr(Color.DarkCyan,"Armor: "));
			Screen.WriteMapChar(11,6,':');
			Screen.WriteMapString(13,1,new cstr(Color.DarkGreen,"Magic items: "));
			Screen.WriteMapChar(13,12,':');
			line = 13;
			foreach(MagicItemType m in magic_items){
				string[] s = MagicItem.Description(m);
				Screen.WriteMapString(line,14,s[0]);
				Screen.WriteMapString(line+1,14,s[1]);
				line += 2;
			}
			ConsoleKeyInfo command;
			bool done = false;
			while(!done){
				line = 2;
				for(WeaponType w = WeaponType.SWORD;w <= WeaponType.BOW;++w){
					if(new_weapon == heldweapon[w]){
						Screen.WriteMapChar(line,5,'>');
						Screen.WriteMapString(line,7,new cstr(Color.Red,"[" + (char)(w+(int)'a') + "]"));
					}
					else{
						Screen.WriteMapChar(line,5,' ');
						Screen.WriteMapString(line,7,new cstr(Color.Cyan,"[" + (char)(w+(int)'a') + "]"));
					}
					++line;
				}
				line = 2;
				for(ArmorType a = ArmorType.LEATHER;a <= ArmorType.FULL_PLATE;++a){
					if(new_armor == heldarmor[a]){
						Screen.WriteMapChar(line,36,'>');
						Screen.WriteMapString(line,38,new cstr(Color.Red,"[" + (char)(a+(int)'f') + "]"));
					}
					else{
						Screen.WriteMapChar(line,36,' ');
						Screen.WriteMapString(line,38,new cstr(Color.Cyan,"[" + (char)(a+(int)'f') + "]"));
					}
					++line;
				}
				Screen.WriteMapString(9,9,Weapon.Description(Weapon.BaseWeapon(new_weapon)).PadRight(COLS));
				if(new_weapon != Weapon.BaseWeapon(new_weapon)){
					Screen.WriteMapString(10,9,Weapon.Description(new_weapon).PadRight(COLS));
				}
				else{
					Screen.WriteMapString(10,9,"".PadRight(COLS));
				}
				Screen.WriteMapString(11,8,Armor.Description(Armor.BaseArmor(new_armor)).PadRight(COLS));
				if(new_armor != Armor.BaseArmor(new_armor)){
					Screen.WriteMapString(12,8,Armor.Description(new_armor).PadRight(COLS));
				}
				else{
					Screen.WriteMapString(12,8,"".PadRight(COLS));
				}
				if(new_weapon == weapons.First.Value && new_armor == armors.First.Value){
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
					if((int)ch - (int)'a' != (int)(Weapon.BaseWeapon(new_weapon))){
						new_weapon = heldweapon[(WeaponType)((int)ch - (int)'a')];
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
					if((int)ch - (int)'f' != (int)(Armor.BaseArmor(new_armor))){
						new_armor = heldarmor[(ArmorType)((int)ch - (int)'f')];
					}
					break;
				case (char)27:
				case ' ':
					new_weapon = weapons.First.Value; //reset
					new_armor = armors.First.Value;
					done = true;
					break;
				case (char)13:
					done = true;
					break;
				default:
					break;
				}
			}
			return new int[]{(int)Weapon.BaseWeapon(new_weapon),(int)Armor.BaseArmor(new_armor)};
		}
		public void IncreaseSkill(SkillType skill){
			List<string> learned = new List<string>();
			skills[skill]++;
			B.Add("You feel a rush of power. ");
			//DisplayStats();
			B.PrintAll();
			ConsoleKeyInfo command;
			FeatType feat_increased = FeatType.NO_FEAT;
			bool done = false;
			while(!done){
				Screen.ResetColors();
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				for(int i=0;i<4;++i){
					FeatType ft = Feat.OfSkill(skill,i);
					Color featcolor = (feat_increased == ft)? Color.Green : Color.Gray;
					Color lettercolor = Color.Cyan;
					int featlevel = (feat_increased == ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){
						featcolor = Color.Magenta;
						lettercolor = Color.DarkRed;
						featlevel = Feat.MaxRank(ft);
					}
					Screen.WriteMapString(1+i*5,0,("["+(char)(i+97)+"] "));
					Screen.WriteMapChar(1+i*5,1,(char)(i+97),lettercolor);
					Screen.WriteMapString(1+i*5,4,Feat.Name(ft).PadRight(21) + "(" + featlevel + "/" + Feat.MaxRank(ft) + ")",featcolor);
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
				if(feat_increased != FeatType.NO_FEAT){
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
					if(feat_increased == ft){
						feat_increased = FeatType.NO_FEAT;
					}
					else{
						if(feat_increased == FeatType.NO_FEAT && !HasFeat(ft)){
							feat_increased = ft;
						}
					}
					break;
				}
				case '?':
					Help.DisplayHelp(HelpTopic.Feats);
					DisplayStats();
					break;
				case (char)13:
					if(feat_increased != FeatType.NO_FEAT){
						done = true;
					}
					break;
				default:
					break;
				}
			}
			feats[feat_increased]--; //negative values are used until you've completely learned a feat
			partial_feats_in_order.AddUnique(feat_increased);
			if(feats[feat_increased] == -(Feat.MaxRank(feat_increased))){
				feats[feat_increased] = 1;
				partial_feats_in_order.Remove(feat_increased);
				feats_in_order.Add(feat_increased);
				learned.Add("You master the " + Feat.Name(feat_increased) + " feat. ");
			}
			else{
				string points = "points";
				if(Feat.MaxRank(feat_increased)+feats[feat_increased] == 1){
					points = "point";
				}
				if(feats[feat_increased] == -1){
					learned.Add("You start learning the " + Feat.Name(feat_increased) + " feat (" + (Feat.MaxRank(feat_increased)+feats[feat_increased]) + " " + points + " left). ");
				}
				else{
					learned.Add("You continue learning the " + Feat.Name(feat_increased) + " feat (" + (Feat.MaxRank(feat_increased)+feats[feat_increased]) + " " + points + " left). ");
				}
			}
			if(skill == SkillType.MAGIC){
				List<SpellType> unknown = new List<SpellType>();
				List<colorstring> unknownstr = new List<colorstring>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(!HasSpell(spell) && spell != SpellType.BLESS && spell != SpellType.MINOR_HEAL
					&& spell != SpellType.HOLY_SHIELD && spell != SpellType.NO_SPELL && spell != SpellType.NUM_SPELLS){
						unknown.Add(spell);
						colorstring cs = new colorstring();
						cs.strings.Add(new cstr(Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray));
						int failrate = (Spell.Level(spell) - TotalSkill(SkillType.MAGIC)) * 5;
						if(failrate < 0){
							failrate = 0;
						}
						cs.strings.Add(new cstr(failrate.ToString().PadLeft(9) + "%",FailColor(failrate)));
						unknownstr.Add(cs + Spell.Description(spell));
					}
				}
				for(int i=unknown.Count+2;i<ROWS;++i){
					Screen.WriteMapString(i,0,"".PadRight(COLS));
				}
				colorstring topborder = new colorstring("------------------Level---Fail rate--------Description------------",Color.Gray);
				int selection = Select("Learn which spell? ",topborder,new colorstring("".PadRight(25,'-') + "[",Color.Gray,"?",Color.Cyan,"] for help".PadRight(COLS,'-'),Color.Gray),unknownstr,false,true,false,true,HelpTopic.Spells);
				spells[unknown[selection]] = 1;
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
			if(o == this){
				return true;
			}
			if(HasAttr(AttrType.ASLEEP)){
				return false;
			}
			Actor a = o as Actor;
			if(a != null){
				if(HasAttr(AttrType.BLOODSCENT) && !a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT)){
					int distance_of_closest = 99;
					foreach(Actor a2 in ActorsWithinDistance(12,true)){
						if(!a2.HasAttr(AttrType.UNDEAD) && !a2.HasAttr(AttrType.CONSTRUCT)){
							if(DistanceFrom(a2) < distance_of_closest){
								distance_of_closest = DistanceFrom(a2);
							}
						}
					}
					if(distance_of_closest == DistanceFrom(a)){
						return true;
					}
				}
				if(HasAttr(AttrType.DETECTING_MONSTERS)){
					return true;
				}
				if(a.HasAttr(AttrType.SHADOW_CLOAK) && !a.tile().IsLit() && !HasAttr(AttrType.BLINDSIGHT)){
					if(a != player || !HasAttr(AttrType.PLAYER_NOTICED)){ //player is visible once noticed
						return false;
					}
				}
			}
			Tile t = o as Tile;
			if(t != null){
				if(t.solid_rock){
					return false;
				}
			}
			if(IsWithinSightRangeOf(o.row,o.col) || M.tile[o.row,o.col].IsLit()){
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
			return false;
		}
		public int SightRange(){
			int divisor = HasAttr(AttrType.DIM_VISION)? 3 : 1;
			if(HasAttr(AttrType.DARKVISION)){
				return 8 / divisor;
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
			if(dist <= 8/divisor && HasAttr(AttrType.DARKVISION)){
				return true;
			}
			if(M.tile[r,c].opaque){
				foreach(Tile t in M.tile[r,c].NeighborsBetween(row,col)){
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
			case ActorType.LARGE_BAT:
				return "The bats here are substantially bigger than most, perhaps because their insect prey is also unusually large.";
			case ActorType.WOLF:
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
			case ActorType.DREAM_CLONE:
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
			case ActorType.COMPY:
				return "Compys are little waste-eating scavengers that possess a subtle poison. These lizards tend to ignore healthy creatures, preferring to surround those who are weak, helpless, or otherwise occupied.";
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
			case ActorType.SKELETAL_SABERTOOTH:
				return "The skeletal remains of an enormous feline predator stand here, seemingly ready to pounce at any moment.";
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
			case ActorType.TROLL_SEER:
				return "The seer is a leader among the solitary troll population, sought for augury and council. Spells and arcane tricks are passed down from seer to seer. ";
			case ActorType.MECHANICAL_KNIGHT:
				return "The mechanical knight's shield moves with unnatural speed, ready to foil any onslaught. Its exposed gears appear vulnerable to any attack that could bypass its shield.";
			case ActorType.ORC_WARMAGE:
				return "The destruction wreaked by warmages evokes respect and feat even among their own kind. They often lead raids and war parties, using tracking spells to complement their lethal magic.";
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
							if(neighbor.EstimatedEuclideanDistanceFromX10(p) < best.Value.EstimatedEuclideanDistanceFromX10(p)){
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
							if(neighbor.EstimatedEuclideanDistanceFromX10(current) < best.Value.EstimatedEuclideanDistanceFromX10(current)){
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
		public List<Tile> GetTarget(){ return GetTarget(false,-1,true); }
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
						|| t.Is(FeatureType.TROLL_CORPSE) || t.Is(FeatureType.TROLL_SEER_CORPSE)
						|| t.Is(FeatureType.RUNE_OF_RETREAT)
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
						items.Add(M.tile[r,c].inv.AName());
					}
					foreach(FeatureType f in M.tile[r,c].features){
						items.Add(Tile.Feature(f).a_name);
					}
					if(items.Count == 0){
						contents += M.tile[r,c].a_name;
					}
					else{
						if(items.Count == 1){
							contents += items[0] + M.tile[r,c].Preposition() + M.tile[r,c].a_name;
						}
						else{
							if(items.Count == 2){
								if(M.tile[r,c].type != TileType.FLOOR){
									if(M.tile[r,c].Preposition() == " and "){
										contents += items[0] + ", " + items[1] + ",";
										contents += M.tile[r,c].Preposition() + M.tile[r,c].a_name;
									}
									else{
										contents += items[0] + " and " + items[1];
										contents += M.tile[r,c].Preposition() + M.tile[r,c].a_name;
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
									contents += M.tile[r,c].Preposition() + M.tile[r,c].a_name;
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
									B.DisplayNow("You can no longer see this " + M.tile[r,c].name + ". ");
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
								/*if(!blocked){
									last_good = t;
								}*/
								if(t.seen && !t.passable && (t.row != r || t.col != c)){
									blocked=true;
								}
							}
							oldline.Remove(t);
						}
						if(radius > 0/* && last_good != null*/){
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
									/*for(int j=start_c;j<COLS;++j){
										line.Add(M.tile[i,j]);
										oldline.Remove(M.tile[i,j]);
									}*/
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
									/*int length = desc[0].Length();
									for(int j=0;j<length;++j){
										line.Add(M.tile[i,j]);
										oldline.Remove(M.tile[i,j]);
									}*/
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
					if(radius > 0/* && last_good != null*/){
						foreach(Tile t in M.tile[r,c].TilesWithinDistance(radius,true)){
							oldline.AddUnique(t);
						}
					}
					first_iteration = false;
					M.tile[r,c].Cursor();
				}
				/*if(lookmode && Screen.MapChar(r,c).c == ' ' && Screen.BackgroundColor == ConsoleColor.Black){
					//testing for foregroundcolor == black does NOT work
					//testing for backgroundcolor == black DOES work.
					Screen.WriteMapChar(r,c,' ');
					Console.SetCursorPosition(c+Global.MAP_OFFSET_COLS,r+Global.MAP_OFFSET_ROWS);
				}*/
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
				M.RedrawWithStrings();
				return result;
			}
		}
		public int Select(string message,colorstring top_border,colorstring bottom_border,List<colorstring> strings,bool no_ask,bool no_cancel,bool easy_cancel,bool help_key,HelpTopic help_topic){
			int result = -2;
			while(result == -2){
				Screen.WriteMapString(0,0,top_border);
				char letter = 'a';
				int i=1;
				foreach(colorstring s in strings){
					Screen.WriteMapString(i,0,new colorstring("[",Color.Gray,letter.ToString(),Color.Cyan,"] ",Color.Gray));
					Screen.WriteMapString(i,4,s);
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
						M.RedrawWithStrings();
						return result;
					}
				}
			}
			return -1;
		}
		public int GetSelection(string s,int count,bool no_cancel,bool easy_cancel,bool help_key){
			if(count == 0){ return -1; }
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
			}
		}
		public void AnimateProjectile(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateProjectile(GetBestLine(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateMapCell(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateMapCell(o.row,o.col,new colorchar(color,c));
		}
		public void AnimateBoltProjectile(PhysicalObject o,Color color){
			B.DisplayNow();
			Screen.AnimateBoltProjectile(GetBestLine(o.row,o.col),color);
		}
		public void AnimateExplosion(PhysicalObject o,int radius,Color color,char c){
			B.DisplayNow();
			Screen.AnimateExplosion(o,radius,new colorchar(color,c));
		}
		public void AnimateBeam(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateBeam(GetBestLine(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateBoltBeam(PhysicalObject o,Color color){
			B.DisplayNow();
			Screen.AnimateBoltBeam(GetBestLine(o.row,o.col),color);
		}
		//
		// i should have made them (char,color) from the start..
		//
		public void AnimateProjectile(PhysicalObject o,char c,Color color){
			B.DisplayNow();
			Screen.AnimateProjectile(GetBestLine(o.row,o.col),new colorchar(color,c));
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
			Screen.AnimateBeam(GetBestLine(o.row,o.col),new colorchar(color,c));
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
	public static class AttackList{ //consider more descriptive attacks, such as the zealot smashing you with a mace
		private static AttackInfo[] attack = new AttackInfo[33];
		static AttackList(){
			attack[0] = new AttackInfo(100,1,DamageType.NORMAL,"& ^hit *"); //the player's default attack
			attack[1] = new AttackInfo(100,2,DamageType.NORMAL,"& ^hits *");
			attack[2] = new AttackInfo(100,1,DamageType.PIERCING,"& ^bites *");
			attack[3] = new AttackInfo(100,1,DamageType.SLASHING,"& ^scratches *");
			attack[4] = new AttackInfo(100,2,DamageType.PIERCING,"& ^bites *");
			attack[5] = new AttackInfo(100,3,DamageType.PIERCING,"& ^bites *");
			attack[6] = new AttackInfo(100,3,DamageType.SLASHING,"& ^rakes *");
			attack[7] = new AttackInfo(100,2,DamageType.COLD,"& hits * with a blast of cold");
			attack[8] = new AttackInfo(100,4,DamageType.COLD,"& releases a burst of cold");
			attack[9] = new AttackInfo(100,0,DamageType.NONE,"& ^hits *"); //dream warrior's clone attack
			attack[10] = new AttackInfo(100,3,DamageType.NORMAL,"& ^hits *");
			attack[11] = new AttackInfo(200,2,DamageType.NORMAL,"& lunges forward and ^hits *");
			attack[12] = new AttackInfo(100,3,DamageType.BASHING,"& ^hammers *");
			attack[13] = new AttackInfo(100,2,DamageType.NORMAL,"& touches *");
			attack[14] = new AttackInfo(100,2,DamageType.SLASHING,"& ^claws *");
			attack[15] = new AttackInfo(100,3,DamageType.NORMAL,"& ^punches *");
			attack[16] = new AttackInfo(100,3,DamageType.NORMAL,"& ^kicks *");
			attack[17] = new AttackInfo(100,3,DamageType.NORMAL,"& ^strikes *");
			attack[18] = new AttackInfo(100,2,DamageType.NORMAL,"& slimes *");
			attack[19] = new AttackInfo(100,0,DamageType.NONE,"& grabs at *");
			attack[20] = new AttackInfo(100,2,DamageType.NORMAL,"& clutches at *");
			attack[21] = new AttackInfo(100,3,DamageType.BASHING,"& ^slams *");
			attack[22] = new AttackInfo(100,3,DamageType.SLASHING,"& ^claws *");
			attack[23] = new AttackInfo(200,5,DamageType.BASHING,"& ^hits * with a huge mace");
			attack[24] = new AttackInfo(100,1,DamageType.NORMAL,"& hits *");
			attack[25] = new AttackInfo(100,4,DamageType.NORMAL,"& ^hits *"); 
			attack[26] = new AttackInfo(100,0,DamageType.NONE,"& lashes * with a tentacle");
			attack[27] = new AttackInfo(100,2,DamageType.SLASHING,"& ^scratches *");
			attack[28] = new AttackInfo(100,4,DamageType.BASHING,"& ^slams *");
			attack[29] = new AttackInfo(120,3,DamageType.NORMAL,"& extends a tentacle and ^hits *");
			attack[30] = new AttackInfo(120,1,DamageType.NORMAL,"& extends a tentacle and drags * closer");
			attack[31] = new AttackInfo(100,5,DamageType.BASHING,"& ^clobbers *");
			attack[32] = new AttackInfo(150,6,DamageType.FIRE,"& breathes fire");
		}
		public static AttackInfo Attack(ActorType type,int num){
			switch(type){
			case ActorType.PLAYER:
				return new AttackInfo(attack[0]);
			case ActorType.RAT:
				return new AttackInfo(attack[2]);
			case ActorType.GOBLIN:
				return new AttackInfo(attack[1]);
			case ActorType.LARGE_BAT:
				switch(num){
				case 0:
					return new AttackInfo(attack[2]);
				case 1:
					return new AttackInfo(attack[3]);
				default:
					return null;
				}
			case ActorType.WOLF:
				return new AttackInfo(attack[4]);
			case ActorType.SKELETON:
				return new AttackInfo(attack[1]);
			case ActorType.BLOOD_MOTH:
				return new AttackInfo(attack[5]);
			case ActorType.SWORDSMAN:
				return new AttackInfo(attack[1]);
			case ActorType.DARKNESS_DWELLER:
				return new AttackInfo(attack[1]);
			case ActorType.CARNIVOROUS_BRAMBLE:
				return new AttackInfo(attack[6]);
			case ActorType.FROSTLING:
				switch(num){
				case 0:
					return new AttackInfo(attack[1]);
				case 1:
					return new AttackInfo(attack[7]);
				case 2:
					return new AttackInfo(attack[8]);
				default:
					return null;
				}
			case ActorType.DREAM_WARRIOR:
				return new AttackInfo(attack[1]);
			case ActorType.DREAM_CLONE:
				return new AttackInfo(attack[9]);
			case ActorType.CULTIST:
				return new AttackInfo(attack[1]);
			case ActorType.GOBLIN_ARCHER:
			case ActorType.PHANTOM_ARCHER:
				return new AttackInfo(attack[1]);
			case ActorType.GOBLIN_SHAMAN:
				return new AttackInfo(attack[1]);
			case ActorType.MIMIC:
				return new AttackInfo(attack[1]);
			case ActorType.SKULKING_KILLER:
				return new AttackInfo(attack[1]);
			case ActorType.ZOMBIE:
			case ActorType.PHANTOM_ZOMBIE:
				switch(num){
				case 0:
					return new AttackInfo(attack[11]);
				case 1:
					return new AttackInfo(attack[5]);
				default:
					return null;
				}
			case ActorType.DIRE_RAT:
				return new AttackInfo(attack[2]);
			case ActorType.ROBED_ZEALOT:
				return new AttackInfo(attack[12]);
			case ActorType.SHADOW:
				return new AttackInfo(attack[13]);
			case ActorType.BANSHEE:
				return new AttackInfo(attack[14]);
			case ActorType.WARG:
				return new AttackInfo(attack[4]);
			case ActorType.PHASE_SPIDER:
				return new AttackInfo(attack[2]);
			case ActorType.DERANGED_ASCETIC:
				switch(num){
				case 0:
					return new AttackInfo(attack[15]);
				case 1:
					return new AttackInfo(attack[16]);
				case 2:
					return new AttackInfo(attack[17]);
				default:
					return null;
				}
			case ActorType.POLTERGEIST:
				switch(num){
				case 0:
					return new AttackInfo(attack[19]);
				case 1:
					return new AttackInfo(attack[18]);
				default:
					return null;
				}
			case ActorType.CAVERN_HAG:
				return new AttackInfo(attack[20]);
			case ActorType.COMPY:
				return new AttackInfo(attack[2]);
			case ActorType.NOXIOUS_WORM:
				switch(num){
				case 0:
					return new AttackInfo(attack[5]);
				case 1:
					return new AttackInfo(attack[21]);
				default:
					return null;
				}
			case ActorType.BERSERKER:
				return new AttackInfo(attack[10]);
			case ActorType.TROLL:
				return new AttackInfo(attack[22]);
			case ActorType.VAMPIRE:
				return new AttackInfo(attack[4]);
			case ActorType.CRUSADING_KNIGHT:
			case ActorType.PHANTOM_CRUSADER:
				return new AttackInfo(attack[23]);
			case ActorType.SKELETAL_SABERTOOTH:
			case ActorType.PHANTOM_TIGER:
				return new AttackInfo(attack[5]);
			case ActorType.MUD_ELEMENTAL:
				return new AttackInfo(attack[1]);
			case ActorType.MUD_TENTACLE:
				return new AttackInfo(attack[24]);
			case ActorType.ENTRANCER:
				return new AttackInfo(attack[1]);
			case ActorType.MARBLE_HORROR:
				return new AttackInfo(attack[10]);
			case ActorType.OGRE:
			case ActorType.PHANTOM_OGRE:
				return new AttackInfo(attack[25]);
			case ActorType.ORC_GRENADIER:
				return new AttackInfo(attack[10]);
			case ActorType.SHADOWVEIL_DUELIST:
				return new AttackInfo(attack[10]);
			case ActorType.CARRION_CRAWLER:
				switch(num){
				case 0:
					return new AttackInfo(attack[2]);
				case 1:
					return new AttackInfo(attack[26]);
				default:
					return null;
				}
			case ActorType.SPELLMUDDLE_PIXIE:
				return new AttackInfo(attack[27]);
			case ActorType.STONE_GOLEM:
				return new AttackInfo(attack[28]);
			case ActorType.PYREN_ARCHER:
				return new AttackInfo(attack[1]);
			case ActorType.ORC_ASSASSIN:
				return new AttackInfo(attack[10]);
			case ActorType.TROLL_SEER:
				return new AttackInfo(attack[22]);
			case ActorType.MECHANICAL_KNIGHT:
				switch(num){
				case 0:
					return new AttackInfo(attack[10]);
				case 1:
					return new AttackInfo(attack[16]);
				default:
					return null;
				}
			case ActorType.ORC_WARMAGE:
				return new AttackInfo(attack[10]);
			case ActorType.LASHER_FUNGUS:
				switch(num){
				case 0:
					return new AttackInfo(attack[29]);
				case 1:
					return new AttackInfo(attack[30]);
				default:
					return null;
				}
			case ActorType.NECROMANCER:
				return new AttackInfo(attack[1]);
			case ActorType.LUMINOUS_AVENGER:
				return new AttackInfo(attack[17]);
			case ActorType.CORPSETOWER_BEHEMOTH:
			case ActorType.PHANTOM_BEHEMOTH:
				return new AttackInfo(attack[31]);
			case ActorType.FIRE_DRAKE:
				switch(num){
				case 0:
					return new AttackInfo(attack[5]);
				case 1:
					return new AttackInfo(attack[22]);
				case 2:
					return new AttackInfo(attack[32]);
				default:
					return null;
				}
			case ActorType.PHANTOM_BLIGHTWING:
				return new AttackInfo(attack[5]);
			case ActorType.PHANTOM_SWORDMASTER:
				return new AttackInfo(attack[10]);
			case ActorType.PHANTOM_CONSTRICTOR:
				return new AttackInfo(attack[21]);
			default:
				return null;
			}
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
		public static int MaxRank(FeatType type){
			switch(type){
			case FeatType.QUICK_DRAW:
			case FeatType.SILENT_CHAINMAIL:
			case FeatType.BOILING_BLOOD:
			case FeatType.DISTRACT:
			case FeatType.DISARM_TRAP:
				return 2;
			case FeatType.MASTERS_EDGE:
			case FeatType.ENDURING_SOUL:
			case FeatType.NECK_SNAP:
			case FeatType.DANGER_SENSE:
				return 4;
			case FeatType.LETHALITY:
			case FeatType.LUNGE:
			case FeatType.DRIVE_BACK:
			case FeatType.ARMORED_MAGE:
			case FeatType.FULL_DEFENSE:
			case FeatType.TUMBLE:
			case FeatType.STUDENTS_LUCK:
			case FeatType.ARCANE_SHIELD:
			case FeatType.FORCE_OF_WILL:
			case FeatType.CONVICTION:
			case FeatType.FEEL_NO_PAIN:
				return 3;
			default:
				return 0;
			}
		}
		public static bool IsActivated(FeatType type){
			switch(type){
			case FeatType.LUNGE:
			case FeatType.TUMBLE:
			case FeatType.ARCANE_SHIELD:
			case FeatType.FORCE_OF_WILL:
			case FeatType.DISARM_TRAP:
			case FeatType.DISTRACT:
				return true;
			case FeatType.QUICK_DRAW:
			case FeatType.LETHALITY:
			case FeatType.DRIVE_BACK:
			case FeatType.SILENT_CHAINMAIL:
			case FeatType.ARMORED_MAGE:
			case FeatType.FULL_DEFENSE:
			case FeatType.MASTERS_EDGE:
			case FeatType.STUDENTS_LUCK:
			case FeatType.CONVICTION:
			case FeatType.ENDURING_SOUL:
			case FeatType.FEEL_NO_PAIN:
			case FeatType.BOILING_BLOOD:
			case FeatType.NECK_SNAP:
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
			case FeatType.DISTRACT:
				return "Distract";
			case FeatType.QUICK_DRAW:
				return "Quick draw";
			case FeatType.SILENT_CHAINMAIL:
				return "Silent chainmail";
			case FeatType.DANGER_SENSE:
				return "Danger sense";
			case FeatType.FULL_DEFENSE:
				return "Full defense";
			case FeatType.ENDURING_SOUL:
				return "Enduring soul";
			case FeatType.NECK_SNAP:
				return "Neck snap";
			case FeatType.BOILING_BLOOD:
				return "Boiling blood";
			case FeatType.LETHALITY:
				return "Lethality";
			case FeatType.LUNGE:
				return "Lunge";
			case FeatType.DRIVE_BACK:
				return "Drive back";
			case FeatType.ARMORED_MAGE:
				return "Armored mage";
			case FeatType.TUMBLE:
				return "Tumble";
			case FeatType.MASTERS_EDGE:
				return "Master's edge";
			case FeatType.STUDENTS_LUCK:
				return "Student's luck";
			case FeatType.ARCANE_SHIELD:
				return "Arcane shield";
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
					"(This also enables you to fire arrows without first switching",
					"to your bow.)"};
			case FeatType.LETHALITY:
				return new List<string>{
					"Your chance to score a critical hit increases by 10%. This",
					"bonus also increases by 5% for each 20% health that the target",
					"is missing."};
			case FeatType.LUNGE:
				return new List<string>{
					"Leap from one space away and attack your target (with a +4",
					"bonus to Combat). The intervening space must be unoccupied."};
			case FeatType.DRIVE_BACK:
				return new List<string>{
					"Enemies must yield ground in order to avoid your attacks.",
					"(If your target has nowhere to run, your attacks will",
					"automatically hit.)"};
			case FeatType.SILENT_CHAINMAIL:
				return new List<string>{
					"You can wear chainmail with no penalty to stealth."};
			case FeatType.ARMORED_MAGE:
				return new List<string>{
					"You can cast spells with no penalty from your armor."};
			case FeatType.FULL_DEFENSE:
				return new List<string>{
					"Stand still to ready yourself for attack. You gain an extra",
					"50% chance to avoid attacks while readied. Enemies that try to",
					"hit you might hit other adjacent enemies instead."};
			case FeatType.TUMBLE:
				return new List<string>{
					"Move up to 2 spaces while avoiding arrows. (Also useful for",
					"slipping behind enemies and putting out fires.)"};
			case FeatType.MASTERS_EDGE:
				/*return new List<string>{
					"Spells you've mastered deal 1d6 extra damage. (You've mastered",
					"a spell if its natural chance of failure is 0%.)"};*/
				return new List<string>{
					"The first offensive spell you've learned will deal 1d6 extra",
					"damage. (Affects the first spell in the list that deals damage",
					"directly.)"};
			case FeatType.STUDENTS_LUCK:
				return new List<string>{
					"Casting a spell of higher level than your Magic skill will now",
					"only drain your magic reserves 50% of the time."};
			case FeatType.ARCANE_SHIELD:
				/*return new List<string>{
					"Drain your magic reserves to heal some of your wounds. Heals",
					"at least 25% of your HP, with a bonus for Magic skill. (Each",
					"drain on your magic reserves gives an extra 25% failure rate",
					"to your spells, and lasts until you rest.)"};*/
				return new List<string>{
					"Drain your magic reserves to shield yourself. The shield lasts",
					"for 20 turns and can block 25 damage, plus a bonus for Magic",
					"skill. (Each drain on your magic reserves gives an extra 25%",
					"failure rate to your spells, and lasts until you rest.)"};
			case FeatType.FORCE_OF_WILL:
				/*return new List<string>{
					"Drain your magic reserves to flawlessly cast a spell. (Having",
					"drained magic reserves still decreases your chance of success,",
					"but nothing else does.)",
					"If you have skill in Spirit, your chances are increased."};*/
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
			case FeatType.DISTRACT:
				return new List<string>{
					"Attempt to misdirect an unaware enemy, causing it to",
					"investigate the source of the sound."};
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

