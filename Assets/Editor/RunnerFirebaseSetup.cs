#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RunnerFirebaseSetup
{
    [MenuItem("Runner/Setup Firebase Scenes")]
    public static void Setup()
    {
        if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        // Empty entry scenes are populated by RunnerFirebase's runtime UI.
        foreach (string name in new[] { "Login", "Menu" })
        {
            string path = "Assets/Scenes/" + name + ".unity";
            if (!System.IO.File.Exists(path))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, path);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/Login.unity",true),
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity",true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",true)
        };
        AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("FIREBASE_SCENES_READY");
    }
}
#endif
