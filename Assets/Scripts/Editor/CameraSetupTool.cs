using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메인 카메라에 CameraController를 붙이고, 콜라이더가 없는 벽(sm_walls*)에 MeshCollider를 추가한다.
/// 메뉴: RoomPick/카메라 이동 설정
/// </summary>
public static class CameraSetupTool
{
    private const string WallNamePrefix = "sm_walls";

    [MenuItem("RoomPick/카메라 이동 설정")]
    private static void Setup()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("MainCamera 태그의 카메라를 찾을 수 없습니다.");
            return;
        }

        if (cam.GetComponent<CameraController>() == null)
        {
            Undo.AddComponent<CameraController>(cam.gameObject); // CharacterController도 함께 추가된다.
        }

        int added = 0;
        foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (!renderer.name.StartsWith(WallNamePrefix)) continue;
            if (renderer.GetComponent<Collider>() != null) continue;

            Undo.AddComponent<MeshCollider>(renderer.gameObject);
            added++;
        }

        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        Debug.Log($"카메라 이동 설정 완료: CameraController 부착, 벽 MeshCollider {added}개 추가");
    }
}
