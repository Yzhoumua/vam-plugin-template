using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class PaperTrackerPlugin : MVRScript
{
    private UdpClient _client;
    private const int Port = 8888;
    private DAZCharacterSelector _dcs;
    private Dictionary<string, string> _morphMap;
    private FreeControllerV3 _leftEye;
    private FreeControllerV3 _rightEye;

    public override void Init()
    {
        try
        {
            _dcs = containingAtom.GetComponentInChildren<DAZCharacterSelector>();
            if (containingAtom != null && containingAtom.freeControllers != null)
            {
                _leftEye = containingAtom.freeControllers.FirstOrDefault(fc => fc.name.ToLower().Contains("eye") && fc.name.ToLower().Contains("left"));
                _rightEye = containingAtom.freeControllers.FirstOrDefault(fc => fc.name.ToLower().Contains("eye") && fc.name.ToLower().Contains("right"));
            }
            BuildMorphMap();

            _client = new UdpClient(Port);
            _client.BeginReceive(OnData, null);
            SuperController.LogMessage("PaperTrackerPlugin listening on OSC port 8888");
        }
        catch (Exception e)
        {
            SuperController.LogError("PaperTrackerPlugin init error: " + e);
        }
    }

    private void BuildMorphMap()
    {
        _morphMap = new Dictionary<string, string>
        {
            // Eyes
            {"EyeBlinkLeft", "Blink Left"},
            {"EyeBlinkRight", "Blink Right"},
            {"EyeWideLeft", "Eye Open Wide Left"},
            {"EyeWideRight", "Eye Open Wide Right"},
            {"EyeSquintLeft", "Eye Squint Left"},
            {"EyeSquintRight", "Eye Squint Right"},

            // Brows
            {"BrowInnerUp", "Brow Inner Up"},
            {"BrowDownLeft", "Brow Down Left"},
            {"BrowDownRight", "Brow Down Right"},
            {"BrowOuterUpLeft", "Brow Outer Up Left"},
            {"BrowOuterUpRight", "Brow Outer Up Right"},

            // Mouth
            {"JawOpen", "Jaw Open"},
            {"MouthClose", "Mouth Close"},
            {"MouthLeft", "Mouth Left"},
            {"MouthRight", "Mouth Right"},
            {"MouthFunnel", "Mouth Funnel"},
            {"MouthPucker", "Mouth Pucker"},
            {"MouthRollUpper", "Mouth Roll Upper"},
            {"MouthRollLower", "Mouth Roll Lower"},
            {"MouthShrugUpper", "Mouth Shrug Upper"},
            {"MouthShrugLower", "Mouth Shrug Lower"},
            {"MouthDimpleLeft", "Mouth Dimple Left"},
            {"MouthDimpleRight", "Mouth Dimple Right"},
            {"MouthStretchLeft", "Mouth Stretch Left"},
            {"MouthStretchRight", "Mouth Stretch Right"},
            {"MouthPressLeft", "Mouth Press Left"},
            {"MouthPressRight", "Mouth Press Right"},
            {"MouthSmileLeft", "Mouth Smile Left"},
            {"MouthSmileRight", "Mouth Smile Right"},
            {"MouthFrownLeft", "Mouth Frown Left"},
            {"MouthFrownRight", "Mouth Frown Right"},

            // Tongue
            {"TongueOut", "Tongue Out"},
            {"TongueUp", "Tongue Up"},
            {"TongueDown", "Tongue Down"},
            {"TongueLeft", "Tongue Left"},
            {"TongueRight", "Tongue Right"}
        };
    }

    private void OnData(IAsyncResult ar)
    {
        IPEndPoint ip = new IPEndPoint(IPAddress.Any, 0);
        byte[] data = _client.EndReceive(ar, ref ip);
        ParseOSC(data);
        _client.BeginReceive(OnData, null);
    }

    private void ParseOSC(byte[] data)
    {
        OscMessage msg = OscMessage.FromBytes(data);
        if (msg == null) return;

        if (msg.Address.StartsWith("/paper_face_tracker/"))
        {
            string param = msg.Address.Substring("/paper_face_tracker/".Length);
            if (msg.Arguments.Count > 0 && msg.Arguments[0] is float)
            {
                ApplyMorph(param, (float)msg.Arguments[0]);
            }
        }
        else if (msg.Address.StartsWith("/paper_eye_tracker/"))
        {
            string param = msg.Address.Substring("/paper_eye_tracker/".Length);
            if (msg.Arguments.Count > 0 && msg.Arguments[0] is float)
            {
                ApplyEye(param, (float)msg.Arguments[0]);
            }
        }
    }

    private void ApplyMorph(string param, float value)
    {
        if (_dcs == null) return;
        string morphName;
        if (_morphMap.TryGetValue(param, out morphName))
        {
            DAZMorph morph = _dcs.morphsControl.GetMorphByDisplayName(morphName);
            if (morph != null)
            {
                morph.morphValue = Mathf.Clamp01(value);
            }
        }
    }

    private void ApplyEye(string param, float value)
    {
        if (param == "leftPitch" && _leftEye != null)
        {
            Vector3 rot = _leftEye.transform.localEulerAngles;
            rot.x = value;
            _leftEye.transform.localEulerAngles = rot;
        }
        else if (param == "leftYaw" && _leftEye != null)
        {
            Vector3 rot = _leftEye.transform.localEulerAngles;
            rot.y = value;
            _leftEye.transform.localEulerAngles = rot;
        }
        else if (param == "rightPitch" && _rightEye != null)
        {
            Vector3 rot = _rightEye.transform.localEulerAngles;
            rot.x = value;
            _rightEye.transform.localEulerAngles = rot;
        }
        else if (param == "rightYaw" && _rightEye != null)
        {
            Vector3 rot = _rightEye.transform.localEulerAngles;
            rot.y = value;
            _rightEye.transform.localEulerAngles = rot;
        }
    }

    public void OnDestroy()
    {
        if (_client != null)
        {
            _client.Close();
            _client = null;
        }
    }

    private class OscMessage
    {
        public string Address;
        public List<object> Arguments;

        public static OscMessage FromBytes(byte[] data)
        {
            try
            {
                int offset = 0;
                string address = ReadString(data, ref offset);
                string typeTag = ReadString(data, ref offset);
                List<object> args = new List<object>();
                foreach (char c in typeTag.Skip(1))
                {
                    switch (c)
                    {
                        case 'f':
                            args.Add(ReadFloat(data, ref offset));
                            break;
                        case 'i':
                            args.Add(ReadInt(data, ref offset));
                            break;
                    }
                }
                return new OscMessage { Address = address, Arguments = args };
            }
            catch
            {
                return null;
            }
        }

        private static string ReadString(byte[] data, ref int offset)
        {
            int start = offset;
            while (offset < data.Length && data[offset] != 0) offset++;
            string str = Encoding.ASCII.GetString(data, start, offset - start);
            offset++;
            while (offset % 4 != 0) offset++;
            return str;
        }

        private static float ReadFloat(byte[] data, ref int offset)
        {
            byte[] buffer = new byte[4];
            Array.Copy(data, offset, buffer, 0, 4);
            if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
            offset += 4;
            return BitConverter.ToSingle(buffer, 0);
        }

        private static int ReadInt(byte[] data, ref int offset)
        {
            byte[] buffer = new byte[4];
            Array.Copy(data, offset, buffer, 0, 4);
            if (BitConverter.IsLittleEndian) Array.Reverse(buffer);
            offset += 4;
            return BitConverter.ToInt32(buffer, 0);
        }
    }
}
