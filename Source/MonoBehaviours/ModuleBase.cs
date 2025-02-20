﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Networking;
using static KMAudio;
using static KMSoundOverride;

namespace KModkit
{
    /// <summary>
    /// Base class for solvable and needy modded modules in Keep Talking and Nobody Explodes. Written by Emik.
    /// </summary>
    [RequireComponent(typeof(ModBundle))]
    public abstract class ModuleBase : MonoBehaviour
    {
        /// <value>
        /// Determines whether the module has been struck. Twitch Plays script will set this to false when a command is interrupted.
        /// </value>
        public bool HasStruck { get; internal set; }

        /// <value>
        /// Determines whether the bomb is currently active, and the timer is ticking.
        /// </value>
        public bool IsActive { get; private set; }

        /// <value>
        /// Determines whether the application is running from inside unity.
        /// </value>
        public static bool IsEditor => Application.isEditor;

        /// <value>
        /// Determines whether this module is the last instantiated instance.
        /// </value>
        public bool IsLastInstantiated => ModuleId == _moduleIds[Module.ModuleType];

        /// <value>
        /// Determines whether the needy is active.
        /// </value>
        public bool IsNeedyActive { get; private set; }

        /// <value>
        /// Determines whether the module has been solved.
        /// </value>
        public bool IsSolved { get; private set; }

        /// <value>
        /// Determines whether the game is being played in Virtual Reality.
        /// </value>
        public static bool IsVR => !IsEditor && Game.KTInputManager.IsCurrentControlTypeVR;

        /// <value>
        /// The Unique Id for this module of this type.
        /// </value>
        public int ModuleId { get; private set; }

        /// <value>
        /// The amount of time left on the bomb, in seconds, rounded down.
        /// </value>
        public int TimeLeft { get; private set; }

        /// <value>
        /// The version number of the entire mod. Requires <see cref="ModBundle.Name"/> to be set.
        /// </value>
        /// <exception cref="OperationCanceledException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public string Version => (IsEditor ? "Can't get Version Number in Editor" : PathManager.GetModInfo(Get<ModBundle>().Name).Version) ?? throw new OperationCanceledException($"{nameof(ModBundle.Name)} couldn't be found. Did you spell your Mod name correctly? Refer to this link for more details: https://github.com/Emik03/KeepCoding/wiki/Chapter-2.1:-ModuleBase#version-string");

        /// <value>
        /// Contains an instance for every <see cref="Sound"/> played by this module using <see cref="PlaySound(Transform, bool, Sound[])"/> or any of its overloads.
        /// </value>
        public Sound[] Sounds { get; private set; } = new Sound[0];

        /// <value>
        /// Contains either <see cref="KMBombModule"/> or <see cref="KMNeedyModule"/>, and allows for running commands through context.
        /// </value>
        public ModuleContainer Module { get; private set; }

        /// <value>
        /// Gets the Twitch Plays <see cref="Component"/> attached to this <see cref="GameObject"/>.
        /// </value>
        /// <remarks>
        /// Due to type ambiguity, a non-generic interface is returned.
        /// </remarks>
        public ITP TP => _tp ??= GetComponents<Component>().FirstValue(c => c is ITP ? c : null) as ITP;
        private ITP _tp;

        private static readonly Dictionary<string, int> _moduleIds = new Dictionary<string, int>();

        private static Dictionary<string, Dictionary<string, object>[]> _database;

        private Action _setActive;

        private Dictionary<Type, Component[]> _components;

