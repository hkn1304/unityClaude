using UnityEngine;

public static class WeaponFactory
{
    public static Weapon Equip(FighterController fighter, WeaponType type)
    {
        if (fighter.equippedWeapon != null)
        {
            Object.Destroy(fighter.equippedWeapon.gameObject);
            fighter.equippedWeapon = null;
        }

        var go = new GameObject(type + "Weapon");
        Weapon w;
        switch (type)
        {
            case WeaponType.Bow:       w = go.AddComponent<BowWeapon>();       break;
            case WeaponType.Dagger:    w = go.AddComponent<DaggerWeapon>();    break;
            case WeaponType.Staff:     w = go.AddComponent<StaffWeapon>();     break;
            case WeaponType.Katana:    w = go.AddComponent<KatanaWeapon>();    break;
            case WeaponType.Hammer:    w = go.AddComponent<HammerWeapon>();    break;
            case WeaponType.Shuriken:  w = go.AddComponent<ShurikenWeapon>();  break;
            case WeaponType.Boomerang: w = go.AddComponent<BoomerangWeapon>(); break;
            case WeaponType.Gun:       w = go.AddComponent<GunWeapon>();       break;
            case WeaponType.Sniper:    w = go.AddComponent<SniperWeapon>();    break;
            case WeaponType.PortalGun:  w = go.AddComponent<PortalGunWeapon>();  break;
            case WeaponType.FightGlove: w = go.AddComponent<FightGloveWeapon>(); break;
            default:                    w = go.AddComponent<SwordWeapon>();      break;
        }
        w.Equip(fighter);
        return w;
    }
}
