using UnityEngine;

public class OpenLink : MonoBehaviour
{
    [SerializeField] private string url;

    public void OpenLinkUrl()      //вешаем на кнопку, в инспекторе вписываем ссылку, через onClick вызываем этот метод и радуемся 
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