        /// <summary>
        /// This initalizes the module. If you have an Awake method, be sure to call <c>base.Awake()</c> as the first statement.
        /// </summary>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="NullIteratorException"></exception>
        protected virtual void Awake()
        {
            _setActive = () =>
            {
                if (Get<KMBombInfo>(allowNull: true) is KMBombInfo bombInfo)
                    StartCoroutine(BombInfo(bombInfo));
                IsActive = true;
                OnActivate();
            };

            _components = new Dictionary<Type, Component[]>() { { typeof(ModuleBase), new[] { this } } };

            _database = new Dictionary<string, Dictionary<string, object>[]>();

            Get<ModBundle>().Name.NullOrEmptyCheck("The public field \"ModBundleName\" is empty! This means that when compiled it won't be able to run! Please set this field to your Mod ID located at Keep Talking ModKit -> Configure Mod. Refer to this link for more details: https://github.com/Emik03/KeepCoding/wiki/Chapter-2.4:-ModBundle");
                     
            Module = new ModuleContainer(Get<KMBombModule>(allowNull: true), Get<KMNeedyModule>(allowNull: true));

            Module.OnActivate(_setActive);

            ModuleId = _moduleIds.SetOrReplace(Module.ModuleType, i => ++i);

            Log($"Version: [{Version.NullOrEmptyCheck("The version number is empty! To fix this, go to Keep Talking ModKit -> Configure Mod, then fill in the version number.")}]");

            StartCoroutine(EditorCheckLatest());
        }

        /// <summary>
        /// Assigns events specified into <see cref="KMBombModule"/> or <see cref="KMNeedyModule"/>. Reassigning them will replace their values.
        /// </summary>
        /// <remarks>
        /// An event that is null will be skipped. This extension method simplifies all of the KMFramework events into Actions or Functions.
        /// </remarks>
        /// <exception cref="MissingComponentException"></exception>
        /// <param name="onActivate">Called when the bomb has been activated and the timer has started.</param>
        /// <param name="onNeedyActivation">Called when the needy timer activates.</param>
        /// <param name="onNeedyDeactivation">Called when the needy gets solved or the bomb explodes.</param>
        /// <param name="onTimerExpired">Called when the timer of the needy runs out.</param>
        public void Assign(Action onActivate = null, Action onNeedyActivation = null, Action onNeedyDeactivation = null, Action onTimerExpired = null)
        {
            Module.OnActivate(_setActive + onActivate);

            if (Module.Module is KMNeedyModule)
                AssignNeedy(onTimerExpired, onNeedyActivation, onNeedyDeactivation);
        }

        /// <summary>
        /// Handles typical button <see cref="KMSelectable.OnInteract"/> behaviour.
        /// </summary>
        /// <exception cref="UnassignedReferenceException"></exception>
        /// <exception cref="UnrecognizedValueException"></exception>
        /// <param name="selectable">The selectable, which is used as a source for sound and bomb shake.</param>
        /// <param name="intensityModifier">The intensity of the bomb shaking.</param>
        /// <param name="sounds">The sounds, these can either be <see cref="string"/>, <see cref="AudioClip"/>, or <see cref="SoundEffect"/>.</param>
        public void ButtonEffect(KMSelectable selectable, float intensityModifier = 0, params Sound[] sounds)
        {
            if (!selectable)
                throw new UnassignedReferenceException("Selectable should not be null when calling this method.");

            selectable.AddInteractionPunch(intensityModifier);

            PlaySound(selectable.transform, sounds);
        }

        /// <summary>
        /// Dumps all information that it can find of the module using reflection. This should only be used to debug.
        /// </summary>
        /// <param name="getVariables">Whether it should search recursively for the elements within the elements.</param>
        public void Dump(bool getVariables = false)
        {
            int index = 0;

            string Format<T>(string name, T value) => Helper.VariableTemplate.Form(index++, name, value?.GetType().ToString() ?? Helper.Null, string.Join(", ", value.Unwrap(getVariables).Select(o => o.ToString()).ToArray()));

            var type = GetType();
            var values = new List<object>();

            type.GetFields(Helper.Flags).ForEach(f => values.Add(Format(f.Name, f.GetValue(this))));
            type.GetProperties(Helper.Flags).ForEach(p => values.Add(Format(p.Name, p.GetValue(this, null))));

            Debug.LogWarning(Helper.DumpTemplate.Form(Module.ModuleDisplayName, ModuleId, string.Join("", values.Select(o => string.Join("", o.Unwrap(getVariables).Select(o => o.ToString()).ToArray())).ToArray())));
        }

