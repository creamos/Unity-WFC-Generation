using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

[Serializable]
public class Module
{
    public GameObject Prefab;
    public int angle;
    public float weight;
    public ModuleFlag flag;
    public string xPrev,xNext, yPrev,yNext, zPrev, zNext;


    public string Socket (int side) => Socket((ModuleSide)(side % 6));
    public string Socket (ModuleSide side) => side switch {
        ModuleSide.XPrev => this.xPrev,
        ModuleSide.XNext => this.xNext,
        ModuleSide.YPrev => this.yPrev,
        ModuleSide.YNext => this.yNext,
        ModuleSide.ZPrev => this.zPrev,
        ModuleSide.ZNext => this.zNext,
        _ => SocketUtility.WILDCARD
    };

    public int CompareTo (Module other)
    {
        int compare;
        compare = this.Prefab.GetInstanceID().CompareTo(other.Prefab.GetInstanceID());
        if (compare != 0) return compare;

        compare = weight.CompareTo(other.weight);
        if (compare != 0) return compare;

        compare = this.xPrev.CompareTo(other.xPrev);
        if (compare != 0) return compare;

        compare = this.xNext.CompareTo(other.xNext);
        if (compare != 0) return compare;

        compare = this.yPrev.CompareTo(other.yPrev);
        if (compare != 0) return compare;

        compare = this.yNext.CompareTo(other.yNext);
        if (compare != 0) return compare;

        compare = this.zPrev.CompareTo(other.zPrev);
        if (compare != 0) return compare;

        compare = this.zNext.CompareTo(other.zNext);
        return compare;
    }


    public int[] ConnectingWith (Module[] otherModules, ModuleSide side) =>
        otherModules
            .Select((otherMod, id) => (data: otherMod, id: id))
            .Where(otherMod =>
                // If Top or Bottom socket
                ((side == ModuleSide.YPrev || side == ModuleSide.YNext)
                && SocketUtility.Matching(this.Socket(side), otherMod.data.Socket((int)(side == ModuleSide.YPrev ? ModuleSide.YNext : ModuleSide.YPrev)), true))
                // If Side socket
                || (!(side == ModuleSide.YPrev || side == ModuleSide.YNext)
                && SocketUtility.Matching(this.Socket(side), otherMod.data.Socket((int)(side + 2) % 4), false))
            )
            .Select(otherMod => otherMod.id)
            .ToArray();


    public class ModuleComparer : IEqualityComparer<Module>
    {
        public bool Equals (Module first, Module second)
        {
            return first.CompareTo(second) == 0;
        }

        public int GetHashCode (Module obj)
        {
            return $"{obj.Prefab.GetHashCode()} {obj.xPrev} {obj.xNext} {obj.yPrev} {obj.yNext} {obj.zPrev} {obj.zNext}".GetHashCode();
        }
    }
}