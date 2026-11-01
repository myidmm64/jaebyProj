using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Stage))]
public class StageEditor : Editor
{
    Vector2 scroll;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "enabledCells", "gridDataSize");
        serializedObject.ApplyModifiedProperties();
        serializedObject.Update();

        int size = serializedObject.FindProperty("mapSize").intValue;
        var cells = serializedObject.FindProperty("enabledCells");
        if (cells.arraySize != size * size) return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("맵 그리드", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("체크 = 사용 가능. 왼쪽 아래가 (0, 0)입니다.", MessageType.None);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전체 체크")) Fill(cells, true);
                if (GUILayout.Button("전체 해제")) Fill(cells, false);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(320));
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(28);
                for (int x = 0; x < size; x++) GUILayout.Label(x.ToString(), GUILayout.Width(24));
            }
            for (int y = size - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(y.ToString(), GUILayout.Width(24));
                    for (int x = 0; x < size; x++)
                    {
                        var cell = cells.GetArrayElementAtIndex(x + y * size);
                        cell.boolValue = GUILayout.Toggle(cell.boolValue,
                            new GUIContent("", $"({x}, {y})"), GUILayout.Width(24));
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        if (serializedObject.ApplyModifiedProperties()) SceneView.RepaintAll();
    }

    static void Fill(SerializedProperty cells, bool enabled)
    {
        for (int i = 0; i < cells.arraySize; i++) cells.GetArrayElementAtIndex(i).boolValue = enabled;
    }
}
