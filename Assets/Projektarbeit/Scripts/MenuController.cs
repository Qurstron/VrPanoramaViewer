using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.UI;
using static JSONClasses;

public class MenuController : MonoBehaviour
{
    public StateControler stateControler;
    public DownloadTexture downloader;
    public GameObject dataSourcePrefab;
    public GameObject buttonPrefab;
    public GameObject serverErrorPrefab;
    public GameObject localRoot;
    //public GameObject serverRoot;
    // TODO: load this from a config
    public Server[] servers = { new("Server", "http://localhost:7206/") }; // serverName, serverUrl
    //public string serverUrl = "http://localhost:7206/";

    private List<GameObject> buttons = new();
    //private List<PanoramaMenuEntry> localPanoramas = new();
    private TaskScheduler scheduler;
    //private string contentPath = "/Content";

    void Start()
    {
        foreach (var server in servers)
        {
            if (!server.url.EndsWith('/')) server.url += "/";

            GameObject serverDataSource = Instantiate(dataSourcePrefab, transform);
            GameObject serverRoot = serverDataSource.transform.Find("Scroll View Server/Viewport/Content").gameObject;
            Uri uri = new Uri(server.url);
            UnityAction reload = () =>
            {
                foreach (Transform child in serverRoot.transform)
                {
                    Destroy(child.gameObject);
                }

                StartCoroutine(downloader.LoadOverview(server.url + "Content/Overview",
                    panoramas => { AddServerButtons(serverRoot, server, panoramas); },
                    () => { DisplayConnectionProblem(serverRoot, server); })
                );
            };

            serverDataSource.GetComponentInChildren<TMP_Text>().text = $"{server.name} ({uri.Host})";
            serverDataSource.GetComponentInChildren<Button>().onClick.AddListener(reload);

            //StartCoroutine(downloader.LoadOverview(server.url + "Content/Overview",
            //    panoramas => { AddServerButtons(serverRoot, server, panoramas); },
            //    () => { DisplayConnectionProblem(serverRoot, server); })
            //);
            reload();
        }
        //if (!serverUrl.EndsWith('/')) serverUrl += "/";

        //StartCoroutine(downloader.LoadOverview(serverUrl + "Content/Overview", AddServerButtons, DisplayConnectionProblem));
        AddLocalButtons(downloader.GetLocalPanoramas().ToArray());
        scheduler = TaskScheduler.FromCurrentSynchronizationContext();
    }

    private void AddServerButtons(GameObject serverRoot, Server server, PanoramaMenuEntry[] panoramas)
    {
        foreach (var panorama in panoramas)
        {
            GameObject button = Instantiate(buttonPrefab, serverRoot.transform);
            buttons.Add(button);
            button.GetComponentInChildren<TextMeshProUGUI>().text = panorama.name;
            button.GetComponentInChildren<Button>().onClick.AddListener(() => 
            {
                Debug.Log(server.url + "Content/" + panorama.name);
                StartCoroutine(downloader.SavePanorama(server.url + "Content/files/" + panorama.name, panorama.name, (d) =>
                {
                    AddLocalButton(panorama);
                }));
            });
            StartCoroutine(downloader.LoadThumbnail(server.url + "Content/thumbnails/" + panorama.name, (tex) =>
            {
                button.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            }));
        }
    }
    private void AddLocalButtons(PanoramaMenuEntry[] panoramas)
    {
        foreach (var panorama in panoramas)
        {
            GameObject button = Instantiate(buttonPrefab, localRoot.transform);
            
            buttons.Add(button);
            button.GetComponentInChildren<TextMeshProUGUI>().text = panorama.name;
            button.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                downloader.LoadLocalPanorama(panorama.name).ContinueWith(task =>
                {
                    stateControler.ToggleMenu();
                }, scheduler);
            });
            StartCoroutine(downloader.GetLocalThumbnail(panorama.name, (tex) =>
            {
                button.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            }));
        }
    }
    private void AddLocalButton(PanoramaMenuEntry panorama)
    {
        AddLocalButtons(new PanoramaMenuEntry[] { panorama });
    }
    private void DisplayConnectionProblem(GameObject serverRoot, Server server)
    {
        Debug.Log($"unable to load overview from {server.url}");
        var error = Instantiate(serverErrorPrefab, serverRoot.transform);
        error.GetComponent<TMP_Text>().text = $"unable to load overview from {server.url}";
        //error.GetComponent<TMP_Text>().text = $"unable to connect to {Application.persistentDataPath}";
    }

    public class Server
    {
        public string name;
        public string url;

        public Server(string name, string url)
        {
            this.name = name;
            this.url = url;
        }
    }
}
