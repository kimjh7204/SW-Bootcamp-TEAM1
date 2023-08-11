using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FadeUI : MonoBehaviour
{
    public static FadeUI Instance { get; private set; }
    public enum FadeState
    {
        None,
        FadingIn,
        Fade,
        FadingOut
    }
    public FadeState CurFadeState { get; private set; }

    private Image fadeImage;
    private Coroutine fadeOutRoutine;
    private Coroutine fadeInRoutine;
    private void Awake()
    {
        if (Instance is null) Instance = this;
        else if (!Instance.Equals(this)) Destroy(gameObject);
        
        CurFadeState = FadeState.None;
        fadeImage = GetComponent<Image>();
    }

    public void StartFadeOut()
    {
        gameObject.SetActive(true);
        if (fadeInRoutine is not null) StopCoroutine(fadeInRoutine);
        fadeOutRoutine = StartCoroutine(FadeOutCoroutine());
    }

    public void StartFadeIn()
    {
        gameObject.SetActive(true);
        if (fadeOutRoutine is not null) StopCoroutine(fadeOutRoutine);
        fadeInRoutine = StartCoroutine(FadeInCoroutine());
    }

    IEnumerator FadeOutCoroutine()
    { float fadeAlpha = 0;
        CurFadeState = FadeState.FadingOut;
        while (fadeAlpha < 1.0f)
        {
            fadeAlpha += 0.01f;
            yield return new WaitForSeconds(0.01f);
            fadeImage.color = new Color(0, 0, 0, fadeAlpha);
        }
        CurFadeState = FadeState.Fade;
    }

    IEnumerator FadeInCoroutine()
    {
        float fadeAlpha = 1;
        CurFadeState = FadeState.FadingIn;
        while (fadeAlpha > 0f)
        {
            fadeAlpha -= 0.01f;
            yield return new WaitForSeconds(0.01f);
            fadeImage.color = new Color(0, 0, 0, fadeAlpha);
        }
        CurFadeState = FadeState.None;
        gameObject.SetActive(false);
    }
}
