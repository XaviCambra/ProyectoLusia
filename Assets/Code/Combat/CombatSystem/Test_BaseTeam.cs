using System.Collections.Generic;
using UnityEngine;

public class Test_BaseTeam : MonoBehaviour
{
    #region SINGLETON
    public static Test_BaseTeam Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    public List<Character> characterList;
}
