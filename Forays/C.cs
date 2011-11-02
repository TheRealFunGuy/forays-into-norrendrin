//constants and enums

using System;
namespace Forays
{
	public enum TT{WALL,FLOOR,DOOR_O,DOOR_C,STAIRS}; //Tile type
	public enum ET{ANY_EVENT,MOVE,REMOVE_ATTR}; //Event type
	public enum AT{UNDEAD,ON_FIRE}; //Attribute type
	public enum AIT{HUMAN,STANDARD}; //AI type
	public static class C
	{
		public static int ROWS{get{return 24;}}
		public static int COLS{get{return 66;}}
	}
}

