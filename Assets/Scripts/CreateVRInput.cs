#if UNITY_EDITOR
using UnityEditor;
using System.IO;

public class CreateVRInput
{
    [MenuItem("Tools/Create VRInputActions")]
    static void Create()
    {
        string json = @"{
    ""name"": ""VRInputActions"",
    ""maps"": [
        {
            ""name"": ""Study"",
            ""id"": ""a1b2c3d4-e5f6-7890-abcd-ef1234567890"",
            ""actions"": [
                {
                    ""name"": ""Detect"",
                    ""type"": ""Button"",
                    ""id"": ""b2c3d4e5-f6a7-8901-bcde-f12345678901"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""c3d4e5f6-a7b8-9012-cdef-123456789012"",
                    ""path"": ""<XRController>{RightHand}/triggerPressed"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Detect"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}";

        string path = "Assets/VRInputActions.inputactions";

        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);

        File.WriteAllText(path, json);
        AssetDatabase.Refresh();

        UnityEngine.Debug.Log("VRInputActions created with triggerPressed binding!");
    }
}
#endif
