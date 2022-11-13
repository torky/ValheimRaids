using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace ValheimRaids.Scripts.AI {
    public class RampBuilder : RaidAI {
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
                    else if (!HasObstruction(RaidTower.towerScanDistance)) m_state = RampState.Building;
                    else if (HasPathToNearbyTower()) m_state = TowerState.PathUpTower;
                    else if (HasPathToTarget()) m_state = AIState.Path;
                    break;
                case AIState.Fall:
                    if (TargetIsInRange(dt)) m_state = AIState.TargetWithinRange;
                    else if (HasPathToTarget()) m_state = AIState.Path;
                    else if (ZoneSystem.instance.GetSolidHeight(transform.position) <= 0) m_state = AIState.NoPath;
                    break;
                case TowerState.AbandonTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    break;
                case TowerState.PathUpTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (IsOnTopOfTower()) m_state = TowerState.OnTopOfTower;
                    else if (!HasPathUpTower()) m_state = TowerState.AbandonTower;
                    break;
                case TowerState.OnTopOfTower:
                    if (!TowerExists()) m_state = AIState.HasTarget;
                    else if (tower.IsComplete()) m_state = TowerState.ExitTower;
                    break;
                case TowerState.ExitTower:
                    if (!TowerExists()) {
                        if (HasPathToTarget(requireFullPath: true)) m_state = AIState.Path;
                        else m_state = AIState.Fall;
                    } else if (!IsOnTopOfTower()) m_state = TowerState.AbandonTower;
                    break;
                case RampState.Building:
                    if (IsDoneBuilding()) m_state = RampState.DoneBuilding;
                    else if (ShouldBuildPlank()) m_state = RampState.BuildingPlank;
                    else if (ShouldBuildStairs()) m_state = RampState.BuildingStairs;
                    else m_state = RampState.BuildingRamp;
                    break;
                case RampState.BuildingPlank:
                case RampState.BuildingRamp:
                case RampState.BuildingStairs:
                    if (DoneBuilding) m_state = RampState.DoneBuilding;
                    break;
                case RampState.DoneBuilding:
                    if (!DoneBuilding) m_state = AIState.HasTarget;
                    break;
                default:
                    throw new Exception("Deciding in unknown RaidAI state! " + m_state);
            }
        }

        public override void ActOutState(float dt) {
            switch (m_state) {
                case AIState.NoTarget: case AIState.NoPath: 
                    StopMoving(); break;
                case AIState.HasTarget: break;
                case AIState.TargetWithinRange:
                    TargetWithinRange(dt); break;
                case AIState.Path:
                    tower = null;
                    FollowPath(); break;
                case AIState.Fall:
                    Fall(); break;
                case TowerState.AbandonTower:
                    StopMoving();
                    tower = null;  break;
                case TowerState.PathUpTower:
                    FollowPath(); break;
                case TowerState.OnTopOfTower:
                    StopMoving(); break;
                case TowerState.ExitTower:
                    tower = null; break;
                case RampState.Building: break;
                case RampState.BuildingPlank:
                    BuildPlank(dt); break;
                case RampState.BuildingRamp: 
                    BuildRamp(dt); break;
                case RampState.BuildingStairs:
                    BuildStairs(dt); break;
                case RampState.DoneBuilding:
                    WaitAfterBuilding(dt); break;
                default:
                    throw new Exception("Acting in unknown RaidAI state! " + m_state);
            }
        }

        public const float buildTime = 5f;
        protected float timeTilBuild = buildTime;
        public readonly static HashSet<string> Ramps = new HashSet<string>();
        private bool DoneBuilding = false;

        protected void BuildStairs(float dt) {
            StopMoving();
            LookAt(Target.GetCenter());
            if (WaitToBuild(dt)) {
                Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, m_solidRayMask);
                Jotunn.Logger.LogInfo(hit.transform.root.gameObject.name);
                Jotunn.Logger.LogInfo(hit.transform.gameObject.name);

                var root = hit.transform.root;
                Vector3 position;
                Quaternion rotation;
                if (IsOnPlank(hit)) {
                    position = root.position - root.right * 2f - root.up * 1f;
                    rotation = root.rotation * Quaternion.Euler(0, -90, 0);
                } else if (IsOnStair(hit)) {
                    position = root.position + root.forward * 2f - root.up * 1f;
                    rotation = root.rotation;
                } else {
                    Jotunn.Logger.LogWarning("Ramp Builder knocked off building platform");
                    DoneBuilding = true;
                    return;
                }

                // May we need to do some syncing here?
                if (root.gameObject.TryGetComponent(out RaidBuildingPiece piece) && piece.attachedPiece == null) {
                    var stair = RaidBuilding.PlaceStairPiece(position, rotation);
                    piece.attachedPiece = stair.gameObject;
                } else {
                    throw new Exception("Not building on raid building piece!");
                }
                DoneBuilding = true;
            }
        }

        protected void BuildPlank(float dt) {
            StopMoving();
            LookAt(Target.GetCenter());
            if (WaitToBuild(dt)) {
                Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, m_solidRayMask);
                Jotunn.Logger.LogInfo(hit.transform.root.gameObject.name);
                Jotunn.Logger.LogInfo(hit.transform.gameObject.name);

                var root = hit.transform.root;
                var direction = Utils.DirectionXZ(Target.GetCenter() - transform.position);
                var position = transform.position + direction.normalized + Vector3.down * 0.05f;
                var rotation = transform.rotation * Quaternion.Euler(0, 90, 0);
                if (IsOnPlank(hit)) {
                    position = root.position - root.right * 2f;
                    rotation = root.rotation;
                }else if (IsOnStair(hit)) {
                    position = root.position + root.forward * 2f;
                    rotation = root.rotation * Quaternion.Euler(0, 90, 0);
                }else if (IsOnRamp(hit)) {
                    position = root.position - root.right * 2f;
                    rotation = root.rotation;
                }

                var plank = RaidBuilding.PlacePlankPiece(position, rotation);
                // May we need to do some syncing here?
                if (root.gameObject.TryGetComponent(out RaidBuildingPiece piece) && piece.attachedPiece == null) {
                    piece.attachedPiece = plank.gameObject;
                }
                DoneBuilding = true;
            }
        }

        protected void BuildRamp(float dt) {
            StopMoving();
            LookAt(Target.GetCenter());
            if (WaitToBuild(dt)) {
                Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, m_solidRayMask);
                Jotunn.Logger.LogInfo(hit.transform.root.gameObject.name);
                Jotunn.Logger.LogInfo(hit.transform.gameObject.name);

                var root = hit.transform.root;
                Vector3 position;
                Quaternion rotation;
                if (IsOnPlank(hit)) {
                    position = root.position - root.right * 2f;
                    rotation = root.rotation;
                } else if (IsOnStair(hit)) {
                    position = root.position + root.forward * 2f;
                    rotation = root.rotation * Quaternion.Euler(0, 90, 0);
                } else {
                    Jotunn.Logger.LogWarning("Ramp Builder knocked off building platform");
                    DoneBuilding = true;
                    return;
                }

                // May we need to do some syncing here?
                if (root.gameObject.TryGetComponent(out RaidBuildingPiece piece) && piece.attachedPiece == null) {
                    var ramp = RaidBuilding.PlaceRampPiece(position, rotation);
                    piece.attachedPiece = ramp.gameObject;
                } else {
                    throw new Exception("Not building on raid building piece!");
                }
                
                DoneBuilding = true;
            }
        }

        protected bool WaitToBuild(float dt) {
            timeTilBuild -= dt;
            if (timeTilBuild <= 0) {
                timeTilBuild = buildTime;
                return true;
            }
            return false;
        }

        protected void WaitAfterBuilding(float dt) {
            if (WaitToBuild(dt)) DoneBuilding = false;
        }

        private bool IsDoneBuilding() {
            Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, m_solidRayMask);
            if (hit.transform.root.gameObject.TryGetComponent(out RaidBuildingPiece piece))  return piece.attachedPiece != null;
            return false;
        }

        protected bool IsOnPlank(RaycastHit hit) {
            return hit.transform.root.gameObject.name == RaidBuilding.RaidPlankPrefab.gameObject.name + "(Clone)";
        }

        protected bool IsOnStair(RaycastHit hit) {
            return hit.transform.root.gameObject.name == RaidBuilding.RaidStairPrefab.gameObject.name + "(Clone)";
        }

        protected bool IsOnRamp(RaycastHit hit) {
            return hit.transform.root.gameObject.name == RaidBuilding.RaidRampPrefab.gameObject.name + "(Clone)";
        }

        protected bool ShouldBuildStairs() {
            Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, m_solidRayMask);
            bool isOnSomeShit = IsOnPlank(hit) || IsOnStair(hit);
            return isOnSomeShit && !ShouldBuildPlank() && !ShouldBuildRamp(hit.transform.root.Find("searchpoint").position);
        }

        protected bool ShouldBuildRamp(Vector3 searchPoint) {
            return TriangularPrismCast(searchPoint, Target.GetCenter(), forwardDistance: 2f, downwardDistance: 1f);
        }
        protected bool ShouldBuildPlank() {
            bool targetIsAbove = Target.GetCenter().y > transform.position.y;
            bool hasDistanceObstruction = HasObstruction(Utils.DistanceXZ(Target.GetCenter(), transform.position));
            Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, m_solidRayMask);
            bool isOnSomeShit = IsOnPlank(hit) || IsOnStair(hit);
            Vector3 searchPoint = transform.position;
            if (isOnSomeShit) searchPoint = hit.transform.root.Find("searchpoint").position;
            bool noShortObstructionBelow = !ShouldBuildRamp(searchPoint);
            bool hasFarObstructionBelow = TriangularPrismCast(searchPoint, Target.GetCenter(), forwardDistance: 4f, downwardDistance: 1f);
            return targetIsAbove || !isOnSomeShit || hasDistanceObstruction || (noShortObstructionBelow && hasFarObstructionBelow);
        }

        protected bool TriangularPrismCast(Vector3 point, Vector3 target, float forwardDistance, float downwardDistance) {
            Vector3 direction = Utils.DirectionXZ(target - point);
            Vector3 leftDir = Quaternion.Euler(0, 90f, 0) * direction.normalized * 1f;
            Vector3 left = point + leftDir;
            Vector3 right = point - leftDir;

            var hitMiddle = TriangularRayCast(point, direction, forwardDistance, downwardDistance);
            var hitLeft = TriangularRayCast(left, direction, forwardDistance, downwardDistance);
            var hitRight = TriangularRayCast(right, direction, forwardDistance, downwardDistance);

            return hitMiddle || hitLeft || hitRight;
        }

        protected bool TriangularRayCast(Vector3 point, Vector3 direction, float forwardDistance, float downwardDistance) {
            if (Physics.Raycast(point, direction, forwardDistance)) return true;

            Vector3 pointFar = point + direction.normalized * forwardDistance;
            if (Physics.Raycast(pointFar, Vector3.down, downwardDistance)) return true;

            Vector3 pointLow = point + direction.normalized * forwardDistance + Vector3.down;
            return Physics.Linecast(point, pointLow);
        }

    }
}
