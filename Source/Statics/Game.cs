﻿using System;
using System.Collections.Generic;
using static KModkit.ComponentPool;
using Connection = IRCConnection;
using KTInfo = Assets.Scripts.Mods.ModInfo;
using KTInput = KTInputManager;
using KTMod = ModManager;
using KTPlayer = Assets.Scripts.Settings.PlayerSettingsManager;
using KTScene = SceneManager;

namespace KModkit
{
    /// <summary>
    /// Allows access into the game's internal code. Written by Emik.
    /// </summary>
    /// <remarks>
    /// You should avoid calling this class from the Editor as it uses the game assembly as a dependancy.
    /// </remarks>
    public static class Game
    {
        /// <summary>
        /// Determines how the game is controlled.
        /// </summary>
        public enum ControlType
        {
            /// <value>
            /// The game is being controlled with a gamepad controller.
            /// </value>
            Gamepad,

            /// <value>
            /// The game is being controlled with a virtual reality headset.
            /// </value>
            Gaze,

            /// <value>
            /// The game is being controlled with a mouse.
            /// </value>
            Mouse,

            /// <value>
            /// The game is being controlled with virtual reality headset and controller.
            /// </value>
            Motion,

            /// <value>
            /// The game is being controlled with touch controls.
            /// </value>
            Touch,

            /// <value>
            /// The game is being controlled with three degrees of freedom, part of virtual reality.
            /// </value>
            ThreeDOF
        }

        /// <summary>
        /// Determines how the mod is stored.
        /// </summary>
        public enum ModSourceEnum
        {
            /// <value>
            /// The mod is invalid.
            /// </value>
            Invalid,

            /// <value>
            /// The mod is stored within the local mods folder.
            /// </value>
            Local,

            /// <value>
            /// The mod is stored within the workshop folder.
            /// </value>
            SteamWorkshop
        }

        /// <summary>
        /// Allows access into Twitch Plays messaging system.
        /// </summary>
        public static class IRCConnection
        {
            /// <value>
            /// Sends a message to the chat.
            /// </value>
            /// <remarks>Arguments: <c>message</c>.</remarks>
            public static Action<string> SendMessage => Connection.SendMessage;

            /// <value>
            /// Sends a message to the chat.
            /// </value>
            /// <remarks>Arguments: <c>message</c> and <c>args</c>.</remarks>
            public static Action<string, object[]> SendMessageFormat => Connection.SendMessageFormat;
                           
            /// <value>
            /// Whispers a message to a person.
            /// </value>
            /// <remarks>Arguments: <c>userNickName</c>, <c>message</c>, and <c>args</c>.</remarks>
            public static Action<string, string, object[]> SendWhisper => Connection.SendWhisper;
        }

        /// <summary>
        /// Allows access relating to how the game is being interacted with.
        /// </summary>
        public static class KTInputManager
        {
            /// <value>
            /// Determines if the current way the game is being controlled is VR-related.
            /// </value>
            public static bool IsCurrentControlTypeVR => CurrentControlType is ControlType.Gaze || CurrentControlType is ControlType.Motion || CurrentControlType is ControlType.ThreeDOF;

            /// <value>
            /// The current way the game is being controlled.
            /// </value>
            public static ControlType CurrentControlType => (ControlType)KTInput.Instance.CurrentControlType;
        }

        /// <summary>
        /// Allows access relating to the current mission.
        /// </summary>
        public static class Mission
        {
            /// <value>
            /// Determines whether or not all pacing events are enabled.
            /// </value>
            public static bool IsPacingEvents => KTScene.Instance.GameplayState.Mission.PacingEventsEnabled;

            /// <value>
            /// The description as it appears in the bomb binder.
            /// </value>
            public static string Description => Localization.GetLocalizedString(KTScene.Instance.GameplayState.Mission.DescriptionTerm);

            /// <value>
            /// The mission name as it appears in the bomb binder.
            /// </value>
            public static string DisplayName => Localization.GetLocalizedString(KTScene.Instance.GameplayState.Mission.DisplayNameTerm);

            /// <value>
            /// The ID of the mission.
            /// </value>
            public static string ID => KTScene.Instance.GameplayState.Mission.ID;

            /// <value>
            /// Gets the generator setting of the mission.
            /// </value>
            public static GeneratorSetting GeneratorSetting
            {
                get
                {
                    var setting = KTScene.Instance.GameplayState.Mission.GeneratorSetting;
                    var list = new List<ComponentPool>();

                    foreach (var pool in setting.ComponentPools)
                    {
                        var types = new List<ComponentPool.ComponentTypeEnum>();

                        foreach (var type in pool.ComponentTypes)
                            types.Add((ComponentPool.ComponentTypeEnum)type);

                        list.Add(new ComponentPool(
                            pool.Count,
                            (ComponentPool.ComponentSource)pool.AllowedSources,
                            (ComponentPool.SpecialComponentTypeEnum)pool.SpecialComponentType,
                            pool.ModTypes,
                            types));
                    }

                    return new GeneratorSetting(
                        setting.FrontFaceOnly,
                        setting.OptionalWidgetCount,
                        setting.NumStrikes,
                        setting.TimeBeforeNeedyActivation,
                        setting.TimeLimit,
                        list
                        );
                }
            }
        }

