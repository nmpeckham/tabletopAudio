using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
            DateTime epoch = new DateTime(1970, 1, 1);
            return dt.Subtract(epoch).TotalSeconds;
        }

        //Takes a value between 0 and 1, maps to -80 and 0
        public static float ToDB(this float volume)
        {
            return (volume * 80) - 80;
        }

        // Takes a value between -80 and 0 and maps between 0 and 1
        public static float ToZeroOne(this float volume)
        {
            return (volume + 80) / 80;
        }
    }
}

