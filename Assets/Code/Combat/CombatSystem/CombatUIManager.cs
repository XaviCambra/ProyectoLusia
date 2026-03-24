using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static CombatTurnManager;

public class CombatUIManager : MonoBehaviour
{
    [Header("Prefab del icono de personaje")]
    [SerializeField] private GameObject iconPrefab;

    [Header("Panel de acciones")]
    [SerializeField] private GameObject actionPanel;

    [Header("Indicador de target")]
    [SerializeField] private GameObject targetPanel;

    [Header("Panel de orden de turnos")]
    [SerializeField] private GameObject turnOrderPanel;


    private List<GameObject> icons = new List<GameObject>();

    public System.Action<Character> OnCharacterClicked;

    public void BuildUI(IReadOnlyList<CharacterTurn> turnOrder)
    {
        // Limpiar iconos previos
        foreach (var icon in icons)
            Destroy(icon);

        icons.Clear();

        // Crear iconos nuevos
        foreach (var ct in turnOrder)
        {
            GameObject icon = Instantiate(iconPrefab, turnOrderPanel.transform);
            icons.Add(icon);

            // Asignar sprite
            icon.GetComponent<Image>().sprite = ct.m_Character.sprite;

            // Registrar clic
            var clickable = icon.GetComponent<ImagenClickable>();
            clickable.onClicked = () => OnCharacterClicked?.Invoke(ct.m_Character);
        }
    }

    public void UpdateUI(ETurnState state)
    {
        actionPanel.SetActive(false);
        targetPanel.SetActive(false);
        turnOrderPanel.SetActive(false);

        switch (state)
        {
            case ETurnState.PickAction:
                actionPanel.SetActive(true);
                break;

            case ETurnState.PickTarget:
                targetPanel.SetActive(true);
                break;

            case ETurnState.Execute:
                // Nada por ahora
                break;
        }
    }

    private void ReorderIcons(IReadOnlyList<CharacterTurn> turnOrder)
    {
        List<GameObject> newOrder = new List<GameObject>();

        foreach (var ct in turnOrder)
        {
            GameObject icon = icons.Find(i =>
                i.GetComponent<Image>().sprite == ct.m_Character.sprite);

            newOrder.Add(icon);
        }

        icons = newOrder;

        // Cambiar el orden en la jerarquía
        for (int i = 0; i < icons.Count; i++)
            icons[i].transform.SetSiblingIndex(i);
    }

    public void AnimateReorder(IReadOnlyList<CharacterTurn> turnOrder, System.Action onFinished)
    {
        // Mostrar barra de turnos
        turnOrderPanel.SetActive(true);

        // Iniciar la secuencia con delay
        StartCoroutine(AnimateReorderSequence(turnOrder, onFinished));
    }

    private IEnumerator AnimateReorderSequence(IReadOnlyList<CharacterTurn> turnOrder, System.Action onFinished)
    {
        // Delay antes de animar (ajusta a tu gusto)
        yield return new WaitForSeconds(0.6f);

        // 1. Guardar posiciones iniciales
        Dictionary<GameObject, Vector3> initialPositions = new();
        foreach (var icon in icons)
            initialPositions[icon] = icon.transform.localPosition;

        // 2. Reordenar iconos
        ReorderIcons(turnOrder);

        // 3. Forzar layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(turnOrderPanel.transform as RectTransform);

        // 4. Guardar posiciones finales
        Dictionary<GameObject, Vector3> finalPositions = new();
        foreach (var icon in icons)
            finalPositions[icon] = icon.transform.localPosition;

        // 5. Desactivar layout
        var layout = turnOrderPanel.transform.GetComponent<GridLayoutGroup>();
        layout.enabled = false;

        // 6. Animar iconos
        foreach (var icon in icons)
        {
            icon.transform.localPosition = initialPositions[icon];
            icon.GetComponent<UISlide>().SetTarget(finalPositions[icon]);
        }

        // 7. Esperar a que termine la animación
        yield return new WaitForSeconds(1.5f);

        // 8. Ocultar barra
        turnOrderPanel.SetActive(false);

        // 9. Callback al CombatManager
        onFinished?.Invoke();

        // 10. Reactivar layout
        layout.enabled = true;
    }
}
