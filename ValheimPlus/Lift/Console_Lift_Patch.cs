using HarmonyLib;
using UnityEngine;

namespace ValheimPlus
{
    public class Console_Lift_Patch
    {
        [HarmonyPatch(typeof(Console), "InputText")]
        public static class Console_InputText_Patch
        {
            private static void Postfix(ref Console __instance)
            {
                string text1 = __instance.m_input.text;
                __instance.AddString(text1);
                string[] strArray1 = text1.Split(' ');
                if (!__instance.IsCheatsEnabled())
                    return;

                if (strArray1[0] == "comps")
                {
                    if (strArray1.Length <= 1)
                        return;
                    string str = strArray1[1];
                    GameObject prefab = ZNetScene.instance.GetPrefab(str);
                    PrintComps(prefab.transform, 0);
                }
            }
        }

        private static void PrintComps(Transform transform, int depth)
        {
            Debug.Log($"{new System.String('-', depth)}Transform {transform.name}");
            foreach (var comp in transform.GetComponents<Component>())
            {
                Debug.Log($"{new System.String('-', depth)}Comp {comp.GetType().Name}");
            }

            depth++;
            for (int i = 0; i < transform.childCount; i++)
            {
                PrintComps(transform.GetChild(i), depth);
            }
        }
    }
}