using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RestartScript : MonoBehaviour
{
    [SerializeField]
    private Button button;

    void Start()
    {
        //rasj: on button click, load scene
        button.onClick.AddListener(() => 
        {
            button.gameObject.SetActive(false);
            LoadScene();
        });
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(0);  //rasj: 0 is the Level Generation 1 scene in build settings
    }
}
