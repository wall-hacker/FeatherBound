using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Helicopter : MonoBehaviour
{
    public void LoadSunTempleZombies()
    {
        SceneManager.LoadScene("SunTempleZombies");
    }

    public void LoadSunTempleCollection()
    {
        SceneManager.LoadScene("SunTempleCollection");
    }

    public void LoadFloodedCityZombies()
    {
        SceneManager.LoadScene("FloodedCityZombies");
    }

    public void LoadFloodedCityCollection()
    {
        SceneManager.LoadScene("FloodedCityCollection");
    }
}
