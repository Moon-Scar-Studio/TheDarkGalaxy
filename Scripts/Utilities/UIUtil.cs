using Nebula.Roles.Complex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial.Helpers;

namespace Dark.Scripts.Utilities;

public static class UIUtil
{
    // 本方法来源于： Plan17 (https://github.com/Plan17DevelopmentTeam/Plan17Resource_Dev)
    static public bool OpenRoleSelectWindowUsingTabsP17(IEnumerable<DefinedRole>? roles, (string? tab, Predicate<DefinedRole>? predicate)[] tabs, bool impRolesArrangeAtFirst, string underText, Action<DefinedRole> onSelected, ref MetaScreen __result)
    {
        var window = MetaScreen.GenerateWindow(new(7.6f, 4.2f), AmongUsLLImpl.HudManagerInstance.transform, new Virial.Compat.Vector3(0, 0, -50f), true, false);

        MetaWidgetOld widget = new();

        MetaWidgetOld inner = new();
        if (roles == null)
        {
            HashSet<DefinedRole> roleSet = [];
            foreach (var r in Nebula.Roles.Roles.AllRoles) foreach (var abilityRole in r.GetGuessableAbilityRoles()) roleSet.Add(abilityRole);
            foreach (var type in AssignmentType.AllTypes)
            {
                if (!type.CanGuessAsAbility) continue;
                foreach (var r in Nebula.Roles.Roles.AllRoles)
                {
                    if (type.Predicate.Invoke(r.AssignmentStatus, r) && r.GetCustomAllocationParameters(type)?.RoleCountSum > 0) roleSet.Add(r);
                }
            }
            roles = roleSet;
        }

        int CategoryToInt(RoleCategory roleCategory) => roleCategory switch
        {
            RoleCategory.ImpostorRole => impRolesArrangeAtFirst ? 0 : 1,
            RoleCategory.CrewmateRole => impRolesArrangeAtFirst ? 1 : 0,
            _ => 2
        };

        bool isFirst = true;
        foreach (var tab in tabs)
        {
            var ary = roles.Where(r => tab.predicate?.Invoke(r) ?? true).ToArray();
            ary.Sort((r1, r2) =>
            {
                if (r1.Category == r2.Category) return r1.InternalName.CompareTo(r2.InternalName);
                return CategoryToInt(r1.Category).CompareTo(CategoryToInt(r2.Category));
            });

            if (isFirst) isFirst = false;
            else inner.Append(new MetaWidgetOld.VerticalMargin(0.1f));

            if (tab.tab != null) inner.Append(new MetaWidgetOld.Text(MeetingRoleSelectWindow.TabAttribute) { MyText = new RawTextComponent(tab.tab), Alignment = IMetaWidgetOld.AlignmentOption.Center });
            inner.Append(ary, r => new CombinedWidgetOld(new MetaWidgetOld.HorizonalMargin(0.1f), new MetaWidgetOld.Button(() => onSelected.Invoke(r), MeetingRoleSelectWindow.ButtonAttribute)
            {
                RawText = r.DisplayColoredName,
                TextHorizonotalExtraMargin = 0.15f,
                PostBuilder = (button, renderer, text) =>
                {
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    button.transform.localPosition += new UnityEngine.Vector3(0.05f, 0f, 0f);
                    text.transform.localPosition += new UnityEngine.Vector3(0.072f, 0f, 0f);

                    var icon = UnityHelper.CreateObject<SpriteRenderer>("Icon", button.transform, new(-0.65f, 0f, -0.1f));
                    icon.sprite = r.GetRoleIcon()?.GetSprite();
                    icon.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    icon.material = RoleIcon.GetRoleIconMaterial(r, 0.8f);
                    icon.transform.localScale = new(0.253f, 0.253f, 1f);
                    icon.SetBothOrder(15);
                }
            }), 4, -1, 0, 0.59f);
        }
        var originalInnerContents = new List<IMetaWidgetOld>(inner.contents);
        GUITextField? textField = null;
        MetaWidgetOld.ScrollView scroller = new(new(6.9f, 3f), inner, true) { Alignment = IMetaWidgetOld.AlignmentOption.Center };
        textField = new GUITextField(GUIAlignment.Center, new(5.8f, 0.38f)) { HintText = Language.Translate("help.search.hint").Color(UnityEngine.Color.gray), WithMaskMaterial = false, IsSharpField = false, MaxLines = 1, EnterAction = text => { OnSearch(); return true; }, GainFocus = true };
        bool HasButtonWithText(IMetaWidgetOld widget, string[] query)
        {
            if (widget is MetaWidgetOld.Button button)
                return query.All(q => button.Text.GetString().Contains(q));
            if (widget is CombinedWidgetOld combined)
            {
                foreach (var child in combined.contents)
                {
                    var childWidget = child as IMetaWidgetOld;
                    if (childWidget == null) return false;

                    if (HasButtonWithText(childWidget, query))
                        return true;
                }
            }
            return false;
        }

        void OnSearch()
        {
            var inputText = textField?.Artifact.Last().Text;
            var queries = inputText?.Split(' ', '　');
            if (string.IsNullOrWhiteSpace(inputText) || (queries?.All(string.IsNullOrEmpty) ?? false))
            {
                (scroller.Inner as MetaWidgetOld)?.contents = originalInnerContents.ToList();
                window.SetWidget(widget);
                return;
            }
            DarkLog.LogInfo("MeetingRoleSelectWindow Search: " + string.Join(" ", queries ?? []));
            var matchedRoles = new List<IMetaParallelPlacableOld>();
            foreach (var widget in originalInnerContents)
            {
                if (widget is CombinedWidgetOld outerCombined)
                {
                    foreach (var inner in outerCombined.contents)
                    {
                        if (inner is CombinedWidgetOld roleWidget && HasButtonWithText(roleWidget, queries))
                        {
                            matchedRoles.Add(roleWidget);
                        }
                    }
                }
            }
            DarkLog.LogInfo($"Searched Role num: {matchedRoles.Count}");
            var newInner = new MetaWidgetOld();
            int perLine = 4;
            int index = 0;
            while (index < matchedRoles.Count)
            {
                var lineRoles = matchedRoles.Skip(index).Take(perLine).ToArray();
                newInner.Append(new CombinedWidgetOld(0.59f, lineRoles)
                {
                    Alignment = IMetaWidgetOld.AlignmentOption.Center
                });
                index += perLine;
                /*if (index < matchedRoles.Count)
                {
                    newInner.Append(new MetaWidgetOld.VerticalMargin(0.1f));
                }*/
            }
            (scroller.Inner as MetaWidgetOld)?.contents = newInner.contents;
            window.SetWidget(widget);
        }
        widget.Append(new MetaWidgetOld.WrappedWidget(NebulaAPI.GUI.HorizontalHolder(GUIAlignment.Center, textField, NebulaAPI.GUI.LocalizedButton(GUIAlignment.Center, AttributeAsset.MarketplaceTabNonMaskedButton, "help.search.search", _ => OnSearch()))));
        widget.Append(new MetaWidgetOld.VerticalMargin(0.05f));
        widget.Append(scroller);

        widget.Append(new MetaWidgetOld.Text(TextAttributeOld.BoldAttr) { MyText = new RawTextComponent(underText), Alignment = IMetaWidgetOld.AlignmentOption.Center });

        window.SetWidget(widget);

        System.Collections.IEnumerator CoCloseOnResult()
        {
            if (MeetingHud.Instance.AsBoolFast())
            {
                while (MeetingHud.Instance.state != MeetingHud.MeetingStates.Results) yield return null;
            }
            else
            {
                while (!MeetingHud.Instance.AsBoolFast()) yield return null;
            }
            window.CloseScreen();
        }

        window.StartCoroutine(CoCloseOnResult().WrapToIl2Cpp());


        __result = window;
        return false;
    }
}
