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
namespace Forays{
	public class Item : PhysicalObject{
		public ConsumableType type{get;set;}
		public int quantity{get;set;}
		public bool ignored{get;set;} //whether autoexplore and autopickup should ignore this item
		public bool do_not_stack{get;set;} //whether the item should be combined with other stacks. used for mimic items too.
		public bool revealed_by_light{get;set;}

		public static Dictionary<ConsumableType,string> unIDed_name = new Dictionary<ConsumableType,string>();
		public static Dict<ConsumableType,bool> identified = new Dict<ConsumableType, bool>();

		private static Dictionary<ConsumableType,Item> proto= new Dictionary<ConsumableType,Item>();
		public static Item Prototype(ConsumableType type){ return proto[type]; }
		//public static Map M{get;set;} //inherited
		//public static Buffer B{get;set;}
		//public static Queue Q{get;set;}
		//public static Actor player{get;set;}
		static Item(){
			Define(ConsumableType.HEALING,"potion~ of healing",'!',Color.White);
			Define(ConsumableType.REGENERATION,"potion~ of regeneration",'!',Color.White);
			Define(ConsumableType.STONEFORM,"potion~ of stoneform",'!',Color.White);
			Define(ConsumableType.VAMPIRISM,"potion~ of vampirism",'!',Color.White);
			Define(ConsumableType.BRUTISH_STRENGTH,"potion~ of brutish strength",'!',Color.White);
			Define(ConsumableType.ROOTS,"potion~ of roots",'!',Color.White);
			Define(ConsumableType.VIGOR,"potion~ of vigor",'!',Color.White);
			Define(ConsumableType.SILENCE,"potion~ of silence",'!',Color.White);
			Define(ConsumableType.CLOAKING,"potion~ of cloaking",'!',Color.White);
			Define(ConsumableType.BLINKING,"scroll~ of blinking",'?',Color.White);
			Define(ConsumableType.PASSAGE,"scroll~ of passage",'?',Color.White);
			Define(ConsumableType.TIME,"scroll~ of time",'?',Color.White);
			Define(ConsumableType.DETECT_MONSTERS,"scroll~ of detect monsters",'?',Color.White);
			Define(ConsumableType.MAGIC_MAP,"scroll~ of magic map",'?',Color.White);
			Define(ConsumableType.SUNLIGHT,"scroll~ of sunlight",'?',Color.White);
			Define(ConsumableType.DARKNESS,"scroll~ of darkness",'?',Color.White);
			Define(ConsumableType.REPAIR,"scroll~ of repair",'?',Color.White);
			Define(ConsumableType.CALLING,"scroll~ of calling",'?',Color.White);
			Define(ConsumableType.TRAP_CLEARING,"scroll~ of trap clearing",'?',Color.White);
			Define(ConsumableType.FREEZING,"orb~ of freezing",'*',Color.White);
			Define(ConsumableType.FLAMES,"orb~ of flames",'*',Color.White);
			Define(ConsumableType.FOG,"orb~ of fog",'*',Color.White);
			Define(ConsumableType.DETONATION,"orb~ of detonation",'*',Color.White);
			Define(ConsumableType.BREACHING,"orb~ of breaching",'*',Color.White);
			Define(ConsumableType.SHIELDING,"orb~ of shielding",'*',Color.White);
			Define(ConsumableType.TELEPORTAL,"orb~ of teleportal",'*',Color.White);
			Define(ConsumableType.PAIN,"orb~ of pain",'*',Color.White);
			Define(ConsumableType.BANDAGE,"bandage~",'{',Color.White);
			Define(ConsumableType.BLAST_FUNGUS,"blast fungus",'"',Color.Red);
			proto[ConsumableType.BLAST_FUNGUS].do_not_stack = true;
		}
		private static void Define(ConsumableType type_,string name_,char symbol_,Color color_){
			proto[type_] = new Item(type_,name_,symbol_,color_);
		}
		public Item(){}
		public Item(ConsumableType type_,string name_,char symbol_,Color color_){
			type = type_;
			quantity = 1;
			ignored = false;
			do_not_stack = false;
			revealed_by_light = false;
			name = name_;
			the_name = "the " + name;
			switch(name[0]){
			case 'a':
			case 'e':
			case 'i':
			case 'o':
			case 'u':
			case 'A':
			case 'E':
			case 'I':
			case 'O':
			case 'U':
				a_name = "an " + name;
				break;
			default:
				a_name = "a " + name;
				break;
			}
			symbol = symbol_;
			color = color_;
			row = -1;
			col = -1;
			light_radius = 0;
		}
		public Item(Item i,int r,int c){
			type = i.type;
			quantity = 1;
			ignored = false;
			do_not_stack = false;
			revealed_by_light = false;
			name = i.name;
			a_name = i.a_name;
			the_name = i.the_name;
			symbol = i.symbol;
			color = i.color;
			row = r;
			col = c;
			light_radius = i.light_radius;
		}
		public static Item Create(ConsumableType type,int r,int c){
			Item i = null;
			if(Global.BoundsCheck(r,c)){
				if(M.tile[r,c].inv == null){
					i = new Item(proto[type],r,c);
					if(i.light_radius > 0){
						i.UpdateRadius(0,i.light_radius);
					}
					M.tile[r,c].inv = i;
				}
				else{
					if(M.tile[r,c].inv.type == type){
						M.tile[r,c].inv.quantity++;
						return M.tile[r,c].inv;
					}
				}
			}
			else{
				i = new Item(proto[type],r,c);
			}
			return i;
		}
		public static Item Create(ConsumableType type,Actor a){
			Item i = null;
			if(a.InventoryCount() < Global.MAX_INVENTORY_SIZE){
				foreach(Item held in a.inv){
					if(held.type == type){
						held.quantity++;
						return held;
					}
				}
				i = new Item(proto[type],-1,-1);
				a.inv.Add(i);
			}
			else{
				i = Create(type,a.row,a.col);
			}
			return i;
		}
		public string SingularName(){
			if(type == ConsumableType.BLAST_FUNGUS){
				//return "blast fungus(" + quantity.ToString() + ")";
				return "blast fungus";
			}
			string result;
			int position;
			if(identified[type]){
				result = name;
			}
			else{
				result = unIDed_name[type];
			}
			position = result.IndexOf('~');
			if(position != -1){
				result = result.Substring(0,position) + result.Substring(position+1);
			}
			return result;
		}
		public string Name(){ return Name(false); }
		public string AName(){ return AName(false); }
		public string TheName(){ return TheName(false); }
		public string Name(bool consider_low_light){
			if(type == ConsumableType.BLAST_FUNGUS){
				return "blast fungus";
				//return "blast fungus(" + quantity.ToString() + ")";
			}
			if(revealed_by_light){
				consider_low_light = false;
			}
			string result;
			int position;
			string qty = quantity.ToString();
			switch(quantity){
			case 0:
				return "buggy item";
			case 1:
				if(!consider_low_light || !Global.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = result.Substring(0,position) + result.Substring(position+1);
					}
					return result;
				}
				else{
					return NameOfItemType();
				}
			default:
				if(!consider_low_light || !Global.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = qty + ' ' + result.Substring(0,position) + 's' + result.Substring(position+1);
					}
					return result;
				}
				else{
					return qty + " " + NameOfItemType() + "s";
				}
			}
		}
		public string AName(bool consider_low_light){
			if(type == ConsumableType.BLAST_FUNGUS){
				return "a blast fungus";
				//return "a blast fungus(" + quantity.ToString() + ")";
			}
			if(revealed_by_light){
				consider_low_light = false;
			}
			string result;
			int position;
			string qty = quantity.ToString();
			switch(quantity){
			case 0:
				return "a buggy item";
			case 1:
				if(!consider_low_light || !Global.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					switch(result[0]){
					case 'a':
					case 'e':
					case 'i':
					case 'o':
					case 'u':
					case 'A':
					case 'E':
					case 'I':
					case 'O':
					case 'U':
						result = "an " + result;
						break;
					default:
						result = "a " + result;
						break;
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = result.Substring(0,position) + result.Substring(position+1);
					}
					return result;
				}
				else{
					if(NameOfItemType() == "orb"){
						return "an orb";
					}
					else{
						return "a " + NameOfItemType();
					}
				}
			default:
				if(!consider_low_light || !Global.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = qty + ' ' + result.Substring(0,position) + 's' + result.Substring(position+1);
					}
					return result;
				}
				else{
					return qty + " " + NameOfItemType() + "s";
				}
			}
		}
		public string TheName(bool consider_low_light){
			if(type == ConsumableType.BLAST_FUNGUS){
				return "the blast fungus";
				//return "the blast fungus(" + quantity.ToString() + ")";
			}
			if(revealed_by_light){
				consider_low_light = false;
			}
			string result;
			int position;
			string qty = quantity.ToString();
			switch(quantity){
			case 0:
				return "the buggy item";
			case 1:
				if(!consider_low_light || !Global.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = the_name;
					}
					else{
						result = "the " + unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = result.Substring(0,position) + result.Substring(position+1);
					}
					return result;
				}
				else{
					return "the " + NameOfItemType();
				}
			default:
				if(!consider_low_light || !Global.BoundsCheck(row,col) || tile().IsLit()){
					if(identified[type]){
						result = name;
					}
					else{
						result = unIDed_name[type];
					}
					position = result.IndexOf('~');
					if(position != -1){
						result = qty + ' ' + result.Substring(0,position) + 's' + result.Substring(position+1);
					}
					return result;
				}
				else{
					return qty + " " + NameOfItemType() + "s";
				}
			}
		}
		public string NameOfItemType(){
			switch(type){
			case ConsumableType.HEALING:
			case ConsumableType.REGENERATION:
			case ConsumableType.STONEFORM:
			case ConsumableType.VAMPIRISM:
			case ConsumableType.BRUTISH_STRENGTH:
			case ConsumableType.ROOTS:
			case ConsumableType.VIGOR:
			case ConsumableType.SILENCE:
			case ConsumableType.CLOAKING:
				return "potion";
			case ConsumableType.BLINKING:
			case ConsumableType.PASSAGE:
			case ConsumableType.TIME:
			case ConsumableType.DETECT_MONSTERS:
			case ConsumableType.MAGIC_MAP:
			case ConsumableType.SUNLIGHT:
			case ConsumableType.DARKNESS:
			case ConsumableType.REPAIR:
			case ConsumableType.CALLING:
			case ConsumableType.TRAP_CLEARING:
				return "scroll";
			case ConsumableType.FREEZING:
			case ConsumableType.FLAMES:
			case ConsumableType.FOG:
			case ConsumableType.DETONATION:
			case ConsumableType.BREACHING:
			case ConsumableType.SHIELDING:
			case ConsumableType.TELEPORTAL:
			case ConsumableType.PAIN:
				return "orb";
			case ConsumableType.BANDAGE:
			case ConsumableType.BLAST_FUNGUS:
				return "other";
			default:
				return "unknown item";
			}
		}
		public static string NameOfItemType(ConsumableType type){
			switch(type){
			case ConsumableType.HEALING:
			case ConsumableType.REGENERATION:
			case ConsumableType.STONEFORM:
			case ConsumableType.VAMPIRISM:
			case ConsumableType.BRUTISH_STRENGTH:
			case ConsumableType.ROOTS:
			case ConsumableType.VIGOR:
			case ConsumableType.SILENCE:
			case ConsumableType.CLOAKING:
				return "potion";
			case ConsumableType.BLINKING:
			case ConsumableType.PASSAGE:
			case ConsumableType.TIME:
			case ConsumableType.DETECT_MONSTERS:
			case ConsumableType.MAGIC_MAP:
			case ConsumableType.SUNLIGHT:
			case ConsumableType.DARKNESS:
			case ConsumableType.REPAIR:
			case ConsumableType.CALLING:
			case ConsumableType.TRAP_CLEARING:
				return "scroll";
			case ConsumableType.FREEZING:
			case ConsumableType.FLAMES:
			case ConsumableType.FOG:
			case ConsumableType.DETONATION:
			case ConsumableType.BREACHING:
			case ConsumableType.SHIELDING:
			case ConsumableType.TELEPORTAL:
			case ConsumableType.PAIN:
				return "orb";
			case ConsumableType.BANDAGE:
			case ConsumableType.BLAST_FUNGUS:
				return "other";
			default:
				return "unknown item";
			}
		}
		public static int Rarity(ConsumableType type){
			switch(type){
			case ConsumableType.VAMPIRISM:
			case ConsumableType.BRUTISH_STRENGTH:
			case ConsumableType.ROOTS:
			case ConsumableType.BREACHING:
			case ConsumableType.SHIELDING:
				return 3;
			case ConsumableType.HEALING:
			case ConsumableType.REGENERATION:
			case ConsumableType.STONEFORM:
			case ConsumableType.SILENCE:
			case ConsumableType.MAGIC_MAP:
			case ConsumableType.REPAIR:
			case ConsumableType.TRAP_CLEARING:
			case ConsumableType.FLAMES:
			case ConsumableType.DETONATION:
			case ConsumableType.TELEPORTAL:
			case ConsumableType.PAIN:
				return 2;
			case ConsumableType.BANDAGE:
			case ConsumableType.BLAST_FUNGUS:
				return 0;
			default:
				return 1;
			}
		}
		public static ConsumableType RandomItem(){
			List<ConsumableType> list = new List<ConsumableType>();
			foreach(ConsumableType item in Enum.GetValues(typeof(ConsumableType))){
				if(Item.Rarity(item) == 1){
					list.Add(item);
				}
				else{
					if(Item.Rarity(item) == 0){
						continue;
					}
					if(Global.Roll(1,Item.Rarity(item)) == Item.Rarity(item)){
						list.Add(item);
					}
				}
			}
			return list.Random();
		}
		public static void GenerateUnIDedNames(){
			identified = new Dict<ConsumableType,bool>();
			List<string> potion_flavors = new List<string>{"vermilion","cerulean","emerald","fuchsia","aquamarine","goldenrod","violet","silver","indigo","crimson"};
			List<Color> potion_colors = new List<Color>{Color.Red,Color.Blue,Color.Green,Color.Magenta,Color.Cyan,Color.Yellow,Color.DarkMagenta,Color.Gray,Color.DarkBlue,Color.DarkRed};
			List<string> orb_flavors = new List<string>{"flickering","iridescent","sparkling","chromatic","psychedelic","scintillating","glimmering","kaleidoscopic"};
			List<Color> orb_colors = new List<Color>{Color.RandomRGB,Color.RandomCMY,Color.RandomDRGB,Color.RandomDCMY,Color.RandomRainbow,Color.RandomBright,Color.RandomDark,Color.RandomAny};
			foreach(ConsumableType type in Enum.GetValues(typeof(ConsumableType))){
				string type_name = NameOfItemType(type);
				if(type_name == "potion"){
					int num = Global.Roll(potion_flavors.Count) - 1;
					unIDed_name[type] = potion_flavors[num] + " potion~";
					proto[type].color = potion_colors[num];
					potion_flavors.RemoveAt(num);
					potion_colors.RemoveAt(num);
				}
				else{
					if(type_name == "scroll"){
						unIDed_name[type] = "scroll~ labeled '" + GenerateScrollName() + "'";
					}
					else{
						if(type_name == "orb"){
							unIDed_name[type] = orb_flavors.RemoveRandom() + " orb~";
							proto[type].color = orb_colors.RemoveRandom(); //note that color isn't tied to name for orbs. they're all random.
							if(type == ConsumableType.TELEPORTAL){
								Tile.Feature(FeatureType.TELEPORTAL).color = proto[type].color;
							}
						}
						else{
							identified[type] = true; //bandages
						}
					}
				}
			}
		}
		public static string GenerateScrollName(){
			//List<string> vowel = new List<string>{"a","e","i","o","u"};
			//List<string> consonant = new List<string>{"k","s","t","n","h","m","y","r","w","g","d","p","b"}; //Japanese-inspired - used AEIOU, 4 syllables max, and 3-9 total
			//List<string> consonant = new List<string>{"h","k","l","n","m","p","w"}; //Hawaiian-inspired
			//List<string> vowel = new List<string>{"y","i","e","u","ae"}; //some kinda Gaelic-inspired
			//List<string> consonant = new List<string>{"r","t","s","rr","m","n","w","b","c","d","f","g","l","ss","v"}; //some kinda Gaelic-inspired
			List<string> vowel = new List<string>{"a","e","i","o","u","ea","ei","io","a","e","i","o","u","a","e","i","o","u","a","e","i","o","oo","ee","a","e","o"}; //the result of a bunch of tweaking
			List<string> consonant = new List<string>{"k","s","t","n","h","m","y","r","w","g","d","p","b","f","l","v","z","ch","br","cr","dr","fr","gr","kr","pr","tr","th","sc","sh","sk","sl","sm","sn","sp","st","k","s","t","n","m","r","g","d","p","b","l","k","s","t","n","m","r","d","p","b","l",};
			int syllables = 0;
			List<int> syllable_count = null;
			do{
				syllables = Global.Roll(4) + 2;
				syllable_count = new List<int>();
				while(syllables > 0){
					if(syllable_count.Count == 2){
						syllable_count.Add(syllables);
						syllables = 0;
						break;
					}
					int R = Math.Min(syllables,3);
					int M = 0;
					if(syllable_count.Count == 0){ //sorry, magic numbers here
						M = 6;
					}
					if(syllable_count.Count == 1){
						M = 5;
					}
					int D = 0;
					if(syllable_count.Count == 0){
						D = Math.Max(0,syllables - M);
					}
					int s = Global.Roll(R - D) + D;
					syllable_count.Add(s);
					syllables -= s;
				}
			}
			while(!syllable_count.Any(x => x!=1)); // if every word has only 1 syllable, try again
			string result = "";
			while(syllable_count.Count > 0){
				string word = "";
				if(Global.OneIn(5)){
					word = word + vowel.Random();
				}
				for(int count = syllable_count.RemoveRandom();count > 0;--count){
					word = word + consonant.Random() + vowel.Random();
					/*if(Global.OneIn(20)){ //used for the Japanese-inspired one
						word = word + "n";
					}*/
				}
				if(result == ""){
					result = result + word;
				}
				else{
					result = result + " " + word;
				}
			}
			return result;
		}
		public bool Use(Actor user){ return Use(user,null); }
		public bool Use(Actor user,List<Tile> line){
			bool used = true;
			bool IDed = true;
			switch(type){
			case ConsumableType.HEALING:
				user.curhp = user.maxhp;
				B.Add("Your wounds are healed completely. ");
				break;
			case ConsumableType.REGENERATION:
			{
				if(user.name == "you"){
					B.Add("Your blood tingles. ",user); //todo: yeah, maybe change this one too?
				}
				else{
					B.Add(user.the_name + " looks energized. ",user);
				}
				user.attrs[AttrType.REGENERATING]++;
				int duration = 100;
				Q.Add(new Event(user,duration*100,AttrType.REGENERATING));
				break;
			}
			case ConsumableType.STONEFORM:
			{
				B.Add("You transform into a being of animated stone. ");
				int duration = Global.Roll(2,20) + 20;
				List<AttrType> attributes = new List<AttrType>{AttrType.REGENERATING,AttrType.BRUTISH_STRENGTH,AttrType.VIGOR,AttrType.SILENCED,AttrType.SHADOW_CLOAK};
				foreach(AttrType at in attributes){
					if(user.HasAttr(at)){
						user.attrs[at] = 0;
						Q.KillEvents(user,at);
					}
				}
				if(user.HasAttr(AttrType.LIGHT_ALLERGY)){ //hacky way to detect vampirism
					user.attrs[AttrType.LIGHT_ALLERGY] = 0;
					user.attrs[AttrType.FLYING] = 0;
					user.attrs[AttrType.LIFE_DRAIN_HIT] = 0; //this will break if the player can gain these from anything else
					Q.KillEvents(user,AttrType.LIGHT_ALLERGY);
					Q.KillEvents(user,AttrType.FLYING);
					Q.KillEvents(user,AttrType.LIFE_DRAIN_HIT);
				}
				if(user.HasAttr(AttrType.IMMOBILE)){ //hacky way to detect roots
					user.attrs[AttrType.IMMOBILE] = 0;
					user.attrs[AttrType.BONUS_DEFENSE] -= 5; //todo: check value
					Q.KillEvents(user,AttrType.IMMOBILE);
					Q.KillEvents(user,AttrType.BONUS_DEFENSE);
				}
				user.attrs[AttrType.RESIST_FIRE]++;
				user.attrs[AttrType.RESIST_COLD]++;
				user.attrs[AttrType.RESIST_ELECTRICITY]++;
				Q.Add(new Event(user,duration*100,AttrType.RESIST_FIRE));
				Q.Add(new Event(user,duration*100,AttrType.RESIST_COLD));
				Q.Add(new Event(user,duration*100,AttrType.RESIST_ELECTRICITY));
				user.RefreshDuration(AttrType.NONLIVING,duration*100,"Your rocky form reverts to flesh. ");
				break;
			}
			case ConsumableType.VAMPIRISM:
			{
				B.Add("You become vampiric. ");
				int duration = Global.Roll(20) + 20;
				user.RefreshDuration(AttrType.LIGHT_ALLERGY,duration*100);
				user.RefreshDuration(AttrType.FLYING,duration*100);
				user.RefreshDuration(AttrType.LIFE_DRAIN_HIT,duration*100,"You are no longer vampiric. ");
				break;
			}
			case ConsumableType.BRUTISH_STRENGTH:
			{
				B.Add("You feel a surge of strength. ");
				user.RefreshDuration(AttrType.BRUTISH_STRENGTH,(Global.Roll(2,6)+6)*100,"Your incredible strength wears off. ");
				break;
			}
			case ConsumableType.ROOTS:
			{
				B.Add("You grow roots and a hard shell of bark. "); //todo! these messages, man.
				int duration = Global.Roll(20) + 20;
				user.attrs[AttrType.BONUS_DEFENSE] = 4; //this should result in a bonus defense of 5 that eventually gets removed. HOWEVER (todo hack !!!),
				user.GainAttrRefreshDuration(AttrType.BONUS_DEFENSE,duration*100); //this doesn't work with bonus defense from other sources. REWORK THIS.
				user.RefreshDuration(AttrType.IMMOBILE,duration*100,"You are no longer rooted to the ground. ");
				break;
			}
			case ConsumableType.VIGOR:
			{
				//B.Add("Your movements become swift and unencumbered. ");
				B.Add("You start moving with extraordinary speed. ");
				if(user.exhaustion > 0){
					user.exhaustion = 0;
					B.Add("Your fatigue disappears. ");
				}
				user.RefreshDuration(AttrType.VIGOR,(Global.Roll(2,10) + 10)*100,"Your extraordinary speed fades. ");
				break;
			}
			case ConsumableType.SILENCE:
			{
				B.Add("A hush falls around you. ");
				user.RefreshDuration(AttrType.SILENCED,(Global.Roll(2,20)+20)*100,"You are no longer silenced. ");
				break;
			}
			case ConsumableType.CLOAKING:
				if(user.tile().IsLit()){
					B.Add("You would feel at home in the shadows. ");
				}
				else{
					B.Add("You fade away in the darkness. ");
				}
				//user.RefreshDuration(AttrType.SHADOW_CLOAK,(Global.Roll(41)+29)*100,"You are no longer cloaked. ",user);
				user.RefreshDuration(AttrType.SHADOW_CLOAK,(Global.Roll(2,20)+30)*100,"You are no longer cloaked. ",user);
				break;
			case ConsumableType.BLINKING:
			{
				List<Tile> tiles = user.TilesWithinDistance(8).Where(x => x.passable && x.actor() == null && user.EstimatedEuclideanDistanceFromX10(x) >= 45);
				if(tiles.Count > 0){
					Tile t = tiles.Random();
					B.Add(user.You("step") + " through a rip in reality. ",M.tile[user.p],t);
					user.AnimateStorm(2,3,4,'*',Color.DarkMagenta);
					user.Move(t.row,t.col);
					M.Draw();
					user.AnimateStorm(2,3,4,'*',Color.DarkMagenta);
				}
				else{
					B.Add("Nothing happens. ");
					IDed = false;
				}
				break;
				/*for(int i=0;i<9999;++i){
					int rr = Global.Roll(1,17) - 9;
					int rc = Global.Roll(1,17) - 9;
					if(Math.Abs(rr) + Math.Abs(rc) >= 6){
						rr += user.row;
						rc += user.col;
						if(M.BoundsCheck(rr,rc) && M.tile[rr,rc].passable && M.actor[rr,rc] == null){
							B.Add(user.You("step") + " through a rip in reality. ",M.tile[user.row,user.col],M.tile[rr,rc]);
							user.AnimateStorm(2,3,4,'*',Color.DarkMagenta);
							user.Move(rr,rc);
							M.Draw();
							user.AnimateStorm(2,3,4,'*',Color.DarkMagenta);
							break;
						}
					}
				}
				break;*/
			}
			/*case ConsumableType.TELEPORTATION:
				for(int i=0;i<9999;++i){
					int rr = Global.Roll(1,Global.ROWS-2);
					int rc = Global.Roll(1,Global.COLS-2);
					if(Math.Abs(rr-user.row) >= 10 || Math.Abs(rc-user.col) >= 10 || (Math.Abs(rr-user.row) >= 7 && Math.Abs(rc-user.col) >= 7)){
						if(M.BoundsCheck(rr,rc) && M.tile[rr,rc].passable && M.actor[rr,rc] == null){
							B.Add(user.You("jump") + " through a rift in reality. ",M.tile[user.row,user.col],M.tile[rr,rc]);
							user.AnimateStorm(3,3,10,'*',Color.Green);
							user.Move(rr,rc);
							M.Draw();
							user.AnimateStorm(3,3,10,'*',Color.Green);
							break;
						}
					}
				}
				break;*/
			case ConsumableType.PASSAGE:
			{
				List<int> valid_dirs = new List<int>();
				foreach(int dir in Global.FourDirections()){
					Tile t = user.TileInDirection(dir);
					if(t != null && t.Is(TileType.WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR)){ //todo: update passage spell with these
						while(!t.passable){
							if(t.row == 0 || t.row == Global.ROWS-1 || t.col == 0 || t.col == Global.COLS-1){
								break;
							}
							t = t.TileInDirection(dir);
						}
						if(t.passable){
							valid_dirs.Add(dir);
						}
					}
				}
				if(valid_dirs.Count > 0){
					int dir = valid_dirs.Random();
					Tile t = user.TileInDirection(dir);
					colorchar ch = new colorchar(Color.Cyan,'!');
					switch(user.DirectionOf(t)){
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
					Console.CursorVisible = false;
					Tile last_wall = null;
					while(!t.passable){
						tiles.Add(t);
						memlist.Add(Screen.MapChar(t.row,t.col));
						Screen.WriteMapChar(t.row,t.col,ch);
						Thread.Sleep(35);
						last_wall = t;
						t = t.TileInDirection(dir);
					}
					if(M.actor[t.row,t.col] == null){
						int r = user.row;
						int c = user.col;
						user.Move(t.row,t.col);
						Screen.WriteMapChar(r,c,M.VisibleColorChar(r,c));
						Screen.WriteMapChar(t.row,t.col,M.VisibleColorChar(t.row,t.col));
						int idx = 0;
						foreach(Tile tile in tiles){
							Screen.WriteMapChar(tile.row,tile.col,memlist[idx++]);
							Thread.Sleep(35);
						}
						B.Add(user.You("travel") + " through the passage. ",user,t);
					}
					else{
						Tile destination = null;
						List<Tile> adjacent = t.TilesAtDistance(1).Where(x=>x.passable && x.actor() == null && x.DistanceFrom(last_wall) == 1);
						if(adjacent.Count > 0){
							destination = adjacent.Random();
						}
						else{
							foreach(Tile tile in M.ReachableTilesByDistance(t.row,t.col,false)){
								if(tile.actor() == null){
									destination = tile;
									break;
								}
							}
						}
						if(destination != null){
							int r = user.row;
							int c = user.col;
							user.Move(destination.row,destination.col);
							Screen.WriteMapChar(r,c,M.VisibleColorChar(r,c));
							Screen.WriteMapChar(destination.row,destination.col,M.VisibleColorChar(destination.row,destination.col));
							int idx = 0;
							foreach(Tile tile in tiles){
								Screen.WriteMapChar(tile.row,tile.col,memlist[idx++]);
								Thread.Sleep(35);
							}
							B.Add(user.You("travel") + " through the passage. ",user,destination);
						}
						else{
							B.Add("Something blocks your movement through the passage. ");
						}
					}
				}
				else{
					B.Add("Nothing happens. ");
					IDed = false;
				}
				break;
			}
			case ConsumableType.TIME:
				B.Add("Time stops for a moment. ");
				Q.turn -= 200;
				break;
			case ConsumableType.DETECT_MONSTERS:
			{
				if(M.AllActors().Count > 1){
					B.Add("The scroll reveals " + user.Your() + " foes. ",user);
				}
				else{
					B.Add("The scroll reveals a lack of foes on this level. ");
				}
				int duration = Global.Roll(20)+30;
				user.RefreshDuration(AttrType.DETECTING_MONSTERS,duration*100,user.Your() + " foes are no longer revealed. ",user);
				break;
			}
			case ConsumableType.MAGIC_MAP:
			{
				B.Add("The scroll reveals the layout of this level. ");
				Event hiddencheck = null;
				foreach(Event e in Q.list){
					if(!e.dead && e.type == EventType.CHECK_FOR_HIDDEN){
						hiddencheck = e;
						break;
					}
				}
				int max_dist = 0;
				List<Tile> last_tiles = new List<Tile>();
				foreach(Tile t in M.ReachableTilesByDistance(user.row,user.col,true,TileType.STONE_SLAB,TileType.DOOR_C,TileType.STALAGMITE,TileType.RUBBLE,TileType.HIDDEN_DOOR)){
					if(t.type != TileType.FLOOR){
						t.seen = true;
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
						Screen.WriteMapChar(t.row,t.col,t.symbol,Color.RandomRainbow);
						//Screen.WriteMapChar(t.row,t.col,M.VisibleColorChar(t.row,t.col));
						if(user.DistanceFrom(t) > max_dist){
							max_dist = user.DistanceFrom(t);
							while(last_tiles.Count > 0){
								Tile t2 = last_tiles.RemoveRandom();
								Screen.WriteMapChar(t2.row,t2.col,M.VisibleColorChar(t2.row,t2.col));
							}
							Thread.Sleep(20);
						}
						last_tiles.Add(t);
					}
				}
				break;
			}
			case ConsumableType.SUNLIGHT:
				if(M.wiz_lite == false){
					B.Add("The air itself seems to shine. ");
					M.wiz_lite = true;
					M.wiz_dark = false;
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((Global.Roll(2,20) + 200) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even brighter for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((Global.Roll(2,20) + 200) * 100,EventType.NORMAL_LIGHTING));
				}
				break;
			case ConsumableType.DARKNESS:
				if(M.wiz_dark == false){
					B.Add("The air itself grows dark. ");
					if(player.light_radius > 0){
						B.Add("Your light is extinguished! ");
					}
					M.wiz_dark = true;
					M.wiz_lite = false;
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((Global.Roll(2,20) + 200) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even darker for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((Global.Roll(2,20) + 200) * 100,EventType.NORMAL_LIGHTING));
				}
				break;
			case ConsumableType.REPAIR:
			{
				B.Add("Your equipment glows briefly. ");
				bool repaired = false;
				foreach(EquipmentStatus eqstatus in Enum.GetValues(typeof(EquipmentStatus))){
					foreach(Weapon w in user.weapons){
						if(w.status[eqstatus]){
							repaired = true;
							w.status[eqstatus] = false;
						}
					}
					foreach(Armor a in user.armors){
						if(a.status[eqstatus]){
							repaired = true;
							a.status[eqstatus] = false;
						}
					}
				}
				if(repaired){
					B.Add("It looks as good as new! ");
				}
				break;
			}
			case ConsumableType.CALLING:
			{
				bool found = false;
				for(int dist = 1;dist < Math.Max(Global.ROWS,Global.COLS);++dist){
					List<Tile> tiles = user.TilesAtDistance(dist).Where(x=>x.actor() != null && !x.actor().HasAttr(AttrType.IMMOBILE));
					if(tiles.Count > 0){
						Actor a = tiles.Random().actor();
						foreach(Tile t in M.ReachableTilesByDistance(user.row,user.col,false)){
							if(t.actor() == null){
								B.Add("The scroll calls " + a.a_name + " to you. ");
								a.Move(t.row,t.col);
								found = true;
								break;
							}
						}
						if(found){
							break;
						}
					}
				}
				if(!found){
					B.Add("Nothing happens. ");
					IDed = false;
				}
				break;
			}
			case ConsumableType.TRAP_CLEARING:
			{
				List<Tile> traps = new List<Tile>();
				{
					List<Tile>[] traparray = new List<Tile>[4];
					for(int i=0;i<4;++i){
						traparray[i] = new List<Tile>();
					}
					for(int i=0;i<=12;++i){
						foreach(Tile t in user.TilesAtDistance(i)){ //all this ensures that the traps go off in the best order
							switch(t.type){
							case TileType.ALARM_TRAP:
							case TileType.TELEPORT_TRAP:
							case TileType.ICE_TRAP:
							case TileType.DIM_VISION_TRAP:
							case TileType.STUN_TRAP:
							case TileType.QUICKFIRE_TRAP:
								traparray[0].Add(t);
								break;
							case TileType.POISON_GAS_TRAP:
							case TileType.GRENADE_TRAP:
								traparray[1].Add(t);
								break;
							case TileType.UNDEAD_TRAP:
							case TileType.PHANTOM_TRAP:
								traparray[2].Add(t);
								break;
							case TileType.LIGHT_TRAP:
							case TileType.DARKNESS_TRAP:
								traparray[3].Add(t);
								break;
							}
						}
					}
					for(int i=0;i<4;++i){
						foreach(Tile t in traparray[i]){
							traps.Add(t);
						}
					}
				}
				if(traps.Count > 0){
					B.Add("*CLICK*. ");
					foreach(Tile t in traps){
						t.TriggerTrap(false);
					}
				}
				else{
					B.Add("Nothing happens. ");
					IDed = false;
				}
				break;
			}
			/*case ConsumableType.PRISMATIC:
			{
				if(line == null){
					int radius = 1;
					if(!identified[type]){
						radius = 0;
					}
					line = user.GetTargetTile(12,radius,true);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " the " + SingularName() + ". ",user);
					bool trigger_trap = true;
					if(first != null && first != user){
						trigger_trap = false;
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',Color.RandomPrismatic);
					List<DamageType> dmg = new List<DamageType>();
					dmg.Add(DamageType.FIRE);
					dmg.Add(DamageType.COLD);
					dmg.Add(DamageType.ELECTRIC);
					while(dmg.Count > 0){
						DamageType damtype = dmg.Random();
						colorchar ch = new colorchar(Color.Black,'*');
						switch(damtype){
						case DamageType.FIRE:
							ch.color = Color.RandomFire;
							break;
						case DamageType.COLD:
							ch.color = Color.RandomIce;
							break;
						case DamageType.ELECTRIC:
							ch.color = Color.RandomLightning;
							break;
						}
						B.DisplayNow();
						Screen.AnimateExplosion(t,1,ch,100);
						if(t.passable){
							foreach(Tile t2 in t.TilesWithinDistance(1)){
								if(t2.actor() != null){
									t2.actor().TakeDamage(damtype,DamageClass.MAGICAL,Global.Roll(2,6),user,"a prismatic orb");
								}
								if(damtype == DamageType.FIRE && t2.Is(FeatureType.TROLL_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_CORPSE);
									B.Add("The troll corpse burns to ashes! ",t2);
								}
								if(damtype == DamageType.FIRE && t2.Is(FeatureType.TROLL_SEER_CORPSE)){
									t2.features.Remove(FeatureType.TROLL_SEER_CORPSE);
									B.Add("The troll seer corpse burns to ashes! ",t2);
								}
							}
						}
						else{
							foreach(Tile t2 in t.TilesWithinDistance(1)){
								if(prev != null && prev.HasBresenhamLine(t2.row,t2.col)){
									if(t2.actor() != null){
										t2.actor().TakeDamage(damtype,DamageClass.MAGICAL,Global.Roll(2,6),user,"a prismatic orb");
									}
									if(damtype == DamageType.FIRE && t2.Is(FeatureType.TROLL_CORPSE)){
										t2.features.Remove(FeatureType.TROLL_CORPSE);
										B.Add("The troll corpse burns to ashes! ",t2);
									}
									if(damtype == DamageType.FIRE && t2.Is(FeatureType.TROLL_SEER_CORPSE)){
										t2.features.Remove(FeatureType.TROLL_SEER_CORPSE);
										B.Add("The troll seer corpse burns to ashes! ",t2);
									}
								}
							}
						}
						dmg.Remove(damtype);
					}
					t.MakeNoise(2);
					if(trigger_trap && t.IsTrap()){
						t.TriggerTrap();
					}
				}
				else{
					used = false;
				}
				break;
			}*/
			case ConsumableType.FREEZING:
			{
				if(line == null){
					int radius = 3;
					if(!identified[type]){
						radius = 0;
					}
					line = user.GetTargetTile(12,radius,true);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " the " + SingularName() + ". ",user);
					bool trigger_trap = true;
					if(first != null && first != user){
						trigger_trap = false;
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',color);
					user.AnimateExplosion(t,3,'*',Color.RandomIce); //todo: improve animation
					List<Actor> targets = new List<Actor>();
					Tile LOE_tile = t;
					if(!t.passable && prev != null){
						LOE_tile = prev;
					}
					foreach(Actor ac in t.ActorsWithinDistance(3)){
						if(LOE_tile.HasLOE(ac)){
							targets.Add(ac);
						}
					}
					/*if(t.passable){
						foreach(Actor ac in t.ActorsWithinDistance(3)){
							if(t.HasLOE(ac)){
								targets.Add(ac);
							}
						}
					}
					else{
						foreach(Actor ac in t.ActorsWithinDistance(3)){
							if(prev != null && prev.HasLOE(ac)){
								targets.Add(ac);
							}
						}
					}*/
					while(targets.Count > 0){
						Actor ac = targets.RemoveRandom();
						B.Add(ac.YouAre() + " encased in ice. ",ac);
						ac.attrs[Forays.AttrType.FROZEN] = 35;
					}
					t.MakeNoise(2);
					if(trigger_trap && t.IsTrap()){
						t.TriggerTrap();
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.FLAMES:
			{
				break;
			}
			/*case ConsumableType.QUICKFIRE:
			{
				if(line == null){
					line = user.GetTargetTile(12,0,true);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " the " + SingularName() + ". ",user);
					bool trigger_trap = true;
					if(first != null && first != user){
						trigger_trap = false;
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',Color.RandomFire);
					if(t.passable){
						t.features.Add(FeatureType.QUICKFIRE);
						Q.Add(new Event(t,new List<Tile>{t},100,EventType.QUICKFIRE,AttrType.NO_ATTR,3,""));
					}
					else{
						prev.features.Add(FeatureType.QUICKFIRE);
						Q.Add(new Event(prev,new List<Tile>{prev},100,EventType.QUICKFIRE,AttrType.NO_ATTR,3,""));
					}
					t.MakeNoise(2);
					if(trigger_trap && t.IsTrap()){
						t.TriggerTrap();
					}
				}
				else{
					used = false;
				}
				break;
			}*/
			case ConsumableType.FOG:
			{
				if(line == null){
					int radius = 3;
					if(!identified[type]){
						radius = 0;
					}
					line = user.GetTargetTile(12,radius,true);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " the " + SingularName() + ". ",user);
					bool trigger_trap = true;
					if(first != null && first != user){
						trigger_trap = false;
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',color);
					List<Tile> area = new List<Tile>();
					List<pos> cells = new List<pos>();
					Tile LOE_tile = t;
					if(!t.passable && prev != null){
						LOE_tile = prev;
					}
					foreach(Tile tile in t.TilesWithinDistance(3)){
						if(tile.passable && LOE_tile.HasLOE(tile)){
							tile.AddFeature(FeatureType.FOG);
							area.Add(tile);
							cells.Add(tile.p);
						}
					}
					/*if(t.passable){
						foreach(Tile tile in t.TilesWithinDistance(3)){
							if(tile.passable && t.HasLOE(tile)){
								tile.AddOpaqueFeature(FeatureType.FOG);
								area.Add(tile);
								cells.Add(tile.p);
							}
						}
					}
					else{
						foreach(Tile tile in t.TilesWithinDistance(3)){
							if(prev != null && tile.passable && prev.HasLOE(tile)){
								tile.AddOpaqueFeature(FeatureType.FOG);
								area.Add(tile);
								cells.Add(tile.p);
							}
						}
					}*/
					Screen.AnimateMapCells(cells,new colorchar('*',Color.Gray));
					Q.Add(new Event(area,600,EventType.FOG));
					t.MakeNoise(2);
					if(trigger_trap && t.IsTrap()){
						t.TriggerTrap();
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.DETONATION:
			{
				//remember to makenoise(8) and use RandomExplosion
				break;
			}
			case ConsumableType.BREACHING:
			{
				if(line == null){
					int radius = 5;
					if(!identified[type]){
						radius = 0;
					}
					line = user.GetTargetTile(12,radius,true);
				}
				if(line != null){
					Tile t = line.Last();
					//Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " the " + SingularName() + ". ",user);
					bool trigger_trap = true;
					if(first != null && first != user){
						trigger_trap = false;
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',color);
					Color breach_color = Color.RandomBreached;
					//List<Tile> last_tiles = new List<Tile>();
					int max_dist = -1;
					foreach(Tile t2 in M.ReachableTilesByDistance(t.row,t.col,false,TileType.WALL,TileType.STONE_SLAB,TileType.DOOR_C,TileType.STALAGMITE,TileType.RUBBLE,TileType.HIDDEN_DOOR)){
						if(t.DistanceFrom(t2) > 5){
							/*while(last_tiles.Count > 0){
								Tile t3 = last_tiles.RemoveRandom();
								Screen.WriteMapChar(t3.row,t3.col,t3.symbol,Color.Cyan);
							}*/
							break;
						}
						if(t2.type == TileType.WALL){
							Screen.WriteMapChar(t2.row,t2.col,t2.symbol,breach_color);
							if(t.DistanceFrom(t2) > max_dist){
								max_dist = t.DistanceFrom(t2);
								/*while(last_tiles.Count > 0){
									Tile t3 = last_tiles.RemoveRandom();
									Screen.WriteMapChar(t3.row,t3.col,M.VisibleColorChar(t3.row,t3.col));
								}*/
								Thread.Sleep(50);
							}
							//last_tiles.Add(t2);
						}
					}
					List<Tile> area = new List<Tile>();
					foreach(Tile tile in t.TilesWithinDistance(5)){
						if(tile.type == TileType.WALL && tile.row > 0 && tile.col > 0 && tile.row < Global.ROWS-1 && tile.col < Global.COLS-1){
							tile.Toggle(null,TileType.BREACHED_WALL);
							tile.solid_rock = false;
							area.Add(tile);
						}
					}
					if(area.Count > 0){
						foreach(Tile tile in area){
							foreach(Tile neighbor in tile.TilesAtDistance(1)){
								neighbor.solid_rock = false;
							}
						}
						Q.Add(new Event(t,area,500,EventType.BREACH));
					}
					t.MakeNoise(2);
					if(trigger_trap && t.IsTrap()){
						t.TriggerTrap();
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.SHIELDING:
			{
				if(line == null){
					int radius = 1;
					if(!identified[type]){
						radius = 0;
					}
					line = user.GetTargetTile(12,radius,true);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " the " + SingularName() + ". ",user);
					bool trigger_trap = true;
					if(first != null && first != user){
						trigger_trap = false;
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',color);
					//List<Tile> area = new List<Tile>();
					List<pos> cells = new List<pos>();
					Tile LOE_tile = t;
					if(!t.passable && prev != null){
						LOE_tile = prev;
					}
					foreach(Tile tile in t.TilesWithinDistance(1)){
						if(tile.actor() != null && LOE_tile.HasLOE(tile)){
							B.Add(tile.actor().YouAre() + " shielded. ",tile.actor()); //todo: fix effect - give it a featuretype and an event to refresh a low-level arcane shield on anything in the zone.
							tile.actor().attrs[AttrType.ARCANE_SHIELDED] = 20; //shouldn't actually be hard to do.
							tile.actor().RefreshDuration(AttrType.ARCANE_SHIELDED,2000,tile.actor().YouAre() + " no longer shielded. ",tile.actor());
							cells.Add(tile.p);
						}
					}
					Screen.AnimateMapCells(cells,new colorchar('*',Color.Blue)); //todo: fix animation
					t.MakeNoise(2);
					if(trigger_trap && t.IsTrap()){
						t.TriggerTrap();
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.TELEPORTAL:
			{
				if(line == null){
					int radius = 0;
					line = user.GetTargetTile(12,radius,true);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " the " + SingularName() + ". ",user);
					bool trigger_trap = true;
					if(first != null && first != user){
						trigger_trap = false;
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',color);
					Tile target_tile = t;
					if(!t.passable && prev != null){
						target_tile = prev;
					}
					target_tile.features.Add(FeatureType.TELEPORTAL);
					Q.Add(new Event(target_tile,0,EventType.TELEPORTAL));
					t.MakeNoise(2);
					if(trigger_trap && t.IsTrap()){
						t.TriggerTrap();
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.PAIN:
			{
				if(line == null){
					int radius = 5;
					if(!identified[type]){
						radius = 0;
					}
					line = user.GetTargetTile(12,radius,true);
				}
				if(line != null){
					Tile t = line.Last();
					Tile prev = line.LastBeforeSolidTile();
					Actor first = user.FirstActorInLine(line);
					B.Add(user.You("fling") + " the " + SingularName() + ". ",user);
					bool trigger_trap = true;
					if(first != null && first != user){
						trigger_trap = false;
						t = first.tile();
						B.Add("It shatters on " + first.the_name + "! ",first);
						first.player_visibility_duration = -1;
						first.attrs[AttrType.PLAYER_NOTICED]++;
					}
					else{
						B.Add("It shatters on " + t.the_name + "! ",t);
					}
					user.AnimateProjectile(line.ToFirstObstruction(),'*',color);
					List<pos> cells = new List<pos>();
					Tile LOE_tile = t;
					if(!t.passable && prev != null){
						LOE_tile = prev;
					}
					foreach(Tile tile in t.TilesWithinDistance(5)){
						if(tile.actor() != null && LOE_tile.HasLOE(tile)){
							if(tile.actor().TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,Global.Roll(2,6),user,"an orb of pain")){
								B.Add(tile.actor().You("become") + " vulnerable. ",tile.actor()); //todo: implement takes_extra_damage
								tile.actor().RefreshDuration(AttrType.TAKES_EXTRA_DAMAGE,(Global.Roll(2,6)+6)*100,tile.actor().YouFeel() + " less vulnerable. ",tile.actor());
							}
							cells.Add(tile.p); //todo: fix animation
						}
					}
					Screen.AnimateMapCells(cells,new colorchar('*',Color.DarkRed)); //maybe a new color for this. dark red, dark gray, a little red maybe.
					t.MakeNoise(2);
					if(trigger_trap && t.IsTrap()){
						t.TriggerTrap();
					}
				}
				else{
					used = false;
				}
				break;
			}
			case ConsumableType.BLAST_FUNGUS:
			{
				if(line == null){
					line = user.GetTarget(false,12,0,false,true);
				}
				revealed_by_light = true;
				ignored = true;
				Tile t = line.LastBeforeSolidTile();
				Actor first = user.FirstActorInLine(line);
				B.Add(user.You("fling") + " " + TheName() + ". ");
				if(first != null && first != user){
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
					user.AnimateProjectile(line,symbol,color);
				}
				t.GetItem(this);
				//inv.Remove(i);
				t.MakeNoise(2);
				if(first != null && first != user){
					first.player_visibility_duration = -1;
					first.attrs[AttrType.PLAYER_NOTICED]++;
				}
				else{
					if(t.IsTrap()){
						t.TriggerTrap();
					}
				}
				break;
			}
			case ConsumableType.BANDAGE:
				if(user.curhp < user.maxhp){
					user.curhp += 1;
				}
				//user.TakeDamage(DamageType.HEAL,DamageClass.NO_TYPE,1,null);
				if(user.HasAttr(AttrType.MAGICAL_BLOOD)){
					user.recover_time = Q.turn + 200;
				}
				else{
					user.recover_time = Q.turn + 500;
				}
				if(user.name == "you"){
					B.Add("You apply a bandage. ");
				}
				else{
					B.Add(user.the_name + " applies a bandage. ",user);
				}
				break;
			default:
				used = false;
				break;
			}
			if(used){
				if(IDed){
					identified[type] = true;
				}
				if(quantity > 1 && type != ConsumableType.BLAST_FUNGUS){
					--quantity;
				}
				else{
					if(user != null){
						user.inv.Remove(this);
					}
				}
			}
			return used;
		}
	}
	public class Weapon{
		public WeaponType type;
		public EnchantmentType enchantment;
		public Dict<EquipmentStatus,bool> status = new Dict<EquipmentStatus,bool>();
		public Weapon(WeaponType type_){
			type = type_;
			enchantment = EnchantmentType.NO_ENCHANTMENT;
		}
		public Weapon(WeaponType type_,EnchantmentType enchantment_){
			type = type_;
			enchantment = enchantment_;
		}
		public AttackInfo Attack(){
			switch(type){
			case WeaponType.SWORD:
				return new AttackInfo(100,2,CriticalEffect.PERCENT_DAMAGE,"& hit *");
			case WeaponType.MACE:
				return new AttackInfo(100,2,CriticalEffect.REDUCE_ACCURACY,"& hit *");
			case WeaponType.DAGGER:
				return new AttackInfo(100,2,CriticalEffect.STUN,"& hit *");
			case WeaponType.STAFF:
				return new AttackInfo(100,2,CriticalEffect.FREEZE,"& hit *");
			case WeaponType.BOW: //bow's melee damage
				return new AttackInfo(100,1,CriticalEffect.MAKE_NOISE,"& hit *"); //todo crit effects
			default:
				return new AttackInfo(100,0,CriticalEffect.NO_CRIT,"error");
			}
		}
		public override string ToString(){
			return NameWithEnchantment();
		}
		public string NameWithoutEnchantment(){
			switch(type){
			case WeaponType.SWORD:
				return "sword";
			case WeaponType.MACE:
				return "mace";
			case WeaponType.DAGGER:
				return "dagger";
			case WeaponType.STAFF:
				return "staff";
			case WeaponType.BOW:
				return "bow";
			default:
				return "no weapon";
			}
		}
		public string NameWithEnchantment(){
			string ench = "";
			switch(enchantment){
			case EnchantmentType.ECHOES:
				ench = " of echoes";
				break;
			case EnchantmentType.FIRE:
				ench = " of fire";
				break;
			case EnchantmentType.FORCE:
				ench = " of force";
				break;
			case EnchantmentType.NULLIFICATION:
				ench = " of nullification";
				break;
			case EnchantmentType.ICE:
				ench = " of ice";
				break;
			default:
				break;
			}
			return NameWithoutEnchantment() + ench;
		}
		public cstr StatsName(){
			cstr cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.Gray;
			switch(type){
			case WeaponType.SWORD:
				cs.s = "Sword";
				break;
			case WeaponType.MACE:
				cs.s = "Mace";
				break;
			case WeaponType.DAGGER:
				cs.s = "Dagger";
				break;
			case WeaponType.STAFF:
				cs.s = "Staff";
				break;
			case WeaponType.BOW:
				cs.s = "Bow";
				break;
			default:
				cs.s = "no weapon";
				break;
			}
			if(enchantment != EnchantmentType.NO_ENCHANTMENT){
				cs.s = "+" + cs.s + "+";
			}
			cs.color = EnchantmentColor();
			return cs;
		}
		public Color EnchantmentColor(){
			switch(enchantment){
			case EnchantmentType.ECHOES:
				return Color.Green;
			case EnchantmentType.FIRE:
				return Color.Red;
			case EnchantmentType.FORCE:
				return Color.Magenta;
			case EnchantmentType.NULLIFICATION:
				return Color.Cyan;
			case EnchantmentType.ICE:
				return Color.Blue;
			default:
				return Color.Gray;
			}
		}
		public static Color StatusColor(EquipmentStatus status){
			switch(status){
			case EquipmentStatus.BURDENSOME:
				return Color.DarkBlue;
			case EquipmentStatus.CURSED:
				return Color.DarkGray;
			case EquipmentStatus.DAMAGED:
				return Color.DarkMagenta;
			case EquipmentStatus.DULLED:
				return Color.DarkCyan;
			case EquipmentStatus.INFESTED:
				return Color.DarkGreen;
			case EquipmentStatus.NEGATED:
				return Color.White;
			case EquipmentStatus.POSSESSED:
				return Color.Red;
			case EquipmentStatus.RUSTED:
				return Color.DarkRed;
			case EquipmentStatus.WEAK_POINT:
				return Color.Blue;
			case EquipmentStatus.WORN_OUT:
				return Color.DarkYellow;
			default:
				return Color.RandomDark;
			}
		}
		public static string StatusName(EquipmentStatus status){
			switch(status){
			case EquipmentStatus.BURDENSOME:
				return "Burdensome";
			case EquipmentStatus.CURSED:
				return "Cursed";
			case EquipmentStatus.DAMAGED:
				return "Damaged";
			case EquipmentStatus.DULLED:
				return "Dulled";
			case EquipmentStatus.INFESTED:
				return "Infested";
			case EquipmentStatus.NEGATED:
				return "Negated";
			case EquipmentStatus.POSSESSED:
				return "Possessed";
			case EquipmentStatus.RUSTED:
				return "Rusted";
			case EquipmentStatus.WEAK_POINT:
				return "Weak point";
			case EquipmentStatus.WORN_OUT:
				return "Worn out";
			default:
				return "No status";
			}
		}
		public colorstring EquipmentScreenName(){
			colorstring result = new colorstring(StatsName());
			result.strings[0] = new cstr(result.strings[0].s + " ",result.strings[0].color);
			for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
				if(status[(EquipmentStatus)i]){
					result.strings.Add(new cstr("*",StatusColor((EquipmentStatus)i)));
				}
			}
			return result;
		}
		public string Description(){
			switch(type){
			case WeaponType.SWORD:
				return "Sword -- A powerful 3d6 damage slashing weapon.";
			case WeaponType.MACE:
				return "Mace -- A powerful 3d6 damage bashing weapon.";
			case WeaponType.DAGGER:
				return "Dagger -- 2d6 damage. Extra chance for critical hits.";
			case WeaponType.STAFF:
				return "Staff -- 1d6 damage. Grants a small bonus to defense.";
			case WeaponType.BOW:
				return "Bow -- 3d6 damage at range. Less accurate than melee.";
			default:
				return "no weapon";
			}
		}
		public string DescriptionOfEnchantment(){
			switch(enchantment){ //todo
			case EnchantmentType.ECHOES:
				return "echoes";
			case EnchantmentType.FIRE:
				return "fire";
			case EnchantmentType.FORCE:
				return "force";
			case EnchantmentType.NULLIFICATION:
				return "nullification";
			case EnchantmentType.ICE:
				return "ice";
			}
			return "";
		}
	}
	public class Armor{
		public ArmorType type;
		public EnchantmentType enchantment;
		public Dict<EquipmentStatus,bool> status = new Dict<EquipmentStatus,bool>();
		public Armor(ArmorType type_){
			type = type_;
			enchantment = EnchantmentType.NO_ENCHANTMENT;
		}
		public Armor(ArmorType type_,EnchantmentType enchantment_){
			type = type_;
			enchantment = enchantment_;
		}
		public int Protection(){
			switch(type){
			case ArmorType.LEATHER:
				return 2;
			case ArmorType.CHAINMAIL:
				return 4;
			case ArmorType.FULL_PLATE:
				return 6;
			default:
				return 0;
			}
		}
		public int StealthPenalty(){ //todo
			switch(type){
			case ArmorType.CHAINMAIL:
				return 1;
			case ArmorType.FULL_PLATE:
				return 3;
			default:
				return 0;
			}
		}
		public override string ToString(){
			return NameWithEnchantment();
		}
		public string NameWithoutEnchantment(){
			switch(type){
			case ArmorType.LEATHER:
				return "leather";
			case ArmorType.CHAINMAIL:
				return "chainmail";
			case ArmorType.FULL_PLATE:
				return "full plate";
			default:
				return "no armor";
			}
		}
		public string NameWithEnchantment(){
			string ench = "";
			switch(enchantment){
			case EnchantmentType.ECHOES:
				ench = " of echoes";
				break;
			case EnchantmentType.FIRE:
				ench = " of fire";
				break;
			case EnchantmentType.FORCE:
				ench = " of force";
				break;
			case EnchantmentType.NULLIFICATION:
				ench = " of nullification";
				break;
			case EnchantmentType.ICE:
				ench = " of ice";
				break;
			default:
				break;
			}
			return NameWithoutEnchantment() + ench;
		}
		public cstr StatsName(){
			cstr cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.Gray;
			switch(type){
			case ArmorType.LEATHER:
				cs.s = "Leather";
				break;
			case ArmorType.CHAINMAIL:
				cs.s = "Chainmail";
				break;
			case ArmorType.FULL_PLATE:
				cs.s = "Full plate";
				break;
			default:
				cs.s = "no armor";
				break;
			}
			if(enchantment != EnchantmentType.NO_ENCHANTMENT){
				cs.s = "+" + cs.s + "+";
			}
			cs.color = EnchantmentColor();
			return cs;
		}
		public Color EnchantmentColor(){
			switch(enchantment){
			case EnchantmentType.ECHOES:
				return Color.Green;
			case EnchantmentType.FIRE:
				return Color.Red;
			case EnchantmentType.FORCE:
				return Color.Magenta;
			case EnchantmentType.NULLIFICATION:
				return Color.Cyan;
			case EnchantmentType.ICE:
				return Color.Blue;
			default:
				return Color.Gray;
			}
		}
		public colorstring EquipmentScreenName(){
			colorstring result = new colorstring(StatsName());
			result.strings[0] = new cstr(result.strings[0].s + " ",result.strings[0].color);
			for(int i=0;i<(int)EquipmentStatus.NUM_STATUS;++i){
				if(status[(EquipmentStatus)i]){
					result.strings.Add(new cstr("*",Weapon.StatusColor((EquipmentStatus)i)));
				}
			}
			return result;
		}
		/*public cstr EquipmentScreenName(){
			cstr cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.Gray;
			cs.s = NameWithEnchantment();
			cs.s = cs.s[0].ToString().ToUpper() + cs.s.Substring(1); //capitalize
			cs.color = EnchantmentColor();
			return cs;
		}*/
		public string Description(){
			switch(type){
			case ArmorType.LEATHER:
				return "Leather -- Light armor. Provides some basic protection.";
			case ArmorType.CHAINMAIL:
				return "Chainmail -- Good protection. Noisy and hard to cast in.";
			case ArmorType.FULL_PLATE:
				return "Full plate -- The thickest, noisiest, and bulkiest armor.";
			default:
				return "no armor";
			}
		}
		public string DescriptionOfEnchantment(){
			switch(enchantment){ //todo
			case EnchantmentType.ECHOES:
				return "echoes";
			case EnchantmentType.FIRE:
				return "fire";
			case EnchantmentType.FORCE:
				return "force";
			case EnchantmentType.NULLIFICATION:
				return "nullification";
			case EnchantmentType.ICE:
				return "ice";
			}
			return "";
		}
	}
	public static class MagicItem{
		public static cstr StatsName(MagicItemType type){
			cstr cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.DarkGreen;
			switch(type){
			case MagicItemType.RING_OF_PROTECTION:
				cs.s = "Ring (prot)";
				break;
			case MagicItemType.RING_OF_RESISTANCE:
				cs.s = "Ring (res)";
				break;
			case MagicItemType.PENDANT_OF_LIFE:
				cs.s = "Pendant";
				break;
			case MagicItemType.CLOAK_OF_DISAPPEARANCE:
				cs.s = "Cloak";
				break;
			default:
				cs.s = "No item";
				break;
			}
			return cs;
		}
		public static string Name(MagicItemType type){
			switch(type){
			case MagicItemType.PENDANT_OF_LIFE:
				return "pendant of life";
			case MagicItemType.RING_OF_PROTECTION:
				return "ring of protection";
			case MagicItemType.RING_OF_RESISTANCE:
				return "ring of resistance";
			case MagicItemType.CLOAK_OF_DISAPPEARANCE:
				return "cloak of disappearance";
			default:
				return "no item";
			}
		}
		public static string[] Description(MagicItemType type){
			switch(type){
			case MagicItemType.PENDANT_OF_LIFE:
				return new string[]{"Pendant of life -- Prevents a lethal attack from","finishing you, but only works once.",""};
			case MagicItemType.RING_OF_PROTECTION:
				return new string[]{"Ring of protection -- Grants a small bonus to","defense.",""};
			case MagicItemType.RING_OF_RESISTANCE:
				return new string[]{"Ring of resistance -- Grants resistance to cold,","fire, and electricity.",""};
			case MagicItemType.CLOAK_OF_DISAPPEARANCE:
				return new string[]{"Cloak of disappearance -- When your health falls,","gives you a chance to escape to safety.",""};
			default:
				return new string[]{"no","item","here"};
			}
		}
	}
}