        /// <summary>
        /// Allows access to methods relating mod paths.
        /// </summary>
        public static class ModManager
        {
            /// <value>
            /// Gets all of the disabled mod paths.
            /// </value>
            public static Func<List<string>> GetDisabledModPaths => KTMod.Instance.GetDisabledModPaths;

            /// <value>
            /// Gets all of the mod paths within the <see cref="ModSourceEnum"/> constraint.
            /// </value>
            public static Func<ModSourceEnum, List<string>> GetAllModPathsFromSource => source => KTMod.Instance.GetAllModPathsFromSource((KTInfo.ModSourceEnum)source);

            /// <value>
            /// Gets all of the enabled mod paths within the <see cref="ModSourceEnum"/> constraint.
            /// </value>
            public static Func<ModSourceEnum, List<string>> GetEnabledModPaths => source => KTMod.Instance.GetEnabledModPaths((KTInfo.ModSourceEnum)source);
        }

        /// <summary>
        /// Allows access into the player settings from the game. Do not use this class in the unity editor. Written by Emik.
        /// </summary>
        public static class PlayerSettings
        {
            /// <value>
            /// Determines if vertical tilting is flipped or not.
            /// </value>
            public static bool InvertTiltControls => KTPlayer.Instance.PlayerSettings.InvertTiltControls;

            /// <value>
            /// Determines if the option to lock the mouse to the window is enabled.
            /// </value>
            public static bool LockMouseToWindow => KTPlayer.Instance.PlayerSettings.LockMouseToWindow;

            /// <value>
            /// Determines if the option to show the leaderboards from the pamphlet.
            /// </value>
            public static bool ShowLeaderBoards => KTPlayer.Instance.PlayerSettings.ShowLeaderBoards;

            /// <value>
            /// Determines if the option to show the rotation of the User Interface is enabled.
            /// </value>
            public static bool ShowRotationUI => KTPlayer.Instance.PlayerSettings.ShowRotationUI;

            /// <value>
            /// Determines if the option to show scanlines is enabled.
            /// </value>
            public static bool ShowScanline => KTPlayer.Instance.PlayerSettings.ShowScanline;

            /// <value>
            /// Determines if the option to skip the title screen is enabled.
            /// </value>
            public static bool SkipTitleScreen => KTPlayer.Instance.PlayerSettings.SkipTitleScreen;

            /// <value>
            /// Determines if the VR or regular controllers vibrate.
            /// </value>
            public static bool RumbleEnabled => KTPlayer.Instance.PlayerSettings.RumbleEnabled;

            /// <value>
            /// Determines if the touchpad controls are inverted.
            /// </value>
            public static bool TouchpadInvert => KTPlayer.Instance.PlayerSettings.TouchpadInvert;

            /// <value>
            /// Determines if the option to always use mods is enabled.
            /// </value>
            public static bool UseModsAlways => KTPlayer.Instance.PlayerSettings.UseModsAlways;

            /// <value>
            /// Determines if the option to use parallel/simultaneous mod loading is enabled.
            /// </value>
            public static bool UseParallelModLoading => KTPlayer.Instance.PlayerSettings.UseParallelModLoading;

            /// <value>
            /// Determines if VR mode is requested.
            /// </value>
            public static bool VRModeRequested => KTPlayer.Instance.PlayerSettings.VRModeRequested;

            /// <value>
            /// The intensity of anti-aliasing currently on the game. Ranges 0 to 8.
            /// </value>
            public static int AntiAliasing => KTPlayer.Instance.PlayerSettings.AntiAliasing;

            /// <value>
            /// The current music volume from the dossier menu. Ranges 0 to 100.
            /// </value>
            public static int MusicVolume => KTPlayer.Instance.PlayerSettings.MusicVolume;

            /// <value>
            /// The current sound effects volume from the dosssier menu. Ranges 0 to 100.
            /// </value>
            public static int SFXVolume => KTPlayer.Instance.PlayerSettings.SFXVolume;

            /// <value>
            /// Determines if VSync is on or off.
            /// </value>
            public static int VSync => KTPlayer.Instance.PlayerSettings.VSync;

            /// <value>
            /// The current language code.
            /// </value>
            public static string LanguageCode => KTPlayer.Instance.PlayerSettings.LanguageCode;
        }
    }
}
