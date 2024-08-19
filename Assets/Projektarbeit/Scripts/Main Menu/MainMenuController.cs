using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static JSONClasses;

public class MainMenuController : MonoBehaviour
{
    public StateControler stateControler;
    public FileManager fileManager;
    public GameObject floppyDiskPrefab;

    public Transform pos;

    //public GameObject dataSourcePrefab;
    //public GameObject buttonPrefab;
    //public GameObject serverErrorPrefab;
    //public GameObject localRoot;

    // TODO: load this from a config
    public Server[] servers = { new("Server", "http://localhost:7206/") }; // serverName, serverUrl

    //private List<GameObject> buttons = new();
    //private TaskScheduler scheduler;

    void Start()
    {
        foreach (var server in servers)
        {
            if (!server.url.EndsWith('/')) server.url += "/";

            //GameObject serverDataSource = Instantiate(dataSourcePrefab, transform);
            //GameObject serverRoot = serverDataSource.transform.Find("Scroll View Server/Viewport/Content").gameObject;
            //Uri uri = new Uri(server.url);
            //UnityAction reload = () =>
            //{
            //    foreach (Transform child in serverRoot.transform)
            //    {
            //        Destroy(child.gameObject);
            //    }

            //    StartCoroutine(fileManager.LoadOverview(server.url + "Content/Overview",
            //        panoramas => { AddServerButtons(serverRoot, server, panoramas); },
            //        () => { DisplayConnectionProblem(serverRoot, server); })
            //    );
            //};

            //serverDataSource.GetComponentInChildren<TMP_Text>().text = $"{server.name} ({uri.Host})";
            //serverDataSource.GetComponentInChildren<Button>().onClick.AddListener(reload);

            //reload();

            //StartCoroutine(fileManager.LoadOverview(server.url + "Content/Overview",
            //    panoramas => { AddServerButtons(serverRoot, server, panoramas); },
            //    () => { DisplayConnectionProblem(serverRoot, server); })
            //);
        }

        SpawnFloppies(pos, fileManager.GetLocalPanoramas().ToArray());

        //AddLocalButtons(fileManager.GetLocalPanoramas().ToArray());
        //scheduler = TaskScheduler.FromCurrentSynchronizationContext();
    }

    private void SpawnFloppies(Transform pos, PanoramaMenuEntry[] panoramas)
    {
        foreach (var panorama in panoramas)
        {
            GameObject floppyObject = Instantiate(floppyDiskPrefab, pos);
            FloppyDisk floppy = floppyObject.GetComponent<FloppyDisk>();

            floppy.PanoramaName = panorama.name;
            StartCoroutine(fileManager.GetLocalThumbnail(panorama.name, tex =>
            {
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                floppy.SetThumbnail(sprite);
            }));
        }
    }

    //private void AddServerButtons(GameObject serverRoot, Server server, PanoramaMenuEntry[] panoramas)
    //{
    //    foreach (var panorama in panoramas)
    //    {
    //        GameObject button = Instantiate(buttonPrefab, serverRoot.transform);
    //        buttons.Add(button);
    //        button.GetComponentInChildren<TextMeshProUGUI>().text = panorama.name;
    //        button.GetComponentInChildren<Button>().onClick.AddListener(() =>
    //        {
    //            Debug.Log(server.url + "Content/" + panorama.name);
    //            StartCoroutine(fileManager.SavePanorama(server.url + "Content/files/" + panorama.name, panorama.name, () =>
    //            {
    //                AddLocalButton(panorama);
    //            }));
    //        });
    //        StartCoroutine(fileManager.LoadThumbnail(server.url + "Content/thumbnails/" + panorama.name, (tex) =>
    //        {
    //            button.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
    //        }));
    //    }
    //}
    //private void AddLocalButtons(PanoramaMenuEntry[] panoramas)
    //{
    //    foreach (var panorama in panoramas)
    //    {
    //        GameObject button = Instantiate(buttonPrefab, localRoot.transform);

    //        buttons.Add(button);
    //        button.GetComponentInChildren<TextMeshProUGUI>().text = panorama.name;
    //        button.GetComponentInChildren<Button>().onClick.AddListener(() =>
    //        {
    //            LoadingAnimation loadAnim = button.GetComponentInChildren<LoadingAnimation>();
    //            loadAnim.StartAnimation();

    //            fileManager.LoadLocalPanorama(panorama.name).ContinueWith(task =>
    //            {
    //                loadAnim.StopAnimation();
    //                if (task.Result == null)
    //                {
    //                    loadAnim.ErrorAnimation();
    //                    return;
    //                }
    //                stateControler.ToggleMenu();
    //            }, scheduler);
    //        });
    //        StartCoroutine(fileManager.GetLocalThumbnail(panorama.name, (tex) =>
    //        {
    //            button.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
    //        }));
    //    }
    //}
    //private void AddLocalButton(PanoramaMenuEntry panorama)
    //{
    //    AddLocalButtons(new PanoramaMenuEntry[] { panorama });
    //}
    //private void DisplayConnectionProblem(GameObject serverRoot, Server server)
    //{
    //    GameObject error = Instantiate(serverErrorPrefab, serverRoot.transform);
    //    error.GetComponent<TMP_Text>().text = $"unable to load overview from {server.url}";
    //}

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
