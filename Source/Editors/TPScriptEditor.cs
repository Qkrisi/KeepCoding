﻿using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KModkit
{
    /// <summary>
    /// Custom inspector for <see cref="TPScript{TModule}"/>. Written by Emik.
    /// </summary>
    [CustomEditor(typeof(TPScript<>),  true)]
    [CanEditMultipleObjects]
    public class TPScriptEditor : Editor
    {
        /// <summary>
        /// Creates the force solve button.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Force Solve"))
                StartVerification((MonoBehaviour)target);

            if (GUILayout.Button("Force Solve All"))
                FindObjectsOfType<MonoBehaviour>().Where(m => m is ITP).Distinct().ToArray().ForEach(m => StartVerification(m));
        }

        private static void StartVerification(MonoBehaviour obj) => obj.StartCoroutine(VerifySolve(obj));

        private static IEnumerator VerifySolve(MonoBehaviour obj)
        {
            var tp = (ITP)obj;
            var module = obj.GetComponent<ModuleBase>();

            yield return tp.TwitchHandleForcedSolve();

            if (!module.IsSolved)
                module.Log($"The module's solve coroutine finished before the module solved! This is a mistake because Twitch Plays will force-solve a module as soon as the {nameof(IEnumerator)} finishes running. You can fix it by adding a while loop for {nameof(ModuleBase.IsSolved)} that yield returns true.", LogType.Error);
        }
    }
}
