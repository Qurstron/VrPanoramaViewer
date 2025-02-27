using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Modal : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Transform buttons;
    [SerializeField] private View view;
    private static Modal modal = null;

    public class Choice
    {
        public string name;
        public UnityAction callback;
        public Color color;

        public Choice(string name, UnityAction callback)
        {
            this.name = name;
            this.callback = callback;
            this.color = Color.white;
        }
        public Choice(string name, UnityAction callback, Color color)
        {
            this.name = name;
            this.callback = callback;
            this.color = color;
        }
    }

    public static void SetModal(string title, string description, params Choice[] choices)
    {
        modal.title.text = title;
        modal.description.text = description;

        foreach (Transform child in modal.buttons)
        {
            Destroy(child.gameObject);
        }
        foreach (var choice in choices)
        {
            Button button = Instantiate(modal.buttonPrefab, modal.buttons).GetComponentInChildren<Button>();
            ColorBlock colorBlock = ColorBlock.defaultColorBlock;

            colorBlock.normalColor = choice.color;
            button.colors = colorBlock;
            button.gameObject.GetComponentInChildren<TMP_Text>().text = choice.name;
            button.onClick.AddListener(() => modal.view.UnDisplay());
            button.onClick.AddListener(choice.callback);
        }

        modal.view.Display();
    }

    private void Awake()
    {
        if (modal != null) throw new Exception("Modal already created");
        modal = this;
    }
}
