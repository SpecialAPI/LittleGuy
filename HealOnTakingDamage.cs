using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleGuy
{
    public class HealOnTakingDamage : BraveBehaviour
    {
        public void Start()
        {
            if(healthHaver != null)
            {
                healthHaver.OnDamaged += HandleDamaged;
            }
        }

		public void HandleDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
		{
			healthHaver.FullHeal();
		}
	}
}
