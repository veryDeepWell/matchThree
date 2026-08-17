using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MainMenuCheatCodeListener : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string SecretWord = "выхухоль";

    private readonly StringBuilder _typedCharacters = new StringBuilder();
    private GameObject _cheatPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MainMenuSceneName)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform cheatPanel = FindDescendant(root.transform, "CheatPanel");
            if (cheatPanel == null)
                continue;

            MainMenuCheatCodeListener listener = root.GetComponent<MainMenuCheatCodeListener>();
            if (listener == null)
                listener = root.AddComponent<MainMenuCheatCodeListener>();
            listener.Configure(cheatPanel.gameObject);
            return;
        }
    }

    private void Configure(GameObject cheatPanel)
    {
        _cheatPanel = cheatPanel;
        _typedCharacters.Clear();
        _cheatPanel.SetActive(false);
    }

    private void Update()
    {
        if (_cheatPanel == null || _cheatPanel.activeSelf || string.IsNullOrEmpty(Input.inputString))
            return;

        foreach (char character in Input.inputString)
        {
            char normalized = NormalizeKeyboardCharacter(character);
            if (normalized == '\0')
                continue;

            _typedCharacters.Append(normalized);
            while (_typedCharacters.Length > SecretWord.Length)
                _typedCharacters.Remove(0, 1);

            if (_typedCharacters.ToString() != SecretWord)
                continue;

            _typedCharacters.Clear();
            _cheatPanel.SetActive(true);
            SoundManager.PlayButtonClick();
            break;
        }
    }

    private static char NormalizeKeyboardCharacter(char character)
    {
        char lower = char.ToLowerInvariant(character);

        // Английские символы переводятся в русские буквы, расположенные на тех же клавишах.
        switch (lower)
        {
            case 'd': return 'в';
            case 's': return 'ы';
            case '[': return 'х';
            case 'e': return 'у';
            case 'j': return 'о';
            case 'k': return 'л';
            case 'm': return 'ь';
        }

        return SecretWord.IndexOf(lower) >= 0 ? lower : '\0';
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;
        for (int index = 0; index < root.childCount; index++)
        {
            Transform found = FindDescendant(root.GetChild(index), objectName);
            if (found != null)
                return found;
        }
        return null;
    }
}
