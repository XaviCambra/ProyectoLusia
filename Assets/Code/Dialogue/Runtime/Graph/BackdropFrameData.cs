// Runtime/BackdropFrameData.cs
using UnityEngine;

[System.Serializable]
public class BackdropFrameData
{
    public string title;
    public Color color = new Color(1f, 0.92f, 0.016f, 1f);
    public Rect rect;

    public BackdropFrameData() { }

    public BackdropFrameData(string title, Color color, Rect rect)
    {
        this.title = title;
        this.color = color;
        this.rect = rect;
    }
}
