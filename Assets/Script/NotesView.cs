using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotesView : MonoBehaviour
{
    readonly Vector3 ver = new Vector2(0, -1);
    float speed = 0;
    public float Speed { set { speed = value; } }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //カメラに写ってるか***カメラ参照
        this.gameObject.transform.position += ver * speed * Time.deltaTime;
        if (transform.position.y + gameObject.transform.localScale.y / 2 < -10)
        {
            Destroy(this.gameObject);
        }
    }
}
