using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ActionButton : MonoBehaviour
{
    public Action OnClick;


    public void UI_Click()
    {
        OnClick?.Invoke();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Level-1 1");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
