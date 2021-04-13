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
    }
}

