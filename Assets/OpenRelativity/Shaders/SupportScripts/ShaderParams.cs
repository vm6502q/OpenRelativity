using System.Runtime.InteropServices;
using UnityEngine;

namespace OpenRelativity
{
    //Shader properties:
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 424)]
    public struct ShaderParams
    {
        //[FieldOffset(0)]
        public Matrix4x4 ltwMatrix; //local-to-world transform matrix
        //[FieldOffset(16)]
        public Matrix4x4 wtlMatrix; //world-to-local transform matrix
        //[FieldOffset(32)]
        public Matrix4x4 vpcLorentzMatrix; //Lorentz transform between world and player
        //[FieldOffset(64)]
        public Matrix4x4 viwLorentzMatrix; //Lorentz transform between world and object
        //[FieldOffset(80)]
        public Vector4 viw; //velocity of object in synchronous coordinates
        //[FieldOffset(84)]
        public Vector4 vpc; //velocity of player
        //[FieldOffset(88)]
        public Vector4 playerOffset; //player position in world
        //[FieldOffset(92)]
        public Vector4 pap; //acceleration of player in world coordinates
        //[FieldOffset(96)]
        public Vector4 avp; //angular velocity of player in world coordinates
        //[FieldOffset(100)]
        public Vector4 aiw; //acceleration of object in world coordinates
        //[FieldOffset(104)]
        public System.Single speed; //speed of player;
        //[FieldOffset(105)]
        public System.Single spdOfLight; //current speed of light
    }
}
