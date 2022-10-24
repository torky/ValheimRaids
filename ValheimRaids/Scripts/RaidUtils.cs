using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimRaids.Scripts
{
    internal class RaidUtils
    {
        public static bool RayCastStraight(Vector3 origin, Vector3 end, out RaycastHit raycastHit, float distance = Mathf.Infinity)
        {
            var direction = end - origin;
            direction.y = 0;
            origin.y += .5f;

            bool hit = Physics.Raycast(origin, direction, out RaycastHit rayHit, distance);

            raycastHit = rayHit;
            return hit;
        }
    }
}
