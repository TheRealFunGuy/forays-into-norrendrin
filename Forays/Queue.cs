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
	public class Queue{
		private LinkedList<Event> list;
		public int turn{get; private set;}public int Count(){return list.Count; }
		public static Buffer B{get;set;}
		public Queue(Game g){
			list = new LinkedList<Event>();
			turn = 0;
			B = g.B;
		}
		public void Add(Event e){
			if(e.TimeToExecute() == turn){ //0-time action
				list.AddFirst(e);
			}
			else{
				if(list.First==null){
					list.AddFirst(e);
				}
				else{
					if(e >= list.Last.Value){
						list.AddLast(e);
					}
					else{
						if(e < list.First.Value){
							list.AddFirst(e);
						}
						else{ //guaranteed to fall in the middle:
							LinkedListNode<Event> current = list.Last;
							while(e < current.Previous.Value){
								current = current.Previous;
							}
							list.AddBefore(current,e);
						}
					}
				}
			}
		}
		public void Pop(){
			turn = list.First.Value.TimeToExecute();
			list.First.Value.Execute();
			list.RemoveFirst();
		}
		public void KillEvents(PhysicalObject target,EventType type){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				current.Value.Kill(target,type);
			}
		}
		public void KillEvents(PhysicalObject target,AttrType attr){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				current.Value.Kill(target,attr);
			}
		}
	}
	public class Event{
		private PhysicalObject target;
		private List<Tile> area;
		private int delay;
		private EventType type;
		private AttrType attr;
		private int value;
		private string msg;
		private List<PhysicalObject> msg_objs; //used to determine visibility of msg
		private int time_created;
		private bool dead;
		public static Queue Q{get;set;}
		public static Buffer B{get;set;}
		public static Map M{get;set;}
		public static Actor player{get;set;}
		public Event(PhysicalObject target_,int delay_){
			target=target_;
			delay=delay_;
			type=EventType.MOVE;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=1;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,int value_){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=value_;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,string msg_){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=1;
			msg=msg_;
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,int value_,string msg_){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=value_;
			msg=msg_;
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,string msg_,params PhysicalObject[] objs){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=1;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,int delay_,AttrType attr_,int value_,string msg_,params PhysicalObject[] objs){
			target=target_;
			delay=delay_;
			type=EventType.REMOVE_ATTR;
			attr=attr_;
			value=value_;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
		}
		public Event(int delay_,EventType type_){
			target=null;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,int delay_,EventType type_){
			target=target_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,int delay_,EventType type_,int value_){
			target=target_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=value_;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(int delay_,string msg_){
			target=null;
			delay=delay_;
			type=EventType.ANY_EVENT;
			attr=AttrType.NO_ATTR;
			value=0;
			msg=msg_;
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(List<Tile> area_,int delay_,EventType type_){
			target=null;
			area = new List<Tile>();
			foreach(Tile t in area_){
				area.Add(t);
			}
			//area=area_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
		}
		public Event(List<Tile> area_,int delay_,EventType type_,string msg_,params PhysicalObject[] objs){
			target=null;
			area=area_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,List<Tile> area_,int delay_,EventType type_,AttrType attr_,int value_,string msg_,params PhysicalObject[] objs){
			target=target_;
			area=area_;
			delay=delay_;
			type=type_;
			attr=attr_;
			value=value_;
			msg=msg_;
			msg_objs = new List<PhysicalObject>();
			foreach(PhysicalObject obj in objs){
				msg_objs.Add(obj);
			}
			time_created=Q.turn;
			dead=false;
		}
		public int TimeToExecute(){ return delay + time_created; }
		public void Kill(PhysicalObject target_,EventType type_){
			if(msg_objs != null && (type==type_ || type_==EventType.ANY_EVENT)){
				if(msg_objs.Contains(target)){
					msg_objs.Remove(target);
				}
			}
			Tile t = target_ as Tile;
			if(t != null && area != null && area.Contains(t)){
/*				target = null;
				if(msg_objs != null){
					msg_objs.Clear();
					msg_objs = null;
				}
				area.Clear();
				area = null;
				dead = true;*/
				area.Remove(t);
			}
			if(target==target_ && (type==type_ || type_==EventType.ANY_EVENT)){
				target = null;
				if(msg_objs != null){
					msg_objs.Clear();
					msg_objs = null;
				}
				if(area != null){
					area.Clear();
					area = null;
				}
				dead = true;
			}
			if(type_ == EventType.CHECK_FOR_HIDDEN && type == EventType.CHECK_FOR_HIDDEN){
				dead = true;
			}
		}
		public void Kill(PhysicalObject target_,AttrType attr_){
			if(target==target_ && type==EventType.REMOVE_ATTR && attr==attr_){
				target = null;
				if(msg_objs != null){
					msg_objs.Clear();
					msg_objs = null;
				}
				if(area != null){
					area.Clear();
					area = null;
				}
				dead = true;
			}
		}
		public void Execute(){
			if(!dead){
				switch(type){
				case EventType.MOVE:
					{
					Actor temp = target as Actor;
					temp.Input();
					}
					break;
				case EventType.REMOVE_ATTR:
					{
					Actor temp = target as Actor;
					if(temp.type == ActorType.BERSERKER && attr == AttrType.COOLDOWN_2){ //hack
						temp.attrs[attr] = 0;
					}
					else{
						temp.attrs[attr] -= value;
					}
					if(attr == AttrType.TELEPORTING){
						temp.attrs[attr] = 0;
					}
					if(attr == AttrType.IMMOBILIZED && temp.attrs[attr] < 0){ //check here for attrs that shouldn't drop below 0
						temp.attrs[attr] = 0;
					}
					if(attr==AttrType.ENHANCED_TORCH && temp.light_radius > 0){
						temp.UpdateRadius(temp.LightRadius(),6 - temp.attrs[AttrType.DIM_LIGHT],true); //where 6 is the default radius
						if(temp.attrs[AttrType.ON_FIRE] > temp.light_radius){
							temp.UpdateRadius(temp.light_radius,temp.attrs[AttrType.ON_FIRE]);
						}
					}
					if(attr==AttrType.SLOWED){
						if(temp.type != ActorType.PLAYER){
							temp.speed = Actor.Prototype(temp.type).speed;
						}
						else{
							temp.speed = 100;
						}
					}
					if(attr==AttrType.COOLDOWN_1 && temp.type == ActorType.BERSERKER){
						B.Add(temp.Your() + " rage diminishes. ",temp);
						B.Add(temp.the_name + " dies. ",temp);
						temp.TakeDamage(DamageType.NORMAL,8888,null);
					}
					break;
					}
				case EventType.CHECK_FOR_HIDDEN:
				{
					List<Tile> removed = new List<Tile>();
					foreach(Tile t in area){
						if(player.CanSee(t)){
							int exponent = player.DistanceFrom(t) + 2; //todo: test this value a bit more
							if(player.HasAttr(AttrType.KEEN_EYES)){
								--exponent;
							}
							if(!t.IsLit()){
								++exponent;
							}
							if(exponent > 8){
								exponent = 8; //because 1 in 256 is enough.
							}
							int difficulty = 1;
							for(int i=exponent;i>0;--i){
								difficulty = difficulty * 2;
							}
							if(Global.Roll(difficulty) == difficulty){
								if(t.IsTrap()){
									t.name = Tile.Prototype(t.type).name;
									t.a_name = Tile.Prototype(t.type).a_name;
									t.the_name = Tile.Prototype(t.type).the_name;
									t.symbol = Tile.Prototype(t.type).symbol;
									t.color = Tile.Prototype(t.type).color;
									B.Add("You notice " + t.a_name + ". ");
								}
								else{
									if(t.type == TileType.HIDDEN_DOOR){
										t.Toggle(null);
										B.Add("You notice a hidden door. ");
									}
								}
								removed.Add(t);
							}
						}
					}
					foreach(Tile t in removed){
						area.Remove(t);
					}
					if(area.Count > 0){
						Q.Add(new Event(area,100,EventType.CHECK_FOR_HIDDEN));
					}
					break;
				}
				case EventType.POLTERGEIST:
					{
					if(Global.CoinFlip()){
						for(int tries=0;tries>=0 && tries < 5;++tries){
							switch(Global.Roll(4)){
							case 1: //doors
								List<Tile> doors = new List<Tile>();
								foreach(Tile t in area){
									if(t.type == TileType.DOOR_C || t.type == TileType.DOOR_O){
										doors.Add(t);
									}
								}
								if(doors.Count > 0){
									Tile t = doors[Global.Roll(doors.Count)-1];
									if(t.type == TileType.DOOR_C){
										if(player.CanSee(t)){
											B.Add(t.the_name + " flies open! ",t);
										}
										else{
											B.Add("You hear a door open. ");
										}
										t.Toggle(null);
									}
									else{
										if(t.actor() == null){
											if(player.CanSee(t)){
												B.Add(t.the_name + " slams closed! ",t);
											}
											else{
												B.Add("You hear a door slam. ");
											}
											t.Toggle(null);
										}
										else{
											B.Add(t.the_name + " slams closed on " + t.actor().the_name + "! ",t);
											t.actor().TakeDamage(DamageType.BASHING,DamageClass.PHYSICAL,Global.Roll(6),null);
										}
									}
								}
								break;
							case 2: //items todo
								break;
							case 3: //shriek todo
								break;
							case 4: //laugh todo
								break;
							}
						}
					}
					break;
					}
				case EventType.GRENADE:
					{
					Tile t = target as Tile;
					if(t.type == TileType.GRENADE){
						B.Add("The grenade explodes! ",t);
						t.Toggle(null);
						foreach(Actor a in t.ActorsWithinDistance(1)){
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),null);
						}
						if(t.actor() != null){
							int dir = Global.RandomDirection();
							t.actor().GetKnockedBack(t.TileInDirection(t.actor().RotateDirection(dir,true,4)));
						}
					}
					break;
					}
				case EventType.STALAGMITE:
					{
					int stalagmites = 0;
					foreach(Tile tile in area){
						if(tile.type == TileType.STALAGMITE){
							stalagmites++;
						}
					}
					if(stalagmites > 0){
						if(stalagmites > 1){
							B.Add("The stalagmites crumble. ",area.ToArray());
						}
						else{
							B.Add("The stalagmite crumbles. ",area.ToArray());
						}
						foreach(Tile tile in area){
							if(tile.type == TileType.STALAGMITE){
								tile.Toggle(null);
							}
						}
					}
					break;
					}
				case EventType.REGENERATING_FROM_DEATH:
					{
					value++;
					if(value > 0 && target.actor() == null){
						Actor.Create(ActorType.TROLL,target.row,target.col);
						target.actor().curhp = value;
						target.actor().level = 0;
						B.Add("The troll stands up! ",target);
						target.actor().player_visibility_duration = -1;
						if(target.tile().type == TileType.DOOR_C){
							target.tile().Toggle(target.actor());
						}
					}
					else{
						int roll = Global.Roll(20);
						if(value == -1){
							roll = 1;
						}
						if(value == 0){
							roll = 3;
						}
						switch(roll){
						case 1:
						case 2:
							B.Add("The troll's corpse twitches. ",target);
							break;
						case 3:
						case 4:
							B.Add("You hear sounds coming from the troll's corpse. ",target);
							break;
						case 5:
							B.Add("The troll on the floor regenerates. ");
							break;
						default:
							break;
						}
						Q.Add(new Event(target,100,EventType.REGENERATING_FROM_DEATH,value));
					}
					break;
					}
				case EventType.QUICKFIRE:
				{
					List<Actor> actors = new List<Actor>();
					if(value >= 0){
						foreach(Tile t in area){
							if(t.actor() != null){
								actors.Add(t.actor());
							}
						}
					}
					if(value > 0){
						int radius = 4 - value;
						List<Tile> added = new List<Tile>();
						foreach(Tile t in target.TilesWithinDistance(radius)){
							if(t.passable && t.type != TileType.QUICKFIRE && t.type != TileType.GRENADE
							&& t.IsAdjacentTo(TileType.QUICKFIRE) && !area.Contains(t)){ //the interaction between grenades and
								added.Add(t);		//				quickfire is hacky
							}
						}
						foreach(Tile t in added){
							area.Add(t);
							TileType oldtype = t.type;
							t.TransformTo(TileType.QUICKFIRE);
							t.toggles_into = oldtype;
							t.passable = Tile.Prototype(oldtype).passable;
							t.opaque = Tile.Prototype(oldtype).opaque;
						}
					}
					if(value < 0){
						int radius = 4 + value;
						List<Tile> removed = new List<Tile>();
						foreach(Tile t in area){
							if(t.DistanceFrom(target) == radius){
								removed.Add(t);
							}
							else{
								if(t.actor() != null){
									actors.Add(t.actor());
								}
							}
						}
						foreach(Tile t in removed){
							area.Remove(t);
							t.Toggle(null);
						}
					}
					foreach(Actor a in actors){
						if(!a.HasAttr(AttrType.IMMUNE_FIRE) && !a.HasAttr(AttrType.INVULNERABLE)){
							B.Add("The quickfire burns " + a.the_name + ". ",a);
							a.TakeDamage(DamageType.FIRE,Global.Roll(6),null);
						}
					}
					--value;
					if(value > -5){
						Q.Add(new Event(target,area,100,EventType.QUICKFIRE,AttrType.NO_ATTR,value,""));
					}
					break;
				}
				case EventType.BOSS_ARRIVE:
					{
					//todo
					break;
					}
				}
				if(msg != ""){
					if(msg_objs == null){
						B.Add(msg);
					}
					else{
						B.Add(msg,msg_objs.ToArray());
					}
				}
			}
		}
		public static bool operator <(Event one,Event two){
			return one.TimeToExecute() < two.TimeToExecute();
		}
		public static bool operator >(Event one,Event two){
			return one.TimeToExecute() > two.TimeToExecute();
		}
		public static bool operator <=(Event one,Event two){
			return one.TimeToExecute() <= two.TimeToExecute();
		}
		public static bool operator >=(Event one,Event two){
			return one.TimeToExecute() >= two.TimeToExecute();
		}
	}
}

