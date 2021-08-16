using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robot : MonoBehaviour {

    #region Variables

    private Transform BS;
    [HideInInspector] public GameObject DB;
    [HideInInspector] public float TotalDist;
    [HideInInspector] public float LifeTime;
    [HideInInspector] public float MeanSpeed;
    /*float totalWheelSpeed;
    int loopCount = 0;*/

    [Header("Movements")]
    public float maxRobotSpeed;
    float maxWheelSpeed;
    [HideInInspector] public float wheel1Speed, wheel2Speed;
    public float wheelDiam;
    float wheelRadius, wheelPerim;
    public float wheelDistance;
    Vector2 wheel1, wheel2;
    private Vector3 PrevPos;
    private Vector3 CurrentPos;

    [Space(10)]

    [Header("Sensors")]
    public Transform[] sensors;
    public float minObstacleDist;
    public float maxObstacleDist;
    [Space(10)]
    public int tempo;

    [Header("Brain")]
    public bool enableBrain = true;
    [Space(10)]
    public int input;
    public int hidden;
    public int output;
    public NeuralNetwork brain;

    [HideInInspector] public long score;
    [HideInInspector] public bool alive, finished, doScore;


    [HideInInspector] public AppManager manager;

 
    #endregion

    void Awake(){
        //manager = GameObject.Find("AppManager").GetComponent<AppManager>();
        score = 1;
        doScore = true;

        foreach (Transform child in transform)
        {
            if (child.name == "BodySprite")
            {
                BS = child;
            }
            if (child.name == "DetectionBox")
            {
                DB = child.gameObject;
            }
        }
    }

    void Start () {
        alive = true;
        finished = false;
        CurrentPos = transform.position;
        PrevPos = transform.position;
        TotalDist = 0.0f;
        DB.SetActive(false);

        if (brain == null){
            // On initialise le cerveau
            brain = new NeuralNetwork(input, hidden, output);
        }
        
        // On calcul les valeurs pour la prédiction de la trajectoire
        wheelRadius = wheelDiam / 2f;
        wheelPerim = wheelRadius * 2f * Mathf.PI;
        // calcul vitesse max roues
        float Nmax = maxRobotSpeed / wheelPerim; // N tours/s
        maxWheelSpeed = Nmax * Mathf.PI * 2f; // vitesse de rotation des roues en rad/s 

        // On fait "tourner" les roues à leurs vitesses max (en rad/s)
        wheel1Speed = maxWheelSpeed;
        wheel2Speed = maxWheelSpeed;
  

        StartCoroutine("Loop");
    }

	void Update () {
        if(alive && !manager.IsPaused){

            // détermination des coordonnées des roues dans le "repère monde" à partir de leur coordonnées dans le repère "robot"
            Vector2 pW1 = transform.position + transform.right * wheelDistance / 2f; // point (roue) 1 du robot (voire sur vrai robot)
            Vector2 pW2 = transform.position - transform.right * wheelDistance / 2f; // point (roue) 2 du robot (voire sur vrai robot)

            // Vecteurs vitesse
            Vector2 vspeed1 = transform.up * wheel1Speed * wheelRadius * Time.deltaTime;
            Vector2 vspeed2 = transform.up * wheel2Speed * wheelRadius * Time.deltaTime;

            // points déduis des vecteurs vitesses
            Vector2 pA = pW1 + vspeed1;
            Vector2 pB = pW2 + vspeed2;

            if(vspeed1 != vspeed2){ // si les deux roues n'ont pas la même vitesse

                // doite d1 : y = a1 * x + b1
                // droite passant par le centre des deux roues
                float a1 = (pW1.y - pW2.y) / (pW1.x - pW2.x);
                float b1 = pW1.y - a1 * pW1.x;
                // doite d2 : y = a2 * x + b2
                // droite déduite des vecteurs vitesse (passant par A et B)
                float a2 = (pA.y - pB.y) / (pA.x - pB.x);
                float b2 = pA.y - a2 * pA.x;

                // centre de rotation cinématique
                Vector2 pO = new Vector2();
                pO.x = (b2 - b1) / (a1 - a2);
                pO.y = a1 * pO.x + b1;

                // dimensions du triangle rectangle (par rapport à la roue 1)
                float adjSide = Vector2.Distance(pW1, pO); // longueur du côté adjacent à l'angle
                float opSide = vspeed1.magnitude; // longueur du côté opposé à l'angle ( = norme du vecteur vitesse)

                float angle = Mathf.Atan(opSide / adjSide); // angle de rotation du robot (en radians)

                // On fait finalement tourner le robot autour du point (dan sun sens ou dans l'autre en fonction de la rotation des roues)
                if(wheel1Speed > wheel2Speed){
                    transform.RotateAround(pO, Vector3.forward, angle * Mathf.Rad2Deg);
                } else {
                    transform.RotateAround(pO, -Vector3.forward, angle * Mathf.Rad2Deg);
                }
            } else {
                transform.position += new Vector3(vspeed1.x, vspeed1.y, 0f);
            }

            CurrentPos = transform.position;
            float d = Vector3.Distance(PrevPos, CurrentPos);
            TotalDist += d;
            PrevPos = transform.position;

            LifeTime += Time.deltaTime;

            CalcultateScore();
        }
        else if (alive && manager.ga.oneFinished == true)
        {
            DB.SetActive(true);
        }
    }

    /*float map(float x, float in_min, float in_max, float out_min, float out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }*/


    float Dist(Transform sensor) // fonction permettant la mesure de distance à un obstacle à partir d'un "capteur virtuel"
    {
        Ray2D SRay = new Ray2D(sensor.transform.position, sensor.transform.up);

        float d = 1f;
        RaycastHit2D hit = Physics2D.Raycast(SRay.origin, SRay.direction, maxObstacleDist, ~( (1 << 1) | (1 << 2) ));

        if (hit.collider != null)
        {
            // on map la valeur entre 0 et 1
            d = (hit.distance - minObstacleDist) / (maxObstacleDist - minObstacleDist);
            d = Mathf.Clamp01(d);

            Debug.DrawRay(SRay.origin, SRay.direction * maxObstacleDist, Color.black);
        } else {
            Debug.DrawRay(SRay.origin, SRay.direction * maxObstacleDist, Color.yellow);
        }

        return d;
    }


    IEnumerator Loop()
    {
        while (true)
        {   
            if(!alive){
                break;
            }
            
            if(manager.IsPaused){
                yield return new WaitUntil(() => !manager.IsPaused);
            }

            float[,] input = new float[sensors.Length, 1]; // tableau pour stocker l'input au nn

            // On mesure les distances aux obstacles
            input[0, 0] = Dist(sensors[0]);
            input[1, 0] = Dist(sensors[1]);
            input[2, 0] = Dist(sensors[2]);

            if(enableBrain){
                float[,] output = brain.Predict(input); // on récupère la prédiction de vitesse avec les distances mesurées
                
                float acc1 = output[0,0];
                float acc2 = output[1,0];
                wheel1Speed += acc1 * maxWheelSpeed;
                wheel2Speed += acc2 * maxWheelSpeed;
                wheel1Speed = Mathf.Clamp(wheel1Speed, 0.01f, maxWheelSpeed);
                wheel2Speed = Mathf.Clamp(wheel2Speed, 0.01f, maxWheelSpeed);

                if(wheel1Speed < 0.025f && wheel2Speed < 0.025f){
                    SetDead();
                }
            }

            yield return new WaitForSeconds(tempo/1000);
        }
    }
    

    void CalcultateScore(){ // Calcul du score du robot
        if (doScore)
        {
            float DistToStart = Vector2.Distance(manager.ga.spawnPoint.position, transform.position);
            MeanSpeed = TotalDist / LifeTime;

            //double fscore = (double)DistToStart * (double)TotalDist * (double)MeanSpeed;
            double fscore = (5 * (double)DistToStart + 15 * (double)TotalDist) * (double)MeanSpeed;
            
            score = (long)fscore;
            //score *= score;
            score = score * score * score;
            score /= 100;

            if (!alive)
            {
                score /= 10;
                
                doScore = false;

                if(score <= 0){
                    score = 1;
                } 
            }
        }
    }


    public void SetDead(){
        alive = false;
        BS.GetComponent<SpriteRenderer>().color = manager.DeadBot;
    }

    public void SetBest(){
        BS.GetComponent<SpriteRenderer>().color = manager.BestBot;
    }

    public void UnsetBest(){
        BS.GetComponent<SpriteRenderer>().color = manager.BaseBot;
    }

    public void SetSelected()
    {
        BS.GetComponent<SpriteRenderer>().color = manager.SelectedBot;
    }

    void OnTriggerEnter2D(Collider2D other){
        if(other.CompareTag("Wall")){
            SetDead();
        }
        if(other.CompareTag("Goal")){
            finished = true;
        }
    }
}