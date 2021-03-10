using HarmonyLib;
using UnityEngine;

namespace ValheimPlus
{
    public class ZNetScene_Lift_Patch
    {
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        public static class ZNetScene_Awake_Patch
        {
            private static void Postfix(ref ZNetScene __instance)
            {
                foreach (var test in __instance.m_namedPrefabs)
                {
                    Debug.Log($"TEST {test.Key} - {test.Value.name}");
                }   
            }
        }
    }
}