using System;
using System.Collections.Generic;
namespace Forays{
	public class AttackInfo{
		public int cost;
		public int dice;
		public DamageType type;
		public string desc;
		public AttackInfo(int cost_,int dice_,DamageType type_,string desc_){
			cost=cost_;
			dice=dice_;
			type=type_;
			desc=desc_;
		}
	}
	/*keen eyes -half check. still needs trap detection bonus.
tough(?)
	-as if
spirit skill
	-matters in lots of places, i guess.
stealth skill
	-matters in AI methods, at least.


danger sense(on)
	 -matters in map.draw...eesh.
quick draw	...
	-matters in Wield command
silent chain	...
	-matters in Stealth() method
neck snap ...
	-matters in Attack.


-when displaying skill level, the format should be base + bonus, with bonus colored differently, just so it's perfectly clear.
*/
	public class Actor : PhysicalObject{
		public ActorType type{get; private set;}
		public int maxhp{get; private set;}
		public int curhp{get; private set;}
		public int speed{get; private set;}
		public int xp{get; private set;}
		public int level{get; private set;}
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
		public Tile player_seen;
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
			//proto[ActorType.PLAYER] = new Actor(); //unused!
			proto[ActorType.GOBLIN] = new Actor(ActorType.GOBLIN,"goblin",'g',ConsoleColor.Green,20,100,5,1,0);
			//
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
			row = r;
			col = c; //todo:UPDATE CONSTRUCTORS TO INCLUDE ALL MEMBERS
			player_seen = null;
			time_of_last_action = 0;
			recover_time = 0;
			weapons = new LinkedList<WeaponType>(a.weapons);
			armors = new LinkedList<ArmorType>(a.armors);
		}
		public Actor(ActorType type_,string name_,char symbol_,ConsoleColor color_,int maxhp_,int speed_,int xp_,int level_,int light_radius_){
			type = type_;
			name = name_;
			the_name = "the " + name;
			a_name = "a " + name; //todo: fix this
			if(name=="you"){
				the_name = "you";
				a_name = "you";
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
			player_seen = null;
			time_of_last_action = 0;
			recover_time = 0;
			weapons.AddFirst(WeaponType.NO_WEAPON);
			armors.AddFirst(ArmorType.NO_ARMOR);
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
		public void UpdateRadius(int from,int to){ UpdateRadius(from,to,false); }
		public void UpdateRadius(int from,int to,bool change){
			if(from > 0){
				for(int i=row-from;i<=row+from;++i){
					for(int j=col-from;j<=col+from;++j){
						if(i>0 && i<ROWS-1 && j>0 && j<COLS-1){
							if(!M.tile[i,j].opaque && HasLOS(i,j)){ //for now, i'm keeping HasLOS here instead of HasBresenham
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
							if(!M.tile[i,j].opaque && HasLOS(i,j)){
								M.tile[i,j].light_value++;
							}
						}
					}
				}
			}
			if(change){
				if(to < 6){ //hack: this is a temporary fix so that you don't permenantly give off light after being on fire
					light_radius = 0;
				}
				else{
					light_radius = to;
				}
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
		public void Input(){
			bool return_after_recovery = false;
			if(HasAttr(AttrType.DEFENSIVE_STANCE)){
				attrs[AttrType.DEFENSIVE_STANCE] = 0;
			}
			if(HasAttr(AttrType.PARALYZED)){
				attrs[AttrType.PARALYZED]--;
				B.Add(the_name + " can't move! ");
				if(type != ActorType.PLAYER){ //handled differently for the player: since the map still needs to be drawn,
					Q1();						// this is handled in InputHuman().
					return_after_recovery = true; //the message is still printed, of course.
				}
			}
			if(curhp < maxhp){
				if(HasAttr(AttrType.REGENERATING) && time_of_last_action < Q.turn){
					curhp += attrs[AttrType.REGENERATING];
					if(curhp > maxhp){
						curhp = maxhp;
					}
					if(type == ActorType.PLAYER || player.CanSee(this)){
						B.Add(You("regenerate") + ". ");
					}
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
				TakeDamage(DamageType.POISON,Global.Roll(1,3)-1,null);
			}
			if(HasAttr(AttrType.ON_FIRE) && time_of_last_action < Q.turn){
				B.Add(YouAre() + " on fire! ");
				TakeDamage(DamageType.FIRE,Global.Roll(attrs[AttrType.ON_FIRE],6),null);
			}
			if(return_after_recovery){
				return;
			}
			if(type==ActorType.PLAYER){
				InputHuman();
			}
			else{
				InputAI();
			}
			if(HasAttr(AttrType.ON_FIRE) && attrs[AttrType.ON_FIRE] < 5 && time_of_last_action < Q.turn){
				if(attrs[AttrType.ON_FIRE] > light_radius){
					UpdateRadius(attrs[AttrType.ON_FIRE],attrs[AttrType.ON_FIRE]+1);
				}
				attrs[AttrType.ON_FIRE]++;
			}
			if(HasAttr(AttrType.CATCHING_FIRE) && time_of_last_action < Q.turn){
				if(light_radius == 0){
					UpdateRadius(0,1);
				}
				attrs[AttrType.CATCHING_FIRE] = 0;
				attrs[AttrType.ON_FIRE] = 1;
			}
			time_of_last_action = Q.turn; //todo: this might eventually need a slight rework for 0-time turns
		}
		private char ConvertInput(ConsoleKeyInfo k){
			switch(k.Key){
			case ConsoleKey.UpArrow:
			case ConsoleKey.D8:
			case ConsoleKey.NumPad8:
				return '8';
			case ConsoleKey.DownArrow:
			case ConsoleKey.D2:
			case ConsoleKey.NumPad2:
				return '2';
			case ConsoleKey.LeftArrow:
			case ConsoleKey.D4:
			case ConsoleKey.NumPad4:
				return '4';
			case ConsoleKey.Clear:
			case ConsoleKey.D5:
			case ConsoleKey.NumPad5:
				return '5';
			case ConsoleKey.RightArrow:
			case ConsoleKey.D6:
			case ConsoleKey.NumPad6:
				return '6';
			case ConsoleKey.Home:
			case ConsoleKey.D7:
			case ConsoleKey.NumPad7:
				return '7';
			case ConsoleKey.PageUp:
			case ConsoleKey.D9:
			case ConsoleKey.NumPad9:
				return '9';
			case ConsoleKey.End:
			case ConsoleKey.D1:
			case ConsoleKey.NumPad1:
				return '1';
			case ConsoleKey.PageDown:
			case ConsoleKey.D3:
			case ConsoleKey.NumPad3:
				return '3';
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
			B.Print(false);
			Cursor();
			if(HasAttr(AttrType.PARALYZED)){
				Q1();
				return;
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
			/*bool alt = false;
			bool ctrl = false;
			if((command.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt){
				alt = true;
			}
			if((command.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control){
				ctrl = true;
			}*/
			switch(ch){
			case '7':
			case '8':
			case '9':
			case '4':
			case '6':
			case '1':
			case '2'://don't forget support for alt-dir
			case '3':
				int dir = ch - 48; //ascii 0-9 are 48-57
				if(dir > 0){
					if(ActorInDirection(dir)!=null){
						if(F[0] == SpellType.NO_SPELL){
							Attack(0,ActorInDirection(dir));
						}
						else{
							CastSpell(F[0],TileInDirection(dir));
						}
					}
					else{
						if(TileInDirection(dir).passable){
							if(Global.Option(OptionType.OPEN_CHESTS) && TileInDirection(dir).type==TileType.CHEST){
								if(StunnedThisTurn()){
									break;
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
									break;
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
				break;
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
							B.Add("You put out some of the fire. "); //todo: better message?
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
				if(Global.Option(OptionType.LAST_TARGET) && target!=null && DistanceFrom(target)==1){ //since you can't fire
					target = null;										//at adjacent targets anyway.
				}
				Tile t = GetTarget();
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
				break;
				}
			case 'Z':
				//todo: for now, this selects from 10 spells.
				{
				List<string> ls = new List<string>();
				foreach(SpellType s in Enum.GetValues(typeof(SpellType))){
					ls.Add(Spell.Name(s));
				}
				ls.RemoveRange(10,15);
				int i = Select("Cast which spell? ",ls);
				B.Add("You almost cast spell " + i + ". ");
				Q1();
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
					DisplayStats(true,false); //todo: display cursor in correct position
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
					break;
				}
				else{
					UpdateOnEquip(old_weapon,weapons.First.Value);
				}
				Q1();
				break;
				}
			case 'A':
				{
				if(StunnedThisTurn()){
					break;
				}
/*				int i=0;
				foreach(ArmorType a in Enum.GetValues(typeof(ArmorType))){
					Screen.WriteMapString(i,0,Armor.StatsName(a));
					++i;
				}*/
				DisplayStats(false,true);
				Console.ReadKey(true);
				Q1();
				break;
				}
			case 'l':
				GetTarget(true);
				Q0();
				break;
			case 'T':
				if(StunnedThisTurn()){
					break;
				}
				if(light_radius==0){
					if(HasAttr(AttrType.ENHANCED_TORCH)){
						UpdateRadius(LightRadius(),Global.MAX_LIGHT_RADIUS,true);
					}
					else{
						UpdateRadius(LightRadius(),6,true); //normal light radius
					}
					B.Add("You activate your everburning torch. ");
				}
				else{
					UpdateRadius(LightRadius(),attrs[AttrType.ON_FIRE],true); //this call has a hack associated with it
					B.Add("You deactivate your everburning torch. ");
				}
				Q1();
				break;
			case 'P':
				Q0();
				break;
			case '=':
				Q0();
				break;
			case '?':
				Q0();
				break;
			case 'Q':
				Environment.Exit(0);
				break;
			case 'B':
				B.DisplayLogtempfunction();
				Q0();
				break;
			case 'V':
				//has_spell[SpellType.SHADOWSIGHT] = 1;
				//CastSpell(SpellType.SHADOWSIGHT);
				if(HasAttr(AttrType.LOW_LIGHT_VISION)){
					attrs[AttrType.LOW_LIGHT_VISION] = 0;
				}
				else{
					attrs[AttrType.LOW_LIGHT_VISION]++;
				}
				Q0();
				break;
			case 'v':
				{
				Tile t = GetTarget();
				if(t != null){
					Screen.AnimateProjectile(GetExtendedBresenhamLine(t.row,t.col),new colorchar(ConsoleColor.Yellow,'$'));
				}
				Q1();
				break;
				}
			case 'X':
				{
				ConsoleKeyInfo command2 = Console.ReadKey(true);
				Console.Write(command2.Key);
				Q0();
				break;
				}
			case 'x':
				{
				Console.CursorVisible = false;
				colorchar cch;
				cch.c = ' ';
				cch.color = ConsoleColor.Black;
				cch.bgcolor = ConsoleColor.Black;
				foreach(Tile t in M.AllTiles()){
					t.seen = false;
					Screen.WriteMapChar(t.row,t.col,cch);
				}
				Console.CursorVisible = true;
				Q0();
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
		public void InputAI(){
			//check for regeneration
			//cansee(player) check, also player_seen
			//and player_seen distance 0 check
			//bool path_step? am i using pathing for anybody?
			if(target != null){
				if(CanSee(target)){ //this is where stealth matters. seeing the tile it's on doesn't always mean you can see
					ActiveAI(); //the monster there. 
				}
				else{
					SeekAI();
				}
			}
			else{
				IdleAI();
			}
		}
		public void ActiveAI(){
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
						else{ //todo: remove 'line up shot' message if immobilized?
							if(AI_Sidestep(target)){
								B.Add(the_name + " tries to line up a shot. ");
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
					else{ //todo: same as above
						if(AI_Sidestep(target)){
							B.Add(the_name + " tries to line up a shot. ");
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
					if(curhp <= 20){ //todo: check these numbers
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
								if(TileInDirection(dir).passable && ActorInDirection(dir) == null){
									tilelist.Add(TileInDirection(dir));
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
											B.IfSeenAdd(this,target,"You don't move far. ");
										}
										else{
											B.IfSeenAdd(this,target,target.the_name + " doesn't move far. ");
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
			case ActorType.FIRE_DRAKE:
				if(false){
					//todo: if player has fire resist upgrades, handle those first, here.
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
				break;
			case ActorType.LASHER_FUNGUS:
				QS();
				break;
			case ActorType.FIRE_DRAKE:
				//todo: needs to hunt player down
				break;
			default:
				if(player_seen != null){
					if(DistanceFrom(player_seen) == 1 && M.actor[player_seen.row,player_seen.col] != null){
						if(HasAttr(AttrType.IMMOBILIZED)){
							B.Add(You("break") + " free. ");
							attrs[AttrType.IMMOBILIZED] = 0;
							QS();
						}
						else{
							if(M.actor[player_seen.row,player_seen.col].HasAttr(AttrType.IMMOBILIZED)){
								if(HasAttr(AttrType.HUMANOID_INTELLIGENCE) && M.actor[player_seen.row,player_seen.col].symbol == symbol){
									B.Add(You("break") + M.actor[player_seen.row,player_seen.col].the_name + " free. ");
									M.actor[player_seen.row,player_seen.col].attrs[AttrType.IMMOBILIZED] = 0;
									QS();
								}
								else{
									QS();
								}
							}
							else{
								Move(player_seen.row,player_seen.col); //swap places
								player_seen = null;
								QS();
							}
						}
					}
					else{
						if(AI_Step(player_seen)){
							QS();
							if(DistanceFrom(player_seen) == 0){
								player_seen = null;
							}
						}
						else{ //could not move, end turn.
							QS();
						}
					}
				}
				else{
					if(DistanceFrom(target) <= 2){ //if close enough, you can still hear them. or at least that's the idea.
						AI_Step(target);
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
			switch(type){
			case ActorType.ORC_WARMAGE:
				break;
			case ActorType.FIRE_DRAKE:
				break;
			default: //simply end turn
				QS();
				break;
			}
		}
		public bool AI_Step(PhysicalObject obj){ return AI_Step(obj,false); }
		public bool AI_Step(PhysicalObject obj,bool flee){
			if(HasAttr(AttrType.IMMOBILIZED)){
				B.Add(You("break") + " free. ");
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
				if(colchange == -1){
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
			bool cw = Global.CoinFlip();
			dirs.Add(RotateDirection(dirs[0],cw));
			dirs.Add(RotateDirection(dirs[0],!cw)); //building a list of directions to try: first the primary direction,
			cw = Global.CoinFlip(); 				//then the ones next to it, then the ones next to THOSE(in random order)
			dirs.Add(RotateDirection(RotateDirection(dirs[0],cw),cw));
			dirs.Add(RotateDirection(RotateDirection(dirs[0],!cw),!cw)); //this completes the list of 5 directions.
			foreach(int i in dirs){
				if(AI_MoveOrOpen(i)){
					return true;
				}
			}
			return false;
		}
		public int RotateDirection(int dir,bool clockwise){
			switch(dir){
			case 7:
				return clockwise?8:4;
			case 8:
				return clockwise?9:7;
			case 9:
				return clockwise?6:8;
			case 4:
				return clockwise?7:1;
			case 5:
				return 5;
			case 6:
				return clockwise?3:9;
			case 1:
				return clockwise?4:2;
			case 2:
				return clockwise?1:3;
			case 3:
				return clockwise?2:6;
			default:
				return 0;
			}
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
			AttackInfo info = AttackList.Attack(type,attack_idx);
			int plus_to_hit = TotalSkill(SkillType.COMBAT);
			if(HasAttr(AttrType.BLESSED)){
				plus_to_hit += 10;
			}
			bool hit = a.IsHit(plus_to_hit);
			if(a.HasAttr(AttrType.DEFENSIVE_STANCE) && Global.CoinFlip()){
				hit = false;
			}
			//todo: handle neck snap here after figuring out how stealth/detection works. i think it'll work as long as you
			// hit them - probably with a bonus to hit because they can't see you.
			if(attack_idx==0 && (type==ActorType.FROSTLING || type==ActorType.FIRE_DRAKE)){
				hit = true; //hack! these are the 2 'area' attacks that always hit
			}
			string s = info.desc + ". ";
			if(hit){
				bool crit = false;
				int pos = s.IndexOf('&');
				if(pos != -1){
					s = s.Substring(0,pos) + the_name + s.Substring(pos+1);
				}
				pos = s.IndexOf('^');
				if(pos != -1){
					string sc = "";
					if(Global.Roll(1,20) == 20){
						crit = true;
						sc = "critically ";
					}
					s = s.Substring(0,pos) + sc + s.Substring(pos+1);
				}
				pos = s.IndexOf('*');
				if(pos != -1){
					s = s.Substring(0,pos) + a.the_name + s.Substring(pos+1);
				}
				if(this==player || a==player || player.CanSee(this) || player.CanSee(a)){
					B.Add(s);
				}
				int dice = info.dice;
				if(weapons.First.Value != WeaponType.NO_WEAPON){
					dice = Weapon.Damage(weapons.First.Value);
				}
				int dmg;
				if(crit){
					dmg = dice * 6;
				}
				else{
					dmg = Global.Roll(dice,6);
				}
				int r = a.row;
				int c = a.col;
				a.TakeDamage(info.type,dmg,this);
				if(M.actor[r,c] != null){
					if(HasAttr(AttrType.FIRE_HIT) || attrs[AttrType.ON_FIRE] >= 3){
						if(this==player || a==player || player.CanSee(this) || player.CanSee(a)){
							B.Add(a.YouAre() + " burned. ");
						}
						a.TakeDamage(DamageType.FIRE,Global.Roll(1,6),this);
					}
				}
				if(HasAttr(AttrType.COLD_HIT) && attack_idx==0 && M.actor[r,c] != null){
					//hack: only applies to attack 0
					if(this==player || a==player || player.CanSee(this) || player.CanSee(a)){
						B.Add(a.YouAre() + " chilled. ");
					}
					a.TakeDamage(DamageType.COLD,Global.Roll(1,6),this);
				}
				if(HasAttr(AttrType.POISON_HIT) && M.actor[r,c] != null){
					if(this==player || a==player || player.CanSee(this) || player.CanSee(a)){
						B.Add(a.YouAre() + " poisoned. ");
					}
					a.attrs[AttrType.POISONED]++; //todo: make sure this is how poison works
				}
				if(HasAttr(AttrType.PARALYSIS_HIT) && attack_idx==0 && M.actor[r,c] != null){
					//hack: only applies to attack 0
					if(this==player || a==player || player.CanSee(this) || player.CanSee(a)){
						B.Add(a.YouAre() + " paralyzed. ");
					}
					a.attrs[AttrType.PARALYZED]++; //todo: make sure this is how paralysis works
				}
				if(HasAttr(AttrType.FORCE_HIT) && M.actor[r,c] != null){
					//todo: mace of force here
				}
			}
			else{
				if(a.HasAttr(AttrType.DEFENSIVE_STANCE)){
					//make an attack against a random enemy next to a
					List<Actor> list = a.ActorsWithinDistance(1,true);
					list.Remove(this); //don't consider yourself or the original target
					if(list.Count > 0){
						B.Add(a.You("deflect") + " the attack. ");
						return Attack(attack_idx,list[Global.Roll(1,list.Count)-1]);
					}
					//this would currently enter an infinite loop if two adjacent things used it at the same time
				}
				if(this==player || a==player || player.CanSee(this) || player.CanSee(a)){
					if(s == "& lunges forward and ^hits *. "){
						B.Add(the_name + " lunges forward and misses " + a.the_name + ". ");
					}
					else{
						if(s == "& hits * with a blast of cold. "){
							B.Add(the_name + " nearly hits " + a.the_name + " with a blast of cold. ");
						}
						else{
							if(s.Substring(0,20) == "& extends a tentacle"){
								B.Add(the_name + " misses " + a.the_name + " with a tentacle. ");
							}
							else{
								B.Add(You("miss",true) + " " + a.the_name + ". ");
							}
						}
					}
				}
				if(HasAttr(AttrType.DRIVE_BACK_ON)){
					if(!a.HasAttr(AttrType.IMMOBILIZED)){
						a.AI_Step(this,true); //todo: should you follow them after driving them back?
					}
				}
			}
			Q.Add(new Event(this,info.cost));
			return hit;
		}
		public void FireArrow(PhysicalObject obj){
			if(StunnedThisTurn()){
				return;
			}
			Actor a = FirstActorInLine(obj);
			int mod = -30; //bows have base accuracy 45%
			if(HasAttr(AttrType.KEEN_EYES)){
				mod = -20; //keen eyes makes it 55%
			}
			mod += TotalSkill(SkillType.COMBAT);
			if(a != null){
				bool hit = a.IsHit(mod);
				if(a.HasAttr(AttrType.TUMBLING)){
					hit = false;
					a.attrs[AttrType.TUMBLING] = 0;
				}
				if(hit){
					if(Global.Roll(1,20) == 20){
						B.Add(You("critically hit") + " " + a.the_name + " with an arrow. ");
						a.TakeDamage(DamageType.NORMAL,18,this); //max(3d6)
					}
					else{
						B.Add(You("hit") + " " + a.the_name + " with an arrow. ");
						a.TakeDamage(DamageType.NORMAL,Global.Roll(3,6),this);
					}
				}
				else{
					B.Add(You("miss",true) + " " + a.the_name + " with an arrow. ");
				}
			}
			else{
				Tile t = M.tile[obj.row,obj.col];
				B.Add(You("hit") + " " + t.the_name + " with an arrow. ");
			}
			Q1();
		}
		public bool IsHit(int plus_to_hit){
			if(Global.Roll(1,100) + plus_to_hit <= 25){ //base hit chance is 75%
				return false;
			}
			return true;
		}
		public void TakeDamage(DamageType dmgtype,int dmg,Actor source){
			bool damage_dealt = false;
			//todo: attr: tough?
			if(HasAttr(AttrType.INVULNERABLE)){
				dmg = 0;
			}
			switch(dmgtype){
			case DamageType.NORMAL:
				if(dmg > 0){
					curhp -= dmg;
					damage_dealt = true;
				}
				else{
					B.Add(YouAre() + " undamaged. ");
				}
				break;
			case DamageType.MAGIC:
				if(dmg > 0){
					curhp -= dmg;
					damage_dealt = true;
				}
				else{
					B.Add(YouAre() + " unharmed. ");
				}
				break;
			case DamageType.FIRE:
				{
				if(HasAttr(AttrType.IMMUNE_FIRE)){
					dmg = 0;
				}
				int div = 1;
				for(int i=attrs[AttrType.RESIST_FIRE];i>0;--i){
					div = div * 2;
				}
				dmg = dmg / div;
				if(dmg > 0){
					curhp -= dmg;
					damage_dealt = true;
					if(type == ActorType.SHAMBLING_SCARECROW){
						speed = 50;
						attrs[AttrType.ON_FIRE]++;
						if(player.CanSee(this)){
							B.Add(the_name + " catches fire! ");
						}
					}
				}
				else{
					B.Add(YouAre() + " unburnt. ");
				}
				break;
				}
			case DamageType.COLD:
				{
				if(HasAttr(AttrType.IMMUNE_COLD)){
					dmg = 0;
					B.Add(YouAre() + " unharmed. ");
				}
				int div = 1;
				for(int i=attrs[AttrType.RESIST_COLD];i>0;--i){
					div = div * 2;
				}
				dmg = dmg / div;
				if(dmg > 0){
					curhp -= dmg;
					damage_dealt = true;
				}
				else{
					B.Add(YouAre() + " unharmed. ");
				}
				break;
				}
			case DamageType.ELECTRIC:
				{
				int div = 1;
				for(int i=attrs[AttrType.RESIST_ELECTRICITY];i>0;--i){
					div = div * 2;
				}
				dmg = dmg / div;
				if(dmg > 0){
					curhp -= dmg;
					damage_dealt = true;
				}
				else{
					B.Add(YouAre() + " unharmed. ");
				}
				break;
				}
			case DamageType.POISON:
				if(HasAttr(AttrType.UNDEAD) || HasAttr(AttrType.CONSTRUCT)){
					dmg = 0;
				}
				if(dmg > 0){
					curhp -= dmg;
					damage_dealt = true;
					if(type == ActorType.PLAYER){
						B.Add("You feel the poison coursing through your veins! ");
					}
					else{
						if(Global.Roll(1,5) == 5 && player.CanSee(this)){
							B.Add(the_name + " shudders. ");
						}
					}
				}
				break;
			case DamageType.HEAL:
				curhp += dmg;
				if(curhp > maxhp){
					curhp = maxhp;
				}
				break;
			}
			if(damage_dealt){
				if(HasAttr(AttrType.MAGICAL_BLOOD)){
					recover_time = Q.turn + 200;
				}
				else{
					recover_time = Q.turn + 500;
				}
				attrs[AttrType.RESTING] = 0;
				if(source != null){
					if(type != ActorType.PLAYER){
						target = source;
						if(light_radius > 0 && HasLOS(source.row,source.col)){//for enemies who can't see in darkness
							player_seen = M.tile[source.row,source.col];
						}
					}
				}
				if(HasAttr(AttrType.SPORE_BURST) && !HasAttr(AttrType.COOLDOWN_1)){
					attrs[AttrType.COOLDOWN_1]++;
					Q.Add(new Event(this,(Global.Roll(1,5)+1)*100,AttrType.COOLDOWN_1));
					B.Add(You("retaliate") + " with a burst of spores! ");
					foreach(Actor a in ActorsWithinDistance(8)){
						if(!a.HasAttr(AttrType.UNDEAD) && !a.HasAttr(AttrType.CONSTRUCT) && !a.HasAttr(AttrType.SPORE_BURST)){
							if(HasLOS(a.row,a.col)){
								B.Add("The spores hit " + a.the_name + ". ");
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
				if(HasAttr(AttrType.HOLY_SHIELDED) && source != null){
					B.Add(Your() + " holy shield burns " + source.the_name + ". ");
					int amount = Global.Roll(1,6);
					if(amount >= source.curhp){
						amount = source.curhp - 1;
					}
					source.TakeDamage(DamageType.FIRE,amount,this); //doesn't yet prevent loops involving 2 holy shields.
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
					if(player.CanSee(this)){
						if(HasAttr(AttrType.UNDEAD) || HasAttr(AttrType.CONSTRUCT)){
							B.Add(the_name + " is destroyed. ");
						}
						else{
							B.Add(the_name + " dies. ");
						}
					}
					//todo: give xp here
					if(LightRadius() > 0){
						UpdateRadius(LightRadius(),0);
					}
					Q.KillEvents(this,EventType.ANY_EVENT);
					M.RemoveTargets(this);
					M.actor[row,col] = null;
				}
			}
			else{
				if(HasFeat(FeatType.FEEL_NO_PAIN) && damage_dealt && curhp < 20){ //todo: does this need a msg?
					attrs[AttrType.INVULNERABLE]++;
					Q.Add(new Event(this,500,AttrType.INVULNERABLE));
				}
				if(magic_items.Contains(MagicItemType.CLOAK_OF_DISAPPEARANCE) && damage_dealt && dmg >= curhp){
					B.PrintAll();
					M.Draw();
					B.DisplayNow("Your cloak becomes translucent. Use your cloak to escape?(Y/N): ");
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
							break;
						default:
							break;
						}
					}
					magic_items.Remove(MagicItemType.CLOAK_OF_DISAPPEARANCE);
				}
			}
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
				B.Add(YouAre() + " knocked back. ");
				if(HasAttr(AttrType.IMMOBILIZED)){
					attrs[AttrType.IMMOBILIZED] = 0;
					B.Add(YouAre() + " no longer immobilized. ");
				}
				Move(next.row,next.col);
			}
			else{
				int r = row;
				int c = col;
				bool immobilized = HasAttr(AttrType.IMMOBILIZED);
				if(!next.passable){
					B.Add(YouAre() + " knocked into " + next.the_name + ". ");
					TakeDamage(DamageType.NORMAL,Global.Roll(1,6),source);
				}
				else{
					B.Add(YouAre() + " knocked into " + M.actor[next.row,next.col].the_name + ". ");
					TakeDamage(DamageType.NORMAL,Global.Roll(1,6),source);
					M.actor[next.row,next.col].TakeDamage(DamageType.NORMAL,Global.Roll(1,6),source);
				}
				if(immobilized && M.actor[r,c] != null){
					B.Add(YouAre() + " no longer immobilized. ");
				}
			}
			return true;
		}
		public bool CastSpell(SpellType spell){ return CastSpell(spell,null); }
		public bool CastSpell(SpellType spell,PhysicalObject obj){ //returns false if targeting is canceled.
			if(StunnedThisTurn()){ //todo: eventually this will be moved to the last possible second
				return true; //returns true because turn was used up. 
			}
			if(!HasSpell(spell)){
				return false;
			}
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
							B.Add("Sparks fly from " + Your() + " fingers. ");
							Q1();
							return true;
						}
						else{
							B.Add("Your luck pays off. ");
						}
					}
					else{
						B.Add("Sparks fly from " + Your() + " fingers. "); //or 'you fail to concentrate hard enough'
						Q1(); //or 'the shaman's mouth and fingers move, but nothing happens'
						return true; //or 'the shaman seems to concentrate hard, but nothing happens'
					}
				}
			}
			else{
				bonus = 1;
			}
			switch(spell){
			case SpellType.SHINE:
				if(!HasAttr(AttrType.ENHANCED_TORCH)){
					B.Add("Your torch begins to shine brightly. ");
					attrs[AttrType.ENHANCED_TORCH]++;
					if(light_radius > 0){
						UpdateRadius(LightRadius(),Global.MAX_LIGHT_RADIUS,true);
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
					Actor a = FirstActorInLine(t);
					if(a != null){
						if(name == "you"){
							B.Add("The missile hits " + a.the_name + ". ");
						}
						else{
							B.Add(the_name + " casts Magic Missile at you. "); //todo: add animations, remove "at you"
						}
						a.TakeDamage(DamageType.MAGIC,Global.Roll(1+bonus,6),this); //i think that will flow properly.
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
					if(a != null){
						if(name != "you"){
							B.Add(the_name + " casts Force Palm at you. "); //todo: descriptive style for ALL spells?
						}
						else{
							B.Add("You strike " + a.the_name + " with your palm. ");
						}
						string s = a.the_name;
						List<Tile> line = GetExtendedBresenhamLine(a.row,a.col);
						int idx = line.IndexOf(M.tile[a.row,a.col]);
						Tile next = line[idx+1];
						a.TakeDamage(DamageType.MAGIC,Global.Roll(1+bonus,6),this);
						if(Global.Roll(1,10) <= 6){
							if(M.actor[t.row,t.col] != null){
								a.GetKnockedBack(this);
							}
							else{
								if(!next.passable){
									B.Add(s + "'s corpse is knocked into " + next.the_name + ". ");
								}
								else{
									if(M.actor[next.row,next.col] != null){
										B.Add(s + "'s corpse is knocked into " + M.actor[next.row,next.col].the_name + ". ");
										M.actor[next.row,next.col].TakeDamage(DamageType.NORMAL,Global.Roll(1,6),this);
									}
								}
							}
						}
					}
					else{
						if(t.passable){
							B.Add("You strike the air with your palm. ");
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
								B.Add(You("step") + " through a rip in reality. ");
								Move(a,b);
								break;
							}
						}
					}
				}
				break;
			case SpellType.IMMOLATE: //perhaps being on fire scales linearly: each level is a bit of damage and +1 light_rad
				if(t == null){
					t = GetTarget();
				}
				if(t != null){
					Actor a = M.actor[t.row,t.col];
					if(a != null){
						if(!a.HasAttr(AttrType.RESIST_FIRE)){
							if(name == "you"){
								B.Add(a.the_name + " starts to catch fire. ");
							}
							else{
								B.Add(the_name + " casts Immolate. You start to catch fire! ");
							}
							a.attrs[AttrType.CATCHING_FIRE]++;
						}
						else{
							if(a.name == "you"){
								B.Add(the_name + " casts Immolate. You shrug off the flames. ");
							}
							else{
								B.Add(a.the_name + " fails to ignite. ");
							}
						}
					}
					else{
						B.Add(You("throw") + " flames. ");
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
					Actor a = FirstActorInLine(t);
					if(a != null){
						if(name == "you"){
							B.Add("The icy blast hits " + a.the_name + ". ");
						}
						else{
							B.Add(the_name + " casts Icy Blast at you. ");
						}
						a.TakeDamage(DamageType.COLD,Global.Roll(2+bonus,6),this);
					}
					else{
						B.Add("The icy blast hits " + t.the_name + ". ");
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
					Actor a = M.actor[t.row,t.col];
					if(a != null){
						if(name == "you"){
							B.Add("You project flames onto " + a.the_name + ". ");
						}
						else{
							B.Add(the_name + " casts Burning Hands. You are seared. ");
						}
						a.TakeDamage(DamageType.FIRE,Global.Roll(3+bonus,6),this);
						if(M.actor[t.row,t.col] != null && Global.Roll(1,10) <= 2){
							B.Add(You("start") + " to catch fire! ");
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
					Actor a = M.actor[t.row,t.col];
					if(a != null){
						if(name == "you"){
							B.Add("Ice forms around " + a.the_name + ". ");
						}
						else{
							B.Add(the_name + " casts Freeze. Ice forms around you. ");
						}
						int r = a.row;
						int c = a.col;
						a.TakeDamage(DamageType.COLD,Global.Roll(1+bonus,6),this);
						if(M.actor[r,c] != null && !a.HasAttr(AttrType.IMMOBILIZED) && Global.Roll(1,10) <= 6){
							B.Add(a.the_name + " is immobilized. ");
							a.attrs[AttrType.IMMOBILIZED]++;
							int duration = Global.Roll(1,3) * 100;
							Q.Add(new Event(a,duration,AttrType.IMMOBILIZED,a.the_name + " is no longer immobilized. "));
						}
					}
					else{
						B.Add("Some ice forms on " + t.the_name + ". ");
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
					Actor a = FirstActorInLine(t);
					if(a != null){
						if(name == "you"){
							B.Add("A wave of sound hits " + a.the_name + ". ");
						}
						else{
							B.Add(the_name + " casts Sonic Boom. A wave of sound hits you. ");
						}
						int r = a.row;
						int c = a.col;
						a.TakeDamage(DamageType.MAGIC,Global.Roll(2+bonus,6),this);
						if(Global.Roll(1,10) <= 5 && M.actor[r,c] != null && !M.actor[r,c].HasAttr(AttrType.STUNNED)){
							B.Add(a.the_name + " is stunned. ");
							a.attrs[AttrType.STUNNED]++;
							int duration = (Global.Roll(1,4)+2) * 100;
							Q.Add(new Event(a,duration,AttrType.STUNNED,the_name + " is no longer stunned. "));
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
				B.Add(You("crackle") + " with electricity. ");
				while(targets.Count > 0){
					int idx = Global.Roll(1,targets.Count) - 1;
					Actor a = targets[idx];
					targets.Remove(a);
					B.Add("Electricity hits " + a.the_name + ". ");
					a.TakeDamage(DamageType.ELECTRIC,Global.Roll(3+bonus,6),this);
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
					Actor a = FirstActorInLine(t);
					if(a != null){
						if(name != "you"){
							B.Add(the_name + " casts Shock. ");
						}
						B.Add("Electricity arcs to " + a.the_name + ". ");
						a.TakeDamage(DamageType.ELECTRIC,Global.Roll(3+bonus,6),this);
					}
					else{
						B.Add("Electricity arcs between your fingers. ");
					}
				}
				else{
					return false;
				}
				break;
			case SpellType.SHADOWSIGHT:
				B.Add("Your eyes pierce the darkness. ");
				if(!HasAttr(AttrType.DARKVISION)){
					attrs[AttrType.DARKVISION]++;
					int duration = (Global.Roll(2,4)+15) * 100;
					Q.Add(new Event(this,duration,AttrType.DARKVISION,"Your darkvision wears off. "));
				}
				break;
			case SpellType.RETREAT: //this is a player-only spell for now because it uses player_seen to track position
				if(player_seen == null){
					player_seen = M.tile[row,col];
					B.Add("You create a rune of transport on " + M.tile[row,col].the_name + ". ");
				}
				else{
					if(M.actor[player_seen.row,player_seen.col] == null && player_seen.passable){
						B.Add("You activate your rune of transport. ");
						Move(player_seen.row,player_seen.col);
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
					B.Add("Fwoosh! ");
					List<Actor> targets = new List<Actor>();
					for(int i=t.row-2;i<=t.row+2;++i){
						for(int j=t.col-2;j<=t.col+2;++j){
							if(M.actor[i,j] != null && M.actor[i,j] != this){
								targets.Add(M.actor[i,j]);
							}
						}
					}
					while(targets.Count > 0){
						int idx = Global.Roll(1,targets.Count) - 1;
						Actor ac = targets[idx];
						targets.Remove(ac);
						B.Add("The explosion hits " + ac.the_name + ". ");
						ac.TakeDamage(DamageType.FIRE,Global.Roll(3+bonus,6),this);
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
						i = GetDirection(true);
					}
					t = TileInDirection(i);
					if(t != null){
						if(t.type == TileType.WALL){
							while(!t.passable){
								if(t.row == 0 || t.row == ROWS-1 || t.col == 0 || t.col == COLS-1){
									break;
								}
								t = t.TileInDirection(i);
							}
							if(t.passable && M.actor[t.row,t.col] == null){
								B.Add("You travel through the passage. ");
								Move(t.row,t.col);
							}
							else{
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
					List<Tile> line = GetExtendedBresenhamLine(t.row,t.col);
					for(int i=0;i<3;++i){ //hits thrice
						Actor firstactor = null;
						Actor nextactor = null;
						Tile firsttile = null;
						Tile nexttile = null;
						foreach(Tile tile in line){
							if(M.actor[tile.row,tile.col] != null && M.actor[tile.row,tile.col] != this){
								int idx = line.IndexOf(tile);
								firsttile = tile;
								firstactor = M.actor[tile.row,tile.col];
								nexttile = line[idx+1];
								nextactor = M.actor[nexttile.row,nexttile.col];
								break;
							}
						}
						if(firstactor != null){
							string s = firstactor.the_name;
							firstactor.TakeDamage(DamageType.MAGIC,Global.Roll(1+bonus,6),this);
							if(Global.Roll(1,10) <= 9){
								if(M.actor[firsttile.row,firsttile.col] != null){
									firstactor.GetKnockedBack(line);
								}
								else{
									if(!nexttile.passable){
										B.Add(s + "'s corpse is knocked into " + nexttile.the_name + ". ");
									}
									else{
										if(nextactor != null){
											B.Add(s + "'s corpse is knocked into " + nextactor.the_name + ". ");
											nextactor.TakeDamage(DamageType.NORMAL,Global.Roll(1,6),this);
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
					Actor a = FirstActorInLine(t);
					if(a != null){
						if(name == "you"){
							B.Add("You direct destructive energies toward " + a.the_name + ". ");
						}
						else{
							B.Add(the_name + " casts Disintegrate. " + the_name + " directs destructive energies toward you. ");
						}
						a.TakeDamage(DamageType.MAGIC,Global.Roll(8+bonus,6),this);
					}
					else{
						if(t.type == TileType.WALL || t.type == TileType.DOOR_C || t.type == TileType.DOOR_O || t.type == TileType.CHEST){
							B.Add(t.the_name + " turns to dust. ");
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
				B.Add("A massive ice storm surrounds " + the_name + ". ");
				while(targets.Count > 0){
					int idx = Global.Roll(1,targets.Count) - 1;
					Actor a = targets[idx];
					targets.Remove(a);
					B.Add("The blizzard hits " + a.the_name + ". ");
					int r = a.row;
					int c = a.col;
					a.TakeDamage(DamageType.COLD,Global.Roll(5+bonus,6),this);
					if(M.actor[r,c] != null && Global.Roll(1,10) <= 8){
						B.Add(a.the_name + " is immobilized. ");
						a.attrs[AttrType.IMMOBILIZED]++;
						int duration = Global.Roll(1,3) * 100;
						Q.Add(new Event(a,duration,AttrType.IMMOBILIZED,a.the_name + " is no longer immobilized. "));
					}
				}
				break;
				}
			case SpellType.BLESS:
				if(!HasAttr(AttrType.BLESSED)){
					B.Add(the_name + " casts Bless. ");
					B.Add(You("shine") + " briefly with inner light. ");
					attrs[AttrType.BLESSED]++;
					Q.Add(new Event(this,400,AttrType.BLESSED));
				}
				else{
					B.Add(YouAre() + " already blessed! ");
					return false;
				}
				break;
			case SpellType.MINOR_HEAL:
				TakeDamage(DamageType.HEAL,Global.Roll(4,6),null);
				B.Add("A bluish glow surrounds " + the_name + ". ");
				break;
			case SpellType.HOLY_SHIELD:
				if(!HasAttr(AttrType.HOLY_SHIELDED)){
					B.Add("A fiery halo appears above " + the_name + ". ");
					attrs[AttrType.HOLY_SHIELDED]++;
					int duration = (Global.Roll(3,2)+1) * 100;
					Q.Add(new Event(this,duration,AttrType.HOLY_SHIELDED,the_name + "'s halo fades. "));
				}
				else{
					B.Add(Your() + " holy shield is already active. ");
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
			//todo: lots here. name?
		}
		public bool UseFeat(FeatType feat){
			switch(feat){
			case FeatType.SPIN_ATTACK:
				{
				int dice = Weapon.Damage(weapons.First.Value);
				bool hit = false;
				foreach(Tile t in TilesAtDistance(1)){
					if(t.Actor() != null){
						hit = true;
						Actor a = t.Actor();
						B.Add("You hit " + a.the_name + ". ");
						a.TakeDamage(DamageType.NORMAL,Global.Roll(dice,6),this);
					}
				}
				foreach(Tile t in TilesAtDistance(2)){
					if(t.Actor() != null){
						hit = true;
						Actor a = t.Actor();
						B.Add("You hit " + a.the_name + ". "); //todo: message check
						a.TakeDamage(DamageType.MAGIC,TotalSkill(SkillType.MAGIC),this);
					}
				}
				if(!hit){
					B.Add("You perform a spin attack. ");
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
				if(t != null && t.Actor() != null){
					bool moved = false;
					foreach(Tile neighbor in t.NeighborsBetween(row,col)){
						if(neighbor.passable && neighbor.Actor() == null){
							Move(neighbor.row,neighbor.col);
							moved = true;
							B.Add("You lunge! ");
							attrs[AttrType.BONUS_COMBAT] += 3;
							Attack(0,t.Actor());
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
				}
				else{
					attrs[AttrType.DRIVE_BACK_ON] = 1;
				}
				break;
			case FeatType.TUMBLE:
				//todo:
/*Tumble - (A, 200 energy) - You pick a tile within distance 2. If there is at least one passable tile between 
you and it(you CAN tumble past actors), you move to that tile. Additional effects: If you move past an actor, 
they lose sight of you and their turns_player_seen is set to X - rand_function_of(stealth skill). (there's a good chance
they'll find you, then attack, but you will have still moved past them) ; You will automatically dodge the first arrow
that would hit you before your next turn.(it's still possible they'll roll 2 successes and hit you) ; Has the same
effect as standing still, if you're on fire or catching fire. */
				{
				if(HasAttr(AttrType.IMMOBILIZED)){
					B.Add("You can't perform this feat while immobilized. ");
					return false;
				}
				Tile t = GetTarget(2);
				if(t != null && t.Actor() != null){
					List<Actor> actors_moved_past = new List<Actor>();
					bool moved = false;
					foreach(Tile neighbor in t.NeighborsBetween(row,col)){
						if(neighbor.Actor() != null){
							actors_moved_past.Add(neighbor.Actor());
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
										B.Add("You put out some of the fire. "); //todo: better message?
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
			case FeatType.ARCANE_HEALING: //okay, 25% fail rate for these.
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
				}
				else{
					attrs[AttrType.DANGER_SENSE_ON] = 1;
				}
				break;
			default:
				return false;
			}
			Q1();
			return true;
		}
		public bool StunnedThisTurn(){
			if(HasAttr(AttrType.STUNNED) && Global.Roll(1,5) == 5){
				int dir = Global.RandomDirection();
				if(HasAttr(AttrType.IMMOBILIZED)){
					B.Add(You("almost fall") + " over. ");
				}
				else{
					if(!TileInDirection(dir).passable){
						B.Add(You("stagger") + " into " + TileInDirection(dir).the_name + ". ");
					}
					else{
						if(ActorInDirection(dir) != null){
							B.Add(You("stagger") + " into " + ActorInDirection(dir).the_name + ". ");
						}
						else{
							Move(TileInDirection(dir).row,TileInDirection(dir).col);
							B.Add(You("stagger") + ". ");
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
			colorstring cs = Weapon.StatsName(weapons.First.Value);
			cs.s = ("W: " + cs.s).PadRight(12); //todo: the W: won't actually fit. well, the A: won't, with full plate.
			Screen.WriteStatsString(6,0,cs);
			if(expand_weapons){ //this can easily be extended to handle a variable number of weapons
				weapon_lines = 5;
				int i = 7;
				foreach(WeaponType w in weapons){
					if(w != weapons.First.Value){
						cs = Weapon.StatsName(w);
						cs.s = ("   " + cs.s).PadRight(12);
						Screen.WriteStatsString(i,0,cs);
						++i;
					}
				}
				
			}
			cs = Armor.StatsName(armors.First.Value);
			cs.s = ("A: " + cs.s).PadRight(12); //does not fit, todo, augh.
			Screen.WriteStatsString(6+weapon_lines,0,cs);
			if(expand_armors){
				armor_lines = 3;
				int i = 7 + weapon_lines;
				foreach(ArmorType a in armors){
					if(a != armors.First.Value){
						cs = Armor.StatsName(a);
						cs.s = ("   " + cs.s).PadRight(12);
						Screen.WriteStatsString(i,0,cs);
						++i;
					}
				}
			}
			Screen.WriteStatsString(6+weapon_lines+armor_lines,0,"~~~~~~~~~~~~");
			for(int i=7+weapon_lines+armor_lines;i<11+weapon_lines+armor_lines;++i){
				Screen.WriteStatsString(i,0,"".PadRight(12));
			}
			Console.ResetColor();
			Console.CursorVisible = true;
		}
		public bool CanSee(int r,int c){ return CanSee(M.tile[r,c]); }
		public bool CanSee(PhysicalObject o){
			if(IsWithinSightRangeOf(o.row,o.col) || M.tile[o.row,o.col].IsLit()){
				if(HasLOS(o.row,o.col)){
					if(o is Actor){
						//todo: stealth check here
						//if((o as Actor).IsStealthed() or whatever
						return true;
					}
					else{
						return true;
					}
				}
			}
			Actor a = o as Actor;
			if(a != null){
				if(HasAttr(AttrType.DETECTING_MONSTERS) && DistanceFrom(a) <= 6){
					return true;
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
			int dy = Math.Abs(row-r);
			int dx = Math.Abs(col-c);
			int maxd = Math.Max(dy,dx);
			if(maxd <= 3){
				return true;
			}
			if(maxd <= 6 && HasAttr(AttrType.LOW_LIGHT_VISION)){
				return true;
			}
			if(maxd <= 12 && HasAttr(AttrType.DARKVISION)){
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
		public int GetDirection(){ return GetDirection("Which direction? ",false); }
		public int GetDirection(bool orth){ return GetDirection("Which direction? ",orth); }
		public int GetDirection(string s,bool orth){
			B.DisplayNow(s);
			ConsoleKeyInfo command;
			char ch;
			while(true){
				command = Console.ReadKey(true);
				ch = ConvertInput(command);
				if(Global.Option(OptionType.VI_KEYS)){
					ch = ConvertVIKeys(ch);
				}
				int i = (int)Char.GetNumericValue(ch);
				if(i>=1 && i<=9 && i!=5){
					if(!orth || i%2==0){ //in orthogonal mode, return only even dirs
						return i;
					}
				}
				if(ch == (char)27){ //escape
					return -1;
				}
				if(ch == ' '){
					return -1;
				}
			}
		}
		public Tile GetTarget(){ return GetTarget(false,-1); }
		public Tile GetTarget(bool lookmode){ return GetTarget(lookmode,-1); }
		public Tile GetTarget(int max_distance){ return GetTarget(false,max_distance); }
		public Tile GetTarget(bool lookmode,int max_distance){ //todo: might need a M.Draw, to handle targeting from inventory or spell screen?
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
			colorchar[,] mem = new colorchar[ROWS,COLS];
			List<Tile> line = new List<Tile>();
			List<Tile> oldline = new List<Tile>();
			for(int i=0;i<ROWS;++i){
				for(int j=0;j<COLS;++j){
					mem[i,j] = Screen.MapChar(i,j);
				}
			}
			if(lookmode){
				B.DisplayNow("Move the cursor to look around. ");
			}
			else{
				B.DisplayNow("Move cursor to choose target, then press Enter. ");
			}
			if(lookmode || target==null || !CanSee(target) || DistanceFrom(target)>max_distance){
				r = row;
				c = col;
				Cursor();
			}
			else{
				r = target.row;
				c = target.col;
				if(Global.Option(OptionType.LAST_TARGET)){
					return M.tile[r,c];
				}
				target.Cursor();	
			}
			bool done=false; //when done==true, we're ready to return 'result'
			while(!done){
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
				case (char)27:
					done = true;
					break;
				case (char)13: //todo: i'm pretty sure you can hit yourself with spells atm. check for self HERE to fix that.
					if(HasBresenhamLine(r,c)){ //uses bresenham until i want symmetrical firing too
						if(M.actor[r,c] != null && CanSee(M.actor[r,c])){
							target = M.actor[r,c];
						}
						result = M.tile[r,c];
					}
					done = true;
					break;
				case ' ':
					if(lookmode){
						done = true;
					}
					break;
				default:
					break;
				}
				if(!done){
					Screen.ResetColors();
					if(r == row && c == col){
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
					else{
						if(CanSee(M.tile[r,c])){
							string s = "";
							int count = 0;
							if(M.actor[r,c] != null && CanSee(M.actor[r,c])){ //todo: test this code
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
						foreach(Tile t in line){
							if(t.row != row || t.col != col){
								colorchar cch = mem[t.row,t.col];
								if(t.row == r && t.col == c){
									if(!blocked){
										if(cch.color == ConsoleColor.Green){
											cch.color = ConsoleColor.Black;
										}
										cch.bgcolor = ConsoleColor.Green;
										Screen.WriteMapChar(t.row,t.col,cch);
									}
									else{
										if(cch.color == ConsoleColor.Red){
											cch.color = ConsoleColor.Black;
										}
										cch.bgcolor = ConsoleColor.Red;
										Screen.WriteMapChar(t.row,t.col,cch);
									}
								}
								else{
									if(!blocked){
										if(cch.color == ConsoleColor.DarkGreen){
											cch.color = ConsoleColor.Black;
										}
										cch.bgcolor = ConsoleColor.DarkGreen;
										Screen.WriteMapChar(t.row,t.col,cch);
									}
									else{
										if(cch.color == ConsoleColor.DarkRed){
											cch.color = ConsoleColor.Black;
										}
										cch.bgcolor = ConsoleColor.DarkRed;
										Screen.WriteMapChar(t.row,t.col,cch);
									}
									if(t.seen && !t.passable){
										blocked=true;
									}
								}
							}
							oldline.Remove(t);
						}
						foreach(Tile t in oldline){
							Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
						}
						oldline = line;
						Console.CursorVisible = true;
					}
					M.tile[r,c].Cursor();
				}
				if(done){
					Console.CursorVisible = false;
					foreach(Tile t in line){
						Screen.WriteMapChar(t.row,t.col,mem[t.row,t.col]);
					}
					Console.CursorVisible = true;
				}
			}
			return result;
		}
		public int Select(string message,List<string> strings){ return Select(message,"".PadLeft(COLS,'-'),strings,false,false); }
		public int Select(string message,string top_border,List<string> strings,bool no_ask,bool no_cancel){
			Screen.WriteMapString(0,0,top_border);
			char letter = 'a';
			int i=1;
			foreach(string s in strings){
				string s2 = "[" + letter + "] " + s;
				Screen.WriteMapString(i,0,s2.PadRight(COLS));
				letter++;
				i++;
			}
			Screen.WriteMapString(i,0,"".PadRight(COLS,'-'));
			Screen.WriteMapString(i+1,0,"".PadRight(COLS));
			if(no_ask){
				return -1;
			}
			else{
				return GetSelection(message,strings.Count,no_cancel);
			}
		}
		public int GetSelection(string s,int count,bool no_cancel){
			B.DisplayNow(s);
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
	}
	public static class AttackList{ //consider more descriptive attacks, such as the zealot smashing you with a mace
		private static AttackInfo[] attack = new AttackInfo[20];
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
			attack[10] = new AttackInfo(100,0,DamageType.NORMAL,"& lashes * with a tentacle");
			attack[11] = new AttackInfo(100,4,DamageType.NORMAL,"& ^hits *");
			attack[12] = new AttackInfo(100,4,DamageType.NORMAL,"& ^slams *");
			attack[13] = new AttackInfo(120,3,DamageType.NORMAL,"& extends a tentacle and ^hits *");
			attack[14] = new AttackInfo(120,1,DamageType.NORMAL,"& extends a tentacle and drags * closer");
			attack[15] = new AttackInfo(100,6,DamageType.NORMAL,"& ^slams *");
			attack[16] = new AttackInfo(100,3,DamageType.NORMAL,"& ^bites *");
			attack[17] = new AttackInfo(100,3,DamageType.NORMAL,"& ^claws *");
			attack[18] = new AttackInfo(150,4,DamageType.FIRE,"& breathes fire");
			attack[19] = new AttackInfo(100,1,DamageType.NORMAL,"& ^hit *");
		}
		public static AttackInfo Attack(ActorType type,int num){
			switch(type){
			case ActorType.PLAYER:
				return attack[19];
			case ActorType.RAT:
				return attack[0];
			case ActorType.GOBLIN:
				return attack[3];
			case ActorType.LARGE_BAT:
				switch(num){
				case 0:
					return attack[0];
				case 1:
					return attack[5];
				default:
					return null;
				}
			case ActorType.SHAMBLING_SCARECROW:
				return attack[17];
			case ActorType.SKELETON:
				return attack[3];
			case ActorType.GOBLIN_ARCHER:
				return attack[3];
			case ActorType.WOLF:
				return attack[2];
			case ActorType.FROSTLING:
				switch(num){
				case 0:
					return attack[3];
				case 1:
					return attack[6];
				case 2:
					return attack[7];
				default:
					return null;
				}
			case ActorType.GOBLIN_SHAMAN:
				return attack[3];
			case ActorType.ZOMBIE:
				switch(num){
				case 0:
					return attack[8];
				case 1:
					return attack[9];
				default:
					return null;
				}
			case ActorType.DIRE_RAT:
				return attack[1];
			case ActorType.ROBED_ZEALOT:
				return attack[4];
			case ActorType.WORG:
				return attack[2];
			case ActorType.CARRION_CRAWLER:
				switch(num){
				case 0:
					return attack[10];
				case 1:
					return attack[1];
				default:
					return null;
				}
			case ActorType.OGRE:
				return attack[11];
			case ActorType.PHASE_SPIDER:
				return attack[1];
			case ActorType.STONE_GOLEM:
				return attack[12];
			case ActorType.ORC_WARMAGE:
				return attack[4];
			case ActorType.LASHER_FUNGUS:
				switch(num){
				case 0:
					return attack[13];
				case 1:
					return attack[14];
				default:
					return null;
				}
			case ActorType.CORPSETOWER_BEHEMOTH:
				return attack[15];
			case ActorType.FIRE_DRAKE:
				switch(num){
				case 0:
					return attack[16];
				case 1:
					return attack[17];
				case 2:
					return attack[18];
				default:
					return null;
				}
			default:
				return null;
			}
		}
	}
}

