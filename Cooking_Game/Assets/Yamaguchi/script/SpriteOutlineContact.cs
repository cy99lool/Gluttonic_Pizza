using UnityEngine;

public class SpriteOutlineContact : MonoBehaviour
{
    [Header("Outline Settings")]
    public Color outlineColor = Color.black;
    public float outlineSize = 0.05f;

    private SpriteRenderer mainRenderer;
    private SpriteRenderer[] outlines;
    private int contactCount = 0;

    Vector2[] dirs = new Vector2[]
    {
        new Vector2( 1, 0), new Vector2(-1, 0),
        new Vector2(0,  1), new Vector2(0, -1),
        new Vector2(1,  1), new Vector2(-1, 1),
        new Vector2(1, -1), new Vector2(-1,-1)
    };

    void Awake()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        CreateOutline();
        SetOutlineActive(false);
    }

    void CreateOutline()
    {
        outlines = new SpriteRenderer[dirs.Length];

        for (int i = 0; i < dirs.Length; i++)
        {
            GameObject obj = new GameObject("Outline_" + i);
            obj.transform.parent = transform;

            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = (Vector3)(dirs[i] * outlineSize);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = mainRenderer.sprite;
            sr.color = outlineColor;

            sr.sortingLayerID = mainRenderer.sortingLayerID;
            sr.sortingOrder = mainRenderer.sortingOrder - 1;

            outlines[i] = sr;
        }
    }

    void LateUpdate()
    {
        foreach (var o in outlines)
        {
            o.sprite = mainRenderer.sprite;
            o.flipX = mainRenderer.flipX;
            o.flipY = mainRenderer.flipY;

            o.sortingLayerID = mainRenderer.sortingLayerID;
            o.sortingOrder = mainRenderer.sortingOrder - 1;
        }
    }

    void SetOutlineActive(bool active)
    {
        foreach (var o in outlines)
            o.enabled = active;
    }

    // -------- CapsuleCollider2D (isTrigger ON) 用 --------

    void OnTriggerEnter2D(Collider2D col)
    {
        contactCount++;
        SetOutlineActive(true);
    }

    void OnTriggerExit2D(Collider2D col)
    {
        contactCount--;
        if (contactCount <= 0)
            SetOutlineActive(false);
    }
}
