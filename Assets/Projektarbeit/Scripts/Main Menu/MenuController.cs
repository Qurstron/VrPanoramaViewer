using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using JSONClasses;

public class MenuController : MonoBehaviour
{
    //public StateControler stateControler;
    [SerializeField] private FileManager fileManager;
    [SerializeField] private ProjectEditor projectEditor;
    [SerializeField] private GameObject dataSourcePrefab;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private GameObject serverErrorPrefab;
    [SerializeField] private GameObject localRoot;

    // TODO: load this from a config
    public Server[] servers = { new("Server", "http://localhost:7206/") }; // serverName, serverUrl

    private List<GameObject> buttons = new();
    private TaskScheduler scheduler;

    private void Start()
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

                StartCoroutine(fileManager.LoadOverview(server.url + "Content/Overview",
                    panoramas => { AddServerButtons(serverRoot, server, panoramas); },
                    () => { DisplayConnectionProblem(serverRoot, server); })
                );
            };

            serverDataSource.GetComponentInChildren<TMP_Text>().text = $"{server.name} ({uri.Host})";
            serverDataSource.GetComponentInChildren<Button>().onClick.AddListener(reload);

            reload();
        }

        scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        fileManager.GetLocalPanoramasInDirectory(Application.persistentDataPath).ContinueWith(task =>
        {
            AddLocalButtons(task.Result);
        }, scheduler);
    }

    private void AddServerButtons(GameObject serverRoot, Server server, PanoramaMenuEntry[] panoramas)
    {
        foreach (var panorama in panoramas)
        {
            GameObject button = Instantiate(buttonPrefab, serverRoot.transform);
            buttons.Add(button);
            button.GetComponentInChildren<TextMeshProUGUI>().text = panorama.config.name;
            button.GetComponentInChildren<Button>().onClick.AddListener(() => 
            {
                Debug.Log(server.url + "Content/" + panorama.config.name);
                StartCoroutine(fileManager.SavePanorama(server.url + "Content/files/" + panorama.config.name, panorama.config.name, () =>
                {
                    AddLocalButton(panorama);
                }));
            });
            StartCoroutine(fileManager.LoadThumbnail(server.url + "Content/thumbnails/" + panorama.config.name, (tex) =>
            {
                button.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            }));
        }
    }
    private void AddLocalButtons(List<PanoramaMenuEntry> panoramas)
    {
        foreach (var panorama in panoramas)
        {
            GameObject button = Instantiate(buttonPrefab, localRoot.transform);
            
            buttons.Add(button);
            button.GetComponentInChildren<TextMeshProUGUI>().text = panorama.config.name;
            button.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                LoadingAnimation loadAnim = button.GetComponentInChildren<LoadingAnimation>();
                loadAnim.StartAnimation();
                
                fileManager.LoadLocalPanorama(panorama.config).ContinueWith(task =>
                {
                    loadAnim.StopAnimation();
                    if (task.Result == null)
                    {
                        loadAnim.ErrorAnimation();
                        return;
                    }
                    projectEditor.SetConfig(task.Result);
                }, scheduler);
            });
            StartCoroutine(fileManager.GetLocalThumbnail(panorama.config.path, (tex) =>
            {
                button.GetComponentInChildren<Image>().sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
            }));
        }
    }
    private void AddLocalButton(PanoramaMenuEntry panorama)
    {
        AddLocalButtons(new() { panorama });
    }
    private void DisplayConnectionProblem(GameObject serverRoot, Server server)
    {
        GameObject error = Instantiate(serverErrorPrefab, serverRoot.transform);
        error.GetComponent<TMP_Text>().text = $"unable to load overview from {server.url}";
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
