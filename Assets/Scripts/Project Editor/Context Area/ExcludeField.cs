using JSONClasses;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ExcludeField : ConfigActor
{
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform entryTarget;
    [SerializeField] GameObject dropIndicator;
    [SerializeField] GameObject emptyText;

    protected override void OnContextChange()
    {
        Context.OnNodeContentChange.AddListener(() =>
        {
            emptyText.SetActive(Context.currentNodeContent.excludes.Count <= 0);

            foreach (Transform child in entryTarget)
            {
                Destroy(child.gameObject);
            }
            foreach (string exclude in Context.currentNodeContent.excludes)
            {
                GameObject go = Instantiate(entryPrefab, entryTarget);
                TMP_Text[] texts = go.GetComponentsInChildren<TMP_Text>();
                texts[0].text = exclude;
                texts[1].text = "";
                Destroy(go.GetComponentsInChildren<Button>()[1].gameObject);
            }
        });
    }


    private void Start()
    {
        dropIndicator.SetActive(false);
    }

    public void DragableBegin(Vector2 pos, GameObject gameObject)
    {
        dropIndicator.SetActive(true);
    }
    public void DragableFinish(Vector2 pos, GameObject gameObject)
    {
        MergeSubject mergeSubject = gameObject.GetComponent<MergeSubjectContainer>().mergeSubject;
        RectTransform rectTransform = transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, null, out Vector2 actualLocalPos);

        dropIndicator.SetActive(false);

        if (!rectTransform.rect.Contains(actualLocalPos)) return;

        Context.editor.ExecuteCommand(new CDExcludeCommand(mergeSubject, false));
    }
}
