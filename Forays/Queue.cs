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
	}
	public class Event{
		private PhysicalObject target;
		private int delay;
		private EventType type;
		private AttrType attr;
		private int value;
		private string msg;
		private int time_created;
		private bool dead;
		public static Queue Q{get;set;}
		public static Buffer B{get;set;}
		public Event(PhysicalObject target_,int delay_){
			target=target_;
			delay=delay_;
			type=EventType.MOVE;
			value=0;
			msg="";
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
			time_created=Q.turn;
			dead=false;
		}
		public Event(PhysicalObject target_,int delay_,EventType type_,AttrType attr_,int value_,string msg_){
			target=target_;
			delay=delay_;
			type=type_;
			attr=attr_;
			value=value_;
			msg=msg_;
			time_created=Q.turn;
			dead=false;
		}
		public int TimeToExecute(){ return delay + time_created; }
		public void Kill(PhysicalObject target_,EventType type_){
			if(target==target_ && (type==type_ || type_==EventType.ANY_EVENT)){
				target = null;
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
					temp.attrs[attr] -= value;
					if(attr == AttrType.IMMOBILIZED && temp.attrs[attr] < 0){ //check here for attrs that shouldn't drop below 0
						temp.attrs[attr] = 0;
					}
					if(attr==AttrType.ENHANCED_TORCH && temp.light_radius > 0){
						temp.UpdateRadius(temp.light_radius,6,true); //where 6 is the default radius
					}
					break;
					}
				}
				if(msg != ""){
					B.Add(msg);
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

