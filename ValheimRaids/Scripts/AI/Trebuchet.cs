using MonoMod.RuntimeDetour.Platforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimRaids.Scripts.AI {
    public class Trebuchet : MonoBehaviour {
        Transform arm;
        Vector3 armPos;
        Quaternion armRot;

        Transform weight;
        Vector3 weightPos;
        Quaternion weightRot;
        Rigidbody weightBody;

        Transform testAmmo;
        Vector3 testAmmoPos;
        Quaternion testAmmoRot;
        Rigidbody testAmmoBody;

        Transform loadPoint;

        Transform ammo;
        HingeJoint joint;
        Rigidbody ammoBody;
        private float originalMass;

        Vector3 position;
        Vector3 anchor;
        Vector3 connectedAnchor;
        Vector3 axis;
        Rigidbody connectedBody;

        private ZNetView m_nview;
        private LineRenderer render;
        private string m_state;
        private static readonly float min = 2f;
        private static readonly float max = min + 0.1f;
        private readonly float releaseReset = 5f;
        private float timer = 0;

        public void Awake() {
            m_nview = GetComponent<ZNetView>();
            render = gameObject.AddComponent<LineRenderer>();
            render.enabled = true;
            render.startWidth = 0.1f;
            render.endWidth = 0.1f;

            weight = transform.Find("weight");
            weightPos = weight.position;
            weightRot = weight.rotation;

            weightBody = weight.GetComponent<Rigidbody>();
            weightBody.isKinematic = true;
            
            arm = transform.Find("arm");
            armPos = arm.position;
            armRot = arm.rotation;

            testAmmo = transform.Find("testAmmo");
            testAmmoPos = testAmmo.position;
            testAmmoRot = testAmmo.rotation;
            testAmmoBody = testAmmo.GetComponent<Rigidbody>();

            loadPoint = transform.Find("loadpoint");
            loadPoint.gameObject.AddComponent<LoadTrebuchet>();

            //var testAmmo = transform.Find("Test Ammo");
            //joint = testAmmo.GetComponent<HingeJoint>();
            //anchor = joint.anchor;
            //axis = joint.axis;
            //connectedBody = joint.connectedBody;
            //connectedAnchor = joint.connectedAnchor;
            //position = testAmmo.position;

            //Jotunn.Logger.LogInfo("start");
            //Jotunn.Logger.LogInfo(anchor);
            //Jotunn.Logger.LogInfo(axis);
            //Jotunn.Logger.LogInfo(connectedBody);
            //Jotunn.Logger.LogInfo(joint.connectedAnchor);

            m_state = TrebuchetState.Unarmed;
        }

        public void OnChildTriggerEnter(Collider collision) {
            var root = collision.transform.root;
            Jotunn.Logger.LogInfo(root.name);
            if (root.name != "RaidGreydwarf(Clone)") return;
            ammo = root;
            //ammo.parent = transform;
            ammo.position = testAmmo.position;
            ammoBody = root.GetComponent<Rigidbody>();
            //originalMass = ammoBody.mass;
            //ammoBody.mass = 1f;
            //joint = root.gameObject.AddComponent<HingeJoint>();
            //joint.anchor = anchor;
            //joint.axis = axis;
            //joint.connectedBody = connectedBody;
            //joint.connectedAnchor = connectedAnchor;
            //joint.autoConfigureConnectedAnchor = true;

            //Jotunn.Logger.LogInfo("Trigger");
            //Jotunn.Logger.LogInfo(joint.anchor);
            //Jotunn.Logger.LogInfo(joint.axis);
            //Jotunn.Logger.LogInfo(joint.connectedBody);
            //Jotunn.Logger.LogInfo(joint.connectedAnchor);

            ammoBody.isKinematic = true;
            weightBody.isKinematic = false;
            timer = UnityEngine.Random.Range(min, max);
            m_state = TrebuchetState.Firing;
        }

        private void Reset() {
            arm.position = armPos;
            arm.rotation = armRot;
            weight.position = weightPos;
            weight.rotation = weightRot;
            weightBody.isKinematic = true;
            testAmmo.position = testAmmoPos;
            testAmmo.rotation = testAmmoRot;
            timer = UnityEngine.Random.Range(min, max);
            m_state = TrebuchetState.Unarmed;
        }

        public void FixedUpdate() {
            if (!m_nview.IsValid()) return;
            if (timer > 0) timer -= Time.fixedDeltaTime;
            switch (m_state) {
                case TrebuchetState.Armed:
                    break;
                case TrebuchetState.Firing:
                    if (ammoBody == null) {
                        m_state = TrebuchetState.Fired;
                        timer = releaseReset;
                        return;
                    }
                    //render.SetPosition(0, ammoBody.transform.position);
                    //render.SetPosition(1, joint.anchor);
                    ammo.position = testAmmo.position + Vector3.up * 2f;
                    if (timer <= 0) {
                        //ammo.parent = null;
                        //ammoBody.mass = originalMass;
                        //Destroy(joint);
                        ammoBody.velocity = testAmmoBody.velocity;
                        ammoBody.isKinematic = false;
                        timer = releaseReset;
                        m_state = TrebuchetState.Fired;
                    }
                    break;
                case TrebuchetState.Fired:
                    if (timer <= 0) {
                        Reset();
                        m_state = TrebuchetState.Unarmed;
                    }
                    break;
                case TrebuchetState.Unarmed:
                    break;
            }
        }
    }
}
