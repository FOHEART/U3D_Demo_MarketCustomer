using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FoheartMC
{
    public class ActorFrameData
    {
        enum FoheartPluginErr
        {
            PROTOCOL_NOMATCH = -1,
            PACKAGE_LEN_NOMATCH = -2
        };

        public UInt16 motionVenusProtoVer;
        //协议版本号
        public string strActorName;
        //角色名称
        public UInt32 suitNumber;
        //套装编号
        public Byte suitType;
        //节点类型
        public UInt32 frameNumber;
        //角色位置
        public Byte boneCount;
        //骨骼数目
        public Dictionary<Byte, Quaternion> boneRotQuat;
        Dictionary<Byte, Vector3> boneRotEuler;
        public Dictionary<Byte, Vector3> bonePositions;

        private const UInt16 EulerScale = (1 << 7);
        private const UInt16 QuatScale = (1 << 8);
        private const UInt32 PositionScale = (1 << 16);
        private const UInt16 ProtocolVerPlugin = (1003);
        private const Byte FrameHeaderSize = 128;

        public ActorFrameData()
        {
            boneRotQuat = new Dictionary<Byte, Quaternion>();
            boneRotEuler = new Dictionary<Byte, Vector3>();
            bonePositions = new Dictionary<Byte, Vector3>();
        }

        //解析data中的数据
        public int deComposeData(byte[] data, bool containPos, bool containEuler, bool containQuat)
        {
            int index = 0;
            //协议版本号必须与主程序相同
            motionVenusProtoVer = BitConverter.ToUInt16(data, index);
            index += Marshal.SizeOf(motionVenusProtoVer);
            if (motionVenusProtoVer != ProtocolVerPlugin)
            {
                Debug.Log("Protocol version: Your Protocol Need to Be update!");
                return PROTOCOL_NOMATCH;//协议错误
            }
            byte nameLength = data[index];
            index += Marshal.SizeOf(nameLength);

            byte[] tempName = new byte[nameLength];
            Array.Copy(data, index, tempName, 0, nameLength);
            strActorName = Encoding.ASCII.GetString(tempName);
            index += (int)nameLength;

            suitNumber = BitConverter.ToUInt32(data, index);
            index += Marshal.SizeOf(suitNumber);

            suitType = data[index];
            index += Marshal.SizeOf(suitType);

            frameNumber = BitConverter.ToUInt32(data, index);
            index += Marshal.SizeOf(frameNumber);

            boneCount = data[index];
            index += Marshal.SizeOf(boneCount);

            int checkDataLength = FrameHeaderSize;
            if (containPos)
            { checkDataLength += boneCount * (3 * Marshal.SizeOf(new Int32())); }
            if (containEuler)
            { checkDataLength += boneCount * (3 * Marshal.SizeOf(new Int16())); }
            if (containQuat)
            { checkDataLength += boneCount * (4 * Marshal.SizeOf(new Int16())); }

            if (checkDataLength != data.Length)
            {
                //检查数据完整性
                Debug.Log("Package length check error!");
                return PACKAGE_LEN_NOMATCH;//数据不完整
            }

            index = FrameHeaderSize;

            bonePositions.Clear();
            boneRotEuler.Clear();
            boneRotQuat.Clear();
            for (byte i = 0; i < boneCount; i++)
            {
                if (containPos)
                {
                    Vector3 posTemp = new Vector3();
                    posTemp.x = (float)BitConverter.ToInt32(data, index) / PositionScale;
                    index += Marshal.SizeOf(new Int32());
                    posTemp.y = (float)BitConverter.ToInt32(data, index) / PositionScale;
                    index += Marshal.SizeOf(new Int32());
                    posTemp.z = (float)BitConverter.ToInt32(data, index) / PositionScale;
                    index += Marshal.SizeOf(new Int32());
                    bonePositions.Add(i, posTemp);
                }
                if (containEuler)
                {
                    Vector3 eulerTemp = new Vector3();
                    eulerTemp.x = (float)BitConverter.ToInt16(data, index) / EulerScale;
                    index += Marshal.SizeOf(new Int16());
                    eulerTemp.y = (float)BitConverter.ToInt16(data, index) / EulerScale;
                    index += Marshal.SizeOf(new Int16());
                    eulerTemp.z = (float)BitConverter.ToInt16(data, index) / EulerScale;
                    index += Marshal.SizeOf(new Int16());
                    boneRotEuler.Add(i, eulerTemp);

                    FOHEARTMath fmath = new FOHEARTMath();
                    float[] quatTemp = fmath.EulerToQuat(
                        eulerTemp.x,
                        eulerTemp.y,
                        eulerTemp.z,
                        FOHEARTMath.ChannelOrder.ZXY);
                    boneRotQuat.Add(i, new Quaternion(quatTemp[0], quatTemp[1], quatTemp[2], quatTemp[3]));
                }
                if (containQuat)
                {
                    Quaternion eulerTemp = new Quaternion();
                    eulerTemp.x = (float)BitConverter.ToInt16(data, index) / QuatScale;
                    index += Marshal.SizeOf(new Int16());
                    eulerTemp.y = (float)BitConverter.ToInt16(data, index) / QuatScale;
                    index += Marshal.SizeOf(new Int16());
                    eulerTemp.z = (float)BitConverter.ToInt16(data, index) / QuatScale;
                    index += Marshal.SizeOf(new Int16());
                    eulerTemp.w = (float)BitConverter.ToInt16(data, index) / QuatScale;
                    index += Marshal.SizeOf(new Int16());
                    boneRotQuat.Add(i, eulerTemp);
                }
            }
            return 0;//解析正确
        }

        //将数据拷贝到other中
        public void CopyTo(ref ActorFrameData other)
        {
            other.motionVenusProtoVer = motionVenusProtoVer;
            other.strActorName = string.Copy(strActorName);
            other.suitNumber = suitNumber;
            other.suitType = suitType;
            other.frameNumber = frameNumber;
            other.boneCount = boneCount;
            other.boneRotQuat = new Dictionary<byte, Quaternion>(boneRotQuat);
            other.bonePositions = new Dictionary<byte, Vector3>(bonePositions);
        }

        public int PROTOCOL_NOMATCH { get; set; }

        public int PACKAGE_LEN_NOMATCH { get; set; }
    }
}
