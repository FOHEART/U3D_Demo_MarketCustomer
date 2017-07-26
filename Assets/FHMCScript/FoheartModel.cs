using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Xml.Linq;
using System;

namespace FoheartMC
{
    public class FoheartModel : MonoBehaviour
    {
        /**
         * ConnectPackNumber
         * 本模型连接的设备包号.0为没有连接
         * 则该值会在设备连接是自动指定,但可能不是你想要的那个模型,此时需要手动指定包号
         */
        public string ActorName;
        //接受哪个模型发来的数据
        public string ConfigPath;

        //是否使用远程位移
        public Vector3 HipsStartLocation;
        public bool FixHipsLocation;

        string OutText;
        //调试输出的文本,可以在任意时候更改,
        [HideInInspector]
        public uint ConnectPackNumber;
        //链接的包号
        protected Dictionary<uint, string> mapBoneIndex;
        //骨骼编号,对应的骨骼名称
        protected ActorFrameData firstFrameData;
        //第一帧数据
        protected bool bDataIsOK = false;
        //数据是否已经准备好

        protected Dictionary<string, Transform> m_mapNameAndBone;
        //骨骼名称对应的骨骼节点
        protected Quaternion m_bodyStartQuat;
        //模型初始的旋转
        protected Vector3 m_bodyStartPosition;
        //模型初始位置
        ActorFrameData frameDataBuffer;
        //动作缓存

        //构造
        public FoheartModel()
        {
            ActorName = "Actor1(Live)";
            ConfigPath = "MarketCustomerRig.xml";
        }

        //模型初始化
        void Awake()
        {
            CheckCondition();
            mapBoneIndex = new Dictionary<uint, string>();
            m_mapNameAndBone = new Dictionary<string, Transform>();
            frameDataBuffer = new ActorFrameData();
            loadBoneMaps();//初始化使用配置文件绑定
            initNameAndBone();
            ChildAwake();

            Transform HipsStartTrans = FindBone(0);
            HipsStartLocation = new Vector3(HipsStartTrans.position.x, HipsStartTrans.position.y, HipsStartTrans.position.z);
        }


        //检查是否已经指定骨骼绑定文件
        void CheckCondition()
        {
            if (ConfigPath.Length == 0)
            {
                //没有指定骨骼配置文件,请先编辑好配置文件,放置在工程目录下(Assets的父目录)
                //并手动将文件名,填写到ConfigPath中
                Debug.LogError("ConfigPath not specified!");
            }
            if (ActorName.Length == 0)
            {
                //该模型接受的动作角色名称
                Debug.LogError("ActorReceiveName not specified!");
            }
        }

        //继承自本类的组件,初始化,需要重写ChildAwake,来达到初始化效果,虚函数
        public virtual void ChildAwake()
        {
        }

        //准备开始
        void Start()
        {
            m_bodyStartQuat = transform.rotation;
        }

        //UI界面
        void OnGUI()
        {
            //调试输出的内容
            GUI.Label(new Rect(0, 10, 500, 300), OutText);
        }

        //从mapBoneIndex中初始化
        void initNameAndBone()
        {
            List<Transform> list = new List<Transform>();
            list.AddRange(gameObject.GetComponentsInChildren<Transform>());
            foreach (var deFineBone in mapBoneIndex)
            {
                bool bFind = false;
                foreach (Transform BoneObj in list)
                {
                    if (BoneObj.name == deFineBone.Value)
                    {
                        m_mapNameAndBone.Add(BoneObj.name, BoneObj);
                        bFind = true;
                        break;
                    }
                }
                if (!bFind)
                {
                    print(deFineBone.Value + " not found in the model!");
                }
            }
        }

        // !!!弃用
        //保存骨骼绑定数据
        void saveBoneMaps()
        {
            //FileStream stream = File.Open("./FoheartMC.dat", FileMode.OpenOrCreate);
            //BinaryFormatter binFormat = new BinaryFormatter();
            //binFormat.Serialize(stream, mapBoneIndex);
            //stream.Close();
            //FileStream fs = new FileStream("./FoheartMC.dat", FileMode.Open);
            //BinaryFormatter bf = new BinaryFormatter();
            //mapBoneIndex = bf.Deserialize(fs) as Dictionary<uint, string>;
            //fs.Close();
        }

        //从配置文件加载骨骼映射
        void loadBoneMaps()
        {
            try
            {
                XDocument doc = XDocument.Load(/*"E:/UnityRelease/" +*/ ConfigPath);
                foreach (XElement element in doc.Element("ActorBones").Elements())
                {
                    uint BoneID = Convert.ToUInt32(element.Attribute("ConnectId").Value);
                    mapBoneIndex.Add(BoneID, element.Attribute("name").Value);
                }
            }
            catch (Exception e)
            {
                OutText = "@num" + e.ToString();
            }
        }

        //this function called by The UDP thread
        public void copyData(ActorFrameData data)
        {
            if (firstFrameData == null)
            {
                firstFrameData = new ActorFrameData();
                data.CopyTo(ref firstFrameData);
                return;
            }
            lock (frameDataBuffer)
            {
                data.CopyTo(ref frameDataBuffer);
                bDataIsOK = true;
            }
        }

