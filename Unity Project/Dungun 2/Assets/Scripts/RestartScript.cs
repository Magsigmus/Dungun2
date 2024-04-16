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
            LoadScene();
        });
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(0);  //rasj: 0 Level Generation 1 in build settings
    }

    void Update()
    {
        
    }
}
