using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimRaids.Scripts
{
    public class DefensePoint : MonoBehaviour
    {
        public static DefensePoint instance = null;
        public StaticTarget m_target;

        public static void SetDefensePoint(GameObject defensePointGameObject)
        {
            if (instance != null)
            {
                var wearNTear = instance.gameObject.GetComponent<WearNTear>();
                wearNTear.Damage(new HitData
                {
                    m_damage = new HitData.DamageTypes
                    {
                        m_damage = 9999999999999,
                    }
                });
                Jotunn.Logger.LogDebug(wearNTear.m_health);
            }
            instance = defensePointGameObject.GetComponent<DefensePoint>();
            instance.m_target = defensePointGameObject.GetComponentInChildren<StaticTarget>();
            Jotunn.Logger.LogDebug(instance.gameObject.name);
        }

        public void Awake()
        {
            var piece = GetComponent<Piece>();
            if (piece.IsPlacedByPlayer())
            {
                SetDefensePoint(gameObject);
            }
        }
    }
}
