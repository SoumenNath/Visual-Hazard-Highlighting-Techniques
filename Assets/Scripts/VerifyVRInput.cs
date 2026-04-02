#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class VerifyVRInput
{
    [MenuItem("Tools/Verify VRInputActions")]
    static void Verify()
    {
        string path = "Assets/VRInputActions.inputactions";
        if (File.Exists(path))
        {
            string contents = File.ReadAllText(path);
            Debug.Log("File contents:\n" + contents);
        }
        else
        {
            Debug.LogError("File not found!");
        }
    }
}
#endif