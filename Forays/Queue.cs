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
namespace Forays{
	public class Queue{
		public LinkedList<Event> list;
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
		public bool Contains(EventType type){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				if(current.Value.type == type){
					return true;
				}
			}
			return false;
		}
	}
	public class Event{
		private PhysicalObject target;
		private List<Tile> area;
		private int delay;
		public EventType type{get;private set;}
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
			if(target_ == null && type_ == EventType.REGENERATING_FROM_DEATH && type == EventType.REGENERATING_FROM_DEATH){
				dead = true;
			}
			if(target_ == null && type_ == EventType.POLTERGEIST && type == EventType.POLTERGEIST){
				dead = true;
			}
			if(target_ == null && type_ == EventType.RELATIVELY_SAFE && type == EventType.RELATIVELY_SAFE){
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
					if(attr==AttrType.POISONED && temp == player){
						if(temp.HasAttr(AttrType.POISONED)){
							B.Add("The poison begins to subside. ");
						}
						else{
							B.Add("You are no longer poisoned. ");
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
							int exponent = player.DistanceFrom(t) + 1; //todo: test this value a bit more
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
				case EventType.RELATIVELY_SAFE:
				{
					if(M.AllActors().Count == 1 && !Q.Contains(EventType.POLTERGEIST) && !Q.Contains(EventType.BOSS_ARRIVE)
					&& !Q.Contains(EventType.REGENERATING_FROM_DEATH)){
						B.Add("All is still and silent. ");
						B.PrintAll();
					}
					else{
						Q.Add(new Event((Global.Roll(20)+40)*100,EventType.RELATIVELY_SAFE));
					}
					break;
				}
				case EventType.POLTERGEIST:
					{
					if(true){ //relic
						if(value < 4){
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
											if(t.seen || player.DistanceFrom(t) <= 12){
												B.Add("You hear a door opening. ");
											}
										}
										t.Toggle(null);
									}
									else{
										if(t.actor() == null){
											if(player.CanSee(t)){
												B.Add(t.the_name + " slams closed! ",t);
											}
											else{
												if(t.seen || player.DistanceFrom(t) <= 12){
													B.Add("You hear a door slamming. ");
												}
											}
											t.Toggle(null);
										}
										else{
											if(player.CanSee(t)){
												B.Add(t.the_name + " slams closed on " + t.actor().the_name + "! ",t);
											}
											else{
												if(player.DistanceFrom(t) <= 12){
													B.Add("You hear a door slamming and a grunt of pain. ");
												}
											}
											t.actor().TakeDamage(DamageType.BASHING,DamageClass.PHYSICAL,Global.Roll(6),null);
										}
									}
								}
								break;
							case 2: //items
							{
								bool player_here = false;
								foreach(Tile t in area){
									if(t.actor() == player){
										player_here = true;
									}
								}
								if(player_here){
									List<Tile> tiles = new List<Tile>();
									foreach(Tile t in area){
										if(t.inv != null && t.actor() == null && t.DistanceFrom(player) >= 2 && player.HasLOS(t.row,t.col)){
											tiles.Add(t);
										}
									}
									if(tiles.Count > 0){
										Tile t = tiles[Global.Roll(tiles.Count)-1];
										List<Tile> line = t.GetExtendedBresenhamLine(player.row,player.col);
										t = line[0];
										int i = 1;
										while(t.passable && t.DistanceFrom(line[0]) <= 9 && line[i].passable){
											if(t.actor() != null && t.actor().IsHit(0)){
												break;
											}
											t = line[i];
											++i;
										}
										if(line[0].inv.type == ConsumableType.PRISMATIC_ORB || line[0].inv.type == ConsumableType.WIZARDS_LIGHT){
											if(line[0].inv.type == ConsumableType.WIZARDS_LIGHT){ //let's say that they don't like the light
												B.Add("The orb bobs up and down in the air for a moment. ",line[0]);
											}
											else{
												B.Add("The orb rises into the air and sails toward you! ",line[0],t);
												Item item = line[0].inv;
												line[0].inv = null;
												List<Tile> anim_line = line[0].GetBresenhamLine(t.row,t.col);
												Screen.AnimateProjectile(anim_line,new colorchar(item.color,item.symbol));
												string qhit = item.quantity > 1? "shatter " : "shatters ";
												if(t.actor() != null){
													B.Add(item.TheName() + " " + qhit + "on " + t.actor().the_name + ". ",line[0],t);
												}
												else{
													B.Add(item.TheName() + " " + qhit + "on " + t.the_name + ". ",line[0],t);
												}
												List<DamageType> dmg = new List<DamageType>();
												dmg.Add(DamageType.FIRE);
												dmg.Add(DamageType.COLD);
												dmg.Add(DamageType.ELECTRIC);
												while(dmg.Count > 0){
													DamageType damtype = dmg[Global.Roll(dmg.Count)-1];
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
													foreach(Actor a in t.ActorsWithinDistance(1)){
														a.TakeDamage(damtype,DamageClass.MAGICAL,Global.Roll(2,6),null);
													}
													dmg.Remove(damtype);
												}
											}
										}
										else{
											Item item = line[0].inv;
											B.Add(item.TheName() + " rises into the air and sails toward you! ",line[0],t);
											line[0].inv = null;
											List<Tile> anim_line = line[0].GetBresenhamLine(t.row,t.col);
											Screen.AnimateProjectile(anim_line,new colorchar(item.color,item.symbol));
											t.GetItem(item);
											string qhit = item.quantity > 1? "hit " : "hits ";
											if(t.actor() != null){
												B.Add(item.TheName() + " " + qhit + t.actor().the_name + ". ",line[0],t);
												t.actor().TakeDamage(DamageType.NORMAL,Global.Roll(6),null);
											}
											else{
												B.Add(item.TheName() + " " + qhit + t.the_name + ". ",line[0],t);
											}
										}
										break;
									}
								}
/*								else{ //right now, does nothing if the player isn't present.
									List<Tile> tiles = new List<Tile>();
									foreach(Tile t in area){
										if(t.inv != null && t.actor() == null && t.seen){
											tiles.Add(t);
										}
									}
									//
								}*/
								break;
							}
							case 3: //shriek
							{
								bool good = false;
								foreach(Tile t in area){
									if(t.actor() == player){
										good = true;
									}
								}
								if(good){
									B.Add("Something shrieks right next to your ear! ");
									player.MakeNoise();
								}
								break;
							}
							case 4: //the no-effect messages
							{
								if(Global.CoinFlip()){
									int distance = 100;
									foreach(Tile t in area){
										if(t.DistanceFrom(player) < distance){
											distance = t.DistanceFrom(player);
										}
									}
									if(distance == 0){
										B.Add("You hear mocking laughter from nearby. ");
									}
									else{
										if(distance <= 3){
											B.Add("You hear laughter from somewhere. ");
										}
									}
								}
								else{
									foreach(Tile t in area){
										if(t.actor() == player){
											B.Add("You feel like you're being watched. ");
											break;
										}
									}
								}
								break;
							}
							default:
								break;
							}
						}
						else{
							for(int tries=0;tries<999;++tries){
								Tile t = area[Global.Roll(area.Count)-1];
								if(t.passable && t.actor() == null && player.CanSee(t)){
									Actor.Create(ActorType.POLTERGEIST,t.row,t.col);
									t.actor().player_visibility_duration = -1;
									B.Add("A poltergeist manifests in front of you! ");
									return;
								}
							}
						}
					}
					bool player_present = false;
					foreach(Tile t in area){
						if(t.actor() == player){
							player_present = true;
						}
					}
					if(player_present){
						Q.Add(new Event(null,area,(Global.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,value+1,"")); //todo check duration
					}
					else{
						Q.Add(new Event(null,area,(Global.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,0,"")); //todo
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
						if(player.DistanceFrom(t) <= 3){
							player.MakeNoise(); //hacky
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
					if(M.AllActors().Count == 1 && !Q.Contains(EventType.POLTERGEIST)){
						List<Tile> trolls = new List<Tile>();
						for(LinkedListNode<Event> current = Q.list.First;current!=null;current = current.Next){
							if(current.Value.type == EventType.REGENERATING_FROM_DEATH){
								trolls.Add((current.Value.target) as Tile);
							}
						}
						foreach(Tile troll in trolls){
							B.Add("The troll corpse on the ground burns to ashes! ",troll);
						}
						Q.KillEvents(null,EventType.REGENERATING_FROM_DEATH);
						B.Add("You hear a loud crash and a nearby roar! ");
						B.PrintAll();
						List<Tile> goodtiles = M.AllTiles();
						List<Tile> removed = new List<Tile>();
						foreach(Tile t in goodtiles){
							if(!t.passable || player.CanSee(t)){
								removed.Add(t);
							}
						}
						foreach(Tile t in removed){
							goodtiles.Remove(t);
						}
						if(goodtiles.Count > 0){
							Tile t = goodtiles[Global.Roll(goodtiles.Count)-1];
							Actor.Create(ActorType.FIRE_DRAKE,t.row,t.col);
							M.actor[t.row,t.col].player_visibility_duration = -1;
						}
						else{
							for(bool done=false;!done;){
								int rr = Global.Roll(Global.ROWS-2);
								int rc = Global.Roll(Global.COLS-2);
								if(M.tile[rr,rc].passable && M.actor[rr,rc] == null && player.DistanceFrom(rr,rc) >= 6){
									Actor.Create(ActorType.FIRE_DRAKE,rr,rc);
									M.actor[rr,rc].player_visibility_duration = -1;
									done = true;
								}
							}
						}
					}
					else{
						string s = "";
						switch(Global.Roll(8)){
						case 1:
							s = "You see scratch marks on the walls and floor. ";
							break;
						case 2:
							s = "There are deep gouges in the floor here. ";
							break;
						case 3:
							s = "The floor here is scorched and blackened. ";
							break;
						case 4:
							s = "You notice bones of an unknown sort on the floor. ";
							break;
						case 5:
							s = "You hear a distant roar. ";
							break;
						case 6:
							s = "You smell smoke. ";
							break;
						case 7:
							s = "You spot a large reddish scale on the floor. ";
							break;
						case 8:
							s = "A small tremor shakes the area. ";
							break;
						default:
							s = "Debug message. ";
							break;
						}
						B.AddIfEmpty(s);
						Q.Add(new Event((Global.Roll(20)+35)*100,EventType.BOSS_ARRIVE));
					}
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

