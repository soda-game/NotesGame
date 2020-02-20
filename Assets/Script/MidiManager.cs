using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MidiLib;

public class MidiManager : MonoBehaviour
{
    const int FAST_SECOND = 1; //速くに出現するように
    float thisObj_initY = 0; //出現位置の初期の値（カメラの上部)

    [SerializeField] Text text;
    [SerializeField] Camera camera;
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject notes;
    [SerializeField] string filePath;

    string midiPath => Application.streamingAssetsPath + filePath;

    bool isPlay = false;
    float startTime = 0;
    int now_noteNum = 0; //リストのインデックス番号 for使いたくなかったので
    [SerializeField] int BASE_SCALE = 4; //４分音符の大きさ
    [SerializeField] float magniSpead = 1f; //速度倍率

    Color[] colors = new Color[] { Color.blue, Color.red, Color.yellow, new Color(1, 0.52f, 0, 1)/*オレンジ*/, Color.green, Color.white };

    void Start()
    {
        MidiSystem.ReadMidi(midiPath, BASE_SCALE, magniSpead);
        text.text = "Spaceキーで再生";
        thisObj_initY = MySystem.Get_ScreenTopLeft(camera).y;
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
        //ノーツ
        MidiSystem.Aftr_NoteData note_pick = MidiSystem.NoteDataPick(now_noteNum, Time.time - startTime, FAST_SECOND);
        if (note_pick.msTime == MidiSystem.NON) return;
        //テンポ
        var temp_pick = MidiSystem.TempDataPick(Time.time - startTime);

        //--生成--
        //位置
        this.transform.position = new Vector3(transform.position.x, thisObj_initY + temp_pick.speed * FAST_SECOND, transform.position.z); //速く出現する時の初期位置 速さによってn秒前の場所が変わるので
        float noteY = MidiSystem.NotesPosition_Y(now_noteNum, Time.time - startTime, FAST_SECOND, temp_pick.speed, note_pick.Length);

        var noteInst = Instantiate(notes, new Vector3(transform.position.x + note_pick.leanNum, transform.position.y + noteY, transform.position.z), Quaternion.identity);
        noteInst.gameObject.GetComponent<NotesView>().SetValue(temp_pick.speed, MySystem.Get_ScreenBottomRight(camera).y);
        noteInst.gameObject.transform.localScale = new Vector3(transform.localScale.x, note_pick.Length, transform.localScale.z);

        //色
        if (note_pick.ch >= colors.Length)  //配列以上は無色
            noteInst.GetComponent<SpriteRenderer>().color = colors[colors.Length - 1];
        else
            noteInst.GetComponent<SpriteRenderer>().color = colors[note_pick.ch];

        now_noteNum++;
    }
}
