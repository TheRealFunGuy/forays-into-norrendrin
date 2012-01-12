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
		public void Resolve(){ amount = Global.Roll(dice,6); }
		public Damage(int dice_,bool resolve_immediately,DamageType type_,DamageClass damclass_,Actor source_){
			dice=dice_;
			if(resolve_immediately){
				num=Global.Roll(dice_,6);
			}
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
	/*keen eyes -half check. still needs trap detection bonus.
spirit skill
	-matters in lots of places, i guess. perhaps a TotalDuration method that calculates this - it could even try to be smart
		and treat values below 50 as 'number of turns' and 50+ as 'number of ticks'


danger sense(on)
	 -matters in map.draw...eesh.
here's how this will work:
Tile will gain a list(or other list-like data type) of integers that represent the chance of the player being seen if he steps
there next turn.
Move will gain another part: depending on LOS(just like the light check), visible tiles will have their lists updated.
	-additional steps are required for distant lit tiles, as follows:
		-it is not practical to check every tile to see whether it is lit. therefore, i will create a list of lit tiles in Map.
		-this list will only be updated when the player has Danger Sense.
		-perhaps light_value-- will become a method. this method will decrement, then, if light_value is 0, remove from list.
		-same with light_value++ - if light_value is greater than 0, add to list. this should still be fairly quick.
		-this, of course, only keeps track of passable tiles, but that's perfect anyway.
		-so, each MOVING monster will check LOS to each LIT tile during its move. this is much, much better than
			checking each monster's LOS to each tile on every turn.
ultimately, during Map.Draw, the highest value in each tile's list will be used to find its danger level, & therefore its color

-when displaying skill level, the format should be base + bonus, with bonus colored differently, just so it's perfectly clear.
-lots of things need to alert nearby monsters:many spells, fighting(including instant kills but not neck snaps), certain feats
*/
	public class Actor : PhysicalObject{
		public ActorType type{get; private set;}
		public int maxhp{get; private set;}
		public int curhp{get; set;}
		public int speed{get; set;}
		public int xp{get; private set;}
		public int level{get;set;}
		public int light_radius{get;set;}
		public Actor target{get;set;}
		public List<Item> inv{get; private set;}
		public SpellType[] F{get; private set;} //F[0] is the 'autospell' you cast instead of attacking, if that option is set
		public Dict<AttrType,int> attrs = new Dict<AttrType,int>();
		public Dict<SkillType,int> skills = new Dict<SkillType,int>();
		public Dict<FeatType,int> feats = new Dict<FeatType,int>();
		public Dict<SpellType,int> spells = new Dict<SpellType,int>();
		private int time_of_last_action;
		private int recover_time;
		private List<pos> path = new List<pos>();
		public Tile target_location;
		public int player_visibility_duration;
		public LinkedList<WeaponType> weapons = new LinkedList<WeaponType>();
		public LinkedList<ArmorType> armors = new LinkedList<ArmorType>();
		public LinkedList<MagicItemType> magic_items = new LinkedList<MagicItemType>();

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
			Define(ActorType.RAT,"rat",'r',Color.DarkGray,15,90,0,1,0,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.GOBLIN,"goblin",'g',Color.Green,25,100,0,1,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.LARGE_BAT,"large bat",'b',Color.DarkGray,20,60,0,1,0,AttrType.DARKVISION);
			Define(ActorType.SHAMBLING_SCARECROW,"shambling scarecrow",'x',Color.DarkYellow,40,90,0,1,0,AttrType.CONSTRUCT,AttrType.RESIST_BASH,AttrType.IMMUNE_ARROWS,AttrType.DARKVISION);
			Define(ActorType.SKELETON,"skeleton",'s',Color.White,50,100,0,2,0,AttrType.UNDEAD,AttrType.RESIST_SLASH,AttrType.RESIST_FIRE,AttrType.RESIST_COLD,AttrType.RESIST_ELECTRICITY,AttrType.DARKVISION);
			Define(ActorType.CULTIST,"cultist",'p',Color.DarkRed,60,100,0,2,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SMALL_GROUP);
			Define(ActorType.POLTERGEIST,"poltergeist",'G',Color.DarkGreen,40,90,0,2,0,AttrType.UNDEAD,AttrType.RESIST_COLD,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.ZOMBIE,"zombie",'z',Color.DarkGray,75,150,0,3,0,AttrType.UNDEAD,AttrType.RESIST_COLD);
			Define(ActorType.WOLF,"wolf",'c',Color.DarkYellow,50,60,0,3,0,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.FROSTLING,"frostling",'E',Color.Gray,60,100,0,3,0,AttrType.IMMUNE_COLD,AttrType.COLD_HIT);
			Define(ActorType.GOBLIN_ARCHER,"goblin archer",'g',Color.DarkCyan,50,100,0,4,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.GOBLIN_SHAMAN,"goblin shaman",'g',Color.Magenta,50,100,0,4,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Prototype(ActorType.GOBLIN_SHAMAN).GainSpell(SpellType.FORCE_PALM,SpellType.BURNING_HANDS,SpellType.SHOCK,SpellType.MAGIC_MISSILE,SpellType.IMMOLATE);
			Define(ActorType.SWORDSMAN,"swordsman",'p',Color.White,60,100,0,4,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.DIRE_RAT,"dire rat",'r',Color.DarkRed,25,90,0,5,0,AttrType.LOW_LIGHT_VISION,AttrType.LARGE_GROUP);
			Define(ActorType.DREAM_WARRIOR,"dream warrior",'p',Color.Cyan,45,100,0,5,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.DREAM_CLONE,"dream warrior",'p',Color.Cyan,1,100,0,0,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.CONSTRUCT,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.BANSHEE,"banshee",'G',Color.Magenta,50,80,0,5,0,AttrType.UNDEAD,AttrType.RESIST_COLD,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.WARG,"warg",'c',Color.White,55,60,0,6,0,AttrType.LOW_LIGHT_VISION,AttrType.MEDIUM_GROUP);
			Define(ActorType.ROBED_ZEALOT,"robed zealot",'p',Color.Yellow,60,100,0,6,6,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Prototype(ActorType.ROBED_ZEALOT).GainSpell(SpellType.MINOR_HEAL,SpellType.BLESS,SpellType.HOLY_SHIELD);
			Define(ActorType.SKULKING_KILLER,"skulking killer",'p',Color.DarkBlue,60,100,0,6,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.STEALTHY,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.CARRION_CRAWLER,"carrion crawler",'i',Color.DarkGreen,50,100,0,7,0,AttrType.PARALYSIS_HIT,AttrType.DARKVISION);
			Define(ActorType.OGRE,"ogre",'O',Color.Green,75,100,0,7,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.DARKVISION,AttrType.SMALL_GROUP);
			Define(ActorType.SHADOW,"shadow",'G',Color.DarkGray,50,100,0,7,0,AttrType.UNDEAD,AttrType.RESIST_COLD,AttrType.DIM_VISION_HIT,AttrType.DARKVISION);
			Define(ActorType.BERSERKER,"berserker",'p',Color.Red,60,100,0,8,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.ORC_GRENADIER,"orc grenadier",'o',Color.DarkYellow,60,100,0,8,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.PHASE_SPIDER,"phase spider",'A',Color.Cyan,60,100,0,8,0,AttrType.POISON_HIT,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.STONE_GOLEM,"stone golem",'x',Color.Gray,80,120,0,9,0,AttrType.CONSTRUCT,AttrType.STALAGMITE_HIT,AttrType.RESIST_SLASH,AttrType.RESIST_FIRE,AttrType.RESIST_COLD,AttrType.RESIST_ELECTRICITY,AttrType.DARKVISION);
			Define(ActorType.NECROMANCER,"necromancer",'p',Color.Blue,60,100,0,9,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.TROLL,"troll",'T',Color.DarkGreen,60,100,0,9,0,AttrType.REGENERATING,AttrType.REGENERATES_FROM_DEATH,AttrType.DARKVISION);
			Define(ActorType.LASHER_FUNGUS,"lasher fungus",'F',Color.DarkGreen,60,100,0,10,0,AttrType.PLANTLIKE,AttrType.SPORE_BURST,AttrType.RESIST_BASH,AttrType.DARKVISION);
			Define(ActorType.ORC_WARMAGE,"orc warmage",'o',Color.Red,60,100,0,10,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Prototype(ActorType.ORC_WARMAGE).GainSpell(SpellType.ARC_LIGHTNING,SpellType.BURNING_HANDS,SpellType.FORCE_BEAM,SpellType.SHOCK,SpellType.SONIC_BOOM,SpellType.IMMOLATE);
			Define(ActorType.CORPSETOWER_BEHEMOTH,"corpsetower behemoth",'z',Color.DarkMagenta,100,120,0,10,0,AttrType.UNDEAD,AttrType.TOUGH,AttrType.REGENERATING,AttrType.RESIST_COLD);
			Define(ActorType.FIRE_DRAKE,"fire drake",'D',Color.DarkRed,150,90,0,10,0,AttrType.BOSS_MONSTER,AttrType.DARKVISION,AttrType.FIRE_HIT,AttrType.IMMUNE_FIRE,AttrType.HUMANOID_INTELLIGENCE);
		}
		private static void Define(ActorType type_,string name_,char symbol_,Color color_,int maxhp_,int speed_,int xp_,int level_,int light_radius_,params AttrType[] attrlist){
			proto[type_] = new Actor(type_,name_,symbol_,color_,maxhp_,speed_,xp_,level_,light_radius_,attrlist);
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
			xp = a.xp;
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
		}
		public Actor(ActorType type_,string name_,char symbol_,Color color_,int maxhp_,int speed_,int xp_,int level_,int light_radius_,params AttrType[] attrlist){
			type = type_;
			name = name_;
			the_name = "the " + name;
			a_name = "a " + name;
			if(name=="you"){
				the_name = "you";
				a_name = "you";
			}
			if(name[0].ToString().ToUpper() == "O"){ //only ogres and orcs currently, therefore *hack*
				a_name = "an " + name;
			}
			symbol = symbol_;
			color = color_;
			maxhp = maxhp_;
			curhp = maxhp;
			speed = speed_;
			xp = xp_;
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
			foreach(AttrType at in attrlist){
				attrs[at]++;
			}//row and col are -1
		}
		public static Actor Create(ActorType type,int r,int c){
			Actor a = null;
			if(M.actor[r,c] == null){
				a = new Actor(proto[type],r,c);
				M.actor[r,c] = a;
				a.QS();
				if(a.light_radius > 0){
					a.UpdateRadius(0,a.light_radius);
				}
			}
			return a;
		}
		public void Move(int r,int c){
			if(r>=0 && r<ROWS && c>=0 && c<COLS){
				if(M.actor[r,c] == null){
					bool torch=false;
					if(LightRadius() > 0){
						torch=true;
						UpdateRadius(LightRadius(),0);
					}
					M.actor[r,c] = this;
					if(row>=0 && row<ROWS && col>=0 && col<COLS){
						M.actor[row,col] = null;
					}
					row = r;
					col = c;
					if(torch){
						UpdateRadius(0,LightRadius());
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
		public bool HasAttr(AttrType attr){ return attrs[attr] > 0; }
		public bool HasFeat(FeatType feat){ return feats[feat] > 0; }
		public bool HasSpell(SpellType spell){ return spells[spell] > 0; }
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
		public int Stealth(){ //this method should probably become part of TotalSkill
			if(LightRadius() > 0){
				return 0; //negative stealth is the same as zero stealth
			}
			int total = TotalSkill(SkillType.STEALTH);
			if(!M.tile[row,col].IsLit()){
				total += 2;
			}
			if(!HasFeat(FeatType.ARMORED_MAGE)){
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
		public int DurationOfMagicalEffect(int original){ //intended to be used with whole turns, i.e. numbers below 50.
			int diff = (original * TotalSkill(SkillType.SPIRIT)) / 20; //each point of Spirit takes off 1/20th of the duration
			return original - diff; //therefore, maxed Spirit cuts durations in half
		}
		public static int Rarity(ActorType type){
			int result = 1;
			if(((int)type)%3 == 2){
				result = 2;
			}
			if(type == ActorType.PLAYER || type == ActorType.FIRE_DRAKE
			|| type == ActorType.RAT || type == ActorType.DREAM_CLONE){
				return 0;
			}
			return result;
		}
		public void UpdateRadius(int from,int to){ UpdateRadius(from,to,false); }
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
		}
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
		public string YouAre(){
			if(name == "you"){
				return "you are";
			}
			else{
				return the_name + " is";
			}
		}
		public string Your(){
			if(name == "you"){
				return "your";
			}
			else{
				return the_name + "'s";
			}
		}
		public string You(string s){ return You(s,false); }
		public string You(string s,bool ends_in_es){
			if(name == "you"){
				return "you " + s;
			}
			else{
				if(ends_in_es){
					return the_name + " " + s + "es";
				}
				else{
					return the_name + " " + s + "s";
				}
			}
		}
		public string YouFeel(){
			if(name == "you"){
				return "you feel";
			}
			else{
				return the_name + " looks";
			}
		}
		public void Input(){
			bool return_before_input = false;
			if(HasAttr(AttrType.DEFENSIVE_STANCE)){
				attrs[AttrType.DEFENSIVE_STANCE] = 0;
			}
			if(HasAttr(AttrType.PARALYZED)){
				attrs[AttrType.PARALYZED]--;
				if(type == ActorType.PLAYER){
					B.AddDependingOnLastPartialMessage("You can't move! ");
				}
				else{ //handled differently for the player: since the map still needs to be drawn,
					B.Add(the_name + " can't move! ",this);
					Q1();						// this is handled in InputHuman().
					return_before_input = true; //the message is still printed, of course.
				}
			}
			if(HasAttr(AttrType.AFRAID)){
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
					return_before_input = true;
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
							recover_time = Q.turn + 200;
						}
						else{
							recover_time = Q.turn + 500;
						}
						curhp++;
					}
				}
					
			}
			if(HasAttr(AttrType.POISONED) && time_of_last_action < Q.turn){
				if(!TakeDamage(DamageType.POISON,Global.Roll(1,3)-1,null)){
					return;
				}
			}
			if(HasAttr(AttrType.ON_FIRE) && time_of_last_action < Q.turn){
				B.Add(YouAre() + " on fire! ",this);
				if(!TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,Global.Roll(attrs[AttrType.ON_FIRE],6),null)){
					return;
				}
			}
			if(return_before_input){
				return;
			}
			if(type==ActorType.PLAYER){
				InputHuman();
			}
			else{
				InputAI();
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
			if(HasAttr(AttrType.ON_FIRE) && attrs[AttrType.ON_FIRE] < 5 && time_of_last_action < Q.turn){
				if(attrs[AttrType.ON_FIRE] >= light_radius){
					UpdateRadius(attrs[AttrType.ON_FIRE],attrs[AttrType.ON_FIRE]+1);
				}
				attrs[AttrType.ON_FIRE]++;
			}
			if(HasAttr(AttrType.CATCHING_FIRE) && time_of_last_action < Q.turn){
				attrs[AttrType.CATCHING_FIRE] = 0;
				if(!HasAttr(AttrType.ON_FIRE)){
					if(light_radius == 0){
						UpdateRadius(0,1);
					}
					attrs[AttrType.ON_FIRE] = 1;
				}
			}
			time_of_last_action = Q.turn; //this might eventually need a slight rework for 0-time turns
		}
		private char ConvertInput(ConsoleKeyInfo k){
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
		private char ConvertVIKeys(char ch){
			switch(ch){
			case 'h':
				return '4';
			case 'j':
				return '2';
			case 'k':
				return '8';
			case 'l':
				return '6';
			case 'y':
				return '7';
			case 'u':
				return '9';
			case 'b':
				return '1';
			case 'n':
				return '3';
			default:
				return ch;
			}
		}
		public void InputHuman(){
			DisplayStats();
			//temporary turn display:
			Console.SetCursorPosition(1,2);
			Console.Write("{0} ",Q.turn / 100);
			//end temporary turn display
			if(Screen.MapChar(0,0).c == '-'){ //kinda hacky. there won't be an open door in the corner, so this looks for
				M.RedrawWithStrings(); //evidence of Select being called (& therefore, the map needing to be redrawn entirely)
			}
			else{
				M.Draw();
			}
			if(!HasAttr(AttrType.AFRAID) && !HasAttr(AttrType.PARALYZED)){
				B.Print(false);
			}
			else{
				B.DisplayNow();
			}
			Cursor();
			Console.CursorVisible = true;
			if(HasAttr(AttrType.PARALYZED) || HasAttr(AttrType.AFRAID)){
				Thread.Sleep(250);
				Q1();
				return;
			}
			if(HasAttr(AttrType.RUNNING)){
				bool monsters_visible = false;
				foreach(Actor a in M.AllActors()){
					if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){ //check LOS, prevents detected mobs from stopping you
						monsters_visible = true;
					}
				}
				if(!monsters_visible && TileInDirection(attrs[AttrType.RUNNING]).passable){
					if(attrs[AttrType.RUNNING] == 5){
						int hplimit = HasFeat(FeatType.ENDURING_SOUL)? 20 : 10;
						if(curhp % hplimit == 0){
							attrs[AttrType.RUNNING] = 0;
						}
						else{
							Q1();
							return;
						}
					}
					else{
						PlayerWalk(attrs[AttrType.RUNNING]);
						return;
					}
				}
				else{
					attrs[AttrType.RUNNING] = 0;
				}
			}
			if(HasAttr(AttrType.RESTING)){
				if(attrs[AttrType.RESTING] == 10){
					attrs[AttrType.RESTING] = -1;
					curhp += ((maxhp - curhp) / 2); //recover half of your missing health
					ResetSpells();
					B.Add("You rest...you feel great! ");
					B.Print(false);
					Cursor();
				}
				else{
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){ //check LOS, prevents detected mobs from stopping you
							monsters_visible = true;
						}
					}
					if(!monsters_visible){
						attrs[AttrType.RESTING]++;
						B.Add("You rest... ");
						Q1();
						return;
					}
					else{
						attrs[AttrType.RESTING] = 0;
						B.Add("You rest...you are interrupted! ");
						B.Print(false);
						Cursor();
					}
				}
			}
			ConsoleKeyInfo command = Console.ReadKey(true);
			char ch = ConvertInput(command);
			if(Global.Option(OptionType.VI_KEYS)){
				ch = ConvertVIKeys(ch);
			}
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
					if(!HasAttr(AttrType.IMMOBILIZED) && !HasAttr(AttrType.CATCHING_FIRE) && !HasAttr(AttrType.ON_FIRE)){
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
				if(HasAttr(AttrType.IMMOBILIZED)){
					attrs[AttrType.IMMOBILIZED] = 0;
					B.Add("You break free. ");
					QS();
					break;
				}
				if(M.tile[row,col].inv != null){
					B.Add("You see " + M.tile[row,col].inv.a_name + ". ");
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
					}
				}
				else{
					Q0();
				}
				break;
				}
			case 'o':
				{
				int door = DirectionOfOnly(TileType.DOOR_C);
				int chest = DirectionOfOnly(TileType.CHEST);
				bool ask=false;
				if(door == -1){
					ask = true;
				}
				else{
					if(door == 0){
						if(chest == -1){
							ask=true;
						}
						else{
							if(chest == 0){
								B.Add("There's nothing to open here. ");
								Q0();
							}
							else{
								if(StunnedThisTurn()){
									break;
								}
								TileInDirection(chest).Toggle(this);
								Q1();
							}
						}
					}
					else{
						if(chest == 0){
							if(StunnedThisTurn()){
								break;
							}
							TileInDirection(door).Toggle(this);
							Q1();
						}
						else{
							ask=true;
						}
					}
				}
				if(ask){
					B.DisplayNow("Open in which direction? ");
					char inchar = ConvertInput(Console.ReadKey(true));
					if(inchar >= '1' && inchar <= '9' && inchar != '5'){
						if(StunnedThisTurn()){
							break;
						}
						TileInDirection((int)inchar - 48).Toggle(this);
						Q1();
					}
					else{
						Q0();
					}
				}
				break;
				}
			case 'c':
				{
				int door = DirectionOfOnly(TileType.DOOR_O);
				if(door == -1){
					B.DisplayNow("Close in which direction? ");
					char inchar = ConvertInput(Console.ReadKey(true));
					if(inchar >= '1' && inchar <= '9' && inchar != '5'){
						if(StunnedThisTurn()){
							break;
						}
						TileInDirection((int)inchar - 48).Toggle(this);
						Q1();
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
				}
			case 'f':
				{
				if(weapons.First.Value == WeaponType.BOW || weapons.First.Value == WeaponType.HOLY_LONGBOW){
					if(Global.Option(OptionType.LAST_TARGET) && target!=null && DistanceFrom(target)==1){ //since you can't fire
						target = null;										//at adjacent targets anyway.
					}
					Tile t = GetTarget(12);
					if(t != null){
						if(DistanceFrom(t) > 1){
							FireArrow(t);
						}
						else{
							B.Add("You can't fire at adjacent targets. ");
							Q0();
						}
					}
					else{
						Q0();
					}
				}
				else{
					B.Add("You can't fire arrows without your bow equipped. ");
					Q0();
				}
				break;
				}
			case 'F':
				{
				List<FeatType> ft = new List<FeatType>();
				List<char> charlist = new List<char>();
				foreach(FeatType f in Enum.GetValues(typeof(FeatType))){
					if(f != FeatType.NO_FEAT && f != FeatType.NUM_FEATS){
						ft.Add(f);
					}
				}
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				char letter = 'a';
				int i=1;
				foreach(FeatType f in ft){
					string s = "[" + letter + "] " + Feat.Name(f);
					if(HasFeat(f)){
						Screen.WriteMapString(i,0,new colorstring(Color.Cyan,s.PadRight(COLS)));
						Screen.WriteMapString(i,0,new colorstring(Color.Gray,"[" + letter + "]"));
						Screen.WriteMapChar(i,1,new colorchar(Color.Cyan,Screen.MapChar(i,1).c));
						charlist.Add(letter);
					}
					else{
						Screen.WriteMapString(i,0,new colorstring(Color.DarkGray,s.PadRight(COLS)));
						Screen.WriteMapChar(i,1,new colorchar(Color.DarkRed,Screen.MapChar(i,1).c));
					}
					letter++;
					i++;
				}
				Screen.WriteMapString(21,0,"".PadRight(COLS,'-'));
				if(ft.Count > 0){
					B.DisplayNow("Select a feat: ");
				}
				else{
					B.DisplayNow("You haven't learned any feats yet: ");
				}
				Console.CursorVisible = true;
				FeatType selected_feat = FeatType.NO_FEAT;
				bool done = false;
				while(!done){
					command = Console.ReadKey(true);
					ch = ConvertInput(command);
					int ii = ch - 'a';
					if(charlist.Contains(ch)){
						selected_feat = (FeatType)ii;
						done = true;
					}
					if(ch == (char)27 || ch == ' '){
						done = true;
					}
				}
				M.RedrawWithStrings();
				if(selected_feat != FeatType.NO_FEAT){
					if(!UseFeat(selected_feat)){
						Q0();
					}
				}
				else{
					Q0();
				}
				break;
				}
			case 'Z':
				{
				List<string> ls = new List<string>();
				List<SpellType> sp = new List<SpellType>();
				foreach(SpellType s in Enum.GetValues(typeof(SpellType))){
					if(HasSpell(s)){
						ls.Add(Spell.Name(s));
						sp.Add(s);
					}
				}
				if(sp.Count > 0){
					int i = Select("Cast which spell? ",ls);
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
					Q0();
				}
				break;
				}
			case 'R':
				if(attrs[AttrType.RESTING] != -1){ //gets set to -1 if you've rested on this level
					bool monsters_visible = false;
					foreach(Actor a in M.AllActors()){
						if(a!=this && CanSee(a) && HasLOS(a.row,a.col)){ //check LOS, prevents detected mobs from stopping you
							monsters_visible = true;
						}
					}
					if(!monsters_visible){
						if(StunnedThisTurn()){
							break;
						}
						attrs[AttrType.RESTING] = 1;
						B.Add("You rest... ");
						Q1();
					}
					else{
						B.Add("You can't rest while there are enemies around! ");
						Q0();
					}
				}
				else{
					B.Add("You find it impossible to rest right now. ");
					Q0();
				}
				break;
			case '>':
				if(M.tile[row,col].type == TileType.STAIRS){
					if(StunnedThisTurn()){
						break;
					}
					//todo: check for RESTING == -1 && curhp<maxhp here?
					B.Add("You walk down the stairs. ");
					B.PrintAll();
					//generate level
					//heal
					//reset spells
					Q0();
				}
				else{
					B.Add("There are no stairs here. ");
					Q0();
				}
				break;
			case 'g':
			case ';': //todo: check stun only if there's an item
				if(StunnedThisTurn()){
					break;
				}
				Q0();
				break;
			case 'd': //todo: check stun after item selection?
				if(StunnedThisTurn()){
					break;
				}
				Q0();
				break;
			case 'i':
				Q0();
				break;
			case 'u': //todo: check stun after item selection?
				if(StunnedThisTurn()){
					break;
				}
				Q0();
				break;
			case 'W':
				{
				if(StunnedThisTurn()){
					break;
				}
				WeaponType old_weapon = weapons.First.Value;
				bool done=false;
				while(!done){
					DisplayStats(true,false);
					Console.SetCursorPosition(4,7);
					ConsoleKeyInfo command2 = Console.ReadKey(true);
					char ch2 = ConvertInput(command2);
					if(Global.Option(OptionType.VI_KEYS)){
						ch2 = ConvertVIKeys(ch2);
					}
					switch(ch2){
					case '8':
						{
						WeaponType w = weapons.Last.Value;
						weapons.Remove(w);
						weapons.AddFirst(w);
						break;
						}
					case '2':
						{
						WeaponType w = weapons.First.Value;
						weapons.Remove(w);
						weapons.AddLast(w);
						break;
						}
					case (char)27:
					case ' ':
						Q0();
						return;
					case (char)13:
						done=true;
						break;
					default:
						break;
					}
				}
				if(old_weapon == weapons.First.Value){
					Q0();
				}
				else{
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
				break;
				}
			case 'A':
				{
				if(StunnedThisTurn()){
					break;
				}
				ArmorType old_armor = armors.First.Value;
				bool done=false;
				while(!done){
					DisplayStats(false,true);
					Console.SetCursorPosition(4,8);
					ConsoleKeyInfo command2 = Console.ReadKey(true);
					char ch2 = ConvertInput(command2);
					if(Global.Option(OptionType.VI_KEYS)){
						ch2 = ConvertVIKeys(ch2);
					}
					switch(ch2){
					case '8':
						{
						ArmorType a = armors.Last.Value;
						armors.Remove(a);
						armors.AddFirst(a);
						break;
						}
					case '2':
						{
						ArmorType a = armors.First.Value;
						armors.Remove(a);
						armors.AddLast(a);
						break;
						}
					case (char)27:
					case ' ':
						Q0();
						return;
					case (char)13:
						done=true;
						break;
					default:
						break;
					}
				}
				if(old_armor == armors.First.Value){
					Q0();
				}
				else{
					B.Add("You wear your " + Armor.Name(armors.First.Value) + ". ");
					Q1();
					UpdateOnEquip(old_armor,armors.First.Value);
				}
				break;
				}
			case '!': //note that these are the top-row numbers, NOT the actual shifted versions
			case '@': //<---this is the '2' above the 'w'    (not the '@')
			case '#':
			case '$':
			case '%':
				{
				if(StunnedThisTurn()){
					break;
				}
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
				break;
				}
			case '*': //these are toprow numbers, not shifted versions. see above.
			case '(':
			case ')':
				{
				if(StunnedThisTurn()){
					break;
				}
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
			case 'l':
				GetTarget(true);
				Q0();
				break;
			case (char)9: //tab
				{
				List<PhysicalObject> interesting_targets = new List<PhysicalObject>(); //c&p go
				foreach(Actor a in M.AllActors()){
					if(a != this && CanSee(a)){
						interesting_targets.Add(a);
					}
				}
				if(Global.Option(OptionType.ITEMS_AND_TILES_ARE_INTERESTING)){
					foreach(Tile t in M.AllTiles()){
						if(t.type == TileType.DOOR_C || t.type == TileType.DOOR_O
						|| t.type == TileType.STAIRS || t.type == TileType.CHEST
						|| t.type == TileType.GRENADE || t.type == TileType.FIREPIT
						|| t.type == TileType.STALAGMITE //todo: traps here
						|| t.inv != null){
							if(CanSee(t)){
								interesting_targets.Add(t);
							}
						}
					}
				}
				if(interesting_targets.Count > 0){
					GetTarget(true,-1,true);
				}
				else{
					B.Add("You don't see anything interesting. ");
				}
				Q0();
				break;
				}
			case 'T':
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
					B.Add("You activate your everburning torch. ");
				}
				else{
					UpdateRadius(LightRadius(),0,true);
					UpdateRadius(0,attrs[AttrType.ON_FIRE]);
					B.Add("You deactivate your everburning torch. ");
				}
				Q1();
				break;
			case 'P':
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
			case '=':
				Q0();
				break;
			case '?':
				Q0();
				break;
			case 'Q':
				Environment.Exit(0);
				break;
			case 'C': //debug mode 
				{
				List<string> l = new List<string>();
				l.Add("Throw a prismatic orb");
				l.Add("Ice explosion animation");
				l.Add("Toggle low light vision");
				l.Add("Check key names");
				l.Add("Forget the map");
				l.Add("Heal to full");
				l.Add("Become invulnerable");
				l.Add("test empty Select list");
				l.Add("Spawn a monster");
				l.Add("Use a rune of passage");
				l.Add("See the entire level");
				l.Add("Generate new level");
				l.Add("Create grenades!");
				l.Add("Level up");
				l.Add("PARALYZED!");
				switch(Select("Activate which cheat? ",l)){
				case 0:
					{
					new Item(ConsumableType.PRISMATIC_ORB,"prismatic orb",'*',Color.White).Use(this);
					Q1();
					break;
					}
				case 1:
					Screen.AnimateExplosion(this,5,new colorchar(Color.RandomIce,'*'),25);
					Q1();
					break;
				case 2:
					if(HasAttr(AttrType.LOW_LIGHT_VISION)){
						attrs[AttrType.LOW_LIGHT_VISION] = 0;
					}
					else{
						attrs[AttrType.LOW_LIGHT_VISION]++;
					}
					Q0();
					break;
				case 3:
					ConsoleKeyInfo command2 = Console.ReadKey(true);
					Console.Write(command2.Key);
					Q0();
					break;
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
					int selection = Select("Learn which spell? ",new List<string>(),false,true);
					B.Add(selection.ToString());
					Q0();
					}
					break;
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
					Tile t = GetTarget();
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
					}
					Q0();
					break;
				case 13:
					LevelUp();
					Q0();
					break;
				case 14:
					attrs[AttrType.PARALYZED] = 10;
					Q0();
					break;
				default:
					Q0();
					break;
				}
				break;
				}
			case ' ':
				Q0();
				break;
			default:
				Q0();
				break;
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
							CastSpell(F[0],TileInDirection(dir));
						}
					}
					else{
						ActorInDirection(dir).attrs[AttrType.TURNS_VISIBLE] = -1;
						if(!IsHiddenFrom(ActorInDirection(dir))){
							B.Add("You walk straight into " + ActorInDirection(dir).a_name + "! ");
						}
						else{
							B.Add("You walk straight into " + ActorInDirection(dir).a_name + "! ");
							B.Add(ActorInDirection(dir).the_name + " looks just as surprised as you. ");
							ActorInDirection(dir).player_visibility_duration = -1;
						}
						Q1();
					}
				}
				else{
					if(TileInDirection(dir).passable){
						if(Global.Option(OptionType.OPEN_CHESTS) && TileInDirection(dir).type==TileType.CHEST){
							if(StunnedThisTurn()){
								return;
							}
							TileInDirection(dir).Toggle(this);
							Q1();
						}
						else{
							if(TileInDirection(dir).inv != null){
								B.Add("You see " + TileInDirection(dir).inv.a_name + ". ");
							}
							Move(TileInDirection(dir).row,TileInDirection(dir).col);
							QS();
						}
					}
					else{
						if(TileInDirection(dir).type == TileType.DOOR_C){
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
			if(CanSee(player)){
				if(target_location == null && HasAttr(AttrType.DETECTING_MONSTERS)){ //orc warmages etc. when they first notice
					player_visibility_duration = -1;
					target = player;
					target_location = M.tile[player.row,player.col];
					B.Add(Your() + " gaze meets your eyes! ",this); //better message?
					//todo: possibly alert others here
					Q1();
					return;
				}
				else{
					target = player;
					target_location = M.tile[player.row,player.col];
					player_visibility_duration = -1;
				}
			}
			else{
				if((IsWithinSightRangeOf(player.row,player.col) || M.tile[player.row,player.col].IsLit())
					&& HasLOS(player.row,player.col)){ //if they're stealthed and nearby...
					if(player.Stealth() * DistanceFrom(player) * 10 - player_visibility_duration++*5 < Global.Roll(1,100)){
						player_visibility_duration = -1;
						target = player;
						target_location = M.tile[player.row,player.col];
						//print different messages here todo
						B.Add(the_name + " notices you. ",this);
						//alert others todo
						Q1();
						return;
					}
				}
				else{
					if(player_visibility_duration >= 0){ //if they hadn't seen the player yet...
						player_visibility_duration = 0;
					}
					else{
						if(target_location == null && player_visibility_duration-- == -10){
							player_visibility_duration = 0;
							target = null; //maybe introduce a TIMES_ALERTED attr that makes it harder and harder for
						}//them to calm down, eventually noticing you instantly?
					}
				}
			}
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
			case ActorType.SHAMBLING_SCARECROW:
				if(DistanceFrom(target) == 1){
					if(HasAttr(AttrType.ON_FIRE)){
						attrs[AttrType.FIRE_HIT]++;
					}
					Attack(0,target);
					if(HasAttr(AttrType.ON_FIRE)){
						attrs[AttrType.FIRE_HIT]--;
					}
				}
				else{
					if(speed == 75){
						if(curhp < maxhp){
							AI_Step(target);
							QS();
						}
						else{
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
							Q1(); //note that the scarecrow doesn't move quickly until it is disturbed.
						}
					}
					else{
						AI_Step(target);
						QS();
					}
				}
				break;
			case ActorType.GOBLIN_ARCHER:
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
							bool immob = false;
							if(HasAttr(AttrType.IMMOBILIZED)){
								immob = true;
							}
							if(AI_Sidestep(target)){
								if(!immob){
									B.Add(the_name + " tries to line up a shot. ",this);
								}
							}
							QS();
						}
					}
					break;
				case 3:
				case 4:
				case 5:
					if(FirstActorInLine(target) == target){
						FireArrow(target);
					}
					else{
						bool immob = false;
						if(HasAttr(AttrType.IMMOBILIZED)){
							immob = true;
						}
						if(AI_Sidestep(target)){
							if(!immob){
								B.Add(the_name + " tries to line up a shot. ",this);
							}
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
			case ActorType.FROSTLING:
				if(DistanceFrom(target) == 1){
					if(!HasAttr(AttrType.COOLDOWN_2)){ //burst attack cooldown
						attrs[AttrType.COOLDOWN_2]++;
						int cooldown = 100 * (Global.Roll(1,3) + 8);
						Q.Add(new Event(this,cooldown,AttrType.COOLDOWN_2));
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
					if(FirstActorInLine(target) == target && !HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 8){
						int cooldown = Global.Roll(1,4);
						if(cooldown != 1){
							attrs[AttrType.COOLDOWN_1]++;
							cooldown *= 100;
							Q.Add(new Event(this,cooldown,AttrType.COOLDOWN_1));
						}
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
			case ActorType.GOBLIN_SHAMAN:
				switch(DistanceFrom(target)){
				case 1:
					if(target.EnemiesAdjacent() > 1 || Global.CoinFlip()){
						CastRandomSpell(target,SpellType.FORCE_PALM,SpellType.BURNING_HANDS);
					}
					else{
						if(AI_Step(target,true)){
							QS();
						}
						else{
							CastRandomSpell(target,SpellType.FORCE_PALM,SpellType.BURNING_HANDS);
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
								CastRandomSpell(target,SpellType.SHOCK,SpellType.MAGIC_MISSILE,SpellType.IMMOLATE);
							}
							else{
								AI_Sidestep(target);
								QS();
							}
						}
					}
					else{
						if(FirstActorInLine(target) == target){
							CastRandomSpell(target,SpellType.SHOCK,SpellType.MAGIC_MISSILE,SpellType.IMMOLATE);
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
					if(FirstActorInLine(target) == target){
						CastRandomSpell(target,SpellType.SHOCK,SpellType.MAGIC_MISSILE,SpellType.IMMOLATE);
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
			case ActorType.ZOMBIE:
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
				switch(DistanceFrom(target)){
				case 1:
					if(curhp <= 20){
						CastSpell(SpellType.MINOR_HEAL);
					}
					else{
						if(HasAttr(AttrType.BLESSED)){
							Attack(0,target);
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
					if(curhp <= 30){
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
					if(curhp <= 40){
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
			case ActorType.CARRION_CRAWLER:
				if(DistanceFrom(target) == 1){
					if(target.HasAttr(AttrType.PARALYZED)){
						Attack(1,target);
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
				}
				break;
			case ActorType.ORC_WARMAGE:
				switch(DistanceFrom(target)){
				case 1:
					if(target.EnemiesAdjacent() > 1 || Global.CoinFlip()){
						CastRandomSpell(target,SpellType.ARC_LIGHTNING,SpellType.BURNING_HANDS);
					}
					else{
						if(AI_Step(target,true)){
							QS();
						}
						else{
							CastRandomSpell(target,SpellType.ARC_LIGHTNING,SpellType.BURNING_HANDS);
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
								CastRandomSpell(target,SpellType.FORCE_BEAM,SpellType.FORCE_BEAM,SpellType.SHOCK,SpellType.SONIC_BOOM,SpellType.IMMOLATE);
							}
							else{
								AI_Sidestep(target);
								QS();
							}
						}
					}
					else{
						if(FirstActorInLine(target) == target){
							CastRandomSpell(target,SpellType.FORCE_BEAM,SpellType.FORCE_BEAM,SpellType.SHOCK,SpellType.SONIC_BOOM,SpellType.IMMOLATE);
						}
						else{
							if(AI_Step(target,true)){
								QS();
							}
							else{
								AI_Sidestep(target);
							}
						}
					}
					break;
				case 3:
				case 4:
				case 5:
					if(FirstActorInLine(target) == target){
						CastRandomSpell(target,SpellType.FORCE_BEAM,SpellType.FORCE_BEAM,SpellType.SHOCK,SpellType.SONIC_BOOM,SpellType.IMMOLATE);
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
			case ActorType.LASHER_FUNGUS:
				if(DistanceFrom(target) <= 6){
					if(DistanceFrom(target) == 1){
						Attack(0,target);
					}
					else{
						if(FirstActorInLine(target) == target){
							if(Global.Roll(1,4) == 4){
								Attack(0,target);
							}
							else{
								if(Attack(1,target)){
									if(target.HasAttr(AttrType.IMMOBILIZED)){
										if(target.name == "you"){
											B.Add("You don't move far. ");
										}
										else{
											B.Add(target.the_name + " doesn't move far. ",target,this);
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
					}
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
						B.Add(You("whisper") + " '" + invocation + "'. ");
					}
					else{
						B.Add(You("scream") + " '" + invocation.ToUpper() + "'. ");
					}
					B.Add("Flames erupt from " + the_name + ". ");
					if(LightRadius() < 2){
						UpdateRadius(LightRadius(),2);
					}
					attrs[AttrType.ON_FIRE] = Math.Max(attrs[AttrType.ON_FIRE],2);
					foreach(Actor a in ActorsAtDistance(1)){
						if(!a.HasAttr(AttrType.RESIST_FIRE) && !a.HasAttr(AttrType.IMMUNE_FIRE)){
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
			case ActorType.POLTERGEIST: //after it materializes, of course
				//
				break;
			case ActorType.SWORDSMAN:
				if(DistanceFrom(target) == 1){
					Attack(0,target);
					if(!HasAttr(AttrType.COOLDOWN_1)){
						B.Add(You("adopt") + " a more aggressive stance. ");
						attrs[AttrType.BONUS_COMBAT] += 3;
					}
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.DREAM_WARRIOR:
				if(DistanceFrom(target) == 1){
					if(curhp <= 20 && !HasAttr(AttrType.COOLDOWN_1)){
						attrs[AttrType.COOLDOWN_1]++;
						List<Tile> openspaces = new List<Tile>();
						foreach(Tile t in target.TilesAtDistance(1)){
							if(t.passable && t.actor() == null){
								openspaces.Add(t);
							}
						}
						foreach(Tile t in openspaces){
							Create(ActorType.DREAM_CLONE,t.row,t.col);
							t.actor().player_visibility_duration = -1;
						}
						openspaces.Add(tile());
						Tile newtile = openspaces[Global.Roll(openspaces.Count)-1];
						if(newtile != tile()){
							Move(newtile.row,newtile.col);
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
			case ActorType.BANSHEE:
				if(!HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(Global.Roll(5)+5)*100,AttrType.COOLDOWN_1));
					B.Add(You("scream") + ". ");
					int i = 1;
					Actor a;
					List<Actor> targets = new List<Actor>();
					for(bool done=false;!done;++i){
						a = FirstActorInLine(target,i);
						if(!a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT) && !a.HasAttr(AttrType.PLANTLIKE)){
							targets.Add(a);
						}
						if(a == target){
							done = true;
						}
					}
					foreach(Actor actor in targets){
						if(actor.TakeDamage(new Damage(1,true,DamageType.MAGIC,DamageClass.MAGICAL,this))){
							actor.attrs[AttrType.AFRAID]++;
							Q.Add(new Event(actor,DurationOfMagicalEffect((Global.Roll(3)+2))*100,AttrType.AFRAID));
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
			case ActorType.SKULKING_KILLER:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) < 4){
					attrs[AttrType.COOLDOWN_1]++;
					attrs[AttrType.TURNS_VISIBLE] = -1;
					if(Global.Roll(10) >= 4){
						AnimateProjectile(target,Color.Gray,'%');
						if(target.CanSee(this)){
							B.Add(the_name + " throws a bola at " + target.the_name + ". ",this,target);
						}
						else{
							B.Add("A bola whirls toward " + target.the_name + ". ",this,target);
						}
						target.attrs[AttrType.SLOWED]++;
						target.speed += 50;
						Q.Add(new Event(target,(Global.Roll(3)+4)*100,AttrType.SLOWED,target.YouAre() + " no longer slowed. ",target));
						B.Add(target.YouAre() + " slowed by the bola. ",target);
						Q1();
					}
					else{
						AnimateProjectile(target,Color.Gray,'*');
						if(target.CanSee(this)){
							B.Add(the_name + " throws a bola at " + target.the_name + ". ",this,target);
						}
						else{
							B.Add("A bola whirls toward " + target.the_name + ". ",this,target);
						}
						B.Add("It hits " + target.Your() + " head! ",target);
						if(target.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(2,6),this)){
							target.attrs[AttrType.STUNNED]++;
							Q.Add(new Event(target,(Global.Roll(3)+4)*100,AttrType.STUNNED,target.YouAre() + " no longer stunned. ",target));
							B.Add(target.YouAre() + " stunned. ",target);
						}
						Q1();
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
			case ActorType.SHADOW: //default for now
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.BERSERKER:
				if(HasAttr(AttrType.COOLDOWN_2)){
					int dir = attrs[AttrType.COOLDOWN_2];
					bool cw = Global.CoinFlip();
					if(TileInDirection(dir).passable && ActorInDirection(dir) == null){
						if(HasAttr(AttrType.IMMOBILIZED)){
							attrs[AttrType.IMMOBILIZED] = 0;
							B.Add(the_name + " breaks free. ",this);
						}
						B.Add(the_name + " leaps forward swinging his axe! ",this);
						Move(TileInDirection(dir).row,TileInDirection(dir).col);
						Actor a = ActorInDirection(RotateDirection(dir,cw));
						if(a != null){
							B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this);
						}
						a = ActorInDirection(dir);
						if(a != null){
							B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this);
						}
						a = ActorInDirection(RotateDirection(dir,!cw));
						if(a != null){
							B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this);
						}
						Q1();
					}
					else{
						if(ActorInDirection(dir) != null){
							B.Add(the_name + " swings his axe furiously! ",this);
							Actor a = ActorInDirection(RotateDirection(dir,cw));
							if(a != null){
								B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this);
							}
							a = ActorInDirection(dir);
							if(a != null){
								B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this);
							}
							a = ActorInDirection(RotateDirection(dir,!cw));
							if(a != null){
								B.Add(Your() + " axe hits " + a.the_name + ". ",this,a);
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this);
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
			case ActorType.ORC_GRENADIER:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 6){
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
							B.Add("It lands under " + t.actor().the_name + ". ");
						}
					}
					else{
						if(t.inv != null){
							B.Add("It lands under " + t.inv.the_name + ". ");
						}
					}
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
					Q1();
				}
				else{
					if(curhp <= 20){
						if(AI_Step(target,true)){
							B.Add(the_name + " backs away. ",this);
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
				}
				break;
			case ActorType.NECROMANCER:
				if(!HasAttr(AttrType.COOLDOWN_1) && DistanceFrom(target) <= 6){
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
					if(tiles.Count == 0 || attrs[AttrType.COOLDOWN_2] >= 4){
						B.Add("Nothing happens. ",this);
					}
					else{
						attrs[AttrType.COOLDOWN_2]++;
						Tile t = tiles[Global.Roll(tiles.Count)-1];
						B.Add(Prototype(summon).a_name + " digs through the floor! ");
						Create(summon,t.row,t.col);
						M.actor[t.row,t.col].player_visibility_duration = -1;
					}
					Q1();
				}
				else{
					bool blast = false;
					switch(DistanceFrom(target)){
					case 1:
						if(Global.CoinFlip() && AI_Step(target,true)){
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
						target.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(6),this);
						Q1();
					}
				}
				break;
			case ActorType.TROLL: //standard for now
				if(DistanceFrom(target) == 1){
					Attack(0,target);
				}
				else{
					AI_Step(target);
					QS();
				}
				break;
			case ActorType.FIRE_DRAKE:
				if((player.armors.First.Value == ArmorType.FULL_PLATE_OF_RESISTANCE
				|| player.magic_items.Contains(MagicItemType.RING_OF_RESISTANCE))
				&& DistanceFrom(player) <= 12 && CanSee(player)){
					//todo: if player has fire resist upgrades, handle those first, here.
					//B.Add(the_name + " exhales slowly toward you. ",this);
					//pick full plate or ring
					//"The runes melt off of your [item]!" or something similar.
					//
				}
				else{
					if(!HasAttr(AttrType.COOLDOWN_1)){
						if(DistanceFrom(target) <= 12){
							attrs[AttrType.COOLDOWN_1]++;
							int cooldown = (Global.Roll(1,4)+1) * 100;
							Q.Add(new Event(this,cooldown,AttrType.COOLDOWN_1));
							Attack(2,target);
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
			if(path.Count > 0){
				AI_Step(M.tile[path[0].row,path[0].col]);
				if(DistanceFrom(path[0]) == 0){
					path.RemoveAt(0);
				}
				QS();
				return;
			}
			switch(type){
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
				//todo: needs to cast detect monsters
				QS();
				break;
			case ActorType.LASHER_FUNGUS:
				QS();
				break;
			case ActorType.FIRE_DRAKE:
				//todo: needs to hunt player down
				QS();
				break;
			default:
				if(target_location != null){
					if(DistanceFrom(target_location) == 1 && M.actor[target_location.row,target_location.col] != null){
						if(HasAttr(AttrType.IMMOBILIZED)){
							B.Add(You("break") + " free. ",this);
							attrs[AttrType.IMMOBILIZED] = 0;
							QS();
						}
						else{
							if(M.actor[target_location.row,target_location.col].HasAttr(AttrType.IMMOBILIZED)){
								if(HasAttr(AttrType.HUMANOID_INTELLIGENCE) && M.actor[target_location.row,target_location.col].symbol == symbol){
									B.Add(You("break") + M.actor[target_location.row,target_location.col].the_name + " free. ",this,M.actor[target_location.row,target_location.col]);
									M.actor[target_location.row,target_location.col].attrs[AttrType.IMMOBILIZED] = 0;
									QS();
								}
								else{
									QS();
								}
							}
							else{
								Move(target_location.row,target_location.col); //swap places
								target_location = null;
								QS();
							}
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
							QS();
						}
					}
				}
				else{
/*					if(DistanceFrom(target) <= 2){ //if close enough, you can still hear them. or at least that's the idea.
						AI_Step(target);
						QS();
					}*/
					if(DistanceFrom(target) <= 5){
						path = FindPath(target);
						if(path.Count > 0){
							AI_Step(M.tile[path[0].row,path[0].col]);
							if(DistanceFrom(path[0]) == 0){
								path.RemoveAt(0);
							}
						}
						QS();
					}
					else{ //if they're too far away, forget them and end turn.
						target = null;
						QS();
					}
				}
				break;
			}
		}
		public void IdleAI(){ //todo: perhaps some more 'ambient' actions, like zombies moaning if you're close enough to hear
			if(path.Count > 0){
				AI_Step(M.tile[path[0].row,path[0].col]);
				if(DistanceFrom(path[0]) == 0){
					path.RemoveAt(0);
				}
				QS();
				return;
			}
			switch(type){
			case ActorType.LARGE_BAT: //flies around
				QS();
				break;
			case ActorType.ZOMBIE:
				QS();
				break;
			case ActorType.ORC_WARMAGE:
				QS();
				break;
			case ActorType.SHAMBLING_SCARECROW:
				QS();
				break;
			case ActorType.POLTERGEIST:
				QS();
				break;
			case ActorType.SWORDSMAN:
				if(attrs[AttrType.BONUS_COMBAT] > 0){
					attrs[AttrType.BONUS_COMBAT] = 0;
				}
				QS();
				break;
			case ActorType.BANSHEE:
				QS();
				break;
			case ActorType.FIRE_DRAKE:
				QS();
				break;
			default: //simply end turn
				QS();
				break;
			}
		}
		public void CalculateDimming(){
			int dist = 100;
			Actor closest_shadow = null;
			foreach(Actor a in player.ActorsWithinDistance(10,true)){
				if(a.type == ActorType.SHADOW){
					if(a.DistanceFrom(player) < dist){
						dist = a.DistanceFrom(player);
						closest_shadow = a;
					}
				}
			}
			if(closest_shadow == null){
				if(player.HasAttr(AttrType.DIM_LIGHT)){
					player.attrs[AttrType.DIM_LIGHT] = 0;
					if(player.light_radius > 0){
						B.Add("Your light grows brighter. ");
						if(player.HasAttr(AttrType.ENHANCED_TORCH)){
							player.UpdateRadius(player.LightRadius(),12,true);
						}
						else{
							player.UpdateRadius(player.LightRadius(),6,true);
						}
					}
				}
			}
			else{
				Actor sh = closest_shadow; //laziness
				int dimness = 0;
				if(sh.DistanceFrom(player) <= 2){
					dimness = 5;
				}
				else{
					if(sh.DistanceFrom(player) <= 3){
						dimness = 4;
					}
					else{
						if(sh.DistanceFrom(player) <= 5){
							dimness = 3;
						}
						else{
							if(sh.DistanceFrom(player) <= 7){
								dimness = 2;
							}
							else{
								if(sh.DistanceFrom(player) <= 10){
									dimness = 1;
								}
							}
						}
					}
				}
				if(dimness > player.attrs[AttrType.DIM_LIGHT]){
					int difference = dimness - player.attrs[AttrType.DIM_LIGHT];
					player.attrs[AttrType.DIM_LIGHT] = dimness;
					if(player.light_radius > 0){
						if(player.attrs[AttrType.ON_FIRE] < player.light_radius){ //if the player should notice...
							B.Add("Your light grows dimmer. ");
							player.UpdateRadius(player.light_radius,player.light_radius - difference,true);
							if(player.attrs[AttrType.ON_FIRE] > player.light_radius){
								player.UpdateRadius(player.light_radius,player.attrs[AttrType.ON_FIRE]);
							}
						}
					}
				}
				else{
					if(dimness < player.attrs[AttrType.DIM_LIGHT]){
						int difference = dimness - player.attrs[AttrType.DIM_LIGHT];
						player.attrs[AttrType.DIM_LIGHT] = dimness;
						if(player.light_radius > 0){
							if(player.attrs[AttrType.ON_FIRE] < player.light_radius - difference){ //if the player should notice...
								B.Add("Your light grows brighter. ");
								player.UpdateRadius(player.LightRadius(),player.light_radius - difference,true);
							}
						}
					}
				}
			}
		}
		public bool AI_Step(PhysicalObject obj){ return AI_Step(obj,false); }
		public bool AI_Step(PhysicalObject obj,bool flee){
			if(HasAttr(AttrType.IMMOBILIZED)){
				B.Add(You("break") + " free. ",this);
				attrs[AttrType.IMMOBILIZED] = 0;
				return true;
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
			bool cw = Global.CoinFlip();
			dirs.Add(RotateDirection(dirs[0],cw));
			dirs.Add(RotateDirection(dirs[0],!cw)); //building a list of directions to try: first the primary direction,
			cw = Global.CoinFlip(); 				//then the ones next to it, then the ones next to THOSE(in random order)
			dirs.Add(RotateDirection(RotateDirection(dirs[0],cw),cw));
			dirs.Add(RotateDirection(RotateDirection(dirs[0],!cw),!cw)); //this completes the list of 5 directions.
			foreach(int i in dirs){
				if(ActorInDirection(i) != null && ActorInDirection(i).IsHiddenFrom(this)){
					player_visibility_duration = -1;
					target = player; //not extensible yet
					target_location = M.tile[player.row,player.col];
					if(!IsHiddenFrom(player)){
						B.Add(the_name + " walks straight into you! ");
						B.Add(the_name + " looks startled. ");
					}
					else{
						attrs[AttrType.TURNS_VISIBLE] = -1;
						B.Add(a_name + " walks straight into you! ");
						B.Add(the_name + " looks just as surprised as you. ");
					}
					return true;
				}
				if(AI_MoveOrOpen(i)){
					return true;
				}
			}
			return false;
		}
		public int RotateDirection(int dir,bool clockwise){ return RotateDirection(dir,clockwise,1); }
		public int RotateDirection(int dir,bool clockwise,int num){
			for(int i=0;i<num;++i){
				switch(dir){
				case 7:
					dir = clockwise?8:4;
					break;
				case 8:
					dir = clockwise?9:7;
					break;
				case 9:
					dir = clockwise?6:8;
					break;
				case 4:
					dir = clockwise?7:1;
					break;
				case 5:
					break;
				case 6:
					dir = clockwise?3:9;
					break;
				case 1:
					dir = clockwise?4:2;
					break;
				case 2:
					dir = clockwise?1:3;
					break;
				case 3:
					dir = clockwise?2:6;
					break;
				default:
					dir = 0;
					break;
				}
			}
			return dir;
		}
		public bool AI_MoveOrOpen(int dir){
			return AI_MoveOrOpen(TileInDirection(dir).row,TileInDirection(dir).col);
		}
		public bool AI_MoveOrOpen(int r,int c){
			if(M.tile[r,c].passable && M.actor[r,c] == null){
				Move(r,c);
				return true;
			}
			else{
				if(M.tile[r,c].type == TileType.DOOR_C && HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
					M.tile[r,c].Toggle(this);
					return true;
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
		public bool Attack(int attack_idx,Actor a){ //returns true if attack hit
			if(StunnedThisTurn()){
				return false;
			}
			pos pos_of_target = new pos(a.row,a.col);
			AttackInfo info = AttackList.Attack(type,attack_idx);
			info.damage.source = this;
			int plus_to_hit = TotalSkill(SkillType.COMBAT);
			if(this.IsHiddenFrom(a)){ //sneak attacks get +25% accuracy. this usually totals 100% vs. unarmored targets.
				plus_to_hit += 25;
			}
			if(HasAttr(AttrType.BLESSED)){
				plus_to_hit += 10;
			}
			bool hit = a.IsHit(plus_to_hit);
			if(a.HasAttr(AttrType.DEFENSIVE_STANCE) && Global.CoinFlip()){
				hit = false;
			}
			if(attack_idx==0 && (type==ActorType.FROSTLING || type==ActorType.FIRE_DRAKE)){
				hit = true; //hack! these are the 2 'area' attacks that always hit
			}
			string s = info.desc + ". ";
			if(hit){
				if(HasFeat(FeatType.NECK_SNAP) && a.HasAttr(AttrType.MEDIUM_HUMANOID) && IsHiddenFrom(a)){
					B.Add(You("quietly snap") + " " + a.Your() + " neck. ");
					a.TakeDamage(DamageType.NORMAL,9001,this);
					Q1();
					return true;
				}
				int dice = info.damage.dice;
				if(weapons.First.Value != WeaponType.NO_WEAPON){
					dice = Weapon.Damage(weapons.First.Value);
				}
				bool crit = false;
				int pos = s.IndexOf('&');
				if(pos != -1){
					s = s.Substring(0,pos) + the_name + s.Substring(pos+1);
				}
				pos = s.IndexOf('^');
				if(pos != -1){
					string sc = "";
					int critical_target = 20;
					if(weapons.First.Value == WeaponType.DAGGER){
						critical_target = 18;
					}
					if(info.damage.type == DamageType.NORMAL && Global.Roll(1,20) >= critical_target){
						crit = true;
						sc = "critically ";
					}
					s = s.Substring(0,pos) + sc + s.Substring(pos+1);
				}
				pos = s.IndexOf('*');
				if(pos != -1){
					s = s.Substring(0,pos) + a.the_name + s.Substring(pos+1);
				}
				if(this.IsHiddenFrom(a) && crit){
					if(!a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT) 
						&& !a.HasAttr(AttrType.PLANTLIKE) && !a.HasAttr(AttrType.BOSS_MONSTER)){
						if(a.type != ActorType.PLAYER){ //being nice to the player here...
							switch(weapons.First.Value){
							case WeaponType.SWORD:
							case WeaponType.FLAMEBRAND:
								B.Add("You run " + a.the_name + " through! ");
								break;
							case WeaponType.MACE:
							case WeaponType.MACE_OF_FORCE:
								B.Add("You bash " + a.Your() + " head in! ");
								break;
							case WeaponType.DAGGER:
							case WeaponType.VENOMOUS_DAGGER:
								B.Add("You pierce one of " + a.Your() + " vital organs! ");
								break;
							case WeaponType.STAFF:
							case WeaponType.STAFF_OF_MAGIC:
								B.Add("You bring your staff down on " + a.Your() + " head with a loud crack! ");
								break;
							case WeaponType.BOW:
							case WeaponType.HOLY_LONGBOW:
								B.Add("You choke " + a.the_name + " with your bowstring! ");
								break;
							default:
								break;
							}
							a.TakeDamage(DamageType.NORMAL,1337,this);
							Q1();
							return true;
						}
						else{ //...but not too nice
							B.Add(a_name + " strikes from the shadows! ");
							B.Add(Your() + " deadly attack leaves you stunned! ");
							int lotsofdamage = Math.Max(dice*6,a.curhp/2);
							a.attrs[AttrType.STUNNED]++;
							Q.Add(new Event(a,Global.Roll(2,5)*100,AttrType.STUNNED,"You are no longer stunned. "));
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,lotsofdamage,this);
						}
					}
				}
				if(IsHiddenFrom(a)){
					B.Add(You("strike") + " from the shadows! ");
					if(type != ActorType.PLAYER){
						attrs[AttrType.TURNS_VISIBLE] = -1;
					}
					else{
						a.player_visibility_duration = -1;
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
				a.TakeDamage(info.damage.type,info.damage.damclass,dmg,this);
				if(M.actor[r,c] != null){
					if(HasAttr(AttrType.FIRE_HIT) || attrs[AttrType.ON_FIRE] >= 3){ //todo: a frostling's ranged attack shouldn't apply this
						if(!a.HasAttr(AttrType.INVULNERABLE)){ //to prevent the message
							B.Add(a.YouAre() + " burned. ",this,a);
							a.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,Global.Roll(1,6),this);
						}
					}
				}
				if(HasAttr(AttrType.COLD_HIT) && attack_idx==0 && M.actor[r,c] != null){
					//hack: only applies to attack 0
					if(!a.HasAttr(AttrType.INVULNERABLE)){ //to prevent the message
						B.Add(a.YouAre() + " chilled. ",this,a);
						a.TakeDamage(DamageType.COLD,DamageClass.PHYSICAL,Global.Roll(1,6),this);
					}
				}
				if(HasAttr(AttrType.POISON_HIT) && M.actor[r,c] != null){
					B.Add(a.YouAre() + " poisoned. ",this,a);
					a.attrs[AttrType.POISONED]++; //todo: make sure this is how poison works
				}
				if(HasAttr(AttrType.PARALYSIS_HIT) && attack_idx==0 && M.actor[r,c] != null){
					//hack: only applies to attack 0
					B.Add(a.YouAre() + " paralyzed. ",this,a); //todo: update to handle paralyzation resistance.
					a.attrs[AttrType.PARALYZED] = Global.Roll(1,3)+3;
				}
				if(HasAttr(AttrType.FORCE_HIT) && M.actor[r,c] != null){
					//todo: mace of force here
				}
				if(HasAttr(AttrType.DIM_VISION_HIT) && M.actor[r,c] != null){
					if(!a.HasAttr(AttrType.DIM_VISION)){
						string str = "";
						if(a.type == ActorType.PLAYER){
							B.Add("Your vision grows weak. ");
							str = "Your vision returns to normal. ";
						}
						a.attrs[AttrType.DIM_VISION]++;
						Q.Add(new Event(a,DurationOfMagicalEffect((Global.Roll(2,20)+20))*100,AttrType.DIM_VISION,str));
					}
				}
				if(HasAttr(AttrType.STALAGMITE_HIT)){
					List<Tile> tiles = new List<Tile>();
					foreach(Tile tile in M.tile[r,c].TilesWithinDistance(1)){
						if(tile.actor() == null && (tile.type == TileType.FLOOR || tile.type == TileType.STALAGMITE)){
							if(Global.CoinFlip()){ //50% for each...
								tiles.Add(tile);
							}
						}
					}
					foreach(Tile tile in tiles){
						if(tile.type == TileType.STALAGMITE){
							Q.KillEvents(tile,EventType.STALAGMITE);
						}
						else{
							tile.Toggle(this,TileType.STALAGMITE);
						}
					}
					Q.Add(new Event(tiles,150,EventType.STALAGMITE));
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
				if(a.HasAttr(AttrType.DEFENSIVE_STANCE)){
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
								B.Add(You("miss",true) + " " + a.the_name + ". ");
							}
						}
					}
				}
				if(HasAttr(AttrType.DRIVE_BACK_ON)){
					if(!a.HasAttr(AttrType.IMMOBILIZED) && !HasAttr(AttrType.IMMOBILIZED)){
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
			Q.Add(new Event(this,info.cost));
			return hit;
		}
		public void FireArrow(PhysicalObject obj){
			if(StunnedThisTurn()){
				return;
			}
			Tile t = M.tile[obj.row,obj.col];
			int mod = -30; //bows have base accuracy 45%
			if(HasAttr(AttrType.KEEN_EYES)){
				mod = -20; //keen eyes makes it 55%
			}
			mod += TotalSkill(SkillType.COMBAT);
			Actor a = FirstActorInLine(obj);
			if(a != null){
				t = a.tile();
			}
			B.Add(You("fire") + " an arrow. ",this);
			B.DisplayNow();
			Screen.AnimateBoltProjectile(GetBresenhamLine(t.row,t.col),Color.DarkYellow,20);
			if(a != null){
				bool hit = a.IsHit(mod);
				if(a.HasAttr(AttrType.TUMBLING)){
					hit = false;
					a.attrs[AttrType.TUMBLING] = 0;
				}
				if(hit){
					if(Global.Roll(1,20) == 20){
						B.Add("The arrow critically hits " + a.the_name + ". ",this,a);
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,18,this); //max(3d6)
					}
					else{
						B.Add("The arrow hits " + a.the_name + ". ",this,a);
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),this);
					}
				}
				else{
					B.Add("The arrow misses " + a.the_name + ". ",this,a);
				}
			}
			else{
				B.Add("The arrow hits " + t.the_name + ". ",this,t);
			}
			Q1();
		}
		public bool IsHit(int plus_to_hit){
			if(Global.Roll(1,100) + plus_to_hit <= 25){ //base hit chance is 75%
				return false;
			}
			return true;
		}
		public bool TakeDamage(DamageType dmgtype,int dmg,Actor source){
			return TakeDamage(new Damage(dmgtype,DamageClass.NO_TYPE,source,dmg));
		}
		public bool TakeDamage(DamageType dmgtype,DamageClass damclass,int dmg,Actor source){
			return TakeDamage(new Damage(dmgtype,damclass,source,dmg));
		}
		public bool TakeDamage(Damage dmg){ //returns true if still alive
			bool damage_dealt = false;
			if(HasAttr(AttrType.INVULNERABLE)){
				dmg.amount = 0;
			}
			if(HasAttr(AttrType.TOUGH) && dmg.damclass == DamageClass.PHYSICAL){
				dmg.amount -= 3; //test this value
			}
			if(dmg.damclass == DamageClass.MAGICAL){
				dmg.amount -= TotalSkill(SkillType.SPIRIT) / 2;
			}
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
				if(HasAttr(AttrType.IMMUNE_FIRE)){
					dmg.amount = 0;
				}
				int div = 1;
				for(int i=attrs[AttrType.RESIST_FIRE];i>0;--i){
					div = div * 2;
				}
				dmg.amount = dmg.amount / div;
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
					if(type == ActorType.SHAMBLING_SCARECROW && speed != 50){
						speed = 50;
						if(attrs[AttrType.ON_FIRE] >= LightRadius()){
							UpdateRadius(LightRadius(),LightRadius()+1);
						}
						attrs[AttrType.ON_FIRE]++;
						B.Add(the_name + " leaps about as it catches fire! ",this);
					}
				}
				else{
					B.Add(YouAre() + " unburnt. ",this);
				}
				break;
				}
			case DamageType.COLD:
				{
				if(HasAttr(AttrType.IMMUNE_COLD)){
					dmg.amount = 0;
					B.Add(YouAre() + " unharmed. ",this);
				}
				int div = 1;
				for(int i=attrs[AttrType.RESIST_COLD];i>0;--i){
					div = div * 2;
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
				for(int i=attrs[AttrType.RESIST_ELECTRICITY];i>0;--i){
					div = div * 2;
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
				if(HasAttr(AttrType.UNDEAD) || HasAttr(AttrType.CONSTRUCT)){
					dmg.amount = 0;
				}
				if(dmg.amount > 0){
					curhp -= dmg.amount;
					damage_dealt = true;
					if(type == ActorType.PLAYER){
						B.Add("You feel the poison coursing through your veins! ");
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
			if(damage_dealt){
				if(HasAttr(AttrType.MAGICAL_BLOOD)){
					recover_time = Q.turn + 200;
				}
				else{
					recover_time = Q.turn + 500;
				}
				Interrupt();
				if(dmg.source != null){
					if(type != ActorType.PLAYER && dmg.source != this){
						target = dmg.source;
						if(light_radius > 0 && HasLOS(dmg.source.row,dmg.source.col)){//for enemies who can't see in darkness
							target_location = M.tile[dmg.source.row,dmg.source.col];
						}
					}
				}
				if(HasAttr(AttrType.SPORE_BURST) && !HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(Global.Roll(1,5)+1)*100,AttrType.COOLDOWN_1));
					B.Add(You("retaliate") + " with a burst of spores! ",this);
					foreach(Actor a in ActorsWithinDistance(8)){
						if(!a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT) && !a.HasAttr(AttrType.SPORE_BURST)){
							if(HasLOS(a.row,a.col)){
								B.Add("The spores hit " + a.the_name + ". ",a);
								int duration = Global.Roll(2,4);
								a.attrs[AttrType.POISONED] += duration;
								if(name == "you"){
									B.Add("You are poisoned. ");
								}
								if(!a.HasAttr(AttrType.STUNNED)){
									a.attrs[AttrType.STUNNED]++;
									Q.Add(new Event(a,duration*100,AttrType.STUNNED));
									if(name == "you"){
										B.Add("You are stunned. ");
									}
								}
							}
						}
					}
				}
				if(HasAttr(AttrType.HOLY_SHIELDED) && dmg.source != null){
					B.Add(Your() + " holy shield burns " + dmg.source.the_name + ". ",this,dmg.source);
					int amount = Global.Roll(1,6);
					if(amount >= dmg.source.curhp){
						amount = dmg.source.curhp - 1;
					}
					dmg.source.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,amount,this); //doesn't yet prevent loops involving 2 holy shields.
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
						M.Draw();
						B.Add("You die. ");
						B.PrintAll();
						Environment.Exit(0);
					}
				}
				else{
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
					if(HasAttr(AttrType.REGENERATES_FROM_DEATH) && dmg.type != DamageType.FIRE){ //hack - works only with trolls
						B.Add(the_name + " falls to the ground, still twitching. ",this);
						Q.Add(new Event(tile(),200,EventType.REGENERATING_FROM_DEATH,curhp - (Global.Roll(10)+5)));
					}
					else{
						if(dmg.amount < 1000){ //everything that deals this much damage prints its own message.
							if(HasAttr(AttrType.UNDEAD) || HasAttr(AttrType.CONSTRUCT)){
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
					int divisor = 1;
					if(HasAttr(AttrType.SMALL_GROUP)){ divisor = 2; }
					if(HasAttr(AttrType.MEDIUM_GROUP)){ divisor = 3; }
					if(HasAttr(AttrType.LARGE_GROUP)){ divisor = 5; }
					player.GainXP(xp + (level*(10 + level - player.level))/divisor); //experimentally giving the player any
					Q.KillEvents(this,EventType.ANY_EVENT);					// XP that the monster had collected.
					M.RemoveTargets(this);
					M.actor[row,col] = null;
					return false;
				}
			}
			else{
				if(HasFeat(FeatType.FEEL_NO_PAIN) && damage_dealt && curhp < 20){
					B.Add("You feel no pain! ");
					attrs[AttrType.INVULNERABLE]++;
					Q.Add(new Event(this,500,AttrType.INVULNERABLE,"You can feel pain again. "));
				}
				if(magic_items.Contains(MagicItemType.CLOAK_OF_DISAPPEARANCE) && damage_dealt && dmg.amount >= curhp){
					B.PrintAll();
					M.Draw();
					B.DisplayNow("Your cloak starts to fade from sight. Use your cloak to escape?(Y/N): ");
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
										if(a.CanSee(t)){
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
									if(M.tile[rr,rc].passable && M.actor[rr,rc] == null && DistanceFrom(rr,rc) >= 4){
										Move(rr,rc);
									}
								}
							}
							B.Add("You escape. ");
							break;
						default:
							break;
						}
					}
					magic_items.Remove(MagicItemType.CLOAK_OF_DISAPPEARANCE);
				}
			}
			return true;
		}
		public bool GetKnockedBack(PhysicalObject obj){ return GetKnockedBack(obj.GetExtendedBresenhamLine(row,col)); }
		public bool GetKnockedBack(List<Tile> line){
			int idx = line.IndexOf(M.tile[row,col]);
			if(idx == -1){
				B.Add("DEBUG: Error - " + the_name + "'s position doesn't seem to be in the line. ");
				return false;
			}
			Tile next = line[idx+1];
			Actor source = M.actor[line[0].row,line[0].col];
			if(next.passable && M.actor[next.row,next.col] == null){
				B.Add(YouAre() + " knocked back. ",this);
				if(HasAttr(AttrType.IMMOBILIZED)){
					attrs[AttrType.IMMOBILIZED] = 0;
					B.Add(YouAre() + " no longer immobilized. ",this);
				}
				Move(next.row,next.col);
			}
			else{
				int r = row;
				int c = col;
				bool immobilized = HasAttr(AttrType.IMMOBILIZED);
				if(!next.passable){
					B.Add(YouAre() + " knocked into " + next.the_name + ". ",this,next);
					TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),source);
				}
				else{
					B.Add(YouAre() + " knocked into " + M.actor[next.row,next.col].the_name + ". ",this,M.actor[next.row,next.col]);
					TakeDamage(DamageType.NORMAL,Global.Roll(1,6),source);
					M.actor[next.row,next.col].TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),source);
				}
				if(immobilized && M.actor[r,c] != null){
					B.Add(YouAre() + " no longer immobilized. ",this);
				}
			}
			return true;
		}
		public bool CastSpell(SpellType spell){ return CastSpell(spell,null); }
		public bool CastSpell(SpellType spell,PhysicalObject obj){ //returns false if targeting is canceled.
			if(StunnedThisTurn()){ //todo: eventually this will be moved to the last possible second
				return true; //returns true because turn was used up. 
			}
			//if(!HasSpell(spell)){
		//		return false;
		//	} todo uncomment this
			Tile t = null;
			if(obj != null){
				t = M.tile[obj.row,obj.col];
			}
			int bonus = 0; //used for bonus damage on spells - currently, only Master's Edge adds bonus damage.
			if(FailRate(spell) > 0){
				if(Global.Roll(1,100) - FailRate(spell) <= 0){
					if(HasFeat(FeatType.STUDENTS_LUCK) && !HasAttr(AttrType.STUDENTS_LUCK_USED)){
						attrs[AttrType.STUDENTS_LUCK_USED]++;
						if(Global.Roll(1,100) - FailRate(spell) <= 0){
							B.Add("Sparks fly from " + Your() + " fingers. ",this);
							Q1();
							return true;
						}
						else{
							B.Add("Your luck pays off. ");
						}
					}
					else{
						B.Add("Sparks fly from " + Your() + " fingers. ",this); //or 'you fail to concentrate hard enough'
						Q1(); //or 'the shaman's mouth and fingers move, but nothing happens'
						return true; //or 'the shaman seems to concentrate hard, but nothing happens'
					}
				}
			}
			else{
				if(HasFeat(FeatType.MASTERS_EDGE)){
					bonus = 1;
				}
			}
			switch(spell){
			case SpellType.SHINE:
				if(!HasAttr(AttrType.ENHANCED_TORCH)){
					B.Add("You cast shine. ");
					B.Add("Your torch begins to shine brightly. ");
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
			case SpellType.MAGIC_MISSILE:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " magic missile. ",this);
					AnimateBoltProjectile(t,Color.Magenta);
					Actor a = FirstActorInLine(t);
					if(a != null){
						B.Add("The missile hits " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(1+bonus,6),this);
					}
					else{
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
					B.Add("You cast detect monsters. ");
					B.Add("You can sense beings around you. ");
					attrs[AttrType.DETECTING_MONSTERS]++;
					Q.Add(new Event(this,2100,AttrType.DETECTING_MONSTERS,"You can no longer sense beings around you. "));
				}
				else{
					B.Add("You are already detecting monsters! ");
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
					AnimateMapCell(t,Color.Cyan,'*');
					if(a != null){
						B.Add(You("strike") + " " + a.the_name + ". ",this,a);
						string s = a.the_name;
						List<Tile> line = GetExtendedBresenhamLine(a.row,a.col);
						int idx = line.IndexOf(M.tile[a.row,a.col]);
						Tile next = line[idx+1];
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(1+bonus,6),this);
						if(Global.Roll(1,10) <= 6){
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
										M.actor[next.row,next.col].TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),this);
									}
								}
							}
						}
					}
					else{
						if(t.passable){
							B.Add("You strike empty space. ");
						}
						else{
							B.Add("You strike " + t.the_name + " with your palm. ");
							if(t.type == TileType.DOOR_C){ //heh, why not?
								B.Add("It flies open! ");
								t.Toggle(this);
							}
						}
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.BLINK:
				if(HasAttr(AttrType.IMMOBILIZED)){
					B.Add("You can't cast this spell while immobilized. ");
					return false; //todo: this will break if a mob casts blink while immobilized. =(
				}
				else{
					for(int i=0;i<9999;++i){
						int a = Global.Roll(1,17) - 9; //-8 to 8
						int b = Global.Roll(1,17) - 9;
						if(Math.Abs(a) + Math.Abs(b) >= 6){
							a += row;
							b += col;
							if(M.BoundsCheck(a,b) && M.tile[a,b].passable && M.actor[a,b] == null){
								B.Add(You("cast") + " blink. ",this);
								B.Add(You("step") + " through a rip in reality. ",this);
								Move(a,b);
								break;
							}
						}
					}
				}
				break;
			case SpellType.IMMOLATE:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " immolate. ",this);
					AnimateMapCell(t,Color.RandomFire,'*'); //eventually i want this to use higher
					Actor a = M.actor[t.row,t.col];	//ascii for this to get a 'growing flame' effect.
					if(a != null){
						if(!a.HasAttr(AttrType.RESIST_FIRE)){
							if(a.name == "you"){
								B.Add("You start to catch fire! ");
							}
							else{
								B.Add(a.the_name + " starts to catch fire. ",a);
							}
							a.attrs[AttrType.CATCHING_FIRE]++;
						}
						else{
							if(a.name == "you"){
								B.Add("You shrug off the flames. ");
							}
							else{
								B.Add(a.the_name + " fails to ignite. ",a);
							}
						}
					}
					else{
						B.Add(You("throw") + " flames. ",this);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.ICY_BLAST:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " icy blast. ",this);
					AnimateProjectile(t,Color.RandomIce,'*');
					Actor a = FirstActorInLine(t);
					if(a != null){
						B.Add("The icy blast hits " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,Global.Roll(2+bonus,6),this);
					}
					else{
						B.Add("The icy blast hits " + t.the_name + ". ",t);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.BURNING_HANDS:
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
			case SpellType.FREEZE:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " freeze. ",this);
					AnimateMapCell(t,Color.DarkBlue,'#');
					Actor a = M.actor[t.row,t.col];
					if(a != null){
						B.Add("Ice forms around " + a.the_name + ". ",a);
						int r = a.row;
						int c = a.col;
						a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,Global.Roll(1+bonus,6),this);
						if(M.actor[r,c] != null && !a.HasAttr(AttrType.IMMOBILIZED) && Global.Roll(1,10) <= 6){
							B.Add(a.YouAre() + " immobilized. ",a);
							a.attrs[AttrType.IMMOBILIZED]++;
							int duration = DurationOfMagicalEffect(Global.Roll(1,3)) * 100;
							Q.Add(new Event(a,duration,AttrType.IMMOBILIZED,a.YouAre() + " no longer immobilized. ",a));
						}
					}
					else{
						B.Add("Some ice forms on " + t.the_name + ". ",t);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.SONIC_BOOM:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " sonic boom. ",this);
					AnimateProjectile(t,Color.Yellow,'~');
					Actor a = FirstActorInLine(t);
					if(a != null){
						B.Add("A wave of sound hits " + a.the_name + ". ",a);
						int r = a.row;
						int c = a.col;
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(2+bonus,6),this);
						if(Global.Roll(1,10) <= 5 && M.actor[r,c] != null && !M.actor[r,c].HasAttr(AttrType.STUNNED)){
							B.Add(a.YouAre() + " stunned. ",a);
							a.attrs[AttrType.STUNNED]++;
							int duration = DurationOfMagicalEffect((Global.Roll(1,4)+2)) * 100;
							Q.Add(new Event(a,duration,AttrType.STUNNED,the_name + " is no longer stunned. ",a));
						}
					}
					else{
						B.Add("Sonic boom! ");
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.ARC_LIGHTNING:
				{
				List<Actor> targets = new List<Actor>();
				for(int i=row-1;i<=row+1;++i){
					for(int j=col-1;j<=col+1;++j){
						if(M.actor[i,j] != null && M.actor[i,j] != this){
							targets.Add(M.actor[i,j]);
						}
					}
				}
				B.Add(You("cast") + " arc lightning. ",this);
				AnimateExplosion(this,1,Color.RandomLightning,'*');
				if(targets.Count == 0){
					B.Add("The air around " + the_name + " crackles. ",this);
				}
				else{
					while(targets.Count > 0){
						int idx = Global.Roll(1,targets.Count) - 1;
						Actor a = targets[idx];
						targets.Remove(a);
						B.Add("Electricity arcs to " + a.the_name + ". ",this,a);
						a.TakeDamage(DamageType.ELECTRIC,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this);
					}
				}
				break;
				}
			case SpellType.PLACEHOLDER:
				B.Add("BOOOOOOOOOOOOOOOOOOOOOOOOOOOM! ");
				break;
			case SpellType.SHOCK:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " shock. ",this);
					Actor a = FirstActorInLine(t);
					if(a != null){
						AnimateBoltProjectile(t,Color.RandomLightning);
						B.Add("Electricity leaps to " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.ELECTRIC,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this);
					}
					else{
						AnimateMapCell(this,Color.RandomLightning,'*');
						B.Add("Electricity arcs between your fingers. ");
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.SHADOWSIGHT:
				B.Add("You cast shadowsight. ");
				B.Add("Your eyes pierce the darkness. ");
				if(!HasAttr(AttrType.DARKVISION)){
					attrs[AttrType.DARKVISION]++;
					int duration = (Global.Roll(2,4)+15) * 100;
					Q.Add(new Event(this,duration,AttrType.DARKVISION,"Your darkvision wears off. "));
				}
				break;
			case SpellType.RETREAT: //this is a player-only spell for now because it uses target_location to track position
				B.Add("You cast retreat. ");
				if(target_location == null){
					target_location = M.tile[row,col];
					B.Add("You create a rune of transport on " + M.tile[row,col].the_name + ". ");
				}
				else{
					if(M.actor[target_location.row,target_location.col] == null && target_location.passable){
						B.Add("You activate your rune of transport. ");
						Move(target_location.row,target_location.col);
						if(HasAttr(AttrType.IMMOBILIZED)){
							attrs[AttrType.IMMOBILIZED] = 0;
							B.Add("You are no longer immobilized. ");
						}
					}
					else{
						B.Add("Something blocks your transport. ");
					}
				}
				break;
			case SpellType.FIREBALL:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					Actor a = FirstActorInLine(t);
					if(a != null){
						t = M.tile[a.row,a.col];
					}
					B.Add(You("cast") + " fireball. ",this);
					AnimateBoltProjectile(t,Color.Red);
					Screen.AnimateExplosion(t,2,new colorchar(Color.RandomFire,'*'));
					B.Add("Fwoosh! ",this,t);
					List<Actor> targets = new List<Actor>();
					foreach(Actor ac in t.ActorsWithinDistance(2)){
						if(ac != this){
							targets.Add(ac);
						}
					}
					while(targets.Count > 0){
						int idx = Global.Roll(1,targets.Count) - 1;
						Actor ac = targets[idx];
						targets.Remove(ac);
						B.Add("The explosion hits " + ac.the_name + ". ",ac);
						ac.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this);
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.PASSAGE:
				{
				if(HasAttr(AttrType.IMMOBILIZED)){
					B.Add("You can't cast this spell while immobilized. ");
					return false;
				}
				int i = DirectionOfOnly(TileType.WALL,true);
				if(i == 0){
					B.Add("There's no wall here. ");
					return false;
				}
				else{
					if(i == -1){
						i = GetDirection(true,false);
					}
					t = TileInDirection(i);
					if(t != null){
						if(t.type == TileType.WALL){
							B.Add("You cast passage. ");
							Console.CursorVisible = false;
							colorchar ch = new colorchar(Color.Cyan,'!');
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
							List<Tile> tiles = new List<Tile>();
							List<colorchar> memlist = new List<colorchar>();
							while(!t.passable){
								if(t.row == 0 || t.row == ROWS-1 || t.col == 0 || t.col == COLS-1){
									break;
								}
								tiles.Add(t);
								memlist.Add(Screen.MapChar(t.row,t.col));
								Screen.WriteMapChar(t.row,t.col,ch);
								Thread.Sleep(35);
								t = t.TileInDirection(i);
							}
							if(t.passable && M.actor[t.row,t.col] == null){
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
								B.Add("You travel through the passage. ");
								Move(t.row,t.col);
							}
							else{
								int j = 0;
								foreach(Tile tile in tiles){
									Screen.WriteMapChar(tile.row,tile.col,memlist[j++]);
									Thread.Sleep(35);
								}
								B.Add("The passage is blocked. ");
							}
						}
						else{
							B.Add("There's no wall here. ");
							return false;
						}
					}
					else{
						return false;
					}
				}
				break;
				}
			case SpellType.FORCE_BEAM:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " force beam. ",this);
					B.DisplayNow();
					List<Tile> line = GetExtendedBresenhamLine(t.row,t.col);
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
								int idx = line.IndexOf(tile);
								firsttile = tile;
								firstactor = M.actor[tile.row,tile.col];
								nexttile = line[idx+1];
								nextactor = M.actor[nexttile.row,nexttile.col];
								break;
							}
						}
						Screen.AnimateBoltBeam(GetBresenhamLine(firsttile.row,firsttile.col),Color.Cyan);
						if(firstactor != null){
							string s = firstactor.the_name;
							firstactor.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(1+bonus,6),this);
							if(Global.Roll(1,10) <= 9){
								if(M.actor[firsttile.row,firsttile.col] != null){
									firstactor.GetKnockedBack(line);
								}
								else{
									if(!nexttile.passable){
										B.Add(s + "'s corpse is knocked into " + nexttile.the_name + ". ",firsttile,nexttile);
									}
									else{
										if(nextactor != null){
											B.Add(s + "'s corpse is knocked into " + nextactor.the_name + ". ",firsttile,nextactor);
											nextactor.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),this);
										}
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
			case SpellType.DISINTEGRATE:
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					B.Add(You("cast") + " disintegrate. ",this);
					AnimateBoltBeam(t,Color.DarkGreen);
					Actor a = FirstActorInLine(t);
					if(a != null){
						B.Add(You("direct") + " destructive energies toward " + a.the_name + ". ",this,a);
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(8+bonus,6),this);
					}
					else{
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
				break;
			case SpellType.BLIZZARD:
				{
				List<Actor> targets = new List<Actor>();
				for(int i=row-5;i<=row+5;++i){
					for(int j=col-5;j<=col+5;++j){
						if(M.BoundsCheck(i,j) && M.actor[i,j] != null && M.actor[i,j] != this){
							targets.Add(M.actor[i,j]);
						}
					}
				}
				B.Add(You("cast") + " blizzard. ",this);
				AnimateExplosion(this,5,Color.RandomIce,'*');
				B.Add("A massive ice storm surrounds " + the_name + ". ",this);
				while(targets.Count > 0){
					int idx = Global.Roll(1,targets.Count) - 1;
					Actor a = targets[idx];
					targets.Remove(a);
					B.Add("The blizzard hits " + a.the_name + ". ",a);
					int r = a.row;
					int c = a.col;
					a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,Global.Roll(5+bonus,6),this);
					if(M.actor[r,c] != null && Global.Roll(1,10) <= 8){
						B.Add(a.the_name + " is immobilized. ",a);
						a.attrs[AttrType.IMMOBILIZED]++;
						int duration = DurationOfMagicalEffect(Global.Roll(1,3)) * 100;
						Q.Add(new Event(a,duration,AttrType.IMMOBILIZED,a.the_name + " is no longer immobilized. ",a));
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
				TakeDamage(DamageType.HEAL,Global.Roll(4,6),null);
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
			spells[spell]++;
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
			int failrate = Spell.Level(spell) - TotalSkill(SkillType.MAGIC)*5;
			if(failrate < 0){
				failrate = 0;
			}
			failrate *= spells[spell];
			failrate += attrs[AttrType.GLOBAL_FAIL_RATE]*25;
			if(!HasFeat(FeatType.ARMORED_MAGE)){
				failrate += Armor.AddedFailRate(armors.First.Value);
			}
			return failrate;
		}
		public void ResetSpells(){
			foreach(SpellType s in Enum.GetValues(typeof(SpellType))){
				if(spells[s] > 1){
					spells[s] = 1;
				}
			}
		}
		public void ResetForNewLevel(){
			if(Global.Option(OptionType.WIZLIGHT_CAST)){
				Global.Options[OptionType.WIZLIGHT_CAST] = false;
			}
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
		}
		public bool UseFeat(FeatType feat){
			switch(feat){
			case FeatType.SPIN_ATTACK:
				{
				B.Add("You perform a spin attack. ");
				int dice = Weapon.Damage(weapons.First.Value);
				foreach(Tile t in TilesAtDistance(1)){
					if(t.actor() != null){
						Actor a = t.actor();
						B.Add("You hit " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(dice,6),this);
					}
				}
				foreach(Tile t in TilesAtDistance(2)){
					if(t.actor() != null){
						Actor a = t.actor();
						B.Add("Your magically charged attack hits " + a.the_name + ". ",a);
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,TotalSkill(SkillType.MAGIC),this);
					}
				}
				break;
				}
			case FeatType.LUNGE:
				{
				if(HasAttr(AttrType.IMMOBILIZED)){
					B.Add("You can't perform this feat while immobilized. ");
					return false;
				}
				Tile t = GetTarget(2);
				if(t != null && t.actor() != null){
					bool moved = false;
					foreach(Tile neighbor in t.NeighborsBetween(row,col)){
						if(neighbor.passable && neighbor.actor() == null){
							Move(neighbor.row,neighbor.col);
							moved = true;
							B.Add("You lunge! ");
							attrs[AttrType.BONUS_COMBAT] += 3;
							Attack(0,t.actor());
							attrs[AttrType.BONUS_COMBAT] -= 3;
							break;
						}
					}
					if(!moved){
						B.Add("The way is blocked! ");
						return false;
					}
				}
				else{
					return false;
				}
				break;
				}
			case FeatType.DRIVE_BACK:
				if(HasAttr(AttrType.DRIVE_BACK_ON)){
					attrs[AttrType.DRIVE_BACK_ON] = 0;
					B.Add("You're no longer using your Drive back feat. ");
				}
				else{
					attrs[AttrType.DRIVE_BACK_ON] = 1;
					B.Add("You're now using your Drive back feat. ");
				}
				Q0();
				return true;
			case FeatType.TUMBLE:
				//todo:
/*Tumble - (A, 200 energy) - You pick a tile within distance 2. If there is at least one passable tile between 
you and it(you CAN tumble past actors), you move to that tile. Additional effects: If you move past an actor, 
they lose sight of you and their turns_target_location is set to X - rand_function_of(stealth skill). (there's a good chance
they'll find you, then attack, but you will have still moved past them) ; You will automatically dodge the first arrow
that would hit you before your next turn.(it's still possible they'll roll 2 successes and hit you) ; Has the same
effect as standing still, if you're on fire or catching fire. */
				{
				if(HasAttr(AttrType.IMMOBILIZED)){
					B.Add("You can't perform this feat while immobilized. ");
					return false;
				}
				Tile t = GetTarget(2);
				if(t != null && t.actor() != null){
					List<Actor> actors_moved_past = new List<Actor>();
					bool moved = false;
					foreach(Tile neighbor in t.NeighborsBetween(row,col)){
						if(neighbor.actor() != null){
							actors_moved_past.Add(neighbor.actor());
						}
						if(neighbor.passable && !moved){
							Move(t.row,t.col);
							moved = true;
							attrs[AttrType.TUMBLING]++;
							B.Add("You tumble. ");
							if(HasAttr(AttrType.CATCHING_FIRE)){ //copy&paste happened here:
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
										B.Add("You put out some of the fire. ");
									}
									else{
										B.Add("You put out the fire. ");
									}
								}
							}
						}
					}
					if(moved){
						foreach(Actor a in actors_moved_past){
							//todo: stealthy stuff here
						}
					}
					else{
						B.Add("The way is blocked! ");
						return false;
					}
				}
				else{
					return false;
				}
				break;
				}
			case FeatType.ARCANE_HEALING: //25% fail rate for the 'failrate' feats
				if(attrs[AttrType.GLOBAL_FAIL_RATE] < 4){
					if(curhp < maxhp){
						attrs[AttrType.GLOBAL_FAIL_RATE]++;
						B.Add("You drain your magic reserves. ");
						int amount = Global.Roll(TotalSkill(SkillType.MAGIC)/2,6) + 25;
						TakeDamage(DamageType.HEAL,amount,null);
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
					}
				}
				else{
					B.Add("Your magic reserves are empty! ");
					return false;
				}
				break;
			case FeatType.FORCE_OF_WILL:
				if(attrs[AttrType.GLOBAL_FAIL_RATE] < 4){
					//todo:selectspell and whatnot
				}
				else{
					B.Add("Your magic reserves are empty! ");
					return false;
				}
				break;
			case FeatType.WAR_SHOUT:
				if(!HasAttr(AttrType.WAR_SHOUTED)){
					B.Add("You bellow a challenge! ");
					attrs[AttrType.WAR_SHOUTED]++;
					attrs[AttrType.BONUS_COMBAT] += 5;
					attrs[AttrType.BONUS_SPIRIT] += 5;
					int duration = (Global.Roll(1,4)+6) * 100;
					Q.Add(new Event(this,duration,AttrType.BONUS_COMBAT,5));
					Q.Add(new Event(this,duration,AttrType.BONUS_SPIRIT,5));
					Q.Add(new Event(this,duration,AttrType.WAR_SHOUTED,"Your morale returns to normal. "));
					foreach(Actor a in M.AllActors()){ //or ActorsWithinDistance?
					}
				}
				else{
					B.Add("You're still pumped up! YEAH! ");
					return false;
				}
				break;
			case FeatType.FOCUSED_RAGE:
				//todo: rename? need a feat here.
				break;
			case FeatType.DISARM_TRAP:
				//todo: traps, yeah..
				B.Add("You disarm all traps. Forever. ");
				break;
			case FeatType.DANGER_SENSE:
				if(HasAttr(AttrType.DANGER_SENSE_ON)){
					attrs[AttrType.DANGER_SENSE_ON] = 0;
					B.Add("You're no longer using your Danger sense feat. ");
				}
				else{
					attrs[AttrType.DANGER_SENSE_ON] = 1;
					B.Add("You're now using your Danger sense feat. ");
				}
				Q0();
				return true;
			default:
				return false;
			}
			Q1();
			return true;
		}
		public void Interrupt(){
			attrs[AttrType.RESTING] = 0;
			attrs[AttrType.RUNNING] = 0;
		}
		public bool StunnedThisTurn(){
			if(HasAttr(AttrType.STUNNED) && Global.Roll(1,5) == 5){
				int dir = Global.RandomDirection();
				if(HasAttr(AttrType.IMMOBILIZED)){
					B.Add(You("almost fall") + " over. ",this);
				}
				else{
					if(!TileInDirection(dir).passable){
						B.Add(You("stagger") + " into " + TileInDirection(dir).the_name + ". ",this);
					}
					else{
						if(ActorInDirection(dir) != null){
							B.Add(You("stagger") + " into " + ActorInDirection(dir).the_name + ". ",this);
						}
						else{
							Move(TileInDirection(dir).row,TileInDirection(dir).col);
							B.Add(You("stagger") + ". ",this);
						}
					}
				}
				QS();
				return true;
			}
			return false;
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
				attrs[AttrType.BONUS_STEALTH] += 2; //todo: balance check
				break;
			case ArmorType.CHAINMAIL_OF_ARCANA:
				attrs[AttrType.BONUS_MAGIC]++;
				break;
			case ArmorType.FULL_PLATE_OF_RESISTANCE:
				attrs[AttrType.RESIST_FIRE]++;
				attrs[AttrType.RESIST_COLD]++;
				attrs[AttrType.RESIST_ELECTRICITY]++;
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
				break;
			}
		}
		public void DisplayStats(){ DisplayStats(false,false); }
		public void DisplayStats(bool expand_weapons,bool expand_armors){
			//color coded HP
			//level and xp
			//ac
			//weapon
			//armor
			//magic items
			//space for status effects
			//(f-key spells?)
			Console.CursorVisible = false;
			Screen.WriteStatsString(2,0,"HP: " + curhp + "  ");
			Screen.WriteStatsString(3,0,"Level: " + level + "  ");
			Screen.WriteStatsString(4,0,"XP: " + xp + "  ");
			Screen.WriteStatsString(5,0,"AC: " + ArmorClass() + "  ");
			int weapon_lines = 1;
			int armor_lines = 1;
			int magic_item_lines = magic_items.Count;
			colorstring cs = Weapon.StatsName(weapons.First.Value);
			cs.s = ("" + cs.s).PadRight(12); //todo: the W: won't actually fit. well, the A: won't, with full plate.
			Screen.WriteStatsString(6,0,cs);
			if(expand_weapons){ //this can easily be extended to handle a variable number of weapons
				weapon_lines = 5;
				int i = 7;
				foreach(WeaponType w in weapons){
					if(w != weapons.First.Value){
						cs = Weapon.StatsName(w);
						cs.s = ("" + cs.s).PadRight(12);
						Screen.WriteStatsString(i,0,cs);
						++i;
					}
				}
				
			}
			cs = Armor.StatsName(armors.First.Value);
			cs.s = ("" + cs.s).PadRight(12); //does not fit, todo, augh.
			Screen.WriteStatsString(6+weapon_lines,0,cs);
			if(expand_armors){
				armor_lines = 3;
				int i = 7 + weapon_lines;
				foreach(ArmorType a in armors){
					if(a != armors.First.Value){
						cs = Armor.StatsName(a);
						cs.s = ("" + cs.s).PadRight(12);
						Screen.WriteStatsString(i,0,cs);
						++i;
					}
				}
			}
			Screen.WriteStatsString(6+weapon_lines+armor_lines,0,"~~~~~~~~~~~~"); //todo: magic items somewhere in here
			for(int i=7+weapon_lines+armor_lines;i<11+weapon_lines+armor_lines;++i){
				Screen.WriteStatsString(i,0,"".PadRight(12));
			}
			Screen.ResetColors();
		}
		public void GainXP(int num){
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
			List<FeatType> new_feats = null;
			switch(level){
			case 0:
				if(xp >= 0){
					new_feats = LevelUp();
				}
				break;
			case 1:
				if(xp >= 100){
					new_feats = LevelUp();
				}
				break;
			case 2:
				if(xp >= 320){
					new_feats = LevelUp();
				}
				break;
			case 3:
				if(xp >= 680){
					new_feats = LevelUp();
				}
				break;
			case 4:
				if(xp >= 1160){
					new_feats = LevelUp();
				}
				break;
			case 5:
				if(xp >= 1810){
					new_feats = LevelUp();
				}
				break;
			case 6:
				if(xp >= 2650){
					new_feats = LevelUp();
				}
				break;
			case 7:
				if(xp >= 3630){
					new_feats = LevelUp();
				}
				break;
			case 8:
				if(xp >= 4830){
					new_feats = LevelUp();
				}
				break;
			case 9:
				if(xp >= 6270){
					new_feats = LevelUp();
				}
				break;
			}
			if(new_feats != null){
				foreach(FeatType feat in new_feats){
					B.Add("You learn the " + Feat.Name(feat) + " feat. ");
				}
			}
		}
		public List<FeatType> LevelUp(){
			List<FeatType> completed_feats = new List<FeatType>();
			++level;
			if(level == 1){
				B.Add("Welcome, adventurer! ");
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
					Screen.WriteMapString(1+i*4,22,new colorstring(levelcolor,("Level " + skill_level).PadRight(70)));
					FeatType ft = Feat.OfSkill(sk,0);
					Color featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					int feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(2+i*4,0,new colorstring(featcolor,("    " + Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(35)));
					ft = Feat.OfSkill(sk,1);
					featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(2+i*4,35,new colorstring(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
					ft = Feat.OfSkill(sk,2);
					featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(3+i*4,0,new colorstring(featcolor,("    " + Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(35)));
					ft = Feat.OfSkill(sk,3);
					featcolor = feats_increased.Contains(ft)? Color.Green : Color.Gray;
					feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
					if(HasFeat(ft)){ featcolor = Color.Magenta; feat_level = Feat.MaxRank(ft); }
					Screen.WriteMapString(3+i*4,35,new colorstring(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
					Screen.WriteMapString(4+i*4,0,"".PadRight(COLS));
				}
				if(skills_increased.Count == 3){
					Screen.WriteMapString(21,0,"--Type [a-e] to choose a skill--[?] for help--[Enter] to accept---");
					Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
					Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'e'));
					Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
					Screen.WriteMapString(21,47,new colorstring(Color.Magenta,"Enter"));
				}
				else{
					Screen.WriteMapString(21,0,"--Type [a-e] to choose a skill--[?] for help----------------------");
					Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
					Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'e'));
					Screen.WriteMapChar(21,33,new colorchar(Color.Cyan,'?'));
				}
				Console.SetCursorPosition(37+Global.MAP_OFFSET_COLS,1);
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
								Screen.WriteMapString(1+i*4,0,new colorstring(graycolor,("    " + Skill.Name(sk)).PadRight(22)));
								Color levelcolor = skills_increased.Contains(sk)? greencolor : graycolor;
								int skill_level = skills_increased.Contains(sk)? skills[sk] + 1 : skills[sk];
								Screen.WriteMapString(1+i*4,22,new colorstring(levelcolor,("Level " + skill_level).PadRight(70)));
								FeatType ft = Feat.OfSkill(sk,0);
								Color featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								int feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(2+i*4,4,new colorstring(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(31)));
								ft = Feat.OfSkill(sk,1);
								featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(2+i*4,35,new colorstring(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
								ft = Feat.OfSkill(sk,2);
								featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(3+i*4,4,new colorstring(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(31)));
								ft = Feat.OfSkill(sk,3);
								featcolor = feats_increased.Contains(ft)? greencolor : graycolor;
								feat_level = feats_increased.Contains(ft)? (-feats[ft]) + 1 : (-feats[ft]);
								if(HasFeat(ft)){ featcolor = magentacolor; feat_level = Feat.MaxRank(ft); }
								Screen.WriteMapString(3+i*4,35,new colorstring(featcolor,(Feat.Name(ft) + " (" + feat_level + "/" + Feat.MaxRank(ft) + ")").PadRight(70)));
								Screen.WriteMapString(4+i*4,0,"".PadRight(COLS));
							}
							Screen.WriteMapString(2+4*(int)chosen_skill,0,"[a]");
							Screen.WriteMapString(2+4*(int)chosen_skill,31,"[b]");
							Screen.WriteMapString(3+4*(int)chosen_skill,0,"[c]");
							Screen.WriteMapString(3+4*(int)chosen_skill,31,"[d]");
							if(feats[Feat.OfSkill(chosen_skill,0)] == Feat.MaxRank(Feat.OfSkill(chosen_skill,0))){
								Screen.WriteMapChar(2+4*(int)chosen_skill,1,new colorchar(Color.DarkRed,'a'));
							}
							else{
								Screen.WriteMapChar(2+4*(int)chosen_skill,1,new colorchar(Color.Cyan,'a'));
							}
							if(feats[Feat.OfSkill(chosen_skill,1)] == Feat.MaxRank(Feat.OfSkill(chosen_skill,1))){
								Screen.WriteMapChar(2+4*(int)chosen_skill,32,new colorchar(Color.DarkRed,'b'));
							}
							else{
								Screen.WriteMapChar(2+4*(int)chosen_skill,32,new colorchar(Color.Cyan,'b'));
							}
							if(feats[Feat.OfSkill(chosen_skill,2)] == Feat.MaxRank(Feat.OfSkill(chosen_skill,2))){
								Screen.WriteMapChar(3+4*(int)chosen_skill,1,new colorchar(Color.DarkRed,'c'));
							}
							else{
								Screen.WriteMapChar(3+4*(int)chosen_skill,1,new colorchar(Color.Cyan,'c'));
							}
							if(feats[Feat.OfSkill(chosen_skill,3)] == Feat.MaxRank(Feat.OfSkill(chosen_skill,3))){
								Screen.WriteMapChar(3+4*(int)chosen_skill,32,new colorchar(Color.DarkRed,'d'));
							}
							else{
								Screen.WriteMapChar(3+4*(int)chosen_skill,32,new colorchar(Color.Cyan,'d'));
							}
							Screen.WriteMapString(21,0,"--Type [a-d] to choose a feat---[?] for help----------------------");
							Screen.WriteMapChar(21,8,new colorchar(Color.Cyan,'a'));
							Screen.WriteMapChar(21,10,new colorchar(Color.Cyan,'d'));
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
			}
			foreach(FeatType feat in feats_increased){
				feats[feat]--; //negative values are used until you've completely learned a feat
				if(feats[feat] == -(Feat.MaxRank(feat))){
					feats[feat] = 1;
					completed_feats.Add(feat);
				}
			}
			if(skills_increased.Contains(SkillType.MAGIC)){
				List<SpellType> unknown = new List<SpellType>();
				List<string> unknownstr = new List<string>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(!HasSpell(spell) && spell != SpellType.BLESS && spell != SpellType.MINOR_HEAL
					&& spell != SpellType.HOLY_SHIELD && spell != SpellType.NO_SPELL && spell != SpellType.NUM_SPELLS){
						unknown.Add(spell);
						unknownstr.Add(Spell.Name(spell));
					}
				}
				for(int i=unknown.Count+2;i<ROWS;++i){
					Screen.WriteMapString(i,0,"".PadRight(COLS));
				}
				int selection = Select("Learn which spell? ",unknownstr,false,true);
				spells[unknown[selection]] = 1;
			}
			return completed_feats;
		}
		public bool CanSee(int r,int c){ return CanSee(M.tile[r,c]); }
		public bool CanSee(PhysicalObject o){
			Actor a = o as Actor;
			if(a != null){
				if(HasAttr(AttrType.DETECTING_MONSTERS) && DistanceFrom(a) <= 6){
					return true;
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
		public bool HasLOS(int r,int c){
			int y1 = row;
			int x1 = col;
			int y2 = r;
			int x2 = c;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			if(dx<=1 && dy<=1){ //everything adjacent
				return true;
			}
			if(HasBresenhamLine(r,c)){ //basic LOS check
				return true;
			}
			if(M.tile[r,c].HasBresenhamLine(row,col)){ //for symmetry!
				return true;
			}
			if(M.tile[r,c].opaque){ //for walls, check nearby tiles
				foreach(Tile t in M.tile[r,c].NeighborsBetween(row,col)){
					if(HasBresenhamLine(t.row,t.col)){
						return true;
					}
				}
			}
			if(HasFeat(FeatType.CORNER_LOOK) && LightRadius() == 0){
				for(int i=2;i<=8;i+=2){ //for each even(orthogonal) direction...
					if(TileInDirection(i).HasBresenhamLine(r,c)){
						return true;
					}
				}
			}
			return false;
		}
		public Actor FirstActorInLine(PhysicalObject obj){ return FirstActorInLine(obj,1); }
		public Actor FirstActorInLine(PhysicalObject obj,int num){
			if(obj == null){
				return null;
			}
			int count = -1; //start at -1 so an actor won't consider itself
			List<Tile> line = GetBresenhamLine(obj.row,obj.col);
			foreach(Tile t in line){
				if(!t.passable){
					return null;
				}
				if(M.actor[t.row,t.col] != null){
					++count;
					if(count == num){
						return M.actor[t.row,t.col];
					}
				}
			}
			return null;
		}
		public bool IsWithinSightRangeOf(int r,int c){
			int dist = DistanceFrom(r,c);
			int divisor = HasAttr(AttrType.DIM_VISION)? 3 : 1;
			if(dist <= 3/divisor){
				return true;
			}
			if(dist <= 6/divisor && HasAttr(AttrType.LOW_LIGHT_VISION)){
				return true;
			}
			if(dist <= 12/divisor && HasAttr(AttrType.DARKVISION)){
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
		public List<pos> FindPath(PhysicalObject o){ return FindPath(o.row,o.col,Math.Max(ROWS,COLS)); }
		public List<pos> FindPath(int r,int c){ return FindPath(r,c,Math.Max(ROWS,COLS)); }
		public List<pos> FindPath(int r,int c,int max_distance){ //tiles past this distance are ignored entirely
			List<pos> path = new List<pos>();
			int[,] values = new int[ROWS,COLS];
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					if(M.tile[i,j].passable){
						values[i,j] = 0;
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
							if(neighbor.DistanceFrom(r,c) < best.Value.DistanceFrom(r,c)){
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
			return path;
		}
		public int DirectionOfOnly(TileType tiletype){ return DirectionOfOnly(tiletype,false); }
		public int DirectionOfOnly(TileType tiletype,bool orth){//if there's only 1 unblocked tile of this kind, return its dir
			int total=0;
			int dir=0;
			for(int i=1;i<=9;++i){
				if(i != 5){
					if(TileInDirection(i).type == tiletype && ActorInDirection(i) == null){
						if(!orth || i%2==0){
							++total;
							dir = i;
						}
					}
				}
			}
			if(total > 1){
				return -1;
			}
			else{
				if(total == 1){
					return dir;
				}
				else{
					return 0;
				}
			}
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
				if(Global.Option(OptionType.VI_KEYS)){
					ch = ConvertVIKeys(ch);
				}
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
		public Tile GetTarget(){ return GetTarget(false,-1,true); }
		public Tile GetTarget(bool lookmode){ return GetTarget(lookmode,-1,!lookmode); } //note default
		public Tile GetTarget(int max_distance){ return GetTarget(false,max_distance,true); }
		public Tile GetTarget(bool lookmode,int max_distance){ return GetTarget(lookmode,max_distance,!lookmode); }
		public Tile GetTarget(bool lookmode,int max_distance,bool start_at_interesting_target){
			Tile result = null;
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
			List<PhysicalObject> interesting_targets = new List<PhysicalObject>();
			int target_idx = 0;
			for(int i=1;(i<=max_distance || max_distance==-1) && i<=COLS;++i){
				foreach(Actor a in ActorsAtDistance(i)){
					if(CanSee(a)){
						interesting_targets.Add(a);
					}
				}
			}
			if(Global.Option(OptionType.ITEMS_AND_TILES_ARE_INTERESTING)){
				for(int i=1;(i<=max_distance || max_distance==-1) && i<=COLS;++i){
					foreach(Tile t in TilesAtDistance(i)){
						if(t.type == TileType.DOOR_C || t.type == TileType.DOOR_O
						|| t.type == TileType.STAIRS || t.type == TileType.CHEST
						|| t.type == TileType.GRENADE || t.type == TileType.FIREPIT
						|| t.type == TileType.STALAGMITE //todo: traps here
						|| t.inv != null){
							if(CanSee(t)){
								interesting_targets.Add(t);
							}
						}
					}
				}
			}
			colorchar[,] mem = new colorchar[ROWS,COLS];
			List<Tile> line = new List<Tile>();
			List<Tile> oldline = new List<Tile>();
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
						return M.tile[r,c];
					}
					target_idx = interesting_targets.IndexOf(target);
				}
			}
			bool first_iteration = true;
			bool done=false; //when done==true, we're ready to return 'result'
			while(!done){
//TextWriter file = new StreamWriter("targetoutput.txt",true);
				if(!done){ //i moved this around, thus this relic.
					Screen.ResetColors();
					if(r == row && c == col){
						if(!first_iteration){
							string s = "You're standing here. ";
							if(M.tile[r,c].inv != null){
								s = s + "You see " + M.tile[r,c].inv.AName() + " here. ";
							}
							else{
								if(M.tile[r,c].type != TileType.FLOOR){
									s = s + "You see " + M.tile[r,c].a_name + " here. ";
								}
							}
							B.DisplayNow(s);
						}
					}
					else{
						if(CanSee(M.tile[r,c])){
							string s = "";
							int count = 0;
							if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
								++count;
								s = s + M.actor[r,c].a_name;
							}
							if(M.tile[r,c].inv != null){
								++count;
								if(s == ""){
									s = s + M.tile[r,c].inv.AName();
								}
								else{
									s = s + " and " + M.tile[r,c].inv.AName();
								}
							}
							if(count == 0){
								s = s + M.tile[r,c].a_name;
							}
							if(count == 1){
								switch(M.tile[r,c].type){
								case TileType.STAIRS:
								case TileType.FLOOR:
									s = s + " on ";
									break;
								case TileType.DOOR_O:
									s = s + " in ";
									break;
								default:
									s = s + " and ";
									break;
								}
								s = s + M.tile[r,c].a_name;
							}
							B.DisplayNow("You see " + s + ". ");
						}
						else{
							if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
								B.DisplayNow("You detect " + M.actor[r,c].a_name + ". ");
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
						line = GetBresenhamLine(r,c);
	//				file.Write("startline: "); //todo
						foreach(Tile t in line){
	//						file.Write("{0}-{1}  ",t.row,t.col); //todo
							if(t.row != row || t.col != col){
//								colorchar cch = mem[t.row,t.col];
colorchar cch;
cch.color = mem[t.row,t.col].color;
cch.bgcolor = mem[t.row,t.col].bgcolor;
cch.c = mem[t.row,t.col].c;
								if(t.row == r && t.col == c){
									if(!blocked){
										cch.bgcolor = Color.Green;
										if(Global.LINUX){ //no bright bg in terminals
											cch.bgcolor = Color.DarkGreen;
										}
										if(cch.color == cch.bgcolor){
											//cch.color = Color.Black;
											cch.color = Color.Yellow;
											cch.bgcolor = Color.DarkBlue;
											cch.c = '!';
										}
										Screen.WriteMapChar(t.row,t.col,cch);
									}
									else{
										cch.bgcolor = Color.Red;
										if(Global.LINUX){
											cch.bgcolor = Color.DarkRed;
										}
										if(cch.color == cch.bgcolor){
											//cch.color = Color.Black;
											cch.color = Color.Yellow;
							cch.bgcolor = Color.DarkBlue;
					cch.c = '@';
										}
										Screen.WriteMapChar(t.row,t.col,cch);
									}
									if(t.seen && !t.passable){
										blocked = true;
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
									if(t.seen && !t.passable){
										blocked=true;
									}
								}
							}
							oldline.Remove(t);
						}
	//					file.Write("startoldline: "); //todo
						foreach(Tile t in oldline){
	//						file.Write("{0}+{1}  ",t.row,t.col); //todo
							Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
						}
	//					file.WriteLine(); //todo
				//		oldline = line;
						oldline = new List<Tile>(line);
					}
					first_iteration = false;
					M.tile[r,c].Cursor();
				}
				Console.CursorVisible = true;
				command = Console.ReadKey(true);
				char ch = ConvertInput(command);
				if(Global.Option(OptionType.VI_KEYS)){
					ch = ConvertVIKeys(ch);
				}
				switch(ch){
				case '7':
					if(r==minrow){
						if(c!=mincol){
							--c;
						}
					}
					else{
						if(c==mincol){
							--r;
						}
						else{
							--r;
							--c;
						}
					}
					break;
				case '8':
					if(r!=minrow){
						--r;
					}
					break;
				case '9':
					if(r==minrow){
						if(c!=maxcol){
							++c;
						}
					}
					else{
						if(c==maxcol){
							--r;
						}
						else{
							--r;
							++c;
						}
					}
					break;
				case '4':
					if(c!=mincol){
						--c;
					}
					break;
				case '6':
					if(c!=maxcol){
						++c;
					}
					break;
				case '1':
					if(r==maxrow){
						if(c!=mincol){
							--c;
						}
					}
					else{
						if(c==mincol){
							++r;
						}
						else{
							++r;
							--c;
						}
					}
					break;
				case '2':
					if(r!=maxrow){
						++r;
					}
					break;
				case '3':
					if(r==maxrow){
						if(c!=maxcol){
							++c;
						}
					}
					else{
						if(c==maxcol){
							++r;
						}
						else{
							++r;
							++c;
						}
					}
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
				case (char)27:
					done = true;
					break;
				case (char)13:
					if(r != row || c != col){
						if(HasBresenhamLine(r,c)){ //uses bresenham until i want symmetrical firing too
							if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
								target = M.actor[r,c];
							}
							result = M.tile[r,c];
						}
						done = true;
					}
					break;
				case ' ':
					if(lookmode){
						done = true;
					}
					break;
				default:
					break;
				}
				if(done){
					Console.CursorVisible = false;
					foreach(Tile t in line){
						Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
					}
					Console.CursorVisible = true;
				}
	//			file.Close(); //todo
			}
			return result;
		}
		public int Select(string message,List<string> strings){ return Select(message,"".PadLeft(COLS,'-'),strings,false,false); }
		public int Select(string message,List<string> strings,bool no_ask,bool no_cancel){ return Select(message,"".PadLeft(COLS,'-'),strings,no_ask,no_cancel); }
		public int Select(string message,string top_border,List<string> strings,bool no_ask,bool no_cancel){
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
			Screen.WriteMapString(i,0,"".PadRight(COLS,'-'));
			if(i < ROWS){
				Screen.WriteMapString(i+1,0,"".PadRight(COLS));
			}
			if(no_ask){
				B.DisplayNow(message);
				return -1;
			}
			else{
				int result = GetSelection(message,strings.Count,no_cancel);
				M.RedrawWithStrings();
				return result;
			}
		}
		public int GetSelection(string s,int count,bool no_cancel){
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
				if(no_cancel == false){
					if(ch == (char)27 || ch == ' '){
						return -1;
					}
				}
			}
		}
		public void AnimateProjectile(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateProjectile(GetBresenhamLine(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateMapCell(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateMapCell(o.row,o.col,new colorchar(color,c));
		}
		public void AnimateBoltProjectile(PhysicalObject o,Color color){
			B.DisplayNow();
			Screen.AnimateBoltProjectile(GetBresenhamLine(o.row,o.col),color);
		}
		public void AnimateExplosion(PhysicalObject o,int radius,Color color,char c){
			B.DisplayNow();
			Screen.AnimateExplosion(o,radius,new colorchar(color,c));
		}
		public void AnimateBeam(PhysicalObject o,Color color,char c){
			B.DisplayNow();
			Screen.AnimateBeam(GetBresenhamLine(o.row,o.col),new colorchar(color,c));
		}
		public void AnimateBoltBeam(PhysicalObject o,Color color){
			B.DisplayNow();
			Screen.AnimateBoltBeam(GetBresenhamLine(o.row,o.col),color);
		}
	}
	public static class AttackList{ //consider more descriptive attacks, such as the zealot smashing you with a mace
		private static AttackInfo[] attack = new AttackInfo[25];
		static AttackList(){
			attack[0] = new AttackInfo(100,1,DamageType.NORMAL,"& ^bites *");
			attack[1] = new AttackInfo(100,2,DamageType.NORMAL,"& ^bites *");
			attack[2] = new AttackInfo(100,3,DamageType.NORMAL,"& ^bites *");
			attack[3] = new AttackInfo(100,2,DamageType.NORMAL,"& ^hits *");
			attack[4] = new AttackInfo(100,3,DamageType.NORMAL,"& ^hits *");
			attack[5] = new AttackInfo(100,1,DamageType.NORMAL,"& ^scratches *");
			attack[6] = new AttackInfo(100,2,DamageType.COLD,"& hits * with a blast of cold");
			attack[7] = new AttackInfo(100,4,DamageType.COLD,"& releases a burst of cold");
			attack[8] = new AttackInfo(200,2,DamageType.NORMAL,"& lunges forward and ^hits *");
			attack[9] = new AttackInfo(100,4,DamageType.NORMAL,"& ^bites *");
			attack[10] = new AttackInfo(100,0,DamageType.NONE,"& lashes * with a tentacle");
			attack[11] = new AttackInfo(100,4,DamageType.NORMAL,"& ^hits *");
			attack[12] = new AttackInfo(100,4,DamageType.NORMAL,"& ^slams *");
			attack[13] = new AttackInfo(120,3,DamageType.NORMAL,"& extends a tentacle and ^hits *");
			attack[14] = new AttackInfo(120,1,DamageType.NORMAL,"& extends a tentacle and drags * closer");
			attack[15] = new AttackInfo(100,6,DamageType.NORMAL,"& ^slams *");
			attack[16] = new AttackInfo(100,3,DamageType.NORMAL,"& ^bites *");
			attack[17] = new AttackInfo(100,3,DamageType.NORMAL,"& ^claws *");
			attack[18] = new AttackInfo(150,4,DamageType.FIRE,"& breathes fire");
			attack[19] = new AttackInfo(100,1,DamageType.NORMAL,"& ^hit *"); //the player's default attack
			attack[20] = new AttackInfo(100,2,DamageType.NORMAL,"& ^pokes *");
			attack[21] = new AttackInfo(100,1,DamageType.NORMAL,"& slimes *");
			attack[22] = new AttackInfo(100,2,DamageType.NORMAL,"& ^claws *");
			attack[23] = new AttackInfo(100,2,DamageType.NORMAL,"& touches *");
			attack[24] = new AttackInfo(100,0,DamageType.NONE,"& ^hits *");
		}
		public static AttackInfo Attack(ActorType type,int num){
			switch(type){
			case ActorType.PLAYER:
				return new AttackInfo(attack[19]);
			case ActorType.RAT:
				return new AttackInfo(attack[0]);
			case ActorType.GOBLIN:
				return new AttackInfo(attack[3]);
			case ActorType.LARGE_BAT:
				switch(num){
				case 0:
					return new AttackInfo(attack[0]);
				case 1:
					return new AttackInfo(attack[5]);
				default:
					return null;
				}
			case ActorType.SHAMBLING_SCARECROW:
				return new AttackInfo(attack[17]);
			case ActorType.SKELETON:
				return new AttackInfo(attack[3]);
			case ActorType.GOBLIN_ARCHER:
				return new AttackInfo(attack[3]);
			case ActorType.WOLF:
				return new AttackInfo(attack[2]);
			case ActorType.FROSTLING:
				switch(num){
				case 0:
					return new AttackInfo(attack[3]);
				case 1:
					return new AttackInfo(attack[6]);
				case 2:
					return new AttackInfo(attack[7]);
				default:
					return null;
				}
			case ActorType.GOBLIN_SHAMAN:
				return new AttackInfo(attack[3]);
			case ActorType.ZOMBIE:
				switch(num){
				case 0:
					return new AttackInfo(attack[8]);
				case 1:
					return new AttackInfo(attack[9]);
				default:
					return null;
				}
			case ActorType.DIRE_RAT:
				return new AttackInfo(attack[1]);
			case ActorType.ROBED_ZEALOT:
				return new AttackInfo(attack[4]);
			case ActorType.WARG:
				return new AttackInfo(attack[2]);
			case ActorType.CARRION_CRAWLER:
				switch(num){
				case 0:
					return new AttackInfo(attack[10]);
				case 1:
					return new AttackInfo(attack[1]);
				default:
					return null;
				}
			case ActorType.OGRE:
				return new AttackInfo(attack[11]);
			case ActorType.PHASE_SPIDER:
				return new AttackInfo(attack[1]);
			case ActorType.STONE_GOLEM:
				return new AttackInfo(attack[12]);
			case ActorType.ORC_WARMAGE:
				return new AttackInfo(attack[4]);
			case ActorType.LASHER_FUNGUS:
				switch(num){
				case 0:
					return new AttackInfo(attack[13]);
				case 1:
					return new AttackInfo(attack[14]);
				default:
					return null;
				}
			case ActorType.CORPSETOWER_BEHEMOTH:
				return new AttackInfo(attack[15]);
			case ActorType.FIRE_DRAKE:
				switch(num){
				case 0:
					return new AttackInfo(attack[16]);
				case 1:
					return new AttackInfo(attack[17]);
				case 2:
					return new AttackInfo(attack[18]);
				default:
					return null;
				}
			case ActorType.CULTIST:
				return new AttackInfo(attack[3]);
			case ActorType.POLTERGEIST:
				switch(num){
				case 0:
					return new AttackInfo(attack[20]);
				case 1:
					return new AttackInfo(attack[21]);
				default:
					return null;
				}
			case ActorType.SWORDSMAN:
				return new AttackInfo(attack[4]);
			case ActorType.DREAM_WARRIOR:
				return new AttackInfo(attack[4]);
			case ActorType.DREAM_CLONE:
				return new AttackInfo(attack[24]);
			case ActorType.BANSHEE:
				return new AttackInfo(attack[22]);
			case ActorType.SKULKING_KILLER:
				return new AttackInfo(attack[4]);
			case ActorType.SHADOW:
				return new AttackInfo(attack[23]);
			case ActorType.BERSERKER:
				return new AttackInfo(attack[4]);
			case ActorType.ORC_GRENADIER:
				return new AttackInfo(attack[4]);
			case ActorType.NECROMANCER:
				return new AttackInfo(attack[3]);
			case ActorType.TROLL:
				return new AttackInfo(attack[17]);
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
		public static int MaxRank(FeatType type){ //todo: update
			switch(type){
			case FeatType.CORNER_LOOK:
				return 1;
			case FeatType.QUICK_DRAW:
			case FeatType.SILENT_CHAINMAIL:
			case FeatType.DANGER_SENSE:
				return 2;
			case FeatType.FULL_DEFENSE:
			case FeatType.ENDURING_SOUL:
				return 4;
			case FeatType.NECK_SNAP:
				return 5;
			case FeatType.FOCUSED_RAGE:
				return 7;
			case FeatType.SPIN_ATTACK:
			case FeatType.LUNGE:
			case FeatType.DRIVE_BACK:
			case FeatType.ARMORED_MAGE:
			case FeatType.TUMBLE:
			case FeatType.MASTERS_EDGE:
			case FeatType.STUDENTS_LUCK:
			case FeatType.ARCANE_HEALING:
			case FeatType.FORCE_OF_WILL:
			case FeatType.WAR_SHOUT:
			case FeatType.FEEL_NO_PAIN:
			case FeatType.DISARM_TRAP:
				return 3;
			default:
				return 0;
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
			case FeatType.CORNER_LOOK:
				return "Corner look";
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
			case FeatType.FOCUSED_RAGE:
				return "Focused rage";
			case FeatType.SPIN_ATTACK:
				return "Spin attack";
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
			case FeatType.ARCANE_HEALING:
				return "Arcane healing";
			case FeatType.FORCE_OF_WILL:
				return "Force of will";
			case FeatType.WAR_SHOUT:
				return "War shout";
			case FeatType.FEEL_NO_PAIN:
				return "Feel no pain";
			case FeatType.DISARM_TRAP:
				return "Disarm trap";
			default:
				return "no feat";
			}
		}
	}
}

