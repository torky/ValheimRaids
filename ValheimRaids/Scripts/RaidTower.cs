using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimRaids.Scripts
{
    public class RaidTower
    {
        public static Dictionary<int, RaidTower> raidTowers = new Dictionary<int, RaidTower>();
        public static float towerScanDistance = 4f;
        public static int maxTowerId = 0;
        public static float buildTime = 5f;

        public Vector3? ramp = null;
        public List<RaidTowerPiece> pieces = new List<RaidTowerPiece>();
        public int towerId;
        public float timeTilBuild = buildTime;

        public RaidTower(int towerId)
        {
            this.towerId = towerId;
            raidTowers.Add(towerId, this);
            if (maxTowerId < towerId) maxTowerId = towerId;
        }

        internal bool IsComplete()
        {
            return !NeedsHeight() && !NeedsRamp();
        }

        internal bool NeedsHeight()
        {
            return RaidUtils.RayCastStraight(TowerTop(), RaidPoint.instance.transform.position, out _, towerScanDistance);
        }

        internal bool NeedsRamp()
        {
            return ramp == null;
        }

        internal GameObject TopPiece()
        {
            var topPiece = pieces.Last();
            if (topPiece != null) return topPiece.gameObject;
            return null;
        }

        internal Vector3 TowerTop()
        {
            return TopPiece().transform.Find("TopOfTower").position;
        }

        internal void Build(float dt)
        {
            timeTilBuild -= dt;
            if (timeTilBuild <= 0)
            {
                Transform t = TopPiece().transform;
                Vector3 v = t.position;
                // piece right 1
                Vector3 right = v + t.forward.normalized * 2.1f;
                // piece 180
                Quaternion rotation = t.rotation * Quaternion.Euler(0, 180, 0);
                // piece up 1
                Vector3 position = right + Vector3.up * 2;
                var piece = RaidBuilding.PlaceFloorPiece(position, rotation);
                piece.SetTower(towerId);
                pieces.Add(piece);
                timeTilBuild = buildTime;
            }
        }

        internal void BuildRamp()
        {
            Vector3 direction = RaidPoint.instance.m_target.transform.position - TowerTop();
            direction.y = 0;
            ramp = 4 * direction.normalized + TowerTop();
        }
    }
}