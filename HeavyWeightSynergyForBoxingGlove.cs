using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LittleGuy
{
    public class HeavyWeightSynergyForBoxingGlove : GunBehaviour
    {
        public override void PostProcessProjectile(Projectile projectile)
        {
            if (PlayerOwner == null || !PlayerOwner.HasActiveBonusSynergy(Plugin.heavyweightSynergy))
                return;

            projectile.baseData.damage *= 1.5f;
        }
    }
}
