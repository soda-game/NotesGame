using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField] float speed = 0.05f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    void Move()
    {
        Vector3 ver = Vector3.zero;
        if (Input.GetKey(KeyCode.A))
            ver.x = -1;
        if (Input.GetKey(KeyCode.D))
            ver.x = 1;
        if (Input.GetKey(KeyCode.W))
            ver.y = 1;
        if (Input.GetKey(KeyCode.S))
            ver.y = -1;

        this.gameObject.transform.position += ver.normalized * speed*Time.deltaTime;
    }
}
