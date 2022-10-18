using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using static ItemDrop;
using static MonoMod.InlineRT.MonoModRule;

namespace ValheimRaids.Scripts
{
    public class RaidAI : BaseAI
    {
        private float m_updateWeaponTimer;
        private float m_lastAttackTime;

        public override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            if ((bool)m_nview && m_nview.IsValid() && m_nview.IsOwner())
            {
                Humanoid humanoid = m_character as Humanoid;
                if ((bool)humanoid)
                {
                    humanoid.EquipBestWeapon(null, null, null, null);
                }
            }
        }

        public bool HasTarget()
        {
            return DefensePoint.instance != null;
        }

        public override void UpdateAI(float dt)
        {
            base.UpdateAI(dt);
            if (!m_nview.IsOwner())
            {
                return;
            }

            if (HasTarget())
            {
                var staticTarget = DefensePoint.instance.m_target;
                var itemData = SelectBestAttack(m_character as Humanoid, dt);
                Vector3 vector = staticTarget.FindClosestPoint(base.transform.position);
                if (Vector3.Distance(vector, base.transform.position) < itemData.m_shared.m_aiAttackRange && CanSeeTarget(staticTarget))
                {
                    LookAt(staticTarget.GetCenter());
                    bool canAttack = itemData != null && Time.time - itemData.m_lastAttackTime > itemData.m_shared.m_aiAttackInterval && !IsTakingOff();
                    if (IsLookingAt(staticTarget.GetCenter(), itemData.m_shared.m_aiAttackMaxAngle) && canAttack)
                    {
                        DoAttack(null);
                    }
                    else
                    {
                        StopMoving();
                    }
                }
                else
                {
                    MoveTo(dt, vector, 0f, true);
                }
            }
        }

        private ItemData SelectBestAttack(Humanoid humanoid, float dt)
        {
            var staticTarget = DefensePoint.instance.m_target;
            m_updateWeaponTimer -= dt;
            if (m_updateWeaponTimer <= 0f && !m_character.InAttack())
            {
                m_updateWeaponTimer = 1f;
                HaveFriendsInRange(m_viewRange, out var hurtFriend, out var friend);
                humanoid.EquipBestWeapon(null, staticTarget, hurtFriend, friend);
            }

            return humanoid.GetCurrentWeapon();
        }

        private bool DoAttack(Character target)
        {
            ItemData currentWeapon = (m_character as Humanoid).GetCurrentWeapon();
            if (currentWeapon != null)
            {
                if (!CanUseAttack(m_character, currentWeapon))
                {
                    return false;
                }
                bool num = m_character.StartAttack(target, charge: false);
                if (num)
                {
                    m_lastAttackTime = Time.time;
                }
                return num;
            }
            return false;
        }
    }
}
