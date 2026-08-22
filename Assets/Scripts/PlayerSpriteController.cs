using UnityEngine;

/// <summary>
/// プレイヤーの向き管理とスプライト切り替えを行うコンポーネント。
/// 4方向スプライト（Back, Front, Right, LeftはRightを反転）を管理し、
/// 表示順序（sortingOrder）は Y 座標で更新します。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteController : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite backSprite;
    public Sprite frontSprite;
    public Sprite rightSprite;

    [Header("Sorting")]
    [Tooltip("sortingOrder = baseOrder + (-gridY) の形で奥行きを表現します")]
    public int baseSortingOrder = 100;

    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 方向ベクトル（Grid 単位。例: Vector2Int.up）に応じてスプライトを切り替えます。
    /// 左向きは rightSprite を左右反転して表示します。
    /// </summary>
    public void SetFacing(Vector2Int dir)
    {
        if (dir == Vector2Int.zero) return;

        if (dir.y > 0) // 上（後ろ）
        {
            sr.sprite = backSprite;
            sr.flipX = false;
        }
        else if (dir.y < 0) // 下（前）
        {
            sr.sprite = frontSprite;
            sr.flipX = false;
        }
        else if (dir.x > 0) // 右
        {
            sr.sprite = rightSprite;
            sr.flipX = false;
        }
        else if (dir.x < 0) // 左（右の反転）
        {
            sr.sprite = rightSprite;
            sr.flipX = true;
        }
    }

    /// <summary>
    /// Grid の Y 値を受け取り、sortingOrder を更新します。
    /// GridPosition.y が高い（上にいる）ほど小さい order にすることで奥行きを表現します>
    /// </summary>
    public void UpdateSortingOrder(int gridY)
    {
        sr.sortingOrder = baseSortingOrder - gridY;
    }
}