using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Linq;
using UnityEngine;

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

            if (TimeOfDay.Instance.daysUntilDeadline == 0)
            {
                ExtendedLevel gordionLevel = PatchedContent.ExtendedLevels.FirstOrDefault(level => level.NumberlessPlanetName.Equals("Gordion"));

                int CompanyID = gordionLevel.SelectableLevel.levelID;

                if (gordionLevel != LevelManager.CurrentExtendedLevel)
                {
                    StartOfRound.Instance.ChangeLevelServerRpc(CompanyID, Object.FindObjectOfType<Terminal>().groupCredits);
                }
            }
            else
            {
                if (Selenes_Choice.PreviousSafetyMoon != null && Selenes_Choice.PreviousSafetyMoon != LevelManager.CurrentExtendedLevel)
                {
                    int PreviousSafetyMoonID = Selenes_Choice.PreviousSafetyMoon.SelectableLevel.levelID;

                    StartOfRound.Instance.ChangeLevelServerRpc(PreviousSafetyMoonID, Object.FindObjectOfType<Terminal>().groupCredits);
                }
            }
            ES3.Save("CurrentPlanetID", StartOfRound.Instance.currentLevelID, GameNetworkManager.Instance.currentSaveFileName);
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
            if (TimeOfDay.Instance.daysUntilDeadline == 0)
            {
                ExtendedLevel gordionLevel = PatchedContent.ExtendedLevels.FirstOrDefault(level => level.NumberlessPlanetName.Equals("Gordion"));

                int CompanyID = gordionLevel.SelectableLevel.levelID;

                if (gordionLevel != LevelManager.CurrentExtendedLevel)
                {
                    StartOfRound.Instance.ChangeLevelServerRpc(CompanyID, Object.FindObjectOfType<Terminal>().groupCredits);
                }
            }
        }
    }
}