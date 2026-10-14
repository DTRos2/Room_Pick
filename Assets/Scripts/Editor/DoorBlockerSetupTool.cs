using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 여닫이문(sm_door, sm_door.001 ...)이 있는 문 구멍을 보이지 않는 BoxCollider로 채운다.
/// 카메라가 문틀에 끼거나 구멍을 지나 밖으로 나가지 못하게 하려는 것이다.
/// Collider는 문과 별개의 오브젝트(DoorBlockers 아래)라서 문이 열리고 닫혀도 움직이지 않는다.
/// 닫힌 자세 기준으로 만들므로, 씬에 열린 자세로 놓인 문(AutoDoor.authoredPoseIsOpen)은 건너뛴다.
/// 메뉴: RoomPick/문 구멍 Collider 설정
/// </summary>
public static class DoorBlockerSetupTool
{
    private const string RootName = "DoorBlockers";
    private const string BlockerSuffix = "_Blocker";
    private const float MinThickness = 0.3f;

    private static readonly Regex DoorName = new Regex(@"^sm_door(\.\d+)?$");
    private static readonly string[] ExcludedDoors = { "sm_door.003" };

    [MenuItem("RoomPick/문 구멍 Collider 설정")]
    private static void Setup()
    {
        GameObject root = GameObject.Find(RootName);
        int added = 0;

        foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            string doorName = renderer.name;
            if (!DoorName.IsMatch(doorName)) continue;
            if (System.Array.IndexOf(ExcludedDoors, doorName) >= 0) continue;

            string blockerName = doorName + BlockerSuffix;
            if (root != null && root.transform.Find(blockerName) != null) continue;

            AutoDoor autoDoor = renderer.GetComponent<AutoDoor>();
            if (autoDoor != null && IsAuthoredOpen(autoDoor))
            {
                Debug.LogWarning($"{doorName}: 열린 자세로 놓인 문이라 건너뜁니다.", renderer);
                continue;
            }

            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "문 구멍 Collider 생성");
            }

            Bounds bounds = renderer.bounds;
            Vector3 size = bounds.size;
            // 문의 얇은 쪽(두께 방향)을 벽 두께쯤으로 늘려, 문틀과 벽 사이로 비집고 들어가지 못하게 한다.
            if (size.x < size.z) size.x = Mathf.Max(size.x, MinThickness);
            else size.z = Mathf.Max(size.z, MinThickness);

            GameObject blocker = new GameObject(blockerName);
            Undo.RegisterCreatedObjectUndo(blocker, "문 구멍 Collider 생성");
            blocker.transform.SetParent(root.transform, false);
            blocker.transform.position = bounds.center;

            BoxCollider box = blocker.AddComponent<BoxCollider>();
            box.size = size;
            added++;
        }

        if (added > 0) EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"문 구멍 Collider 설정 완료: {added}개 추가. 씬을 저장하세요.");
    }

    private static bool IsAuthoredOpen(AutoDoor door)
    {
        SerializedProperty property = new SerializedObject(door).FindProperty("authoredPoseIsOpen");
        return property != null && property.boolValue;
    }
}
