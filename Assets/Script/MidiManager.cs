using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MidiLib;

public class MidiManager : MonoBehaviour
{
    // Start is called before the first frame update7
    [SerializeField] Text text;
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject notes;
    [SerializeField] string filePath;

    bool isPlay = false;
    float startTime = 0;
    int now_noteNum = 0; //リストのインデックス番号 for使いたくなかったので

    void Start()
    {
        MidiSystem.ReadMidi(filePath);
        text.text = "Spaceキーで再生";
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
        if (audioSource.time == 0.0f && !audioSource.isPlaying )
        {
            text.text = "Spaceキーで再生";
            isPlay = false;
            return;
        }

        if (now_noteNum >= MidiSystem.noteDataList.Count) return;

        //経過時間からリストを参照
        if (MidiSystem.noteDataList[now_noteNum].msTime / 1000 <= Time.time - startTime)
        {
            if (MidiSystem.noteDataList[now_noteNum].type == MidiSystem.NoteType.ON)
            {
                int leanPos = (int)((MidiSystem.noteDataList[now_noteNum].leanNum - 60)); //レーン番号をピアノロールっぽく使う
                Instantiate(notes, new Vector3(transform.position.x + leanPos, transform.position.y, 0), Quaternion.identity);
            }
            now_noteNum++;
        }

    }
}
