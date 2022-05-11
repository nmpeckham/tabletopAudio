using System;
using UnityEngine;

namespace Extensions
{
    public static class Extensions
    {
        public static float ToLog(this float a)
        {
            return Mathf.Exp(Mathf.Log(a) / 2);
        }

        public static float ToActual(this float a)
        {
            return Mathf.Pow(a, 2);
        }

        public static double ToUnixTime(this DateTime dt)
        {
            DateTime epoch = new(1970, 1, 1);
            return dt.Subtract(epoch).TotalSeconds;
        }

        //These values were revealed to me in a dream
        //Takes a value between 0 and 1, maps to -80 and 0
        public static float ToDB(this float volume)
        {
            if (volume == 0)
            {
                return -100f;    //log(x) is undefined for x = 0
            }

            return 20f * Mathf.Log(volume);
        }

        // Takes a value between -80 and 0 and maps between 0 and 1
        public static float ToZeroOne(this float volume)
        {
            return Mathf.Pow((float)Math.E, volume / 20f);
        }

        public static int Mod(this int a, int b)
        {
            return (a % b + b) % b;
        }

        public static float Map(this float num, float inMin, float inMax, float outMin, float outMax)
        {
            return outMin + ((num - inMin) * (outMax - outMin) / (inMax - inMin));
        }
    }
}

