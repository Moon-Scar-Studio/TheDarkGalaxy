using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dark.Scripts.Utilities;
public static class DarkAssets
{
    static readonly Dictionary<string, AudioClip?> cs = []; 
    public static AudioClip? GetAudioClip(string p)
    {
        try
        {
            if (cs.TryGetValue(p, out var cached)) return cached;

            AudioClip? clip = null;
            var resource = NebulaAPI.AddonAsset.GetResource(p);
            var stream = resource?.AsStream();
            if (stream != null)
            {
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                clip = ParseWav(memory.ToArray());
            }
            if (clip != null) cs[p] = clip;
            return clip;
        }
        catch(Exception ex)
        {
            DarkLog.LogError(ex.ToString());
            return null;
        }
    }
    public static void Play(string p,float volume = 0.8f)
    {
        var clip = GetAudioClip(p);
        if (clip != null) AmongUsLLImpl.SoundManagerInstance.PlaySound(clip,false,volume);
    }
    #region internal
    static AudioClip? ParseWav(byte[] data)
    {
        if (data.Length < 44) return null;
        if (data[0] != 'R' || data[1] != 'I' || data[2] != 'F' || data[3] != 'F') return null;

        int channels = 1;
        int frequency = 44100;
        int bits = 16;
        int dataOffset = -1;
        int dataLength = 0;

        int pos = 12;
        while (pos + 8 <= data.Length)
        {
            int size = BitConverter.ToInt32(data, pos + 4);
            int body = pos + 8;
            if (data[pos] == 'f' && data[pos + 1] == 'm' && data[pos + 2] == 't')
            {
                channels = BitConverter.ToInt16(data, body + 2);
                frequency = BitConverter.ToInt32(data, body + 4);
                bits = BitConverter.ToInt16(data, body + 14);
            }
            else if (data[pos] == 'd' && data[pos + 1] == 'a' && data[pos + 2] == 't')
            {
                dataOffset = body;
                dataLength = Math.Min(size, data.Length - body);
                break;
            }
            pos = body + size + (size & 1);
        }

        if (dataOffset < 0 || dataLength <= 0 || bits != 16) return null;

        int sampleCount = dataLength / 2;
        var samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            samples[i] = BitConverter.ToInt16(data, dataOffset + i * 2) / 32768f;

        var clip = AudioClip.Create("TDGClip", sampleCount / Math.Max(1, channels), channels, frequency, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}