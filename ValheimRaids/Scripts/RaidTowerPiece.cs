using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimRaids.Scripts
{
    public class RaidTowerPiece : MonoBehaviour
    {
        internal RaidTower Tower => RaidTower.raidTowers.GetValueSafe(m_towerId);
        private ZNetView m_nview;
        int m_towerId = 0;

        public void Awake()
        {
            m_nview = GetComponent<ZNetView>();

            if ((bool)m_nview && m_nview.IsValid())
            {
                m_towerId = m_nview.GetZDO().GetInt("towerId", 0);
            }
            if (m_towerId != 0)
            {
                var tower = RaidTower.raidTowers.GetValueSafe(m_towerId) ?? new RaidTower(m_towerId);
                tower.pieces.Add(this);
            }

        }

        public void SetTower(int id)
        {
            if (!(m_nview == null) && m_nview.IsOwner() && m_towerId == 0)
            {
                m_towerId = id;
                m_nview.GetZDO().Set("towerId", id);
                var tower = RaidTower.raidTowers.GetValueSafe(m_towerId) ?? new RaidTower(m_towerId);
                tower.pieces.Add(this);
            }
        }
    }
}