        // Update is called once per frame after Update
        void Update()
        {
            lock (frameDataBuffer)
            {
                if (!bDataIsOK)
                {
                    return;
                }
                applyBoneRotations(frameDataBuffer);
            }
        }

        public virtual void applyBoneRotations(ActorFrameData data)
        {

            foreach (var BoneR in data.boneRotQuat)
            {
                Transform BoneT = FindBone(BoneR.Key);
                if (BoneT)
                {
                    var BV = BoneR.Value;
                    if (BoneR.Key == 0)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.z, BV.x, BV.y, BV.w);
                        Quaternion rot = Quaternion.Euler(0.0f, 0.0f, -90.0f);
                        BoneT.localRotation = rot * convQuatApply;
                        //print("set BoneT pos:" + BoneR.Key + "pos:x y z " + data.location.x + "," + data.location.y + "," + data.location.z);

                        Vector3 LoactionInUnity = new Vector3();
                        UInt32 scale = 1;
                        LoactionInUnity.Set(
                            -data.bonePositions[BoneR.Key].x * scale,
                            data.bonePositions[BoneR.Key].z * scale,
                            -data.bonePositions[BoneR.Key].y * scale);

                        if (FixHipsLocation)
                        {

                        }
                        else
                        {
                            BoneT.transform.position = LoactionInUnity;
                        }
                    }
                    else if (BoneR.Key == 1)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.z, BV.x, BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                    else if (BoneR.Key == 3)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.z, BV.x, BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                    else if (BoneR.Key == 6)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.z, BV.x, BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }

                    /*右胳膊*/
                    if (BoneR.Key == 7)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.x, BV.z, -BV.y, BV.w);
                        Quaternion rot = Quaternion.Euler(180.0f, 0.0f, -90.0f);
                        BoneT.localRotation = rot * convQuatApply;
                    }
                    else if (BoneR.Key == 8)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.x, BV.z, -BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                    else if (BoneR.Key == 9)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.x, BV.z, -BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                    else if (BoneR.Key == 10)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.x, BV.z, -BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }

                    /*左胳膊*/
                    if (BoneR.Key == 11)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.x, -BV.z, BV.y, BV.w);
                        Quaternion rot = Quaternion.Euler(0.0f, 0.0f, 90.0f);
                        BoneT.localRotation = rot * convQuatApply;
                    }
                    else if (BoneR.Key == 12)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.x, -BV.z, BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                    else if (BoneR.Key == 13)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.x, -BV.z, BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                    else if (BoneR.Key == 14)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.x, -BV.z, BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                    /*右腿*/
                    if (BoneR.Key == 15)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.z, -BV.x, -BV.y, BV.w);
                        Quaternion rot = Quaternion.Euler(-180.0f, 0.0f, 0.0f);
                        BoneT.localRotation = rot * convQuatApply;
                    }
                    else if (BoneR.Key == 16)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.z, -BV.x, -BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                    else if (BoneR.Key == 17)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.z, -BV.x, -BV.y, BV.w);
                        Quaternion rot = Quaternion.Euler(0, 75.0f, 0.0f);
                        BoneT.localRotation = rot * Quaternion.Inverse(rot) * convQuatApply * rot;
                    }
                    else if (BoneR.Key == 18)
                    {
                        Quaternion convQuatApply = new Quaternion(BV.z, -BV.x, -BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }

                    /*左腿*/
                    if (BoneR.Key == 19)
                    {
                        Quaternion convQuatApply = new Quaternion(-BV.z, -BV.x, BV.y, BV.w);
                        Quaternion rot = Quaternion.Euler(0, 0.0f, 180.0f);
                        BoneT.localRotation = rot * convQuatApply;
                    }
                    else if (BoneR.Key == 20)
                    {
                        Quaternion convQuatApply = new Quaternion(-BV.z, -BV.x, BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                    else if (BoneR.Key == 21)
                    {
                        Quaternion parentQuat = BoneT.parent.localRotation;
                        Quaternion convQuatApply = new Quaternion(-BV.z, -BV.x, BV.y, BV.w);
                        Quaternion rot = Quaternion.Euler(0, 75.0f, 0.0f);
                        BoneT.localRotation = rot * Quaternion.Inverse(rot) * convQuatApply * rot;
                    }
                    else if (BoneR.Key == 22)
                    {
                        Quaternion convQuatApply = new Quaternion(-BV.z, -BV.x, BV.y, BV.w);
                        BoneT.localRotation = convQuatApply;
                    }
                }
            }
        }

        //快捷查找骨骼节点.如果没有找到则返回 null
        protected Transform FindBone(uint BoneIndex)
        {
            string boneName;
            if (mapBoneIndex.TryGetValue(BoneIndex, out boneName) == false)
            {
                return null;
            }
            Transform transform;
            m_mapNameAndBone.TryGetValue(boneName, out transform);
            return transform;
        }
    }
}
