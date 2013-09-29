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
using Utilities;
namespace Forays{
	public class PhysicalObject{
		public pos p;
		public int row{
			get{
				return p.row;
			}
			set{
				p.row = value;
			}
		}
		public int col{
			get{
				return p.col;
			}
			set{
				p.col = value;
			}
		}
		public string name{get;set;}
		public string a_name{get;set;}
		public string the_name{get;set;}
		public char symbol{get;set;}
		public Color color{get;set;}
		public int light_radius{get;set;}
		
		public static Map M{get;set;}
		public static Buffer B{get;set;}
		public static Queue Q{get;set;}
		public static Actor player{get;set;}
		public PhysicalObject(){
			row=-1;
			col=-1;
			name="";
			a_name="";
			the_name="";
			symbol='%';
			color=Color.White;
			light_radius = 0;
		}
		public PhysicalObject(string name_,char symbol_,Color color_){
			row = -1;
			col = -1;
			SetName(name_);
			symbol = symbol_;
			color = color_;
			light_radius = 0;
		}
		public void SetName(string new_name){
			name = new_name;
			the_name = "the " + name;
			a_name = "a " + name;
			if(name=="you"){
				the_name = "you";
				a_name = "you";
			}
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
			}
		}
		public void Cursor(){
			Console.SetCursorPosition(col+Global.MAP_OFFSET_COLS,row+Global.MAP_OFFSET_ROWS);
		}
		public void UpdateRadius(int from,int to){ UpdateRadius(from,to,false); }
		public void UpdateRadius(int from,int to,bool change){
			if(from > 0){
				for(int i=row-from;i<=row+from;++i){
					for(int j=col-from;j<=col+from;++j){
						if(i>0 && i<Global.ROWS-1 && j>0 && j<Global.COLS-1){
							if(!M.tile[i,j].opaque && HasBresenhamLineOfSight(i,j)){
								M.tile[i,j].light_value--;
							}
						}
					}
				}
			}
			if(to > 0){
				for(int i=row-to;i<=row+to;++i){
					for(int j=col-to;j<=col+to;++j){
						if(i>0 && i<Global.ROWS-1 && j>0 && j<Global.COLS-1){
							if(!M.tile[i,j].opaque && HasBresenhamLineOfSight(i,j)){
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
		public void MakeNoise(int volume){
			if(actor() != null && actor().HasAttr(AttrType.SILENCED)){
				return;
			}
			List<Actor> actors = new List<Actor>();
			int minrow = Math.Max(1,row-volume);
			int maxrow = Math.Min(Global.ROWS-2,row+volume);
			int mincol = Math.Max(1,col-volume);
			int maxcol = Math.Min(Global.COLS-2,col+volume);
			int[,] values = new int[Global.ROWS,Global.COLS];
			for(int i=minrow;i<=maxrow;++i){
				for(int j=mincol;j<=maxcol;++j){
					if(M.tile[i,j].passable){
						values[i,j] = 0;
					}
					else{
						values[i,j] = -1;
					}
				}
			}
			values[row,col] = 1;
			/*if(actor() != null){
				actors.Add(actor());
			}*/
			int val = 1;
			while(true){
				for(int i=minrow;i<=maxrow;++i){
					for(int j=mincol;j<=maxcol;++j){
						if(values[i,j] == val){
							for(int s=i-1;s<=i+1;++s){
								for(int t=j-1;t<=j+1;++t){
									if(s != i || t != j){
										if(values[s,t] == 0){
											values[s,t] = val + 1;
											if(M.actor[s,t] != null){
												actors.Add(M.actor[s,t]);
											}
										}
									}
								}
							}
						}
					}
				}
				++val;
				if(val > volume){
					break;
				}
			}
			foreach(Actor a in actors){
				if(a != player){ //todo: let the player hear sounds with a message?
					if(!a.CanSee(player) && a.target_location == null && !a.HasAttr(AttrType.AMNESIA_STUN)){ //if they already have an idea of where the player is/was, they won't bother
						if(volume > 2 || !a.HasAttr(AttrType.IGNORES_QUIET_SOUNDS)){ //(and amnesia stun makes them ignore all sounds)
							a.FindPath(this);
							if(R.CoinFlip()){
								a.attrs[AttrType.IGNORES_QUIET_SOUNDS]++; //repeated quiet sounds are ignored, eventually...
							}
						}
					}
				}
				else{
					if(this != player && this is Actor){
						(this as Actor).attrs[AttrType.DANGER_SENSED] = 1;
					}
				}
			}
		}
		public bool KnockObjectBack(Actor a){ return KnockObjectBack(a,1); }
		public bool KnockObjectBack(Actor a,int knockback_strength){ //todo the return value should answer the question "did it survive?" just like TakeDamage does.
			if(knockback_strength == 0){ //note that TURN_INTO_CORPSE should be set for 'a' - therefore it won't be removed and we can do what we want with it.
				return a.CollideWith(a.tile()); //todo message?
			}
			List<Tile> line = null;
			if(DistanceFrom(a) == 0){
				line = GetBestExtendedLineOfEffect(TileInDirection(Global.RandomDirection()));
			}
			else{
				line = GetBestExtendedLineOfEffect(a); //todo: print "you are knocked back" !
			}
			int i=0;
			while(true){
				Tile t = line[i];
				if(t.actor() == a){
					break;
				}
				++i;
			}
			line.RemoveRange(0,i+1);
			bool immobile = false;
			while(knockback_strength > 1){ //if the knockback strength is greater than 1, you're passing *over* at least one tile.
				Tile t = line[0];
				line.RemoveAt(0);
				immobile = (a.GrabPreventsMovement(t) || a.HasAttr(AttrType.IMMOBILE) || a.HasAttr(AttrType.FROZEN));
				if(immobile){
					if(player.CanSee(a.tile())){
						B.Add(a.YouVisibleAre() + " knocked about. ",a);
					}
					return a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"crashing into the floor");
				}
				if(!t.passable){
					string deathstringname = t.AName(false);
					if(t.Is(TileType.CRACKED_WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR) && !a.HasAttr(AttrType.SMALL)){
						string tilename = t.TheName(true);
						if(t.type == TileType.HIDDEN_DOOR){
							tilename = "a hidden door";
							t.Toggle(null);
						}
						if(player.CanSee(a.tile())){
							B.Add(a.YouVisibleAre() + " knocked through " + tilename + ". ",a,t);
						}
						knockback_strength -= 2; //todo: is this what I want to do here?
						t.Toggle(null);
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"slamming into " + deathstringname);
						a.Move(t.row,t.col); //todo: test this code
					}
					else{
						if(player.CanSee(a.tile())){
							B.Add(a.YouVisibleAre() + " knocked into " + t.TheName(true) + ". ",a,t);
						}
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"slamming into " + deathstringname);
						a.CollideWith(a.tile());
						return !a.HasAttr(AttrType.CORPSE);
					}
				}
				else{
					if(t.actor() != null){
						if(player.CanSee(a.tile()) || player.CanSee(t)){
							B.Add(a.YouVisibleAre() + " knocked into " + t.actor().TheName(true) + ". ",a,t.actor());
						}
						string actorname = t.actor().AName(false);
						string actorname2 = a.AName(false);
						t.actor().TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"slamming into " + actorname2); //todo: how about "colliding with a foo" instead?
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"slamming into " + actorname);
						a.CollideWith(a.tile());
						return !a.HasAttr(AttrType.CORPSE);
					}
					else{
						a.Move(t.row,t.col,false);
						if(a.HasAttr(AttrType.BURNING)){
							t.ApplyEffect(DamageType.FIRE); //todo: is this redundant? - does Move call this anyway?
						}
					}
				}
				knockback_strength--;
			}
			if(knockback_strength < 1){
				return !a.HasAttr(AttrType.CORPSE);
			}
			bool slip = false;
			bool slip_message_printed = false;
			do{
				Tile t = line[0];
				line.RemoveAt(0);
				immobile = (a.GrabPreventsMovement(t) || a.HasAttr(AttrType.IMMOBILE) || a.HasAttr(AttrType.FROZEN));
				if(immobile){
					if(player.CanSee(a.tile())){
						B.Add(a.YouVisibleAre() + " knocked about. ",a);
					}
					return a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"crashing into the floor");
				}
				if(!t.passable){
					string deathstringname = t.AName(false);
					if(t.Is(TileType.CRACKED_WALL,TileType.DOOR_C,TileType.HIDDEN_DOOR) && !a.HasAttr(AttrType.SMALL)){
						string tilename = t.TheName(true);
						if(t.type == TileType.HIDDEN_DOOR){
							tilename = "a hidden door";
							t.Toggle(a);
						}
						if(player.CanSee(a.tile())){
							B.Add(a.YouVisibleAre() + " knocked through " + tilename + ". ",a,t);
						}
						t.Toggle(a);
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"slamming into " + deathstringname);
						a.Move(t.row,t.col);
						return !a.HasAttr(AttrType.CORPSE);
					}
					else{
						if(player.CanSee(a.tile())){
							B.Add(a.YouVisibleAre() + " knocked into " + t.TheName(true) + ". ",a,t);
						}
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"slamming into " + deathstringname);
						a.CollideWith(a.tile());
						return !a.HasAttr(AttrType.CORPSE);
					}
				}
				else{
					if(t.actor() != null){
						if(player.CanSee(a.tile()) || player.CanSee(t)){
							B.Add(a.YouVisibleAre() + " knocked into " + t.actor().TheName(true) + ". ",a,t.actor());
						}
						string actorname = t.actor().AName(false);
						string actorname2 = a.AName(false);
						t.actor().TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"slamming into " + actorname2);
						a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,R.Roll(6),null,"slamming into " + actorname);
						a.CollideWith(a.tile());
						return !a.HasAttr(AttrType.CORPSE);
					}
					else{
						slip = false;
						if(t.Is(TileType.ICE) || t.Is(FeatureType.OIL,FeatureType.SLIME)){
							slip = true;
							if(!slip_message_printed){
								slip_message_printed = true;
								B.Add(a.You("slide") + "! ");
							}
						}
						bool interrupted = false;
						if(t.inv != null && t.inv.type == ConsumableType.DETONATION){ //this will cause a new knockback effect and end the current one
							interrupted = true;
						}
						a.Move(t.row,t.col);
						//todo: something around here needs to handle being knocked into fire - does it refresh burning immediately?
						a.CollideWith(a.tile());
						/*if(t.Is(TileType.FIREPIT)){
							B.Add("TODO: fire pit! ");
						}
						if(a.HasAttr(AttrType.BURNING)){
							t.ApplyEffect(DamageType.FIRE); //todo: is this redundant? - does Move call this anyway?
						}
						t.ApplyEffect(DamageType.NORMAL); //todo: corpse check here, or does 'interrupted' handle that?*/
						if(interrupted){
							return !a.HasAttr(AttrType.CORPSE);
						}
					}
				}
			}
			while(slip);
			return !a.HasAttr(AttrType.CORPSE);
		}
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
		virtual public string YouVisible(string s){ return YouVisible(s,false); }
		virtual public string YouVisible(string s,bool ends_in_es){ //same as You(). overridden by Actor.
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
		public int DistanceFrom(PhysicalObject o){ return DistanceFrom(o.row,o.col); }
		public int DistanceFrom(pos p){ return DistanceFrom(p.row,p.col); }
		public int DistanceFrom(int r,int c){
			int dy = Math.Abs(r-row);
			int dx = Math.Abs(c-col);
			if(dx > dy){
				return dx;
			}
			else{
				return dy;
			}
		}
		public int ApproximateEuclideanDistanceFromX10(PhysicalObject o){ return ApproximateEuclideanDistanceFromX10(o.row,o.col); }
		public int ApproximateEuclideanDistanceFromX10(pos p){ return ApproximateEuclideanDistanceFromX10(p.row,p.col); }
		public int ApproximateEuclideanDistanceFromX10(int r,int c){ // x10 so that orthogonal directions are closer than diagonals
			int dy = Math.Abs(r-row) * 10;
			int dx = Math.Abs(c-col) * 10;
			if(dx > dy){
				return dx + (dy/2); //not perfect, but it gets the job done
			}
			else{
				return dy + (dx/2);
			}
		}
		public Actor ActorInDirection(int dir){
			switch(dir){
			case 7:
				if(M.BoundsCheck(row-1,col-1)){
					return M.actor[row-1,col-1];
				}
				break;
			case 8:
				if(M.BoundsCheck(row-1,col)){
					return M.actor[row-1,col];
				}
				break;
			case 9:
				if(M.BoundsCheck(row-1,col+1)){
					return M.actor[row-1,col+1];
				}
				break;
			case 4:
				if(M.BoundsCheck(row,col-1)){
					return M.actor[row,col-1];
				}
				break;
			case 5:
				if(M.BoundsCheck(row,col)){
					return M.actor[row,col];
				}
				break;
			case 6:
				if(M.BoundsCheck(row,col+1)){
					return M.actor[row,col+1];
				}
				break;
			case 1:
				if(M.BoundsCheck(row+1,col-1)){
					return M.actor[row+1,col-1];
				}
				break;
			case 2:
				if(M.BoundsCheck(row+1,col)){
					return M.actor[row+1,col];
				}
				break;
			case 3:
				if(M.BoundsCheck(row+1,col+1)){
					return M.actor[row+1,col+1];
				}
				break;
			default:
				return null;
			}
			return null;
		}
		public Tile TileInDirection(int dir){
			switch(dir){
			case 7:
				if(M.BoundsCheck(row-1,col-1)){
					return M.tile[row-1,col-1];
				}
				break;
			case 8:
				if(M.BoundsCheck(row-1,col)){
					return M.tile[row-1,col];
				}
				break;
			case 9:
				if(M.BoundsCheck(row-1,col+1)){
					return M.tile[row-1,col+1];
				}
				break;
			case 4:
				if(M.BoundsCheck(row,col-1)){
					return M.tile[row,col-1];
				}
				break;
			case 5:
				if(M.BoundsCheck(row,col)){
					return M.tile[row,col];
				}
				break;
			case 6:
				if(M.BoundsCheck(row,col+1)){
					return M.tile[row,col+1];
				}
				break;
			case 1:
				if(M.BoundsCheck(row+1,col-1)){
					return M.tile[row+1,col-1];
				}
				break;
			case 2:
				if(M.BoundsCheck(row+1,col)){
					return M.tile[row+1,col];
				}
				break;
			case 3:
				if(M.BoundsCheck(row+1,col+1)){
					return M.tile[row+1,col+1];
				}
				break;
			default:
				return null;
			}
			return null;
		}
		public Actor FirstActorInLine(PhysicalObject obj){ return FirstActorInLine(obj,1); }
		public Actor FirstActorInLine(PhysicalObject obj,int num){
			if(obj == null){
				return null;
			}
			int count = 0;
			List<Tile> line = GetBestLineOfEffect(obj.row,obj.col);
			line.RemoveAt(0);
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
		public Actor FirstActorInLine(List<Tile> line){ return FirstActorInLine(line,1); }
		public Actor FirstActorInLine(List<Tile> line,int num){
			if(line == null){
				return null;
			}
			int count = 0;
			int idx = 0; //note that the first position is thrown out, as it is assumed to be the origin of the line
			foreach(Tile t in line){
				if(idx != 0){
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
				++idx;
			}
			return null;
		}
		public Actor FirstActorInExtendedLine(PhysicalObject obj){ return FirstActorInExtendedLine(obj,1,-1); }
		public Actor FirstActorInExtendedLine(PhysicalObject obj,int max_distance){ return FirstActorInExtendedLine(obj,1,max_distance); }
		public Actor FirstActorInExtendedLine(PhysicalObject obj,int num,int max_distance){
			if(obj == null){
				return null;
			}
			int count = 0;
			List<Tile> line = GetBestExtendedLineOfEffect(obj.row,obj.col);
			line.RemoveAt(0);
			foreach(Tile t in line){
				if(!t.passable){
					return null;
				}
				if(max_distance != -1 && DistanceFrom(t) > max_distance){
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
		public Tile FirstSolidTileInLine(PhysicalObject obj){ return FirstSolidTileInLine(obj,1); }
		public Tile FirstSolidTileInLine(PhysicalObject obj,int num){
			if(obj == null){
				return null;
			}
			int count = 0;
			List<Tile> line = GetBestLineOfEffect(obj.row,obj.col);
			line.RemoveAt(0);
			foreach(Tile t in line){
				if(!t.passable){
					++count;
					if(count == num){
						return t;
					}
				}
			}
			return null;
		}
		public int DirectionOf(PhysicalObject obj){ return DirectionOf(obj.p); }
		public int DirectionOf(pos obj){
			int dy = Math.Abs(obj.row - row);
			int dx = Math.Abs(obj.col - col);
			if(dy == 0){
				if(col < obj.col){
					return 6;
				}
				if(col > obj.col){
					return 4;
				}
				else{
					if(dx == 0){
						return 5;
					}
				}
			}
			if(dx == 0){
				if(row > obj.row){
					return 8;
				}
				else{
					if(row < obj.row){
						return 2;
					}
				}
			}
			if(row+col == obj.row+obj.col){ //slope is -1
				if(row > obj.row){
					return 9;
				}
				else{
					if(row < obj.row){
						return 1;
					}
				}
			}
			if(row-col == obj.row-obj.col){ //slope is 1
				if(row > obj.row){
					return 7;
				}
				else{
					if(row < obj.row){
						return 3;
					}
				}
			}
			// calculate all other dirs here
			/*.................flipped y
........m........
.......l|n.......
........|........
.....k..|..o.....
......\.|./......
...j...\|/...p...
..i-----@-----a.1
...h.../|\...b.2.
....../.|.\.B.3..
.....g..|..c.4...
........|...5....
.......f|d.......
........e........

@-------------...
|\;..b.2.........
|.\.B.3..........
|..\.4;..........
|...\...;........
|....\....;6.....
|.....\.....;....
|......\.....5;..
	rise:	run:	ri/ru:	angle(flipped y):
b:	1	5	1/5		(obviously the dividing line should be 22.5 degrees here)
d:	5	1	5		67.5
f:	5	-1	-5		112.5
h:	1	-5	-1/5		157.5
j:	-1	-5	1/5		202.5
l:	-5	-1	5		247.5
n:	-5	1	-5		292.5
p:	-1	5	-1/5		337.5
algorithm for determining direction...			(for b)		(for 4)		(for 6)		(for 5)		(for B)
first, determine 'major' direction - NSEW		E		E		E		E		E
then, determine 'minor' direction - diagonals		SE		SE		SE		SE		SE
find the ratio of d-major/d(other dir) (both positive)	1/5		3/5		5/11		7/13		2/4
compare this number to 1/2:  if less than 1/2, major.	
	if more than 1/2, minor.
	if exactly 1/2, tiebreaker.
							major(E)	minor(SE)	major(E)	minor(SE)	tiebreak


*/
			int primary; //orthogonal
			int secondary; //diagonal
			int dprimary = Math.Min(dy,dx);
			int dsecondary = Math.Max(dy,dx);
			if(row < obj.row){ //down
				if(col < obj.col){ //right
					secondary = 3;
					if(dx > dy){ //slope less than 1
						primary = 6;
					}
					else{ //slope greater than 1
						primary = 2;
					}
				}
				else{ //left
					secondary = 1;
					if(dx > dy){ //slope less than 1
						primary = 4;
					}
					else{ //slope greater than 1
						primary = 2;
					}
				}
			}
			else{ //up
				if(col < obj.col){ //right
					secondary = 9;
					if(dx > dy){ //slope less than 1
						primary = 6;
					}
					else{ //slope greater than 1
						primary = 8;
					}
				}
				else{ //left
					secondary = 7;
					if(dx > dy){ //slope less than 1
						primary = 4;
					}
					else{ //slope greater than 1
						primary = 8;
					}
				}
			}
			int tiebreaker = primary;
			float ratio = (float)dprimary / (float)dsecondary;
			if(ratio < 0.5f){
				return primary;
			}
			else{
				if(ratio > 0.5f){
					return secondary;
				}
				else{
					return tiebreaker;
				}
			}
		}
		public int DirectionOfOnlyUnblocked(TileType tiletype){ return DirectionOfOnlyUnblocked(tiletype,false); }
		public int DirectionOfOnlyUnblocked(TileType tiletype,bool orth){//if there's only 1 unblocked tile of this kind, return its dir
			int total=0;
			int dir=0;
			for(int i=1;i<=9;++i){
				if(i != 5){
					if(TileInDirection(i).type == tiletype && ActorInDirection(i) == null && TileInDirection(i).inv == null){
						if(!orth || i%2==0){
							++total;
							dir = i;
						}
					}
				}
				/*else{
					if(tile().type == tiletype && !orth){
						++total;
						dir = i;
					}
				}*/
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
		public Actor actor(){
			return M.actor[row,col];
		}
		public Tile tile(){
			return M.tile[row,col];
		}
		public List<Actor> ActorsWithinDistance(int dist){ return ActorsWithinDistance(dist,false); }
		public List<Actor> ActorsWithinDistance(int dist,bool exclude_origin){
			List<Actor> result = new List<Actor>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(i!=row || j!=col || exclude_origin==false){
						if(M.BoundsCheck(i,j) && M.actor[i,j] != null){
							result.Add(M.actor[i,j]);
						}
					}
				}
			}
			return result;
		}
		public List<Actor> ActorsAtDistance(int dist){
			List<Actor> result = new List<Actor>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(DistanceFrom(i,j) == dist && M.BoundsCheck(i,j) && M.actor[i,j] != null){
						result.Add(M.actor[i,j]);
					}
				}
			}
			return result;
		}
		public List<Tile> TilesWithinDistance(int dist){ return TilesWithinDistance(dist,false); }
		public List<Tile> TilesWithinDistance(int dist,bool exclude_origin){
			List<Tile> result = new List<Tile>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(i!=row || j!=col || exclude_origin==false){
						if(M.BoundsCheck(i,j)){
							result.Add(M.tile[i,j]);
						}
					}
				}
			}
			return result;
		}
		public List<Tile> TilesAtDistance(int dist){
			List<Tile> result = new List<Tile>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(DistanceFrom(i,j) == dist && M.BoundsCheck(i,j)){
						result.Add(M.tile[i,j]);
					}
				}
			}
			return result;
		}
		public List<pos> PositionsWithinDistance(int dist){ return PositionsWithinDistance(dist,false); }
		public List<pos> PositionsWithinDistance(int dist,bool exclude_origin){
			List<pos> result = new List<pos>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(i!=row || j!=col || exclude_origin==false){
						if(M.BoundsCheck(i,j)){
							result.Add(new pos(i,j));
						}
					}
				}
			}
			return result;
		}
		public List<pos> PositionsAtDistance(int dist){
			List<pos> result = new List<pos>();
			for(int i=row-dist;i<=row+dist;++i){
				for(int j=col-dist;j<=col+dist;++j){
					if(DistanceFrom(i,j) == dist && M.BoundsCheck(i,j)){
						result.Add(new pos(i,j));
					}
				}
			}
			return result;
		}
		public bool IsAdjacentTo(TileType type){ return IsAdjacentTo(type,false); } //didn't need an Actor (or Item) version yet
		public bool IsAdjacentTo(TileType type,bool consider_origin){
			foreach(Tile t in TilesWithinDistance(1,!consider_origin)){
				if(t.type == type){
					return true;
				}
			}
			return false;
		}
		public bool IsAdjacentTo(FeatureType type){ return IsAdjacentTo(type,false); } //didn't need an Actor (or Item) version yet
		public bool IsAdjacentTo(FeatureType type,bool consider_origin){
			foreach(Tile t in TilesWithinDistance(1,!consider_origin)){
				if(t.features.Contains(type)){
					return true;
				}
			}
			return false;
		}
		public bool HasLOS(PhysicalObject o){ return HasLOS(o.row,o.col); } //line of sight
		public bool HasLOS(int r,int c){
			if(HasBresenhamLineOfSight(r,c)){
				return true;
			}
			if(M.tile[r,c].opaque){ //for walls, check nearby tiles
				foreach(Tile t in M.tile[r,c].NonOpaqueNeighborsBetween(row,col)){
					if(HasBresenhamLineOfSight(t.row,t.col)){
						return true;
					}
				}
			}
			return false;
		}
		public bool HasLOE(PhysicalObject o){ return HasLOE(o.row,o.col); } //line of effect
		public bool HasLOE(int r,int c){
			if(HasBresenhamLineOfEffect(r,c)){ //basic LOE check
				return true;
			}
			if(!M.tile[r,c].passable){ //for walls, check nearby tiles
				foreach(Tile t in M.tile[r,c].NonOpaqueNeighborsBetween(row,col)){
					if(HasBresenhamLineOfEffect(t.row,t.col)){
						return true;
					}
				}
			}
			return false;
		}
		public List<Tile> GetBestLineOfSight(PhysicalObject o){ return GetBestLineOfSight(o.row,o.col); }
		public List<Tile> GetBestLineOfSight(int r,int c){
			List<Tile>[] lists = GetBothBresenhamLines(r,c);
			for(int i=0;i<lists[0].Count;++i){
				if(lists[1][i].opaque){
					return lists[0];
				}
				if(lists[0][i].opaque){
					return lists[1];
				}
			}
			return lists[0];
		}
		public List<Tile> GetBestLineOfEffect(PhysicalObject o){ return GetBestLineOfEffect(o.row,o.col); }
		public List<Tile> GetBestLineOfEffect(int r,int c){
			List<Tile>[] lists = GetBothBresenhamLines(r,c);
			for(int i=0;i<lists[0].Count;++i){
				if(!lists[1][i].passable){
					return lists[0];
				}
				if(!lists[0][i].passable){
					return lists[1];
				}
			}
			return lists[0];
		}
		public List<Tile> GetBestExtendedLineOfSight(PhysicalObject o){ return GetBestExtendedLineOfSight(o.row,o.col); }
		public List<Tile> GetBestExtendedLineOfSight(int r,int c){
			List<Tile>[] lists = GetBothExtendedBresenhamLines(r,c);
			for(int i=0;i<lists[0].Count;++i){
				if(lists[1][i].opaque){
					return lists[0];
				}
				if(lists[0][i].opaque){
					return lists[1];
				}
			}
			return lists[0];
		}
		public List<Tile> GetBestExtendedLineOfEffect(PhysicalObject o){ return GetBestExtendedLineOfEffect(o.row,o.col); }
		public List<Tile> GetBestExtendedLineOfEffect(int r,int c){
			List<Tile>[] lists = GetBothExtendedBresenhamLines(r,c);
			for(int i=0;i<lists[0].Count;++i){
				if(!lists[1][i].passable){
					return lists[0];
				}
				if(!lists[0][i].passable){
					return lists[1];
				}
			}
			return lists[0];
		}
		public bool HasBresenhamLineOfSight(PhysicalObject o){ return HasBresenhamLineOfSight(o.row,o.col); }
		public bool HasBresenhamLineOfSight(int r,int c){
			int y1 = row;
			int x1 = col;
			int y2 = r;
			int x2 = c;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			int er = 0;
			bool a_blocked = false;
			bool b_blocked = false;
			if(dy==0){
				if(x1<x2){
					++x1; //incrementing once before checking opacity lets you see out of solid tiles
					for(;x1<x2;++x1){ //right
						if(M.tile[y1,x1].opaque){
							return false;
						}
					}
				}
				else{
					--x1;
					for(;x1>x2;--x1){ //left
						if(M.tile[y1,x1].opaque){
							return false;
						}
					}
				}
				return true;
			}
			if(dx==0){
				if(y1>y2){
					--y1;
					for(;y1>y2;--y1){ //up
						if(M.tile[y1,x1].opaque){
							return false;
						}
					}
				}
				else{
					++y1;
					for(;y1<y2;++y1){ //down
						if(M.tile[y1,x1].opaque){
							return false;
						}
					}
				}
				return true;
			}
			if(y1+x1==y2+x2){ //slope is -1
				if(x1<x2){
					++x1;
					--y1;
					for(;x1<x2;++x1){ //up-right
						if(M.tile[y1,x1].opaque){
							return false;
						}
						--y1;
					}
				}
				else{
					--x1;
					++y1;
					for(;x1>x2;--x1){ //down-left
						if(M.tile[y1,x1].opaque){
							return false;
						}
						++y1;
					}
				}
				return true;
			}
			if(y1-x1==y2-x2){ //slope is 1
				if(x1<x2){
					++x1;
					++y1;
					for(;x1<x2;++x1){ //down-right
						if(M.tile[y1,x1].opaque){
							return false;
						}
						++y1;
					}
				}
				else{
					--x1;
					--y1;
					for(;x1>x2;--x1){ //up-left
						if(M.tile[y1,x1].opaque){
							return false;
						}
						--y1;
					}
				}
				return true;
			}
			if(y1<y2){ //down
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						++x1;
						er += dy;
						if(er<<1 > dx){
							++y1;
							er -= dx;
						}
						for(;x1<x2;++x1){
							if(M.tile[y1,x1].opaque){
								if(er<<1 != dx || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dx){
								++y1;
								if(M.tile[y1,x1].opaque){
									if(er<<1 != dx || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dx;
							}
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						return true;
					}
					else{ //slope greater than 1
						++y1;
						er += dx;
						if(er<<1 > dy){
							++x1;
							er -= dy;
						}
						for(;y1<y2;++y1){
							if(M.tile[y1,x1].opaque){
								if(er<<1 != dy || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dy){
								++x1;
								if(M.tile[y1,x1].opaque){
									if(er<<1 != dy || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dy;
							}
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						return true;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						--x1;
						er += dy;
						if(er<<1 > dx){
							++y1;
							er -= dx;
						}
						for(;x1>x2;--x1){
							if(M.tile[y1,x1].opaque){
								if(er<<1 != dx || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dx){
								++y1;
								if(M.tile[y1,x1].opaque){
									if(er<<1 != dx || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dx;
							}
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						return true;
					}
					else{ //slope greater than 1
						++y1;
						er += dx;
						if(er<<1 > dy){
							--x1;
							er -= dy;
						}
						for(;y1<y2;++y1){
							if(M.tile[y1,x1].opaque){
								if(er<<1 != dy || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dy){
								--x1;
								if(M.tile[y1,x1].opaque){
									if(er<<1 != dy || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dy;
							}
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						return true;
					}
				}
			}
			else{ //up
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						++x1;
						er += dy;
						if(er<<1 > dx){
							--y1;
							er -= dx;
						}
						for(;x1<x2;++x1){
							if(M.tile[y1,x1].opaque){
								if(er<<1 != dx || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dx){
								--y1;
								if(M.tile[y1,x1].opaque){
									if(er<<1 != dx || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dx;
							}
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						return true;
					}
					else{ //slope greater than 1
						--y1;
						er += dx;
						if(er<<1 > dy){
							++x1;
							er -= dy;
						}
						for(;y1>y2;--y1){
							if(M.tile[y1,x1].opaque){
								if(er<<1 != dy || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dy){
								++x1;
								if(M.tile[y1,x1].opaque){
									if(er<<1 != dy || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dy;
							}
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						return true;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						--x1;
						er += dy;
						if(er<<1 > dx){
							--y1;
							er -= dx;
						}
						for(;x1>x2;--x1){
							if(M.tile[y1,x1].opaque){
								if(er<<1 != dx || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dx){
								--y1;
								if(M.tile[y1,x1].opaque){
									if(er<<1 != dx || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dx;
							}
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						return true;
					}
					else{ //slope greater than 1
						--y1;
						er += dx;
						if(er<<1 > dy){
							--x1;
							er -= dy;
						}
						for(;y1>y2;--y1){
							if(M.tile[y1,x1].opaque){
								if(er<<1 != dy || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dy){
								--x1;
								if(M.tile[y1,x1].opaque){
									if(er<<1 != dy || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dy;
							}
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						return true;
					}
				}
			}
		}
		public bool HasBresenhamLineOfEffect(PhysicalObject o){ return HasBresenhamLineOfEffect(o.row,o.col); }
		public bool HasBresenhamLineOfEffect(int r,int c){
			int y1 = row;
			int x1 = col;
			int y2 = r;
			int x2 = c;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			int er = 0;
			bool a_blocked = false;
			bool b_blocked = false;
			if(dy==0){
				if(x1<x2){
					++x1; //incrementing once before checking opacity lets you see out of solid tiles
					for(;x1<x2;++x1){ //right
						if(!M.tile[y1,x1].passable){
							return false;
						}
					}
				}
				else{
					--x1;
					for(;x1>x2;--x1){ //left
						if(!M.tile[y1,x1].passable){
							return false;
						}
					}
				}
				return true;
			}
			if(dx==0){
				if(y1>y2){
					--y1;
					for(;y1>y2;--y1){ //up
						if(!M.tile[y1,x1].passable){
							return false;
						}
					}
				}
				else{
					++y1;
					for(;y1<y2;++y1){ //down
						if(!M.tile[y1,x1].passable){
							return false;
						}
					}
				}
				return true;
			}
			if(y1+x1==y2+x2){ //slope is -1
				if(x1<x2){
					++x1;
					--y1;
					for(;x1<x2;++x1){ //up-right
						if(!M.tile[y1,x1].passable){
							return false;
						}
						--y1;
					}
				}
				else{
					--x1;
					++y1;
					for(;x1>x2;--x1){ //down-left
						if(!M.tile[y1,x1].passable){
							return false;
						}
						++y1;
					}
				}
				return true;
			}
			if(y1-x1==y2-x2){ //slope is 1
				if(x1<x2){
					++x1;
					++y1;
					for(;x1<x2;++x1){ //down-right
						if(!M.tile[y1,x1].passable){
							return false;
						}
						++y1;
					}
				}
				else{
					--x1;
					--y1;
					for(;x1>x2;--x1){ //up-left
						if(!M.tile[y1,x1].passable){
							return false;
						}
						--y1;
					}
				}
				return true;
			}
			if(y1<y2){ //down
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						++x1;
						er += dy;
						if(er<<1 > dx){
							++y1;
							er -= dx;
						}
						for(;x1<x2;++x1){
							if(!M.tile[y1,x1].passable){
								if(er<<1 != dx || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dx){
								++y1;
								if(!M.tile[y1,x1].passable){
									if(er<<1 != dx || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dx;
							}
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						return true;
					}
					else{ //slope greater than 1
						++y1;
						er += dx;
						if(er<<1 > dy){
							++x1;
							er -= dy;
						}
						for(;y1<y2;++y1){
							if(!M.tile[y1,x1].passable){
								if(er<<1 != dy || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dy){
								++x1;
								if(!M.tile[y1,x1].passable){
									if(er<<1 != dy || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dy;
							}
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						return true;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						--x1;
						er += dy;
						if(er<<1 > dx){
							++y1;
							er -= dx;
						}
						for(;x1>x2;--x1){
							if(!M.tile[y1,x1].passable){
								if(er<<1 != dx || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dx){
								++y1;
								if(!M.tile[y1,x1].passable){
									if(er<<1 != dx || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dx;
							}
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						return true;
					}
					else{ //slope greater than 1
						++y1;
						er += dx;
						if(er<<1 > dy){
							--x1;
							er -= dy;
						}
						for(;y1<y2;++y1){
							if(!M.tile[y1,x1].passable){
								if(er<<1 != dy || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dy){
								--x1;
								if(!M.tile[y1,x1].passable){
									if(er<<1 != dy || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dy;
							}
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						return true;
					}
				}
			}
			else{ //up
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						++x1;
						er += dy;
						if(er<<1 > dx){
							--y1;
							er -= dx;
						}
						for(;x1<x2;++x1){
							if(!M.tile[y1,x1].passable){
								if(er<<1 != dx || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dx){
								--y1;
								if(!M.tile[y1,x1].passable){
									if(er<<1 != dx || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dx;
							}
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						return true;
					}
					else{ //slope greater than 1
						--y1;
						er += dx;
						if(er<<1 > dy){
							++x1;
							er -= dy;
						}
						for(;y1>y2;--y1){
							if(!M.tile[y1,x1].passable){
								if(er<<1 != dy || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dy){
								++x1;
								if(!M.tile[y1,x1].passable){
									if(er<<1 != dy || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dy;
							}
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						return true;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						--x1;
						er += dy;
						if(er<<1 > dx){
							--y1;
							er -= dx;
						}
						for(;x1>x2;--x1){
							if(!M.tile[y1,x1].passable){
								if(er<<1 != dx || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dx){
								--y1;
								if(!M.tile[y1,x1].passable){
									if(er<<1 != dx || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dx;
							}
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						return true;
					}
					else{ //slope greater than 1
						--y1;
						er += dx;
						if(er<<1 > dy){
							--x1;
							er -= dy;
						}
						for(;y1>y2;--y1){
							if(!M.tile[y1,x1].passable){
								if(er<<1 != dy || b_blocked){
									return false;
								}
								a_blocked = true;
							}
							if(er<<1 == dy){
								--x1;
								if(!M.tile[y1,x1].passable){
									if(er<<1 != dy || a_blocked){
										return false;
									}
									b_blocked = true;
								}
								er -= dy;
							}
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						return true;
					}
				}
			}
		}
		public List<Tile>[] GetBothBresenhamLines(PhysicalObject o){ return GetBothBresenhamLines(o.row,o.col); }
		public List<Tile>[] GetBothBresenhamLines(int r,int c){ //can return the same list if both would be identical
			int y2 = r;
			int x2 = c;
			int y1 = row;
			int x1 = col;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			int er = 0;
			List<Tile> alist = new List<Tile>();
			List<Tile> blist = new List<Tile>();
			List<Tile>[] result = new List<Tile>[2];
			result[0] = alist;
			result[1] = blist;
			if(dy==0){
				if(dx==0){
					alist.Add(M.tile[row,col]);
					blist.Add(M.tile[row,col]);
					return result;
				}
				for(;x1<x2;++x1){ //right
					alist.Add(M.tile[y1,x1]);
				}
				for(;x1>x2;--x1){ //left
					alist.Add(M.tile[y1,x1]);
				}
				alist.Add(M.tile[r,c]);
				result[1] = alist;
				return result;
			}
			if(dx==0){
				for(;y1>y2;--y1){ //up
					alist.Add(M.tile[y1,x1]);
				}
				for(;y1<y2;++y1){ //down
					alist.Add(M.tile[y1,x1]);
				}
				alist.Add(M.tile[r,c]);
				result[1] = alist;
				return result;
			}
			if(y1+x1==y2+x2){ //slope is -1
				for(;x1<x2;++x1){ //up-right
					alist.Add(M.tile[y1,x1]);
					--y1;
				}
				for(;x1>x2;--x1){ //down-left
					alist.Add(M.tile[y1,x1]);
					++y1;
				}
				alist.Add(M.tile[r,c]);
				result[1] = alist;
				return result;
			}
			if(y1-x1==y2-x2){ //slope is 1
				for(;x1<x2;++x1){ //down-right
					alist.Add(M.tile[y1,x1]);
					++y1;
				}
				for(;x1>x2;--x1){ //up-left
					alist.Add(M.tile[y1,x1]);
					--y1;
				}
				alist.Add(M.tile[r,c]);
				result[1] = alist;
				return result;
			}
			if(y1<y2){ //down
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<x2;++x1){
							if(er<<1 == dx){
								alist.Add(M.tile[y1,x1]);
								++y1;
								er -= dx;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						alist.Add(M.tile[r,c]);
						blist.Add(M.tile[r,c]);
						return result;
					}
					else{ //slope greater than 1
						for(;y1<y2;++y1){
							if(er<<1 == dy){
								alist.Add(M.tile[y1,x1]);
								++x1;
								er -= dy;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						alist.Add(M.tile[r,c]);
						blist.Add(M.tile[r,c]);
						return result;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>x2;--x1){
							if(er<<1 == dx){
								alist.Add(M.tile[y1,x1]);
								++y1;
								er -= dx;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						alist.Add(M.tile[r,c]);
						blist.Add(M.tile[r,c]);
						return result;
					}
					else{ //slope greater than 1
						for(;y1<y2;++y1){
							if(er<<1 == dy){
								alist.Add(M.tile[y1,x1]);
								--x1;
								er -= dy;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						alist.Add(M.tile[r,c]);
						blist.Add(M.tile[r,c]);
						return result;
					}
				}
			}
			else{ //up
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<x2;++x1){
							if(er<<1 == dx){
								alist.Add(M.tile[y1,x1]);
								--y1;
								er -= dx;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						alist.Add(M.tile[r,c]);
						blist.Add(M.tile[r,c]);
						return result;
					}
					else{ //slope greater than 1
						for(;y1>y2;--y1){
							if(er<<1 == dy){
								alist.Add(M.tile[y1,x1]);
								++x1;
								er -= dy;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						alist.Add(M.tile[r,c]);
						blist.Add(M.tile[r,c]);
						return result;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>x2;--x1){
							if(er<<1 == dx){
								alist.Add(M.tile[y1,x1]);
								--y1;
								er -= dx;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						alist.Add(M.tile[r,c]);
						blist.Add(M.tile[r,c]);
						return result;
					}
					else{ //slope greater than 1
						for(;y1>y2;--y1){
							if(er<<1 == dy){
								alist.Add(M.tile[y1,x1]);
								--x1;
								er -= dy;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						alist.Add(M.tile[r,c]);
						blist.Add(M.tile[r,c]);
						return result;
					}
				}
			}
		}
		public List<Tile>[] GetBothExtendedBresenhamLines(PhysicalObject o){ return GetBothExtendedBresenhamLines(o.row,o.col); }
		public List<Tile>[] GetBothExtendedBresenhamLines(int r,int c){ //extends to edge of map
			int y2 = r;
			int x2 = c;
			int y1 = row;
			int x1 = col;
			int dx = Math.Abs(x2-x1);
			int dy = Math.Abs(y2-y1);
			int er = 0;
			int COLS = Global.COLS; //for laziness
			int ROWS = Global.ROWS;
			List<Tile> alist = new List<Tile>();
			List<Tile> blist = new List<Tile>();
			List<Tile>[] result = new List<Tile>[2];
			result[0] = alist;
			result[1] = blist;
			if(dy==0){
				if(dx==0){
					alist.Add(M.tile[row,col]);
					blist.Add(M.tile[row,col]);
					return result;
				}
				if(x1<x2){
					for(;x1<=COLS-1;++x1){ //right
						alist.Add(M.tile[y1,x1]);
					}
				}
				else{
					for(;x1>=0;--x1){ //left
						alist.Add(M.tile[y1,x1]);
					}
				}
				result[1] = alist;
				return result;
			}
			if(dx==0){
				if(y1>y2){
					for(;y1>=0;--y1){ //up
						alist.Add(M.tile[y1,x1]);
					}
				}
				else{
					for(;y1<=ROWS-1;++y1){ //down
						alist.Add(M.tile[y1,x1]);
					}
				}
				result[1] = alist;
				return result;
			}
			if(y1+x1==y2+x2){ //slope is -1
				if(x1<x2){
					for(;x1<=COLS-1 && y1>=0;++x1){ //up-right
						alist.Add(M.tile[y1,x1]);
						--y1;
					}
				}
				else{
					for(;x1>=0 && y1<=ROWS-1;--x1){ //down-left
						alist.Add(M.tile[y1,x1]);
						++y1;
					}
				}
				result[1] = alist;
				return result;
			}
			if(y1-x1==y2-x2){ //slope is 1
				if(x1<x2){
					for(;x1<=COLS-1 && y1<=ROWS-1;++x1){ //down-right
						alist.Add(M.tile[y1,x1]);
						++y1;
					}
				}
				else{
					for(;x1>=0 && y1>=0;--x1){ //up-left
						alist.Add(M.tile[y1,x1]);
						--y1;
					}
				}
				result[1] = alist;
				return result;
			}
			if(y1<y2){ //down
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<=COLS-1 && y1<=ROWS-1;++x1){
							if(er<<1 == dx){
								alist.Add(M.tile[y1,x1]);
								++y1;
								if(y1 == ROWS){
									return result;
								}
								er -= dx;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						return result;
					}
					else{ //slope greater than 1
						for(;y1<=ROWS-1 && x1<=COLS-1;++y1){
							if(er<<1 == dy){
								alist.Add(M.tile[y1,x1]);
								++x1;
								if(x1 == COLS){
									return result;
								}
								er -= dy;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						return result;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>=0 && y1<=ROWS-1;--x1){
							if(er<<1 == dx){
								alist.Add(M.tile[y1,x1]);
								++y1;
								if(y1 == ROWS){
									return result;
								}
								er -= dx;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dy;
							if(er<<1 > dx){
								++y1;
								er -= dx;
							}
						}
						return result;
					}
					else{ //slope greater than 1
						for(;y1<=ROWS-1 && x1>=0;++y1){
							if(er<<1 == dy){
								alist.Add(M.tile[y1,x1]);
								--x1;
								if(x1 == -1){
									return result;
								}
								er -= dy;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						return result;
					}
				}
			}
			else{ //up
				if(x1<x2){ //right
					if(dx>dy){ //slope less than 1
						for(;x1<=COLS-1 && y1>=0;++x1){
							if(er<<1 == dx){
								alist.Add(M.tile[y1,x1]);
								--y1;
								if(y1 == -1){
									return result;
								}
								er -= dx;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						return result;
					}
					else{ //slope greater than 1
						for(;y1>=0 && x1<=COLS-1;--y1){
							if(er<<1 == dy){
								alist.Add(M.tile[y1,x1]);
								++x1;
								if(x1 == COLS){
									return result;
								}
								er -= dy;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dx;
							if(er<<1 > dy){
								++x1;
								er -= dy;
							}
						}
						return result;
					}
				}
				else{ //left
					if(dx>dy){ //slope less than 1
						for(;x1>=0 && y1>=0;--x1){
							if(er<<1 == dx){
								alist.Add(M.tile[y1,x1]);
								--y1;
								if(y1 == -1){
									return result;
								}
								er -= dx;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dy;
							if(er<<1 > dx){
								--y1;
								er -= dx;
							}
						}
						return result;
					}
					else{ //slope greater than 1
						for(;y1>=0 && x1>=0;--y1){
							if(er<<1 == dy){
								alist.Add(M.tile[y1,x1]);
								--x1;
								if(x1 == -1){
									return result;
								}
								er -= dy;
								blist.Add(M.tile[y1,x1]);
							}
							else{
								alist.Add(M.tile[y1,x1]);
								blist.Add(M.tile[y1,x1]);
							}
							er += dx;
							if(er<<1 > dy){
								--x1;
								er -= dy;
							}
						}
						return result;
					}
				}
			}
		}
		public List<Tile> GetCone(int direction,int distance,bool exclude_origin){
			List<Tile> result = new List<Tile>();
			if(direction < 1 || direction == 5 || direction > 9 || distance < 1){
				return result;
			}
			else{
				pos target = p;
				for(int i=0;i<6;++i){
					target = target.PosInDir(direction); //make target the position 6 away in that direction
				}
				if(direction % 2 == 0){
					result = TilesWithinDistance(distance).Where(x=>target.ManhattanDistanceFromX10(x.p) <= 60);
				}
				else{
					result = TilesWithinDistance(distance).Where(x=>target.ChebyshevDistanceFromX10(x.p) <= 60);
				}
				if(exclude_origin){
					result.Remove(tile());
				}
				return result;
			}
		}
	}
}

