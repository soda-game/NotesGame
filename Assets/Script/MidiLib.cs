using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MidiLib
{
    class MidiSystem
    {
        const float H = 2;
        public const int NON = -1;

        const int LEAN_BASE = 60; //中央ド
        const int SECOND_BASE = 60; //中央ド

        //チャンクデータ
        public struct HeaderChunkData
        {
            public byte[] chunkID; //MThd
            public int dataLength; //ヘッダの長さ
            public short format; //フォーマット
            public short tracks; //トラック数
            public short timeBase; //分解能
        }
        public struct TrackChunkData
        {
            public byte[] chunkID; //MTrk
            public int dataLength; //トラックのデータ長
            public byte[] data; //演奏データ
        }

        //MIDI読み込み時のデータ
        enum NoteType
        {
            ON, OFF
        }
        struct Bfr_NoteData
        {
            public float tickTime;
            public float msTime;
            public int leanNum; //音階
            public NoteType type;
            public int ch; //チャンネル 色分け用
        }
        struct Bfr_TempData
        {
            public float tickTime;
            public float msTime;
            public float bpm;
            public float tick;
        }

        //音ゲーに欲しいデータ
        public struct Aftr_NoteData
        {
            public float msTime;
            public float Length; //ノーツの長さ
            public int leanNum; //音階
            public int ch; //チャンネル 色分け用
        }
        public static List<Aftr_NoteData> a_noteDataList;
        public struct Aftr_TempData
        {
            public float msTime;
            public float speed; //ノーツの速さ
        }
        public static List<Aftr_TempData> a_tempDataList;



        //main--------------------
        public static void ReadMidi(string filePath, int _baseScale, float magniSpeed/*速度倍率*/)
        {
            //リスト生成
            int baseScale = _baseScale; //四分音符の長さ

            var headerData = new HeaderChunkData();
            TrackChunkData[] trackChunks;
            var b_noteDataList = new List<Bfr_NoteData>();
            var b_tempDataList = new List<Bfr_TempData>();

            //ファイル読み込み 読み込み終わるまで出ない!
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(file))
            {
                //-------- ヘッダ解析 -------
                HeaderDataAnaly(ref headerData, reader);

                //-------- トラック解析 -------
                trackChunks = new TrackChunkData[headerData.tracks]; //ヘッダからトラック数を参照

                for (int i = 0; i < trackChunks.Length; i++)  //トラック数分回す
                {
                    TrackDataAnaly(ref trackChunks, reader, i);

                    //演奏データ解析へ
                    TrackMusicAnaly(trackChunks[i].data, headerData, ref b_noteDataList, ref b_tempDataList);
                }
            }

            MstimeFix(ref b_noteDataList, ref b_tempDataList); //曲中のBPM変更に対応

            //欲しいデータに変換
            a_noteDataList = new List<Aftr_NoteData>();
            AftrNoteCreate(b_noteDataList, headerData.timeBase, baseScale);

            a_tempDataList = new List<Aftr_TempData>();
            AftrTempCreate(b_tempDataList, baseScale, magniSpeed);

            //以下ログ
            DataTestLog(headerData);
            DataTestLog(trackChunks);
            DataTestLog(b_tempDataList);
            DataTestLog(b_noteDataList);
            DataTestLog(a_noteDataList);
            DataTestLog(a_tempDataList);
        }

        //ヘッダー解析
        static void HeaderDataAnaly(ref HeaderChunkData header, BinaryReader reader)
        {
            //チャンクID
            header.chunkID = reader.ReadBytes(4);
            //リトルエンディアンなら逆に
            if (BitConverter.IsLittleEndian)
            {
                //データ長
                var bytePick = reader.ReadBytes(4);
                Array.Reverse(bytePick);
                header.dataLength = BitConverter.ToInt32(bytePick, 0);

                //フォーマット
                bytePick = reader.ReadBytes(2);
                Array.Reverse(bytePick);
                header.format = BitConverter.ToInt16(bytePick, 0);

                //トラック数
                bytePick = reader.ReadBytes(2);
                Array.Reverse(bytePick);
                header.tracks = BitConverter.ToInt16(bytePick, 0);

                //分解能
                bytePick = reader.ReadBytes(2);
                Array.Reverse(bytePick);
                header.timeBase = BitConverter.ToInt16(bytePick, 0);
            }
            else
            {
                //データ長
                header.dataLength = BitConverter.ToInt32(reader.ReadBytes(4), 0);
                //フォーマット
                header.format = BitConverter.ToInt16(reader.ReadBytes(2), 0);
                //トラック数
                header.tracks = BitConverter.ToInt16(reader.ReadBytes(2), 0);
                //分解能
                header.timeBase = BitConverter.ToInt16(reader.ReadBytes(2), 0);
            }

        }

        //トラック解析 周回対応
        static void TrackDataAnaly(ref TrackChunkData[] tracks, BinaryReader reader, int i)
        {
            //チャンクID
            tracks[i].chunkID = reader.ReadBytes(4);

            if (BitConverter.IsLittleEndian)
            {
                //データ長
                var bytePick = reader.ReadBytes(4);
                Array.Reverse(bytePick);
                tracks[i].dataLength = BitConverter.ToInt32(bytePick, 0);
            }
            else
            {
                //データ長
                tracks[i].dataLength = BitConverter.ToInt32(reader.ReadBytes(4), 0);
            }

            //演奏データ
            tracks[i].data = reader.ReadBytes(tracks[i].dataLength);
        }

        //デルタ（可変長）計算用
        static uint DeltaMath(byte[] data, ref int i)
        {
            uint delta = 0;

            while (true)
            {
                byte bytePick = data[i++]; //1byte取る
                delta |= bytePick & (uint)0x7f; //最初をゼロにして 前のdeltaとくっつける

                if ((bytePick & 0x80) == 0) break; //0なら続かないのでループ抜け

                delta = delta << 7; //次のdeltaのために増設
            }

            return delta;
        }

        //トラック演奏データ解析
        static void TrackMusicAnaly(byte[] data, HeaderChunkData header, ref List<Bfr_NoteData> b_noteL, ref List<Bfr_TempData> b_tmpL)
        {
            //トラック内で引き継ぎたいもの
            uint tickTime = 0; //開始からのTick数
            byte statusByte = 0; //FFとか入る
            uint Instrument = 0; //楽器

            //データ分
            for (int i = 0; i < data.Length;)
            {
                //---デルタタイム---
                tickTime += DeltaMath(data, ref i);

                //---ランニングステータス---
                if (data[i] < 0x80)
                {
                    //***
                }
                else
                {
                    statusByte = data[i++];
                }

                //---ステータスバイト---

                //ステバ分岐 この辺はもう筋肉
                //--Midiイベント--
                if (statusByte >= 0x80 & statusByte <= 0xef)
                {
                    switch (statusByte & 0xf0)
                    {
                        case 0x90://ノートオン
                            {
                                byte leanNum = data[i++]; //音階
                                byte velocity = data[i++]; //音の強さ

                                //ノート情報まとめる 
                                Bfr_NoteData noteData = new Bfr_NoteData();
                                noteData.tickTime = (int)tickTime;
                                noteData.msTime = 0; //後でやる
                                noteData.leanNum = (int)leanNum;
                                noteData.ch = statusByte & 0x0f; //下4を取得

                                //ベロ値でオンオフを送ってくる奴に対応
                                if (velocity > 0) //音が鳴っていたらオン
                                    noteData.type = NoteType.ON;
                                else
                                    noteData.type = NoteType.OFF;

                                b_noteL.Add(noteData);
                            }
                            break;

                        case 0x80: //ノートオフ
                            {
                                byte leanNum = data[i++];
                                byte velocity = data[i++];

                                Bfr_NoteData noteData = new Bfr_NoteData();
                                noteData.tickTime = (int)tickTime;
                                noteData.msTime = 0;
                                noteData.leanNum = (int)leanNum;
                                noteData.ch = statusByte & 0x0f; //下4を取得
                                noteData.type = NoteType.OFF; //オフしか来ない

                                b_noteL.Add(noteData);
                            }
                            break;

                        case 0xc0: //プログラムチェンジ　音色 楽器を変える
                            Instrument = data[i++];
                            break;

                        case 0xa0: //キープッシャー
                            i += 2;
                            break;
                        case 0xb0: //コンチェ
                            i += 2;
                            break;
                        case 0xd0: //チェンネルプレッシャー
                            i += 1;
                            break;
                        case 0xe0: //ピッチベンド
                            i += 2;
                            break;
                    }
                }

                //--システムエクスクルーシブイベント--
                else if (statusByte == 0x70 || statusByte == 0x7f)
                {
                    byte dataLen = data[i++];
                    i += dataLen;
                }

                //--メタイベ--
                else if (statusByte == 0xff)
                {
                    byte eveNum = data[i++];
                    byte dataLen = (byte)DeltaMath(data, ref i); //可変長

                    switch (eveNum)
                    {
                        case 0x51:
                            {
                                Bfr_TempData tempData = new Bfr_TempData();
                                tempData.tickTime = (int)tickTime;
                                tempData.msTime = 0;//後でやる

                                //3byte固定 4分音符の長さをマイクロ秒で
                                uint temp = 0;
                                temp |= data[i++];
                                temp <<= 8;
                                temp |= data[i++];
                                temp <<= 8;
                                temp |= data[i++];

                                //BPM計算 = 60秒のマクロ秒/4分音符のマイクロ秒
                                tempData.bpm = SECOND_BASE * 1000000 / (float)temp;
                                //小数点第１位切り捨て
                                tempData.bpm = (float)Math.Floor(tempData.bpm * 10) / 10;
                                //tick値=60/分解能*1000
                                tempData.tick = (SECOND_BASE / tempData.bpm / header.timeBase * 1000);
                                b_tmpL.Add(tempData);
                            }
                            break;

                        default:
                            i += dataLen; //メタはデータ長で全てとばせる 書くの面倒だった
                            break;
                    }
                }
            }

        }

        //ms修正
        static void MstimeFix(ref List<Bfr_NoteData> b_noteL, ref List<Bfr_TempData> b_tmpL)
        {
            var beforTempList = new List<Bfr_TempData>(b_tmpL); //変更前の値を保存

            //--テンポのmsTime修正--
            for (int i = 1; i < b_tmpL.Count; i++)
            {
                Bfr_TempData tempData = b_tmpL[i]; //現在対象のデータ

                float timeDiff = tempData.tickTime - b_tmpL[i - 1].tickTime;
                tempData.msTime = timeDiff * b_tmpL[i - 1].tick + b_tmpL[i - 1].msTime;

                b_tmpL[i] = tempData;
            }

            //--ノーツのmsTime修正--
            for (int i = 0; i < b_noteL.Count; i++)
            {
                for (int j = b_tmpL.Count - 1; j >= 0; j--)
                {
                    if (b_noteL[i].tickTime >= b_tmpL[j].tickTime) //テンポ変更した後
                    {
                        Bfr_NoteData note = b_noteL[i];

                        float timeDifference = b_noteL[i].tickTime - b_tmpL[j].tickTime;
                        note.msTime = timeDifference * b_tmpL[j].tick + b_tmpL[j].msTime;   // 計算後のテンポ変更イベント時間+そこからの自分の時間
                        b_noteL[i] = note;
                        break;
                    }
                }
            }
        }

        //欲しいデータに変換
        static void AftrNoteCreate(List<Bfr_NoteData> nL, int timeBase, int baseScale)
        {
            for (int i = 0; i < nL.Count; i++)
            {
                //--オンノートを探す--
                if (nL[i].type != NoteType.ON) continue;
                var onNote = nL[i];

                //--長さを決める--
                float length = 0;
                for (int j = i + 1; j < nL.Count; j++)
                {
                    //対応するオフを探す
                    if (!(nL[j].type == NoteType.OFF && onNote.leanNum == nL[j].leanNum && onNote.ch == nL[j].ch)) continue;
                    var offNote = nL[j];
                    //計算
                    float diff = offNote.tickTime - onNote.tickTime;
                    length = diff / timeBase * baseScale;
                    break;
                }

                //--出現位置調整　中央ド(c3)を0とする--
                int leanNum = onNote.leanNum - LEAN_BASE;

                //--リスイン--
                var a_noteData = new Aftr_NoteData { msTime = onNote.msTime, Length = length, leanNum = leanNum, ch = onNote.ch };
                a_noteDataList.Add(a_noteData);
            }
        }
        static void AftrTempCreate(List<Bfr_TempData> b_tmpL, int baseScale, float magniSpeed)
        {
            foreach (var b_tL in b_tmpL)
            {
                //速さ
                float speed = b_tL.bpm / SECOND_BASE * baseScale * magniSpeed;
                //リスイン
                var a_tmpData = new Aftr_TempData { msTime = b_tL.msTime, speed = speed };
                a_tempDataList.Add(a_tmpData);
            }
        }

        //テストログ
        static void DataTestLog(HeaderChunkData h)
        {
            Console.WriteLine(
                "チャンクID：" + (char)h.chunkID[0] + (char)h.chunkID[1] + (char)h.chunkID[2] + (char)h.chunkID[3] + "\n" +
                "データ長：" + h.dataLength + "\n" +
                "フォーマット：" + h.format + "\n" +
                "トラック数：" + h.tracks + "\n" +
                "分解能：" + h.timeBase + "\n");
        }
        static void DataTestLog(TrackChunkData[] tArr)
        {
            foreach (var t in tArr)  //トラック数分回す
            {
                Console.WriteLine(
                 "チャンクID：" + (char)t.chunkID[0] + (char)t.chunkID[1] + (char)t.chunkID[2] + (char)t.chunkID[3] + "\n" +
                 "データ長：" + t.dataLength + "\n");
            }
        }
        static void DataTestLog(List<Bfr_NoteData> nList)
        {
            foreach (Bfr_NoteData n in nList)
            {
                Console.WriteLine(
                    "開始から:" + n.tickTime + "Tick\n" +
                    "開始から:" + n.msTime + "ms\n" +
                    "音階:" + n.leanNum + "\n" +
                    "タイプ:ノート" + n.type.ToString() + "\n" +
                    "チャンネル:" + n.ch + "\n");
            }
        }
        static void DataTestLog(List<Bfr_TempData> tList)
        {
            foreach (Bfr_TempData t in tList)
            {
                Console.WriteLine(
                "開始から:" + t.tickTime + "Tick\n" +
                "開始から:" + t.msTime + "ms\n" +
                "BPM値:" + t.bpm + "\n" +
                "１秒=:" + t.tick + "Tick\n");
            }
        }
        static void DataTestLog(List<Aftr_NoteData> nList)
        {
            foreach (Aftr_NoteData n in nList)
            {
                Console.WriteLine(
                    "開始から:" + n.msTime + "ms\n" +
                    "長さ：" + n.Length + "\n" +
                    "音階:" + n.leanNum + "\n" +
                    "チャンネル:" + n.ch + "\n");
            }
        }
        static void DataTestLog(List<Aftr_TempData> tList)
        {
            foreach (Aftr_TempData t in tList)
            {
                Console.WriteLine(
                "開始から:" + t.msTime + "ms\n" +
                "速さ：" + t.speed + "\n");
            }
        }

        //外部参照系----------------------------------------------------------------------

        //ノーツリストから時間が合うノーツを取り出す
        public static Aftr_NoteData NoteDataPick(int noteNum, float time, int fastSecond)
        {
            if (!(noteNum < a_noteDataList.Count && a_noteDataList[noteNum].msTime / 1000 <= time + fastSecond)) return new Aftr_NoteData { msTime = NON };
            return a_noteDataList[noteNum];
        }
        //テンポリストから(同上
        public static Aftr_TempData TempDataPick(float time)
        {
            return a_tempDataList.Find(n => n.msTime <= time);
        }

        //ちゃんと出現するように差分を求める
        public static float NotesPosition_Y(int noteNum, float time, int fastSecond, float speed, float length)
        {
            return (a_noteDataList[noteNum].msTime / 1000 - (time + fastSecond)) * speed + length / H;
        }

    }
}
