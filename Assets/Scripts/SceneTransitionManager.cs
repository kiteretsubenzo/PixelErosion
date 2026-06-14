using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : SingletonMonoBehaviour<SceneTransitionManager>
{
    private Dictionary<string, object> _parameters = new Dictionary<string, object>();

    private Stack<Scene> _sceneStack = new Stack<Scene>();

    private Coroutine _transitionSceneCoroutine = null;
    private Coroutine _pushSceneCoroutine = null;
    private Coroutine _popSceneCoroutine = null;
    public bool IsBusy
    {
        get
        {
            return
                _transitionSceneCoroutine != null ||
                _pushSceneCoroutine != null ||
                _popSceneCoroutine != null;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        _parameters = new Dictionary<string, object>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.name == "SceneTransitionManager")
            {
                continue;
            }

            if (!scene.isLoaded)
            {
                continue;
            }

            _sceneStack.Push(scene);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Set(string key, object value)
    {
        _parameters[key] = value;
    }

    public T Get<T>(string key)
    {
        return (T)_parameters[key];
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (_parameters.TryGetValue(key, out object obj) &&
            obj is T t)
        {
            value = t;
            return true;
        }

        value = default;
        return false;
    }

    public bool Has(string key)
    {
        return _parameters.ContainsKey(key);
    }

    public void Remove(string key)
    {
        _parameters.Remove(key);
    }

    public void Clear()
    {
        _parameters.Clear();
    }

    private void ApplyParameters(Dictionary<string, object> parameters)
    {
        _parameters.Clear();

        if (parameters == null)
        {
            return;
        }

        foreach (KeyValuePair<string, object> pair in parameters)
        {
            _parameters[pair.Key] = pair.Value;
        }
    }

    public void TransitionScene(string sceneName, Dictionary<string, object> parameters = null)
    {
        if (IsBusy)
        {
            return;
        }

        _transitionSceneCoroutine = StartCoroutine(TransitionSceneCoroutine(sceneName, parameters));
    }

    private IEnumerator TransitionSceneCoroutine(string sceneName, Dictionary<string, object> parameters)
    {
        ApplyParameters(parameters);

        while (_sceneStack.Count > 0)
        {
            Scene scene = _sceneStack.Pop();

            if (scene.isLoaded)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(scene);
                while (!unloadOperation.isDone)
                {
                    yield return null;
                }
            }
        }

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);

        _sceneStack.Push(loadedScene);

        _transitionSceneCoroutine = null;
    }



    public void PushScene(string sceneName, Dictionary<string, object> parameters = null)
    {
        if (IsBusy)
        {
            return;
        }

        _pushSceneCoroutine = StartCoroutine(PushSceneCoroutine(sceneName, parameters));
    }

    private IEnumerator PushSceneCoroutine(string sceneName, Dictionary<string, object> parameters)
    {
        ApplyParameters(parameters);

        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(loadedScene);

        _sceneStack.Push(loadedScene);

        _pushSceneCoroutine = null;
    }

    public void PopScene(Dictionary<string, object> parameters = null)
    {
        if (IsBusy)
        {
            return;
        }

        _popSceneCoroutine = StartCoroutine(PopSceneCoroutine(parameters));
    }

    private IEnumerator PopSceneCoroutine(Dictionary<string, object> parameters)
    {
        if (_sceneStack.Count <= 1)
        {
            yield break;
        }

        ApplyParameters(parameters);

        Scene unloadScene = _sceneStack.Pop();

        if (unloadScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(unloadScene);
        }

        SceneManager.SetActiveScene(_sceneStack.Peek());

        _popSceneCoroutine = null;
    }
}
