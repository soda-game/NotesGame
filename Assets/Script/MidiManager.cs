using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiLib;

public class MidiManager : MonoBehaviour
{
    // Start is called before the first frame update7
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject notes;
    [SerializeField] string filePath ;
    bool isPlay = false;
    float startTime;

    int now_noteNum = 0;

    void Start()
    {
        MidiSystem.ReadMidi(filePath);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            audioSource.Play();
            isPlay = true;
            startTime = Time.time;
        }

        if (!isPlay || now_noteNum >= MidiSystem.noteDataList.Count) return;

        if (MidiSystem.noteDataList[now_noteNum].msTime / 1000 <= Time.time - startTime )
        {
            if (MidiSystem.noteDataList[now_noteNum].type == MidiSystem.NoteType.ON)
            {
                int leanPos = (int)((MidiSystem.noteDataList[now_noteNum].leanNum - 60));
                Instantiate(notes, new Vector3(transform.position.x + leanPos, transform.position.y, 0), Quaternion.identity);
            }
            now_noteNum++;
        }
    }
}
