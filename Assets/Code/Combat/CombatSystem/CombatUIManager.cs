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

    public System.Action<CombatCharacter> OnCharacterClicked;

    public void BuildUI(IReadOnlyList<CharacterTurn> turnOrder)
    {
        foreach (var icon in icons)
            Destroy(icon);

        icons.Clear();
        foreach (var ct in turnOrder)
        {
            GameObject icon = Instantiate(iconPrefab, turnOrderPanel.transform);
            icons.Add(icon);
            icon.GetComponent<Image>().sprite = ct.m_Character.GetCharacter().sprite;
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
                i.GetComponent<Image>().sprite == ct.m_Character.GetCharacter().sprite);

            newOrder.Add(icon);
        }

        icons = newOrder;

        for (int i = 0; i < icons.Count; i++)
            icons[i].transform.SetSiblingIndex(i);
    }

    public void AnimateReorder(IReadOnlyList<CharacterTurn> turnOrder, System.Action onFinished)
    {
        turnOrderPanel.SetActive(true);
        StartCoroutine(AnimateReorderSequence(turnOrder, onFinished));
    }

    private IEnumerator AnimateReorderSequence(IReadOnlyList<CharacterTurn> turnOrder, System.Action onFinished)
    {
        yield return new WaitForSeconds(0.6f);

        Dictionary<GameObject, Vector3> initialPositions = new();
        foreach (var icon in icons)
            initialPositions[icon] = icon.transform.localPosition;

        ReorderIcons(turnOrder);
        LayoutRebuilder.ForceRebuildLayoutImmediate(turnOrderPanel.transform as RectTransform);

        Dictionary<GameObject, Vector3> finalPositions = new();
        foreach (var icon in icons)
            finalPositions[icon] = icon.transform.localPosition;

        var layout = turnOrderPanel.transform.GetComponent<GridLayoutGroup>();
        layout.enabled = false;

        foreach (var icon in icons)
        {
            icon.transform.localPosition = initialPositions[icon];
            icon.GetComponent<UISlide>().SetTarget(finalPositions[icon]);
        }

        yield return new WaitForSeconds(1.5f);

        turnOrderPanel.SetActive(false);
        onFinished?.Invoke();
        layout.enabled = true;
    }
}
