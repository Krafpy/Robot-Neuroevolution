using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneticAlgorithm : MonoBehaviour {

	AppManager manager;

	public GameObject robot;
    [HideInInspector] public List<Robot> population;
    [HideInInspector] public int populationSize;
    float mutationRate;
    [HideInInspector] public bool oneFinished;

    public Transform spawnPoint;
    public Transform endPoint;

    [HideInInspector] public Robot bestRobot;

	bool simulating = false;
	
	[Space(10)]
	public Text generationText;
    [HideInInspector] public int generation;

    public Text BSTxt;

    private Stats stats;

	[HideInInspector] public bool dontAutoNext = false;

	void Awake() {
		manager = GetComponent<AppManager>();
        stats = GetComponent<Stats>();
    }
	
	void Update () {
		if(!dontAutoNext){
			if (simulating && !manager.IsPaused){
				long bestScore = 0;
				int deadRobots = 0;
				oneFinished = false;
				bestRobot.UnsetBest();

				foreach(Robot rob in population){
					if(rob.score > bestScore){
						bestRobot = rob;
						bestScore = rob.score;
						BSTxt.text = "Best score\n" + bestScore.ToString();
					}
					if(!rob.alive){
						rob.SetDead();
						deadRobots++;
					}
					if(rob.finished){
						oneFinished = true;
					}
				}
				manager.camController.Target = bestRobot.gameObject;
				
				if(manager.IsPaused == false)
				{
					bestRobot.SetBest();
				}

				if (oneFinished)
				{
					manager.PauseButton.SetActive(false);
					manager.NextButton.SetActive(true);
                    manager.SelectedRob = bestRobot;
                    manager.SelectedRob.SetSelected();
                    manager.HasTakenBot = true;
                    manager.Pause();
					stats.SaveGenerationStats();
				}
				else if (deadRobots == populationSize || Input.GetKeyDown(KeyCode.Return))
				{
					stats.SaveGenerationStats();
					NextGeneration();
				}
			}
		} else {
			dontAutoNext = false;
		}
    }


	public void NextGeneration(){

		List<Robot> childPopulation = new List<Robot>(); // on crée les enfants


		// on fait uen copie du meilleur robot
		/*GameObject bestRobCopy = Instantiate(robot, spawnPoint.position, Quaternion.identity);
		bestRobCopy.name = "Robot 0";
		Robot copyRobot = bestRobCopy.GetComponent<Robot>();
		copyRobot.brain = bestRobot.brain;
		copyRobot.manager = manager;

		childPopulation.Add(copyRobot); // on l'ajoute immédiatement*/


		for(int n = 0; n < populationSize; n++){ // nouveaux robots enfants (N-1)
			// Sélection naturelle
			Robot parentA = PickOne();
			Robot parentB = PickOne();

			GameObject child = Instantiate(robot, spawnPoint.position, Quaternion.identity);
			child.name = "Robot " + n.ToString();
			Robot childRobot = child.GetComponent<Robot>();
			// Croisement
			childRobot.brain = NeuralNetwork.Crossover(parentA.brain, parentB.brain);
			childRobot.brain.Mutate(mutationRate);

			childRobot.manager = manager;

			childPopulation.Add(childRobot);
		}

        KillPopulation();

		foreach(Robot crob in childPopulation){
			population.Add(crob);
		}
		bestRobot = population[0];

		generation++;
		generationText.text = "Generation n°" + generation.ToString();
    }

	Robot PickOne(){
		int hack = 0;
		while(hack < 10000){
			int index = Random.Range(0, population.Count);
			Robot rob = population[index];

            long r = (long)((double)Random.value * bestRobot.score);

			if(r < rob.score){
				return rob;
			}
			hack++;
		}
		return population[0];
	}

	void KillPopulation(){
		// On détruit la population actuelle
		foreach(Robot rob in population){
			Destroy(rob.gameObject);
		}
		population.Clear();
	}

	void OnEnable(){
		// On crée une nouvelle population
		populationSize = manager.Pop;
		mutationRate = manager.Mut / 100f;

		population = new List<Robot>();

		for(int n = 0; n < populationSize; n++){
			GameObject rob = Instantiate(robot, spawnPoint.position, Quaternion.identity);
			rob.name = "Robot " + n.ToString();
			population.Add(rob.GetComponent<Robot>());
			population[n].manager = manager;
		}
		
		bestRobot = population[0];

		simulating = true;
		generation = 0;
        generationText.text = "Generation n°" + generation.ToString();

        stats.NewStatFile();
    }

	void OnDisable(){
		KillPopulation();

		simulating = false;
	}
}