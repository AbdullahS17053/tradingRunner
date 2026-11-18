using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance;

    [Header("Transition Settings")]
    public Animator transitionAnimator;
    public float transitionTime = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(int sceneIndex)
    {
        StartCoroutine(LoadLevelRoutine(sceneIndex));
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadLevelByNameRoutine(sceneName));
    }

    public void RestartLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadLevelRoutine(currentSceneIndex));
    }

    private IEnumerator LoadLevelByNameRoutine(string sceneName)
    {
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("Start");
        }

        yield return new WaitForSecondsRealtime(transitionTime);

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadLevelRoutine(int sceneIndex)
    {
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("Start");
        }

        yield return new WaitForSecondsRealtime(transitionTime);

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }
}
