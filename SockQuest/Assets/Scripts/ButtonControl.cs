using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonControl : MonoBehaviour
{
    [SerializeField] private string menuLevelName;
    private IEnumerator ExitLevel()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(menuLevelName);
    }

}