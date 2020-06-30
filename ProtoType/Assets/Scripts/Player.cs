using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 
public class Player : MonoBehaviour
{

    [SerializeField]
    private float speed;
    private Vector2 direction;
    private Vector3 min, max;

    void Start()
    {
        direction = Vector2.zero;
    }

    public void SetLimits(Vector3 min, Vector3 max)
    {
        this.min = min;
        this.max = max;
    }

    void Update()
    {
        GetInput();
        Move();
    }


    public void Move()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void GetInput()
    {
        Vector2 moveVector;

        moveVector.x = Input.GetAxisRaw("Horizontal");
        moveVector.y = Input.GetAxisRaw("Vertical");

        direction = moveVector;
    }
}
