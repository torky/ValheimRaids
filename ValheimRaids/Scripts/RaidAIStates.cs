using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValheimRaids.Scripts {
    public static class AIState {
        public const string NoTarget = "NoTarget";
        public const string HasTarget = "HasTarget";
        public const string TargetWithinRange = "TargetWithinRange";
        public const string Path = "Path";
        public const string NoPath = "NoPath";
        public const string Fall = "Fall";
        public const string StartTower = "StartTower";
        public const string AbandonTower = "AbandonTower";
        public const string PathUpTower = "PathUpTower";
        public const string OnTopOfTower = "OnTopOfTower";
        public const string BuildingTower = "BuildingTower";
        public const string ExitTower = "ExitTower";
    }
}