        /// <summary>
        /// Dumps all information about the variables specified. Each element uses the syntax () => varName. This should only be used to debug.
        /// </summary>
        /// <param name="getVariables">Whether it should search recursively for the elements within the elements.</param>
        /// <param name="logs">All of the variables to throughly log.</param>
        public void Dump(bool getVariables, params Expression<Func<object>>[] logs) => Debug.LogWarning(Helper.DumpTemplate.Form(Module.ModuleDisplayName, ModuleId, string.Join("", logs.Select((l, n) => Helper.VariableTemplate.Form(n, Helper.NameOfVariable(l), l.Compile()()?.GetType().ToString() ?? Helper.Null, string.Join(", ", l.Compile()().Unwrap(getVariables).Select(o => o.ToString()).ToArray()))).ToArray())));

        /// <summary>
        /// Dumps all information about the variables specified. Each element uses the syntax () => varName. This should only be used to debug.
        /// </summary>
        /// <param name="logs">All of the variables to throughly log.</param>
        public void Dump(params Expression<Func<object>>[] logs) => Dump(false, logs);

        /// <summary>
        /// Solves the module, and logs all of the parameters.
        /// </summary>
        /// <param name="logs">All of the entries to log.</param>
        public void Solve(params string[] logs)
        {
            if (IsSolved)
                return;

            LogMultiple(in logs);

            IsSolved = true;
            Module.HandlePass();
        }

        /// <summary>
        /// Strikes the module, and logs all of the parameters.
        /// </summary>
        /// <param name="logs">All of the entries to log.</param>
        public void Strike(params string[] logs)
        {
            LogMultiple(in logs);

            HasStruck = true;
            Module.HandleStrike();
        }

        /// <summary>
        /// Logs message, but formats it to be compliant with the Logfile Analyzer.
        /// </summary>
        /// <exception cref="UnrecognizedValueException"></exception>
        /// <param name="message">The message to log.</param>
        /// <param name="logType">The type of logging. Different logging types have different icons within the editor.</param>
        public void Log<T>(T message, LogType logType = LogType.Log) => GetLogMethod(logType)($"[{Module.ModuleDisplayName} #{ModuleId}] {message.UnwrapToString()}");

        /// <summary>
        /// Logs multiple entries, but formats it to be compliant with the Logfile Analyzer.
        /// </summary>
        /// <exception cref="UnrecognizedValueException"></exception>
        /// <param name="message">The message to log.</param>
        /// <param name="args">All of the arguments to embed into <paramref name="message"/>.</param>
        public void Log<T>(T message, params object[] args) => Log(message.UnwrapToString().Form(args));

        /// <summary>
        /// Called when the lights turn on.
        /// </summary>
        public virtual void OnActivate() { }

        /// <summary>
        /// Called when any module on the current bomb has issued a strike.
        /// </summary>
        /// <param name="moduleId">The sender's module id, which caused a strike.</param>
        public virtual void OnModuleStrike(string moduleId) { }

        /// <summary>
        /// Called when any <see cref="KMNeedyModule"/> on the current bomb has been solved.
        /// </summary>
        /// <param name="moduleId">The sender's module id, which was solved.</param>
        public virtual void OnNeedySolved(string moduleId) { }

        /// <summary>
        /// Called when any <see cref="KMBombModule"/> on the current bomb has been solved.
        /// </summary>
        /// <param name="moduleId">The sender's module id, which was solved.</param>
        public virtual void OnSolvableSolved(string moduleId) { }

        /// <summary>
        /// Called when the timer's seconds-digit changes.
        /// </summary>
        public virtual void OnTimerTick() { }

