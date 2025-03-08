using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using System.Linq;

public class TimelineAssetPath : EditorWindow
{
    private TimelineAsset _timelineAsset;
    private string[] _dependencies;
    private Vector2 _scrollPos = Vector2.zero;
    private Dictionary<string, List<string>> _assetDependencies = new Dictionary<string, List<string>>();

    [MenuItem("Window/TimelineAssetPath")]
    public static void ShowWindow()
    {
        GetWindow<TimelineAssetPath>("TimelineAssetPath");
    }

    private void OnGUI()
    {
        _timelineAsset = (TimelineAsset)EditorGUILayout.ObjectField("Timeline Asset", _timelineAsset, typeof(TimelineAsset), false);
        if(GUILayout.Button("出力"))
        {
            // AssetDatabase.GetDependenciesを使う方法
            //_dependencies = GetDependencies(_timelineAsset);

            // Clipから参照を調べる方法
            GetDependencies(_timelineAsset);
        }

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        // AssetDatabase.GetDependenciesを使う方法
        //for (int i = 0; i < _dependencies.Length; i++)
        //{
        //    GUILayout.Label(_dependencies[i]);
        //}

        // Clipから参照を調べる方法
        foreach (var asset in _assetDependencies)
        {
            var assetName = asset.Key;
            GUILayout.Label(assetName, EditorStyles.boldLabel);

            foreach (var dependency in asset.Value)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label(dependency);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// AssetDatabase.GetDependenciesを使う方法
    /// timelineAssetが参照してるアセットを取得する
    /// </summary>
    //private string[] GetDependencies(TimelineAsset timelineAsset)
    //{
    //    var assetPath = AssetDatabase.GetAssetPath(timelineAsset);
    //    var dependencies = AssetDatabase.GetDependencies(assetPath, true);
    //    return dependencies;
    //}

    /// <summary>
    /// Clipから参照を調べる方法
    /// Clipから参照を取得する
    /// </summary>
    private void GetDependencies(TimelineAsset timelineAsset)
    {
        // TimelineにあるTrackの数だけループする
        foreach (var track in timelineAsset.GetOutputTracks())
        {
            GetDependenciesFromTrack(track);
        }
    }

    /// <summary>
    /// Clipから参照を調べる方法
    /// TrackのからClipを探し、参照を調べる
    /// </summary>
    private void GetDependenciesFromTrack(TrackAsset trackAsset)
    {
        // Clipの数だけループする
        foreach (var clip in trackAsset.GetClips())
        {
            // シリアライズされたオブジェクト、プロパティを探し、その数だけループさせる
            SerializedObject serializedObject = new SerializedObject(clip.asset);
            SerializedProperty prop = serializedObject.GetIterator();
            while (prop.NextVisible(true))
            {
                // プロパティの種類がUnityEngine.Objectのものだけ調べます。
                if(prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    // プロパティとしてついているスクリプトは参照を調べなくてよいので(今回は)分岐します
                    Object referencedObject = prop.objectReferenceValue;
                    if(referencedObject != null && referencedObject.GetType() != typeof(MonoScript))
                    {
                        // GetDependenciesを使用してプロパティに入れられたアセットの参照を取得します。
                        var assetPath = AssetDatabase.GetAssetPath(referencedObject);
                        // .csは参照から除きます。(今回は)
                        var dependencies = AssetDatabase.GetDependencies(assetPath, true).Where(dependency => !dependency.EndsWith(".cs")).ToList();
                        var assetName = referencedObject.name;
                        if(!_assetDependencies.ContainsKey(assetName))
                        {
                            _assetDependencies[assetName] = dependencies;
                        }
                    }
                }
            }

            // SubTrackに関して再帰的に処理します。
            foreach (var subTrack in trackAsset.GetChildTracks())
            {
                GetDependenciesFromTrack(subTrack);
            }
        }
    }
}
