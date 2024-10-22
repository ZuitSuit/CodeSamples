using DG.Tweening;
using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;

    [SerializeField] GameObject soundSettingsWindow, videoSettingsWindow, gameWindow, savesWindow;

    [SerializeField] Slider masterVolumeSlider, sfxVolumeSlider, musicVolumeSlider;
    [SerializeField] TextMeshProUGUI masterVolumePercentage, sfxVolumePercentage, musicVolumePercentage;
    [SerializeField] GameObject menuSpecificStuff;

    [SerializeField] Transform loadingSpinnerTransform;
    [SerializeField] TextMeshProUGUI loadingText, loadingPercentage;
    [SerializeField] TMP_Text lastSave;

    [SerializeField] Image fadeImage;
    [SerializeField] GameObject menuParentObject;
    [SerializeField] RectTransform loadingRectTransform;

    [SerializeField] AudioSource menuAudio;

    [SerializeField] GameObject gameLoadingGraphic;
    [SerializeField] RenderTexture gameTexture;
    [SerializeField] CanvasGroup gameRenderCanvasGroup;


    bool isGameLaunching;
    bool isFading;
    bool isMenuOpen = true;

    public bool IsMenuOpen => isMenuOpen;

    Vector2Int gameWindowSize;
    Vector2 previousGameWindowPos;
    public static MainMenu Instance;

    private void Awake()
    {
        Instance = this;
    }


    private void Start()
    {
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", .8f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", .8f);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", .8f);

        //set and remember game window size and position
        //based on screen size

        loadingRectTransform.sizeDelta = gameWindowSize = (new Vector2Int(Screen.width, Screen.height)) /2;

        gameTexture.Release();
        gameTexture.width = gameWindowSize.x / 4;
        gameTexture.height = gameWindowSize.y / 4;
        gameTexture.Create();
    }

    public void MasterVolumeChanged(float newValue) 
    {
        mixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Lerp(0.00001f, 1f, newValue)) * 20f);
        PlayerPrefs.SetFloat("MasterVolume", newValue);
        masterVolumePercentage.text = (newValue * 100).ToString("000") + "%";
        //save new value?
    }

    public void SoundVolumeChanged(float newValue)
    {
        mixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Lerp(0.00001f, 1f, newValue)) * 20f);
        PlayerPrefs.SetFloat("SFXVolume", newValue);
        sfxVolumePercentage.text = (newValue * 100).ToString("000") + "%";
        //save new value?
    }

    public void MusicVolumeChanged(float newValue)
    {
        mixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Lerp(0.00001f, 1f, newValue)) * 20f);
        PlayerPrefs.SetFloat("MusicVolume", newValue);
        musicVolumePercentage.text = (newValue * 100).ToString("000") + "%";
        //save new value?
    }

    public void OpenWindow(GameObject go)
    {
        if (go.activeSelf)
        {
            go.transform.SetAsLastSibling();
            return;
        }

        go.transform.localScale = new Vector3(1f,0f,1f);
        go.SetActive(true);
        go.transform.DOScaleY(1f, .2f).SetEase(Ease.OutCirc);
    }

    public void CloseWindow(GameObject go)
    {
        go.transform.DOScaleY(0f, .2f).SetEase(Ease.InCirc).OnComplete(() => go.SetActive(false));
    }

    public void PlayGame()
    {
        if (isGameLaunching) return;
        isGameLaunching = true;
        //prevent second click on this?
        StartCoroutine(GameLoading());

    }

    public void GameSceneLoaded()
    {
        if(menuSpecificStuff != null) menuSpecificStuff.SetActive(false);
    }

    public IEnumerator GameLoading()
    {
        float timeSpent = 0f;
        float minLoadTime = 3f;
        loadingSpinnerTransform.localScale = Vector3.zero;
        loadingSpinnerTransform.DOScale(1f, .2f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;

        loadingText.transform.DOLocalRotate(Vector3.forward * -360f, 3f, RotateMode.LocalAxisAdd).SetLoops(-1).SetEase(Ease.InOutCirc);

        while (asyncLoad.progress < .9f)
        {
            timeSpent += Time.deltaTime;
            loadingPercentage.text = (asyncLoad.progress/.9f * 100f).ToString("000") + "%";
            yield return null;
        }

        loadingPercentage.text = "100%";

        yield return new WaitForSeconds(Mathf.Max(0f, minLoadTime - timeSpent));


        StartCoroutine(SceneTransition(() => ActivateGameScene(asyncLoad)));

        yield return null;
    }

    public void ActivateGameScene(AsyncOperation operation)
    {
        //SceneManager.SetActiveScene(SceneManager.GetSceneByName("Game"));
        if (menuSpecificStuff != null) menuSpecificStuff.SetActive(false);
        operation.allowSceneActivation = true;
    }

    public void TogglePause()
    {
        if (!isFading)
        {
            StartCoroutine(SceneTransition());
        }
    }

    public IEnumerator SceneTransition(System.Action OnMidPoint = null)
    {
        if (!isFading)
        {
            isFading = true;

            float windowScaleTime = 1f;
            menuAudio.DOFade(!isMenuOpen ? 1f : 0f, windowScaleTime/2f);

            if(!isMenuOpen)
            {
                menuParentObject.SetActive(true);
                //do the render texture thing
            }

            Sequence sequence = DOTween.Sequence();

            if (isMenuOpen)
            {
                previousGameWindowPos = loadingRectTransform.anchoredPosition;

                
                sequence.Append(loadingRectTransform.DOAnchorMin(Vector2.zero, windowScaleTime));
                sequence.Join(loadingRectTransform.DOAnchorMax(Vector2.one, windowScaleTime));
                sequence.Join(loadingRectTransform.DOAnchorPos(Vector2.zero, windowScaleTime));
                sequence.Join(loadingRectTransform.DOSizeDelta(Vector2.one * 80f, windowScaleTime)).OnComplete(() => SwapMenu(false));
                //sequence.Append(gameRenderCanvasGroup.DOFade(0f,.2f));
            }
            else
            {
                //sequence.Append(gameRenderCanvasGroup.DOFade(1f, .2f));
                sequence.Append(loadingRectTransform.DOAnchorMin(Vector2.one * .5f, windowScaleTime));
                sequence.Join(loadingRectTransform.DOAnchorMax(Vector2.one * .5f, windowScaleTime));
                sequence.Join(loadingRectTransform.DOAnchorPos(previousGameWindowPos, windowScaleTime));
                sequence.Join(loadingRectTransform.DOSizeDelta(gameWindowSize, windowScaleTime)).OnComplete(() => SwapMenu(true));
            }

            OnMidPoint?.Invoke();

            isMenuOpen = !isMenuOpen;

            yield return null;
        }
    }

    public void SwapMenu(bool toggle)
    {
        gameLoadingGraphic.SetActive(false);
        menuParentObject.SetActive(toggle);
        isFading = false;
        Cursor.lockState = toggle ? CursorLockMode.Confined : CursorLockMode.Locked;
        Cursor.visible = toggle;
    }

    public void QuitGame()
    {
        //mb tween smth
        Application.Quit();
    }

    //TODO save name as parameter
    public void ResetSave()
    {
        CloseWindow(gameWindow);
        Scene gameScene = SceneManager.GetSceneByName("Game");
        if(gameScene.isLoaded) SceneManager.UnloadSceneAsync("Game");

        string path = $"{Application.persistentDataPath}/demoV3.JSON";
        if (File.Exists(path)) File.Delete(path);

        menuSpecificStuff.SetActive(true);
        isGameLaunching = false;
        gameLoadingGraphic.SetActive(true);

        PlayerPrefs.DeleteKey("lastSave");
    }


    public void SetLastSave()
    {
        //PlayerPrefs.SetString("lastSave", System.DateTime.Now.Ticks.ToString());
        if (PlayerPrefs.HasKey("lastSave"))
        {
            DateTime lastSaveTime = new DateTime(long.Parse(PlayerPrefs.GetString("lastSave")));
            lastSave.text =  $"{lastSaveTime.ToString("HH:mm, MMMM d")}";
        }
        else
        {
            lastSave.text = "...";
        }

    }
}
