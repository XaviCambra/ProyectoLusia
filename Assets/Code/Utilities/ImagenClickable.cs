using UnityEngine;
using UnityEngine.UI;
using System;

public class ImagenClickable : MonoBehaviour
{
    public Action onClicked;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            onClicked?.Invoke();
        });
    }
}
