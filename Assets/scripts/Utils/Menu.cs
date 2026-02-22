using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public void play()
    {
        SceneManager.LoadScene(0);
    }
    public void settings()
    {
        SceneManager.LoadScene(2);
    }
    public void backmenu()
    {
        SceneManager.LoadScene(1);
    }
    public void quite()
    {
        Application.Quit();
    }
}