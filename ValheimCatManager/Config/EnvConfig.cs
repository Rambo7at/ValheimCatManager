using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimCatManager.Config;

public class EnvConfig
{
    /// <summary>注：自定义天气名(必填项)</summary>
    public string Name { get; set; } = "";

    /// <summary>注：基于哪个官方天气克隆，继承粒子效果和环境音效(必填项)</summary>
    public string FromOfficial { get; set; } = "";

    /// <summary>注：是否为默认天气 (默认值：false)</summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>注：是否潮湿 (默认值：false)</summary>
    public bool IsWet { get; set; } = false;

    /// <summary>注：是否冰冻 (默认值：false)</summary>
    public bool IsFreezing { get; set; } = false;

    /// <summary>注：夜晚是否冰冻 (默认值：false)</summary>
    public bool IsFreezingAtNight { get; set; } = false;

    /// <summary>注：是否寒冷 (默认值：false)</summary>
    public bool IsCold { get; set; } = false;

    /// <summary>注：夜晚是否寒冷 (默认值：true)</summary>
    public bool IsColdAtNight { get; set; } = true;

    /// <summary>注：是否恒黑 (默认值：false)</summary>
    public bool AlwaysDark { get; set; } = false;

    /// <summary>注：积雪量 (默认值：0)</summary>
    public float SnowBuildup { get; set; } = 0f;

    /// <summary>注：环境色(白天)</summary>
    public Color AmbColorDay { get; set; } = Color.white;

    /// <summary>注：环境色(夜晚)</summary>
    public Color AmbColorNight { get; set; } = Color.white;

    /// <summary>注：雾色(白天)</summary>
    public Color FogColorDay { get; set; } = Color.white;

    /// <summary>注：雾色(夜晚)</summary>
    public Color FogColorNight { get; set; } = Color.white;

    /// <summary>注：雾色(早晨)</summary>
    public Color FogColorMorning { get; set; } = Color.white;

    /// <summary>注：雾色(傍晚)</summary>
    public Color FogColorEvening { get; set; } = Color.white;

    /// <summary>注：雾太阳光(白天)</summary>
    public Color FogColorSunDay { get; set; } = Color.white;

    /// <summary>注：雾太阳光(夜晚)</summary>
    public Color FogColorSunNight { get; set; } = Color.white;

    /// <summary>注：雾太阳光(早晨)</summary>
    public Color FogColorSunMorning { get; set; } = Color.white;

    /// <summary>注：雾太阳光(傍晚)</summary>
    public Color FogColorSunEvening { get; set; } = Color.white;

    /// <summary>注：雾密度(白天) (默认值：0.01)</summary>
    public float FogDensityDay { get; set; } = 0.01f;

    /// <summary>注：雾密度(夜晚) (默认值：0.01)</summary>
    public float FogDensityNight { get; set; } = 0.01f;

    /// <summary>注：雾密度(早晨) (默认值：0.01)</summary>
    public float FogDensityMorning { get; set; } = 0.01f;

    /// <summary>注：雾密度(傍晚) (默认值：0.01)</summary>
    public float FogDensityEvening { get; set; } = 0.01f;

    /// <summary>注：太阳色(白天)</summary>
    public Color SunColorDay { get; set; } = Color.white;

    /// <summary>注：太阳色(夜晚)</summary>
    public Color SunColorNight { get; set; } = Color.white;

    /// <summary>注：太阳色(早晨)</summary>
    public Color SunColorMorning { get; set; } = Color.white;

    /// <summary>注：太阳色(傍晚)</summary>
    public Color SunColorEvening { get; set; } = Color.white;

    /// <summary>注：光照强度(白天) (默认值：1.2)</summary>
    public float LightIntensityDay { get; set; } = 1.2f;

    /// <summary>注：光照强度(夜晚) (默认值：0)</summary>
    public float LightIntensityNight { get; set; } = 0f;

    /// <summary>注：太阳角度 (默认值：60)</summary>
    public float SunAngle { get; set; } = 60f;

