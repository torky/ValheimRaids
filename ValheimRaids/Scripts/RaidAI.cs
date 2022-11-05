using System;
using System.Collections.Generic;
using UnityEngine;
using static ItemDrop;

namespace ValheimRaids.Scripts
{
    public class RaidAI : BaseAI
    {
        private float m_updateWeaponTimer;
        private float m_lastAttackTime;
        private float timeTilBuild = RaidTower.buildTime;
        private readonly float m_towerSearchRange = 5f;
        private LineRenderer render;
        public RaidTower tower;

        private StaticTarget Target => RaidPoint.instance?.m_target;

        public override void Awake()
        {
            base.Awake();
            render = gameObject.AddComponent<LineRenderer>();
            render.enabled = true;
            render.startWidth = 0.1f;
            render.endWidth = 0.1f;
        }

        public void Start()
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

        public override void UpdateAI(float dt)
        {
            base.UpdateAI(dt);
            if (!m_nview.IsOwner())
            {
                return;
            }
            DetermineState(dt);
            ActOutState(dt);
        }

        public enum State
        {
            NoTarget,
            HasTarget,
            TargetWithinRange,
            Path,
            NoPath,
            Fall,
            StartTower,
            AbandonTower,
            PathUpTower,
            OnTopOfTower,
            BuildingTower,
            BuildingRamp,
            ExitTower,
        }

        private State m_state;
        private State previousState;

        public void DetermineState(float dt)
        {
            if (!HasTarget())
            {
                m_state = State.NoTarget; 
                return;
            }
            switch (m_state)
            {
                case State.NoTarget:
                    if (HasTarget()) m_state = State.HasTarget;
                    break;
                case State.HasTarget:
                    if (TargetIsInRange(dt)) m_state = State.TargetWithinRange;
                    else if (HasPathToTarget()) m_state = State.Path;
                    else m_state = State.NoPath;
                    break;
                case State.TargetWithinRange:
                    if (!TargetIsInRange(dt)) m_state = State.HasTarget;
                    break;
                case State.Path:
                    if (TargetIsInRange(dt)) m_state = State.TargetWithinRange;
                    else if (!HasPathToTarget()) m_state = State.NoPath;
                    break;
                case State.NoPath:
                    if (TargetIsInRange(dt)) m_state = State.TargetWithinRange;
                    else if (HasNoObstruction()) m_state = State.Fall;
                    else if (HasPathToNearbyTower()) m_state = State.PathUpTower;
                    else m_state = State.StartTower;
                    break;
                case State.Fall:
                    if (TargetIsInRange(dt)) m_state = State.TargetWithinRange;
                    else if (HasPathToTarget()) m_state = State.Path;
                    else m_state = State.NoPath;
                    break;
                case State.StartTower:
                    if (TowerExists() && HasPathUpTower()) m_state = State.PathUpTower;
                    else if (TowerExists()) m_state = State.AbandonTower;
                    break;
                case State.AbandonTower:
                    if (!TowerExists()) m_state = State.HasTarget;
                    break;
                case State.PathUpTower:
                    if (!TowerExists()) m_state = State.HasTarget;
                    else if (IsOnTopOfTower()) m_state = State.OnTopOfTower;
                    else if (!HasPathUpTower()) m_state = State.AbandonTower;
                    break;
                case State.OnTopOfTower:
                    if (!TowerExists()) m_state = State.HasTarget;
                    else if (!IsOnTopOfTower()) m_state = State.HasTarget;
                    else if (TowerNeedsHeight()) m_state = State.BuildingTower;
                    else if (TowerNeedsRamp()) m_state = State.BuildingRamp;
                    else if (TowerIsComplete()) m_state = State.ExitTower;
                    break;
                case State.BuildingTower:
                    if (!TowerExists()) m_state = State.HasTarget;
                    else if (!IsOnTopOfTower() && HasPathUpTower()) m_state = State.PathUpTower;
                    else if (!TowerNeedsHeight()) m_state = State.BuildingRamp;
                    break;
                case State.BuildingRamp:
                    if (!TowerExists()) m_state = State.HasTarget;
                    else if (TowerIsComplete()) m_state = State.ExitTower;
                    break;
                case State.ExitTower:
                    if (!TowerExists()) m_state = State.HasTarget;
                    else if (TowerIsComplete() && TowerRampHasFallen(dt)) m_state = State.HasTarget;
                    break;
                default:
                    throw new Exception("In unknown RaidAI state!");
            }

            if (m_state != previousState) Jotunn.Logger.LogInfo(m_state);
            previousState = m_state;
        }

        public void ActOutState(float dt)
        {
            switch (m_state)
            {
                case State.NoTarget: case State.HasTarget: case State.NoPath: case State.ExitTower: break;
                case State.TargetWithinRange:
                    TargetWithinRange(dt); break;
                case State.Path:
                    FollowPath(); break;
                case State.Fall:
                    Fall(); break;
                case State.StartTower:
                    StartTower(dt); break;
                case State.AbandonTower:
                    tower = null; break;
                case State.PathUpTower:
                    FollowPath(); break;
                case State.OnTopOfTower:
                    StopMoving(); break;
                case State.BuildingTower:
                    StopMoving();
                    tower.Build(dt); break;
                case State.BuildingRamp:
                    StopMoving();
                    tower.BuildRamp(); break;
                default:
                    throw new Exception("In unknown RaidAI state!");
            }
        }

