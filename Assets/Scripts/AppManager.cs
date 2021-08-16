using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AppManager : MonoBehaviour {

    #region Variables

    public GameObject StartPanel;
    public GameObject TrainPanel;

    public Button UploadButton;
    public Button SaveButton;
    public GameObject PauseButton;
    public GameObject NextButton;

    [HideInInspector] public CameraController camController;
    [HideInInspector] public Camera cam;

    private Stats stats;

    [Header("Inputs")]
    public InputField PopInput;
    
    [Header("Texts")]
    //public Text PopInputTxt;
    public Text MutTxt;
    public Text TSTxt;

    [Header("Sliders")]
    public Slider MutValue;
    public Slider TSValue;

    [Header("Bools")]
    public bool IsPaused;
    public bool Simulate;
    public bool HasTakenBot;

    [Header("Populations")]
    public int Pop;
    public int PopMax;
    public int PopMin;

    [Header("Mutations")]
    public float Mut;
    public float MutMax;
    public float MutMin;
    public float stdDev;

    [Header("Time")]
    public float TimeScale;
    public float TSMax;
    public float TSMin;

    [Header("Robots")]
    [HideInInspector] public Robot SelectedRob;
    public Color DeadBot;
    public Color BestBot;
    public Color BaseBot;
    public Color SelectedBot;

    [HideInInspector] public GeneticAlgorithm ga;

    Arduino arduino;

    #endregion

    void Awake() {
        Physics2D.IgnoreLayerCollision(1, 1, true);
    }
    
    void Start () {
        camController = GameObject.Find("AICamera").GetComponent<CameraController>();
        cam = GameObject.Find("AICamera").GetComponent<Camera>();
        ga = GetComponent<GeneticAlgorithm>();
        arduino = GetComponent<Arduino>();
        stats = gameObject.GetComponent<Stats>();

        MutValue.minValue = MutMin;
        MutValue.maxValue = MutMax;
        MutValue.value = Mut;

        Matrix.std = stdDev;

        TimeScale = 1f;
        TSValue.minValue = TSMin;
        TSValue.maxValue = TSMax;
        TSValue.value = TimeScale;

        //PopInputTxt.text = Pop.ToString();
        PopInput.text = Pop.ToString();

        UploadButton.interactable = false;
        SaveButton.interactable = false;
    }
	
	void Update () {
        Time.timeScale = TimeScale;
        if (Simulate == true)
        {
            TimeScale = Mathf.Round(TSValue.value * 10f) * 0.1f;
            TSTxt.text = "Time x" + TimeScale;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            //Debug.DrawRay(ray.origin, ray.direction * 30, Color.blue);

            if (ga.oneFinished == true)
            {
                if (Input.GetMouseButtonDown(0))
                {

                    RaycastHit hit;

                    if (Physics.Raycast(ray.origin, ray.direction, out hit, 30))
                    {
                        if (hit.transform.parent.transform.gameObject.tag == "Robot")
                        {
                            HasTakenBot = false;
                            foreach (Robot rob in ga.population)
                            {
                                if (rob.alive == true)
                                {
                                    if (rob != ga.bestRobot)
                                    {
                                        rob.UnsetBest();
                                    }
                                    else
                                    {
                                        rob.SetBest();
                                    }
                                }
                            }
                            SelectedRob = hit.transform.parent.transform.gameObject.GetComponent<Robot>();
                            SelectedRob.SetSelected();
                            HasTakenBot = true;
                        }
                    }
                }

                if (HasTakenBot == true)
                {
                    UploadButton.interactable = true;
                }
                else
                {
                    UploadButton.interactable = false;
                }
            }
            else
            {
                UploadButton.interactable = false;
            }
        }
        else if (Simulate == false)
        {
            Mut = Mathf.Round(MutValue.value * 100f) * 0.01f;
            MutTxt.text = "Mut rate : " + Mut + "%";
        }
    }

    public void Population ()
    {
        Pop = int.Parse(PopInput.text);
        Pop = Mathf.Clamp(Pop, PopMin, PopMax);
    }

    public void Play ()
    {
        if(IsPaused){
            Pause();
        }
        
        StartPanel.SetActive(false);
        TrainPanel.SetActive(true);
        Simulate = true;
        ga.enabled = true;
    }

    public void Stop ()
    {
        if(IsPaused){
            Pause();
        }
        
        StartPanel.SetActive(true);
        TrainPanel.SetActive(false);
        Simulate = false;
        ga.enabled = false;
        TimeScale = 1f;
        TSValue.value = TSValue.minValue;

        PauseButton.SetActive(true);
        NextButton.SetActive(false);
    }

    public void Pause ()
    {

        IsPaused = !IsPaused;

        if (IsPaused == false)
        {
            Time.timeScale = 1f;
            SaveButton.interactable = false;
        }
        else if (IsPaused == true)
        {
            Time.timeScale = 0f;
            if (stats.FileName != "")
            {
                SaveButton.interactable = true;
            }
        }
    }

    public void Continue ()
    {
        ga.dontAutoNext = true;
        ga.NextGeneration();
        PauseButton.SetActive(true);
        NextButton.SetActive(false);
        Pause();
    }

    public void Exit ()
    {
        Application.Quit();
    }

    public void Upload (){
        arduino.TransferData(SelectedRob.brain.toDataArray());
    }
}