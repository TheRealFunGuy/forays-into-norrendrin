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
		public ActorType type{get; private set;}
		public int maxhp{get; private set;}
		public int curhp{get; set;}
		public int speed{get; set;}
		public int xp{get; set;}
		public int level{get;set;}
		public int light_radius{get;set;}
		public Actor target{get;set;}
		public List<Item> inv{get;set;}
		public SpellType[] F{get; private set;} //F[0] is the 'autospell' you cast instead of attacking, if that option is set
		public Dict<AttrType,int> attrs = new Dict<AttrType,int>();
		public Dict<SkillType,int> skills = new Dict<SkillType,int>();
		public Dict<FeatType,int> feats = new Dict<FeatType,int>();
		public Dict<SpellType,int> spells = new Dict<SpellType,int>(); //change to bool? todo
		public int magic_penalty;
		private int time_of_last_action;
		private int recover_time;
		private List<pos> path = new List<pos>();
		public Tile target_location;
		public int player_visibility_duration;
		public LinkedList<WeaponType> weapons = new LinkedList<WeaponType>();
		public LinkedList<ArmorType> armors = new LinkedList<ArmorType>();
		public LinkedList<MagicItemType> magic_items = new LinkedList<MagicItemType>();
		
		public static string player_name;
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
			Define(ActorType.RAT,"rat",'r',Color.DarkGray,15,90,0,1,0,AttrType.LOW_LIGHT_VISION,AttrType.SMALL);
			Define(ActorType.GOBLIN,"goblin",'g',Color.Green,25,100,0,1,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.LARGE_BAT,"large bat",'b',Color.DarkGray,20,60,0,1,0,AttrType.DARKVISION,AttrType.FLYING,AttrType.SMALL);
			Define(ActorType.SHAMBLING_SCARECROW,"shambling scarecrow",'x',Color.DarkYellow,30,90,0,1,0,AttrType.CONSTRUCT,AttrType.RESIST_BASH,AttrType.RESIST_PIERCE,AttrType.IMMUNE_ARROWS,AttrType.DARKVISION);
			Define(ActorType.SKELETON,"skeleton",'s',Color.White,30,100,0,2,0,AttrType.UNDEAD,AttrType.RESIST_SLASH,AttrType.RESIST_PIERCE,AttrType.RESIST_FIRE,AttrType.RESIST_COLD,AttrType.RESIST_ELECTRICITY,AttrType.DARKVISION);
			Define(ActorType.CULTIST,"cultist",'p',Color.DarkRed,35,100,0,2,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.SMALL_GROUP);
			Define(ActorType.POLTERGEIST,"poltergeist",'G',Color.DarkGreen,40,90,0,2,0,AttrType.UNDEAD,AttrType.RESIST_COLD,AttrType.LOW_LIGHT_VISION,AttrType.SMALL,AttrType.FLYING);
			Define(ActorType.ZOMBIE,"zombie",'z',Color.DarkGray,50,150,0,3,0,AttrType.UNDEAD,AttrType.RESIST_PIERCE,AttrType.RESIST_COLD);
			Define(ActorType.WOLF,"wolf",'c',Color.DarkYellow,30,50,0,3,0,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.FROSTLING,"frostling",'E',Color.Gray,35,100,0,3,0,AttrType.IMMUNE_COLD,AttrType.COLD_HIT);
			Define(ActorType.GOBLIN_ARCHER,"goblin archer",'g',Color.DarkCyan,25,100,0,4,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.GOBLIN_SHAMAN,"goblin shaman",'g',Color.Magenta,25,100,0,4,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Prototype(ActorType.GOBLIN_SHAMAN).GainSpell(SpellType.FORCE_PALM,SpellType.IMMOLATE,SpellType.SCORCH);
			Prototype(ActorType.GOBLIN_SHAMAN).skills[SkillType.MAGIC] = 4;
			Define(ActorType.SWORDSMAN,"swordsman",'p',Color.White,40,100,0,4,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.DIRE_RAT,"dire rat",'r',Color.DarkRed,25,90,0,5,0,AttrType.LOW_LIGHT_VISION,AttrType.LARGE_GROUP,AttrType.SMALL);
			Define(ActorType.DREAM_WARRIOR,"dream warrior",'p',Color.Cyan,40,100,0,5,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.DREAM_CLONE,"dream warrior",'p',Color.Cyan,1,100,0,0,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.CONSTRUCT,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.BANSHEE,"banshee",'G',Color.Magenta,40,80,0,5,0,AttrType.UNDEAD,AttrType.RESIST_COLD,AttrType.LOW_LIGHT_VISION,AttrType.FLYING);
			Define(ActorType.WARG,"warg",'c',Color.White,40,50,0,6,0,AttrType.LOW_LIGHT_VISION,AttrType.MEDIUM_GROUP);
			Define(ActorType.ROBED_ZEALOT,"robed zealot",'p',Color.Yellow,40,100,0,6,6,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Prototype(ActorType.ROBED_ZEALOT).GainSpell(SpellType.MINOR_HEAL,SpellType.BLESS,SpellType.HOLY_SHIELD);
			Prototype(ActorType.ROBED_ZEALOT).skills[SkillType.MAGIC] = 6;
			Define(ActorType.SKULKING_KILLER,"skulking killer",'p',Color.DarkBlue,40,100,0,6,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.STEALTHY,AttrType.LOW_LIGHT_VISION);
			Prototype(ActorType.SKULKING_KILLER).skills[SkillType.STEALTH] = 6;
			Define(ActorType.CARRION_CRAWLER,"carrion crawler",'i',Color.DarkGreen,35,100,0,7,0,AttrType.PARALYSIS_HIT,AttrType.DARKVISION);
			Define(ActorType.OGRE,"ogre",'O',Color.Green,55,100,0,7,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.DARKVISION,AttrType.SMALL_GROUP);
			Define(ActorType.SHADOW,"shadow",'G',Color.DarkGray,45,100,0,7,0,AttrType.UNDEAD,AttrType.RESIST_COLD,AttrType.DIM_VISION_HIT,AttrType.DARKVISION);
			Define(ActorType.BERSERKER,"berserker",'p',Color.Red,40,100,0,8,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.ORC_GRENADIER,"orc grenadier",'o',Color.DarkYellow,50,100,0,8,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.PHASE_SPIDER,"phase spider",'A',Color.Cyan,50,100,0,8,0,AttrType.POISON_HIT,AttrType.LOW_LIGHT_VISION);
			Define(ActorType.STONE_GOLEM,"stone golem",'x',Color.Gray,65,120,0,9,0,AttrType.CONSTRUCT,AttrType.STALAGMITE_HIT,AttrType.RESIST_SLASH,AttrType.RESIST_PIERCE,AttrType.RESIST_FIRE,AttrType.RESIST_COLD,AttrType.RESIST_ELECTRICITY,AttrType.DARKVISION);
			Define(ActorType.NECROMANCER,"necromancer",'p',Color.Blue,40,100,0,9,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID);
			Define(ActorType.TROLL,"troll",'T',Color.DarkGreen,55,100,0,9,0,AttrType.REGENERATING,AttrType.REGENERATES_FROM_DEATH,AttrType.DARKVISION);
			Define(ActorType.LASHER_FUNGUS,"lasher fungus",'F',Color.DarkGreen,50,100,0,10,0,AttrType.PLANTLIKE,AttrType.SPORE_BURST,AttrType.RESIST_BASH,AttrType.RESIST_FIRE,AttrType.DARKVISION);
			Define(ActorType.ORC_WARMAGE,"orc warmage",'o',Color.Red,50,100,0,10,0,AttrType.HUMANOID_INTELLIGENCE,AttrType.MEDIUM_HUMANOID,AttrType.LOW_LIGHT_VISION);
			Prototype(ActorType.ORC_WARMAGE).GainSpell(SpellType.FORCE_BEAM,SpellType.IMMOLATE,SpellType.VOLTAIC_SURGE,SpellType.MAGIC_HAMMER,SpellType.GLACIAL_BLAST,SpellType.BLOODSCENT,SpellType.PASSAGE);
			Prototype(ActorType.ORC_WARMAGE).skills[SkillType.MAGIC] = 10;
			Define(ActorType.CORPSETOWER_BEHEMOTH,"corpsetower behemoth",'z',Color.DarkMagenta,75,120,0,10,0,AttrType.UNDEAD,AttrType.TOUGH,AttrType.REGENERATING,AttrType.RESIST_COLD,AttrType.STUN_HIT);
			Define(ActorType.FIRE_DRAKE,"fire drake",'D',Color.DarkRed,120,90,0,10,3,AttrType.BOSS_MONSTER,AttrType.DARKVISION,AttrType.FIRE_HIT,AttrType.IMMUNE_FIRE,AttrType.HUMANOID_INTELLIGENCE);
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
			magic_penalty = 0;
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
			magic_penalty = 0;
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
		public void Move(int r,int c){ Move(r,c,true); }
		public void Move(int r,int c,bool trigger_traps){
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
						if(this == player && M.tile[row,col].inv != null){
							M.tile[row,col].inv.ignored = true;
						}
					}
					row = r;
					col = c;
					if(torch){
						UpdateRadius(0,LightRadius());
					}
					if(trigger_traps && tile().IsTrap() && !HasAttr(AttrType.FLYING)){
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
		public int DurationOfMagicalEffect(int original){ //intended to be used with whole turns, i.e. numbers below 50.
			int diff = (original * TotalSkill(SkillType.SPIRIT)) / 20; //each point of Spirit takes off 1/20th of the duration
			int result = original - diff; //therefore, maxed Spirit cuts durations in half
			if(result < 1){
				result = 1; //no negative turncounts please
			}
			return result;
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
		public void Input(){
			bool skip_input = false;
			if(HasAttr(AttrType.DEFENSIVE_STANCE)){
				attrs[AttrType.DEFENSIVE_STANCE] = 0;
			}
			if(HasFeat(FeatType.CONVICTION) && HasAttr(AttrType.IN_COMBAT)){
				attrs[Forays.AttrType.IN_COMBAT] = 0;
				GainAttrRefreshDuration(AttrType.CONVICTION,Math.Max(speed,100));
				attrs[Forays.AttrType.BONUS_SPIRIT]++;
				if(attrs[Forays.AttrType.CONVICTION] % 2 == 0){
					attrs[Forays.AttrType.BONUS_COMBAT]++;
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
								B.Add(the_name + " suddenly disappears. ",this);
								Move(rr,rc);
								if(seen){
									B.Add(the_name + " reappears. ",this);
								}
								else{
									B.Add(a_name + " suddenly appears! ",this);
								}
							}
							break;
						}
					}
					attrs[AttrType.TELEPORTING] = Global.Roll(2,10) + 5;
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
			if(HasAttr(AttrType.POISONED) && time_of_last_action < Q.turn){ //normal poison is 1d3-1
				if(!TakeDamage(DamageType.POISON,DamageClass.NO_TYPE,Global.Roll(1,attrs[AttrType.POISONED]+2)-1,null)){
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
				if(!TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,Global.Roll(attrs[AttrType.ON_FIRE],6),null)){
					return;
				}
			}
			if(!skip_input){ //todo - is this working correctly?
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
			if(!HasAttr(AttrType.AFRAID) && !HasAttr(AttrType.PARALYZED) && !HasAttr(AttrType.FROZEN)){
				B.Print(false);
			}
			else{
				B.DisplayNow();
			}
			Cursor();
			Console.CursorVisible = true;
			if(HasAttr(AttrType.PARALYZED) || HasAttr(AttrType.AFRAID)){
				if(HasAttr(AttrType.AFRAID)){
					Thread.Sleep(250);
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
			if(Global.Option(OptionType.AUTOPICKUP) && tile().inv != null && !tile().inv.ignored && tile().type != TileType.QUICKFIRE){
				bool grenade = false;
				foreach(Tile t in TilesWithinDistance(1)){
					if(t.type == TileType.GRENADE){
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
							if(item.type == i.type){
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
							if(DistanceFrom(path[0].row,path[0].col) == 0){
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
				if(!monsters_visible && t.passable && (!t.IsTrap() || t.name == "floor")
				&& (t.type != TileType.STAIRS || attrs[AttrType.RUNNING] == 5) && !Console.KeyAvailable){
					if(attrs[AttrType.RUNNING] == 5){
						int hplimit = HasFeat(FeatType.ENDURING_SOUL)? 20 : 10;
						if(curhp % hplimit == 0){
							attrs[AttrType.RUNNING] = 0; //todo: this is where i'll implement 'wait here for a while'
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
					if(Console.KeyAvailable){
						Console.ReadKey(true);
					}
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
					DisplayStats();
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
					|| (HasFeat(FeatType.DISARM_TRAP) && t.IsTrap() && t.name != "floor")){
						if(t.actor() == null && (t.inv == null || t.IsTrap())){
							dir = DirectionOf(t);
							++total;
						}
					}
				}
				if(total == 1){
					if(StunnedThisTurn()){
						return;
					}
					Tile t = TileInDirection(dir);
					if(t.type == TileType.DOOR_C || t.type == TileType.DOOR_O || t.type == TileType.RUBBLE){
						t.Toggle(this);
						Q1();
					}
					else{
						if(t.IsTrap()){
							if(Global.Roll(5) <= 4){
								B.Add("You disarm " + Tile.Prototype(t.type).the_name + ". ");
								t.Toggle(this);
								Q1();
							}
							else{
								if(Global.Roll(20) <= TotalSkill(Forays.SkillType.DEFENSE)){ //changed to totalskill
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
							Q0(); //shouldn't happen
						}
					}
				}
				else{
					dir = GetDirection("Operate something in which direction? ");
					if(dir != -1){
						Tile t = TileInDirection(dir);
						if(t.IsTrap() && HasFeat(FeatType.DISARM_TRAP) && t.name != "floor"){
							if(Global.Roll(5) <= 4){
								B.Add("You disarm " + Tile.Prototype(t.type).the_name + ". ");
								t.Toggle(this);
								Q1();
							}
							else{
								if(Global.Roll(20) <= TotalSkill(Forays.SkillType.DEFENSE)){ //changed to totalskill
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
				List<FeatType> ft = new List<FeatType>();
				List<char> charlist = new List<char>();
				bool feat_learned = false;
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
						feat_learned = true;
						Color lettercolor = Color.Cyan;
						string feattype;
						if(Feat.IsActivated(f)){
							if(f == FeatType.DANGER_SENSE){
								if(HasAttr(AttrType.DANGER_SENSE_ON)){
									feattype = "Toggle (currently on) ";
								}
								else{
									feattype = "Toggle (currently off)";
								}
							}
							else{
								if(f == FeatType.DRIVE_BACK){
									if(HasAttr(AttrType.DRIVE_BACK_ON)){
										feattype = "Toggle (currently on) ";
									}
									else{
										feattype = "Toggle (currently off)";
									}
								}
								else{
									feattype = "Activated";
								}
							}
							charlist.Add(letter);
						}
						else{
							feattype = "Passive";
							lettercolor = Color.DarkRed;
						}
						Screen.WriteMapString(i,0,new cstr(Color.Cyan,(s.PadRight(44) + feattype).PadRight(COLS)));
						Screen.WriteMapString(i,0,new cstr(lettercolor,"[" + letter + "]"));
						Screen.WriteMapChar(i,1,new colorchar(lettercolor,Screen.MapChar(i,1).c));
					}
					else{
						string feattype;
						if(Feat.IsActivated(f)){
							if(f == FeatType.DANGER_SENSE){
								feattype = "Toggle";
							}
							else{
								if(f == FeatType.DRIVE_BACK){
									feattype = "Toggle";
								}
								else{
									feattype = "Activated";
								}
							}
						}
						else{
							feattype = "Passive";
						}
						Screen.WriteMapString(i,0,new cstr(Color.DarkGray,(s.PadRight(44) + feattype).PadRight(COLS)));
//						Screen.WriteMapChar(i,1,new colorchar(Color.DarkRed,Screen.MapChar(i,1).c));
						Screen.WriteMapString(i,0,new cstr(Color.DarkRed,"[" + letter + "]"));
						if(feats[f] != 0){
							Screen.WriteMapString(i,27,"(" + (-feats[f]) + "/" + Feat.MaxRank(f) + ")");
						}
					}
					letter++;
					i++;
				}
				Screen.WriteMapString(21,0,("".PadRight(25,'-') + "[?] for help").PadRight(COLS,'-'));
				Screen.WriteMapChar(21,26,new colorchar(Color.Cyan,'?'));
				Screen.ResetColors();
				if(feat_learned){
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
					else{
						if(ch == '?'){
							Global.DisplayHelp(Help.Feats);
							done = true;
						}
						else{
							done = true;
						}
					}
					//if(ch == (char)27 || ch == ' '){
					//	done = true;
					//} //made everything break out, see 5 lines up.
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
				List<colorstring> ls = new List<colorstring>();
				List<SpellType> sp = new List<SpellType>();
				foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
					if(HasSpell(spell)){
						string s = Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3);
						s = s + FailRate(spell).ToString().PadLeft(9) + "%";
						s = s + Spell.Description(spell).PadLeft(34);
						cstr cs1 = new cstr(Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3),Color.Gray);
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
						cstr cs2 = new cstr(FailRate(spell).ToString().PadLeft(9) + "%",failcolor);
						cstr cs3 = new cstr(Spell.Description(spell).PadLeft(34),Color.Gray);
						ls.Add(new colorstring(cs1,cs2,cs3));
						sp.Add(spell);
					}
				}
				if(sp.Count > 0){
					colorstring topborder = new colorstring("------------------Level---Fail rate--------Description------------",Color.Gray);
					int basefail = magic_penalty * 5;
					if(!HasFeat(FeatType.ARMORED_MAGE)){
						basefail += Armor.AddedFailRate(armors.First.Value);
					}
					Color globalfailcolor = Color.White;
					if(basefail > 50){
						globalfailcolor = Color.DarkRed;
					}
					else{
						if(basefail > 20){
							globalfailcolor = Color.Red;
						}
						else{
							if(basefail > 0){
								globalfailcolor = Color.Yellow;
							}
						}
					}
					colorstring bottomborder = new colorstring("-------------Base fail rate: ",Color.Gray,(basefail.ToString().PadLeft(2) + "%"),globalfailcolor,"".PadRight(37,'-'),Color.Gray);
					//int i = Select("Cast which spell? ",topborder,bottomborder,ls);
					int i = Select("Cast which spell? ",topborder,bottomborder,ls,false,false,true);
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
						foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
							if(spells[spell] > 1){
								can_recover_spells = true;
							}
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
					B.Add("You find it impossible to rest right now. ");
					Q0();
				}
				break;
			case '>':
				if(M.tile[row,col].type == TileType.STAIRS){
					if(StunnedThisTurn()){
						break;
					}
					bool can_recover_spells = false;
					foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
						if(spells[spell] > 1){
							can_recover_spells = true; //todo - this seems to be old code
						}
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
					M.GenerateLevel();
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
						B.Add("There's nothing here to pick up. ");
						Q0();
					}
				}
				else{
					if(StunnedThisTurn()){
						break;
					}
					Item i = tile().inv;
					i.row = -1;
					i.col = -1;
					tile().inv = null;
					B.Add("You pick up " + i.TheName() + ". ");
					bool added = false;
					foreach(Item item in inv){
						if(item.type == i.type){
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
					Screen.WriteMapString(line,0,"-----------------------[?] for help".PadRight(COLS,'-'));
					Screen.WriteMapChar(line,24,new colorchar(Color.Cyan,'?'));
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
								Global.DisplayHelp(Help.Items);
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
							B.DisplayNow("Drop how many? (1-" + i.quantity + "): ");
							int count = Global.EnterInt();
							if(count < 1){
								Q0();
							}
							else{
								if(count >= i.quantity){
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
					Screen.WriteMapString(line,0,"-----------------------[?] for help".PadRight(COLS,'-'));
					Screen.WriteMapChar(line,24,new colorchar(Color.Cyan,'?'));
					if(line < ROWS){
						Screen.WriteMapString(line+1,0,"".PadRight(COLS));
					}
					B.DisplayNow("In your pack: ");
					Console.CursorVisible = true;
					command = Console.ReadKey(true);
					ch = ConvertInput(command);
					if(ch == '?'){
						Global.DisplayHelp(Help.Items);
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
					Screen.WriteMapString(line,0,"-----------------------[?] for help".PadRight(COLS,'-'));
					Screen.WriteMapChar(line,24,new colorchar(Color.Cyan,'?'));
					if(line < ROWS){
						Screen.WriteMapString(line+1,0,"".PadRight(COLS));
					}
					B.DisplayNow("Use which item? ");
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
								Global.DisplayHelp(Help.Items);
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
				if(!weapon_changed && !armor_changed){
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
/*			case (char)9: //tab
				{
				List<PhysicalObject> interesting_targets = new List<PhysicalObject>(); //c&p go
				foreach(Actor a in M.AllActors()){
					if(a != this && CanSee(a)){
						interesting_targets.Add(a);
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
				}*/
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
					//ls.Add("Use vi-style keys".PadRight(58) + (Global.Option(OptionType.VI_KEYS)? "yes ":"no ").PadLeft(4));
					ls.Add("Use last target when possible".PadRight(58) + (Global.Option(OptionType.LAST_TARGET)? "yes ":"no ").PadLeft(4));
					ls.Add("Automatically pick up items (if safe)".PadRight(58) + (Global.Option(OptionType.AUTOPICKUP)? "yes ":"no ").PadLeft(4));
					//ls.Add("Open chests by walking into them".PadRight(58) + (Global.Option(OptionType.OPEN_CHESTS)? "yes ":"no ").PadLeft(4));
					ls.Add("Hide old messages instead of darkening them".PadRight(58) + (Global.Option(OptionType.HIDE_OLD_MESSAGES)? "yes ":"no ").PadLeft(4));
					ls.Add("Hide the command hints on the side".PadRight(58) + (Global.Option(OptionType.HIDE_COMMANDS)? "yes ":"no ").PadLeft(4));
					ls.Add("Cast a spell instead of attacking".PadRight(46) + (F[0]==SpellType.NO_SPELL? "no ":Spell.Name(F[0])).PadLeft(16));
					//ls.Add("Don't print a message for the Blood boil feat".PadRight(58) + (Global.Option(OptionType.NO_BLOOD_BOIL_MESSAGE)? "yes ":"no ").PadLeft(4));
					ls.Add("Don't use roman numerals for automatic naming".PadRight(58) + (Global.Option(OptionType.NO_ROMAN_NUMERALS)? "yes ":"no ").PadLeft(4));
					//ls.Add("Make unseen walls and floors dark gray".PadRight(58) + (Global.Option(OptionType.DARK_GRAY_UNSEEN)? "yes ":"no ").PadLeft(4));
					//ls.Add("Consider items and tiles interesting".PadRight(58) + (Global.Option(OptionType.ITEMS_AND_TILES_ARE_INTERESTING)? "yes ":"no ").PadLeft(4));
					Select("Options: ",ls,true,false,false);
					Console.CursorVisible = true;
					ch = ConvertInput(Console.ReadKey(true));
					switch(ch){
					/*case 'a':
						Global.Options[OptionType.VI_KEYS] = !Global.Option(OptionType.VI_KEYS);
						break;*/
					case 'a':
						Global.Options[OptionType.LAST_TARGET] = !Global.Option(OptionType.LAST_TARGET);
						break;
					case 'b':
						Global.Options[OptionType.AUTOPICKUP] = !Global.Option(OptionType.AUTOPICKUP);
						break;
					/*case 'd':
						Global.Options[OptionType.OPEN_CHESTS] = !Global.Option(OptionType.OPEN_CHESTS);
						break;*/
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
							List<string> list = new List<string>();
							List<SpellType> sp = new List<SpellType>();
							foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
								if(HasSpell(spell)){
									string s = Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3);
									s = s + FailRate(spell).ToString().PadLeft(9) + "%";
									s = s + Spell.Description(spell).PadLeft(34);
									list.Add(s);
									sp.Add(spell);
								}
							}
							if(sp.Count > 0){
								string topborder = "------------------Level---Fail rate--------Description------------";
								int basefail = magic_penalty * 5;
								if(!HasFeat(FeatType.ARMORED_MAGE)){
									basefail += Armor.AddedFailRate(armors.First.Value);
								}
								string bottomborder = "-------------Base fail rate: " + (basefail.ToString().PadLeft(2) + "%").PadRight(37,'-');
								int i = Select("Automatically cast which spell? ",topborder,bottomborder,list);
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
					/*case 'g':
						Global.Options[OptionType.NO_BLOOD_BOIL_MESSAGE] = !Global.Option(OptionType.NO_BLOOD_BOIL_MESSAGE);
						break;*/
					case 'f':
						Global.Options[OptionType.NO_ROMAN_NUMERALS] = !Global.Option(OptionType.NO_ROMAN_NUMERALS);
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
				Global.DisplayHelp();
				Q0();
				break;
			}
			case '-':
			{
				Console.CursorVisible = false;
				List<string> commandhelp = Global.HelpTopic(Help.Commands);
				commandhelp.RemoveRange(0,2);
				Screen.WriteMapString(0,0,"".PadRight(COLS,'-'));
				for(int i=0;i<20;++i){
					Screen.WriteMapString(i+1,0,commandhelp[i].PadRight(COLS));
				}
				Screen.WriteMapString(ROWS-1,0,"".PadRight(COLS,'-'));
				B.DisplayNow("Commands: ");
				Console.CursorVisible = true;
				Console.ReadKey(true);
				Screen.Blank();
				Q0();
				break;
			}
			case 'q':
				List<string> ls = new List<string>();
				ls.Add("Abandon character and exit to main menu");
				ls.Add("Abandon character and quit game");
				ls.Add("Quit game immediately - don't save anything");
				ls.Add("Continue playing");
				Console.CursorVisible = true;
				switch(Select("Quit? ",ls)){
				case 0:
					Global.GAME_OVER = true;
					break;
				case 1:
					Global.GAME_OVER = true;
					Global.QUITTING = true;
					break;
				case 2:
					Global.Quit();
					break;
				case 3:
				default:
					break;
				}
				Q0();
				break;
/*			case 'm':
				Screen.WriteMapString(21,0,"The first option. Move it beyond a wall and it turns red. ");
				List<Tile> line2 = GetTarget(false,15,true,3);
				AnimateBoltBeam(line2,Color.Yellow);
				AnimateExplosion(line2.Last(),3,'*',Color.RandomLightning);
				Screen.WriteMapString(21,0,"The 2nd option. Like the first, but with an outline.      ");
				line2 = GetTarget3Temp(false,15,true,3);
				AnimateBoltBeam(line2,Color.Cyan);
				AnimateExplosion(line2.Last(),3,'*',Color.Cyan);
				Screen.WriteMapString(21,0,"The 3rd option. Notice that the radius stops at walls.    ");
				line2 = GetTarget2Temp(false,15,true,3);
				AnimateBeam(line2,'*',Color.RandomFire);
				AnimateExplosion(line2.Last(),3,'*',Color.RandomFire);
				Screen.WriteMapString(21,0,"                                                          ");
				Q0();
				break;*/
			case '~': //debug mode 
				if(false){
				List<string> l = new List<string>();
				l.Add("Throw a prismatic orb");
				l.Add("create chests");
				l.Add("Toggle low light vision");
				l.Add("Check key names");
				l.Add("Forget the map");
				l.Add("Heal to full");
				l.Add("Become invulnerable");
				l.Add("get items!");
				l.Add("Spawn a monster");
				l.Add("Use a rune of passage");
				l.Add("See the entire level");
				l.Add("Generate new level");
				l.Add("Gain all skills and feats");
				l.Add("Level up");
				l.Add("create trap");
				l.Add("create door");
				l.Add("spawn lots of goblins and lose neck snap");
				l.Add("test help");
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
					xp = 9999;
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
					LevelUp();
					Q0();
					break;
				case 14:
				{
					foreach(Tile t in TilesAtDistance(1)){
						t.TransformTo((TileType)Global.Roll(6)+9);
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
					Global.DisplayHelp(Help.Overview);
					Q0();
					break;
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
						/*if(Global.Option(OptionType.OPEN_CHESTS) && TileInDirection(dir).type==TileType.CHEST){
							if(StunnedThisTurn()){
								return;
							}
							TileInDirection(dir).OpenChest();
							Q1();
						}
						else{*/
						if(TileInDirection(dir).type == TileType.STAIRS && !Global.Option(OptionType.HIDE_COMMANDS)){
							B.Add("There are stairs here - press > to descend. ");
						}
						if(TileInDirection(dir).inv != null){
							B.Add("You see " + TileInDirection(dir).inv.AName() + ". ");
						}
						Move(TileInDirection(dir).row,TileInDirection(dir).col);
						QS();
						//}
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
					B.Add(the_name + "'s gaze meets your eyes! ",this); //better message?
					B.Add(the_name + " snarls loudly. ",this);
					if(DistanceFrom(player) <= 6){
						player.MakeNoise();
					}
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
				if((IsWithinSightRangeOf(player.row,player.col) || M.tile[player.row,player.col].IsLit())
					&& HasLOS(player.row,player.col)){ //if they're stealthed and nearby...
					if(player.Stealth() * DistanceFrom(player) * 10 - player_visibility_duration++*5 < Global.Roll(1,100)){
						player_visibility_duration = -1;
						target = player;
						target_location = M.tile[player.row,player.col];
						switch(type){
						case ActorType.RAT:
						case ActorType.DIRE_RAT:
							B.Add(the_name + " squeaks at you. ",this);
							player.MakeNoise();
							break;
						case ActorType.GOBLIN:
						case ActorType.GOBLIN_ARCHER:
						case ActorType.GOBLIN_SHAMAN:
							B.Add(the_name + " growls. ",this);
							player.MakeNoise();
							break;
						case ActorType.CULTIST:
						case ActorType.ROBED_ZEALOT:
							B.Add(the_name + " yells. ",this);
							player.MakeNoise();
							break;
						case ActorType.ZOMBIE:
							B.Add(the_name + " moans. Uhhhhhhghhh. ",this);
							player.MakeNoise();
							break;
						case ActorType.WOLF:
							B.Add(the_name + " snarls at you. ",this);
							player.MakeNoise();
							break;
						case ActorType.FROSTLING:
							B.Add(the_name + " makes a chittering sound. ",this);
							player.MakeNoise();
							break;
						case ActorType.SWORDSMAN:
						case ActorType.BERSERKER:
							B.Add(the_name + " shouts. ",this);
							player.MakeNoise();
							break;
						case ActorType.BANSHEE:
							B.Add(the_name + " shrieks. ",this);
							player.MakeNoise();
							break;
						case ActorType.WARG:
							B.Add(the_name + " howls. ",this);
							player.MakeNoise();
							break;
						case ActorType.OGRE:
							B.Add(the_name + " bellows at you. ",this);
							player.MakeNoise();
							break;
						case ActorType.SHADOW:
							B.Add(the_name + " hisses faintly. ",this);
							break;
						case ActorType.ORC_GRENADIER:
						case ActorType.ORC_WARMAGE:
							B.Add(the_name + " snarls loudly. ",this);
							player.MakeNoise();
							break;
						case ActorType.STONE_GOLEM:
							B.Add(the_name + " starts moving. ",this);
							break;
						case ActorType.NECROMANCER:
							B.Add(the_name + " starts chanting in low tones. ",this);
							break;
						case ActorType.TROLL:
							B.Add(the_name + " growls viciously. ",this);
							player.MakeNoise();
							break;
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
			if(!no_act && type != ActorType.CULTIST && type != ActorType.SHAMBLING_SCARECROW && type != ActorType.CORPSETOWER_BEHEMOTH
			&& type != ActorType.DREAM_CLONE && type != ActorType.ZOMBIE){
				if(HasAttr(AttrType.HUMANOID_INTELLIGENCE)){
					if(HasAttr(AttrType.CATCHING_FIRE) && Global.Roll(10) == 10){
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
							if(type == ActorType.BANSHEE){
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
			case ActorType.GOBLIN_SHAMAN:
			{
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
					if(curhp <= 13){
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
			{
				if(curhp <= 15){
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
					if(FirstActorInLine(target) != target){
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
								if(Attack(1,target)){
									if(target.HasAttr(AttrType.FROZEN)){
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
						else{
							Q1();
						}
					}
				}
				else{
					Q1();
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
			case ActorType.POLTERGEIST: //after it materializes, of course
				if(DistanceFrom(target) == 1){
					if(Global.CoinFlip() || HasAttr(AttrType.COOLDOWN_1)){
						Attack(Global.Roll(2)-1,target);
					}
					else{
						attrs[AttrType.COOLDOWN_1]++;
						Q.Add(new Event(this,450,AttrType.COOLDOWN_1));
						B.Add(the_name + " cackles. ",this);
						AI_Step(target,true);
						AI_Step(TileInDirection(Global.RandomDirection()));
						QS();
					}
				}
				else{
					if(HasAttr(AttrType.COOLDOWN_1)){
						AI_Step(target,true);
						AI_Step(TileInDirection(Global.RandomDirection()));
						QS();
					}
					else{
						AI_Step(target);
						QS();
					}
				}
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
			case ActorType.BANSHEE:
				if(!HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(Global.Roll(5)+5)*100,AttrType.COOLDOWN_1));
					B.Add(You("scream") + ". ",this);
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
							B.Add(target.You("resist") + " the scream. ",target,this);
							Q1();
							return;
						}
					}
					foreach(Actor actor in targets){
						if(actor.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(6),this)){
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
					Q.Add(new Event(target,(Global.Roll(3)+4)*100,AttrType.SLOWED,target.YouAre() + " no longer slowed. ",target));
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
					if(tiles.Count == 0 || attrs[AttrType.COOLDOWN_2] >= 10){
						B.Add("Nothing happens. ",this);
					}
					else{
						attrs[AttrType.COOLDOWN_2]++;
						Tile t = tiles.Random();
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
				if(player.magic_items.Contains(MagicItemType.RING_OF_RESISTANCE) && DistanceFrom(player) <= 12 && CanSee(player)){
					B.Add(the_name + " exhales an orange mist toward you. ");
					B.Add("Your ring of resistance melts and drips onto the floor! ");
					player.magic_items.Remove(MagicItemType.RING_OF_RESISTANCE);
					Q.Add(new Event(this,150,EventType.MOVE));  //todo untested
				}
				else{
					if(player.armors.First.Value == ArmorType.FULL_PLATE_OF_RESISTANCE && DistanceFrom(player) <= 12 && CanSee(player)){
						B.Add(the_name + " exhales an orange mist toward you. ");
						B.Add("The runes melt off of your full plate of resistance! ");
						player.armors.First.Value = ArmorType.FULL_PLATE;
						player.UpdateOnEquip(ArmorType.FULL_PLATE_OF_RESISTANCE,ArmorType.FULL_PLATE);
						Q.Add(new Event(this,150,EventType.MOVE)); //todo untested
					}
					else{
						if(!HasAttr(AttrType.COOLDOWN_1)){
							if(DistanceFrom(target) <= 12){
								attrs[AttrType.COOLDOWN_1]++;
								int cooldown = (Global.Roll(1,4)+1) * 100;
								Q.Add(new Event(this,cooldown,AttrType.COOLDOWN_1));
								AnimateBeam(target,Color.RandomFire,'*');
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
			if(path.Count > 0 && type != ActorType.LASHER_FUNGUS){
				AI_Step(M.tile[path[0].row,path[0].col]);
				if(DistanceFrom(path[0]) == 0){
					path.RemoveAt(0);
				}
				QS();
				return;
			}
			switch(type){
			case ActorType.SHAMBLING_SCARECROW:
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
				break;
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
				if(!HasAttr(AttrType.BLOODSCENT)){
					CastSpell(SpellType.BLOODSCENT);
				}
				else{
					QS();
				}
				break;
			case ActorType.LASHER_FUNGUS:
				QS();
				break;
			case ActorType.FIRE_DRAKE:
				FindPath(player);
				QS();
				break;
			default:
				if(target_location != null){
					if(DistanceFrom(target_location) == 1 && M.actor[target_location.row,target_location.col] != null){
						Move(target_location.row,target_location.col); //swap places
						target_location = null;
						QS();
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
					if(DistanceFrom(target) <= 5){
						List<pos> path2 = GetPath(target,8);
						if(path2.Count <= 10){
							path = path2;
						}
						//FindPath(target,8);
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
		public void IdleAI(){
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
				AI_Step(TileInDirection(Global.RandomDirection()));
				QS();
				break;
			case ActorType.ZOMBIE: //makes sound, or does nothing
				QS();
				break;
			case ActorType.ORC_WARMAGE:
				if(!HasAttr(AttrType.BLOODSCENT)){
					CastSpell(SpellType.BLOODSCENT);
				}
				else{
					QS();
				}
				break;
			case ActorType.SHAMBLING_SCARECROW:
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
				break;
			case ActorType.POLTERGEIST: //makes sound, or does nothing
				QS();
				break;
			case ActorType.SWORDSMAN:
				if(attrs[AttrType.BONUS_COMBAT] > 0){
					attrs[AttrType.BONUS_COMBAT] = 0;
				}
				QS();
				break;
			case ActorType.BANSHEE: //makes sound, or does nothing
				QS();
				break;
			case ActorType.FIRE_DRAKE:
				FindPath(player);
				QS();
				break;
			default: //simply end turn
				QS();
				break;
			}
		}
		public void CalculateDimming(){
			if(M.wiz_lite || M.wiz_dark){
				return;
			}
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
					string walks = " walks straight into you! ";
					if(HasAttr(AttrType.FLYING)){
						walks = " flies straight into you! ";
					}
					if(!IsHiddenFrom(player)){
						B.Add(the_name + walks);
						B.Add(the_name + " looks startled. ");
					}
					else{
						attrs[AttrType.TURNS_VISIBLE] = -1;
						B.Add(a_name + walks);
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
				else{
					if(M.tile[r,c].type == TileType.RUBBLE){
						if(HasAttr(AttrType.SMALL)){
							if(M.actor[r,c] == null){
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
			//pos pos_of_target = new pos(a.row,a.col);
			AttackInfo info = AttackList.Attack(type,attack_idx);
			if(weapons.First.Value != WeaponType.NO_WEAPON){
				info.damage = Weapon.Damage(weapons.First.Value);
			}
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
			bool player_in_combat = false;
			if(player.HasFeat(FeatType.CONVICTION) && (this == player || a == player)){
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
				if(a.HasAttr(AttrType.FROZEN) || a.type == ActorType.LASHER_FUNGUS){
					nowhere_to_run = true;
				}
				if(nowhere_to_run){
					hit = true;
				}
			}
			string s = info.desc + ". ";
			if(hit){
				if(HasFeat(FeatType.NECK_SNAP) && a.HasAttr(AttrType.MEDIUM_HUMANOID) && IsHiddenFrom(a)){
					B.Add(You("silently snap") + " " + a.Your() + " neck. ");
					a.TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,9001,this);
					Q1();
					return true;
				}
				int dice = info.damage.dice;
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
							MakeNoise();
							a.TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,1337,this);
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
							int amount = Global.Roll(6);
							if(!a.HasAttr(AttrType.RESIST_FIRE) || amount / a.attrs[AttrType.RESIST_FIRE] > 0){ //todo i think resistance is wrong here
								B.Add(a.YouAre() + " burned. ",this,a);
							}
							a.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,amount,this);
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
					if(!a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT)
					&& !a.HasAttr(AttrType.POISON_HIT) && !a.HasAttr(AttrType.IMMUNE_TOXINS)){
						if(a.HasAttr(AttrType.POISONED)){
							B.Add(a.YouAre() + " more poisoned. ",this,a);
						}
						else{
							B.Add(a.YouAre() + " poisoned. ",this,a);
						}
						a.attrs[AttrType.POISONED]++;
						Q.Add(new Event(a,(Global.Roll(6)+6)*100,AttrType.POISONED));
					}
				}
				if(HasAttr(AttrType.PARALYSIS_HIT) && attack_idx==1 && type == ActorType.CARRION_CRAWLER && M.actor[r,c] != null){
					if(!a.HasAttr(AttrType.IMMUNE_TOXINS)){
						//hack: carrion crawler only
						B.Add(a.YouAre() + " paralyzed. ",this,a); //todo: update to handle paralyzation resistance. (maybe)
						a.attrs[AttrType.PARALYZED] = Global.Roll(1,3)+3;
					}
				}
				if(HasAttr(AttrType.FORCE_HIT) && M.actor[r,c] != null){
					if(Global.CoinFlip()){
						if(Global.CoinFlip()){
							a.GetKnockedBack(this);
						}
						else{
							if(!a.HasAttr(AttrType.STUNNED)){
								B.Add(a.YouAre() + " stunned. ",a);
								a.attrs[AttrType.STUNNED]++;
								int duration = (Global.Roll(4)+3)*100;
								Q.Add(new Event(a,duration,AttrType.STUNNED,a.YouAre() + " no longer stunned. ",a));
							}
						}
					}
				}
				if(HasAttr(AttrType.DIM_VISION_HIT) && M.actor[r,c] != null){
					if(!a.HasAttr(AttrType.DIM_VISION)){
						string str = "";
						if(a.type == ActorType.PLAYER){
							B.Add("Your vision grows weak. ");
							str = "Your vision returns to normal. ";
						}
						a.attrs[AttrType.DIM_VISION]++;
						Q.Add(new Event(a,a.DurationOfMagicalEffect(Global.Roll(2,20)+20)*100,AttrType.DIM_VISION,str));
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
				if(HasAttr(AttrType.STUN_HIT) && M.actor[r,c] != null){
					B.Add(a.YouAre() + " stunned. ",a);
					a.GainAttrRefreshDuration(AttrType.STUNNED,550,a.YouAre() + " no longer stunned. ",a);
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
								if(HasAttr(AttrType.DRIVE_BACK_ON)){
									B.Add(You("drive") + " " + a.the_name + " back. ");
								}
								else{
									B.Add(You("miss",true) + " " + a.the_name + ". ");
								}
							}
						}
					}
				}
				if(HasAttr(AttrType.DRIVE_BACK_ON)){
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
			B.Add(You("fire") + " an arrow. ",this);
			B.DisplayNow();
			if(a != null){
				Screen.AnimateBoltProjectile(line.To(a),Color.DarkYellow,20);
			}
			else{
				Screen.AnimateBoltProjectile(line.To(t),Color.DarkYellow,20);
			}
			int idx = 0;
			foreach(string s in misses){
				B.Add(s,this,missed[idx]);
				++idx;
			}
			if(a != null){
				if(a.HasAttr(AttrType.IMMUNE_ARROWS)){
					B.Add("The arrow sticks out ineffectively from " + a.the_name + ". ",this,a);
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
						B.Add("The arrow critically hits " + a.the_name + ". ",this,a);
						if(!a.TakeDamage(DamageType.PIERCING,DamageClass.PHYSICAL,18+TotalSkill(SkillType.COMBAT),this)){
							alive = false;
						}
					}
					else{
						B.Add("The arrow hits " + a.the_name + ". ",this,a);
						if(!a.TakeDamage(DamageType.PIERCING,DamageClass.PHYSICAL,Global.Roll(3,6)+TotalSkill(SkillType.COMBAT),this)){
							alive = false;
						}
					}
					if(alive && (a.HasAttr(AttrType.DEMON) || a.HasAttr(AttrType.UNDEAD))){
						foreach(WeaponType w in weapons){
							if(w == WeaponType.HOLY_LONGBOW){
								B.Add(a.the_name + " is blasted with holy energy! ",a);
								a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(3,6),this);
								break;
							}
						}
					}
				}
			}
			else{
				if(!actor_present){
					B.Add("The arrow hits " + t.the_name + ". ",this,t);
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
			return TakeDamage(new Damage(dmgtype,damclass,source,dmg));
		}
		public bool TakeDamage(Damage dmg){ //returns true if still alive
			bool damage_dealt = false;
			int old_hp = curhp;
			if(HasAttr(AttrType.INVULNERABLE)){
				dmg.amount = 0;
			}
			if(HasAttr(AttrType.FROZEN)){
				attrs[Forays.AttrType.FROZEN] -= (dmg.amount+1) / 2;
				if(attrs[Forays.AttrType.FROZEN] <= 0){
					attrs[Forays.AttrType.FROZEN] = 0;
					B.Add("The ice breaks! ",this);
				}
				dmg.amount = dmg.amount / 2;
			}
			if(HasAttr(AttrType.TOUGH) && dmg.damclass == DamageClass.PHYSICAL){
				dmg.amount -= 2; //test this value
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
			case DamageType.SLASHING:
				{
				int div = 1;
				if(HasAttr(AttrType.RESIST_SLASH)){
					for(int i=attrs[AttrType.RESIST_SLASH];i>0;--i){
						div = div * 2;
					}
					B.Add(You("resist") + ". ",this);
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
					}
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
						target_location = M.tile[dmg.source.row,dmg.source.col];
						if(dmg.source.IsHiddenFrom(this)){
							player_visibility_duration = -1;
						}
						//if(light_radius > 0 && HasLOS(dmg.source.row,dmg.source.col)){//for enemies who can't see in darkness
						//}
					}
				}
				if(HasAttr(AttrType.SPORE_BURST) && !HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(Global.Roll(1,5)+1)*100,AttrType.COOLDOWN_1));
					B.Add(You("retaliate") + " with a burst of spores! ",this);
					for(int i=1;i<=8;++i){
						AnimateStorm(i,1,(((i*2)+1)*((i*2)+1)) / 4,'*',Color.DarkYellow);
					}
					foreach(Actor a in ActorsWithinDistance(8)){
						if(HasLOS(a.row,a.col) && a != this){
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
					B.Add(Your() + " holy shield burns " + dmg.source.the_name + ". ",this,dmg.source);
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
					Q.KillEvents(this,AttrType.BLOOD_BOILED);
					//GainAttr(AttrType.BLOOD_BOILED,1001,attrs[Forays.AttrType.BLOOD_BOILED],"Your blood cools. ");
					Q.Add(new Event(this,1001,Forays.AttrType.BLOOD_BOILED,attrs[Forays.AttrType.BLOOD_BOILED],"Your blood cools. "));
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
					if(HasAttr(AttrType.REGENERATES_FROM_DEATH) && dmg.type != DamageType.FIRE){ //hack - works only with trolls
						B.Add(the_name + " falls to the ground, still twitching. ",this);
						Q.Add(new Event(tile(),200,EventType.REGENERATING_FROM_DEATH,curhp - (Global.Roll(10)+5)));
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
							if(t.type == TileType.FLOOR && (t.actor() == null || t.actor() == this) && HasLOS(t)){
								if(DistanceFrom(t) <= 2 || Global.CoinFlip()){
									t.TransformTo(TileType.RUBBLE);
								}
							}
						}
					}
					if(player.HasAttr(AttrType.CONVICTION)){
						player.attrs[Forays.AttrType.KILLSTREAK]++;
					}
					int divisor = 1;
					if(HasAttr(AttrType.SMALL_GROUP)){ divisor = 2; }
					if(HasAttr(AttrType.MEDIUM_GROUP)){ divisor = 3; }
					if(HasAttr(AttrType.LARGE_GROUP)){ divisor = 5; }
					if(!Global.GAME_OVER){
						player.GainXP(xp + (level*(10 + level - player.level))/divisor); //experimentally giving the player any
					}
					Q.KillEvents(this,EventType.ANY_EVENT);					// XP that the monster had collected. currently always 0.
					M.RemoveTargets(this);
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
			if(next.passable && M.actor[next.row,next.col] == null){
				B.Add(YouAre() + " knocked back. ",this);
				if(HasAttr(AttrType.FROZEN)){
					attrs[AttrType.FROZEN] = 0;
					B.Add("The ice breaks! ",this);
				}
				Move(next.row,next.col);
			}
			else{
				int r = row;
				int c = col;
				bool immobilized = HasAttr(AttrType.FROZEN);
				if(!next.passable){
					B.Add(YouAre() + " knocked into " + next.the_name + ". ",this,next);
					TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),source);
				}
				else{
					B.Add(YouAre() + " knocked into " + M.actor[next.row,next.col].the_name + ". ",this,M.actor[next.row,next.col]);
					TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),source);
					M.actor[next.row,next.col].TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),source);
				}
				if(immobilized && M.actor[r,c] != null){
					B.Add("The ice breaks! ",this);
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
			else{
				if(HasFeat(FeatType.MASTERS_EDGE)){
					bonus = 1;
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
						AnimateBeam(line,'*',Color.RandomFire);
						B.Add(You("throw") + " flames. ",this);
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
						B.Add(You("strike") + " " + a.the_name + ". ",this,a);
						string s = a.the_name;
						List<Tile> line2 = GetBestExtendedLine(a.row,a.col);
						int idx = line2.IndexOf(M.tile[a.row,a.col]);
						Tile next = line2[idx+1];
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(1+bonus,6),this);
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
										M.actor[next.row,next.col].TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(1,6),this);
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
							if(t.type == TileType.HIDDEN_DOOR){
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
							a.attrs[AttrType.FROZEN] = 15;
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
						a.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,Global.Roll(2+bonus,6),this);
					}
					else{
						AnimateProjectile(line,'*',Color.RandomFire);
						B.Add("The scorching bolt hits " + t.the_name + ". ",t);
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
						Q.Add(new Event(this,10001,Forays.AttrType.BLOODSCENT,"You lose the scent. "));
					}
					else{
						Q.Add(new Event(this,10001,Forays.AttrType.BLOODSCENT));
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
										if(added.HasLOS(nearby)){
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
							ac.TakeDamage(DamageType.ELECTRIC,DamageClass.MAGICAL,Global.Roll(2+bonus,6),this);
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
				List<Actor> targets = ActorsWithinDistance(2,true);
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
						a.TakeDamage(DamageType.ELECTRIC,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this);
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
						B.Add(You("smash",true) + " " + a.the_name + ". ",this,a);
						if(a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(4+bonus,6),this)){
							a.GainAttr(AttrType.STUNNED,201,a.YouAre() + " no longer stunned. ",a);
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
					target_location.symbol = '_';
					target_location.color = Color.RandomPrismatic;
				}
				else{
					if(M.actor[target_location.row,target_location.col] == null && target_location.passable){
						B.Add("You activate your rune of transport. ");
						Move(target_location.row,target_location.col);
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
						a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this);
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
					B.Add("There's no wall here. ");
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
								Move(t.row,t.col);
								M.Draw();
								B.Add(You("travel") + " through the passage. ",this);
							}
							else{
								int j = 0;
								foreach(Tile tile in tiles){
									Screen.WriteMapChar(tile.row,tile.col,memlist[j++]);
									Thread.Sleep(35);
								}
								B.Add("The passage is blocked. ",this);
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
					while(targets.Count > 0){
						Actor ac = targets.RemoveRandom();
						B.Add("The explosion hits " + ac.the_name + ". ",ac);
						ac.TakeDamage(DamageType.FIRE,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this);
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
						a.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(3+bonus,6),this);
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
						a.TakeDamage(DamageType.BASHING,DamageClass.PHYSICAL,Global.Roll(4+bonus,6),this);
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
							chosen.actor().TakeDamage(DamageType.BASHING,Forays.DamageClass.PHYSICAL,Global.Roll(2,6),this);
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
							string s = firstactor.the_name;
							firstactor.TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(1+bonus,6),this);
							if(M.actor[firsttile.row,firsttile.col] != null){
								firstactor.GetKnockedBack(full_line);
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
						B.Add("You fade from " + a.the_name + "'s awareness. ");
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
					a.TakeDamage(DamageType.COLD,DamageClass.MAGICAL,Global.Roll(5+bonus,6),this);
					if(M.actor[r,c] != null && Global.Roll(1,10) <= 8){
						B.Add(a.the_name + " is encased in ice. ",a);
						a.attrs[AttrType.FROZEN] = 15;
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
				spells[spell]++; //todo ...should this line just be removed?
				if(Spell.Level(spell) - TotalSkill(SkillType.MAGIC) > 0){
					if(HasFeat(FeatType.STUDENTS_LUCK)){
						if(Global.CoinFlip()){
							magic_penalty++;
							if(type == ActorType.PLAYER){
								B.Add("You feel drained. ");
							}
						}
						else{
							if(type == ActorType.PLAYER){
								B.Add("You feel lucky. "); //...punk
							}
						}
					}
					else{
						magic_penalty++;
						if(type == ActorType.PLAYER){
							B.Add("You feel drained. "); //todo maybe this should just use YouFeel()
						}
					}
				}
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
					if(DistanceFrom(t) == 2 && line[1].passable && line[1].actor() == null){
						moved = true;
						B.Add("You lunge! ");
						Move(line[1].row,line[1].col);
						attrs[AttrType.BONUS_COMBAT] += 4;
						Attack(0,t.actor());
						attrs[AttrType.BONUS_COMBAT] -= 4;
					}
					if(!moved){
						B.Add("The way is blocked! ");
						return false;
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
/*Tumble - (A, 200 energy) - You pick a tile within distance 2. If there is at least one passable tile between 
you and it(you CAN tumble past actors), you move to that tile. Additional effects: If you move past an actor, 
they lose sight of you and their turns_target_location is set to X - rand_function_of(stealth skill). (there's a good chance
they'll find you, then attack, but you will have still moved past them) ; You will automatically dodge the first arrow
that would hit you before your next turn.(it's still possible they'll roll 2 successes and hit you) ; Has the same
effect as standing still, if you're on fire or catching fire. */
				{
				List<Tile> line = GetTarget(false,2,false);
				Tile t = null;
				if(line != null){
					t = line.Last();
				}
				if(t != null && t.passable && t.actor() == null){
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
					return false;
				}
				//break;
				}
			case FeatType.ARCANE_HEALING: //25% fail rate for the 'failrate' feats
				if(magic_penalty < 20){
					if(curhp < maxhp){
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
					}
				}
				else{
					B.Add("Your magic reserves are empty! ");
					return false;
				}
				break;
			case FeatType.FORCE_OF_WILL:
				if(magic_penalty < 20){
					List<string> ls = new List<string>();
					List<SpellType> sp = new List<SpellType>();
					foreach(SpellType spell in Enum.GetValues(typeof(SpellType))){
						if(HasSpell(spell)){
							string s = Spell.Name(spell).PadRight(15) + Spell.Level(spell).ToString().PadLeft(3);
							s = s + FailRate(spell).ToString().PadLeft(9) + "%";
							s = s + Spell.Description(spell).PadLeft(34);
							ls.Add(s);
							sp.Add(spell);
						}
					}
					if(sp.Count > 0){
						string topborder = "------------------Level---Fail rate--------Description------------";
						int basefail = magic_penalty * 5;
						//if(!HasFeat(FeatType.ARMORED_MAGE)){
						//	basefail += Armor.AddedFailRate(armors.First.Value);
						//}
						basefail -= skills[SkillType.SPIRIT]*2;
						if(basefail > 100){
							basefail = 100;
						}
						if(basefail < 0){
							basefail = 0;
						}
						string bottomborder = "----Force of will fail rate: " + (basefail.ToString().PadLeft(2) + "%").PadRight(37,'-');
						int i = Select("Use force of will to cast which spell? ",topborder,bottomborder,ls);
						if(i != -1){
							if(!CastSpell(sp[i],true)){
								Q0();
								return true;
							}
							else{
								magic_penalty += 5;
								if(magic_penalty > 20){
									magic_penalty = 20;
								}
								B.Add("You drain your magic reserves. ");
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
				if(dir != -1 && TileInDirection(dir).IsTrap()){
					if(ActorInDirection(dir) != null){
						B.Add("There is " + ActorInDirection(dir).a_name + " in the way. ");
					}
					else{
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
				if(!t.passable){
					t = line.LastBeforeSolidTile();
				}
				if(t != null){
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
			if(HasAttr(AttrType.RESTING)){
				attrs[AttrType.RESTING] = 0;
			}
			attrs[AttrType.RUNNING] = 0;
			attrs[AttrType.AUTOEXPLORE] = 0;
			if(path != null && path.Count > 0){
				path.Clear();
			}
		}
		public bool StunnedThisTurn(){
			if(HasAttr(AttrType.STUNNED) && Global.OneIn(3)){
				int dir = Global.RandomDirection();
				if(!TileInDirection(dir).passable){
					B.Add(You("stagger") + " into " + TileInDirection(dir).the_name + ". ",this);
				}
				else{
					if(ActorInDirection(dir) != null){
						B.Add(You("stagger") + " into " + ActorInDirection(dir).the_name + ". ",this);
					}
					else{
						B.Add(You("stagger") + ". ",this);
						Move(TileInDirection(dir).row,TileInDirection(dir).col);
					}
				}
				QS();
				return true;
			}
			return false;
		}
		public void MakeNoise(){
			foreach(Actor a in ActorsWithinDistance(12,true)){
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
					if(los){
						a.target_location = tile();
					}
					else{
						a.FindPath(this);
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
			Screen.WriteStatsString(3,0,"Level: " + level + "  ");
			Screen.WriteStatsString(4,0,"Depth: " + M.current_level + "  ");
			Screen.WriteStatsString(5,0,"AC: " + ArmorClass() + "  ");
			int magic_item_lines = magic_items.Count;
			cstr cs = Weapon.StatsName(weapons.First.Value);
			cs.s = cs.s.PadRight(12);
			Screen.WriteStatsString(6,0,cs);
			cs = Armor.StatsName(armors.First.Value);
			cs.s = cs.s.PadRight(12);
			Screen.WriteStatsString(7,0,cs);
			int line = 8;
			foreach(MagicItemType m in magic_items){
				cs = MagicItem.StatsName(m);
				cs.s = cs.s.PadRight(12);
				Screen.WriteStatsString(line,0,cs);
				++line;
			}
			if(!Global.Option(OptionType.HIDE_COMMANDS)){
				string[] commandhints = new string[]{"[i]nventory ","[e]quipment ","[c]haracter ","SPECIAL",
					"Use [f]eat  ","Cast [z]    ","[s]hoot     ","[a]pply item","[g]et item  ","[r]est      ",
					"[w]alk      ","e[x]plore   ","[o]perate   "};
				if(light_radius > 0){
					commandhints[3] = "[t]orch off ";
				}
				else{
					commandhints[3] = "[t]orch on  ";
				}
				Color lettercolor = cyan_letters? Color.Cyan : Color.Gray;
				for(int i=0;i<commandhints.Length;++i){
					int open = commandhints[i].LastIndexOf('[');
					cstr front = new cstr(commandhints[i].Substring(0,open+1),Color.Gray);
					int close = commandhints[i].LastIndexOf(']');
					cstr middle = new cstr(commandhints[i].Substring(open+1,(close - open)-1),lettercolor);
					cstr end = new cstr(commandhints[i].Substring(close),Color.Gray);
					Screen.WriteString(12+i,0,new colorstring(front,middle,end));
				}
			}
			else{
				for(int i=8+magic_item_lines;i<Global.SCREEN_H;++i){
					Screen.WriteStatsString(i,0,"".PadRight(12));
				}
			}
			Screen.ResetColors();
		}
		public void DisplayStats(bool expand_weapons,bool expand_armors){
/*[i]nventory
[e]quipment
[c]haracter
[t]orch off
Use [f]eat
Cast [z]
[s]hoot			todo - here is the full list, to be completed when there's enough room.
[a]pply item
[g]et item
[d]rop item
[r]est
[w]alk
E[x]plore
[o]perate
[tab] Look
*/
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
		}
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
								Global.DisplayHelp(Help.Feats);
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
					Global.DisplayHelp(Help.Feats);
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
				int selection = Select("Learn which spell? ",topborder,new colorstring("".PadRight(COLS,'-'),Color.Gray),unknownstr,false,true,false);
				spells[unknown[selection]] = 1;
				learned.Add("You learn " + Spell.Name(unknown[selection]) + ". ");
				if(Global.quickstartinfo != null){
					Global.quickstartinfo.Add(unknown[selection].ToString());
				}
			}
			return learned;
		}
		public bool CanSee(int r,int c){ return CanSee(M.tile[r,c]); }
		public bool CanSee(PhysicalObject o){
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
					if(M.tile[i,j].passable){
						if(path_around_seen_traps && M.tile[i,j].IsTrap() && M.tile[i,j].name != "floor"){
							values[i,j] = -1;
						}
						else{
							values[i,j] = 0;
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
					if(M.tile[i,j].IsTrap() && M.tile[i,j].name != "floor"){
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
			for(int i=1;(i<=max_distance || max_distance==-1) && i<=COLS;++i){
				foreach(Tile t in TilesAtDistance(i)){
					if(t.type == TileType.STAIRS || t.type == TileType.CHEST
					|| t.type == TileType.GRENADE || t.type == TileType.FIREPIT
					|| t.type == TileType.QUICKFIRE || t.type == TileType.STALAGMITE
					|| t.IsShrine() || t.inv != null){
						if(CanSee(t)){
							interesting_targets.Add(t);
						}
					}
					if(lookmode && t.IsTrap() && !interesting_targets.Contains(t) && CanSee(t) && t.name != "floor"){
						interesting_targets.Add(t);
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
						//return M.tile[r,c];
						return GetBestLine(target); //todo: this might break since it doesn't extend to a wall, change this.
					}
					target_idx = interesting_targets.IndexOf(target);
				}
			}
			bool first_iteration = true;
			bool done=false; //when done==true, we're ready to return 'result'
			while(!done){
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
								B.DisplayNow("You sense " + M.actor[r,c].a_name + ". ");
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
						line = GetBestLine(r,c);
						//Tile last_good = tile();
						foreach(Tile t in line){
							if(t.row != row || t.col != col){
								colorchar cch = mem[t.row,t.col];
/*colorchar cch;
cch.color = mem[t.row,t.col].color;
cch.bgcolor = mem[t.row,t.col].bgcolor;
cch.c = mem[t.row,t.col].c;*/
								if(t.row == r && t.col == c){
									if(!blocked){
										cch.bgcolor = Color.Green;
										if(Global.LINUX){ //no bright bg in terminals
											cch.bgcolor = Color.DarkGreen;
										}
										if(cch.color == cch.bgcolor){
											cch.color = Color.Black;
											//cch.color = Color.Yellow;
											//cch.bgcolor = Color.DarkBlue;
											//cch.c = '!';
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
											//cch.color = Color.Yellow;
											//cch.bgcolor = Color.DarkBlue;
											//cch.c = '@';
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
				case ' ':
					done = true;
					break;
				case (char)13:
					if(r != row || c != col){
						if(HasBresenhamLine(r,c)){
							if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
								target = M.actor[r,c];
							}
							//result = M.tile[r,c];
							if(radius == 0){
								result = GetBestExtendedLine(r,c).ToFirstSolidTile();
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
								if(nearby_actors || radius == -1){ //radius -1 means "allow targeting the ground", basically.
									result = GetBestLine(r,c);
									if(max_distance > 0 && result.Count > max_distance+1){
										result = result.GetRange(0,max_distance+1);
									}
								}
								else{ //same as for radius 0
									result = GetBestExtendedLine(r,c).ToFirstSolidTile();
									if(max_distance > 0 && result.Count > max_distance+1){
										result = result.GetRange(0,max_distance+1);
									}
								}
							}
						}
						else{
							//result = FirstSolidTileInLine(M.tile[r,c]);
							//result = M.tile[r,c];
							result = GetBestExtendedLine(r,c).ToFirstSolidTile();
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
							result = GetBestLine(this);
							done = true;
						}
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
/*		public List<Tile> GetTarget2Temp(bool lookmode,int max_distance,bool start_at_interesting_target,int radius){
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
			colorchar[,] mem = new colorchar[ROWS,COLS];
			List<Tile> line = new List<Tile>();
			List<Tile> oldline = new List<Tile>();
			Tile last_good = tile();
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
						return GetBestLine(target); //todo: this might break since it doesn't extend to a wall, change this.
					}
					target_idx = interesting_targets.IndexOf(target);
				}
			}
			bool first_iteration = true;
			bool done=false; //when done==true, we're ready to return 'result'
			while(!done){
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
								B.DisplayNow("You sense " + M.actor[r,c].a_name + ". ");
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
						line = GetBestLine(r,c);
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
											//cch.color = Color.Yellow;
											//cch.bgcolor = Color.DarkBlue;
											//cch.c = '!';
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
											//cch.color = Color.Yellow;
											//cch.bgcolor = Color.DarkBlue;
											//cch.c = '@';
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
								if(!blocked){
									last_good = t;
								}
								if(t.seen && !t.passable && (t.row != r || t.col != c)){
									blocked=true;
								}
							}
							oldline.Remove(t);
						}
						if(radius > 0 && last_good != null){
							foreach(Tile t in last_good.TilesAtDistance(radius)){
								if(!line.Contains(t)){
									colorchar cch = mem[t.row,t.col];
									cch.bgcolor = Color.DarkCyan;
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
				//		oldline = line;
						oldline = new List<Tile>(line);
						if(radius > 0 && last_good != null){
							foreach(Tile t in last_good.TilesAtDistance(radius)){
								oldline.AddUnique(t);
							}
						}
					}
					first_iteration = false;
					M.tile[r,c].Cursor();
				}
				if(lookmode && Screen.MapChar(r,c).c == ' ' && Screen.BackgroundColor == ConsoleColor.Black){
					//testing for foregroundcolor == black does NOT work
					//testing for backgroundcolor == black DOES work.
					Screen.WriteMapChar(r,c,' ');
					Console.SetCursorPosition(c+Global.MAP_OFFSET_COLS,r+Global.MAP_OFFSET_ROWS);
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
				case ' ':
					done = true;
					break;
				case (char)13:
					if(r != row || c != col){
						if(HasBresenhamLine(r,c)){
							if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
								target = M.actor[r,c];
							}
							//result = M.tile[r,c];
							if(radius == 0){
								result = GetBestExtendedLine(r,c).ToFirstSolidTile();
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
								if(nearby_actors || radius == -1){ //radius -1 means "allow targeting the ground", basically.
									result = GetBestLine(r,c);
									if(max_distance > 0 && result.Count > max_distance+1){
										result = result.GetRange(0,max_distance+1);
									}
								}
								else{ //same as for radius 0
									result = GetBestExtendedLine(r,c).ToFirstSolidTile();
									if(max_distance > 0 && result.Count > max_distance+1){
										result = result.GetRange(0,max_distance+1);
									}
								}
							}
						}
						else{
							//result = FirstSolidTileInLine(M.tile[r,c]);
							//result = M.tile[r,c];
							result = GetBestExtendedLine(r,c).ToFirstSolidTile();
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
							result = GetBestLine(this);
							done = true;
						}
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
					if(radius > 0){
						foreach(Tile t in last_good.TilesWithinDistance(radius,true)){
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
		public List<Tile> GetTarget3Temp(bool lookmode,int max_distance,bool start_at_interesting_target,int radius){
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
						//return M.tile[r,c];
						return GetBestLine(target); //todo: this might break since it doesn't extend to a wall, change this.
					}
					target_idx = interesting_targets.IndexOf(target);
				}
			}
			bool first_iteration = true;
			bool done=false; //when done==true, we're ready to return 'result'
			while(!done){
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
								B.DisplayNow("You sense " + M.actor[r,c].a_name + ". ");
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
						line = GetBestLine(r,c);
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
											//cch.color = Color.Yellow;
											//cch.bgcolor = Color.DarkBlue;
											//cch.c = '!';
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
											//cch.color = Color.Yellow;
											//cch.bgcolor = Color.DarkBlue;
											//cch.c = '@';
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
							foreach(Tile t in M.tile[r,c].TilesAtDistance(radius)){
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
				//		oldline = line;
						oldline = new List<Tile>(line);
						if(radius > 0){
							foreach(Tile t in M.tile[r,c].TilesAtDistance(radius)){
								oldline.AddUnique(t);
							}
						}
					}
					first_iteration = false;
					M.tile[r,c].Cursor();
				}
				if(lookmode && Screen.MapChar(r,c).c == ' ' && Screen.BackgroundColor == ConsoleColor.Black){
					//testing for foregroundcolor == black does NOT work
					//testing for backgroundcolor == black DOES work.
					Screen.WriteMapChar(r,c,' ');
					Console.SetCursorPosition(c+Global.MAP_OFFSET_COLS,r+Global.MAP_OFFSET_ROWS);
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
				case ' ':
					done = true;
					break;
				case (char)13:
					if(r != row || c != col){
						if(HasBresenhamLine(r,c)){
							if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
								target = M.actor[r,c];
							}
							//result = M.tile[r,c];
							if(radius == 0){
								result = GetBestExtendedLine(r,c).ToFirstSolidTile();
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
								if(nearby_actors || radius == -1){ //radius -1 means "allow targeting the ground", basically.
									result = GetBestLine(r,c);
									if(max_distance > 0 && result.Count > max_distance+1){
										result = result.GetRange(0,max_distance+1);
									}
								}
								else{ //same as for radius 0
									result = GetBestExtendedLine(r,c).ToFirstSolidTile();
									if(max_distance > 0 && result.Count > max_distance+1){
										result = result.GetRange(0,max_distance+1);
									}
								}
							}
						}
						else{
							//result = FirstSolidTileInLine(M.tile[r,c]);
							//result = M.tile[r,c];
							result = GetBestExtendedLine(r,c).ToFirstSolidTile();
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
							result = GetBestLine(this);
							done = true;
						}
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
			if(i < ROWS){
				Screen.WriteMapString(i+1,0,"".PadRight(COLS));
			}
			if(no_ask){
				B.DisplayNow(message);
				return -1;
			}
			else{
				int result = GetSelection(message,strings.Count,no_cancel,easy_cancel);
				M.RedrawWithStrings();
				return result;
			}
		}
		public int Select(string message,colorstring top_border,colorstring bottom_border,List<colorstring> strings,bool no_ask,bool no_cancel,bool easy_cancel){
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
				int result = GetSelection(message,strings.Count,no_cancel,easy_cancel);
				M.RedrawWithStrings();
				return result;
			}
		}
		public int GetSelection(string s,int count,bool no_cancel,bool easy_cancel){
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
		private static AttackInfo[] attack = new AttackInfo[26];
		static AttackList(){
			attack[0] = new AttackInfo(100,1,DamageType.PIERCING,"& ^bites *");
			attack[1] = new AttackInfo(100,1,DamageType.PIERCING,"& ^bites *");
			attack[2] = new AttackInfo(100,2,DamageType.PIERCING,"& ^bites *");
			attack[3] = new AttackInfo(100,2,DamageType.NORMAL,"& ^hits *");
			attack[4] = new AttackInfo(100,3,DamageType.NORMAL,"& ^hits *");
			attack[5] = new AttackInfo(100,1,DamageType.SLASHING,"& ^scratches *");
			attack[6] = new AttackInfo(100,2,DamageType.COLD,"& hits * with a blast of cold");
			attack[7] = new AttackInfo(100,4,DamageType.COLD,"& releases a burst of cold");
			attack[8] = new AttackInfo(200,2,DamageType.NORMAL,"& lunges forward and ^hits *");
			attack[9] = new AttackInfo(100,3,DamageType.PIERCING,"& ^bites *");
			attack[10] = new AttackInfo(100,0,DamageType.NONE,"& lashes * with a tentacle");
			attack[11] = new AttackInfo(100,4,DamageType.NORMAL,"& ^hits *"); 
			attack[12] = new AttackInfo(100,4,DamageType.BASHING,"& ^slams *");
			attack[13] = new AttackInfo(120,3,DamageType.NORMAL,"& extends a tentacle and ^hits *");
			attack[14] = new AttackInfo(120,1,DamageType.NORMAL,"& extends a tentacle and drags * closer");
			attack[15] = new AttackInfo(100,5,DamageType.BASHING,"& ^slams *");
			attack[16] = new AttackInfo(100,3,DamageType.PIERCING,"& ^bites *");
			attack[17] = new AttackInfo(100,3,DamageType.SLASHING,"& ^claws *");
			attack[18] = new AttackInfo(150,4,DamageType.FIRE,"& breathes fire");
			attack[19] = new AttackInfo(100,1,DamageType.NORMAL,"& ^hit *"); //the player's default attack
			attack[20] = new AttackInfo(100,2,DamageType.NORMAL,"& ^pokes *");
			attack[21] = new AttackInfo(100,1,DamageType.NORMAL,"& slimes *");
			attack[22] = new AttackInfo(100,2,DamageType.SLASHING,"& ^claws *");
			attack[23] = new AttackInfo(100,2,DamageType.NORMAL,"& touches *");
			attack[24] = new AttackInfo(100,0,DamageType.NONE,"& ^hits *");
			attack[25] = new AttackInfo(100,2,DamageType.SLASHING,"& ^claws *");
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
				return new AttackInfo(attack[25]);
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
					return new AttackInfo(attack[1]);
				case 1:
					return new AttackInfo(attack[10]);
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
		public static int MaxRank(FeatType type){
			switch(type){
			case FeatType.QUICK_DRAW:
			case FeatType.SILENT_CHAINMAIL:
			case FeatType.BOILING_BLOOD:
			case FeatType.DISTRACT:
			case FeatType.DANGER_SENSE:
				return 2;
			case FeatType.FULL_DEFENSE:
			case FeatType.ENDURING_SOUL:
				return 4;
			case FeatType.NECK_SNAP:
			case FeatType.MASTERS_EDGE:
				return 5;
			case FeatType.LETHALITY:
			case FeatType.LUNGE:
			case FeatType.DRIVE_BACK:
			case FeatType.ARMORED_MAGE:
			case FeatType.TUMBLE:
			case FeatType.STUDENTS_LUCK:
			case FeatType.ARCANE_HEALING:
			case FeatType.FORCE_OF_WILL:
			case FeatType.CONVICTION:
			case FeatType.FEEL_NO_PAIN:
			case FeatType.DISARM_TRAP:
				return 3;
			default:
				return 0;
			}
		}
		public static bool IsActivated(FeatType type){
			switch(type){
			case FeatType.LUNGE:
			case FeatType.DRIVE_BACK:
			case FeatType.TUMBLE:
			case FeatType.ARCANE_HEALING:
			case FeatType.FORCE_OF_WILL:
			case FeatType.DISARM_TRAP:
			case FeatType.DISTRACT:
			case FeatType.DANGER_SENSE:
				return true;
			case FeatType.QUICK_DRAW:
			case FeatType.LETHALITY:
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
			case FeatType.ARCANE_HEALING:
				return "Arcane healing";
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
	}
}

