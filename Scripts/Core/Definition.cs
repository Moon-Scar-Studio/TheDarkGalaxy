using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Virial;
using Color = Virial.Color;

namespace Dark.Scripts.Core;

public static class Cor
{
    static readonly public Color Cyan = new(0f, 1f, 1f);
    static readonly public Color LightYellow = new(1f, 0.9f, 0.6f);
    static readonly public Color Green = new(0f, 1f, 0f);
    static readonly public Color Blue = new(0f, 0f, 1f);
    static readonly public Color White = new(1f, 1f, 1f);
    static readonly public Color Yellow = new(1f, 1f, 0f);
    static readonly public Color ImaginationCor = new(128, 128, 128);
    static readonly public Color Golden = new(0.8313725f, 0.6862745f, 0.2156863f);
    static readonly public Color Violet = new(0.93f, 0.51f, 0.93f);
}
public static class State
{
    /// <summary>
    /// 死因：舞会事故
    /// </summary>
    public static TranslatableTag PartyAccident = new("state.partyAccident");
}
public static class Team
{

}