        /// <summary>
        /// Sends information to a static variable such that other modules can access it.
        /// </summary>
        /// <remarks>
        /// To ensure that this method works correctly, make sure that both modules have the same version of KeepCoding.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Write<T>(string key, T value)
        {
            if (!_database.ContainsKey(Module.ModuleType))
                _database.Add(Module.ModuleType, new Dictionary<string, object>[] { });

            int index = _moduleIds[Module.ModuleType] - ModuleId;

            while (index >= _database[Module.ModuleType].Length)
                _database[Module.ModuleType].Append(new Dictionary<string, object>());

            if (!_database[Module.ModuleType][index].ContainsKey(key))
                _database[Module.ModuleType][index].Add(key, null);

            _database[Module.ModuleType][index][key] = value;
        }

        /// <summary>
        /// Plays a sound. Requires <see cref="KMAudio"/> to be assigned.
        /// </summary>
        /// <exception cref="EmptyIteratorException"></exception>
        /// <exception cref="NullIteratorException"></exception>
        /// <exception cref="UnrecognizedValueException"></exception>
        /// <param name="transform">The location or sound source of the sound.</param>
        /// <param name="loop">Whether all sounds listed should loop or not.</param>
        /// <param name="sounds">The sounds, these can either be <see cref="string"/>, <see cref="AudioClip"/>, or <see cref="SoundEffect"/>.</param>
        /// <returns>A <see cref="KMAudioRef"/> for each argument you provide.</returns>
        public Sound[] PlaySound(Transform transform, bool loop, params Sound[] sounds)
        {
            sounds.NullOrEmptyCheck($"{nameof(sounds)} is null or empty.");

            if (loop && sounds.Any(s => s.Game.HasValue))
                throw new ArgumentException("The game doesn't support looping in-game sounds.");

            sounds.ForEach(s => s.Reference = GetSoundMethod(s)(transform, loop));

            Sounds = Sounds.Concat(sounds).ToArray();

            return sounds;
        }

        /// <summary>
        /// Plays a sound. Requires <see cref="KMAudio"/> to be assigned.
        /// </summary>
        /// <exception cref="UnrecognizedValueException"></exception>
        /// <param name="transform">The location or sound source of the sound.</param>
        /// <param name="sounds">The sounds, these can either be <see cref="string"/>, <see cref="AudioClip"/>, or <see cref="SoundEffect"/>.</param>
        /// <returns>A <see cref="KMAudioRef"/> for each argument you provide.</returns>
        public Sound[] PlaySound(Transform transform, params Sound[] sounds) => PlaySound(transform, false, sounds);

        /// <summary>
        /// Plays a sound, the sound source is the game object it is attached to. Requires <see cref="KMAudio"/> to be assigned.
        /// </summary>
        /// <exception cref="UnrecognizedValueException"></exception>
        /// <param name="loop">Whether all sounds listed should loop or not.</param>
        /// <param name="sounds">The sounds, these can either be <see cref="string"/>, <see cref="AudioClip"/>, or <see cref="SoundEffect"/>.</param>
        /// <returns>A <see cref="KMAudioRef"/> for each argument you provide.</returns>
        public Sound[] PlaySound(bool loop, params Sound[] sounds) => PlaySound(transform, loop, sounds);

        /// <summary>
        /// Plays a sound, the sound source is the game object it is attached to. Requires <see cref="KMAudio"/> to be assigned.
        /// </summary>
        /// <exception cref="UnrecognizedValueException"></exception>
        /// <param name="sounds">The sounds, these can either be <see cref="string"/>, <see cref="AudioClip"/>, or <see cref="SoundEffect"/>.</param>
        /// <returns>A <see cref="KMAudioRef"/> for each argument you provide.</returns>
        public Sound[] PlaySound(params Sound[] sounds) => PlaySound(transform, false, sounds);

