using MonoMod.RuntimeDetour.Platforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace ValheimRaids.Scripts.AI {
    public class Trebuchet : MonoBehaviour {
        Transform arm;
        Vector3 armPos;
        Quaternion armRot;
        Rigidbody armBody;

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
        Rigidbody ammoBody;

        private ZNetView m_nview;
        private LineRenderer render;
        private string m_state;
        private static readonly float min = 1.5f;
        private static readonly float max = 2.0f;
        public static float? timeOverride;
        public static float? magnitudeOverride;
        public static GameObject LaunchSound;

        public static HashSet<string> AvailableAmmo = new HashSet<string>();
        private readonly float releaseReset = 5f;
        private float timer = 0;

        // Time   X   Y    Z Distance
        // 0.00 340, 42, 455 0
        // 1.50 340, 42, 473 18
        // 1.52 340, 42, 479 24
        // 1.54 340, 42, 485 30
        // 1.56 340, 42, 488 33
        // 1.58 340, 42, 491 36
        // 1.60 340, 42, 495 40
        // 1.65 340, 42, 499 44
        // 1.70 340, 42, 503 48
        // 1.80 340, 42, 507 52
        // 2.00 340, 42, 510 55
        private static readonly Dictionary<float, float> RangeToTime = new Dictionary<float, float>{
            { 0F, 0.00F },
            { 18F, 1.50F },
            { 24F, 1.52F },
            { 30F, 1.54F },
            { 33F, 1.56F },
            { 36F, 1.58F },
            { 40F, 1.60F },
            { 44F, 1.65F },
            { 48F, 1.70F },
            { 52F, 1.80F },
            { 55F, 2.00F },
        };
        private static readonly List<float> ranges = new List<float>(RangeToTime.Keys);
        private static readonly List<float> times = new List<float>(RangeToTime.Values);

        public void Awake() {
            m_nview = GetComponent<ZNetView>();
            render = gameObject.AddComponent<LineRenderer>();
            render.enabled = true;
            render.startWidth = 0.1f;
            render.endWidth = 0.1f;

            weight = transform.Find("weight");
            weightBody = weight.GetComponent<Rigidbody>();
            weightBody.isKinematic = true;
            
            arm = transform.Find("arm");
            armBody = arm.GetComponent<Rigidbody>();
            armBody.isKinematic = true;

            testAmmo = transform.Find("testAmmo");
            testAmmoBody = testAmmo.GetComponent<Rigidbody>();

            SetUnarmedPositionsAndRotations();

            loadPoint = transform.Find("loadpoint");
            loadPoint.gameObject.AddComponent<LoadTrebuchet>();

            m_state = TrebuchetState.Unarmed;
        }

        public void OnChildTriggerEnter(Collider collision) {
            var root = collision.transform.root;
            Jotunn.Logger.LogInfo(root.name);
            if (!AvailableAmmo.Contains(root.name)) return;
            if (m_state != TrebuchetState.Unarmed) return;
            ammo = root;
            ammo.position = testAmmo.position;
            ammoBody = root.GetComponent<Rigidbody>();

            if (timeOverride != null) {
                Jotunn.Logger.LogInfo("Time Override");
                timer = (float)timeOverride;
            } else if (RaidPoint.instance != null) {
                float distance = Utils.DistanceXZ(testAmmoPos, RaidPoint.instance.transform.position);
                var range = ClosestRange(distance);
                Jotunn.Logger.LogInfo("Aiming for range: " + range);
                timer = GetVariableTime(range);
            } else {
                Jotunn.Logger.LogInfo("Randomly shooting");
                timer = UnityEngine.Random.Range(min, max);
            }

            Jotunn.Logger.LogInfo("Time: " + timer);
            m_state = TrebuchetState.Aiming;
        }

        private void SetUnarmedPositionsAndRotations() {
            weightPos = weight.position;
            weightRot = weight.rotation;
            armPos = arm.position;
            armRot = arm.rotation;
            testAmmoPos = testAmmo.position;
            testAmmoRot = testAmmo.rotation;
        }

        public float GetVariableTime(float range) {
            var time = RangeToTime[range];
            var i = times.IndexOf(time);
            var max = time == times.Last() ? time : times[i + 1];
            var min = time == times.First() ? time : times[i - 1];
            return UnityEngine.Random.Range(min, max);
        }

        private float ClosestRange(float distance) {
            float shortest = distance;
            float closestRange = 0F;
            foreach (float range in ranges) {
                var closeness = Math.Abs(distance - range);
                if (closeness < shortest) {
                    shortest = closeness;
                    closestRange = range;
                }
            }
            return closestRange;
        }

        private void Reset() {
            arm.position = armPos;
            arm.rotation = armRot;
            armBody.isKinematic = true;
            weight.position = weightPos;
            weight.rotation = weightRot;
            weightBody.isKinematic = true;
            testAmmo.position = testAmmoPos;
            testAmmo.rotation = testAmmoRot;
            testAmmoBody.velocity = Vector3.zero;
            testAmmoBody.isKinematic = true;
            timer = 0;
            m_state = TrebuchetState.Unarmed;
        }

        private void LockAmmoToTestAmmo() {
            ammoBody.isKinematic = true;
            ammo.position = testAmmo.position;
            if (ammo.gameObject.TryGetComponent(out RaidAI ai)) {
                ai.m_state = TrebuchetState.Firing;
            }
        }

        public void FixedUpdate() {
            if (!m_nview.IsValid()) return;
            if (timer > 0 && m_state != TrebuchetState.Aiming) timer -= Time.fixedDeltaTime;
            switch (m_state) {
                case TrebuchetState.Aiming:
                    if (RaidPoint.instance == null) {
                        m_state = TrebuchetState.Firing;
                        Instantiate(LaunchSound);
                        return;
                    }
                    var direction = RaidPoint.instance.transform.position - transform.position;
                    var directionXZ = Utils.DirectionXZ(direction);

                    var lookRotation = Quaternion.LookRotation(directionXZ) * Quaternion.Euler(0, -90f, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 1f);

                    SetUnarmedPositionsAndRotations();
                    LockAmmoToTestAmmo();
                    if (transform.rotation == lookRotation) {
                        var degree = UnityEngine.Random.Range(-95f, -85f);
                        var finalRotation = Quaternion.LookRotation(directionXZ) * Quaternion.Euler(0, degree, 0);
                        transform.rotation = finalRotation;

                        m_state = TrebuchetState.Firing;
                        Instantiate(LaunchSound, transform.position, transform.rotation);
                    }
                    break;
                case TrebuchetState.Firing:
                    if (ammoBody == null) {
                        timer = releaseReset;
                        m_state = TrebuchetState.Fired;
                        return;
                    }
                    armBody.isKinematic = false;
                    weightBody.isKinematic = false;
                    testAmmoBody.isKinematic = false;
                    LockAmmoToTestAmmo();

                    if (timer <= 0) {
                        ammoBody.velocity = testAmmoBody.velocity;
                        var magnitude = magnitudeOverride ?? ammoBody.velocity.magnitude;
                        ammoBody.velocity = ammoBody.velocity.normalized * magnitude;
                        Jotunn.Logger.LogInfo("Magnitude: " + magnitude);
                        ammoBody.isKinematic = false;
                        ammoBody.collisionDetectionMode = CollisionDetectionMode.Continuous;

                        if (ammo.TryGetComponent(out RaidProjectile projectile)) {
                            projectile.m_state = TrebuchetState.Fired;
                        }

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
                default:
                    throw new Exception("Unexpected Trebuchet State!");
            }
        }
    }
}
