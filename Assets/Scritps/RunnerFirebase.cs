using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// REST only: no Firebase SDK or browser popup is required by the guest flow.
public sealed class RunnerFirebase : MonoBehaviour
{
    public static RunnerFirebase Instance { get; private set; }
    const string ApiKey = "AIzaSyAuksMT1De3hrF85FUdJkAMKlLz198qPWo";
    const string Database = "https://networkprog2026-db914-default-rtdb.firebaseio.com";
    const string RefreshKey = "Runner.Firebase.networkprog2026-db914.Refresh";
    public string Uid { get; private set; } = "";
    public string GuestName => Uid.Length >= 6 ? "손님 " + Uid.Substring(0, 6) : "";
    string token = "";
    double expires;
    bool busy;
    Text message;
    Button login;
    string pendingKey => "Runner.Firebase.Best." + Uid;
    [Serializable] class Auth { public string localId, idToken, refreshToken, expiresIn, user_id, id_token, refresh_token, expires_in; }
    [Serializable] public class Score { public string name; public int score; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("RunnerFirebase").AddComponent<RunnerFirebase>();
    }
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += Loaded;
    }
    void OnDestroy() { if (Instance == this) { SceneManager.sceneLoaded -= Loaded; Instance = null; } }
    void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Login" || scene.name == "Menu") BuildScreen(scene.name == "Login");
        else if (Uid == "") SceneManager.LoadScene("Login");
    }
    void Status(string text) { if (message != null) message.text = text; }
    static UnityWebRequest JsonPost(string url, string body)
    {
        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 20;
        return request;
    }
    IEnumerator Authenticate()
    {
        string saved = PlayerPrefs.GetString(RefreshKey, "");
        UnityWebRequest request;
        if (saved == "") request = JsonPost("https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + ApiKey, "{\"returnSecureToken\":true}");
        else
        {
            var form = new WWWForm(); form.AddField("grant_type", "refresh_token"); form.AddField("refresh_token", saved);
            request = UnityWebRequest.Post("https://securetoken.googleapis.com/v1/token?key=" + ApiKey, form);
            request.timeout = 20;
        }
        using (request)
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                // A transient network failure must not erase the guest identity.
                string body = request.downloadHandler.text;
                if (saved != "" && (body.Contains("INVALID_REFRESH_TOKEN") || body.Contains("TOKEN_EXPIRED") || body.Contains("USER_NOT_FOUND") || body.Contains("USER_DISABLED")))
                { PlayerPrefs.DeleteKey(RefreshKey); PlayerPrefs.Save(); }
                Status("Login failed (" + request.responseCode + ")\nPlease retry.");
                Debug.LogWarning("Firebase authentication failed, HTTP " + request.responseCode);
                token = "";
                yield break;
            }
            Auth auth = JsonUtility.FromJson<Auth>(request.downloadHandler.text);
            string nextUid = saved == "" ? auth.localId : auth.user_id;
            string nextToken = saved == "" ? auth.idToken : auth.id_token;
            string nextRefresh = saved == "" ? auth.refreshToken : auth.refresh_token;
            if (string.IsNullOrEmpty(nextUid) || string.IsNullOrEmpty(nextToken) || string.IsNullOrEmpty(nextRefresh))
            { Status("Login response invalid. Retry."); token = ""; yield break; }
            Uid = nextUid; token = nextToken;
            int seconds; if (!int.TryParse(saved == "" ? auth.expiresIn : auth.expires_in, out seconds)) seconds = 3600;
            expires = Time.realtimeSinceStartupAsDouble + Math.Max(0, seconds - 60);
            PlayerPrefs.SetString(RefreshKey, nextRefresh); PlayerPrefs.Save();
        }
    }
    public void GuestLogin() { if (!busy) StartCoroutine(SignIn()); }
    IEnumerator SignIn()
    {
        busy = true; login.interactable = false; Status("Login...");
        yield return Authenticate();
        busy = false;
        if (token != "") { SceneManager.LoadScene("Menu"); SaveBest(PlayerPrefs.GetInt(pendingKey, 0)); }
        else if (login != null) login.interactable = true;
    }
    public void SaveBest(int score)
    {
        if (Uid == "") return;
        PlayerPrefs.SetInt(pendingKey, Math.Max(score, PlayerPrefs.GetInt(pendingKey, 0))); PlayerPrefs.Save();
        if (!busy) StartCoroutine(Save());
    }
    IEnumerator Save()
    {
        busy = true;
        if (token == "" || Time.realtimeSinceStartupAsDouble >= expires) yield return Authenticate();
        if (token == "") { busy = false; yield break; }
        string user = Uid;
        // Read before PUT. The supplied database rules also enforce monotonic scores.
        string url = Database + "/scores/" + Uri.EscapeDataString(user) + ".json?auth=" + Uri.EscapeDataString(token);
        using (var get = UnityWebRequest.Get(url))
        {
            get.timeout = 20;
            yield return get.SendWebRequest();
            if (get.result != UnityWebRequest.Result.Success) { Failed(get.responseCode); busy = false; yield break; }
            string data = get.downloadHandler.text.Trim();
            int old = data == "null" ? -1 : JsonUtility.FromJson<Score>(data).score;
            int best = Math.Max(old, PlayerPrefs.GetInt(pendingKey, 0));
            PlayerPrefs.SetInt(pendingKey, best); PlayerPrefs.Save();
            if (old >= best) { Status(GuestName + "\nBest " + best + " (server)"); busy = false; yield break; }
            using (var put = UnityWebRequest.Put(url, JsonUtility.ToJson(new Score { name = GuestName, score = best })))
            {
                put.SetRequestHeader("Content-Type", "application/json"); put.timeout = 20;
                yield return put.SendWebRequest();
                if (put.result == UnityWebRequest.Result.Success) { Status(GuestName + "\nBest " + best + " saved"); Debug.Log("Firebase score saved"); }
                else Failed(put.responseCode);
            }
        }
        busy = false;
    }
    void Failed(long code) { Status("Save failed (" + code + "). Retry from Menu."); Debug.LogWarning("Firebase score save failed, HTTP " + code); }
    public void Logout()
    {
        if (busy) { Status("Please wait..."); return; }
        Uid = token = ""; expires = 0; SceneManager.LoadScene("Login");
    }
    public void ReturnToMenu() { SceneManager.LoadScene("Menu"); }
    public void BindStatus(Text target) { message = target; Status(GuestName); }
    void BuildScreen(bool isLogin)
    {
        if (!isLogin && Uid == "") { SceneManager.LoadScene("Login"); return; }
        var canvasObject = new GameObject("FirebaseScreen", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(480, 854);
        if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var panel = new GameObject("Background", typeof(Image)); panel.transform.SetParent(canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.025f, .055f, .09f);
        Label(canvas.transform, "MONSTER STRIKE\nRUNNER", 190, 34);
        message = Label(canvas.transform, isLogin ? "게스트 로그인" : GuestName + "\nBest " + PlayerPrefs.GetInt(pendingKey, 0), 55, 25);
        if (isLogin) login = Button(canvas.transform, "게스트 로그인", -75, GuestLogin);
        else
        {
            Button(canvas.transform, "게임 시작", -65, () => SceneManager.LoadScene("SampleScene"));
            Button(canvas.transform, "Retry save", -155, () => SaveBest(PlayerPrefs.GetInt(pendingKey, 0)));
            Button(canvas.transform, "Logout", -245, Logout);
        }
    }
    static Text Label(Transform parent, string text, float y, int size)
    {
        var obj = new GameObject("Label", typeof(Text)); obj.transform.SetParent(parent, false);
        var label = obj.GetComponent<Text>(); label.font = Resources.Load<Font>("Fonts/RunnerKorean"); label.fontSize = size; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.text = text; label.raycastTarget = false;
        var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f); rect.sizeDelta = new Vector2(450,140); rect.anchoredPosition = new Vector2(0,y); return label;
    }
    static Button Button(Transform parent, string text, float y, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(text, typeof(Image), typeof(Button)); obj.transform.SetParent(parent,false);
        var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f); rect.sizeDelta = new Vector2(320,70); rect.anchoredPosition = new Vector2(0,y);
        obj.GetComponent<Image>().color = new Color(.07f,.4f,.4f);
        var button = obj.GetComponent<Button>(); button.targetGraphic = obj.GetComponent<Image>(); button.onClick.AddListener(action); Label(obj.transform,text,0,25); return button;
    }
}
