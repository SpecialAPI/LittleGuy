using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleGuy
{
    public class StayFartherAwayOutsideCombat : BehaviorBase
    {
        public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
        {
            base.Init(gameObject, aiActor, aiShooter);

            if (aiActor == null || aiActor.behaviorSpeculator == null || aiActor.behaviorSpeculator.MovementBehaviors == null)
                return;

            follow = aiActor.behaviorSpeculator.MovementBehaviors.OfType<CompanionFollowPlayerBehavior>().FirstOrDefault();
        }

        public override BehaviorResult Update()
        {
            if(follow != null)
                follow.IdealRadius = m_aiActor.CompanionOwner.IsInCombat ? CombatDistance : OutOfCombatDistance;

            return base.Update();
        }

        private CompanionFollowPlayerBehavior follow;

        public float CombatDistance;
        public float OutOfCombatDistance;
    }
}
