using UnityEngine;

public class FOVManager : MonoBehaviour
{
    public static FOVManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// プレイヤー位置と部屋フラグから視界を再計算するフック。
    /// 実際の可視化（Tilemap 操作など）は後で実装します。
    /// </summary>
    public void RecomputeFOV(Vector2Int playerGrid, bool isInRoom)
    {
        // TODO: 実際の FOV ロジックをここに実装
        Debug.Log($"RecomputeFOV called. player={playerGrid}, isInRoom={isInRoom}");
    }
}