using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(GameBalanceSettings))]
public sealed class GameBalanceSettingsEditor : Editor
{
    private SerializedProperty specialTowerEvolutionStats;
    private SerializedProperty towerVisualScaleMultipliers;
    private ReorderableList towerProfilesList;
    private ReorderableList towerVisualScalesList;

    private void OnEnable()
    {
        specialTowerEvolutionStats = serializedObject.FindProperty("specialTowerEvolutionStats");
        towerVisualScaleMultipliers = serializedObject.FindProperty("towerVisualScaleMultipliers");
        if (specialTowerEvolutionStats == null)
        {
            return;
        }

        towerProfilesList = new ReorderableList(serializedObject, specialTowerEvolutionStats,
            true, true, true, true);
        towerProfilesList.drawHeaderCallback = rect =>
            EditorGUI.LabelField(rect, "Tower Skill Profiles", EditorStyles.boldLabel);
        towerProfilesList.elementHeightCallback = index =>
        {
            SerializedProperty element = specialTowerEvolutionStats.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + EditorGUIUtility.standardVerticalSpacing;
        };
        towerProfilesList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = specialTowerEvolutionStats.GetArrayElementAtIndex(index);
            SerializedProperty towerType = element.FindPropertyRelative("towerType");
            string label = GetTowerLabel(towerType);
            rect.y += 1f;
            rect.height = EditorGUI.GetPropertyHeight(element, true);
            EditorGUI.PropertyField(rect, element, new GUIContent(label), true);
        };
        towerProfilesList.onAddCallback = list =>
        {
            int index = specialTowerEvolutionStats.arraySize;
            specialTowerEvolutionStats.arraySize++;
            SerializedProperty element = specialTowerEvolutionStats.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("towerType").enumValueIndex = 0;
            element.isExpanded = true;
        };

        towerVisualScalesList = CreateTowerList(towerVisualScaleMultipliers, "Tower Visual Scale Multipliers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "specialTowerEvolutionStats",
            "towerVisualScaleMultipliers");
        EditorGUILayout.Space(6f);
        towerProfilesList?.DoLayoutList();
        EditorGUILayout.Space(6f);
        towerVisualScalesList?.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private static ReorderableList CreateTowerList(SerializedProperty property, string header)
    {
        if (property == null)
        {
            return null;
        }

        ReorderableList list = new ReorderableList(property.serializedObject, property,
            true, true, true, true);
        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, header, EditorStyles.boldLabel);
        list.elementHeightCallback = index =>
        {
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + EditorGUIUtility.standardVerticalSpacing;
        };
        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            SerializedProperty towerType = element.FindPropertyRelative("towerType");
            rect.y += 1f;
            rect.height = EditorGUI.GetPropertyHeight(element, true);
            EditorGUI.PropertyField(rect, element,
                new GUIContent(GetTowerLabel(towerType)), true);
        };
        list.onAddCallback = addedList =>
        {
            int index = property.arraySize;
            property.arraySize++;
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("towerType").enumValueIndex = 0;
            element.isExpanded = true;
        };
        return list;
    }

    private static string GetTowerLabel(SerializedProperty towerType)
    {
        if (towerType == null || towerType.enumNames == null || towerType.enumNames.Length == 0)
        {
            return "Unassigned Tower";
        }

        int index = Mathf.Clamp(towerType.enumValueIndex, 0, towerType.enumNames.Length - 1);
        string towerName = towerType.enumNames[index];
        return towerName == nameof(SpecialTowerType.None)
            ? "Unassigned Tower"
            : ObjectNames.NicifyVariableName(towerName);
    }
}
