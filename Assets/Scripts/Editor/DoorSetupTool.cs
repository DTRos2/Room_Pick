using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 여닫이문(sm_door, sm_door.001 ...)에 AutoDoor를 붙인다. 문 틀(sm_door_frame*)과 붙박이장 문(sm_closet_door*)은 제외한다.
/// 문이 실행 중에 움직일 수 있도록 Static 설정도 함께 해제한다.
/// 메뉴: RoomPick/자동문 설정
/// </summary>
public static class DoorSetupTool
{
    private static readonly Regex DoorName = new Regex(@"^sm_door(\.\d+)?$");

    [MenuItem("RoomPick/자동문 설정")]
    private static void Setup()
    {
        int added = 0;
        foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            GameObject door = renderer.gameObject;
            if (!DoorName.IsMatch(door.name)) continue;
            if (door.GetComponent<AutoDoor>() != null) continue;

            Undo.AddComponent<AutoDoor>(door);
            Undo.RecordObject(door, "문 Static 해제");
            GameObjectUtility.SetStaticEditorFlags(door, 0);
            EditorSceneManager.MarkSceneDirty(door.scene);
            added++;
        }

        Debug.Log($"자동문 설정 완료: AutoDoor {added}개 추가. 씬을 저장하세요.");
    }
}
