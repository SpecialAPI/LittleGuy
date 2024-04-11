using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleGuy
{
    public class LittleGalDoStuffBehavior : BehaviorBase
	{
		private float idleTimer;
		private bool hadPlayedAnimThisIdle;
        private bool wasPlayingIdle;

        public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter)
        {
            base.Init(gameObject, aiActor, aiShooter);
            m_aiAnimator.OverrideIdleAnimation = BraveUtility.RandomBool() ? "idle2" : null;
        }

        public override BehaviorResult Update()
		{
			var playingIdle = IsPlayingIdle();

            if (m_aiAnimator.IsPlaying("pet_end"))
            {
                m_aiActor.ClearPath();
            }

            if (m_aiAnimator.IsPlaying("removehelmet"))
            {
                m_aiActor.ClearPath();
            }
            else if (playingIdle && !m_aiActor.CompanionOwner.IsInCombat)
            {
                if (!hadPlayedAnimThisIdle)
                {
                    idleTimer += m_deltaTime;
                    if (idleTimer > 8f)
                    {
                        m_aiAnimator.PlayUntilFinished("removehelmet");
                        hadPlayedAnimThisIdle = true;
                    }
                }
            }
            else
            {
                idleTimer = 0f;
                hadPlayedAnimThisIdle = false;
            }

            if (playingIdle)
            {
                wasPlayingIdle = true;
            }
            else
            {
                if (wasPlayingIdle)
                {
                    m_aiAnimator.OverrideIdleAnimation = BraveUtility.RandomBool() ? "idle2" : null;
                }
                wasPlayingIdle = false;
            }

			return base.Update();
		}

		public bool IsPlayingIdle()
		{
			return m_aiAnimator.IsPlaying("idle_right") || m_aiAnimator.IsPlaying("idle_left") || m_aiAnimator.IsPlaying("idle2_right") || m_aiAnimator.IsPlaying("idle2_left");
        }
	}
}
