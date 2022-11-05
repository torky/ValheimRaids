using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    else if (HasNoObstruction()) m_state = AIState.Fall;
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
                    break;
                case AIState.PathUpTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (IsOnTopOfTower()) m_state = AIState.OnTopOfTower;
                    else if (!HasPathUpTower()) m_state = AIState.AbandonTower;
                    break;
                case AIState.OnTopOfTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (!IsOnTopOfTower()) m_state = AIState.HasTarget;
                    else if (TowerNeedsHeight()) m_state = AIState.BuildingTower;
                    else if (TowerIsComplete()) m_state = AIState.ExitTower;
                    break;
                case AIState.BuildingTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (!IsOnTopOfTower() && HasPathUpTower()) m_state = AIState.PathUpTower;
                    else if (TowerIsComplete()) m_state = AIState.ExitTower;
                    break;
                case AIState.ExitTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (TowerIsComplete() && TowerRampHasFallen(dt)) m_state = AIState.HasTarget;
                    break;
                default:
                    throw new Exception("Deciding in unknown RaidAI state! " + m_state);
            }
        }

        public override void ActOutState(float dt) {
            switch (m_state) {
                case AIState.NoTarget:
                    StopMoving(); break;
                case AIState.HasTarget: case AIState.NoPath: case AIState.ExitTower: break;
                case AIState.TargetWithinRange:
                    TargetWithinRange(dt); break;
                case AIState.Path:
                    FollowPath(); break;
                case AIState.Fall:
                    Fall(); break;
                case AIState.StartTower:
                    StartTower(dt); break;
                case AIState.AbandonTower:
                    tower = null; break;
                case AIState.PathUpTower:
                    FollowPath(); break;
                case AIState.OnTopOfTower:
                    StopMoving(); break;
                case AIState.BuildingTower:
                    StopMoving();
                    tower.Build(dt); break;
                default:
                    throw new Exception("Acting in unknown RaidAI state! " + m_state);
            }
        }
    }
}
