using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleGuy
{
    public class HeavyWeightSynergyForLittleGuy : BraveBehaviour
    {
        public void Start()
        {
            ai = aiActor;

            if (ai == null)
                return;

            p = ai.CompanionOwner;
            origcontact = ai.CollisionDamage;
        }

        public void Update()
        {
            if (ai == null)
                return;

            if (p != null && p.HasActiveBonusSynergy(Plugin.heavyweightSynergy))
                ai.CollisionDamage = origcontact * 2f;

            else
                ai.CollisionDamage = origcontact;
        }

        public AIActor ai;
        public PlayerController p;
        public float origcontact;
    }
}
