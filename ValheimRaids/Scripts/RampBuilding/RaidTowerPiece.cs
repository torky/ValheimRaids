using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimRaids.Scripts {
    public class RaidTowerPiece : MonoBehaviour {
        internal RaidTower Tower => RaidTower.raidTowers.GetValueSafe(m_towerId);
        private ZNetView m_nview;
        int m_towerId = 0;
        public Transform ramp1;
        public Transform point5;
        public Transform point6;
        public Transform ramp2;
        public Transform point7;
        public Transform point8;

        public RaidRamp rampBuilt1;
        public RaidRamp rampBuilt2;

        public void Awake() {
            m_nview = GetComponent<ZNetView>();
            ramp1 = transform.Find("floor/ramppoint1");
            point5 = transform.Find("floor/point5");
            point6 = transform.Find("floor/point6");
            ramp2 = transform.Find("floor/ramppoint2");
            point7 = transform.Find("floor/point7");
            point8 = transform.Find("floor/point8");

            if ((bool)m_nview && m_nview.IsValid()) m_towerId = m_nview.GetZDO().GetInt("towerId", 0);
            if (m_towerId != 0) {
                var tower = RaidTower.raidTowers.GetValueSafe(m_towerId) ?? new RaidTower(m_towerId);
                tower.pieces.Add(this);
            }

        }

        public void SetTower(int id) {
            if (!(m_nview == null) && m_nview.IsOwner() && m_towerId == 0) {
                m_towerId = id;
                m_nview.GetZDO().Set("towerId", id);
                var tower = RaidTower.raidTowers.GetValueSafe(m_towerId) ?? new RaidTower(m_towerId);
                tower.pieces.Add(this);
            }
        }
    }
}
