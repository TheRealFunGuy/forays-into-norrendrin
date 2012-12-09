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
		public int turn{get;set;}
		public int Count(){return list.Count; }
		public int Tiebreaker{get{
				if(list.Count > 0){
					return list.First.Value.tiebreaker;
				}
				else{
					return -1;
				}
			}
		}
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
						else{ //it's going between two events
							LinkedListNode<Event> current = list.Last;
							while(true){
								if(e >= current.Previous.Value){
									list.AddAfter(current.Previous,e);
									return;
								}
								else{
									current = current.Previous;
								}
							}
								/*if(e.TimeToExecute() == current.Value.TimeToExecute()){
									if(e.type != EventType.MOVE){
										list.AddAfter(current,e);
										return;
									}
									else{
										while(current.Value.type != EventType.MOVE){
											if(current == list.First){
												list.AddFirst(e);
												return;
											}
											else{
												if(e.TimeToExecute() != current.Previous.Value.TimeToExecute()){
													list.AddBefore(current,e);
													return;
												}
												else{
													current = current.Previous;
												}
											}
										}
										list.AddAfter(current,e);
										return;
									}
								}
								else{
									if(e < current.Value){
										if(e > current.Previous.Value){
											list.AddBefore(current,e);
											return;
										}
										else{
											current = current.Previous;
										}
									}
								}
							}*/
						}
					}
				}
			}
		}
		public void Pop(){
			turn = list.First.Value.TimeToExecute();
			Event e = list.First.Value;
			//list.First.Value.Execute();
			//list.RemoveFirst();
			e.Execute();
			list.Remove(e);
		}
		public void ResetForNewLevel(){
			LinkedList<Event> newlist = new LinkedList<Event>();
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				if(current.Value.target == Event.player){
					newlist.AddLast(current.Value);
				}
			}
			list = newlist;
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
		public void UpdateTiebreaker(int new_tiebreaker){
			for(LinkedListNode<Event> current = list.First;current!=null;current = current.Next){
				if(current.Value.tiebreaker >= new_tiebreaker){
					current.Value.tiebreaker++;
				}
			}
		}
	}
	public class Event{
		public PhysicalObject target{get;set;}
		public List<Tile> area = null;
		public int delay{get;set;}
		public EventType type{get;set;}
		public AttrType attr{get;set;}
		public int value{get;set;}
		public string msg{get;set;}
		public List<PhysicalObject> msg_objs; //used to determine visibility of msg
		public int time_created{get;set;}
		public bool dead{get;set;}
		public int tiebreaker{get;set;}
		public static Queue Q{get;set;}
		public static Buffer B{get;set;}
		public static Map M{get;set;}
		public static Actor player{get;set;}
		public Event(){}
		public Event(PhysicalObject target_,int delay_){
			target=target_;
			delay=delay_;
			type=EventType.MOVE;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
		}
		public Event(PhysicalObject target_,List<Tile> area_,int delay_,EventType type_){
			target=target_;
			area=area_;
			delay=delay_;
			type=type_;
			attr=AttrType.NO_ATTR;
			value=0;
			msg="";
			msg_objs = null;
			time_created=Q.turn;
			dead=false;
			tiebreaker = Q.Tiebreaker;
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
			tiebreaker = Q.Tiebreaker;
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
			if(target_ == null && type_ == EventType.BLAST_FUNGUS && type == EventType.BLAST_FUNGUS){
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
					break;
				}
				case EventType.REMOVE_ATTR:
				{
					Actor temp = target as Actor;
					if(temp.type == ActorType.BERSERKER && attr == AttrType.COOLDOWN_2){
						temp.attrs[attr] = 0;
					}
					else{
						temp.attrs[attr] -= value;
					}
					if(attr == AttrType.TELEPORTING || attr == AttrType.ARCANE_SHIELDED){
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
							if(temp.HasAttr(AttrType.LONG_STRIDE)){
								temp.speed = 80;
							}
							else{
								temp.speed = 100;
							}
						}
					}
					if(attr==AttrType.AFRAID && target == player){
						Global.FlushInput();
					}
					if(attr==AttrType.BLOOD_BOILED){
						temp.speed += (10 * value);
					}
					if(attr==AttrType.CONVICTION){
						if(temp.HasAttr(AttrType.IN_COMBAT)){
							temp.attrs[Forays.AttrType.CONVICTION] += value; //whoops, undo that
						}
						else{
							temp.attrs[Forays.AttrType.BONUS_SPIRIT] -= value;      //otherwise, set things to normal
							temp.attrs[Forays.AttrType.BONUS_COMBAT] -= value / 2;
							if(temp.attrs[Forays.AttrType.KILLSTREAK] >= 2){
								B.Add("You wipe off your weapon. ");
							}
							temp.attrs[Forays.AttrType.KILLSTREAK] = 0;
						}
					}
					if(attr==AttrType.STUNNED && msg.Contains("disoriented")){
						if(!player.CanSee(target)){
							msg = "";
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
						temp.TakeDamage(DamageType.NORMAL,DamageClass.NO_TYPE,8888,null);
					}
					break;
				}
				case EventType.CHECK_FOR_HIDDEN:
				{
					List<Tile> removed = new List<Tile>();
					foreach(Tile t in area){
						if(player.CanSee(t)){
							int exponent = player.DistanceFrom(t) + 1;
							if(player.HasAttr(AttrType.KEEN_EYES)){
								--exponent;
							}
							if(!t.IsLit()){
								if(!player.HasAttr(AttrType.SHADOWSIGHT)){
									++exponent;
								}
							}
							if(exponent > 8){
								exponent = 8; //because 1 in 256 is enough.
							}
							int difficulty = 1;
							for(int i=exponent;i>0;--i){
								difficulty = difficulty * 2;
							}
							if(Global.Roll(difficulty) == difficulty){
								if(t.IsTrap() || t.Is(TileType.FIRE_GEYSER) || t.Is(TileType.FOG_VENT) || t.Is(TileType.POISON_GAS_VENT)){
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
					&& !Q.Contains(EventType.REGENERATING_FROM_DEATH) && !Q.Contains(EventType.MIMIC) && !Q.Contains(EventType.MARBLE_HORROR)){
						B.Add("The dungeon is still and silent. ");
						B.PrintAll();
					}
					else{
						Q.Add(new Event((Global.Roll(20)+40)*100,EventType.RELATIVELY_SAFE));
					}
					break;
				}
				case EventType.POLTERGEIST:
				{
					if(target != null && target is Actor){ //target can either be a stolen item, or the currently manifested poltergeist.
						Q.Add(new Event(target,area,(Global.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,0,""));
						break; //if it's manifested, the event does nothing for now.
					}
					if(area.Any(t => t.actor() == player)){
						bool manifested = false;
						if(value == 0){
							B.Add("You feel like you're being watched. ");
						}
						else{
							if(target != null){ //if it has a stolen item
								Tile tile = null;
								tile = area.Where(t => t.actor() == null && t.DistanceFrom(player) >= 2
								                  && t.HasLOE(player) && t.FirstActorInLine(player) == player).Random();
								if(tile != null){
									Actor temporary = new Actor(ActorType.POLTERGEIST,"something",'G',Color.DarkGreen,1,1,0,0);
									temporary.a_name = "something";
									temporary.the_name = "something";
									temporary.p = tile.p;
									temporary.inv = new List<Item>();
									temporary.inv.Add(target as Item);
									Item item = temporary.inv[0];
									if(item.symbol == '*'){ //orbs
										if(item.type == ConsumableType.SUNLIGHT || item.type == ConsumableType.DARKNESS){
											B.Add(temporary.You("throw") + " " + item.AName() + ". ",temporary);
											B.DisplayNow();
											Screen.AnimateProjectile(tile.GetBestExtendedLineOfEffect(player).ToFirstObstruction(),new colorchar(item.color,item.symbol));
											B.Add(item.TheName() + " shatters on you! ");
										}
										temporary.inv[0].Use(temporary,temporary.GetBestExtendedLineOfEffect(player));
									}
									else{
										B.Add(temporary.You("throw") + " " + item.AName() + ". ",temporary);
										B.DisplayNow();
										Screen.AnimateProjectile(tile.GetBestExtendedLineOfEffect(player).ToFirstObstruction(),new colorchar(item.color,item.symbol));
										player.tile().GetItem(item);
										B.Add(item.TheName() + " hits you. ");
										player.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(6),temporary,"a flying " + item.Name());
									}
									target = null;
								}
								else{
									Q.Add(new Event(target,area,100,EventType.POLTERGEIST,AttrType.NO_ATTR,value,""));
									return; //try again next turn
								}
							}
							else{
								if(value >= 3 && area.Any(t => t.DistanceFrom(player) == 1 && t.passable && t.actor() == null)){
									Tile tile = area.Where(t => t.DistanceFrom(player) == 1 && t.passable && t.actor() == null).Random();
									B.DisplayNow();
									for(int i=4;i>0;--i){
										Screen.AnimateStorm(tile.p,i,2,1,'G',Color.DarkGreen);
									}
									Actor a = Actor.Create(ActorType.POLTERGEIST,tile.row,tile.col);
									Q.KillEvents(a,EventType.MOVE);
									a.Q0();
									a.player_visibility_duration = -1;
									foreach(Event e in Q.list){
										if(e.target == a && e.type == EventType.MOVE){
											e.tiebreaker = this.tiebreaker;
											break;
										}
									}
									Actor.tiebreakers[tiebreaker] = a;
									B.Add("A poltergeist manifests in front of you! ");
									Q.Add(new Event(a,area,(Global.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,0,""));
									manifested = true;
								}
								else{
									if(player.tile().type == TileType.DOOR_O){
										B.Add("The door slams closed on you! ");
										player.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(6),null,"a slamming door");
									}
									else{
										Tile tile = null; //check for items to throw...
										tile = area.Where(t => t.inv != null && t.actor() == null && t.DistanceFrom(player) >= 2
										                  && t.HasLOE(player) && t.FirstActorInLine(player) == player).Random();
										if(tile != null){
											Actor temporary = new Actor(ActorType.POLTERGEIST,"something",'G',Color.DarkGreen,1,1,0,0);
											temporary.a_name = "something";
											temporary.the_name = "something";
											temporary.p = tile.p;
											temporary.inv = new List<Item>();
											if(tile.inv.quantity <= 1){
												temporary.inv.Add(tile.inv);
												tile.inv = null;
											}
											else{
												temporary.inv.Add(new Item(tile.inv,-1,-1));
												tile.inv.quantity--;
											}
											M.Draw();
											Item item = temporary.inv[0];
											if(item.symbol == '*'){ //orbs
												if(item.type == ConsumableType.SUNLIGHT || item.type == ConsumableType.DARKNESS){
													B.Add(temporary.You("throw") + " " + item.TheName() + ". ",temporary);
													B.DisplayNow();
													Screen.AnimateProjectile(tile.GetBestExtendedLineOfEffect(player).ToFirstObstruction(),new colorchar(item.color,item.symbol));
													B.Add(item.TheName() + " shatters on you! ");
												}
												temporary.inv[0].Use(temporary,temporary.GetBestExtendedLineOfEffect(player));
											}
											else{
												B.Add(temporary.You("throw") + " " + item.TheName() + ". ",temporary);
												B.DisplayNow();
												Screen.AnimateProjectile(tile.GetBestExtendedLineOfEffect(player).ToFirstObstruction(),new colorchar(item.color,item.symbol));
												player.tile().GetItem(item);
												B.Add(item.TheName() + " hits you. ");
												player.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(6),temporary,"a flying " + item.Name());
											}
										}
										else{
											if(area.Any(t => t.type == TileType.DOOR_O || t.type == TileType.DOOR_C)){
												Tile door = area.Where(t=>t.type == TileType.DOOR_O || t.type == TileType.DOOR_C).Random();
												if(door.type == TileType.DOOR_C){
													if(player.CanSee(door)){
														B.Add("The door flies open! ",door);
													}
													else{
														if(door.seen || player.DistanceFrom(door) <= 12){
															B.Add("You hear a door slamming. ");
														}
													}
													door.Toggle(null);
												}
												else{
													if(door.actor() == null){
														if(player.CanSee(door)){
															B.Add("The door slams closed! ",door);
														}
														else{
															if(door.seen || player.DistanceFrom(door) <= 12){
																B.Add("You hear a door slamming. ");
															}
														}
														door.Toggle(null);
													}
													else{
														if(player.CanSee(door)){
															B.Add("The door slams closed on " + door.actor().TheVisible() + "! ",door);
														}
														else{
															if(player.DistanceFrom(door) <= 12){
																B.Add("You hear a door slamming and a grunt of pain. ");
															}
														}
														door.actor().TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(6),null,"a slamming door");
													}
												}
											}
											else{
												B.Add("You hear mocking laughter from nearby. ");
											}
										}
									}
								}
							}
						}
						if(!manifested){
							Q.Add(new Event(target,area,(Global.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,value+1,""));
						}
					}
					else{
						Q.Add(new Event(target,area,(Global.Roll(8)+6)*100,EventType.POLTERGEIST,AttrType.NO_ATTR,0,""));
					}
					break;
				}
				case EventType.MIMIC:
				{
					Item item = target as Item;
					if(area[0].inv != item){ //it could have been picked up by the player or moved in another way
						foreach(Tile t in M.AllTiles()){ //if it was moved, make the correction to the event's area.
							if(t.inv == item){
								area = new List<Tile>{t};
								break;
							}
						}
					}
					if(area[0].inv == item){
						bool attacked = false;
						if(player.DistanceFrom(area[0]) == 1 && area[0].actor() == null){
							if(player.Stealth() * 5 < Global.Roll(1,100)){
								B.Add(item.TheName() + " suddenly grows tentacles! ");
								attacked = true;
								area[0].inv = null;
								Actor a = Actor.Create(ActorType.MIMIC,area[0].row,area[0].col);
								Q.KillEvents(a,EventType.MOVE);
								a.Q0();
								a.player_visibility_duration = -1;
								a.symbol = item.symbol;
								a.color = item.color;
								foreach(Event e in Q.list){
									if(e.target == a && e.type == EventType.MOVE){
										e.tiebreaker = this.tiebreaker;
										break;
									}
								}
								Actor.tiebreakers[tiebreaker] = a;
							}
						}
						if(!attacked){
							Q.Add(new Event(target,area,100,EventType.MIMIC,AttrType.NO_ATTR,0,""));
						}
					}
					else{ //if the item is missing, we assume that the player just picked it up
						List<Tile> open = new List<Tile>();
						foreach(Tile t in player.TilesAtDistance(1)){
							if(t.passable && t.actor() == null){
								open.Add(t);
							}
						}
						if(open.Count > 0){
							Tile t = open.Random();
							B.Add(item.TheName() + " suddenly grows tentacles! ");
							Actor a = Actor.Create(ActorType.MIMIC,t.row,t.col);
							Q.KillEvents(a,EventType.MOVE);
							a.Q0();
							a.player_visibility_duration = -1;
							a.symbol = item.symbol;
							a.color = item.color;
							foreach(Event e in Q.list){
								if(e.target == a && e.type == EventType.MOVE){
									e.tiebreaker = this.tiebreaker;
									break;
								}
							}
							Actor.tiebreakers[tiebreaker] = a;
							player.inv.Remove(item);
						}
						else{
							B.Add("Your pack feels lighter. ");
							player.inv.Remove(item);
						}
					}
					break;
				}
				case EventType.GRENADE:
					{
					Tile t = target as Tile;
					if(t.Is(FeatureType.GRENADE)){
						t.features.Remove(FeatureType.GRENADE);
						B.Add("The grenade explodes! ",t);
						if(t.seen){
							Screen.WriteMapChar(t.row,t.col,M.VisibleColorChar(t.row,t.col));
						}
						B.DisplayNow();
						List<pos> cells = new List<pos>();
						foreach(Tile tile in t.TilesWithinDistance(1)){
							if(tile.passable && tile.seen){
								cells.Add(tile.p);
							}
						}
						Screen.AnimateMapCells(cells,new colorchar('*',Color.DarkRed));
						//Screen.AnimateExplosion(t,1,new colorchar('*',Color.DarkRed));
						foreach(Actor a in t.ActorsWithinDistance(1)){
							a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(3,6),null,"an exploding grenade");
						}
						if(t.actor() != null){
							int dir = Global.RandomDirection();
							t.actor().GetKnockedBack(t.TileInDirection(t.actor().RotateDirection(dir,true,4)));
						}
						if(player.DistanceFrom(t) <= 3){
							player.MakeNoise(); //hacky - todo change
						}
					}
					break;
					}
				case EventType.BLAST_FUNGUS:
				{
					Tile t = target as Tile;
					if(t.Is(FeatureType.FUNGUS_PRIMED)){
						t.features.Remove(FeatureType.FUNGUS_PRIMED);
						B.Add("The blast fungus explodes! ",t);
						if(t.seen){
							Screen.WriteMapChar(t.row,t.col,M.VisibleColorChar(t.row,t.col));
						}
						B.DisplayNow();
						for(int i=1;i<=3;++i){
							List<pos> cells = new List<pos>();
							foreach(Tile tile in t.TilesWithinDistance(i)){
								if(t.HasLOE(tile) && tile.passable && tile.seen){
									cells.Add(tile.p);
								}
							}
							Screen.AnimateMapCells(cells,new colorchar('*',Color.DarkRed));
						}
						foreach(Actor a in t.ActorsWithinDistance(3)){
							if(t.HasLOE(a)){
								a.TakeDamage(DamageType.NORMAL,DamageClass.PHYSICAL,Global.Roll(5,6),null,"an exploding blast fungus");
							}
						}
						if(t.actor() != null){
							int dir = Global.RandomDirection();
							t.actor().GetKnockedBack(t.TileInDirection(t.actor().RotateDirection(dir,true,4)));
						}
						if(player.DistanceFrom(t) <= 3){
							player.MakeNoise(); //hacky - todo change
						}
					}
					if(t.Is(FeatureType.FUNGUS_ACTIVE)){
						t.features.Remove(FeatureType.FUNGUS_ACTIVE);
						t.features.Add(FeatureType.FUNGUS_PRIMED);
						Q.Add(new Event(t,100,EventType.BLAST_FUNGUS));
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
				case EventType.FIRE_GEYSER:
				{
					int frequency = value / 10; //5-25
					int variance = value % 10; //0-9
					int variance_amount = (frequency * variance) / 10;
					int number_of_values = variance_amount*2 + 1;
					int minimum_value = frequency - variance_amount;
					if(minimum_value < 5){
						int diff = 5 - minimum_value;
						number_of_values -= diff;
						minimum_value = 5;
					}
					int delay = ((minimum_value - 1) + Global.Roll(number_of_values)) * 100;
					Q.Add(new Event(target,delay+200,EventType.FIRE_GEYSER,value));
					Q.Add(new Event(target,delay,EventType.FIRE_GEYSER_ERUPTION,2));
					break;
				}
				case EventType.FIRE_GEYSER_ERUPTION:
				{
					if(target.name == "floor"){
						Event hiddencheck = null;
						Tile t = target as Tile;
						foreach(Event e in Q.list){
							if(!e.dead && e.type == EventType.CHECK_FOR_HIDDEN){
								hiddencheck = e;
								break;
							}
						}
						if(player.HasLOS(t)){
							//t.seen = true;
							if(hiddencheck != null){
								hiddencheck.area.Remove(t);
							}
							t.name = Tile.Prototype(t.type).name;
							t.a_name = Tile.Prototype(t.type).a_name;
							t.the_name = Tile.Prototype(t.type).the_name;
							t.symbol = Tile.Prototype(t.type).symbol;
							t.color = Tile.Prototype(t.type).color;
						}
					}
					if(value >= 0){ //a value of -1 means 'reset light radius to 0'
						if(target.light_radius == 0){
							target.UpdateRadius(0,8,true);
						}
						B.Add(target.the_name + " spouts flames! ",target);
						M.Draw();
						for(int i=0;i<3;++i){
							List<pos> cells = new List<pos>();
							List<Tile> tiles = target.TilesWithinDistance(1);
							for(int j=0;j<5;++j){
								Tile t = tiles.RemoveRandom();
								if(player.CanSee(t)){
									cells.Add(t.p);
								}
							}
							if(cells.Count > 0){
								Screen.AnimateMapCells(cells,new colorchar('*',Color.Red),35);
							}
						}
						foreach(Tile t in target.TilesWithinDistance(1)){
							Actor a = t.actor();
							if(a != null){
								if(a.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,Global.Roll(2,6),null,"a fiery eruption")){
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
							}
							if(t.Is(FeatureType.TROLL_CORPSE)){
								t.features.Remove(FeatureType.TROLL_CORPSE);
								B.Add("The troll corpse burns to ashes! ",t);
							}
							if(t.Is(FeatureType.TROLL_SEER_CORPSE)){
								t.features.Remove(FeatureType.TROLL_SEER_CORPSE);
								B.Add("The troll seer corpse burns to ashes! ",t);
							}
						}
						Q.Add(new Event(target,100,EventType.FIRE_GEYSER_ERUPTION,value - 1));
					}
					else{
						target.UpdateRadius(8,0,true);
					}
					break;
				}
				case EventType.FOG_VENT:
				{
					if(target.name == "floor"){
						Event hiddencheck = null;
						Tile t = target as Tile;
						foreach(Event e in Q.list){
							if(!e.dead && e.type == EventType.CHECK_FOR_HIDDEN){
								hiddencheck = e;
								break;
							}
						}
						if(player.CanSee(t)){
							//t.seen = true;
							if(hiddencheck != null){
								hiddencheck.area.Remove(t);
							}
							t.name = Tile.Prototype(t.type).name;
							t.a_name = Tile.Prototype(t.type).a_name;
							t.the_name = Tile.Prototype(t.type).the_name;
							t.symbol = Tile.Prototype(t.type).symbol;
							t.color = Tile.Prototype(t.type).color;
						}
					}
					Tile current = target as Tile;
					if(!current.Is(FeatureType.FOG)){
						current.AddOpaqueFeature(FeatureType.FOG);
						Q.Add(new Event(new List<Tile>{current},400,EventType.FOG));
					}
					else{
						for(int tries=0;tries<50;++tries){
							List<Tile> open = new List<Tile>();
							foreach(Tile t in current.TilesAtDistance(1)){
								if(t.passable){
									open.Add(t);
								}
							}
							if(open.Count > 0){
								Tile possible = open.Random();
								if(!possible.Is(FeatureType.FOG)){
									possible.AddOpaqueFeature(FeatureType.FOG);
									Q.Add(new Event(new List<Tile>{possible},400,EventType.FOG));
									break;
								}
								else{
									current = possible;
								}
							}
							else{
								break;
							}
						}
					}
					Q.Add(new Event(target,100,EventType.FOG_VENT));
					break;
				}
				case EventType.FOG:
				{
					List<Tile> removed = new List<Tile>();
					foreach(Tile t in area){
						if(t.Is(FeatureType.FOG) && Global.OneIn(4)){
							t.RemoveOpaqueFeature(FeatureType.FOG);
							removed.Add(t);
						}
					}
					foreach(Tile t in removed){
						area.Remove(t);
					}
					if(area.Count > 0){
						Q.Add(new Event(area,100,EventType.FOG));
					}
					break;
				}
				case EventType.POISON_GAS_VENT:
				{
					if(target.name == "floor"){
						Event hiddencheck = null;
						Tile t = target as Tile;
						foreach(Event e in Q.list){
							if(!e.dead && e.type == EventType.CHECK_FOR_HIDDEN){
								hiddencheck = e;
								break;
							}
						}
						if(player.CanSee(t)){
							//t.seen = true;
							if(hiddencheck != null){
								hiddencheck.area.Remove(t);
							}
							t.name = Tile.Prototype(t.type).name;
							t.a_name = Tile.Prototype(t.type).a_name;
							t.the_name = Tile.Prototype(t.type).the_name;
							t.symbol = Tile.Prototype(t.type).symbol;
							t.color = Tile.Prototype(t.type).color;
						}
					}
					Tile current = target as Tile;
					if(Global.OneIn(7)){
						int num = Global.Roll(5) + 2;
						List<Tile> new_area = new List<Tile>();
						for(int i=0;i<num;++i){
							if(!current.Is(FeatureType.POISON_GAS)){
								current.features.Add(FeatureType.POISON_GAS);
								new_area.Add(current);
							}
							else{
								for(int tries=0;tries<50;++tries){
									List<Tile> open = new List<Tile>();
									foreach(Tile t in current.TilesAtDistance(1)){
										if(t.passable){
											open.Add(t);
										}
									}
									if(open.Count > 0){
										Tile possible = open.Random();
										if(!possible.Is(FeatureType.POISON_GAS)){
											possible.features.Add(FeatureType.POISON_GAS);
											new_area.Add(possible);
											break;
										}
										else{
											current = possible;
										}
									}
									else{
										break;
									}
								}
							}
						}
						if(new_area.Count > 0){
							B.Add("Toxic vapors pour from " + target.the_name + "! ",target);
							Q.Add(new Event(new_area,200,EventType.POISON_GAS));
						}
					}
					Q.Add(new Event(target,100,EventType.POISON_GAS_VENT));
					break;
				}
				case EventType.POISON_GAS:
				{
					List<Tile> removed = new List<Tile>();
					foreach(Tile t in area){
						if(t.Is(FeatureType.POISON_GAS) && Global.OneIn(6)){
							t.RemoveOpaqueFeature(FeatureType.POISON_GAS);
							removed.Add(t);
						}
					}
					foreach(Tile t in removed){
						area.Remove(t);
					}
					if(area.Count > 0){
						Q.Add(new Event(area,100,EventType.POISON_GAS));
					}
					break;
				}
				case EventType.STONE_SLAB:
				{
					Tile t = target as Tile;
					if(t.type == TileType.STONE_SLAB && (t.IsLitFromAnywhere(true) || area.Any(x=>x.actor()!=null))){
						bool vis = player.CanSee(t);
						t.Toggle(null,Forays.TileType.FLOOR);
						if(!vis && player.CanSee(t)){
							vis = true;
						}
						if(vis){
							B.Add("The stone slab rises with a grinding sound. ");
						}
						else{
							if(player.DistanceFrom(t) <= 6){
								B.Add("You hear a grinding sound. ");
							}
						}
					}
					else{
						if(t.type == TileType.FLOOR && !t.IsLitFromAnywhere(true) && t.actor() == null && !area.Any(x=>x.actor()!=null)){
							bool vis = player.CanSee(t);
							t.Toggle(null,Forays.TileType.STONE_SLAB);
							if(!vis && player.CanSee(t)){
								vis = true;
							}
							if(vis){
								B.Add("The stone slab descends with a grinding sound. ");
							}
							else{
								if(player.DistanceFrom(t) <= 6){
									B.Add("You hear a grinding sound. ");
								}
							}
						}
					}
					Q.Add(new Event(target,area,100,EventType.STONE_SLAB));
					break;
				}
				case EventType.MARBLE_HORROR:
				{
					Tile t = target as Tile;
					if(t.type == TileType.STATUE){
						if(value == 1 && player.CanSee(t) && !t.IsLit() && t.actor() == null){ //if target was visible last turn & this turn, and it's currently in darkness...
							t.TransformTo(TileType.FLOOR);
							Actor a = Actor.Create(ActorType.MARBLE_HORROR,t.row,t.col,true,true);
							foreach(Event e in Q.list){
								if(e.target == a && e.type == EventType.MOVE){
									e.dead = true;
									break;
								}
							}
							a.Q0();
							switch(Global.Roll(2)){
							case 1:
								B.Add("You think that statue might have just moved... ");
								break;
							case 2:
								B.Add("The statue turns its head to face you. ");
								break;
							}
						}
						else{
							if(player.CanSee(t)){
								Q.Add(new Event(target,100,EventType.MARBLE_HORROR,1));
							}
							else{
								Q.Add(new Event(target,100,EventType.MARBLE_HORROR,0));
							}
						}
					}
					break;
				}
				case EventType.REGENERATING_FROM_DEATH:
				{
					if(target.tile().Is(FeatureType.TROLL_CORPSE)){ //otherwise, assume it was destroyed by fire
						value++;
						if(value > 0 && target.actor() == null){
							Actor a = Actor.Create(ActorType.TROLL,target.row,target.col);
							foreach(Event e in Q.list){
								if(e.target == M.actor[target.row,target.col] && e.type == EventType.MOVE){
									e.tiebreaker = this.tiebreaker;
									break;
								}
							}
							Actor.tiebreakers[tiebreaker] = a;
							target.actor().curhp = value;
							target.actor().level = 0;
							target.actor().attrs[Forays.AttrType.NO_ITEM]++;
							B.Add("The troll stands up! ",target);
							target.actor().player_visibility_duration = -1;
							if(target.tile().type == TileType.DOOR_C){
								target.tile().Toggle(target.actor());
							}
							target.tile().features.Remove(FeatureType.TROLL_CORPSE);
							if(Global.OneIn(3)){
								target.actor().attrs[Forays.AttrType.WANDERING]++;
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
								B.Add("The troll on the floor regenerates. ",target);
								break;
							default:
								break;
							}
							Q.Add(new Event(target,null,100,EventType.REGENERATING_FROM_DEATH,attr,value,""));
						}
					}
					if(target.tile().Is(FeatureType.TROLL_SEER_CORPSE)){ //otherwise, assume it was destroyed by fire
						value++;
						if(value > 0 && target.actor() == null){
							Actor a = Actor.Create(ActorType.TROLL_SEER,target.row,target.col);
							foreach(Event e in Q.list){
								if(e.target == M.actor[target.row,target.col] && e.type == EventType.MOVE){
									e.tiebreaker = this.tiebreaker;
									break;
								}
							}
							Actor.tiebreakers[tiebreaker] = a;
							target.actor().curhp = value;
							target.actor().level = 0;
							target.actor().attrs[Forays.AttrType.NO_ITEM]++;
							B.Add("The troll seer stands up! ",target);
							target.actor().player_visibility_duration = -1;
							if(attr == AttrType.COOLDOWN_1){
								target.actor().attrs[Forays.AttrType.COOLDOWN_1]++;
							}
							if(target.tile().type == TileType.DOOR_C){
								target.tile().Toggle(target.actor());
							}
							target.tile().features.Remove(FeatureType.TROLL_SEER_CORPSE);
							if(Global.OneIn(3)){
								target.actor().attrs[Forays.AttrType.WANDERING]++;
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
								B.Add("The troll seer's corpse twitches. ",target);
								break;
							case 3:
							case 4:
								B.Add("You hear sounds coming from the troll seer's corpse. ",target);
								break;
							case 5:
								B.Add("The troll seer on the floor regenerates. ",target);
								break;
							default:
								break;
							}
							Q.Add(new Event(target,null,100,EventType.REGENERATING_FROM_DEATH,attr,value,""));
						}
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
							if(t.Is(FeatureType.TROLL_CORPSE)){
								t.features.Remove(FeatureType.TROLL_CORPSE);
								B.Add("The troll corpse burns to ashes! ",t);
							}
							if(t.Is(FeatureType.TROLL_SEER_CORPSE)){
								t.features.Remove(FeatureType.TROLL_SEER_CORPSE);
								B.Add("The troll seer corpse burns to ashes! ",t);
							}
							if(t.Is(FeatureType.FUNGUS)){
								Q.Add(new Event(t,200,EventType.BLAST_FUNGUS));
								Actor.B.Add("The blast fungus starts to smolder in the light. ",t);
								t.features.Remove(FeatureType.FUNGUS);
								t.features.Add(FeatureType.FUNGUS_ACTIVE);
							}
						}
					}
					if(value > 0){
						int radius = 4 - value;
						List<Tile> added = new List<Tile>();
						foreach(Tile t in target.TilesWithinDistance(radius)){
							if(t.passable && !t.Is(FeatureType.QUICKFIRE)
							&& t.IsAdjacentTo(FeatureType.QUICKFIRE) && !area.Contains(t)){
								added.Add(t);
							}
						}
						foreach(Tile t in added){
							area.Add(t);
							t.features.Add(FeatureType.QUICKFIRE);
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
								if(t.Is(FeatureType.TROLL_CORPSE)){
									t.features.Remove(FeatureType.TROLL_CORPSE);
									B.Add("The troll corpse burns to ashes! ",t);
								}
								if(t.Is(FeatureType.TROLL_SEER_CORPSE)){
									t.features.Remove(FeatureType.TROLL_SEER_CORPSE);
									B.Add("The troll seer corpse burns to ashes! ",t);
								}
								if(t.Is(FeatureType.FUNGUS)){
									Q.Add(new Event(t,200,EventType.BLAST_FUNGUS));
									Actor.B.Add("The blast fungus starts to smolder in the light. ",t);
									t.features.Remove(FeatureType.FUNGUS);
									t.features.Add(FeatureType.FUNGUS_ACTIVE);
								}
							}
						}
						foreach(Tile t in removed){
							area.Remove(t);
							t.features.Remove(FeatureType.QUICKFIRE);
						}
					}
					foreach(Actor a in actors){
						if(!a.HasAttr(AttrType.IMMUNE_FIRE) && !a.HasAttr(AttrType.INVULNERABLE)){
							if(player.CanSee(a.tile())){
								B.Add("The quickfire burns " + a.the_name + ". ",a);
							}
							a.TakeDamage(DamageType.FIRE,DamageClass.PHYSICAL,Global.Roll(6),null,"quickfire");
						}
					}
					--value;
					if(value > -5){
						Q.Add(new Event(target,area,100,EventType.QUICKFIRE,AttrType.NO_ATTR,value,""));
					}
					break;
				}
				case EventType.BOSS_SIGN:
				{
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
					if(!player.HasAttr(AttrType.RESTING)){
						B.AddIfEmpty(s);
					}
					Q.Add(new Event((Global.Roll(20)+35)*100,EventType.BOSS_SIGN));
					break;
				}
				case EventType.BOSS_ARRIVE:
				{
					bool spawned = false;
					Actor a = null;
					if(M.AllActors().Count == 1 && !Q.Contains(EventType.POLTERGEIST)){
						List<Tile> trolls = new List<Tile>();
						for(LinkedListNode<Event> current = Q.list.First;current!=null;current = current.Next){
							if(current.Value.type == EventType.REGENERATING_FROM_DEATH){
								trolls.Add((current.Value.target) as Tile);
							}
						}
						foreach(Tile troll in trolls){
							if(troll.Is(FeatureType.TROLL_CORPSE)){
								B.Add("The troll corpse burns to ashes! ",troll);
								troll.features.Remove(FeatureType.TROLL_CORPSE);
							}
							else{
								if(troll.Is(FeatureType.TROLL_SEER_CORPSE)){
									B.Add("The troll seer corpse burns to ashes! ",troll);
									troll.features.Remove(FeatureType.TROLL_SEER_CORPSE);
								}
							}
						}
						Q.KillEvents(null,EventType.REGENERATING_FROM_DEATH);
						List<Tile> goodtiles = M.AllTiles();
						List<Tile> removed = new List<Tile>();
						foreach(Tile t in goodtiles){
							if(!t.passable || t.Is(TileType.CHASM) || player.CanSee(t)){
								removed.Add(t);
							}
						}
						foreach(Tile t in removed){
							goodtiles.Remove(t);
						}
						if(goodtiles.Count > 0){
							B.Add("You hear a loud crash and a nearby roar! ");
							Tile t = goodtiles[Global.Roll(goodtiles.Count)-1];
							a = Actor.Create(ActorType.FIRE_DRAKE,t.row,t.col,true,false);
							spawned = true;
						}
						else{
							if(M.AllTiles().Any(t=>t.passable && !t.Is(TileType.CHASM) && t.actor() == null)){
								B.Add("You hear a loud crash and a nearby roar! ");
								Tile tile = M.AllTiles().Where(t=>t.passable && !t.Is(TileType.CHASM) && t.actor() == null).Random();
								a = Actor.Create(ActorType.FIRE_DRAKE,tile.row,tile.col,true,false);
								spawned = true;
							}
						}
					}
					if(!spawned){
						Q.Add(new Event(null,null,(Global.Roll(20)+10)*100,EventType.BOSS_ARRIVE,attr,value,""));
					}
					else{
						if(value > 0){
							a.curhp = value;
						}
						else{ //if there's no good value, this means that this is the first appearance.
							B.Add("The ground shakes as dust and rocks fall from the cavern ceiling. ");
							B.Add("This place is falling apart! ");
							List<Tile> floors = M.AllTiles().Where(t=>t.passable && t.type != TileType.CHASM && player.tile() != t);
							Tile tile = null;
							if(floors.Count > 0){
								tile = floors.Random();
								(tile as Tile).Toggle(null,TileType.CHASM);
							}
							Q.Add(new Event(tile,100,EventType.FLOOR_COLLAPSE));
							Q.Add(new Event((Global.Roll(20)+20)*100,EventType.CEILING_COLLAPSE));

						}
					}
					break;
				}
				case EventType.FLOOR_COLLAPSE:
				{
					Tile current = target as Tile;
					int tries = 0;
					if(current != null){
						for(tries=0;tries<50;++tries){
							List<Tile> open = new List<Tile>();
							foreach(Tile t in current.TilesAtDistance(1)){
								if(t.passable || t.Is(TileType.RUBBLE)){
									open.Add(t);
								}
							}
							if(open.Count > 0){
								Tile possible = open.Random();
								if(!possible.Is(TileType.CHASM)){
									possible.Toggle(null,TileType.CHASM);
									List<Tile> open_neighbors = possible.TilesAtDistance(1).Where(t=>t.passable && t.type != TileType.CHASM);
									int num_neighbors = open_neighbors.Count;
									while(open_neighbors.Count > num_neighbors/2){
										Tile neighbor = open_neighbors.RemoveRandom();
										neighbor.Toggle(null,TileType.CHASM);
									}
									break;
								}
								else{
									current = possible;
								}
							}
							else{
								break;
							}
						}
					}
					if(tries == 50 || current == null){
						List<Tile> floors = M.AllTiles().Where(t=>t.passable && t.type != TileType.CHASM && player.tile() != t);
						if(floors.Count > 0){
							target = floors.Random();
							(target as Tile).Toggle(null,TileType.CHASM);
						}
					}
					Q.Add(new Event(target,100,EventType.FLOOR_COLLAPSE));
					break;
				}
				case EventType.CEILING_COLLAPSE:
				{
					B.Add("The ground shakes and debris falls from the ceiling! ");
					for(int i=1;i<Global.ROWS-1;++i){
						for(int j=1;j<Global.COLS-1;++j){
							Tile t = M.tile[i,j];
							if(t.Is(TileType.WALL)){
								int num_walls = t.TilesAtDistance(1).Where(x=>x.Is(TileType.WALL)).Count;
								if(num_walls < 8 && Global.OneIn(20)){
									if(Global.CoinFlip()){
										t.Toggle(null,Forays.TileType.FLOOR);
										foreach(Tile neighbor in t.TilesAtDistance(1)){
											neighbor.solid_rock = false;
										}
									}
									else{
										t.Toggle(null,Forays.TileType.RUBBLE);
										foreach(Tile neighbor in t.TilesAtDistance(1)){
											neighbor.solid_rock = false;
											if(neighbor.type == TileType.FLOOR && Global.OneIn(10)){
												neighbor.Toggle(null,Forays.TileType.RUBBLE);
											}
										}
									}
								}
							}
							else{
								int num_walls = t.TilesAtDistance(1).Where(x=>x.Is(TileType.WALL)).Count;
								if(num_walls == 0 && Global.OneIn(100)){
									if(Global.OneIn(6)){
										t.Toggle(null,Forays.TileType.RUBBLE);
									}
									foreach(Tile neighbor in t.TilesAtDistance(1)){
										if(neighbor.type == TileType.FLOOR && Global.OneIn(6)){
											neighbor.Toggle(null,Forays.TileType.RUBBLE);
										}
									}
								}
							}
						}
					}
					Q.Add(new Event((Global.Roll(20)+20)*100,EventType.CEILING_COLLAPSE));
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
		/*public static bool operator <(Event one,Event two){
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
		}*/
		public static bool operator <(Event one,Event two){
			if(one.TimeToExecute() < two.TimeToExecute()){
				return true;
			}
			if(one.TimeToExecute() > two.TimeToExecute()){
				return false;
			}
			if(one.tiebreaker < two.tiebreaker){
				return true;
			}
			if(one.tiebreaker > two.tiebreaker){
				return false;
			}
			if(one.type == EventType.MOVE && two.type != EventType.MOVE){
				return true;
			}
			return false;
		}
		public static bool operator >(Event one,Event two){ //currently unused
			if(one.TimeToExecute() > two.TimeToExecute()){
				return true;
			}
			if(one.TimeToExecute() < two.TimeToExecute()){
				return false;
			}
			if(one.tiebreaker > two.tiebreaker){
				return true;
			}
			if(one.tiebreaker < two.tiebreaker){
				return false;
			}
			if(one.type != EventType.MOVE && two.type == EventType.MOVE){
				return true;
			}
			return false;
		}
		public static bool operator <=(Event one,Event two){ //currently unused
			if(one.TimeToExecute() < two.TimeToExecute()){
				return true;
			}
			if(one.TimeToExecute() > two.TimeToExecute()){
				return false;
			}
			if(one.tiebreaker < two.tiebreaker){
				return true;
			}
			if(one.tiebreaker > two.tiebreaker){
				return false;
			}
			if(one.type == EventType.MOVE){
				return true;
			}
			if(one.type != EventType.MOVE && two.type != EventType.MOVE){
				return true;
			}
			return false;
		}
		public static bool operator >=(Event one,Event two){
			if(one.TimeToExecute() > two.TimeToExecute()){
				return true;
			}
			if(one.TimeToExecute() < two.TimeToExecute()){
				return false;
			}
			if(one.tiebreaker > two.tiebreaker){
				return true;
			}
			if(one.tiebreaker < two.tiebreaker){
				return false;
			}
			if(one.type != EventType.MOVE){
				return true;
			}
			if(one.type == EventType.MOVE && two.type == EventType.MOVE){
				return true;
			}
			return false;
		}
	}
}

