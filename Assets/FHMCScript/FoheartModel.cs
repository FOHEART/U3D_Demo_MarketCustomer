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
        public string ConfigName;

        //是否使用远程位移
        public Vector3 HipsStartLocation;
        public bool FixHipsLocation;

        string OutText;
        //调试输出的文本,可以在任意时候更改,
        [HideInInspector]
        public uint ConnectPackNumber;
        //链接的包号
        protected Dictionary<uint, string> mapBoneIndex;
        class initAxisAndRot
        {
            public int[] xyz;

            public float[] xryrzr;

            public initAxisAndRot()
            {
                xyz = new int[3];
                xryrzr = new float[3];
            }
            public void setAxis(int _x, int _y, int _z)
            {
                xyz[0] = _x; xyz[1] = _y; xyz[2] = _z;
            }
            public void setRot(float _xr, float _yr, float _zr)
            {
                xryrzr[0] = _xr; xryrzr[1] = _yr; xryrzr[2] = _zr;
            }
        }
        Dictionary<uint, initAxisAndRot> mapBoneInitAxisTrans;
        protected bool dataPending = false;
        protected Dictionary<string, Transform> m_mapNameAndBone;
        protected Quaternion m_bodyStartQuat;
        protected Vector3 m_bodyStartPosition;
        ActorFrameData frameDataBuffer;

        public FoheartModel()
        {
            ActorName = "Actor1(Live)";
            ConfigName = "DefaultActor.xml";
        }

        void Awake()
        {
            CheckCondition();
            mapBoneIndex = new Dictionary<uint, string>();
            m_mapNameAndBone = new Dictionary<string, Transform>();
            mapBoneInitAxisTrans = new Dictionary<uint, initAxisAndRot>();
            frameDataBuffer = new ActorFrameData();
            loadBoneMaps();
            initNameAndBone();
            ChildAwake();

            Transform HipsStartTrans = FindBone(0);
            if (HipsStartTrans == null)
            {
                Debug.LogError("Please check Config Name is Right or ConnectId's name is valid in Model!");
            }
            HipsStartLocation = new Vector3(HipsStartTrans.position.x, HipsStartTrans.position.y, HipsStartTrans.position.z);
        }


        //检查是否已经指定骨骼绑定文件
        void CheckCondition()
        {
            if (ConfigName.Length == 0)
            {
                //没有指定骨骼配置文件,请先编辑好配置文件,放置在工程目录下(Assets的父目录)
                //并手动将文件名,填写到ConfigName中
                Debug.LogError("ConfigName not specified!");
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

        //从配置文件加载骨骼映射
        void loadBoneMaps()
        {
            try
            {
                XDocument doc = XDocument.Load(ConfigName);
                foreach (XElement element in doc.Element("ActorBones").Elements())
                {
                    uint BoneID = Convert.ToUInt32(element.Attribute("ConnectId").Value);
                    mapBoneIndex.Add(BoneID, element.Attribute("name").Value);

                    if (mapBoneInitAxisTrans.Count == 0 || !mapBoneInitAxisTrans.ContainsKey(BoneID))
                        mapBoneInitAxisTrans.Add(BoneID, new initAxisAndRot());

                    /*--------------Get local axis conversion-----------------*/
                    string[] expectAxisAttr = new string[3] { "X", "Y", "Z" };
                    string[] standardAxisValue = new string[6] { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };
                    int[] initAxis = new int[3] { 0, 0, 0 };
                    for (int i = 0; i < expectAxisAttr.Length; i++)
                    {
                        string convertedAttr = "";
                        if (element.Attribute(expectAxisAttr[i]) == null || element.Attribute(expectAxisAttr[i]).Value == "")
                        {
                            convertedAttr = expectAxisAttr[i];
                        }
                        else
                        {
                            convertedAttr = element.Attribute(expectAxisAttr[i]).Value.ToUpper();
                        }

                        if (convertedAttr.Length == 1)
                        {
                            convertedAttr = "+" + convertedAttr;
                        }
                        int[] singleInitAxis = new int[3];
                        for (int j = 0; j < standardAxisValue.Length; j++)
                        {
                            if (convertedAttr == standardAxisValue[j])
                            {
                                singleInitAxis[i] = (j + 1) % 2 == 0 ? -((j + 2) / 2) : ((j + 2) / 2);
                                break;
                            }
                        }

                        for (int k = 0; k < 3; k++)
                        {
                            initAxis[k] += singleInitAxis[k];
                        }
                    }
                    mapBoneInitAxisTrans[BoneID].setAxis(
                        initAxis[0],
                        initAxis[1],
                        initAxis[2]
                        );
                    /*--------------Get initial local rotation-----------------*/
                    float[] initRot = new float[3];
                    string[] expectRotAttr = new string[3] { "XR", "YR", "ZR" };

                    for (int i = 0; i < expectRotAttr.Length; i++)
                    {
                        if (element.Attribute(expectRotAttr[i]) == null || element.Attribute(expectRotAttr[i]).Value == "")
                        {
                            initRot[i] = 0.0f;
                        }
                        else
                        {
                            try
                            {
                                initRot[i] = Convert.ToSingle(element.Attribute(expectRotAttr[i]).Value);
                            }
                            catch (Exception e)
                            {
                                print("[ERROR] " + e.ToString() + " Failed conversion, please check your input!");
                                initRot[i] = 0.0f;
                            }
                        }
                    }
                    mapBoneInitAxisTrans[BoneID].setRot(
                        initRot[0],
                        initRot[1],
                        initRot[2]
                        );

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
            lock (frameDataBuffer)
            {
                data.CopyTo(ref frameDataBuffer);
                dataPending = true;
            }
        }

        // Update is called once per frame after Update
        void Update()
        {
            lock (frameDataBuffer)
            {
                if (!dataPending)
                {
                    return;
                }
                applyBoneTransRot(frameDataBuffer);
            }
        }

        public virtual void applyBoneTransRot(ActorFrameData data)
        {
            Transform BoneHip = FindBone(0);
            if (BoneHip)
            {
                Vector3 LocInUnity = new Vector3();
                UInt32 scale = 1;
                LocInUnity.Set(
                    -data.bonePositions[0].x * scale,
                    data.bonePositions[0].z * scale,
                    -data.bonePositions[0].y * scale);
                if (FixHipsLocation)
                {
                }
                else
                {
                    BoneHip.transform.position = LocInUnity;
                }
            }
            else
            {
                print("[ERROR] Do not find HIP, can not set actor location.");
                return;
            }

            foreach (var BoneR in data.boneRotQuat)
            {
                Transform BoneT = FindBone(BoneR.Key);
                if (BoneT)
                {
                    var BR = BoneR.Value;

                    float[] remoteRot = new float[3] { BR.x, BR.y, BR.z };
                    float[] transRot = new float[3];
                    for (int i = 0; i < transRot.Length; i++)
                    {
                        bool isNeg;
                        if (mapBoneInitAxisTrans[BoneR.Key].xyz[i] < 0) isNeg = true; else isNeg = false;
                        int tempIndex = isNeg ? (-mapBoneInitAxisTrans[BoneR.Key].xyz[i]) - 1 : mapBoneInitAxisTrans[BoneR.Key].xyz[i] - 1;
                        transRot[i] = remoteRot[tempIndex] * (isNeg ? -1 : 1);
                    }

                    Quaternion convQuatApply = new Quaternion(transRot[0], transRot[1], transRot[2], BR.w);
                    Quaternion rot = Quaternion.Euler(
                        mapBoneInitAxisTrans[BoneR.Key].xryrzr[0],
                        mapBoneInitAxisTrans[BoneR.Key].xryrzr[1],
                        mapBoneInitAxisTrans[BoneR.Key].xryrzr[2]);
                    BoneT.localRotation = rot * convQuatApply;
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
