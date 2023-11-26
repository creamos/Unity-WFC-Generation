using System;

[Flags]
public enum ModuleFlag
{
    Grounded = 1 << 0,
    Airborne = 1 << 1,
    Roof = 1 << 2,
    Inside = 1 << 3
}
