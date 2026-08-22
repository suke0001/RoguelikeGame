using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// グリッド単位でのプレイヤー移動を扱うコントローラ（ターン毎に1マス移動、移動中は入力を受けない）。
/// Map の当たり判定や FOV 更新は MapManager / FOVManager に委譲する作りになっています（下記コメント参照）。
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("タイル1マスのサイズ（World 単位）。Tilemap の Cell Size に合わせてください。")]
    public float tileSize = 1f;

    [Tooltip("移動アニメーションにかかる時間（秒）。0 にすると瞬間移動。")]
    public float moveDuration = 0.12f;

    [Header("初期位置（グリッド座標）")]
    public Vector2Int startGridPosition = Vector2Int.zero;

    // 現在のグリッド座標（外部から参照・保存可能）
    public Vector2Int GridPosition { get; private set; }

    // 移動中フラグ
    private bool isMoving = false;

    // C# イベントで移動を通知（ゲームターン進行に使えます）
    public static event Action<Vector2Int> OnPlayerMoved;

    // Unity Inspector からも反応させたい場合用（引数なしで移動検知）
    public UnityEvent onMovedUnityEvent;

    private void Start()
    {
        GridPosition = startGridPosition;
        SnapToGrid();
    }

    private void Update()
    {
        if (isMoving) return;

        // 入力（四方向）。必要ならここを拡張して斜めやキーボード設定を変える
        Vector2Int dir = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = Vector2Int.right;

        if (dir != Vector2Int.zero)
        {
            TryMove(dir);
        }
    }

    private void TryMove(Vector2Int dir)
    {
        if (isMoving) return;

        Vector2Int target = GridPosition + dir;

        // MapManager がある場合は通行可能か問い合わせる（推奨）
        bool walkable = true;
        bool hasMapManager = MapManager.Instance != null;
        if (hasMapManager)
        {
            walkable = MapManager.Instance.IsWalkable(target);
        }
        else
        {
            // MapManager が無ければレイヤー判定で簡易チェック（オプション）
            // ここはプロジェクトの設計に合わせて調整してください。
        }

        if (walkable)
        {
            StartCoroutine(MoveTo(target));
        }
        else
        {
            // ぶつかる音やアニメーションを入れるならここ
        }
    }

    private IEnumerator MoveTo(Vector2Int targetGrid)
    {
        isMoving = true;

        Vector3 startWorld = transform.position;
        Vector3 endWorld = GridToWorld(targetGrid);

        if (moveDuration <= 0f)
        {
            transform.position = endWorld;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                // イージングを入れたい場合はここを変更
                transform.position = Vector3.Lerp(startWorld, endWorld, t);
                yield return null;
            }
            transform.position = endWorld;
        }

        GridPosition = targetGrid;

        // 移動後の処理：ターン進行通知、FOV 更新など
        OnPlayerMoved?.Invoke(GridPosition);
        onMovedUnityEvent?.Invoke();

        // FOV 更新（MapManager から TileType を取得して判定する想定）
        if (FOVManager.Instance != null)
        {
            bool isInRoom = false;
            if (MapManager.Instance != null)
            {
                TileType tt = MapManager.Instance.GetTileType(GridPosition);
                isInRoom = (tt == TileType.Room || tt == TileType.Floor /* adjust as needed */);
            }
            FOVManager.Instance.RecomputeFOV(GridPosition, isInRoom);
        }

        isMoving = false;
        yield break;
    }

    private Vector3 GridToWorld(Vector2Int grid)
    {
        // Z は元の Z を維持
        return new Vector3(grid.x * tileSize, grid.y * tileSize, transform.position.z);
    }

    private void SnapToGrid()
    {
        transform.position = GridToWorld(GridPosition);
    }

    // public API：外部から直接移動させたい（テレポート等）
    public void TeleportTo(Vector2Int grid)
    {
        GridPosition = grid;
        SnapToGrid();

        OnPlayerMoved?.Invoke(GridPosition);
        onMovedUnityEvent?.Invoke();

        if (FOVManager.Instance != null)
        {
            bool isInRoom = MapManager.Instance != null && MapManager.Instance.GetTileType(GridPosition) == TileType.Room;
            FOVManager.Instance.RecomputeFOV(GridPosition, isInRoom);
        }
    }
}

/// <summary>
/// 以下はこの PlayerController が依存する外部コンポーネントの想定インターフェース（プロジェクトに合わせて実装してください）。
/// 例：Assets/Scripts/MapManager.cs, Assets/Scripts/FOVManager.cs に相当する機能を提供すること。
/// </summary>
public enum TileType { Wall, Floor, Room, Corridor, Stairs }

public class MapManager : MonoBehaviour
{
    // シングルトン参照（プロジェクト内で実装）
    public static MapManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // 指定グリッドが通行可能か
    public bool IsWalkable(Vector2Int grid)
    {
        // 実装：範囲チェック、タイルタイプ判定、敵や障害物の有無など
        return true;
    }

    // タイルタイプを返す（Room / Corridor 判定に使います）
    public TileType GetTileType(Vector2Int grid)
    {
        return TileType.Floor;
    }
}

public class FOVManager : MonoBehaviour
{
    public static FOVManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // isInRoom が true のときは部屋全体 + 周囲1マス、false のときは周囲1マスのみを表示するなどを実装する
    public void RecomputeFOV(Vector2Int playerGrid, bool isInRoom)
    {
        // 実装してください
    }
}