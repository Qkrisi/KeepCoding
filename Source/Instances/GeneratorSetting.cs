﻿using System.Collections.Generic;
using System.Linq;

namespace KModkit
{
    /// <summary>
    /// Everything needed to generate a single bomb. Written by Emik.
    /// </summary>
    public sealed class GeneratorSetting
    {
        /// <summary>
        /// Creates a generator setting based on the provided parameters.
        /// </summary>
        /// <param name="frontFaceOnly">Force modules to be placed only on the front face. By default this will limit the module count to 5 (+1 for the timer). Useful in rare circumstances where you wish to generate a bomb with a few modules and guarantee that they will all be visibile simultaneously.</param>
        /// <param name="optionalWidgetCount">Set how many optional widgets (widgets other than serial number) that should be placed on the bomb.</param>
        /// <param name="numStrikes">The number of strikes at which the bomb explodes. Hardcore is 1. More forgiving is 3.</param>
        /// <param name="timeBeforeNeedyActivation">Time, in seconds, which must elapse before any unactivated Needy modules are automatically activated.</param>
        /// <param name="timeLimit">Initial value for the timer when the bomb is activated, in seconds.</param>
        /// <param name="componentPools">The pools of modules that will be used when generating a bomb.</param>
        public GeneratorSetting(bool frontFaceOnly, int optionalWidgetCount, int numStrikes, int timeBeforeNeedyActivation, float timeLimit, List<ComponentPool> componentPools)
        {
            FrontFaceOnly = frontFaceOnly;
            OptionalWidgetCount = optionalWidgetCount;
            NumStrikes = numStrikes;
            TimeBeforeNeedyActivation = timeBeforeNeedyActivation;
            TimeLimit = timeLimit;
            ComponentPools = componentPools;
        }

        /// <summary>
        /// Creates a generator setting with the default parameters.
        /// </summary>
        public GeneratorSetting() : this(false, 5, 3, 90, 300, new List<ComponentPool>()) { }

        /// <value>
        /// Force modules to be placed only on the front face. By default this will limit the module count to 5 (+1 for the timer). Useful in rare circumstances where you wish to generate a bomb with a few modules and guarantee that they will all be visibile simultaneously.
        /// </value>
        public bool FrontFaceOnly { get; private set; }

        /// <value>
        /// Set how many optional widgets (widgets other than serial number) that should be placed on the bomb.
        /// </value>
        public int OptionalWidgetCount { get; private set; }

        /// <value>
        /// The number of strikes at which the bomb explodes. Hardcore is 1. More forgiving is 3.
        /// </value>
        public int NumStrikes { get; private set; }

        /// <value>
        /// Time, in seconds, which must elapse before any unactivated Needy modules are automatically activated.
        /// </value>
        public int TimeBeforeNeedyActivation { get; private set; }

        /// <value>
        /// Initial value for the timer when the bomb is activated, in seconds.
        /// </value>
        public float TimeLimit { get; private set; }

        /// <value>
        /// The pools of modules that will be used when generating a bomb.
        /// </value>
        public List<ComponentPool> ComponentPools { get; private set; }

        /// <summary>
        /// The total count of modules that will be added according to the component pools.
        /// </summary>
        /// <returns>The amount of components.</returns>
        public int GetComponentCount() => ComponentPools.ConvertAll(c => c.Count).Sum();

        /// <summary>
        /// Converts itself to a string.
        /// </summary>
        /// <returns>Time, number of strikes, and the pools converted to a string.</returns>
        public override string ToString() => $"Time: {TimeLimit}, NumStrikes: {NumStrikes}\n{ComponentPools.Count} Pools:\n{string.Join("", ComponentPools.ConvertAll(c => $"{c}\n").ToArray())}";
    }
}
