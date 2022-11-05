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
        public static bool RayCastStraight(Vector3 origin, Vector3 end, out RaycastHit raycastHit, LineRenderer render, float distance = Mathf.Infinity)
        {
            var direction = new Vector3(end.x - origin.x, 0, end.z - origin.z);
            var start = new Vector3(origin.x, origin.y + 0.5f, origin.z);

            bool hit = Physics.Raycast(start, direction, out RaycastHit rayHit, distance);
            render.SetPosition(0, start);
            render.SetPosition(1, rayHit.point);
            if (end == Vector3.zero)
            {
                Jotunn.Logger.LogInfo("ZERO");
            }

            raycastHit = rayHit;
            return hit;
        }
    }
}
