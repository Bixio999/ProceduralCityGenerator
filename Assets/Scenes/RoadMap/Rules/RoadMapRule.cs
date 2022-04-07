using UnityEngine;
using System;

public interface RoadMapRule 
{
    public abstract RoadAttributes generateHighway(RoadAttributes roadAttr, Crossroad start, in float[,] populationDensity);
    public abstract RoadAttributes genereateByway(RoadAttributes roadAttr, Crossroad start, in float[,] populationDensity);
}

// public abstract class Singleton<T> where T : Singleton<T>
// {
//     private static T instance;

//     public static T Instance
//     {
//         get
//         {
//             if (instance == null)
//             {
//                 instance = (T)Activator.CreateInstance(typeof(T), true);
//             }
//             return instance;
//         }
//     }

//     public static void setInstance(T instance)
//     {
//         Singleton<T>.instance = instance;
//     }

//     // private static readonly Lazy<T> Lazy =
//     //     new Lazy<T>(() => Activator.CreateInstance(typeof(T), true) as T);

//     // public static T Instance => Lazy.Value;
// }