    /// <summary>注：风最小值 (默认值：0)</summary>
    public float WindMin { get; set; } = 0f;

    /// <summary>注：风最大值 (默认值：1)</summary>
    public float WindMax { get; set; } = 1f;

    /// <summary>注：云透明度(白天) (默认值：8.53)</summary>
    public float CloudOpacityDay { get; set; } = 8.53f;

    /// <summary>注：云透明度(夜晚) (默认值：8.53)</summary>
    public float CloudOpacityNight { get; set; } = 8.53f;

    /// <summary>注：云透明度(早晨) (默认值：8.53)</summary>
    public float CloudOpacityMorning { get; set; } = 8.53f;

    /// <summary>注：云透明度(傍晚) (默认值：8.53)</summary>
    public float CloudOpacityEvening { get; set; } = 8.53f;

    /// <summary>注：雨云透明度 (默认值：0)</summary>
    public float RainCloudAlpha { get; set; } = 0f;

    /// <summary>注：环境音效音量 (默认值：0.3)</summary>
    public float AmbientVol { get; set; } = 0.3f;


    /// <summary>注：转换为EnvSetup对象，从官方天气克隆后覆盖参数</summary>
    /// <returns>生成的EnvSetup实例</returns>
    public EnvSetup GetEnvSetup()
    {
        if (string.IsNullOrEmpty(FromOfficial))
        {
            Debug.LogError($"[EnvConfig.GetEnvSetup] 没有填写官方模板，检查天气->{Name}");
            return null;
        }

        var template = EnvMan.instance.GetEnv(FromOfficial);
        if (template == null)
        {
            Debug.LogError($"[EnvConfig.GetEnvSetup] 找不到官方天气: {FromOfficial}，检查天气->{Name}");
            return null;
        }

        EnvSetup env = template.Clone();
        env.m_name = Name;
        env.m_default = IsDefault;
        env.m_isWet = IsWet;
        env.m_isFreezing = IsFreezing;
        env.m_isFreezingAtNight = IsFreezingAtNight;
        env.m_isCold = IsCold;
        env.m_isColdAtNight = IsColdAtNight;
        env.m_alwaysDark = AlwaysDark;
        env.m_snowBuildup = SnowBuildup;
        env.m_ambColorDay = AmbColorDay;
        env.m_ambColorNight = AmbColorNight;
        env.m_fogColorDay = FogColorDay;
        env.m_fogColorNight = FogColorNight;
        env.m_fogColorMorning = FogColorMorning;
        env.m_fogColorEvening = FogColorEvening;
        env.m_fogColorSunDay = FogColorSunDay;
        env.m_fogColorSunNight = FogColorSunNight;
        env.m_fogColorSunMorning = FogColorSunMorning;
        env.m_fogColorSunEvening = FogColorSunEvening;
        env.m_fogDensityDay = FogDensityDay;
        env.m_fogDensityNight = FogDensityNight;
        env.m_fogDensityMorning = FogDensityMorning;
        env.m_fogDensityEvening = FogDensityEvening;
        env.m_sunColorDay = SunColorDay;
        env.m_sunColorNight = SunColorNight;
        env.m_sunColorMorning = SunColorMorning;
        env.m_sunColorEvening = SunColorEvening;
        env.m_lightIntensityDay = LightIntensityDay;
        env.m_lightIntensityNight = LightIntensityNight;
        env.m_sunAngle = SunAngle;
        env.m_windMin = WindMin;
        env.m_windMax = WindMax;
        env.m_cloudOpacityDay = CloudOpacityDay;
        env.m_cloudOpacityNight = CloudOpacityNight;
        env.m_cloudOpacityMorning = CloudOpacityMorning;
        env.m_cloudOpacityEvening = CloudOpacityEvening;
        env.m_rainCloudAlpha = RainCloudAlpha;
        env.m_ambientVol = AmbientVol;

        return env;
    }
}

