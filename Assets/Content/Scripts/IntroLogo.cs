using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroLogo : MonoBehaviour 
{
    [SerializeField] private CanvasGroup logoGroup;

    [SerializeField] private float fadeInTime = 1.5f;   //время перехода появления
    [SerializeField] private float holdTime = 1f;       //сколько лого должно показываться 
    [SerializeField] private float fadeOutTime = 0.5f;  //время перехода исчезновения

    [SerializeField] private string nextSceneName = ""; //имя сцены, которая будет забущена (берет то, что укажешь в инспекторе)

    private void Start()
    {
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        logoGroup.alpha = 0f;

        yield return Fade(0f, 1f, fadeInTime);
        yield return new WaitForSeconds(holdTime);
        yield return Fade(1f, 0f, fadeOutTime);

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            logoGroup.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }

        logoGroup.alpha = to;
    }
}