using Jotunn.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValheimRaids.Scripts.AI;

namespace ValheimRaids.Scripts.Commands {
    public class TrebuchetTime : ConsoleCommand {
        public override string Name => "trebTime";

        public override string Help => "Sets time it takes before a trebuchet launches. No arg for reset, 1 arg as float for time override";

        public override void Run(string[] args) {
            if (args.Length == 0) {
                Trebuchet.timeOverride = null;
                return;
            }
            Trebuchet.timeOverride = float.Parse(args[0]);
        }
    }
}
