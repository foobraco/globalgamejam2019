using UnityEngine;
using Cinemachine;

public class Item : MonoBehaviour
{

    public enum Type
    {
        Breakable,
        NotBreakable
    }

    [SerializeField]
    private Type itemType;
    [SerializeField]
    private SpriteRenderer spriteToActivate;
    [SerializeField]
    private Explodable objectToExplode;
    [SerializeField]
    private CinemachineVirtualCamera cameraToTransition;

    private bool isInDroppingArea;

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

    public void ReturnItem()
    {
        spriteToActivate.enabled = true;
        objectToExplode.explode();
        cameraToTransition.gameObject.SetActive(true);
        transform.parent.GetComponent<PlayerController2D>().isCarryingItem = false;
        Invoke("DeactivateCamera", 6f);
    }

    private void DeactivateCamera()
    {
        cameraToTransition.gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void Explode()
    {
        GetComponent<Explodable>().explode();
    }

    private void OnTriggeEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("DroppingArea"))
        {
            ReturnItem();
        }
    }
}
