using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

namespace Selenes_Choice
{
    [HarmonyPatch(typeof(StartOfRound), "PassTimeToNextDay")]
    public class HideMoonsOnDayChange
    {
        static void Postfix()
        {
            ShareSnT.Instance.StartCoroutine(WaitOnDay());
        }
        static IEnumerator WaitOnDay()
        {
            yield return new WaitForSeconds(2);
            ProcessData();
        }
        static void ProcessData()
        {
            if (Selenes_Choice.Config.DailyOrQuota)
            {
                return;
            }

            CommonShuffle.ShuffleMoons(StartOfRound.Instance.randomMapSeed);

            int NewLevel = -1;
            if (NetworkManager.Singleton.IsHost)
            {
                if (TimeOfDay.Instance.daysUntilDeadline == 0)
                {
                    ExtendedLevel gordionLevel = PatchedContent.ExtendedLevels.FirstOrDefault(level => level.NumberlessPlanetName.Equals("Gordion"));

                    int CompanyID = gordionLevel.SelectableLevel.levelID;

                    if (gordionLevel != LevelManager.CurrentExtendedLevel)
                    {
                        StartOfRound.Instance.ChangeLevelClientRpc(CompanyID, Object.FindObjectOfType<Terminal>().groupCredits);
                        NewLevel = CompanyID;
                    }
                }
                else
                {
                    if (Selenes_Choice.PreviousSafetyMoon != null && Selenes_Choice.PreviousSafetyMoon != LevelManager.CurrentExtendedLevel)
                    {
                        int PreviousSafetyMoonID = Selenes_Choice.PreviousSafetyMoon.SelectableLevel.levelID;

                        StartOfRound.Instance.ChangeLevelClientRpc(PreviousSafetyMoonID, Object.FindObjectOfType<Terminal>().groupCredits);
                        NewLevel = PreviousSafetyMoonID;
                    }
                }
                if (NewLevel != -1)
                    ES3.Save("CurrentPlanetID", NewLevel, GameNetworkManager.Instance.currentSaveFileName);
            }
        }
    }
    [HarmonyPatch(typeof(StartOfRound), "PassTimeToNextDay")]
    public class KeepWeather
    {
        static void Postfix()
        {
            if (!Selenes_Choice.Config.DailyOrQuota)
            {
                return;
            }
            ShareSnT.Instance.StartCoroutine(WaitOnWeather());
        }
        static IEnumerator WaitOnWeather()
        {
            yield return new WaitForSeconds(2);
            SetTheWeather();
        }
        static void SetTheWeather()
        {
            if (Selenes_Choice.Config.ClearWeather)
            {
                if (WeatherRegistryCompatibility.enabled)
                {
                    WeatherRegistryCompatibility.ClearWeatherWithWR(Selenes_Choice.PreviousSafetyMoon);
                }
                {
                    Selenes_Choice.PreviousSafetyMoon.SelectableLevel.currentWeather = LevelWeatherType.None;
                }
                StartOfRound.Instance.SetMapScreenInfoToCurrentLevel();
            }
        }
    }
    [HarmonyPatch(typeof(StartOfRound), "PassTimeToNextDay")]
    public class AutoRouteToCompany
    {
        static void Postfix()
        {
            if (!Selenes_Choice.Config.DailyOrQuota)
            {
                return;
            }
            ShareSnT.Instance.StartCoroutine(WaitOnDay());
        }
        static IEnumerator WaitOnDay()
        {
            yield return new WaitForSeconds(2);
            GoToCompany();
        }
        static void GoToCompany()
        {
            if (NetworkManager.Singleton.IsHost)
                if (TimeOfDay.Instance.daysUntilDeadline == 0)
                {
                    ExtendedLevel gordionLevel = PatchedContent.ExtendedLevels.FirstOrDefault(level => level.NumberlessPlanetName.Equals("Gordion"));

                    int CompanyID = gordionLevel.SelectableLevel.levelID;

                    if (gordionLevel != LevelManager.CurrentExtendedLevel)
                    {
                        StartOfRound.Instance.ChangeLevelClientRpc(CompanyID, Object.FindObjectOfType<Terminal>().groupCredits);
                    }
                    ES3.Save("CurrentPlanetID", CompanyID, GameNetworkManager.Instance.currentSaveFileName);
                }
        }
    }
    [HarmonyPatch(typeof(StartOfRound), "ChangeLevelClientRpc")]
    public class SaveAfterRouting
    {
        static void Postfix()
        {
            if (NetworkManager.Singleton.IsHost)
                ES3.Save("CurrentPlanetID", StartOfRound.Instance.currentLevelID, GameNetworkManager.Instance.currentSaveFileName);
        }
    }
}