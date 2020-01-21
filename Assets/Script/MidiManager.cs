using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MidiLib;

public class MidiManager : MonoBehaviour
{
    // Start is called before the first frame update7
    const float H = 2;

    [SerializeField] Text text;
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject notes;
    [SerializeField] string filePath; //フォーマット0にしてください***

    bool isPlay = false;
    float startTime = 0;
    int now_noteNum = 0; //リストのインデックス番号 for使いたくなかったので
    const int FOUR_TICK = 480; //四分音符***ヘッダを参照
    [SerializeField] float BASE_SCALE; //４分音符の大きさ //***多分速さに比例するんだろうけど分からん

    Color[] colors = { Color.blue, Color.red, new Color(1, 0.3897537f, 0, 1), Color.green, Color.green, Color.white }; //６ch以上はむ属性
    [SerializeField] float alpha;

    void Start()
    {
        MidiSystem.ReadMidi(filePath);
        text.text = "Spaceキーで再生";
        for (int i = 0; i < colors.Length; i++) colors[i].a = alpha;
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


        //経過時間からリストを参照
        if (now_noteNum >= MidiSystem.noteDataList.Count) return;
        if (MidiSystem.noteDataList[now_noteNum].msTime / 1000 <= Time.time - startTime)
        {
            //ピアノロールっぽく
            var onNote = MidiSystem.noteDataList[now_noteNum];
            if (onNote.type == MidiSystem.NoteType.ON)
            {
                //長さを決める
                float scale = 0;
                //offを探す Libに入れた方がいいかも？***
                for (int i = now_noteNum + 1; i < MidiSystem.noteDataList.Count; i++)
                {
                    var offNote = MidiSystem.noteDataList[i];
                    if (onNote.leanNum == offNote.leanNum && onNote.ch == offNote.ch && offNote.type == MidiSystem.NoteType.OFF)
                    {
                        //計算
                        float diff = (offNote.tickTime - onNote.tickTime);
                        scale = (diff / FOUR_TICK) * BASE_SCALE; //四分音符をBASEとする
                        break;
                    }
                }

                //出現位置を求める
                int leanPos = (int)onNote.leanNum - 60;
                float noteSpeed = notes.GetComponent<NotesView>().Speed;
                float noteY = (onNote.msTime / 1000 - (Time.time - startTime)) * noteSpeed; //動作が遅くなってもちゃんと出現するように

                //--生成--
                var noteInst = Instantiate(notes, new Vector3(transform.position.x + leanPos, transform.position.y + noteY + scale / H, transform.position.z), Quaternion.identity); ;
                noteInst.gameObject.transform.localScale = new Vector3(transform.localScale.x, scale, transform.localScale.z);

                if (onNote.ch >= colors.Length)
                    noteInst.GetComponent<SpriteRenderer>().color = colors[0];
                else
                    noteInst.GetComponent<SpriteRenderer>().color = colors[onNote.ch];
            }

            now_noteNum++;
        }

    }
}
