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
namespace Forays{
	public static class Global{
		public const int SCREEN_H = 25;
		public const int SCREEN_W = 80;
		public const int ROWS = 22;
		public const int COLS = 66;
		public const int MAP_OFFSET_ROWS = 2;
		public const int MAP_OFFSET_COLS = 13;
		public const int MAX_LIGHT_RADIUS = 12; //the maximum POSSIBLE light radius. used in light calculations.
		public static Dictionary<OptionType,bool> Options = new Dictionary<OptionType, bool>();
		public static bool Option(OptionType option){
			bool result = false;
			Options.TryGetValue(option,out result);
			return result;
		}
		public static Random r = new Random();
		public static int Roll(int dice,int sides){
			int total = 0;
			for(int i=0;i<dice;++i){
				total += r.Next(1,sides+1); //Next's maxvalue is exclusive, thus the +1
			}
			return total;
		}
		public static bool CoinFlip(){
			if(r.Next(1,2) == 2){
				return true;
			}
			else{
				return false;
			}
		}
		public static int RandomDirection(){
			int result = r.Next(1,8);
			if(result == 5){
				result = 9;
			}
			return result;
		}
	}
	public class Dict<TKey,TValue>{
		private Dictionary<TKey,TValue> d = new Dictionary<TKey,TValue>();
		public TValue this[TKey key]{
			get{
				return d.ContainsKey(key)? d[key] : default(TValue);
			}
			set{
				d[key] = value;
			}
		}
	}
	public struct pos{ //eventually, this might become part of PhysicalObject. if so, i'll create a property for 'row' and 'col'
		public int row; //so the change will be entirely transparent.
		public int col;
		public pos(int r,int c){
			row = r;
			col = c;
		}
	}
}
