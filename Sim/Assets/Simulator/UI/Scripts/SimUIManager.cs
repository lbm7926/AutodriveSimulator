using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimUIManager : MonoBehaviour
{
    public GameObject NewSceneGO;

    public void OpenNewScenePanel()
    {
        NewSceneGO.SetActive(true);
    }
}