        /// <summary>
        /// Similar to <see cref="Component.GetComponent{T}"/>, however it caches the result in a dictionary, and will return the cached result if called again.
        /// </summary>
        /// <remarks>
        /// Use this in-place of public fields that refer to itself.
        /// </remarks>
        /// <exception cref="MissingComponentException"></exception>
        /// <typeparam name="T">The type of component to search for.</typeparam>
        /// <param name="allowNull">Whether it should throw an exception if it sees null, if not it will return the default value. (Likely null)</param>
        /// <returns>The component specified by <typeparamref name="T"/>.</returns>
        public T Get<T>(bool allowNull = false) where T : Component => GetAll<T>(allowNull).FirstOrDefault();

        /// <summary>
        /// Caches the result of a function call that returns a component array in a dictionary, and will return the cached result if called again. Use this to alleviate expensive function calls.
        /// </summary>
        /// <remarks>
        /// <see cref="GameObject.GetComponent{T}"/> and <see cref="GameObject.GetComponents{T}()"/> have their own implementations already, so use these functions instead for that purpose; 
        /// <seealso cref="Get{T}(bool)"/>, <seealso cref="GetAll{T}(bool)"/>
        /// </remarks>
        /// <exception cref="MissingComponentException"></exception>
        /// <typeparam name="T">The type of component to search for.</typeparam>
        /// <param name="func">The expensive function to call, only if it hasn't ever been called by this method on the current instance before.</param>
        /// <param name="allowNull">Whether it should throw an exception if it sees null, if not it will return the default value. (Likely null)</param>
        /// <returns>The components specified by <typeparamref name="T"/>.</returns>
        public T[] Cache<T>(Func<T[]> func, bool allowNull = false) where T : Component
        {
            if (!_components.ContainsKey(typeof(T)))
                _components.Add(typeof(T), func());

            return allowNull || !_components[typeof(T)].IsNullOrEmpty() ? (T[])_components[typeof(T)] : throw new MissingComponentException($"Tried to get component {typeof(T).Name} from {this}, but was unable to find one.");
        }

        /// <summary>
        /// Similar to <see cref="Component.GetComponents{T}()"/>, however it caches the result in a dictionary, and will return the cached result if called again.
        /// </summary>
        /// <remarks>
        /// Use this in-place of public fields that refer to itself.
        /// </remarks>
        /// <exception cref="MissingComponentException"></exception>
        /// <typeparam name="T">The type of component to search for.</typeparam>
        /// <param name="allowNull">Whether it should throw an exception if it sees null, if not it will return the default value. (Likely null)</param>
        /// <returns>The component specified by <typeparamref name="T"/>.</returns>
        public T[] GetAll<T>(bool allowNull = false) where T : Component => Cache(() => GetComponents<T>(), allowNull);

        /// <summary>
        /// Allows you to read a module's data that uses <see cref="Write{T}(string, T)"/>, even from a different assembly.
        /// </summary>
        /// <remarks>
        /// To ensure that this method works correctly, make sure that both modules have the same version of KeepCoding.
        /// </remarks>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="WrongDatatypeException"></exception>
        /// <typeparam name="T">The type of the expected output.</typeparam>
        /// <param name="module">The module to look into.</param>
        /// <param name="key">The key of the variable, a lot like a variable name.</param>
        /// <param name="allowDefault">Whether it should throw an exception if no value is found, or provide the default value instead.</param>
        /// <returns>Every instance of the value from the every instance of the module specified.</returns>
        public static T[] Read<T>(string module, string key, bool allowDefault = false) => !_database.ContainsKey(module) && !IsEditor ? throw new KeyNotFoundException($"The module {module} does not have an entry!") : _database[module].ConvertAll(d =>
        {
            if (!d.ContainsKey(key))
                return allowDefault || IsEditor ? default(T) : throw new KeyNotFoundException($"The key {key} could not be found in the module {module}!");

            if (d[key] is T t)
                return t;

            throw new WrongDatatypeException($"The data type {typeof(T).Name} was expected, but received {d[key].GetType()} from module {module} with key {key}!");
        });

