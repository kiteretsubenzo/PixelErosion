using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransitionController
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SceneTransitionManager")
        {
            return;
        }

        if (UnityEngine.SceneManagement.SceneManager.GetSceneByName("SceneTransitionManager").isLoaded)
        {
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("SceneTransitionManager", LoadSceneMode.Additive);
    }
}