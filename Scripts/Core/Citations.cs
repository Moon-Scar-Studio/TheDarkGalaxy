using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epic.OnlineServices.P2P;
using Nebula;
using Nebula.Utilities;
using Virial;
using Virial.Assignable;
using Color = Virial.Color;

namespace Dark.Scripts.Core;

public static class Citations
{
    public static Citation TheDarkGalaxy_hvtXsvc { get; private set; } = new(
        "TheDarkGalaxy",
        DarkUtils.AsImage("Citations/TDG.png",125),
        new ColorTextComponent(
            Cor.Golden,new RawTextComponent("TheDarkGalaxy")
            ),
        "https://tdg.moonscar.cn/"
        );

    public static Citation Werewolf { get; private set; } = new(
        "Werewolf", null,new ColorTextComponent(Color.Red,new RawTextComponent("狼人杀")),"https://langrensha.163.com/"
        );

    public static Citation TheDarkGalaxy_GoodEndGambler { get; private set; } = new(
        "GoodEndGambler",DarkUtils.AsImage("Citations/TDG_GoodEndGambler.png",125),new ColorTextComponent(Cor.Cyan,new RawTextComponent("GoodEndGambler")),"https://tdg.moonscar.cn/"
        );
}