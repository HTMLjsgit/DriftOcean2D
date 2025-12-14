using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    [SerializeField] private float _speed;
    private Rigidbody2D _rigid;
    private ObstaclesSpawner _obstaclesSpawner;

    void Start()
    {
        _obstaclesSpawner = ObstaclesSpawner.instance;
        _rigid = GetComponent<Rigidbody2D>();
    }

    public void Move(float speed)
    {
        _rigid = GetComponent<Rigidbody2D>();
        _speed = speed;
        _rigid.linearVelocity = new Vector2(-1 * _speed, 0); 
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("ObstacleDestroy")) 
        {
            _obstaclesSpawner.ObstacleListRemove(this.gameObject);
            Destroy(this.gameObject);
        }
    }
}