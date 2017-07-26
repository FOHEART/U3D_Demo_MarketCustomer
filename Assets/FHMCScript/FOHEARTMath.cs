using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoheartMC
{
    public class FOHEARTMath
    {
        public enum ChannelOrder
        {
            XYZ,
            XZY,
            YXZ,
            YZX,
            ZXY,
            ZYX,
            NONE
        };

        private float[] rotation2quat( float[] rotmat)
        {
            float[] q = new float[4];
            float T = 1 + rotmat[0] + rotmat[4] + rotmat[8];

            // Calculate quaternion based on largest diagonal element
            if (T > (0.00000001f))
            {
                float S = 0.5f / (float)Math.Sqrt(T);
                q[3] = 0.25f / S;
                q[0] = (rotmat[7] - rotmat[5]) * S;
                q[1] = (rotmat[2] - rotmat[6]) * S;
                q[2] = (rotmat[3] - rotmat[1]) * S;
            }
            else if ((rotmat[0] > rotmat[4]) && (rotmat[0] > rotmat[8]))
            {
                float S = (float)Math.Sqrt(1 + rotmat[0] - rotmat[4] - rotmat[8]) * 2;
                q[3] = (rotmat[6] - rotmat[5]) / S;
                q[0] = 0.25f * S;
                q[1] = (rotmat[1] + rotmat[3]) / S;
                q[2] = (rotmat[2] + rotmat[6]) / S;
            }
            else if (rotmat[4] > rotmat[8])
            {
                float S = (float)Math.Sqrt(1 + rotmat[4] - rotmat[0] - rotmat[8]) * 2;
                q[3] = (rotmat[2] - rotmat[6]) / S;
                q[0] = (rotmat[1] + rotmat[3]) / S;
                q[1] = 0.25f * S;
                q[2] = (rotmat[5] + rotmat[7]) / S;
            }
            else
            {
                float S = (float)Math.Sqrt(1 + rotmat[8] - rotmat[0] - rotmat[4]) * 2;
                q[3] = (rotmat[3] - rotmat[1]) / S;
                q[0] = (rotmat[2] + rotmat[6]) / S;
                q[1] = (rotmat[1] + rotmat[3]) / S;
                q[2] = 0.25f * S;
            }

            //Normalize the quaternion
            T = (float)Math.Sqrt(q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3]);

            q[0] = -q[0] / T;
            q[1] = -q[1] / T;
            q[2] = -q[2] / T;
            q[3] = q[3] / T;

            return q;
        }

        public float[] EulerToQuat(float x, float y, float z, ChannelOrder RotOrder)
        {
            float[] RotMatrix = new float[9];
            float[] fData = new float[4] { 0.0f, 0.0f, 0.0f, 1.0f };/*X,Y,Z,W*/

            float XR, YR, ZR;

            switch (RotOrder)
            {
                case ChannelOrder.XYZ:
                    {
                        XR = x / 180.0f * (float)Math.PI;
                        YR = y / 180.0f * (float)Math.PI;
                        ZR = z / 180.0f * (float)Math.PI;

                        float SX = (float)Math.Sin(XR);
                        float CX = (float)Math.Cos(XR);
                        float SY = (float)Math.Sin(YR);
                        float CY = (float)Math.Cos(YR);
                        float SZ = (float)Math.Sin(ZR);
                        float CZ = (float)Math.Cos(ZR);

                        RotMatrix[0] = CY * CZ;
                        RotMatrix[3] = -CY * SZ;
                        RotMatrix[6] = SY;
                        RotMatrix[1] = CZ * SX * SY + CX * SZ;
                        RotMatrix[4] = CX * CZ - SX * SY * SZ;
                        RotMatrix[7] = -CY * SX;
                        RotMatrix[2] = SX * SZ - CX * CZ * SY;
                        RotMatrix[5] = CZ * SX + CX * SY * SZ;
                        RotMatrix[8] = CX * CY;
                        break;
                    }
                case ChannelOrder.XZY:
                    {
                        XR = x / 180.0f * (float)Math.PI;
                        YR = z / 180.0f * (float)Math.PI;
                        ZR = y / 180.0f * (float)Math.PI;

                        float SX = (float)Math.Sin(XR);
                        float CX = (float)Math.Cos(XR);
                        float SY = (float)Math.Sin(YR);
                        float CY = (float)Math.Cos(YR);
                        float SZ = (float)Math.Sin(ZR);
                        float CZ = (float)Math.Cos(ZR);

                        RotMatrix[0] = CY * CZ;
                        RotMatrix[3] = -SZ;
                        RotMatrix[6] = CZ * SY;
                        RotMatrix[1] = SX * SY + CX * CY * SZ;
                        RotMatrix[4] = CX * CZ;
                        RotMatrix[7] = CX * SY * SZ - CY * SX;
                        RotMatrix[2] = CY * SX * SZ - CX * SY;
                        RotMatrix[5] = CZ * SX;
                        RotMatrix[8] = CX * CY + SX * SY * SZ;
                        break;
                    }
                case ChannelOrder.YXZ:
                    {
                        XR = y / 180.0f * (float)Math.PI;
                        YR = x / 180.0f * (float)Math.PI;
                        ZR = z / 180.0f * (float)Math.PI;

                        float SX = (float)Math.Sin(XR);
                        float CX = (float)Math.Cos(XR);
                        float SY = (float)Math.Sin(YR);
                        float CY = (float)Math.Cos(YR);
                        float SZ = (float)Math.Sin(ZR);
                        float CZ = (float)Math.Cos(ZR);

                        RotMatrix[0] = CY * CZ + SX * SY * SZ;
                        RotMatrix[3] = CZ * SX * SY - CY * SZ;
                        RotMatrix[6] = CX * SY;
                        RotMatrix[1] = CX * SZ;
                        RotMatrix[4] = CX * CZ;
                        RotMatrix[7] = -SX;
                        RotMatrix[2] = CY * SX * SZ - CZ * SY;
                        RotMatrix[5] = CY * CZ * SX + SY * SZ;
                        RotMatrix[8] = CX * CY;
                        break;
                    }
                case ChannelOrder.YZX:
                    {
                        XR = y / 180.0f * (float)Math.PI;
                        YR = z / 180.0f * (float)Math.PI;
                        ZR = x / 180.0f * (float)Math.PI;

                        float SX = (float)Math.Sin(XR);
                        float CX = (float)Math.Cos(XR);
                        float SY = (float)Math.Sin(YR);
                        float CY = (float)Math.Cos(YR);
                        float SZ = (float)Math.Sin(ZR);
                        float CZ = (float)Math.Cos(ZR);

                        RotMatrix[0] = CY * CZ;
                        RotMatrix[3] = SX * SY - CX * CY * SZ;
                        RotMatrix[6] = CX * SY + CY * SX * SZ;
                        RotMatrix[1] = SZ;
                        RotMatrix[4] = CX * CZ;
                        RotMatrix[7] = -CZ * SX;
                        RotMatrix[2] = -CZ * SY;
                        RotMatrix[5] = CY * SX + CX * SY * SZ;
                        RotMatrix[8] = CX * CY - SX * SY * SZ;
                        break;
                    }
                case ChannelOrder.ZXY:
                    {
                        XR = z / 180.0f * (float)Math.PI;
                        YR = x / 180.0f * (float)Math.PI;
                        ZR = y / 180.0f * (float)Math.PI;

                        float SX = (float)Math.Sin(XR);
                        float CX = (float)Math.Cos(XR);
                        float SY = (float)Math.Sin(YR);
                        float CY = (float)Math.Cos(YR);
                        float SZ = (float)Math.Sin(ZR);
                        float CZ = (float)Math.Cos(ZR);

                        RotMatrix[0] = CY * CZ - SX * SY * SZ;
                        RotMatrix[3] = -CX * SZ;
                        RotMatrix[6] = CZ * SY + CY * SX * SZ;
                        RotMatrix[1] = CZ * SX * SY + CY * SZ;
                        RotMatrix[4] = CX * CZ;
                        RotMatrix[7] = SY * SZ - CY * CZ * SX;
                        RotMatrix[2] = -CX * SY;
                        RotMatrix[5] = SX;
                        RotMatrix[8] = CX * CY;
                        break;
                    }

                case ChannelOrder.ZYX:
                    {
                        XR = z / 180.0f * (float)Math.PI;
                        YR = y / 180.0f * (float)Math.PI;
                        ZR = x / 180.0f * (float)Math.PI;

                        float SX = (float)Math.Sin(XR);
                        float CX = (float)Math.Cos(XR);
                        float SY = (float)Math.Sin(YR);
                        float CY = (float)Math.Cos(YR);
                        float SZ = (float)Math.Sin(ZR);
                        float CZ = (float)Math.Cos(ZR);

                        RotMatrix[0] = CY * CZ;
                        RotMatrix[3] = CZ * SX * SY - CX * SZ;
                        RotMatrix[6] = CX * CZ * SY + SX * SZ;
                        RotMatrix[1] = CY * SZ;
                        RotMatrix[4] = CX * CZ + SX * SY * SZ;
                        RotMatrix[7] = CX * SY * SZ - CZ * SX;
                        RotMatrix[2] = -SY;
                        RotMatrix[5] = CY * SX;
                        RotMatrix[8] = CX * CY;
                        break;
                    }
                default:
                    break;
            }

            fData[0] = 0;
            fData[1] = 0;
            fData[2] = 0;
            fData[3] = 1;

            fData=rotation2quat( RotMatrix);
            return fData;
        }
    }
}
