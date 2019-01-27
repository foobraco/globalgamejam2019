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
        if (spriteToActivate != null)
        {
            spriteToActivate.enabled = true;

        }
        if (objectToExplode != null)
        {
            objectToExplode.explode();
        }
        if (cameraToTransition != null)
        {
            cameraToTransition.gameObject.SetActive(true);
        }
        transform.parent.GetComponent<PlayerController2D>().isCarryingItem = false;
        transform.parent.GetComponent<PlayerController2D>().ReturnNormalValues();
        Invoke("DeactivateCamera", 6f);
    }

    private void DeactivateCamera()
    {
        if (cameraToTransition != null)
        {
            cameraToTransition.gameObject.SetActive(false);
        }
        Destroy(gameObject);
    }

    private void Explode()
    {
        GetComponent<Explodable>().explode();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("it triggered with " + collision.name);
        if (collision.CompareTag("DroppingArea"))
        {
            ReturnItem();
        }
    }
}
