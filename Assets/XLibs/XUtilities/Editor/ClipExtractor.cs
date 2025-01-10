#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using UnityEditor;

public class ClipExtractor : MonoBehaviour
{
	#region Common

	static string NormalizeDirectoryPath(string path)
	{
        if (path.Length == 0)
            return path;

        path = path.Replace('\\', '/');
        if (path[path.Length - 1] == '/')
            path = path.Substring(0, path.Length - 1);

        return path;
    }

    delegate void TraverseCallback(int index, int sum, string path);

    static void TraverseAssetsInDirectory(string filter, string directory, TraverseCallback cb)
	{
        // TODO recursive traverse

        directory = NormalizeDirectoryPath(directory);

        var guids = AssetDatabase.FindAssets(filter, new string[] { directory });

        int index = 0;

        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);

            cb?.Invoke(index++, guids.Length, assetPath);
        }
    }

    /// <summary>
    /// including folders and their immediate child
    /// filter is only useful for TraverseAssetsInDirectory for now
    /// for selected single asset, do your own filtering in cb
    /// </summary>
    static void TraverseSelectedAssets(string filter, TraverseCallback cb)
	{
        int _index = 0;

        foreach (var selectionGuid in Selection.assetGUIDs)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(selectionGuid);
            // if is single asset
            if (Path.HasExtension(assetPath))
                cb?.Invoke(_index++, Selection.assetGUIDs.Length, assetPath);
            // if is folder
            else
                TraverseAssetsInDirectory(filter, assetPath, cb);
        }

        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// if nodes in Max have the same name, unity will rename them by adding " n", where n is a number
    /// this function removes trailing " n" from a string
    /// </summary>
    /// <returns></returns>
    static string UndoUnityNameClashRename(string name)
	{
        // TODO, this is a quick and dirty fix

        var lastSpaceIndex = name.LastIndexOf(" ");
        if (lastSpaceIndex > 0 && name.Length > lastSpaceIndex + 1)
		{
            if (int.TryParse(name.Substring(lastSpaceIndex + 1), out int n))
                name = name.Substring(0, lastSpaceIndex);
		}

        return name;
	}

	#endregion

    static void ExtractAnimClipFromModelFile(string fbxAsset, string directory)
    {
        var ext = Path.GetExtension(fbxAsset).ToLower();

        if (ext != ".fbx" && ext != ".max")
            return;

        var fileName = Path.GetFileNameWithoutExtension(fbxAsset);
        var targetPath = $"{directory}/{fileName}.anim";
        AnimationClip src = AssetDatabase.LoadAssetAtPath<AnimationClip>(fbxAsset);
        AnimationClip temp = new AnimationClip();
        EditorUtility.CopySerialized(src, temp);
        AssetDatabase.CreateAsset(temp, targetPath);
        AssetDatabase.SaveAssets();
    }

    static void ExtractSeparatedAnimClipFromModelFile(string fbxAsset, string directory)
    {
        var ext = Path.GetExtension(fbxAsset).ToLower();

        if (ext != ".fbx" && ext != ".max")
            return;

        var fileName = Path.GetFileNameWithoutExtension(fbxAsset);
        AnimationClip src = AssetDatabase.LoadAssetAtPath<AnimationClip>(fbxAsset);

        // begin separation

        //// record characters and cameras
        var srcCurveBindings = AnimationUtility.GetCurveBindings(src);
        SortedSet<string> separateRoots = new SortedSet<string>();
        {
            foreach (var binding in srcCurveBindings)
			{
                // this is curve on a character bone root or camera
                if ((binding.path.IndexOf("000") == 0 || binding.path.IndexOf("Camera") == 0) &&
                    binding.path.IndexOf("/") == -1)
                {
                    separateRoots.Add(binding.path);
                }
            }
        }

		//// separate character animation tracks
		{
            // for each character
            foreach (var rootName in separateRoots)
            {
                var characterClip = new AnimationClip();

                // for each curve
                foreach (var srcBinding in srcCurveBindings)
                {
                    // if this curve belongs to this character
                    if (srcBinding.path.IndexOf(rootName) == 0)
                    {
                        // copy this curve to a separated clip

                        var curve = AnimationUtility.GetEditorCurve(src, srcBinding);

                        var newBinding = srcBinding;

                        newBinding.path = UndoUnityNameClashRename(newBinding.path);

                        // if root is a character bone root
                        // they always have names starting with 000
                        if (rootName.IndexOf("000") == 0)
                            newBinding.path = newBinding.path.Replace(rootName, "000"); // all separated character bone root should be named 000

                        AnimationUtility.SetEditorCurve(characterClip, newBinding, curve);
                    }
                }

                // save new asset
                // root named "000_xxx" saved to filename_xxx.anim
                // root named "000" saved to filename_0.anim
                // root named "000 1" saved to filename_1.anim
                // root named "CameraN" saved to filename_CameraN.anim

                var clipName = rootName;
                clipName = clipName.Replace("000", "");
                clipName = clipName.Replace(" ", "_");
                if (clipName.Length == 0)
                    clipName = "0";

                if (clipName[0] != '_')
                    clipName = "_" + clipName;

                AssetDatabase.CreateAsset(characterClip, directory + "/" + fileName + clipName + ".anim");
            }
		}

        AssetDatabase.SaveAssets();
    }

    [MenuItem("Assets/XAnim/Extract Anim Clips", false, 0)]
    static void ExtractAnimClipMenuFunc()
	{
        TraverseCallback cb = (index, sum, path) =>
        {
            ExtractAnimClipFromModelFile(path, Path.GetDirectoryName(path));

            EditorUtility.DisplayProgressBar("Extract Anim Clips", "Extracting from " + path, (float)index / Mathf.Max(1, (float)sum - 1));
        };

        TraverseSelectedAssets("t:AnimationClip", cb);

        EditorUtility.ClearProgressBar();
    }

    [MenuItem("Assets/XAnim/Extract Separated Anim Clips", false, 0)]
    static void ExtractSeparatedAnimClipMenuFunc()
    {
        TraverseCallback cb = (index, sum, path) =>
        {
            ExtractSeparatedAnimClipFromModelFile(path, Path.GetDirectoryName(path));

            EditorUtility.DisplayProgressBar("Extract Separated Anim Clips", "Extracting from " + path, (float)index / Mathf.Max(1, (float)sum - 1));
        };

        TraverseSelectedAssets("t:AnimationClip", cb);

        EditorUtility.ClearProgressBar();
    }

    [MenuItem("Assets/XAnim/Delete Anim Clips", false, 0)]
    static void DeleteAnimClipsMenuFunc()
    {
        if (!EditorUtility.DisplayDialog("Deleting Anim Clips", "You are deleting anim clips, are you sure?", "Delete Anim Clips", "Cancel"))
            return;

        TraverseCallback cb = (index, sum, path) =>
        {
            if (Path.GetExtension(path).ToLower() == ".anim")
                AssetDatabase.DeleteAsset(path);

            EditorUtility.DisplayProgressBar("Deleting Anim Clips", "Deleting " + path, (float)index / Mathf.Max(1, (float)sum - 1));
        };

        TraverseSelectedAssets("t:AnimationClip", cb);

        EditorUtility.ClearProgressBar();
    }

    [MenuItem("Assets/XAnim/Delete Model(Fbx & Max) Files", false, 0)]
    static void DeleteFbxFilesMenuFunc()
    {
        if (!EditorUtility.DisplayDialog("Deleting Model Files", "You are deleting model files, are you sure?", "Delete Model Files", "Cancel"))
            return;

        TraverseCallback cb = (index, sum, path) =>
        {
            var ext = Path.GetExtension(path).ToLower();

            if (ext == ".fbx"|| ext == ".max")
                AssetDatabase.DeleteAsset(path);

            EditorUtility.DisplayProgressBar("Deleting Model Files", "Deleting " + path, (float)index / Mathf.Max(1, (float)sum - 1));
        };

        TraverseSelectedAssets("t:AnimationClip", cb);

        EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// Trim 1 underscore and leading chars: aaa_bbb -> bbb
    /// </summary>
    [MenuItem("Assets/XAnim/Trim Anim Clips Name", false, 0)]
    static void TrimAnimClipsNameMenuFunc()
    {
        TraverseCallback cb = (index, sum, path) =>
        {
            if (Path.GetExtension(path).ToLower() == ".anim")
            {
                var filename = Path.GetFileName(path);

                var indexOfUnderScore = filename.IndexOf('_');
                if (indexOfUnderScore > 0)
                    filename = filename.Substring(indexOfUnderScore + 1);

                var error = AssetDatabase.RenameAsset(path, filename);
                AssetDatabase.SaveAssets();
            }

            EditorUtility.DisplayProgressBar("Trimming Anim Clip Names", "Trimming " + path, (float)index / Mathf.Max(1, (float)sum - 1));
        };

        TraverseSelectedAssets("t:AnimationClip", cb);

        EditorUtility.ClearProgressBar();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
#endif
