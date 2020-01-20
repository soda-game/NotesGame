using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiLib
{
    class MidiSystem
    {
        //チャンクデータ
        struct HeaderChunkData
        {
            public byte[] chunkID; //MThd
            public int dataLength; //ヘッダの長さ
            public short format; //フォーマット
            public short tracks; //トラック数
            public short timeBase; //分解能
        }
        struct TrackChunkData
        {
            public byte[] chunkID; //MTrk
            public int dataLength; //トラックのデータ長
            public byte[] data; //演奏データ
        }

        //音げーに必要なものたち
        public enum NoteType
        {
            ON, OFF
        }
        public struct NoteData
        {
            public float tickTime;
            public float msTime;
            public int leanNum; //音階
            public NoteType type;
            public int ch; //チャンネル 色分け用
        }
        public static List<NoteData> noteDataList = new List<NoteData>();

        public struct TempData
        {
            public float tickTime;
            public float msTime;
            public float bpm;
            public float tick;
        }
        public static List<TempData> tempDataList = new List<TempData>();

        //main
        public static void ReadMidi(string filePath)
        {
            noteDataList.Clear();
            tempDataList.Clear();

            //ファイル読み込み 読み込み終わるまで出ない!
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(file))
            {
                //-------- ヘッダ解析 -------
                var headerChunk = new HeaderChunkData();

                //チャンクID
                headerChunk.chunkID = reader.ReadBytes(4);
                //リトルエンディアンなら逆に
                if (BitConverter.IsLittleEndian)
                {
                    //データ長
                    var bytePick = reader.ReadBytes(4);
                    Array.Reverse(bytePick);
                    headerChunk.dataLength = BitConverter.ToInt32(bytePick, 0);

                    //フォーマット
                    bytePick = reader.ReadBytes(2);
                    Array.Reverse(bytePick);
                    headerChunk.format = BitConverter.ToInt16(bytePick, 0);

                    //トラック数
                    bytePick = reader.ReadBytes(2);
                    Array.Reverse(bytePick);
                    headerChunk.tracks = BitConverter.ToInt16(bytePick, 0);

                    //分解能
                    bytePick = reader.ReadBytes(2);
                    Array.Reverse(bytePick);
                    headerChunk.timeBase = BitConverter.ToInt16(bytePick, 0);
                }
                else
                {
                    //データ長
                    headerChunk.dataLength = BitConverter.ToInt32(reader.ReadBytes(4), 0);
                    //フォーマット
                    headerChunk.format = BitConverter.ToInt16(reader.ReadBytes(2), 0);
                    //トラック数
                    headerChunk.tracks = BitConverter.ToInt16(reader.ReadBytes(2), 0);
                    //分解能
                    headerChunk.timeBase = BitConverter.ToInt16(reader.ReadBytes(2), 0);
                }
                //***ヘッダーテスト用
                HeaderTestLog(headerChunk);


                //-------- トラック解析 -------
                TrackChunkData[] trackChunks = new TrackChunkData[headerChunk.tracks]; //トラック数分 作る

                //トラック数分回す
                for (int i = 0; i < trackChunks.Length; i++)
                {
                    //チャンクID
                    trackChunks[i].chunkID = reader.ReadBytes(4);

                    if (BitConverter.IsLittleEndian)
                    {
                        //データ長
                        var bytePick = reader.ReadBytes(4);
                        Array.Reverse(bytePick);
                        trackChunks[i].dataLength = BitConverter.ToInt32(bytePick, 0);
                    }
                    else
                    {
                        //データ長
                        trackChunks[i].dataLength = BitConverter.ToInt32(reader.ReadBytes(4), 0);
                    }

                    //演奏データ
                    trackChunks[i].data = reader.ReadBytes(trackChunks[i].dataLength);
                    //***トラックテスト用
                    TrackTestLog(trackChunks[i]);
                    //演奏データ解析へ
                    TrackDataAnaly(trackChunks[i].data, headerChunk);
                }

            }

            MstimeFix(ref tempDataList, ref noteDataList);

            //テンポ確認用
            TempTestLog(tempDataList);
            //***Notes確認用
            NoteTestLog(noteDataList);


        }

        static void TrackDataAnaly(byte[] data, HeaderChunkData header)
        {
            //トラック内で引き継ぎたいもの
            uint tickTime = 0; //開始からのTick数
            byte statusByte = 0; //FFとか入る
            uint Instrument = 0; //楽器

            //データ分
            for (int i = 0; i < data.Length;)
            {
                //---デルタタイム---
                uint delta = 0;
                while (true)
                {
                    byte bytePick = data[i++];
                    delta |= bytePick & (uint)0x7f;

                    if ((bytePick & 0x80) == 0) break;

                    delta = delta << 7;
                }
                tickTime += delta;

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

                //ステバ分岐 
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
                                NoteData noteData = new NoteData();
                                noteData.tickTime = (int)tickTime;
                                noteData.msTime = 0; //後でやる
                                noteData.leanNum = (int)leanNum;
                                noteData.ch = statusByte & 0x0f; //下4を取得

                                //ベロ値でオンオフを送ってくる奴に対応
                                if (velocity > 0) //音が鳴っていたらオン
                                    noteData.type = NoteType.ON;
                                else
                                    noteData.type = NoteType.OFF;

                                noteDataList.Add(noteData);
                            }
                            break;

                        case 0x80: //ノートオフ
                            {
                                byte leanNum = data[i++];
                                byte velocity = data[i++];

                                NoteData noteData = new NoteData();
                                noteData.tickTime = (int)tickTime;
                                noteData.msTime = 0;
                                noteData.leanNum = (int)leanNum;
                                noteData.ch = statusByte & 0x0f; //下4を取得
                                noteData.type = NoteType.OFF; //オフしか来ない


                                noteDataList.Add(noteData);
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
                    byte dataLen = data[i++]; //ホントは可変長***

                    switch (eveNum)
                    {
                        case 0x51:
                            {
                                TempData tempData = new TempData();
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
                                tempData.bpm = 60000000 / (float)temp;
                                //小数点第１位切り捨て
                                tempData.bpm = (float)Math.Floor(tempData.bpm * 10) / 10;
                                //tick値=60/分解能*1000
                                tempData.tick = (60 / tempData.bpm / header.timeBase * 1000);
                                tempDataList.Add(tempData);
                            }
                            break;

                        default:
                            i += dataLen; //メタはデータ長で全てとばせる 書くの面倒だった
                            break;
                    }
                }
            }

        }

        static void MstimeFix(ref List<TempData> tL, ref List<NoteData> nL)
        {
            var beforTempList = new List<TempData>(tL); //変更前の値を保存

            //--テンポのmsTime修正--
            for (int i = 1; i < tL.Count; i++)
            {
                TempData tempData = tL[i]; //現在対象のデータ

                float timeDiff = tempData.tickTime - tL[i - 1].tickTime;
                tempData.msTime = timeDiff * tL[i - 1].tick + tL[i - 1].msTime;

                tL[i] = tempData;
            }

            //--ノーツのmsTime修正--
            for (int i = 0; i < nL.Count; i++)
            {
                for (int j = tL.Count - 1; j >= 0; j--)
                {
                    if (nL[i].tickTime >= tL[j].tickTime) //テンポ変更した後
                    {
                        NoteData note = nL[i];

                        float timeDifference = nL[i].tickTime - tL[j].tickTime;
                        note.msTime = timeDifference * tL[j].tick + tL[j].msTime;   // 計算後のテンポ変更イベント時間+そこからの自分の時間
                        nL[i] = note;
                        break;
                    }
                }
            }
        }

        static void HeaderTestLog(HeaderChunkData h)
        {
            Console.WriteLine(
                "チャンクID：" + (char)h.chunkID[0] + (char)h.chunkID[1] + (char)h.chunkID[2] + (char)h.chunkID[3] + "\n" +
                "データ長：" + h.dataLength + "\n" +
                "フォーマット：" + h.format + "\n" +
                "トラック数：" + h.tracks + "\n" +
                "分解能：" + h.timeBase + "\n");
        }
        static void TrackTestLog(TrackChunkData t)
        {
            Console.WriteLine(
                 "チャンクID：" + (char)t.chunkID[0] + (char)t.chunkID[1] + (char)t.chunkID[2] + (char)t.chunkID[3] + "\n" +
                 "データ長：" + t.dataLength + "\n");
        }

        static void NoteTestLog(List<NoteData> nList)
        {
            foreach (NoteData n in nList)
            {
                Console.WriteLine(
                    "開始から:" + n.tickTime + "Tick\n" +
                    "開始から:" + n.msTime + "ms\n" +
                    "音階:" + n.leanNum + "\n" +
                    "タイプ:ノート" + n.type.ToString() + "\n" +
                    "チャンネル:" + n.ch + "\n");
            }
        }

        static void TempTestLog(List<TempData> tList)
        {
            foreach (TempData t in tList)
            {
                Console.WriteLine(
                "開始から:" + t.tickTime + "Tick\n" +
                "開始から:" + t.msTime + "ms\n" +
                "BPM値:" + t.bpm + "\n" +
                "１秒=:" + t.tick + "Tick\n");
            }
        }
    }
}