        public void TargetWithinRange(float dt)
        {
            var itemData = SelectBestAttack(m_character as Humanoid, dt);
            LookAt(Target.GetCenter());
            if (itemData != null && Time.time - m_lastAttackTime >= itemData.m_shared.m_aiAttackInterval && CanSeeTarget(Target))
            {
                bool canAttack = itemData != null && Time.time - itemData.m_lastAttackTime > itemData.m_shared.m_aiAttackInterval && !IsTakingOff();
                bool lookingAtTarget = IsLookingAt(Target.GetCenter(), itemData.m_shared.m_aiAttackMaxAngle);
                if (canAttack && lookingAtTarget)
                {
                    DoAttack(null);
                }
                else
                {
                    StopMoving();
                }
            }
        }

        public void Fall()
        {
            Vector3 normalized = (RaidPoint.instance.transform.position - transform.position).normalized;
            MoveTowards(normalized, run: true);
        }

        public void StartTower(float dt)
        {
            StopMoving();
            LookAt(Target.GetCenter());
            timeTilBuild -= dt;
            if (timeTilBuild <= 0)
            {
                tower = RaidTower.StartTower(transform);
                timeTilBuild = RaidTower.buildTime;
            }
        }

        public void FollowPath()
        {
            if (m_path.Count == 0) return;
            Vector3 vector = m_path[0];
            while (Utils.DistanceXZ(vector, transform.position) < 0.5f)
            {
                m_path.RemoveAt(0);
                if (m_path.Count == 0)
                {
                    StopMoving();
                    return;
                }
                vector = m_path[0];
            }
            Vector3 normalized2 = (vector - transform.position).normalized;
            MoveTowards(normalized2, run: true);
        }

        public bool FindPath(Vector3 target, bool requireFullPath = false)
        {
            float time = Time.time;
            float num = time - m_lastFindPathTime;

            if (Vector3.Distance(target, m_lastFindPathTarget) < 0.5f && num < 5f)
            {
                return m_lastFindPathResult;
            }

            m_lastFindPathTarget = target;
            m_lastFindPathTime = time;
            m_lastFindPathResult = Pathfinding.instance.GetPath(transform.position, target, m_path, m_pathAgentType, requireFullPath);
            return m_lastFindPathResult;
        }

        private ItemData SelectBestAttack(Humanoid humanoid, float dt)
        {
            m_updateWeaponTimer -= dt;
            if (m_updateWeaponTimer <= 0f && !m_character.InAttack())
            {
                m_updateWeaponTimer = 1f;
                HaveFriendsInRange(m_viewRange, out var hurtFriend, out var friend);
                humanoid.EquipBestWeapon(null, Target, hurtFriend, friend);
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

        private bool TowerExists()
        {
            if (tower != null && tower.TopPiece() == null)
            {
                tower = null;
            }
            return tower != null;
        }

        private bool TowerNeedsHeight()
        {
            return tower.NeedsHeight();
        }

        private bool TowerNeedsRamp()
        {
            return tower.NeedsRamp();
        }

        private bool TowerIsComplete()
        {
            return tower.IsComplete();
        }

        private bool TowerRampHasFallen(float dt)
        {
            return tower.RampHasFallen(dt);
        }

        private bool IsOnTopOfTower()
        {
            var distance = Vector3.Distance(tower.TowerTop(), transform.position);
            return distance <= 0.5f;
        }

        private bool HasNoObstruction()
        {
            bool hit = !RaidUtils.RayCastStraight(origin: transform.position, end: RaidPoint.instance.transform.position, out RaycastHit raycast, render, RaidTower.towerScanDistance);
            Jotunn.Logger.LogInfo(raycast.transform?.gameObject?.name);
            return hit;
        }

        private bool HasPathToNearbyTower()
        {
            foreach (var tower in RaidTower.raidTowers.Values)
            {
                if (tower == null || tower.TopPiece() == null) continue;
                Vector2 aiPos = new Vector2(transform.position.x, transform.position.z);
                Vector2 towerPos = new Vector2(tower.TopPiece().transform.position.x, tower.TopPiece().transform.position.z);
                var withinRange = Vector2.Distance(aiPos, towerPos) <= m_towerSearchRange;
                if (!withinRange) continue;
                var path = new List<Vector3>();
                var hasFullPath = Pathfinding.instance.GetPath(transform.position, tower.TowerTop(), m_path, m_pathAgentType, requireFullPath: true);
                if (hasFullPath)
                {
                    m_path = path;
                    this.tower = tower;
                    return true;
                }
            }
            return false;
        }

        private bool HasPathToTarget()
        {
            return SearchForPath(Target.FindClosestPoint(transform.position));
        }

        private bool HasPathUpTower()
        {
            bool hasPath = SearchForPath(tower.TowerTop(), requireFullPath: true);
            return hasPath;
        }
        private bool SearchForPath(Vector3 destination, bool requireFullPath = false)
        {
            bool foundPath = FindPath(destination, requireFullPath);
            if (!foundPath || m_path.Count <= 0) return false;
            var distance = Vector3.Distance(m_path[m_path.Count-1], transform.position);
            return m_path.Count > 0 && distance >= 0.3;
        }

        private bool TargetIsInRange(float dt)
        {
            Vector3 closestPoint = Target.FindClosestPoint(transform.position);
            var itemData = SelectBestAttack(m_character as Humanoid, dt);
            render.startColor = Color.green;
            render.endColor = Color.green;
            RaidUtils.RayCastStraight(transform.position, Target.transform.position, out RaycastHit raycast, render);

            if (raycast.transform == null) return false;
            var hasRaidPoint = raycast.transform.gameObject?.transform?.parent?.TryGetComponent<RaidPoint>(out _) ?? false;
            return Vector3.Distance(closestPoint, transform.position) < itemData.m_shared.m_aiAttackRange && hasRaidPoint || Vector3.Distance(closestPoint, transform.position) < 0.5f;
        }

        public bool HasTarget()
        {
            return RaidPoint.instance != null;
        }
    }
}
