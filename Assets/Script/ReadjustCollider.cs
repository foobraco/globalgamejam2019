using UnityEngine;

[ExecuteInEditMode]
public class ReadjustCollider : MonoBehaviour
{
    private SpriteRenderer sprite;
    private BoxCollider2D collider2D;

    void Awake()
    {
        runInEditMode = true;
        sprite = GetComponentInChildren<SpriteRenderer>();
        collider2D = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        collider2D.offset = new Vector2(0, 0);
        collider2D.size = new Vector3(sprite.bounds.size.x / transform.lossyScale.x,
                                     sprite.bounds.size.y / transform.lossyScale.y,
                                     sprite.bounds.size.z / transform.lossyScale.z);
    }
}

