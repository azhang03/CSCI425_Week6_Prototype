using UnityEngine;
using UnityEngine.Tilemaps;

public class StageVisuals : MonoBehaviour
{
    [Header("References")]
    public TilemapRenderer tilemapRenderer;
    public SpriteRenderer  circleStageRenderer;
    public Tilemap         stageTilemap;

    void Start()
    {
        int index = SceneFlowManager.Instance != null
            ? SceneFlowManager.Instance.SelectedVariant
            : 0;
        ApplyVariant(index);
    }

    void ApplyVariant(int index)
    {
        if (tilemapRenderer == null || circleStageRenderer == null) return;

        Sprite[] variants = SceneFlowManager.Instance != null
            ? SceneFlowManager.Instance.stageVariants
            : null;

        bool useCircle = index > 0 && variants != null && index - 1 < variants.Length;

        tilemapRenderer.enabled = !useCircle;

        if (useCircle)
        {
            circleStageRenderer.sprite = variants[index - 1];
            ScaleToMatchTilemap();
        }

        circleStageRenderer.enabled = useCircle;
    }

    void ScaleToMatchTilemap()
    {
        if (stageTilemap == null || circleStageRenderer.sprite == null) return;

        BoundsInt cellBounds = stageTilemap.cellBounds;
        Vector3   min        = stageTilemap.CellToWorld(cellBounds.min);
        Vector3   max        = stageTilemap.CellToWorld(cellBounds.max);
        float     radius     = Mathf.Min((max.x - min.x) * 0.5f, (max.y - min.y) * 0.5f);
        float     naturalSize = circleStageRenderer.sprite.rect.width
                                / circleStageRenderer.sprite.pixelsPerUnit;
        float     scale      = (radius * 2f) / naturalSize;
        circleStageRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
