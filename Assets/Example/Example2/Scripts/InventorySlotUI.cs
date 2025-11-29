using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;

    private int _slotIndex;
    private Inventory _inventory;
    private Canvas _parentCanvas;
    private Image _dragGhost; // Картинка, которая летает за мышкой

    public void Init(int index, Inventory inventory, Canvas canvas)
    {
        _slotIndex = index;
        _inventory = inventory;
        _parentCanvas = canvas;
    }

    public void UpdateSlot(InventorySlot slot)
    {
        if (slot.IsEmpty)
        {
            iconImage.gameObject.SetActive(false);
            amountText.text = "";
        }
        else
        {
            iconImage.gameObject.SetActive(true);
            iconImage.sprite = slot.item.icon;
            amountText.text = slot.count > 1 ? slot.count.ToString() : "";
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_inventory.Slots[_slotIndex].IsEmpty) return;

        _dragGhost = new GameObject("DragIcon").AddComponent<Image>();
        _dragGhost.transform.SetParent(_parentCanvas.transform);
        _dragGhost.sprite = iconImage.sprite;
        _dragGhost.raycastTarget = false; 
        _dragGhost.rectTransform.sizeDelta = new Vector2(50, 50); 

        iconImage.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragGhost != null)
        {

            _dragGhost.rectTransform.anchoredPosition += eventData.delta / _parentCanvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragGhost != null)
        {
            Destroy(_dragGhost.gameObject);
        }
        iconImage.color = Color.white;
    }

    public void OnDrop(PointerEventData eventData)
    {

        var otherSlotUI = eventData.pointerDrag?.GetComponent<InventorySlotUI>();

        if (otherSlotUI != null)
        {

            _inventory.MoveItem(otherSlotUI._slotIndex, _slotIndex);
        }
    }
}

