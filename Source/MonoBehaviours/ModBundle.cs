﻿using UnityEngine;

namespace KModkit
{
    /// <summary>
    /// Mod bundle component. This is used to get information about the module at runtime, as this cannot be determined elsewhere. Written by Emik.
    /// </summary>
    public sealed class ModBundle : MonoBehaviour
    {
        /// <summary>
        /// The name of the bundle. This is required for the version number.
        /// </summary>
        public string Name;
    }
}
