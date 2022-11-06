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
                    else if (HasNoObstruction(RaidTower.towerScanDistance)) m_state = AIState.Fall;
                    else if (HasPathToNearbyTower()) m_state = AIState.PathUpTower;
                    else m_state = AIState.StartTower;
                    break;
                case AIState.Fall:
                    if (TargetIsInRange(dt)) m_state = AIState.TargetWithinRange;
                    else if (HasPathToTarget()) m_state = AIState.Path;
                    else m_state = AIState.NoPath;
                    break;
                case AIState.StartTower:
                    if (TowerExists() && HasPathUpTower()) m_state = AIState.PathUpTower;
                    else if (TowerExists()) m_state = AIState.AbandonTower;
                    break;
                case AIState.AbandonTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (HasPathUpTower(useCache: false)) m_state = AIState.PathUpTower;
                    else if (HasPathToTarget(requireFullPath: true)) m_state = AIState.Path;
                    break;
                case AIState.PathUpTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (IsOnTopOfTower()) m_state = AIState.OnTopOfTower;
                    else if (!HasPathUpTower(useCache: true)) m_state = AIState.AbandonTower;
                    break;
                case AIState.OnTopOfTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (!IsOnTopOfTower() && HasPathUpTower()) m_state = AIState.PathUpTower;
                    else if (tower.NeedsHeight()) m_state = AIState.BuildingTower;
                    else if (tower.NeedsRamp()) m_state = AIState.BuildingTowerRamp;
                    else if (tower.IsComplete()) m_state = AIState.ExitTower;
                    break;
                case AIState.BuildingTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (!IsOnTopOfTower() && HasPathUpTower()) m_state = AIState.PathUpTower;
                    else if (!IsOnTopOfTower()) m_state = AIState.PathUpTower;
                    else if (tower.IsComplete()) m_state = AIState.ExitTower;
                    else if (HasPathToTarget(requireFullPath: true)) m_state = AIState.Path;
                    break;
                case AIState.BuildingTowerRamp:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (tower.IsComplete()) m_state = AIState.ExitTower;
                    break;
                case AIState.ExitTower:
                    if (!TowerExists()) {
                        if (HasPathToTarget(requireFullPath: true)) m_state = AIState.Path;
                        else m_state = AIState.Fall;
                    } else if (!IsOnTopOfTower()) m_state = AIState.AbandonTower;
                    break;
                default:
                    throw new Exception("Deciding in unknown RaidAI state! " + m_state);
            }
        }

        public override void ActOutState(float dt) {
            switch (m_state) {
                case AIState.NoTarget:
                    StopMoving(); break;
                case AIState.HasTarget: case AIState.NoPath: break;
                case AIState.TargetWithinRange:
                    TargetWithinRange(dt); break;
                case AIState.Path:
                    tower = null;
                    FollowPath(); break;
                case AIState.Fall:
                    Fall(); break;
                case AIState.StartTower:
                    StartTower(dt); break;
                case AIState.AbandonTower:
                    break;
                case AIState.PathUpTower:
                    FollowPath(); break;
                case AIState.OnTopOfTower:
                    StopMoving(); break;
                case AIState.BuildingTower:
                    StopMoving();
                    tower.Build(dt); break;
                case AIState.BuildingTowerRamp:
                    tower.BuildRamp(); break;
                case AIState.ExitTower:
                    tower.UpdateRampWait(dt);
                    if (tower.IsComplete()) tower = null; break;
                default:
                    throw new Exception("Acting in unknown RaidAI state! " + m_state);
            }
        }

        protected float timeTilBuild = RaidTower.buildTime;

        public void StartTower(float dt) {
            StopMoving();
            LookAt(Target.GetCenter());
            timeTilBuild -= dt;
            if (timeTilBuild <= 0) {
                Quaternion rotation = transform.rotation;
                bool didHit = RaidUtils.RayCastStraight(transform.position, Target.GetCenter(), out RaycastHit hit, render);
                if (didHit) {
                    rotation = Quaternion.LookRotation(hit.normal);
                }
                
                tower = RaidTower.StartTower(transform, rotation);
                timeTilBuild = RaidTower.buildTime;
            }
        }
    }
}
