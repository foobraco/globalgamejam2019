using UnityEngine;

public class Item : MonoBehaviour
{

    public enum Type
    {
        Breakable,
        NotBreakable
    }

    [SerializeField]
    private Type itemType;

    Rigidbody2D rigidbody2D;
    SpriteRenderer spriteRenderer;

    private void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void ReleaseItem()
    {
        if (itemType == Type.Breakable)
        {
            spriteRenderer.enabled = true;
            transform.parent = null;
            Invoke("Explode", 0.2f);
        }
        if (itemType == Type.NotBreakable)
        {
            spriteRenderer.enabled = true;
            transform.parent = null;
        }
    }

    private void Explode()
    {
        GetComponent<Explodable>().explode();
    }
}
