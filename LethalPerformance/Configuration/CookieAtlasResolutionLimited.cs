using System;
using System.Collections.Generic;
using System.Text;

namespace LethalPerformance.Configuration;
/// <summary>
/// <see cref="UnityEngine.Rendering.HighDefinition.CookieAtlasResolution"/>, but limited from 1024 to 16384
/// </summary>
internal enum CookieAtlasResolutionLimited
{
    CookieResolution1024 = 0x400,
    CookieResolution2048 = 0x800,
    CookieResolution4096 = 0x1000,
    CookieResolution8192 = 0x2000,
    CookieResolution16384 = 0x4000
}
