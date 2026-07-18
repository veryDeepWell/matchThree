using UnityEngine;

public class LinkButton : MonoBehaviour
{
    [SerializeField] private string url;

    public void OpenLink()
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Debug.LogWarning($"{name}: ссылка не задана.");
        }
    }
}