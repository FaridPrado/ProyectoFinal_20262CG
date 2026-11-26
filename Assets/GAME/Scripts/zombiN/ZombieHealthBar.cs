using UnityEngine;
using UnityEngine.UI;

// Ponlo en el Canvas (World Space) que es hijo del zombie.
public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Zombie zombie;      // Si lo dejas vacío, lo busca en el padre
    [SerializeField] private Image fillImage;    // Imagen de relleno (Image Type = Filled)

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
        if (zombie == null) zombie = GetComponentInParent<Zombie>();
    }

    private void Start()
    {
        zombie.OnHealthChanged += UpdateBar;
        zombie.OnDied += Hide;
        UpdateBar(zombie.CurrentHealth, zombie.MaxHealth);
    }

    private void OnDestroy()
    {
        if (zombie == null) return;
        zombie.OnHealthChanged -= UpdateBar;
        zombie.OnDied -= Hide;
    }

    // La barra siempre mira a la cámara
    private void LateUpdate()
    {
        if (cam == null) return;
        transform.rotation = cam.transform.rotation;
    }

    private void UpdateBar(float current, float max)
    {
        fillImage.fillAmount = current / max;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
