using Jotunn.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValheimRaids.Scripts.AI;

namespace ValheimRaids.Scripts.Commands {
    public class TrebuchetVelocity : ConsoleCommand {
        public override string Name => "trebmag";

        public override string Help => "Sets magnitude of a trebuchet launch. No arg for reset, 1 arg as float for magnitude override";

        public override void Run(string[] args) {
            if (args.Length == 0) {
                Trebuchet.magnitudeOverride = null;
                return;
            }
            Trebuchet.magnitudeOverride = float.Parse(args[0]);
        }
    }
}
