using System.Linq;
using UnityEngine;

// Captures an inventory gesture independently of IMGUI button hot controls.
// A changed inventory or round invalidates the gesture before it can consume a slot.
public sealed class EquipmentDrag
{
    public enum Result { None, Click, Drop, Cancel }
    public int Slot { get; private set; } = -1;
    public bool Dragging { get; private set; }
    int[] inventory;
    string context;
    Vector2 origin;
    public void Reset(){Slot=-1;Dragging=false;inventory=null;context=null;}
    public Result Update(Event e,int hovered,int[] current,string currentContext,bool allowed)
    {
        if(Slot>=0&&(!allowed||context!=currentContext||!inventory.SequenceEqual(current)))
        {Reset();return Result.Cancel;}
        if(!allowed)return Result.None;
        if(Slot>=0&&((e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape)||(e.type==EventType.MouseDown&&e.button==1)))
        {Reset();e.Use();return Result.Cancel;}
        if(e.type==EventType.MouseDown&&e.button==0&&hovered>=0&&hovered<current.Length)
        {Slot=hovered;inventory=(int[])current.Clone();context=currentContext;origin=e.mousePosition;Dragging=false;e.Use();}
        else if(Slot>=0&&e.type==EventType.MouseDrag)
        {if(Vector2.Distance(origin,e.mousePosition)>8)Dragging=true;e.Use();}
        else if(Slot>=0&&e.type==EventType.MouseUp&&e.button==0)
        {e.Use();return Dragging?Result.Drop:hovered==Slot?Result.Click:Result.Cancel;}
        return Result.None;
    }
}
