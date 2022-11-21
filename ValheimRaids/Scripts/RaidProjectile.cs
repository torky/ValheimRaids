using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimRaids.Scripts {
    public class RaidProjectile : MonoBehaviour {
        public static HitData hitData = new HitData {
            m_damage = new HitData.DamageTypes {
                m_damage = 200,
                m_blunt = 0,
                m_slash = 0,
                m_pierce = 0,
                m_chop = 0,
                m_pickaxe = 0,
                m_fire = 0,
                m_frost = 0,
                m_lightning = 0,
                m_poison = 0,
                m_spirit = 0,
            },
            
            m_blockable = true,
            m_pushForce = 1f,
            m_ranged = true,
        };

        private ZNetView m_nview;
        private Rigidbody m_body;
        private readonly static float radius = 1f;
        public string m_state = TrebuchetState.Unarmed;
        public GameObject pieces;

        public void Awake() {
            m_nview = GetComponent<ZNetView>();
            m_body = GetComponent<Rigidbody>();
        }

        public void OnCollisionEnter(Collision collision) {
            if (!m_nview.IsValid() || !m_nview.IsOwner() || m_state == TrebuchetState.Unarmed) return;
            ContactPoint contactPoint = collision.contacts[0];
            Vector3 dir = contactPoint.point - transform.position;
            foreach (var collider in Physics.OverlapSphere(contactPoint.point, radius)) {
                var root = collider.transform.root;
                if (root.gameObject.TryGetComponent(out IDestructible destructible)) {
                    var data = hitData.Clone();
                    data.m_point = contactPoint.point;
                    data.m_dir = dir.normalized;
                    destructible.Damage(data);
                }
            }

            Instantiate(pieces, transform.position, transform.rotation);
            pieces.transform.Find("1").GetComponent<Rigidbody>().velocity = m_body.velocity;
            pieces.transform.Find("2").GetComponent<Rigidbody>().velocity = m_body.velocity;
            pieces.transform.Find("3").GetComponent<Rigidbody>().velocity = m_body.velocity;
            pieces.transform.Find("4").GetComponent<Rigidbody>().velocity = m_body.velocity;
            pieces.transform.Find("5").GetComponent<Rigidbody>().velocity = m_body.velocity;
            pieces.transform.Find("6").GetComponent<Rigidbody>().velocity = m_body.velocity;
            pieces.transform.Find("7").GetComponent<Rigidbody>().velocity = m_body.velocity;
            ZNetScene.instance.Destroy(gameObject);
        }
    }
}
