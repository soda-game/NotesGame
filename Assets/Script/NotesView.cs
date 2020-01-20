using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotesView : MonoBehaviour
{
    readonly Vector3 ver = new Vector2(0, -1);
    [SerializeField]  float speed = 0.2f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        this.gameObject.transform.position += ver * speed;
        if (transform.position.y < -10) { Destroy(this.gameObject); }
    }
}
