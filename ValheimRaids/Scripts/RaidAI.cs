using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ItemDrop;

namespace ValheimRaids.Scripts {
    public class RaidAI : BaseAI {
        protected float m_updateWeaponTimer;
        protected float m_lastAttackTime;
        protected LineRenderer render;
        protected readonly float m_towerSearchRange = 7f;
        public RaidTower tower;

        protected StaticTarget Target => RaidPoint.instance?.m_target;

        public override void Awake() {
            base.Awake();
            render = gameObject.AddComponent<LineRenderer>();
            render.enabled = true;
            render.startWidth = 0.1f;
            render.endWidth = 0.1f;
        }

        public void Start() {
            if ((bool)m_nview && m_nview.IsValid() && m_nview.IsOwner()) {
                Humanoid humanoid = m_character as Humanoid;
                if ((bool)humanoid) {
                    humanoid.EquipBestWeapon(null, null, null, null);
                }
            }
        }

        public override void UpdateAI(float dt) {
            base.UpdateAI(dt);
            if (!m_nview.IsOwner()) return;
            if (!HasTarget()) m_state = AIState.NoTarget;
            else DetermineState(dt);
            if (m_state != previousState) Jotunn.Logger.LogInfo(m_state);
            previousState = m_state;
            ActOutState(dt);
        }

        protected string m_state = AIState.NoTarget;
        protected string previousState;

        public virtual void DetermineState(float dt) { }

        public virtual void ActOutState(float dt) { }

        protected bool TowerExists() {
            if (tower != null && tower.TopPiece() == null) tower = null;
            return tower != null;
        }

        protected bool IsOnTopOfTower() {
            var distance = Vector3.Distance(tower.TowerTop(), transform.position);
            return distance <= 0.5f;
        }
        protected bool HasPathToNearbyTower() {
            foreach (var tower in RaidTower.raidTowers.Values) {
                if (tower == null || tower.TopPiece() == null) continue;
                Vector2 aiPos = new Vector2(transform.position.x, transform.position.z);
                Vector2 towerPos = new Vector2(tower.TopPiece().transform.position.x, tower.TopPiece().transform.position.z);
                var withinRange = Vector2.Distance(aiPos, towerPos) <= m_towerSearchRange;
                if (!withinRange) continue;
                var path = new List<Vector3>();
                var hasPath = Pathfinding.instance.GetPath(transform.position, tower.TowerTop(), path, m_pathAgentType, requireFullPath: true);
                if (hasPath) {
                    m_path = path;
                    this.tower = tower;
                    return true;
                }
            }
            return false;
        }

        protected bool HasPathUpTower(bool useCache = false) {
            bool hasPath = SearchForPath(tower.TowerTop(), requireFullPath: true, useCache);
            return hasPath;
        }

        public void TargetWithinRange(float dt) {
            var itemData = SelectBestAttack(m_character as Humanoid, dt);
            LookAt(Target.GetCenter());
            if (itemData != null && Time.time - m_lastAttackTime >= itemData.m_shared.m_aiAttackInterval && CanSeeTarget(Target)) {
                bool canAttack = itemData != null && Time.time - itemData.m_lastAttackTime > itemData.m_shared.m_aiAttackInterval && !IsTakingOff();
                bool lookingAtTarget = IsLookingAt(Target.GetCenter(), itemData.m_shared.m_aiAttackMaxAngle);
                if (canAttack && lookingAtTarget) DoAttack(null);
                else StopMoving();
            }
        }

        public void Fall() {
            Vector3 normalized = (RaidPoint.instance.transform.position - transform.position).normalized;
            MoveTowards(normalized, run: true);
        }

        public void FollowPath() {
            if (m_path.Count == 0) return;
            Vector3 vector = m_path[0];
            while (Utils.DistanceXZ(vector, transform.position) < 0.5f) {
                m_path.RemoveAt(0);
                if (m_path.Count == 0) {
                    StopMoving();
                    return;
                }
                vector = m_path[0];
            }
            Vector3 normalized2 = (vector - transform.position).normalized;
            MoveTowards(normalized2, run: true);
        }

        public bool FindPath(Vector3 target, bool requireFullPath = false, bool useCache = true) {
            float time = Time.time;
            float num = time - m_lastFindPathTime;

            if (Vector3.Distance(target, m_lastFindPathTarget) < 0.5f && num < 2f && useCache)  return m_lastFindPathResult;

            m_lastFindPathTarget = target;
            m_lastFindPathTime = time;
            m_lastFindPathResult = Pathfinding.instance.GetPath(transform.position, target, m_path, m_pathAgentType, requireFullPath);
            return m_lastFindPathResult;
        }

        protected ItemData SelectBestAttack(Humanoid humanoid, float dt) {
            m_updateWeaponTimer -= dt;
            if (m_updateWeaponTimer <= 0f && !m_character.InAttack()) {
                m_updateWeaponTimer = 1f;
                HaveFriendsInRange(m_viewRange, out var hurtFriend, out var friend);
                humanoid.EquipBestWeapon(null, Target, hurtFriend, friend);
            }

            return humanoid.GetCurrentWeapon();
        }

        protected bool DoAttack(Character target) {
            ItemData currentWeapon = (m_character as Humanoid).GetCurrentWeapon();
            if (currentWeapon != null) {
                if (!CanUseAttack(m_character, currentWeapon)) return false;
                bool num = m_character.StartAttack(target, charge: false);
                if (num)  m_lastAttackTime = Time.time;
                return num;
            }
            return false;
        }

        protected bool HasNoObstruction(float scanDistance) {
            bool hit = !RaidUtils.RayCastStraight(origin: transform.position, end: RaidPoint.instance.transform.position, out RaycastHit raycast, render, scanDistance);
            Jotunn.Logger.LogInfo(raycast.transform?.gameObject?.name);
            return hit;
        }

        protected bool HasPathToTarget(bool requireFullPath = false) {
            return SearchForPath(Target.FindClosestPoint(transform.position), requireFullPath);
        }

        protected bool SearchForPath(Vector3 destination, bool requireFullPath = false, bool useCache = true) {
            bool foundPath = FindPath(destination, requireFullPath, useCache);
            if (!foundPath || m_path.Count <= 0) return false;
            var endDistanceFromPosition = Vector3.Distance(m_path.Last(), transform.position);
            var endDistanceFromDestination = Vector3.Distance(m_path.Last(), destination);
            return m_path.Count > 0 && endDistanceFromPosition >= 0.3f && (!requireFullPath || endDistanceFromDestination <= 0.3f);
        }

        protected bool TargetIsInRange(float dt) {
            Vector3 closestPoint = Target.FindClosestPoint(transform.position);
            var itemData = SelectBestAttack(m_character as Humanoid, dt);
            render.startColor = Color.green;
            render.endColor = Color.green;
            RaidUtils.RayCastStraight(transform.position, Target.transform.position, out RaycastHit raycast, render);

            if (raycast.transform == null) return false;
            var hasRaidPoint = raycast.transform.gameObject?.transform?.parent?.TryGetComponent<RaidPoint>(out _) ?? false;
            return Vector3.Distance(closestPoint, transform.position) < itemData.m_shared.m_aiAttackRange && hasRaidPoint || Vector3.Distance(closestPoint, transform.position) < 0.5f;
        }

        public bool HasTarget() {
            return RaidPoint.instance != null;
        }
    }
}
