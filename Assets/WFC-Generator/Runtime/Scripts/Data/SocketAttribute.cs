using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class SocketAttribute : 
    PropertyAttribute
{
    public ESocketMode mode;
    public SocketAttribute (ESocketMode mode)
    {
        this.mode = mode;
    }

    public enum ESocketMode
    {
        Side, Vertical
    }
}