        private void AssignNeedy(Action onTimerExpired, Action onNeedyActivation, Action onNeedyDeactivation)
        {
            if (onTimerExpired is { })
                Module.Needy.OnTimerExpired += () => onTimerExpired();

            if (onNeedyActivation is { })
                Module.Needy.OnNeedyActivation += () =>
                {
                    onNeedyActivation();
                    IsNeedyActive = true;
                };

            if (onNeedyDeactivation is { })
                Module.Needy.OnNeedyDeactivation += () =>
                {
                    onNeedyDeactivation();
                    IsNeedyActive = false;
                };
        }

        private void CheckForTime(in KMBombInfo bombInfo)
        {
            if (TimeLeft != (int)bombInfo.GetTime())
            {
                TimeLeft = (int)bombInfo.GetTime();
                OnTimerTick();
            }
        }

        private void LogMultiple(in string[] logs) => logs.ForEach(s => Log(s));

        private static short VersionToNumber(string s) => short.Parse(s.Replace(".", "").PadRight(4, '0'));

        private IEnumerator EditorCheckLatest()
        {
            if (!IsEditor)
                yield break;

            using var req = UnityWebRequest.Get("https://raw.githubusercontent.com/Emik03/KeepCoding/main/latest");

            yield return req.SendWebRequest();

            if (req.isNetworkError || req.isHttpError)
            {
                Log("The KeepCoding version could not be pulled, presumably because the user is offline.");
                yield break;
            }

            if (VersionToNumber(PathManager.Version().ProductVersion) < VersionToNumber(req.downloadHandler.text.Trim()))
                Log($"The library is out of date! Latest Version: {req.downloadHandler.text.Trim()}, Local Version: {PathManager.Version().ProductVersion}. Please download the latest version here: https://github.com/Emik03/KeepCoding/releases/latest", LogType.Error);
        }

        private IEnumerator BombInfo(KMBombInfo bombInfo)
        {
            var bomb = GetComponentInParent<KMBomb>();

            var solvables = bomb.GetComponentsInChildren<KMBombModule>();
            var needies = bomb.GetComponentsInChildren<KMNeedyModule>();
            
            solvables.ForEach(m => 
            {
                m.OnPass += () => false.Call(b => OnSolvableSolved(m.ModuleType));
                m.OnStrike += () => false.Call(b => OnModuleStrike(m.ModuleType));
            });

            needies.ForEach(m =>
            {
                m.OnPass += () => false.Call(b => OnNeedySolved(m.ModuleType));
                m.OnStrike += () => false.Call(b => OnModuleStrike(m.ModuleType));
            });

            while (true)
            {
                CheckForTime(in bombInfo);
                yield return null;
            }
        }

        private static Action<object> GetLogMethod(LogType logType) => logType switch
        {
            LogType.Error => Debug.LogError,
            LogType.Assert => o => Debug.LogAssertion(o),
            LogType.Warning => Debug.LogWarning,
            LogType.Log => Debug.Log,
            LogType.Exception => o => Debug.LogException((Exception)o),
            _ => throw new UnrecognizedValueException($"{logType} is not a valid log type."),
        };

        private Func<Transform, bool, KMAudioRef> GetSoundMethod(Sound sound) => (t, b) => 
            sound.Custom is { } ? Get<KMAudio>().HandlePlaySoundAtTransformWithRef(sound.Custom, t, b) : 
            sound.Game is { } ? Get<KMAudio>().HandlePlayGameSoundAtTransformWithRef(sound.Game.Value, t) : 
            throw new UnrecognizedValueException($"{sound}'s properties {nameof(Sound.Custom)} and {nameof(Sound.Game)} are both null!");
    }
}
