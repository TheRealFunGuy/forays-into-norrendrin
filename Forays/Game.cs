using System;
namespace Forays{
	public class Game{
		public Map M;
		public Queue Q;
		public Buffer B;
		public Actor player;
		private int turn_count;
		public int turn(){ return turn_count; }
		public void advance(int turn){ turn_count = turn; }
		public Game (){
			M = new Map();
			Q = new Queue();
			B = new Buffer();
			player = new Actor();
			turn_count=0;
		}
	}
}

