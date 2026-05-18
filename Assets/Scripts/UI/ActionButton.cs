using System;
using UnityEngine;

public class ActionButton : MonoBehaviour
{
    public Action OnClick;


    public void UI_Click()
    {
        OnClick?.Invoke();
    }
}
