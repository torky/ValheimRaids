using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimRaids.Scripts.AI {
    public class TowerBuilder : RaidAI {
        public override void DetermineState(float dt) {
            switch (m_state) {
                case AIState.NoTarget:
                    if (HasTarget()) m_state = AIState.HasTarget;
                    break;
                case AIState.HasTarget:
                    if (TargetIsInRange(dt)) m_state = AIState.TargetWithinRange;
                    else if (HasPathToTarget()) m_state = AIState.Path;
                    else m_state = AIState.NoPath;
                    break;
                case AIState.TargetWithinRange:
                    if (!TargetIsInRange(dt)) m_state = AIState.HasTarget;
                    break;
                case AIState.Path:
                    if (TargetIsInRange(dt)) m_state = AIState.TargetWithinRange;
                    else if (!HasPathToTarget()) m_state = AIState.NoPath;
                    break;
                case AIState.NoPath:
                    if (TargetIsInRange(dt)) m_state = AIState.TargetWithinRange;
                    else if (ShouldFall()) m_state = AIState.Fall;
                    else if (HasPathToNearbyTower(isCompleteTower: false)) m_state = TowerState.PathUpTower;
                    else if (!NearByBlockers() && HasObstruction(RaidTower.towerScanDistance)) m_state = TowerState.StartTower;
                    else if (HasPathToTarget()) m_state = AIState.Path;
                    else m_state = AIState.Fall;
                    break;
                case AIState.Fall:
                    if (TargetIsInRange(dt)) m_state = AIState.TargetWithinRange;
                    else if (HasPathToTarget()) m_state = AIState.Path;
                    else if (HasObstruction(RaidTower.towerScanDistance)) m_state = AIState.NoPath;
                    else if (ZoneSystem.instance.GetSolidHeight(transform.position) <= 0) m_state = AIState.NoPath;
                    break;
                case TowerState.StartTower:
                    if (TowerExists() && HasPathUpTower()) m_state = TowerState.PathUpTower;
                    else if (TowerExists()) m_state = TowerState.AbandonTower;
                    else if (!HasObstruction(RaidTower.towerScanDistance)) m_state = AIState.Fall;
                    break;
                case TowerState.AbandonTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (HasPathUpTower(useCache: false)) m_state = TowerState.PathUpTower;
                    else if (HasPathToTarget(requireFullPath: true)) m_state = AIState.Path;
                    else if (WaitHasExpired(dt)) m_state = AIState.HasTarget;
                    break;
                case TowerState.PathUpTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (IsOnTopOfTower()) m_state = TowerState.OnTopOfTower;
                    else if (!HasPathUpTower(useCache: true)) m_state = TowerState.AbandonTower;
                    break;
                case TowerState.OnTopOfTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (!IsOnTopOfTower() && HasPathUpTower()) m_state = TowerState.PathUpTower;
                    else if (tower.NeedsHeight()) m_state = TowerState.BuildingTower;
                    else if (tower.NeedsRamp()) m_state = TowerState.BuildingTowerRamp;
                    else if (tower.IsComplete()) m_state = TowerState.ExitTower;
                    break;
                case TowerState.BuildingTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (!IsOnTopOfTower() && HasPathUpTower()) m_state = TowerState.PathUpTower;
                    else if (!IsOnTopOfTower()) m_state = TowerState.PathUpTower;
                    else if (tower.IsComplete()) m_state = TowerState.ExitTower;
                    else if (HasPathToTarget(requireFullPath: true)) m_state = AIState.Path;
                    break;
                case TowerState.BuildingTowerRamp:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (tower.IsComplete()) m_state = TowerState.ExitTower;
                    break;
                case TowerState.ExitTower:
                    if (!TowerExists()) {
                        if (HasPathToTarget(requireFullPath: true)) m_state = AIState.Path;
                        else m_state = AIState.Fall;
                    } else if (!IsOnTopOfTower()) m_state = TowerState.AbandonTower;
                    break;
                default:
                    throw new Exception("Deciding in unknown RaidAI state! " + m_state);
            }
        }

        public override void ActOutState(float dt) {
            switch (m_state) {
                case AIState.NoTarget: case AIState.NoPath:
                    StopMoving(); break;
                case AIState.HasTarget:
                    tower = null; break;
                case AIState.TargetWithinRange:
                    TargetWithinRange(dt); break;
                case AIState.Path:
                    tower = null;
                    FollowPath(); break;
                case AIState.Fall:
                    Fall(); break;
                case TowerState.StartTower:
                    StartTower(dt); break;
                case TowerState.AbandonTower:
                    StopMoving(); break;
                case TowerState.PathUpTower:
                    FollowPath(); break;
                case TowerState.OnTopOfTower:
                    StopMoving(); break;
                case TowerState.BuildingTower:
                    StopMoving();
                    tower.Build(dt); break;
                case TowerState.BuildingTowerRamp:
                    tower.BuildRamp(); break;
                case TowerState.ExitTower:
                    tower.UpdateRampWait(dt);
                    if (tower.IsComplete()) tower = null; break;
                default:
                    throw new Exception("Acting in unknown RaidAI state! " + m_state);
            }
        }

        protected float timeTilBuild = RaidTower.buildTime;
        protected float timeToWait = RaidTower.buildTime;

        public void StartTower(float dt) {
            StopMoving();
            LookAt(Target.GetCenter());
            timeTilBuild -= dt;
            if (timeTilBuild <= 0) {
                Quaternion rotation = transform.rotation;
                bool didHit = RaidUtils.RayCastStraight(transform.position, Target.GetCenter(), out RaycastHit hit, render, m_solidRayMask);
                if (didHit) {
                    rotation = Quaternion.LookRotation(hit.normal);
                    tower = RaidTower.StartTower(transform, rotation);
                }
                timeTilBuild = RaidTower.buildTime;
            }
        }

        private bool WaitHasExpired(float dt) {
            timeToWait -= dt;
            if (timeToWait <= 0) {
                timeToWait = RaidTower.buildTime;
                return true;
            }
            return false;
        }

        private bool NearByBlockers() {
            var collisions = Physics.SphereCastAll(transform.position, 4f, Vector3.zero, m_solidRayMask);
            foreach (var collision in collisions) {
                var parent = collision.transform.root;
                if (parent.name == RaidBuilding.FloorPiecePrefab.name + "(Clone)") return true;
            }
            return false;
        }
    }
}
