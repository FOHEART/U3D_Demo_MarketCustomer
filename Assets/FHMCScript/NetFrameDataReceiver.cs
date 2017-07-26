using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace FoheartMC
{
    public class NetFrameDataReceiver : MonoBehaviour
    {
        //UDP广播接收端口
        public int UDPPort = 5001;
        //角色列表,需要手动指定
        public FoheartModel[] PlayerList;
        //调试文本
        string OutText;

        public NetFrameDataReceiver()
        {
            UDPPort = 5001;
        }

        //初始化
        void Start()
        {
            initRec();
        }

        //UI界面
        void OnGUI()
        {
            GUI.Label(new Rect(0, 0, 100, 50), OutText);
        }
        //UDP接收线程
        Thread thrUDPRecv;
        //UDP接收端
        UdpClient udpReceiver;

        //初始化接收器
        void initRec()
        {
            udpReceiver = new UdpClient(new IPEndPoint(IPAddress.Any, UDPPort));
            thrUDPRecv = new Thread(mcReceiveData);
            thrUDPRecv.Start();
        }

        //接收数据的工作线程
        void mcReceiveData()
        {
            //接收广播
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
            ActorFrameData frameDataTemp = new ActorFrameData();
            while (true)
            {
                try
                {
                    byte[] data = udpReceiver.Receive(ref endpoint);
                    int dataErro = frameDataTemp.deComposeData(data);
                    if (dataErro != 0)
                    {
                        Debug.Log("Data Erro:" + dataErro);
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.ToString());
                }

                foreach (FoheartModel model in PlayerList)
                {
                    if (string.Equals(model.ActorName, frameDataTemp.strActorName))
                    {
                        model.copyData(frameDataTemp);
                    }
                }
            }
        }
        //脚本退出时,停止线程
        void OnApplicationQuit()
        {
            thrUDPRecv.Abort();
            Debug.Log("OnApplicationQuit...");
        }

        void OnDisable()
        {
            //结束接收
            udpReceiver.Close();
        }
        //查找
        FoheartModel getConnectModel(uint packNumber)
        {
            if (packNumber == 0)
            {
                return null;
            }
            //遍历已经连接的模型
            foreach (FoheartModel player in PlayerList)
            {
                if (player && player.ConnectPackNumber == packNumber)
                {
                    return player;
                }
            }
            //如果没有连接的模型,则指定一个,并保存下来
            foreach (FoheartModel player in PlayerList)
            {
                if (player.ConnectPackNumber == 0)
                {
                    player.ConnectPackNumber = packNumber;
                    return player;
                }
            }
            //没有找到可用的模型,则返回空
            return null;
        }
    }
}
