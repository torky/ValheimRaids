using System;
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
                Vector3 closestPoint = staticTarget.FindClosestPoint(base.transform.position);
                bool withinAttackRange = Vector3.Distance(closestPoint, transform.position) < itemData.m_shared.m_aiAttackRange && CanSeeTarget(staticTarget);
                if (withinAttackRange && itemData != null && Time.time - m_lastAttackTime >= itemData.m_shared.m_aiAttackInterval)
                {
                    LookAt(staticTarget.GetCenter());
                    bool canAttack = itemData != null && Time.time - itemData.m_lastAttackTime > itemData.m_shared.m_aiAttackInterval && !IsTakingOff();
                    bool lookingAtTarget = IsLookingAt(staticTarget.GetCenter(), itemData.m_shared.m_aiAttackMaxAngle);
                    if (withinAttackRange && canAttack && lookingAtTarget)
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
                    MoveTo(dt, closestPoint, 0f, true);
                }
            }
        }

        public new bool MoveTo(float dt, Vector3 point, float dist, bool run)
        {
            if (!FindPath(point))
            {
                StopMoving();
                return true;
            }

            if (m_path.Count == 0)
            {
                RetryFindPath(point);
                return true;
            }
            float num = 0.5f;
            Vector3 vector = m_path[0];
            while (Utils.DistanceXZ(vector, base.transform.position) < num)
            {
                m_path.RemoveAt(0);
                if (m_path.Count == 0)
                {
                    StopMoving();
                    return true;
                }
                vector = m_path[0];
            }
            Vector3 normalized2 = (vector - base.transform.position).normalized;
            MoveTowards(normalized2, run);
            return false;
        }

        public new bool FindPath(Vector3 target)
        {
            float time = Time.time;
            float num = time - m_lastFindPathTime;
            if (num < 1f)
            {
                return m_lastFindPathResult;
            }

            if (Vector3.Distance(target, m_lastFindPathTarget) < 1f && num < 5f)
            {
                return m_lastFindPathResult;
            }

            m_lastFindPathTarget = target;
            m_lastFindPathTime = time;
            m_lastFindPathResult = Pathfinding.instance.GetPath(base.transform.position, target, m_path, m_pathAgentType);
            return m_lastFindPathResult;
        }

        public bool RetryFindPath(Vector3 target)
        {
            float time = Time.time;
            float num = time - m_lastFindPathTime;
            if (num < 1f)
            {
                return m_lastFindPathResult;
            }
            m_lastFindPathTime = time;
            m_lastFindPathResult = Pathfinding.instance.GetPath(base.transform.position, target, m_path, m_pathAgentType);
            return m_lastFindPathResult;
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
