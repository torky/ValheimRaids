using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

namespace ValheimRaids.Scripts {
    public class RaidTower {
        public static Dictionary<int, RaidTower> raidTowers = new Dictionary<int, RaidTower>();
        public static float towerScanDistance = 2f;
        public static int maxTowerId = 0;
        public static float buildTime = 5f;
        public static float towerFinishTime = 3f;

        public static RaidTower StartTower(Transform transform) {
            Quaternion rotate90 = Quaternion.Euler(0, 90, 0);
            Vector3 position = transform.position + transform.rotation * Vector3.back * 1.9f + transform.rotation * Vector3.left * 2;
            RaidTowerPiece raidTowerPiece = BuildPiece(position, rotation: transform.rotation * rotate90);
            raidTowerPiece.SetTower(maxTowerId + 1);
            return raidTowerPiece.Tower;
        }

        private static RaidTowerPiece BuildPiece(Vector3 position, Quaternion rotation) {
            var piece = RaidBuilding.PlaceFloorPiece(position, rotation);
            RaidRamp[] ramps = RaidBuilding.PlaceRampPieces(piece);
            piece.rampBuilt1 = ramps[0];
            piece.rampBuilt2 = ramps[1];
            return piece;
        }

        public List<RaidTowerPiece> pieces = new List<RaidTowerPiece>();
        public int towerId;
        public float timeTilBuild = buildTime;
        public float rampWaitTime = 0f;

        public RaidTower(int towerId) {
            this.towerId = towerId;
            raidTowers.Add(towerId, this);
            if (maxTowerId < towerId) maxTowerId = towerId;
        }

        internal bool IsComplete() {
            return !NeedsHeight();
        }

        internal bool NeedsHeight() {
            bool hit1 = Physics.Raycast(Top().ramp1.position, Top().ramp1.right, towerScanDistance);
            bool hit5 = Physics.Raycast(Top().point5.position, Top().point5.right, towerScanDistance);
            bool hit6 = Physics.Raycast(Top().point6.position, Top().point6.right, towerScanDistance);
            bool hit2 = Physics.Raycast(Top().ramp2.position, -Top().ramp2.right, towerScanDistance);
            bool hit7 = Physics.Raycast(Top().point7.position, Top().point7.right, towerScanDistance);
            bool hit8 = Physics.Raycast(Top().point8.position, Top().point8.right, towerScanDistance);
            // Jotunn.Logger.LogInfo("hit1:" + hit1 + ", hit5" + hit5 + ", hit6" + hit6 + ", hit2" + hit2 + ", hit7" + hit7 + ", hit8" + hit8);
            return hit1 || hit5 || hit6 || hit2 || hit7 || hit8;
        }

        internal bool NeedsRamp() {
            return Top().rampBuilt1 == null && Top().rampBuilt2 == null;
        }

        internal bool RampHasFallen(float dt) {
            rampWaitTime += dt;
            return rampWaitTime >= towerFinishTime;
        }

        internal RaidTowerPiece Top() {
            return pieces.Last();
        }

        internal GameObject TopPiece() {
            var topPiece = pieces.Last();
            if (topPiece != null) return topPiece.gameObject;
            return null;
        }

        internal Vector3 TowerTop() {
            return TopPiece().transform.Find("TopOfTower").position;
        }

        internal void Build(float dt) {
            timeTilBuild -= dt;
            if (timeTilBuild <= 0) {
                Transform t = TopPiece().transform;
                Vector3 v = t.position;
                // piece right 1
                Vector3 right = v + t.forward.normalized * 2.1f;
                // piece 180
                Quaternion rotation = t.rotation * Quaternion.Euler(0, 180, 0);
                // piece up 1
                Vector3 position = right + Vector3.up * 2;
                var piece = BuildPiece(position, rotation);
                piece.SetTower(towerId);
                pieces.Add(piece);
                timeTilBuild = buildTime;
            }
        }
    }
}