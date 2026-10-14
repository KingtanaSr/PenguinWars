// INVENTARIO DE CADA EQUIPO: MUNICIÓN, ARMA SELECCIONADA Y BOTIQUINES
// EACH TEAM'S INVENTORY: AMMO, SELECTED WEAPON AND MEDKITS

using System;
using System.Collections.Generic;
using System.Linq;
using PenguinWars.Combat;
using PenguinWars.Core;

namespace PenguinWars.Gameplay;

public class TeamInventory
{
    // Munición que da una caja.
    // Ammo given by a crate.
    public const int CrateAmmoAmount = 2;

    // Munición restante de cada arma; -1 significa ilimitada.
    // Remaining ammo for each weapon; -1 means unlimited.
    private readonly Dictionary<Weapon, int> _ammo = new();

    public IReadOnlyList<Weapon> Weapons { get; }
    public int Medkits { get; private set; }
    public int SelectedIndex { get; private set; }
    public Weapon SelectedWeapon => Weapons[SelectedIndex];

    public TeamInventory(Team team)
    {
        Weapons = Weapon.GetTeamWeapons(team);

        foreach (Weapon weapon in Weapons)
            _ammo[weapon] = weapon.StartingAmmo;
    }

    public int GetAmmo(Weapon weapon) => _ammo[weapon];

    // Un arma tiene munición si es ilimitada (-1) o le queda al menos una.
    // A weapon has ammo if it is unlimited (-1) or has at least one left.
    public bool HasAmmo(Weapon weapon) => _ammo[weapon] != 0;

    // Selecciona un arma por posición, solo si tiene munición.
    // Selects a weapon by position, only if it has ammo.
    public void Select(int index)
    {
        if (index >= 0 && index < Weapons.Count && HasAmmo(Weapons[index]))
            SelectedIndex = index;
    }

    // Pasa a la siguiente (1) o anterior (-1) arma con munición.
    // Moves to the next (1) or previous (-1) weapon with ammo.
    public void SelectNext(int direction)
    {
        int index = SelectedIndex;

        for (int i = 0; i < Weapons.Count; i++)
        {
            index = (index + direction + Weapons.Count) % Weapons.Count;
            if (HasAmmo(Weapons[index]))
            {
                SelectedIndex = index;
                return;
            }
        }
    }

    // Gasta una unidad de munición; si se acaba, vuelve al arma ilimitada.
    // Spends one unit of ammo; if it runs out, switches back to the unlimited weapon.
    public void ConsumeAmmo(Weapon weapon)
    {
        if (_ammo[weapon] > 0)
            _ammo[weapon]--;

        if (!HasAmmo(SelectedWeapon))
            SelectedIndex = 0;
    }

    // Agrega munición a un arma aleatoria con munición limitada y la devuelve.
    // Adds ammo to a random weapon with limited ammo and returns it.
    public Weapon AddRandomAmmo(Random random)
    {
        Weapon[] limited = Weapons.Where(w => !w.HasUnlimitedAmmo).ToArray();
        Weapon weapon = limited[random.Next(limited.Length)];
        _ammo[weapon] += CrateAmmoAmount;
        return weapon;
    }

    public void AddMedkit() => Medkits++;

    // Gasta un botiquín si hay alguno disponible.
    // Spends a medkit if one is available.
    public bool UseMedkit()
    {
        if (Medkits <= 0)
            return false;

        Medkits--;
        return true;
    }
}