using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotesView : MonoBehaviour
{
    readonly Vector3 ver = new Vector2(0, -1);
    float speed = 0;
    float bottom = 0;

    // Start is called before the first frame update
    void Start()
    {

    }
    public void SetValue(float speed, float bottom)
    {
        this.speed = speed; this.bottom = bottom;
    }

    // Update is called once per frame
    void Update()
    {
        this.gameObject.transform.position += ver * speed * Time.deltaTime;

        //カメラ下部より下だったら消す
        if (transform.position.y + gameObject.transform.localScale.y / MySystem.H < bottom)
        {
            Destroy(this.gameObject);
        }
    }
}
