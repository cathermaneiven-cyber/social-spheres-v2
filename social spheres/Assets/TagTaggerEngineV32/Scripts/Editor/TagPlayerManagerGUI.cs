using Photon.Pun;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TagPlayerManager))]
public class TagPlayerManagerEditor : Editor
{
    private SerializedProperty playerManagerProp;

    private void OnEnable()
    {
        playerManagerProp = serializedObject.FindProperty("m_Script");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(playerManagerProp);
        GUI.enabled = true;

        base.OnInspectorGUI();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Tagging Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Tag This Player"))
        {
            TagPlayerManager targetManager = (TagPlayerManager)target;
            if (targetManager != null)
            {
                targetManager.TagThisPlayer();
            }
        }

        if (GUILayout.Button("Auto-Setup TagPlayerManager"))
        {
            TagPlayerManager targetManager = (TagPlayerManager)target;
            if (targetManager != null)
            {
                AutoSetupTagPlayerManager(targetManager);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AutoSetupTagPlayerManager(TagPlayerManager targetManager)
    {
        if (!targetManager.GetComponent<PhotonView>())
        {
            targetManager.gameObject.AddComponent<PhotonView>();
            Debug.Log("PhotonView component added to TagPlayerManager.");
        }

        Transform headTransform = targetManager.transform.Find("Head");
        if (headTransform)
        {
            GameObject audioSourceObj = new GameObject("Tag Player Audio");
            audioSourceObj.transform.SetParent(headTransform);
            audioSourceObj.transform.localPosition = Vector3.zero;
            audioSourceObj.AddComponent<AudioSource>();
            targetManager.PlayerAudio = audioSourceObj.GetComponent<AudioSource>();
            audioSourceObj.GetComponent<AudioSource>().playOnAwake = false;
            audioSourceObj.GetComponent<AudioSource>().spatialBlend = 1;
            Debug.Log("AudioSource component added to the child object 'Head'.");

            GameObject TagTriggerObj = new GameObject("Tag Collider");
            TagTriggerObj.transform.SetParent(headTransform);
            TagTriggerObj.transform.localPosition = Vector3.zero;

            CapsuleCollider capsuleCollider = TagTriggerObj.AddComponent<CapsuleCollider>();
            capsuleCollider.isTrigger = true;
            capsuleCollider.height = 0.61f;
            capsuleCollider.radius = 0.18f;
            capsuleCollider.center = new Vector3(0, -0.21f, 0);
            CreateLayerIfNotExists("TagCollider");
            TagTriggerObj.layer = LayerMask.NameToLayer("TagCollider");
            TagCollider tagCollider = TagTriggerObj.AddComponent<TagCollider>();
            Debug.Log("Capsule GameObject 'Tag Collider' added as a child of the 'Head' object.");
        }

        if (targetManager.PlayerParts == null || targetManager.PlayerParts.Length == 0)
        {
            Renderer[] allRenderers = targetManager.GetComponentsInChildren<Renderer>();
            targetManager.PlayerParts = allRenderers.Where(r => r.GetComponent<TMPro.TextMeshPro>() == null).ToArray();
            Debug.Log("PlayerParts array auto-setup with Renderer components from children (excluding TextMeshPro).");
        }
    }


    private void CreateLayerIfNotExists(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 0; i < layersProp.arraySize; i++)
        {
            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
            if (layerProp.stringValue == layerName)
            {
                // Layer already exists, no need to create it
                return;
            }
        }

        // Create a new layer
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
            if (layerProp.stringValue == "")
            {
                layerProp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }

        Debug.LogError("Could not create layer '" + layerName + "'. The maximum number of layers (32) has been reached.");
    }
}
