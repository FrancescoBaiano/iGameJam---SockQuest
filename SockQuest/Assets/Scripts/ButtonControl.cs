using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonControl : MonoBehaviour
{
    [SerializeField] private string menuLevelName;

    public void ReturnToMenu()
    {
        StartCoroutine(ExitLevel());
    }

    private IEnumerator ExitLevel()
    {
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(menuLevelName);
    }


}