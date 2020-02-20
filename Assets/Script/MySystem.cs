using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MySystem : MonoBehaviour
{
    public const int H = 2;
    // Start is called before the first frame update
    void Start()
    {
        Screen.SetResolution(600, 400, false, 60); //ビルド時のscreenサイズ固定
    }

    // Update is called once per frame
    void Update()
    {

    }

    //画面の左上を習得
    public static Vector3 Get_ScreenTopLeft(Camera camera)
    {
        Vector3 vec = camera.ScreenToWorldPoint(Vector3.zero);
        vec.Scale(new Vector3(1f, -1f, 1f)); //上に行くほど-になるので反転する
        return vec;
    }
    //画面の右下を習得
    public static Vector3 Get_ScreenBottomRight(Camera camera)
    {
        Vector3 vec = camera.ScreenToWorldPoint(new Vector3(camera.pixelWidth, camera.pixelHeight, 0f));
        vec.Scale(new Vector3(1f, -1f, 1f));
        return vec;
    }
}
