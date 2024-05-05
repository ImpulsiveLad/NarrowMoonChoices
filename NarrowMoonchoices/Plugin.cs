﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace NarrowMoonChoices
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class NarrowMoonChoices : BaseUnityPlugin
    {
        private const string modGUID = "impulse.NarrowMoonChoices";
        private const string modName = "NarrowMoonChoices";
        private const string modVersion = "1.0.0";
        private readonly Harmony harmony = new Harmony(modGUID);

        public ManualLogSource mls;

        public static NarrowMoonChoices instance;

        //public new static SyncConfig Config;
        void Awake()
        {
            instance = this;

            //    Config = new SyncConfig(base.Config);
            harmony.PatchAll(typeof(HideMoonsOnDayChange));
            harmony.PatchAll(typeof(HideMoonsOnStart));
            harmony.PatchAll(typeof(HideMoonsOnGameOver));

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }
    }
}