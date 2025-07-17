using UnityEngine;

public class FrameLimiter : MonoBehaviour
{
    /// <summary>
    /// Melimit frame rate game ke 60 FPS
    /// Limit dilakukan agar kalkulasi CPU yang digunakan sesuai dengan target FPS
    /// </summary>

    [SerializeField] private int frameRate = 60;
    private int vSyncValue = 0;
    void Start()
    {
        QualitySettings.vSyncCount = vSyncValue;
        Application.targetFrameRate = frameRate;
    }
}
