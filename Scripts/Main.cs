using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Runtime;

namespace Dark.Scripts;

[NebulaPreprocess(PreprocessPhase.PostLoadAddons)]
public class Main
{
    public static void Load()
    {
        Harmony harmony = new("cn.moonscar.thedarkgalaxy");
        harmony.PatchAll(); // harmony 注册
        OptionsManager.Init();// 客户端选项注入
        
    }

    public static IEnumerator Preprocess(NebulaPreprocessor preprocessor)
    {
        yield return preprocessor.SetLoadingText("Loading TheDarkGalaxy...");
        Load();
    }


}
public static class OptionsManager
{
    const int OpsId = 91145;
    const ClientOption.ClientOptionType ShowTaskPanelType = (ClientOption.ClientOptionType)OpsId;
    const ClientOption.ClientOptionType ShowPlayerLeftDetailType = (ClientOption.ClientOptionType)OpsId + 1;
    public static readonly ClientOption ShowTaskPanelOpt;
    public static readonly ClientOption ShowPlayerLeftDetailOpt;
    static OptionsManager()
    {
        string[] BoolSwitch = { "options.switch.off", "options.switch.on" };
        ShowTaskPanelOpt = new(ShowTaskPanelType, "options.tdg.showtaskpanel", BoolSwitch, 1);
        ShowPlayerLeftDetailOpt = new(ShowPlayerLeftDetailType, "options.tdg.showplayerleftdetail", BoolSwitch,1);
        DarkLog.LogInfo("初始化完毕。");
    }
    public static void Init()
    {
        DarkLog.LogInfo("开始初始化OptionsManager。");
        // 主要功能其实是调用静态构造函数。？
        // 事实上外部访问变量就行了。
        // 滚木。
    }
}