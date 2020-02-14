﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MidiLib;

public class MidiManager : MonoBehaviour
{
    // Start is called before the first frame update7
    const float H = 2;
    const int FAST_SECOND = 1; //速くに出現するように
    float thisObj_initY = 0; //出現位置の初期の値（カメラの上部）***

    [SerializeField] Text text;
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject notes;
    [SerializeField] string filePath; //フォーマット0にしてください***

    string midiPath => Application.streamingAssetsPath + filePath;

    bool isPlay = false;
    float startTime = 0;
    int now_noteNum = 0; //リストのインデックス番号 for使いたくなかったので
    [SerializeField] int BASE_SCALE = 10; //４分音符の大きさ

    Color[] colors = new Color[] { Color.blue, Color.red, Color.yellow, new Color(1, 0.52f, 0, 1)/*オレンジ*/, Color.green, Color.white };

    void Start()
    {
        MidiSystem.ReadMidi(midiPath, BASE_SCALE);
        text.text = "Spaceキーで再生";
        thisObj_initY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        //spaceで再生
        if (Input.GetKeyDown(KeyCode.Space) && !isPlay)
        {
            audioSource.Play();
            isPlay = true;
            startTime = Time.time;
            text.text = "再生中！！";
            now_noteNum = 0;
        }
        //曲が再生されてない
        if (audioSource.time == 0.0f && !audioSource.isPlaying)
        {
            text.text = "Spaceキーで再生";
            isPlay = false;
            return;
        }


        //--経過時間からリストを参照--
        float musicTime = Time.time - startTime;

        //ノーツリスト
        if (!(now_noteNum < MidiSystem.a_noteDataList.Count && MidiSystem.a_noteDataList[now_noteNum].msTime / 1000 <= musicTime + FAST_SECOND)) return;
        var note_pick = MidiSystem.a_noteDataList[now_noteNum];
        now_noteNum++;

        //テンポリスト
        var temp_pick = MidiSystem.a_tempDataList.Find(n => n.msTime <= musicTime);
        this.transform.position = new Vector3(transform.position.x, thisObj_initY + temp_pick.speed * FAST_SECOND, transform.position.z); //速さによってn秒前の場所が変わるので

        //--生成--
        float noteY = (MidiSystem.a_noteDataList[now_noteNum].msTime / 1000 - (musicTime + FAST_SECOND)) * temp_pick.speed; //動作が遅くなってもちゃんと出現するように
        var noteInst = Instantiate(notes, new Vector3(transform.position.x + note_pick.leanNum, transform.position.y + noteY + note_pick.Length / H, transform.position.z), Quaternion.identity);

        noteInst.gameObject.GetComponent<NotesView>().Speed = temp_pick.speed;
        noteInst.gameObject.transform.localScale = new Vector3(transform.localScale.x, note_pick.Length, transform.localScale.z);

        if (note_pick.ch >= colors.Length)  //配列以上は無色
            noteInst.GetComponent<SpriteRenderer>().color = colors[colors.Length - 1];
        else
            noteInst.GetComponent<SpriteRenderer>().color = colors[note_pick.ch];

    }
}
