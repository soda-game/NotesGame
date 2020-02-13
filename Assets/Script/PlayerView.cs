using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField] float speed = 1f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Dash();
    }

    void Move()
    {
        Vector3 ver = Vector3.zero;
        ver.x = Input.GetAxis("Horizontal") * speed;
        ver.y = Input.GetAxis("Vertical") * speed;

        this.gameObject.transform.Translate(ver.x, ver.y, 0f);
    }

    void Dash()
    {
        if (Input.GetButtonDown("Dash"))
            Debug.Log("ダッシュ！");
    }
}
