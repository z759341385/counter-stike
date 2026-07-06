using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

class Program
{
    static void Main()
    {
        CHandle<CBasePlayerWeapon> weapon = null!;
        bool isPrimary = weapon.Value?.As<CCSWeaponBase>().VData?.GearSlot == gear_slot_t.GEAR_SLOT_RIFLE;
        Console.WriteLine("Compiled: " + isPrimary);
    }
}
