using UnityEngine;

[CreateAssetMenu(fileName = "ObstacleData", menuName = "ScriptableObject/ObstacleData")]
public class ObstacleData : ScriptableObject
{
    public string name;         
    public GameObject prefab;   
    public float baseSpeed;    
}