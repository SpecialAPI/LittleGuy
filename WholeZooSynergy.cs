using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LittleGuy
{
    public class WholeZooSynergy : GunBehaviour
    {
        public override void PostProcessVolley(ProjectileVolleyData volley)
        {
            if(PlayerOwner != null && PlayerOwner.HasActiveBonusSynergy(Plugin.wholeZooSynergy))
            {
                int count = volley.projectiles.Count;
                for (int i = 0; i < count; i++)
                {
                    var module = volley.projectiles[i];
                    var mod = ProjectileModule.CreateClone(module, false, module.CloneSourceIndex >= 0 ? module.CloneSourceIndex : i);
                    mod.ignoredForReloadPurposes = true;
                    mod.ammoCost = 0;
                    volley.projectiles.Add(mod);
                }
            }
        }
    }
}
