using UnityEngine;

public class ObstacleDiscoveryMarker : MonoBehaviour
{
    public ObstacleData Data { get; private set; }

    public void Initialize(ObstacleData data)
    {
        Data = data;
    }
}
