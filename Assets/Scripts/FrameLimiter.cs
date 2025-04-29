using UnityEngine;

public class FrameLimiter : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private int frameRate = 60;
    void Start()
    {
        // Set the target frame rate to 60
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = frameRate;
    }
}
