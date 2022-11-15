using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimRaids.Scripts.AI {
    public class LoadTrebuchet : MonoBehaviour {
        public Trebuchet trebuchet;
        public void Awake() {
            trebuchet = transform.root.gameObject.GetComponent<Trebuchet>();
        }

        public void OnTriggerEnter(Collider collison) {
            trebuchet.OnChildTriggerEnter(collison);
        }
    }
}
