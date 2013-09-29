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
using Utilities;
namespace Forays{
	public class Item : PhysicalObject{
		public ConsumableType type{get;set;}
		public int quantity{get;set;}
		public int other_data{get;set;}
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
			Define(ConsumableType.ENCHANTMENT,"scroll~ of enchantment",'?',Color.White);
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
			Define(ConsumableType.TRAP,"trap",'%',Color.White);
			proto[ConsumableType.TRAP].do_not_stack = true;
		}
		private static void Define(ConsumableType type_,string name_,char symbol_,Color color_){
			proto[type_] = new Item(type_,name_,symbol_,color_);
		}
		public Item(){}
		public Item(ConsumableType type_,string name_,char symbol_,Color color_){
			type = type_;
			quantity = 1;
			other_data = 0;
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
			other_data = i.other_data;
			ignored = false;
			do_not_stack = proto[type].do_not_stack;
			revealed_by_light = proto[type].revealed_by_light;
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
			if(U.BoundsCheck(r,c)){
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
				i = new Item(proto[type],-1,-1);
				a.GetItem(i);
				/*foreach(Item held in a.inv){
					if(held.type == type && !held.do_not_stack){
						held.quantity++;
						return held;
					}
				}
				a.inv.Add(i);*/
			}
			else{
				i = Create(type,a.row,a.col);
			}
			return i;
		}
		public string SingularName(){ return SingularName(false); }
		public string SingularName(bool include_a_or_an){
			string result;
			int position;
			if(identified[type]){
				result = name;
			}
			else{
				result = unIDed_name[type];
			}
			if(include_a_or_an){
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
			}
			position = result.IndexOf('~');
			if(position != -1){
				result = result.Substring(0,position) + result.Substring(position+1);
			}
			return result;
		}
		public string PluralName(){ //with no quantity attached
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
				result = result.Substring(0,position) + 's' + result.Substring(position+1);
			}
			return result;
		}
		public string NameWithoutQuantity(){
			if(quantity > 1){
				return PluralName();
			}
			return SingularName(false);
		}
		public string Name(){ return Name(false); }
		public string AName(){ return AName(false); }
		public string TheName(){ return TheName(false); }
		public string Name(bool consider_low_light){
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
				if(!consider_low_light || !U.BoundsCheck(row,col) || tile().IsLit()){
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
				if(!consider_low_light || !U.BoundsCheck(row,col) || tile().IsLit()){
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
				if(!consider_low_light || !U.BoundsCheck(row,col) || tile().IsLit()){
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
				if(!consider_low_light || !U.BoundsCheck(row,col) || tile().IsLit()){
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
				if(!consider_low_light || !U.BoundsCheck(row,col) || tile().IsLit()){
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
				if(!consider_low_light || !U.BoundsCheck(row,col) || tile().IsLit()){
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
			case ConsumableType.ENCHANTMENT:
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
			case ConsumableType.TRAP:
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
			case ConsumableType.ENCHANTMENT:
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
			case ConsumableType.TRAP:
				return "other";
			default:
				return "unknown item";
			}
		}
		public int SortOrderOfItemType(){
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
				return 0;
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
			case ConsumableType.ENCHANTMENT:
				return 1;
			case ConsumableType.FREEZING:
			case ConsumableType.FLAMES:
			case ConsumableType.FOG:
			case ConsumableType.DETONATION:
			case ConsumableType.BREACHING:
			case ConsumableType.SHIELDING:
			case ConsumableType.TELEPORTAL:
			case ConsumableType.PAIN:
				return 2;
			case ConsumableType.BANDAGE:
			case ConsumableType.TRAP:
				return 3;
			case ConsumableType.BLAST_FUNGUS:
				return 4;
			default:
				return 3;
			}
		}
		public static int Rarity(ConsumableType type){
			switch(type){
			case ConsumableType.ENCHANTMENT:
				return 7;
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
			case ConsumableType.TRAP:
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
					if(R.OneIn(Item.Rarity(item))){
						list.Add(item);
					}
				}
			}
			return list.Random();
		}
		public static ConsumableType RandomChestItem(){ //ignores item rarity and includes magic trinkets
			List<ConsumableType> list = new List<ConsumableType>();
			foreach(ConsumableType item in Enum.GetValues(typeof(ConsumableType))){
				if(Item.Rarity(item) >= 1){
					list.Add(item);
				}
			}
			if(R.OneIn(player.magic_trinkets.Count + 1) && player.magic_trinkets.Count < 10){
				for(int i=0;i<3;++i){
					list.Add(ConsumableType.TRAP); //hack! traps aren't ever actually generated - this value means 'generate a magic trinket'
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
					int num = R.Roll(potion_flavors.Count) - 1;
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
							identified[type] = true; //bandage, trap, blast fungus...
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
				syllables = R.Roll(4) + 2;
				syllable_count = new List<int>();
				while(syllables > 0){
					if(syllable_count.Count == 2){
						syllable_count.Add(syllables);
						syllables = 0;
						break;
					}
					int R2 = Math.Min(syllables,3);
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
					int s = R.Roll(R2 - D) + D;
					syllable_count.Add(s);
					syllables -= s;
				}
			}
			while(!syllable_count.Any(x => x!=1)); // if every word has only 1 syllable, try again
			string result = "";
			while(syllable_count.Count > 0){
				string word = "";
				if(R.OneIn(5)){
					word = word + vowel.Random();
				}
				for(int count = syllable_count.RemoveRandom();count > 0;--count){
					word = word + consonant.Random() + vowel.Random();
					/*if(R.OneIn(20)){ //used for the Japanese-inspired one
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
				B.Add("You transform into a being of animated stone. "); //todo message
				int duration = R.Roll(2,20) + 20;
				List<AttrType> attributes = new List<AttrType>{AttrType.REGENERATING,AttrType.BRUTISH_STRENGTH,AttrType.VIGOR,AttrType.SILENCED,AttrType.SHADOW_CLOAK};
				foreach(AttrType at in attributes){
					if(user.HasAttr(at)){
						user.attrs[at] = 0;
						Q.KillEvents(user,at);
						switch(at){
						case AttrType.REGENERATING:
							B.Add("You no longer regenerate. ");
							break;
						case AttrType.BRUTISH_STRENGTH:
							B.Add("Your brutish strength fades. ");
							break;
						case AttrType.VIGOR:
							B.Add("Your extraordinary speed fades. ");
							break;
						case AttrType.SILENCED:
							B.Add("You are no longer silenced. ");
							break;
						case AttrType.SHADOW_CLOAK:
							B.Add("You are no longer cloaked. ");
							break;
						}
					}
				}
				if(user.HasAttr(AttrType.LIGHT_SENSITIVE)){ //hacky way to detect vampirism
					user.attrs[AttrType.LIGHT_SENSITIVE] = 0;
					user.attrs[AttrType.FLYING] = 0;
					user.attrs[AttrType.LIFE_DRAIN_HIT] = 0; //this will break if the player can gain these from anything else
					Q.KillEvents(user,AttrType.LIGHT_SENSITIVE);
					Q.KillEvents(user,AttrType.FLYING);
					Q.KillEvents(user,AttrType.LIFE_DRAIN_HIT);
					B.Add("You are no longer vampiric. ");
				}
				if(user.HasAttr(AttrType.ROOTS)){
					foreach(Event e in Q.list){
						if(e.target == user && !e.dead){
							if(e.attr == AttrType.IMMOBILE && e.msg.Contains("rooted to the ground")){
								e.dead = true;
								user.attrs[AttrType.IMMOBILE]--;
								B.Add("You are no longer rooted to the ground. ");
							}
							else{
								if(e.attr == AttrType.BONUS_DEFENSE && e.value == 10){
									e.dead = true; //this would break if there were other timed effects that gave 5 defense
									user.attrs[AttrType.BONUS_DEFENSE] -= 10; //todo test this. make sure all the effects end properly.
								}
								else{
									if(e.attr == AttrType.ROOTS){
										e.dead = true;
										user.attrs[AttrType.ROOTS]--;
									}
								}
							}
						}
					}
				}
				user.attrs[AttrType.IMMUNE_BURNING]++;
				Q.Add(new Event(user,duration*100,AttrType.IMMUNE_BURNING));
				user.RefreshDuration(AttrType.NONLIVING,duration*100,"Your rocky form reverts to flesh. ");
				break;
			}
			case ConsumableType.VAMPIRISM:
			{
				B.Add("You become vampiric. ");
				int duration = R.Roll(20) + 20;
				user.RefreshDuration(AttrType.LIGHT_SENSITIVE,duration*100);
				user.RefreshDuration(AttrType.FLYING,duration*100); //todo: i'm pretty sure this can break with other sources of flying.
				user.RefreshDuration(AttrType.LIFE_DRAIN_HIT,duration*100,"You are no longer vampiric. ");
				break;
			}
			case ConsumableType.BRUTISH_STRENGTH:
			{
				B.Add("You feel a surge of strength. ");
				user.RefreshDuration(AttrType.BRUTISH_STRENGTH,(R.Roll(3,6)+6)*100,"Your incredible strength wears off. ");
				break;
			}
			case ConsumableType.ROOTS:
			{
				if(user.HasAttr(AttrType.ROOTS)){
					foreach(Event e in Q.list){
						if(e.target == user && !e.dead){
							if(e.attr == AttrType.IMMOBILE && e.msg.Contains("rooted to the ground")){
								e.dead = true;
								user.attrs[AttrType.IMMOBILE]--;
							}
							else{
								if(e.attr == AttrType.BONUS_DEFENSE && e.value == 10){
									e.dead = true; //this would break if there were other timed effects that gave 5 defense
									user.attrs[AttrType.BONUS_DEFENSE] -= 10; //todo test this. make sure all the effects end properly.
								}
								else{
									if(e.attr == AttrType.ROOTS){
										e.dead = true;
										user.attrs[AttrType.ROOTS]--;
									}
								}
							}
						}
					}
					//todo: message when refreshed?
				}
				else{
					B.Add("You grow roots and a hard shell of bark. ");
				}
				int duration = R.Roll(20) + 20;
				user.RefreshDuration(AttrType.ROOTS,duration*100);
				user.attrs[AttrType.BONUS_DEFENSE] += 10;
				Q.Add(new Event(user,duration*100,AttrType.BONUS_DEFENSE,10)); //todo now, check every form of movement to see whether it should be stopped.
				user.attrs[AttrType.IMMOBILE]++; //MOST of them should be stopped, but not breach events.
				Q.Add(new Event(user,duration*100,AttrType.IMMOBILE,"You are no longer rooted to the ground. "));
				break;
			}
			case ConsumableType.VIGOR:
			{
				B.Add("You start moving with extraordinary speed. ");
				if(user.exhaustion > 0){
					user.exhaustion = 0;
					B.Add("Your fatigue disappears. ");
				}
				user.RefreshDuration(AttrType.VIGOR,(R.Roll(2,10) + 10)*100,"Your extraordinary speed fades. ");
				break;
			}
			case ConsumableType.SILENCE:
			{
				B.Add("A hush falls around you. ");
				user.RefreshDuration(AttrType.SILENCED,(R.Roll(2,20)+20)*100,"You are no longer silenced. ");
				break;
			}
			case ConsumableType.CLOAKING:
				if(user.tile().IsLit()){
					B.Add("You would feel at home in the shadows. ");
				}
				else{
					B.Add("You fade away in the darkness. ");
				}
				user.RefreshDuration(AttrType.SHADOW_CLOAK,(R.Roll(2,20)+30)*100,"You are no longer cloaked. ",user);
				break;
			case ConsumableType.BLINKING:
			{
				List<Tile> tiles = user.TilesWithinDistance(8).Where(x => x.passable && x.actor() == null && user.ApproximateEuclideanDistanceFromX10(x) >= 45);
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
			}
			case ConsumableType.PASSAGE:
			{
				List<int> valid_dirs = new List<int>();
				foreach(int dir in U.FourDirections){
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
				int duration = R.Roll(20)+30;
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
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even brighter for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
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
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
				}
				else{
					B.Add("The air grows even darker for a moment. ");
					Q.KillEvents(null,EventType.NORMAL_LIGHTING);
					Q.Add(new Event((R.Roll(2,20) + 120) * 100,EventType.NORMAL_LIGHTING));
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
			case ConsumableType.ENCHANTMENT:
			{
				EnchantmentType ench = (EnchantmentType)R.Between(0,4);
				while(ench == user.EquippedWeapon.enchantment){
					ench = (EnchantmentType)R.Between(0,4);
				}
				user.EquippedWeapon.enchantment = ench;
				B.Add("Your " + user.EquippedWeapon.NameWithoutEnchantment() + " is now a " + user.EquippedWeapon.NameWithEnchantment() + "! ");
				//todo message?
				break;
			}
			case ConsumableType.FREEZING:
			{
				if(line == null){
					int radius = 2;
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
					user.AnimateExplosion(t,2,'*',Color.RandomIce); //todo: improve animation
					List<Actor> targets = new List<Actor>();
					Tile LOE_tile = t;
					if(!t.passable && prev != null){
						LOE_tile = prev;
					}
					foreach(Actor ac in t.ActorsWithinDistance(2)){
						if(LOE_tile.HasLOE(ac)){
							targets.Add(ac);
						}
					}
					while(targets.Count > 0){
						Actor ac = targets.RemoveRandom();
						B.Add(ac.YouAre() + " encased in ice. ",ac);
						ac.attrs[AttrType.FROZEN] = 35;
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
				if(line == null){
					int radius = 2;
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
					foreach(Tile tile in t.TilesWithinDistance(2)){
						if(tile.passable && LOE_tile.HasLOE(tile)){
							tile.AddFeature(FeatureType.FIRE);
							area.Add(tile);
							cells.Add(tile.p);
						}
					}
					Screen.AnimateMapCells(cells,new colorchar('&',Color.RandomFire));
					//Q.Add(new Event(area,600,EventType.,25)); //todo: fire event
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
					Screen.AnimateMapCells(cells,new colorchar('*',Color.Gray));
					Q.RemoveTilesFromEventAreas(area,EventType.FOG);
					Q.Add(new Event(area,600,EventType.FOG,25));
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
				if(line == null){
					int radius = 2;
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
					foreach(Tile tile in LOE_tile.TilesWithinDistance(2)){
						if(LOE_tile.HasLOE(tile)){
							cells.Add(tile.p);
						}
					}
					Screen.AnimateMapCells(cells,new colorchar('*',Color.RandomExplosion));
					for(int rad=2;rad>=0;--rad){
						foreach(pos p in LOE_tile.PositionsAtDistance(rad)){
							if(LOE_tile.HasLOE(p.row,p.col)){
								Actor a2 = M.actor[p];
								if(a2 != null){
									switch(rad){
									case 2:
										a2.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(10,6),user,"an orb of detonation");
										break;
									case 1:
									case 0:
									{
										a2.attrs[AttrType.TURN_INTO_CORPSE] = 1;
										a2.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(10,6),user,"an orb of detonation");
										if(a2.curhp > 0 || !a2.HasAttr(AttrType.NO_CORPSE_KNOCKBACK)){
											LOE_tile.KnockObjectBack(a2);
										}
										a2.CorpseCleanup();
										break;
									}
									}
								}
								if(p.BoundsCheck(M.tile,false)){
									Tile t2 = M.tile[p];
									if(t2.type == TileType.CRACKED_WALL){
										t2.Toggle(null,TileType.FLOOR);
										foreach(Tile neighbor in t2.TilesAtDistance(1)){
											neighbor.solid_rock = false;
										}
									}
									else{
										if(t2.type == TileType.WALL && R.PercentChance(70)){
											t2.Toggle(null,TileType.CRACKED_WALL);
										}
									}
								}
							}
						}
					}
					t.MakeNoise(8);
					if(trigger_trap && t.IsTrap()){
						t.TriggerTrap();
					}
				}
				else{
					used = false;
				}
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
					int max_dist = -1;
					foreach(Tile t2 in M.ReachableTilesByDistance(t.row,t.col,false,TileType.WALL,TileType.STONE_SLAB,TileType.DOOR_C,TileType.STALAGMITE,TileType.RUBBLE,TileType.HIDDEN_DOOR)){
						if(t.DistanceFrom(t2) > 5){
							break;
						}
						if(t2.type == TileType.WALL){
							Screen.WriteMapChar(t2.row,t2.col,t2.symbol,Color.RandomBreached);
							if(t.DistanceFrom(t2) > max_dist){
								max_dist = t.DistanceFrom(t2);
								Thread.Sleep(50);
							}
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
					line = user.GetTargetTile(12,radius,!identified[type]); //don't suggest shielding monsters once it's known
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
					List<colorchar> symbols = new List<colorchar>();
					Tile LOE_tile = t;
					if(!t.passable && prev != null){
						LOE_tile = prev;
					}
					foreach(Tile tile in t.TilesWithinDistance(1)){
						if(tile.passable && LOE_tile.HasLOE(tile)){
							if(tile.actor() != null){
								if(tile.actor().attrs[AttrType.ARCANE_SHIELDED] < 10){
									tile.actor().attrs[AttrType.ARCANE_SHIELDED] = 10;
								}
								symbols.Add(new colorchar(tile.actor().symbol,Color.Blue));
							}
							else{
								symbols.Add(new colorchar('+',Color.Blue));
							}
							cells.Add(tile.p);
							area.Add(tile);
						}
					}
					Screen.AnimateMapCells(cells,symbols,150);
					foreach(Tile tile in area){
						if(player.CanSee(tile)){
							B.Add("A zone of protection is created. ");
							break;
						}
					}
					Q.Add(new Event(area,100,EventType.SHIELDING,R.Roll(2,6)+6));
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
					Q.Add(new Event(target_tile,0,EventType.TELEPORTAL,100));
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
					List<colorchar> symbols = new List<colorchar>();
					Tile LOE_tile = t;
					if(!t.passable && prev != null){
						LOE_tile = prev;
					}
					foreach(Tile tile in t.TilesWithinDistance(5)){
						if(LOE_tile.HasLOE(tile)){
							if(tile.actor() != null){
								if(tile.actor().TakeDamage(DamageType.MAGIC,DamageClass.MAGICAL,R.Roll(2,6),user,"an orb of pain")){
									B.Add(tile.actor().You("become") + " vulnerable. ",tile.actor()); //todo: implement VULNERABLE
									tile.actor().RefreshDuration(AttrType.VULNERABLE,(R.Roll(2,6)+6)*100,tile.actor().YouFeel() + " less vulnerable. ",tile.actor());
								}
							}
							if(tile.DistanceFrom(t) % 2 == 0){
								symbols.Add(new colorchar('*',Color.DarkMagenta));
							}
							else{
								symbols.Add(new colorchar('*',Color.DarkRed));
							}
							cells.Add(tile.p);
						}
					}
					Screen.AnimateMapCells(cells,symbols,80); //todo: I need that "reduce to visible" method for these animations.
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
				user.recover_time = Q.turn + 500;
				if(user.name == "you"){
					B.Add("You apply a bandage. ");
				}
				else{
					B.Add(user.the_name + " applies a bandage. ",user);
				}
				break;
			case ConsumableType.TRAP:
			{
				if(user.tile().Is(TileType.FLOOR)){
					user.tile().Toggle(user,(TileType)other_data);
					B.Add("You arm " + user.tile().the_name + ". ");
				}
				else{
					B.Add("You can't place a trap here. ");
					used = false;
				}
				break;
			}
			default:
				used = false;
				break;
			}
			if(used){
				if(IDed){
					if(!identified[type]){
						identified[type] = true;
						B.Add("(It was " + SingularName(true) + "!) ");
					}
				}
				else{
					if(!unIDed_name[type].Contains("{tried}")){
						unIDed_name[type] = unIDed_name[type] + " {tried}";
					}
				}
				if(quantity > 1){
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
		public string Description(){
			if(!revealed_by_light){
				if(NameOfItemType(type) == "scroll"){
					return "Unseen scroll. Lorem ipsum blah blah etc etc more info about scroltions and porbs goes here, bizzaro am number one. ";
				}
				else{
					if(NameOfItemType(type) == "potion"){
						return "Unseen potion. Lorem ipsum blah blah etc etc more info about scroltions and porbs goes here, bizzaro am number one. ";
					}
					else{
						if(NameOfItemType(type) == "orb"){
							return "Unseen orb. Lorem ipsum blah blah etc etc more info about scroltions and porbs goes here, bizzaro am number one. ";
						}
					}
				}
			}
			else{
				if(!identified[type]){
					if(NameOfItemType(type) == "scroll"){
						return "Unknown scroll. Lorem ipsum blah blah etc etc more info about scroltions and porbs goes here, bizzaro am number one. ";
					}
					else{
						if(NameOfItemType(type) == "potion"){
							return "Unknown potion. Lorem ipsum blah blah etc etc more info about scroltions and porbs goes here, bizzaro am number one. ";
						}
						else{
							if(NameOfItemType(type) == "orb"){
								return "Unknown orb. Lorem ipsum blah blah etc etc more info about scroltions and porbs goes here, bizzaro am number one. ";
							}
						}
					}
				}
				switch(type){
				default:
					return "[DATA EXPUNGED] YOU DO NOT HAVE CLEARANCE TO ACCESS THIS INFORMATION [DATA EXPUNGED]";
				}
			}
			return "Unknown item.";
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
				return new AttackInfo(100,2,CriticalEffect.KNOCKBACK,"& hit *");
			case WeaponType.DAGGER:
				return new AttackInfo(100,2,CriticalEffect.STUN,"& hit *");
			case WeaponType.STAFF:
				return new AttackInfo(100,2,CriticalEffect.TRIP,"& hit *");
			case WeaponType.BOW: //bow's melee damage
				return new AttackInfo(100,1,CriticalEffect.NO_CRIT,"& hit *");
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
			case EnchantmentType.CHILLING:
				ench = " of chilling";
				break;
			case EnchantmentType.PRECISION:
				ench = " of precision";
				break;
			case EnchantmentType.DISRUPTION:
				ench = " of disruption";
				break;
			case EnchantmentType.VICTORY:
				ench = " of victory";
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
				return Color.Magenta;
			case EnchantmentType.CHILLING:
				return Color.Blue;
			case EnchantmentType.PRECISION:
				return Color.White;
			case EnchantmentType.DISRUPTION:
				return Color.Yellow;
			case EnchantmentType.VICTORY:
				return Color.Red;
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
			case EquipmentStatus.BURDENSOME: //todo update
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
				return "Sword -- A powerful 3d6 damage slashing weapon."; //todo update
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
			case EnchantmentType.CHILLING:
				return "todo";
			case EnchantmentType.PRECISION:
				return "todo";
			case EnchantmentType.DISRUPTION:
				return "todo";
			case EnchantmentType.VICTORY:
				return "todo";
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
				return 5;
			case ArmorType.FULL_PLATE:
				return 8;
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
			/*switch(enchantment){ todo
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
			}*/
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
			/*switch(enchantment){
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
			default:*/
				return Color.Gray;
			//}todo
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
			/*switch(enchantment){ //todo
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
			}*/
			return "";
		}
	}
	public static class MagicTrinket{
		public static cstr StatsName(MagicTrinketType type){ //todo remove this, right?
			cstr cs;
			cs.bgcolor = Color.Black;
			cs.color = Color.DarkGreen;
			switch(type){
			/*case MagicTrinketType.RING_OF_PROTECTION:
				cs.s = "Ring (prot)";
				break;
			case MagicTrinketType.RING_OF_RESISTANCE:
				cs.s = "Ring (res)";
				break;*/
			case MagicTrinketType.PENDANT_OF_LIFE:
				cs.s = "Pendant";
				break;
			case MagicTrinketType.CLOAK_OF_SAFETY:
				cs.s = "Cloak";
				break;
			default:
				cs.s = "No item";
				break;
			}
			return cs;
		}
		public static string Name(MagicTrinketType type){
			switch(type){
			case MagicTrinketType.PENDANT_OF_LIFE:
				return "pendant of life";
			/*case MagicTrinketType.RING_OF_PROTECTION:
				return "ring of protection";
			case MagicTrinketType.RING_OF_RESISTANCE:
				return "ring of resistance";*/
			case MagicTrinketType.CLOAK_OF_SAFETY:
				return "cloak of safety";
			case MagicTrinketType.BELT_OF_TOUGHNESS:
				return "belt of toughness";
			case MagicTrinketType.BRACERS_OF_ARROW_DEFLECTION:
				return "bracers of arrow deflection";
			case MagicTrinketType.CIRCLET_OF_THE_THIRD_EYE:
				return "circlet of the third eye";
			case MagicTrinketType.LENS_OF_SCRYING:
				return "lens of scrying";
			case MagicTrinketType.RING_OF_KEEN_SIGHT:
				return "ring of keen sight";
			case MagicTrinketType.RING_OF_THE_LETHARGIC_FLAME:
				return "ring of the lethargic flame";
			default:
				return "no item";
			}
		}
		public static string[] Description(MagicTrinketType type){
			switch(type){
			case MagicTrinketType.PENDANT_OF_LIFE:
				return new string[]{"Pendant of life -- Prevents a lethal attack from","finishing you, but only works once.",""};
			/*case MagicTrinketType.RING_OF_PROTECTION:
				return new string[]{"Ring of protection -- Grants a small bonus to","defense.",""};
			case MagicTrinketType.RING_OF_RESISTANCE:
				return new string[]{"Ring of resistance -- Grants resistance to cold,","fire, and electricity.",""};*/
			case MagicTrinketType.CLOAK_OF_SAFETY:
				return new string[]{"Cloak of disappearance -- When your health falls,","gives you a chance to escape to safety.",""};
			default:
				return new string[]{"no","item","here"};
			}
		}
	}
}

