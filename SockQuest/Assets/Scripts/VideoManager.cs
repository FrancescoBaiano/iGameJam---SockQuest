using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoManager : MonoBehaviour
{
    [Header("Scena successiva")]
    [SerializeField] private string sceneToLoad = "Livello1";

    private VideoPlayer videoPlayer;
    [SerializeField] private string videoName;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    void Start()
    {
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, videoName);
        videoPlayer.url = videoPath;
        videoPlayer.Play();
    }

    private void OnEnable()
    {
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void OnDisable()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}