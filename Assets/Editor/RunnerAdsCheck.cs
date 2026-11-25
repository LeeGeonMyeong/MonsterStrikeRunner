#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RunnerAdsCheck
{
    static int stage;
    static double deadline, waitUntil;
    static bool callback;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static RunnerAdsCheck() { if (SessionState.GetBool("RunnerAdsCheck", false)) EditorApplication.update += Tick; }
    public static void Start()
    {
        SessionState.SetBool("RunnerAdsCheck", true);
        EditorSceneManager.OpenScene("Assets/Scenes/Login.unity");
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }
    static void Require(bool value, string text) { if (!value) throw new Exception(text); Debug.Log("ADS_CHECK: " + text); }
    static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Private).SetValue(obj,value);
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 60;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Ad check timed out, stage " + stage);
            if (stage == 0)
            {
                // Test gameplay offline; do not create Firebase accounts or alter real scores.
                UnityEngine.Object.DestroyImmediate(RunnerFirebase.Instance.gameObject);
                SceneManager.LoadScene("SampleScene"); stage++; return;
            }
            var game = GameManager.Instance;
            var ads = DummyAds.Instance;
            if (game == null || ads == null) return;
            switch (stage)
            {
                case 1:
                    Require(!GameManager.CanShowInterstitial(1,100,0,30,true), "First run skips interstitial");
                    Require(!GameManager.CanShowInterstitial(2,20,0,30,true), "Cooldown blocks interstitial");
                    Require(!GameManager.CanShowInterstitial(2,100,0,30,false), "No fill skips interstitial");
                    Require(GameManager.CanShowInterstitial(2,100,0,30,true), "Second run after cooldown allows interstitial");
                    ads.networks = new[] { new DummyNetwork { name="Test", eCPM=10, fillRate=1 } }; ads.askSeconds=0;
                    ads.LoadRewarded(); ads.LoadInterstitial();
                    game.StartGame(); game.GetComponent<ObstacleSpawner>().enabled=false; game.GetComponent<RunnerItemSpawner>().enabled=false;
                    game.RecordKill(100); game.EndGame(false);
                    stage++; break;
                case 2:
                    if (!ads.IsRewardedReady()) return;
                    ads.rewardedSeconds=10; game.WatchAdForDouble();
                    Require(Time.timeScale == 0 && AudioListener.pause, "Ad pauses game and sound");
                    Set(ads,"closed",true); stage++; break;
                case 3:
                    if (ads.IsShowing || !ads.IsRewardedReady()) return;
                    Require(game.CurrentScore==100, "Early close gives no reward");
                    Require(Time.timeScale==1 && !AudioListener.pause, "Early close restores game and sound");
                    ads.rewardedSeconds=.05f; game.WatchAdForDouble(); waitUntil=EditorApplication.timeSinceStartup+.25; stage++; break;
                case 4:
                    if (EditorApplication.timeSinceStartup < waitUntil) return;
                    Set(ads,"closed",true); stage++; break;
                case 5:
                    if (ads.IsShowing) return;
                    Require(game.CurrentScore==200, "Completed reward doubles score");
                    typeof(GameManager).GetMethod("OnRewardClosed",Private).Invoke(game,new object[]{true});
                    Require(game.CurrentScore==200, "Duplicate reward callback is ignored");
                    Require(!GameObject.Find("DoubleScoreButton"), "Reward button hidden after grant");
                    ads.noFill=true;
                    Require(!ads.IsRewardedReady() && !ads.IsInterstitialReady(), "No fill invalidates cached ads");
                    ads.ShowRewarded(earned => Require(!earned,"Unavailable ad gives no reward"));
                    ads.noFill=false; ads.LoadInterstitial(); stage++; break;
                case 6:
                    if (!ads.IsInterstitialReady()) return;
                    ads.interstitialSeconds=.05f; ads.ShowInterstitial(()=>callback=true); waitUntil=EditorApplication.timeSinceStartup+.25; stage++; break;
                case 7:
                    if (EditorApplication.timeSinceStartup < waitUntil) return;
                    Set(ads,"closed",true); stage++; break;
                case 8:
                    if (!callback) return;
                    Require(Time.timeScale==1 && !AudioListener.pause,"Interstitial close resumes game");
                    SessionState.SetBool("RunnerAdsCheck",false); Debug.Log("RUNNER_ADS_PASS"); EditorApplication.Exit(0); break;
            }
        }
        catch(Exception error) { SessionState.SetBool("RunnerAdsCheck",false); Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
#endif
