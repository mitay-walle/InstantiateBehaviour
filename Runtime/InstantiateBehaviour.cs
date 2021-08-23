using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class InstantiateBehaviour : MonoBehaviour
{
    [SerializeField] private Vector2 Distance = new Vector2(5f, 5f);

#if UNITY_EDITOR
    [Reorderable, SerializeField] private DefaultAsset[] folders;
#endif
    [SerializeField] private GameObject[] prefabs;
    private string[] folderPaths;
    private List<string> relativePrefabPaths = new List<string>();
    private List<GameObject> allPrefabs = new List<GameObject>();
    [SerializeField] private bool InstantiateOnEnable;
    [SerializeField, ReadOnly] private bool markedDontSave;

    private bool waitingInst;

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (!InstantiateOnEnable) return;
        if (transform.childCount > 0) return;
        OnWizardOtherButton();
    }

    [ContextMenu("Clear")]
    private void Clear()
    {
        waitingInst = false;
        RecursiveDestroyChild();
    }

    [ContextMenu("Reinstantiate")]
    private void OnWizardOtherButton()
    {
        if (Application.isPlaying) return;
        waitingInst = true;
        //folders = Selection.GetFiltered<DefaultAsset>(SelectionMode.TopLevel | SelectionMode.ExcludePrefab);
        //prefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets).ToArray();
        if (folders.Length > 0)
        {
            folderPaths = folders.Select(AssetDatabase.GetAssetPath).ToArray();
        }
        else
        {
            folderPaths = new string[0];
        }

        relativePrefabPaths = new List<string>();

        prefabs = prefabs.Distinct().ToArray();

        allPrefabs = new List<GameObject>(prefabs);

        if (folders.Length > 0)
        {
            relativePrefabPaths.AddRange(AssetDatabase.FindAssets("t:prefab", folderPaths).Select(AssetDatabase.GUIDToAssetPath));

            allPrefabs.AddRange(AssetDatabase.FindAssets("t:prefab", folderPaths).Select(guid =>
                AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))));
        }

        if (folders.Length > 0)
            for (var j = 0; j < folderPaths.Length; j++)
            {
                var folderPath = folderPaths[j];
                for (var i = 0; i < relativePrefabPaths.Count; i++)
                {
                    if (relativePrefabPaths[i].Contains(folderPath))
                    {
                        relativePrefabPaths[i] =
                            relativePrefabPaths[i].Replace(folderPath.Replace($"{folders[j].name}", string.Empty),
                                string.Empty);
                    }
                }
            }

        if (folders.Length > 0)
            for (var j = 0; j < relativePrefabPaths.Count; j++)
            {
                var prefabPath = relativePrefabPaths[j];
                for (var i = 0; i < allPrefabs.Count; i++)
                {
                    var prefabName = AssetDatabase.GetAssetPath(allPrefabs[i]);
                    if (prefabPath.Contains(prefabName))
                        relativePrefabPaths[j] = prefabPath.Replace(prefabName, string.Empty);
                }
            }


        relativePrefabPaths = relativePrefabPaths.Distinct().ToList();

        allPrefabs = allPrefabs.Distinct().ToList();


        RecursiveDestroyChild();
    }

    private void RecursiveDestroyChild()
    {
        var parent = transform;

        if (parent.childCount > 0)
        {
            for (int i = 0; i < parent.childCount; i++)
                DestroyImmediate(parent.GetChild(i).gameObject);

            EditorApplication.delayCall += RecursiveDestroyChild;
        }
        else
        {
            if (waitingInst) EditorApplication.delayCall += OnWizardOtherButtonInternal;
        }
    }

    private void OnWizardOtherButtonInternal()
    {
        var parent = transform;

        var count = relativePrefabPaths.Count;
        var pos = Vector3.zero;

        Transform lastPrefabFolder = null;

        if (count > 0)
            for (var i = 0; i < count; i++)
            {
                var path = relativePrefabPaths[i];
                var splitted = path.Split('.')[0].Split('/');
                var lastFolder = parent;
                foreach (var subFloderName in splitted)
                {
                    Transform existingFolder = null;
                    if (!subFloderName.Equals(allPrefabs[i].name))
                    {
                        existingFolder = lastFolder.Find(subFloderName);

                        if (existingFolder == null) existingFolder = new GameObject(subFloderName).transform;

                        existingFolder.SetParent(lastFolder);

                        lastFolder = existingFolder;
                    }
                }

                if (PreEditorExtensions.NeedShowProgressBarWithSkipCounter()) EditorUtility.DisplayProgressBar("Instantiating...", allPrefabs[i].name, (float) i / count);

                if (lastPrefabFolder == null) lastPrefabFolder = lastFolder;

                if (lastPrefabFolder != lastFolder)
                {
                    pos.z += Distance.y;
                    pos.x = 0f;
                }

                lastPrefabFolder = lastFolder;

                var newTr = CreatePrefab(allPrefabs[i], lastFolder);

                //Debug.Log(newTr.name);

                newTr.localPosition = pos;
                pos.x += Distance.x;
            }

        if (prefabs.Length > 0)
        {
            pos.x = 0f;

            for (int i = 0; i < prefabs.Length; i++)
            {
                var tr = CreatePrefab(prefabs[i], transform);
                pos.x += Distance.x;
                tr.position = pos;
            }
        }


        EditorUtility.ClearProgressBar();
        EditorGUIUtility.PingObject(parent.gameObject);
    }

    private Transform CreatePrefab(GameObject prefab, Transform parent)
    {
        var newTr = (PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject).transform;
        newTr.gameObject.SetActive(true);

        markedDontSave = InstantiateOnEnable;

        if (InstantiateOnEnable)
        {
            newTr.gameObject.hideFlags = HideFlags.DontSave;
        }

        return newTr;
    }
#endif
}