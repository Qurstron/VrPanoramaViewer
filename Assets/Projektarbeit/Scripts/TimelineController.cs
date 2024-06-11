using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using static JSONClasses;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(GameObject))]
public class TimelineController : MonoBehaviour
{
    public RectTransform group;
    public GameObject buttonPrefab;
    public ToggleGroup toggleGroup;
    public TMP_Dropdown dropdown;
    public PanoramaSphereController panoramaSphereController;
    public float buttonSize = .06f;

    private List<GameObject> buttons = new List<GameObject>();
    private Config currentConfig;
    private int currentCategoryIndex = 0;
    private int currentTimelineIndex = 0;
    private Pic currentPic;

    public Config Config { get { return currentConfig; } }

    //const string digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // Start is called before the first frame update
    void Start()
    {
        if (toggleGroup == null) toggleGroup = gameObject.GetComponent<ToggleGroup>();

        dropdown.onValueChanged.AddListener((i) =>
        {
            currentCategoryIndex = i;
            StateChanged();
        }); // currently not working
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // , Action<int> onClick
    public void Fill(Config config, bool tryKeepIndices = false)
    {
        Clear();
        currentConfig = config;

        gameObject.GetNamedChild("TimeLeft").GetComponent<TMP_Text>().text = getConvertedTime(config.pics.First().time);
        gameObject.GetNamedChild("TimeRight").GetComponent<TMP_Text>().text = getConvertedTime(config.pics.Last().time);
        var timeIndicator = gameObject.GetNamedChild("TimeFloaty");

        float globalOffset = group.rect.width / 2f;
        float offset = group.rect.width / (config.pics.Length - 1);
        int i = 0;
        foreach (Pic p in config.pics)
        {
            GameObject button = Instantiate(buttonPrefab, group);
            buttons.Add(button);
            button.transform.localScale = new Vector3(buttonSize, buttonSize, buttonSize);
            button.transform.localPosition = new Vector3(offset * i - globalOffset, 0, 0);
            TimelineButton timelineButton = button.AddComponent<TimelineButton>();
            timelineButton.timelineIndex = i;
            var b = button.GetComponentInChildren<Toggle>();
            b.onValueChanged.AddListener((a) => 
            {
                if (!a) return;

                //panoramaSphereController.changePanorama(i);
                timeIndicator.GetComponent<TMP_Text>().text = UnixTimeStampToDateTime(p.time).ToString(config.timeformat);
                timeIndicator.transform.position = button.transform.position;
                currentPic = p;
                currentTimelineIndex = timelineButton.timelineIndex;

                StateChanged();
            });
            button.GetComponentInChildren<Toggle>().group = toggleGroup;
            //Debug.Log(getConvertedTime(p.time));

            i++;
        }

        dropdown.options.AddRange(config.categorynames.Select(c => new TMP_Dropdown.OptionData(c)));

        if (!tryKeepIndices || currentCategoryIndex > config.categorynames.Length) currentCategoryIndex = 0;
        if (!tryKeepIndices || currentTimelineIndex >= config.pics.Length) currentTimelineIndex = 0;
        dropdown.value = currentCategoryIndex; // TODO: fix
        buttons[currentTimelineIndex].GetComponentInChildren<Toggle>().enabled = true;

        // trigger on start so the Panorama gets updated
        //StateChanged();
        return;
    }
    private void Clear()
    {
        foreach (GameObject go in buttons) 
        {
            Destroy(go);
        }
        buttons.Clear();

        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData("Main"));
        // TODO: test if it works without this line
        //dropdown.value = 0;
    }
    //private void CategoryChanged(int i)
    //{

    //}
    private void StateChanged()
    {
        string pName = currentPic.name;
        if (currentPic.categories == null || currentCategoryIndex >= currentPic.categories.Length)
        {
            panoramaSphereController.SetApperance(pName, null);
            return;
        }

        if (!string.IsNullOrEmpty(currentPic.categories[currentCategoryIndex].textureoverride)) pName = currentPic.categories[currentCategoryIndex].textureoverride;
        panoramaSphereController.SetApperance(pName, currentPic.categories[currentCategoryIndex]);
    }
    private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
    private string getConvertedTime(double unixTimeStamp)
    {
        return UnixTimeStampToDateTime(unixTimeStamp).ToString(Config.timeformat);
    }

    //private char IndexToChar(int index)
    //{
    //    if (index < 0 || index >= digits.Length) throw new ArgumentOutOfRangeException("index");
    //    return digits[index];
    //}
}
