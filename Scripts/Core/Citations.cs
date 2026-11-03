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
    // Citation 的 logo 允许为 null。原来传 g("") 会拿空路径去 GetResource，必然 NRE 再被吞，
    // 每次加载固定刷两条 "路径是null" 的错误日志，纯噪声 → 直接传 null。
    public static Citation TheDarkGalaxy { get; private set; } = new(
        "TheDarkGalaxy",
        null,
        new ColorTextComponent(
            Cor.Golden,new RawTextComponent("TheDarkGalaxy")
            ),
        "https://tdg.moonscar.cn/"
        );
    public static Citation Werewolf { get; private set; } = new(
        "Werewolf", null,new ColorTextComponent(Color.Red,new RawTextComponent(Language.Translate("Citation.WereWolf.name"))), "https://langrensha.163.com/"
        );
}